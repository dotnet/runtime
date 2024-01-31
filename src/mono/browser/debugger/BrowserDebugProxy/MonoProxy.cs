// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using BrowserDebugProxy;
using static System.Formats.Asn1.AsnWriter;
using System.Reflection;
using System.Collections.Concurrent;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class MonoProxy : DevToolsProxy
    {
        internal List<string> UrlSymbolServerList { get; private set; }
        internal string CachePathSymbolServer { get; private set; }
        private readonly HashSet<SessionId> sessions = new HashSet<SessionId>();
        private static readonly string[] s_executionContextIndependentCDPCommandNames = { "DotnetDebugger.setDebuggerProperty", "DotnetDebugger.runTests" };
        internal ConcurrentExecutionContextDictionary Contexts = new ();

        public static HttpClient HttpClient => new HttpClient();

        // index of the runtime in a same JS page/process
        public int RuntimeId { get; private init; }
        public bool JustMyCode { get; private set; }
        private PauseOnExceptionsKind _defaultPauseOnExceptions { get; set; }

        public MonoProxy(ILogger logger, int runtimeId = 0, string loggerId = "", ProxyOptions options = null) : base(options, logger, loggerId)
        {
            UrlSymbolServerList = new List<string>();
            RuntimeId = runtimeId;
            _defaultPauseOnExceptions = PauseOnExceptionsKind.Unset;
            JustMyCode = options?.JustMyCode ?? false;
        }

        internal virtual Task<Result> SendMonoCommand(SessionId id, MonoCommands cmd, CancellationToken token) => SendCommand(id, "Runtime.evaluate", JObject.FromObject(cmd), token);

        internal void SendLog(SessionId sessionId, string message, CancellationToken token, string type = "warning")
        {
            if (!Contexts.TryGetCurrentExecutionContextValue(sessionId, out ExecutionContext context))
                return;
            /*var o = JObject.FromObject(new
            {
                entry = JObject.FromObject(new
                {
                    source = "recommendation",
                    level = "warning",
                    text = message
                })
            });
            SendEvent(id, "Log.enabled", null, token);
            SendEvent(id, "Log.entryAdded", o, token);*/
            var o = JObject.FromObject(new
            {
                type,
                args = new JArray(JObject.FromObject(new
                {
                    type = "string",
                    value = message,
                })),
                executionContextId = context.Id
            });
            SendEvent(sessionId, "Runtime.consoleAPICalled", o, token);
        }

        protected override async Task<bool> AcceptEvent(SessionId sessionId, JObject parms, CancellationToken token)
        {
            var method = parms["method"].Value<string>();
            var args = parms["params"] as JObject;
            switch (method)
            {
                case "Runtime.consoleAPICalled":
                    {
                        // Don't process events from sessions we aren't tracking
                        if (!Contexts.ContainsKey(sessionId))
                            return false;
                        string type = args["type"]?.ToString();
                        if (type == "debug")
                        {
                            JToken a = args["args"];
                            if (a is null)
                                break;

                            int aCount = a.Count();
                            if (aCount > 1 && a[0]?["value"]?.ToString() == MonoConstants.EVENT_RAISED)
                            {
                                if (a.Type != JTokenType.Array)
                                {
                                    logger.LogDebug($"Invalid event raised args, expected an array: {a.Type}");
                                }
                                else
                                {
                                    if (aCount > 2 &&
                                        JObjectTryParse(a?[2]?["value"]?.Value<string>(), out JObject raiseArgs) &&
                                        JObjectTryParse(a?[1]?["value"]?.Value<string>(), out JObject eventArgs))
                                    {
                                        await OnJSEventRaised(sessionId, eventArgs, token);

                                        if (raiseArgs?["trace"]?.Value<bool>() == true) {
                                            // Let the message show up on the console
                                            return false;
                                        }
                                    }
                                }

                                // Don't log this message in the console
                                return true;
                            }
                        }
                        break;
                    }

                case "Runtime.executionContextCreated":
                    {
                        await SendEvent(sessionId, method, args, token);
                        JToken ctx = args?["context"];
                        var aux_data = ctx?["auxData"] as JObject;
                        int id = ctx["id"].Value<int>();
                        if (aux_data != null)
                        {
                            bool? is_default = aux_data["isDefault"]?.Value<bool>();
                            if (is_default == true)
                            {
                                await OnDefaultContext(sessionId, new ExecutionContext(new MonoSDBHelper(this, logger, sessionId), id, aux_data, _defaultPauseOnExceptions), token);
                            }
                        }
                        return true;
                    }
                case "Runtime.executionContextDestroyed":
                    {
                        Contexts.DestroyContext(sessionId, args["executionContextId"].Value<int>());
                        return false;
                    }
                case "Runtime.executionContextsCleared":
                    {
                        Contexts.ClearContexts(sessionId);
                        return false;
                    }
                case "Debugger.scriptParsed":
                    {
                        try
                        {
                            var url = args["url"]?.ToString();
                            if (url?.Contains("/_framework/") == true)//it is from dotnet runtime framework
                            {
                                if (Contexts.TryGetCurrentExecutionContextValue(sessionId, out ExecutionContext context))
                                    context.FrameworkScriptList.Add(args["scriptId"].Value<int>());
                                return false;
                            }
                            if (url?.Equals("") == false)
                                return false;
                            var callStack = args["stackTrace"]?["callFrames"]?.Value<JArray>();
                            var topFrameFunctionName = callStack?.Count > 0 ? callStack?[0]?["functionName"]?.Value<string>() : null;
                            //skip mono_wasm_fire_debugger_agent_message_with_data_to_pause or mono_wasm_runtime_ready (both of them have debugger; statement)
                            if (topFrameFunctionName?.StartsWith("mono_wasm_", StringComparison.OrdinalIgnoreCase) == true)
                                return true;
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug($"Debugger.scriptParsed - {args} - failed with exception: {ex}");
                        }
                        return false;
                    }
                case "Debugger.paused":
                    {
                        return await OnDebuggerPaused(sessionId, args, token);
                    }

                case "Debugger.breakpointResolved":
                    {
                        break;
                    }

                case "Target.attachedToTarget":
                    {
                        var targetType = args["targetInfo"]["type"]?.ToString();
                        if (targetType == "page")
                            await AttachToTarget(new SessionId(args["sessionId"]?.ToString()), token);
                        else if (targetType == "worker")
                            Contexts.CreateWorkerExecutionContext(new SessionId(args["sessionId"]?.ToString()), new SessionId(parms["sessionId"]?.ToString()), logger);
                        break;
                    }

                case "Target.targetDestroyed":
                    {
                        await SendMonoCommand(sessionId, MonoCommands.DetachDebugger(RuntimeId), token);
                        break;
                    }
            }
            return false;
        }

        protected async Task<bool> OnDebuggerPaused(SessionId sessionId, JObject args, CancellationToken token)
        {
            if (args?["callFrames"]?.Value<JArray>()?.Count == 0) //new browser version can send pause of type "instrumentation" with an empty callstack
                return false;

            if (args["asyncStackTraceId"] != null)
            {
                if (!Contexts.TryGetCurrentExecutionContextValue(sessionId, out ExecutionContext context))
                    return false;
                if (context.CopyDataFromParentContext())
                {
                    var store = await LoadStore(sessionId, true, token);
                    foreach (var source in store.AllSources())
                    {
                        await OnSourceFileAdded(sessionId, source, context, token, false);
                    }
                }
            }

            //TODO figure out how to stich out more frames and, in particular what happens when real wasm is on the stack
            string top_func = args?["callFrames"]?[0]?["functionName"]?.Value<string>();
            switch (top_func) {
                // keep function names un-mangled via src\mono\browser\runtime\rollup.config.js
                case "mono_wasm_set_entrypoint_breakpoint":
                case "_mono_wasm_set_entrypoint_breakpoint":
                    {
                        await OnSetEntrypointBreakpoint(sessionId, args, token);
                        return true;
                    }
                case "mono_wasm_runtime_ready":
                case "_mono_wasm_runtime_ready":
                    {
                        await RuntimeReady(sessionId, token);
                        await SendResume(sessionId, token);
                        if (!JustMyCode)
                            await ReloadSymbolsFromSymbolServer(sessionId, Contexts.GetCurrentContext(sessionId), token);
                        return true;
                    }
                case "mono_wasm_fire_debugger_agent_message_with_data_to_pause":
                case "_mono_wasm_fire_debugger_agent_message_with_data_to_pause":
                    try
                    {
                        return await OnReceiveDebuggerAgentEvent(sessionId, args, await GetLastDebuggerAgentBuffer(sessionId, args, token), token);
                    }
                    catch (Exception) //if the page is refreshed maybe it stops here.
                    {
                        await SendResume(sessionId, token);
                        return true;
                    }
                case "mono_wasm_fire_debugger_agent_message_with_data":
                case "_mono_wasm_fire_debugger_agent_message_with_data":
                    {
                        //the only reason that we would get pause in this method is because the user is stepping out
                        //and as we don't want to pause in a debugger related function we continue stepping out
                        await SendCommand(sessionId, "Debugger.stepOut", new JObject(), token);
                        return true;
                    }
                default:
                    {
                        if (JustMyCode)
                        {
                            if (!Contexts.TryGetCurrentExecutionContextValue(sessionId, out ExecutionContext context) || !context.IsRuntimeReady)
                                return false;
                            //avoid pausing when justMyCode is enabled and it's a wasm function
                            if (args?["callFrames"]?[0]?["scopeChain"]?[0]?["type"]?.Value<string>()?.Equals("wasm-expression-stack") == true)
                            {
                                await SendCommand(sessionId, "Debugger.stepOut", new JObject(), token);
                                return true;
                            }
                            //avoid pausing when justMyCode is enabled and it's a framework function
                            var scriptId = args?["callFrames"]?[0]?["location"]?["scriptId"]?.Value<int>();
                            if (!context.IsSkippingHiddenMethod && !context.IsSteppingThroughMethod && scriptId is not null && context.FrameworkScriptList.Contains(scriptId.Value))
                            {
                                await SendCommand(sessionId, "Debugger.stepOut", new JObject(), token);
                                return true;
                            }
                        }
                        break;
                    }
            }
            return false;
        }

        protected virtual async Task SendResume(SessionId id, CancellationToken token)
        {
            await SendCommand(id, "Debugger.resume", new JObject(), token);
        }
        protected async Task<bool> IsRuntimeAlreadyReadyAlready(SessionId sessionId, CancellationToken token)
        {
            if (Contexts.TryGetCurrentExecutionContextValue(sessionId, out ExecutionContext context) && context.IsRuntimeReady)
                return true;

            Result res = await SendMonoCommand(sessionId, MonoCommands.IsRuntimeReady(RuntimeId), token);
            if (!res.IsOk || res.Value?["result"]?["value"]?.Type != JTokenType.Boolean) //if runtime is not ready this may be the response
                return false;
            return res.Value?["result"]?["value"]?.Value<bool>() ?? false;
        }
        private static PauseOnExceptionsKind GetPauseOnExceptionsStatusFromString(string state)
        {
            PauseOnExceptionsKind pauseOnException;
            if (Enum.TryParse(state, true, out pauseOnException))
                return pauseOnException;
            return PauseOnExceptionsKind.Unset;
        }

        protected override async Task<bool> AcceptCommand(MessageId id, JObject parms, CancellationToken token)
        {
            var method = parms["method"].Value<string>();
            var args = parms["params"] as JObject;
            // Inspector doesn't use the Target domain or sessions
            // so we try to init immediately
            if (id == SessionId.Null)
                await AttachToTarget(id, token);

            if (!Contexts.TryGetCurrentExecutionContextValue(id, out ExecutionContext context) && !s_executionContextIndependentCDPCommandNames.Contains(method))
            {
                if (method == "Debugger.setPauseOnExceptions")
                {
                    string state = args["state"].Value<string>();
                    var pauseOnException = GetPauseOnExceptionsStatusFromString(state);
                    if (pauseOnException != PauseOnExceptionsKind.Unset)
                        _defaultPauseOnExceptions = pauseOnException;
                }
                return method.StartsWith("DotnetDebugger.", StringComparison.OrdinalIgnoreCase);
            }

            switch (method)
            {
                case "Target.attachToTarget":
                    {
                        Result resp = await SendCommand(id, method, args, token);
                        await AttachToTarget(new SessionId(resp.Value["sessionId"]?.ToString()), token);
                        break;
                    }

                case "Debugger.enable":
                    {
                        Result resp = await SendCommand(id, method, args, token);

                        if (!resp.IsOk)
                        {
                            SendResponse(id, resp, token);
                            return true;
                        }

                        context.DebugId = resp.Value["DebugId"]?.ToString();

                        if (await IsRuntimeAlreadyReadyAlready(id, token))
                            await RuntimeReady(id, token);

                        SendResponse(id, resp, token);
                        return true;
                    }

                case "Debugger.getScriptSource":
                    {
                        string script = args?["scriptId"]?.Value<string>();
                        return await OnGetScriptSource(id, script, token);
                    }

                case "Runtime.compileScript":
                    {
                        string exp = args?["expression"]?.Value<string>();
                        if (exp.StartsWith("//dotnet:", StringComparison.Ordinal))
                        {
                            OnCompileDotnetScript(id, token);
                            return true;
                        }
                        break;
                    }

                case "Debugger.getPossibleBreakpoints":
                    {
                        Result resp = await SendCommand(id, method, args, token);
                        if (resp.IsOk && resp.Value["locations"].HasValues)
                        {
                            SendResponse(id, resp, token);
                            return true;
                        }

                        var start = SourceLocation.Parse(args?["start"] as JObject);
                        //FIXME support variant where restrictToFunction=true and end is omitted
                        var end = SourceLocation.Parse(args?["end"] as JObject);
                        if (start != null && end != null && await GetPossibleBreakpoints(id, start, end, token))
                            return true;

                        SendResponse(id, resp, token);
                        return true;
                    }

                case "Debugger.setBreakpoint":
                    {
                        break;
                    }

                case "Debugger.setBreakpointByUrl":
                    {
                        Result resp = await SendCommand(id, method, args, token);
                        if (!resp.IsOk)
                        {
                            SendResponse(id, resp, token);
                            return true;
                        }
                        try
                        {
                            string bpid = resp.Value["breakpointId"]?.ToString();
                            IEnumerable<object> locations = resp.Value["locations"]?.Values<object>();
                            var request = BreakpointRequest.Parse(bpid, args);

                            // is the store done loading?
                            bool loaded = context.Source.Task.IsCompleted;
                            if (!loaded)
                            {
                                // Send and empty response immediately if not
                                // and register the breakpoint for resolution
                                context.BreakpointRequests[bpid] = request;
                                SendResponse(id, resp, token);
                            }

                            if (await IsRuntimeAlreadyReadyAlready(id, token))
                            {
                                DebugStore store = await RuntimeReady(id, token);

                                Log("verbose", $"BP req {args}");
                                await SetBreakpoint(id, store, request, !loaded, false, token);
                            }

                            if (loaded)
                            {
                                // we were already loaded so we should send a response
                                // with the locations included and register the request
                                context.BreakpointRequests[bpid] = request;
                                var result = Result.OkFromObject(request.AsSetBreakpointByUrlResponse(locations));
                                SendResponse(id, result, token);

                            }
                        }
                        catch (Exception e)
                        {
                            logger.LogDebug($"Debugger.setBreakpointByUrl - {args} - failed with exception: {e}");
                            SendResponse(id, Result.Err($"Debugger.setBreakpointByUrl - {args} - failed with exception: {e}"), token);
                        }
                        return true;
                    }

                case "Debugger.removeBreakpoint":
                    {
                        await RemoveBreakpoint(id, args, false, token);
                        break;
                    }

                case "Debugger.resume":
                    {
                        await OnResume(id, token);
                        break;
                    }

                case "Debugger.stepInto":
                    {
                        return await Step(id, StepKind.Into, token);
                    }
                case "Debugger.setVariableValue":
                    {
                        if (!DotnetObjectId.TryParse(args?["callFrameId"], out DotnetObjectId objectId))
                            return false;
                        switch (objectId.Scheme)
                        {
                            case "scope":
                                return await OnSetVariableValue(id,
                                    objectId.Value,
                                    args?["variableName"]?.Value<string>(),
                                    args?["newValue"],
                                    token);
                            default:
                                return false;
                        }
                    }

                case "Debugger.stepOut":
                    {
                        return await Step(id, StepKind.Out, token);
                    }

                case "Debugger.stepOver":
                    {
                        return await Step(id, StepKind.Over, token);
                    }
                case "Runtime.evaluate":
                    {
                        if (context.CallStack != null)
                        {
                            Frame scope = context.CallStack.First<Frame>();
                            return await OnEvaluateOnCallFrame(id,
                                    scope.Id,
                                    args?["expression"]?.Value<string>(), token);
                        }
                        break;
                    }
                case "Debugger.evaluateOnCallFrame":
                    {
                        if (!DotnetObjectId.TryParse(args?["callFrameId"], out DotnetObjectId objectId))
                            return false;

                        switch (objectId.Scheme)
                        {
                            case "scope":
                                return await OnEvaluateOnCallFrame(id,
                                    objectId.Value,
                                    args?["expression"]?.Value<string>(), token);
                            default:
                                return false;
                        }
                    }

                case "Runtime.getProperties":
                    {
                        if (!DotnetObjectId.TryParse(args?["objectId"], out DotnetObjectId objectId))
                            break;

                        var valueOrError = await RuntimeGetObjectMembers(id, objectId, args, token, true);
                        if (valueOrError.IsError)
                        {
                            logger.LogDebug($"Runtime.getProperties: {valueOrError.Error}");
                            SendResponse(id, valueOrError.Error.Value, token);
                            return true;
                        }
                        if (valueOrError.Value.JObject == null)
                        {
                            SendResponse(id, Result.Err($"Failed to get properties for '{objectId}'"), token);
                            return true;
                        }
                        SendResponse(id, Result.OkFromObject(valueOrError.Value.JObject), token);
                        return true;
                    }

                case "Runtime.releaseObject":
                    {
                        if (!(DotnetObjectId.TryParse(args["objectId"], out DotnetObjectId objectId) && objectId.Scheme == "cfo_res"))
                            break;

                        await SendMonoCommand(id, MonoCommands.ReleaseObject(RuntimeId, objectId), token);
                        SendResponse(id, Result.OkFromObject(new { }), token);
                        return true;
                    }

                case "Debugger.setPauseOnExceptions":
                    {
                        string state = args["state"].Value<string>();
                        var pauseOnException = GetPauseOnExceptionsStatusFromString(state);
                        if (pauseOnException != PauseOnExceptionsKind.Unset)
                            context.PauseOnExceptions = pauseOnException;

                        if (context.IsRuntimeReady)
                            await context.SdbAgent.EnableExceptions(context.PauseOnExceptions, token);
                        // Pass this on to JS too
                        return false;
                    }

                case "Runtime.callFunctionOn":
                    {
                        try {
                            return await CallOnFunction(id, args, token);
                        }
                        catch (Exception ex) {
                            logger.LogDebug($"Runtime.callFunctionOn failed for {id} with args {args}: {ex}");
                            SendResponse(id,
                                Result.Exception(new ArgumentException(
                                    $"Runtime.callFunctionOn not supported with ({args["objectId"]}).")),
                                token);
                            return true;
                        }
                    }

                // Protocol extensions
                case "DotnetDebugger.setDebuggerProperty":
                    {
                        foreach (KeyValuePair<string, JToken> property in args)
                        {
                            switch (property.Key)
                            {
                                case "JustMyCodeStepping":
                                    await SetJustMyCode(id, (bool)property.Value, context, token);
                                    break;
                                default:
                                    logger.LogDebug($"DotnetDebugger.setDebuggerProperty failed for {property.Key} with value {property.Value}");
                                    break;
                            }
                        }
                        return true;
                    }
                case "DotnetDebugger.setNextIP":
                    {
                        var loc = SourceLocation.Parse(args?["location"] as JObject);
                        if (loc == null)
                            return false;
                        bool ret = await OnSetNextIP(id, loc, token);
                        if (ret)
                            SendResponse(id, Result.OkFromObject(new { }), token);
                        else
                            SendResponse(id, Result.Err("Set next instruction pointer failed."), token);
                        return true;
                    }
                case "DotnetDebugger.applyUpdates":
                    {
                        if (await ApplyUpdates(id, args, token))
                            SendResponse(id, Result.OkFromObject(new { }), token);
                        else
                            SendResponse(id, Result.Err("ApplyUpdate failed."), token);
                        return true;
                    }
                case "DotnetDebugger.setSymbolOptions":
                    {
                        SendResponse(id, Result.OkFromObject(new { }), token);
                        CachePathSymbolServer = args["symbolOptions"]?["cachePath"]?.Value<string>();
                        var urls = args["symbolOptions"]?["searchPaths"]?.Value<JArray>();
                        if (urls == null)
                            return true;
                        UrlSymbolServerList.Clear();
                        UrlSymbolServerList.AddRange(urls.Values<string>());
                        if (!JustMyCode)
                        {
                            if (!await IsRuntimeAlreadyReadyAlready(id, token))
                                return true;
                            return await ReloadSymbolsFromSymbolServer(id, context, token);
                        }
                        return true;
                    }
                case "DotnetDebugger.getMethodLocation":
                    {
                        SendResponse(id, await GetMethodLocation(id, args, token), token);
                        return true;
                    }
                case "DotnetDebugger.setEvaluationOptions":
                    {
                        //receive the available options from DAP to variables, stack and evaluate commands.
                        try {
                            if (args["options"]?["noFuncEval"]?.Value<bool>() == true)
                                context.AutoEvaluateProperties = false;
                            else
                                context.AutoEvaluateProperties = true;
                            SendResponse(id, Result.OkFromObject(new { }), token);
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug($"DotnetDebugger.setEvaluationOptions failed for {id} with args {args}: {ex}");
                            SendResponse(id,
                                Result.Exception(new ArgumentException(
                                    $"DotnetDebugger.setEvaluationOptions got incorrect argument ({args})")),
                                token);
                        }
                        return true;
                    }
                case "DotnetDebugger.runTests":
                    {
                        SendResponse(id, Result.OkFromObject(new { }), token);
                        while (!await IsRuntimeAlreadyReadyAlready(id, token)) //retry on debugger-tests until the runtime is ready
                            await Task.Delay(1000, token);
                        await RuntimeReady(id, token);
                        return true;
                    }
            }
            // for Dotnetdebugger.* messages, treat them as handled, thus not passing them on to the browser
            return method.StartsWith("DotnetDebugger.", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> ReloadSymbolsFromSymbolServer(SessionId id, ExecutionContext context, CancellationToken token)
        {
            DebugStore store = await LoadStore(id, true, token);
            store.UpdateSymbolStore(UrlSymbolServerList, CachePathSymbolServer);
            await store.ReloadAllPDBsFromSymbolServersAndSendSources(this, id, context, token);
            return true;
        }

        private async Task<bool> ApplyUpdates(MessageId id, JObject args, CancellationToken token)
        {
            var context = Contexts.GetCurrentContext(id);
            string moduleGUID = args["moduleGUID"]?.Value<string>();
            string dmeta = args["dmeta"]?.Value<string>();
            string dil = args["dil"]?.Value<string>();
            string dpdb = args["dpdb"]?.Value<string>();
            var moduleId = await context.SdbAgent.GetModuleId(moduleGUID, token);
            var applyUpdates =  await context.SdbAgent.ApplyUpdates(moduleId, dmeta, dil, dpdb, token);
            return applyUpdates;
        }

        private async Task SetJustMyCode(MessageId id, bool isEnabled, ExecutionContext context, CancellationToken token)
        {
            if (JustMyCode != isEnabled && isEnabled == false)
            {
                JustMyCode = isEnabled;
                if (await IsRuntimeAlreadyReadyAlready(id, token))
                    await ReloadSymbolsFromSymbolServer(id, context, token);
            }
            JustMyCode = isEnabled;
            SendResponse(id, Result.OkFromObject(new { justMyCodeEnabled = JustMyCode }), token);
        }
        internal async Task<Result> GetMethodLocation(MessageId id, JObject args, CancellationToken token)
        {
            DebugStore store = await RuntimeReady(id, token);
            string aname = args["assemblyName"]?.Value<string>();
            string typeName = args["typeName"]?.Value<string>();
            string methodName = args["methodName"]?.Value<string>();
            if (aname == null || typeName == null || methodName == null)
            {
                return Result.Err("Invalid protocol message '" + args + "'.");
            }

            // GetAssemblyByName seems to work on file names
            AssemblyInfo assembly = store.GetAssemblyByName(aname);
            assembly ??= store.GetAssemblyByName(aname + ".dll");
            if (assembly == null)
            {
                return Result.Err($"Assembly '{aname}' not found," +
                                    $"needed to get method location of '{typeName}:{methodName}'");
            }

            TypeInfo type = assembly.GetTypeByName(typeName);
            if (type == null)
            {
                return Result.Err($"Type '{typeName}' not found.");
            }

            MethodInfo methodInfo = type.Methods.FirstOrDefault(m => m.Name == methodName);
            if (methodInfo?.Source is null)
            {
                // Maybe this is an async method, in which case the debug info is attached
                // to the async method implementation, in class named:
                //      `{type_name}.<method_name>::MoveNext`
                methodInfo = assembly.TypesByName.Values.SingleOrDefault(t => t.FullName.StartsWith($"{typeName}.<{methodName}>"))?
                    .Methods.FirstOrDefault(mi => mi.Name == "MoveNext");
            }

            if (methodInfo == null)
            {
                return Result.Err($"Method '{typeName}:{methodName}' not found.");
            }

            string src_url = methodInfo.Assembly.Sources.Single(sf => sf.SourceId == methodInfo.SourceId).Url.ToString();

            return Result.OkFromObject(new
            {
                result = new { line = methodInfo.StartLocation.Line, column = methodInfo.StartLocation.Column, url = src_url }
            });
        }

        private async Task<bool> CallOnFunction(MessageId id, JObject args, CancellationToken token)
        {
            var context = Contexts.GetCurrentContext(id);
            if (!DotnetObjectId.TryParse(args["objectId"], out DotnetObjectId objectId)) {
                return false;
            }
            switch (objectId.Scheme)
            {
                case "method":
                    args["details"] = await context.SdbAgent.GetMethodProxy(objectId.ValueAsJson, token);
                    break;
                case "object":
                    args["details"] = await context.SdbAgent.GetObjectProxy(objectId.Value, token);
                    break;
                case "valuetype":
                    var valueType = context.SdbAgent.GetValueTypeClass(objectId.Value);
                    if (valueType == null)
                        throw new Exception($"Internal Error: No valuetype found for {objectId}.");
                    args["details"] = await valueType.GetProxy(context.SdbAgent, token);
                    break;
                case "pointer":
                    args["details"] = await context.SdbAgent.GetPointerContent(objectId.Value, token);
                    break;
                case "array":
                    args["details"] = await context.SdbAgent.GetArrayValuesProxy(objectId.Value, token);
                    break;
                case "cfo_res":
                    Result cfo_res = await SendMonoCommand(id, MonoCommands.CallFunctionOn(RuntimeId, args), token);
                    cfo_res = Result.OkFromObject(new { result = cfo_res.Value?["result"]?["value"]});
                    SendResponse(id, cfo_res, token);
                    return true;
                case "scope":
                {
                    SendResponse(id,
                    Result.Exception(new ArgumentException(
                        $"Runtime.callFunctionOn not supported with scope ({objectId}).")),
                        token);
                    return true;
                }
                default:
                    return false;
            }
            Result res = await SendMonoCommand(id, MonoCommands.CallFunctionOn(RuntimeId, args), token);
            if (!res.IsOk)
            {
                SendResponse(id, res, token);
                return true;
            }
            if (res.Value?["result"]?["value"]?["type"] == null) //it means that is not a buffer returned from the debugger-agent
            {
                byte[] newBytes = Convert.FromBase64String(res.Value?["result"]?["value"]?["value"]?.Value<string>());
                var retDebuggerCmdReader = new MonoBinaryReader(newBytes);
                retDebuggerCmdReader.ReadByte(); //number of objects returned.
                var obj = await context.SdbAgent.ValueCreator.ReadAsVariableValue(retDebuggerCmdReader, "ret", token);
                /*JTokenType? res_value_type = res.Value?["result"]?["value"]?.Type;*/
                res = Result.OkFromObject(new { result = obj["value"]});
                SendResponse(id, res, token);
                return true;
            }
            res = Result.OkFromObject(new { result = res.Value?["result"]?["value"]});
            SendResponse(id, res, token);
            return true;
        }

        private async Task<bool> OnSetVariableValue(MessageId id, int scopeId, string varName, JToken varValue, CancellationToken token)
        {
            ExecutionContext context = Contexts.GetCurrentContext(id);
            Frame scope = context.CallStack.FirstOrDefault(s => s.Id == scopeId);
            if (scope == null)
                return false;
            var varIds = scope.Method.Info.GetLiveVarsAt(scope.Location.IlLocation.Offset);
            if (varIds == null)
                return false;
            var varToSetValue = varIds.FirstOrDefault(v => v.Name == varName);
            if (varToSetValue == null)
                return false;
            var res = await context.SdbAgent.SetVariableValue(context.ThreadId, scopeId, varToSetValue.Index, varValue["value"].Value<string>(), token);
            if (res)
                SendResponse(id, Result.Ok(new JObject()), token);
            else
                SendResponse(id, Result.Err($"Unable to set '{varValue["value"].Value<string>()}' to variable '{varName}'"), token);
            return true;
        }

        internal async Task<ValueOrError<GetMembersResult>> RuntimeGetObjectMembers(SessionId id, DotnetObjectId objectId, JToken args, CancellationToken token, bool sortByAccessLevel = false)
        {
            var context = Contexts.GetCurrentContext(id);
            GetObjectCommandOptions getObjectOptions = GetObjectCommandOptions.WithProperties;
            if (args != null)
            {
                if (args["accessorPropertiesOnly"]?.Value<bool>() == true)
                    getObjectOptions |= GetObjectCommandOptions.AccessorPropertiesOnly;

                if (args["ownProperties"]?.Value<bool>() == true)
                    getObjectOptions |= GetObjectCommandOptions.OwnProperties;

                if (args["forDebuggerDisplayAttribute"]?.Value<bool>() == true)
                    getObjectOptions |= GetObjectCommandOptions.ForDebuggerDisplayAttribute;
            }
            if (context.AutoEvaluateProperties)
                getObjectOptions |= GetObjectCommandOptions.AutoExpandable;
            if (JustMyCode)
                getObjectOptions |= GetObjectCommandOptions.JustMyCode;
            try
            {
                switch (objectId.Scheme)
                {
                    // ToDo: fix Exception types here
                    case "scope":
                        GetMembersResult resScope = await GetScopeProperties(id, objectId.Value, token);
                        resScope.CleanUp();
                        return ValueOrError<GetMembersResult>.WithValue(resScope);
                    case "valuetype":
                        var resValue = await MemberObjectsExplorer.GetValueTypeMemberValues(
                            context.SdbAgent, objectId.Value, getObjectOptions, token, sortByAccessLevel, includeStatic: true);
                        resValue?.CleanUp();
                        return resValue switch
                        {
                            null => ValueOrError<GetMembersResult>.WithError($"Could not get properties for {objectId}"),
                            _ => ValueOrError<GetMembersResult>.WithValue(resValue)
                        };
                    case "array":
                        var resArr = await context.SdbAgent.GetArrayValues(objectId.Value, token);
                        return ValueOrError<GetMembersResult>.WithValue(GetMembersResult.FromValues(resArr));
                    case "method":
                        var resMethod = await context.SdbAgent.InvokeMethod(objectId, token);
                        return ValueOrError<GetMembersResult>.WithValue(GetMembersResult.FromValues(new JArray(resMethod)));
                    case "object":
                        var resObj = await MemberObjectsExplorer.GetObjectMemberValues(
                            context.SdbAgent, objectId.Value, getObjectOptions, token, sortByAccessLevel, includeStatic: true);
                        resObj.CleanUp();
                        return ValueOrError<GetMembersResult>.WithValue(resObj);
                    case "pointer":
                        var resPointer = new JArray { await context.SdbAgent.GetPointerContent(objectId.Value, token) };
                        return ValueOrError<GetMembersResult>.WithValue(GetMembersResult.FromValues(resPointer));
                    case "cfo_res":
                        Result res = await SendMonoCommand(id, MonoCommands.GetDetails(RuntimeId, objectId.Value, args), token);
                        string value_json_str = res.Value["result"]?["value"]?["__value_as_json_string__"]?.Value<string>();
                        if (res.IsOk && value_json_str == null)
                            return ValueOrError<GetMembersResult>.WithError(
                                $"Internal error: Could not find expected __value_as_json_string__ field in the result: {res}");

                        return value_json_str != null
                                    ? ValueOrError<GetMembersResult>.WithValue(GetMembersResult.FromValues(JArray.Parse(value_json_str)))
                                    : ValueOrError<GetMembersResult>.WithError(res);
                    case "evaluationResult":
                        JArray evaluationRes = (JArray)context.SdbAgent.GetEvaluationResultProperties(objectId.ToString());
                        return ValueOrError<GetMembersResult>.WithValue(GetMembersResult.FromValues(evaluationRes));
                    default:
                        return ValueOrError<GetMembersResult>.WithError($"RuntimeGetProperties: unknown object id scheme: {objectId.Scheme}");
                }
            }
            catch (Exception ex)
            {
                return ValueOrError<GetMembersResult>.WithError($"RuntimeGetProperties: Failed to get properties for {objectId}: {ex}");
            }
        }

        protected async Task<bool> EvaluateCondition(SessionId sessionId, ExecutionContext context, Frame mono_frame, Breakpoint bp, CancellationToken token)
        {
            if (string.IsNullOrEmpty(bp?.Condition) || mono_frame == null)
                return true;

            string condition = bp.Condition;

            if (bp.ConditionAlreadyEvaluatedWithError)
                return false;
            try {
                var resolver = new MemberReferenceResolver(this, context, sessionId, mono_frame.Id, logger);
                JObject retValue = await resolver.Resolve(condition, token);
                retValue ??= await ExpressionEvaluator.CompileAndRunTheExpression(condition, resolver, logger, token);
                if (retValue?["value"]?.Type == JTokenType.Boolean ||
                    retValue?["value"]?.Type == JTokenType.Integer ||
                    retValue?["value"]?.Type == JTokenType.Float) {
                    if (retValue?["value"]?.Value<bool>() == true)
                        return true;
                }
                else if (retValue?["value"] != null && // null object, missing value
                         retValue?["value"]?.Type != JTokenType.Null)
                {
                    return true;
                }
            }
            catch (ReturnAsErrorException raee)
            {
                logger.LogDebug($"Unable to evaluate breakpoint condition '{condition}': {raee}");
                SendLog(sessionId, $"Unable to evaluate breakpoint condition '{condition}': {raee.Message}", token, type: "error");
                bp.ConditionAlreadyEvaluatedWithError = true;
            }
            catch (Exception e)
            {
                Log("info", $"Unable to evaluate breakpoint condition '{condition}': {e}");
                bp.ConditionAlreadyEvaluatedWithError = true;
            }
            return false;
        }

        private async Task<bool> ProcessEnC(SessionId sessionId, ExecutionContext context, MonoBinaryReader retDebuggerCmdReader, CancellationToken token)
        {
            int moduleId = retDebuggerCmdReader.ReadInt32();
            int meta_size = retDebuggerCmdReader.ReadInt32();
            byte[] meta_buf = retDebuggerCmdReader.ReadBytes(meta_size);
            int pdb_size = retDebuggerCmdReader.ReadInt32();
            byte[] pdb_buf = retDebuggerCmdReader.ReadBytes(pdb_size);

            var assemblyName = await context.SdbAgent.GetAssemblyNameFromModule(moduleId, token);
            DebugStore store = await LoadStore(sessionId, true, token);
            AssemblyInfo asm = store.GetAssemblyByName(assemblyName);
            var methods = DebugStore.EnC(context.SdbAgent, asm, meta_buf, pdb_buf);
            foreach (var method in methods)
            {
                await ResetBreakpoint(sessionId, store, method, token);
            }
            var files = methods.Distinct(new MethodInfo.SourceComparer());
            foreach (var file in files)
            {
                JObject scriptSource = JObject.FromObject(file.Source.ToScriptSource(context.Id, context.AuxData));
                Log("debug", $"sending after update {file.Source.Url} {context.Id} {sessionId.sessionId}");
                await SendEvent(sessionId, "Debugger.scriptParsed", scriptSource, token);
            }
            return true;
        }

        private async Task<bool> SendBreakpointsOfMethodUpdated(SessionId sessionId, ExecutionContext context, MonoBinaryReader retDebuggerCmdReader, CancellationToken token)
        {
            var methodId = retDebuggerCmdReader.ReadInt32();
            var method = await context.SdbAgent.GetMethodInfo(methodId, token);
            if (method == null || method.Info.Source is null)
            {
                return true;
            }
            foreach (var req in context.BreakpointRequests.Values)
            {
                if (req.TryResolve(method.Info.Source))
                {
                    await SetBreakpoint(sessionId, context.store, req, true, true, token);
                }
            }
            return true;
        }

        protected virtual async Task<bool> ShouldSkipMethod(SessionId sessionId, ExecutionContext context, EventKind event_kind, int frameNumber, int totalFrames, MethodInfoWithDebugInformation method, CancellationToken token)
        {
            var shouldReturn = await SkipMethod(
                    isSkippable: context.IsSkippingHiddenMethod,
                    shouldBeSkipped: event_kind != EventKind.UserBreak,
                    StepKind.Over);
            context.IsSkippingHiddenMethod = false;
            if (shouldReturn)
                return true;

            shouldReturn = await SkipMethod(
                isSkippable: context.IsSteppingThroughMethod,
                shouldBeSkipped: event_kind != EventKind.UserBreak && event_kind != EventKind.Breakpoint,
                StepKind.Over);
            context.IsSteppingThroughMethod = false;
            if (shouldReturn)
                return true;

            if (frameNumber != 0)
                return false;

            if (method?.Info?.DebuggerAttrInfo?.DoAttributesAffectCallStack(JustMyCode) == true)
            {
                if (method.Info.DebuggerAttrInfo.ShouldStepOut(event_kind))
                {
                    if (event_kind == EventKind.Step)
                        context.IsSkippingHiddenMethod = true;
                    if (await SkipMethod(isSkippable: true, shouldBeSkipped: true, StepKind.Out))
                        return true;
                }
                if (!method.Info.DebuggerAttrInfo.HasStepperBoundary)
                {
                    if (event_kind == EventKind.Step ||
                    (JustMyCode && (event_kind == EventKind.Breakpoint || event_kind == EventKind.UserBreak)))
                    {
                        if (context.IsResumedAfterBp)
                            context.IsResumedAfterBp = false;
                        else if (event_kind != EventKind.UserBreak)
                            context.IsSteppingThroughMethod = true;
                        if (await SkipMethod(isSkippable: true, shouldBeSkipped: true, StepKind.Out))
                            return true;
                    }
                    if (event_kind == EventKind.Breakpoint)
                        context.IsResumedAfterBp = true;
                }
            }
            else
            {
                if (!JustMyCode && method?.Info?.DebuggerAttrInfo?.HasNonUserCode == true && !method.Info.hasDebugInformation)
                {
                    if (event_kind == EventKind.Step)
                        context.IsSkippingHiddenMethod = true;
                    if (await SkipMethod(isSkippable: true, shouldBeSkipped: true, StepKind.Out))
                        return true;
                }
            }
            return false;
            async Task<bool> SkipMethod(bool isSkippable, bool shouldBeSkipped, StepKind stepKind)
            {
                if (isSkippable && shouldBeSkipped)
                {
                    if (frameNumber + 1 == totalFrames && stepKind == StepKind.Out) //is the last managed frame
                        await SendCommand(sessionId, "Debugger.stepOut", new JObject(), token);
                    else
                        await TryStepOnManagedCodeAndStepOutIfNotPossible(sessionId, context, stepKind, token);
                    return true;
                }
                return false;
            }
        }


        protected virtual async Task<bool> SendCallStack(SessionId sessionId, ExecutionContext context, string reason, int thread_id, Breakpoint bp, JObject data, JObject args, EventKind event_kind, CancellationToken token)
        {
            var orig_callframes = args?["callFrames"]?.Values<JObject>();
            var callFrames = new List<object>();
            var frames = new List<Frame>();
            using var commandParamsWriter = new MonoBinaryWriter();
            commandParamsWriter.Write(thread_id);
            commandParamsWriter.Write(0);
            commandParamsWriter.Write(-1);
            using var retDebuggerCmdReader = await context.SdbAgent.SendDebuggerAgentCommand(CmdThread.GetFrameInfo, commandParamsWriter, token);
            var frame_count = retDebuggerCmdReader.ReadInt32();
            //Console.WriteLine("frame_count - " + frame_count);
            for (int j = 0; j < frame_count; j++) {
                var frame_id = retDebuggerCmdReader.ReadInt32();
                var methodId = retDebuggerCmdReader.ReadInt32();
                var il_pos = retDebuggerCmdReader.ReadInt32();
                var flags = retDebuggerCmdReader.ReadByte();
                DebugStore store = await LoadStore(sessionId, true, token);
                var method = await context.SdbAgent.GetMethodInfo(methodId, token);

                if (await ShouldSkipMethod(sessionId, context, event_kind, j, frame_count, method, token))
                    return true;

                SourceLocation location = method?.Info.GetLocationByIl(il_pos);

                // When hitting a breakpoint on the "IncrementCount" method in the standard
                // Blazor project template, one of the stack frames is inside mscorlib.dll
                // and we get location==null for it. It will trigger a NullReferenceException
                // if we don't skip over that stack frame.
                if (location == null)
                {
                    continue;
                }

                // logger.LogTrace($"frame il offset: {il_pos} method token: {method.Info.Token} assembly name: {method.Info.Assembly.Name}");
                // logger.LogTrace($"\tmethod {method.Name} location: {location}");
                frames.Add(new Frame(method, location, frame_id));

                callFrames.Add(new
                {
                    functionName = method.Name,
                    callFrameId = $"dotnet:scope:{frame_id}",
                    functionLocation = method.Info.StartLocation.AsLocation(),

                    location = location.AsLocation(),

                    url = store.ToUrl(location),

                    scopeChain = new[]
                        {
                            new
                            {
                                type = "local",
                                    @object = new
                                    {
                                        @type = "object",
                                            className = "Object",
                                            description = "Object",
                                            objectId = $"dotnet:scope:{frame_id}",
                                    },
                                    name = method.Name,
                                    startLocation = method.Info.StartLocation.AsLocation(),
                                    endLocation = method.Info.EndLocation.AsLocation(),
                            }
                        }
                });

                context.CallStack = frames;
            }
            string[] bp_list = new string[bp == null ? 0 : 1];
            if (bp != null)
                bp_list[0] = bp.StackId;

            foreach (JObject frame in orig_callframes)
            {
                string function_name = frame["functionName"]?.Value<string>();
                string url = frame["url"]?.Value<string>();
                var isWasmExpressionStack = frame["scopeChain"]?[0]?["type"]?.Value<string>()?.Equals("wasm-expression-stack") == true;
                if (!(function_name.StartsWith("wasm-function", StringComparison.Ordinal) ||
                        url.StartsWith("wasm://", StringComparison.Ordinal) ||
                        url.EndsWith(".wasm", StringComparison.Ordinal) ||
                        JustMyCode && isWasmExpressionStack ||
                        function_name.StartsWith("_mono_wasm_fire_debugger_agent_message", StringComparison.Ordinal) ||
                        function_name.StartsWith("mono_wasm_fire_debugger_agent_message", StringComparison.Ordinal)))
                {
                    await SymbolicateFunctionName(sessionId, context, frame, token);
                    callFrames.Add(frame);
                }
            }
            var o = JObject.FromObject(new
            {
                callFrames,
                reason,
                data,
                hitBreakpoints = bp_list,
            });
            if (args["asyncStackTraceId"] != null)
                o["asyncStackTraceId"] = args["asyncStackTraceId"];
            if (!await EvaluateCondition(sessionId, context, context.CallStack.First(), bp, token))
            {
                context.ClearState();
                await SendResume(sessionId, token);
                return true;
            }
            await SendEvent(sessionId, "Debugger.paused", o, token);

            return true;
        }

        private async Task SymbolicateFunctionName(SessionId sessionId, ExecutionContext context, JObject frame, CancellationToken token)
        {
            string funcPrefix = "$func";
            string functionName = frame["functionName"]?.Value<string>();
            if (!functionName.StartsWith(funcPrefix, StringComparison.Ordinal) || !int.TryParse(functionName[funcPrefix.Length..], out var funcId))
            {
                return;
            }

            if (context.WasmFunctionIds is null)
            {
                Result getIds = await SendMonoCommand(sessionId, MonoCommands.GetWasmFunctionIds(RuntimeId), token);
                if (getIds.IsOk)
                {
                    string[] symbols = getIds.Value?["result"]?["value"]?.ToObject<string[]>();
                    context.WasmFunctionIds = symbols;
                }
                else
                {
                    context.WasmFunctionIds = Array.Empty<string>();
                }
            }

            if (context.WasmFunctionIds.Length > funcId)
                frame["functionName"] = context.WasmFunctionIds[funcId];
        }

        internal virtual void SaveLastDebuggerAgentBufferReceivedToContext(SessionId sessionId, Task<Result> debuggerAgentBufferTask)
        {
        }

        internal async Task<Result> GetLastDebuggerAgentBuffer(SessionId sessionId, JObject args, CancellationToken token)
        {
            if (args?["callFrames"].Value<JArray>().Count == 0 || args["callFrames"][0]["scopeChain"].Value<JArray>().Count == 0)
                return Result.Err($"Unexpected callFrames {args}");
            var argsNew = JObject.FromObject(new
            {
                objectId = args["callFrames"][0]["scopeChain"][0]["object"]["objectId"].Value<string>(),
            });
            Result res = await SendCommand(sessionId, "Runtime.getProperties", argsNew, token);
            return res;
        }

        internal async Task<bool> OnReceiveDebuggerAgentEvent(SessionId sessionId, JObject args, Result debuggerAgentBuffer, CancellationToken token)
        {
            var debuggerAgentBufferTask = SendMonoCommand(sessionId, MonoCommands.GetDebuggerAgentBufferReceived(RuntimeId), token);
            SaveLastDebuggerAgentBufferReceivedToContext(sessionId, debuggerAgentBufferTask);
            if (!debuggerAgentBuffer.IsOk || debuggerAgentBuffer.Value?["result"].Value<JArray>().Count == 0)
            {
                logger.LogTrace($"Unexpected DebuggerAgentBufferReceived {debuggerAgentBuffer}");
                return false;
            }
            ExecutionContext context = Contexts.GetCurrentContext(sessionId);
            byte[] newBytes = Convert.FromBase64String(debuggerAgentBuffer.Value?["result"]?[0]?["value"]?["value"]?.Value<string>());
            using var retDebuggerCmdReader = new MonoBinaryReader(newBytes);
            retDebuggerCmdReader.ReadBytes(11); //skip HEADER_LEN
            retDebuggerCmdReader.ReadByte(); //suspend_policy
            var number_of_events = retDebuggerCmdReader.ReadInt32(); //number of events -> should be always one
            for (int i = 0 ; i < number_of_events; i++) {
                var event_kind = (EventKind)retDebuggerCmdReader.ReadByte(); //event kind
                var request_id = retDebuggerCmdReader.ReadInt32(); //request id
                if (event_kind == EventKind.Step)
                    await context.SdbAgent.ClearSingleStep(request_id, token);
                int thread_id = retDebuggerCmdReader.ReadInt32();
                context.ThreadId = thread_id;
                switch (event_kind)
                {
                    case EventKind.MethodUpdate:
                    {
                        var ret = await SendBreakpointsOfMethodUpdated(sessionId, context, retDebuggerCmdReader, token);
                        await SendResume(sessionId, token);
                        return ret;
                    }
                    case EventKind.EnC:
                    {
                        var ret = await ProcessEnC(sessionId, context, retDebuggerCmdReader, token);
                        await SendResume(sessionId, token);
                        return ret;
                    }
                    case EventKind.Exception:
                    {
                        string reason = "exception";
                        int object_id = retDebuggerCmdReader.ReadInt32();
                        var caught = retDebuggerCmdReader.ReadByte();
                        var exceptionObject = await MemberObjectsExplorer.GetObjectMemberValues(
                            context.SdbAgent, object_id, GetObjectCommandOptions.WithProperties | GetObjectCommandOptions.OwnProperties, token);
                        var exceptionObjectMessage = exceptionObject.FirstOrDefault(attr => attr["name"].Value<string>().Equals("_message"));
                        var data = JObject.FromObject(new
                        {
                            type = "object",
                            subtype = "error",
                            className = await context.SdbAgent.GetClassNameFromObject(object_id, token),
                            uncaught = caught == 0,
                            description = exceptionObjectMessage["value"]["value"].Value<string>(),
                            objectId = $"dotnet:object:{object_id}"
                        });

                        var ret = await SendCallStack(sessionId, context, reason, thread_id, null, data, args, event_kind, token);
                        return ret;
                    }
                    case EventKind.UserBreak:
                    case EventKind.Step:
                    case EventKind.Breakpoint:
                    {
                        if (event_kind == EventKind.Step)
                            context.PauseKind = "resumeLimit";
                        else if (event_kind == EventKind.Breakpoint)
                            context.PauseKind = "breakpoint";
                        Breakpoint bp = context.BreakpointRequests.Values.SelectMany(v => v.Locations).FirstOrDefault(b => b.RemoteId == request_id);
                        if (bp == null && context.ParentContext != null)
                        {
                            bp = context.ParentContext.BreakpointRequests.Values.SelectMany(v => v.Locations).FirstOrDefault(b => b.RemoteId == request_id);
                        }
                        if (request_id == context.TempBreakpointForSetNextIP)
                        {
                            context.TempBreakpointForSetNextIP = -1;
                            await context.SdbAgent.RemoveBreakpoint(request_id, token);
                        }
                        string reason = "other";//other means breakpoint
                        int methodId = 0;
                        if (event_kind != EventKind.UserBreak)
                            methodId = retDebuggerCmdReader.ReadInt32();
                        var ret = await SendCallStack(sessionId, context, reason, thread_id, bp, null, args, event_kind, token);
                        return ret;
                    }
                }
            }
            return false;
        }

        protected async Task OnDefaultContext(SessionId sessionId, ExecutionContext context, CancellationToken token)
        {
            Log("verbose", "Default context created, clearing state and sending events");
            Contexts.OnDefaultContextUpdate(sessionId, context);
            if (await IsRuntimeAlreadyReadyAlready(sessionId, token))
                await RuntimeReady(sessionId, token);
        }

        protected async Task OnResume(MessageId msg_id, CancellationToken token)
        {
            ExecutionContext context = Contexts.GetCurrentContext(msg_id);
            if (context.CallStack != null)
            {
                // Stopped on managed code
                await SendMonoCommand(msg_id, MonoCommands.Resume(RuntimeId), token);
            }

            //discard managed frames
            Contexts.GetCurrentContext(msg_id).ClearState();
        }
        protected async Task<bool> TryStepOnManagedCodeAndStepOutIfNotPossible(SessionId sessionId, ExecutionContext context, StepKind kind, CancellationToken token)
        {
            var step = await context.SdbAgent.Step(context.ThreadId, kind, token);
            if (step == false) //it will return false if it's the last managed frame and the runtime added the single step breakpoint in a MONO_WRAPPER_RUNTIME_INVOKE
            {
                context.ClearState();
                await SendCommand(sessionId, "Debugger.stepOut", new JObject(), token);
                return false;
            }

            context.ClearState();

            await SendResume(sessionId, token);
            return true;
        }

        protected async Task<bool> Step(MessageId msgId, StepKind kind, CancellationToken token)
        {
            ExecutionContext context = Contexts.GetCurrentContext(msgId);
            if (context.CallStack == null)
                return false;

            if (context.CallStack.Count <= 1 && kind == StepKind.Out)
            {
                Frame scope = context.CallStack.FirstOrDefault<Frame>();
                if (scope is null || !(await context.SdbAgent.IsAsyncMethod(scope.Method.DebugId, token)))
                    return false;
            }
            var ret = await TryStepOnManagedCodeAndStepOutIfNotPossible(msgId, context, kind, token);
            if (ret)
                SendResponse(msgId, Result.Ok(new JObject()), token);
            return ret;
        }

        private async Task<bool> OnJSEventRaised(SessionId sessionId, JObject eventArgs, CancellationToken token)
        {
            string eventName = eventArgs?["eventName"]?.Value<string>();
            if (string.IsNullOrEmpty(eventName))
            {
                logger.LogDebug($"Missing name for raised js event: {eventArgs}");
                return false;
            }

            logger.LogDebug($"OnJsEventRaised: args: {eventArgs.ToString().TruncateLogMessage()}");

            switch (eventName)
            {
                case "AssemblyLoaded":
                    return await OnAssemblyLoadedJSEvent(sessionId, eventArgs, token);
                default:
                {
                    logger.LogDebug($"Unknown js event name: {eventName} with args {eventArgs}");
                    return await Task.FromResult(false);
                }
            }
        }

        private async Task<bool> OnAssemblyLoadedJSEvent(SessionId sessionId, JObject eventArgs, CancellationToken token)
        {
            try
            {
                var store = await LoadStore(sessionId, true, token);
                var assembly_name = eventArgs?["assembly_name"]?.Value<string>();

                if (store.GetAssemblyByName(assembly_name) != null)
                {
                    Log("debug", $"Got AssemblyLoaded event for {assembly_name}, but skipping it as it has already been loaded.");
                    return true;
                }

                var assembly_b64 = eventArgs?["assembly_b64"]?.ToObject<string>();
                var pdb_b64 = eventArgs?["pdb_b64"]?.ToObject<string>();

                if (string.IsNullOrEmpty(assembly_b64))
                {
                    logger.LogDebug("No assembly data provided to load.");
                    return false;
                }

                var assembly_data = Convert.FromBase64String(assembly_b64);
                var pdb_data = string.IsNullOrEmpty(pdb_b64) ? null : Convert.FromBase64String(pdb_b64);

                var context = Contexts.GetCurrentContext(sessionId);
                foreach (var source in store.Add(sessionId, new AssemblyAndPdbData(assembly_data, pdb_data), token))
                {
                    await OnSourceFileAdded(sessionId, source, context, token);
                }

                return true;
            }
            catch (Exception e)
            {
                logger.LogDebug($"Failed to load assemblies and PDBs: {e}");
                return false;
            }
        }

        private async Task OnSetEntrypointBreakpoint(SessionId sessionId, JObject args, CancellationToken token)
        {
            try
            {
                ExecutionContext context = Contexts.GetCurrentContext(sessionId);

                var argsNew = JObject.FromObject(new
                {
                    callFrameId = args?["callFrames"]?[0]?["callFrameId"]?.Value<string>(),
                    expression = "_assembly_name_str + '|' + _entrypoint_method_token",
                });
                Result assemblyAndMethodToken = await SendCommand(sessionId, "Debugger.evaluateOnCallFrame", argsNew, token);
                if (!assemblyAndMethodToken.IsOk)
                {
                    logger.LogDebug("Failure evaluating _assembly_name_str + '|' + _entrypoint_method_token");
                    return;
                }
                logger.LogDebug($"Entrypoint assembly and method token {assemblyAndMethodToken.Value["result"]["value"].Value<string>()}");

                var assemblyAndMethodTokenArr = assemblyAndMethodToken.Value["result"]["value"].Value<string>().Split('|', StringSplitOptions.TrimEntries);
                var assemblyName = assemblyAndMethodTokenArr[0];
                var methodToken = Convert.ToInt32(assemblyAndMethodTokenArr[1]) & 0xffffff; //token

                var store = await LoadStore(sessionId, true, token);
                AssemblyInfo assembly = store.GetAssemblyByName(assemblyName);
                if (assembly == null)
                {
                    logger.LogDebug($"Could not find entrypoint assembly {assemblyName} in the store");
                    return;
                }
                var method = assembly.GetMethodByToken(methodToken);
                if (method.StartLocation == null) //It's an async method and we need to get the MoveNext method to add the breakpoint
                    method = assembly.Methods.FirstOrDefault(m => m.Value.KickOffMethod == methodToken).Value;
                if (method == null)
                {
                    logger.LogDebug($"Could not find entrypoint method {methodToken} in assembly {assemblyName}");
                    return;
                }
                var sourceFile = assembly.Sources.FirstOrDefault(sf => sf.SourceId == method.SourceId);
                if (sourceFile == null)
                {
                    logger.LogDebug($"Could not source file {method.SourceName} for method {method.Name} in assembly {assemblyName}");
                    return;
                }
                string bpId = $"auto:{method.StartLocation.Line}:{method.StartLocation.Column}:{sourceFile.DotNetUrlEscaped}";
                BreakpointRequest request = new(bpId, JObject.FromObject(new
                {
                    lineNumber = method.StartLocation.Line,
                    columnNumber = method.StartLocation.Column,
                    url = sourceFile.Url
                }));
                context.BreakpointRequests[bpId] = request;
                if (request.TryResolve(sourceFile))
                    await SetBreakpoint(sessionId, context.store, request, sendResolvedEvent: false, fromEnC: false, token);
                logger.LogInformation($"Adding bp req {request}");
            }
            catch (Exception e)
            {
                logger.LogDebug($"Unable to set entrypoint breakpoint. {e}");
            }
            finally
            {
                await SendResume(sessionId, token);
            }
        }
        private Result AddCallStackInfoToException(Result _error, ExecutionContext context, int scopeId)
        {
            try {
                var retStackTrace = new JArray();
                foreach(var call in context.CallStack)
                {
                    if (call.Id < scopeId)
                        continue;
                    retStackTrace.Add(JObject.FromObject(new
                    {
                        functionName = call.Method.Name,
                        scriptId = call.Location.Id.ToString(),
                        url = context.Store.ToUrl(call.Location),
                        lineNumber = call.Location.Line,
                        columnNumber = call.Location.Column
                    }));
                }
                if (!_error.Value.ContainsKey("exceptionDetails"))
                    _error.Value["exceptionDetails"] = new JObject();
                _error.Value["exceptionDetails"]["stackTrace"] = JObject.FromObject(new {callFrames = retStackTrace});
                return _error;
            }
            catch (Exception e)
            {
                logger.LogDebug($"Unable to add stackTrace information to exception. {e}");
            }
            return _error;
        }

        private async Task<bool> OnEvaluateOnCallFrame(MessageId msg_id, int scopeId, string expression, CancellationToken token)
        {
            ExecutionContext context = Contexts.GetCurrentContext(msg_id);
            try
            {
                if (context.CallStack == null)
                    return false;

                var resolver = new MemberReferenceResolver(this, context, msg_id, scopeId, logger);

                JObject retValue = await resolver.Resolve(expression, token);
                retValue ??= await ExpressionEvaluator.CompileAndRunTheExpression(expression, resolver, logger, token);

                if (retValue != null)
                {
                    SendResponse(msg_id, Result.OkFromObject(new
                    {
                        result = retValue
                    }), token);
                }
                else
                {
                    SendResponse(msg_id, AddCallStackInfoToException(Result.Err($"Unable to evaluate '{expression}'"), context, scopeId), token);
                }
            }
            catch (ReturnAsErrorException ree)
            {
                SendResponse(msg_id, AddCallStackInfoToException(ree.Error, context, scopeId), token);
            }
            catch (Exception e)
            {
                logger.LogDebug($"Error in EvaluateOnCallFrame for expression '{expression}' with '{e}.");
                var exc = new ReturnAsErrorException(e.Message, e.GetType().Name);
                SendResponse(msg_id, AddCallStackInfoToException(exc.Error, context, scopeId), token);
            }

            return true;
        }

        internal async Task<GetMembersResult> GetScopeProperties(SessionId msg_id, int scopeId, CancellationToken token)
        {
            try
            {
                ExecutionContext context = Contexts.GetCurrentContext(msg_id);
                Frame scope = context.CallStack.FirstOrDefault(s => s.Id == scopeId);
                if (scope == null)
                    throw new Exception($"Could not find scope with id #{scopeId}");

                VarInfo[] varIds = scope.Method.Info.GetLiveVarsAt(scope.Location.IlLocation.Offset);

                var values = await context.SdbAgent.StackFrameGetValues(scope.Method, context.ThreadId, scopeId, varIds, scope.Location.IlLocation.Offset, token);
                if (values != null)
                {
                    if (values == null || values.Count == 0)
                        return new GetMembersResult();

                    PerScopeCache frameCache = context.GetCacheForScope(scopeId);
                    foreach (JObject value in values)
                    {
                        frameCache.Locals[value["name"]?.Value<string>()] = value;
                    }
                    return GetMembersResult.FromValues(values);
                }
                return new GetMembersResult();
            }
            catch (Exception exception)
            {
                throw new Exception($"Error resolving scope properties {exception.Message}");
            }
        }

        private async Task<Breakpoint> SetMonoBreakpoint(SessionId sessionId, string reqId, SourceLocation location, string condition, CancellationToken token)
        {
            var context = Contexts.GetCurrentContext(sessionId);
            var bp = new Breakpoint(reqId, location, condition, BreakpointState.Pending);
            string asm_name = bp.Location.IlLocation.Method.Assembly.Name;
            int method_token = bp.Location.IlLocation.Method.Token;
            int il_offset = bp.Location.IlLocation.Offset;

            var assembly_id = await context.SdbAgent.GetAssemblyId(asm_name, token);
            var methodId = await context.SdbAgent.GetMethodIdByToken(assembly_id, method_token, token);
            //the breakpoint can be invalid because a race condition between the changes already applied on runtime and not applied yet on debugger side
            var breakpoint_id = await context.SdbAgent.SetBreakpointNoThrow(methodId, il_offset, token);

            if (breakpoint_id > 0)
            {
                bp.RemoteId = breakpoint_id;
                bp.State = BreakpointState.Active;
                //Log ("verbose", $"BP local id {bp.LocalId} enabled with remote id {bp.RemoteId}");
            }
            return bp;
        }

        internal virtual async Task OnSourceFileAdded(SessionId sessionId, SourceFile source, ExecutionContext context, CancellationToken token, bool resolveBreakpoints = true)
        {
            JObject scriptSource = JObject.FromObject(source.ToScriptSource(context.Id, context.AuxData));
            // Log("debug", $"sending {source.Url} {context.Id} {sessionId.sessionId}");
            await SendEvent(sessionId, "Debugger.scriptParsed", scriptSource, token);
            if (!resolveBreakpoints)
                return;
            foreach (var req in context.BreakpointRequests.Values)
            {
                if (req.TryResolve(source))
                {
                    try
                    {
                        await SetBreakpoint(sessionId, context.store, req, true, false, token);
                    }
                    catch (DebuggerAgentException e)
                    {
                        //it's not a wasm page then the command throws an error
                        if (!e.Message.Contains("getDotnetRuntime is not defined"))
                            logger.LogDebug($"Unexpected error on RuntimeReady {e}");
                        return;
                    }
                }
            }
        }

        internal virtual async Task<DebugStore> LoadStore(SessionId sessionId, bool tryUseDebuggerProtocol, CancellationToken token)
        {
            ExecutionContext context = Contexts.GetCurrentContext(sessionId);

            if (Interlocked.CompareExchange(ref context.store, new DebugStore(this, logger), null) != null)
                return await context.Source.Task;

            try
            {
                string[] loaded_files = await GetLoadedFiles(sessionId, context, token);
                if (loaded_files == null)
                {
                    SendLog(sessionId, $"Failed to get the list of loaded files. Managed code debugging won't work due to this.", token);
                }
                else
                {
                    var useDebuggerProtocol = false;
                    if (tryUseDebuggerProtocol)
                    {
                        (int MajorVersion, int MinorVersion) = await context.SdbAgent.GetVMVersion(token);
                        if (MajorVersion == 2 && MinorVersion >= 61)
                            useDebuggerProtocol = true;
                    }

                    await foreach (SourceFile source in context.store.Load(sessionId, loaded_files, context, useDebuggerProtocol, token))
                    {
                        await OnSourceFileAdded(sessionId, source, context, token);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError($"failed: {e}");
                context.Source.SetException(e);
            }

            if (!context.Source.Task.IsCompleted)
                context.Source.SetResult(context.store);
            return context.store;
            async Task<string[]> GetLoadedFiles(SessionId sessionId, ExecutionContext context, CancellationToken token)
            {
                if (context.LoadedFiles != null)
                    return context.LoadedFiles;

                Result loaded = await SendMonoCommand(sessionId, MonoCommands.GetLoadedFiles(RuntimeId), token);
                if (!loaded.IsOk)
                {
                    SendLog(sessionId, $"Error on mono_wasm_get_loaded_files {loaded}", token);
                    return null;
                }

                string[] files = loaded.Value?["result"]?["value"]?.ToObject<string[]>();
                if (files == null)
                    SendLog(sessionId, $"Error extracting the list of loaded_files from the result of mono_wasm_get_loaded_files: {loaded}", token);

                return files;
            }
        }

        protected async Task<DebugStore> RuntimeReady(SessionId sessionId, CancellationToken token)
        {
            try
            {
                ExecutionContext context = Contexts.GetCurrentContext(sessionId);
                if (Interlocked.CompareExchange(ref context.ready, new TaskCompletionSource<DebugStore>(), null) != null)
                    return await context.ready.Task;
                await context.SdbAgent.SendDebuggerAgentCommand(CmdEventRequest.ClearAllBreakpoints, null, token);

                if (context.PauseOnExceptions != PauseOnExceptionsKind.None && context.PauseOnExceptions != PauseOnExceptionsKind.Unset)
                    await context.SdbAgent.EnableExceptions(context.PauseOnExceptions, token);

                await context.SdbAgent.SetProtocolVersion(token);
                await context.SdbAgent.EnableReceiveRequests(EventKind.UserBreak, token);
                await context.SdbAgent.EnableReceiveRequests(EventKind.EnC, token);
                await context.SdbAgent.EnableReceiveRequests(EventKind.MethodUpdate, token);

                DebugStore store = await LoadStore(sessionId, true, token);
                context.ready.SetResult(store);
                await SendEvent(sessionId, "Mono.runtimeReady", new JObject(), token);
                await SendMonoCommand(sessionId, MonoCommands.SetDebuggerAttached(RuntimeId), token);
                context.SdbAgent.ResetStore(store);
                return store;
            }
            catch (DebuggerAgentException e)
            {
                //it's not a wasm page then the command throws an error
                if (!e.Message.Contains("getDotnetRuntime is not defined"))
                    logger.LogDebug($"Unexpected error on RuntimeReady {e}");
                return null;
            }
            catch (Exception e)
            {
                logger.LogDebug($"Unexpected error on RuntimeReady {e}");
                return null;
            }
        }

        private static IEnumerable<IGrouping<SourceId, SourceLocation>> GetBPReqLocations(DebugStore store, BreakpointRequest req, bool ifNoneFoundThenFindNext = false)
        {
            var comparer = new SourceLocation.LocationComparer();
            // if column is specified the frontend wants the exact matches
            // and will clear the bp if it isn't close enough
            var bpLocations = store.FindBreakpointLocations(req, ifNoneFoundThenFindNext);
            IEnumerable<IGrouping<SourceId, SourceLocation>> locations = bpLocations.Distinct(comparer)
                .OrderBy(l => l.Column)
                .GroupBy(l => l.Id);
            if (ifNoneFoundThenFindNext && !locations.Any())
            {
                locations = bpLocations.GroupBy(l => l.Id);
            }
            return locations;
        }

        private async Task ResetBreakpoint(SessionId msg_id, DebugStore store, MethodInfo method, CancellationToken token)
        {
            ExecutionContext context = Contexts.GetCurrentContext(msg_id);
            foreach (var req in context.BreakpointRequests.Values)
            {
                if (req.Method != null)
                {
                    if (req.Method.Assembly.Id == method.Assembly.Id && req.Method.Token == method.Token) {
                        var locations = GetBPReqLocations(store, req);
                        foreach (IGrouping<SourceId, SourceLocation> sourceId in locations)
                        {
                            SourceLocation loc = sourceId.First();
                            if (req.Locations.Any(b => b.Location.IlLocation.Offset != loc.IlLocation.Offset))
                            {
                                await RemoveBreakpoint(msg_id, JObject.FromObject(new {breakpointId = req.Id}), true, token);
                                break;
                            }
                        }
                    }
                }
            }
        }

        protected async Task RemoveBreakpoint(SessionId msg_id, JObject args, bool isEnCReset, CancellationToken token)
        {
            string bpid = args?["breakpointId"]?.Value<string>();

            ExecutionContext context = Contexts.GetCurrentContext(msg_id);
            if (!context.BreakpointRequests.TryGetValue(bpid, out BreakpointRequest breakpointRequest))
                return;

            foreach (Breakpoint bp in breakpointRequest.Locations)
            {
                var breakpoint_removed = await context.SdbAgent.RemoveBreakpoint(bp.RemoteId, token);
                if (breakpoint_removed)
                {
                    bp.RemoteId = -1;
                    if (isEnCReset)
                        bp.State = BreakpointState.Pending;
                    else
                        bp.State = BreakpointState.Disabled;
                }
            }
            if (!isEnCReset)
                context.BreakpointRequests.Remove(bpid);
        }

        protected async Task SetBreakpoint(SessionId sessionId, DebugStore store, BreakpointRequest req, bool sendResolvedEvent, bool fromEnC, CancellationToken token)
        {
            ExecutionContext context = Contexts.GetCurrentContext(sessionId);
            if ((!fromEnC && req.Locations.Count != 0) || (fromEnC && req.Locations.Any(bp => bp.State == BreakpointState.Active)))
            {
                if (!fromEnC)
                    Log("debug", $"locations already loaded for {req.Id}");
                return;
            }

            var locations = GetBPReqLocations(store, req, true);

            logger.LogDebug("BP request for '{Req}' runtime ready {Context.RuntimeReady}", req, context.IsRuntimeReady);

            var breakpoints = new List<Breakpoint>();
            foreach (IGrouping<SourceId, SourceLocation> sourceId in locations)
            {
                SourceLocation loc = sourceId.First();
                req.Method = loc.IlLocation.Method;
                if (req.Method.DebuggerAttrInfo.HasDebuggerHidden)
                    continue;
                Breakpoint bp = await SetMonoBreakpoint(sessionId, req.Id, loc, req.Condition, token);

                // If we didn't successfully enable the breakpoint
                // don't add it to the list of locations for this id
                if (bp.State != BreakpointState.Active)
                    continue;

                breakpoints.Add(bp);

                var resolvedLocation = new
                {
                    breakpointId = req.Id,
                    location = loc.AsLocation()
                };

                if (sendResolvedEvent)
                    await SendEvent(sessionId, "Debugger.breakpointResolved", JObject.FromObject(resolvedLocation), token);
            }

            req.Locations.AddRange(breakpoints);
            return;
        }

        private async Task<bool> GetPossibleBreakpoints(MessageId msg, SourceLocation start, SourceLocation end, CancellationToken token)
        {
            List<SourceLocation> bps = (await RuntimeReady(msg, token)).FindPossibleBreakpoints(start, end);

            if (bps == null)
                return false;

            var response = new { locations = bps.Select(b => b.AsLocation()) };

            SendResponse(msg, Result.OkFromObject(response), token);
            return true;
        }

        private void OnCompileDotnetScript(MessageId msg_id, CancellationToken token)
        {
            SendResponse(msg_id, Result.OkFromObject(new { }), token);
        }

        private static bool IsNestedMethod(DebugStore store, Frame scope, SourceLocation foundLocation, SourceLocation targetLocation)
        {
            if (foundLocation.Line != targetLocation.Line || foundLocation.Column != targetLocation.Column)
            {
                SourceFile doc = store.GetFileById(scope.Method.Info.SourceId);
                foreach (var method in doc.Methods)
                {
                    if (method.Token == scope.Method.Info.Token)
                        continue;
                    if (method.IsLexicallyContainedInMethod(scope.Method.Info))
                        continue;
                    SourceLocation newFoundLocation = DebugStore.FindBreakpointLocations(targetLocation, targetLocation, scope.Method.Info)
                                                .FirstOrDefault();
                    if (!(newFoundLocation is null))
                        return true;
                }
            }
            return false;
        }

        private async Task<bool> OnSetNextIP(MessageId sessionId, SourceLocation targetLocation, CancellationToken token)
        {
            DebugStore store = await RuntimeReady(sessionId, token);
            ExecutionContext context = Contexts.GetCurrentContext(sessionId);
            Frame scope = context.CallStack.First<Frame>();

            SourceLocation foundLocation = DebugStore.FindBreakpointLocations(targetLocation, targetLocation, scope.Method.Info)
                                                    .FirstOrDefault();

            if (foundLocation is null)
                 return false;

            //search if it's a nested method and it's return false because we cannot move to another method
            if (IsNestedMethod(store, scope, foundLocation, targetLocation))
                return false;

            var ilOffset = foundLocation.IlLocation;
            var ret = await context.SdbAgent.SetNextIP(scope.Method, context.ThreadId, ilOffset, token);

            if (!ret)
                return false;

            var breakpointId = await context.SdbAgent.SetBreakpointNoThrow(scope.Method.DebugId, ilOffset.Offset, token);
            if (breakpointId == -1)
                return false;

            context.TempBreakpointForSetNextIP = breakpointId;
            await SendResume(sessionId, token);
            return true;
        }

        internal virtual async Task<bool> OnGetScriptSource(MessageId msg_id, string script_id, CancellationToken token)
        {
            if (!SourceId.TryParse(script_id, out SourceId id))
                return false;

            SourceFile src_file = (await LoadStore(msg_id, true, token)).GetFileById(id);

            try
            {
                string source = $"// Unable to find document {src_file.FileUriEscaped}";

                using (Stream data = await src_file.GetSourceAsync(checkHash: false, token: token))
                {
                    if (data is MemoryStream && data.Length == 0)
                        return false;

                    using (var reader = new StreamReader(data))
                        source = await reader.ReadToEndAsync(token);
                }
                SendResponse(msg_id, Result.OkFromObject(new { scriptSource = source }), token);
            }
            catch (Exception e)
            {
                var o = new
                {
                    scriptSource = $"// Unable to read document ({e.Message})\n" +
                    $"Local path: {src_file?.FileUriEscaped}\n" +
                    $"SourceLink path: {src_file?.SourceLinkUri}\n"
                };

                SendResponse(msg_id, Result.OkFromObject(o), token);
            }
            return true;
        }

        private async Task AttachToTarget(SessionId sessionId, CancellationToken token)
        {
            // see https://github.com/mono/mono/issues/19549 for background
            if (sessions.Add(sessionId))
            {
                await SendMonoCommand(sessionId, new MonoCommands("globalThis.dotnetDebugger = true"), token);
                Result res = await SendCommand(sessionId,
                    "Page.addScriptToEvaluateOnNewDocument",
                    JObject.FromObject(new { source = $"globalThis.dotnetDebugger = true; delete navigator.constructor.prototype.webdriver;" }),
                    token);

                if (sessionId != SessionId.Null && !res.IsOk)
                    sessions.Remove(sessionId);
            }
        }

        private bool JObjectTryParse(string str, out JObject obj, bool log_exception = true)
        {
            obj = null;
            if (string.IsNullOrEmpty(str))
                return false;

            try
            {
                obj = JObject.Parse(str);
                return true;
            }
            catch (JsonReaderException jre)
            {
                if (log_exception)
                    logger.LogDebug($"Could not parse {str}. Failed with {jre}");
                return false;
            }
        }
    }
}
