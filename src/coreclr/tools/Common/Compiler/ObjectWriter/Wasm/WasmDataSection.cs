// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ILCompiler.DependencyAnalysis;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.ObjectWriter
{
    internal sealed class WasmDataSection : IWasmEmittable, IWasmSection
    {
        private readonly List<IWasmDataSegment> _segments;
        private readonly int _contentAlign;
        private bool _layoutAssigned;

        public WasmDataSection(List<IWasmDataSegment> segments, Utf8String name, int contentAlign = 1)
        {
            _segments = segments;
            _contentAlign = contentAlign;
            Name = name;
        }

        public Utf8String Name { get; }
        public WasmSectionType Type => WasmSectionType.Data;
        public int SegmentCount => _segments.Count;

        public int ContentSize
        {
            get
            {
                AssignSegmentLayout();
                int size = (int)DwarfHelper.SizeOfULEB128((ulong)_segments.Count);
                foreach (IWasmDataSegment segment in _segments)
                {
                    size += segment.EncodeSize();
                }

                return size;
            }
        }

        // Webcil could shrink the header to a non-padded int, but in nativeaot this is the patch site of a reloc
        private static int HeaderSize => 1 + Relocation.WASM_PADDED_RELOC_SIZE_32;

        private int EncodeHeader(Span<byte> headerBuffer)
        {
            uint encodeLength = Relocation.WASM_PADDED_RELOC_SIZE_32;
            headerBuffer[0] = (byte)Type;
            DwarfHelper.WritePaddedULEB128(headerBuffer.Slice(1), (ulong)ContentSize);
            return 1 + (int)encodeLength;
        }

        public int EncodeSize()
        {
            // The active segment memory offset expression may change the size of a segment, so the layout must be
            // assigneed before calculating the total size of the data section.
            AssignSegmentLayout();
            return HeaderSize + ContentSize;
        }

        public int EmitToStream(Stream outputFileStream)
        {
            AssignSegmentLayout();
            int size = 0;
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int wroteHeaderSize = EncodeHeader(headerBuffer);
            Debug.Assert(wroteHeaderSize == HeaderSize);
            outputFileStream.Write(headerBuffer);
            size += wroteHeaderSize;

            Span<byte> countBuffer = stackalloc byte[(int)DwarfHelper.SizeOfULEB128((ulong)_segments.Count)];
            int countSize = DwarfHelper.WriteULEB128(countBuffer, (ulong)_segments.Count);
            outputFileStream.Write(countBuffer.Slice(0, countSize));
            size += countSize;

            foreach (IWasmDataSegment segment in _segments)
            {
                size += segment.EmitToStream(outputFileStream);
            }

            return size;
        }

        /// <summary>
        /// Assign the layout of segments within the data section, placing active segments at the appropriate memory offsets.
        /// </summary>
        private void AssignSegmentLayout()
        {
            if (_layoutAssigned)
                return;

            // Assign memory offsets and padding for each segment to ensure that the next segment's content is aligned.
            int currentOffset = 0;
            for (int i = 0; i < _segments.Count; i++)
            {
                IWasmDataSegment segment = _segments[i];
                if (segment.SegmentType == WasmDataSegmentType.Passive)
                {
                    // Passive segments are loaded at runtime and alignment requirements should be handled there.
                    continue;
                }
                // ActiveMemorySpecified segments are not supported yet
                Debug.Assert(segment.SegmentType != WasmDataSegmentType.ActiveMemorySpecified);
                int alignment = Math.Max(_contentAlign, segment.Alignment);
                currentOffset = AlignmentHelper.AlignUp(currentOffset, alignment);
                segment.SetMemoryOffset(currentOffset);
                currentOffset += segment.RawContentSize;
                // TODO: Do we need explicit padding between segments?
                // TODO native aot: Generate relocations to __memory_base + segment offset
            }
            _layoutAssigned = true;
        }
    }
}
