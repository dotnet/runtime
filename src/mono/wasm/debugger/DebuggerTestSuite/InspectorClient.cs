// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;

namespace DebuggerTests
{
    internal class InspectorClient : DevToolsClient
    {
        protected Dictionary<MessageId, TaskCompletionSource<Result>> pending_cmds = new Dictionary<MessageId, TaskCompletionSource<Result>>();
        protected Func<string, JObject, CancellationToken, Task> onEvent;
        protected int next_cmd_id;

        public InspectorClient(ILogger logger) : base(logger) { }

        protected override async Task<WasmDebuggerConnection> SetupConnection(Uri webserverUri, CancellationToken token)
            => new DevToolsDebuggerConnection(
                        await ConnectToWebServer(webserverUri, token),
                        "client",
                         logger);

        protected virtual Task HandleMessage(string msg, CancellationToken token)
        {
            var res = JObject.Parse(msg);

            if (res["id"] == null)
                return onEvent(res["method"].Value<string>(), res["params"] as JObject, token);

            var id = res.ToObject<MessageId>();
            if (!pending_cmds.Remove(id, out var item))
                logger.LogError($"Unable to find command {id}");

            item.SetResult(Result.FromJson(res));
            return null;
        }

        public virtual async Task ProcessCommand(Result command, CancellationToken token)
        {
            await Task.FromResult(true);
        }

        public virtual async Task Connect(
            Uri uri,
            Func<string, JObject, CancellationToken, Task> onEvent,
            CancellationTokenSource cts)
        {
            this.onEvent = onEvent;

            RunLoopStopped += (_, args) =>
            {
                logger.LogDebug($"Failing {pending_cmds.Count} pending cmds");
                if (args.reason == RunLoopStopReason.Exception)
                {
                    foreach (var cmd in pending_cmds.Values)
                        cmd.SetException(args.exception);
                }
                else
                {
                    foreach (var cmd in pending_cmds.Values)
                        cmd.SetCanceled();
                }
            };

            await ConnectWithMainLoops(uri, HandleMessage, cts);
        }

        public Task<Result> SendCommand(string method, JObject args, CancellationToken token)
            => SendCommand(new SessionId(null), method, args, token);

        public virtual Task<Result> SendCommand(SessionId sessionId, string method, JObject args, CancellationToken token)
        {
            int id = ++next_cmd_id;
            if (args == null)
                args = new JObject();

            var o = JObject.FromObject(new
            {
                id = id,
                method = method,
                @params = args
            });

            var tcs = new TaskCompletionSource<Result>();
            pending_cmds[new MessageId(sessionId.sessionId, id)] = tcs;

            var str = o.ToString();

            var bytes = Encoding.UTF8.GetBytes(str);
            Send(bytes, token);
            return tcs.Task;
        }
    }
}
