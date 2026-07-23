// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ILCompiler.Reflection.ReadyToRun;

namespace ILCompiler.ReadyToRun.Tests.TestCasesRunner;

internal static class WasmR2RAssert
{
    /// <summary>
    /// Returns true if any WASM function body in the image contains a <c>global.get</c> of the
    /// given ABI well-known-global index, emitted as a maximally padded 5-byte
    /// <c>WASM_GLOBAL_INDEX_LEB</c> reference (the <c>global.get</c> opcode <c>0x23</c> followed
    /// by the 5-byte padded ULEB128 of the index).
    /// </summary>
    /// <remarks>
    /// The wasm JIT references only the three ABI well-known globals (0 = stack pointer, 1 = image base,
    /// 2 = table base) in this padded form; ordinary <c>global.get</c> instructions use the minimal
    /// LEB128 encoding. The R2R object writer self-resolves the relocation in place, so after
    /// compilation the padded slot holds the fixed index, e.g. image base -&gt;
    /// <c>23 81 80 80 80 00</c> and table base -&gt; <c>23 82 80 80 80 00</c>. This is a regression
    /// smoke check for that self-resolution: it scans raw instruction bytes and does not decode
    /// wasm instruction boundaries.
    /// </remarks>
    public static bool WasmImageContainsWellKnownGlobalGet(WebcilImageReader reader, int wellKnownGlobalIndex)
    {
        // The well-known globals are 0/1/2, which all fit in a single ULEB128 payload byte. The padded
        // encoding below only writes that single payload byte, so it is correct for indices <= 0x7F.
        Debug.Assert((uint)wellKnownGlobalIndex <= 0x7F,
            $"Only single-byte well-known-global indices are supported; got {wellKnownGlobalIndex}.");

        // global.get (0x23) followed by the 5-byte padded ULEB128 of wellKnownGlobalIndex. Padding sets
        // the continuation bit on the first four bytes and clears the last, so a small index N
        // encodes as (N | 0x80), 0x80, 0x80, 0x80, 0x00.
        Span<byte> pattern = stackalloc byte[6];
        pattern[0] = 0x23;
        pattern[1] = (byte)((wellKnownGlobalIndex & 0x7F) | 0x80);
        pattern[2] = 0x80;
        pattern[3] = 0x80;
        pattern[4] = 0x80;
        pattern[5] = 0x00;

        for (int functionIndex = 0; ; functionIndex++)
        {
            WebcilImageReader.WasmFunctionInfo? body = reader.GetWasmFunctionBody(functionIndex);
            if (body is null)
                break;

            ReadOnlySpan<byte> instructions = body.Value.Image.AsSpan().Slice(
                body.Value.InstructionOffset, body.Value.InstructionLength);
            if (instructions.IndexOf(pattern) >= 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the default Webcil imports and defined section entries occupy the expected
    /// indices in their respective WASM external-kind index spaces.
    /// </summary>
    public static bool WasmIndexSpacesHaveExpectedEntries(WebcilImageReader reader, out string diagnostic)
    {
        if (!reader.IsWasmWrapped)
        {
            diagnostic = "Expected a WASM-wrapped Webcil image.";
            return false;
        }

        Dictionary<(string Module, string Name), WasmImportIndex> imports = ReadWasmImports(reader);
        (string Name, WasmImportKind Kind, uint Index)[] expectedImports =
        [
            ("stackPointer", WasmImportKind.Global, 0),
            ("imageBase", WasmImportKind.Global, 1),
            ("tableBase", WasmImportKind.Global, 2),
            ("table", WasmImportKind.Table, 0),
            ("memory", WasmImportKind.Memory, 0),
            ("rtlRestoreContextTag", WasmImportKind.Tag, 0),
        ];

        var failures = new List<string>();
        foreach ((string name, WasmImportKind expectedKind, uint expectedIndex) in expectedImports)
        {
            if (!imports.TryGetValue(("webcil", name), out WasmImportIndex actual))
            {
                failures.Add($"Expected import 'webcil.{name}' was not found.");
            }
            else if (actual.Kind != expectedKind || actual.Index != expectedIndex)
            {
                failures.Add(
                    $"Import 'webcil.{name}' was {actual.Kind} index {actual.Index}; " +
                    $"expected {expectedKind} index {expectedIndex}.");
            }
        }

        uint importedFunctionCount = CountWasmImports(imports, WasmImportKind.Function);
        uint importedTableCount = CountWasmImports(imports, WasmImportKind.Table);
        uint importedMemoryCount = CountWasmImports(imports, WasmImportKind.Memory);
        uint importedGlobalCount = CountWasmImports(imports, WasmImportKind.Global);
        uint importedTagCount = CountWasmImports(imports, WasmImportKind.Tag);

        CheckImportCount(WasmImportKind.Function, importedFunctionCount, 0, failures);
        CheckImportCount(WasmImportKind.Table, importedTableCount, 1, failures);
        CheckImportCount(WasmImportKind.Memory, importedMemoryCount, 1, failures);
        CheckImportCount(WasmImportKind.Global, importedGlobalCount, 3, failures);
        CheckImportCount(WasmImportKind.Tag, importedTagCount, 1, failures);

        uint definedFunctionCount = ReadWasmSectionEntryCount(reader, WasmSectionKind.Function);
        uint definedTableCount = ReadWasmSectionEntryCount(reader, WasmSectionKind.Table);
        uint definedMemoryCount = ReadWasmSectionEntryCount(reader, WasmSectionKind.Memory);
        uint definedGlobalCount = ReadWasmSectionEntryCount(reader, WasmSectionKind.Global);
        uint definedTagCount = ReadWasmSectionEntryCount(reader, WasmSectionKind.Tag);
        Dictionary<string, WasmExportIndex> exports = ReadWasmExports(reader);

        CheckSectionEntryCount(WasmSectionKind.Table, definedTableCount, 0, failures);
        CheckSectionEntryCount(WasmSectionKind.Memory, definedMemoryCount, 0, failures);
        CheckSectionEntryCount(WasmSectionKind.Global, definedGlobalCount, 1, failures);
        CheckSectionEntryCount(WasmSectionKind.Tag, definedTagCount, 0, failures);

        CheckWasmExport(exports, "table", WasmImportKind.Table, 0, failures);
        // webcilVersion is the first global defined in the module (not imported), so it's index should be the count of imported globals
        CheckWasmExport(exports, "webcilVersion", WasmImportKind.Global, importedGlobalCount, failures);
        CheckFunctionExports(exports, importedFunctionCount, definedFunctionCount, failures);

        diagnostic = failures.Count == 0
            ? "WASM imports, definitions, exports, and instruction references use the expected per-kind indices."
            : string.Join(Environment.NewLine, failures);
        return failures.Count == 0;
    }

    private static uint CountWasmImports(
        Dictionary<(string Module, string Name), WasmImportIndex> imports,
        WasmImportKind kind)
    {
        uint count = 0;
        foreach (WasmImportIndex import in imports.Values)
        {
            if (import.Kind == kind)
                count++;
        }

        return count;
    }

    private static void CheckImportCount(
        WasmImportKind kind,
        uint actualCount,
        uint expectedCount,
        List<string> failures)
    {
        if (actualCount != expectedCount)
            failures.Add($"Found {actualCount} {kind} imports; expected {expectedCount}.");
    }

    private static void CheckSectionEntryCount(
        WasmSectionKind section,
        uint actualCount,
        uint expectedCount,
        List<string> failures)
    {
        if (actualCount != expectedCount)
            failures.Add($"Found {actualCount} entries in the WASM {section} section; expected {expectedCount}.");
    }

    private static void CheckWasmExport(
        Dictionary<string, WasmExportIndex> exports,
        string name,
        WasmImportKind expectedKind,
        uint expectedIndex,
        List<string> failures)
    {
        if (!exports.TryGetValue(name, out WasmExportIndex actual))
        {
            failures.Add($"Expected WASM export '{name}' was not found.");
        }
        else if (actual.Kind != expectedKind || actual.Index != expectedIndex)
        {
            failures.Add(
                $"WASM export '{name}' was {actual.Kind} index {actual.Index}; " +
                $"expected {expectedKind} index {expectedIndex}.");
        }
    }

    private static void CheckFunctionExports(
        Dictionary<string, WasmExportIndex> exports,
        uint importedFunctionCount,
        uint definedFunctionCount,
        List<string> failures)
    {
        List<uint> functionIndices = exports.Values
            .Where(export => export.Kind == WasmImportKind.Function)
            .Select(export => export.Index)
            .Order()
            .ToList();

        if (functionIndices.Count != definedFunctionCount)
        {
            failures.Add(
                $"Found {functionIndices.Count} function exports for {definedFunctionCount} " +
                "function-section entries.");
            return;
        }

        for (int i = 0; i < functionIndices.Count; i++)
        {
            uint expectedIndex = importedFunctionCount + (uint)i;
            if (functionIndices[i] != expectedIndex)
            {
                failures.Add(
                    $"Function export index {functionIndices[i]} at sorted position {i}; " +
                    $"expected {expectedIndex} after {importedFunctionCount} function imports.");
            }
        }
    }

    private static Dictionary<(string Module, string Name), WasmImportIndex> ReadWasmImports(WebcilImageReader reader)
    {
        ReadOnlySpan<byte> image = reader.GetEntireImage().AsSpan();
        if (image.Length < 8)
            throw new BadImageFormatException("WASM image is shorter than its magic and version header.");

        int offset = 8;
        while (offset < image.Length)
        {
            byte sectionId = ReadWasmByte(image, ref offset, image.Length);
            uint sectionSize = ReadWasmUleb32(image, ref offset, image.Length);
            if (sectionSize > int.MaxValue || sectionSize > image.Length - offset)
                throw new BadImageFormatException($"WASM section {sectionId} extends beyond the image boundary.");

            int sectionEnd = offset + (int)sectionSize;
            if (sectionId == (byte)WasmSectionKind.Import)
                return ReadWasmImportSection(image, ref offset, sectionEnd);

            offset = sectionEnd;
        }

        throw new BadImageFormatException("WASM image does not contain an import section.");
    }

    private static uint ReadWasmSectionEntryCount(WebcilImageReader reader, WasmSectionKind section)
    {
        ReadOnlySpan<byte> image = reader.GetEntireImage().AsSpan();
        if (!TryGetWasmSectionBounds(image, section, out int offset, out int sectionEnd))
            return 0;

        return ReadWasmUleb32(image, ref offset, sectionEnd);
    }

    private static Dictionary<string, WasmExportIndex> ReadWasmExports(WebcilImageReader reader)
    {
        ReadOnlySpan<byte> image = reader.GetEntireImage().AsSpan();
        if (!TryGetWasmSectionBounds(image, WasmSectionKind.Export, out int offset, out int sectionEnd))
            throw new BadImageFormatException("WASM image does not contain an export section.");

        uint exportCount = ReadWasmUleb32(image, ref offset, sectionEnd);
        var exports = new Dictionary<string, WasmExportIndex>();
        for (uint i = 0; i < exportCount; i++)
        {
            string name = ReadWasmName(image, ref offset, sectionEnd);
            byte kindValue = ReadWasmByte(image, ref offset, sectionEnd);
            if (kindValue >= (byte)WasmImportKind.Count)
                throw new BadImageFormatException($"Invalid WASM export kind {kindValue}.");

            uint index = ReadWasmUleb32(image, ref offset, sectionEnd);
            if (!exports.TryAdd(name, new WasmExportIndex((WasmImportKind)kindValue, index)))
                throw new BadImageFormatException($"Duplicate WASM export '{name}'.");
        }

        if (offset != sectionEnd)
            throw new BadImageFormatException("WASM export section contains trailing data.");

        return exports;
    }

    private static bool TryGetWasmSectionBounds(
        ReadOnlySpan<byte> image,
        WasmSectionKind expectedSection,
        out int sectionOffset,
        out int sectionEnd)
    {
        if (image.Length < 8)
            throw new BadImageFormatException("WASM image is shorter than its magic and version header.");

        int offset = 8;
        while (offset < image.Length)
        {
            byte sectionId = ReadWasmByte(image, ref offset, image.Length);
            uint sectionSize = ReadWasmUleb32(image, ref offset, image.Length);
            if (sectionSize > int.MaxValue || sectionSize > image.Length - offset)
                throw new BadImageFormatException($"WASM section {sectionId} extends beyond the image boundary.");

            int currentSectionEnd = offset + (int)sectionSize;
            if (sectionId == (byte)expectedSection)
            {
                sectionOffset = offset;
                sectionEnd = currentSectionEnd;
                return true;
            }

            offset = currentSectionEnd;
        }

        sectionOffset = 0;
        sectionEnd = 0;
        return false;
    }

    private static Dictionary<(string Module, string Name), WasmImportIndex> ReadWasmImportSection(
        ReadOnlySpan<byte> image,
        ref int offset,
        int sectionEnd)
    {
        uint importCount = ReadWasmUleb32(image, ref offset, sectionEnd);
        uint[] nextIndices = new uint[(int)WasmImportKind.Count];
        var imports = new Dictionary<(string Module, string Name), WasmImportIndex>();

        for (uint i = 0; i < importCount; i++)
        {
            string module = ReadWasmName(image, ref offset, sectionEnd);
            string name = ReadWasmName(image, ref offset, sectionEnd);
            byte kindValue = ReadWasmByte(image, ref offset, sectionEnd);
            if (kindValue >= (byte)WasmImportKind.Count)
                throw new BadImageFormatException($"Invalid WASM import kind {kindValue}.");

            var kind = (WasmImportKind)kindValue;
            uint index = nextIndices[kindValue]++;

            switch (kind)
            {
                case WasmImportKind.Function:
                    ReadWasmUleb32(image, ref offset, sectionEnd);
                    break;
                case WasmImportKind.Table:
                    SkipWasmRefType(image, ref offset, sectionEnd);
                    SkipWasmLimits(image, ref offset, sectionEnd);
                    break;
                case WasmImportKind.Memory:
                    SkipWasmLimits(image, ref offset, sectionEnd);
                    break;
                case WasmImportKind.Global:
                    SkipWasmValueType(image, ref offset, sectionEnd);
                    ReadWasmByte(image, ref offset, sectionEnd);
                    break;
                case WasmImportKind.Tag:
                    if (ReadWasmByte(image, ref offset, sectionEnd) != 0)
                        throw new BadImageFormatException("WASM exception tag import has a non-zero attribute.");
                    ReadWasmUleb32(image, ref offset, sectionEnd);
                    break;
            }

            if (!imports.TryAdd((module, name), new WasmImportIndex(kind, index)))
                throw new BadImageFormatException($"Duplicate WASM import '{module}.{name}'.");
        }

        if (offset != sectionEnd)
            throw new BadImageFormatException("WASM import section contains trailing data.");

        return imports;
    }

    private static string ReadWasmName(ReadOnlySpan<byte> image, ref int offset, int end)
    {
        uint length = ReadWasmUleb32(image, ref offset, end);
        if (length > int.MaxValue || length > end - offset)
            throw new BadImageFormatException("WASM import name extends beyond the import section.");

        string name = Encoding.UTF8.GetString(image.Slice(offset, (int)length));
        offset += (int)length;
        return name;
    }

    private static byte ReadWasmByte(ReadOnlySpan<byte> image, ref int offset, int end)
    {
        if (offset >= end)
            throw new BadImageFormatException("Unexpected end of WASM data.");

        return image[offset++];
    }

    private static uint ReadWasmUleb32(ReadOnlySpan<byte> image, ref int offset, int end)
    {
        uint result = 0;
        for (int shift = 0; shift < 35; shift += 7)
        {
            byte value = ReadWasmByte(image, ref offset, end);
            if (shift == 28 && (value & 0xF0) != 0)
                throw new BadImageFormatException("Invalid 32-bit WASM ULEB128 value.");

            result |= (uint)(value & 0x7F) << shift;
            if ((value & 0x80) == 0)
                return result;
        }

        throw new BadImageFormatException("Invalid 32-bit WASM ULEB128 value.");
    }

    private static void SkipWasmValueType(ReadOnlySpan<byte> image, ref int offset, int end)
    {
        byte valueType = ReadWasmByte(image, ref offset, end);
        if (valueType >= 0x7B && valueType <= 0x7F)
            return;

        offset--;
        SkipWasmRefType(image, ref offset, end);
    }

    private static void SkipWasmRefType(ReadOnlySpan<byte> image, ref int offset, int end)
    {
        byte refType = ReadWasmByte(image, ref offset, end);
        if (refType >= 0x69 && refType <= 0x74)
            return;

        if (refType is not 0x63 and not 0x64)
            throw new BadImageFormatException($"Invalid WASM reference type 0x{refType:X2}.");

        byte heapType = ReadWasmByte(image, ref offset, end);
        if (heapType >= 0x69 && heapType <= 0x74)
            return;

        offset--;
        if (ReadWasmSleb33(image, ref offset, end) < 0)
            throw new BadImageFormatException("WASM reference type has a negative heap type index.");
    }

    private static long ReadWasmSleb33(ReadOnlySpan<byte> image, ref int offset, int end)
    {
        long result = 0;
        int shift = 0;
        byte value;
        do
        {
            value = ReadWasmByte(image, ref offset, end);
            result |= (long)(value & 0x7F) << shift;
            shift += 7;
            if (shift > 35)
                throw new BadImageFormatException("Invalid 33-bit WASM SLEB128 value.");
        }
        while ((value & 0x80) != 0);

        if ((value & 0x40) != 0 && shift < 64)
            result |= -1L << shift;

        return result;
    }

    private static void SkipWasmLimits(ReadOnlySpan<byte> image, ref int offset, int end)
    {
        uint flags = ReadWasmUleb32(image, ref offset, end);
        ReadWasmUleb32(image, ref offset, end);
        if ((flags & 0x01) != 0)
            ReadWasmUleb32(image, ref offset, end);
    }

    private enum WasmSectionKind : byte
    {
        Import = 2,
        Function = 3,
        Table = 4,
        Memory = 5,
        Global = 6,
        Export = 7,
        Element = 9,
        Tag = 13,
    }

    private enum WasmImportKind : byte
    {
        Function,
        Table,
        Memory,
        Global,
        Tag,
        Count,
    }

    private readonly struct WasmImportIndex
    {
        public WasmImportIndex(WasmImportKind kind, uint index)
        {
            Kind = kind;
            Index = index;
        }

        public WasmImportKind Kind { get; }
        public uint Index { get; }
    }

    private readonly struct WasmExportIndex
    {
        public WasmExportIndex(WasmImportKind kind, uint index)
        {
            Kind = kind;
            Index = index;
        }

        public WasmImportKind Kind { get; }
        public uint Index { get; }
    }
}
