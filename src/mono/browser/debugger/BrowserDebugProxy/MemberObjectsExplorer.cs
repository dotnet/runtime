// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;

namespace BrowserDebugProxy
{
    internal static class MemberObjectsExplorer
    {
        private static bool IsACollectionType(string typeName)
            => typeName is not null &&
                    (typeName.StartsWith("System.Collections.Generic", StringComparison.Ordinal) ||
                    typeName.EndsWith("[]", StringComparison.Ordinal));

        private static string GetNamePrefixForValues(string memberName, string typeName, bool isOwn, DebuggerBrowsableState? state)
        {
            if (isOwn || state != DebuggerBrowsableState.RootHidden)
                return memberName;

            string justClassName = Path.GetExtension(typeName);
            if (justClassName[0] == '.')
                justClassName = justClassName[1..];
            return $"{memberName} ({justClassName})";
        }

        private static async Task<JObject> ReadFieldValue(
            MonoSDBHelper sdbHelper,
            MonoBinaryReader reader,
            FieldTypeClass field,
            int objectId,
            TypeInfoWithDebugInformation typeInfo,
            int fieldValueType,
            bool isOwn,
            int parentTypeId,
            GetObjectCommandOptions getObjectOptions,
            CancellationToken token)
        {
            var fieldValue = await sdbHelper.ValueCreator.ReadAsVariableValue(
                reader,
                field.Name,
                token,
                isOwn: isOwn,
                field.TypeId,
                getObjectOptions.HasFlag(GetObjectCommandOptions.ForDebuggerDisplayAttribute));

            var typeFieldsBrowsableInfo = typeInfo?.Info?.DebuggerBrowsableFields;
            var typePropertiesBrowsableInfo = typeInfo?.Info?.DebuggerBrowsableProperties;

            if (!typeFieldsBrowsableInfo.TryGetValue(field.Name, out DebuggerBrowsableState? state))
            {
                // for backing fields, we are getting it from the properties
                typePropertiesBrowsableInfo.TryGetValue(field.Name, out state);
            }
            fieldValue[InternalUseFieldName.State.Name] = state?.ToString();
            fieldValue[InternalUseFieldName.Section.Name] = field.Attributes.HasFlag(FieldAttributes.Private)
                ? "private" : "result";

            if (field.IsBackingField)
            {
                fieldValue[InternalUseFieldName.IsBackingField.Name] = true;
                fieldValue[InternalUseFieldName.ParentTypeId.Name] = parentTypeId;
            }
            if (field.Attributes.HasFlag(FieldAttributes.Static))
                fieldValue[InternalUseFieldName.IsStatic.Name] = true;

            if (getObjectOptions.HasFlag(GetObjectCommandOptions.WithSetter))
            {
                var command_params_writer_to_set = new MonoBinaryWriter();
                command_params_writer_to_set.Write(objectId);
                command_params_writer_to_set.Write(1);
                command_params_writer_to_set.Write(field.Id);
                var (data, length) = command_params_writer_to_set.ToBase64();

                fieldValue.Add("set", JObject.FromObject(new
                {
                    commandSet = CommandSet.ObjectRef,
                    command = CmdObject.RefSetValues,
                    buffer = data,
                    valtype = fieldValueType,
                    length = length,
                    id = MonoSDBHelper.GetNewId()
                }));
            }

            return fieldValue;
        }

        private static async Task<JArray> GetRootHiddenChildren(
            MonoSDBHelper sdbHelper,
            JObject root,
            string rootNamePrefix,
            string rootTypeName,
            GetObjectCommandOptions getCommandOptions,
            bool includeStatic,
            CancellationToken token)
        {
            var rootValue = root?["value"] ?? root["get"];

            if (rootValue?["subtype"]?.Value<string>() == "null")
                return new JArray();

            var type = rootValue?["type"]?.Value<string>();
            if (type != "object" && type != "function")
                return new JArray();

            if (!DotnetObjectId.TryParse(rootValue?["objectId"]?.Value<string>(), out DotnetObjectId rootObjectId))
                throw new Exception($"Cannot parse object id from {root} for {rootNamePrefix}");

            // if it's an accessor
            if (root["get"] != null)
                return await GetRootHiddenChildrenForProperty();

            if (rootValue?["type"]?.Value<string>() != "object")
                return new JArray();

            // unpack object/valuetype
            if (rootObjectId.Scheme is "object" or "valuetype")
            {
                GetMembersResult members;
                if (rootObjectId.Scheme is "valuetype")
                {
                    var valType = sdbHelper.GetValueTypeClass(rootObjectId.Value);
                    if (valType == null || valType.IsEnum)
                        return new JArray();
                    members = await valType.GetMemberValues(sdbHelper, getCommandOptions, false, includeStatic, token);
                }
                else members = await GetObjectMemberValues(sdbHelper, rootObjectId.Value, getCommandOptions, token, false, includeStatic);

                if (!IsACollectionType(rootTypeName))
                {
                    // is a class/valuetype with members
                    var resultValue = members.Flatten();
                    foreach (var item in resultValue)
                        item["name"] = $"{rootNamePrefix}.{item["name"]}";
                    return resultValue;
                }
                else
                {
                    // a collection - expose elements to be of array scheme
                    var memberNamedItems = members
                        .Where(m => m["name"]?.Value<string>() == "Items")
                        .FirstOrDefault();
                    if (memberNamedItems is not null &&
                        DotnetObjectId.TryParse(memberNamedItems["value"]?["objectId"]?.Value<string>(), out DotnetObjectId itemsObjectId) &&
                        itemsObjectId.Scheme == "array")
                    {
                        rootObjectId = itemsObjectId;
                    }
                }
            }

            if (rootObjectId.Scheme == "array")
            {
                JArray resultValue = await sdbHelper.GetArrayValues(rootObjectId.Value, token);

                // root hidden item name has to be unique, so we concatenate the root's name to it
                foreach (var item in resultValue)
                    item["name"] = $"{rootNamePrefix}[{item["name"]}]";

                return resultValue;
            }
            else
            {
                return new JArray();
            }

            async Task<JArray> GetRootHiddenChildrenForProperty()
            {
                var resMethod = await sdbHelper.InvokeMethod(rootObjectId, token);
                return await GetRootHiddenChildren(sdbHelper, resMethod, rootNamePrefix, rootTypeName, getCommandOptions, includeStatic, token);
            }
        }

        public static Task<GetMembersResult> GetTypeMemberValues(
            MonoSDBHelper sdbHelper,
            DotnetObjectId dotnetObjectId,
            GetObjectCommandOptions getObjectOptions,
            CancellationToken token,
            bool sortByAccessLevel = false,
            bool includeStatic = false)
            => dotnetObjectId.IsValueType
                    ? GetValueTypeMemberValues(sdbHelper, dotnetObjectId.Value, getObjectOptions, token, sortByAccessLevel, includeStatic)
                    : GetObjectMemberValues(sdbHelper, dotnetObjectId.Value, getObjectOptions, token, sortByAccessLevel, includeStatic);

        public static async Task<JArray> ExpandFieldValues(
            MonoSDBHelper sdbHelper,
            DotnetObjectId id,
            int containerTypeId,
            int parentTypeId,
            IReadOnlyList<FieldTypeClass> fields,
            GetObjectCommandOptions getCommandOptions,
            bool isOwn,
            bool includeStatic,
            CancellationToken token)
        {
            JArray fieldValues = new JArray();
            if (fields.Count == 0)
                return fieldValues;

            if (getCommandOptions.HasFlag(GetObjectCommandOptions.ForDebuggerProxyAttribute))
                fields = fields.Where(field => field.IsNotPrivate).ToList();

            using var commandParamsWriter = new MonoBinaryWriter();
            commandParamsWriter.Write(id.Value);
            commandParamsWriter.Write(fields.Count);
            foreach (var field in fields)
                commandParamsWriter.Write(field.Id);
            MonoBinaryReader retDebuggerCmdReader = id.IsValueType
                                                    ? await sdbHelper.SendDebuggerAgentCommand(CmdType.GetValues, commandParamsWriter, token) :
                                                    await sdbHelper.SendDebuggerAgentCommand(CmdObject.RefGetValues, commandParamsWriter, token);

            var typeInfo = await sdbHelper.GetTypeInfo(containerTypeId, token);

            int numFieldsRead = 0;
            foreach (FieldTypeClass field in fields)
            {
                long initialPos = retDebuggerCmdReader.BaseStream.Position;
                int valtype = retDebuggerCmdReader.ReadByte();
                retDebuggerCmdReader.BaseStream.Position = initialPos;

                JObject fieldValue = await ReadFieldValue(sdbHelper, retDebuggerCmdReader, field, id.Value, typeInfo, valtype, isOwn, parentTypeId, getCommandOptions, token);
                numFieldsRead++;

                if (typeInfo.Info.IsNonUserCode && getCommandOptions.HasFlag(GetObjectCommandOptions.JustMyCode) && field.Attributes.HasFlag(FieldAttributes.Private))
                    continue;

                if (!Enum.TryParse(fieldValue[InternalUseFieldName.State.Name].Value<string>(), out DebuggerBrowsableState fieldState)
                    || fieldState == DebuggerBrowsableState.Collapsed)
                {
                    fieldValues.Add(fieldValue);
                    continue;
                }

                if (fieldState == DebuggerBrowsableState.Never)
                    continue;

                string namePrefix = field.Name;
                string containerTypeName = await sdbHelper.GetTypeName(containerTypeId, token);
                namePrefix = GetNamePrefixForValues(field.Name, containerTypeName, isOwn, fieldState);
                string typeName = await sdbHelper.GetTypeName(field.TypeId, token);

                var enumeratedValues = await GetRootHiddenChildren(
                    sdbHelper, fieldValue, namePrefix, typeName, getCommandOptions, includeStatic, token);
                if (enumeratedValues != null)
                    fieldValues.AddRange(enumeratedValues);
            }

            if (numFieldsRead != fields.Count)
                throw new Exception($"Bug: Got {numFieldsRead} instead of expected {fields.Count} field values");

            return fieldValues;
        }

        public static Task<GetMembersResult> GetValueTypeMemberValues(
            MonoSDBHelper sdbHelper, int valueTypeId, GetObjectCommandOptions getCommandOptions, CancellationToken token, bool sortByAccessLevel = false, bool includeStatic = false)
        {
            return sdbHelper.ValueCreator.TryGetValueTypeById(valueTypeId, out ValueTypeClass valueType)
                ? valueType.GetMemberValues(sdbHelper, getCommandOptions, sortByAccessLevel, includeStatic, token)
                : throw new ArgumentException($"Could not find any valuetype with id: {valueTypeId}", nameof(valueTypeId));
        }

        public static async Task<JArray> GetExpandedMemberValues(
            MonoSDBHelper sdbHelper,
            string typeName,
            string namePrefix,
            JObject value,
            DebuggerBrowsableState? state,
            bool includeStatic,
            CancellationToken token)
        {
            if (state is DebuggerBrowsableState.RootHidden)
            {
                if (MonoSDBHelper.IsPrimitiveType(typeName))
                    return GetHiddenElement();

                return await GetRootHiddenChildren(sdbHelper, value, namePrefix, typeName, GetObjectCommandOptions.None, includeStatic, token);

            }
            else if (state is DebuggerBrowsableState.Never)
            {
                return GetHiddenElement();
            }
            return new JArray(value);

            JArray GetHiddenElement()
            {
                var emptyHidden = JObject.FromObject(new { name = namePrefix });
                emptyHidden.Add(InternalUseFieldName.Hidden.Name, true);
                return new JArray(emptyHidden);
            }
        }

        public static async Task<Dictionary<string, JObject>> ExpandPropertyValues(
            MonoSDBHelper sdbHelper,
            int typeId,
            string typeName,
            ArraySegment<byte> getterParamsBuffer,
            GetObjectCommandOptions getCommandOptions,
            DotnetObjectId objectId,
            bool isValueType,
            bool isOwn,
            CancellationToken token,
            Dictionary<string, JObject> allMembers,
            bool includeStatic,
            int parentTypeId = -1)
        {
            using var retDebuggerCmdReader = await sdbHelper.GetTypePropertiesReader(typeId, token);
            if (retDebuggerCmdReader == null)
                return null;

            var nProperties = retDebuggerCmdReader.ReadInt32();
            var typeInfo = await sdbHelper.GetTypeInfo(typeId, token);
            var typePropertiesBrowsableInfo = typeInfo?.Info?.DebuggerBrowsableProperties;
            var parentSuffix = typeName.Split('.')[^1];

            GetMembersResult ret = new();
            for (int i = 0; i < nProperties; i++)
            {
                retDebuggerCmdReader.ReadInt32(); //propertyId
                string propName = retDebuggerCmdReader.ReadString();
                var getMethodId = retDebuggerCmdReader.ReadInt32();
                retDebuggerCmdReader.ReadInt32(); //setmethod
                var attrs = (PropertyAttributes)retDebuggerCmdReader.ReadInt32(); //attrs
                if (getMethodId == 0 || await sdbHelper.GetParamCount(getMethodId, token) != 0)
                    continue;
                bool isStatic = await sdbHelper.MethodIsStatic(getMethodId, token);
                if (!includeStatic && isStatic)
                    continue;

                MethodInfoWithDebugInformation getterInfo = await sdbHelper.GetMethodInfo(getMethodId, token);
                MethodAttributes getterAttrs = getterInfo.Info.Attributes;
                MethodAttributes getterMemberAccessAttrs = getterAttrs & MethodAttributes.MemberAccessMask;
                MethodAttributes vtableLayout = getterAttrs & MethodAttributes.VtableLayoutMask;

                if (typeInfo.Info.IsNonUserCode && getCommandOptions.HasFlag(GetObjectCommandOptions.JustMyCode) && getterMemberAccessAttrs == MethodAttributes.Private)
                    continue;

                bool isNewSlot = (vtableLayout & MethodAttributes.NewSlot) == MethodAttributes.NewSlot;

                typePropertiesBrowsableInfo.TryGetValue(propName, out DebuggerBrowsableState? state);

                // handle parents' members:
                if (!allMembers.TryGetValue(propName, out JObject existingMember))
                {
                    // new member
                    await AddProperty(getMethodId, parentTypeId, state, propName, getterMemberAccessAttrs, isStatic, isNewSlot: isNewSlot);
                    continue;
                }

                bool isExistingMemberABackingField = existingMember[InternalUseFieldName.IsBackingField.Name]?.Value<bool>() == true;
                if (isOwn && !isExistingMemberABackingField)
                {
                    // repeated propname on the same type! cannot happen
                    throw new Exception($"Internal Error: should not happen. propName: {propName}. Existing all members: {string.Join(",", allMembers.Keys)}");
                }

                bool isExistingMemberABackingFieldOwnedByThisType = isExistingMemberABackingField && existingMember[InternalUseFieldName.Owner.Name]?.Value<string>() == typeName;
                if (isExistingMemberABackingField && (isOwn || isExistingMemberABackingFieldOwnedByThisType))
                {
                    // this is the property corresponding to the backing field in *this* type
                    // `isOwn` would mean that this is the first type that we are looking at
                    await UpdateBackingFieldWithPropertyAttributes(existingMember, propName, getterMemberAccessAttrs, state);
                    continue;
                }

                var overriddenOrHiddenPropName = $"{propName} ({parentSuffix})";
                if (isNewSlot)
                {
                    // this has `new` keyword if it is newSlot but direct child was not a newSlot:
                    var child = allMembers.FirstOrDefault(
                        kvp => (kvp.Key == propName || kvp.Key.StartsWith($"{propName} (")) && kvp.Value[InternalUseFieldName.ParentTypeId.Name]?.Value<int>() == typeId).Value;
                    bool wasOverriddenByDerivedType = child != null && child[InternalUseFieldName.IsNewSlot.Name]?.Value<bool>() != true;
                    if (wasOverriddenByDerivedType)
                    {
                        /*
                         * property was overridden by a derived type member. We want to show
                         * only the overridden members. So, remove the backing field
                         * for this auto-property that was added, with the type name suffix
                         *
                         * Two cases:
                         * 1. auto-prop in base, overridden by auto-prop in derived
                         * 2. auto-prop in base, overridden by prop in derived
                         *
                         *    And in both cases we want to remove the backing field for the auto-prop for
                         *      *this* base type
                         */
                        allMembers.Remove(overriddenOrHiddenPropName);
                        continue;
                    }
                }

                /*
                 * property was *hidden* by a derived type member. In this case, we
                 * want to show *both* the members
                 */

                JObject backingFieldForHiddenProp = allMembers.GetValueOrDefault(overriddenOrHiddenPropName);
                if (backingFieldForHiddenProp is null || backingFieldForHiddenProp[InternalUseFieldName.IsBackingField.Name]?.Value<bool>() != true)
                {
                    // hiding with a non-auto property, so nothing to adjust
                    // add the new property
                    await AddProperty(getMethodId, parentTypeId, state, overriddenOrHiddenPropName, getterMemberAccessAttrs, isStatic, isNewSlot: isNewSlot);
                    continue;
                }

                await UpdateBackingFieldWithPropertyAttributes(backingFieldForHiddenProp, overriddenOrHiddenPropName, getterMemberAccessAttrs, state);
            }

            return allMembers;

            async Task UpdateBackingFieldWithPropertyAttributes(JObject backingField, string autoPropName, MethodAttributes getterMemberAccessAttrs, DebuggerBrowsableState? state)
            {
                backingField[InternalUseFieldName.Section.Name] = getterMemberAccessAttrs switch
                {
                    MethodAttributes.Private => "private",
                    _ => "result"
                };
                backingField[InternalUseFieldName.State.Name] = state?.ToString();

                if (state is null)
                    return;

                string namePrefix = GetNamePrefixForValues(autoPropName, typeName, isOwn, state);
                string backingPropTypeName = backingField["value"]?["className"]?.Value<string>();
                var expanded = await GetExpandedMemberValues(
                    sdbHelper, backingPropTypeName, namePrefix, backingField, state, includeStatic, token);
                backingField.Remove();
                allMembers.Remove(autoPropName);
                foreach (JObject evalue in expanded)
                    allMembers[evalue["name"].Value<string>()] = evalue;
            }

            async Task AddProperty(
                int getMethodId,
                int parentTypeId,
                DebuggerBrowsableState? state,
                string propNameWithSufix,
                MethodAttributes getterAttrs,
                bool isPropertyStatic,
                bool isNewSlot)
            {
                string returnTypeName = await sdbHelper.GetReturnType(getMethodId, token);
                JObject propRet = null;
                if (getCommandOptions.HasFlag(GetObjectCommandOptions.AutoExpandable) || getCommandOptions.HasFlag(GetObjectCommandOptions.ForDebuggerProxyAttribute) || (state is DebuggerBrowsableState.RootHidden && IsACollectionType(returnTypeName)))
                {
                    try
                    {
                        propRet = await sdbHelper.InvokeMethod(getterParamsBuffer, getMethodId, token, name: propNameWithSufix, isPropertyStatic && !isValueType);
                    }
                    catch (Exception)
                    {
                        propRet = GetNotAutoExpandableObject(getMethodId, propNameWithSufix, isPropertyStatic);
                    }
                }
                else
                {
                    propRet = GetNotAutoExpandableObject(getMethodId, propNameWithSufix, isPropertyStatic);
                }

                propRet["isOwn"] = isOwn;
                propRet[InternalUseFieldName.Section.Name] = getterAttrs switch
                {
                    MethodAttributes.Private => "private",
                    _ => "result"
                };
                propRet[InternalUseFieldName.State.Name] = state?.ToString();
                if (parentTypeId != -1)
                {
                    propRet[InternalUseFieldName.ParentTypeId.Name] = parentTypeId;
                    propRet[InternalUseFieldName.IsNewSlot.Name] = isNewSlot;
                }

                string namePrefix = GetNamePrefixForValues(propNameWithSufix, typeName, isOwn, state);
                var expandedMembers = await GetExpandedMemberValues(
                    sdbHelper, returnTypeName, namePrefix, propRet, state, includeStatic, token);
                foreach (var member in expandedMembers)
                {
                    var key = member["name"]?.Value<string>();
                    if (key != null)
                    {
                        allMembers.TryAdd(key, member as JObject);
                    }
                }
            }

            JObject GetNotAutoExpandableObject(int methodId, string propertyName, bool isStatic)
            {
                JObject methodIdArgs = JObject.FromObject(new
                {
                    isStatic = isStatic,
                    containerId = isStatic ? typeId : objectId.Value,
                    isValueType = isValueType,
                    methodId = methodId
                });

                return JObject.FromObject(new
                {
                    get = new
                    {
                        type = "function",
                        objectId = $"dotnet:method:{methodIdArgs.ToString(Newtonsoft.Json.Formatting.None)}",
                        className = "Function",
                        description = "get " + propertyName + " ()"
                    },
                    name = propertyName
                });
            }
        }

        public static async Task<GetMembersResult> GetObjectMemberValues(
            MonoSDBHelper sdbHelper,
            int objectId,
            GetObjectCommandOptions getCommandType,
            CancellationToken token,
            bool sortByAccessLevel = false,
            bool includeStatic = false)
        {
            if (await sdbHelper.IsDelegate(objectId, token))
            {
                var description = await sdbHelper.GetDelegateMethodDescription(objectId, token);
                var objValues = JObject.FromObject(new
                {
                    value = new
                    {
                        type = "symbol",
                        value = description,
                        description
                    },
                    name = "Target"
                });

                return GetMembersResult.FromValues(new List<JObject>() { objValues });
            }

            // 1
            var typeIdsIncludingParents = await sdbHelper.GetTypeIdsForObject(objectId, true, token);

            // 2
            if (!getCommandType.HasFlag(GetObjectCommandOptions.ForDebuggerDisplayAttribute))
            {
                GetMembersResult debuggerProxy = await sdbHelper.GetValuesFromDebuggerProxyAttributeForObject(
                    objectId, typeIdsIncludingParents[0], token);
                if (debuggerProxy != null)
                    return debuggerProxy;
            }

            // 3. GetProperties
            DotnetObjectId id = new DotnetObjectId("object", objectId);
            using var commandParamsObjWriter = new MonoBinaryWriter();
            commandParamsObjWriter.WriteObj(id, sdbHelper);
            ArraySegment<byte> getPropertiesParamBuffer = commandParamsObjWriter.GetParameterBuffer();

            var allMembers = new Dictionary<string, JObject>();
            int typeIdsCnt = typeIdsIncludingParents.Count;
            for (int i = 0; i < typeIdsCnt; i++)
            {
                int typeId = typeIdsIncludingParents[i];

                int parentTypeId = i + 1 < typeIdsCnt ? typeIdsIncludingParents[i + 1] : -1;
                string typeName = await sdbHelper.GetTypeName(typeId, token);
                // 0th id is for the object itself, and then its ancestors
                bool isOwn = i == 0;

                List<FieldTypeClass> thisTypeFields = await sdbHelper.GetTypeFields(typeId, token);
                if (!includeStatic)
                    thisTypeFields = thisTypeFields.Where(f => !f.Attributes.HasFlag(FieldAttributes.Static)).ToList();

                if (thisTypeFields.Count > 0)
                {
                    var allFields = await ExpandFieldValues(
                        sdbHelper, id, typeId, parentTypeId, thisTypeFields, getCommandType, isOwn, includeStatic, token);

                    if (getCommandType.HasFlag(GetObjectCommandOptions.AccessorPropertiesOnly))
                    {
                        foreach (var f in allFields)
                            f[InternalUseFieldName.Hidden.Name] = true;
                    }
                    AddOnlyNewFieldValuesByNameTo(allFields, allMembers, typeName, isOwn);
                }

                // skip loading properties if not necessary
                if (!getCommandType.HasFlag(GetObjectCommandOptions.WithProperties))
                    return GetMembersResult.FromValues(allMembers.Values, sortByAccessLevel);

                allMembers = await ExpandPropertyValues(
                    sdbHelper,
                    typeId,
                    typeName,
                    getPropertiesParamBuffer,
                    getCommandType,
                    id,
                    isValueType: false,
                    isOwn,
                    token,
                    allMembers,
                    includeStatic,
                    parentTypeId);

                // ownProperties
                // Note: ownProperties should mean that we return members of the klass itself,
                // but we are going to ignore that here, because otherwise vscode/chrome don't
                // seem to ask for inherited fields at all.
                //if (ownProperties)
                //break;
                /*if (accessorPropertiesOnly)
                    break;*/
            }

            return GetMembersResult.FromValues(allMembers.Values, sortByAccessLevel);

            static void AddOnlyNewFieldValuesByNameTo(JArray namedValues, IDictionary<string, JObject> valuesDict, string typeName, bool isOwn)
            {
                foreach (var item in namedValues)
                {
                    var name = item["name"]?.Value<string>();
                    if (name == null)
                        continue;

                    if (valuesDict.TryAdd(name, item as JObject))
                    {
                        // new member
                        if (item[InternalUseFieldName.IsBackingField.Name]?.Value<bool>() == true)
                            item[InternalUseFieldName.Owner.Name] = typeName;
                        continue;
                    }

                    if (isOwn)
                        throw new Exception($"Internal Error: found an existing member on own type. item: {item}, typeName: {typeName}");

                    var parentSuffix = typeName.Split('.')[^1];
                    var parentMemberName = $"{name} ({parentSuffix})";
                    valuesDict.Add(parentMemberName, item as JObject);
                    item["name"] = parentMemberName;
                }
            }
        }

    }

    internal sealed class GetMembersResult
    {
        // public / protected / internal:
        public JArray Result { get; set; }
        // private:
        public JArray PrivateMembers { get; set; }

        public JObject JObject => JObject.FromObject(new
        {
            result = Result,
            privateProperties = PrivateMembers
        });

        public GetMembersResult()
        {
            Result = new JArray();
            PrivateMembers = new JArray();
        }

        public GetMembersResult(JArray value, bool sortByAccessLevel)
        {
            var t = FromValues(value, sortByAccessLevel);
            Result = t.Result;
            PrivateMembers = t.PrivateMembers;
        }

        public void CleanUp()
        {
            JProperty[] toRemoveInObject = new JProperty[InternalUseFieldName.Count];

            CleanUpJArray(Result);
            CleanUpJArray(PrivateMembers);

            void CleanUpJArray(JArray arr)
            {
                foreach (JToken item in arr)
                {
                    if (item is not JObject jobj || jobj.Count == 0)
                        continue;

                    int removeCount = 0;
                    foreach (JProperty jp in jobj.Properties())
                    {
                        if (InternalUseFieldName.IsKnown(jp.Name))
                            toRemoveInObject[removeCount++] = jp;
                    }

                    for (int i = 0; i < removeCount; i++)
                        toRemoveInObject[i].Remove();
                }
            }
        }

        public static GetMembersResult FromValues(IEnumerable<JToken> values, bool splitMembersByAccessLevel = false) =>
            FromValues(new JArray(values), splitMembersByAccessLevel);

        public static GetMembersResult FromValues(JArray values, bool splitMembersByAccessLevel = false)
        {
            GetMembersResult result = new();
            if (splitMembersByAccessLevel)
            {
                foreach (var member in values)
                    result.Split(member);
                return result;
            }
            result.Result.AddRange(values);
            return result;
        }

        private void Split(JToken member)
        {
            if (member[InternalUseFieldName.Hidden.Name]?.Value<bool>() == true)
                return;

            if (member[InternalUseFieldName.Section.Name]?.Value<string>() is not string section)
            {
                Result.Add(member);
                return;
            }

            switch (section)
            {
                case "private":
                    PrivateMembers.Add(member);
                    return;
                default:
                    Result.Add(member);
                    return;
            }
        }

        public GetMembersResult Clone() => new GetMembersResult()
        {
            Result = (JArray)Result.DeepClone(),
            PrivateMembers = (JArray)PrivateMembers.DeepClone(),
        };

        public IEnumerable<JToken> Where(Func<JToken, bool> predicate)
        {
            foreach (var item in Result)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
            foreach (var item in PrivateMembers)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        internal JToken FirstOrDefault(Func<JToken, bool> p)
            => Result.FirstOrDefault(p)
            ?? PrivateMembers.FirstOrDefault(p);

        internal JArray Flatten()
        {
            var result = new JArray();
            result.AddRange(Result);
            result.AddRange(PrivateMembers);
            return result;
        }
        public override string ToString() => $"{JObject}\n";
    }
}
