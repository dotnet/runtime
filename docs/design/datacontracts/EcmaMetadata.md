# Contract EcmaMetadata

This contract provides methods to get a view of the ECMA-335 metadata for a given module.

## APIs of contract

```csharp
TargetSpan GetReadOnlyMetadataAddress(ModuleHandle handle);
TargetSpan GetReadWriteSavedMetadataAddress(ModuleHandle handle);
System.Reflection.Metadata.MetadataReader? GetMetadata(ModuleHandle handle);
byte[] GetReadWriteMetadata(ModuleHandle handle);
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
| `Module` | `MetadataGeneration` | Counter incremented each time a module's metadata changes |
| `Module` | `FieldDefToDescMap` | Mapping table |
| `DynamicMetadata` | `Size` | Size of the dynamic metadata blob (as a 32bit uint) |
| `DynamicMetadata` | `Data` | Start of dynamic metadata data array |
| `PEAssembly` | `MDImport` | An `MDInternalRW` when module has writable metadata |
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

Contracts used:
| Contract Name |
| --- |
| `Loader` |


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

    // Webcil (flat) images -- e.g. a ReadyToRun corelib on WASM -- are a stripped/rewrapped PE that
    // cannot be parsed as a standard PE. They begin with the magic 'WbIL'. For those, the webcil
    // header's PeCliHeaderRva locates the CLI (COR20) header, whose metadata directory (RVA + size at
    // offset 8) locates the ECMA-335 metadata. RVAs are resolved via the loader's webcil-aware
    // GetILAddr. For non-webcil images, read the CLI header from the PE headers as below.

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
            // Reconstruct a contiguous ECMA-335 image from the module's writable
            // (MDInternalRW) metadata and return a reader over it.
            byte[] data = GetReadWriteMetadata(handle);
            return MetadataReaderProvider.FromMetadataImage(ImmutableCollectionsMarshal.AsImmutableArray(data)).GetMetadataReader();
        }
    }
}

// Reconstructs the module's writable (MDInternalRW) metadata as a single contiguous
// ECMA-335 metadata image. The result is cached per module and reused until the
// module's metadata generation counter (Module::MetadataGeneration) changes.
byte[] GetReadWriteMetadata(ModuleHandle handle)
{
    // If a blob was previously built for this handle and the module's metadata
    // generation counter is unchanged, return the cached blob.

    // Get the module's PEAssembly from the Loader contract.
    // Read PEAssembly::MDImport as an MDInternalRW.
    // Read MDInternalRW::Stgdb as a CLiteWeightStgdbRW.
    // Read the embedded CLiteWeightStgdbRW::MiniMd as a CMiniMdRW.
    // Read CMiniMdRW::Schema as a CMiniMdSchema.
    //
    // Validate that CMiniMdRW::TableCount does not exceed the ECMA-335 table count.
    // For each table, read its row count from CMiniMdSchema::RecordCounts.
    // For each table, test its bit in CMiniMdSchema::Sorted to determine whether it is sorted.
    // Decode CMiniMdSchema::Heaps to determine whether the string, GUID, and blob heaps use large indexes.
    // Record CMiniMdRW::All4ByteColumns so the reconstructed image can preserve fixed-width variable columns.
    //
    // To read a storage pool:
    // Read the pool head using the StgPool descriptor.
    // Record the head segment's SegData and DataSize.
    // Follow NextSegment until it is null, reading each remaining node as a StgPoolSeg.
    // Record each non-empty segment's SegData and DataSize.
    // Allocate one byte array large enough for all recorded segments.
    // Read each segment into the array in chain order to produce one contiguous blob.
    //
    // Read CMiniMdRW::StringHeap as a storage pool.
    // Read CMiniMdRW::BlobHeap as a storage pool.
    // Read CMiniMdRW::UserStringHeap as a storage pool.
    // Read CMiniMdRW::GuidHeap as a storage pool.
    // For each table, read CMiniMdRW::Tables[i] as a storage pool containing that table's records.
    // Read the metadata version string from CLiteWeightStgdbRW::MetadataAddress.
    // Combine the schema, heaps, and table record blobs into a TargetEcmaMetadata value.
    //
    // Create a builder for a new contiguous ECMA-335 metadata image.
    // Write the metadata root header and version string.
    // Add stream headers for #Strings, #Blob, #GUID, #US, and the uncompressed tables stream #-.
    // If all variable-width columns are 4 bytes, also add the #JTD marker
    // stream. The official ECMA-335 metadata format doesn't encode columns this
    // way but System.Reflection.Metadata does support this encoding variation
    // when it observes the #JTD marker stream.
    // See [MetadataReader](https://github.com/dotnet/runtime/blob/1b945942604aa94b4717243b6d301a17b7ae41f1/src/libraries/System.Reflection.Metadata/src/System/Reflection/Metadata/MetadataReader.cs#L166)
    // Append the string, blob, GUID, and user-string heap data and fill in their stream offsets.
    //
    // Begin the #- tables stream.
    // Write the tables stream header and the heap-size flags from the reconstructed schema.
    // Build the valid-table mask from tables with non-zero row counts.
    // Build the sorted-table mask from the schema's per-table sorted flags.
    // Write the valid and sorted masks.
    // Write the row count for each valid table.
    // Append each table's contiguous record blob in table-number order.
    // Fill in the final tables stream offset and size.
    //
    // Cache the reconstructed image against the module's current metadata generation
    // counter and return it.
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
    AvailableMetadataType flags = AvailableMetadataType.None;

    TargetPointer dynamicMetadata = Target.ReadPointer(handle.Address + /* Module::DynamicMetadata offset */);
    uint metadataGeneration = Target.Read<uint>(handle.Address + /* Module::MetadataGeneration offset */);

    if (dynamicMetadata != TargetPointer.Null)
    {
        flags |= AvailableMetadataType.ReadWriteSavedCopy;
    }
    else if (metadataGeneration != 0)
    {
        flags |= AvailableMetadataType.ReadWrite;
    }
    else
    {
        flags |= AvailableMetadataType.ReadOnly;
    }

    return flags;
}

TargetSpan GetReadWriteSavedMetadataAddress(ModuleHandle handle)
{
    TargetPointer dynamicMetadata = Target.ReadPointer(handle.Address + /* Module::DynamicMetadata offset */);
    ulong size = Target.Read<uint>(handle.Address + /* DynamicMetadata::Size offset */);
    TargetPointer result = handle.Address + /* DynamicMetadata::Data offset */;
    return new(result, size);
}
```
