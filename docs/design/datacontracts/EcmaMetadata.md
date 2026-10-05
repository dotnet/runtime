# Contract EcmaMetadata

This contract provides methods to get a view of the ECMA-335 metadata for a given module.

## APIs of contract

```csharp
bool HasReadWriteMetadata(TargetPointer peAssembly);
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


<!-- BEGIN GENERATED: usage contract=EcmaMetadata version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `CLiteWeightStgdbRW` | `MetadataAddress` | `pointer` | Pointer to the metadata image |
| `CLiteWeightStgdbRW` | `MiniMd` | `pointer` | Address of the embedded `CMiniMdRW` model |
| `CMiniMdRW` | `All4ByteColumns` | `uint32` | Whether all variable-width columns are 4 bytes wide |
| `CMiniMdRW` | `BlobHeap` | `pointer` | Address of the blob heap's storage pool |
| `CMiniMdRW` | `GuidHeap` | `pointer` | Address of the GUID heap's storage pool |
| `CMiniMdRW` | `Schema` | `pointer` | Address of the embedded `CMiniMdSchema` |
| `CMiniMdRW` | `StringHeap` | `pointer` | Address of the string heap's storage pool |
| `CMiniMdRW` | `TableCount` | `uint32` | Number of valid tables |
| `CMiniMdRW` | `Tables` | `pointer` | Address of the first table's record storage pool |
| `CMiniMdRW` | `UserStringHeap` | `pointer` | Address of the user-string heap's storage pool |
| `CMiniMdSchema` | `Heaps` | `uint8` | Heap-size flags byte |
| `CMiniMdSchema` | `RecordCounts` | `pointer` | Address of the inline per-table row count array |
| `CMiniMdSchema` | `Sorted` | `uint64` | Sorted-table bit mask |
| `DynamicMetadata` | `Data` | `pointer` | Start of dynamic metadata data array |
| `DynamicMetadata` | `Size` | `uint32` | Size of the dynamic metadata blob (as a 32bit uint) |
| `ImageDataDirectory` | `Size` | `uint32` | Size of the data |
| `ImageDataDirectory` | `VirtualAddress` | `uint32` | Virtual address of the image data directory |
| `MDInternalRW` | `Stgdb` | `pointer` | Pointer to the read-write storage database |
| `Module` | `DynamicMetadata` | `pointer` | Pointer to metadata updated dynamically through Edit and Continue |
| `Module` | `MetadataGeneration` | `uint32` | Counter incremented each time a module's metadata changes |
| `Module` | `PEAssembly` | `pointer` | Pointer to the module's PE assembly |
| `PEAssembly` | `MDImport` | `pointer` | An `MDInternalRW` when module has writable metadata |
| `PEAssembly` | `MDImportIsRW` | `int32` | Whether the metadata import supports updates |
| `StgPool` | `DataSize` | `uint32` | Live byte count of the head segment |
| `StgPool` | `NextSegment` | `pointer` | Pointer to the next pool segment |
| `StgPool` | `SegData` | `pointer` | Pointer to the head segment's data |
| `StgPoolSeg` | `DataSize` | `uint32` | Live byte count of this extension segment |
| `StgPoolSeg` | `NextSegment` | `pointer` | Pointer to the next pool segment, or null |
| `StgPoolSeg` | `SegData` | `pointer` | Pointer to this extension segment's data |
| `TableRW` | *(type size)* | `uint32` | Size in bytes of each TableRW entry in the CMiniMdRW tables array |

### Global variables used

_None._

### Contracts used

| Contract Name |
| --- |
| `Loader` |
<!-- END GENERATED: usage contract=EcmaMetadata version=c1 -->


```csharp
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

bool HasReadWriteMetadata(TargetPointer peAssembly)
{
    return Target.Read<int>(peAssembly + /* PEAssembly::MDImportIsRW offset */) != 0;
}

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

## Version 2

<!-- BEGIN GENERATED: usage contract=EcmaMetadata version=c2 diff-from=c1 -->
### Data descriptor changes from `c1`

| Change | Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- | --- |
| Removed | `CLiteWeightStgdbRW` | `MetadataAddress` | `pointer` | Pointer to the metadata image |
| Removed | `CLiteWeightStgdbRW` | `MiniMd` | `pointer` | Address of the embedded `CMiniMdRW` model |
| Removed | `CMiniMdRW` | `All4ByteColumns` | `uint32` | Whether all variable-width columns are 4 bytes wide |
| Removed | `CMiniMdRW` | `BlobHeap` | `pointer` | Address of the blob heap's storage pool |
| Removed | `CMiniMdRW` | `GuidHeap` | `pointer` | Address of the GUID heap's storage pool |
| Removed | `CMiniMdRW` | `Schema` | `pointer` | Address of the embedded `CMiniMdSchema` |
| Removed | `CMiniMdRW` | `StringHeap` | `pointer` | Address of the string heap's storage pool |
| Removed | `CMiniMdRW` | `TableCount` | `uint32` | Number of valid tables |
| Removed | `CMiniMdRW` | `Tables` | `pointer` | Address of the first table's record storage pool |
| Removed | `CMiniMdRW` | `UserStringHeap` | `pointer` | Address of the user-string heap's storage pool |
| Removed | `CMiniMdSchema` | `Heaps` | `uint8` | Heap-size flags byte |
| Removed | `CMiniMdSchema` | `RecordCounts` | `pointer` | Address of the inline per-table row count array |
| Removed | `CMiniMdSchema` | `Sorted` | `uint64` | Sorted-table bit mask |
| Added | `DNMDContext` | `BlobHeap` | `pointer` | Address of the inline DNMDData describing the #Blob heap |
| Added | `DNMDContext` | `Flags` | `uint32` | Heap index widths, minimal-delta mode, and tables stream format |
| Added | `DNMDContext` | `GuidHeap` | `pointer` | Address of the inline DNMDData describing the #GUID heap |
| Added | `DNMDContext` | `Magic` | `uint32` | Value identifying a valid DNMD metadata context |
| Added | `DNMDContext` | `StringsHeap` | `pointer` | Address of the inline DNMDData describing the #Strings heap |
| Added | `DNMDContext` | `Tables` | `pointer` | Pointer to the array of DNMD table descriptors |
| Added | `DNMDContext` | `UserStringHeap` | `pointer` | Address of the inline DNMDData describing the #US heap |
| Added | `DNMDContext` | `Version` | `pointer` | Pointer to the null-terminated UTF-8 metadata version |
| Added | `DNMDData` | `Ptr` | `pointer` | Pointer to the current heap or table row bytes |
| Added | `DNMDData` | `Size` | `nuint` | Live byte count of the heap or table rows |
| Added | `DNMDTable` | `AddingNewRow` | `uint8` | Nonzero while a new row has not been committed |
| Added | `DNMDTable` | `Context` | `pointer` | Pointer to the owning DNMD metadata context for an initialized table |
| Added | `DNMDTable` | `Data` | `pointer` | Address of the inline DNMDData describing this table's rows |
| Added | `DNMDTable` | `RowCount` | `uint32` | Number of rows in the table |
| Added | `DNMDTable` | `RowSize` | `uint8` | Byte width of a row in this table |
| Added | `DNMDTable` | `Sorted` | `uint8` | Whether this table's rows remain sorted |
| Added | `DNMDTable` | `TableId` | `uint8` | ECMA-335 table index |
| Removed | `MDInternalRW` | `Stgdb` | `pointer` | Pointer to the read-write storage database |
| Added | `PEAssembly` | `DNMDMetadataHandleSlot` | `pointer` | Address of the current DNMD metadata handle pointer, updated when the handle is replaced |
| Removed | `PEAssembly` | `MDImport` | `pointer` | An `MDInternalRW` when module has writable metadata |
| Removed | `StgPool` | `DataSize` | `uint32` | Live byte count of the head segment |
| Removed | `StgPool` | `NextSegment` | `pointer` | Pointer to the next pool segment |
| Removed | `StgPool` | `SegData` | `pointer` | Pointer to the head segment's data |
| Removed | `StgPoolSeg` | `DataSize` | `uint32` | Live byte count of this extension segment |
| Removed | `StgPoolSeg` | `NextSegment` | `pointer` | Pointer to the next pool segment, or null |
| Removed | `StgPoolSeg` | `SegData` | `pointer` | Pointer to this extension segment's data |
| Removed | `TableRW` | *(type size)* | `uint32` | Size in bytes of each TableRW entry in the CMiniMdRW tables array |

### Global variable changes from `c1`

| Change | Global | Type | Meaning |
| --- | --- | --- | --- |
| Added | `DNMDContextMagic` | `uint32` | Magic value used to validate a DNMD metadata context |
| Added | `DNMDLargeBlobHeap` | `uint32` | Flag for four-byte #Blob heap indexes |
| Added | `DNMDLargeGuidHeap` | `uint32` | Flag for four-byte #GUID heap indexes |
| Added | `DNMDLargeStringHeap` | `uint32` | Flag for four-byte #Strings heap indexes |
| Added | `DNMDMinimalDelta` | `uint32` | Flag for four-byte table and coded indexes, indicated by a #JTD stream |
| Added | `DNMDTableCount` | `uint32` | Number of DNMD table descriptors in a metadata context |
| Added | `DNMDUncompressedTables` | `uint32` | Flag indicating the tables stream is #- rather than #~ |

### Contract dependency changes from `c1`

_No changes._
<!-- END GENERATED: usage contract=EcmaMetadata version=c2 diff-from=c1 -->

Version 2 retains the read-only and saved-copy behavior of version 1. For live read/write
metadata, CoreCLR provides `PEAssembly.DNMDMetadataHandleSlot`, the address of a pointer-sized
slot in the DNMD metadata owner. The slot contains the current `mdcxt_t` address and remains
stable when EnC or reopen replaces the handle. The contract reads this slot instead of
interpreting `PEAssembly.MDImport` as a legacy `MDInternalRW` object.

The DNMD sub-descriptor provides:

| Type | Fields used |
| --- | --- |
| `DNMDContext` | `Magic`, `Flags`, `Version`, `Tables`, and inline `DNMDData` for the `Strings`, `Blob`, `Guid`, and `UserString` heaps |
| `DNMDData` | `Ptr` and `Size` for a live heap or table data region |
| `DNMDTable` | Inline `Data` (`DNMDData`), `RowCount`, `RowSize`, `Sorted`, `AddingNewRow`, `TableId`, and `Context` |

Its `DNMDTableCount`, `DNMDContextMagic`, and heap/table flag globals specify the table
count, handle validation, heap index widths, minimal-delta mode, and compressed versus
uncompressed tables stream. Read table data from each table's current `Data.Ptr` and `Data.Size`;
the original metadata image does not reflect subsequent edits. Reject an incomplete row or
inconsistent row count, row width, or data length.

Reconstruct a contiguous ECMA-335 image using the same serialization as version 1, but
preserve DNMD's current heap index widths and use `#~` or `#-` according to
`DNMDUncompressedTables`. Include `#JTD` **only** when `DNMDMinimalDelta` indicates
four-byte table and coded indexes; heap index widths still follow the DNMD heap-size
flags. Ordinary writable DNMD metadata omits `#JTD`.
Cache the resulting image by both `Module.MetadataGeneration` and the current metadata
handle address. Retain version 1's handling of read-only images and saved dynamic metadata.

```csharp
// Reconstructs live DNMD metadata as a contiguous ECMA-335 image.
byte[] GetReadWriteMetadata(ModuleHandle handle)
{
    // Require read/write metadata. Read Module.MetadataGeneration and the current
    // DNMD handle; return the cached image only if both still match.
    //
    // Get the PEAssembly from the Loader contract. Read the pointer-sized slot
    // at PEAssembly.DNMDMetadataHandleSlot to find the current DNMDContext.
    // Reject a missing handle or a context whose Magic != DNMDContextMagic.
    // Validate DNMDTableCount against the ECMA-335 table limit and require
    // DNMDContext.Tables to point to the table array.
    //
    // For each DNMDTableCount entry at Tables + index * the DNMDTable type size:
    // Reject AddingNewRow and record RowCount, including zero for empty tables.
    // For nonempty tables, validate Context, TableId, RowSize, and Sorted.
    // Read the inline DNMDData at Data; before allocating, require
    // Size == RowCount * RowSize, Size within the component limit, and a
    // nonnull Ptr; then copy the live table rows.
    // Record the sorted flag for each nonempty table.
    //
    // Read DNMDContext.Version as a strict UTF-8 string, rejecting a null pointer.
    // Read the inline DNMDData at StringsHeap, BlobHeap, GuidHeap, and
    // UserStringHeap. Reject any region whose Size exceeds the component limit
    // or whose nonzero Size has a null Ptr; copy Size bytes from each Ptr.
    // These live views reflect edits; the original metadata image does not.
    //
    // Decode DNMDContext.Flags using DNMDLargeStringHeap, DNMDLargeGuidHeap,
    // and DNMDLargeBlobHeap for heap index widths, DNMDMinimalDelta for
    // four-byte table and coded indexes, and DNMDUncompressedTables for
    // the #- versus #~ tables stream.
    // Combine the version, flags, row counts, sorted flags, and live data
    // into a TargetEcmaMetadata value.
    //
    // Use version 1's serializer to write the metadata root and version,
    // #Strings, #Blob, #GUID, #US, and #- or #~ streams. Write the tables
    // stream's heap-size flags, valid/sorted masks, row counts, and rows
    // in table order. Include #JTD only for a minimal delta; heap index
    // widths still follow the DNMD heap-size flags.
    //
    // Cache the image by Module.MetadataGeneration and the current DNMD
    // handle address, then return it.
}
```
