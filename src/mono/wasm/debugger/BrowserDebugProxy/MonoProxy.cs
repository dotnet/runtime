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
        internal MonoSDBHelper SdbHelper { get; set; }
        private IList<string> urlSymbolServerList;
        private static HttpClient client = new HttpClient();
        private HashSet<SessionId> sessions = new HashSet<SessionId>();
        private Dictionary<SessionId, ExecutionContext> contexts = new Dictionary<SessionId, ExecutionContext>();
        private const string sPauseOnUncaught = "pause_on_uncaught";
        private const string sPauseOnCaught = "pause_on_caught";

        public MonoProxy(ILoggerFactory loggerFactory, IList<string> urlSymbolServerList) : base(loggerFactory)
        {
            this.urlSymbolServerList = urlSymbolServerList ?? new List<string>();
            SdbHelper = new MonoSDBHelper(this, logger);
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
                            if (a?[0]?["value"]?.ToString() == MonoConstants.RUNTIME_IS_READY &&
                                a?[1]?["value"]?.ToString() == "fe00e07a-5519-4dfe-b35a-f867dbaf2e28")
                            {
                                if (a.Count() > 2)
                                {
                                    try
                                    {
                                        // The optional 3rd argument is the stringified assembly
                                        // list so that we don't have to make more round trips
                                        ExecutionContext context = GetContext(sessionId);
                                        string loaded = a?[2]?["value"]?.ToString();
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
                            else if (a?[0]?["value"]?.ToString() == MonoConstants.EVENT_RAISED)
                            {
                                if (a.Type != JTokenType.Array)
                                {
                                    logger.LogDebug($"Invalid event raised args, expected an array: {a.Type}");
                                }
                                else
                                {
                                    if (JObjectTryParse(a?[2]?["value"]?.Value<string>(), out JObject raiseArgs) &&
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
                        SendEvent(sessionId, method, args, token);
                        JToken ctx = args?["context"];
                        var aux_data = ctx?["auxData"] as JObject;
                        int id = ctx["id"].Value<int>();
                        if (aux_data != null)
                        {
                            bool? is_default = aux_data["isDefault"]?.Value<bool>();
                            if (is_default == true)
                            {
                                await OnDefaultContext(sessionId, new ExecutionContext { Id = id, AuxData = aux_data }, token);
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
                            {
                                return true;
                            }
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
                                    context.PauseOnUncaught = true;
                                    return true;
                                }
                                if (exceptionError == sPauseOnCaught)
                                {
                                    await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
                                    context.PauseOnCaught = true;
                                    return true;
                                }
                            }
                        }
                        //TODO figure out how to stich out more frames and, in particular what happens when real wasm is on the stack
                        string top_func = args?["callFrames"]?[0]?["functionName"]?.Value<string>();
                        switch (top_func) {
                            case "mono_wasm_runtime_ready":
                            case "_mono_wasm_runtime_ready":
                                {
                                    await RuntimeReady(sessionId, token);
                                    await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
                                    return true;
                                }
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
                                {
                                    Log("verbose", $"ignoring wasm: Debugger.scriptParsed {url}");
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
                        await SendMonoCommand(sessionId, MonoCommands.DetachDebugger(), token);
                        break;
                    }
            }

            return false;
        }

        private async Task<bool> IsRuntimeAlreadyReadyAlready(SessionId sessionId, CancellationToken token)
        {
            if (contexts.TryGetValue(sessionId, out ExecutionContext context) && context.IsRuntimeReady)
                return true;

            Result res = await SendMonoCommand(sessionId, MonoCommands.IsRuntimeReady(), token);
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
                                    int.Parse(objectId.Value),
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

                case "Debugger.evaluateOnCallFrame":
                    {
                        if (!DotnetObjectId.TryParse(args?["callFrameId"], out DotnetObjectId objectId))
                            return false;

                        switch (objectId.Scheme)
                        {
                            case "scope":
                                return await OnEvaluateOnCallFrame(id,
                                    int.Parse(objectId.Value),
                                    args?["expression"]?.Value<string>(), token);
                            default:
                                return false;
                        }
                    }

                case "Runtime.getProperties":
                    {
                        if (!DotnetObjectId.TryParse(args?["objectId"], out DotnetObjectId objectId))
                            break;

                        var ret = await RuntimeGetPropertiesInternal(id, objectId, args, token);
                        if (ret == null) {
                            SendResponse(id, Result.Err($"Unable to RuntimeGetProperties '{objectId}'"), token);
                        }
                        else
                            SendResponse(id, Result.OkFromObject(new { result = ret }), token);
                        return true;
                    }

                case "Runtime.releaseObject":
                    {
                        if (!(DotnetObjectId.TryParse(args["objectId"], out DotnetObjectId objectId) && objectId.Scheme == "cfo_res"))
                            break;

                        await SendMonoCommand(id, MonoCommands.ReleaseObject(objectId), token);
                        SendResponse(id, Result.OkFromObject(new { }), token);
                        return true;
                    }

                case "Debugger.setPauseOnExceptions":
                    {
                        string state = args["state"].Value<string>();
                        if (!context.IsRuntimeReady)
                        {
                            context.PauseOnCaught = false;
                            context.PauseOnUncaught = false;
                            switch (state)
                            {
                                case "all":
                                    context.PauseOnCaught = true;
                                    context.PauseOnUncaught = true;
                                    break;
                                case "uncaught":
                                    context.PauseOnUncaught = true;
                                    break;
                            }
                        }
                        else
                            await SdbHelper.EnableExceptions(id, state, token);
                        // Pass this on to JS too
                        return false;
                    }

                // Protocol extensions
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
                            //      `{type_name}/<method_name>::MoveNext`
                            methodInfo = assembly.TypesByName.Values.SingleOrDefault(t => t.FullName.StartsWith($"{typeName}/<{methodName}>"))?
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
                        catch (Exception){
                            SendResponse(id,
                                Result.Exception(new ArgumentException(
                                    $"Runtime.callFunctionOn not supported with ({args["objectId"]}).")),
                                token);
                            return true;
                        }
                    }
            }

            return false;
        }
        private async Task<bool> CallOnFunction(MessageId id, JObject args, CancellationToken token)
        {
            if (!DotnetObjectId.TryParse(args["objectId"], out DotnetObjectId objectId)) {
                return false;
            }
            switch (objectId.Scheme)
            {
                case "object":
                    args["details"]  = await SdbHelper.GetObjectProxy(id, int.Parse(objectId.Value), token);
                    break;
                case "valuetype":
                    args["details"]  = await SdbHelper.GetValueTypeProxy(id, int.Parse(objectId.Value), token);
                    break;
                case "pointer":
                    args["details"]  = await SdbHelper.GetPointerContent(id, int.Parse(objectId.Value), token);
                    break;
                case "array":
                    args["details"]  = await SdbHelper.GetArrayValues(id, int.Parse(objectId.Value), token);
                    break;
                case "cfo_res":
                {
                    Result cfo_res = await SendMonoCommand(id, MonoCommands.CallFunctionOn(args), token);
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
            Result res = await SendMonoCommand(id, MonoCommands.CallFunctionOn(args), token);
            if (res.IsErr)
            {
                SendResponse(id, res, token);
                return true;
            }
            if (res.Value?["result"]?["value"]?["type"] == null) //it means that is not a buffer returned from the debugger-agent
            {
                byte[] newBytes = Convert.FromBase64String(res.Value?["result"]?["value"]?["value"]?.Value<string>());
                var retDebuggerCmd = new MemoryStream(newBytes);
                var retDebuggerCmdReader = new MonoBinaryReader(retDebuggerCmd);
                retDebuggerCmdReader.ReadByte(); //number of objects returned.
                var obj = await SdbHelper.CreateJObjectForVariableValue(id, retDebuggerCmdReader, "ret", false, -1, false, token);
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
            ExecutionContext ctx = GetContext(id);
            Frame scope = ctx.CallStack.FirstOrDefault(s => s.Id == scopeId);
            if (scope == null)
                return false;
            var varIds = scope.Method.Info.GetLiveVarsAt(scope.Location.CliLocation.Offset);
            if (varIds == null)
                return false;
            var varToSetValue = varIds.FirstOrDefault(v => v.Name == varName);
            if (varToSetValue == null)
                return false;
            var res = await SdbHelper.SetVariableValue(id, ctx.ThreadId, scopeId, varToSetValue.Index, varValue["value"].Value<string>(), token);
            if (res)
                SendResponse(id, Result.Ok(new JObject()), token);
            else
                SendResponse(id, Result.Err($"Unable to set '{varValue["value"].Value<string>()}' to variable '{varName}'"), token);
            return true;
        }

        internal async Task<JToken> RuntimeGetPropertiesInternal(SessionId id, DotnetObjectId objectId, JToken args, CancellationToken token)
        {
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
            //Console.WriteLine($"RuntimeGetProperties - {args}");
            try {
                switch (objectId.Scheme)
                {
                    case "scope":
                    {
                        var res = await GetScopeProperties(id, int.Parse(objectId.Value), token);
                        return res.Value?["result"];
                    }
                    case "valuetype":
                        return await SdbHelper.GetValueTypeValues(id, int.Parse(objectId.Value), accessorPropertiesOnly, token);
                    case "array":
                        return await SdbHelper.GetArrayValues(id, int.Parse(objectId.Value), token);
                    case "object":
                        return await SdbHelper.GetObjectValues(id, int.Parse(objectId.Value), objectValuesOpt, token);
                    case "pointer":
                        return new JArray{await SdbHelper.GetPointerContent(id, int.Parse(objectId.Value), token)};
                    case "cfo_res":
                    {
                        Result res = await SendMonoCommand(id, MonoCommands.GetDetails(int.Parse(objectId.Value), args), token);
                        string value_json_str = res.Value["result"]?["value"]?["__value_as_json_string__"]?.Value<string>();
                        return value_json_str != null ? JArray.Parse(value_json_str) : null;
                    }
                    default:
                        return null;

                }
            }
            catch (Exception) {
                return null;
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
                else if (retValue?["value"]?.Type != JTokenType.Null)
                    return true;
            }
            catch (Exception e)
            {
                Log("info", $"Unable evaluate conditional breakpoint: {e} condition:{condition}");
                bp.ConditionAlreadyEvaluatedWithError = true;
                return false;
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

            var assemblyName = await SdbHelper.GetAssemblyNameFromModule(sessionId, moduleId, token);
            DebugStore store = await LoadStore(sessionId, token);
            AssemblyInfo asm = store.GetAssemblyByName(assemblyName);
            foreach (var method in store.EnC(sessionId, asm, meta_buf, pdb_buf))
                await ResetBreakpoint(sessionId, method, token);
            return true;
        }

        private async Task<bool> SendBreakpointsOfMethodUpdated(SessionId sessionId, ExecutionContext context, MonoBinaryReader retDebuggerCmdReader, CancellationToken token)
        {
            var methodId = retDebuggerCmdReader.ReadInt32();
            var method = await SdbHelper.GetMethodInfo(sessionId, methodId, token);
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

        private async Task<bool> SendCallStack(SessionId sessionId, ExecutionContext context, string reason, int thread_id, Breakpoint bp, JObject data, IEnumerable<JObject> orig_callframes, CancellationToken token)
        {
            var callFrames = new List<object>();
            var frames = new List<Frame>();
            var commandParams = new MemoryStream();
            var commandParamsWriter = new MonoBinaryWriter(commandParams);
            commandParamsWriter.Write(thread_id);
            commandParamsWriter.Write(0);
            commandParamsWriter.Write(-1);
            var retDebuggerCmdReader = await SdbHelper.SendDebuggerAgentCommand<CmdThread>(sessionId, CmdThread.GetFrameInfo, commandParams, token);
            var frame_count = retDebuggerCmdReader.ReadInt32();
            //Console.WriteLine("frame_count - " + frame_count);
            for (int j = 0; j < frame_count; j++) {
                var frame_id = retDebuggerCmdReader.ReadInt32();
                var methodId = retDebuggerCmdReader.ReadInt32();
                var il_pos = retDebuggerCmdReader.ReadInt32();
                var flags = retDebuggerCmdReader.ReadByte();
                DebugStore store = await LoadStore(sessionId, token);
                var method = await SdbHelper.GetMethodInfo(sessionId, methodId, token);

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
                        url.StartsWith("wasm://wasm/", StringComparison.Ordinal) || function_name == "_mono_wasm_fire_debugger_agent_message"))
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
                await SendCommand(sessionId, "Debugger.resume", new JObject(), token);
                return true;
            }
            SendEvent(sessionId, "Debugger.paused", o, token);

            return true;
        }
        private async Task<bool> OnReceiveDebuggerAgentEvent(SessionId sessionId, JObject args, CancellationToken token)
        {
            Result res = await SendMonoCommand(sessionId, MonoCommands.GetDebuggerAgentBufferReceived(), token);
            if (res.IsErr)
                return false;

            ExecutionContext context = GetContext(sessionId);
            byte[] newBytes = Convert.FromBase64String(res.Value?["result"]?["value"]?["value"]?.Value<string>());
            var retDebuggerCmd = new MemoryStream(newBytes);
            var retDebuggerCmdReader = new MonoBinaryReader(retDebuggerCmd);
            retDebuggerCmdReader.ReadBytes(11); //skip HEADER_LEN
            retDebuggerCmdReader.ReadByte(); //suspend_policy
            var number_of_events = retDebuggerCmdReader.ReadInt32(); //number of events -> should be always one
            for (int i = 0 ; i < number_of_events; i++) {
                var event_kind = (EventKind)retDebuggerCmdReader.ReadByte(); //event kind
                var request_id = retDebuggerCmdReader.ReadInt32(); //request id
                if (event_kind == EventKind.Step)
                    await SdbHelper.ClearSingleStep(sessionId, request_id, token);
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
                        var exceptionObject = await SdbHelper.GetObjectValues(sessionId, object_id, GetObjectCommandOptions.WithProperties | GetObjectCommandOptions.OwnProperties, token);
                        var exceptionObjectMessage = exceptionObject.FirstOrDefault(attr => attr["name"].Value<string>().Equals("message"));
                        var data = JObject.FromObject(new
                        {
                            type = "object",
                            subtype = "error",
                            className = await SdbHelper.GetClassNameFromObject(sessionId, object_id, token),
                            uncaught = caught == 0,
                            description = exceptionObjectMessage["value"]["value"].Value<string>(),
                            objectId = $"dotnet:object:{object_id}"
                        });

                        var ret = await SendCallStack(sessionId, context, reason, thread_id, null, data, args?["callFrames"]?.Values<JObject>(), token);
                        return ret;
                    }
                    case EventKind.UserBreak:
                    case EventKind.Step:
                    case EventKind.Breakpoint:
                    {
                        Breakpoint bp = context.BreakpointRequests.Values.SelectMany(v => v.Locations).FirstOrDefault(b => b.RemoteId == request_id);
                        string reason = "other";//other means breakpoint
                        int methodId = 0;
                        if (event_kind != EventKind.UserBreak)
                            methodId = retDebuggerCmdReader.ReadInt32();
                        var ret = await SendCallStack(sessionId, context, reason, thread_id, bp, null, args?["callFrames"]?.Values<JObject>(), token);
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
            if (asm.TriedToLoadSymbolsOnDemand)
                return null;
            asm.TriedToLoadSymbolsOnDemand = true;
            var peReader = asm.peReader;
            var entries = peReader.ReadDebugDirectory();
            var codeView = entries[0];
            var codeViewData = peReader.ReadCodeViewDebugDirectoryData(codeView);
            int pdbAge = codeViewData.Age;
            var pdbGuid = codeViewData.Guid;
            string pdbName = codeViewData.Path;
            pdbName = Path.GetFileName(pdbName);

            foreach (string urlSymbolServer in urlSymbolServerList)
            {
                string downloadURL = $"{urlSymbolServer}/{pdbName}/{pdbGuid.ToString("N").ToUpper() + pdbAge}/{pdbName}";

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
                        SendEvent(sessionId, "Debugger.scriptParsed", scriptSource, token);
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
            }

            if (await IsRuntimeAlreadyReadyAlready(sessionId, token))
                await RuntimeReady(sessionId, token);
        }

        private async Task OnResume(MessageId msg_id, CancellationToken token)
        {
            ExecutionContext ctx = GetContext(msg_id);
            if (ctx.CallStack != null)
            {
                // Stopped on managed code
                await SendMonoCommand(msg_id, MonoCommands.Resume(), token);
            }

            //discard managed frames
            SdbHelper.ClearCache();
            GetContext(msg_id).ClearState();
        }

        private async Task<bool> Step(MessageId msg_id, StepKind kind, CancellationToken token)
        {
            ExecutionContext context = GetContext(msg_id);
            if (context.CallStack == null)
                return false;

            if (context.CallStack.Count <= 1 && kind == StepKind.Out)
                return false;

            var step = await SdbHelper.Step(msg_id, context.ThreadId, kind, token);
            if (step == false) {
                SdbHelper.ClearCache();
                context.ClearState();
                await SendCommand(msg_id, "Debugger.stepOut", new JObject(), token);
                return false;
            }

            SendResponse(msg_id, Result.Ok(new JObject()), token);

            context.ClearState();

            await SendCommand(msg_id, "Debugger.resume", new JObject(), token);
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

                if (store.GetAssemblyByUnqualifiedName(assembly_name) != null)
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
                foreach (var source in store.Add(sessionId, assembly_data, pdb_data))
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
                ExecutionContext ctx = GetContext(msg_id);
                Frame scope = ctx.CallStack.FirstOrDefault(s => s.Id == scopeId);
                if (scope == null)
                    return Result.Err(JObject.FromObject(new { message = $"Could not find scope with id #{scopeId}" }));

                VarInfo[] varIds = scope.Method.Info.GetLiveVarsAt(scope.Location.CliLocation.Offset);

                var values = await SdbHelper.StackFrameGetValues(msg_id, scope.Method, ctx.ThreadId, scopeId, varIds, token);
                if (values != null)
                {
                    if (values == null || values.Count == 0)
                        return Result.OkFromObject(new { result = Array.Empty<object>() });

                    PerScopeCache frameCache = ctx.GetCacheForScope(scopeId);
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
            var bp = new Breakpoint(reqId, location, condition, BreakpointState.Pending);
            string asm_name = bp.Location.CliLocation.Method.Assembly.Name;
            int method_token = bp.Location.CliLocation.Method.Token;
            int il_offset = bp.Location.CliLocation.Offset;

            var assembly_id = await SdbHelper.GetAssemblyId(sessionId, asm_name, token);
            var methodId = await SdbHelper.GetMethodIdByToken(sessionId, assembly_id, method_token, token);
            var breakpoint_id = await SdbHelper.SetBreakpoint(sessionId, methodId, il_offset, token);

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
            SendEvent(sessionId, "Debugger.scriptParsed", scriptSource, token);

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

            if (Interlocked.CompareExchange(ref context.store, new DebugStore(logger), null) != null)
                return await context.Source.Task;

            try
            {
                string[] loaded_files = context.LoadedFiles;

                if (loaded_files == null)
                {
                    Result loaded = await SendMonoCommand(sessionId, MonoCommands.GetLoadedFiles(), token);
                    loaded_files = loaded.Value?["result"]?["value"]?.ToObject<string[]>();
                }

                await
                foreach (SourceFile source in context.store.Load(sessionId, loaded_files, token).WithCancellation(token))
                {
                    await OnSourceFileAdded(sessionId, source, context, token);
                }
            }
            catch (Exception e)
            {
                context.Source.SetException(e);
            }

            if (!context.Source.Task.IsCompleted)
                context.Source.SetResult(context.store);
            return context.store;
        }

        private async Task<DebugStore> RuntimeReady(SessionId sessionId, CancellationToken token)
        {
            ExecutionContext context = GetContext(sessionId);
            if (Interlocked.CompareExchange(ref context.ready, new TaskCompletionSource<DebugStore>(), null) != null)
                return await context.ready.Task;

            var commandParams = new MemoryStream();
            var retDebuggerCmdReader = await SdbHelper.SendDebuggerAgentCommand<CmdEventRequest>(sessionId, CmdEventRequest.ClearAllBreakpoints, commandParams, token);
            if (retDebuggerCmdReader == null)
            {
                Log("verbose", $"Failed to clear breakpoints");
            }

            if (context.PauseOnCaught && context.PauseOnUncaught)
                await SdbHelper.EnableExceptions(sessionId, "all", token);
            else if (context.PauseOnUncaught)
                await SdbHelper.EnableExceptions(sessionId, "uncaught", token);

            await SdbHelper.SetProtocolVersion(sessionId, token);
            await SdbHelper.EnableReceiveRequests(sessionId, EventKind.UserBreak, token);
            await SdbHelper.EnableReceiveRequests(sessionId, EventKind.EnC, token);
            await SdbHelper.EnableReceiveRequests(sessionId, EventKind.MethodUpdate, token);

            DebugStore store = await LoadStore(sessionId, token);
            context.ready.SetResult(store);
            SendEvent(sessionId, "Mono.runtimeReady", new JObject(), token);
            SdbHelper.SetStore(store);
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
                var breakpoint_removed = await SdbHelper.RemoveBreakpoint(msg_id, bp.RemoteId, token);
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
                req.Method = loc.CliLocation.Method;
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
                    SendEvent(sessionId, "Debugger.breakpointResolved", JObject.FromObject(resolvedLocation), token);
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
                    if (data.Length == 0)
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
                string checkUncaughtExceptions = $"throw \"{sPauseOnUncaught}\";";
                string checkCaughtExceptions = $"try {{throw \"{sPauseOnCaught}\";}} catch {{}}";
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
