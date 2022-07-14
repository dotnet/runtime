// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using BrowserDebugProxy;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics;

internal sealed class FirefoxMonoProxy : MonoProxy
{
    public FirefoxMonoProxy(ILogger logger, string loggerId = null, ProxyOptions options = null) : base(logger, null, loggerId: loggerId, options: options)
    {
    }

    public FirefoxExecutionContext GetContextFixefox(SessionId sessionId)
    {
        if (contexts.TryGetValue(sessionId, out ExecutionContext context))
            return context as FirefoxExecutionContext;
        throw new ArgumentException($"Invalid Session: \"{sessionId}\"", nameof(sessionId));
    }

    public async Task RunForFirefox(TcpClient ideClient, int portBrowser, CancellationTokenSource cts)
    {
        TcpClient browserClient = null;
        try
        {
            using var ideConn = new FirefoxDebuggerConnection(ideClient, "ide", logger);
            browserClient = new TcpClient();
            using var browserConn = new FirefoxDebuggerConnection(browserClient, "browser", logger);

            logger.LogDebug($"Connecting to the browser at tcp://127.0.0.1:{portBrowser} ..");
            await browserClient.ConnectAsync("127.0.0.1", portBrowser);
            logger.LogTrace($".. connected to the browser!");

            await RunLoopAsync(ideConn, browserConn, cts);
            if (Stopped?.reason == RunLoopStopReason.Exception)
                ExceptionDispatchInfo.Capture(Stopped.exception).Throw();
        }
        finally
        {
            browserClient?.Close();
            ideClient?.Close();
        }
    }

    protected override async Task OnEvent(SessionId sessionId, JObject parms, CancellationToken token)
    {
        try
        {
            // logger.LogTrace($"OnEvent: {parms}");
            if (!await AcceptEvent(sessionId, parms, token))
            {
                await ForwardMessageToIde(parms, token);
            }
        }
        catch (Exception e)
        {
            _runLoop.Fail(e);
        }
    }

    protected override async Task OnCommand(MessageId id, JObject parms, CancellationToken token)
    {
        try
        {
            // logger.LogDebug($"OnCommand: id: {id}, {parms}");
            if (!await AcceptCommand(id, parms, token))
            {
                await ForwardMessageToBrowser(parms, token);
            }
        }
        catch (Exception e)
        {
            logger.LogError($"OnCommand for id: {id}, {parms} failed: {e}");
            _runLoop.Fail(e);
        }
    }

    protected override void OnResponse(MessageId id, Result result)
    {
        if (pending_cmds.Remove(id, out TaskCompletionSource<Result> task))
        {
            task.SetResult(result);
            return;
        }
        logger.LogError($"Cannot respond to command: {id} with result: {result} - command is not pending");
    }

    protected override Task ProcessBrowserMessage(string msg, CancellationToken token)
    {
        try
        {
            logger.LogTrace($"from-browser: {msg}");
            var res = JObject.Parse(msg);
            if (res["error"] is not null)
                logger.LogDebug($"from-browser: {res}");

            //if (method != "Debugger.scriptParsed" && method != "Runtime.consoleAPICalled")

            if (res["prototype"] != null || res["frames"] != null)
            {
                var msgId = new FirefoxMessageId(null, 0, res["from"].Value<string>());
                // if (pending_cmds.ContainsKey(msgId))
                {
                    // HACK for now, as we don't correctly handle responses yet
                    OnResponse(msgId, Result.FromJsonFirefox(res));
                }
            }
            else if (res["resultID"] == null)
            {
                return OnEvent(res.ToObject<SessionId>(), res, token);
            }
            else if (res["type"] == null || res["type"].Value<string>() != "evaluationResult")
            {
                var o = JObject.FromObject(new
                {
                    type = "evaluationResult",
                    resultID = res["resultID"].Value<string>()
                });
                var id = int.Parse(res["resultID"].Value<string>().Split('-')[1]);
                var msgId = new MessageId(null, id + 1);

                return SendCommandInternal(msgId, "", o, token);
            }
            else if (res["result"] is JObject && res["result"]["type"].Value<string>() == "object" && res["result"]["class"].Value<string>() == "Array")
            {
                var msgIdNew = new FirefoxMessageId(null, 0, res["result"]["actor"].Value<string>());
                var id = int.Parse(res["resultID"].Value<string>().Split('-')[1]);

                var msgId = new FirefoxMessageId(null, id + 1, "");
                var pendingTask = pending_cmds[msgId];
                pending_cmds.Remove(msgId);
                pending_cmds.Add(msgIdNew, pendingTask);
                return SendCommandInternal(msgIdNew, "", JObject.FromObject(new
                                                            {
                                                                type = "prototypeAndProperties",
                                                                to = res["result"]["actor"].Value<string>()
                                                            }), token);
            }
            else
            {
                var id = int.Parse(res["resultID"].Value<string>().Split('-')[1]);
                var msgId = new FirefoxMessageId(null, id + 1, "");
                if (pending_cmds.ContainsKey(msgId))
                    OnResponse(msgId, Result.FromJsonFirefox(res));
                else
                    return SendCommandInternal(msgId, "", res, token);
                return null;
            }
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            _runLoop.Fail(ex);
            throw;
        }
    }

    protected override Task ProcessIdeMessage(string msg, CancellationToken token)
    {
        try
        {
            if (!string.IsNullOrEmpty(msg))
            {
                var res = JObject.Parse(msg);
                Log("protocol", $"from-ide: {GetFromOrTo(res)} {msg}");
                var id = res.ToObject<MessageId>();
                return OnCommand(
                    id,
                    res,
                    token);
            }
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            _runLoop.Fail(ex);
            throw;
        }
    }

    protected override string GetFromOrTo(JObject o)
    {
        if (o?["to"]?.Value<string>() is string to)
            return $"[ to: {to} ]";
        if (o?["from"]?.Value<string>() is string from)
            return $"[ from: {from} ]";
        return string.Empty;
    }

    protected override async Task<Result> SendCommandInternal(SessionId sessionId, string method, JObject args, CancellationToken token)
    {
        // logger.LogTrace($"SendCommandInternal: to-browser: {method}, {args}");
        if (method != null && method != "")
        {
            var tcs = new TaskCompletionSource<Result>();
            MessageId msgId;
            if (method == "evaluateJSAsync")
            {
                int id = GetNewCmdId();
                msgId = new FirefoxMessageId(sessionId.sessionId, id, "");
            }
            else
            {
                msgId = new FirefoxMessageId(sessionId.sessionId, 0, args["to"].Value<string>());
            }
            pending_cmds.Add(msgId, tcs);
            await Send(browser, args, token);

            return await tcs.Task;
        }
        await Send(browser, args, token);
        return await Task.FromResult(Result.OkFromObject(new { }));
    }

    protected override Task SendEventInternal(SessionId sessionId, string method, JObject args, CancellationToken token)
    {
        logger.LogTrace($"to-ide {method}: {args}");
        return method != ""
                ? Send(ide, new JObject(JObject.FromObject(new { type = method })), token)
                : Send(ide, args, token);
    }

    protected override async Task<bool> AcceptEvent(SessionId sessionId, JObject args, CancellationToken token)
    {
        if (args["messages"] != null)
        {
            // FIXME: duplicate, and will miss any non-runtime-ready messages being forwarded
            var messages = args["messages"].Value<JArray>();
            foreach (var message in messages)
            {
                var messageArgs = message["message"]?["arguments"]?.Value<JArray>();
                if (messageArgs != null && messageArgs.Count == 2)
                {
                    if (messageArgs[0].Value<string>() == MonoConstants.RUNTIME_IS_READY && messageArgs[1].Value<string>() == MonoConstants.RUNTIME_IS_READY_ID)
                    {
                        ResetCmdId();
                        await RuntimeReady(sessionId, token);
                    }
                }
            }
            return true;
        }
        if (args["frame"] != null && args["type"] == null)
        {
            OnDefaultContextUpdate(sessionId, new FirefoxExecutionContext(new MonoSDBHelper (this, logger, sessionId), 0, args["frame"]["consoleActor"].Value<string>()));
            return false;
        }

        if (args["resultID"] != null)
            return true;

        if (args["type"] == null)
            return await Task.FromResult(false);
        switch (args["type"].Value<string>())
        {
            case "paused":
                {
                    var ctx = GetContextFixefox(sessionId);
                    var topFunc = args["frame"]["displayName"].Value<string>();
                    switch (topFunc)
                    {
                        case "mono_wasm_fire_debugger_agent_message":
                        case "_mono_wasm_fire_debugger_agent_message":
                            {
                                ctx.PausedOnWasm = true;
                                return await OnReceiveDebuggerAgentEvent(sessionId, args, token);
                            }
                        default:
                            ctx.PausedOnWasm = false;
                            return false;
                    }
                }
            //when debugging from firefox
            case "resource-available-form":
                {
                    var messages = args["resources"].Value<JArray>();
                    foreach (var message in messages)
                    {
                        if (message["resourceType"].Value<string>() == "thread-state" && message["state"].Value<string>() == "paused")
                        {
                            var context = GetContextFixefox(sessionId);
                            if (context.PausedOnWasm)
                            {
                                await SendPauseToBrowser(sessionId, args, token);
                                return true;
                            }
                        }
                        if (message["resourceType"].Value<string>() != "console-message")
                            continue;
                        var messageArgs = message["message"]?["arguments"]?.Value<JArray>();
                        var ctx = GetContextFixefox(sessionId);
                        ctx.GlobalName = args["from"].Value<string>();
                        if (messageArgs != null && messageArgs.Count == 2)
                        {
                            if (messageArgs[0].Value<string>() == MonoConstants.RUNTIME_IS_READY && messageArgs[1].Value<string>() == MonoConstants.RUNTIME_IS_READY_ID)
                            {
                                ResetCmdId();
                                await Task.WhenAll(
                                    ForwardMessageToIde(args, token),
                                    RuntimeReady(sessionId, token));
                            }
                        }
                    }
                    break;
                }
            case "target-available-form":
                {
                    OnDefaultContextUpdate(sessionId, new FirefoxExecutionContext(new MonoSDBHelper (this, logger, sessionId), 0, args["target"]["consoleActor"].Value<string>()));
                    break;
                }
        }
        return false;
    }

    //from ide
    protected override async Task<bool> AcceptCommand(MessageId sessionId, JObject args, CancellationToken token)
    {
        if (args["type"] == null)
            return false;

        switch (args["type"].Value<string>())
        {
            case "resume":
                {
                    if (!contexts.TryGetValue(sessionId, out ExecutionContext context))
                        return false;
                    context.PausedOnWasm = false;
                    if (context.CallStack == null)
                        return false;
                    if (args["resumeLimit"] == null || args["resumeLimit"].Type == JTokenType.Null)
                    {
                        await OnResume(sessionId, token);
                        return false;
                    }
                    switch (args["resumeLimit"]["type"].Value<string>())
                    {
                        case "next":
                            await context.SdbAgent.Step(context.ThreadId, StepKind.Over, token);
                            break;
                        case "finish":
                            await context.SdbAgent.Step(context.ThreadId, StepKind.Out, token);
                            break;
                        case "step":
                            await context.SdbAgent.Step(context.ThreadId, StepKind.Into, token);
                            break;
                    }
                    await SendResume(sessionId, token);
                    return true;
                }
            case "isAttached":
            case "attach":
                {
                    var ctx = GetContextFixefox(sessionId);
                    ctx.ThreadName = args["to"].Value<string>();
                    break;
                }
            case "source":
                {
                    return await OnGetScriptSource(sessionId, args["to"].Value<string>(), token);
                }
            case "getBreakableLines":
                {
                    return await OnGetBreakableLines(sessionId, args["to"].Value<string>(), token);
                }
            case "getBreakpointPositionsCompressed":
                {
                    //{"positions":{"39":[20,28]},"from":"server1.conn2.child10/source27"}
                    if (args["to"].Value<string>().StartsWith("dotnet://"))
                    {
                        var line = new JObject();
                        var offsets = new JArray();
                        offsets.Add(0);
                        line.Add(args["query"]["start"]["line"].Value<string>(), offsets);
                        var o = JObject.FromObject(new
                        {
                            positions = line,
                            from = args["to"].Value<string>()
                        });

                        await SendEventInternal(sessionId, "", o, token);
                        return true;
                    }
                    break;
                }
            case "setBreakpoint":
                {
                    if (!contexts.TryGetValue(sessionId, out ExecutionContext context))
                        return false;
                    var req = JObject.FromObject(new
                    {
                        url = args["location"]["sourceUrl"].Value<string>(),
                        lineNumber = args["location"]["line"].Value<int>() - 1,
                        columnNumber = args["location"]["column"].Value<int>()
                    });

                    var bp = context.BreakpointRequests.Where(request => request.Value.CompareRequest(req)).FirstOrDefault();

                    if (bp.Value != null)
                    {
                        bp.Value.UpdateCondition(args["options"]?["condition"]?.Value<string>());
                        await SendCommand(sessionId, "", args, token);
                        return true;
                    }

                    string bpid = Interlocked.Increment(ref context.breakpointId).ToString();

                    if (args["options"]?["condition"]?.Value<string>() != null)
                        req["condition"] = args["options"]?["condition"]?.Value<string>();

                    var request = BreakpointRequest.Parse(bpid, req);
                    bool loaded = context.Source.Task.IsCompleted;

                    context.BreakpointRequests[bpid] = request;

                    if (await IsRuntimeAlreadyReadyAlready(sessionId, token))
                    {
                        DebugStore store = await RuntimeReady(sessionId, token);

                        Log("verbose", $"BP req {args}");
                        await SetBreakpoint(sessionId, store, request, !loaded, false, token);
                    }
                    await SendCommand(sessionId, "", args, token);
                    return true;
                }
            case "removeBreakpoint":
                {
                    if (!contexts.TryGetValue(sessionId, out ExecutionContext context))
                        return false;
                    Result resp = await SendCommand(sessionId, "", args, token);

                    var reqToRemove = JObject.FromObject(new
                    {
                        url = args["location"]["sourceUrl"].Value<string>(),
                        lineNumber = args["location"]["line"].Value<int>() - 1,
                        columnNumber = args["location"]["column"].Value<int>()
                    });

                    foreach (var req in context.BreakpointRequests.Values)
                    {
                        if (req.CompareRequest(reqToRemove))
                        {
                            foreach (var bp in req.Locations)
                            {
                                var breakpoint_removed = await context.SdbAgent.RemoveBreakpoint(bp.RemoteId, token);
                                if (breakpoint_removed)
                                {
                                    bp.RemoteId = -1;
                                    bp.State = BreakpointState.Disabled;
                                }
                            }
                        }
                    }
                    return true;
                }
            case "prototypeAndProperties":
            case "slice":
                {
                    var to = args?["to"].Value<string>().Replace("propertyIterator", "");
                    if (!DotnetObjectId.TryParse(to, out DotnetObjectId objectId))
                        return false;
                    var res = await RuntimeGetObjectMembers(sessionId, objectId, args, token);
                    var variables = ConvertToFirefoxContent(res);
                    var o = JObject.FromObject(new
                    {
                        ownProperties = variables,
                        from = args["to"].Value<string>()
                    });
                    if (args["type"].Value<string>() == "prototypeAndProperties")
                        o.Add("prototype", GetPrototype(objectId, args));
                    await SendEvent(sessionId, "", o, token);
                    return true;
                }
            case "prototype":
                {
                    if (!DotnetObjectId.TryParse(args?["to"], out DotnetObjectId objectId))
                        return false;
                    var o = JObject.FromObject(new
                    {
                        prototype = GetPrototype(objectId, args),
                        from = args["to"].Value<string>()
                    });
                    await SendEvent(sessionId, "", o, token);
                    return true;
                }
            case "enumSymbols":
                {
                    if (!DotnetObjectId.TryParse(args?["to"], out DotnetObjectId objectId))
                        return false;
                    var o = JObject.FromObject(new
                    {
                        type = "symbolIterator",
                        count = 0,
                        actor = args["to"].Value<string>() + "symbolIterator"
                    });

                    var iterator = JObject.FromObject(new
                    {
                        iterator = o,
                        from = args["to"].Value<string>()
                    });

                    await SendEvent(sessionId, "", iterator, token);
                    return true;
                }
            case "enumProperties":
                {
                    //{"iterator":{"type":"propertyIterator","actor":"server1.conn19.child63/propertyIterator73","count":3},"from":"server1.conn19.child63/obj71"}
                    if (!DotnetObjectId.TryParse(args?["to"], out DotnetObjectId objectId))
                        return false;
                    var res = await RuntimeGetObjectMembers(sessionId, objectId, args, token);
                    var variables = ConvertToFirefoxContent(res);
                    var o = JObject.FromObject(new
                    {
                        type = "propertyIterator",
                        count = variables.Count,
                        actor = args["to"].Value<string>() + "propertyIterator"
                    });

                    var iterator = JObject.FromObject(new
                    {
                        iterator = o,
                        from = args["to"].Value<string>()
                    });

                    await SendEvent(sessionId, "", iterator, token);
                    return true;
                }
            case "getEnvironment":
                {
                    if (!DotnetObjectId.TryParse(args?["to"], out DotnetObjectId objectId))
                        return false;
                    var ctx = GetContextFixefox(sessionId);
                    if (ctx.CallStack == null)
                        return false;
                    Frame scope = ctx.CallStack.FirstOrDefault(s => s.Id == objectId.Value);
                    var res = await RuntimeGetObjectMembers(sessionId, objectId, args, token);
                    var variables = ConvertToFirefoxContent(res);
                    var o = JObject.FromObject(new
                    {
                        actor = args["to"].Value<string>() + "_0",
                        type = "function",
                        scopeKind = "function",
                        function = new
                        {
                            displayName = scope.Method.Name
                        },
                        bindings = new
                        {
                            arguments = new JArray(),
                            variables
                        },
                        from = args["to"].Value<string>()
                    });

                    await SendEvent(sessionId, "", o, token);
                    return true;
                }
            case "frames":
                {
                    ExecutionContext ctx = GetContextFixefox(sessionId);
                    if (ctx.PausedOnWasm)
                    {
                        try
                        {
                            await GetFrames(sessionId, ctx, args, token);
                            return true;
                        }
                        catch (Exception) //if the page is refreshed maybe it stops here.
                        {
                            await SendResume(sessionId, token);
                            return true;
                        }
                    }
                    //var ret = await SendCommand(sessionId, "frames", args, token);
                    //await SendEvent(sessionId, "", ret.Value["result"]["fullContent"] as JObject, token);
                    return false;
                }
            case "evaluateJSAsync":
                {
                    var context = GetContextFixefox(sessionId);
                    if (context.CallStack != null)
                    {
                        var resultID = $"runtimeResult-{context.GetResultID()}";
                        var o = JObject.FromObject(new
                        {
                            resultID,
                            from = args["to"].Value<string>()
                        });
                        await SendEvent(sessionId, "", o, token);

                        Frame scope = context.CallStack.First<Frame>();
                        string expression = args?["text"]?.Value<string>();
                        var osend = JObject.FromObject(new
                        {
                            type = "evaluationResult",
                            resultID,
                            hasException = false,
                            input = args?["text"],
                            from = args["to"].Value<string>()
                        });
                        try
                        {
                            var resolver = new MemberReferenceResolver(this, context, sessionId, scope.Id, logger);
                            JObject retValue = await resolver.Resolve(expression, token);
                            if (retValue == null)
                                retValue = await ExpressionEvaluator.CompileAndRunTheExpression(expression, resolver, logger, token);
                            if (retValue["type"].Value<string>() == "object")
                            {
                                osend["result"] = JObject.FromObject(new
                                {
                                    type = retValue["type"],
                                    @class = retValue["className"],
                                    description = retValue["description"],
                                    actor = retValue["objectId"],
                                });
                            }
                            else
                            {
                                osend["result"] = retValue["value"];
                                osend["resultType"] = retValue["type"];
                                osend["resultDescription"] = retValue["description"];
                            }
                            await SendEvent(sessionId, "", osend, token);
                        }
                        catch (ReturnAsErrorException ree)
                        {
                            osend["hasException"] = true;
                            osend.Add("exception", JObject.FromObject(new
                            {
                                type = "object",
                                @class = ree.Error.Value["result"]["className"],
                                isError = true,
                                preview = JObject.FromObject(new
                                {
                                    kind = "Error",
                                    name = ree.Error.Value["result"]["className"],
                                    message = ree.Error.Value["result"]["description"],
                                    isError = true
                                })
                            }));
                            await SendEvent(sessionId, "", osend, token);
                        }
                        catch (Exception e)
                        {
                            logger.LogDebug($"Error in EvaluateOnCallFrame for expression '{expression}' with '{e}.");
                            osend["hasException"] = true;
                            osend.Add("exception", JObject.FromObject(new
                            {
                                type = "object",
                                @class = "InternalError",
                                isError = true,
                                preview = JObject.FromObject(new
                                {
                                    kind = "Error",
                                    name = "InternalError",
                                    message = e.Message,
                                    isError = true
                                })
                            }));
                            await SendEvent(sessionId, "", osend, token);
                        }
                    }
                    else
                    {
                        var ret = await SendCommand(sessionId, "evaluateJSAsync", args, token);
                        var o = JObject.FromObject(new
                        {
                            resultID = ret.FullContent["resultID"],
                            from = args["to"].Value<string>()
                        });
                        await SendEvent(sessionId, "", o, token);
                        await SendEvent(sessionId, "", ret.FullContent, token);
                    }
                    return true;
                }
            case "DotnetDebugger.getMethodLocation":
                {
                    var ret = await GetMethodLocation(sessionId, args, token);
                    ret.Value["from"] = "internal";
                    await SendEvent(sessionId, "", ret.Value, token);
                    return true;
                }
            default:
                return false;
        }
        return false;
    }

    internal override void SaveLastDebuggerAgentBufferReceivedToContext(SessionId sessionId, Result res)
    {
        var context = GetContextFixefox(sessionId);
        context.LastDebuggerAgentBufferReceived = res;
    }

    private async Task<bool> SendPauseToBrowser(SessionId sessionId, JObject args, CancellationToken token)
    {
        var context = GetContextFixefox(sessionId);
        Result res = context.LastDebuggerAgentBufferReceived;
        if (!res.IsOk)
            return false;

        byte[] newBytes = Convert.FromBase64String(res.Value?["result"]?["value"]?["value"]?.Value<string>());
        using var retDebuggerCmdReader = new MonoBinaryReader(newBytes);
        retDebuggerCmdReader.ReadBytes(11);
        retDebuggerCmdReader.ReadByte();
        var number_of_events = retDebuggerCmdReader.ReadInt32();
        var event_kind = (EventKind)retDebuggerCmdReader.ReadByte();
        if (event_kind == EventKind.Step)
            context.PauseKind = "resumeLimit";
        else if (event_kind == EventKind.Breakpoint)
            context.PauseKind = "breakpoint";

        args["resources"][0]["why"]["type"] = context.PauseKind;
        await SendEvent(sessionId, "", args, token);
        return true;
    }

    private static JObject GetPrototype(DotnetObjectId objectId, JObject args)
    {
        var o = JObject.FromObject(new
        {
            type = "object",
            @class = "Object",
            actor = args?["to"],
            from = args?["to"]
        });
        return o;
    }

    private static JObject ConvertToFirefoxContent(ValueOrError<GetMembersResult> res)
    {
        JObject variables = new JObject();
        //TODO check if res.Error and do something
        var resVars = res.Value.Flatten();
        foreach (var variable in resVars)
        {
            JObject variableDesc;
            if (variable["get"] != null)
            {
                variableDesc = JObject.FromObject(new
                {
                    value = JObject.FromObject(new
                    {
                        @class = variable["value"]?["className"]?.Value<string>(),
                        value = variable["value"]?["description"]?.Value<string>(),
                        actor = variable["get"]["objectId"].Value<string>(),
                        type = "function"
                    }),
                    enumerable = true,
                    configurable = false,
                    actor = variable["get"]["objectId"].Value<string>()
                });
            }
            else if (variable["value"]["objectId"] != null)
            {
                variableDesc = JObject.FromObject(new
                {
                    value = JObject.FromObject(new
                    {
                        @class = variable["value"]?["className"]?.Value<string>(),
                        value = variable["value"]?["description"]?.Value<string>(),
                        actor = variable["value"]["objectId"].Value<string>(),
                        type = variable["value"]?["type"]?.Value<string>() ?? "object"
                    }),
                    enumerable = true,
                    configurable = false,
                    actor = variable["value"]["objectId"].Value<string>()
                });
            }
            else
            {
                variableDesc = JObject.FromObject(new
                {
                    writable = variable["writable"],
                    enumerable = true,
                    configurable = false,
                    type = variable["value"]?["type"]?.Value<string>()
                });
                if (variable["value"]["value"].Type != JTokenType.Null)
                    variableDesc.Add("value", variable["value"]["value"]);
                else //{"type":"null"}
                {
                    variableDesc.Add("value", JObject.FromObject(new {
                        type = "null",
                        @class = variable["value"]["className"]
                    }));
                }
            }
            variables.Add(variable["name"].Value<string>(), variableDesc);
        }
        return variables;
    }

    protected override async Task SendResume(SessionId id, CancellationToken token)
    {
        var ctx = GetContextFixefox(id);
        await SendCommand(id, "", JObject.FromObject(new
        {
            to = ctx.ThreadName,
            type = "resume"
        }), token);
    }

    internal override Task<Result> SendMonoCommand(SessionId id, MonoCommands cmd, CancellationToken token)
    {
        var ctx = GetContextFixefox(id);
        var o = JObject.FromObject(new
        {
            to = ctx.ActorName,
            type = "evaluateJSAsync",
            text = cmd.expression,
            options = new { eager = true, mapped = new { await = true } }
            });
        return SendCommand(id, "evaluateJSAsync", o, token);
    }

    internal override async Task OnSourceFileAdded(SessionId sessionId, SourceFile source, ExecutionContext context, CancellationToken token)
    {
        //different behavior when debugging from VSCode and from Firefox
        var ctx = context as FirefoxExecutionContext;
        logger.LogTrace($"sending {source.Url} {context.Id} {sessionId.sessionId}");
        var obj = JObject.FromObject(new
        {
            actor = source.SourceId.ToString(),
            extensionName = (string)null,
            url = source.Url,
            isBlackBoxed = false,
            introductionType = "scriptElement",
            resourceType = "source",
            dotNetUrl = source.DotNetUrl
        });
        JObject sourcesJObj;
        if (!string.IsNullOrEmpty(ctx.GlobalName))
        {
            sourcesJObj = JObject.FromObject(new
            {
                type = "resource-available-form",
                resources = new JArray(obj),
                from = ctx.GlobalName
            });
        }
        else
        {
            sourcesJObj = JObject.FromObject(new
            {
                type = "newSource",
                source = obj,
                from = ctx.ThreadName
            });
        }
        await SendEvent(sessionId, "", sourcesJObj, token);

        foreach (var req in context.BreakpointRequests.Values)
        {
            if (req.TryResolve(source))
            {
                await SetBreakpoint(sessionId, context.store, req, true, false, token);
            }
        }
    }

    protected override async Task<bool> SendCallStack(SessionId sessionId, ExecutionContext context, string reason, int thread_id, Breakpoint bp, JObject data, JObject args, EventKind event_kind, CancellationToken token)
    {
        Frame frame = null;
        var commandParamsWriter = new MonoBinaryWriter();
        commandParamsWriter.Write(thread_id);
        commandParamsWriter.Write(0);
        commandParamsWriter.Write(1);
        var retDebuggerCmdReader = await context.SdbAgent.SendDebuggerAgentCommand(CmdThread.GetFrameInfo, commandParamsWriter, token);
        var frame_count = retDebuggerCmdReader.ReadInt32();
        if (frame_count > 0)
        {
            var frame_id = retDebuggerCmdReader.ReadInt32();
            var methodId = retDebuggerCmdReader.ReadInt32();
            var il_pos = retDebuggerCmdReader.ReadInt32();
            retDebuggerCmdReader.ReadByte();
            var method = await context.SdbAgent.GetMethodInfo(methodId, token);
            if (method is null)
                return false;

            if (await ShouldSkipMethod(sessionId, context, event_kind, 0, method, token))
            {
                await SendResume(sessionId, token);
                return true;
            }

            SourceLocation location = method?.Info.GetLocationByIl(il_pos);
            if (location == null)
            {
                return false;
            }

            Log("debug", $"frame il offset: {il_pos} method token: {method.Info.Token} assembly name: {method.Info.Assembly.Name}");
            Log("debug", $"\tmethod {method.Name} location: {location}");
            frame = new Frame(method, location, frame_id);
            context.CallStack = new List<Frame>();
            context.CallStack.Add(frame);
        }
        if (!await EvaluateCondition(sessionId, context, frame, bp, token))
        {
            context.ClearState();
            await SendResume(sessionId, token);
            return true;
        }

        args["why"]["type"] = context.PauseKind;

        await SendEvent(sessionId, "", args, token);
        return true;
    }

    private async Task<bool> GetFrames(SessionId sessionId, ExecutionContext context, JObject args, CancellationToken token)
    {
        var ctx = context as FirefoxExecutionContext;
        var orig_callframes = await SendCommand(sessionId, "frames", args, token);

        var callFrames = new List<object>();
        var frames = new List<Frame>();
        var commandParamsWriter = new MonoBinaryWriter();
        commandParamsWriter.Write(context.ThreadId);
        commandParamsWriter.Write(0);
        commandParamsWriter.Write(-1);
        var retDebuggerCmdReader = await context.SdbAgent.SendDebuggerAgentCommand(CmdThread.GetFrameInfo, commandParamsWriter, token);
        var frame_count = retDebuggerCmdReader.ReadInt32();
        for (int j = 0; j < frame_count; j++)
        {
            var frame_id = retDebuggerCmdReader.ReadInt32();
            var methodId = retDebuggerCmdReader.ReadInt32();
            var il_pos = retDebuggerCmdReader.ReadInt32();
            retDebuggerCmdReader.ReadByte();
            MethodInfoWithDebugInformation method = await context.SdbAgent.GetMethodInfo(methodId, token);
            if (method is null)
                continue;

            SourceLocation location = method.Info?.GetLocationByIl(il_pos);
            if (location == null)
            {
                continue;
            }

            Log("debug", $"frame il offset: {il_pos} method token: {method.Info.Token} assembly name: {method.Info.Assembly.Name}");
            Log("debug", $"\tmethod {method.Name} location: {location}");
            frames.Add(new Frame(method, location, frame_id));

            var frameItem = JObject.FromObject(new
            {
                actor = $"dotnet:scope:{frame_id}",
                displayName = method.Name,
                type = "call",
                state = "on-stack",
                asyncCause = (string)null,
                where = new
                {
                    actor = location.Id.ToString(),
                    line = location.Line + 1,
                    column = location.Column
                }
            });
            if (j > 0)
                frameItem.Add("depth", j);
            callFrames.Add(frameItem);

            context.CallStack = frames;
        }
        foreach (JObject frame in orig_callframes.Value["result"]?["value"]?["frames"])
        {
            string function_name = frame["displayName"]?.Value<string>();
            if (function_name != null && !(function_name.StartsWith("Module._mono_wasm", StringComparison.Ordinal) ||
                    function_name.StartsWith("Module.mono_wasm", StringComparison.Ordinal) ||
                    function_name == "mono_wasm_fire_debugger_agent_message" ||
                    function_name == "_mono_wasm_fire_debugger_agent_message" ||
                    function_name == "(wasmcall)"))
            {
                callFrames.Add(frame);
            }
        }
        var o = JObject.FromObject(new
        {
            frames = callFrames,
            from = ctx.ThreadName
        });

        await SendEvent(sessionId, "", o, token);
        return false;
    }
    internal async Task<bool> OnGetBreakableLines(MessageId msg_id, string script_id, CancellationToken token)
    {
        if (!SourceId.TryParse(script_id, out SourceId id))
            return false;

        SourceFile src_file = (await LoadStore(msg_id, false, token)).GetFileById(id);

        await SendEvent(msg_id, "", JObject.FromObject(new { lines = src_file.BreakableLines.ToArray(), from = script_id }), token);
        return true;
    }

    internal override async Task<bool> OnGetScriptSource(MessageId msg_id, string script_id, CancellationToken token)
    {
        if (!SourceId.TryParse(script_id, out SourceId id))
            return false;

        SourceFile src_file = (await LoadStore(msg_id, false, token)).GetFileById(id);

        try
        {
            var uri = new Uri(src_file.Url);
            string source = $"// Unable to find document {src_file.SourceUri}";

            using (Stream data = await src_file.GetSourceAsync(checkHash: false, token: token))
            {
                if (data.Length == 0)
                    return false;

                using (var reader = new StreamReader(data))
                    source = await reader.ReadToEndAsync(token);
            }
            await SendEvent(msg_id, "", JObject.FromObject(new { source, from = script_id }), token);
        }
        catch (Exception e)
        {
            var o = JObject.FromObject(new
            {
                source = $"// Unable to read document ({e.Message})\n" +
                $"Local path: {src_file?.SourceUri}\n" +
                $"SourceLink path: {src_file?.SourceLinkUri}\n",
                from = script_id
            });

            await SendEvent(msg_id, "", o, token);
        }
        return true;
    }

    internal override Task<DebugStore> LoadStore(SessionId sessionId, bool tryUseDebuggerProtocol, CancellationToken token)
        => base.LoadStore(sessionId, false, token);
}
