using System.Text.Json.Serialization;

namespace Ro.Teams.LocalApi;

internal class ClientMessage
{
    public MeetingAction Action { get; set; }

    public ClientMessageParameter? Parameters { get; set; }

    public int RequestId { get; set; }
}

internal class ClientMessageParameter
{
    [JsonPropertyName("type")]
    public ClientMessageParameterType Type { get; set; }
}

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
internal enum MeetingAction
{
    [JsonPropertyName("none")]
    None = 0,

    [JsonPropertyName("pair")]
    Pair = 0b0000_0001_0000_0001,

    [JsonPropertyName("query-state")]
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
    [JsonPropertyName("toggle-background-blur")]
    ToggleBlurBackground = 0b0000_0100_0000_0010,

    [JsonPropertyName("lower-hand")]
    LowerHand = 0b0000_0101_0000_0000,
    [JsonPropertyName("raise-hand")]
    RaiseHand = 0b0000_0101_0000_0001,
    [JsonPropertyName("toggle-hand")]
    ToggleHand = 0b0000_0101_0000_0010,

    //[JsonPropertyName("stop-recording")]
    //StopRecording = 0b0000_0110_0000_0000,
    //[JsonPropertyName("start-recording")]
    //StartRecording = 0b0000_0110_0000_0001,
    //[JsonPropertyName("toggle-recording")]
    //ToggleRecording = 0b0000_0110_0000_0010,

    [JsonPropertyName("leave-call")]
    LeaveCall = 0b0000_0111_0000_0000,

    [JsonPropertyName("send-react")]
    React     = 0b0000_1000_0000_0000,

    [JsonPropertyName("toggle-ui")]
    ToggleUI  = 0b0000_1001_0000_0000,

    [JsonPropertyName("stop-sharing")]
    StopSharing = 0b0000_1010_0000_0000,
}


[JsonConverter(typeof(JsonStringEnumMemberConverter))]
internal enum ClientMessageParameterType
{
    [JsonPropertyName("applause")]
    ReactApplause = 0b0000_0111_0001_0000,
    [JsonPropertyName("laugh")]
    ReactLaugh    = 0b0000_0111_0001_0001,
    [JsonPropertyName("like")]
    ReactLike     = 0b0000_0111_0001_0010,
    [JsonPropertyName("love")]
    ReactLove     = 0b0000_0111_0001_0011,
    [JsonPropertyName("wow")]
    ReactWow      = 0b0000_0111_0001_0100,

    [JsonPropertyName("chat")]
    ToggleUiChat    = 0b0000_1001_0000_0001,
    [JsonPropertyName("sharing-tray")]
    ToggleUiSharing = 0b0000_1001_0000_0010,

}
