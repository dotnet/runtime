// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;

#nullable enable

namespace DebuggerTests
{
    public class TestHarnessProxy
    {
        static IWebHost? host;
        static Task? hostTask;
        static CancellationTokenSource cts = new CancellationTokenSource();
        static object proxyLock = new object();

        public static readonly Uri Endpoint = new Uri("http://localhost:9400");

        // FIXME: use concurrentdictionary?
        // And remove the "used" proxy entries
        private static readonly ConcurrentBag<(string id, DebuggerProxyBase proxy)> s_proxyTable = new();
        private static readonly ConcurrentBag<(string id, Action<RunLoopExitState> handler)> s_exitHandlers = new();
        private static readonly ConcurrentBag<(string id, RunLoopExitState state)> s_statusTable = new();

        public static Task Start(string appPath, string pagePath, string url)
        {
            lock (proxyLock)
            {
                if (hostTask != null)
                    return hostTask;

                host = WebHost.CreateDefaultBuilder()
                    .UseSetting("UseIISIntegration", false.ToString())
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.AddEnvironmentVariables(prefix: "WASM_TESTS_");
                    })
                    .ConfigureLogging(logging =>
                    {
                        logging.AddSimpleConsole(options =>
                                {
                                    options.SingleLine = true;
                                    options.TimestampFormat = "[HH:mm:ss] ";
                                })
                                .AddFilter("DevToolsProxy", LogLevel.Debug)
                                .AddFile(Path.Combine(DebuggerTestBase.TestLogPath, "proxy.log"),
                                            minimumLevel: LogLevel.Trace,
                                            levelOverrides: new Dictionary<string, LogLevel>
                                            {
                                                ["Microsoft.AspNetCore"] = LogLevel.Warning
                                            },
                                            outputTemplate: "{Timestamp:o} [{Level:u3}] {SourceContext}: {Message}{NewLine}{Exception}")
                                .AddFilter(null, LogLevel.Information);
                    })
                .ConfigureServices((ctx, services) =>
                    {
                        services.Configure<TestHarnessOptions>(ctx.Configuration);
                        services.Configure<TestHarnessOptions>(options =>
                        {
                            options.AppPath = appPath;
                            options.PagePath = pagePath;
                            options.DevToolsUrl = new Uri(url);
                        });
                    })
                    .UseStartup<TestHarnessStartup>()
                    .UseUrls(Endpoint.ToString())
                    .Build();
                hostTask = host.StartAsync(cts.Token);
            }

            Console.WriteLine("WebServer Ready!");
            return hostTask;
        }

        public static void RegisterNewProxy(string id, DebuggerProxyBase proxy)
        {
            if (s_proxyTable.Where(t => t.id == id).Any())
                throw new ArgumentException($"Proxy with id {id} already exists");

            s_proxyTable.Add((id, proxy));
        }

        private static bool TryGetProxyById(string id, [NotNullWhen(true)] out DebuggerProxyBase? proxy)
        {
            proxy = null;
            IEnumerable<(string id, DebuggerProxyBase proxy)> found = s_proxyTable.Where(t => t.id == id);
            if (found.Any())
                proxy = found.First().proxy;

            return proxy != null;
        }

        public static void RegisterExitHandler(string id, Action<RunLoopExitState> handler)
        {
            if (s_exitHandlers.Any(t => t.id == id))
                throw new Exception($"Cannot register a duplicate exit handler for {id}");

            s_exitHandlers.Add(new(id, handler));
        }

        public static void RegisterProxyExitState(string id, RunLoopExitState status)
        {
            Console.WriteLine ($"[{id}] RegisterProxyExitState: {status}");
            s_statusTable.Add((id, status));
            (string id, Action<RunLoopExitState> handler)[]? found = s_exitHandlers.Where(e => e.id == id).ToArray();
            if (found.Length > 0)
                found[0].handler.Invoke(status);
        }

        // FIXME: remove
        public static bool TryGetProxyExitState(string id, [NotNullWhen(true)] out RunLoopExitState? state)
        {
            state = new(RunLoopStopReason.Cancelled, null);

            if (!TryGetProxyById(id, out DebuggerProxyBase? proxy))
            {
                (string id, RunLoopExitState state)[]? found = s_statusTable.Where(t => t.id == id).ToArray();
                if (found.Length == 0)
                {
                    Console.WriteLine($"[{id}] Cannot find exit proxy for {id}");
                    return false;
                }

                state = found[0].state;
                return true;
            }

            state = proxy.ExitState;
            return state is not null;
        }

        public static DebuggerProxyBase? ShutdownProxy(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                (_, DebuggerProxyBase? proxy) = s_proxyTable.FirstOrDefault(t => t.id == id);
                if (proxy is not null)
                {
                    proxy.Shutdown();
                    return proxy;
                }
            }
            return null;
        }
    }

}
