namespace Ro.Teams.LocalApi.EventArgs;

public class TokenReceivedEventArgs(string token) : System.EventArgs
{
    public string Token { get; } = token;
}
