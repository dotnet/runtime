// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using HttpStress;

/// <summary>
/// Simple HttpClient stress app that launches Kestrel in-proc and runs many concurrent requests of varying types against it.
/// </summary>
public class Program
{
    [Flags]
    enum RunMode { server = 1, client = 2, both = 3 }; 

    public static async Task Main(string[] args)
    {
        var cmd = new RootCommand();
        cmd.AddOption(new Option("-n", "Max number of requests to make concurrently.") { Argument = new Argument<int>("numWorkers", Environment.ProcessorCount) });
        cmd.AddOption(new Option("-serverUri", "Stress suite server uri.") { Argument = new Argument<Uri>("serverUri", new Uri("https://localhost:5001")) });
        cmd.AddOption(new Option("-runMode", "Stress suite execution mode. Defaults to Both.") { Argument = new Argument<RunMode>("runMode", RunMode.both)});
        cmd.AddOption(new Option("-maxContentLength", "Max content length for request and response bodies.") { Argument = new Argument<int>("numBytes", 1000) });
        cmd.AddOption(new Option("-maxRequestUriSize", "Max query string length support by the server.") { Argument = new Argument<int>("numChars", 8000) });
        cmd.AddOption(new Option("-http", "HTTP version (1.1 or 2.0)") { Argument = new Argument<Version>("version", HttpVersion.Version20) });
        cmd.AddOption(new Option("-connectionLifetime", "Max connection lifetime length (milliseconds).") { Argument = new Argument<int?>("connectionLifetime", null)});
        cmd.AddOption(new Option("-ops", "Indices of the operations to use") { Argument = new Argument<int[]>("space-delimited indices", null) });
        cmd.AddOption(new Option("-xops", "Indices of the operations to exclude") { Argument = new Argument<int[]>("space-delimited indices", null) });
        cmd.AddOption(new Option("-trace", "Enable Microsoft-System-Net-Http tracing.") { Argument = new Argument<string>("\"console\" or path") });
        cmd.AddOption(new Option("-aspnetlog", "Enable ASP.NET warning and error logging.") { Argument = new Argument<bool>("enable", false) });
        cmd.AddOption(new Option("-listOps", "List available options.") { Argument = new Argument<bool>("enable", false) });
        cmd.AddOption(new Option("-seed", "Seed for generating pseudo-random parameters for a given -n argument.") { Argument = new Argument<int?>("seed", null)});
        cmd.AddOption(new Option("-numParameters", "Max number of query parameters or form fields for a request.") { Argument = new Argument<int>("queryParameters", 1) });
        cmd.AddOption(new Option("-cancelRate", "Number between 0 and 1 indicating rate of client-side request cancellation attempts. Defaults to 0.1.") { Argument = new Argument<double>("probability", 0.1) });
        cmd.AddOption(new Option("-httpSys", "Use http.sys instead of Kestrel.") { Argument = new Argument<bool>("enable", false) });
        cmd.AddOption(new Option("-displayInterval", "Client stats display interval in seconds. Defaults to 1 second.") { Argument = new Argument<int>("displayInterval", 1) });

        ParseResult cmdline = cmd.Parse(args);
        if (cmdline.Errors.Count > 0)
        {
            foreach (ParseError error in cmdline.Errors)
            {
                Console.WriteLine(error);
            }
            Console.WriteLine();
            new HelpBuilder(new SystemConsole()).Write(cmd);
            return;
        }

        await Run(
            runMode                 : cmdline.ValueForOption<RunMode>("-runMode"),
            serverUri               : cmdline.ValueForOption<Uri>("-serverUri"),
            httpSys                 : cmdline.ValueForOption<bool>("-httpSys"),
            concurrentRequests      : cmdline.ValueForOption<int>("-n"),
            maxContentLength        : cmdline.ValueForOption<int>("-maxContentLength"),
            maxRequestUriSize      : cmdline.ValueForOption<int>("-maxRequestUriSize"),
            httpVersion             : cmdline.ValueForOption<Version>("-http"),
            connectionLifetime      : cmdline.ValueForOption<int?>("-connectionLifetime"),
            opIndices               : cmdline.ValueForOption<int[]>("-ops"),
            excludedOpIndices       : cmdline.ValueForOption<int[]>("-xops"),
            logPath                 : cmdline.HasOption("-trace") ? cmdline.ValueForOption<string>("-trace") : null,
            aspnetLog               : cmdline.ValueForOption<bool>("-aspnetlog"),
            listOps                 : cmdline.ValueForOption<bool>("-listOps"),
            seed                    : cmdline.ValueForOption<int?>("-seed") ?? new Random().Next(),
            numParameters           : cmdline.ValueForOption<int>("-numParameters"),
            cancellationProbability : Math.Max(0, Math.Min(1, cmdline.ValueForOption<double>("-cancelRate"))),
            displayIntervalSeconds  : cmdline.ValueForOption<int>("-displayInterval"));
    }

    private static async Task Run(RunMode runMode, Uri serverUri, bool httpSys, int concurrentRequests, int maxContentLength, int maxRequestUriSize, Version httpVersion, int? connectionLifetime, int[] opIndices, int[] excludedOpIndices, string logPath, bool aspnetLog, bool listOps, int seed, int numParameters, double cancellationProbability, int displayIntervalSeconds)
    {

        (string name, Func<RequestContext, Task> op)[] clientOperations = ClientOperations.Operations;

        // handle operation index arguments
        switch (opIndices, excludedOpIndices)
        {
            case (null, null):
                break;
            case (_, null):
                clientOperations = opIndices.Select(i => clientOperations[i]).ToArray();
                break;
            case (null, _):
                opIndices =
                    Enumerable
                        .Range(0, clientOperations.Length)
                        .Concat(excludedOpIndices)
                        .GroupBy(x => x)
                        .Where(gp => gp.Count() < 2)
                        .Select(gp => gp.Key)
                        .ToArray();

                clientOperations = opIndices.Select(i => clientOperations[i]).ToArray();
                break;
            default:
                Console.Error.WriteLine("Cannot specify both -ops and -xops flags simultaneously");
                return;
        }

        Console.WriteLine("       .NET Core: " + Path.GetFileName(Path.GetDirectoryName(typeof(object).Assembly.Location)));
        Console.WriteLine("    ASP.NET Core: " + Path.GetFileName(Path.GetDirectoryName(typeof(WebHost).Assembly.Location)));
        Console.WriteLine("          Server: " + (httpSys ? "http.sys" : "Kestrel"));
        Console.WriteLine("      Server URL: " + serverUri);
        Console.WriteLine("         Tracing: " + (logPath == null ? (object)false : logPath.Length == 0 ? (object)true : logPath));
        Console.WriteLine("     ASP.NET Log: " + aspnetLog);
        Console.WriteLine("     Concurrency: " + concurrentRequests);
        Console.WriteLine("  Content Length: " + maxContentLength);
        Console.WriteLine("    HTTP Version: " + httpVersion);
        Console.WriteLine("        Lifetime: " + (connectionLifetime.HasValue ? $"{connectionLifetime}ms" : "(infinite)"));
        Console.WriteLine("      Operations: " + string.Join(", ", clientOperations.Select(o => o.name)));
        Console.WriteLine("     Random Seed: " + seed);
        Console.WriteLine("    Cancellation: " + 100 * cancellationProbability + "%");
        Console.WriteLine("Query Parameters: " + numParameters);
        Console.WriteLine();

        if (listOps)
        {
            for (int i = 0; i < clientOperations.Length; i++)
            {
                Console.WriteLine($"{i} = {clientOperations[i].name}");
            }
            return;
        }

        if ((runMode & RunMode.both) == 0)
        {
            Console.Error.WriteLine("Must specify a valid run mode");
            return;
        }

        if (serverUri.Scheme != "https")
        {
            Console.Error.WriteLine("Server uri must be https.");
            return;
        }

        if (runMode.HasFlag(RunMode.server))
        {
            // Start the Kestrel web server in-proc.
            Console.WriteLine($"Starting {(httpSys ? "http.sys" : "Kestrel")} server.");
            var server = new StressServer(serverUri, httpSys, maxContentLength, maxRequestUriSize, logPath, aspnetLog);
            Console.WriteLine($"Server started at {server.ServerUri}");
        }

        if (runMode.HasFlag(RunMode.client))
        {
            // Start the client.
            Console.WriteLine($"Starting {concurrentRequests} client workers.");

            new StressClient(
                serverUri: serverUri,
                clientOperations: clientOperations,
                concurrentRequests: concurrentRequests,
                maxContentLength: maxContentLength,
                maxRequestParameters: numParameters,
                maxRequestUriSize: maxRequestUriSize,
                randomSeed: seed,
                cancellationProbability: cancellationProbability,
                http2Probability: (httpVersion == new Version(2, 0)) ? 1 : 0,
                connectionLifetime: connectionLifetime,
                displayInterval: TimeSpan.FromSeconds(displayIntervalSeconds));
        }

        await AwaitCancelKeyPress();
    }

    private static async Task AwaitCancelKeyPress()
    {
        var tcs = new TaskCompletionSource<bool>();
        Console.CancelKeyPress += (sender,args) => { Console.Error.WriteLine("Keyboard interrupt"); tcs.TrySetResult(false); };
        await tcs.Task;
    }

}
