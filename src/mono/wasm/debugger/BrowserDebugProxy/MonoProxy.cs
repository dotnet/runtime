// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class MonoProxy : DevToolsProxy
    {
        private IList<string> urlSymbolServerList;
        private static HttpClient client = new HttpClient();
        private HashSet<SessionId> sessions = new HashSet<SessionId>();
        private Dictionary<SessionId, ExecutionContext> contexts = new Dictionary<SessionId, ExecutionContext>();
        private const string sPauseOnUncaught = "pause_on_uncaught";
        private const string sPauseOnCaught = "pause_on_caught";
        // index of the runtime in a same JS page/process
        public int RuntimeId { get; private init; }
        public bool JustMyCode { get; private set; }

        public MonoProxy(ILoggerFactory loggerFactory, IList<string> urlSymbolServerList, int runtimeId = 0, string loggerId = "") : base(loggerFactory, loggerId)
        {
            this.urlSymbolServerList = urlSymbolServerList ?? new List<string>();
            RuntimeId = runtimeId;
        }

        internal ExecutionContext GetContext(SessionId sessionId)
        {
            if (contexts.TryGetValue(sessionId, out ExecutionContext context))
                return context;

            throw new ArgumentException($"Invalid Session: \"{sessionId}\"", nameof(sessionId));
        }

        private bool UpdateContext(SessionId sessionId, ExecutionContext executionContext, out ExecutionContext previousExecutionContext)
        {
            bool previous = contexts.TryGetValue(sessionId, out previousExecutionContext);
            contexts[sessionId] = executionContext;
            return previous;
        }

        internal Task<Result> SendMonoCommand(SessionId id, MonoCommands cmd, CancellationToken token) => SendCommand(id, "Runtime.evaluate", JObject.FromObject(cmd), token);

        internal void SendLog(SessionId sessionId, string message, CancellationToken token, string type = "warning")
        {
            if (!contexts.TryGetValue(sessionId, out ExecutionContext context))
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

        protected override async Task<bool> AcceptEvent(SessionId sessionId, string method, JObject args, CancellationToken token)
        {
            switch (method)
            {
                case "Runtime.consoleAPICalled":
                    {
                        string type = args["type"]?.ToString();
                        if (type == "debug")
                        {
                            JToken a = args["args"];
                            if (a is null)
                                break;

                            int aCount = a.Count();
                            if (aCount >= 2 &&
                                a[0]?["value"]?.ToString() == MonoConstants.RUNTIME_IS_READY &&
                                a[1]?["value"]?.ToString() == "fe00e07a-5519-4dfe-b35a-f867dbaf2e28")
                            {
                                if (aCount > 2)
                                {
                                    try
                                    {
                                        // The optional 3rd argument is the stringified assembly
                                        // list so that we don't have to make more round trips
                                        ExecutionContext context = GetContext(sessionId);
                                        string loaded = a[2]?["value"]?.ToString();
                                        if (loaded != null)
                                            context.LoadedFiles = JToken.Parse(loaded).ToObject<string[]>();
                                    }
                                    catch (InvalidCastException ice)
                                    {
                                        Log("verbose", ice.ToString());
                                    }
                                }
                                await RuntimeReady(sessionId, token);
                            }
                            else if (aCount > 1 && a[0]?["value"]?.ToString() == MonoConstants.EVENT_RAISED)
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
                                await OnDefaultContext(sessionId, new ExecutionContext(new MonoSDBHelper (this, logger, sessionId), id, aux_data), token);
                            }
                        }
                        return true;
                    }

                case "Runtime.exceptionThrown":
                    {
                        // Don't process events from sessions we aren't tracking
                        if (!contexts.TryGetValue(sessionId, out ExecutionContext context))
                            return false;

                        if (!context.IsRuntimeReady)
                        {
                            string exceptionError = args?["exceptionDetails"]?["exception"]?["value"]?.Value<string>();
                            if (exceptionError == sPauseOnUncaught || exceptionError == sPauseOnCaught)
                                return true;
                        }
                        break;
                    }

                case "Debugger.paused":
                    {
                        // Don't process events from sessions we aren't tracking
                        if (!contexts.TryGetValue(sessionId, out ExecutionContext context))
                            return false;

                        if (!context.IsRuntimeReady)
                        {
                            string reason = args?["reason"]?.Value<string>();
                            if (reason == "exception")
                            {
                                string exceptionError = args?["data"]?["value"]?.Value<string>();
                                if (exceptionError == sPauseOnUncaught)
                                {
                                    await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
                                    if (context.PauseOnExceptions == PauseOnExceptionsKind.Unset)
                                        context.PauseOnExceptions = PauseOnExceptionsKind.Uncaught;
                                    return true;
                                }
                                if (exceptionError == sPauseOnCaught)
                                {
                                    await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
                                    context.PauseOnExceptions = PauseOnExceptionsKind.All;
                                    return true;
                                }
                            }
                        }

                        //TODO figure out how to stich out more frames and, in particular what happens when real wasm is on the stack
                        string top_func = args?["callFrames"]?[0]?["functionName"]?.Value<string>();
                        switch (top_func) {
                            // keep function names un-mangled via src\mono\wasm\runtime\rollup.config.js
                            case "mono_wasm_runtime_ready":
                            case "_mono_wasm_runtime_ready":
                                {
                                    await RuntimeReady(sessionId, token);
                                    await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
                                    return true;
                                }
                            case "mono_wasm_fire_debugger_agent_message":
                            case "_mono_wasm_fire_debugger_agent_message":
                                {
                                    try {
                                        return await OnReceiveDebuggerAgentEvent(sessionId, args, token);
                                    }
                                    catch (Exception) //if the page is refreshed maybe it stops here.
                                    {
                                        await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
                                        return true;
                                    }
                                }
                        }
                        break;
                    }

                case "Debugger.breakpointResolved":
                    {
                        break;
                    }

                case "Debugger.scriptParsed":
                    {
                        string url = args?["url"]?.Value<string>() ?? "";

                        switch (url)
                        {
                            case var _ when url == "":
                            case var _ when url.StartsWith("wasm://", StringComparison.Ordinal):
                            case var _ when url.EndsWith(".wasm", StringComparison.Ordinal):
                                {
                                    logger.LogTrace($"ignoring wasm: Debugger.scriptParsed {url}");
                                    return true;
                                }
                        }
                        Log("verbose", $"proxying Debugger.scriptParsed ({sessionId.sessionId}) {url} {args}");
                        break;
                    }

                case "Target.attachedToTarget":
                    {
                        if (args["targetInfo"]["type"]?.ToString() == "page")
                            await AttachToTarget(new SessionId(args["sessionId"]?.ToString()), token);
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

        private async Task<bool> IsRuntimeAlreadyReadyAlready(SessionId sessionId, CancellationToken token)
        {
            if (contexts.TryGetValue(sessionId, out ExecutionContext context) && context.IsRuntimeReady)
                return true;

            Result res = await SendMonoCommand(sessionId, MonoCommands.IsRuntimeReady(RuntimeId), token);
            return res.Value?["result"]?["value"]?.Value<bool>() ?? false;
        }

        protected override async Task<bool> AcceptCommand(MessageId id, string method, JObject args, CancellationToken token)
        {
            // Inspector doesn't use the Target domain or sessions
            // so we try to init immediately
            if (id == SessionId.Null)
                await AttachToTarget(id, token);

            if (!contexts.TryGetValue(id, out ExecutionContext context))
                return false;

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
                            await SetBreakpoint(id, store, request, !loaded, token);
                        }

                        if (loaded)
                        {
                            // we were already loaded so we should send a response
                            // with the locations included and register the request
                            context.BreakpointRequests[bpid] = request;
                            var result = Result.OkFromObject(request.AsSetBreakpointByUrlResponse(locations));
                            SendResponse(id, result, token);

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

                        var valueOrError = await RuntimeGetPropertiesInternal(id, objectId, args, token, true);
                        if (valueOrError.IsError)
                        {
                            logger.LogDebug($"Runtime.getProperties: {valueOrError.Error}");
                            SendResponse(id, valueOrError.Error.Value, token);
                        }
                        else
                        {
                            SendResponse(id, Result.OkFromObject(valueOrError.Value), token);
                        }
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
                        context.PauseOnExceptions = state switch
                        {
                            "all"      => PauseOnExceptionsKind.All,
                            "uncaught" => PauseOnExceptionsKind.Uncaught,
                            "none"     => PauseOnExceptionsKind.None,
                            _          => PauseOnExceptionsKind.Unset
                        };

                        if (context.IsRuntimeReady)
                            await context.SdbAgent.EnableExceptions(context.PauseOnExceptions, token);
                        // Pass this on to JS too
                        return false;
                    }

                // Protocol extensions
                case "DotnetDebugger.setNextIP":
                    {
                        var loc = SourceLocation.Parse(args?["location"] as JObject);
                        if (loc == null)
                            return false;
                        var ret = await OnSetNextIP(id, loc, token);
                        if (ret == true)
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
                case "DotnetDebugger.addSymbolServerUrl":
                    {
                        string url = args["url"]?.Value<string>();
                        if (!string.IsNullOrEmpty(url) && !urlSymbolServerList.Contains(url))
                            urlSymbolServerList.Add(url);
                        return true;
                    }
                case "DotnetDebugger.getMethodLocation":
                    {
                        DebugStore store = await RuntimeReady(id, token);
                        string aname = args["assemblyName"]?.Value<string>();
                        string typeName = args["typeName"]?.Value<string>();
                        string methodName = args["methodName"]?.Value<string>();
                        if (aname == null || typeName == null || methodName == null)
                        {
                            SendResponse(id, Result.Err("Invalid protocol message '" + args + "'."), token);
                            return true;
                        }

                        // GetAssemblyByName seems to work on file names
                        AssemblyInfo assembly = store.GetAssemblyByName(aname);
                        if (assembly == null)
                            assembly = store.GetAssemblyByName(aname + ".exe");
                        if (assembly == null)
                            assembly = store.GetAssemblyByName(aname + ".dll");
                        if (assembly == null)
                        {
                            SendResponse(id, Result.Err("Assembly '" + aname + "' not found."), token);
                            return true;
                        }

                        TypeInfo type = assembly.GetTypeByName(typeName);
                        if (type == null)
                        {
                            SendResponse(id, Result.Err($"Type '{typeName}' not found."), token);
                            return true;
                        }

                        MethodInfo methodInfo = type.Methods.FirstOrDefault(m => m.Name == methodName);
                        if (methodInfo == null)
                        {
                            // Maybe this is an async method, in which case the debug info is attached
                            // to the async method implementation, in class named:
                            //      `{type_name}.<method_name>::MoveNext`
                            methodInfo = assembly.TypesByName.Values.SingleOrDefault(t => t.FullName.StartsWith($"{typeName}.<{methodName}>"))?
                                .Methods.FirstOrDefault(mi => mi.Name == "MoveNext");
                        }

                        if (methodInfo == null)
                        {
                            SendResponse(id, Result.Err($"Method '{typeName}:{methodName}' not found."), token);
                            return true;
                        }

                        string src_url = methodInfo.Assembly.Sources.Single(sf => sf.SourceId == methodInfo.SourceId).Url;
                        SendResponse(id, Result.OkFromObject(new
                        {
                            result = new { line = methodInfo.StartLocation.Line, column = methodInfo.StartLocation.Column, url = src_url }
                        }), token);

                        return true;
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
                case "DotnetDebugger.justMyCode":
                    {
                        try
                        {
                            SetJustMyCode(id, args, token);
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug($"DotnetDebugger.justMyCode failed for {id} with args {args}: {ex}");
                            SendResponse(id,
                                Result.Exception(new ArgumentException(
                                    $"DotnetDebugger.justMyCode got incorrect argument ({args})")),
                                token);
                        }
                        return true;
                    }
            }

            return false;
        }

        private async Task<bool> ApplyUpdates(MessageId id, JObject args, CancellationToken token)
        {
            var context = GetContext(id);
            string moduleGUID = args["moduleGUID"]?.Value<string>();
            string dmeta = args["dmeta"]?.Value<string>();
            string dil = args["dil"]?.Value<string>();
            string dpdb = args["dpdb"]?.Value<string>();
            var moduleId = await context.SdbAgent.GetModuleId(moduleGUID, token);
            var applyUpdates =  await context.SdbAgent.ApplyUpdates(moduleId, dmeta, dil, dpdb, token);
            return applyUpdates;
        }

        private void SetJustMyCode(MessageId id, JObject args, CancellationToken token)
        {
            var isEnabled = args["enabled"]?.Value<bool>();
            if (isEnabled == null)
                throw new ArgumentException();
            JustMyCode = isEnabled.Value;
            SendResponse(id, Result.OkFromObject(new { justMyCodeEnabled = JustMyCode }), token);
        }
        private async Task<bool> CallOnFunction(MessageId id, JObject args, CancellationToken token)
        {
            var context = GetContext(id);
            if (!DotnetObjectId.TryParse(args["objectId"], out DotnetObjectId objectId)) {
                return false;
            }
            switch (objectId.Scheme)
            {
                case "object":
                case "methodId":
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
                {
                    Result cfo_res = await SendMonoCommand(id, MonoCommands.CallFunctionOn(RuntimeId, args), token);
                    cfo_res = Result.OkFromObject(new { result = cfo_res.Value?["result"]?["value"]});
                    SendResponse(id, cfo_res, token);
                    return true;
                }
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
                var obj = await context.SdbAgent.CreateJObjectForVariableValue(retDebuggerCmdReader, "ret", false, -1, false, token);
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
            ExecutionContext context = GetContext(id);
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

        internal async Task<ValueOrError<JToken>> RuntimeGetPropertiesInternal(SessionId id, DotnetObjectId objectId, JToken args, CancellationToken token, bool sortByAccessLevel = false)
        {
            var context = GetContext(id);
            var accessorPropertiesOnly = false;
            GetObjectCommandOptions objectValuesOpt = GetObjectCommandOptions.WithProperties;
            if (args != null)
            {
                if (args["accessorPropertiesOnly"] != null && args["accessorPropertiesOnly"].Value<bool>())
                {
                    objectValuesOpt |= GetObjectCommandOptions.AccessorPropertiesOnly;
                    accessorPropertiesOnly = true;
                }
                if (args["ownProperties"] != null && args["ownProperties"].Value<bool>())
                {
                    objectValuesOpt |= GetObjectCommandOptions.OwnProperties;
                }
            }
            try {
                switch (objectId.Scheme)
                {
                    case "scope":
                    {
                        Result resScope = await GetScopeProperties(id, objectId.Value, token);
                        return resScope.IsOk
                            ? ValueOrError<JToken>.WithValue(sortByAccessLevel ? resScope.Value : resScope.Value?["result"])
                            : ValueOrError<JToken>.WithError(resScope);
                    }
                    case "valuetype":
                    {
                        var valType = context.SdbAgent.GetValueTypeClass(objectId.Value);
                        if (valType == null)
                            return ValueOrError<JToken>.WithError($"Internal Error: No valuetype found for {objectId}.");
                        var resValue = await valType.GetValues(context.SdbAgent, accessorPropertiesOnly, token);
                        return resValue switch
                        {
                            null => ValueOrError<JToken>.WithError($"Could not get properties for {objectId}"),
                            _ => ValueOrError<JToken>.WithValue(sortByAccessLevel ? JObject.FromObject(new { result = resValue }) : resValue)
                        };
                    }
                    case "array":
                    {
                        var resArr = await context.SdbAgent.GetArrayValues(objectId.Value, token);
                        return ValueOrError<JToken>.WithValue(sortByAccessLevel ? JObject.FromObject(new { result = resArr }) : resArr);
                    }
                    case "methodId":
                    {
                        var resMethod = await context.SdbAgent.InvokeMethodInObject(objectId, objectId.SubValue, null, token);
                        return ValueOrError<JToken>.WithValue(sortByAccessLevel ? JObject.FromObject(new { result = new JArray(resMethod) }) : new JArray(resMethod));
                    }
                    case "object":
                    {
                        var resObj = await context.SdbAgent.GetObjectValues(objectId.Value, objectValuesOpt, token, sortByAccessLevel);
                        return ValueOrError<JToken>.WithValue(sortByAccessLevel ? resObj[0] : resObj);
                    }
                    case "pointer":
                    {
                        var resPointer = new JArray { await context.SdbAgent.GetPointerContent(objectId.Value, token) };
                        return ValueOrError<JToken>.WithValue(sortByAccessLevel ? JObject.FromObject(new { result = resPointer }) : resPointer);
                    }
                    case "cfo_res":
                    {
                        Result res = await SendMonoCommand(id, MonoCommands.GetDetails(RuntimeId, objectId.Value, args), token);
                        string value_json_str = res.Value["result"]?["value"]?["__value_as_json_string__"]?.Value<string>();
                        if (res.IsOk && value_json_str == null)
                            return ValueOrError<JToken>.WithError($"Internal error: Could not find expected __value_as_json_string__ field in the result: {res}");

                        return value_json_str != null
                                    ? ValueOrError<JToken>.WithValue(sortByAccessLevel ? JObject.FromObject(new { result = JArray.Parse(value_json_str) }) : JArray.Parse(value_json_str))
                                    : ValueOrError<JToken>.WithError(res);
                    }
                    default:
                        return ValueOrError<JToken>.WithError($"RuntimeGetProperties: unknown object id scheme: {objectId.Scheme}");
                }
            }
            catch (Exception ex)
            {
                return ValueOrError<JToken>.WithError($"RuntimeGetProperties: Failed to get properties for {objectId}: {ex}");
            }
        }

        private async Task<bool> EvaluateCondition(SessionId sessionId, ExecutionContext context, Frame mono_frame, Breakpoint bp, CancellationToken token)
        {
            if (string.IsNullOrEmpty(bp?.Condition) || mono_frame == null)
                return true;

            string condition = bp.Condition;

            if (bp.ConditionAlreadyEvaluatedWithError)
                return false;
            try {
                var resolver = new MemberReferenceResolver(this, context, sessionId, mono_frame.Id, logger);
                JObject retValue = await resolver.Resolve(condition, token);
                if (retValue == null)
                    retValue = await EvaluateExpression.CompileAndRunTheExpression(condition, resolver, token);
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
            DebugStore store = await LoadStore(sessionId, token);
            AssemblyInfo asm = store.GetAssemblyByName(assemblyName);
            foreach (var method in DebugStore.EnC(asm, meta_buf, pdb_buf))
                await ResetBreakpoint(sessionId, method, token);
            return true;
        }

        private async Task<bool> SendBreakpointsOfMethodUpdated(SessionId sessionId, ExecutionContext context, MonoBinaryReader retDebuggerCmdReader, CancellationToken token)
        {
            var methodId = retDebuggerCmdReader.ReadInt32();
            var method = await context.SdbAgent.GetMethodInfo(methodId, token);
            if (method == null)
            {
                return true;
            }
            foreach (var req in context.BreakpointRequests.Values)
            {
                if (req.Method != null && req.Method.Assembly.Id == method.Info.Assembly.Id && req.Method.Token == method.Info.Token)
                {
                    await SetBreakpoint(sessionId, context.store, req, true, token);
                }
            }
            return true;
        }

        private async Task<bool> SendCallStack(SessionId sessionId, ExecutionContext context, string reason, int thread_id, Breakpoint bp, JObject data, IEnumerable<JObject> orig_callframes, EventKind event_kind, CancellationToken token)
        {
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
                DebugStore store = await LoadStore(sessionId, token);
                var method = await context.SdbAgent.GetMethodInfo(methodId, token);

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

                if (j == 0 && method?.Info.DebuggerAttrInfo.DoAttributesAffectCallStack(JustMyCode) == true)
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

                SourceLocation location = method?.Info.GetLocationByIl(il_pos);

                // When hitting a breakpoint on the "IncrementCount" method in the standard
                // Blazor project template, one of the stack frames is inside mscorlib.dll
                // and we get location==null for it. It will trigger a NullReferenceException
                // if we don't skip over that stack frame.
                if (location == null)
                {
                    continue;
                }

                Log("debug", $"frame il offset: {il_pos} method token: {method.Info.Token} assembly name: {method.Info.Assembly.Name}");
                Log("debug", $"\tmethod {method.Name} location: {location}");
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
                context.ThreadId = thread_id;
            }
            string[] bp_list = new string[bp == null ? 0 : 1];
            if (bp != null)
                bp_list[0] = bp.StackId;

            foreach (JObject frame in orig_callframes)
            {
                string function_name = frame["functionName"]?.Value<string>();
                string url = frame["url"]?.Value<string>();
                if (!(function_name.StartsWith("wasm-function", StringComparison.Ordinal) ||
                        url.StartsWith("wasm://", StringComparison.Ordinal) ||
                        url.EndsWith(".wasm", StringComparison.Ordinal) ||
                        function_name == "_mono_wasm_fire_debugger_agent_message"))
                {
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
            if (!await EvaluateCondition(sessionId, context, context.CallStack.First(), bp, token))
            {
                context.ClearState();
                await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
                return true;
            }
            await SendEvent(sessionId, "Debugger.paused", o, token);

            return true;

            async Task<bool> SkipMethod(bool isSkippable, bool shouldBeSkipped, StepKind stepKind)
            {
                if (isSkippable && shouldBeSkipped)
                {
                    await context.SdbAgent.Step(context.ThreadId, stepKind, token);
                    await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
                    return true;
                }
                return false;
            }
        }
        private async Task<bool> OnReceiveDebuggerAgentEvent(SessionId sessionId, JObject args, CancellationToken token)
        {
            Result res = await SendMonoCommand(sessionId, MonoCommands.GetDebuggerAgentBufferReceived(RuntimeId), token);
            if (!res.IsOk)
                return false;

            ExecutionContext context = GetContext(sessionId);
            byte[] newBytes = Convert.FromBase64String(res.Value?["result"]?["value"]?["value"]?.Value<string>());
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
                switch (event_kind)
                {
                    case EventKind.MethodUpdate:
                    {
                        var ret = await SendBreakpointsOfMethodUpdated(sessionId, context, retDebuggerCmdReader, token);
                        await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
                        return ret;
                    }
                    case EventKind.EnC:
                    {
                        var ret = await ProcessEnC(sessionId, context, retDebuggerCmdReader, token);
                        await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
                        return ret;
                    }
                    case EventKind.Exception:
                    {
                        string reason = "exception";
                        int object_id = retDebuggerCmdReader.ReadInt32();
                        var caught = retDebuggerCmdReader.ReadByte();
                        var exceptionObject = await context.SdbAgent.GetObjectValues(object_id, GetObjectCommandOptions.WithProperties | GetObjectCommandOptions.OwnProperties, token);
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

                        var ret = await SendCallStack(sessionId, context, reason, thread_id, null, data, args?["callFrames"]?.Values<JObject>(), event_kind, token);
                        return ret;
                    }
                    case EventKind.UserBreak:
                    case EventKind.Step:
                    case EventKind.Breakpoint:
                    {
                        Breakpoint bp = context.BreakpointRequests.Values.SelectMany(v => v.Locations).FirstOrDefault(b => b.RemoteId == request_id);
                        if (request_id == context.TempBreakpointForSetNextIP)
                        {
                            context.TempBreakpointForSetNextIP = -1;
                            await context.SdbAgent.RemoveBreakpoint(request_id, token);
                        }
                        string reason = "other";//other means breakpoint
                        int methodId = 0;
                        if (event_kind != EventKind.UserBreak)
                            methodId = retDebuggerCmdReader.ReadInt32();
                        var ret = await SendCallStack(sessionId, context, reason, thread_id, bp, null, args?["callFrames"]?.Values<JObject>(), event_kind, token);
                        return ret;
                    }
                }
            }
            return false;
        }

        internal async Task<MethodInfo> LoadSymbolsOnDemand(AssemblyInfo asm, int method_token, SessionId sessionId, CancellationToken token)
        {
            ExecutionContext context = GetContext(sessionId);
            if (urlSymbolServerList.Count == 0)
                return null;
            if (asm.TriedToLoadSymbolsOnDemand || !asm.PdbInformationAvailable)
                return null;
            asm.TriedToLoadSymbolsOnDemand = true;
            var pdbName = Path.GetFileName(asm.PdbName);

            foreach (string urlSymbolServer in urlSymbolServerList)
            {
                string downloadURL = $"{urlSymbolServer}/{pdbName}/{asm.PdbGuid.ToString("N").ToUpper() + asm.PdbAge}/{pdbName}";

                try
                {
                    using HttpResponseMessage response = await client.GetAsync(downloadURL, token);
                    if (!response.IsSuccessStatusCode)
                    {
                        Log("info", $"Unable to download symbols on demand url:{downloadURL} assembly: {asm.Name}");
                        continue;
                    }

                    using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync(token);
                    asm.UpdatePdbInformation(streamToReadFrom);
                    foreach (SourceFile source in asm.Sources)
                    {
                        var scriptSource = JObject.FromObject(source.ToScriptSource(context.Id, context.AuxData));
                        await SendEvent(sessionId, "Debugger.scriptParsed", scriptSource, token);
                    }
                    return asm.GetMethodByToken(method_token);
                }
                catch (Exception e)
                {
                    Log("info", $"Unable to load symbols on demand exception: {e} url:{downloadURL} assembly: {asm.Name}");
                }
                break;
            }

            Log("info", $"Unable to load symbols on demand assembly: {asm.Name}");
            return null;
        }

        private async Task OnDefaultContext(SessionId sessionId, ExecutionContext context, CancellationToken token)
        {
            Log("verbose", "Default context created, clearing state and sending events");
            if (UpdateContext(sessionId, context, out ExecutionContext previousContext))
            {
                foreach (KeyValuePair<string, BreakpointRequest> kvp in previousContext.BreakpointRequests)
                {
                    context.BreakpointRequests[kvp.Key] = kvp.Value.Clone();
                }
                context.PauseOnExceptions = previousContext.PauseOnExceptions;
            }

            if (await IsRuntimeAlreadyReadyAlready(sessionId, token))
                await RuntimeReady(sessionId, token);
        }

        private async Task OnResume(MessageId msg_id, CancellationToken token)
        {
            ExecutionContext context = GetContext(msg_id);
            if (context.CallStack != null)
            {
                // Stopped on managed code
                await SendMonoCommand(msg_id, MonoCommands.Resume(RuntimeId), token);
            }

            //discard managed frames
            GetContext(msg_id).ClearState();
        }

        private async Task<bool> Step(MessageId msgId, StepKind kind, CancellationToken token)
        {
            ExecutionContext context = GetContext(msgId);
            if (context.CallStack == null)
                return false;

            if (context.CallStack.Count <= 1 && kind == StepKind.Out)
                return false;

            var step = await context.SdbAgent.Step(context.ThreadId, kind, token);
            if (step == false) {
                context.ClearState();
                await SendCommand(msgId, "Debugger.stepOut", new JObject(), token);
                return false;
            }

            SendResponse(msgId, Result.Ok(new JObject()), token);

            context.ClearState();

            await SendCommand(msgId, "Debugger.resume", new JObject(), token);
            return true;
        }

        private async Task<bool> OnJSEventRaised(SessionId sessionId, JObject eventArgs, CancellationToken token)
        {
            string eventName = eventArgs?["eventName"]?.Value<string>();
            if (string.IsNullOrEmpty(eventName))
            {
                logger.LogDebug($"Missing name for raised js event: {eventArgs}");
                return false;
            }

            logger.LogDebug($"OnJsEventRaised: args: {eventArgs}");

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
                var store = await LoadStore(sessionId, token);
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

                var context = GetContext(sessionId);
                foreach (var source in store.Add(sessionId, assembly_name, assembly_data, pdb_data, token))
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

        private async Task<bool> OnEvaluateOnCallFrame(MessageId msg_id, int scopeId, string expression, CancellationToken token)
        {
            try
            {
                ExecutionContext context = GetContext(msg_id);
                if (context.CallStack == null)
                    return false;

                var resolver = new MemberReferenceResolver(this, context, msg_id, scopeId, logger);

                JObject retValue = await resolver.Resolve(expression, token);
                if (retValue == null)
                {
                    retValue = await EvaluateExpression.CompileAndRunTheExpression(expression, resolver, token);
                }

                if (retValue != null)
                {
                    SendResponse(msg_id, Result.OkFromObject(new
                    {
                        result = retValue
                    }), token);
                }
                else
                {
                    SendResponse(msg_id, Result.Err($"Unable to evaluate '{expression}'"), token);
                }
            }
            catch (ReturnAsErrorException ree)
            {
                SendResponse(msg_id, ree.Error, token);
            }
            catch (ExpressionEvaluationFailedException eefe)
            {
                logger.LogDebug($"Error in EvaluateOnCallFrame for expression '{expression}' with '{eefe}.");
                SendResponse(msg_id, Result.Exception(eefe), token);
            }
            catch (Exception e)
            {
                logger.LogDebug($"Error in EvaluateOnCallFrame for expression '{expression}' with '{e}.");
                SendResponse(msg_id, Result.Exception(e), token);
            }

            return true;
        }

        internal async Task<Result> GetScopeProperties(SessionId msg_id, int scopeId, CancellationToken token)
        {
            try
            {
                ExecutionContext context = GetContext(msg_id);
                Frame scope = context.CallStack.FirstOrDefault(s => s.Id == scopeId);
                if (scope == null)
                    return Result.Err(JObject.FromObject(new { message = $"Could not find scope with id #{scopeId}" }));

                VarInfo[] varIds = scope.Method.Info.GetLiveVarsAt(scope.Location.IlLocation.Offset);

                var values = await context.SdbAgent.StackFrameGetValues(scope.Method, context.ThreadId, scopeId, varIds, token);
                if (values != null)
                {
                    if (values == null || values.Count == 0)
                        return Result.OkFromObject(new { result = Array.Empty<object>() });

                    PerScopeCache frameCache = context.GetCacheForScope(scopeId);
                    foreach (JObject value in values)
                    {
                        frameCache.Locals[value["name"]?.Value<string>()] = value;
                    }
                    return Result.OkFromObject(new { result = values });
                }
                return Result.OkFromObject(new { result = Array.Empty<object>() });
            }
            catch (Exception exception)
            {
                Log("verbose", $"Error resolving scope properties {exception.Message}");
                return Result.Exception(exception);
            }
        }

        private async Task<Breakpoint> SetMonoBreakpoint(SessionId sessionId, string reqId, SourceLocation location, string condition, CancellationToken token)
        {
            var context = GetContext(sessionId);
            var bp = new Breakpoint(reqId, location, condition, BreakpointState.Pending);
            string asm_name = bp.Location.IlLocation.Method.Assembly.Name;
            int method_token = bp.Location.IlLocation.Method.Token;
            int il_offset = bp.Location.IlLocation.Offset;

            var assembly_id = await context.SdbAgent.GetAssemblyId(asm_name, token);
            var methodId = await context.SdbAgent.GetMethodIdByToken(assembly_id, method_token, token);
            var breakpoint_id = await context.SdbAgent.SetBreakpoint(methodId, il_offset, token);

            if (breakpoint_id > 0)
            {
                bp.RemoteId = breakpoint_id;
                bp.State = BreakpointState.Active;
                //Log ("verbose", $"BP local id {bp.LocalId} enabled with remote id {bp.RemoteId}");
            }
            return bp;
        }

        private async Task OnSourceFileAdded(SessionId sessionId, SourceFile source, ExecutionContext context, CancellationToken token)
        {
            JObject scriptSource = JObject.FromObject(source.ToScriptSource(context.Id, context.AuxData));
            Log("debug", $"sending {source.Url} {context.Id} {sessionId.sessionId}");
            await SendEvent(sessionId, "Debugger.scriptParsed", scriptSource, token);

            foreach (var req in context.BreakpointRequests.Values)
            {
                if (req.TryResolve(source))
                {
                    await SetBreakpoint(sessionId, context.store, req, true, token);
                }
            }
        }

        internal async Task<DebugStore> LoadStore(SessionId sessionId, CancellationToken token)
        {
            ExecutionContext context = GetContext(sessionId);

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
                    await foreach (SourceFile source in context.store.Load(sessionId, loaded_files, token).WithCancellation(token))
                    {
                        await OnSourceFileAdded(sessionId, source, context, token);
                    }
                }
            }
            catch (Exception e)
            {
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

        private async Task<DebugStore> RuntimeReady(SessionId sessionId, CancellationToken token)
        {
            ExecutionContext context = GetContext(sessionId);
            if (Interlocked.CompareExchange(ref context.ready, new TaskCompletionSource<DebugStore>(), null) != null)
                return await context.ready.Task;

            await context.SdbAgent.SendDebuggerAgentCommand(CmdEventRequest.ClearAllBreakpoints, null, token);

            if (context.PauseOnExceptions != PauseOnExceptionsKind.None && context.PauseOnExceptions != PauseOnExceptionsKind.Unset)
                await context.SdbAgent.EnableExceptions(context.PauseOnExceptions, token);

            await context.SdbAgent.SetProtocolVersion(token);
            await context.SdbAgent.EnableReceiveRequests(EventKind.UserBreak, token);
            await context.SdbAgent.EnableReceiveRequests(EventKind.EnC, token);
            await context.SdbAgent.EnableReceiveRequests(EventKind.MethodUpdate, token);

            DebugStore store = await LoadStore(sessionId, token);
            context.ready.SetResult(store);
            await SendEvent(sessionId, "Mono.runtimeReady", new JObject(), token);
            context.SdbAgent.ResetStore(store);
            return store;
        }

        private async Task ResetBreakpoint(SessionId msg_id, MethodInfo method, CancellationToken token)
        {
            ExecutionContext context = GetContext(msg_id);
            foreach (var req in context.BreakpointRequests.Values)
            {
                if (req.Method != null)
                {
                    if (req.Method.Assembly.Id == method.Assembly.Id && req.Method.Token == method.Token) {
                        await RemoveBreakpoint(msg_id, JObject.FromObject(new {breakpointId = req.Id}), true, token);
                    }
                }
            }
        }

        private async Task RemoveBreakpoint(SessionId msg_id, JObject args, bool isEnCReset, CancellationToken token)
        {
            string bpid = args?["breakpointId"]?.Value<string>();

            ExecutionContext context = GetContext(msg_id);
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
            else
                breakpointRequest.Locations = new List<Breakpoint>();
        }

        private async Task SetBreakpoint(SessionId sessionId, DebugStore store, BreakpointRequest req, bool sendResolvedEvent, CancellationToken token)
        {
            ExecutionContext context = GetContext(sessionId);
            if (req.Locations.Any())
            {
                Log("debug", $"locations already loaded for {req.Id}");
                return;
            }

            var comparer = new SourceLocation.LocationComparer();
            // if column is specified the frontend wants the exact matches
            // and will clear the bp if it isn't close enoug
            IEnumerable<IGrouping<SourceId, SourceLocation>> locations = store.FindBreakpointLocations(req)
                .Distinct(comparer)
                .Where(l => l.Line == req.Line && (req.Column == 0 || l.Column == req.Column))
                .OrderBy(l => l.Column)
                .GroupBy(l => l.Id);

            logger.LogDebug("BP request for '{req}' runtime ready {context.RuntimeReady}", req, GetContext(sessionId).IsRuntimeReady);

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
            ExecutionContext context = GetContext(sessionId);
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

            var breakpointId = await context.SdbAgent.SetBreakpoint(scope.Method.DebugId, ilOffset.Offset, token);
            context.TempBreakpointForSetNextIP = breakpointId;
            await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
            return true;
        }

        private async Task<bool> OnGetScriptSource(MessageId msg_id, string script_id, CancellationToken token)
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
                    if (data is MemoryStream && data.Length == 0)
                        return false;

                    using (var reader = new StreamReader(data))
                        source = await reader.ReadToEndAsync();
                }
                SendResponse(msg_id, Result.OkFromObject(new { scriptSource = source }), token);
            }
            catch (Exception e)
            {
                var o = new
                {
                    scriptSource = $"// Unable to read document ({e.Message})\n" +
                    $"Local path: {src_file?.SourceUri}\n" +
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
                string checkUncaughtExceptions = string.Empty;
                string checkCaughtExceptions = string.Empty;

                //we only need this check if it's a non-vs debugging
                if (sessionId == SessionId.Null)
                {
                    if (!contexts.TryGetValue(sessionId, out ExecutionContext context) || context.PauseOnExceptions == PauseOnExceptionsKind.Unset)
                    {
                        checkUncaughtExceptions = $"throw \"{sPauseOnUncaught}\";";
                        checkCaughtExceptions = $"try {{throw \"{sPauseOnCaught}\";}} catch {{}}";
                    }
                }

                await SendMonoCommand(sessionId, new MonoCommands("globalThis.dotnetDebugger = true"), token);
                Result res = await SendCommand(sessionId,
                    "Page.addScriptToEvaluateOnNewDocument",
                    JObject.FromObject(new { source = $"globalThis.dotnetDebugger = true; delete navigator.constructor.prototype.webdriver; {checkCaughtExceptions} {checkUncaughtExceptions}" }),
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
