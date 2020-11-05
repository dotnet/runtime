// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;

namespace DebuggerTests
{
    class Inspector
    {
        // InspectorClient client;
        Dictionary<string, TaskCompletionSource<JObject>> notifications = new Dictionary<string, TaskCompletionSource<JObject>>();
        Dictionary<string, Func<JObject, CancellationToken, Task>> eventListeners = new Dictionary<string, Func<JObject, CancellationToken, Task>>();

        public const string PAUSE = "pause";
        public const string READY = "ready";

        public Task<JObject> WaitFor(string what)
        {
            if (notifications.ContainsKey(what))
                throw new Exception($"Invalid internal state, waiting for {what} while another wait is already setup");
            var n = new TaskCompletionSource<JObject>();
            notifications[what] = n;
            return n.Task;
        }

        void NotifyOf(string what, JObject args)
        {
            if (!notifications.ContainsKey(what))
                throw new Exception($"Invalid internal state, notifying of {what}, but nobody waiting");
            notifications[what].SetResult(args);
            notifications.Remove(what);
        }

        public void On(string evtName, Func<JObject, CancellationToken, Task> cb)
        {
            eventListeners[evtName] = cb;
        }

        void FailAllWaitersWithException(JObject exception)
        {
            foreach (var tcs in notifications.Values)
                tcs.SetException(new ArgumentException(exception.ToString()));
        }

        async Task OnMessage(string method, JObject args, CancellationToken token)
        {
            //System.Console.WriteLine("OnMessage " + method + args);
            switch (method)
            {
                case "Debugger.paused":
                    NotifyOf(PAUSE, args);
                    break;
                case "Mono.runtimeReady":
                    NotifyOf(READY, args);
                    break;
                case "Runtime.consoleAPICalled":
                    Console.WriteLine("CWL: {0}", args?["args"]);
                    break;
            }
            if (eventListeners.ContainsKey(method))
                await eventListeners[method](args, token);
            else if (String.Compare(method, "Runtime.exceptionThrown") == 0)
                FailAllWaitersWithException(args);
        }

        public async Task Ready(Func<InspectorClient, CancellationToken, Task> cb = null, TimeSpan? span = null)
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(span?.Milliseconds ?? 60 * 1000); //tests have 1 minute to complete by default
                var uri = new Uri($"ws://{TestHarnessProxy.Endpoint.Authority}/launch-chrome-and-connect");
                using var loggerFactory = LoggerFactory.Create(
                    builder => builder.AddConsole().AddFilter(null, LogLevel.Information));
                using (var client = new InspectorClient(loggerFactory.CreateLogger<Inspector>()))
                {
                    await client.Connect(uri, OnMessage, async token =>
                    {
                        Task[] init_cmds = {
                    client.SendCommand("Profiler.enable", null, token),
                    client.SendCommand("Runtime.enable", null, token),
                    client.SendCommand("Debugger.enable", null, token),
                    client.SendCommand("Runtime.runIfWaitingForDebugger", null, token),
                    WaitFor(READY),
                        };
                        // await Task.WhenAll (init_cmds);
                        Console.WriteLine("waiting for the runtime to be ready");
                        await init_cmds[4];
                        Console.WriteLine("runtime ready, TEST TIME");
                        if (cb != null)
                        {
                            Console.WriteLine("await cb(client, token)");
                            await cb(client, token);
                        }

                    }, cts.Token);
                    await client.Close(cts.Token);
                }
            }
        }
    }
}
