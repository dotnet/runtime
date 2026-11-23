// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed partial class XmlObjectSerializerWriteContextComplexJson
{
    internal override Task WriteArraySizeAsync(XmlWriterDelegator writer, int size) => Task.CompletedTask;

    protected override Task WriteTypeInfoAsync(XmlWriterDelegator writer, string name, string? ns)
    {
        if (_emitXsiType == EmitTypeInformation.Never)
        {
            return Task.CompletedTask;
        }
        string information = string.IsNullOrEmpty(ns) ? name :
            string.Concat(name, JsonGlobals.NameValueSeparatorString, TruncateDefaultDataContractNamespace(ns));
        return writer.WriteAttributeStringAsync(null, JsonGlobals.serverTypeString, null, information);
    }

    protected override Task WriteTypeInfoAsync(XmlWriterDelegator writer, XmlDictionaryString name, XmlDictionaryString ns) =>
        WriteTypeInfoAsync(writer, name.Value, ns.Value);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    protected override async ValueTask<bool> WriteTypeInfoAsync(XmlWriterDelegator writer, DataContract contract, DataContract declaredContract)
    {
        bool different = !(ReferenceEquals(contract.Name, declaredContract.Name) && ReferenceEquals(contract.Namespace, declaredContract.Namespace)) &&
            !(contract.Name.Value == declaredContract.Name.Value && contract.Namespace.Value == declaredContract.Namespace.Value);
        if (different && contract.UnderlyingType != typeof(object[]) && _emitXsiType != EmitTypeInformation.Never)
        {
            if (RequiresJsonTypeInfo(contract))
            {
                _perCallXsiTypeAlreadyEmitted = true;
                await WriteTypeInfoAsync(writer, contract.Name.Value, contract.Namespace.Value).ConfigureAwait(false);
            }
            else if (declaredContract.UnderlyingType == typeof(Enum))
            {
                throw new SerializationException(SR.Format(SR.EnumTypeNotSupportedByDataContractJsonSerializer, declaredContract.UnderlyingType));
            }
            return true;
        }

        return false;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    protected override Task WriteDataContractValueAsync(DataContract contract, XmlWriterDelegator writer, object value, RuntimeTypeHandle declaredHandle)
    {
        JsonDataContract jsonContract = JsonDataContract.GetJsonDataContract(contract);
        if (_emitXsiType == EmitTypeInformation.Always && !_perCallXsiTypeAlreadyEmitted && RequiresJsonTypeInfo(contract))
        {
            return WriteTypeAndValueAsync(jsonContract, writer, value, declaredHandle);
        }
        _perCallXsiTypeAlreadyEmitted = false;
        return jsonContract.WriteJsonValueAsync(writer, value, this, declaredHandle, CancellationToken);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task WriteTypeAndValueAsync(JsonDataContract contract, XmlWriterDelegator writer, object value, RuntimeTypeHandle declaredHandle)
    {
        await writer.WriteAttributeStringAsync(null, JsonGlobals.serverTypeString, null, contract.TypeName).ConfigureAwait(false);
        _perCallXsiTypeAlreadyEmitted = false;
        await contract.WriteJsonValueAsync(writer, value, this, declaredHandle, CancellationToken).ConfigureAwait(false);
    }

    internal override Task WriteNullAsync(XmlWriterDelegator writer) =>
        writer.WriteAttributeStringAsync(null, JsonGlobals.typeString, null, JsonGlobals.nullString);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    protected override async Task SerializeWithXsiTypeAsync(XmlWriterDelegator writer, object value, RuntimeTypeHandle valueHandle, Type? valueType, int declaredId, RuntimeTypeHandle declaredHandle, Type declaredType)
    {
        DataContract contract;
        bool verify = false;
        bool isInterface = declaredType.IsInterface;
        if (isInterface && CollectionDataContract.IsCollectionInterface(declaredType) || declaredType.IsArray)
        {
            contract = GetDataContract(declaredHandle, declaredType);
        }
        else
        {
            contract = GetDataContract(valueHandle, valueType);
            DataContract declared = declaredId >= 0 ? GetDataContract(declaredId, declaredHandle) : GetDataContract(declaredHandle, declaredType);
            verify = await WriteTypeInfoAsync(writer, contract, declared).ConfigureAwait(false);
            HandleCollectionAssignedToObject(declaredType, ref contract, ref value, ref verify);
        }
        if (isInterface)
        {
            VerifyObjectCompatibilityWithInterface(contract, value, declaredType);
        }
        await SerializeAndVerifyTypeAsync(contract, writer, value, verify, declaredType.TypeHandle, declaredType).ConfigureAwait(false);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal override async Task SerializeWithXsiTypeAtTopLevelAsync(DataContract contract, XmlWriterDelegator writer, object value, RuntimeTypeHandle originalDeclaredHandle, Type graphType)
    {
        Type declaredType = rootTypeDataContract!.UnderlyingType;
        bool verify = false;
        bool isInterface = declaredType.IsInterface;
        if (!(isInterface && CollectionDataContract.IsCollectionInterface(declaredType)) && !declaredType.IsArray)
        {
            verify = await WriteTypeInfoAsync(writer, contract, rootTypeDataContract).ConfigureAwait(false);
            HandleCollectionAssignedToObject(declaredType, ref contract, ref value, ref verify);
        }
        if (isInterface)
        {
            VerifyObjectCompatibilityWithInterface(contract, value, declaredType);
        }
        await SerializeAndVerifyTypeAsync(contract, writer, value, verify, declaredType.TypeHandle, declaredType).ConfigureAwait(false);
    }

    internal static async Task WriteJsonNameWithMappingAsync(XmlWriterDelegator writer, XmlDictionaryString[] names, int index)
    {
        await writer.WriteStartElementAsync("a", JsonGlobals.itemString, JsonGlobals.itemString).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, JsonGlobals.itemString, null, names[index].Value).ConfigureAwait(false);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal override async Task WriteISerializableAsync(XmlWriterDelegator writer, ISerializable value)
    {
#pragma warning disable SYSLIB0050
        Type type = value.GetType();
        SerializationInfo info = new SerializationInfo(type, XmlObjectSerializer.FormatterConverter);
#pragma warning restore SYSLIB0050
        GetObjectData(value, info, GetStreamingContext());
        if (DataContract.GetClrTypeFullName(type) != info.FullTypeName)
        {
            throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ChangingFullTypeNameNotSupported, info.FullTypeName, DataContract.GetClrTypeFullName(type)));
        }
        await WriteSerializationInfoAsync(writer, type, info).ConfigureAwait(false);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal override async Task WriteExtensionDataTypeInfoAsync(XmlWriterDelegator writer, IDataNode node)
    {
        Type type = node.DataType;
        if (type == typeof(ClassDataNode) || type == typeof(ISerializableDataNode))
        {
            await writer.WriteAttributeStringAsync(null, JsonGlobals.typeString, null, JsonGlobals.objectString).ConfigureAwait(false);
            await base.WriteExtensionDataTypeInfoAsync(writer, node).ConfigureAwait(false);
        }
        else if (type == typeof(CollectionDataNode))
        {
            await writer.WriteAttributeStringAsync(null, JsonGlobals.typeString, null, JsonGlobals.arrayString).ConfigureAwait(false);
        }
        else if (type == typeof(object) && node.Value is object value && RequiresJsonTypeInfo(GetDataContract(value.GetType())))
        {
            await base.WriteExtensionDataTypeInfoAsync(writer, node).ConfigureAwait(false);
        }
    }
}
