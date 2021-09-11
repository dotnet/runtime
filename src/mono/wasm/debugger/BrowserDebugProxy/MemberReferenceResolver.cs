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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

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
        private bool localsFetched;
        private int linqTypeId;
        private MonoSDBHelper sdbHelper;

        public MemberReferenceResolver(MonoProxy proxy, ExecutionContext ctx, SessionId sessionId, int scopeId, ILogger logger)
        {
            this.sessionId = sessionId;
            this.scopeId = scopeId;
            this.proxy = proxy;
            this.ctx = ctx;
            this.logger = logger;
            scopeCache = ctx.GetCacheForScope(scopeId);
            sdbHelper = proxy.SdbHelper;
            linqTypeId = -1;
        }

        public MemberReferenceResolver(MonoProxy proxy, ExecutionContext ctx, SessionId sessionId, JArray objectValues, ILogger logger)
        {
            this.sessionId = sessionId;
            scopeId = -1;
            this.proxy = proxy;
            this.ctx = ctx;
            this.logger = logger;
            scopeCache = new PerScopeCache(objectValues);
            localsFetched = true;
            sdbHelper = proxy.SdbHelper;
            linqTypeId = -1;
        }

        public async Task<JObject> GetValueFromObject(JToken objRet, CancellationToken token)
        {
            if (objRet["value"]?["className"]?.Value<string>() == "System.Exception")
            {
                if (DotnetObjectId.TryParse(objRet?["value"]?["objectId"]?.Value<string>(), out DotnetObjectId objectId))
                {
                    var exceptionObject = await sdbHelper.GetObjectValues(sessionId, int.Parse(objectId.Value), GetObjectCommandOptions.WithProperties | GetObjectCommandOptions.OwnProperties, token);
                    var exceptionObjectMessage = exceptionObject.FirstOrDefault(attr => attr["name"].Value<string>().Equals("_message"));
                    exceptionObjectMessage["value"]["value"] = objRet["value"]?["className"]?.Value<string>() + ": " + exceptionObjectMessage["value"]?["value"]?.Value<string>();
                    return exceptionObjectMessage["value"]?.Value<JObject>();
                }
                return objRet["value"]?.Value<JObject>();
            }

            if (objRet["value"]?.Value<JObject>() != null)
                return objRet["value"]?.Value<JObject>();
            if (objRet["get"]?.Value<JObject>() != null)
            {
                if (DotnetObjectId.TryParse(objRet?["get"]?["objectIdValue"]?.Value<string>(), out DotnetObjectId objectId))
                {
                    var commandParams = new MemoryStream();
                    var commandParamsWriter = new MonoBinaryWriter(commandParams);
                    commandParamsWriter.WriteObj(objectId, sdbHelper);
                    var ret = await sdbHelper.InvokeMethod(sessionId, commandParams.ToArray(), objRet["get"]["methodId"].Value<int>(), objRet["name"].Value<string>(), token);
                    return await GetValueFromObject(ret, token);
                }

            }
            return null;
        }

        public async Task<JObject> TryToRunOnLoadedClasses(string varName, CancellationToken token)
        {
            string classNameToFind = "";
            string[] parts = varName.Split(".");
            var typeId = -1;
            foreach (string part in parts)
            {
                if (classNameToFind.Length > 0)
                    classNameToFind += ".";
                classNameToFind += part.Trim();
                if (typeId != -1)
                {
                    var fields = await sdbHelper.GetTypeFields(sessionId, typeId, token);
                    foreach (var field in fields)
                    {
                        if (field.Name == part.Trim())
                        {
                            var isInitialized = await sdbHelper.TypeIsInitialized(sessionId, typeId, token);
                            if (isInitialized == 0)
                            {
                                isInitialized = await sdbHelper.TypeInitialize(sessionId, typeId, token);
                            }
                            var valueRet = await sdbHelper.GetFieldValue(sessionId, typeId, field.Id, token);
                            return await GetValueFromObject(valueRet, token);
                        }
                    }
                    var methodId = await sdbHelper.GetPropertyMethodIdByName(sessionId, typeId, part.Trim(), token);
                    if (methodId != -1)
                    {
                        var commandParamsObj = new MemoryStream();
                        var commandParamsObjWriter = new MonoBinaryWriter(commandParamsObj);
                        commandParamsObjWriter.Write(0); //param count
                        var retMethod = await sdbHelper.InvokeMethod(sessionId, commandParamsObj.ToArray(), methodId, "methodRet", token);
                        return await GetValueFromObject(retMethod, token);
                    }
                }
                var store = await proxy.LoadStore(sessionId, token);
                foreach (var asm in store.assemblies)
                {
                    var type = asm.GetTypeByName(classNameToFind);
                    if (type != null)
                    {
                        typeId = await sdbHelper.GetTypeIdFromToken(sessionId, asm.DebugId, type.Token, token);
                    }
                }
            }
            return null;
        }

        // Checks Locals, followed by `this`
        public async Task<JObject> Resolve(string varName, CancellationToken token)
        {
            //has method calls
            if (varName.Contains('('))
                return null;

            string[] parts = varName.Split(".");
            JObject rootObject = null;

            if (scopeCache.MemberReferences.TryGetValue(varName, out JObject ret)) {
                return ret;
            }

            if (scopeCache.ObjectFields.TryGetValue(varName, out JObject valueRet)) {
                return await GetValueFromObject(valueRet, token);
            }

            foreach (string part in parts)
            {
                string partTrimmed = part.Trim();
                if (partTrimmed == "")
                    return null;
                if (rootObject != null)
                {
                    if (rootObject?["subtype"]?.Value<string>() == "null")
                        return null;
                    if (DotnetObjectId.TryParse(rootObject?["objectId"]?.Value<string>(), out DotnetObjectId objectId))
                    {
                        var rootResObj = await proxy.RuntimeGetPropertiesInternal(sessionId, objectId, null, token);
                        var objRet = rootResObj.FirstOrDefault(objPropAttr => objPropAttr["name"].Value<string>() == partTrimmed);
                        if (objRet == null)
                            return null;

                        rootObject = await GetValueFromObject(objRet, token);
                    }
                    continue;
                }
                if (scopeCache.Locals.Count == 0 && !localsFetched)
                {
                    Result scope_res = await proxy.GetScopeProperties(sessionId, scopeId, token);
                    if (scope_res.IsErr)
                        throw new Exception($"BUG: Unable to get properties for scope: {scopeId}. {scope_res}");
                    localsFetched = true;
                }
                if (scopeCache.Locals.TryGetValue(partTrimmed, out JObject obj))
                {
                    rootObject = obj["value"]?.Value<JObject>();
                }
                else if (scopeCache.Locals.TryGetValue("this", out JObject objThis))
                {
                    if (partTrimmed == "this")
                    {
                        rootObject = objThis?["value"].Value<JObject>();
                    }
                    else if (DotnetObjectId.TryParse(objThis?["value"]?["objectId"]?.Value<string>(), out DotnetObjectId objectId))
                    {
                        var rootResObj = await proxy.RuntimeGetPropertiesInternal(sessionId, objectId, null, token);
                        var objRet = rootResObj.FirstOrDefault(objPropAttr => objPropAttr["name"].Value<string>() == partTrimmed);
                        if (objRet != null)
                        {
                            rootObject = await GetValueFromObject(objRet, token);
                        }
                        else
                        {
                            rootObject = await TryToRunOnLoadedClasses(varName, token);
                            return rootObject;
                        }
                    }
                }
            }
            scopeCache.MemberReferences[varName] = rootObject;
            return rootObject;
        }

        public async Task<JObject> Resolve(InvocationExpressionSyntax method, Dictionary<string, JObject> memberAccessValues, CancellationToken token)
        {
            var methodName = "";
            int isTryingLinq = 0;
            try
            {
                JObject rootObject = null;
                var expr = method.Expression;
                if (expr is MemberAccessExpressionSyntax)
                {
                    var memberAccessExpressionSyntax = expr as MemberAccessExpressionSyntax;
                    rootObject = await Resolve(memberAccessExpressionSyntax.Expression.ToString(), token);
                    methodName = memberAccessExpressionSyntax.Name.ToString();
                }
                else if (expr is IdentifierNameSyntax)
                    if (scopeCache.ObjectFields.TryGetValue("this", out JObject valueRet)) {
                        rootObject = await GetValueFromObject(valueRet, token);
                    methodName = expr.ToString();
                }

                if (rootObject != null)
                {
                    DotnetObjectId.TryParse(rootObject?["objectId"]?.Value<string>(), out DotnetObjectId objectId);
                    var typeIds = await sdbHelper.GetTypeIdFromObject(sessionId, int.Parse(objectId.Value), true, token);
                    int methodId = await sdbHelper.GetMethodIdByName(sessionId, typeIds[0], methodName, token);
                    var className = await sdbHelper.GetTypeNameOriginal(sessionId, typeIds[0], token);
                    if (methodId == 0) //try to search on System.Linq.Enumerable
                    {
                        if (linqTypeId == -1)
                            linqTypeId = await sdbHelper.GetTypeByName(sessionId, "System.Linq.Enumerable", token);
                        methodId = await sdbHelper.GetMethodIdByName(sessionId, linqTypeId, methodName, token);
                        if (methodId != 0)
                        {
                            foreach (var typeId in typeIds)
                            {
                                var genericTypeArgs = await sdbHelper.GetTypeParamsOrArgsForGenericType(sessionId, typeId, token);
                                if (genericTypeArgs.Count > 0)
                                {
                                    isTryingLinq = 1;
                                    methodId = await sdbHelper.MakeGenericMethod(sessionId, methodId, genericTypeArgs, token);
                                    break;
                                }
                            }
                        }
                    }
                    if (methodId == 0) {
                        var typeName = await sdbHelper.GetTypeName(sessionId, typeIds[0], token);
                        throw new Exception($"Method '{methodName}' not found in type '{typeName}'");
                    }
                    var commandParamsObj = new MemoryStream();
                    var commandParamsObjWriter = new MonoBinaryWriter(commandParamsObj);
                    if (isTryingLinq == 0)
                        commandParamsObjWriter.WriteObj(objectId, sdbHelper);
                    if (method.ArgumentList != null)
                    {
                        commandParamsObjWriter.Write((int)method.ArgumentList.Arguments.Count + isTryingLinq);
                        if (isTryingLinq == 1)
                            commandParamsObjWriter.WriteObj(objectId, sdbHelper);
                        foreach (var arg in method.ArgumentList.Arguments)
                        {
                            if (arg.Expression is LiteralExpressionSyntax)
                            {
                                if (!await commandParamsObjWriter.WriteConst(sessionId, arg.Expression as LiteralExpressionSyntax, sdbHelper, token))
                                    return null;
                            }
                            if (arg.Expression is IdentifierNameSyntax)
                            {
                                var argParm = arg.Expression as IdentifierNameSyntax;
                                if (!await commandParamsObjWriter.WriteJsonValue(sessionId, memberAccessValues[argParm.Identifier.Text], sdbHelper, token))
                                    return null;
                            }
                        }
                        var retMethod = await sdbHelper.InvokeMethod(sessionId, commandParamsObj.ToArray(), methodId, "methodRet", token);
                        return await GetValueFromObject(retMethod, token);
                    }
                }
                return null;
            }
            catch (Exception)
            {
                throw new Exception($"Unable to evaluate method '{methodName}'");
            }
        }
    }
}
