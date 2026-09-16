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
    /// It is always a passive segment.
    /// </summary>
    internal sealed class WasmByteArrayDataSegment : IWasmDataSegment
    {
        private readonly byte[] _contents;
        private int _paddingBytesCount;

        public WasmByteArrayDataSegment(
            byte[] contents,
            Utf8String name,
            int fileAlignment)
        {
            Debug.Assert(BitOperations.IsPow2(fileAlignment));
            _contents = contents;
            Name = name;
            FileAlignment = fileAlignment;
        }

        public Utf8String Name { get; }
        public int FileAlignment { get; }
        public int HeaderSize => WasmDataSegmentEncoding.GetHeaderSize(WasmDataSegmentType.Passive, initExpr: null);
        public int ContentSize => checked(_contents.Length + _paddingBytesCount);

        public int EncodeSize() => HeaderSize + ContentSize;

        public int EmitToStream(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = WasmDataSegmentEncoding.EncodeHeader(
                headerBuffer,
                WasmDataSegmentType.Passive,
                initExpr: null,
                ContentSize);
            Debug.Assert(headerSize == HeaderSize);

            outputFileStream.Write(headerBuffer);
            outputFileStream.Write(_contents);
            WasmDataSegmentEncoding.EmitPadding(outputFileStream, _paddingBytesCount);
            return headerSize + _contents.Length + _paddingBytesCount;
        }

        public int GetMemoryAddressOfOffset(int offsetInSegment)
        {
            Debug.Assert(offsetInSegment >= 0 && offsetInSegment <= _contents.Length);
            return offsetInSegment;
        }

        public void SetTrailingPadding(int trailingBytesCount)
        {
            Debug.Assert(trailingBytesCount >= 0);
            _paddingBytesCount = trailingBytesCount;
        }
    }
}
