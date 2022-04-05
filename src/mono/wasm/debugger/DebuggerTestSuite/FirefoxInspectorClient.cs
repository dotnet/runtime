// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace Microsoft.WebAssembly.Diagnostics;

class FirefoxInspectorClient : InspectorClient
{
    protected TaskCompletionSource connectToProxy = new TaskCompletionSource();
    TcpClient proxyConnection;
    internal string BreakpointActorId {get; set;}
    internal string ConsoleActorId {get; set;}
    internal string ThreadActorId {get; set;}
    public FirefoxInspectorClient(ILogger logger) : base(logger) 
    {
    }

    public override async Task Connect(
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
            socket.Abort();
            proxyConnection.Close();
        };

        await ConnectWithMainLoops(uri, HandleMessage, token);
        proxyConnection = new TcpClient();
        for (int i = 0 ; i < 10; i++)
        {
            try {
                proxyConnection.Connect("127.0.0.1", 6002);
                break;
            }
            catch (Exception)
            {
                Console.WriteLine("retrying...");
                Thread.Sleep(1000);
            }
        }
        connectToProxy.TrySetResult();
    }

    internal void Send(JObject o, CancellationToken token)
    {
        NetworkStream toStream = proxyConnection.GetStream();
        var msg = o.ToString(Formatting.None);
        var bytes = Encoding.UTF8.GetBytes(msg);
        toStream.Write(Encoding.UTF8.GetBytes($"{bytes.Length}:"));
        toStream.Write(bytes);
        toStream.Flush();
    }

    public override async Task ProcessCommand(Result command, CancellationToken token)
    {
        if (command.Value?["result"]?["value"]?["tabs"] != null)
        {
            var toCmd = command.Value["result"]["value"]["tabs"][0]["actor"].Value<string>();
            var res = await SendCommand("getWatcher", JObject.FromObject(new { type = "getWatcher", isServerTargetSwitchingEnabled = true, to = toCmd}), token);
            var watcherId = res.Value["result"]["value"]["actor"].Value<string>();
            res = await SendCommand("watchResources", JObject.FromObject(new { type = "watchResources", resourceTypes = new JArray("console-message"), to = watcherId}), token);            
            res = await SendCommand("watchTargets", JObject.FromObject(new { type = "watchTargets", targetType = "frame", to = watcherId}), token);
            ThreadActorId = res.Value["result"]["value"]["target"]["threadActor"].Value<string>();
            ConsoleActorId = res.Value["result"]["value"]["target"]["consoleActor"].Value<string>();
            await SendCommand("attach", JObject.FromObject(new 
                { 
                    type = "attach", 
                    options =  JObject.FromObject(new 
                        {
                            pauseOnExceptions = false, 
                            ignoreCaughtExceptions = true, 
                            shouldShowOverlay = true, 
                            shouldIncludeSavedFrames = true,
                            shouldIncludeAsyncLiveFrames = false, 
                            skipBreakpoints = false, 
                            logEventBreakpoints = false, 
                            observeAsmJS = true,
                            breakpoints = new JArray(),
                            eventBreakpoints = new JArray()
                        }),
                    to = ThreadActorId
                }), token);
            res = await SendCommand("getBreakpointListActor", JObject.FromObject(new { type = "getBreakpointListActor", to = watcherId}), token);
            BreakpointActorId = res.Value["result"]["value"]["breakpointList"]["actor"].Value<string>();
        }
    }


    protected override Task HandleMessage(string msg, CancellationToken token)
    {
        var res = JObject.Parse(msg);
        if (res["type"]?.Value<string>() == "newSource")
        {
            var method = res["type"].Value<string>();
            return onEvent(method, res, token);
        }
        if (res["applicationType"] != null)
            return null;
        if (res["resultID"] != null)
        {
            if (res["type"]?.Value<string>() == "evaluationResult")
            {
                var messageId = new FirefoxMessageId("", 0, res["from"].Value<string>());
                if (pending_cmds.Remove(messageId, out var item))
                    item.SetResult(Result.FromJsonFirefox(res));
            }
            return null;
        }
        if (res["from"] != null)
        {
            var messageId = new FirefoxMessageId("", 0, res["from"].Value<string>());
            if (pending_cmds.Remove(messageId, out var item))
            {
                item.SetResult(Result.FromJsonFirefox(res));
                return null;
            }
        }
        if (res["type"] != null)
        {
            var method = res["type"].Value<string>();
            switch (method)
            {
                case "paused":
                {
                    method = "Debugger.paused";
                    break;
                }
                case "resource-available-form":
                {
                    if (res["resources"][0]["resourceType"].Value<string>() == "console-message" /*&& res["resources"][0]["arguments"] != null*/)
                    {
                        method = "Runtime.consoleAPICalled";
                        var args = new JArray();
                        foreach (var argument in res["resources"][0]["message"]["arguments"].Value<JArray>())
                        {
                            args.Add(JObject.FromObject(new { value = argument.Value<string>()}));
                        }
                        res = JObject.FromObject(new 
                            {
                                type =  res["resources"][0]["message"]["level"].Value<string>(), 
                                args
                            });
                    }
                    break;
                }
            }
            return onEvent(method, res, token);
        }
        return null;
    }

    protected override async Task<string> ReadOne(CancellationToken token)
    {
        try
        {
            await connectToProxy.Task;
            while (true)
            {
                byte[] buffer = new byte[1000000];
                var stream = proxyConnection.GetStream();
                int bytesRead = 0;
                while (bytesRead == 0 || Convert.ToChar(buffer[bytesRead - 1]) != ':')
                {
                    var readLen = await stream.ReadAsync(buffer, bytesRead, 1, token);
                    bytesRead++;
                }
                var str = Encoding.UTF8.GetString(buffer, 0, bytesRead - 1);
                int len = int.Parse(str);
                bytesRead = await stream.ReadAsync(buffer, 0, len, token);
                while (bytesRead != len)
                    bytesRead += await stream.ReadAsync(buffer, bytesRead, len - bytesRead, token);
                str = Encoding.UTF8.GetString(buffer, 0, len);
                return str;
            }
        }
        catch (Exception)
        {
            _clientInitiatedClose.TrySetResult();
            return null;
        }
    }

    public override Task<Result> SendCommand(SessionId sessionId, string method, JObject args, CancellationToken token)
    {
        if (args == null)
            args = new JObject();

        var tcs = new TaskCompletionSource<Result>();
        MessageId msgId;
        msgId = new FirefoxMessageId("", 0, args["to"].Value<string>());
        pending_cmds[msgId] = tcs;
        Send(args, token);
        return tcs.Task;
    }
}