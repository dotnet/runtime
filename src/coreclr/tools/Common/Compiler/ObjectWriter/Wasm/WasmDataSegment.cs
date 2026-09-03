// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ILCompiler.DependencyAnalysis;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.Text;

namespace ILCompiler.ObjectWriter
{
    internal enum WasmDataSegmentType : byte
    {
        Active = 0,  // (data list(byte) (active offset-expr))
        Passive = 1, // (data list(byte) passive)
        ActiveMemorySpecified = 2 // (data list(byte) (active memidx offset-expr))
    }

    internal class WasmDataSegment
    {
        // The segments are not sections per se, but they represent data segments within the data section.
        Stream _stream;
        WasmDataSegmentType _type;
        WasmInstructionGroup _initExpr;
        private PaddingHelper _paddingHelper;

        public WasmDataSegment(Stream contents, Utf8String name, WasmDataSegmentType type, WasmInstructionGroup initExpr)
        {
            _stream = contents;
            _type = type;
            _initExpr = initExpr;
            _paddingHelper = new PaddingHelper(4);
        }

        public int HeaderSize
        {
            get
            {
                return _type switch
                {
                    WasmDataSegmentType.Active =>
                        (int)DwarfHelper.SizeOfULEB128((ulong)_type) + // type indicator
                        _initExpr.EncodeSize() + // init expr encodeSize
                        Relocation.WASM_PADDED_RELOC_SIZE_32, // encode size of data length
                    WasmDataSegmentType.Passive =>
                        (int)DwarfHelper.SizeOfULEB128((ulong)_type) +
                        Relocation.WASM_PADDED_RELOC_SIZE_32, // encode size of data length
                    _ =>
                        throw new NotImplementedException()
                };
            }
        }

        public int EncodeSize()
        {
            return HeaderSize + ContentSize;
        }

        private bool _paddingSet = false;
        int _padding = 0;
        public int Padding
        {
            set
            {
                _paddingSet = true;
                _padding = value;
            }
            get
            {
                Debug.Assert(_paddingSet);
                return _padding;
            }
        }

        public int ContentSize => (int)_stream.Length + Padding;
        public int RawContentSize => (int)_stream.Length;

        public int EncodeHeader(Span<byte> headerBuffer)
        {
            switch (_type)
            {
                case WasmDataSegmentType.Active:
                {
                    int len = 0;
                    len = DwarfHelper.WriteULEB128(headerBuffer, (ulong)_type);
                    len += _initExpr.Encode(headerBuffer.Slice(len));
                    Debug.Assert(headerBuffer.Slice(len).Length == Relocation.WASM_PADDED_RELOC_SIZE_32);
                    DwarfHelper.WritePaddedULEB128(headerBuffer.Slice(len), (ulong)ContentSize);
                    len += headerBuffer.Slice(len).Length;
                    return len;
                }
                case WasmDataSegmentType.Passive:
                {
                    int len = 0;
                    len = DwarfHelper.WriteULEB128(headerBuffer, (ulong)_type);
                    Debug.Assert(headerBuffer.Slice(len).Length == Relocation.WASM_PADDED_RELOC_SIZE_32, $"{headerBuffer.Slice(len).Length} != {Relocation.WASM_PADDED_RELOC_SIZE_32}");
                    DwarfHelper.WritePaddedULEB128(headerBuffer.Slice(len), (ulong)ContentSize);
                    len += headerBuffer.Slice(len).Length;
                    return len;
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public int Emit(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = EncodeHeader(headerBuffer);
            Debug.Assert(headerSize == HeaderSize);
            outputFileStream.Write(headerBuffer);

            _stream.Position = 0;
            _stream.CopyTo(outputFileStream);
            _paddingHelper.PadStream(outputFileStream, (int)Padding);

            return headerSize + (int)_stream.Length + Padding;
        }
    }
}
