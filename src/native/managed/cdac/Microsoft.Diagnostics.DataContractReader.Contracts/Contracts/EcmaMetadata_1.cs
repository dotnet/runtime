// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class EcmaMetadata_1(Target target) : IEcmaMetadata
{
    // Heap index size flags (ECMA-335 II.24.2.6)
    private const byte HEAP_STRING_4 = 0x01;
    private const byte HEAP_GUID_4 = 0x02;
    private const byte HEAP_BLOB_4 = 0x04;
    private const uint ModuleFlagsEncCapable = 0x200;
    private readonly Dictionary<ModuleHandle, (uint Generation, MetadataReaderProvider? Provider)> _metadata = [];
    private readonly Dictionary<ModuleHandle, TargetSpan> _readOnlyMetadataAddress = [];

    public void Flush(FlushScope scope)
    {
        if (scope == FlushScope.All)
        {
            foreach ((uint _, MetadataReaderProvider? provider) in _metadata.Values)
            {
                provider?.Dispose();
            }

            _metadata.Clear();
            _readOnlyMetadataAddress.Clear();
        }
    }

    public TargetSpan GetReadOnlyMetadataAddress(ModuleHandle handle)
    {
        if (_readOnlyMetadataAddress.TryGetValue(handle, out TargetSpan cached))
            return cached;

        ILoader loader = target.Contracts.Loader;

        if (!loader.TryGetLoadedImageContents(handle, out TargetPointer baseAddress, out uint size, out uint imageFlags))
        {
            throw new InvalidOperationException("Module is not loaded.");
        }
        bool isMapped = (imageFlags & 0x1) != 0; // FLAG_MAPPED = 0x1
        PEStreamOptions isLoaded = isMapped ? PEStreamOptions.IsLoadedImage : PEStreamOptions.Default;

        TargetStream stream = new(target, baseAddress, size);
        using PEReader peReader = new PEReader(stream, isLoaded);

        int metadataStartOffset = peReader.PEHeaders.MetadataStartOffset;
        int metadataSize = peReader.PEHeaders.MetadataSize;

        TargetSpan result = new TargetSpan(baseAddress + (ulong)metadataStartOffset, (ulong)metadataSize);
        _readOnlyMetadataAddress[handle] = result;
        return result;
    }

    public MetadataReader? GetMetadata(ModuleHandle handle)
    {
        uint generation = GetMetadataGeneration(handle);

        if (_metadata.TryGetValue(handle, out (uint Generation, MetadataReaderProvider? Provider) cached))
        {
            if (cached.Generation == generation)
            {
                return cached.Provider?.GetMetadataReader();
            }
            cached.Provider?.Dispose();
        }

        MetadataReaderProvider? provider = GetMetadataProvider(handle);
        _metadata[handle] = (generation, provider);
        return provider?.GetMetadataReader();
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
                builder.WriteInt32(AlignUp(version.Length + 1, 4));
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

                BlobWriter stringsOffset = WriteStreamHeader(builder, "#Strings", (int)AlignUp((ulong)targetEcmaMetadata.StringHeap.Length, 4ul));
                BlobWriter blobOffset = WriteStreamHeader(builder, "#Blob", (int)AlignUp((ulong)targetEcmaMetadata.BlobHeap.Length, 4ul));
                BlobWriter guidOffset = WriteStreamHeader(builder, "#GUID", (int)AlignUp((ulong)targetEcmaMetadata.GuidHeap.Length, 4ul));
                BlobWriter userStringOffset = WriteStreamHeader(builder, "#US", (int)AlignUp((ulong)targetEcmaMetadata.UserStringHeap.Length, 4ul));

                // We'll use the "uncompressed" tables stream name as the runtime may have created the *Ptr tables
                // that are only present in the uncompressed tables stream.
                BlobWriter tablesOffset = new(builder.ReserveBytes(4));
                BlobWriter tablesSize = new(builder.ReserveBytes(4));
                Write4ByteAlignedString(builder, "#-");

                // Write the heap-style Streams

                stringsOffset.WriteInt32(builder.Count);
                WriteAlignedHeap(builder, targetEcmaMetadata.StringHeap);

                blobOffset.WriteInt32(builder.Count);
                WriteAlignedHeap(builder, targetEcmaMetadata.BlobHeap);

                guidOffset.WriteInt32(builder.Count);
                WriteAlignedHeap(builder, targetEcmaMetadata.GuidHeap);

                userStringOffset.WriteInt32(builder.Count);
                WriteAlignedHeap(builder, targetEcmaMetadata.UserStringHeap);

                // Write tables stream
                int tableStreamStart = builder.Count;
                tablesOffset.WriteInt32(tableStreamStart);

                // Write tables stream header
                builder.WriteInt32(0); // reserved
                // ECMA-335 II.24.2.6: MajorVersion shall be 2, MinorVersion shall be 0.
                builder.WriteByte(2); // major version
                builder.WriteByte(0); // minor version
                uint heapSizes =
                    (targetEcmaMetadata.Schema.LargeStringHeap ? (uint)HEAP_STRING_4 : 0) |
                    (targetEcmaMetadata.Schema.LargeGuidHeap ? (uint)HEAP_GUID_4 : 0) |
                    (targetEcmaMetadata.Schema.LargeBlobHeap ? (uint)HEAP_BLOB_4 : 0);

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
                foreach (byte[] table in targetEcmaMetadata.Tables)
                {
                    builder.WriteBytes(table);
                }

                // Patch the #- stream size now that the full table stream has been written.
                tablesSize.WriteInt32(builder.Count - tableStreamStart);

                MemoryStream metadataStream = new MemoryStream();
                builder.WriteContentTo(metadataStream);
                metadataStream.Position = 0;
                return MetadataReaderProvider.FromMetadataStream(metadataStream);

                static BlobWriter WriteStreamHeader(BlobBuilder builder, string name, int size)
                {
                    BlobWriter offset = new(builder.ReserveBytes(4));
                    builder.WriteInt32(size);
                    Write4ByteAlignedString(builder, name);
                    return offset;
                }

                static void WriteAlignedHeap(BlobBuilder builder, byte[] heap)
                {
                    builder.WriteBytes(heap);
                    for (int i = heap.Length; i < (int)AlignUp((ulong)heap.Length, 4ul); i++)
                    {
                        builder.WriteByte(0);
                    }
                }

                static void Write4ByteAlignedString(BlobBuilder builder, string value)
                {
                    int bufferStart = builder.Count;
                    builder.WriteUTF8(value);
                    builder.WriteByte(0);
                    int stringEnd = builder.Count;
                    // The name field occupies the null-terminated string padded to a 4-byte boundary,
                    // i.e. AlignUp(length + 1, 4) bytes (the +1 accounts for the null terminator).
                    for (int i = stringEnd; i < bufferStart + AlignUp(value.Length + 1, 4); i++)
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
                            byte[][] tables,
                            byte[] stringHeap,
                            byte[] userStringHeap,
                            byte[] blobHeap,
                            byte[] guidHeap)
        {
            Schema = schema;
            _tables = tables;
            StringHeap = stringHeap;
            UserStringHeap = userStringHeap;
            BlobHeap = blobHeap;
            GuidHeap = guidHeap;
        }

        public EcmaMetadataSchema Schema { get; init; }

        private byte[][] _tables;
        public ReadOnlySpan<byte[]> Tables => _tables;
        public byte[] StringHeap { get; init; }
        public byte[] UserStringHeap { get; init; }
        public byte[] BlobHeap { get; init; }
        public byte[] GuidHeap { get; init; }
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

    private bool UseReadWriteMetadata(Data.Module module)
    {
        if (module.PEAssembly == TargetPointer.Null)
            return false;

        Data.PEAssembly peAssembly = target.ProcessedData.GetOrAdd<Data.PEAssembly>(module.PEAssembly);
        return peAssembly.MDImportIsRW != 0 && peAssembly.MDImport != TargetPointer.Null && (module.Flags & ModuleFlagsEncCapable) != 0;
    }

    private uint GetMetadataGeneration(ModuleHandle handle)
    {
        Data.Module module = new(target, handle.Address); // do not cache

        if (module.DynamicMetadata != TargetPointer.Null)
        {
            return module.MetadataGeneration;
        }

        if (UseReadWriteMetadata(module))
        {
            Data.EditAndContinueModule encModule = new(target, handle.Address); // do not cache
            return (uint)encModule.ApplyChangesCount;
        }

        return 0;
    }

    private TargetSpan GetReadWriteSavedMetadataAddress(ModuleHandle handle)
    {
        Data.Module module = target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        Data.DynamicMetadata dynamicMetadata = target.ProcessedData.GetOrAdd<Data.DynamicMetadata>(module.DynamicMetadata);

        return new TargetSpan(dynamicMetadata.Data, dynamicMetadata.Size);
    }

    private TargetEcmaMetadata GetReadWriteMetadata(ModuleHandle handle)
    {
        Data.Module module = target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        Data.PEAssembly peAssembly = target.ProcessedData.GetOrAdd<Data.PEAssembly>(module.PEAssembly);
        Data.MDInternalRW mdRW = target.ProcessedData.GetOrAdd<Data.MDInternalRW>(peAssembly.MDImport);
        Data.CLiteWeightStgdbRW stgdb = target.ProcessedData.GetOrAdd<Data.CLiteWeightStgdbRW>(mdRW.Stgdb);
        Data.CMiniMdRW miniMd = target.ProcessedData.GetOrAdd<Data.CMiniMdRW>(stgdb.MiniMd);
        Data.CMiniMdSchema schema = new(target, miniMd.Schema);

        int tableCount = checked((int)miniMd.TableCount);
        if ((uint)tableCount > (uint)MetadataTokens.TableCount)
        {
            throw new InvalidOperationException($"Unexpected metadata table count {tableCount}.");
        }

        // ECMA-335 II.24.2.6
        int[] rowCounts = new int[tableCount];
        for (int i = 0; i < tableCount; i++)
        {
            rowCounts[i] = checked((int)target.Read<uint>(schema.RecordCounts + (ulong)(i * sizeof(uint))));
        }

        // ECMA-335 II.24.2.6
        bool[] isSorted = new bool[tableCount];
        for (int i = 0; i < tableCount; i++)
        {
            isSorted[i] = (schema.Sorted & (1UL << i)) != 0;
        }

        bool largeStringHeap = (schema.Heaps & HEAP_STRING_4) != 0;
        bool largeGuidHeap = (schema.Heaps & HEAP_GUID_4) != 0;
        bool largeBlobHeap = (schema.Heaps & HEAP_BLOB_4) != 0;

        bool variableColumnsAll4Bytes =
            miniMd.All4ByteColumns != 0;

        byte[] stringHeap = ReadStoragePool(miniMd.StringHeap);
        byte[] blobHeap = ReadStoragePool(miniMd.BlobHeap);
        byte[] userStringHeap = ReadStoragePool(miniMd.UserStringHeap);
        byte[] guidHeap = ReadStoragePool(miniMd.GuidHeap);

        // Coalesce the record data for each table.
        byte[][] tables = new byte[tableCount][];
        for (int i = 0; i < tableCount; i++)
        {
            tables[i] = ReadStoragePool(miniMd.TableSegments[i]);
        }

        string version = EcmaMetadataUtils.ReadMetadataVersion(target, stgdb.MetadataAddress);

        EcmaMetadataSchema ecmaSchema = new EcmaMetadataSchema(
            version,
            largeStringHeap,
            largeBlobHeap,
            largeGuidHeap,
            rowCounts,
            isSorted,
            variableColumnsAll4Bytes);
        return new TargetEcmaMetadata(ecmaSchema, tables, stringHeap, userStringHeap, blobHeap, guidHeap);
    }

    private byte[] ReadStoragePool(TargetPointer poolAddress)
    {
        List<(TargetPointer Data, uint Size)> segments = [];
        long totalSize = 0;

        Data.StgPool head = new(target, poolAddress);
        TargetPointer segData = head.SegData;
        uint dataSize = head.DataSize;
        TargetPointer nextSegment = head.NextSegment;

        while (true)
        {
            if (dataSize > 0)
            {
                segments.Add((segData, dataSize));
                totalSize += dataSize;
            }
            if (nextSegment == TargetPointer.Null)
            {
                break;
            }

            Data.StgPoolSeg segment = new(target, nextSegment);
            segData = segment.SegData;
            dataSize = segment.DataSize;
            nextSegment = segment.NextSegment;
        }

        byte[] result = new byte[checked((int)totalSize)];
        int offset = 0;
        foreach ((TargetPointer data, uint size) in segments)
        {
            target.ReadBuffer(data, result.AsSpan(offset, checked((int)size)));
            offset += (int)size;
        }
        return result;
    }

    private static T AlignUp<T>(T input, T alignment)
        where T : IBinaryInteger<T>
    {
        return input + (alignment - T.One) & ~(alignment - T.One);
    }
}
