// See https://aka.ms/new-console-template for more information
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using WebSocketStress;

static async Task Test()
{
    byte[] s_endLine = [(byte)'\n'];

    Console.WriteLine(typeof(object).Assembly.Location);

    using Socket listenerSock = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    listenerSock.Bind(new IPEndPoint(IPAddress.Loopback, 0));
    listenerSock.Listen();
    using Socket clientSock = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    Task<Socket> acceptTask = listenerSock.AcceptAsync();
    await clientSock.ConnectAsync(listenerSock.LocalEndPoint!);
    using Socket handlerSock = await acceptTask;

    using WebSocket serverWs = WebSocket.CreateFromStream(new NetworkStream(handlerSock, ownsSocket: true), isServer: true, null, TimeSpan.Zero);
    using WebSocket clientWs = WebSocket.CreateFromStream(new NetworkStream(clientSock, ownsSocket: true), isServer: false, null, TimeSpan.Zero);

    Log log = new Log("Test", 1235);

    DataSegment sent = DataSegment.CreateRandom(Random.Shared, 10);
    Console.WriteLine(sent);
    DataSegmentSerializer serializer = new DataSegmentSerializer(log);
    DataSegmentSerializer deserializer = new DataSegmentSerializer(log);

    await serializer.SerializeAsync(clientWs, sent);
    await clientWs.WriteAsync(s_endLine, default);

    Console.WriteLine("----");

    await serializer.SerializeAsync(clientWs, DataSegment.CreateRandom(Random.Shared, 50));
    await clientWs.WriteAsync(s_endLine, default);


    InputProcessor processor = new InputProcessor(serverWs, log);
    await processor.RunAsync(buffer =>
    {
        DataSegment received = deserializer.Deserialize(buffer);
        Console.WriteLine($"Server Deserialized L={buffer.Length}");
        Console.WriteLine(received);
        received.Return();
        return Task.FromResult(false);
    });
    
    Console.WriteLine("yay?");
}

async Task Test2()
{
    byte[] s_endLine = [(byte)'\n'];

    Console.WriteLine(typeof(object).Assembly.Location);

    using Socket listenerSock = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    listenerSock.Bind(new IPEndPoint(IPAddress.Loopback, 0));
    listenerSock.Listen();
    using Socket clientSock = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    Task<Socket> acceptTask = listenerSock.AcceptAsync();
    await clientSock.ConnectAsync(listenerSock.LocalEndPoint!);
    using Socket handlerSock = await acceptTask;

    using WebSocket serverWs = WebSocket.CreateFromStream(new NetworkStream(handlerSock, ownsSocket: true), isServer: true, null, TimeSpan.Zero);
    using WebSocket clientWs = WebSocket.CreateFromStream(new NetworkStream(clientSock, ownsSocket: true), isServer: false, null, TimeSpan.Zero);

    CancellationToken precancelled = new CancellationToken(true);

    try
    {
        await clientWs.SendAsync(new byte[12345], WebSocketMessageType.Binary, false, precancelled);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("yay!");
    }
    try
    {
        var res = await serverWs.ReceiveAsync(new byte[43252], default);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.Message} | {serverWs.State}");
    }
}

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


