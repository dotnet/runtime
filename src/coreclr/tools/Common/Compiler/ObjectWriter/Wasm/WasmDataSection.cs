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
    internal class WasmDataSection : IWasmEmittable, IWasmSection
    {
        private List<WasmDataSegment> _segments;
        public List<WasmDataSegment> Segments => _segments;
        private int _contentAlign = 1;
        public WasmDataSection(List<WasmDataSegment> segments, Utf8String name, int contentAlign = 1)
        {
            _segments = segments;
            _contentAlign = contentAlign;
        }

        public WasmSectionType Type => WasmSectionType.Data;

        public int ContentSize
        {
            get
            {
                int size = 0;
                size += (int)DwarfHelper.SizeOfULEB128((ulong)_segments.Count);
                foreach (WasmDataSegment segment in _segments)
                {
                    size += segment.EncodeSize();
                }

                return size;
            }
        }

        public int HeaderSize => 1 + Relocation.WASM_PADDED_RELOC_SIZE_32;

        private int EncodeHeader(Span<byte> headerBuffer)
        {
            uint encodeLength = Relocation.WASM_PADDED_RELOC_SIZE_32;

            headerBuffer[0] = (byte)Type;
            DwarfHelper.WritePaddedULEB128(headerBuffer.Slice(1), (ulong)ContentSize);
            Debug.Assert(headerBuffer.Slice(1).Length == Relocation.WASM_PADDED_RELOC_SIZE_32);
            ulong readCheck = DwarfHelper.ReadULEB128(headerBuffer.Slice(1));
            Debug.Assert((int)readCheck == ContentSize);

            return 1 + (int)encodeLength;
        }

        public int EncodeSize()
        {
            return HeaderSize + ContentSize;
        }

        public int EmitToStream(Stream outputFileStream)
        {
            int size = 0;
            int headerPosition = (int)outputFileStream.Position;

            // seek forward past pre-allocated header portion
            outputFileStream.Position += (int)HeaderSize;
            size += (int)HeaderSize;

            Span<byte> countBuffer = stackalloc byte[(int)DwarfHelper.SizeOfULEB128((ulong)_segments.Count)];
            int countSize = DwarfHelper.WriteULEB128(countBuffer, (ulong)_segments.Count);
            outputFileStream.Write(countBuffer.Slice(0, countSize));
            size += countSize;

            for (int i = 0; i < _segments.Count; i++)
            {
                WasmDataSegment segment = _segments[i];
                // Do we have a next segment?
                if ((i + 1) < _segments.Count)
                {
                    // Calculate end padding to insert after end of this segment's contents, before the wasm header for the next section
                    // to ensure that the next section's content is aligned at the file level
                    int position = (int)outputFileStream.Position + segment.HeaderSize + (int)segment.RawContentSize + _segments[i + 1].HeaderSize;
                    int padding = AlignmentHelper.AlignUp(position, _contentAlign) - position;
                    segment.Padding = padding;
                }
                else
                {
                    segment.Padding = 0;
                }
                size += segment.Emit(outputFileStream);
            }

            // Write the header (this must be done second because we first need to determine inter-segment padding based on file placement)
            outputFileStream.Position = headerPosition;
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int wroteHeaderSize = EncodeHeader(headerBuffer);
            Debug.Assert(wroteHeaderSize == HeaderSize);
            outputFileStream.Write(headerBuffer);

            outputFileStream.Seek(0, SeekOrigin.End);

            return size;
        }
    }
}
