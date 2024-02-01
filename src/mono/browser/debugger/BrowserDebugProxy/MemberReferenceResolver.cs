// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Net.WebSockets;
using BrowserDebugProxy;
using System.Globalization;
using System.Reflection;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal sealed class MemberReferenceResolver
    {
        private static int evaluationResultObjectId;
        private readonly SessionId sessionId;
        private readonly int scopeId;
        private readonly MonoProxy proxy;
        private readonly ExecutionContext context;
        private readonly PerScopeCache scopeCache;
        private readonly ILogger logger;
        private bool localsFetched;
        private int linqTypeId;
        public ExecutionContext GetContext() => context;

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
                    try
                    {
                        await proxy.GetScopeProperties(sessionId, scopeId, token);
                    }
                    catch (Exception ex)
                    {
                        throw new ReturnAsErrorException($"BUG: Unable to get properties for scope: {scopeId}. {ex}", ex.GetType().Name);
                    }
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

        public async Task<JObject> Resolve(
            ElementAccessExpressionSyntax elementAccess,
            Dictionary<string, JObject> memberAccessValues,
            List<JObject> nestedIndexObject,
            List<VariableDefinition> variableDefinitions,
            CancellationToken token)
        {
            try
            {
                JObject rootObject = null;
                string elementAccessStrExpression = elementAccess.Expression.ToString();
                rootObject = await Resolve(elementAccessStrExpression, token);

                if (rootObject == null)
                {
                    // it might be a jagged array where the previously added nestedIndexObject should be treated as a new rootObject
                    rootObject = nestedIndexObject.LastOrDefault();
                    if (rootObject != null)
                        nestedIndexObject.RemoveAt(nestedIndexObject.Count - 1);
                }

                ElementIndexInfo elementIdxInfo = await GetElementIndexInfo(nestedIndexObject);
                if (elementIdxInfo is null)
                    return null;

                // 1. Parse the indexes
                int elementIdx = 0;
                var elementAccessStr = elementAccess.ToString();

                // 2. Get the value
                var type = rootObject?["type"]?.Value<string>();
                if (!DotnetObjectId.TryParse(rootObject?["objectId"]?.Value<string>(), out DotnetObjectId objectId))
                    throw new InvalidOperationException($"Cannot apply indexing with [] to a primitive object of type '{type}'");

                bool isMultidimensional = elementIdxInfo.DimensionsCount != 1;
                switch (objectId.Scheme)
                {
                    case "valuetype": //can be an inlined array
                    {
                        if (!context.SdbAgent.ValueCreator.TryGetValueTypeById(objectId.Value, out ValueTypeClass valueType))
                            throw new InvalidOperationException($"Cannot apply indexing with [] to an expression of scheme '{objectId.Scheme}'");
                        var typeInfo = await context.SdbAgent.GetTypeInfo(valueType.TypeId, token);
                        if (valueType.InlineArray == null)
                        {
                            JObject vtResult = await InvokeGetItemOnJObject(rootObject, valueType.TypeId, objectId, elementIdxInfo, token);
                            if (vtResult != null)
                                return vtResult;
                        }
                        if (int.TryParse(elementIdxInfo.ElementIdxStr, out elementIdx) && elementIdx >= 0 && elementIdx < valueType.InlineArray.Count)
                            return (JObject)valueType.InlineArray[elementIdx]["value"];
                        throw new InvalidOperationException($"Index is outside the bounds of the inline array");
                    }
                    case "array":
                        rootObject["value"] = await context.SdbAgent.GetArrayValues(objectId.Value, token);
                        if (!isMultidimensional)
                        {
                            int.TryParse(elementIdxInfo.ElementIdxStr, out elementIdx);
                            return (JObject)rootObject["value"][elementIdx]["value"];
                        }
                        else
                        {
                            return (JObject)(((JArray)rootObject["value"]).FirstOrDefault(x => x["name"].Value<string>() == elementIdxInfo.ElementIdxStr)["value"]);
                        }
                    case "object":
                        // ToDo: try to use the get_Item for string as well
                        if (!isMultidimensional && type == "string")
                        {
                            var eaExpressionFormatted = elementAccessStrExpression.Replace('.', '_'); // instance_str
                            variableDefinitions.Add(new (eaExpressionFormatted, rootObject, ExpressionEvaluator.ConvertJSToCSharpLocalVariableAssignment(eaExpressionFormatted, rootObject)));
                            var eaFormatted = elementAccessStr.Replace('.', '_'); // instance_str[1]
                            var variableDef = await ExpressionEvaluator.GetVariableDefinitions(this, variableDefinitions, invokeToStringInObject: false, token);
                            return await ExpressionEvaluator.EvaluateSimpleExpression(this, eaFormatted, elementAccessStr, variableDef, logger, token);
                        }
                        if (elementIdxInfo.Indexers is null || elementIdxInfo.Indexers.Count == 0)
                            throw new InternalErrorException($"Unable to write index parameter to invoke the method in the runtime.");

                        List<int> typeIds = await context.SdbAgent.GetTypeIdsForObject(objectId.Value, true, token);
                        JObject objResult = await InvokeGetItemOnJObject(rootObject, typeIds[0], objectId, elementIdxInfo, token);
                        if (objResult == null)
                            throw new InvalidOperationException($"Cannot apply indexing with [] to an object of type '{rootObject?["className"]?.Value<string>()}'");
                        return objResult;
                    default:
                        throw new InvalidOperationException($"Cannot apply indexing with [] to an expression of scheme '{objectId.Scheme}'");
                }
            }
            catch (Exception ex)
            {
                throw new ReturnAsErrorException($"Unable to evaluate element access '{elementAccess}': {ex.Message}", ex.GetType().Name);
            }

            async Task<ElementIndexInfo> GetElementIndexInfo(List<JObject> nestedIndexers)
            {
                if (elementAccess.ArgumentList is null)
                    return null;

                int dimCnt = elementAccess.ArgumentList.Arguments.Count;
                LiteralExpressionSyntax indexingExpression = null;
                StringBuilder elementIdxStr = new StringBuilder();
                List<object> indexers = new();
                // nesting should be resolved in reverse order
                int nestedIndexersCnt = nestedIndexers.Count - 1;
                for (int i = 0; i < dimCnt; i++)
                {
                    JObject indexObject;
                    var arg = elementAccess.ArgumentList.Arguments[i];
                    if (i != 0)
                    {
                        elementIdxStr.Append(", ");
                    }
                    // e.g. x[1]
                    if (arg.Expression is LiteralExpressionSyntax)
                    {
                        indexingExpression = arg.Expression as LiteralExpressionSyntax;
                        string expression = indexingExpression.ToString();
                        elementIdxStr.Append(expression);
                        indexers.Add(indexingExpression);
                    }

                    // e.g. x[a] or x[a.b]
                    else if (arg.Expression is IdentifierNameSyntax)
                    {
                        var argParm = arg.Expression as IdentifierNameSyntax;

                        // x[a.b]
                        memberAccessValues.TryGetValue(argParm.Identifier.Text, out indexObject);

                        // x[a]
                        indexObject ??= await Resolve(argParm.Identifier.Text, token);
                        elementIdxStr.Append(indexObject["value"].ToString());
                        indexers.Add(indexObject);
                    }
                    // nested indexing, e.g. x[a[0]], x[a[b[1]]], x[a[0], b[1]]
                    else if (arg.Expression is ElementAccessExpressionSyntax)
                    {
                        if (nestedIndexers == null || nestedIndexersCnt < 0)
                            throw new InvalidOperationException($"Cannot resolve nested indexing");
                        JObject nestedIndexObject = nestedIndexers[nestedIndexersCnt];
                        nestedIndexers.RemoveAt(nestedIndexersCnt);
                        elementIdxStr.Append(nestedIndexObject["value"].ToString());
                        indexers.Add(nestedIndexObject);
                        nestedIndexersCnt--;
                    }
                    // indexing with expressions, e.g. x[a + 1]
                    else
                    {
                        string expression = arg.ToString();
                        var variableDef = await ExpressionEvaluator.GetVariableDefinitions(this, variableDefinitions, invokeToStringInObject: false, token);
                        indexObject = await ExpressionEvaluator.EvaluateSimpleExpression(this, expression, expression, variableDef, logger, token);
                        string idxType = indexObject["type"].Value<string>();
                        if (idxType != "number")
                            throw new InvalidOperationException($"Cannot index with an object of type '{idxType}'");
                        elementIdxStr.Append(indexObject["value"].ToString());
                        indexers.Add(indexObject);
                    }
                }
                return new ElementIndexInfo(
                    DimensionsCount: dimCnt,
                    ElementIdxStr: elementIdxStr.ToString(),
                    Indexers: indexers);
            }
        }

        private async Task<JObject> InvokeGetItemOnJObject(
            JObject rootObject,
            int typeId,
            DotnetObjectId objectId,
            ElementIndexInfo elementIdxInfo,
            CancellationToken token)
        {
            int[] methodIds = await context.SdbAgent.GetMethodIdsByName(typeId, "get_Item", BindingFlags.Default, token);
            if (methodIds == null || methodIds.Length == 0)
                throw new InvalidOperationException($"Type '{rootObject?["className"]?.Value<string>()}' cannot be indexed.");
            var type = rootObject?["type"]?.Value<string>();

            // ToDo: optimize the loop by choosing the right method at once without trying out them all
            for (int i = 0; i < methodIds.Length; i++)
            {
                MethodInfoWithDebugInformation methodInfo = await context.SdbAgent.GetMethodInfo(methodIds[i], token);
                ParameterInfo[] paramInfo = methodInfo.GetParametersInfo();
                if (paramInfo.Length != elementIdxInfo.DimensionsCount)
                    continue;
                try
                {
                    if (!CheckParametersCompatibility(paramInfo, elementIdxInfo.Indexers))
                        continue;
                    ArraySegment<byte> buffer = await WriteIndexObjectAsIndices(objectId, elementIdxInfo.Indexers, paramInfo);
                    JObject getItemRetObj = await context.SdbAgent.InvokeMethod(buffer, methodIds[i], token);
                    return (JObject)getItemRetObj["value"];
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"Attempt number {i + 1} out of {methodIds.Length} of invoking method {methodInfo.Name} with parameter named {paramInfo[0].Name} on type {type} failed. Method Id = {methodIds[i]}.\nInner exception: {ex}.");
                    continue;
                }
            }
            return null;

            async Task<ArraySegment<byte>> WriteIndexObjectAsIndices(DotnetObjectId rootObjId, List<object> indexObjects, ParameterInfo[] paramInfo)
            {
                using var writer = new MonoBinaryWriter();
                writer.WriteObj(rootObjId, context.SdbAgent);
                writer.Write(indexObjects.Count); // number of method args
                foreach ((ParameterInfo pi, object indexObject) in paramInfo.Zip(indexObjects))
                {
                    if (indexObject is JObject indexJObject)
                    {
                        // indexed by an identifier name syntax
                        if (!await writer.WriteJsonValue(indexJObject, context.SdbAgent, pi.TypeCode, token))
                            throw new InternalErrorException($"Parsing index of type {indexJObject["type"].Value<string>()} to write it into the buffer failed.");
                    }
                    else if (indexObject is LiteralExpressionSyntax expression)
                    {
                        // indexed by a literal expression syntax
                        if (!await writer.WriteConst(expression, context.SdbAgent, token))
                            throw new InternalErrorException($"Parsing literal expression index = {expression} to write it into the buffer failed.");
                    }
                    else
                    {
                        throw new InternalErrorException($"Unexpected index type.");
                    }
                }
                return writer.GetParameterBuffer();
            }
        }

        private static bool CheckParametersCompatibility(ParameterInfo[] paramInfos, List<object> indexObjects)
        {
            if (paramInfos.Length != indexObjects.Count)
                return false;
            foreach ((ParameterInfo paramInfo, object indexObj) in paramInfos.Zip(indexObjects))
            {
                string argumentType = "", argumentClassName = "";
                bool isArray = false;
                if (indexObj is JObject indexJObj)
                {
                    argumentType = indexJObj["type"]?.Value<string>();
                    argumentClassName = indexJObj["className"]?.Value<string>();
                    isArray = indexJObj["subtype"]?.Value<string>()?.Equals("array") == true;
                }
                else if (indexObj is LiteralExpressionSyntax literal)
                {
                    // any primitive literal is an object
                    if (paramInfo.TypeCode.Value == ElementType.Object)
                        continue;
                    switch (literal.Kind())
                    {
                        case SyntaxKind.NumericLiteralExpression:
                            argumentType = "number";
                            break;
                        case SyntaxKind.StringLiteralExpression:
                            argumentType = "string";
                            break;
                        case SyntaxKind.TrueLiteralExpression:
                        case SyntaxKind.FalseLiteralExpression:
                            argumentType = "boolean";
                            break;
                        case SyntaxKind.CharacterLiteralExpression:
                            argumentType = "symbol";
                            break;
                        case SyntaxKind.NullLiteralExpression:
                            // do not check
                            continue;
                    }
                }
                if (!CheckParameterCompatibility(paramInfo.TypeCode, argumentType, argumentClassName, isArray))
                    return false;
            }
            return true;
        }

        private static bool CheckParameterCompatibility(ElementType? paramTypeCode, string argumentType, string argumentClassName, bool isArray)
        {
            if (!paramTypeCode.HasValue)
                return true;

            switch (paramTypeCode.Value)
            {
                case ElementType.Object:
                    if (argumentType != "object" || isArray)
                        return false;
                    break;
                case ElementType.I2:
                case ElementType.I4:
                case ElementType.I8:
                case ElementType.R4:
                case ElementType.R8:
                case ElementType.U2:
                case ElementType.U4:
                case ElementType.U8:
                    if (argumentType != "number")
                        return false;
                    if (argumentType == "object")
                        return false;
                    break;
                case ElementType.Char:
                    if (argumentType != "string" && argumentType != "symbol")
                        return false;
                    if (argumentType == "object")
                        return false;
                    break;
                case ElementType.Boolean:
                    if (argumentType == "boolean")
                        return true;
                    if (argumentType == "number" && (argumentClassName == "Single" || argumentClassName == "Double"))
                        return false;
                    if (argumentType == "object")
                        return false;
                    if (argumentType == "string" || argumentType == "symbol")
                        return false;
                    break;
                case ElementType.String:
                    if (argumentType != "string")
                        return false;
                    break;
                default:
                    return true;
            }
            return true;
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
                        throw new ReturnAsErrorException($"Expression '{memberAccessExpressionSyntax}' evaluated to null", "NullReferenceException");
                }
                else if (expr is IdentifierNameSyntax && scopeCache.ObjectFields.TryGetValue("this", out JObject thisValue))
                {
                    rootObject = await GetValueFromObject(thisValue, token);
                    methodName = expr.ToString();
                }
                return (rootObject, methodName);
            }
            catch (Exception ex) when (ex is not ReturnAsErrorException)
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
                int[] methodIds = await context.SdbAgent.GetMethodIdsByName(typeIds[0], methodName, BindingFlags.Default, token);
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
                        else if (arg.Expression is PrefixUnaryExpressionSyntax negativeLiteral)
                        {
                            if (!commandParamsObjWriter.WriteConst(negativeLiteral))
                                throw new InternalErrorException($"Unable to evaluate method '{methodName}'. Unable to write PrefixUnaryExpressionSyntax into binary writer.");
                        }
                        else if (arg.Expression is IdentifierNameSyntax identifierName)
                        {
                            if (!memberAccessValues.TryGetValue(identifierName.Identifier.Text, out JObject argValue))
                                argValue = await Resolve(identifierName.Identifier.Text, token);
                            if (!await commandParamsObjWriter.WriteJsonValue(argValue, context.SdbAgent, methodParamsInfo[argIndex].TypeCode, token))
                                throw new InternalErrorException($"Unable to evaluate method '{methodName}'. Unable to write IdentifierNameSyntax into binary writer.");
                        }
                        else if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
                        {
                            JObject argValue = await Resolve(memberAccess.ToString(), token);
                            if (!await commandParamsObjWriter.WriteJsonValue(argValue, context.SdbAgent, methodParamsInfo[argIndex].TypeCode, token))
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
            catch (Exception ex) when (ex is not ReturnAsErrorException)
            {
                throw new ReturnAsErrorException($"Unable to evaluate method '{method}': {ex.Message}", ex.GetType().Name);
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

                int[] newMethodIds = await context.SdbAgent.GetMethodIdsByName(linqTypeId, methodName, BindingFlags.Default, token);
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

        private sealed record ElementIndexInfo(
            string ElementIdxStr,
            // keeps JObjects and LiteralExpressionSyntaxes:
            List<object> Indexers,
            int DimensionsCount = 1);
    }
}
