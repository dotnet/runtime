// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILCompiler.DependencyAnalysis;

namespace ILCompiler.ObjectWriter
{
    public struct SectionWriter
    {
        private readonly ObjectWriter _objectWriter;

        public int SectionIndex { get; init; }
        public ObjectWriterStream Stream { get; init; }

        internal SectionWriter(
            ObjectWriter objectWriter,
            int sectionIndex,
            ObjectWriterStream stream)
        {
            _objectWriter = objectWriter;
            SectionIndex = sectionIndex;
            Stream = stream;
        }

        public readonly void EmitData(ReadOnlyMemory<byte> data)
        {
            Stream.AppendData(data);
        }

        public readonly void EmitAlignment(int alignment)
        {
            _objectWriter.UpdateSectionAlignment(SectionIndex, alignment);

            int padding = (int)(((Stream.Position + alignment - 1) & ~(alignment - 1)) - Stream.Position);
            Stream.AppendPadding(padding);
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
                Stream.Position + relativeOffset,
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
                Stream.Position + relativeOffset,
                size,
                global);
        }

        public readonly void EmitSymbolReference(
            RelocType relocType,
            string symbolName,
            long addend = 0)
        {
            Span<byte> buffer = stackalloc byte[relocType == RelocType.IMAGE_REL_BASED_DIR64 ? sizeof(ulong) : sizeof(uint)];
            buffer.Clear();
            _objectWriter.EmitRelocation(
                SectionIndex,
                Stream.Position,
                buffer,
                relocType,
                symbolName,
                addend);
            Stream.Write(buffer);
        }
    }
}
