// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class InspectorClient : DevToolsClient
    {
        Dictionary<MessageId, TaskCompletionSource<Result>> pending_cmds = new Dictionary<MessageId, TaskCompletionSource<Result>>();
        Func<string, JObject, CancellationToken, Task> onEvent;
        int next_cmd_id;

        public InspectorClient(ILogger logger) : base(logger) { }

        Task HandleMessage(string msg, CancellationToken token)
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

        public async Task Connect(
            Uri uri,
            Func<string, JObject, CancellationToken, Task> onEvent,
            CancellationToken token)
        {
            this.onEvent = onEvent;

            RunLoopStopped += (_, args) =>
            {
                logger.LogDebug($"Failing {pending_cmds.Count} pending cmds");
                if (args.reason == RunLoopStopReason.Exception)
                {
                    foreach (var cmd in pending_cmds.Values)
                        cmd.SetException(args.ex);
                }
                else
                {
                    foreach (var cmd in pending_cmds.Values)
                        cmd.SetCanceled();
                }
            };

            await ConnectWithMainLoops(uri, HandleMessage, token);
        }

        public Task<Result> SendCommand(string method, JObject args, CancellationToken token)
            => SendCommand(new SessionId(null), method, args, token);

        public Task<Result> SendCommand(SessionId sessionId, string method, JObject args, CancellationToken token)
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
