// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    public struct SessionId
    {
        public readonly string sessionId;

        public SessionId(string sessionId)
        {
            this.sessionId = sessionId;
        }

        // hashset treats 0 as unset
        public override int GetHashCode() => sessionId?.GetHashCode() ?? -1;

        public override bool Equals(object obj) => (obj is SessionId) ? ((SessionId)obj).sessionId == sessionId : false;

        public static bool operator ==(SessionId a, SessionId b) => a.sessionId == b.sessionId;

        public static bool operator !=(SessionId a, SessionId b) => a.sessionId != b.sessionId;

        public static SessionId Null { get; }

        public override string ToString() => $"session-{sessionId}";
    }

    public struct MessageId
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

        public override int GetHashCode() => (sessionId?.GetHashCode() ?? 0) ^ id.GetHashCode();

        public override bool Equals(object obj) => (obj is MessageId) ? ((MessageId)obj).sessionId == sessionId && ((MessageId)obj).id == id : false;
    }

    internal class DotnetObjectId
    {
        public string Scheme { get; }
        public string Value { get; }

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

        public DotnetObjectId(string scheme, string value)
        {
            Scheme = scheme;
            Value = value;
        }

        public override string ToString() => $"dotnet:{Scheme}:{Value}";
    }

    public struct Result
    {
        public JObject Value { get; private set; }
        public JObject Error { get; private set; }

        public bool IsOk => Value != null;
        public bool IsErr => Error != null;

        private Result(JObject result, JObject error)
        {
            if (result != null && error != null)
                throw new ArgumentException($"Both {nameof(result)} and {nameof(error)} arguments cannot be non-null.");

            bool resultHasError = string.Equals((result?["result"] as JObject)?["subtype"]?.Value<string>(), "error");
            if (result != null && resultHasError)
            {
                this.Value = null;
                this.Error = result;
            }
            else
            {
                this.Value = result;
                this.Error = error;
            }
        }

        public static Result FromJson(JObject obj)
        {
            //Log ("protocol", $"from result: {obj}");
            return new Result(obj["result"] as JObject, obj["error"] as JObject);
        }

        public static Result Ok(JObject ok) => new Result(ok, null);

        public static Result OkFromObject(object ok) => Ok(JObject.FromObject(ok));

        public static Result Err(JObject err) => new Result(null, err);

        public static Result Err(string msg) => new Result(null, JObject.FromObject(new { message = msg }));

        public static Result Exception(Exception e) => new Result(null, JObject.FromObject(new { message = e.Message }));

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
            return $"[Result: IsOk: {IsOk}, IsErr: {IsErr}, Value: {Value?.ToString()}, Error: {Error?.ToString()} ]";
        }
    }

    internal class MonoCommands
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
    }

    internal enum MonoErrorCodes
    {
        BpNotFound = 100000,
    }

    internal static class MonoConstants
    {
        public const string RUNTIME_IS_READY = "mono_wasm_runtime_ready";
        public const string EVENT_RAISED = "mono_wasm_debug_event_raised:aef14bca-5519-4dfe-b35a-f867abc123ae";
    }

    internal class Frame
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

    internal class Breakpoint
    {
        public SourceLocation Location { get; private set; }
        public int RemoteId { get; set; }
        public BreakpointState State { get; set; }
        public string StackId { get; private set; }
        public string Condition { get; private set; }
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
        public ExecutionContext(MonoSDBHelper sdbAgent, int id, object auxData)
        {
            Id = id;
            AuxData = auxData;
            SdbAgent = sdbAgent;
        }

        public string DebugId { get; set; }
        public Dictionary<string, BreakpointRequest> BreakpointRequests { get; } = new Dictionary<string, BreakpointRequest>();

        public TaskCompletionSource<DebugStore> ready;
        public bool IsRuntimeReady => ready != null && ready.Task.IsCompleted;
        public bool IsSkippingHiddenMethod { get; set; }
        public int ThreadId { get; set; }
        public int Id { get; set; }
        public object AuxData { get; set; }

        public PauseOnExceptionsKind PauseOnExceptions { get; set; }

        public List<Frame> CallStack { get; set; }

        public string[] LoadedFiles { get; set; }
        internal DebugStore store;
        internal MonoSDBHelper SdbAgent { get; init; }
        public TaskCompletionSource<DebugStore> Source { get; } = new TaskCompletionSource<DebugStore>();

        private Dictionary<int, PerScopeCache> perScopeCaches { get; } = new Dictionary<int, PerScopeCache>();

        public DebugStore Store
        {
            get
            {
                if (store == null || !Source.Task.IsCompleted)
                    return null;

                return store;
            }
        }

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

    internal class PerScopeCache
    {
        public Dictionary<string, JObject> Locals { get; } = new Dictionary<string, JObject>();
        public Dictionary<string, JObject> MemberReferences { get; } = new Dictionary<string, JObject>();
        public Dictionary<string, JObject> ObjectFields { get; } = new Dictionary<string, JObject>();
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
}
