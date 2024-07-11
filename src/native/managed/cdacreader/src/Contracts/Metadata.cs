// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

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
    Count = 0x2c
}

internal struct EcmaMetadataSchema
{
    public EcmaMetadataSchema(string metadataVersion, bool largeStringHeap, bool largeBlobHeap, bool largeGuidHeap, int[] rowCount, bool[] isSorted, bool variableSizedColumnsAre4BytesLong)
    {
        MetadataVersion = metadataVersion;
        LargeStringHeap = largeStringHeap;
        LargeBlobHeap = largeBlobHeap;
        LargeGuidHeap = largeGuidHeap;

        _rowCount = rowCount;
        _isSorted = isSorted;

        VariableSizedColumnsAreAll4BytesLong = variableSizedColumnsAre4BytesLong;
    }

    public readonly string MetadataVersion;

    public readonly bool LargeStringHeap;
    public readonly bool LargeBlobHeap;
    public readonly bool LargeGuidHeap;

    // Table data, these structures hold MetadataTable.Count entries
    private readonly int[] _rowCount;
    public readonly ReadOnlySpan<int> RowCount => _rowCount;

    private readonly bool[] _isSorted;
    public readonly ReadOnlySpan<bool> IsSorted => _isSorted;

    // In certain scenarios the size of the tables is forced to be the maximum size
    // Otherwise the size of columns should be computed based on RowSize/the various heap flags
    public readonly bool VariableSizedColumnsAreAll4BytesLong;
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

    // This isn't technically part of the contract, but it is here to reduce the complexity of using this contract
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

internal interface IMetadata : IContract
{
    static string IContract.Name => nameof(Metadata);
    static IContract IContract.Create(Target target, int version)
    {
        return version switch
        {
            _ => default(Metadata),
        };
    }

    public virtual EcmaMetadata GetMetadata(ModuleHandle module) => throw new NotImplementedException();

    // Allow users to provide metadata from outside the contract system. Used to enable supporting scenarios where the metadata is not in a
    // dump file, or the contract api user wishes to provide a memory mapped metadata instead of reading it from the target process.
    public virtual void RegisterMetadataProvider(Func<ModuleHandle, EcmaMetadata> provider) => throw new NotImplementedException();

    // Helper api intended for users of RegisterMetadataProvider, not officially part of the documented contract, but placed here in the code for greater visibility
    public EcmaMetadata ProduceEcmaMetadataFromMemory(ReadOnlyMemory<byte> image)
    {
        return (new Helpers.EcmaMetadataReader(image)).UnderlyingMetadata;
    }
}

internal readonly struct Metadata : IMetadata
{
    // Everything throws NotImplementedException
}
