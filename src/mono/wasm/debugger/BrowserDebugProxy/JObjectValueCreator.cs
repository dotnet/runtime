// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrowserDebugProxy;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Microsoft.WebAssembly.Diagnostics;

internal sealed class JObjectValueCreator
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

    public static JObject CreateFromPrimitiveType(object v, int? stringId = null)
        => v switch
        {
            string s => Create(s, type: "string", description: s, objectId: $"dotnet:object:{stringId}"),
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

    public static JObject CreateNull(string className)
    {
        ArgumentNullException.ThrowIfNull(className);
        return Create<object>(value: null,
                          type: "object",
                          description: className,
                          className: className,
                          subtype: "null");
    }

    public async Task<JObject> ReadAsVariableValue(
        MonoBinaryReader retDebuggerCmdReader,
        string name,
        CancellationToken token,
        bool isOwn = false,
        int typeIdForObject = -1,
        bool forDebuggerDisplayAttribute = false,
        bool includeStatic = false)
    {
        long initialPos =  /*retDebuggerCmdReader == null ? 0 : */retDebuggerCmdReader.BaseStream.Position;
        ElementType etype = (ElementType)retDebuggerCmdReader.ReadByte();
        JObject ret = null;
        switch (etype)
        {
            case ElementType.I:
            case ElementType.U:
            case ElementType.Void:
            case (ElementType)ValueTypeId.VType:
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
                    ret = CreateFromPrimitiveType(value, stringId);
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
                    ret = await ReadAsObjectValue(retDebuggerCmdReader, typeIdForObject, forDebuggerDisplayAttribute, token);
                    break;
                }
            case ElementType.ValueType:
                {
                    ret = await ReadAsValueType(retDebuggerCmdReader, name, initialPos, forDebuggerDisplayAttribute, includeStatic, token);
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
        var typeIds = await _sdbAgent.GetTypeIdsForObject(objectId, withParents: true, token);
        string className = await _sdbAgent.GetTypeName(typeIds[0], token);
        string debuggerDisplayAttribute = null;
        if (!forDebuggerDisplayAttribute)
            debuggerDisplayAttribute = await _sdbAgent.GetValueFromDebuggerDisplayAttribute(
                new DotnetObjectId("object", objectId), typeIds[0], token);
        var description = className.ToString();

        if (debuggerDisplayAttribute != null)
        {
            description = debuggerDisplayAttribute;
        }
        else if (await _sdbAgent.IsDelegate(objectId, token))
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
        else
        {
            var toString = await _sdbAgent.InvokeToStringAsync(typeIds, isValueType: false, isEnum: false, objectId, BindingFlags.DeclaredOnly, invokeToStringInObject: false, token);
            if (toString != null)
                description = toString;
        }
        return Create<object>(value: null, type: "object", description: description, className: className, objectId: $"dotnet:object:{objectId}");
    }

    public async Task<JObject> ReadAsValueType(
        MonoBinaryReader retDebuggerCmdReader,
        string name,
        long initialPos,
        bool forDebuggerDisplayAttribute,
        bool includeStatic,
        CancellationToken token)
    {
        // FIXME: debugger proxy
        var isEnum = retDebuggerCmdReader.ReadByte() == 1;
        var isBoxed = retDebuggerCmdReader.ReadByte() == 1;
        var typeId = retDebuggerCmdReader.ReadInt32();
        var className = await _sdbAgent.GetTypeName(typeId, token);
        var inlineArraySize = -1;
        (int MajorVersion, int MinorVersion) = await _sdbAgent.GetVMVersion(token);
        if (MajorVersion == 2 && MinorVersion >= 65)
            inlineArraySize = retDebuggerCmdReader.ReadInt32();
        var numValues = retDebuggerCmdReader.ReadInt32();

        if (className.StartsWith("System.Nullable<", StringComparison.Ordinal)) //should we call something on debugger-agent to check???
        {
            retDebuggerCmdReader.ReadByte(); //ignoring the boolean type
            var isNull = retDebuggerCmdReader.ReadInt32();

            // Read the value, even if isNull==true, to correctly advance the reader
            var value = await ReadAsVariableValue(retDebuggerCmdReader, name, token);
            if (isNull != 0)
                return value;
            else
                return Create<object>(null, "object", className, className, subtype: "null", isValueType: true);
        }
        if (isBoxed && numValues == 1)
        {
            if (MonoSDBHelper.IsPrimitiveType(className))
            {
                return await ReadAsVariableValue(retDebuggerCmdReader, name: null, token);
            }
        }

        ValueTypeClass valueType = await ValueTypeClass.CreateFromReader(
                                                    _sdbAgent,
                                                    retDebuggerCmdReader,
                                                    initialPos,
                                                    className,
                                                    typeId,
                                                    isEnum,
                                                    includeStatic,
                                                    inlineArraySize,
                                                    token);
        _valueTypes[valueType.Id.Value] = valueType;
        return await valueType.ToJObject(_sdbAgent, forDebuggerDisplayAttribute, token);
    }
    public void ClearCache()
    {
        _valueTypes = new Dictionary<int, ValueTypeClass>();
        _pointerValues = new Dictionary<int, PointerValue>();
    }

    public bool TryGetValueTypeById(int valueTypeId, out ValueTypeClass vt) => _valueTypes.TryGetValue(valueTypeId, out vt);
    public PointerValue GetPointerValue(int pointerId) => _pointerValues.TryGetValue(pointerId, out PointerValue pv) ? pv : null;

    private static JObject CreateJObjectForNumber<T>(T value) => Create(value, "number", value.ToString(), writable: true, className: typeof(T).Name);

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

    public async Task<JObject> CreateFixedArrayElement(MonoBinaryReader retDebuggerCmdReader, ElementType etype, string name, CancellationToken token)
    {
        JObject ret = null;
        switch (etype)
        {
            case ElementType.I:
            case ElementType.U:
            case ElementType.Void:
            case (ElementType)ValueTypeId.VType:
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
            default:
                {
                    _logger.LogDebug($"Could not evaluate CreateFixedArrayElement invalid type {etype}");
                    break;
                }
        }
        ret["name"] = name;
        return ret;
    }
}
