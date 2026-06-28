# Contract EcmaMetadata

This contract provides methods to get a view of the ECMA-335 metadata for a given module.

## APIs of contract

```csharp
TargetSpan GetReadOnlyMetadataAddress(ModuleHandle handle);
System.Reflection.Metadata.MetadataReader? GetMetadata(ModuleHandle handle);
```

Types from other contracts:

| Type | Contract |
|------|----------|
| ModuleHandle | [Loader](./Loader.md#apis-of-contract) |

## Version 1


Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Module` | `Base` | Pointer to start of PE file in memory |
| `Module` | `DynamicMetadata` | Pointer to saved metadata for reflection emit modules |
| `Module` | `MetadataGeneration` | Counter incremented each time a dynamic module re-serializes its saved metadata copy |
| `Module` | `FieldDefToDescMap` | Mapping table |
| `Module` | `PEAssembly` | Pointer to the module's `PEAssembly` |
| `Module` | `Flags` | Module transient flags |
| `EditAndContinueModule` | `ApplyChangesCount` | Counter incremented each time an EnC edit is applied |
| `DynamicMetadata` | `Size` | Size of the dynamic metadata blob (as a 32bit uint) |
| `DynamicMetadata` | `Data` | Start of dynamic metadata data array |
| `PEAssembly` | `MDImportIsRW` | Non-zero when `MDImport` is a read-write importer |
| `PEAssembly` | `MDImport` | An `MDInternalRW` when `MDImportIsRW` is set |
| `MDInternalRW` | `Stgdb` | Pointer to the read-write storage database |
| `CLiteWeightStgdbRW` | `MiniMd` | Address of the embedded `CMiniMdRW` model |
| `CLiteWeightStgdbRW` | `MetadataAddress` | Pointer to the metadata image |
| `CMiniMdRW` | `Schema` | Address of the embedded `CMiniMdSchema` |
| `CMiniMdRW` | `TableCount` | Number of valid tables |
| `CMiniMdRW` | `All4ByteColumns` | Whether all variable-width columns are 4 bytes wide |
| `CMiniMdRW` | `Tables` | Address of the first table's record storage pool |
| `CMiniMdRW` | `StringHeap` | Address of the string heap's storage pool |
| `CMiniMdRW` | `BlobHeap` | Address of the blob heap's storage pool |
| `CMiniMdRW` | `UserStringHeap` | Address of the user-string heap's storage pool |
| `CMiniMdRW` | `GuidHeap` | Address of the GUID heap's storage pool |
| `CMiniMdSchema` | `Heaps` | Heap-size flags byte |
| `CMiniMdSchema` | `Sorted` | Sorted-table bit mask |
| `CMiniMdSchema` | `RecordCounts` | Address of the inline per-table row count array |
| `StgPool` | `SegData` | Pointer to the head segment's data |
| `StgPool` | `NextSegment` | Pointer to the next pool segment |
| `StgPool` | `DataSize` | Live byte count of the head segment |
| `StgPoolSeg` | `SegData` | Pointer to this extension segment's data |
| `StgPoolSeg` | `NextSegment` | Pointer to the next pool segment, or null |
| `StgPoolSeg` | `DataSize` | Live byte count of this extension segment |

### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `ModuleFlagsEncCapable` | uint | `Module` transient-flags bit (`IS_ENC_CAPABLE`) indicating the module is an `EditAndContinueModule` | `0x200` |


```csharp
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

TargetSpan GetReadOnlyMetadataAddress(ModuleHandle handle)
{
    TargetPointer baseAddress = Target.ReadPointer(handle.Address + /* Module::Base offset */);
    if (baseAddress == TargetPointer.Null)
    {
        return default;
    }

    // Read CLR header per https://learn.microsoft.com/windows/win32/debug/pe-format
    ulong clrHeaderRVA = ...

    // Read Metadata per ECMA-335 II.25.3.3 CLI Header
    ulong metadataDirectoryAddress = baseAddress + clrHeaderRva + /* offset to Metadata */
    int rva = Target.Read<int>(metadataDirectoryAddress);
    ulong size = Target.Read<int>(metadataDirectoryAddress + sizeof(int));
    return new(baseAddress + rva, size);
}

MetadataReader? GetMetadata(ModuleHandle handle)
{
    AvailableMetadataType type = GetAvailableMetadataType(handle);

    switch (type)
    {
        case AvailableMetadataType.None:
            return null;
        case AvailableMetadataType.ReadOnly:
        {
            TargetSpan address = GetReadOnlyMetadataAddress(handle);
            byte[] data = new byte[address.Size];
            _target.ReadBuffer(address.Address, data);
            return MetadataReaderProvider.FromMetadataImage(ImmutableCollectionsMarshal.AsImmutableArray(data)).GetMetadataReader();
        }
        case AvailableMetadataType.ReadWriteSavedCopy:
        {
            TargetSpan address = GetReadWriteSavedMetadataAddress(handle);
            byte[] data = new byte[address.Size];
            _target.ReadBuffer(address.Address, data);
            return MetadataReaderProvider.FromMetadataImage(ImmutableCollectionsMarshal.AsImmutableArray(data)).GetMetadataReader();
        }
        case AvailableMetadataType.ReadWrite:
        {
            // From the ModuleHandle, walk Module -> PEAssembly -> MDInternalRW -> CLiteWeightStgdbRW -> CMiniMdRW.
            // Read the schema and whether every variable-width column is 4 bytes, which selects the #JTD "minimal delta" marker when reserializing.
            // Each heap and each entry of the Tables array is a storage pool; read its head segment with the
            // StgPool descriptor and walk the rest of the chain as bare StgPoolSeg via NextSegment, concatenating every segment's
            // data into a contiguous buffer. The resulting heaps and per-table record blobs,
            // together with the schema, are returned as TargetEcmaMetadata and reserialized into an ECMA-335 image.
        }
    }
}
```

### Helper Methods

``` csharp

[Flags]
enum AvailableMetadataType
{
    None = 0,
    ReadOnly = 1,
    ReadWriteSavedCopy = 2,
    ReadWrite = 4
}

AvailableMetadataType GetAvailableMetadataType(ModuleHandle handle)
{
    Data.Module module = new Data.Module(Target, handle.Address);

    AvailableMetadataType flags = AvailableMetadataType.None;

    TargetPointer dynamicMetadata = Target.ReadPointer(handle.Address + /* Module::DynamicMetadata offset */);

    if (dynamicMetadata != TargetPointer.Null)
    {
        flags |= AvailableMetadataType.ReadWriteSavedCopy;
    }
    else if (UseReadWriteMetadata(module))
    {
        flags |= AvailableMetadataType.ReadWrite;
    }
    else
    {
        flags |= AvailableMetadataType.ReadOnly;
    }

    return flags;
}

bool UseReadWriteMetadata(ModuleHandle handle)
{
    TargetPointer PEAssembly = Target.ReadPointer(handle.Address + /* Module::PEAssembly offset */);
    if (PEAssembly == TargetPointer.Null)
        return false;

    bool isEnCCapable = (Target.Read<uint>(handle.Address + /* Module::Flags offset */) & ModuleFlagsEncCapable) != 0;
    bool hasRWMetadata = Target.Read<uint>(PEAssembly + /* PEAssembly::MDImportIsRW offset */) != 0;
    bool hasMDImport = Target.ReadPointer(PEAssembly + /* PEAssembly::MDImport offset */) != TargetPointer.Null;
    return hasRWMetadata && hasMDImport && isEnCCapable;
}

TargetSpan GetReadWriteSavedMetadataAddress(ModuleHandle handle)
{
    TargetPointer dynamicMetadata = Target.ReadPointer(handle.Address + /* Module::DynamicMetadata offset */);
    ulong size = Target.Read<uint>(handle.Address + /* DynamicMetadata::Size offset */);
    TargetPointer result = handle.Address + /* DynamicMetadata::Data offset */;
    return new(result, size);
}
```
