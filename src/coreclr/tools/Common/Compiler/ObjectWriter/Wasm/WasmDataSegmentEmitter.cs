// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.Text;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// A SectionDataEmitter for an ObjectNodeSection emits into a single WASM data segment.
    /// </summary>
    internal sealed class WasmDataSegmentEmitter : SectionDataEmitter, IWasmDataSegment
    {
        private int _alignment = 1;
        private int _memoryOffset;

        public WasmDataSegmentEmitter(
            Stream contents,
            Utf8String name,
            int sectionIndex)
            : base(contents, name, sectionIndex)
        {
        }

        public int Alignment => _alignment;
        public int HeaderSize => WasmDataSegmentEncoding.GetHeaderSize(WasmDataSegmentType.Active, GetMemoryOffsetInitExpr());
        public int RawContentSize => (int)ContentReadStream.Length;
        public WasmDataSegmentType SegmentType => WasmDataSegmentType.Active;

        public void UpdateAlignment(int alignment)
        {
            Debug.Assert(BitOperations.IsPow2(alignment));
            _alignment = Math.Max(_alignment, alignment);
        }

        public override int EncodeSize() => HeaderSize + RawContentSize;

        public override int EmitToStream(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = WasmDataSegmentEncoding.EncodeHeader(
                headerBuffer,
                WasmDataSegmentType.Active,
                GetMemoryOffsetInitExpr(),
                RawContentSize);
            Debug.Assert(headerSize == HeaderSize);
            outputFileStream.Write(headerBuffer);

            ContentReadStream.Position = 0;
            ContentReadStream.CopyTo(outputFileStream);

            return EncodeSize();
        }

        public void SetMemoryOffset(int offset)
        {
            _memoryOffset = offset;
        }

        private WasmInstructionGroup GetMemoryOffsetInitExpr()
        {
            return new WasmInstructionGroup([I32.Const(_memoryOffset)]);
        }

        public int GetMemoryAddressOfOffset(int offsetInSegment)
        {
            Debug.Assert(offsetInSegment >= 0 && offsetInSegment <= RawContentSize);
            return _memoryOffset + offsetInSegment;
        }
    }
}
