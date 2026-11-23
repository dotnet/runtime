// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization;

internal partial class XmlObjectSerializerReadContext
{
    internal void InitializeAsync(XmlReaderDelegator reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationToken = cancellationToken;
        reader.CancellationToken = cancellationToken;
        attributes ??= new Attributes();
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal ValueTask<object?> ReadIXmlSerializableAsync(XmlReaderDelegator reader, XmlDataContract contract, bool isMemberType) =>
        AsyncXmlSerializableServices.ReadAsync(reader, contract, isMemberType, CancellationToken);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal static ValueTask<object?> ReadRootIXmlSerializableAsync(XmlReaderDelegator reader, XmlDataContract contract, bool isMemberType, CancellationToken cancellationToken) =>
        AsyncXmlSerializableServices.ReadAsync(reader, contract, isMemberType, cancellationToken);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal virtual ValueTask<object?> InternalDeserializeAsync(XmlReaderDelegator reader, int id, RuntimeTypeHandle declaredHandle, string name, string ns) =>
        InternalDeserializeAsync(reader, Type.GetTypeFromHandle(declaredHandle)!, GetDataContract(id, declaredHandle), name, ns);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal ValueTask<object?> InternalDeserializeAsync(XmlReaderDelegator reader, Type declaredType, string? name, string? ns) =>
        InternalDeserializeAsync(reader, declaredType, null, name, ns);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal virtual ValueTask<object?> InternalDeserializeAsync(XmlReaderDelegator reader, Type declaredType, DataContract? contract, string? name, string? ns)
        => DeserializeDataContractAsync(reader, declaredType, contract ?? GetDataContract(declaredType), name, ns, out _);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private protected ValueTask<object?> DeserializeDataContractAsync(XmlReaderDelegator reader, Type declaredType, DataContract contract,
        string? name, string? ns, out DataContract resolvedContract)
    {
        CancellationToken.ThrowIfCancellationRequested();
        ReadAttributes(reader);
        resolvedContract = contract;
        if (attributes.Ref != Globals.NewObjectId || attributes.XsiNil)
        {
            return ReadNullOrRefDataContractAsync(reader, contract, name, ns);
        }

        contract = ResolveDataContractForRead(reader, declaredType, contract, out bool scoped);
        resolvedContract = contract;
        if (scoped)
        {
            return ReadScopedDataContractValueAsync(contract, reader);
        }

        return ReadDataContractValueAsync(contract, reader);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<object?> ReadNullOrRefDataContractAsync(XmlReaderDelegator reader, DataContract contract, string? name, string? ns)
    {
        Debug.Assert(attributes is not null);
        if (attributes.Ref != Globals.NewObjectId)
        {
            if (IsGetOnlyCollection)
            {
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ErrorDeserializing,
                    SR.Format(SR.ErrorTypeInfo, DataContract.GetClrTypeFullName(contract.UnderlyingType)),
                    SR.Format(SR.XmlStartElementExpected, Globals.RefLocalName)));
            }
            object? value = await GetExistingObjectAsync(attributes.Ref, contract.UnderlyingType, name, ns).ConfigureAwait(false);
            await reader.SkipAsync(CancellationToken).ConfigureAwait(false);
            return value;
        }
        await reader.SkipAsync(CancellationToken).ConfigureAwait(false);
        return null;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<object?> ReadScopedDataContractValueAsync(DataContract contract, XmlReaderDelegator reader)
    {
        try
        {
            return await ReadDataContractValueAsync(contract, reader).ConfigureAwait(false);
        }
        finally
        {
            scopedKnownTypes.Pop();
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    protected virtual ValueTask<object?> ReadDataContractValueAsync(DataContract contract, XmlReaderDelegator reader) =>
        contract.ReadXmlValueAsync(reader, this, CancellationToken);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal ValueTask<string?> ReadIfNullOrRefAsync(XmlReaderDelegator reader, Type type, bool isSerializable)
    {
        Debug.Assert(attributes is not null);
        if (attributes.Ref == Globals.NewObjectId && !attributes.XsiNil)
            return new ValueTask<string?>(Globals.NewObjectId);

        return ReadNullOrRefAsync(reader, type, isSerializable);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<string?> ReadNullOrRefAsync(XmlReaderDelegator reader, Type type, bool isSerializable)
    {
        Debug.Assert(attributes is not null);
        if (attributes.Ref != Globals.NewObjectId)
        {
            CheckIfTypeSerializable(type, isSerializable);
            string reference = attributes.Ref;
            await reader.SkipAsync(CancellationToken).ConfigureAwait(false);
            return reference;
        }
        if (attributes.XsiNil)
        {
            CheckIfTypeSerializable(type, isSerializable);
            await reader.SkipAsync(CancellationToken).ConfigureAwait(false);
            return Globals.NullObjectId;
        }

        return Globals.NewObjectId;
    }

    internal async ValueTask SkipUnknownElementAsync(XmlReaderDelegator reader)
    {
        ReadAttributes(reader);
        await reader.SkipAsync(CancellationToken).ConfigureAwait(false);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal ValueTask<int> GetMemberIndexAsync(XmlReaderDelegator reader, XmlDictionaryString[] names, XmlDictionaryString[] namespaces, int memberIndex, int requiredIndex, ExtensionDataObject? extensionData)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (reader.NodeType == XmlNodeType.Element)
        {
            for (int i = memberIndex + 1; i < names.Length; i++)
            {
                if (reader.IsLocalName(names[i]) && reader.IsNamespaceUri(namespaces[i]))
                {
                    if (requiredIndex < i)
                        ThrowRequiredMemberMissingException(reader, memberIndex, requiredIndex, names);
                    return new ValueTask<int>(i);
                }
            }
        }

        return GetMemberIndexSlowAsync(reader, names, namespaces, memberIndex, requiredIndex, extensionData);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<int> GetMemberIndexSlowAsync(XmlReaderDelegator reader, XmlDictionaryString[] names, XmlDictionaryString[] namespaces, int memberIndex, int requiredIndex, ExtensionDataObject? extensionData)
    {
        for (int i = memberIndex + 1; i < names.Length; i++)
        {
            if (await reader.IsStartElementAsync(names[i], namespaces[i], CancellationToken).ConfigureAwait(false))
            {
                if (requiredIndex < i)
                {
                    ThrowRequiredMemberMissingException(reader, memberIndex, requiredIndex, names);
                }
                return i;
            }
        }

        await HandleMemberNotFoundAsync(reader, extensionData, memberIndex).ConfigureAwait(false);
        return names.Length;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    protected async ValueTask HandleMemberNotFoundAsync(XmlReaderDelegator reader, ExtensionDataObject? extensionData, int memberIndex)
    {
        if (await reader.MoveToContentAsync(CancellationToken).ConfigureAwait(false) != XmlNodeType.Element)
        {
            throw CreateUnexpectedStateException(XmlNodeType.Element, reader);
        }
        if (IgnoreExtensionDataObject || extensionData is null)
        {
            await SkipUnknownElementAsync(reader).ConfigureAwait(false);
        }
        else
        {
            extensionData.Members ??= new List<ExtensionDataMember>();
            extensionData.Members.Add(await ReadExtensionDataMemberAsync(reader, memberIndex).ConfigureAwait(false));
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal async ValueTask<object?> GetExistingObjectAsync(string id, Type? type, string? name, string? ns)
    {
        object? value = DeserializedObjects.GetObject(id);
        if (value is null)
        {
            throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.DeserializedObjectWithIdNotFound, id));
        }
        if (value is IDataNode node)
        {
            if (node.Value is not null && node.IsFinalValue)
            {
                return node.Value;
            }

            _extensionDataReader ??= CreateReaderDelegatorForReader(new ExtensionDataReader(this));
            _extensionDataReader.CancellationToken = CancellationToken;
            _extensionDataReader.UnderlyingExtensionDataReader!.SetDataNode(node, name, ns);
            object? result = await InternalDeserializeAsync(_extensionDataReader, type ?? node.DataType, null, name, ns).ConfigureAwait(false);
            node.Clear();
            _extensionDataReader.UnderlyingExtensionDataReader.Reset();
            return result;
        }

        return value;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal async ValueTask<SerializationInfo> ReadSerializationInfoAsync(XmlReaderDelegator reader, Type type)
    {
#pragma warning disable SYSLIB0050
        SerializationInfo info = new SerializationInfo(type, XmlObjectSerializer.FormatterConverter);
#pragma warning restore SYSLIB0050
        XmlNodeType nodeType;
        while ((nodeType = await reader.MoveToContentAsync(CancellationToken).ConfigureAwait(false)) != XmlNodeType.EndElement)
        {
            if (nodeType != XmlNodeType.Element)
            {
                throw CreateUnexpectedStateException(XmlNodeType.Element, reader);
            }
            if (reader.NamespaceURI.Length != 0)
            {
                await SkipUnknownElementAsync(reader).ConfigureAwait(false);
                continue;
            }

            string name = XmlConvert.DecodeName(reader.LocalName);
            IncrementItemCount(1);
            ReadAttributes(reader);
            object? value;
            if (attributes.Ref != Globals.NewObjectId)
            {
                string reference = attributes.Ref;
                await reader.SkipAsync(CancellationToken).ConfigureAwait(false);
                value = await GetExistingObjectAsync(reference, null, name, string.Empty).ConfigureAwait(false);
            }
            else if (attributes.XsiNil)
            {
                await reader.SkipAsync(CancellationToken).ConfigureAwait(false);
                value = null;
            }
            else
            {
                value = await InternalDeserializeAsync(reader, typeof(object), null, name, string.Empty).ConfigureAwait(false);
            }
            info.AddValue(name, value);
        }

        return info;
    }

    internal async ValueTask CheckEndOfArrayAsync(XmlReaderDelegator reader, int size, XmlDictionaryString name, XmlDictionaryString ns)
    {
        if (reader.NodeType == XmlNodeType.EndElement)
        {
            return;
        }
        while (await reader.IsStartElementAsync(CancellationToken).ConfigureAwait(false))
        {
            if (reader.IsLocalName(name) && reader.IsNamespaceUri(ns))
            {
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArrayExceededSizeAttribute, size, name.Value, ns.Value));
            }
            await SkipUnknownElementAsync(reader).ConfigureAwait(false);
        }
        if (reader.NodeType != XmlNodeType.EndElement)
        {
            throw CreateUnexpectedStateException(XmlNodeType.EndElement, reader);
        }
    }
}

internal partial class XmlObjectSerializerReadContextComplex
{
    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal override ValueTask<object?> InternalDeserializeAsync(XmlReaderDelegator reader, int id, RuntimeTypeHandle declaredHandle, string name, string ns) =>
        _serializationSurrogateProvider is null
            ? base.InternalDeserializeAsync(reader, id, declaredHandle, name, ns)
            : InternalDeserializeWithSurrogateAsync(reader, Type.GetTypeFromHandle(declaredHandle)!, null, name, ns);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal override ValueTask<object?> InternalDeserializeAsync(XmlReaderDelegator reader, Type declaredType, DataContract? contract, string? name, string? ns)
    {
        if (_serializationSurrogateProvider is null)
        {
            return base.InternalDeserializeAsync(reader, declaredType, contract, name, ns);
        }

        return InternalDeserializeWithSurrogateAsync(reader, declaredType, contract, name, ns);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async ValueTask<object?> InternalDeserializeWithSurrogateAsync(XmlReaderDelegator reader, Type declaredType, DataContract? contract, string? name, string? ns)
    {
        Debug.Assert(_serializationSurrogateProvider is not null);
        contract ??= GetDataContract(DataContractSurrogateCaller.GetDataContractType(_serializationSurrogateProvider, declaredType));
        if (IsGetOnlyCollection && contract.UnderlyingType != declaredType)
        {
            throw new InvalidDataContractException(SR.Format(SR.SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser, DataContract.GetClrTypeFullName(declaredType)));
        }
        ReadAttributes(reader);
        string objectId = GetObjectId();
        object? value = await DeserializeDataContractAsync(reader, declaredType, contract, name, ns, out DataContract resolvedContract).ConfigureAwait(false);
        object? result = DataContractSurrogateCaller.GetDeserializedObject(_serializationSurrogateProvider, value, resolvedContract.UnderlyingType, declaredType);
        ReplaceDeserializedObject(objectId, value, result);
        return result;
    }
}
