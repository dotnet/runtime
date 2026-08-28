// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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
        private static readonly byte[] SectionName = "name"u8.ToArray();

        private readonly byte[] _payload;

        public WasmNameSection(IEnumerable<WasmSymbol> functionSymbols)
        {
            _payload = EncodePayload(functionSymbols);
        }

        /// <summary>Number of functions named by this section.</summary>
        public int FunctionCount { get; private set; }

        private byte[] EncodePayload(IEnumerable<WasmSymbol> functionSymbols)
        {
            // The name map must be sorted by index and free of duplicates. GetDefinitions already
            // orders by index, so only assert here rather than re-sorting a very large sequence.
            MemoryStream nameMap = new MemoryStream();
            int count = 0;
            int previousIndex = -1;
            foreach (WasmSymbol symbol in functionSymbols)
            {
                Debug.Assert(symbol.Index > previousIndex, "Function name map must be sorted by index and duplicate-free");
                previousIndex = symbol.Index;

                WriteULEB128(nameMap, (ulong)symbol.Index);
                WriteName(nameMap, symbol.Name.AsSpan());
                count++;
            }

            FunctionCount = count;

            MemoryStream subsection = new MemoryStream();
            WriteULEB128(subsection, (ulong)count);
            nameMap.Position = 0;
            nameMap.CopyTo(subsection);

            MemoryStream payload = new MemoryStream();
            WriteName(payload, SectionName);
            payload.WriteByte(FunctionNameSubsectionId);
            WriteULEB128(payload, (ulong)subsection.Length);
            subsection.Position = 0;
            subsection.CopyTo(payload);

            return payload.ToArray();
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

        public int EncodeSize() =>
            1 + (int)DwarfHelper.SizeOfULEB128((ulong)_payload.Length) + _payload.Length;

        public int EmitToStream(Stream outputFileStream)
        {
            outputFileStream.WriteByte((byte)WasmSectionType.Custom);
            WriteULEB128(outputFileStream, (ulong)_payload.Length);
            outputFileStream.Write(_payload);

            return EncodeSize();
        }
    }
}
