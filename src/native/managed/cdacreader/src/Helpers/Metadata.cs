// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Helpers;

internal enum MetadataTable
{
    Unused = -1,
    Module = 0x0,
    TypeRef = 0x01,
    TypeDef = 0x02,
    FieldPtr = 0x03,
    Field = 0x04,
    MethodPtr = 0x05,
    MethodDef = 0x06,
    ParamPtr = 0x07,
    Param = 0x08,
    InterfaceImpl = 0x09,
    MemberRef = 0x0a,
    Constant = 0x0b,
    CustomAttribute = 0x0c,
    FieldMarshal = 0x0d,
    DeclSecurity = 0x0e,
    ClassLayout = 0x0f,
    FieldLayout = 0x10,
    StandAloneSig = 0x11,
    EventMap = 0x12,
    EventPtr = 0x13,
    Event = 0x14,
    PropertyMap = 0x15,
    PropertyPtr = 0x16,
    Property = 0x17,
    MethodSemantics = 0x18,
    MethodImpl = 0x19,
    ModuleRef = 0x1a,
    TypeSpec = 0x1b,
    ImplMap = 0x1c,
    FieldRva = 0x1d,
    ENCLog = 0x1e,
    ENCMap = 0x1f,
    Assembly = 0x20,
    AssemblyProcessor = 0x21,
    AssemblyOS = 0x22,
    AssemblyRef = 0x23,
    AssemblyRefProcessor = 0x24,
    AssemblyRefOS = 0x25,
    File = 0x26,
    ExportedType = 0x27,
    ManifestResource = 0x28,
    NestedClass = 0x29,
    GenericParam = 0x2a,
    MethodSpec = 0x2b,
    GenericParamConstraint = 0x2c,
    Count = 0x2d
}

internal class EcmaMetadata
{
    public EcmaMetadata(EcmaMetadataSchema schema,
                        ReadOnlyMemory<byte>[] tables,
                        ReadOnlyMemory<byte> stringHeap,
                        ReadOnlyMemory<byte> userStringHeap,
                        ReadOnlyMemory<byte> blobHeap,
                        ReadOnlyMemory<byte> guidHeap)
    {
        Schema = schema;
        _tables = tables;
        StringHeap = stringHeap;
        UserStringHeap = userStringHeap;
        BlobHeap = blobHeap;
        GuidHeap = guidHeap;
    }

    public EcmaMetadataSchema Schema { get; init; }

    private ReadOnlyMemory<byte>[] _tables;
    public ReadOnlySpan<ReadOnlyMemory<byte>> Tables => _tables;
    public ReadOnlyMemory<byte> StringHeap { get; init; }
    public ReadOnlyMemory<byte> UserStringHeap { get; init; }
    public ReadOnlyMemory<byte> BlobHeap { get; init; }
    public ReadOnlyMemory<byte> GuidHeap { get; init; }

    private Microsoft.Diagnostics.DataContractReader.Helpers.EcmaMetadataReader? _ecmaMetadataReader;
    public Microsoft.Diagnostics.DataContractReader.Helpers.EcmaMetadataReader EcmaMetadataReader
    {
        get
        {
            _ecmaMetadataReader ??= new Helpers.EcmaMetadataReader(this);
            return _ecmaMetadataReader;
        }
    }
}

internal enum MetadataColumnIndex
{
    Module_Generation,
    Module_Name,
    Module_Mvid,
    Module_EncId,
    Module_EncBaseId,

    TypeRef_ResolutionScope,
    TypeRef_TypeName,
    TypeRef_TypeNamespace,

    TypeDef_Flags,
    TypeDef_TypeName,
    TypeDef_TypeNamespace,
    TypeDef_Extends,
    TypeDef_FieldList,
    TypeDef_MethodList,

    FieldPtr_Field,

    Field_Flags,
    Field_Name,
    Field_Signature,

    MethodPtr_Method,

    MethodDef_Rva,
    MethodDef_ImplFlags,
    MethodDef_Flags,
    MethodDef_Name,
    MethodDef_Signature,
    MethodDef_ParamList,

    ParamPtr_Param,

    Param_Flags,
    Param_Sequence,
    Param_Name,

    InterfaceImpl_Class,
    InterfaceImpl_Interface,

    MemberRef_Class,
    MemberRef_Name,
    MemberRef_Signature,

    Constant_Type,
    Constant_Parent,
    Constant_Value,

    CustomAttribute_Parent,
    CustomAttribute_Type,
    CustomAttribute_Value,

    FieldMarshal_Parent,
    FieldMarshal_NativeType,

    DeclSecurity_Action,
    DeclSecurity_Parent,
    DeclSecurity_PermissionSet,

    ClassLayout_PackingSize,
    ClassLayout_ClassSize,
    ClassLayout_Parent,

    FieldLayout_Offset,
    FieldLayout_Field,

    StandAloneSig_Signature,

    EventMap_Parent,
    EventMap_EventList,

    EventPtr_Event,

    Event_EventFlags,
    Event_Name,
    Event_EventType,

    PropertyMap_Parent,
    PropertyMap_PropertyList,

    PropertyPtr_Property,

    Property_Flags,
    Property_Name,
    Property_Type,

    MethodSemantics_Semantics,
    MethodSemantics_Method,
    MethodSemantics_Association,

    MethodImpl_Class,
    MethodImpl_MethodBody,
    MethodImpl_MethodDeclaration,

    ModuleRef_Name,

    TypeSpec_Signature,

    ImplMap_MappingFlags,
    ImplMap_MemberForwarded,
    ImplMap_ImportName,
    ImplMap_ImportScope,

    FieldRva_Rva,
    FieldRva_Field,

    ENCLog_Token,
    ENCLog_Op,

    ENCMap_Token,

    Assembly_HashAlgId,
    Assembly_MajorVersion,
    Assembly_MinorVersion,
    Assembly_BuildNumber,
    Assembly_RevisionNumber,
    Assembly_Flags,
    Assembly_PublicKey,
    Assembly_Name,
    Assembly_Culture,

    AssemblyRef_MajorVersion,
    AssemblyRef_MinorVersion,
    AssemblyRef_BuildNumber,
    AssemblyRef_RevisionNumber,
    AssemblyRef_Flags,
    AssemblyRef_PublicKeyOrToken,
    AssemblyRef_Name,
    AssemblyRef_Culture,
    AssemblyRef_HashValue,

    File_Flags,
    File_Name,
    File_HashValue,

    ExportedType_Flags,
    ExportedType_TypeDefId,
    ExportedType_TypeName,
    ExportedType_TypeNamespace,
    ExportedType_Implementation,

    ManifestResource_Offset,
    ManifestResource_Flags,
    ManifestResource_Name,
    ManifestResource_Implementation,

    NestedClass_NestedClass,
    NestedClass_EnclosingClass,

    GenericParam_Number,
    GenericParam_Flags,
    GenericParam_Owner,
    GenericParam_Name,

    MethodSpec_Method,
    MethodSpec_Instantiation,

    GenericParamConstraint_Owner,
    GenericParamConstraint_Constraint,

    Count
}

internal class Metadata
{
    private readonly Target _target;
    private readonly Dictionary<ulong, EcmaMetadata> _metadata = [];

    public Metadata(Target target)
    {
        _target = target;
    }

    public virtual EcmaMetadata GetMetadata(Contracts.ModuleHandle module)
    {
        if (_metadata.TryGetValue(module.Address, out EcmaMetadata? result))
            return result;

        AvailableMetadataType metadataType = _target.Contracts.Loader.GetAvailableMetadataType(module);

        if (metadataType == AvailableMetadataType.ReadOnly)
        {
            if (this.MetadataProvider != null)
                result = this.MetadataProvider(module);
            if (result == null)
            {
                TargetPointer address = _target.Contracts.Loader.GetMetadataAddress(module, out ulong size);
                byte[] data = new byte[size];
                _target.ReadBuffer(address, data);
                result = (new Helpers.EcmaMetadataReader(new ReadOnlyMemory<byte>(data))).UnderlyingMetadata;
            }
        }
        else if (metadataType == AvailableMetadataType.ReadWriteSavedCopy)
        {
            TargetPointer address = _target.Contracts.Loader.GetReadWriteSavedMetadataAddress(module, out ulong size);
            byte[] data = new byte[size];
            _target.ReadBuffer(address, data);
            result = (new Helpers.EcmaMetadataReader(new ReadOnlyMemory<byte>(data))).UnderlyingMetadata;
        }
        else
        {
            var targetEcmaMetadata = _target.Contracts.Loader.GetReadWriteMetadata(module);
            result = new EcmaMetadata(targetEcmaMetadata.Schema,
                GetReadOnlyMemoryFromTargetSpans(targetEcmaMetadata.Tables),
                GetReadOnlyMemoryFromTargetSpan(targetEcmaMetadata.StringHeap),
                GetReadOnlyMemoryFromTargetSpan(targetEcmaMetadata.UserStringHeap),
                GetReadOnlyMemoryFromTargetSpan(targetEcmaMetadata.BlobHeap),
                GetReadOnlyMemoryFromTargetSpan(targetEcmaMetadata.GuidHeap));

            ReadOnlyMemory<byte> GetReadOnlyMemoryFromTargetSpan(TargetSpan span)
            {
                if (span.Size == 0)
                    return default;
                byte[] data = new byte[span.Size];
                _target.ReadBuffer(span.Address, data);
                return new ReadOnlyMemory<byte>(data);
            }
            ReadOnlyMemory<byte>[] GetReadOnlyMemoryFromTargetSpans(ReadOnlySpan<TargetSpan> spans)
            {
                ReadOnlyMemory<byte>[] memories = new ReadOnlyMemory<byte>[spans.Length];
                for (int i = 0; i < spans.Length; i++)
                {
                    memories[i] = GetReadOnlyMemoryFromTargetSpan(spans[i]);
                }
                return memories;
            }
        }

        _metadata.Add(module.Address, result);
        return result;
    }

    public Func<Contracts.ModuleHandle, EcmaMetadata>? MetadataProvider;
}
