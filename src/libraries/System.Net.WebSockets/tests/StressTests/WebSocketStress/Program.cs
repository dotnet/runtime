using System.Reflection;
using WebSocketStress;

if (Configuration.TryParseCli(args, out Configuration? config))
{
    await Run(config);
}

static async Task<ExitCode> Run(Configuration config)
{
    if ((config.RunMode & RunMode.both) == 0)
    {
        Console.Error.WriteLine("Must specify a valid run mode");
        return ExitCode.CliError;
    }

    static string GetAssemblyInfo(Assembly assembly) => $"{assembly.Location}, modified {new FileInfo(assembly.Location).LastWriteTime}";

    Console.WriteLine("             .NET Core: " + GetAssemblyInfo(typeof(object).Assembly));
    Console.WriteLine(" System.Net.WebSockets: " + GetAssemblyInfo(typeof(System.Net.WebSockets.WebSocket).Assembly));
    Console.WriteLine("       Server Endpoint: " + config.ServerEndpoint);
    Console.WriteLine("           Concurrency: " + config.MaxConnections);
    Console.WriteLine("    Max Execution Time: " + ((config.MaxExecutionTime != null) ? config.MaxExecutionTime.Value.ToString() : "infinite"));
    Console.WriteLine("           Random Seed: " + config.RandomSeed);
    Console.WriteLine("       Cancellation Pb: " + 100 * config.CancellationProbability + "%");
    Console.WriteLine();

    StressServer? server = null;
    if (config.RunMode.HasFlag(RunMode.server))
    {
        // Start the SSL web server in-proc.
        Console.WriteLine($"Starting WebSocket server.");
        server = new StressServer(config);
        _ = server.Start();

        Console.WriteLine($"Server listening to {server.ServerEndpoint}");
    }

    StressClient? client = null;
    if (config.RunMode.HasFlag(RunMode.client))
    {
        // Start the client.
        Console.WriteLine($"Starting {config.MaxConnections} client workers.");
        Console.WriteLine();

        client = new StressClient(config);

        await client.InitializeAsync();
        _ = client.Start();
    }

    await WaitUntilMaxExecutionTimeElapsedOrKeyboardInterrupt(config.MaxExecutionTime);

    Log.Close();

    try
    {
        if (client != null)
        {
            await client.StopAsync();
            Console.WriteLine("client stopped");
        }

        if (server != null)
        {
            await server.StopAsync();
            Console.WriteLine("server stopped");
        }
    }
    finally
    {
        client?.PrintFinalReport();
    }

    return client?.TotalErrorCount == 0 ? ExitCode.Success : ExitCode.StressError;

    static async Task WaitUntilMaxExecutionTimeElapsedOrKeyboardInterrupt(TimeSpan? maxExecutionTime = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        Console.CancelKeyPress += (sender, args) => { Console.Error.WriteLine("Keyboard interrupt"); args.Cancel = true; tcs.TrySetResult(false); };
        if (maxExecutionTime.HasValue)
        {
            Console.WriteLine($"Running for a total of {maxExecutionTime.Value.TotalMinutes:0.##} minutes");
            var cts = new System.Threading.CancellationTokenSource(delay: maxExecutionTime.Value);
            cts.Token.Register(() => { Console.WriteLine("Max execution time elapsed"); tcs.TrySetResult(false); });
        }

        await tcs.Task;
    }
}

public enum ExitCode { Success = 0, StressError = 1, CliError = 2 };

[Flags]
public enum RunMode { server = 1, client = 2, both = server | client };


