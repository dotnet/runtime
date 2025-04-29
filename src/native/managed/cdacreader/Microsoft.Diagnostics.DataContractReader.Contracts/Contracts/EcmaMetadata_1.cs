// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class EcmaMetadata_1(Target target) : IEcmaMetadata
{
    private Dictionary<ModuleHandle, MetadataReaderProvider?> _metadata = new();

    public TargetSpan GetReadOnlyMetadataAddress(ModuleHandle handle)
    {
        Data.Module module = target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);

        TargetPointer baseAddress = module.GetLoadedMetadata(out ulong size);

        return new TargetSpan(baseAddress, size);
    }

    public MetadataReader? GetMetadata(ModuleHandle handle)
    {
        if (_metadata.TryGetValue(handle, out MetadataReaderProvider? result))
        {
            return result?.GetMetadataReader();
        }
        else
        {
            MetadataReaderProvider? provider = GetMetadataProvider(handle);
            _metadata.Add(handle, provider);
            return provider?.GetMetadataReader();
        }
    }

    private MetadataReaderProvider? GetMetadataProvider(ModuleHandle handle)
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
                target.ReadBuffer(address.Address, data);
                return MetadataReaderProvider.FromMetadataImage(ImmutableCollectionsMarshal.AsImmutableArray(data));
            }
            case AvailableMetadataType.ReadWriteSavedCopy:
            {
                TargetSpan address = GetReadWriteSavedMetadataAddress(handle);
                byte[] data = new byte[address.Size];
                target.ReadBuffer(address.Address, data);
                return MetadataReaderProvider.FromMetadataImage(ImmutableCollectionsMarshal.AsImmutableArray(data));
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
                return MetadataReaderProvider.FromMetadataStream(metadataStream);

                void WriteTargetSpan(BlobBuilder builder, TargetSpan span)
                {
                    Blob blob = builder.ReserveBytes(checked((int)span.Size));
                    target.ReadBuffer(span.Address, blob.GetBytes().AsSpan());
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
            default:
                throw new NotImplementedException();
        }
    }

    private struct EcmaMetadataSchema
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

    private sealed class TargetEcmaMetadata
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
    private enum AvailableMetadataType
    {
        None = 0,
        ReadOnly = 1,
        ReadWriteSavedCopy = 2,
        ReadWrite = 4
    }

    private AvailableMetadataType GetAvailableMetadataType(ModuleHandle handle)
    {
        Data.Module module = target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);

        AvailableMetadataType flags = AvailableMetadataType.None;

        if (module.DynamicMetadata != TargetPointer.Null)
            flags |= AvailableMetadataType.ReadWriteSavedCopy;
        else
            flags |= AvailableMetadataType.ReadOnly;

        // TODO(cdac) implement direct reading of unsaved ReadWrite metadata
        return flags;
    }

    private TargetSpan GetReadWriteSavedMetadataAddress(ModuleHandle handle)
    {
        Data.Module module = target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        Data.DynamicMetadata dynamicMetadata = target.ProcessedData.GetOrAdd<Data.DynamicMetadata>(module.DynamicMetadata);

        return new TargetSpan(dynamicMetadata.Data, dynamicMetadata.Size);
    }

    private TargetEcmaMetadata GetReadWriteMetadata(ModuleHandle handle)
    {
        throw new NotImplementedException();
    }

    private static T AlignUp<T>(T input, T alignment)
        where T : IBinaryInteger<T>
    {
        return input + (alignment - T.One) & ~(alignment - T.One);
    }
}
