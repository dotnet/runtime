// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ILCompiler.ObjectWriter;
using Internal.Text;
using Xunit;

namespace ILCompiler.Compiler.Tests
{
    public class CoffStringTableTests
    {
        [Fact]
        public void EncodesSectionAndSymbolNames()
        {
            string[] sectionNames =
            [
                "12345678",
                "éééé",
                ".very.long.section",
                "ééééx",
            ];
            string[] symbolNames =
            [
                "short",
                "12345678",
                "123456789",
                "éééé",
                "ééééx",
                ".very.long.section",
                "prefix.symbol.name",
                "symbol.name",
                "short",
            ];

            CoffStringTableResult result = CoffObjectWriterAccessor.BuildStringTable(sectionNames, symbolNames);

            Assert.Equal(CreateNameField("12345678"), result.SectionNameFields[0]);
            Assert.Equal(CreateNameField("éééé"), result.SectionNameFields[1]);
            Assert.Equal(CreateNameField("/4"), result.SectionNameFields[2]);
            Assert.Equal(CreateNameField("/23"), result.SectionNameFields[3]);

            byte[] expectedStringTable = CreateExpectedStringTable(
                ".very.long.section",
                "ééééx",
                "éééé",
                "short",
                "prefix.symbol.name",
                "123456789",
                "12345678");

            Assert.Equal(expectedStringTable, result.StringTable);
            Assert.Equal((uint)expectedStringTable.Length, result.Size);
            Assert.Equal(4u, result.SymbolOffsets[".very.long.section"]);
            Assert.Equal(23u, result.SymbolOffsets["ééééx"]);
            Assert.Equal(33u, result.SymbolOffsets["éééé"]);
            Assert.Equal(42u, result.SymbolOffsets["short"]);
            Assert.Equal(48u, result.SymbolOffsets["prefix.symbol.name"]);
            Assert.Equal(55u, result.SymbolOffsets["symbol.name"]);
            Assert.Equal(67u, result.SymbolOffsets["123456789"]);
            Assert.Equal(77u, result.SymbolOffsets["12345678"]);
        }

        [Fact]
        public void SharesSuffixesAndDeduplicatesNames()
        {
            string[] symbolNames =
            [
                "symbol",
                "shared.symbol",
                "prefix.shared.symbol",
                "shared.symbol",
                "prefix.shared.symbol",
            ];

            CoffStringTableResult result = CoffObjectWriterAccessor.BuildStringTable(Array.Empty<string>(), symbolNames);
            byte[] expectedStringTable = CreateExpectedStringTable("prefix.shared.symbol");

            Assert.Equal(expectedStringTable, result.StringTable);
            Assert.Equal((uint)expectedStringTable.Length, result.Size);
            Assert.Equal(4u, result.SymbolOffsets["prefix.shared.symbol"]);
            Assert.Equal(11u, result.SymbolOffsets["shared.symbol"]);
            Assert.Equal(18u, result.SymbolOffsets["symbol"]);
        }

        [Fact]
        public void SupportsMultipleReservationBatchesAndRepeatedWrites()
        {
            var stringTable = new StringTableBuilder();
            var sharedSymbol = new Utf8String("shared.symbol");
            var symbol = new Utf8String("symbol");

            stringTable.ReserveString(sharedSymbol);
            stringTable.ReserveString(symbol);

            Assert.Equal(14u, stringTable.Size);
            Assert.Equal(0u, stringTable.GetStringOffset(sharedSymbol));
            Assert.Equal(7u, stringTable.GetStringOffset(symbol));

            var prefixedSymbol = new Utf8String("prefix.shared.symbol");
            stringTable.ReserveString(prefixedSymbol);
            stringTable.ReserveString(sharedSymbol);

            Assert.Equal(35u, stringTable.Size);
            Assert.Equal(14u, stringTable.GetStringOffset(prefixedSymbol));

            byte[] expected = Encoding.UTF8.GetBytes("shared.symbol\0prefix.shared.symbol\0");
            Assert.Equal(expected, WriteStringTable(stringTable));
            Assert.Equal(expected, WriteStringTable(stringTable));
        }

        [Fact]
        public void OutputIsDeterministicAndOrdinalAcrossReservationOrders()
        {
            string[][] reservationOrders =
            [
                ["", "alpha", "ALPHA", "prefix.shared", "shared", "méthode", "方法", "12345678", "123456789"],
                ["123456789", "12345678", "方法", "méthode", "shared", "prefix.shared", "ALPHA", "alpha", ""],
                ["shared", "", "12345678", "alpha", "方法", "123456789", "prefix.shared", "méthode", "ALPHA"],
            ];

            CoffStringTableResult expected = CoffObjectWriterAccessor.BuildStringTable(
                Array.Empty<string>(),
                reservationOrders[0]);
            Assert.Equal(
                CreateExpectedStringTable("方法", "méthode", "prefix.shared", "alpha", "ALPHA", "123456789", "12345678"),
                expected.StringTable);
            Assert.Equal(64u, expected.SymbolOffsets[""]);
            Assert.Equal(27u, expected.SymbolOffsets["shared"]);

            foreach (string[] reservationOrder in reservationOrders)
            {
                CoffStringTableResult actual = CoffObjectWriterAccessor.BuildStringTable(
                    Array.Empty<string>(),
                    reservationOrder);

                Assert.Equal(expected.Size, actual.Size);
                Assert.Equal(expected.StringTable, actual.StringTable);
                foreach (KeyValuePair<string, uint> pair in expected.SymbolOffsets)
                {
                    Assert.Equal(pair.Value, actual.SymbolOffsets[pair.Key]);
                }
            }
        }

        private static byte[] WriteStringTable(StringTableBuilder stringTable)
        {
            using MemoryStream stream = new();
            stringTable.Write(stream);
            return stream.ToArray();
        }

        private static byte[] CreateNameField(string text)
        {
            byte[] result = new byte[8];
            Encoding.UTF8.GetBytes(text).CopyTo(result, 0);
            return result;
        }

        private static byte[] CreateExpectedStringTable(params string[] entries)
        {
            using MemoryStream stream = new();
            stream.Write(new byte[sizeof(uint)]);
            foreach (string entry in entries)
            {
                stream.Write(Encoding.UTF8.GetBytes(entry));
                stream.WriteByte(0);
            }

            byte[] result = stream.ToArray();
            BinaryPrimitives.WriteUInt32LittleEndian(result, checked((uint)result.Length));
            return result;
        }

        private sealed class CoffStringTableResult
        {
            public CoffStringTableResult(
                byte[][] sectionNameFields,
                byte[] stringTable,
                uint size,
                Dictionary<string, uint> symbolOffsets)
            {
                SectionNameFields = sectionNameFields;
                StringTable = stringTable;
                Size = size;
                SymbolOffsets = symbolOffsets;
            }

            public byte[][] SectionNameFields { get; }
            public byte[] StringTable { get; }
            public uint Size { get; }
            public Dictionary<string, uint> SymbolOffsets { get; }
        }

        private sealed class CoffObjectWriterAccessor : CoffObjectWriter
        {
            private CoffObjectWriterAccessor()
                : base(null, default)
            {
            }

            public static CoffStringTableResult BuildStringTable(
                IReadOnlyList<string> sectionNames,
                IReadOnlyList<string> symbolNames)
            {
                CoffStringTable stringTable = new();
                byte[][] sectionNameFields = new byte[sectionNames.Count][];

                using (MemoryStream sectionHeaders = new())
                {
                    for (int i = 0; i < sectionNames.Count; i++)
                    {
                        long headerOffset = sectionHeaders.Position;
                        var sectionHeader = new CoffSectionHeader
                        {
                            Name = sectionNames[i],
                        };
                        sectionHeader.Write(sectionHeaders, stringTable);
                        sectionNameFields[i] = sectionHeaders.GetBuffer().AsSpan((int)headerOffset, 8).ToArray();
                    }
                }

                Utf8String[] utf8SymbolNames = new Utf8String[symbolNames.Count];
                for (int i = 0; i < symbolNames.Count; i++)
                {
                    utf8SymbolNames[i] = new Utf8String(symbolNames[i]);
                    stringTable.ReserveString(utf8SymbolNames[i]);
                }

                var symbolOffsets = new Dictionary<string, uint>(StringComparer.Ordinal);
                for (int i = 0; i < symbolNames.Count; i++)
                {
                    if (!symbolOffsets.ContainsKey(symbolNames[i]))
                    {
                        symbolOffsets.Add(symbolNames[i], stringTable.GetStringOffset(utf8SymbolNames[i]));
                    }
                }
                uint size = stringTable.Size;

                using MemoryStream tableStream = new();
                stringTable.Write(tableStream);

                return new CoffStringTableResult(
                    sectionNameFields,
                    tableStream.ToArray(),
                    size,
                    symbolOffsets);
            }
        }
    }
}
