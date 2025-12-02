// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Net;
using HttpStress;
using System.Net.Quic;
using Microsoft.Quic;

[assembly: SupportedOSPlatform("windows")]
[assembly: SupportedOSPlatform("linux")]

namespace HttpStress
{
    /// <summary>
    /// Simple HttpClient stress app that launches Kestrel in-proc and runs many concurrent requests of varying types against it.
    /// </summary>
    public static class Program
    {
        public enum ExitCode { Success = 0, StressError = 1, CliError = 2 };

        public static readonly bool IsQuicSupported = QuicListener.IsSupported && QuicConnection.IsSupported;

        private static readonly Dictionary<string, int> s_unobservedExceptions = new Dictionary<string, int>();

        public static async Task<int> Main(string[] args)
        {
            if (!TryParseCli(args, out Configuration? config))
            {
                return (int)ExitCode.CliError;
            }

            return (int)await Run(config);
        }

        private static bool TryParseCli(string[] args, [NotNullWhen(true)] out Configuration? config)
        {
            var cmd = new RootCommand();
            cmd.Options.Add(new Option<int>("-n") { Description = "Max number of requests to make concurrently.", DefaultValueFactory = (_) => Environment.ProcessorCount });
            cmd.Options.Add(new Option<string>("-serverUri") { Description = "Stress suite server uri.", DefaultValueFactory = (_) => "https://localhost:5001" });
            cmd.Options.Add(new Option<RunMode>("-runMode") { Description = "Stress suite execution mode. Defaults to Both.", DefaultValueFactory = (_) => RunMode.both });
            cmd.Options.Add(new Option<double>("-maxExecutionTime") { Description = "Maximum stress execution time, in minutes. Defaults to infinity." });
            cmd.Options.Add(new Option<int>("-maxContentLength") { Description = "Max content length for request and response bodies.", DefaultValueFactory = (_) => 1000 });
            cmd.Options.Add(new Option<int>("-maxRequestUriSize") { Description = "Max query string length support by the server.", DefaultValueFactory = (_) => 5000 });
            cmd.Options.Add(new Option<int>("-maxRequestHeaderCount") { Description = "Maximum number of headers to place in request", DefaultValueFactory = (_) => 90 });
            cmd.Options.Add(new Option<int>("-maxRequestHeaderTotalSize") { Description = "Max request header total size.", DefaultValueFactory = (_) => 1000 });
            cmd.Options.Add(new Option<Version>("-http")
            {
                Description = "HTTP version (1.1 or 2.0 or 3.0)",
                DefaultValueFactory = (_) => HttpVersion.Version20,
                CustomParser = result =>
                {
                    if (!Version.TryParse(result.Tokens.Single().Value, out Version? parsed))
                    {
                        result.AddError($"'{result.Tokens[0].Value}' is not a valid Version");
                    }

                    return parsed;
                }
            });
            cmd.Options.Add(new Option<int?>("-connectionLifetime") { Description = "Max connection lifetime length (milliseconds)." });
            cmd.Options.Add(new Option<int[]>("-ops") { Description = "Indices of the operations to use" });
            cmd.Options.Add(new Option<int[]>("-xops") { Description = "Indices of the operations to exclude" });
            cmd.Options.Add(new Option<bool>("-trace") { Description = "Enable System.Net.Http.InternalDiagnostics (client) and/or ASP.NET dignostics (server) tracing." });
            cmd.Options.Add(new Option<bool>("-aspnetlog") { Description = "Enable ASP.NET warning and error logging." });
            cmd.Options.Add(new Option<bool>("-listOps") { Description = "List available options." });
            cmd.Options.Add(new Option<int?>("-seed") { Description = "Seed for generating pseudo-random parameters for a given -n argument." });
            cmd.Options.Add(new Option<int>("-numParameters") { Description = "Max number of query parameters or form fields for a request.", DefaultValueFactory = (_) => 1 });
            cmd.Options.Add(new Option<double>("-cancelRate") { Description = "Number between 0 and 1 indicating rate of client-side request cancellation attempts. Defaults to 0.1.", DefaultValueFactory = (_) => 0.1 });
            cmd.Options.Add(new Option<bool>("-httpSys") { Description = "Use http.sys instead of Kestrel." });
            cmd.Options.Add(new Option<bool>("-winHttp") { Description = "Use WinHttpHandler for the stress client." });
            cmd.Options.Add(new Option<int>("-displayInterval") { Description = "Client stats display interval in seconds. Defaults to 5 seconds.", DefaultValueFactory = (_) => 5 });
            cmd.Options.Add(new Option<int>("-clientTimeout") { Description = "Default HttpClient timeout in seconds. Defaults to 60 seconds.", DefaultValueFactory = (_) => 60 });
            cmd.Options.Add(new Option<int?>("-serverMaxConcurrentStreams") { Description = "Overrides kestrel max concurrent streams per connection." });
            cmd.Options.Add(new Option<int?>("-serverMaxFrameSize") { Description = "Overrides kestrel max frame size setting." });
            cmd.Options.Add(new Option<int?>("-serverInitialConnectionWindowSize") { Description = "Overrides kestrel initial connection window size setting." });
            cmd.Options.Add(new Option<int?>("-serverMaxRequestHeaderFieldSize") { Description = "Overrides kestrel max request header field size." });
            cmd.Options.Add(new Option<bool?>("-unobservedEx") { Description = "Enable tracking unobserved exceptions." });

            ParseResult cmdline = cmd.Parse(args);
            if (cmdline.Errors.Count > 0)
            {
                cmdline.Invoke(); // this is going to print all the errors and help
                config = null;
                return false;
            }

            config = new Configuration()
            {
                RunMode = cmdline.GetValue<RunMode>("-runMode"),
                ServerUri = cmdline.GetValue<string>("-serverUri")!,
                ListOperations = cmdline.GetValue<bool>("-listOps"),

                HttpVersion = cmdline.GetValue<Version>("-http")!,
                UseWinHttpHandler = cmdline.GetValue<bool>("-winHttp"),
                ConcurrentRequests = cmdline.GetValue<int>("-n"),
                RandomSeed = cmdline.GetValue<int?>("-seed") ?? new Random().Next(),
                MaxContentLength = cmdline.GetValue<int>("-maxContentLength"),
                MaxRequestUriSize = cmdline.GetValue<int>("-maxRequestUriSize"),
                MaxRequestHeaderCount = cmdline.GetValue<int>("-maxRequestHeaderCount"),
                MaxRequestHeaderTotalSize = cmdline.GetValue<int>("-maxRequestHeaderTotalSize"),
                OpIndices = cmdline.GetValue<int[]>("-ops"),
                ExcludedOpIndices = cmdline.GetValue<int[]>("-xops"),
                MaxParameters = cmdline.GetValue<int>("-numParameters"),
                DisplayInterval = TimeSpan.FromSeconds(cmdline.GetValue<int>("-displayInterval")),
                DefaultTimeout = TimeSpan.FromSeconds(cmdline.GetValue<int>("-clientTimeout")),
                ConnectionLifetime = cmdline.GetValue<double?>("-connectionLifetime").Select(TimeSpan.FromMilliseconds),
                CancellationProbability = Math.Max(0, Math.Min(1, cmdline.GetValue<double>("-cancelRate"))),
                MaximumExecutionTime = cmdline.GetValue<double?>("-maxExecutionTime").Select(TimeSpan.FromMinutes),

                UseHttpSys = cmdline.GetValue<bool>("-httpSys"),
                LogAspNet = cmdline.GetValue<bool>("-aspnetlog"),
                Trace = cmdline.GetValue<bool>("-trace"),
                TrackUnobservedExceptions = cmdline.GetValue<bool?>("-unobservedEx"),
                ServerMaxConcurrentStreams = cmdline.GetValue<int?>("-serverMaxConcurrentStreams"),
                ServerMaxFrameSize = cmdline.GetValue<int?>("-serverMaxFrameSize"),
                ServerInitialConnectionWindowSize = cmdline.GetValue<int?>("-serverInitialConnectionWindowSize"),
                ServerMaxRequestHeaderFieldSize = cmdline.GetValue<int?>("-serverMaxRequestHeaderFieldSize"),
            };

            return true;
        }

        private static async Task<ExitCode> Run(Configuration config)
        {
            (string name, Func<RequestContext, Task> op)[] clientOperations =
                ClientOperations.Operations
                    // annotate the operation name with its index
                    .Select((op, i) => ($"{i.ToString().PadLeft(2)}: {op.name}", op.operation))
                    .ToArray();

            if ((config.RunMode & RunMode.both) == 0)
            {
                Console.Error.WriteLine("Must specify a valid run mode");
                return ExitCode.CliError;
            }

            if (!config.ServerUri.StartsWith("http"))
            {
                Console.Error.WriteLine("Invalid server uri");
                return ExitCode.CliError;
            }

            if (config.ListOperations)
            {
                for (int i = 0; i < clientOperations.Length; i++)
                {
                    Console.WriteLine(clientOperations[i].name);
                }
                return ExitCode.Success;
            }

            // derive client operations based on arguments
            (string name, Func<RequestContext, Task> op)[] usedClientOperations = (config.OpIndices, config.ExcludedOpIndices) switch
            {
                (null, null) => clientOperations,
                (int[] incl, null) => incl.Select(i => clientOperations[i]).ToArray(),
                (_, int[] excl) =>
                    Enumerable
                    .Range(0, clientOperations.Length)
                    .Except(excl)
                    .Select(i => clientOperations[i])
                    .ToArray(),
            };

            string GetAssemblyInfo(Assembly assembly) => $"{assembly.Location}, modified {new FileInfo(assembly.Location).LastWriteTime}";

            Type msQuicApiType = Type.GetType("System.Net.Quic.MsQuicApi, System.Net.Quic")!;
            string msQuicLibraryVersion = (string)msQuicApiType.GetProperty("MsQuicLibraryVersion", BindingFlags.NonPublic | BindingFlags.Static)!.GetGetMethod(true)!.Invoke(null, Array.Empty<object?>())!;
            bool trackUnobservedExceptions = config.TrackUnobservedExceptions.HasValue
                ? config.TrackUnobservedExceptions.Value
                : config.RunMode.HasFlag(RunMode.client);

            Console.WriteLine("       .NET Core: " + GetAssemblyInfo(typeof(object).Assembly));
            Console.WriteLine("    ASP.NET Core: " + GetAssemblyInfo(typeof(WebHost).Assembly));
            Console.WriteLine(" System.Net.Http: " + GetAssemblyInfo(typeof(System.Net.Http.HttpClient).Assembly));
            Console.WriteLine("          Server: " + (config.UseHttpSys ? "http.sys" : "Kestrel"));
            Console.WriteLine("      Server URL: " + config.ServerUri);
            Console.WriteLine("  Client Tracing: " + (config.Trace && config.RunMode.HasFlag(RunMode.client) ? "ON (client.log)" : "OFF"));
            Console.WriteLine("  Server Tracing: " + (config.Trace && config.RunMode.HasFlag(RunMode.server) ? "ON (server.log)" : "OFF"));
            Console.WriteLine("     ASP.NET Log: " + config.LogAspNet);
            Console.WriteLine("     Concurrency: " + config.ConcurrentRequests);
            Console.WriteLine("  Content Length: " + config.MaxContentLength);
            Console.WriteLine("    HTTP Version: " + config.HttpVersion);
            Console.WriteLine("  QUIC supported: " + (IsQuicSupported ? "yes" : "no"));
            Console.WriteLine("  MsQuic Version: " + msQuicLibraryVersion);
            Console.WriteLine("        Lifetime: " + (config.ConnectionLifetime.HasValue ? $"{config.ConnectionLifetime.Value.TotalMilliseconds}ms" : "(infinite)"));
            Console.WriteLine("      Operations: " + string.Join(", ", usedClientOperations.Select(o => o.name)));
            Console.WriteLine("     Random Seed: " + config.RandomSeed);
            Console.WriteLine("    Cancellation: " + 100 * config.CancellationProbability + "%");
            Console.WriteLine("Max Content Size: " + config.MaxContentLength);
            Console.WriteLine("Query Parameters: " + config.MaxParameters);
            Console.WriteLine("   Unobserved Ex: " + (trackUnobservedExceptions ? "Tracked" : "Not tracked"));
            Console.WriteLine();

            if (trackUnobservedExceptions)
            {
                TaskScheduler.UnobservedTaskException += (_, e) =>
                {
                    lock (s_unobservedExceptions)
                    {
                        string text = e.Exception.ToString();
                        s_unobservedExceptions[text] = s_unobservedExceptions.GetValueOrDefault(text) + 1;
                    }
                };
            }

            StressServer? server = null;
            if (config.RunMode.HasFlag(RunMode.server))
            {
                // Start the Kestrel web server in-proc.
                Console.WriteLine($"Starting {(config.UseHttpSys ? "http.sys" : "Kestrel")} server.");
                server = new StressServer(config);
                Console.WriteLine($"Server started at {server.ServerUri}");
            }

            StressClient? client = null;
            if (config.RunMode.HasFlag(RunMode.client))
            {
                // Start the client.
                Console.WriteLine($"Starting {config.ConcurrentRequests} client workers.");

                client = new StressClient(usedClientOperations, config);
                client.Start();
            }

            await WaitUntilMaxExecutionTimeElapsedOrKeyboardInterrupt(config.MaximumExecutionTime);

            client?.Stop();
            client?.PrintFinalReport();

            if (trackUnobservedExceptions)
            {
                PrintUnobservedExceptions();
            }

            // return nonzero status code if there are stress errors
            return client?.TotalErrorCount == 0 && s_unobservedExceptions.Count == 0 ? ExitCode.Success : ExitCode.StressError;
        }

        private static void PrintUnobservedExceptions()
        {
            if (s_unobservedExceptions.Count == 0)
            {
                Console.WriteLine("No unobserved exceptions detected.");
                return;
            }

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Detected {s_unobservedExceptions.Count} unobserved exceptions:");
            Console.ResetColor();

            int i = 1;
            foreach (KeyValuePair<string, int> kv in s_unobservedExceptions.OrderByDescending(p => p.Value))
            {
                Console.WriteLine($"Exception type {i++}/{s_unobservedExceptions.Count} (hit {kv.Value} times):");
                Console.WriteLine(kv.Key);
                Console.WriteLine();
            }
        }

        private static async Task WaitUntilMaxExecutionTimeElapsedOrKeyboardInterrupt(TimeSpan? maxExecutionTime = null)
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

        private static S? Select<T, S>(this T? value, Func<T, S> mapper) where T : struct where S : struct
        {
            return value is null ? null : new S?(mapper(value.Value));
        }
    }
}
