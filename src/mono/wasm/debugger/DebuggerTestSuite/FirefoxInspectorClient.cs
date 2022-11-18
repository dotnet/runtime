// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.WebAssembly.Diagnostics;
using System;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.Net;

#nullable enable

namespace DebuggerTests;

class FirefoxInspectorClient : InspectorClient
{
    internal string? BreakpointActorId {get; set;}
    internal string? ConsoleActorId {get; set;}
    internal string? ThreadActorId {get; set;}
    private ClientWebSocket? _clientSocket;

    public FirefoxInspectorClient(ILogger logger) : base(logger)
    {
    }

    protected override async Task<WasmDebuggerConnection> SetupConnection(Uri webserverUri, CancellationToken token)
    {
        _clientSocket = await ConnectToWebServer(webserverUri, token);

        ArraySegment<byte> buff = new(new byte[10]);
        _ = _clientSocket.ReceiveAsync(buff, token)
                        .ContinueWith(async t =>
                        {
                            if (token.IsCancellationRequested)
                                return;

                            logger.LogTrace($"** client socket closed, so stopping the client loop too");
                            // Webserver connection is closed
                            // So, stop the loop here too
                            // _clientInitiatedClose.TrySetResult();
                            await ShutdownAsync(token);
                        }, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.RunContinuationsAsynchronously)
                        .ConfigureAwait(false);

        RunLoopStopped += (_, _) =>
        {
            logger.LogDebug($"RunLoop stopped, closing the websocket, state: {_clientSocket.State}");
            if (_clientSocket.State == WebSocketState.Open)
            {
                _clientSocket.Abort();
            }
        };

        IPEndPoint endpoint = new (IPAddress.Parse("127.0.0.1"), DebuggerTestBase.FirefoxProxyPort);
        try
        {
            TcpClient tcpClient = new();

            logger.LogDebug($"Connecting to the proxy at tcp://{endpoint} ..");
            await tcpClient.ConnectAsync(endpoint, token);
            logger.LogDebug($".. connected to the proxy!");
            return new FirefoxDebuggerConnection(tcpClient, "client", logger);
        }
        catch (SocketException se)
        {
            throw new Exception($"Failed to connect to the proxy at {endpoint}", se);
        }
    }

    public override async Task ProcessCommand(Result command, CancellationToken token)
    {
        if (command.Value?["result"]?["value"]?["tabs"] != null)
        {
            var toCmd = command.Value?["result"]?["value"]?["tabs"]?[0]?["actor"]?.Value<string>();
            var res = await SendCommand("getWatcher", JObject.FromObject(new { type = "getWatcher", isServerTargetSwitchingEnabled = true, to = toCmd}), token);
            var watcherId = res.Value?["result"]?["value"]?["actor"]?.Value<string>();
            res = await SendCommand("watchResources", JObject.FromObject(new { type = "watchResources", resourceTypes = new JArray("console-message"), to = watcherId}), token);
            res = await SendCommand("watchTargets", JObject.FromObject(new { type = "watchTargets", targetType = "frame", to = watcherId}), token);
            UpdateTarget(res.Value?["result"]?["value"]?["target"] as JObject);
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
            BreakpointActorId = res.Value?["result"]?["value"]?["breakpointList"]?["actor"]?.Value<string>();
        }
    }

    protected override Task? HandleMessage(string msg, CancellationToken token)
    {
        var res = JObject.Parse(msg);
        if (res["type"]?.Value<string>() == "newSource")
        {
            var method = res["type"]?.Value<string>();
            return onEvent(method, res, token);
        }

        if (res["type"]?.Value<string>() == "target-available-form" && res["target"] is JObject target)
        {
            UpdateTarget(target);
            return Task.CompletedTask;
        }
        if (res["applicationType"] != null)
            return null;
        if (res["resultID"] != null)
        {
            if (res["type"]?.Value<string>() == "evaluationResult")
            {
                if (res["from"]?.Value<string>() is not string from_str)
                    return null;

                var messageId = new FirefoxMessageId("", 0, from_str);
                if (pending_cmds.Remove(messageId, out var item))
                    item.SetResult(Result.FromJsonFirefox(res));
                else
                    logger.LogDebug($"HandleMessage: Could not find any pending cmd for {messageId}. msg: {msg}");
            }
            return null;
        }
        if (res["from"] is not null)
        {
            if (res["from"]?.Value<string>() is not string from_str)
                return null;

            var messageId = new FirefoxMessageId("", 0, from_str);
            if (pending_cmds.Remove(messageId, out var item))
            {
                item.SetResult(Result.FromJsonFirefox(res));
                return null;
            }
        }
        if (res["type"] != null)
        {
            var method = res["type"]?.Value<string>();
            switch (method)
            {
                case "paused":
                {
                    method = "Debugger.paused";
                    break;
                }
                case "resource-available-form":
                {
                    if (res["resources"]?[0]?["resourceType"]?.Value<string>() == "console-message" /*&& res["resources"][0]["arguments"] != null*/)
                    {
                        method = "Runtime.consoleAPICalled";
                        var args = new JArray();
                        // FIXME: unnecessary alloc
                        foreach (JToken? argument in res["resources"]?[0]?["message"]?["arguments"]?.Value<JArray>() ?? new JArray())
                        {
                            args.Add(JObject.FromObject(new { value = argument.Value<string>()}));
                        }
                        res = JObject.FromObject(new
                            {
                                type =  res["resources"]?[0]?["message"]?["level"]?.Value<string>(),
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

    public override Task<Result> SendCommand(SessionId sessionId, string method, JObject? args, CancellationToken token)
    {
        if (args == null)
            args = new JObject();

        var tcs = new TaskCompletionSource<Result>();
        MessageId msgId;
        if (args["to"]?.Value<string>() is not string to_str)
            throw new Exception($"No 'to' field found in '{args}'");

        msgId = new FirefoxMessageId("", 0, to_str);
        pending_cmds[msgId] = tcs;
        logger.LogTrace($"SendCommand: to: {args}");

        var msg = args.ToString(Formatting.None);
        var bytes = Encoding.UTF8.GetBytes(msg);
        Send(bytes, token);

        return tcs.Task;
    }

    private void UpdateTarget(JObject? target)
    {
        if (target?["threadActor"]?.Value<string>() is string threadActorId)
        {
            ThreadActorId = threadActorId;
            logger.LogTrace($"Updated threadActorId to {threadActorId}");
        }
        if (target?["consoleActor"]?.Value<string>() is string consoleActorId)
        {
            ConsoleActorId = consoleActorId;
            logger.LogTrace($"Updated consoleActorId to {consoleActorId}");
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && _clientSocket?.State == WebSocketState.Open)
        {
            _clientSocket?.Abort();
            _clientSocket?.Dispose();
        }
    }
}
