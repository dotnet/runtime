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

        private static async Task<JObject> ReadFieldValue(MonoSDBHelper sdbHelper, MonoBinaryReader reader, FieldTypeClass field, int objectId, TypeInfoWithDebugInformation typeInfo, int fieldValueType, bool isOwn, GetObjectCommandOptions getObjectOptions, CancellationToken token)
        {
            var fieldValue = await sdbHelper.CreateJObjectForVariableValue(
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
                // for backing fields, we should get it from the properties
                typePropertiesBrowsableInfo.TryGetValue(field.Name, out state);
            }
            fieldValue["__state"] = state?.ToString();

            fieldValue["__section"] = field.Attributes switch
            {
                FieldAttributes.Private => "private",
                FieldAttributes.Public => "result",
                _ => "internal"
            };
            if (field.IsBackingField)
                fieldValue["__isBackingField"] = true;
            if (field.Attributes.HasFlag(FieldAttributes.Static))
                fieldValue["__isStatic"] = true;

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
                    fieldValueType,
                    length = length
                }));
            }

            return fieldValue;
        }

        private static async Task<JArray> GetRootHiddenChildren(MonoSDBHelper sdbHelper, JObject root, string rootNamePrefix, GetObjectCommandOptions getCommandOptions, CancellationToken token)
        {
            if (root?["value"]?["type"]?.Value<string>() != "object")
            {
                //Console.WriteLine ($"GetRootHiddenChildren: not an object: {root} - name: {rootNamePrefix}");
                return new JArray();
            }

            if (root?["value"]?["type"]?.Value<string>() == "object" && root?["value"]?["subtype"]?.Value<string>() == "null")
            {
                // FIXME: test for this
                //Console.WriteLine ($"GetRootHiddenChildren: null root named {rootNamePrefix}: {root}");
                return new JArray();
            }

            if (!DotnetObjectId.TryParse(root?["value"]?["objectId"]?.Value<string>(), out DotnetObjectId rootObjectId))
            {
                throw new Exception($"Cannot parse object id from {root} for {rootNamePrefix}");
                // return null;
            }

            // collections require extracting items to get inner values; items are of array type
            // arrays have "subtype": "array" field, collections don't
            // var subtype = root?["value"]?["subtype"];
            //var rootHiddenObjectIdInt = rootHiddenObjectId.Value;

            // DotnetObjectId rootObjectId = rootObjectId;
            //Console.WriteLine ($"GetRootHiddenChildren: rootName: {rootNamePrefix}: {root}");
            // FIXME: this just assumes that the first returned member will be an array `Items`!
            // if (subtype == null || subtype?.Value<string>() != "array")

            // FIXME: test for valuetype collection
            if (rootObjectId.Scheme is "object" or "valuetype")
            {
                GetMembersResult members = await GetObjectMemberValues(sdbHelper, rootObjectId.Value, getCommandOptions, token);
                //Console.WriteLine ($"GetObjectMemberValues on that returned: {members}");

                var memberNamedItems = members.Where(m => m["name"]?.Value<string>() == "Items").FirstOrDefault();
                if (memberNamedItems is not null &&
                    DotnetObjectId.TryParse(memberNamedItems["value"]?["objectId"]?.Value<string>(), out DotnetObjectId itemsObjectId) &&
                    itemsObjectId.Scheme == "array")
                {
                    rootObjectId = itemsObjectId;
                }
            }

            if (rootObjectId.Scheme == "array")
            {
                JArray resultValue = await sdbHelper.GetArrayValues(rootObjectId.Value, token);

                // root hidden item name has to be unique, so we concatenate the root's name to it
                foreach (var item in resultValue)
                    item["name"] = string.Concat(rootNamePrefix, "[", item["name"], "]");

                return resultValue;
            }
            else
            {
                return new JArray();
            }
        }

        private static async Task<JArray> GetExpandedFieldValues(
            MonoSDBHelper sdbHelper,
            int objectId,
            int containerTypeId,
            IReadOnlyList<FieldTypeClass> fields,
            GetObjectCommandOptions getCommandOptions,
            bool isOwn,
            CancellationToken token)
        {
            JArray fieldValues = new JArray();
            if (fields.Count == 0)
                return fieldValues;

            // Console.WriteLine ($"GetFieldsValues: ENTER for {objectId}");
            // FIXME: What is this doing? is there a test for it?
            if (getCommandOptions.HasFlag(GetObjectCommandOptions.ForDebuggerProxyAttribute))
                fields = fields.Where(field => field.IsNotPrivate).ToList();

            using var commandParamsWriter = new MonoBinaryWriter();
            commandParamsWriter.Write(objectId);
            commandParamsWriter.Write(fields.Count);
            foreach (var field in fields)
                commandParamsWriter.Write(field.Id);
            MonoBinaryReader retDebuggerCmdReader = //isValueType
                                                    // ? await SendDebuggerAgentCommand(CmdType.GetValues, commandParamsWriter, token)
                                await sdbHelper.SendDebuggerAgentCommand(CmdObject.RefGetValues, commandParamsWriter, token);

            // FIXME: needed only if there is a RootHidden field
            var typeInfo = await sdbHelper.GetTypeInfo(containerTypeId, token);

            int numFieldsRead = 0;
            foreach (FieldTypeClass field in fields)
            {
                long initialPos = retDebuggerCmdReader.BaseStream.Position;
                int valtype = retDebuggerCmdReader.ReadByte();
                retDebuggerCmdReader.BaseStream.Position = initialPos;

                JObject fieldValue = await ReadFieldValue(sdbHelper, retDebuggerCmdReader, field, objectId, typeInfo, valtype, isOwn, getCommandOptions, token);
                numFieldsRead++;

                if (!Enum.TryParse(fieldValue["__state"].Value<string>(), out DebuggerBrowsableState fieldState)
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
                
                var enumeratedValues = await GetRootHiddenChildren(sdbHelper, fieldValue, namePrefix, getCommandOptions, token);
                if (enumeratedValues != null)
                    fieldValues.AddRange(enumeratedValues);
            }

            if (numFieldsRead != fields.Count)
                throw new Exception($"Bug: Got {numFieldsRead} instead of expected {fields.Count} field values");

            return fieldValues;
        }

        public static Task<GetMembersResult> GetValueTypeMemberValues(MonoSDBHelper sdbHelper, int valueTypeId, GetObjectCommandOptions getCommandOptions, CancellationToken token, bool sortByAccessLevel = false)
        {
            if (!sdbHelper.valueTypes.TryGetValue(valueTypeId, out ValueTypeClass valueType))
                throw new ArgumentException($"Could not find any valuetype with id: {valueTypeId}", nameof(valueTypeId));

            return valueType.GetMemberValues(sdbHelper, getCommandOptions, sortByAccessLevel, token);
        }

        public static async Task<JArray> GetExpandedMemberValues(MonoSDBHelper sdbHelper, string typeName, string namePrefix, JObject value, DebuggerBrowsableState? state, CancellationToken token)
        {
            //Console.WriteLine ($"- GetExpandedMemberValues: {typeName}, {namePrefix}, {value}, state: {state}");
            if (state == DebuggerBrowsableState.RootHidden)
            {
                if (IsACollectionType(typeName))
                    return await GetRootHiddenChildren(sdbHelper, value, namePrefix, GetObjectCommandOptions.None, token);

                //Console.WriteLine ($"- AddMemberValue: adding hidden obj for {typeName} - {namePrefix} with value: {value}, state: {state}");
                return GetHiddenElement();
            }
            else if (state is DebuggerBrowsableState.Never)
            {
                return GetHiddenElement();
            }

            // existing.Add(value);
            return new JArray(value);

            JArray GetHiddenElement()
            {
                return new JArray(JObject.FromObject(new
                {
                    name = namePrefix,
                    __hidden = true
                }));
            }
        }

        public static async Task<GetMembersResult> GetNonAutomaticPropertyValues(
            MonoSDBHelper sdbHelper,
            int typeId,
            string containerTypeName,
            ArraySegment<byte> getterParamsBuffer,
            bool isAutoExpandable,
            DotnetObjectId objectId,
            bool isValueType,
            bool isOwn,
            CancellationToken token,
            IDictionary<string, JObject> allMembers,
            IReadOnlyList<FieldTypeClass> fields,
            bool includeStatic = false)
        {
            using var retDebuggerCmdReader = await sdbHelper.GetTypePropertiesReader(typeId, token);
            if (retDebuggerCmdReader == null)
                return null;

            var nProperties = retDebuggerCmdReader.ReadInt32();
            var typeInfo = await sdbHelper.GetTypeInfo(typeId, token);
            var typePropertiesBrowsableInfo = typeInfo?.Info?.DebuggerBrowsableProperties;

            GetMembersResult ret = new();
            for (int i = 0; i < nProperties; i++)
            {
                retDebuggerCmdReader.ReadInt32(); //propertyId
                string propName = retDebuggerCmdReader.ReadString();
                var getMethodId = retDebuggerCmdReader.ReadInt32();
                retDebuggerCmdReader.ReadInt32(); //setmethod
                var attrs = (PropertyAttributes)retDebuggerCmdReader.ReadInt32(); //attrs
                if (getMethodId == 0 || await sdbHelper.GetParamCount(getMethodId, token) != 0)// || await MethodIsStatic(getMethodId, token))
                    continue;
                if (!includeStatic && await sdbHelper.MethodIsStatic(getMethodId, token))
                    continue;

                MethodInfoWithDebugInformation getterInfo = await sdbHelper.GetMethodInfo(getMethodId, token);
                MethodAttributes getterAttrs = getterInfo?.Info.Attributes ?? MethodAttributes.Public;
                getterAttrs &= MethodAttributes.MemberAccessMask;

                typePropertiesBrowsableInfo.TryGetValue(propName, out DebuggerBrowsableState? state);

                // FIXME: if it's accessorPropertiesOnly, and we want to skip any auto-props, then we need the backing field
                // here to detect that.. or have access to the FieldTypeClasses
                if (allMembers.TryGetValue(propName, out JObject backingField))
                {
                    //Console.WriteLine ($"\tFound {propName} property is allMembers: {backingField}");
                    if (backingField["__isBackingField"]?.Value<bool>() == true)
                    {
                        // FIXME: this should be checked only for this type - add a typeID to it?
                        // Update backingField's access with the one from the property getter
                        backingField["__section"] = getterAttrs switch
                        {
                            MethodAttributes.Private => "private",
                            MethodAttributes.Public => "result",
                            _ => "internal"
                        };
                        backingField["__state"] = state?.ToString();

                        if (state is not null)
                        {
                            // FIXME: primitive types?
                            string namePrefix = GetNamePrefixForValues(propName, containerTypeName, isOwn, state);

                            string backingFieldTypeName = backingField["value"]?["className"]?.Value<string>();
                            var expanded = await GetExpandedMemberValues(sdbHelper, backingFieldTypeName, namePrefix, backingField, state, token);
                            backingField.Remove();
                            allMembers.Remove(propName);
                            foreach (JObject evalue in expanded)
                                allMembers[evalue["name"].Value<string>()] = evalue;
                        }
                    }

                    // derived type already had a member by this name
                    continue;
                }
                else if (fields?.Where(f => f.Name == propName && f.IsBackingField)?.Any() == true)
                {
                    //Console.WriteLine ($"\t* {propName} is a backing field.. not adding the corresponding getter");
                }
                else
                {
                    string returnTypeName = await sdbHelper.GetReturnType(getMethodId, token);
                    JObject propRet = null;
                    if (isAutoExpandable || (state is DebuggerBrowsableState.RootHidden && IsACollectionType(returnTypeName)))
                        propRet = await sdbHelper.InvokeMethod(getterParamsBuffer, getMethodId, token, name: propName);
                    else
                        propRet = GetNotAutoExpandableObject(getMethodId, propName);

                    if (isOwn)
                        propRet["isOwn"] = true;

                    propRet["__section"] = getterAttrs switch
                    {
                        MethodAttributes.Private => "private",
                        MethodAttributes.Public => "result",
                        _ => "internal"
                    };
                    propRet["__state"] = state?.ToString();

                    string namePrefix = GetNamePrefixForValues(propName, containerTypeName, isOwn, state);
                    var expandedMembers = await GetExpandedMemberValues(sdbHelper, returnTypeName, namePrefix, propRet, state, token);
                    ret.Result.AddRange(expandedMembers);
                }
            }
            return ret;

            JObject GetNotAutoExpandableObject(int methodId, string propertyName)
            {
                JObject methodIdArgs = JObject.FromObject(new
                {
                    containerId = objectId.Value,
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
                        description = "get " + propertyName + " ()",
                        //methodId = methodId,
                        //objectIdValue = objectIdStr
                    },
                    name = propertyName
                });
            }
        }


        public static async Task<GetMembersResult> GetObjectMemberValues(MonoSDBHelper sdbHelper, int objectId, GetObjectCommandOptions getCommandType, CancellationToken token, bool sortByAccessLevel = false, bool includeStatic = false)
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
                GetMembersResult debuggerProxy = await sdbHelper.GetValuesFromDebuggerProxyAttribute(objectId, typeIdsIncludingParents[0], token);
                if (debuggerProxy != null)
                    return debuggerProxy;
            }

            // 3. GetProperties

            DotnetObjectId id = new DotnetObjectId("object", objectId);
            using var commandParamsObjWriter = new MonoBinaryWriter();
            commandParamsObjWriter.WriteObj(id, sdbHelper);
            ArraySegment<byte> getPropertiesParamBuffer = commandParamsObjWriter.GetParameterBuffer();

            // FIXME: change to HashSet
            var allMembers = new Dictionary<string, JObject>();
            for (int i = 0; i < typeIdsIncludingParents.Count; i++)
            {
                int typeId = typeIdsIncludingParents[i];
                string typeName = await sdbHelper.GetTypeName(typeId, token);
                // 0th id is for the object itself, and then its parents
                bool isOwn = i == 0;
                IReadOnlyList<FieldTypeClass> thisTypeFields = await sdbHelper.GetTypeFields(typeId, token);
                if (!includeStatic)
                    thisTypeFields = thisTypeFields.Where(f => !f.Attributes.HasFlag(FieldAttributes.Static)).ToList();

                // case:
                // derived type overrides with an automatic property with a getter

                // if (!getCommandOptions.HasFlag(GetObjectCommandOptions.AccessorPropertiesOnly) && thisTypeFields.Count > 0)
                if (thisTypeFields.Count > 0)
                {
                    var allFields = await GetExpandedFieldValues(sdbHelper, objectId, typeId, thisTypeFields, getCommandType, isOwn: isOwn, token);

                    if (getCommandType.HasFlag(GetObjectCommandOptions.AccessorPropertiesOnly))
                    {
                        foreach (var f in allFields)
                            f["__hidden"] = true;
                    }
                    // AddOnlyNewValuesByNameTo(new JArray(allFields.Where(f => f["__isBackingField"]?.Value<bool>() == true)), allMembers);
                    // else
                    AddOnlyNewValuesByNameTo(allFields, allMembers);
                }

                // skip loading properties if not necessary
                if (!getCommandType.HasFlag(GetObjectCommandOptions.WithProperties))
                    return GetMembersResult.FromValues(allMembers.Values, sortByAccessLevel);

                GetMembersResult props = await GetNonAutomaticPropertyValues(
                    sdbHelper,
                    typeId,
                    typeName,
                    getPropertiesParamBuffer,
                    getCommandType.HasFlag(GetObjectCommandOptions.ForDebuggerProxyAttribute),
                    id,
                    isValueType: false,
                    isOwn,
                    token,
                    allMembers,
                    thisTypeFields);

                // TODO: merge with GetNonAuto*
                AddOnlyNewValuesByNameTo(props.Result, allMembers);

                // ownProperties
                // Note: ownProperties should mean that we return members of the klass itself,
                // but we are going to ignore that here, because otherwise vscode/chrome don't
                // seem to ask for inherited fields at all.
                //if (ownProperties)
                //break;
                /*if (accessorPropertiesOnly)
                    break;*/
            }

            // FIXME: SplitMemberValuesByProtectionLevel needs ProtectionLevel, and Name
            return GetMembersResult.FromValues(allMembers.Values, sortByAccessLevel);

            static void AddOnlyNewValuesByNameTo(JArray namedValues, IDictionary<string, JObject> valuesDict)//, IDictionary<string, FieldTypeClass> fields)
            {
                foreach (var item in namedValues)
                {
                    var key = item["name"]?.Value<string>();
                    if (key != null)
                    {
                        if (!valuesDict.TryAdd(key, item as JObject))
                        {
                            //Console.WriteLine($"- AddOnlyNewValuesByNameTo: skipping {key}");
                            //Logger.LogDebug($"- AddOnlyNewValuesByNameTo: skipping {key} = {item}");
                        }
                    }
                }
            }
        }

    }

    internal class GetMembersResult : IEnumerable
    {
        // public:
        public JArray Result { get; set; }
        // private:
        public JArray PrivateMembers { get; set; }
        // other:
        public JArray OtherMembers { get; set; }

        public JObject JObject => JObject.FromObject(new
        {
            result = Result,
            privateProperties = PrivateMembers,
            internalProperties = OtherMembers
        });

        public GetMembersResult()
        {
            Result = new JArray();
            PrivateMembers = new JArray();
            OtherMembers = new JArray();
        }

        public GetMembersResult(JArray value, bool IsFlattened = false)
        {
            Result = value;
            PrivateMembers = new JArray();
            OtherMembers = new JArray();
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
            if (member["__hidden"]?.Value<bool>() == true)
                return;

            if (member["__section"]?.Value<string>() is not string section)
            {
                Result.Add(member);
                return;
            }

            switch (section)
            {
                case "private":
                    PrivateMembers.Add(member);
                    return;
                case "internal":
                    OtherMembers.Add(member);
                    return;
                default:
                    Result.Add(member);
                    return;
            }
        }

        public GetMembersResult Clone() => new GetMembersResult() { Result = Result, PrivateMembers = PrivateMembers, OtherMembers = OtherMembers };

        public IEnumerator GetEnumerator() => Flatten().GetEnumerator();

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
            foreach (var item in OtherMembers)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        internal JToken FirstOrDefault(Func<JToken, bool> p) => Result.FirstOrDefault(p) ?? PrivateMembers.FirstOrDefault(p) ?? OtherMembers.FirstOrDefault(p);

        internal void Add(JObject thisObj) => Result.Add(thisObj);

        internal JArray Flatten()
        {
            var result = new JArray();
            result.AddRange(Result);
            result.AddRange(PrivateMembers);
            result.AddRange(OtherMembers);
            return result;
        }

        public override string ToString() => $"{JObject}\n";
    }
}
