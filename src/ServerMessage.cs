namespace ms.robin.TeamsLocalApi;

internal class ServerMessage
{
    public string ApiVersion { get; set; } = "1.0.0.0";
    public MeetingUpdate MeetingUpdate { get; set; } = new();
}

internal class MeetingUpdate
{
    public MeetingState MeetingState { get; set; } = new ();
    public MeetingPermissions MeetingPermissions { get; set; } = new();

}

internal class MeetingState
{
    public bool IsMuted { get; set; }
    public bool IsCameraOn { get; set; }
    public bool IsHandRaised { get; set; }
    public bool IsInMeeting { get; set; }
    public bool IsRecordingOn { get; set; }
    public bool IsBackgroundBlurred { get; set; }

}

internal class MeetingPermissions
{
    public bool CanToggleMute { get; set; }
    public bool CanToggleVideo { get; set; }
    public bool CanToggleHand { get; set; }
    public bool CanToggleBlur { get; set; }
    public bool CanToggleRecord { get; set; }
    public bool CanLeave { get; set; }
    public bool CanReact { get; set; }
}
