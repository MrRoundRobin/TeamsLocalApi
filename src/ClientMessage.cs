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
    public ClientMessageParameterType Type { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum MeetingAction
{
    [JsonStringEnumMemberName("none")]
    None = 0,

    [JsonStringEnumMemberName("pair")]
    Pair = 0b0000_0001_0000_0001,

    [JsonStringEnumMemberName("query-state")]
    QueryMeetingState = 0b0000_0001_0000_0000,

    [JsonStringEnumMemberName("mute")]
    Mute = 0b0000_0010_0000_0000,
    [JsonStringEnumMemberName("unmute")]
    Unmute = 0b0000_0010_0000_0001,
    [JsonStringEnumMemberName("toggle-mute")]
    ToggleMute = 0b0000_0010_0000_0010,

    [JsonStringEnumMemberName("hide-video")]
    HideVideo = 0b0000_0011_0000_0000,
    [JsonStringEnumMemberName("show-video")]
    ShowVideo = 0b0000_0011_0000_0001,
    [JsonStringEnumMemberName("toggle-video")]
    ToggleVideo = 0b0000_0011_0000_0010,

    [JsonStringEnumMemberName("unblur-background")]
    UnblurBackground = 0b0000_0100_0000_0000,
    [JsonStringEnumMemberName("blur-background")]
    BlurBackground = 0b0000_0100_0000_0001,
    [JsonStringEnumMemberName("toggle-background-blur")]
    ToggleBlurBackground = 0b0000_0100_0000_0010,

    [JsonStringEnumMemberName("lower-hand")]
    LowerHand = 0b0000_0101_0000_0000,
    [JsonStringEnumMemberName("raise-hand")]
    RaiseHand = 0b0000_0101_0000_0001,
    [JsonStringEnumMemberName("toggle-hand")]
    ToggleHand = 0b0000_0101_0000_0010,

    //[JsonStringEnumMemberName("stop-recording")]
    //StopRecording = 0b0000_0110_0000_0000,
    //[JsonStringEnumMemberName("start-recording")]
    //StartRecording = 0b0000_0110_0000_0001,
    //[JsonStringEnumMemberName("toggle-recording")]
    //ToggleRecording = 0b0000_0110_0000_0010,

    [JsonStringEnumMemberName("leave-call")]
    LeaveCall = 0b0000_0111_0000_0000,

    [JsonStringEnumMemberName("send-reaction")]
    React     = 0b0000_1000_0000_0000,

    [JsonStringEnumMemberName("toggle-ui")]
    ToggleUI  = 0b0000_1001_0000_0000,

    [JsonStringEnumMemberName("stop-sharing")]
    StopSharing = 0b0000_1010_0000_0000,
}


[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ClientMessageParameterType
{
    [JsonStringEnumMemberName("applause")]
    ReactApplause = 0b0000_0111_0001_0000,
    [JsonStringEnumMemberName("laugh")]
    ReactLaugh    = 0b0000_0111_0001_0001,
    [JsonStringEnumMemberName("like")]
    ReactLike     = 0b0000_0111_0001_0010,
    [JsonStringEnumMemberName("love")]
    ReactLove     = 0b0000_0111_0001_0011,
    [JsonStringEnumMemberName("wow")]
    ReactWow      = 0b0000_0111_0001_0100,

    [JsonStringEnumMemberName("chat")]
    ToggleUiChat    = 0b0000_1001_0000_0001,
    [JsonStringEnumMemberName("share-tray")]
    ToggleUiSharing = 0b0000_1001_0000_0010,
}
