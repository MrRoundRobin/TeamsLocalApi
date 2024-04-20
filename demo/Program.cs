using Ro.Teams.LocalApi;
using Ro.Teams.LocalApi.Demo;
using static System.Console;

CancellationTokenSource cts = new();
CancelKeyPress += (source, args) =>
{
    Error.WriteLine("Cancelling...");

    args.Cancel = true;
    cts.Cancel();
};

var token = Settings.Default.Token != string.Empty ? Settings.Default.Token : null;

Client Teams = new Client(autoConnect: false, token: token) ?? throw new Exception("Could not create client");

Teams.TokenReceived += (_, args) =>
{
    WriteLine("Event: TokenReceived");
    Settings.Default.Token = args.Token;
    Settings.Default.Save();
};
Teams.Connected += (_, _) => WriteLine("Event: Connected");
Teams.Disconnected += (_, _) => WriteLine("Event: Disconnected");
Teams.PropertyChanged += (o, e) =>
{
    if (o is null || e.PropertyName is null) return;
    WriteLine("Event: PropertyChanged: {0}={1}", e.PropertyName, o.GetType()?.GetProperty(e.PropertyName)?.GetValue(o, null));
};
Teams.ErrorReceived += (_, args) => WriteLine("Event: ErrorReceived: {0}", args.ErrorMessage);

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

            var parameters = methods[call].GetParameters();

            if (parameters.Length == 0)
                _ = methods[call].Invoke(Teams, null);
            else
            {
                var arguments = new object?[parameters.Length];

                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].Name == "cancellationToken")
                    {
                        arguments[i] = cts.Token;
                        continue;
                    }

                    Write("Enter {0} ({1}): ", parameters[i].Name, parameters[i].ParameterType.Name);
                    arguments[i] = Convert.ChangeType(ReadLine(), parameters[i].ParameterType);
                }

                _ = methods[call].Invoke(Teams, arguments);
            }

            break;
    }
} while (choice != "q");
