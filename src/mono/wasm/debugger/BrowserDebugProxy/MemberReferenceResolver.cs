// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class MemberReferenceResolver
    {
        private SessionId sessionId;
        private int scopeId;
        private MonoProxy proxy;
        private ExecutionContext ctx;
        private PerScopeCache scopeCache;
        private VarInfo[] varIds;
        private ILogger logger;
        private bool locals_fetched;

        public MemberReferenceResolver(MonoProxy proxy, ExecutionContext ctx, SessionId session_id, int scope_id, ILogger logger)
        {
            sessionId = session_id;
            scopeId = scope_id;
            this.proxy = proxy;
            this.ctx = ctx;
            this.logger = logger;
            scopeCache = ctx.GetCacheForScope(scope_id);
        }

        // Checks Locals, followed by `this`
        public async Task<JObject> Resolve(string var_name, CancellationToken token)
        {
            if (scopeCache.Locals.Count == 0 && !locals_fetched)
            {
                Result scope_res = await proxy.GetScopeProperties(sessionId, scopeId, token);
                if (scope_res.IsErr)
                    throw new Exception($"BUG: Unable to get properties for scope: {scopeId}. {scope_res}");
                locals_fetched = true;
            }

            if (scopeCache.Locals.TryGetValue(var_name, out JObject obj))
            {
                return obj["value"]?.Value<JObject>();
            }

            if (scopeCache.MemberReferences.TryGetValue(var_name, out JObject ret))
                return ret;

            if (varIds == null)
            {
                Frame scope = ctx.CallStack.FirstOrDefault(s => s.Id == scopeId);
                varIds = scope.Method.GetLiveVarsAt(scope.Location.CliLocation.Offset);
            }

            Result res = await proxy.SendMonoCommand(sessionId, MonoCommands.EvaluateMemberAccess(scopeId, var_name, varIds), token);
            if (res.IsOk)
            {
                ret = res.Value?["result"]?["value"]?["value"]?.Value<JObject>();
                scopeCache.MemberReferences[var_name] = ret;
            }
            else
            {
                logger.LogDebug(res.Error.ToString());
            }

            return ret;
        }

    }
}
