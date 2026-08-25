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
    /// A data segment that takes a byte array as its content.
    /// This is used for data segments that are known or constructed at compile time without any ObjectNodes.
    /// </summary>
    internal sealed class WasmByteArrayDataSegment : IWasmDataSegment
    {
        private int _memoryOffset;
        private readonly byte[] _contents;
        private readonly WasmDataSegmentType _type;
        private int _padding;

        public WasmByteArrayDataSegment(
            byte[] contents,
            Utf8String name,
            WasmDataSegmentType type,
            int alignment)
        {
            // ActiveMemorySpecified isn't implemented yet and probably shouldn't be needed here.
            Debug.Assert(type is WasmDataSegmentType.Active or WasmDataSegmentType.Passive);
            Debug.Assert(BitOperations.IsPow2(alignment));

            _contents = contents;
            _type = type;
            Name = name;
            Alignment = alignment;
        }

        public Utf8String Name { get; }
        public int Alignment { get; }
        public int HeaderSize => WasmDataSegmentEncoding.GetHeaderSize(_type, GetInitExpr());
        public int ContentSize => _contents.Length + _padding;
        public int RawContentSize => _contents.Length;
        public WasmDataSegmentType SegmentType => _type;

        public int EncodeSize() => HeaderSize + ContentSize;

        public int EmitToStream(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = WasmDataSegmentEncoding.EncodeHeader(
                headerBuffer,
                _type,
                GetInitExpr(),
                ContentSize);
            Debug.Assert(headerSize == HeaderSize);
            outputFileStream.Write(headerBuffer);

            outputFileStream.Write(_contents);
            WasmDataSegmentEncoding.EmitPadding(outputFileStream, _padding);

            return headerSize + _contents.Length + _padding;
        }
        private WasmInstructionGroup GetInitExpr() => _type == WasmDataSegmentType.Active ? new WasmInstructionGroup([I32.Const(_memoryOffset)]) : null;

        public void SetMemoryOffset(int offset) => _memoryOffset = offset;
        public int GetMemoryAddressOfOffset(int offsetInSegment)
        {
            Debug.Assert(offsetInSegment >= 0 && offsetInSegment <= RawContentSize);
            return _memoryOffset + offsetInSegment;
        }
    }
}
