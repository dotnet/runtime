// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Helpers;

internal class Metadata
{
    private readonly Target _target;
    private readonly Dictionary<ulong, MetadataReaderProvider> _metadata = [];

    public Metadata(Target target)
    {
        _target = target;
    }

    public virtual MetadataReader GetMetadata(Contracts.ModuleHandle module)
    {
        if (_metadata.TryGetValue(module.Address, out MetadataReaderProvider? result))
            return result.GetMetadataReader();

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
                result = MetadataReaderProvider.FromMetadataImage(ImmutableCollectionsMarshal.AsImmutableArray(data));
            }
        }
        else if (metadataType == AvailableMetadataType.ReadWriteSavedCopy)
        {
            TargetPointer address = _target.Contracts.Loader.GetReadWriteSavedMetadataAddress(module, out ulong size);
            byte[] data = new byte[size];
            _target.ReadBuffer(address, data);
            result = MetadataReaderProvider.FromMetadataImage(ImmutableCollectionsMarshal.AsImmutableArray(data));
        }
        else
        {
            var targetEcmaMetadata = _target.Contracts.Loader.GetReadWriteMetadata(module);

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
            result = MetadataReaderProvider.FromMetadataStream(metadataStream);

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

        _metadata.Add(module.Address, result);
        return result.GetMetadataReader();
    }

    public Func<Contracts.ModuleHandle, MetadataReaderProvider>? MetadataProvider;

    private static T AlignUp<T>(T input, T alignment)
        where T : IBinaryInteger<T>
    {
        return input + (alignment - T.One) & ~(alignment - T.One);
    }
}
