// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Net.WebSockets;
using BrowserDebugProxy;
using System.Globalization;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal sealed class MemberReferenceResolver
    {
        private static int evaluationResultObjectId;
        private SessionId sessionId;
        private int scopeId;
        private MonoProxy proxy;
        private ExecutionContext context;
        private PerScopeCache scopeCache;
        private ILogger logger;
        private bool localsFetched;
        private int linqTypeId;

        public MemberReferenceResolver(MonoProxy proxy, ExecutionContext ctx, SessionId sessionId, int scopeId, ILogger logger)
        {
            this.sessionId = sessionId;
            this.scopeId = scopeId;
            this.proxy = proxy;
            this.context = ctx;
            this.logger = logger;
            scopeCache = ctx.GetCacheForScope(scopeId);
            linqTypeId = -1;
        }

        public MemberReferenceResolver(MonoProxy proxy, ExecutionContext ctx, SessionId sessionId, JArray objectValues, ILogger logger)
        {
            this.sessionId = sessionId;
            scopeId = -1;
            this.proxy = proxy;
            this.context = ctx;
            this.logger = logger;
            scopeCache = new PerScopeCache(objectValues);
            localsFetched = true;
            linqTypeId = -1;
        }

        public async Task<JObject> GetValueFromObject(JToken objRet, CancellationToken token)
        {
            if (objRet["value"]?["className"]?.Value<string>() == "System.Exception")
            {
                if (DotnetObjectId.TryParse(objRet?["value"]?["objectId"]?.Value<string>(), out DotnetObjectId objectId))
                {
                    GetMembersResult exceptionObject = await MemberObjectsExplorer.GetTypeMemberValues(context.SdbAgent, objectId, GetObjectCommandOptions.WithProperties | GetObjectCommandOptions.OwnProperties, token);
                    var exceptionObjectMessage = exceptionObject.FirstOrDefault(attr => attr["name"].Value<string>().Equals("_message"));
                    exceptionObjectMessage["value"]["value"] = objRet["value"]?["className"]?.Value<string>() + ": " + exceptionObjectMessage["value"]?["value"]?.Value<string>();
                    return exceptionObjectMessage["value"]?.Value<JObject>();
                }
                return objRet["value"]?.Value<JObject>();
            }

            if (objRet["value"]?.Value<JObject>() != null)
                return objRet["value"]?.Value<JObject>();

            if (objRet["get"]?.Value<JObject>() != null &&
                DotnetObjectId.TryParse(objRet?["get"]?["objectId"]?.Value<string>(), out DotnetObjectId getterObjectId))
            {
                var ret = await context.SdbAgent.InvokeMethod(getterObjectId, token);
                return await GetValueFromObject(ret, token);
            }
            return null;
        }

        public async Task<(JObject containerObject, ArraySegment<string> remaining)> ResolveStaticMembersInStaticTypes(ArraySegment<string> parts, CancellationToken token)
        {
            string classNameToFind = "";
            var store = await proxy.LoadStore(sessionId, token);
            var methodInfo = context.CallStack.FirstOrDefault(s => s.Id == scopeId)?.Method?.Info;

            if (methodInfo == null)
                return (null, null);

            int typeId = -1;
            for (int i = 0; i < parts.Count; i++)
            {
                string part = parts[i];

                if (typeId != -1)
                {
                    JObject memberObject = await FindStaticMemberInType(classNameToFind, part, typeId);
                    if (memberObject != null)
                    {
                        ArraySegment<string> remaining = null;
                        if (i < parts.Count - 1)
                            remaining = parts[i..];

                        return (memberObject, remaining);
                    }

                    // Didn't find a member named `part` in `typeId`.
                    // Could be a nested type. Let's continue the search
                    // with `part` added to the type name

                    typeId = -1;
                }

                if (classNameToFind.Length > 0)
                    classNameToFind += ".";
                classNameToFind += part;

                if (!string.IsNullOrEmpty(methodInfo?.TypeInfo?.Namespace))
                {
                    typeId = await FindStaticTypeId(methodInfo?.TypeInfo?.Namespace + "." + classNameToFind);
                    if (typeId != -1)
                        continue;
                }
                typeId = await FindStaticTypeId(classNameToFind);
            }

            return (null, null);

            async Task<JObject> FindStaticMemberInType(string classNameToFind, string name, int typeId)
            {
                var fields = await context.SdbAgent.GetTypeFields(typeId, token);
                foreach (var field in fields)
                {
                    if (field.Name != name)
                        continue;

                    var isInitialized = await context.SdbAgent.TypeIsInitialized(typeId, token);
                    if (isInitialized == 0)
                    {
                        isInitialized = await context.SdbAgent.TypeInitialize(typeId, token);
                    }
                    var staticFieldValue = await context.SdbAgent.GetFieldValue(typeId, field.Id, token);
                    var valueRet = await GetValueFromObject(staticFieldValue, token);
                    // we need the full name here
                    valueRet["className"] = classNameToFind;
                    return valueRet;
                }

                var methodId = await context.SdbAgent.GetPropertyMethodIdByName(typeId, name, token);
                if (methodId != -1)
                {
                    using var commandParamsObjWriter = new MonoBinaryWriter();
                    commandParamsObjWriter.Write(0); //param count
                    var retMethod = await context.SdbAgent.InvokeMethod(commandParamsObjWriter.GetParameterBuffer(), methodId, token);
                    return await GetValueFromObject(retMethod, token);
                }
                return null;
            }

            async Task<int> FindStaticTypeId(string typeName)
            {
                foreach (var asm in store.assemblies)
                {
                    var type = asm.GetTypeByName(typeName);
                    if (type == null)
                        continue;

                    int id = await context.SdbAgent.GetTypeIdFromToken(await asm.GetDebugId(context.SdbAgent, token), type.Token, token);
                    if (id != -1)
                        return id;
                }

                return -1;
            }
        }

        // Checks Locals, followed by `this`
        public async Task<JObject> Resolve(string varName, CancellationToken token)
        {
            // question mark at the end of expression is invalid
            if (varName[^1] == '?')
                throw new ReturnAsErrorException($"Expected expression.", "ReferenceError");

            //has method calls
            if (varName.Contains('('))
                return null;

            if (scopeCache.MemberReferences.TryGetValue(varName, out JObject ret))
                return ret;

            if (scopeCache.ObjectFields.TryGetValue(varName, out JObject valueRet))
                return await GetValueFromObject(valueRet, token);

            string[] parts = varName.Split(".", StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
                throw new ReturnAsErrorException($"Failed to resolve expression: {varName}", "ReferenceError");

            JObject retObject = await ResolveAsLocalOrThisMember(parts[0]);
            bool throwOnNullReference = parts[0][^1] != '?';
            if (retObject != null && parts.Length > 1)
                retObject = await ResolveAsInstanceMember(parts, retObject, throwOnNullReference);

            if (retObject == null)
            {
                (retObject, ArraySegment<string> remaining) = await ResolveStaticMembersInStaticTypes(parts, token);
                if (remaining != null && remaining.Count != 0)
                {
                    if (retObject.IsNullValuedObject())
                    {
                        // NRE on null.$remaining
                        retObject = null;
                    }
                    else
                    {
                        retObject = await ResolveAsInstanceMember(remaining, retObject, throwOnNullReference);
                    }
                }
            }

            scopeCache.MemberReferences[varName] = retObject;
            return retObject;

            async Task<JObject> ResolveAsLocalOrThisMember(string name)
            {
                if (scopeCache.Locals.Count == 0 && !localsFetched)
                {
                    Result scope_res = await proxy.GetScopeProperties(sessionId, scopeId, token);
                    if (!scope_res.IsOk)
                        throw new ExpressionEvaluationFailedException($"BUG: Unable to get properties for scope: {scopeId}. {scope_res}");
                    localsFetched = true;
                }

                // remove null-condition, otherwise TryGet by name fails
                if (name[^1] == '?' || name[^1] == '!')
                    name = name.Remove(name.Length - 1);

                if (scopeCache.Locals.TryGetValue(name, out JObject obj))
                    return obj["value"]?.Value<JObject>();

                if (!scopeCache.Locals.TryGetValue("this", out JObject objThis))
                    return null;

                if (!DotnetObjectId.TryParse(objThis?["value"]?["objectId"]?.Value<string>(), out DotnetObjectId objectId))
                    return null;

                ValueOrError<GetMembersResult> valueOrError = await proxy.RuntimeGetObjectMembers(sessionId, objectId, null, token);
                if (valueOrError.IsError)
                {
                    logger.LogDebug($"ResolveAsLocalOrThisMember failed with : {valueOrError.Error}");
                    return null;
                }

                JToken objRet = valueOrError.Value.FirstOrDefault(objPropAttr => objPropAttr["name"].Value<string>() == name);
                if (objRet != null)
                    return await GetValueFromObject(objRet, token);

                return null;
            }

            async Task<JObject> ResolveAsInstanceMember(ArraySegment<string> parts, JObject baseObject, bool throwOnNullReference)
            {
                JObject resolvedObject = baseObject;
                // parts[0] - name of baseObject
                for (int i = 1; i < parts.Count; i++)
                {
                    string part = parts[i];
                    if (part.Length == 0)
                        return null;

                    bool hasCurrentPartNullCondition = part[^1] == '?';

                    // current value of resolvedObject is on parts[i - 1]
                    if (resolvedObject.IsNullValuedObject())
                    {
                        // trying null.$member
                        if (throwOnNullReference)
                            throw new ReturnAsErrorException($"Expression threw NullReferenceException trying to access \"{part}\" on a null-valued object.", "ReferenceError");

                        if (i == parts.Count - 1)
                        {
                            // this is not ideal, it returns the last object
                            // that had objectId and was null-valued,
                            // so the class/description of object are not of the last part
                            return resolvedObject;
                        }

                        // check if null condition is correctly applied: should we throw or return null-object
                        throwOnNullReference = !hasCurrentPartNullCondition;
                        continue;
                    }

                    if (!DotnetObjectId.TryParse(resolvedObject?["objectId"]?.Value<string>(), out DotnetObjectId objectId))
                    {
                        if (resolvedObject["type"].Value<string>() == "string")
                            throw new ReturnAsErrorException($"String properties evaluation is not supported yet.", "ReferenceError"); // Issue #66823
                        if (!throwOnNullReference)
                            throw new ReturnAsErrorException($"Operation '?' not allowed on primitive type - '{parts[i - 1]}'", "ReferenceError");
                        throw new ReturnAsErrorException($"Cannot find member '{part}' on a primitive type", "ReferenceError");
                    }

                    var args = JObject.FromObject(new { forDebuggerDisplayAttribute = true });
                    ValueOrError<GetMembersResult> valueOrError = await proxy.RuntimeGetObjectMembers(sessionId, objectId, args, token);
                    if (valueOrError.IsError)
                    {
                        logger.LogDebug($"ResolveAsInstanceMember failed with : {valueOrError.Error}");
                        return null;
                    }

                    if (part[^1] == '!' || part[^1] == '?')
                        part = part.Remove(part.Length - 1);

                    JToken objRet = valueOrError.Value.FirstOrDefault(objPropAttr => objPropAttr["name"]?.Value<string>() == part);
                    if (objRet == null)
                        return null;

                    resolvedObject = await GetValueFromObject(objRet, token);
                    if (resolvedObject == null)
                        return null;
                    throwOnNullReference = !hasCurrentPartNullCondition;
                }
                return resolvedObject;
            }
        }

        public async Task<JObject> Resolve(ElementAccessExpressionSyntax elementAccess, Dictionary<string, JObject> memberAccessValues, JObject indexObject, CancellationToken token)
        {
            try
            {
                JObject rootObject = null;
                string elementAccessStrExpression = elementAccess.Expression.ToString();
                rootObject = await Resolve(elementAccessStrExpression, token);
                if (rootObject == null)
                {
                    rootObject = indexObject;
                    indexObject = null;
                }
                if (rootObject != null)
                {
                    string elementIdxStr;
                    int elementIdx = 0;
                    // x[1] or x[a] or x[a.b]
                    if (indexObject == null)
                    {
                        if (elementAccess.ArgumentList != null)
                        {
                            foreach (var arg in elementAccess.ArgumentList.Arguments)
                            {
                                // e.g. x[1]
                                if (arg.Expression is LiteralExpressionSyntax)
                                {
                                    var argParm = arg.Expression as LiteralExpressionSyntax;
                                    elementIdxStr = argParm.ToString();
                                    int.TryParse(elementIdxStr, out elementIdx);
                                }

                                // e.g. x[a] or x[a.b]
                                if (arg.Expression is IdentifierNameSyntax)
                                {
                                    var argParm = arg.Expression as IdentifierNameSyntax;

                                    // x[a.b]
                                    memberAccessValues.TryGetValue(argParm.Identifier.Text, out indexObject);

                                    // x[a]
                                    if (indexObject == null)
                                    {
                                        indexObject = await Resolve(argParm.Identifier.Text, token);
                                    }
                                    elementIdxStr = indexObject["value"].ToString();
                                    int.TryParse(elementIdxStr, out elementIdx);
                                }
                            }
                        }
                    }
                    // e.g. x[a[0]], x[a[b[1]]] etc.
                    else
                    {
                        elementIdxStr = indexObject["value"].ToString();
                        int.TryParse(elementIdxStr, out elementIdx);
                    }
                    if (elementIdx >= 0)
                    {
                        DotnetObjectId.TryParse(rootObject?["objectId"]?.Value<string>(), out DotnetObjectId objectId);
                        switch (objectId.Scheme)
                        {
                            case "array":
                                rootObject["value"] = await context.SdbAgent.GetArrayValues(objectId.Value, token);
                                return (JObject)rootObject["value"][elementIdx]["value"];
                            case "object":
                                var typeIds = await context.SdbAgent.GetTypeIdsForObject(objectId.Value, true, token);
                                int methodId = await context.SdbAgent.GetMethodIdByName(typeIds[0], "ToArray", token);
                                var toArrayRetMethod = await context.SdbAgent.InvokeMethod(objectId.Value, methodId, isValueType: false, token);
                                rootObject = await GetValueFromObject(toArrayRetMethod, token);
                                DotnetObjectId.TryParse(rootObject?["objectId"]?.Value<string>(), out DotnetObjectId arrayObjectId);
                                rootObject["value"] = await context.SdbAgent.GetArrayValues(arrayObjectId.Value, token);
                                return (JObject)rootObject["value"][elementIdx]["value"];
                            default:
                                throw new InvalidOperationException($"Cannot apply indexing with [] to an expression of type '{objectId.Scheme}'");
                        }
                    }
                }
                return null;
            }
            catch (Exception ex) when (ex is not ExpressionEvaluationFailedException)
            {
                throw new ExpressionEvaluationFailedException($"Unable to evaluate element access '{elementAccess}': {ex.Message}", ex);
            }
        }

        public async Task<(JObject, string)> ResolveInvokationInfo(InvocationExpressionSyntax method, CancellationToken token)
        {
            var methodName = "";
            try
            {
                JObject rootObject = null;
                var expr = method.Expression;
                if (expr is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                {
                    rootObject = await Resolve(memberAccessExpressionSyntax.Expression.ToString(), token);
                    methodName = memberAccessExpressionSyntax.Name.ToString();

                    if (rootObject.IsNullValuedObject())
                        throw new ExpressionEvaluationFailedException($"Expression '{memberAccessExpressionSyntax}' evaluated to null");
                }
                else if (expr is IdentifierNameSyntax && scopeCache.ObjectFields.TryGetValue("this", out JObject thisValue))
                {
                    rootObject = await GetValueFromObject(thisValue, token);
                    methodName = expr.ToString();
                }
                return (rootObject, methodName);
            }
            catch (Exception ex) when (ex is not (ExpressionEvaluationFailedException or ReturnAsErrorException))
            {
                throw new Exception($"Unable to evaluate method '{methodName}'", ex);
            }
        }

        private static readonly string[] primitiveTypes = new string[] { "string", "number", "boolean", "symbol" };

        public async Task<JObject> Resolve(InvocationExpressionSyntax method, Dictionary<string, JObject> memberAccessValues, CancellationToken token)
        {
            (JObject rootObject, string methodName) = await ResolveInvokationInfo(method, token);
            if (rootObject == null)
                throw new ReturnAsErrorException($"Failed to resolve root object for {method}", "ReferenceError");

            // primitives don't have objectId
            if (!DotnetObjectId.TryParse(rootObject["objectId"]?.Value<string>(), out DotnetObjectId objectId) &&
                 primitiveTypes.Contains(rootObject["type"]?.Value<string>()))
                return null;

            if (method.ArgumentList == null)
                throw new InternalErrorException($"Failed to resolve method call for {method}, list of arguments is null.");

            bool isExtensionMethod = false;
            try
            {
                List<int> typeIds;
                if (objectId.IsValueType)
                {
                    if (!context.SdbAgent.ValueCreator.TryGetValueTypeById(objectId.Value, out ValueTypeClass valueType))
                        throw new Exception($"Could not find valuetype {objectId}");

                    typeIds = new List<int>(1) { valueType.TypeId };
                }
                else
                {
                    typeIds = await context.SdbAgent.GetTypeIdsForObject(objectId.Value, true, token);
                }
                int methodId = await context.SdbAgent.GetMethodIdByName(typeIds[0], methodName, token);
                var className = await context.SdbAgent.GetTypeNameOriginal(typeIds[0], token);
                if (methodId == 0) //try to search on System.Linq.Enumerable
                    methodId = await FindMethodIdOnLinqEnumerable(typeIds, methodName);

                if (methodId == 0)
                {
                    var typeName = await context.SdbAgent.GetTypeName(typeIds[0], token);
                    throw new ReturnAsErrorException($"Method '{methodName}' not found in type '{typeName}'", "ReferenceError");
                }
                using var commandParamsObjWriter = new MonoBinaryWriter();
                if (!isExtensionMethod)
                {
                    // instance method
                    commandParamsObjWriter.WriteObj(objectId, context.SdbAgent);
                }

                int passedArgsCnt = method.ArgumentList.Arguments.Count;
                int methodParamsCnt = passedArgsCnt;
                ParameterInfo[] methodParamsInfo = null;
                var methodInfo = await context.SdbAgent.GetMethodInfo(methodId, token);
                if (methodInfo != null)
                {
                    methodParamsInfo = methodInfo.Info.GetParametersInfo();
                    methodParamsCnt = methodParamsInfo.Length;
                    if (isExtensionMethod)
                    {
                        // implicit *this* parameter
                        methodParamsCnt--;
                    }
                    if (passedArgsCnt > methodParamsCnt)
                        throw new ReturnAsErrorException($"Unable to evaluate method '{methodName}'. Too many arguments passed.", "ArgumentError");
                }

                if (isExtensionMethod)
                {
                    commandParamsObjWriter.Write(methodParamsCnt + 1);
                    commandParamsObjWriter.WriteObj(objectId, context.SdbAgent);
                }
                else
                {
                    commandParamsObjWriter.Write(methodParamsCnt);
                }

                int argIndex = 0;
                // explicitly passed arguments
                for (; argIndex < passedArgsCnt; argIndex++)
                {
                    var arg = method.ArgumentList.Arguments[argIndex];
                    if (arg.Expression is LiteralExpressionSyntax literal)
                    {
                        if (!await commandParamsObjWriter.WriteConst(literal, context.SdbAgent, token))
                            throw new InternalErrorException($"Unable to evaluate method '{methodName}'. Unable to write LiteralExpressionSyntax into binary writer.");
                    }
                    else if (arg.Expression is IdentifierNameSyntax identifierName)
                    {
                        if (!await commandParamsObjWriter.WriteJsonValue(memberAccessValues[identifierName.Identifier.Text], context.SdbAgent, token))
                            throw new InternalErrorException($"Unable to evaluate method '{methodName}'. Unable to write IdentifierNameSyntax into binary writer.");
                    }
                    else
                    {
                        throw new InternalErrorException($"Unable to evaluate method '{methodName}'. Unable to write into binary writer, not recognized expression type: {arg.Expression.GetType().Name}");
                    }
                }
                // optional arguments that were not overwritten
                for (; argIndex < methodParamsCnt; argIndex++)
                {
                    if (!await commandParamsObjWriter.WriteConst(methodParamsInfo[argIndex].TypeCode, methodParamsInfo[argIndex].Value, context.SdbAgent, token))
                        throw new InternalErrorException($"Unable to write optional parameter {methodParamsInfo[argIndex].Name} value in method '{methodName}' to the mono buffer.");
                }
                var retMethod = await context.SdbAgent.InvokeMethod(commandParamsObjWriter.GetParameterBuffer(), methodId, token);
                return await GetValueFromObject(retMethod, token);
            }
            catch (Exception ex) when (ex is not (ExpressionEvaluationFailedException or ReturnAsErrorException))
            {
                throw new ExpressionEvaluationFailedException($"Unable to evaluate method '{method}': {ex.Message}", ex);
            }

            async Task<int> FindMethodIdOnLinqEnumerable(IList<int> typeIds, string methodName)
            {
                if (linqTypeId == -1)
                {
                    linqTypeId = await context.SdbAgent.GetTypeByName("System.Linq.Enumerable", token);
                    if (linqTypeId == 0)
                    {
                        logger.LogDebug($"Cannot find type 'System.Linq.Enumerable'");
                        return 0;
                    }
                }

                int newMethodId = await context.SdbAgent.GetMethodIdByName(linqTypeId, methodName, token);
                if (newMethodId == 0)
                    return 0;

                foreach (int typeId in typeIds)
                {
                    List<int> genericTypeArgs = await context.SdbAgent.GetTypeParamsOrArgsForGenericType(typeId, token);
                    if (genericTypeArgs.Count > 0)
                    {
                        isExtensionMethod = true;
                        return await context.SdbAgent.MakeGenericMethod(newMethodId, genericTypeArgs, token);
                    }
                }

                return 0;
            }
        }

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(decimal), typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(float), typeof(double)
        };

        public object ConvertCSharpToJSType(object v, Type type)
        {
            if (v == null)
                return new { type = "object", subtype = "null", className = type?.ToString(), description = type?.ToString() };
            if (v is string s)
                return new { type = "string", value = s, description = s };
            if (v is char c)
                return new { type = "symbol", value = c, description = $"{(int)c} '{c}'" };
            if (NumericTypes.Contains(v.GetType()))
                return new { type = "number", value = v, description = Convert.ToDouble(v).ToString(CultureInfo.InvariantCulture) };
            if (v is bool)
                return new { type = "boolean", value = v, description = v.ToString().ToLowerInvariant(), className = type.ToString() };
            if (v is JObject)
                return v;
            if (v is Array arr)
            {
                return CacheEvaluationResult(
                    JObject.FromObject(
                        new
                        {
                            type = "object",
                            subtype = "array",
                            value = new JArray(arr.Cast<object>().Select((val, idx) => JObject.FromObject(
                                new
                                {
                                    value = ConvertCSharpToJSType(val, val.GetType()),
                                    name = $"{idx}"
                                }))),
                            description = v.ToString(),
                            className = type.ToString()
                        }));
            }
            return new { type = "object", value = v, description = v.ToString(), className = type.ToString() };
        }

        private JObject CacheEvaluationResult(JObject value)
        {
            if (IsDuplicated(value, out JObject duplicate))
                return value;

            var evalResultId = Interlocked.Increment(ref evaluationResultObjectId);
            string id = $"dotnet:evaluationResult:{evalResultId}";
            if (!value.TryAdd("objectId", id))
            {
                logger.LogWarning($"EvaluationResult cache request passed with ID: {value["objectId"].Value<string>()}. Overwritting it with a automatically assigned ID: {id}.");
                value["objectId"] = id;
            }
            scopeCache.EvaluationResults.Add(id, value);
            return value;

            bool IsDuplicated(JObject er, out JObject duplicate)
            {
                var type = er["type"].Value<string>();
                var subtype = er["subtype"].Value<string>();
                var value = er["value"];
                var description = er["description"].Value<string>();
                var className = er["className"].Value<string>();
                duplicate = scopeCache.EvaluationResults.FirstOrDefault(
                    pair => pair.Value["type"].Value<string>() == type
                    && pair.Value["subtype"].Value<string>() == subtype
                    && pair.Value["description"].Value<string>() == description
                    && pair.Value["className"].Value<string>() == className
                    && JToken.DeepEquals(pair.Value["value"], value)).Value;
                return duplicate != null;
            }
        }

        public JObject TryGetEvaluationResult(string id)
        {
            JObject val;
            if (!scopeCache.EvaluationResults.TryGetValue(id, out val))
                logger.LogError($"EvaluationResult of ID: {id} does not exist in the cache.");
            return val;
        }
    }
}
