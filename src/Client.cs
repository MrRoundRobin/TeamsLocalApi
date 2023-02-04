using System.ComponentModel;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ro.TeamsLocalApi;

public class Client : INotifyPropertyChanged
{
    public bool IsMuted { get => isMuted; set { SendCommand(value ? MeetingAction.Mute : MeetingAction.Unmute); } }
    public bool IsCameraOn { get => isCameraOn; set { SendCommand(value ? MeetingAction.ShowVideo : MeetingAction.HideVideo); } }
    public bool IsHandRaised { get => isHandRaised; set { SendCommand(value ? MeetingAction.RaiseHand : MeetingAction.LowerHand); } }
    public bool IsInMeeting { get => isInMeeting; }
    public bool IsRecordingOn { get => isRecordingOn; set { SendCommand(value ? MeetingAction.StartRecording : MeetingAction.StopRecording); } }
    public bool IsBackgroundBlurred { get => isBackgroundBlurred; set { SendCommand(value ? MeetingAction.BlurBackground : MeetingAction.UnblurBackground); } }
    public bool IsConnected => ws is not null && ws.State == WebSocketState.Open;

    public bool CanToggleMute { get; private set; }
    public bool CanToggleVideo { get; private set; }
    public bool CanToggleHand { get; private set; }
    public bool CanToggleBlur { get; private set; }
    public bool CanToggleRecord { get; private set; }
    public bool CanLeave { get; private set; }
    public bool CanReact { get; private set; }

    private bool isMuted;
    private bool isCameraOn;
    private bool isHandRaised;
    private bool isInMeeting;
    private bool isRecordingOn;
    private bool isBackgroundBlurred;

    private const string Manufacturer = "Elgato";
    private const string Device = "Stream Deck";

    private readonly Uri Uri;
    private ClientWebSocket? ws;

    public event EventHandler? Closed;
    public event PropertyChangedEventHandler? PropertyChanged;

    public Client(string token, bool autoConnect = false)
    {
        Uri = new($"ws://localhost:8124?token={token}");

        if (autoConnect)
            Connect();
    }

    public async void Disconnect()
    {
        if (ws is null) return;

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
    }

    private async Task Reconnect()
    {
        if (ws?.State == WebSocketState.Open)
            await ws.CloseAsync(WebSocketCloseStatus.Empty, null, default);

        if (Process.GetProcessesByName("Teams").Length == 0)
            return;

        ws = new();

        try
        {
            await ws.ConnectAsync(Uri, default);
        }
        catch (WebSocketException)
        {
            throw;
        }
    }

    public async void Connect()
    {
        await Reconnect();

        if (ws is null)
            return;

        var buffer = new byte[1024];

        while (true)
        {
            var result = await ws.ReceiveAsync(buffer, default);

            if (result.CloseStatus.HasValue)
            {
                if (ws.CloseStatus != WebSocketCloseStatus.NormalClosure)
                {
                    await Reconnect();
                    continue;
                }
                Closed?.Invoke(this, EventArgs.Empty);
                break;
            }

            if (!result.EndOfMessage || result.Count == 0)
                throw new NotImplementedException("Invalid Message received");

            string data = Encoding.UTF8.GetString(buffer, 0, result.Count);

            try
            {
                var message = JsonSerializer.Deserialize<ServerMessage>(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? throw new ArgumentNullException();

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
            catch (Exception ex)
            {
                throw new NotImplementedException("Invalid Message received", ex);
            }
        }
    }

    private async void SendCommand(MeetingAction action)
    {
        if (ws is null || ws.State != WebSocketState.Open) return;

        var message = JsonSerializer.Serialize(new ClientMessage()
        {
            Action = action,
            Device = Device,
            Manufacturer = Manufacturer,
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        await ws.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, default);
    }

    public void LeaveCall() => SendCommand(MeetingAction.LeaveCall);

    public void ReactApplause() => SendCommand(MeetingAction.ReactApplause);

    public void ReactLaugh() => SendCommand(MeetingAction.ReactLaugh);

    public void ReactLike() => SendCommand(MeetingAction.ReactLike);

    public void ReactLove() => SendCommand(MeetingAction.ReactLove);

    public void ReactWow() => SendCommand(MeetingAction.ReactWow);

    public void UpdateState() => SendCommand(MeetingAction.QueryMeetingState);
}
