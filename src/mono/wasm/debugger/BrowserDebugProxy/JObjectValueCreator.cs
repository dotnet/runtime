// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics;

internal class JObjectValueCreator
{
    private Dictionary<int, ValueTypeClass> _valueTypes = new();
    private Dictionary<int, PointerValue> _pointerValues = new();
    private readonly MonoSDBHelper _sdbAgent;
    private readonly ILogger _logger;

    public JObjectValueCreator(MonoSDBHelper sdbAgent, ILogger logger)
    {
        _sdbAgent = sdbAgent;
        _logger = logger;
    }

    public static JObject Create<T>(T value,
                             string type,
                             string description,
                             string className = null,
                             string objectId = null,
                             string subtype = null,
                             bool writable = false,
                             bool isValueType = false,
                             bool isEnum = false)
    {
        var ret = JObject.FromObject(new
        {
            value = new
            {
                type,
                value,
                description
            },
            writable
        });
        if (className != null)
            ret["value"]["className"] = className;
        if (objectId != null)
            ret["value"]["objectId"] = objectId;
        if (subtype != null)
            ret["value"]["subtype"] = subtype;
        if (isValueType)
            ret["value"]["isValueType"] = isValueType;
        if (isEnum)
            ret["value"]["isEnum"] = isEnum;
        return ret;
    }

    public static JObject CreateFromPrimitiveType(object v)
        => v switch
        {
            string s => Create(s, type: "string", description: s),
            char c => CreateJObjectForChar(Convert.ToInt32(c)),
            bool b => Create(b, type: "boolean", description: b ? "true" : "false", className: "System.Boolean"),

            decimal or float or double or
            byte or sbyte or
            short or ushort or
            int or uint or
            long or ulong
                => CreateJObjectForNumber(v),

            _ => null
        };

    public static JObject CreateNull(string className!!)
        => Create<object>(value: null,
                          type: "object",
                          description: className,
                          className: className,
                          subtype: "null");

    public async Task<JObject> ReadAsVariableValue(MonoBinaryReader retDebuggerCmdReader, string name, bool isOwn, int typeIdFromAttribute, bool forDebuggerDisplayAttribute, CancellationToken token)
    {
        long initialPos = retDebuggerCmdReader == null ? 0 : retDebuggerCmdReader.BaseStream.Position;
        ElementType etype = (ElementType)retDebuggerCmdReader.ReadByte();
        JObject ret = null;
        switch (etype)
        {
            case ElementType.I:
            case ElementType.U:
            case ElementType.Void:
            case (ElementType)ValueTypeId.VType:
            case (ElementType)ValueTypeId.FixedArray:
                ret = Create(value: "void", type: "void", description: "void");
                break;
            case ElementType.Boolean:
                {
                    var value = retDebuggerCmdReader.ReadInt32();
                    ret = CreateFromPrimitiveType(value == 1);
                    break;
                }
            case ElementType.I1:
                {
                    var value = retDebuggerCmdReader.ReadSByte();
                    ret = CreateJObjectForNumber<int>(value);
                    break;
                }
            case ElementType.I2:
            case ElementType.I4:
                {
                    var value = retDebuggerCmdReader.ReadInt32();
                    ret = CreateJObjectForNumber<int>(value);
                    break;
                }
            case ElementType.U1:
                {
                    var value = retDebuggerCmdReader.ReadUByte();
                    ret = CreateJObjectForNumber<int>(value);
                    break;
                }
            case ElementType.U2:
                {
                    var value = retDebuggerCmdReader.ReadUShort();
                    ret = CreateJObjectForNumber<int>(value);
                    break;
                }
            case ElementType.U4:
                {
                    var value = retDebuggerCmdReader.ReadUInt32();
                    ret = CreateJObjectForNumber<uint>(value);
                    break;
                }
            case ElementType.R4:
                {
                    float value = retDebuggerCmdReader.ReadSingle();
                    ret = CreateJObjectForNumber<float>(value);
                    break;
                }
            case ElementType.Char:
                {
                    var value = retDebuggerCmdReader.ReadInt32();
                    ret = CreateJObjectForChar(value);
                    break;
                }
            case ElementType.I8:
                {
                    long value = retDebuggerCmdReader.ReadInt64();
                    ret = CreateJObjectForNumber<long>(value);
                    break;
                }
            case ElementType.U8:
                {
                    ulong value = retDebuggerCmdReader.ReadUInt64();
                    ret = CreateJObjectForNumber<ulong>(value);
                    break;
                }
            case ElementType.R8:
                {
                    double value = retDebuggerCmdReader.ReadDouble();
                    ret = CreateJObjectForNumber<double>(value);
                    break;
                }
            case ElementType.FnPtr:
            case ElementType.Ptr:
                {
                    ret = await ReadAsPtrValue(etype, retDebuggerCmdReader, name, token);
                    break;
                }
            case ElementType.String:
                {
                    var stringId = retDebuggerCmdReader.ReadInt32();
                    string value = await _sdbAgent.GetStringValue(stringId, token);
                    ret = CreateFromPrimitiveType(value);
                    break;
                }
            case ElementType.SzArray:
            case ElementType.Array:
                {
                    ret = await ReadAsArray(retDebuggerCmdReader, token);
                    break;
                }
            case ElementType.Class:
            case ElementType.Object:
                {
                    ret = await ReadAsObjectValue(retDebuggerCmdReader, typeIdFromAttribute, forDebuggerDisplayAttribute, token);
                    break;
                }
            case ElementType.ValueType:
                {
                    ret = await ReadAsValueType(retDebuggerCmdReader, name, initialPos, token);
                    break;
                }
            case (ElementType)ValueTypeId.Null:
                {
                    var className = await GetNullObjectClassName();
                    ret = CreateNull(className);
                    break;
                }
            case (ElementType)ValueTypeId.Type:
                {
                    retDebuggerCmdReader.ReadInt32();
                    break;
                }
            default:
                {
                    _logger.LogDebug($"Could not evaluate CreateJObjectForVariableValue invalid type {etype}");
                    break;
                }
        }
        if (ret != null)
        {
            if (isOwn)
                ret["isOwn"] = true;
            ret["name"] = name;
        }
        return ret;

        async Task<string> GetNullObjectClassName()
        {
            string className;
            ElementType variableType = (ElementType)retDebuggerCmdReader.ReadByte();
            switch (variableType)
            {
                case ElementType.String:
                case ElementType.Class:
                    {
                        var type_id = retDebuggerCmdReader.ReadInt32();
                        className = await _sdbAgent.GetTypeName(type_id, token);
                        break;

                    }
                case ElementType.SzArray:
                case ElementType.Array:
                    {
                        ElementType byte_type = (ElementType)retDebuggerCmdReader.ReadByte();
                        retDebuggerCmdReader.ReadInt32(); // rank
                        if (byte_type == ElementType.Class)
                        {
                            retDebuggerCmdReader.ReadInt32(); // internal_type_id
                        }
                        var type_id = retDebuggerCmdReader.ReadInt32();
                        className = await _sdbAgent.GetTypeName(type_id, token);
                        break;
                    }
                default:
                    {
                        var type_id = retDebuggerCmdReader.ReadInt32();
                        className = await _sdbAgent.GetTypeName(type_id, token);
                        break;
                    }
            }
            return className;
        }
    }

    private async Task<JObject> ReadAsObjectValue(MonoBinaryReader retDebuggerCmdReader, int typeIdFromAttribute, bool forDebuggerDisplayAttribute, CancellationToken token)
    {
        var objectId = retDebuggerCmdReader.ReadInt32();
        var type_id = await _sdbAgent.GetTypeIdFromObject(objectId, false, token);
        string className = await _sdbAgent.GetTypeName(type_id[0], token);
        string debuggerDisplayAttribute = null;
        if (!forDebuggerDisplayAttribute)
            debuggerDisplayAttribute = await _sdbAgent.GetValueFromDebuggerDisplayAttribute(objectId, type_id[0], token);
        var description = className.ToString();

        if (debuggerDisplayAttribute != null)
            description = debuggerDisplayAttribute;

        if (await _sdbAgent.IsDelegate(objectId, token))
        {
            if (typeIdFromAttribute != -1)
            {
                className = await _sdbAgent.GetTypeName(typeIdFromAttribute, token);
            }

            description = await _sdbAgent.GetDelegateMethodDescription(objectId, token);
            if (description == "")
            {
                return Create(value: className, type: "symbol", description: className);
            }
        }
        return Create<object>(value: null, type: "object", description: description, className: className, objectId: $"dotnet:object:{objectId}");
    }

    public async Task<JObject> ReadAsValueType(MonoBinaryReader retDebuggerCmdReader, string name, long initialPos, CancellationToken token)
    {
        JObject fieldValueType = null;
        var isEnum = retDebuggerCmdReader.ReadByte();
        var isBoxed = retDebuggerCmdReader.ReadByte() == 1;
        var typeId = retDebuggerCmdReader.ReadInt32();
        var className = await _sdbAgent.GetTypeName(typeId, token);
        var description = className;
        var numFields = retDebuggerCmdReader.ReadInt32();
        var fields = await _sdbAgent.GetTypeFields(typeId, token);
        JArray valueTypeFields = new JArray();
        if (className.IndexOf("System.Nullable<") == 0) //should we call something on debugger-agent to check???
        {
            retDebuggerCmdReader.ReadByte(); //ignoring the boolean type
            var isNull = retDebuggerCmdReader.ReadInt32();
            var value = await ReadAsVariableValue(retDebuggerCmdReader, name, false, -1, false, token);
            if (isNull != 0)
                return value;
            else
                return Create<object>(null, "object", className, className, subtype: "null", isValueType: true);
        }
        for (int i = 0; i < numFields; i++)
        {
            fieldValueType = await ReadAsVariableValue(retDebuggerCmdReader, fields.ElementAt(i).Name, true, fields.ElementAt(i).TypeId, false, token);
            valueTypeFields.Add(fieldValueType);
        }

        long endPos = retDebuggerCmdReader.BaseStream.Position;
        var valueTypeId = MonoSDBHelper.GetNextDebuggerObjectId();

        retDebuggerCmdReader.BaseStream.Position = initialPos;
        byte[] valueTypeBuffer = new byte[endPos - initialPos];
        retDebuggerCmdReader.Read(valueTypeBuffer, 0, (int)(endPos - initialPos));
        retDebuggerCmdReader.BaseStream.Position = endPos;
        _valueTypes[valueTypeId] = new ValueTypeClass(name, valueTypeBuffer, valueTypeFields, typeId, AutoExpandable(className), valueTypeId);
        if (AutoInvokeToString(className) || isEnum == 1)
        {
            int methodId = await _sdbAgent.GetMethodIdByName(typeId, "ToString", token);
            var retMethod = await _sdbAgent.InvokeMethod(valueTypeBuffer, methodId, "methodRet", token);
            description = retMethod["value"]?["value"].Value<string>();
            if (className.Equals("System.Guid"))
                description = description.ToUpper(); //to keep the old behavior
        }
        else if (isBoxed && numFields == 1)
        {
            return fieldValueType;
        }
        return Create<string>(value: null,
                              type: "object",
                              description: description,
                              className: className,
                              objectId: $"dotnet:valuetype:{valueTypeId}",
                              isValueType: true,
                              isEnum: isEnum == 1);

        static bool AutoExpandable(string className)
            => className is "System.DateTime" or
                "System.DateTimeOffset" or
                "System.TimeSpan";

        static bool AutoInvokeToString(string className)
            => className is "System.DateTime" or
                "System.DateTimeOffset" or
                "System.TimeSpan" or
                "System.Decimal" or
                "System.Guid";
    }

    public void ClearCache()
    {
        _valueTypes = new Dictionary<int, ValueTypeClass>();
        _pointerValues = new Dictionary<int, PointerValue>();
    }

    public ValueTypeClass GetValueTypeById(int valueTypeId) => _valueTypes.TryGetValue(valueTypeId, out ValueTypeClass vt) ? vt : null;
    public PointerValue GetPointerValue(int pointerId) => _pointerValues.TryGetValue(pointerId, out PointerValue pv) ? pv : null;

    private static JObject CreateJObjectForNumber<T>(T value) => Create(value, "number", value.ToString(), writable: true);

    private static JObject CreateJObjectForChar(int value)
    {
        char charValue = Convert.ToChar(value);
        var description = $"{value} '{charValue}'";
        return Create(charValue, "symbol", description, writable: true);
    }

    private async Task<JObject> ReadAsPtrValue(ElementType etype, MonoBinaryReader retDebuggerCmdReader, string name, CancellationToken token)
    {
        string type;
        string value;
        long valueAddress = retDebuggerCmdReader.ReadInt64();
        var typeId = retDebuggerCmdReader.ReadInt32();
        string className;
        if (etype == ElementType.FnPtr)
            className = "(*())"; //to keep the old behavior
        else
            className = "(" + await _sdbAgent.GetTypeName(typeId, token) + ")";

        int pointerId = 0;
        if (valueAddress != 0 && className != "(void*)")
        {
            pointerId = MonoSDBHelper.GetNextDebuggerObjectId();
            type = "object";
            value = className;
            _pointerValues[pointerId] = new PointerValue(valueAddress, typeId, name);
        }
        else
        {
            type = "symbol";
            value = className + " " + valueAddress;
        }
        return Create(value: value, type: type, description: value, className: className, objectId: $"dotnet:pointer:{pointerId}", subtype: "pointer");
    }

    private async Task<JObject> ReadAsArray(MonoBinaryReader retDebuggerCmdReader, CancellationToken token)
    {
        var objectId = retDebuggerCmdReader.ReadInt32();
        var className = await _sdbAgent.GetClassNameFromObject(objectId, token);
        var arrayType = className.ToString();
        var length = await _sdbAgent.GetArrayDimensions(objectId, token);
        if (arrayType.LastIndexOf('[') > 0)
            arrayType = arrayType.Insert(arrayType.LastIndexOf('[') + 1, length.ToString());
        if (className.LastIndexOf('[') > 0)
            className = className.Insert(arrayType.LastIndexOf('[') + 1, new string(',', length.Rank - 1));
        return Create<object>(value: null,
                              type: "object",
                              description: arrayType,
                              className: className.ToString(),
                              objectId: "dotnet:array:" + objectId,
                              subtype: length.Rank == 1 ? "array" : null);
    }
}
