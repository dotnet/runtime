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
        private readonly Dictionary<IWasmDataSegment, (int Index, int ContentOffset)> _segmentLayouts = new();
        private int _contentSize;

        public WasmDataSection(List<IWasmDataSegment> segments, Utf8String name)
        {
            _segments = segments;
            Name = name;
        }

        public Utf8String Name { get; }
        public WasmSectionType Type => WasmSectionType.Data;
        public int SegmentCount => _segments.Count;

        /// <summary>
        /// Gets the data segments in their serialized order.
        /// </summary>
        public IReadOnlyList<IWasmDataSegment> Segments => _segments;
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
                return _contentSize;
            }
        }

        /// <summary>
        /// Gets the index of <paramref name="segment"/> in the data section.
        /// </summary>
        public int GetSegmentIndex(IWasmDataSegment segment)
        {
            AssignSegmentLayout();
            return _segmentLayouts[segment].Index;
        }

        /// <summary>
        /// Gets the offset of <paramref name="segment"/>'s content relative to the data section payload.
        /// </summary>
        public int GetSegmentContentOffset(IWasmDataSegment segment)
        {
            AssignSegmentLayout();
            return _segmentLayouts[segment].ContentOffset;
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
            // assigned before calculating the total size of the data section.
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
        public void AssignSegmentLayout()
        {
            // The segment-count prefix makes the completed content size nonzero, even for an empty section.
            if (_contentSize != 0)
                return;

            // Assign sizes to ensure each segment's content is aligned to its FileAlignment.
            // The first should have no alignment requirements - this simplifies that the alignment of the data section itself.
            Debug.Assert(_segments.Count == 0 || _segments[0].FileAlignment == 1);
            int fileOffset = HeaderSize + (int)DwarfHelper.SizeOfULEB128((ulong)_segments.Count);
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

            int memoryOffset = 0;
            int contentOffset = (int)DwarfHelper.SizeOfULEB128((ulong)_segments.Count);
            for (int i = 0; i < _segments.Count; i++)
            {
                IWasmDataSegment segment = _segments[i];
                // Handle memory layout for active segments
                if (segment.SegmentType == WasmDataSegmentType.Active)
                {
                    IWasmActiveDataSegment activeSegment = (IWasmActiveDataSegment)segment;
                    int alignment = activeSegment.MemoryAlignment;
                    memoryOffset = AlignmentHelper.AlignUp(memoryOffset, alignment);
                    activeSegment.SetMemoryOffset(memoryOffset);
                    memoryOffset += activeSegment.ContentSize;
                }

                _segmentLayouts.Add(segment, (i, checked(contentOffset + segment.HeaderSize)));
                contentOffset = checked(contentOffset + segment.EncodeSize());
            }
            _contentSize = contentOffset;
        }
    }
}
