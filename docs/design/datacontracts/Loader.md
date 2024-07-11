# Contract Loader

This contract is for getting information about loaded modules and assemblies

## APIs of contract

``` csharp
readonly struct ModuleHandle
{
    // Opaque handle - no public members

    internal TargetPointer Address;
}

[Flags]
enum ModuleFlags
{
    EditAndContinue = 0x00000008,   // Edit and Continue is enabled for this module
    ReflectionEmit = 0x00000040,    // Reflection.Emit was used to create this module
}

record struct ModuleLookupTables(
    TargetPointer FieldDefToDesc,
    TargetPointer ManifestModuleReferences,
    TargetPointer MemberRefToDesc,
    TargetPointer MethodDefToDesc,
    TargetPointer TypeDefToMethodTable,
    TargetPointer TypeRefToMethodTable);

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

internal class TargetEcmaMetadata
{
    public TargetEcmaMetadata(EcmaMetadataSchema schema,
                        TargetSpan[] tables,
                        TargetSpan stringHeap,
                        TargetSpan userStringHeap,
                        TargetSpan blobHeap,
                        TargetSpan guidHeap)
    {
        Schema = schema;
        _tables = tables;
        StringHeap = stringHeap;
        UserStringHeap = userStringHeap;
        BlobHeap = blobHeap;
        GuidHeap = guidHeap;
    }

    public EcmaMetadataSchema Schema { get; init; }

    private TargetSpan[] _tables;
    public ReadOnlySpan<TargetSpan> Tables => _tables;
    public TargetSpan StringHeap { get; init; }
    public TargetSpan UserStringHeap { get; init; }
    public TargetSpan BlobHeap { get; init; }
    public TargetSpan GuidHeap { get; init; }
}

[Flags]
internal enum AvailableMetadataType
{
    None = 0,
    ReadOnly = 1,
    ReadWriteSavedCopy = 2,
    ReadWrite = 4
}
```

``` csharp
ModuleHandle GetModuleHandle(TargetPointer);
TargetPointer GetAssembly(ModuleHandle handle);
ModuleFlags GetFlags(ModuleHandle handle);
TargetPointer GetLoaderAllocator(ModuleHandle handle);
TargetPointer GetThunkHeap(ModuleHandle handle);
TargetPointer GetILBase(ModuleHandle handle);
TargetPointer GetMetadataAddress(ModuleHandle handle, out ulong size);
AvailableMetadataType GetAvailableMetadataType(ModuleHandle handle);
TargetPointer GetReadWriteSavedMetadataAddress(ModuleHandle handle, out ulong size);
TargetEcmaMetadata GetReadWriteMetadata(ModuleHandle handle);
ModuleLookupTables GetLookupTables(ModuleHandle handle);
```

## Version 1

Data descriptors used:
- `Module`

``` csharp
ModuleHandle GetModuleHandle(TargetPointer modulePointer)
{
    return new ModuleHandle(modulePointer);
}

TargetPointer GetAssembly(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::Assrembly offset */);
}

ModuleFlags GetFlags(ModuleHandle handle)
{
    return target.Read<uint>(handle.Address + /* Module::Flags offset */);
}

TargetPointer GetLoaderAllocator(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::LoaderAllocator offset */);
}

TargetPointer GetThunkHeap(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::ThunkHeap offset */);
}

TargetPointer GetILBase(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::Base offset */);
}

TargetPointer GetMetadataAddress(ModuleHandle handle, out ulong size)
{
    TargetPointer baseAddress = GetILBase(handle);
    if (baseAddress == TargetPointer.Null)
    {
        size = 0;
        return TargetPointer.Null;
    }

    // Read CLR header per https://learn.microsoft.com/windows/win32/debug/pe-format
    ulong clrHeaderRVA = ...

    // Read Metadata per ECMA-335 II.25.3.3 CLI Header
    ulong metadataDirectoryAddress = baseAddress + clrHeaderRva + /* offset to Metadata */
    int rva = target.Read<int>(metadataDirectoryAddress);
    size = target.Read<int>(metadataDirectoryAddress + sizeof(int));
    return baseAddress + rva;
}

AvailableMetadataType ILoader.GetAvailableMetadataType(ModuleHandle handle)
{
    Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);

    AvailableMetadataType flags = AvailableMetadataType.None;

    TargetPointer dynamicMetadata = target.ReadPointer(handle.Address + /* Module::DynamicMetadata offset */);

    if (dynamicMetadata != TargetPointer.Null)
        flags |= AvailableMetadataType.ReadWriteSavedCopy;
    else
        flags |= AvailableMetadataType.ReadOnly;

    return flags;
}

TargetPointer ILoader.GetReadWriteSavedMetadataAddress(ModuleHandle handle, out ulong size)
{
    Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
    TargetPointer dynamicMetadata = target.ReadPointer(handle.Address + /* Module::DynamicMetadata offset */);
    size = _target.Read<uint>(dynamicMetadata);
    TargetPointer result = module.DynamicMetadata + (ulong)_target.PointerSize;
    return result;
}

ModuleLookupTables GetLookupTables(ModuleHandle handle)
{
    return new ModuleLookupTables(
        FieldDefToDescMap: target.ReadPointer(handle.Address + /* Module::FieldDefToDescMap */),
        ManifestModuleReferencesMap: target.ReadPointer(handle.Address + /* Module::ManifestModuleReferencesMap */),
        MemberRefToDescMap: target.ReadPointer(handle.Address + /* Module::MemberRefToDescMap */),
        MethodDefToDescMap: target.ReadPointer(handle.Address + /* Module::MethodDefToDescMap */),
        TypeDefToMethodTableMap: target.ReadPointer(handle.Address + /* Module::TypeDefToMethodTableMap */),
        TypeRefToMethodTableMap: target.ReadPointer(handle.Address + /* Module::TypeRefToMethodTableMap */));
}
```
