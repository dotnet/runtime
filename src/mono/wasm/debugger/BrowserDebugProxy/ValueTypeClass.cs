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
    internal class ValueTypeClass
    {
        private readonly JArray fields;
        private readonly bool autoExpand;
        private JArray proxy;
        private GetMembersResult _combinedResult;
        // private GetMembersResult _combinedStaticResult;
        private bool propertiesExpanded;

        public string ClassName { get; init; }
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
            ClassName = className;
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
                                                CancellationToken token)
        {
            var typeInfo = await sdbAgent.GetTypeInfo(typeId, token);
            var typeFieldsBrowsableInfo = typeInfo?.Info?.DebuggerBrowsableFields;
            var typePropertiesBrowsableInfo = typeInfo?.Info?.DebuggerBrowsableProperties;

            IReadOnlyList<FieldTypeClass> fieldTypes = await sdbAgent.GetTypeFields(typeId, token);
            IEnumerable<FieldTypeClass> writableFields = fieldTypes.Where(f => !f.Attributes.HasFlag(FieldAttributes.Literal) && !f.Attributes.HasFlag(FieldAttributes.Static));

            // FIXME: save the field values buffer, and expand on demand
            int numWritableFields = writableFields.Count();

            // FIXME: add the static oens too? and tests for that! EvaluateOnCallFrame has some?
            JArray fields = new();
            foreach (var field in writableFields)
            {
                var fieldValue = await sdbAgent.CreateJObjectForVariableValue(cmdReader, field.Name, token, true, field.TypeId, false);

                //     state == DebuggerBrowsableState.Never)
                // {
                //     continue;
                // }

                fieldValue["__section"] = field.Attributes switch
                {
                    FieldAttributes.Private => "private",
                    FieldAttributes.Public => "result",
                    _ => "internal"
                };

                if (field.IsBackingField)
                    fieldValue["__isBackingField"] = true;

                // should be only when not backing
                string typeName = await sdbAgent.GetTypeName(field.TypeId, token);
                typeFieldsBrowsableInfo.TryGetValue(field.Name, out DebuggerBrowsableState? state);

                fieldValue["__state"] = state?.ToString();
                fields.Merge(await MemberObjectsExplorer.GetExpandedMemberValues(sdbAgent, typeName, field.Name, fieldValue, state, token));
            }

            long endPos = cmdReader.BaseStream.Position;
            cmdReader.BaseStream.Position = initialPos;
            byte[] valueTypeBuffer = new byte[endPos - initialPos];
            cmdReader.Read(valueTypeBuffer, 0, (int)(endPos - initialPos));
            cmdReader.BaseStream.Position = endPos;

            return new ValueTypeClass(valueTypeBuffer, className, fields, typeId, isEnum);
        }

        public async Task<JObject> ToJObject(MonoSDBHelper sdbAgent, bool forDebuggerDisplayAttribute, CancellationToken token)
        {
            string description = ClassName;
            if (ShouldAutoInvokeToString(ClassName) || IsEnum)
            {
                int methodId = await sdbAgent.GetMethodIdByName(TypeId, "ToString", token);
                var retMethod = await sdbAgent.InvokeMethod(Buffer, methodId, token);
                description = retMethod["value"]?["value"].Value<string>();
                if (ClassName.Equals("System.Guid"))
                    description = description.ToUpper(); //to keep the old behavior
            }
            else if (!forDebuggerDisplayAttribute)
            {
                string displayString = await sdbAgent.GetValueFromDebuggerDisplayAttribute(Id, TypeId, token);
                if (displayString != null)
                    description = displayString;
            }

            var obj = MonoSDBHelper.CreateJObject<string>(null, "object", description, false, ClassName, Id.ToString(), null, null, true, true, IsEnum);
            return obj;
        }

        public async Task<JArray> GetProxy(MonoSDBHelper sdbHelper, CancellationToken token)
        {
            if (proxy != null)
                return proxy;

            var retDebuggerCmdReader = await sdbHelper.GetTypePropertiesReader(TypeId, token);
            if (retDebuggerCmdReader == null)
                return null;

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

        // FIXME: this is flattening
        public async Task<GetMembersResult> GetMemberValues(MonoSDBHelper sdbHelper, GetObjectCommandOptions getObjectOptions, bool sortByAccessLevel, CancellationToken token)
        {
            // 1
            if (!propertiesExpanded)
            {
                await ExpandPropertyValues(sdbHelper, sortByAccessLevel, token);
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

        public async Task ExpandPropertyValues(MonoSDBHelper sdbHelper, bool splitMembersByAccessLevel, CancellationToken token)
        {

            using var commandParamsWriter = new MonoBinaryWriter();
            commandParamsWriter.Write(TypeId);
            using MonoBinaryReader getParentsReader = await sdbHelper.SendDebuggerAgentCommand(CmdType.GetParents, commandParamsWriter, token);
            int numParents = getParentsReader.ReadInt32();

            List<int> typesToGetProperties = new();
            typesToGetProperties.Add(TypeId);

            // FIXME: this list can be removed.. but also need to process for object's own typeId first
            for (int i = 0; i < numParents; i++)
                typesToGetProperties.Add(getParentsReader.ReadInt32());

            var allMembers = new Dictionary<string, JObject>();
            foreach (var f in fields)
                allMembers[f["name"].Value<string>()] = f as JObject;

            for (int i = 0; i < typesToGetProperties.Count; i++)
            {
                //FIXME: change GetNonAutomaticPropertyValues to return a jobject instead
                var res = await MemberObjectsExplorer.GetNonAutomaticPropertyValues(
                    sdbHelper,
                    typesToGetProperties[i],
                    ClassName,
                    Buffer,
                    autoExpand,
                    Id,
                    isValueType: true,
                    isOwn: i == 0,
                    token,
                    allMembers);
                foreach (var kvp in res)
                    allMembers[kvp.Key] = kvp.Value;
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
