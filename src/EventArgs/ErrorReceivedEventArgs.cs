namespace Ro.Teams.LocalApi.EventArgs;

public class ErrorReceivedEventArgs(string errorMessage) : System.EventArgs
{
    public string ErrorMessage { get; } = errorMessage;
}
