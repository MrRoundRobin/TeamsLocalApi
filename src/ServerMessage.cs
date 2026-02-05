namespace Ro.Teams.LocalApi;

internal class ServerMessage
{
    public int? RequestId { get; set; }
    public string? Response { get; set; }
    public string? ErrorMsg { get; set; }
    public string? TokenRefresh { get; set; }
    public MeetingUpdate? MeetingUpdate { get; set; }
}

internal class MeetingUpdate
{
    public MeetingState? MeetingState { get; set; }
    public MeetingPermissions MeetingPermissions { get; set; } = new();
}

internal class MeetingState
{
    public bool IsMuted { get; set; }
    public bool IsHandRaised { get; set; }
    public bool IsInMeeting { get; set; }
    public bool IsRecordingOn { get; set; }
    public bool IsBackgroundBlurred { get; set; }
    public bool IsSharing { get; set; }
    public bool HasUnreadMessages { get; set; }
    public bool IsVideoOn { get; set; }
}

internal class MeetingPermissions
{
    public bool CanToggleMute { get; set; }
    public bool CanToggleVideo { get; set; }
    public bool CanToggleHand { get; set; }
    public bool CanToggleBlur { get; set; }
    public bool CanLeave { get; set; }
    public bool CanReact { get; set; }
    public bool CanToggleShareTray { get; set; }
    public bool CanToggleChat { get; set; }
    public bool CanStopSharing { get; set; }
    public bool CanPair { get; set; }
}
