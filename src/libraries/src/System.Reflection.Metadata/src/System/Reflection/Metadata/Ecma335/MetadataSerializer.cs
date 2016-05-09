// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace System.Reflection.Metadata.Ecma335
{
    public sealed class StandaloneDebugMetadataSerializer : MetadataSerializer
    {
        private const string DebugMetadataVersionString = "PDB v1.0";

        private Blob _pdbIdBlob;
        private readonly MethodDefinitionHandle _entryPoint;

        public StandaloneDebugMetadataSerializer(
            MetadataBuilder builder, 
            ImmutableArray<int> typeSystemRowCounts,
            MethodDefinitionHandle entryPoint,
            bool isMinimalDelta)
            : base(builder, CreateSizes(builder, typeSystemRowCounts, isMinimalDelta, isStandaloneDebugMetadata: true), DebugMetadataVersionString)
        {
            _entryPoint = entryPoint;
        }

        /// <summary>
        /// Serialized #Pdb stream.
        /// </summary>
        protected override void SerializeStandalonePdbStream(BlobBuilder builder)
        {
            int startPosition = builder.Count;

            // the id will be filled in later
            _pdbIdBlob = builder.ReserveBytes(MetadataSizes.PdbIdSize);
            
            builder.WriteInt32(_entryPoint.IsNil ? 0 : MetadataTokens.GetToken(_entryPoint));

            builder.WriteUInt64(MetadataSizes.ExternalTablesMask);
            MetadataWriterUtilities.SerializeRowCounts(builder, MetadataSizes.ExternalRowCounts);

            int endPosition = builder.Count;
            Debug.Assert(MetadataSizes.CalculateStandalonePdbStreamSize() == endPosition - startPosition);
        }

        public void SerializeMetadata(BlobBuilder builder, Func<BlobBuilder, ContentId> idProvider, out ContentId contentId)
        {
            SerializeMetadataImpl(builder, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);

            contentId = idProvider(builder);

            // fill in the id:
            var idWriter = new BlobWriter(_pdbIdBlob);
            idWriter.WriteBytes(contentId.Guid);
            idWriter.WriteBytes(contentId.Stamp);
            Debug.Assert(idWriter.RemainingBytes == 0);
        }
    }

    public sealed class TypeSystemMetadataSerializer : MetadataSerializer
    {
        private static readonly ImmutableArray<int> EmptyRowCounts = ImmutableArray.CreateRange(Enumerable.Repeat(0, MetadataTokens.TableCount));

        public TypeSystemMetadataSerializer(
            MetadataBuilder builder, 
            string metadataVersion,
            bool isMinimalDelta)
            : base(builder, CreateSizes(builder, EmptyRowCounts, isMinimalDelta, isStandaloneDebugMetadata: false), metadataVersion)
        {
            
        }

        protected override void SerializeStandalonePdbStream(BlobBuilder builder)
        {
            // nop
        }

        public void SerializeMetadata(BlobBuilder builder, int methodBodyStreamRva, int mappedFieldDataStreamRva)
        {
            SerializeMetadataImpl(builder, methodBodyStreamRva, mappedFieldDataStreamRva);
        }
    }

    public abstract class MetadataSerializer
    {
        protected readonly MetadataBuilder _builder;
        private readonly MetadataSizes _sizes;
        private readonly string _metadataVersion;

        public MetadataSerializer(MetadataBuilder builder, MetadataSizes sizes, string metadataVersion)
        {
            _builder = builder;
            _sizes = sizes;
            _metadataVersion = metadataVersion;
        }

        internal static MetadataSizes CreateSizes(MetadataBuilder builder, ImmutableArray<int> externalRowCounts, bool isMinimalDelta, bool isStandaloneDebugMetadata)
        {
            builder.CompleteHeaps();

            return new MetadataSizes(
                builder.GetRowCounts(),
                externalRowCounts,
                builder.GetHeapSizes(),
                isMinimalDelta,
                isStandaloneDebugMetadata);
        }

        protected abstract void SerializeStandalonePdbStream(BlobBuilder builder);

        public MetadataSizes MetadataSizes => _sizes;

        protected void SerializeMetadataImpl(BlobBuilder builder, int methodBodyStreamRva, int mappedFieldDataStreamRva)
        {
            // header:
            SerializeMetadataHeader(builder);

            // #Pdb stream
            SerializeStandalonePdbStream(builder);
            
            // #~ or #- stream:
            _builder.SerializeMetadataTables(builder, _sizes, methodBodyStreamRva, mappedFieldDataStreamRva);

            // #Strings, #US, #Guid and #Blob streams:
            _builder.WriteHeapsTo(builder);
        }

        private void SerializeMetadataHeader(BlobBuilder builder)
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

            // metadata version length
            builder.WriteUInt32(MetadataSizes.MetadataVersionPaddedLength);

            int n = Math.Min(MetadataSizes.MetadataVersionPaddedLength, _metadataVersion.Length);
            for (int i = 0; i < n; i++)
            {
                builder.WriteByte((byte)_metadataVersion[i]);
            }

            for (int i = n; i < MetadataSizes.MetadataVersionPaddedLength; i++)
            {
                builder.WriteByte(0);
            }

            // reserved
            builder.WriteUInt16(0);

            // number of streams
            builder.WriteUInt16((ushort)(5 + (_sizes.IsMinimalDelta ? 1 : 0) + (_sizes.IsStandaloneDebugMetadata ? 1 : 0)));

            // stream headers
            int offsetFromStartOfMetadata = _sizes.MetadataHeaderSize;

            // emit the #Pdb stream first so that only a single page has to be read in order to find out PDB ID
            if (_sizes.IsStandaloneDebugMetadata)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.StandalonePdbStreamSize, "#Pdb", builder);
            }

            // Spec: Some compilers store metadata in a #- stream, which holds an uncompressed, or non-optimized, representation of metadata tables;
            // this includes extra metadata -Ptr tables. Such PE files do not form part of ECMA-335 standard.
            //
            // Note: EnC delta is stored as uncompressed metadata stream.
            SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.MetadataTableStreamSize, (_sizes.IsMetadataTableStreamCompressed ? "#~" : "#-"), builder);

            SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.GetAlignedHeapSize(HeapIndex.String), "#Strings", builder);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.GetAlignedHeapSize(HeapIndex.UserString), "#US", builder);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.GetAlignedHeapSize(HeapIndex.Guid), "#GUID", builder);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, _sizes.GetAlignedHeapSize(HeapIndex.Blob), "#Blob", builder);

            if (_sizes.IsMinimalDelta)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, 0, "#JTD", builder);
            }

            int endOffset = builder.Count;
            Debug.Assert(endOffset - startOffset == _sizes.MetadataHeaderSize);
        }

        private static void SerializeStreamHeader(ref int offsetFromStartOfMetadata, int alignedStreamSize, string streamName, BlobBuilder builder)
        {
            // 4 for the first uint (offset), 4 for the second uint (padded size), length of stream name + 1 for null terminator (then padded)
            int sizeOfStreamHeader = MetadataSizes.GetMetadataStreamHeaderSize(streamName);
            builder.WriteInt32(offsetFromStartOfMetadata);
            builder.WriteInt32(alignedStreamSize);
            foreach (char ch in streamName)
            {
                builder.WriteByte((byte)ch);
            }

            // After offset, size, and stream name, write 0-bytes until we reach our padded size.
            for (uint i = 8 + (uint)streamName.Length; i < sizeOfStreamHeader; i++)
            {
                builder.WriteByte(0);
            }

            offsetFromStartOfMetadata += alignedStreamSize;
        }
    }
}
