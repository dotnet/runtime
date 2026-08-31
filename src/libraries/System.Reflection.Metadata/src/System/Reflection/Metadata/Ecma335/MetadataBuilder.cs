// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335
{
    public sealed partial class MetadataBuilder
    {
        internal SerializedMetadata GetSerializedMetadata(ImmutableArray<int> externalRowCounts, int metadataVersionByteCount, bool isStandaloneDebugMetadata)
        {
            var stringHeapBuilder = new HeapBlobBuilder(_stringHeapCapacity);
            var stringMap = SerializeStringHeap(stringHeapBuilder, _strings, _stringHeapStartOffset);

            Debug.Assert(HeapIndex.UserString == 0);
            Debug.Assert((int)HeapIndex.String == 1);
            Debug.Assert((int)HeapIndex.Blob == 2);
            Debug.Assert((int)HeapIndex.Guid == 3);

            var heapSizes = ImmutableArray.Create(
                _userStringBuilder.Count,
                stringHeapBuilder.Count,
                _blobHeapSize,
                _guidBuilder.Count);

            var sizes = new MetadataSizes(GetRowCounts(), externalRowCounts, heapSizes, metadataVersionByteCount, isStandaloneDebugMetadata);

            return new SerializedMetadata(sizes, stringHeapBuilder, stringMap);
        }

        internal static void SerializeMetadataHeader(BlobBuilder builder, string metadataVersion, MetadataSizes sizes)
        {
            int startOffset = builder.Count;

            // signature
            builder.WriteUInt32(0x424A5342);

            // major version
            builder.WriteUInt16(1);

            // minor version
            builder.WriteUInt16(1);

            // reserved
            builder.WriteUInt32(0);

            // Spec (section 24.2.1 Metadata Root):
            // Length ... Number of bytes allocated to hold version string (including null terminator), call this x.
            //            Call the length of the string (including the terminator) m (we require m <= 255);
            //            the length x is m rounded up to a multiple of four.
            builder.WriteInt32(sizes.MetadataVersionPaddedLength);

            int metadataVersionStart = builder.Count;
            builder.WriteUTF8(metadataVersion);
            builder.WriteByte(0);
            int metadataVersionEnd = builder.Count;

            builder.WriteBytes(0, sizes.MetadataVersionPaddedLength - (metadataVersionEnd - metadataVersionStart));

            // reserved
            builder.WriteUInt16(0);

            // number of streams
            builder.WriteUInt16((ushort)(5 + (sizes.IsEncDelta ? 1 : 0) + (sizes.IsStandaloneDebugMetadata ? 1 : 0)));

            // stream headers
            int offsetFromStartOfMetadata = sizes.MetadataHeaderSize;

            // emit the #Pdb stream first so that only a single page has to be read in order to find out PDB ID
            if (sizes.IsStandaloneDebugMetadata)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.StandalonePdbStreamSize, "#Pdb"u8, builder);
            }

            // Spec: Some compilers store metadata in a #- stream, which holds an uncompressed, or non-optimized, representation of metadata tables;
            // this includes extra metadata -Ptr tables. Such PE files do not form part of ECMA-335 standard.
            //
            // Note: EnC delta is stored as uncompressed metadata stream.
            SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.MetadataTableStreamSize, (sizes.IsCompressed ? "#~"u8 : "#-"u8), builder);

            SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.GetAlignedHeapSize(HeapIndex.String), "#Strings"u8, builder);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.GetAlignedHeapSize(HeapIndex.UserString), "#US"u8, builder);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.GetAlignedHeapSize(HeapIndex.Guid), "#GUID"u8, builder);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.GetAlignedHeapSize(HeapIndex.Blob), "#Blob"u8, builder);

            if (sizes.IsEncDelta)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, 0, "#JTD"u8, builder);
            }

            int endOffset = builder.Count;
            Debug.Assert(endOffset - startOffset == sizes.MetadataHeaderSize);
        }

        private static void SerializeStreamHeader(ref int offsetFromStartOfMetadata, int alignedStreamSize, ReadOnlySpan<byte> streamName, BlobBuilder builder)
        {
            // 4 for the first uint (offset), 4 for the second uint (padded size), length of stream name + 1 for null terminator (then padded)
            int sizeOfStreamHeader = MetadataSizes.GetMetadataStreamHeaderSize(streamName);
            builder.WriteInt32(offsetFromStartOfMetadata);
            builder.WriteInt32(alignedStreamSize);
            builder.WriteBytes(streamName);

            // After offset, size, and stream name, write 0-bytes until we reach our padded size.
            builder.WriteBytes(0, sizeOfStreamHeader - (8 + streamName.Length));

            offsetFromStartOfMetadata += alignedStreamSize;
        }
    }
}
