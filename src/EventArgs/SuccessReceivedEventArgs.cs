namespace Ro.Teams.LocalApi.EventArgs;

public class SuccessReceivedEventArgs(int requestId) : System.EventArgs
{
    public int RequestId { get; } = requestId;
}
