// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Numerics;
using System.Text;
using ILCompiler.DependencyAnalysis;

namespace ILCompiler.ObjectWriter
{
    internal struct SectionWriter
    {
        private readonly ObjectWriter _objectWriter;
        private readonly SectionData _sectionData;

        public int SectionIndex { get; init; }

        internal SectionWriter(
            ObjectWriter objectWriter,
            int sectionIndex,
            SectionData sectionData)
        {
            _objectWriter = objectWriter;
            SectionIndex = sectionIndex;
            _sectionData = sectionData;
        }

        public readonly void EmitData(ReadOnlyMemory<byte> data)
        {
            _sectionData.AppendData(data);
        }

        public readonly void EmitAlignment(int alignment)
        {
            _objectWriter.UpdateSectionAlignment(SectionIndex, alignment);

            long position = Position;
            int padding = (int)(((position + alignment - 1) & ~(alignment - 1)) - position);
            _sectionData.AppendPadding(padding);
        }

        public readonly void EmitRelocation(
            long relativeOffset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            long addend)
        {
            _objectWriter.EmitRelocation(
                SectionIndex,
                Position + relativeOffset,
                data,
                relocType,
                symbolName,
                addend);
        }

        public readonly void EmitSymbolDefinition(
            string symbolName,
            long relativeOffset = 0,
            int size = 0,
            bool global = false)
        {
            _objectWriter.EmitSymbolDefinition(
                SectionIndex,
                symbolName,
                Position + relativeOffset,
                size,
                global);
        }

        public readonly void EmitSymbolReference(
            RelocType relocType,
            string symbolName,
            long addend = 0)
        {
            IBufferWriter<byte> bufferWriter = _sectionData.BufferWriter;
            int size = relocType == RelocType.IMAGE_REL_BASED_DIR64 ? sizeof(ulong) : sizeof(uint);
            Span<byte> buffer = bufferWriter.GetSpan(size);
            buffer.Slice(0, size).Clear();
            _objectWriter.EmitRelocation(
                SectionIndex,
                Position,
                buffer,
                relocType,
                symbolName,
                addend);
            bufferWriter.Advance(size);
        }

        public readonly void Write(ReadOnlySpan<byte> value)
        {
            IBufferWriter<byte> bufferWriter = _sectionData.BufferWriter;
            value.CopyTo(bufferWriter.GetSpan(value.Length));
            bufferWriter.Advance(value.Length);
        }

        public readonly void WriteULEB128(ulong value) => DwarfHelper.WriteULEB128(_sectionData.BufferWriter, value);

        public readonly void WriteSLEB128(long value) => DwarfHelper.WriteSLEB128(_sectionData.BufferWriter, value);

        public readonly void WriteByte(byte value)
        {
            IBufferWriter<byte> bufferWriter = _sectionData.BufferWriter;
            bufferWriter.GetSpan(1)[0] = value;
            bufferWriter.Advance(1);
        }

        public readonly void WriteLittleEndian<T>(T value)
            where T : IBinaryInteger<T>
        {
            IBufferWriter<byte> bufferWriter = _sectionData.BufferWriter;
            Span<byte> buffer = bufferWriter.GetSpan(value.GetByteCount());
            bufferWriter.Advance(value.WriteLittleEndian(buffer));
        }

        public readonly void WriteUtf8String(string value)
        {
            IBufferWriter<byte> bufferWriter = _sectionData.BufferWriter;
            int size = Encoding.UTF8.GetByteCount(value) + 1;
            Span<byte> buffer = bufferWriter.GetSpan(size);
            Encoding.UTF8.GetBytes(value, buffer);
            buffer[size - 1] = 0;
            bufferWriter.Advance(size);
        }

        public readonly void WritePadding(int size) => _sectionData.AppendPadding(size);

        public readonly long Position => _sectionData.Length;
    }
}
