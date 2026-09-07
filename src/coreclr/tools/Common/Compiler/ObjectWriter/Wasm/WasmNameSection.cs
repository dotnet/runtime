// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Internal.Text;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// The <c>name</c> custom section, carrying the module's function names.
    /// </summary>
    /// <remarks>
    /// Function names would otherwise have to be carried by the export section, which does not scale:
    /// exports count towards the engine's effective-type-size limit, and a framework-sized composite
    /// exceeds it. A custom section is ignored by engines, counts towards no limit, and can be
    /// stripped when size matters, so it is the only one of the three properties - loadable at scale,
    /// named, and small - that does not cost the other two.
    /// </remarks>
    internal sealed class WasmNameSection : IWasmEmittable
    {
        private const byte FunctionNameSubsectionId = 1;
        private static ReadOnlySpan<byte> SectionName => "name"u8;

        private readonly WasmSymbol[] _functionSymbols;
        private readonly int _subsectionSize;
        private readonly int _payloadSize;

        public WasmNameSection(IEnumerable<WasmSymbol> functionSymbols)
        {
            _functionSymbols = functionSymbols.ToArray();

            // Sized in a first pass so that the payload can be written straight to the output stream.
            // At framework scale the payload is tens of megabytes, so buffering it would put several
            // copies of it on the large object heap.
            long nameMapSize = 0;
            foreach (WasmSymbol symbol in _functionSymbols)
            {
                int nameLength = symbol.Name.Length;
                nameMapSize += DwarfHelper.SizeOfULEB128((ulong)symbol.Index)
                    + DwarfHelper.SizeOfULEB128((ulong)nameLength)
                    + nameLength;
            }

            _subsectionSize = checked((int)(DwarfHelper.SizeOfULEB128((ulong)_functionSymbols.Length) + nameMapSize));
            _payloadSize = checked(
                (int)DwarfHelper.SizeOfULEB128((ulong)SectionName.Length) + SectionName.Length +
                1 + // subsection id
                (int)DwarfHelper.SizeOfULEB128((ulong)_subsectionSize) + _subsectionSize);
        }

        /// <summary>Number of functions named by this section.</summary>
        public int FunctionCount => _functionSymbols.Length;

        public int EncodeSize() =>
            1 + (int)DwarfHelper.SizeOfULEB128((ulong)_payloadSize) + _payloadSize;

        public int EmitToStream(Stream outputFileStream)
        {
            outputFileStream.WriteByte((byte)WasmSectionType.Custom);
            WriteULEB128(outputFileStream, (ulong)_payloadSize);

            WriteName(outputFileStream, SectionName);
            outputFileStream.WriteByte(FunctionNameSubsectionId);
            WriteULEB128(outputFileStream, (ulong)_subsectionSize);
            WriteULEB128(outputFileStream, (ulong)_functionSymbols.Length);

            // The name map must be sorted by index and free of duplicates. GetDefinitions already
            // orders by index, so this only asserts rather than re-sorting a very large sequence.
            int previousIndex = -1;
            foreach (WasmSymbol symbol in _functionSymbols)
            {
                Debug.Assert(symbol.Index > previousIndex, "Function name map must be sorted by index and duplicate-free");
                previousIndex = symbol.Index;

                WriteULEB128(outputFileStream, (ulong)symbol.Index);
                WriteName(outputFileStream, symbol.Name.AsSpan());
            }

            return EncodeSize();
        }

        private static void WriteName(Stream stream, ReadOnlySpan<byte> utf8Name)
        {
            WriteULEB128(stream, (ulong)utf8Name.Length);
            stream.Write(utf8Name);
        }

        private static void WriteULEB128(Stream stream, ulong value)
        {
            Span<byte> buffer = stackalloc byte[(int)DwarfHelper.SizeOfULEB128(value)];
            int written = DwarfHelper.WriteULEB128(buffer, value);
            stream.Write(buffer.Slice(0, written));
        }
    }
}
