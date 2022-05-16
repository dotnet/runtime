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

        private static readonly ConcurrentDictionary<string, WeakReference<DebuggerProxyBase>> s_proxyTable = new();
        private static readonly ConcurrentDictionary<int, WeakReference<Action<RunLoopExitState>>> s_exitHandlers = new();
        private static readonly ConcurrentDictionary<string, RunLoopExitState> s_statusTable = new();

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
            if (s_proxyTable.ContainsKey(id))
                throw new ArgumentException($"Proxy with id {id} already exists");

            s_proxyTable[id] = new WeakReference<DebuggerProxyBase>(proxy);
        }

        public static void RegisterExitHandler(string id, Action<RunLoopExitState> handler)
        {
            int intId = int.Parse(id);
            if (s_exitHandlers.ContainsKey(intId))
                throw new Exception($"Cannot register a duplicate exit handler for {id}");

            s_exitHandlers[intId] = new WeakReference<Action<RunLoopExitState>>(handler);
        }

        public static void RegisterProxyExitState(string id, RunLoopExitState status)
        {
            int intId = int.Parse(id);
            s_statusTable[id] = status;
            // we have the explicit state now, so we can drop the reference
            // to the proxy
            s_proxyTable.TryRemove(id, out WeakReference<DebuggerProxyBase> _);

            if (s_exitHandlers.TryRemove(intId, out WeakReference<Action<RunLoopExitState>>? handlerRef)
                && handlerRef.TryGetTarget(out Action<RunLoopExitState>? handler))
            {
                handler(status);
            }
        }

        // FIXME: remove
        public static bool TryGetProxyExitState(string id, [NotNullWhen(true)] out RunLoopExitState? state)
        {
            state = null;

            if (s_proxyTable.TryGetValue(id, out WeakReference<DebuggerProxyBase>? proxyRef) && proxyRef.TryGetTarget(out DebuggerProxyBase? proxy))
            {
                state = proxy.ExitState;
            }
            else if (!s_statusTable.TryGetValue(id, out state))
            {
                Console.WriteLine($"[{id}] Cannot find exit proxy for {id}");
                state = null;
            }

            return state is not null;
        }

        public static DebuggerProxyBase? ShutdownProxy(string id)
        {
            if (!string.IsNullOrEmpty(id)
                && s_proxyTable.TryGetValue(id, out WeakReference<DebuggerProxyBase>? proxyRef)
                && proxyRef.TryGetTarget(out DebuggerProxyBase? proxy))
            {
                proxy.Shutdown();
                return proxy;
            }
            return null;
        }
    }

}
