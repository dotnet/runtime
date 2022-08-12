// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;

namespace BrowserDebugProxy
{
    internal sealed class ValueTypeClass
    {
        private readonly bool autoExpand;
        private JArray proxy;
        private GetMembersResult _combinedResult;
        private bool propertiesExpanded;
        private bool fieldsExpanded;
        private string className;
        private JArray fields;

        public DotnetObjectId Id { get; init; }
        public byte[] Buffer { get; init; }
        public int TypeId { get; init; }
        public bool IsEnum { get; init; }

        public ValueTypeClass(byte[] buffer, string className, JArray fields, int typeId, bool isEnum)
        {
            var valueTypeId = MonoSDBHelper.GetNewObjectId();
            var objectId = new DotnetObjectId("valuetype", valueTypeId);

            Buffer = buffer;
            this.fields = fields;
            this.className = className;
            TypeId = typeId;
            autoExpand = ShouldAutoExpand(className);
            Id = objectId;
            IsEnum = isEnum;
        }

        public override string ToString() => $"{{ ValueTypeClass: typeId: {TypeId}, Id: {Id}, Id: {Id}, fields: {fields} }}";

        public static async Task<ValueTypeClass> CreateFromReader(
                                                MonoSDBHelper sdbAgent,
                                                MonoBinaryReader cmdReader,
                                                long initialPos,
                                                string className,
                                                int typeId,
                                                int numValues,
                                                bool isEnum,
                                                bool includeStatic,
                                                CancellationToken token)
        {
            var typeInfo = await sdbAgent.GetTypeInfo(typeId, token);
            var typeFieldsBrowsableInfo = typeInfo?.Info?.DebuggerBrowsableFields;
            var typePropertiesBrowsableInfo = typeInfo?.Info?.DebuggerBrowsableProperties;

            IReadOnlyList<FieldTypeClass> fieldTypes = await sdbAgent.GetTypeFields(typeId, token);

            JArray fields = new();
            if (includeStatic)
            {
                IEnumerable<FieldTypeClass> staticFields =
                    fieldTypes.Where(f => f.Attributes.HasFlag(FieldAttributes.Static));
                foreach (var field in staticFields)
                {
                    var fieldValue = await sdbAgent.GetFieldValue(typeId, field.Id, token);
                    fields.Add(GetFieldWithMetadata(field, fieldValue, isStatic: true));
                }
            }

            IEnumerable<FieldTypeClass> writableFields = fieldTypes
                .Where(f => !f.Attributes.HasFlag(FieldAttributes.Literal)
                    && !f.Attributes.HasFlag(FieldAttributes.Static));

            foreach (var field in writableFields)
            {
                var fieldValue = await sdbAgent.ValueCreator.ReadAsVariableValue(cmdReader, field.Name, token, true, field.TypeId, false);
                fields.Add(GetFieldWithMetadata(field, fieldValue, isStatic: false));
            }

            long endPos = cmdReader.BaseStream.Position;
            cmdReader.BaseStream.Position = initialPos;
            byte[] valueTypeBuffer = new byte[endPos - initialPos];
            cmdReader.Read(valueTypeBuffer, 0, (int)(endPos - initialPos));
            cmdReader.BaseStream.Position = endPos;

            return new ValueTypeClass(valueTypeBuffer, className, fields, typeId, isEnum);

            JObject GetFieldWithMetadata(FieldTypeClass field, JObject fieldValue, bool isStatic)
            {
                // GetFieldValue returns JObject without name and we need this information
                if (isStatic)
                    fieldValue["name"] = field.Name;
                FieldAttributes attr = field.Attributes & FieldAttributes.FieldAccessMask;
                fieldValue["__section"] = attr == FieldAttributes.Public
                    ? "public" :
                    attr == FieldAttributes.Private ? "private" : "internal";

                if (field.IsBackingField)
                {
                    fieldValue["__isBackingField"] = true;
                    return fieldValue;
                }
                typeFieldsBrowsableInfo.TryGetValue(field.Name, out DebuggerBrowsableState? state);
                fieldValue["__state"] = state?.ToString();
                return fieldValue;
            }
        }

        public async Task<JObject> ToJObject(MonoSDBHelper sdbAgent, bool forDebuggerDisplayAttribute, CancellationToken token)
        {
            string description = className;
            if (ShouldAutoInvokeToString(className) || IsEnum)
            {
                int[] methodIds = await sdbAgent.GetMethodIdsByName(TypeId, "ToString", token);
                if (methodIds == null)
                    throw new InternalErrorException($"Cannot find method 'ToString' on typeId = {TypeId}");
                var retMethod = await sdbAgent.InvokeMethod(Buffer, methodIds[0], token, "methodRet");
                description = retMethod["value"]?["value"].Value<string>();
                if (className.Equals("System.Guid"))
                    description = description.ToUpperInvariant(); //to keep the old behavior
            }
            else if (!forDebuggerDisplayAttribute)
            {
                string displayString = await sdbAgent.GetValueFromDebuggerDisplayAttribute(Id, TypeId, token);
                if (displayString != null)
                    description = displayString;
            }
            return JObjectValueCreator.Create(
                IsEnum ? fields[0]["value"] : null,
                "object",
                description,
                className,
                Id.ToString(),
                isValueType: true,
                isEnum: IsEnum);
        }

        public async Task<JArray> GetProxy(MonoSDBHelper sdbHelper, CancellationToken token)
        {
            if (proxy != null)
                return proxy;

            var retDebuggerCmdReader = await sdbHelper.GetTypePropertiesReader(TypeId, token);
            if (retDebuggerCmdReader == null)
                return null;

            if (!fieldsExpanded)
            {
                await ExpandedFieldValues(sdbHelper, includeStatic: false, token);
                fieldsExpanded = true;
            }
            proxy = new JArray(fields);

            var nProperties = retDebuggerCmdReader.ReadInt32();

            for (int i = 0; i < nProperties; i++)
            {
                retDebuggerCmdReader.ReadInt32(); //propertyId
                string propertyNameStr = retDebuggerCmdReader.ReadString();

                var getMethodId = retDebuggerCmdReader.ReadInt32();
                retDebuggerCmdReader.ReadInt32(); //setmethod
                retDebuggerCmdReader.ReadInt32(); //attrs
                if (await sdbHelper.MethodIsStatic(getMethodId, token))
                    continue;
                using var command_params_writer_to_proxy = new MonoBinaryWriter();
                command_params_writer_to_proxy.Write(getMethodId);
                command_params_writer_to_proxy.Write(Buffer);
                command_params_writer_to_proxy.Write(0);

                var (data, length) = command_params_writer_to_proxy.ToBase64();
                proxy.Add(JObject.FromObject(new
                {
                    get = JObject.FromObject(new
                    {
                        commandSet = CommandSet.Vm,
                        command = CmdVM.InvokeMethod,
                        buffer = data,
                        length = length,
                        id = MonoSDBHelper.GetNewId()
                    }),
                    name = propertyNameStr
                }));
            }
            return proxy;
        }

        public async Task<GetMembersResult> GetMemberValues(
            MonoSDBHelper sdbHelper, GetObjectCommandOptions getObjectOptions, bool sortByAccessLevel, bool includeStatic, CancellationToken token)
        {
            // 1
            if (!propertiesExpanded)
            {
                await ExpandPropertyValues(sdbHelper, sortByAccessLevel, includeStatic, token);
                propertiesExpanded = true;
            }

            // 2
            GetMembersResult result = null;
            if (!getObjectOptions.HasFlag(GetObjectCommandOptions.ForDebuggerDisplayAttribute))
            {
                // FIXME: cache?
                result = await sdbHelper.GetValuesFromDebuggerProxyAttribute(Id.Value, TypeId, token);
                if (result != null)
                    Console.WriteLine($"Investigate GetValuesFromDebuggerProxyAttribute\n{result}. There was a change of logic from loop to one iteration");
            }

            if (result == null && getObjectOptions.HasFlag(GetObjectCommandOptions.AccessorPropertiesOnly))
            {
                // 3 - just properties, skip fields
                result = _combinedResult.Clone();
                RemovePropertiesFrom(result.Result);
                RemovePropertiesFrom(result.PrivateMembers);
                RemovePropertiesFrom(result.OtherMembers);
            }

            if (result == null)
            {
                // 4 - fields + properties
                result = _combinedResult.Clone();
            }

            return result;

            static void RemovePropertiesFrom(JArray collection)
            {
                List<JToken> toRemove = new();
                foreach (JToken jt in collection)
                {
                    if (jt is not JObject obj || obj["get"] != null)
                        continue;
                    toRemove.Add(jt);
                }
                foreach (var jt in toRemove)
                {
                    collection.Remove(jt);
                }
            }
        }

        public async Task ExpandedFieldValues(MonoSDBHelper sdbHelper, bool includeStatic, CancellationToken token)
        {
            JArray visibleFields = new();
            foreach (JObject field in fields)
            {
                if (!Enum.TryParse(field["__state"]?.Value<string>(), out DebuggerBrowsableState state))
                {
                    visibleFields.Add(field);
                    continue;
                }
                var fieldValue = field["value"] ?? field["get"];
                string typeName = fieldValue?["className"]?.Value<string>();
                JArray fieldMembers = await MemberObjectsExplorer.GetExpandedMemberValues(
                    sdbHelper, typeName, field["name"]?.Value<string>(), field, state, includeStatic, token);
                visibleFields.AddRange(fieldMembers);
            }
            fields = visibleFields;
        }

        public async Task ExpandPropertyValues(MonoSDBHelper sdbHelper, bool splitMembersByAccessLevel, bool includeStatic, CancellationToken token)
        {
            using var commandParamsWriter = new MonoBinaryWriter();
            commandParamsWriter.Write(TypeId);
            using MonoBinaryReader getParentsReader = await sdbHelper.SendDebuggerAgentCommand(CmdType.GetParents, commandParamsWriter, token);
            int numParents = getParentsReader.ReadInt32();

            if (!fieldsExpanded)
            {
                await ExpandedFieldValues(sdbHelper, includeStatic, token);
                fieldsExpanded = true;
            }

            var allMembers = new Dictionary<string, JObject>();
            foreach (var f in fields)
                allMembers[f["name"].Value<string>()] = f as JObject;

            int typeId = TypeId;
            var parentsCntPlusSelf = numParents + 1;
            for (int i = 0; i < parentsCntPlusSelf; i++)
            {
                // isParent:
                if (i != 0) typeId = getParentsReader.ReadInt32();

                allMembers = await MemberObjectsExplorer.ExpandPropertyValues(
                    sdbHelper,
                    typeId,
                    className,
                    Buffer,
                    autoExpand,
                    Id,
                    isValueType: true,
                    isOwn: i == 0,
                    token,
                    allMembers,
                    includeStatic);
            }
            _combinedResult = GetMembersResult.FromValues(allMembers.Values, splitMembersByAccessLevel);
        }

        private static bool ShouldAutoExpand(string className)
            => className is "System.DateTime" or
            "System.DateTimeOffset" or
            "System.TimeSpan";

        private static bool ShouldAutoInvokeToString(string className)
            => className is "System.DateTime" or
            "System.DateTimeOffset" or
            "System.TimeSpan" or
            "System.Decimal" or
            "System.Guid";
    }
}
