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

        public async Task<(JObject containerObject, ArraySegment<string> remaining)> ResolveStaticMembersInStaticTypes(ArraySegment<string> expressionParts, CancellationToken token)
        {
            var store = await proxy.LoadStore(sessionId, false, token);
            var methodInfo = context.CallStack.FirstOrDefault(s => s.Id == scopeId)?.Method?.Info;

            if (methodInfo == null)
                return (null, null);

            string[] parts = expressionParts.ToArray();

            string fullName = methodInfo.IsAsync == 0 ? methodInfo.TypeInfo.FullName : StripAsyncPartOfFullName(methodInfo.TypeInfo.FullName);
            string[] fullNameParts = fullName.Split(".", StringSplitOptions.TrimEntries).ToArray();
            for (int i = 0; i < fullNameParts.Length; i++)
            {
                string[] fullNamePrefix = fullNameParts[..^i];
                var (memberObject, remaining) = await FindStaticMemberMatchingParts(parts, fullNamePrefix);
                if (memberObject != null)
                    return (memberObject, remaining);
            }
            return await FindStaticMemberMatchingParts(parts);

            async Task<(JObject, ArraySegment<string>)> FindStaticMemberMatchingParts(string[] parts, string[] fullNameParts = null)
            {
                string classNameToFind = fullNameParts == null ? "" : string.Join(".", fullNameParts);
                int typeId = -1;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (!string.IsNullOrEmpty(methodInfo.TypeInfo.Namespace))
                    {
                        typeId = await FindStaticTypeId(methodInfo.TypeInfo.Namespace + "." + classNameToFind);
                        if (typeId != -1)
                            continue;
                    }
                    typeId = await FindStaticTypeId(classNameToFind);

                    string part = parts[i];
                    if (typeId != -1)
                    {
                        JObject memberObject = await FindStaticMemberInType(classNameToFind, part, typeId);
                        if (memberObject != null)
                        {
                            ArraySegment<string> remaining = null;
                            if (i < parts.Length - 1)
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
                }
                return (null, null);
            }

            // async function full name has a form: namespaceName.<currentFrame'sMethodName>d__integer
            static string StripAsyncPartOfFullName(string fullName)
                => fullName.IndexOf(".<") is int index && index < 0
                    ? fullName
                    : fullName.Substring(0, index);


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
                    try
                    {
                        var staticFieldValue = await context.SdbAgent.GetFieldValue(typeId, field.Id, token);
                        var valueRet = await GetValueFromObject(staticFieldValue, token);
                        // we need the full name here
                        valueRet["className"] = classNameToFind;
                        return valueRet;
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, $"Failed to get value of field {field.Name} on {classNameToFind} " +
                            $"because {field.Name} is not a static member of {classNameToFind}.");
                    }
                    return null;
                }

                var methodId = await context.SdbAgent.GetPropertyMethodIdByName(typeId, name, token);
                if (methodId != -1)
                {
                    using var commandParamsObjWriter = new MonoBinaryWriter();
                    commandParamsObjWriter.Write(0); //param count
                    try
                    {
                        var retMethod = await context.SdbAgent.InvokeMethod(commandParamsObjWriter.GetParameterBuffer(), methodId, token);
                        return await GetValueFromObject(retMethod, token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, $"Failed to invoke getter of id={methodId} on {classNameToFind}.{name} " +
                            $"because {name} is not a static member of {classNameToFind}.");
                    }
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

        public async Task<JObject> Resolve(ElementAccessExpressionSyntax elementAccess, Dictionary<string, JObject> memberAccessValues, JObject indexObject, List<string> variableDefinitions, CancellationToken token)
        {
            try
            {
                JObject rootObject = null;
                string elementAccessStrExpression = elementAccess.Expression.ToString();
                var multiDimensionalArray = false;
                rootObject = await Resolve(elementAccessStrExpression, token);
                if (rootObject == null)
                {
                    rootObject = indexObject;
                    indexObject = null;
                }
                if (rootObject != null)
                {
                    string elementIdxStr = null;
                    int elementIdx = 0;
                    var elementAccessStr = elementAccess.ToString();
                    // x[1] or x[a] or x[a.b]
                    if (indexObject == null)
                    {
                        if (elementAccess.ArgumentList != null)
                        {
                            for (int i = 0 ; i < elementAccess.ArgumentList.Arguments.Count; i++)
                            {
                                var arg = elementAccess.ArgumentList.Arguments[i];
                                if (i != 0)
                                {
                                    elementIdxStr += ", ";
                                    multiDimensionalArray = true;
                                }
                                // e.g. x[1]
                                if (arg.Expression is LiteralExpressionSyntax)
                                {
                                    var argParm = arg.Expression as LiteralExpressionSyntax;
                                    elementIdxStr += argParm.ToString();
                                }

                                // e.g. x[a] or x[a.b]
                                else if (arg.Expression is IdentifierNameSyntax)
                                {
                                    var argParm = arg.Expression as IdentifierNameSyntax;

                                    // x[a.b]
                                    memberAccessValues.TryGetValue(argParm.Identifier.Text, out indexObject);

                                    // x[a]
                                    if (indexObject == null)
                                    {
                                        indexObject = await Resolve(argParm.Identifier.Text, token);
                                    }
                                    elementIdxStr += indexObject["value"].ToString();
                                }
                                // FixMe: indexing with expressions, e.g. x[a + 1]
                            }
                        }
                    }
                    // e.g. x[a[0]], x[a[b[1]]] etc.
                    else
                    {
                        elementIdxStr = indexObject["value"].ToString();
                    }
                    if (elementIdxStr != null)
                    {
                        var type = rootObject?["type"]?.Value<string>();
                        if (!DotnetObjectId.TryParse(rootObject?["objectId"]?.Value<string>(), out DotnetObjectId objectId))
                            throw new InvalidOperationException($"Cannot apply indexing with [] to a primitive object of type '{type}'");

                        switch (objectId.Scheme)
                        {
                            case "array":
                                rootObject["value"] = await context.SdbAgent.GetArrayValues(objectId.Value, token);
                                if (!multiDimensionalArray)
                                {
                                    int.TryParse(elementIdxStr, out elementIdx);
                                    return (JObject)rootObject["value"][elementIdx]["value"];
                                }
                                else
                                {
                                    return (JObject)(((JArray)rootObject["value"]).FirstOrDefault(x => x["name"].Value<string>() == elementIdxStr)["value"]);
                                }
                            case "object":
                                if (multiDimensionalArray)
                                    throw new InvalidOperationException($"Cannot apply indexing with [,] to an object of type '{type}'");
                                int.TryParse(elementIdxStr, out elementIdx);
                                if (type == "string")
                                {
                                    // ToArray() does not exist on string
                                    var eaExpressionFormatted = elementAccessStrExpression.Replace('.', '_'); // instance_str
                                    variableDefinitions.Add(ExpressionEvaluator.ConvertJSToCSharpLocalVariableAssignment(eaExpressionFormatted, rootObject));
                                    var eaFormatted = elementAccessStr.Replace('.', '_'); // instance_str[1]
                                    return await ExpressionEvaluator.EvaluateSimpleExpression(this, eaFormatted, elementAccessStr, variableDefinitions, logger, token);
                                }
                                var typeIds = await context.SdbAgent.GetTypeIdsForObject(objectId.Value, true, token);
                                int[] methodIds = await context.SdbAgent.GetMethodIdsByName(typeIds[0], "ToArray", token);
                                // ToArray should not have an overload, but if user defined it, take the default one: without params
                                if (methodIds == null)
                                    throw new InvalidOperationException($"Type '{rootObject?["className"]?.Value<string>()}' cannot be indexed.");

                                int toArrayId = methodIds[0];
                                if (methodIds.Length > 1)
                                {
                                    foreach (var methodId in methodIds)
                                    {
                                        MethodInfoWithDebugInformation methodInfo = await context.SdbAgent.GetMethodInfo(methodId, token);
                                        ParameterInfo[] paramInfo = methodInfo.GetParametersInfo();
                                        if (paramInfo.Length == 0)
                                        {
                                            toArrayId = methodId;
                                            break;
                                        }
                                    }
                                }
                                try
                                {
                                    var toArrayRetMethod = await context.SdbAgent.InvokeMethod(objectId.Value, toArrayId, isValueType: false, token);
                                    rootObject = await GetValueFromObject(toArrayRetMethod, token);
                                    DotnetObjectId.TryParse(rootObject?["objectId"]?.Value<string>(), out DotnetObjectId arrayObjectId);
                                    rootObject["value"] = await context.SdbAgent.GetArrayValues(arrayObjectId.Value, token);
                                    return (JObject)rootObject["value"][elementIdx]["value"];
                                }
                                catch
                                {
                                    throw new InvalidOperationException($"Cannot apply indexing with [] to an object of type '{rootObject?["className"]?.Value<string>()}'");
                                }
                            default:
                                throw new InvalidOperationException($"Cannot apply indexing with [] to an expression of scheme '{objectId.Scheme}'");
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

        public async Task<(JObject, string)> ResolveInvocationInfo(InvocationExpressionSyntax method, CancellationToken token)
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
            (JObject rootObject, string methodName) = await ResolveInvocationInfo(method, token);
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
                int[] methodIds = await context.SdbAgent.GetMethodIdsByName(typeIds[0], methodName, token);
                if (methodIds == null)
                {
                    //try to search on System.Linq.Enumerable
                    int methodId = await FindMethodIdOnLinqEnumerable(typeIds, methodName);
                    if (methodId == 0)
                    {
                        var typeName = await context.SdbAgent.GetTypeName(typeIds[0], token);
                        throw new ReturnAsErrorException($"Method '{methodName}' not found in type '{typeName}'", "ReferenceError");
                    }
                    methodIds = new int[] { methodId };
                }
                // get information about params in all overloads for *methodName*
                List<MethodInfoWithDebugInformation> methodInfos = await GetMethodParamInfosForMethods(methodIds);
                int passedArgsCnt = method.ArgumentList.Arguments.Count;
                int maxMethodParamsCnt = methodInfos.Max(v => v.GetParametersInfo().Length);
                if (isExtensionMethod)
                {
                    // implicit *this* parameter
                    maxMethodParamsCnt--;
                }
                if (passedArgsCnt > maxMethodParamsCnt)
                    throw new ReturnAsErrorException($"Unable to evaluate method '{methodName}'. Too many arguments passed.", "ArgumentError");

                foreach (var methodInfo in methodInfos)
                {
                    ParameterInfo[] methodParamsInfo = methodInfo.GetParametersInfo();
                    int methodParamsCnt = isExtensionMethod ? methodParamsInfo.Length - 1 : methodParamsInfo.Length;
                    int optionalParams = methodParamsInfo.Count(v => v.Value != null);
                    if (passedArgsCnt > methodParamsCnt || passedArgsCnt < methodParamsCnt - optionalParams)
                    {
                        // this overload does not match the number of params passed, try another one
                        continue;
                    }
                    int methodId = methodInfo.DebugId;
                    using var commandParamsObjWriter = new MonoBinaryWriter();

                    if (isExtensionMethod)
                    {
                        commandParamsObjWriter.Write(methodParamsCnt + 1);
                        commandParamsObjWriter.WriteObj(objectId, context.SdbAgent);
                    }
                    else
                    {
                        // instance method
                        commandParamsObjWriter.WriteObj(objectId, context.SdbAgent);
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
                    try
                    {
                        var retMethod = await context.SdbAgent.InvokeMethod(commandParamsObjWriter.GetParameterBuffer(), methodId, token);
                        return await GetValueFromObject(retMethod, token);
                    }
                    catch
                    {
                        // try further methodIds, we're looking for a method with the same type of params that the user passed
                        logger.LogDebug($"InvokeMethod failed due to parameter type mismatch for {methodName} with {methodParamsCnt} parameters, including {optionalParams} optional.");
                        continue;
                    }
                }
                throw new ReturnAsErrorException($"No implementation of method '{methodName}' matching '{method}' found in type {rootObject["className"]}.", "ArgumentError");
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

                int[] newMethodIds = await context.SdbAgent.GetMethodIdsByName(linqTypeId, methodName, token);
                if (newMethodIds == null)
                    return 0;

                foreach (int typeId in typeIds)
                {
                    List<int> genericTypeArgs = await context.SdbAgent.GetTypeParamsOrArgsForGenericType(typeId, token);
                    if (genericTypeArgs.Count > 0)
                    {
                        isExtensionMethod = true;
                        return await context.SdbAgent.MakeGenericMethod(newMethodIds[0], genericTypeArgs, token);
                    }
                }

                return 0;
            }

            async Task<List<MethodInfoWithDebugInformation>> GetMethodParamInfosForMethods(int[] methodIds)
            {
                List<MethodInfoWithDebugInformation> allMethodInfos = new();
                for (int i = 0; i < methodIds.Length; i++)
                {
                    var ithMethodInfo = await context.SdbAgent.GetMethodInfo(methodIds[i], token);
                    if (ithMethodInfo != null)
                        allMethodInfos.Add(ithMethodInfo);
                }
                return allMethodInfos;
            }
        }

        public JObject ConvertCSharpToJSType(object v, Type type)
        {
            if (v is JObject jobj)
                return jobj;

            if (v is null)
                return JObjectValueCreator.CreateNull("<unknown>")?["value"] as JObject;

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

            string typeName = v.GetType().ToString();
            jobj = JObjectValueCreator.CreateFromPrimitiveType(v);
            return jobj is not null
                ? jobj["value"] as JObject
                : JObjectValueCreator.Create<object>(value: null,
                                                    type: "object",
                                                    description: v.ToString(),
                                                    className: typeName)?["value"] as JObject;
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
