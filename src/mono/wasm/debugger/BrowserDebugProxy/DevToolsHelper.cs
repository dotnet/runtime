// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics
{
    public struct SessionId : IEquatable<SessionId>
    {
        public readonly string sessionId;

        public SessionId(string sessionId)
        {
            this.sessionId = sessionId;
        }

        // hashset treats 0 as unset
        public override int GetHashCode() => sessionId?.GetHashCode() ?? -1;

        public override bool Equals(object obj) => obj is SessionId other && Equals(other);

        public bool Equals(SessionId other) => other.sessionId == sessionId;

        public static bool operator ==(SessionId a, SessionId b) => a.sessionId == b.sessionId;

        public static bool operator !=(SessionId a, SessionId b) => a.sessionId != b.sessionId;

        public static SessionId Null { get; }

        public override string ToString() => $"session-{sessionId}";
    }

    public class MessageId : IEquatable<MessageId>
    {
        public readonly string sessionId;
        public readonly int id;

        public MessageId(string sessionId, int id)
        {
            this.sessionId = sessionId;
            this.id = id;
        }

        public static implicit operator SessionId(MessageId id) => new SessionId(id.sessionId);

        public override string ToString() => $"msg-{sessionId}:::{id}";

        public override int GetHashCode() => id;

        public override bool Equals(object obj) => obj is MessageId other && Equals(other);

        public bool Equals(MessageId other) => other.id == id;
    }

    internal sealed class DotnetObjectId
    {
        private readonly int? _intValue;

        public string Scheme { get; }
        public int Value
        {
            get
            {
                if (_intValue == null)
                    throw new ArgumentException($"DotnetObjectId (scheme: {Scheme}, ValueAsJson: {ValueAsJson}) does not have an int value");
                return _intValue.Value;
            }
        }
        public int SubValue { get; set; }
        public bool IsValueType => Scheme == "valuetype";

        public JObject ValueAsJson { get; init; }

        public static bool TryParse(JToken jToken, out DotnetObjectId objectId) => TryParse(jToken?.Value<string>(), out objectId);

        public static bool TryParse(string id, out DotnetObjectId objectId)
        {
            objectId = null;
            if (id == null)
                return false;

            if (!id.StartsWith("dotnet:"))
                return false;

            string[] parts = id.Split(":", 3);

            if (parts.Length < 3)
                return false;

            objectId = new DotnetObjectId(parts[1], parts[2]);
            return true;
        }

        public DotnetObjectId(string scheme, int value)
                : this(scheme, value.ToString()) { }

        public DotnetObjectId(string scheme, string value)
        {
            Scheme = scheme;
            if (int.TryParse(value, out int ival))
            {
                _intValue = ival;
            }
            else
            {
                try
                {
                    ValueAsJson = JObject.Parse(value);
                }
                catch (JsonReaderException) { }
            }
        }

        public override string ToString()
            => _intValue != null
                    ? $"dotnet:{Scheme}:{_intValue}"
                    : $"dotnet:{Scheme}:{ValueAsJson}";
    }

    public struct Result
    {
        public JObject Value { get; private set; }
        public JObject Error { get; private set; }
        public JObject FullContent { get; private set; }

        public bool IsOk => Error == null;

        private Result(JObject resultOrError, bool isError, JObject fullContent = null)
        {
            ArgumentNullException.ThrowIfNull(resultOrError);

            bool resultHasError = isError || string.Equals((resultOrError["result"] as JObject)?["subtype"]?.Value<string>(), "error");
            resultHasError |= resultOrError["exceptionDetails"] != null;
            if (resultHasError)
            {
                Value = null;
                Error = resultOrError;
            }
            else
            {
                Value = resultOrError;
                Error = null;
            }
            FullContent = fullContent;
        }
        public static Result FromJson(JObject obj)
        {
            var error = obj["error"] as JObject;
            if (error != null)
                return new Result(error, true);
            var result = (obj["result"] as JObject) ?? new JObject();
            return new Result(result, false);
        }
        public static Result FromJsonFirefox(JObject obj)
        {
            //Log ("protocol", $"from result: {obj}");
            JObject o;
            if (obj["ownProperties"] != null && obj["prototype"]?["class"]?.Value<string>() == "Array")
            {
                var ret = new JArray();
                var arrayItems = obj["ownProperties"];
                foreach (JProperty arrayItem in arrayItems)
                {
                    if (arrayItem.Name != "length")
                        ret.Add(arrayItem.Value["value"]);
                }
                o = JObject.FromObject(new
                {
                    result = new
                    {
                        value = ret
                    }
                });
            }
            else if (obj["result"] is JObject && obj["result"]?["type"]?.Value<string>() == "object")
            {
                if (obj["result"]["class"].Value<string>() == "Array")
                {
                    o = JObject.FromObject(new
                    {
                        result = new
                        {
                            value = obj["result"]["preview"]["items"]
                        }
                    });
                }
                else if (obj["result"]?["preview"] != null)
                {
                    o = JObject.FromObject(new
                    {
                        result = new
                        {
                            value = obj["result"]?["preview"]?["ownProperties"]?["value"]
                        }
                    });
                }
                else
                {
                    o = JObject.FromObject(new
                    {
                        result = new
                        {
                            value = obj["result"]
                        }
                    });
                }
            }
            else if (obj["result"] != null)
            {
                o = JObject.FromObject(new
                {
                    result = new
                    {
                        value = obj["result"],
                        type = obj["resultType"],
                        description = obj["resultDescription"]
                    }
                });
            }
            else
            {
                o = JObject.FromObject(new
                {
                    result = new
                    {
                        value = obj
                    }
                });
            }
            bool resultHasError = obj["hasException"] != null && obj["hasException"].Value<bool>();
            if (resultHasError)
            {
                return new Result(obj["exception"] as JObject, resultHasError, obj);
            }
            return new Result(o, false, obj);
        }

        public static Result Ok(JObject ok) => new Result(ok, false);

        public static Result OkFromObject(object ok) => Ok(JObject.FromObject(ok));

        public static Result Err(JObject err) => new Result(err, true);

        public static Result Err(string msg) => new Result(JObject.FromObject(new { message = msg }), true);

        public static Result UserVisibleErr(JObject result) => new Result { Value = result };

        public static Result Exception(Exception e) => new Result(JObject.FromObject(new { message = e.Message }), true);

        public JObject ToJObject(MessageId target)
        {
            if (IsOk)
            {
                return JObject.FromObject(new
                {
                    target.id,
                    target.sessionId,
                    result = Value
                });
            }
            else
            {
                return JObject.FromObject(new
                {
                    target.id,
                    target.sessionId,
                    error = Error
                });
            }
        }

        public override string ToString()
        {
            return $"[Result: IsOk: {IsOk}, IsErr: {!IsOk}, Value: {Value?.ToString()}, Error: {Error?.ToString()} ]";
        }
    }

    internal sealed class MonoCommands
    {
        public string expression { get; set; }
        public string objectGroup { get; set; } = "mono-debugger";
        public bool includeCommandLineAPI { get; set; }
        public bool silent { get; set; }
        public bool returnByValue { get; set; } = true;

        public MonoCommands(string expression) => this.expression = expression;

        public static MonoCommands GetDebuggerAgentBufferReceived(int runtimeId) => new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_get_dbg_command_info()");

        public static MonoCommands IsRuntimeReady(int runtimeId) => new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_runtime_is_ready");

        public static MonoCommands GetLoadedFiles(int runtimeId) => new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_get_loaded_files()");

        public static MonoCommands SetDebuggerAttached(int runtimeId) => new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_debugger_attached()");

        public static MonoCommands SendDebuggerAgentCommand(int runtimeId, int id, int command_set, int command, string command_parameters)
        {
            return new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_send_dbg_command ({id}, {command_set}, {command},'{command_parameters}')");
        }

        public static MonoCommands SendDebuggerAgentCommandWithParms(int runtimeId, int id, int command_set, int command, string command_parameters, int len, int type, string parm)
        {
            return new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_send_dbg_command_with_parms ({id}, {command_set}, {command},'{command_parameters}', {len}, {type}, '{parm}')");
        }

        public static MonoCommands CallFunctionOn(int runtimeId, JToken args) => new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_call_function_on ({args})");

        public static MonoCommands GetDetails(int runtimeId, int objectId, JToken args = null) => new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_get_details ({objectId}, {(args ?? "{ }")})");

        public static MonoCommands Resume(int runtimeId) => new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_debugger_resume ()");

        public static MonoCommands DetachDebugger(int runtimeId) => new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_detach_debugger()");

        public static MonoCommands ReleaseObject(int runtimeId, DotnetObjectId objectId) => new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_release_object('{objectId}')");

        public static MonoCommands GetWasmFunctionIds(int runtimeId) => new MonoCommands($"getDotnetRuntime({runtimeId}).INTERNAL.mono_wasm_get_func_id_to_name_mappings()");
    }

    internal enum MonoErrorCodes
    {
        BpNotFound = 100000,
    }

    internal static class MonoConstants
    {
        public const string EVENT_RAISED = "mono_wasm_debug_event_raised:aef14bca-5519-4dfe-b35a-f867abc123ae";
    }

    internal sealed class Frame
    {
        public Frame(MethodInfoWithDebugInformation method, SourceLocation location, int id)
        {
            this.Method = method;
            this.Location = location;
            this.Id = id;
        }

        public MethodInfoWithDebugInformation Method { get; private set; }
        public SourceLocation Location { get; private set; }
        public int Id { get; private set; }
    }

    internal sealed class Breakpoint
    {
        public SourceLocation Location { get; private set; }
        public int RemoteId { get; set; }
        public BreakpointState State { get; set; }
        public string StackId { get; private set; }
        public string Condition { get; set; }
        public bool ConditionAlreadyEvaluatedWithError { get; set; }
        public static bool TryParseId(string stackId, out int id)
        {
            id = -1;
            if (stackId?.StartsWith("dotnet:", StringComparison.Ordinal) != true)
                return false;

            return int.TryParse(stackId.AsSpan("dotnet:".Length), out id);
        }

        public Breakpoint(string stackId, SourceLocation loc, string condition, BreakpointState state)
        {
            this.StackId = stackId;
            this.Location = loc;
            this.State = state;
            this.Condition = condition;
            this.ConditionAlreadyEvaluatedWithError = false;
        }
    }

    internal enum BreakpointState
    {
        Active,
        Disabled,
        Pending
    }

    internal enum StepKind
    {
        Into,
        Over,
        Out
    }

    internal enum PauseOnExceptionsKind
    {
        Unset,
        None,
        Uncaught,
        All
    }

    internal class ExecutionContext
    {
        public ExecutionContext(MonoSDBHelper sdbAgent, int id, object auxData, PauseOnExceptionsKind pauseOnExceptions)
        {
            Id = id;
            AuxData = auxData;
            SdbAgent = sdbAgent;
            PauseOnExceptions = pauseOnExceptions;
            Destroyed = false;
            FrameworkScriptList = new();
        }
        public ExecutionContext CreateChildAsyncExecutionContext(SessionId sessionId)
            => new ExecutionContext(null, Id, AuxData, PauseOnExceptions)
            {
                ParentContext = this,
                SessionId = sessionId
            };
        public bool CopyDataFromParentContext()
        {
            if (SdbAgent != null)
                return false;
            ready = ParentContext.ready;
            store = ParentContext.store;
            Source = ParentContext.Source;
            SdbAgent = ParentContext.SdbAgent.Clone(SessionId);
            return true;
        }
        public string DebugId { get; set; }
        public Dictionary<string, BreakpointRequest> BreakpointRequests { get; } = new Dictionary<string, BreakpointRequest>();
        public int breakpointId;
        public TaskCompletionSource<DebugStore> ready;
        public bool IsRuntimeReady => ready != null && ready.Task.IsCompleted;
        public bool IsSkippingHiddenMethod { get; set; }
        public bool IsSteppingThroughMethod { get; set; }
        public bool IsResumedAfterBp { get; set; }
        public int ThreadId { get; set; }
        public int Id { get; set; }
        public ExecutionContext ParentContext { get; private set; }

        public List<int> FrameworkScriptList { get; init; }
        public SessionId SessionId { get; private set; }

        public bool PausedOnWasm { get; set; }

        public string PauseKind { get; set; }

        public object AuxData { get; set; }

        public bool AutoEvaluateProperties { get; set; }

        public PauseOnExceptionsKind PauseOnExceptions { get; set; }

        public List<Frame> CallStack { get; set; }

        public string[] LoadedFiles { get; set; }
        internal DebugStore store;
        internal MonoSDBHelper SdbAgent { get; private set; }
        public TaskCompletionSource<DebugStore> Source { get; private set; } = new TaskCompletionSource<DebugStore>();

        private Dictionary<int, PerScopeCache> perScopeCaches { get; } = new Dictionary<int, PerScopeCache>();

        internal int TempBreakpointForSetNextIP { get; set; }
        internal bool FirstBreakpoint { get; set; }

        internal bool Destroyed { get; set; }

        public DebugStore Store
        {
            get
            {
                if (store == null || !Source.Task.IsCompleted)
                    return null;

                return store;
            }
        }
        public string[] WasmFunctionIds { get; internal set; }

        public PerScopeCache GetCacheForScope(int scopeId)
        {
            if (perScopeCaches.TryGetValue(scopeId, out PerScopeCache cache))
                return cache;

            cache = new PerScopeCache();
            perScopeCaches[scopeId] = cache;
            return cache;
        }

        public void ClearState()
        {
            CallStack = null;
            SdbAgent.ClearCache();
            perScopeCaches.Clear();
        }
    }

    internal sealed class PerScopeCache
    {
        public Dictionary<string, JObject> Locals { get; } = new Dictionary<string, JObject>();
        public Dictionary<string, JObject> MemberReferences { get; } = new Dictionary<string, JObject>();
        public Dictionary<string, JObject> ObjectFields { get; } = new Dictionary<string, JObject>();
        public Dictionary<string, JObject> EvaluationResults { get; } = new();
        public PerScopeCache(JArray objectValues)
        {
            foreach (var objectValue in objectValues)
            {
                ObjectFields[objectValue["name"].Value<string>()] = objectValue.Value<JObject>();
            }
        }
        public PerScopeCache()
        {
        }
    }

    internal sealed class ConcurrentExecutionContextDictionary
    {
        private ConcurrentDictionary<SessionId, ConcurrentBag<ExecutionContext>> contexts = new ();
        public ExecutionContext GetCurrentContext(SessionId sessionId)
            => TryGetCurrentExecutionContextValue(sessionId, out ExecutionContext context)
                ? context
                : throw new KeyNotFoundException($"No execution context found for session {sessionId}");

        public bool TryGetCurrentExecutionContextValue(SessionId id, out ExecutionContext executionContext, bool ignoreDestroyedContext = true)
        {
            executionContext = null;
            if (!contexts.TryGetValue(id, out ConcurrentBag<ExecutionContext> contextBag))
                return false;
            if (contextBag.IsEmpty)
                return false;
            IEnumerable<ExecutionContext> validContexts = null;
            if (ignoreDestroyedContext)
                validContexts = contextBag.Where(context => context.Destroyed == false);
            else
                validContexts = contextBag;
            if (!validContexts.Any())
                return false;
            int maxId = validContexts.Max(context => context.Id);
            executionContext = contextBag.FirstOrDefault(context => context.Id == maxId);
            return executionContext != null;
        }

        public void OnDefaultContextUpdate(SessionId sessionId, ExecutionContext newContext)
        {
            if (TryGetAndAddContext(sessionId, newContext, out ExecutionContext previousContext))
            {
                foreach (KeyValuePair<string, BreakpointRequest> kvp in previousContext.BreakpointRequests)
                {
                    newContext.BreakpointRequests[kvp.Key] = kvp.Value.Clone();
                }
                newContext.PauseOnExceptions = previousContext.PauseOnExceptions;
            }
        }

        public bool TryGetAndAddContext(SessionId sessionId, ExecutionContext newExecutionContext, out ExecutionContext previousExecutionContext)
        {
            bool hasExisting = TryGetCurrentExecutionContextValue(sessionId, out previousExecutionContext, ignoreDestroyedContext: false);
            ConcurrentBag<ExecutionContext> bag = contexts.GetOrAdd(sessionId, _ => new ConcurrentBag<ExecutionContext>());
            bag.Add(newExecutionContext);
            return hasExisting;
        }

        public void CreateWorkerExecutionContext(SessionId workerSessionId, SessionId originSessionId, ILogger logger)
        {
            if (!TryGetCurrentExecutionContextValue(originSessionId, out ExecutionContext context))
            {
                logger.LogDebug($"Origin sessionId does not exist - {originSessionId}");
                return;
            }
            if (contexts.ContainsKey(workerSessionId))
            {
                logger.LogDebug($"Worker sessionId already exists - {originSessionId}");
                return;
            }
            contexts[workerSessionId] = new();
            contexts[workerSessionId].Add(context.CreateChildAsyncExecutionContext(workerSessionId));
        }

        public void DestroyContext(SessionId sessionId, int id)
        {
            if (!contexts.TryGetValue(sessionId, out ConcurrentBag<ExecutionContext> contextBag))
                return;
            foreach (ExecutionContext context in contextBag.Where(x => x.Id == id).ToList())
                context.Destroyed = true;
        }
        public void ClearContexts(SessionId sessionId)
        {
            if (!contexts.TryGetValue(sessionId, out ConcurrentBag<ExecutionContext> contextBag))
                return;
            foreach (ExecutionContext context in contextBag)
                context.Destroyed = true;
        }
        public bool ContainsKey(SessionId sessionId) => contexts.ContainsKey(sessionId);
    }
}
