using System.Text.Json.Serialization;

namespace Ro.Teams.LocalApi;

internal class ClientMessage
{
    public string ApiVersion { get; set; } = "1.0.0";

    public MeetingAction Action { get; set; }

    public MeetingService Service => (MeetingService)((int)Action >> 8);

    [JsonConverter(typeof(UnixEpochDateTimeConverter))]
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Manufacturer { get; set; } = "Elgato";
    public string Device { get; set; } = "Stream Deck";
}

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
internal enum MeetingAction
{
    [JsonPropertyName("none")]
    None = 0,

    [JsonPropertyName("query-meeting-state")]
    QueryMeetingState = 0b0000_0001_0000_0000,

    [JsonPropertyName("mute")]
    Mute = 0b0000_0010_0000_0000,
    [JsonPropertyName("unmute")]
    Unmute = 0b0000_0010_0000_0001,
    [JsonPropertyName("toggle-mute")]
    ToggleMute = 0b0000_0010_0000_0010,

    [JsonPropertyName("hide-video")]
    HideVideo = 0b0000_0011_0000_0000,
    [JsonPropertyName("show-video")]
    ShowVideo = 0b0000_0011_0000_0001,
    [JsonPropertyName("toggle-video")]
    ToggleVideo = 0b0000_0011_0000_0010,

    [JsonPropertyName("unblur-background")]
    UnblurBackground = 0b0000_0100_0000_0000,
    [JsonPropertyName("blur-background")]
    BlurBackground = 0b0000_0100_0000_0001,
    [JsonPropertyName("toggle-blur-background")]
    ToggleBlurBackground = 0b0000_0100_0000_0010,

    [JsonPropertyName("lower-hand")]
    LowerHand = 0b0000_0101_0000_0000,
    [JsonPropertyName("raise-hand")]
    RaiseHand = 0b0000_0101_0000_0001,
    [JsonPropertyName("toggle-hand")]
    ToggleHand = 0b0000_0101_0000_0010,

    [JsonPropertyName("stop-recording")]
    StopRecording = 0b0000_0110_0000_0000,
    [JsonPropertyName("start-recording")]
    StartRecording = 0b0000_0110_0000_0001,
    [JsonPropertyName("toggle-recording")]
    ToggleRecording = 0b0000_0110_0000_0010,

    [JsonPropertyName("leave-call")]
    LeaveCall = 0b0000_0111_0000_0000,

    [JsonPropertyName("react-applause")]
    ReactApplause = 0b0000_0111_0001_0000,
    [JsonPropertyName("react-laugh")]
    ReactLaugh = 0b0000_0111_0001_0001,
    [JsonPropertyName("react-like")]
    ReactLike = 0b0000_0111_0001_0010,
    [JsonPropertyName("react-love")]
    ReactLove = 0b0000_0111_0001_0011,
    [JsonPropertyName("react-wow")]
    ReactWow = 0b0000_0111_0001_0100,
}

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
internal enum MeetingService
{
    [JsonPropertyName("none")]
    None = 0,

    [JsonPropertyName("query-meeting-state")]
    QueryMeetingState = 0b0000_0001,
    [JsonPropertyName("toggle-mute")]
    ToggleMute = 0b0000_0010,
    [JsonPropertyName("toggle-video")]
    ToggleVideo = 0b0000_0011,
    [JsonPropertyName("background-blur")]
    BackgroundBlur = 0b0000_0100,
    [JsonPropertyName("raise-hand")]
    RaiseHand = 0b0000_0101,
    [JsonPropertyName("recording")]
    Recording = 0b0000_0110,
    [JsonPropertyName("call")]
    Call = 0b0000_0111,
}
