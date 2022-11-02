// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Numerics;
using System.Buffers;
using System.Buffers.Binary;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using Internal.JitInterface;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

namespace ILCompiler.ObjectWriter
{
    public struct SectionWriter
    {
        private ObjectWriter _objectWriter;
        private byte _paddingByte;

        public int SectionIndex { get; init; }
        public Stream Stream { get; init; }

        internal SectionWriter(
            ObjectWriter objectWriter,
            int sectionIndex,
            Stream stream,
            byte paddingByte)
        {
            _objectWriter = objectWriter;
            _paddingByte = paddingByte;
            SectionIndex = sectionIndex;
            Stream = stream;
        }

        public void EmitData(ReadOnlyMemory<byte> data)
        {
            if (Stream is ObjectWriterStream objectWriterStream)
            {
                objectWriterStream.AppendData(data);
            }
            else
            {
                Stream.Write(data.Span);
            }
        }

        public void EmitAlignment(int alignment)
        {
            _objectWriter.UpdateSectionAlignment(SectionIndex, alignment);

            int padding = (int)(((Stream.Position + alignment - 1) & ~(alignment - 1)) - Stream.Position);
            if (Stream is ObjectWriterStream objectWriterStream)
            {
                objectWriterStream.AppendPadding(padding);
            }
            else
            {
                Span<byte> buffer = stackalloc byte[padding];
                buffer.Fill(_paddingByte);
                Stream.Write(buffer);
            }
        }

        public void EmitRelocation(
            int relativeOffset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            int addend)
        {
            _objectWriter.EmitRelocation(
                SectionIndex,
                (int)Stream.Position + relativeOffset,
                data,
                relocType,
                symbolName,
                addend);
        }

        public void EmitSymbolDefinition(
            string symbolName,
            int relativeOffset = 0,
            int size = 0)
        {
            _objectWriter.EmitSymbolDefinition(
                SectionIndex,
                symbolName,
                (int)Stream.Position + relativeOffset,
                size);
        }

        public void EmitSymbolReference(
            RelocType relocType,
            string symbolName,
            int addend = 0)
        {
            Span<byte> buffer = stackalloc byte[relocType == RelocType.IMAGE_REL_BASED_DIR64 ? sizeof(ulong) : sizeof(uint)];
            buffer.Clear();
            _objectWriter.EmitRelocation(
                SectionIndex,
                (int)Stream.Position,
                buffer,
                relocType,
                symbolName,
                addend);
            Stream.Write(buffer);
        }
    }
}
