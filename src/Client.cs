using System.ComponentModel;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Ro.Teams.LocalApi;

public class Client : INotifyPropertyChanged
{
    public bool IsMuted { get => isMuted; set { if (value != IsMuted) _ = SendCommand(value ? MeetingAction.Mute : MeetingAction.Unmute); } }
    public bool IsCameraOn { get => isCameraOn; set { if (value != IsCameraOn) _ = SendCommand(value ? MeetingAction.ShowVideo : MeetingAction.HideVideo); } }
    public bool IsHandRaised { get => isHandRaised; set { if (value != IsHandRaised) _ = SendCommand(value ? MeetingAction.RaiseHand : MeetingAction.LowerHand); } }
    public bool IsInMeeting { get => isInMeeting; }
    public bool IsRecordingOn { get => isRecordingOn; set { if (value != isRecordingOn) _ = SendCommand(value ? MeetingAction.StartRecording : MeetingAction.StopRecording); } }
    public bool IsBackgroundBlurred { get => isBackgroundBlurred; set { if (value != IsBackgroundBlurred) _ = SendCommand(value ? MeetingAction.BlurBackground : MeetingAction.UnblurBackground); } }
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

    private readonly SemaphoreSlim sendSemaphore       = new(1);
    private readonly SemaphoreSlim receiveSemaphore    = new(1);
    private readonly SemaphoreSlim connectingSemaphore = new(1);
    private readonly bool autoConnect;

    private readonly Uri Uri;
    private ClientWebSocket? ws;

    public event EventHandler? Disconnected;
    public event EventHandler? Connected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Client(string? token = null, bool autoConnect = false, CancellationToken cancellationToken = default)
    {
        if (token is null)
        {
            var teamsStoragePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Teams\storage.json");

            if (!File.Exists(teamsStoragePath))
                throw new FileNotFoundException(null, teamsStoragePath);

            var options = JsonSerializer.Deserialize<TeamsStoragePartial>(File.ReadAllBytes(teamsStoragePath), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            if (options == null || options.EnableThirdPartyDevicesService != true || !Guid.TryParse(options.TpdApiTokenString, out Guid autoToken))
                throw new ArgumentException(null, nameof(token));

            token = autoToken.ToString();
        }

        Uri = new($"ws://localhost:8124?token={token}");

        this.autoConnect = autoConnect;

        if (autoConnect)
            _ = Connect(true, cancellationToken);
    }

    public async Task Disconnect(CancellationToken cancellationToken = default)
    {
        if (ws is null) return;

        var initialState = ws.State;

        switch (ws.State)
        {
            case WebSocketState.Open:
            case WebSocketState.Connecting:
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
                break;
            case WebSocketState.CloseSent:
                await WaitClose(cancellationToken);
                break;
            case WebSocketState.CloseReceived:
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

    public async Task Reconnect(bool waitForTeams, CancellationToken cancellationToken = default)
    {
        await Disconnect(cancellationToken);
        await Connect(waitForTeams, cancellationToken);
    }

    private async Task WaitClose(CancellationToken cancellationToken = default)
    {
        if (ws is null) return;

        while (ws is not null && ws.State != WebSocketState.Closed && ws.State != WebSocketState.Aborted)
            await Task.Delay(25, cancellationToken);
    }

    private async Task WaitOpen(CancellationToken cancellationToken = default)
    {
        if (ws is null) throw new InvalidOperationException();

        while (ws.State == WebSocketState.Connecting)
            await Task.Delay(25, cancellationToken);
    }

    private async void Receive(CancellationToken cancellationToken = default)
    {
        if (ws is null || ws.State != WebSocketState.Open)
            throw new InvalidOperationException("Not connected");

        try
        {
            await receiveSemaphore.WaitAsync(cancellationToken);

            var buffer = new byte[1024];

            while (true)
            {
                var result = await ws.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (ws.CloseStatus != WebSocketCloseStatus.NormalClosure)
                    {
                        Disconnected?.Invoke(this, EventArgs.Empty);
                        _ = Reconnect(true, cancellationToken);
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

            if (ws is not null && ws.State != WebSocketState.Open && ws.State != WebSocketState.Connecting)
                _ = Reconnect(autoConnect, cancellationToken);
        }
    }

    public async Task Connect(bool waitForTeams = true, CancellationToken cancellationToken = default)
    {
        try
        {
            await connectingSemaphore.WaitAsync(cancellationToken);

            if (Process.GetProcessesByName("Teams").Length == 0 && !waitForTeams)
                return;

            while (Process.GetProcessesByName("Teams").Length == 0)
                await Task.Delay(1000, cancellationToken);

            if (ws is not null)
            {
                switch (ws.State)
                {
                    case WebSocketState.Open:
                        return;
                    case WebSocketState.Connecting:
                        await WaitOpen(cancellationToken);
                        return;
                    case WebSocketState.Aborted:
                    case WebSocketState.Closed:
                    case WebSocketState.None:
                        await Disconnect(cancellationToken);
                        break;
                    case WebSocketState.CloseReceived:
                    case WebSocketState.CloseSent:
                        await WaitClose(cancellationToken);
                        await Disconnect(cancellationToken);
                        break;
                }
            }

            ws = new();

            await ws.ConnectAsync(Uri, cancellationToken);

            await WaitOpen(cancellationToken);

            if (ws.State != WebSocketState.Open)
                throw new WebSocketException($"Invalid state after connect: ${ws.State}");

            Connected?.Invoke(this, EventArgs.Empty);
            Receive(cancellationToken);
            await SendCommand(MeetingAction.QueryMeetingState, cancellationToken);
        }
        finally
        {
            connectingSemaphore.Release();
        }
    }

    private async Task SendCommand(MeetingAction action, CancellationToken cancellationToken = default)
    {
        if (ws is null)
        {
            if (!autoConnect)
                throw new InvalidOperationException("Not Connected");

            await Connect(true, cancellationToken);
        }

        switch (ws!.State)
        {
            case WebSocketState.Aborted:
            case WebSocketState.CloseReceived:
                await Connect(autoConnect, cancellationToken);
                break;
            case WebSocketState.Connecting:
                await WaitOpen(cancellationToken);
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

        try
        {
            await sendSemaphore.WaitAsync(cancellationToken);

            await ws.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cancellationToken);
        }
        finally
        {
            sendSemaphore.Release();
        }
    }

    public async Task LeaveCall(CancellationToken cancellationToken = default)
        => await SendCommand(MeetingAction.LeaveCall, cancellationToken);

    public async Task ReactApplause(CancellationToken cancellationToken = default)
        => await SendCommand(MeetingAction.ReactApplause, cancellationToken);

    public async Task ReactLaugh(CancellationToken cancellationToken = default)
        => await SendCommand(MeetingAction.ReactLaugh, cancellationToken);

    public async Task ReactLike(CancellationToken cancellationToken = default)
        => await SendCommand(MeetingAction.ReactLike, cancellationToken);

    public async Task ReactLove(CancellationToken cancellationToken = default)
        => await SendCommand(MeetingAction.ReactLove, cancellationToken);

    public async Task ReactWow(CancellationToken cancellationToken = default)
        => await SendCommand(MeetingAction.ReactWow, cancellationToken);

    public async Task UpdateState(CancellationToken cancellationToken = default)
        => await SendCommand(MeetingAction.QueryMeetingState, cancellationToken);
}
