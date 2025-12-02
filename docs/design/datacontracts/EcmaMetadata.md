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
| `Module` | `FieldDefToDescMap` | Mapping table |
| `DynamicMetadata` | `Size` | Size of the dynamic metadata blob (as a 32bit uint) |
| `DynamicMetadata` | `Data` | Start of dynamic metadata data array |


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
            var targetEcmaMetadata = GetReadWriteMetadata(handle);

            // From the multiple different target spans, we need to build a single
            // contiguous ECMA-335 metadata blob.
            BlobBuilder builder = new BlobBuilder();
            builder.WriteUInt32(0x424A5342);

            // major version
            builder.WriteUInt16(1);

            // minor version
            builder.WriteUInt16(1);

            // reserved
            builder.WriteUInt32(0);

            string version = targetEcmaMetadata.Schema.MetadataVersion;
            builder.WriteInt32(AlignUp(version.Length, 4));
            Write4ByteAlignedString(builder, version);

            // reserved
            builder.WriteUInt16(0);

            // number of streams
            ushort numStreams = 5; // #Strings, #US, #Blob, #GUID, #~ (metadata)
            if (targetEcmaMetadata.Schema.VariableSizedColumnsAreAll4BytesLong)
            {
                // We direct MetadataReader to use 4-byte encoding for all variable-sized columns
                // by providing the marker stream for a "minimal delta" image.
                numStreams++;
            }
            builder.WriteUInt16(numStreams);

            // Write Stream headers
            if (targetEcmaMetadata.Schema.VariableSizedColumnsAreAll4BytesLong)
            {
                // Write the #JTD stream to indicate that all variable-sized columns are 4 bytes long.
                WriteStreamHeader(builder, "#JTD", 0).WriteInt32(builder.Count);
            }

            BlobWriter stringsOffset = WriteStreamHeader(builder, "#Strings", (int)AlignUp(targetEcmaMetadata.StringHeap.Size, 4ul));
            BlobWriter blobOffset = WriteStreamHeader(builder, "#Blob", (int)targetEcmaMetadata.BlobHeap.Size);
            BlobWriter guidOffset = WriteStreamHeader(builder, "#GUID", (int)targetEcmaMetadata.GuidHeap.Size);
            BlobWriter userStringOffset = WriteStreamHeader(builder, "#US", (int)targetEcmaMetadata.UserStringHeap.Size);

            // We'll use the "uncompressed" tables stream name as the runtime may have created the *Ptr tables
            // that are only present in the uncompressed tables stream.
            BlobWriter tablesOffset = WriteStreamHeader(builder, "#-", 0);

            // Write the heap-style Streams

            stringsOffset.WriteInt32(builder.Count);
            WriteTargetSpan(builder, targetEcmaMetadata.StringHeap);
            for (ulong i = targetEcmaMetadata.StringHeap.Size; i < AlignUp(targetEcmaMetadata.StringHeap.Size, 4ul); i++)
            {
                builder.WriteByte(0);
            }

            blobOffset.WriteInt32(builder.Count);
            WriteTargetSpan(builder, targetEcmaMetadata.BlobHeap);

            guidOffset.WriteInt32(builder.Count);
            WriteTargetSpan(builder, targetEcmaMetadata.GuidHeap);

            userStringOffset.WriteInt32(builder.Count);
            WriteTargetSpan(builder, targetEcmaMetadata.UserStringHeap);

            // Write tables stream
            tablesOffset.WriteInt32(builder.Count);

            // Write tables stream header
            builder.WriteInt32(0); // reserved
            builder.WriteByte(2); // major version
            builder.WriteByte(0); // minor version
            uint heapSizes =
                (targetEcmaMetadata.Schema.LargeStringHeap ? 1u << 0 : 0) |
                (targetEcmaMetadata.Schema.LargeBlobHeap ? 1u << 1 : 0) |
                (targetEcmaMetadata.Schema.LargeGuidHeap ? 1u << 2 : 0);

            builder.WriteByte((byte)heapSizes);
            builder.WriteByte(1); // reserved

            ulong validTables = 0;
            for (int i = 0; i < targetEcmaMetadata.Schema.RowCount.Length; i++)
            {
                if (targetEcmaMetadata.Schema.RowCount[i] != 0)
                {
                    validTables |= 1ul << i;
                }
            }

            ulong sortedTables = 0;
            for (int i = 0; i < targetEcmaMetadata.Schema.IsSorted.Length; i++)
            {
                if (targetEcmaMetadata.Schema.IsSorted[i])
                {
                    sortedTables |= 1ul << i;
                }
            }

            builder.WriteUInt64(validTables);
            builder.WriteUInt64(sortedTables);

            foreach (int rowCount in targetEcmaMetadata.Schema.RowCount)
            {
                if (rowCount > 0)
                {
                    builder.WriteInt32(rowCount);
                }
            }

            // Write the tables
            foreach (TargetSpan span in targetEcmaMetadata.Tables)
            {
                WriteTargetSpan(builder, span);
            }

            MemoryStream metadataStream = new MemoryStream();
            builder.WriteContentTo(metadataStream);
            return MetadataReaderProvider.FromMetadataStream(metadataStream).GetMetadataReader();

            void WriteTargetSpan(BlobBuilder builder, TargetSpan span)
            {
                Blob blob = builder.ReserveBytes(checked((int)span.Size));
                _target.ReadBuffer(span.Address, blob.GetBytes().AsSpan());
            }

            static BlobWriter WriteStreamHeader(BlobBuilder builder, string name, int size)
            {
                BlobWriter offset = new(builder.ReserveBytes(4));
                builder.WriteInt32(size);
                Write4ByteAlignedString(builder, name);
                return offset;
            }

            static void Write4ByteAlignedString(BlobBuilder builder, string value)
            {
                int bufferStart = builder.Count;
                builder.WriteUTF8(value);
                builder.WriteByte(0);
                int stringEnd = builder.Count;
                for (int i = stringEnd; i < bufferStart + AlignUp(value.Length, 4); i++)
                {
                    builder.WriteByte(0);
                }
            }
        }
    }
}
```

### Helper Methods

``` csharp
using System;
using System.Numerics;

struct EcmaMetadataSchema
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

class TargetEcmaMetadata
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
        flags |= AvailableMetadataType.ReadWriteSavedCopy;
    else
        flags |= AvailableMetadataType.ReadOnly;

    return flags;
}

TargetSpan GetReadWriteSavedMetadataAddress(ModuleHandle handle)
{
    Data.Module module = new Data.Module(Target, handle.Address);
    TargetPointer dynamicMetadata = Target.ReadPointer(handle.Address + /* Module::DynamicMetadata offset */);

    ulong size = Target.Read<uint>(handle.Address + /* DynamicMetadata::Size offset */);
    TargetPointer result = handle.Address + /* DynamicMetadata::Data offset */;
    return new(result, size);
}

TargetEcmaMetadata GetReadWriteMetadata(ModuleHandle handle)
{
    // [cdac] TODO.
}

T AlignUp<T>(T input, T alignment)
    where T : IBinaryInteger<T>
{
    return input + (alignment - T.One) & ~(alignment - T.One);
}
```
