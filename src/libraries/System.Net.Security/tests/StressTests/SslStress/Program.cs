// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Help;
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
            Console.WriteLine("     Cancellation Pb: " + 100 * config.CancellationProbability + "%");
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

                await client.InitializeAsync();
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
            cmd.Options.Add(new Option<RunMode>("--mode", "-m") { Description = "Stress suite execution mode. Defaults to 'both'.", DefaultValueFactory = (_) => RunMode.both });
            cmd.Options.Add(new Option<double>("--cancellation-probability", "-p") { Description = "Cancellation probability 0 <= p <= 1 for a given connection. Defaults to 0.1", DefaultValueFactory = (_) => 0.1 });
            cmd.Options.Add(new Option<int>("--num-connections", "-n" ) { Description = "Max number of connections to open concurrently.", DefaultValueFactory = (_) => Environment.ProcessorCount });
            cmd.Options.Add(new Option<IPEndPoint>("--server-endpoint", "-e" )
            {
                Description = "Endpoint to bind to if server, endpoint to listen to if client.",
                DefaultValueFactory = (_) => IPEndPoint.Parse("127.0.0.1:5002"),
                CustomParser = result =>
                {
                    try
                    {
                        return ParseEndpoint(result.Tokens[0].Value);
                    }
                    catch
                    {
                        result.AddError($"'{result.Tokens[0].Value}' is not a valid endpoint");
                        return default;
                    }
                }
            });
            cmd.Options.Add(new Option<double?>("--max-execution-time", "-t" ) { Description = "Maximum stress suite execution time, in minutes. Defaults to infinity." });
            cmd.Options.Add(new Option<int>("--max-buffer-length", "-b" ) { Description = "Maximum buffer length to write on ssl stream. Defaults to 8192.", DefaultValueFactory = (_) => 8192 });
            cmd.Options.Add(new Option<int>("--min-connection-lifetime", "-l" ) { Description = "Minimum duration for a single connection, in seconds. Defaults to 5 seconds.", DefaultValueFactory = (_) => 5 });
            cmd.Options.Add(new Option<double>("--max-connection-lifetime", "-L" ) { Description = "Maximum duration for a single connection, in seconds. Defaults to 120 seconds.", DefaultValueFactory = (_) => 120.0 });
            cmd.Options.Add(new Option<double>("--display-interval", "-i" ) { Description = "Client stats display interval, in seconds. Defaults to 5 seconds.", DefaultValueFactory = (_) => 5 });
            cmd.Options.Add(new Option<bool>("--log-server", "-S") { Description = "Print server logs to stdout." });
            cmd.Options.Add(new Option<int>("--seed", "-s" ) { Description = "Seed for generating pseudo-random parameters. Also depends on the -n argument.", DefaultValueFactory = (_) => new Random().Next() });

            ParseResult parseResult = cmd.Parse(args);
            if (parseResult.Errors.Count > 0 || parseResult.Action is HelpAction)
            {
                parseResult.Invoke(); // this is going to print all the errors and help
                config = null;
                return false;
            }

            config = new Configuration()
            {
                RunMode = parseResult.GetValue<RunMode>("--mode"),
                MaxConnections = parseResult.GetValue<int>("--num-connections"),
                CancellationProbability = Math.Max(0, Math.Min(1, parseResult.GetValue<double>("--cancellation-probability"))),
                ServerEndpoint = parseResult.GetValue<IPEndPoint>("--server-endpoint"),
                MaxExecutionTime = parseResult.GetValue<double?>("--max-execution-time")?.Pipe(TimeSpan.FromMinutes),
                MaxBufferLength = parseResult.GetValue<int>("--max-buffer-length"),
                MinConnectionLifetime = TimeSpan.FromSeconds(parseResult.GetValue<double>("--min-connection-lifetime")),
                MaxConnectionLifetime = TimeSpan.FromSeconds(parseResult.GetValue<double>("--max-connection-lifetime")),
                DisplayInterval = TimeSpan.FromSeconds(parseResult.GetValue<double>("--display-interval")),
                LogServer = parseResult.GetValue<bool>("--log-server"),
                RandomSeed = parseResult.GetValue<int>("--seed"),
            };

            if (config.MaxConnectionLifetime < config.MinConnectionLifetime)
            {
                Console.WriteLine("Max connection lifetime should be greater than or equal to min connection lifetime");
                config = null;
                return false;
            }

            return true;

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
                        switch (hostname)
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
