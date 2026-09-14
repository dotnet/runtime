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
    // WasmDataSection should be aligned to the maximum file alignment of its segments,
    // and each segment should be aligned to its own alignment within the section.
    internal sealed class WasmDataSection : IWasmEmittable, IWasmSection
    {
        private readonly List<IWasmDataSegment> _segments;
        private bool _layoutAssigned;

        public WasmDataSection(List<IWasmDataSegment> segments, Utf8String name)
        {
            _segments = segments;
            Name = name;
        }

        public Utf8String Name { get; }
        public WasmSectionType Type => WasmSectionType.Data;
        public int SegmentCount => _segments.Count;
        public int FileAlignment
        {
            get
            {
                int alignment = 1;
                foreach (IWasmDataSegment segment in _segments)
                {
                    alignment = Math.Max(alignment, segment.FileAlignment);
                }
                return alignment;
            }
        }

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
        /// Assign the layout of segments within the data section, padding segments for file alignment, and placing
        /// active segments at the appropriate memory offsets.
        /// </summary>
        private void AssignSegmentLayout()
        {
            if (_layoutAssigned)
                return;

            // Assign sizes to ensure each segment's content is aligned to its FileAlignment.
            // The first should have no alignment requirements - this simplifies that the alignment of the data section itself.
            Debug.Assert(_segments.Count == 0 || _segments[0].FileAlignment == 1);
            int fileOffset = HeaderSize + (int)DwarfHelper.SizeOfULEB128((ulong)_segments.Count);
            int memoryOffset = 0;
            for (int i = 1; i < _segments.Count; i++)
            {
                IWasmDataSegment segment = _segments[i];
                IWasmDataSegment previousSegment = _segments[i - 1];
                int previousSegmentEnd = fileOffset + previousSegment.HeaderSize + previousSegment.ContentSize;
                int contentStart = previousSegmentEnd + segment.HeaderSize;
                int padding = AlignmentHelper.AlignUp(contentStart, segment.FileAlignment) - contentStart;
                previousSegment.SetTrailingPadding(padding);
                // Use updated previous segment size as the file offset.
                fileOffset = fileOffset + previousSegment.HeaderSize + previousSegment.ContentSize;
                Debug.Assert((fileOffset + segment.HeaderSize) % segment.FileAlignment == 0);
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                IWasmDataSegment segment = _segments[i];
                // Handle memory layout for active segments
                if (segment.SegmentType == WasmDataSegmentType.Passive)
                {
                    // Passive segments are loaded at runtime and alignment requirements should be handled there.
                    continue;
                }
                IWasmActiveDataSegment activeSegment = (IWasmActiveDataSegment)segment;
                int alignment = activeSegment.MemoryAlignment;
                memoryOffset = AlignmentHelper.AlignUp(memoryOffset, alignment);
                activeSegment.SetMemoryOffset(memoryOffset);
                memoryOffset += activeSegment.ContentSize;
            }
            _layoutAssigned = true;
        }
    }
}
