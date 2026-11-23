// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.DataContracts;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization;

internal partial class XmlObjectSerializerWriteContext
{
    internal void InitializeAsync(XmlWriterDelegator writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationToken = cancellationToken;
        writer.CancellationToken = cancellationToken;
    }

    internal Task WriteIXmlSerializableAsync(XmlWriterDelegator writer, object value) =>
        AsyncXmlSerializableServices.WriteAsync(writer, value, CancellationToken).AsTask();

    internal static Task WriteRootIXmlSerializableAsync(XmlWriterDelegator writer, object value, CancellationToken cancellationToken) =>
        AsyncXmlSerializableServices.WriteAsync(writer, value, cancellationToken).AsTask();

    internal virtual bool CanWriteCachedReference(XmlWriterDelegator writer) =>
        !preserveObjectReferences && !_isGetOnlyCollection && writer.depth < depthToCheckCyclicReference;

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Task InternalSerializeReferenceAsync(XmlWriterDelegator writer, object value, bool isDeclaredType, bool writeXsiType, int declaredId, RuntimeTypeHandle declaredHandle)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if ((!preserveObjectReferences || _isGetOnlyCollection) && writer.depth < depthToCheckCyclicReference)
            return InternalSerializeAsync(writer, value, isDeclaredType, writeXsiType, declaredId, declaredHandle);

        return SerializeReferenceWithScopeAsync(writer, value, isDeclaredType, writeXsiType, declaredId, declaredHandle);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private Task SerializeReferenceWithScopeAsync(XmlWriterDelegator writer, object value, bool isDeclaredType, bool writeXsiType, int declaredId, RuntimeTypeHandle declaredHandle)
    {
        Task<bool> reference = OnHandleReferenceAsync(writer, value, true);
        if (!reference.IsCompletedSuccessfully)
            return AwaitReferenceAndSerializeAsync(reference, writer, value, isDeclaredType, writeXsiType, declaredId, declaredHandle);
        if (reference.Result)
        {
            OnEndHandleReference(writer, value, true);
            return Task.CompletedTask;
        }

        Task serialization;
        try
        {
            serialization = InternalSerializeAsync(writer, value, isDeclaredType, writeXsiType, declaredId, declaredHandle);
        }
        catch
        {
            OnEndHandleReference(writer, value, true);
            throw;
        }
        if (!serialization.IsCompletedSuccessfully)
            return AwaitSerializationAndEndReferenceAsync(serialization, writer, value);
        OnEndHandleReference(writer, value, true);

        return Task.CompletedTask;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task AwaitReferenceAndSerializeAsync(Task<bool> reference, XmlWriterDelegator writer,
        object value, bool isDeclaredType, bool writeXsiType, int declaredId, RuntimeTypeHandle declaredHandle)
    {
        bool handled = await reference.ConfigureAwait(false);
        try
        {
            if (!handled)
                await InternalSerializeAsync(writer, value, isDeclaredType, writeXsiType, declaredId, declaredHandle).ConfigureAwait(false);
        }
        finally
        {
            OnEndHandleReference(writer, value, true);
        }
    }

    private async Task AwaitSerializationAndEndReferenceAsync(Task serialization, XmlWriterDelegator writer, object value)
    {
        try
        {
            await serialization.ConfigureAwait(false);
        }
        finally
        {
            OnEndHandleReference(writer, value, true);
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal virtual Task InternalSerializeAsync(XmlWriterDelegator writer, object value, bool isDeclaredType, bool writeXsiType, int declaredId, RuntimeTypeHandle declaredHandle)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (writeXsiType)
        {
            return SerializeWithXsiTypeAsync(writer, value, value.GetType().TypeHandle, null, -1, typeof(object).TypeHandle, typeof(object));
        }
        if (isDeclaredType)
        {
            return SerializeWithoutXsiTypeAsync(GetDataContract(declaredId, declaredHandle), writer, value, declaredHandle);
        }
        RuntimeTypeHandle valueHandle = value.GetType().TypeHandle;
        if (declaredHandle.GetHashCode() == valueHandle.GetHashCode())
        {
            DataContract contract = declaredId >= 0 ? GetDataContract(declaredId, declaredHandle) : GetDataContract(declaredHandle, null);
            return SerializeWithoutXsiTypeAsync(contract, writer, value, declaredHandle);
        }

        return SerializeWithXsiTypeAsync(writer, value, valueHandle, null, declaredId, declaredHandle, Type.GetTypeFromHandle(declaredHandle)!);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal Task SerializeWithoutXsiTypeAsync(DataContract contract, XmlWriterDelegator writer, object value, RuntimeTypeHandle declaredHandle)
    {
        if (!TryGetIsReferenceId(contract, value, out int objectId, out bool isNew))
            return WriteWithKnownTypesAsync(contract, writer, value, declaredHandle);

        Task<bool> reference = WriteIsReferenceAsync(writer, objectId, isNew);
        if (reference.IsCompletedSuccessfully)
        {
            return reference.Result ? Task.CompletedTask : WriteWithKnownTypesAsync(contract, writer, value, declaredHandle);
        }

        return AwaitReferenceAndWriteAsync(reference, contract, writer, value, declaredHandle);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task AwaitReferenceAndWriteAsync(Task<bool> reference, DataContract contract,
        XmlWriterDelegator writer, object value, RuntimeTypeHandle declaredHandle)
    {
        if (!await reference.ConfigureAwait(false))
            await WriteWithKnownTypesAsync(contract, writer, value, declaredHandle).ConfigureAwait(false);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal Task WriteWithKnownTypesAsync(DataContract contract, XmlWriterDelegator writer, object value, RuntimeTypeHandle declaredHandle) =>
        contract.KnownDataContracts is { Count: > 0 } knownTypes
            ? WriteWithKnownTypeScopeAsync(knownTypes, contract, writer, value, declaredHandle)
            : WriteDataContractValueAsync(contract, writer, value, declaredHandle);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task WriteWithKnownTypeScopeAsync(Dictionary<XmlQualifiedName, DataContract> knownTypes,
        DataContract contract, XmlWriterDelegator writer, object value, RuntimeTypeHandle declaredHandle)
    {
        scopedKnownTypes.Push(knownTypes);
        try
        {
            await WriteDataContractValueAsync(contract, writer, value, declaredHandle).ConfigureAwait(false);
        }
        finally
        {
            scopedKnownTypes.Pop();
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal virtual async Task SerializeWithXsiTypeAtTopLevelAsync(DataContract contract, XmlWriterDelegator writer, object value, RuntimeTypeHandle originalDeclaredHandle, Type graphType)
    {
        Debug.Assert(rootTypeDataContract is not null);
        Type declaredType = rootTypeDataContract.OriginalUnderlyingType;
        bool verify = false;
        if (declaredType.IsInterface && CollectionDataContract.IsCollectionInterface(declaredType))
        {
            if (DataContractResolver is not null)
            {
                await WriteResolvedTypeInfoAsync(writer, graphType, declaredType).ConfigureAwait(false);
            }
        }
        else if (!declaredType.IsArray)
        {
            verify = await WriteTypeInfoAsync(writer, contract, rootTypeDataContract).ConfigureAwait(false);
        }
        await SerializeAndVerifyTypeAsync(contract, writer, value, verify, originalDeclaredHandle, declaredType).ConfigureAwait(false);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    protected virtual Task SerializeWithXsiTypeAsync(XmlWriterDelegator writer, object value, RuntimeTypeHandle valueHandle, Type? valueType, int declaredId, RuntimeTypeHandle declaredHandle, Type declaredType)
    {
        if (declaredType.IsInterface || declaredType.IsArray)
            return SerializeWithXsiTypeCoreAsync(writer, value, valueHandle, valueType, declaredId, declaredHandle, declaredType);
        DataContract contract = GetDataContract(valueHandle, valueType);
        if (TryGetIsReferenceId(contract, value, out int objectId, out bool isNew))
            return SerializeWithXsiTypeReferenceAsync(writer, value, contract, objectId, isNew, declaredId, declaredHandle, declaredType);
        return WriteTypeAndSerializeAsync(writer, value, contract, declaredId, declaredHandle, declaredType);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task SerializeWithXsiTypeReferenceAsync(XmlWriterDelegator writer, object value, DataContract contract,
        int objectId, bool isNew, int declaredId, RuntimeTypeHandle declaredHandle, Type declaredType)
    {
        if (!await WriteIsReferenceAsync(writer, objectId, isNew).ConfigureAwait(false))
            await WriteTypeAndSerializeAsync(writer, value, contract, declaredId, declaredHandle, declaredType).ConfigureAwait(false);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private Task WriteTypeAndSerializeAsync(XmlWriterDelegator writer, object value, DataContract contract,
        int declaredId, RuntimeTypeHandle declaredHandle, Type declaredType)
    {
        DataContract declared = declaredId >= 0 ? GetDataContract(declaredId, declaredHandle) : GetDataContract(declaredHandle, declaredType);
        ValueTask<bool> information = WriteTypeInfoAsync(writer, contract, declared);
        return information.IsCompletedSuccessfully
            ? SerializeAndVerifyTypeAsync(contract, writer, value, information.Result, declaredHandle, declaredType)
            : AwaitTypeAndSerializeAsync(information, contract, writer, value, declaredHandle, declaredType);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task AwaitTypeAndSerializeAsync(ValueTask<bool> information, DataContract contract, XmlWriterDelegator writer,
        object value, RuntimeTypeHandle declaredHandle, Type declaredType) =>
        await SerializeAndVerifyTypeAsync(contract, writer, value, await information.ConfigureAwait(false), declaredHandle, declaredType).ConfigureAwait(false);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task SerializeWithXsiTypeCoreAsync(XmlWriterDelegator writer, object value, RuntimeTypeHandle valueHandle, Type? valueType, int declaredId, RuntimeTypeHandle declaredHandle, Type declaredType)
    {
        bool verify = false;
        DataContract contract;
        if (declaredType.IsInterface && CollectionDataContract.IsCollectionInterface(declaredType))
        {
            contract = GetDataContractSkipValidation(DataContract.GetId(valueHandle), valueHandle, valueType);
            if (await OnHandleIsReferenceAsync(writer, contract, value).ConfigureAwait(false))
            {
                return;
            }
            contract = GetDataContract(declaredHandle, declaredType);
            if (DataContractResolver is not null)
            {
                await WriteResolvedTypeInfoAsync(writer, valueType ?? Type.GetTypeFromHandle(valueHandle)!, declaredType).ConfigureAwait(false);
            }
        }
        else if (declaredType.IsArray)
        {
            _ = GetDataContract(valueHandle, valueType);
            contract = GetDataContract(declaredHandle, declaredType);
        }
        else
        {
            contract = GetDataContract(valueHandle, valueType);
            if (await OnHandleIsReferenceAsync(writer, contract, value).ConfigureAwait(false))
            {
                return;
            }
            DataContract declared = declaredId >= 0 ? GetDataContract(declaredId, declaredHandle) : GetDataContract(declaredHandle, declaredType);
            verify = await WriteTypeInfoAsync(writer, contract, declared).ConfigureAwait(false);
        }
        await SerializeAndVerifyTypeAsync(contract, writer, value, verify, declaredHandle, declaredType).ConfigureAwait(false);
    }

    internal Task<bool> OnHandleIsReferenceAsync(XmlWriterDelegator writer, DataContract contract, object value)
    {
        if (!TryGetIsReferenceId(contract, value, out int objectId, out bool isNew))
        {
            return Task.FromResult(false);
        }
        return WriteIsReferenceAsync(writer, objectId, isNew);
    }

    private static async Task<bool> WriteIsReferenceAsync(XmlWriterDelegator writer, int objectId, bool isNew)
    {
        await writer.WriteAttributeStringAsync(Globals.SerPrefix,
            isNew ? DictionaryGlobals.IdLocalName : DictionaryGlobals.RefLocalName,
            DictionaryGlobals.SerializationNamespace, string.Create(CultureInfo.InvariantCulture, $"i{objectId}")).ConfigureAwait(false);
        return !isNew;
    }

    private bool TryGetIsReferenceId(DataContract contract, object value, out int objectId, out bool isNew)
    {
        objectId = 0;
        isNew = true;
        if (preserveObjectReferences || !contract.IsReference || _isGetOnlyCollection)
        {
            return false;
        }
        objectId = SerializedObjects.GetId(value, ref isNew);
        _byValObjectsInScope.EnsureSetAsIsReference(value);
        return true;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    protected Task SerializeAndVerifyTypeAsync(DataContract contract, XmlWriterDelegator writer, object value, bool verify, RuntimeTypeHandle declaredHandle, Type declaredType)
    {
        if (contract.KnownDataContracts is { Count: > 0 })
            return SerializeAndVerifyTypeWithScopeAsync(contract, writer, value, verify, declaredHandle, declaredType);
        if (verify)
            VerifyKnownTypeForWrite(contract, declaredType);
        return WriteDataContractValueAsync(contract, writer, value, declaredHandle);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task SerializeAndVerifyTypeWithScopeAsync(DataContract contract, XmlWriterDelegator writer, object value, bool verify, RuntimeTypeHandle declaredHandle, Type declaredType)
    {
        bool scoped = contract.KnownDataContracts?.Count > 0;
        if (scoped)
        {
            scopedKnownTypes.Push(contract.KnownDataContracts!);
        }
        try
        {
            if (verify)
            {
                VerifyKnownTypeForWrite(contract, declaredType);
            }
            await WriteDataContractValueAsync(contract, writer, value, declaredHandle).ConfigureAwait(false);
        }
        finally
        {
            if (scoped)
            {
                scopedKnownTypes.Pop();
            }
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private void VerifyKnownTypeForWrite(DataContract contract, Type declaredType)
    {
        if (!IsKnownType(contract, declaredType))
        {
            DataContract? known = ResolveDataContractFromKnownTypes(contract.XmlName.Name, contract.XmlName.Namespace, null, declaredType);
            if (known is null || known.UnderlyingType != contract.UnderlyingType)
            {
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.DcTypeNotFoundOnSerialize,
                    DataContract.GetClrTypeFullName(contract.UnderlyingType), contract.XmlName.Name, contract.XmlName.Namespace));
            }
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    protected virtual Task WriteDataContractValueAsync(DataContract contract, XmlWriterDelegator writer, object value, RuntimeTypeHandle declaredHandle) =>
        contract.WriteXmlValueAsync(writer, value, this, CancellationToken);

    internal Task HandleGraphAtTopLevelAsync(XmlWriterDelegator writer, object value, DataContract contract)
    {
        Task prefix = writer.WriteXmlnsAttributeAsync(Globals.XsiPrefix, DictionaryGlobals.SchemaInstanceNamespace);
        return prefix.IsCompletedSuccessfully && !contract.IsISerializable
            ? OnHandleReferenceAsync(writer, value, true)
            : AwaitGraphPrefixAsync(prefix, writer, value, contract);
    }

    private async Task AwaitGraphPrefixAsync(Task prefix, XmlWriterDelegator writer, object value, DataContract contract)
    {
        await prefix.ConfigureAwait(false);
        if (contract.IsISerializable)
        {
            await writer.WriteXmlnsAttributeAsync(Globals.XsdPrefix, DictionaryGlobals.SchemaNamespace).ConfigureAwait(false);
        }
        await OnHandleReferenceAsync(writer, value, true).ConfigureAwait(false);
    }

    internal virtual Task<bool> OnHandleReferenceAsync(XmlWriterDelegator writer, object value, bool canContainCyclicReference)
    {
        CancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(HandleReferenceScope(writer, value, canContainCyclicReference));
    }

    internal virtual Task WriteNullAsync(XmlWriterDelegator writer) =>
        writer.WriteAttributeBoolAsync(Globals.XsiPrefix, DictionaryGlobals.XsiNilLocalName, DictionaryGlobals.SchemaInstanceNamespace, true);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal Task WriteNullAsync(XmlWriterDelegator writer, Type type, bool isSerializable)
    {
        CheckIfTypeSerializable(type, isSerializable);
        return WriteNullAsync(writer);
    }

    internal virtual Task WriteArraySizeAsync(XmlWriterDelegator writer, int size) => Task.CompletedTask;

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal Task WriteReferencePrimitiveElementAsync(XmlWriterDelegator writer, object? value, Type type, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        if (type == typeof(string))
            return WriteReferencePrimitiveElementAsync<ReflectionPlan.StringValue>(writer, value, type, name, ns, null);
        if (type == typeof(Uri))
            return WriteReferencePrimitiveElementAsync<ReflectionPlan.UriValue>(writer, value, type, name, ns, null);

        return WriteReferencePrimitiveElementAsync<ReflectionPlan.QualifiedValue>(writer, value, type, name, ns,
            ns is not null && ns.Value.Length > 0 ? Globals.ElementPrefix : null);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal Task WriteReferencePrimitiveElementAsync<TPrimitiveKind>(
        XmlWriterDelegator writer, object? value, Type type, XmlDictionaryString name, XmlDictionaryString? ns, string? prefix)
        where TPrimitiveKind : struct
    {
        if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.StringValue) && value is not null && !preserveObjectReferences)
        {
            CancellationToken.ThrowIfCancellationRequested();
            return writer.WritePrimitiveElementAsync<ReflectionPlan.StringValue>(value, name, ns);
        }

        return WriteReferencePrimitiveElementAsyncCore<TPrimitiveKind>(writer, value, type, name, ns, prefix);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task WriteReferencePrimitiveElementAsyncCore<TPrimitiveKind>(
        XmlWriterDelegator writer, object? value, Type type, XmlDictionaryString name, XmlDictionaryString? ns, string? prefix)
        where TPrimitiveKind : struct
    {
        if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.QualifiedValue))
        {
            await writer.WriteStartElementAsync(prefix, name, ns).ConfigureAwait(false);
        }
        else
        {
            await writer.WriteStartElementPrimitiveAsync(name, ns).ConfigureAwait(false);
        }
        if (value is null)
        {
            await WriteNullAsync(writer, type, true).ConfigureAwait(false);
        }
        else if (!await OnHandleReferenceAsync(writer, value, false).ConfigureAwait(false))
        {
            if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.StringValue))
            {
                await writer.WriteStringAsync((string)value).ConfigureAwait(false);
            }
            else if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.UriValue))
            {
                await writer.WriteUriAsync((Uri)value).ConfigureAwait(false);
            }
            else
            {
                await writer.WriteQNameAsync((XmlQualifiedName)value).ConfigureAwait(false);
            }
        }
        if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.QualifiedValue))
        {
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
        else
        {
            await writer.WriteEndElementPrimitiveAsync().ConfigureAwait(false);
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    protected virtual async ValueTask<bool> WriteTypeInfoAsync(XmlWriterDelegator writer, DataContract contract, DataContract declaredContract)
    {
        if (XmlObjectSerializer.IsContractDeclared(contract, declaredContract))
        {
            return false;
        }
        if (DataContractResolver is not null)
        {
            await WriteResolvedTypeInfoAsync(writer, contract.OriginalUnderlyingType, declaredContract.OriginalUnderlyingType).ConfigureAwait(false);
            return false;
        }

        await WriteTypeInfoAsync(writer, contract.Name, contract.Namespace).ConfigureAwait(false);
        return true;
    }

    protected virtual Task WriteTypeInfoAsync(XmlWriterDelegator writer, string name, string? ns) =>
        writer.WriteAttributeQualifiedNameAsync(Globals.XsiPrefix, DictionaryGlobals.XsiTypeLocalName, DictionaryGlobals.SchemaInstanceNamespace, name, ns);

    protected virtual Task WriteTypeInfoAsync(XmlWriterDelegator writer, XmlDictionaryString name, XmlDictionaryString ns) =>
        writer.WriteAttributeQualifiedNameAsync(Globals.XsiPrefix, DictionaryGlobals.XsiTypeLocalName, DictionaryGlobals.SchemaInstanceNamespace, name, ns);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private Task WriteResolvedTypeInfoAsync(XmlWriterDelegator writer, Type type, Type declaredType) =>
        ResolveType(type, declaredType, out XmlDictionaryString? name, out XmlDictionaryString? ns)
            ? WriteTypeInfoAsync(writer, name, ns) : Task.CompletedTask;

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal virtual async Task WriteISerializableAsync(XmlWriterDelegator writer, ISerializable value)
    {
#pragma warning disable SYSLIB0050
        SerializationInfo info = new SerializationInfo(value.GetType(), XmlObjectSerializer.FormatterConverter);
#pragma warning restore SYSLIB0050
        GetObjectData(value, info, GetStreamingContext());
        await WriteSerializationInfoAsync(writer, value.GetType(), info).ConfigureAwait(false);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal async Task WriteSerializationInfoAsync(XmlWriterDelegator writer, Type type, SerializationInfo info)
    {
        if (DataContract.GetClrTypeFullName(type) != info.FullTypeName)
        {
            if (DataContractResolver is not null)
            {
                if (ResolveType(info.ObjectType, type, out XmlDictionaryString? name, out XmlDictionaryString? ns))
                {
                    await writer.WriteAttributeQualifiedNameAsync(Globals.SerPrefix, DictionaryGlobals.ISerializableFactoryTypeLocalName, DictionaryGlobals.SerializationNamespace, name, ns).ConfigureAwait(false);
                }
            }
            else
            {
                DataContract.GetDefaultXmlName(info.FullTypeName, out string name, out string ns);
                await writer.WriteAttributeQualifiedNameAsync(Globals.SerPrefix, DictionaryGlobals.ISerializableFactoryTypeLocalName,
                    DictionaryGlobals.SerializationNamespace, DataContract.GetClrTypeString(name), DataContract.GetClrTypeString(ns)).ConfigureAwait(false);
            }
        }
        IncrementItemCount(info.MemberCount);
        foreach (SerializationEntry entry in info)
        {
            await writer.WriteStartElementAsync(null, DataContract.GetClrTypeString(DataContract.EncodeLocalName(entry.Name)), DictionaryGlobals.EmptyString).ConfigureAwait(false);
            if (entry.Value is null)
            {
                await WriteNullAsync(writer).ConfigureAwait(false);
            }
            else
            {
                await InternalSerializeReferenceAsync(writer, entry.Value, false, false, -1, typeof(object).TypeHandle).ConfigureAwait(false);
            }
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}

internal partial class XmlObjectSerializerWriteContextComplex
{
    internal override bool CanWriteCachedReference(XmlWriterDelegator writer) =>
        _serializationSurrogateProvider is null && base.CanWriteCachedReference(writer);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal override Task InternalSerializeAsync(XmlWriterDelegator writer, object value, bool isDeclaredType, bool writeXsiType, int declaredId, RuntimeTypeHandle declaredHandle) =>
        _serializationSurrogateProvider is ISerializationSurrogateProvider provider
            ? SerializeWithSurrogateAsync(writer, value, isDeclaredType, writeXsiType, declaredId, declaredHandle, provider)
            : base.InternalSerializeAsync(writer, value, isDeclaredType, writeXsiType, declaredId, declaredHandle);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task SerializeWithSurrogateAsync(XmlWriterDelegator writer, object value, bool isDeclaredType,
        bool writeXsiType, int declaredId, RuntimeTypeHandle declaredHandle, ISerializationSurrogateProvider provider)
    {
        RuntimeTypeHandle valueHandle = isDeclaredType ? declaredHandle : value.GetType().TypeHandle;
        Type valueType = Type.GetTypeFromHandle(valueHandle)!;
        Type declaredType = GetSurrogatedType(Type.GetTypeFromHandle(declaredHandle)!);
        object original = value;
        value = DataContractSerializer.SurrogateToDataContractType(provider, value, declaredType, ref valueType);
        int oldId = !ReferenceEquals(original, value) ? SerializedObjects.ReassignId(0, original, value) : 0;
        try
        {
            if (writeXsiType)
            {
                await SerializeWithXsiTypeAsync(writer, value, valueType.TypeHandle, valueType, -1, typeof(object).TypeHandle, typeof(object)).ConfigureAwait(false);
            }
            else if (declaredType.TypeHandle.Equals(valueType.TypeHandle))
            {
                await SerializeWithoutXsiTypeAsync(GetDataContract(valueType.TypeHandle, valueType), writer, value, declaredType.TypeHandle).ConfigureAwait(false);
            }
            else
            {
                await SerializeWithXsiTypeAsync(writer, value, valueType.TypeHandle, valueType, -1, declaredType.TypeHandle, declaredType).ConfigureAwait(false);
            }
        }
        finally
        {
            if (!ReferenceEquals(original, value))
            {
                SerializedObjects.ReassignId(oldId, value, original);
            }
        }
    }

    internal override Task<bool> OnHandleReferenceAsync(XmlWriterDelegator writer, object value, bool canContainCyclicReference)
    {
        if (preserveObjectReferences && !IsGetOnlyCollection)
        {
            return WriteReferenceAsync(writer, value);
        }

        return base.OnHandleReferenceAsync(writer, value, canContainCyclicReference);
    }

    private async Task<bool> WriteReferenceAsync(XmlWriterDelegator writer, object value)
    {
        bool isNew = true;
        int id = SerializedObjects.GetId(value, ref isNew);
        await writer.WriteAttributeIntAsync(Globals.SerPrefix, isNew ? DictionaryGlobals.IdLocalName : DictionaryGlobals.RefLocalName, DictionaryGlobals.SerializationNamespace, id).ConfigureAwait(false);
        if (!isNew)
            await writer.WriteAttributeBoolAsync(Globals.XsiPrefix, DictionaryGlobals.XsiNilLocalName, DictionaryGlobals.SchemaInstanceNamespace, true).ConfigureAwait(false);

        return !isNew;
    }

    internal override Task WriteArraySizeAsync(XmlWriterDelegator writer, int size) =>
        preserveObjectReferences && size > -1
            ? writer.WriteAttributeIntAsync(Globals.SerPrefix, DictionaryGlobals.ArraySizeLocalName, DictionaryGlobals.SerializationNamespace, size)
            : Task.CompletedTask;
}
