// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class FirefoxMonoProxy : MonoProxy
    {
        private int portBrowser;
        private TcpClient ide;
        private TcpClient browser;
        private bool pausedOnWasm;
        private string actorName = "";
        private string threadName = "";
        private string globalName = "";

        public FirefoxMonoProxy(ILoggerFactory loggerFactory, int portBrowser) : base(loggerFactory, null)
        {
            this.portBrowser = portBrowser;
        }

        private async Task<string> ReadOne(TcpClient socket, CancellationToken token)
        {
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1000000];
                    var stream = socket.GetStream();
                    int bytesRead = 0;
                    while (bytesRead == 0 || Convert.ToChar(buffer[bytesRead - 1]) != ':')
                    {
                        var readLen = await stream.ReadAsync(buffer, bytesRead, 1, token);
                        bytesRead++;
                    }
                    var str = Encoding.ASCII.GetString(buffer, 0, bytesRead - 1);
                    int len = int.Parse(str);
                    bytesRead = await stream.ReadAsync(buffer, 0, len, token);
                    while (bytesRead != len)
                        bytesRead += await stream.ReadAsync(buffer, bytesRead, len - bytesRead, token);
                    str = Encoding.ASCII.GetString(buffer, 0, len);
                    Console.WriteLine($"{len}:{str}");
                    return str;
                }
            }
            catch (Exception)
            {
                client_initiated_close.TrySetResult();
                return null;
            }
        }

        public async void Run(TcpClient ideClient)
        {
            ide = ideClient;
            browser = new TcpClient();
            browser.Connect("127.0.0.1", portBrowser);

            var x = new CancellationTokenSource();

            pending_ops.Add(ReadOne(browser, x.Token));
            pending_ops.Add(ReadOne(ide, x.Token));
            pending_ops.Add(side_exception.Task);
            pending_ops.Add(client_initiated_close.Task);

            try
            {
                while (!x.IsCancellationRequested)
                {
                    Task task = await Task.WhenAny(pending_ops.ToArray());

                    if (client_initiated_close.Task.IsCompleted)
                    {
                        await client_initiated_close.Task.ConfigureAwait(false);
                        x.Cancel();

                        break;
                    }

                    //logger.LogTrace ("pump {0} {1}", task, pending_ops.IndexOf (task));
                    if (task == pending_ops[0])
                    {
                        string msg = ((Task<string>)task).Result;
                        if (msg != null)
                        {
                            pending_ops[0] = ReadOne(browser, x.Token); //queue next read
                            ProcessBrowserMessage(msg, x.Token);
                        }
                    }
                    else if (task == pending_ops[1])
                    {
                        string msg = ((Task<string>)task).Result;
                        if (msg != null)
                        {
                            pending_ops[1] = ReadOne(ide, x.Token); //queue next read
                            ProcessIdeMessage(msg, x.Token);
                        }
                    }
                    else if (task == pending_ops[2])
                    {
                        bool res = ((Task<bool>)task).Result;
                        throw new Exception("side task must always complete with an exception, what's going on???");
                    }
                    else
                    {
                        //must be a background task
                        pending_ops.Remove(task);
                        DevToolsQueue queue = GetQueueForTask(task);
                        if (queue != null)
                        {
                            if (queue.TryPumpIfCurrentCompleted(x.Token, out Task tsk))
                                pending_ops.Add(tsk);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log("error", $"DevToolsProxy::Run: Exception {e}");
                //throw;
            }
            finally
            {
                if (!x.IsCancellationRequested)
                    x.Cancel();
            }
        }

        internal void Send(TcpClient to, JObject o, CancellationToken token)
        {
            NetworkStream toStream = to.GetStream();

            var msg = o.ToString(Formatting.None);
            msg = $"{msg.Length}:{msg}";
            toStream.Write(Encoding.ASCII.GetBytes(msg), 0, msg.Length);
            toStream.Flush();
        }

        internal override async Task OnEvent(SessionId sessionId, JObject parms, CancellationToken token)
        {
            try
            {
                if (!await AcceptEvent(sessionId, parms, token))
                {
                    //logger.LogDebug ("proxy browser: {0}::{1}",method, args);
                    SendEventInternal(sessionId, "", parms, token);
                }
            }
            catch (Exception e)
            {
                side_exception.TrySetException(e);
            }
        }

        internal override async Task OnCommand(MessageId id, JObject parms, CancellationToken token)
        {
            try
            {
                if (!await AcceptCommand(id, parms, token))
                {
                    await SendCommandInternal(id, "", parms, token);
                }
            }
            catch (Exception e)
            {
                side_exception.TrySetException(e);
            }
        }

        internal override void OnResponse(MessageId id, Result result)
        {
            //logger.LogTrace ("got id {0} res {1}", id, result);
            // Fixme
            if (pending_cmds.Remove(id, out TaskCompletionSource<Result> task))
            {
                task.SetResult(result);
                return;
            }
            logger.LogError("Cannot respond to command: {id} with result: {result} - command is not pending", id, result);
        }

        internal override void ProcessBrowserMessage(string msg, CancellationToken token)
        {
            var res = JObject.Parse(msg);

            //if (method != "Debugger.scriptParsed" && method != "Runtime.consoleAPICalled")
            Log("protocol", $"browser: {msg}");

            if (res["resultID"] == null)
                pending_ops.Add(OnEvent(res.ToObject<SessionId>(), res, token));
            else
            {
                if (res["type"] == null || res["type"].Value<string>() != "evaluationResult")
                {
                    var o = JObject.FromObject(new
                    {
                        type = "evaluationResult",
                        resultID = res["resultID"].Value<string>()
                    });
                    var id = int.Parse(res["resultID"].Value<string>().Split('-')[1]);
                    var msgId = new MessageId(null, id + 1);

                    SendCommandInternal(msgId, "", o, token);
                }
                else
                {
                    var id = int.Parse(res["resultID"].Value<string>().Split('-')[1]);
                    var msgId = new MessageId(null, id + 1);

                    OnResponse(msgId, Result.FromJsonFirefox(res));

                }
                //{"type":"evaluationResult","resultID":"1634575904746-0","hasException":false,"input":"ret = 10","result":10,"startTime":1634575904746,"timestamp":1634575904748,"from":"server1.conn21.child10/consoleActor2"}
            }
        }

        internal override void ProcessIdeMessage(string msg, CancellationToken token)
        {
            Log("protocol", $"ide: {msg}");
            if (!string.IsNullOrEmpty(msg))
            {
                var res = JObject.Parse(msg);
                var id = res.ToObject<MessageId>();
                pending_ops.Add(OnCommand(
                    id,
                    res,
                    token));
            }
        }

        internal override async Task<Result> SendCommand(SessionId id, string method, JObject args, CancellationToken token)
        {
            //Log ("verbose", $"sending command {method}: {args}");
            return await SendCommandInternal(id, method, args, token);
        }

        internal override Task<Result> SendCommandInternal(SessionId sessionId, string method, JObject args, CancellationToken token)
        {
            if (method == "evaluateJSAsync")
            {
                int id = Interlocked.Increment(ref next_cmd_id);
                var tcs = new TaskCompletionSource<Result>();
                var msgId = new MessageId(sessionId.sessionId, id);
                //Log ("verbose", $"add cmd id {sessionId}-{id}");
                pending_cmds[msgId] = tcs;
                Send(this.browser, args, token);

                return tcs.Task;
            }
            Send(this.browser, args, token);
            return Task.FromResult(Result.OkFromObject(new { }));
        }

        internal override void SendEvent(SessionId sessionId, string method, JObject args, CancellationToken token)
        {
            //Log ("verbose", $"sending event {method}: {args}");
            SendEventInternal(sessionId, method, args, token);
        }

        internal override void SendEventInternal(SessionId sessionId, string method, JObject args, CancellationToken token)
        {
            if (method != "")
            {
                Console.WriteLine("O que faremos");
                return;
            }
            Send(this.ide, args, token);
        }

        protected override async Task<bool> AcceptEvent(SessionId sessionId, JObject args, CancellationToken token)
        {
            if (args["messages"] != null)
            {
                var messages = args["messages"].Value<JArray>();
                foreach (var message in messages)
                {
                    var messageArgs = message["message"]?["arguments"]?.Value<JArray>();
                    if (messageArgs != null && messageArgs.Count == 2)
                    {
                        if (messageArgs[0].Value<string>() == MonoConstants.RUNTIME_IS_READY && messageArgs[1].Value<string>() == MonoConstants.RUNTIME_IS_READY_ID)
                        {
                            await OnDefaultContext(sessionId, new ExecutionContext { Id = 0, AuxData = actorName }, token);
                            await RuntimeReady(sessionId, token);
                        }
                    }
                }
                return true;
            }
            if (args["frame"] != null && args["type"] == null)
            {
                actorName = args["frame"]["consoleActor"].Value<string>();
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
                        var topFunc = args["frame"]["displayName"].Value<string>();
                        switch (topFunc)
                        {
                            case "mono_wasm_fire_debugger_agent_message":
                            case "_mono_wasm_fire_debugger_agent_message":
                                {
                                    pausedOnWasm = true;
                                    return false;
                                }
                        }
                        break;
                    }
                //when debugging from firefox
                case "resource-available-form":
                    {
                        var messages = args["resources"].Value<JArray>();
                        foreach (var message in messages)
                        {
                            if (message["resourceType"].Value<string>() != "console-message")
                                continue;
                            var messageArgs = message["message"]?["arguments"]?.Value<JArray>();
                            globalName = args["from"].Value<string>();
                            if (messageArgs != null && messageArgs.Count == 2)
                            {
                                if (messageArgs[0].Value<string>() == MonoConstants.RUNTIME_IS_READY && messageArgs[1].Value<string>() == MonoConstants.RUNTIME_IS_READY_ID)
                                {
                                    await OnDefaultContext(sessionId, new ExecutionContext { Id = 0, AuxData = actorName }, token);
                                    await RuntimeReady(sessionId, token);
                                }
                            }
                        }
                        break;
                    }
            }
            return false;
        }

        //from ide
        protected override async Task<bool> AcceptCommand(MessageId sessionId, JObject args, CancellationToken token)
        {
            if (args["type"] == null)
                return await Task.FromResult(false);

            switch (args["type"].Value<string>())
            {
                case "resume":
                    {
                        if (!contexts.TryGetValue(sessionId, out ExecutionContext context))
                            return false;
                        if (args["resumeLimit"]?["type"]?.Value<string>() != null)
                        {
                            await OnResume(sessionId, token);
                            return true;
                        }
                        switch (args["resumeLimit"]["type"].Value<string>())
                        {
                            case "next":
                                await SdbHelper.Step(sessionId, context.ThreadId, StepKind.Over, token);
                                break;
                            case "finish":
                                await SdbHelper.Step(sessionId, context.ThreadId, StepKind.Out, token);
                                break;
                            case "step":
                                await SdbHelper.Step(sessionId, context.ThreadId, StepKind.Into, token);
                                break;
                        }
                        await SendResume(sessionId, token);
                        return true;
                    }
                case "attach":
                    {
                        threadName = args["to"].Value<string>();
                        break;
                    }
                case "source":
                    {
                        if (args["to"].Value<string>().StartsWith("dotnet://"))
                        {
                            return await OnGetScriptSource(sessionId, args["to"].Value<string>(), token);
                        }
                        break;
                    }
                case "getBreakableLines":
                    {
                        if (args["to"].Value<string>().StartsWith("dotnet://"))
                        {
                            return await OnGetBreakableLines(sessionId, args["to"].Value<string>(), token);
                        }
                        break;
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

                            SendEventInternal(sessionId, "", o, token);
                            return true;
                        }
                        break;
                    }
                case "setBreakpoint":
                    {
                        if (!contexts.TryGetValue(sessionId, out ExecutionContext context))
                            return false;
                        Result resp = await SendCommand(sessionId, "", args, token);

                        if (args["location"]["sourceUrl"].Value<string>().EndsWith(".cs"))
                        {
                            string bpid = "";

                            var req = JObject.FromObject(new
                            {
                                url = args["location"]["sourceUrl"].Value<string>(),
                                lineNumber = args["location"]["line"].Value<int>(),
                                columnNumber = args["location"]["column"].Value<int>()
                            });

                            var request = BreakpointRequest.Parse(bpid, req);
                            bool loaded = context.Source.Task.IsCompleted;

                            if (await IsRuntimeAlreadyReadyAlready(sessionId, token))
                            {
                                DebugStore store = await RuntimeReady(sessionId, token);

                                Log("verbose", $"BP req {args}");
                                await SetBreakpoint(sessionId, store, request, !loaded, token);
                            }

                            if (loaded)
                            {
                                context.BreakpointRequests[bpid] = request;
                            }
                            var o = JObject.FromObject(new
                            {
                                from = args["to"].Value<string>()
                            });
                            //SendEventInternal(id, "", o, token);
                            return true;
                        }
                        break;
                    }
                case "prototypeAndProperties":
                case "slice":
                    {
                        var to = args?["to"].Value<string>().Replace("propertyIterator", "");
                        if (!DotnetObjectId.TryParse(to, out DotnetObjectId objectId))
                            return false;
                        var res = await RuntimeGetPropertiesInternal(sessionId, objectId, args, token);
                        var variables = ConvertToFirefoxContent(res);
                        var o = JObject.FromObject(new
                        {
                            ownProperties = variables,
                            from = args["to"].Value<string>()
                        });
                        if (args["type"].Value<string>() == "prototypeAndProperties")
                            o.Add("prototype", GetPrototype(objectId, args));
                        SendEvent(sessionId, "", o, token);
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
                        SendEvent(sessionId, "", o, token);
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

                        SendEvent(sessionId, "", iterator, token);
                        return true;
                    }
                case "enumProperties":
                    {
                        //{"iterator":{"type":"propertyIterator","actor":"server1.conn19.child63/propertyIterator73","count":3},"from":"server1.conn19.child63/obj71"}
                        if (!DotnetObjectId.TryParse(args?["to"], out DotnetObjectId objectId))
                            return false;
                        var res = await RuntimeGetPropertiesInternal(sessionId, objectId, args, token);
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

                        SendEvent(sessionId, "", iterator, token);
                        return true;
                    }
                case "getEnvironment":
                    {
                        if (!DotnetObjectId.TryParse(args?["to"], out DotnetObjectId objectId))
                            return false;
                        ExecutionContext ctx = GetContext(sessionId);
                        Frame scope = ctx.CallStack.FirstOrDefault(s => s.Id == int.Parse(objectId.Value));
                        var res = await RuntimeGetPropertiesInternal(sessionId, objectId, args, token);
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

                        SendEvent(sessionId, "", o, token);
                        return true;
                    }
                case "frames":
                    {
                        if (pausedOnWasm)
                        {
                                try
                                {
                                    return await OnReceiveDebuggerAgentEvent(sessionId, args, token);
                                }
                                catch (Exception) //if the page is refreshed maybe it stops here.
                                {
                                    await SendResume(sessionId, token);
                                    return true;
                                }
                        }
                        return false;
                    }
                default:
                    return false;
            }
            return false;
        }

        private JObject GetPrototype(DotnetObjectId objectId, JObject args)
        {
            var o = JObject.FromObject(new
            {
                type = "object",
                @class = objectId.Scheme,
                actor = args?["to"],
                from = args?["to"]
            });
            return o;
        }

        private JObject ConvertToFirefoxContent(JToken res)
        {
            JObject variables = new JObject();
            foreach (var variable in res)
            {
                JObject variableDesc;
                if (variable["value"]["objectId"] != null)
                {
                    variableDesc = JObject.FromObject(new
                    {
                        value = JObject.FromObject(new
                        {
                            @class = variable["value"]?["description"]?.Value<string>(),
                            actor = variable["value"]["objectId"].Value<string>(),
                            type = "object"
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
                        value = variable["value"]["value"],
                        enumerable = true,
                        configurable = false
                    });
                }
                variables.Add(variable["name"].Value<string>(), variableDesc);
            }
            return variables;
        }
        private async Task SendResume(SessionId id, CancellationToken token)
        {
            await SendCommand(id, "", JObject.FromObject(new
            {
                to = threadName,
                type = "resume"
            }), token);
        }
        internal override Task<Result> SendMonoCommand(SessionId id, MonoCommands cmd, CancellationToken token)
        {
            // {"to":"server1.conn0.child10/consoleActor2","type":"evaluateJSAsync","text":"console.log(\"oi thays \")","frameActor":"server1.conn0.child10/frame36"}
            var o = JObject.FromObject(new
            {
                to = actorName,
                type = "evaluateJSAsync",
                text = cmd.expression
            });
            return SendCommand(id, "evaluateJSAsync", o, token);
        }

        internal override async Task OnSourceFileAdded(SessionId sessionId, SourceFile source, ExecutionContext context, CancellationToken token)
        {
            //different behavior when debugging from VSCode and from Firefox
            Log("debug", $"sending {source.Url} {context.Id} {sessionId.sessionId}");
            var obj = JObject.FromObject(new
            {
                actor = source.SourceId.ToString(),
                extensionName = (string)null,
                url = source.Url,
                isBlackBoxed = false,
                introductionType = "scriptElement",
                resourceType = "source"
            });
            JObject sourcesJObj;
            if (globalName != "")
            {
                sourcesJObj = JObject.FromObject(new
                {
                    type = "resource-available-form",
                    resources = new JArray(obj),
                    from = globalName
                });
            }
            else
            {
                sourcesJObj = JObject.FromObject(new
                {
                    type = "newSource",
                    source = obj,
                    from = threadName
                });
            }
            SendEvent(sessionId, "", sourcesJObj, token);

            foreach (var req in context.BreakpointRequests.Values)
            {
                if (req.TryResolve(source))
                {
                    await SetBreakpoint(sessionId, context.store, req, true, token);
                }
            }
        }

        protected override async Task<bool> SendCallStack(SessionId sessionId, ExecutionContext context, string reason, int thread_id, Breakpoint bp, JObject data, JObject args, CancellationToken token)
        {
            var orig_callframes = args?["callFrames"]?.Values<JObject>();
            var callFrames = new List<object>();
            var frames = new List<Frame>();
            var commandParams = new MemoryStream();
            var commandParamsWriter = new MonoBinaryWriter(commandParams);
            commandParamsWriter.Write(thread_id);
            commandParamsWriter.Write(0);
            commandParamsWriter.Write(-1);
            var retDebuggerCmdReader = await SdbHelper.SendDebuggerAgentCommand<CmdThread>(sessionId, CmdThread.GetFrameInfo, commandParams, token);
            var frame_count = retDebuggerCmdReader.ReadInt32();
            for (int j = 0; j < frame_count; j++)
            {
                var frame_id = retDebuggerCmdReader.ReadInt32();
                var methodId = retDebuggerCmdReader.ReadInt32();
                var il_pos = retDebuggerCmdReader.ReadInt32();
                var flags = retDebuggerCmdReader.ReadByte();
                DebugStore store = await LoadStore(sessionId, token);
                var method = await SdbHelper.GetMethodInfo(sessionId, methodId, token);

                SourceLocation location = method?.Info.GetLocationByIl(il_pos);
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
                        line = location.Line,
                        column = location.Column
                    }
                });
                if (j > 0)
                    frameItem.Add("depth", j);
                callFrames.Add(frameItem);

                context.CallStack = frames;
                context.ThreadId = thread_id;
            }
            string[] bp_list = new string[bp == null ? 0 : 1];
            if (bp != null)
                bp_list[0] = bp.StackId;
            if (orig_callframes != null)
            {
                foreach (JObject frame in orig_callframes)
                {
                    string function_name = frame["functionName"]?.Value<string>();
                    string url = frame["url"]?.Value<string>();
                    if (!(function_name.StartsWith("wasm-function", StringComparison.Ordinal) ||
                            url.StartsWith("wasm://wasm/", StringComparison.Ordinal) || function_name == "_mono_wasm_fire_debugger_agent_message"))
                    {
                        callFrames.Add(frame);
                    }
                }
            }
            var o = JObject.FromObject(new
            {
                frames = callFrames,
                from = threadName
            });
            if (!await EvaluateCondition(sessionId, context, context.CallStack.First(), bp, token))
            {
                context.ClearState();
                await SendResume(sessionId, token);
                return true;
            }
            SendEvent(sessionId, "", o, token);
            return true;
        }
        internal async Task<bool> OnGetBreakableLines(MessageId msg_id, string script_id, CancellationToken token)
        {
            if (!SourceId.TryParse(script_id, out SourceId id))
                return false;

            SourceFile src_file = (await LoadStore(msg_id, token)).GetFileById(id);

            SendEvent(msg_id, "", JObject.FromObject(new { lines = src_file.BreakableLines.ToArray(), from = script_id }), token);
            return true;
        }

        internal override async Task<bool> OnGetScriptSource(MessageId msg_id, string script_id, CancellationToken token)
        {
            if (!SourceId.TryParse(script_id, out SourceId id))
                return false;

            SourceFile src_file = (await LoadStore(msg_id, token)).GetFileById(id);

            try
            {
                var uri = new Uri(src_file.Url);
                string source = $"// Unable to find document {src_file.SourceUri}";

                using (Stream data = await src_file.GetSourceAsync(checkHash: false, token: token))
                {
                    if (data.Length == 0)
                        return false;

                    using (var reader = new StreamReader(data))
                        source = await reader.ReadToEndAsync();
                }
                SendEvent(msg_id, "", JObject.FromObject(new { source, from = script_id }), token);
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

                SendEvent(msg_id, "", o, token);
            }
            return true;
        }

    }
}
