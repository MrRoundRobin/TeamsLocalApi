using System.ComponentModel;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ro.TeamsLocalApi;

public class Client : INotifyPropertyChanged
{
    public bool IsMuted { get => isMuted; set { _ = SendCommand(value ? MeetingAction.Mute : MeetingAction.Unmute); } }
    public bool IsCameraOn { get => isCameraOn; set { _ = SendCommand(value ? MeetingAction.ShowVideo : MeetingAction.HideVideo); } }
    public bool IsHandRaised { get => isHandRaised; set { _ = SendCommand(value ? MeetingAction.RaiseHand : MeetingAction.LowerHand); } }
    public bool IsInMeeting { get => isInMeeting; }
    public bool IsRecordingOn { get => isRecordingOn; set { _ = SendCommand(value ? MeetingAction.StartRecording : MeetingAction.StopRecording); } }
    public bool IsBackgroundBlurred { get => isBackgroundBlurred; set { _ = SendCommand(value ? MeetingAction.BlurBackground : MeetingAction.UnblurBackground); } }
    public bool IsConnected => ws is not null && ws.State == WebSocketState.Open;

    public bool CanToggleMute { get; private set; }
    public bool CanToggleVideo { get; private set; }
    public bool CanToggleHand { get; private set; }
    public bool CanToggleBlur { get; private set; }
    public bool CanToggleRecord { get; private set; }
    public bool CanLeave { get; private set; }
    public bool CanReact { get; private set; }

    public string Manufacturer { get; set; } = "Elgato";
    public string Device { get; set; } = "Stream Deck";

    private bool isMuted;
    private bool isCameraOn;
    private bool isHandRaised;
    private bool isInMeeting;
    private bool isRecordingOn;
    private bool isBackgroundBlurred;

    private static readonly SemaphoreSlim sendSemaphore    = new(0, 1);
    private static readonly SemaphoreSlim receiveSemaphore = new(0, 1);

    private readonly Uri Uri;
    private ClientWebSocket? ws;

    public event EventHandler? Disconnected;
    public event EventHandler? Connected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Client(string token, bool autoConnect = false)
    {
        Uri = new($"ws://localhost:8124?token={token}");

        if (autoConnect)
            _ = Connect();
    }

    public async Task Disconnect()
    {
        if (ws is null) return;

        var initialState = ws.State; 

        switch (ws.State)
        {
            case WebSocketState.Open:
            case WebSocketState.Connecting:
                await ws!.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
                break;
            case WebSocketState.CloseReceived:
            case WebSocketState.CloseSent:
                await WaitClose();
                break;
            case WebSocketState.Aborted:
            case WebSocketState.Closed:
            case WebSocketState.None:
                break;
        }

        ws.Dispose();
        ws = null;

        if (initialState != WebSocketState.Closed)
            Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public async Task Reconnect()
    {
        await Disconnect();
        await Connect();
    }

    private async Task WaitClose()
    {
        if (ws is null) return;

        while (ws.State != WebSocketState.Closed && ws.State != WebSocketState.Aborted)
            await Task.Delay(25);
    }

    private async Task WaitOpen()
    {
        if (ws is null) throw new InvalidOperationException();

        while (ws.State == WebSocketState.Connecting)
            await Task.Delay(25);
    }

    private async void Receive()
    {
        if (ws is null || ws.State != WebSocketState.Open)
            throw new InvalidOperationException("Not connected");

        try
        {
            await receiveSemaphore.WaitAsync();

            var buffer = new byte[1024];
            
            while (true)
            {
                var result = await ws.ReceiveAsync(buffer, default);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (ws.CloseStatus != WebSocketCloseStatus.NormalClosure)
                    {
                        Disconnected?.Invoke(this, EventArgs.Empty);
                        _ = Reconnect();
                    }

                    return;
                }

                if (!result.EndOfMessage || result.Count == 0)
                    throw new NotImplementedException("Invalid Message received");

                var data = Encoding.UTF8.GetString(buffer, 0, result.Count);

                var message = JsonSerializer.Deserialize<ServerMessage>(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? throw new InvalidDataException();

                if (!string.IsNullOrEmpty(message.ErrorMsg))
                {
                    if (message.ErrorMsg.EndsWith("no active call"))
                        continue;

                    throw new Exception(message.ErrorMsg);
                }

                if (message.MeetingUpdate is null)
                    continue;

                if (isMuted != message.MeetingUpdate.MeetingState.IsMuted)
                {
                    isMuted = message.MeetingUpdate.MeetingState.IsMuted;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMuted)));
                }

                if (isCameraOn != message.MeetingUpdate.MeetingState.IsCameraOn)
                {
                    isCameraOn = message.MeetingUpdate.MeetingState.IsCameraOn;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCameraOn)));
                }

                if (isHandRaised != message.MeetingUpdate.MeetingState.IsHandRaised)
                {
                    isHandRaised = message.MeetingUpdate.MeetingState.IsHandRaised;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHandRaised)));
                }

                if (isInMeeting != message.MeetingUpdate.MeetingState.IsInMeeting)
                {
                    isInMeeting = message.MeetingUpdate.MeetingState.IsInMeeting;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInMeeting)));
                }

                if (isRecordingOn != message.MeetingUpdate.MeetingState.IsRecordingOn)
                {
                    isRecordingOn = message.MeetingUpdate.MeetingState.IsRecordingOn;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRecordingOn)));
                }

                if (isBackgroundBlurred != message.MeetingUpdate.MeetingState.IsBackgroundBlurred)
                {
                    isBackgroundBlurred = message.MeetingUpdate.MeetingState.IsBackgroundBlurred;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBackgroundBlurred)));
                }

                if (CanToggleMute != message.MeetingUpdate.MeetingPermissions.CanToggleMute)
                {
                    CanToggleMute = message.MeetingUpdate.MeetingPermissions.CanToggleMute;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanToggleMute)));
                }

                if (CanToggleVideo != message.MeetingUpdate.MeetingPermissions.CanToggleVideo)
                {
                    CanToggleVideo = message.MeetingUpdate.MeetingPermissions.CanToggleVideo;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanToggleVideo)));
                }

                if (CanToggleHand != message.MeetingUpdate.MeetingPermissions.CanToggleHand)
                {
                    CanToggleHand = message.MeetingUpdate.MeetingPermissions.CanToggleHand;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanToggleHand)));
                }

                if (CanToggleBlur != message.MeetingUpdate.MeetingPermissions.CanToggleBlur)
                {
                    CanToggleBlur = message.MeetingUpdate.MeetingPermissions.CanToggleBlur;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanToggleBlur)));
                }

                if (CanToggleRecord != message.MeetingUpdate.MeetingPermissions.CanToggleRecord)
                {
                    CanToggleRecord = message.MeetingUpdate.MeetingPermissions.CanToggleRecord;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanToggleRecord)));
                }

                if (CanLeave != message.MeetingUpdate.MeetingPermissions.CanLeave)
                {
                    CanLeave = message.MeetingUpdate.MeetingPermissions.CanLeave;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanLeave)));
                }

                if (CanReact != message.MeetingUpdate.MeetingPermissions.CanReact)
                {
                    CanReact = message.MeetingUpdate.MeetingPermissions.CanReact;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanReact)));
                }
            }
        } 
        finally
        {
            receiveSemaphore.Release();
        }
    }

    public async Task Connect()
    {
        if (Process.GetProcessesByName("Teams").Length == 0)
            return;

        if (ws is not null)
        {
            switch (ws.State)
            {
                case WebSocketState.Open:
                case WebSocketState.Connecting:
                    return;
                case WebSocketState.Aborted:
                case WebSocketState.Closed:
                case WebSocketState.None:
                    await Disconnect();
                    break;
                case WebSocketState.CloseReceived:
                case WebSocketState.CloseSent:
                    await WaitClose();
                    await Disconnect();
                    break;
            }
        }

        ws = new();

        await ws.ConnectAsync(Uri, default);

        await WaitOpen();

        if (ws.State == WebSocketState.Open)
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            throw new WebSocketException($"Invalid state after connect: ${ws.State}");
        }

        Receive();
    }

    private async Task SendCommand(MeetingAction action)
    {
        if (ws is null) throw new InvalidOperationException("Not Connected");

        switch (ws.State)
        {
            case WebSocketState.Aborted:
            case WebSocketState.CloseReceived:
                await Connect();
                break;
            case WebSocketState.Connecting:
                await WaitOpen();
                break;
            case WebSocketState.CloseSent:
            case WebSocketState.Closed:
                throw new InvalidOperationException("Not Connected");
            case WebSocketState.Open:
            case WebSocketState.None:
                break;
        }

        var message = JsonSerializer.Serialize(new ClientMessage()
        {
            Action = action,
            Device = Device,
            Manufacturer = Manufacturer,
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        await sendSemaphore.WaitAsync();

        await ws.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, default);

        sendSemaphore.Release();
    }

    public async Task LeaveCall() => await SendCommand(MeetingAction.LeaveCall);
    public async Task ReactApplause() => await SendCommand(MeetingAction.ReactApplause);
    public async Task ReactLaugh() => await SendCommand(MeetingAction.ReactLaugh);
    public async Task ReactLike() => await SendCommand(MeetingAction.ReactLike);
    public async Task ReactLove() => await SendCommand(MeetingAction.ReactLove);
    public async Task ReactWow() => await SendCommand(MeetingAction.ReactWow);
    public async Task UpdateState() => await SendCommand(MeetingAction.QueryMeetingState);
}
