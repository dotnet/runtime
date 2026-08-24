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
        private readonly byte[] _contents;
        private readonly WasmDataSegmentType _type;
        private readonly WasmInstructionGroup _initExpr;
        private int _padding;

        public WasmByteArrayDataSegment(
            byte[] contents,
            Utf8String name,
            WasmDataSegmentType type,
            WasmInstructionGroup initExpr,
            int alignment)
        {
            // ActiveMemorySpecified isn't implemented yet and probably shouldn't be needed here.
            Debug.Assert(type is WasmDataSegmentType.Active or WasmDataSegmentType.Passive);
            Debug.Assert((type == WasmDataSegmentType.Active) == (initExpr is not null));
            Debug.Assert(BitOperations.IsPow2(alignment));

            _contents = contents;
            _type = type;
            _initExpr = initExpr;
            Name = name;
            Alignment = alignment;
        }

        public Utf8String Name { get; }
        public int Alignment { get; }
        public int HeaderSize => WasmDataSegmentEncoding.GetHeaderSize(_type, _initExpr);

        public void SetPadding(int value)
        {
            _padding = value;
        }

        public int ContentSize => _contents.Length + _padding;
        public int RawContentSize => _contents.Length;

        public int EncodeSize() => HeaderSize + ContentSize;

        public int EmitToStream(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = WasmDataSegmentEncoding.EncodeHeader(
                headerBuffer,
                _type,
                _initExpr,
                ContentSize);
            Debug.Assert(headerSize == HeaderSize);
            outputFileStream.Write(headerBuffer);

            outputFileStream.Write(_contents);
            WasmDataSegmentEncoding.EmitPadding(outputFileStream, _padding);

            return headerSize + _contents.Length + _padding;
        }
    }
}
