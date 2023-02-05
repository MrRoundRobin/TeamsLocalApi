using Ro.Teams.LocalApi;
using static System.Console;

CancellationTokenSource cts = new();
CancelKeyPress += (source, args) =>
{
    Error.WriteLine("Cancelling...");

    args.Cancel = true;
    cts.Cancel();
};

Client Teams = new Client(autoConnect: true) ?? throw new Exception("Could not create client");

Teams.Connected += (_, _) => WriteLine("Event: Connected");
Teams.Disconnected += (_, _) => WriteLine("Event: Disconnected");
Teams.PropertyChanged += (o, e) =>
{
    if (o is null || e.PropertyName is null) return;
    WriteLine("Event: PropertyChanged: {0}={1}", e.PropertyName, o.GetType()?.GetProperty(e.PropertyName)?.GetValue(o, null));
};

WriteLine("Connecting...");
await Teams.Connect(true, cts.Token);
WriteLine("Connected...");

var methods = Teams.GetType().GetMethods().Where(m => m.IsPublic).ToArray();
string choice;
do
{
    WriteLine("Choose method:\r\n");

    for (var i = 0; i < methods.Length; i++)
        WriteLine("{0}:\t{1}", i, methods[i].Name);

    WriteLine("{0}:\t{1}", "q", "Quit");

    choice = ReadLine() ?? "";

    if (choice == "q")
        continue;

    switch (choice)
    {
        case "q":
            continue;
        case "c":
            cts.Cancel();
            break;
        case "m+":
            Teams.IsMuted = true;
            break;
        case "m-":
            Teams.IsMuted = false;
            break;
        default:
            if (!int.TryParse(choice, out int call))
                continue;

            _ = methods[call].Invoke(Teams, null);
            break;
    }


} while (choice != "q");
