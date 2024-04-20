using System.Web;

namespace Ro.Teams.LocalApi;

internal struct ClientInfo
{
    public string Manufacturer { init; get; }
    public string Device { init; get; }
    public string App { init; get; }
    public string AppVersion { init; get; }
    public string? Token { set; get; }

    public readonly Uri GetServerUrl()
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        query["protocol-version"] = "2.0.0";
        query["manufacturer"] = Manufacturer;
        query["device"] = Device;
        query["app"] = App;
        query["app-version"] = AppVersion;

        if (Token is not null)
            query["token"] = Token;

        return new UriBuilder("ws://127.0.0.1:8124")
        {
            Query = query.ToString()
        }.Uri;
    }
}
