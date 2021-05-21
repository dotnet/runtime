// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class MemberReferenceResolver
    {
        private SessionId sessionId;
        private int scopeId;
        private MonoProxy proxy;
        private ExecutionContext ctx;
        private PerScopeCache scopeCache;
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
        public async Task<JObject> GetValueFromObject(JToken objRet, CancellationToken token)
        {
            if (objRet["value"]?.Value<JObject>() != null)
                return objRet["value"]?.Value<JObject>();
            if (objRet["get"]?.Value<JObject>() != null)
            {
                if (DotnetObjectId.TryParse(objRet?["get"]?["objectIdValue"]?.Value<string>(), out DotnetObjectId objectId))
                {
                    var command_params = new MemoryStream();
                    var command_params_writer = new MonoBinaryWriter(command_params);
                    command_params_writer.WriteObj(objectId);
                    var ret = await proxy.sdbHelper.InvokeMethod(sessionId, command_params.ToArray(), objRet["get"]["methodId"].Value<int>(), objRet["name"].Value<string>(), token);
                    return ret["value"]?.Value<JObject>();
                }

            }
            return null;
        }
        // Checks Locals, followed by `this`
        public async Task<JObject> Resolve(string var_name, CancellationToken token)
        {
            string[] parts = var_name.Split(".");
            JObject rootObject = null;
            foreach (string part in parts)
            {
                if (rootObject != null)
                {
                    if (DotnetObjectId.TryParse(rootObject?["objectId"]?.Value<string>(), out DotnetObjectId objectId))
                    {
                        var root_res = await proxy.RuntimeGetProperties(sessionId, objectId, null, token);
                        var root_res_obj = root_res.Value?["result"];
                        var objRet = root_res_obj.FirstOrDefault(objPropAttr => objPropAttr["name"].Value<string>() == part);
                        if (objRet != null)
                        {
                            rootObject = await GetValueFromObject(objRet, token);
                        }
                    }
                    continue;
                }
                if (scopeCache.Locals.Count == 0 && !locals_fetched)
                {
                    Result scope_res = await proxy.GetScopeProperties(sessionId, scopeId, token);
                    if (scope_res.IsErr)
                        throw new Exception($"BUG: Unable to get properties for scope: {scopeId}. {scope_res}");
                    locals_fetched = true;
                }
                if (scopeCache.Locals.TryGetValue(part, out JObject obj))
                {
                    rootObject = obj["value"]?.Value<JObject>();
                }
                if (scopeCache.Locals.TryGetValue("this", out JObject objThis))
                {
                    if (DotnetObjectId.TryParse(objThis?["value"]?["objectId"]?.Value<string>(), out DotnetObjectId objectId))
                    {
                        var this_res = await proxy.sdbHelper.GetObjectValues(sessionId, int.Parse(objectId.Value), true, false, token);
                        var objRet = this_res.FirstOrDefault(objPropAttr => objPropAttr["name"].Value<string>() == part);
                        if (objRet != null)
                        {
                            rootObject = await GetValueFromObject(objRet, token);
                            //rootObject = objRet["value"]?.Value<JObject>();
                        }
                    }
                }
                /*
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
                */
            }
            return rootObject;
        }

    }
}
