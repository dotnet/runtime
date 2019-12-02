// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SslStress.Utils;

namespace SslStress
{
    public static class Program
    {
        public enum ExitCode { Success = 0, StressError = 1, CliError = 2 };

        public static async Task<int> Main(string[] args)
        {
            if (!TryParseCli(args, out Configuration? config))
            {
                return (int)ExitCode.CliError;
            }

            return (int)await Run(config);
        }

        private static async Task<ExitCode> Run(Configuration config)
        {
            if ((config.RunMode & RunMode.both) == 0)
            {
                Console.Error.WriteLine("Must specify a valid run mode");
                return ExitCode.CliError;
            }

            static string GetAssemblyInfo(Assembly assembly) => $"{assembly.Location}, modified {new FileInfo(assembly.Location).LastWriteTime}";

            Console.WriteLine("           .NET Core: " + GetAssemblyInfo(typeof(object).Assembly));
            Console.WriteLine(" System.Net.Security: " + GetAssemblyInfo(typeof(System.Net.Security.SslStream).Assembly));
            Console.WriteLine("     Server Endpoint: " + config.ServerEndpoint);
            Console.WriteLine("         Concurrency: " + config.MaxConnections);
            Console.WriteLine("  Max Execution Time: " + ((config.MaxExecutionTime != null) ? config.MaxExecutionTime.Value.ToString() : "infinite"));
            Console.WriteLine("  Min Conn. Lifetime: " + config.MinConnectionLifetime);
            Console.WriteLine("  Max Conn. Lifetime: " + config.MaxConnectionLifetime);
            Console.WriteLine("         Random Seed: " + config.RandomSeed);
            Console.WriteLine();

            StressServer? server = null;
            if (config.RunMode.HasFlag(RunMode.server))
            {
                // Start the SSL web server in-proc.
                Console.WriteLine($"Starting SSL server.");
                server = new StressServer(config);
                server.Start();

                Console.WriteLine($"Server listening to {server.ServerEndpoint}");
            }

            StressClient? client = null;
            if (config.RunMode.HasFlag(RunMode.client))
            {
                // Start the client.
                Console.WriteLine($"Starting {config.MaxConnections} client workers.");
                Console.WriteLine();

                client = new StressClient(config);
                client.Start();
            }

            await WaitUntilMaxExecutionTimeElapsedOrKeyboardInterrupt(config.MaxExecutionTime);

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

        private static bool TryParseCli(string[] args, [NotNullWhen(true)] out Configuration? config)
        {
            var cmd = new RootCommand();
            cmd.AddOption(new Option(new[] { "--help", "-h" }, "Display this help text."));
            cmd.AddOption(new Option(new[] { "--mode", "-m" }, "Stress suite execution mode. Defaults to 'both'.") { Argument = new Argument<RunMode>("runMode", RunMode.both) });
            cmd.AddOption(new Option(new[] { "--num-connections", "-n" }, "Max number of connections to open concurrently.") { Argument = new Argument<int>("connections", Environment.ProcessorCount) });
            cmd.AddOption(new Option(new[] { "--server-endpoint", "-e" }, "Endpoint to bind to if server, endpoint to listen to if client.") { Argument = new Argument<string>("ipEndpoint", "127.0.0.1:5002") });
            cmd.AddOption(new Option(new[] { "--max-execution-time", "-t" }, "Maximum stress suite execution time, in minutes. Defaults to infinity.") { Argument = new Argument<double?>("minutes", null) });
            cmd.AddOption(new Option(new[] { "--max-buffer-length", "-b" }, "Maximum buffer length to write on ssl stream. Defaults to 8192.") { Argument = new Argument<int>("bytes", 8192) });
            cmd.AddOption(new Option(new[] { "--min-connection-lifetime", "-l" }, "Minimum duration for a single connection, in seconds. Defaults to 5 seconds.") { Argument = new Argument<double>("seconds", 5) });
            cmd.AddOption(new Option(new[] { "--max-connection-lifetime", "-L" }, "Maximum duration for a single connection, in seconds. Defaults to 120 seconds.") { Argument = new Argument<double>("seconds", 120) });
            cmd.AddOption(new Option(new[] { "--display-interval", "-i" }, "Client stats display interval, in seconds. Defaults to 5 seconds.") { Argument = new Argument<double>("seconds", 5) });
            cmd.AddOption(new Option(new[] { "--log-server", "-S" }, "Print server logs to stdout."));
            cmd.AddOption(new Option(new[] { "--seed", "-s" }, "Seed for generating pseudo-random parameters. Also depends on the -n argument.") { Argument = new Argument<int>("seed", (new Random().Next())) });

            ParseResult parseResult = cmd.Parse(args);
            if (parseResult.Errors.Count > 0 || parseResult.HasOption("-h"))
            {
                foreach (ParseError error in parseResult.Errors)
                {
                    Console.WriteLine(error);
                }
                WriteHelpText();
                config = null;
                return false;
            }

            config = new Configuration()
            {
                RunMode = parseResult.ValueForOption<RunMode>("-m"),
                MaxConnections = parseResult.ValueForOption<int>("-n"),
                ServerEndpoint = ParseEndpoint(parseResult.ValueForOption<string>("-e")),
                MaxExecutionTime = parseResult.ValueForOption<double?>("-t")?.Pipe(TimeSpan.FromMinutes),
                MaxBufferLength = parseResult.ValueForOption<int>("-b"),
                MinConnectionLifetime = TimeSpan.FromSeconds(parseResult.ValueForOption<double>("-l")),
                MaxConnectionLifetime = TimeSpan.FromSeconds(parseResult.ValueForOption<double>("-L")),
                DisplayInterval = TimeSpan.FromSeconds(parseResult.ValueForOption<double>("-i")),
                LogServer = parseResult.HasOption("-S"),
                RandomSeed = parseResult.ValueForOption<int>("-s"),
            };

            if (config.MaxConnectionLifetime < config.MinConnectionLifetime)
            {
                Console.WriteLine("Max connection lifetime should be greater than or equal to min connection lifetime");
                WriteHelpText();
                config = null;
                return false;
            }

            return true;

            void WriteHelpText()
            {
                Console.WriteLine();
                new HelpBuilder(new SystemConsole()).Write(cmd);
            }

            static IPEndPoint ParseEndpoint(string value)
            {
                try
                {
                    return IPEndPoint.Parse(value);
                }
                catch (FormatException)
                {
                    // support hostname:port endpoints
                    Match match = Regex.Match(value, "^([^:]+):([0-9]+)$");
                    if (match.Success)
                    {
                        string hostname = match.Groups[1].Value;
                        int port = int.Parse(match.Groups[2].Value);
                        switch(hostname)
                        {
                            case "+":
                            case "*":
                                return new IPEndPoint(IPAddress.Any, port);
                            default:
                                IPAddress[] addresses = Dns.GetHostAddresses(hostname);
                                return new IPEndPoint(addresses[0], port);
                        }
                    }

                    throw;
                }
            }
        }
    }
}
