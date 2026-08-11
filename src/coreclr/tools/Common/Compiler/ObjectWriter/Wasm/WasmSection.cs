// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using Internal.Text;

namespace ILCompiler.ObjectWriter
{
    internal class WasmSection
    {
        public WasmSectionType Type { get; }
        public Utf8String Name { get; }

        public int? PrependCount = null;
        public int PrependCountSize => PrependCount.HasValue ? (int)DwarfHelper.SizeOfULEB128((ulong)PrependCount.Value) : 0;

        private int EncodePrependCount(Span<byte> dest)
        {
            if (PrependCount.HasValue)
            {
                return DwarfHelper.WriteULEB128(dest, (ulong)PrependCount.Value);
            }

            return 0;
        }

        public Stream Stream
        {
            get
            {
                Debug.Assert(_dataStream != null, $"{this.Name} has null data stream");
                return _dataStream;
            }

            set
            {
                Debug.Assert(value != null);
                _dataStream = value;
            }
        }

        Stream _dataStream;

        public virtual int HeaderSize
        {
            get
            {
                uint sizeEncodeLength = DwarfHelper.SizeOfULEB128((ulong)ContentSize);
                return 1 + (int)sizeEncodeLength;
            }
        }

        public virtual int ContentSize => (int)_dataStream.Length + PrependCountSize;

        public virtual int EncodeSize()
        {
            return HeaderSize + ContentSize;
        }

        public virtual int EncodeHeader(Span<byte> headerBuffer)
        {
            ulong contentSize = (ulong)ContentSize;
            uint encodeLength = DwarfHelper.SizeOfULEB128(contentSize);

            // Section header consists of:
            // 1 byte: section type
            // ULEB128: size of section
            headerBuffer[0] = (byte)Type;
            DwarfHelper.WriteULEB128(headerBuffer.Slice(1), contentSize);

            return 1 + (int)encodeLength;
        }

        public virtual int Emit(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            EncodeHeader(headerBuffer);

            outputFileStream.Write(headerBuffer);

            if (PrependCount.HasValue)
            {
                Span<byte> prependCount = stackalloc byte[PrependCountSize];
                int encoded = EncodePrependCount(prependCount);
                outputFileStream.Write(prependCount);
            }

            Stream.Position = 0;
            Stream.CopyTo(outputFileStream);

            return HeaderSize + (int)(PrependCountSize + Stream.Length);
        }

        public WasmSection(WasmSectionType type, Stream stream, Utf8String name, int? prependCount = null)
        {
            Type = type;
            Name = name;
            _dataStream = stream;
            PrependCount = prependCount;
        }
    }
}
