// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using ILCompiler.Reflection.ReadyToRun;
using Internal.ReadyToRunConstants;
using Internal.Runtime;
using Xunit;

namespace ILCompiler.ReadyToRun.Tests.TestCasesRunner;

/// <summary>
/// Static assertion helpers for validating R2R images via <see cref="ReadyToRunReader"/>.
/// Use these in <see cref="CrossgenCompilation.Validate"/> callbacks.
/// </summary>
internal static class R2RAssert
{
    /// <summary>
    /// Returns all methods (assembly methods + instance methods) from the reader.
    /// </summary>
    public static List<ReadyToRunMethod> GetAllMethods(ReadyToRunReader reader)
    {
        var methods = new List<ReadyToRunMethod>();
        foreach (var assembly in reader.ReadyToRunAssemblies)
            methods.AddRange(assembly.Methods);
        foreach (var instanceMethod in reader.InstanceMethods)
            methods.Add(instanceMethod.Method);

        return methods;
    }

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
        CheckElementFunctionReferences(reader, importedFunctionCount, definedFunctionCount, failures);

        const uint RestoreContextTagIndex = 0;
        if (!WasmImageContainsRestoreContextTagReference(reader, RestoreContextTagIndex))
        {
            failures.Add(
                $"Expected a try_table catch_ref of imported tag index {RestoreContextTagIndex}.");
        }

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

    private static void CheckElementFunctionReferences(
        WebcilImageReader reader,
        uint importedFunctionCount,
        uint definedFunctionCount,
        List<string> failures)
    {
        List<uint> functionIndices = ReadWasmElementFunctionIndices(reader);
        if (functionIndices.Count != definedFunctionCount)
        {
            failures.Add(
                $"Found {functionIndices.Count} element function references for " +
                $"{definedFunctionCount} function-section entries.");
            return;
        }

        for (int i = 0; i < functionIndices.Count; i++)
        {
            uint expectedIndex = importedFunctionCount + (uint)i;
            if (functionIndices[i] != expectedIndex)
            {
                failures.Add(
                    $"Element function reference {functionIndices[i]} at position {i}; " +
                    $"expected {expectedIndex} after {importedFunctionCount} function imports.");
            }
        }
    }

    private static bool WasmImageContainsRestoreContextTagReference(
        WebcilImageReader reader,
        uint restoreContextTagIndex)
    {
        Debug.Assert(restoreContextTagIndex <= 0x7F);

        // try_table, empty block type, one catch clause, catch_ref, padded tagidx, label 0
        Span<byte> pattern = stackalloc byte[10];
        pattern[0] = 0x1F;
        pattern[1] = 0x40;
        pattern[2] = 0x01;
        pattern[3] = 0x01;
        pattern[4] = (byte)((restoreContextTagIndex & 0x7F) | 0x80);
        pattern[5] = 0x80;
        pattern[6] = 0x80;
        pattern[7] = 0x80;
        pattern[8] = 0x00;
        pattern[9] = 0x00;

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

    private static List<uint> ReadWasmElementFunctionIndices(WebcilImageReader reader)
    {
        ReadOnlySpan<byte> image = reader.GetEntireImage().AsSpan();
        if (!TryGetWasmSectionBounds(image, WasmSectionKind.Element, out int offset, out int sectionEnd))
            throw new BadImageFormatException("WASM image does not contain an element section.");

        uint segmentCount = ReadWasmUleb32(image, ref offset, sectionEnd);
        if (segmentCount != 1)
            throw new BadImageFormatException($"Expected one WASM element segment, found {segmentCount}.");

        uint segmentFlags = ReadWasmUleb32(image, ref offset, sectionEnd);
        if (segmentFlags != 1)
            throw new BadImageFormatException($"Expected a passive WASM element segment, found flags {segmentFlags}.");

        byte elementKind = ReadWasmByte(image, ref offset, sectionEnd);
        if (elementKind != 0)
            throw new BadImageFormatException($"Expected a funcref WASM element segment, found kind {elementKind}.");

        uint functionCount = ReadWasmUleb32(image, ref offset, sectionEnd);
        var functionIndices = new List<uint>(checked((int)functionCount));
        for (uint i = 0; i < functionCount; i++)
            functionIndices.Add(ReadWasmUleb32(image, ref offset, sectionEnd));

        if (offset != sectionEnd)
            throw new BadImageFormatException("WASM element section contains trailing data.");

        return functionIndices;
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

    /// <summary>
    /// Returns true if the R2R image contains a manifest or MSIL assembly reference with the given name.
    /// </summary>
    public static bool HasManifestRef(ReadyToRunReader reader, string assemblyName, out string diagnostic)
    {
        var allRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var globalMetadata = reader.GetGlobalMetadata();
        if (globalMetadata is not null)
        {
            var mdReader = globalMetadata.MetadataReader;
            foreach (var handle in mdReader.AssemblyReferences)
            {
                var assemblyRef = mdReader.GetAssemblyReference(handle);
                allRefs.Add(mdReader.GetString(assemblyRef.Name));
            }
        }

        foreach (var kvp in reader.ManifestReferenceAssemblies)
            allRefs.Add(kvp.Key);

        bool found = allRefs.Contains(assemblyName);
        diagnostic = found
            ? $"Found manifest/MSIL ref '{assemblyName}'."
            : $"Expected assembly reference '{assemblyName}' not found. " +
              $"Found: [{string.Join(", ", allRefs.OrderBy(s => s))}]";
        return found;
    }

    /// <summary>
    /// Returns true if ARM R2R relocations preserve the Thumb bit only for raw runtime function
    /// start RVAs, while non-code pointers keep even RVAs.
    /// </summary>
    public static bool HasExpectedArmThumbBitTargets(ReadyToRunReader reader, out string diagnostic)
    {
        if (reader.Machine != Machine.ArmThumb2)
        {
            diagnostic = $"Expected ARM Thumb2 image, but found {reader.Machine}.";
            return false;
        }

        var failures = new List<string>();

        bool result = true;
        result &= SectionRVAIsEven(reader, ReadyToRunSectionType.ExceptionInfo, failures);
        result &= SectionRVAIsEven(reader, ReadyToRunSectionType.DelayLoadMethodCallThunks, failures);
        result &= DelayLoadHelperImportTargetsAreOdd(reader, failures);
        result &= ExceptionInfoMethodRVAsAreEven(reader, failures);
        result &= RuntimeFunctionStartRVAsAreOdd(reader, failures);
        result &= BaseRelocatedCodePointersAreOdd(reader, failures);

        diagnostic = result
            ? "ARM Thumb-bit relocation targets are encoded as expected."
            : string.Join(Environment.NewLine, failures);
        return result;
    }

    /// <summary>
    /// Returns true if the image contains hot/cold split runtime functions and the raw cold
    /// runtime function start RVAs carry exactly one ARM Thumb bit.
    /// </summary>
    public static bool HasExpectedArmHotColdRuntimeFunctionTargets(ReadyToRunReader reader, out string diagnostic)
    {
        if (reader.Machine != Machine.ArmThumb2)
        {
            diagnostic = $"Expected ARM Thumb2 image, but found {reader.Machine}.";
            return false;
        }

        var failures = new List<string>();

        bool result = true;
        result &= RuntimeFunctionStartRVAsAreOdd(reader, failures);
        result &= ColdRuntimeFunctionStartRVAsAreOdd(reader, failures);

        diagnostic = result
            ? "ARM hot/cold runtime function targets are encoded as expected."
            : string.Join(Environment.NewLine, failures);
        return result;
    }

    /// <summary>
    /// Returns true if the manifest metadata and component assembly tables in a composite image
    /// start on 4-byte aligned RVAs. Both sections contain DWORD fields that the runtime reads
    /// directly, so unaligned sections can fault on architectures such as 32-bit ARM.
    /// </summary>
    public static bool CompositeManifestSectionsAreAligned(ReadyToRunReader reader, out string diagnostic)
    {
        const int RequiredAlignment = 4;
        var failures = new List<string>();

        bool result = true;
        result &= SectionRVAIsAligned(reader, ReadyToRunSectionType.ManifestMetadata, RequiredAlignment, failures);
        result &= SectionRVAIsAligned(reader, ReadyToRunSectionType.ComponentAssemblies, RequiredAlignment, failures);

        diagnostic = result
            ? $"Composite manifest sections are {RequiredAlignment}-byte aligned."
            : string.Join(Environment.NewLine, failures);
        return result;
    }

    /// <summary>
    /// Returns true if the manifest assembly MVID table in a composite image is present, holds a
    /// whole number of 16-byte GUID entries, and starts on a 4-byte aligned RVA. The runtime reads
    /// each entry as a GUID by value, so the table must be 4-byte aligned to avoid alignment faults
    /// (SIGBUS) on architectures such as 32-bit ARM that do not permit unaligned multi-word loads.
    /// </summary>
    public static bool ManifestAssemblyMvidsTableIsAligned(ReadyToRunReader reader, out string diagnostic)
    {
        const int GuidByteSize = 16;
        const int RequiredAlignment = 4;

        if (!reader.ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.ManifestAssemblyMvids, out ReadyToRunSection section))
        {
            diagnostic = "Expected ManifestAssemblyMvids section not found.";
            return false;
        }

        var failures = new List<string>();

        if (section.Size <= 0)
            failures.Add("Expected ManifestAssemblyMvids section to be non-empty.");

        if (section.Size % GuidByteSize != 0)
            failures.Add($"ManifestAssemblyMvids section size {section.Size} should be a multiple of {GuidByteSize} (a table of GUIDs).");

        if ((section.RelativeVirtualAddress % RequiredAlignment) != 0)
            failures.Add($"ManifestAssemblyMvids section RVA 0x{section.RelativeVirtualAddress:X8} should be aligned to {RequiredAlignment} bytes.");

        diagnostic = failures.Count == 0
            ? $"ManifestAssemblyMvids table is {RequiredAlignment}-byte aligned ({section.Size / GuidByteSize} entries)."
            : string.Join(Environment.NewLine, failures);
        return failures.Count == 0;
    }

    private static bool SectionRVAIsAligned(ReadyToRunReader reader, ReadyToRunSectionType sectionType, int requiredAlignment, List<string> failures)
    {
        if (!reader.ReadyToRunHeader.Sections.TryGetValue(sectionType, out ReadyToRunSection section))
        {
            failures.Add($"Expected {sectionType} section not found.");
            return false;
        }

        bool result = true;
        if (section.Size <= 0)
        {
            failures.Add($"Expected {sectionType} section to be non-empty.");
            result = false;
        }

        if ((section.RelativeVirtualAddress % requiredAlignment) != 0)
        {
            failures.Add($"{sectionType} section RVA 0x{section.RelativeVirtualAddress:X8} should be aligned to {requiredAlignment} bytes.");
            result = false;
        }

        return result;
    }

    private static bool SectionRVAIsEven(ReadyToRunReader reader, ReadyToRunSectionType sectionType, List<string> failures)
    {
        if (!reader.ReadyToRunHeader.Sections.TryGetValue(sectionType, out ReadyToRunSection section))
        {
            failures.Add($"Expected {sectionType} section not found.");
            return false;
        }

        bool result = true;
        if (section.Size <= 0)
        {
            failures.Add($"Expected {sectionType} section to be non-empty.");
            result = false;
        }

        if ((section.RelativeVirtualAddress & 1) != 0)
        {
            failures.Add($"{sectionType} section RVA 0x{section.RelativeVirtualAddress:X8} should be even.");
            result = false;
        }

        return result;
    }

    private static bool ExceptionInfoMethodRVAsAreEven(ReadyToRunReader reader, List<string> failures)
    {
        if (!reader.ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.ExceptionInfo, out ReadyToRunSection section))
        {
            return true;
        }

        bool result = true;
        int offset = reader.CompositeReader.GetOffset(section.RelativeVirtualAddress);
        int endOffset = offset + section.Size;
        int entries = 0;
        for (; offset + 2 * sizeof(int) <= endOffset; offset += 2 * sizeof(int))
        {
            int methodRva = BinaryPrimitives.ReadInt32LittleEndian(reader.Image.AsSpan(offset));
            if (methodRva == -1)
            {
                break;
            }

            entries++;
            if ((methodRva & 1) != 0)
            {
                failures.Add($"ExceptionInfo method RVA 0x{methodRva:X8} should be even.");
                result = false;
            }
        }

        if (entries == 0)
        {
            failures.Add("Expected ExceptionInfo to contain at least one method entry.");
            result = false;
        }

        return result;
    }

    private static bool DelayLoadHelperImportTargetsAreOdd(ReadyToRunReader reader, List<string> failures)
    {
        bool result = true;
        int entries = 0;
        foreach (ReadyToRunImportSection section in reader.ImportSections)
        {
            if ((section.Flags & ReadyToRunImportSectionFlags.PCode) == 0)
            {
                continue;
            }

            int offset = reader.CompositeReader.GetOffset(section.SectionRVA);
            int endOffset = offset + section.SectionSize;
            for (; offset + section.EntrySize <= endOffset; offset += section.EntrySize)
            {
                int targetAddress = BinaryPrimitives.ReadInt32LittleEndian(reader.Image.AsSpan(offset));
                if (targetAddress == 0)
                {
                    continue;
                }

                entries++;
                if ((targetAddress & 1) == 0)
                {
                    failures.Add($"PCode import target 0x{targetAddress:X8} should have the Thumb bit set.");
                    result = false;
                }
            }
        }

        if (entries == 0)
        {
            failures.Add("Expected at least one non-empty PCode import target.");
            result = false;
        }

        return result;
    }

    private static bool BaseRelocatedCodePointersAreOdd(ReadyToRunReader reader, List<string> failures)
    {
        byte[] image = reader.Image.ToArray();
        using var peReader = new PEReader(new MemoryStream(image));

        if (peReader.PEHeaders.PEHeader is null)
        {
            failures.Add("Expected PE header to be present.");
            return false;
        }

        DirectoryEntry relocDirectory = peReader.PEHeaders.PEHeader.BaseRelocationTableDirectory;
        if (relocDirectory.Size == 0)
        {
            failures.Add("Expected base relocation table to contain entries.");
            return false;
        }

        List<(int Start, int End)> runtimeFunctionRanges = GetAllMethods(reader)
            .SelectMany(method => method.RuntimeFunctions)
            .Select(function =>
            {
                int start = function.StartAddress & ~1;
                int size = function.Size >= 0 ? function.Size : (function.EndAddress & ~1) - start;
                return (Start: start, End: start + size);
            })
            .Where(range => range.End > range.Start)
            .OrderBy(range => range.Start)
            .ToList();

        bool result = true;
        int entries = 0;
        int offset = peReader.GetOffset(relocDirectory.RelativeVirtualAddress);
        int endOffset = offset + relocDirectory.Size;
        while (offset + 2 * sizeof(int) <= endOffset)
        {
            int pageRva = BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(offset));
            int blockSize = BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(offset + sizeof(int)));
            offset += 2 * sizeof(int);

            if (pageRva == 0 || blockSize < 2 * sizeof(int))
                break;

            int entryCount = (blockSize - 2 * sizeof(int)) / sizeof(ushort);
            for (int i = 0; i < entryCount && offset + sizeof(ushort) <= endOffset; i++, offset += sizeof(ushort))
            {
                ushort entry = BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(offset));
                const int ImageRelBasedHighLow = 3;
                if (entry >> 12 != ImageRelBasedHighLow)
                    continue;

                int relocatedRva = pageRva + (entry & 0x0FFF);
                int relocatedOffset = peReader.GetOffset(relocatedRva);
                int targetAddress = BinaryPrimitives.ReadInt32LittleEndian(image.AsSpan(relocatedOffset));
                int targetRva = targetAddress - (int)reader.ImageBase;
                if (!IsRuntimeFunctionAddress(runtimeFunctionRanges, targetRva & ~1))
                    continue;

                entries++;
                if ((targetAddress & 1) == 0)
                {
                    failures.Add($"Base relocation at RVA 0x{relocatedRva:X8} points to code address 0x{targetAddress:X8} without the Thumb bit set.");
                    result = false;
                }
            }
        }

        if (entries == 0)
        {
            failures.Add("Expected at least one base-relocated code pointer.");
            result = false;
        }

        return result;
    }

    private static bool IsRuntimeFunctionAddress(List<(int Start, int End)> ranges, int rva)
    {
        foreach ((int start, int end) in ranges)
        {
            if (rva < start)
                return false;
            if (rva < end)
                return true;
        }

        return false;
    }

    private static bool ColdRuntimeFunctionStartRVAsAreOdd(ReadyToRunReader reader, List<string> failures)
    {
        if (!reader.ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.RuntimeFunctions, out ReadyToRunSection runtimeFunctionsSection))
        {
            failures.Add("Expected RuntimeFunctions section not found.");
            return false;
        }

        if (!reader.ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.HotColdMap, out ReadyToRunSection hotColdMapSection))
        {
            failures.Add("Expected HotColdMap section not found.");
            return false;
        }

        const int RuntimeFunctionSize = 2 * sizeof(int);
        int runtimeFunctionCount = runtimeFunctionsSection.Size / RuntimeFunctionSize;
        int hotColdMapCount = hotColdMapSection.Size / (2 * sizeof(int));
        if (hotColdMapCount == 0)
        {
            failures.Add("Expected HotColdMap to contain at least one mapping.");
            return false;
        }

        var mappings = new List<(int ColdIndex, int HotIndex)>(hotColdMapCount);
        int hotColdMapOffset = reader.CompositeReader.GetOffset(hotColdMapSection.RelativeVirtualAddress);
        for (int i = 0; i < hotColdMapCount; i++, hotColdMapOffset += 2 * sizeof(int))
        {
            int coldIndex = BinaryPrimitives.ReadInt32LittleEndian(reader.Image.AsSpan(hotColdMapOffset));
            int hotIndex = BinaryPrimitives.ReadInt32LittleEndian(reader.Image.AsSpan(hotColdMapOffset + sizeof(int)));
            mappings.Add((coldIndex, hotIndex));
        }

        bool result = true;
        int entries = 0;
        int runtimeFunctionsOffset = reader.CompositeReader.GetOffset(runtimeFunctionsSection.RelativeVirtualAddress);
        for (int i = 0; i < mappings.Count; i++)
        {
            int coldIndex = mappings[i].ColdIndex;
            int coldEndIndex = i + 1 < mappings.Count ? mappings[i + 1].ColdIndex : runtimeFunctionCount;
            if (coldIndex < 0 || coldEndIndex > runtimeFunctionCount || coldIndex >= coldEndIndex)
            {
                failures.Add($"Invalid HotColdMap cold runtime function range [{coldIndex}, {coldEndIndex}) for {runtimeFunctionCount} runtime functions.");
                result = false;
                continue;
            }

            for (int runtimeFunctionIndex = coldIndex; runtimeFunctionIndex < coldEndIndex; runtimeFunctionIndex++)
            {
                int entryOffset = runtimeFunctionsOffset + runtimeFunctionIndex * RuntimeFunctionSize;
                int startRva = BinaryPrimitives.ReadInt32LittleEndian(reader.Image.AsSpan(entryOffset));
                entries++;
                if ((startRva & 1) == 0)
                {
                    failures.Add($"Cold RuntimeFunctions[{runtimeFunctionIndex}] start RVA 0x{startRva:X8} should have the Thumb bit set.");
                    result = false;
                }
            }
        }

        if (entries == 0)
        {
            failures.Add("Expected at least one cold runtime function entry.");
            result = false;
        }

        return result;
    }

    private static bool RuntimeFunctionStartRVAsAreOdd(ReadyToRunReader reader, List<string> failures)
    {
        if (!reader.ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.RuntimeFunctions, out ReadyToRunSection section))
        {
            failures.Add("Expected RuntimeFunctions section not found.");
            return false;
        }

        int runtimeFunctionSize = 2 * sizeof(int);
        if (section.Size < runtimeFunctionSize)
        {
            failures.Add($"Expected RuntimeFunctions section to contain at least one entry, but size is {section.Size}.");
            return false;
        }

        bool result = true;
        int offset = reader.CompositeReader.GetOffset(section.RelativeVirtualAddress);
        int count = section.Size / runtimeFunctionSize;
        int entries = 0;
        for (int index = 0; index < count; index++, offset += runtimeFunctionSize)
        {
            int startRva = BinaryPrimitives.ReadInt32LittleEndian(reader.Image.AsSpan(offset));
            if (startRva == -1)
            {
                break;
            }

            entries++;
            if ((startRva & 1) == 0)
            {
                failures.Add($"RuntimeFunctions[{index}] start RVA 0x{startRva:X8} should have the Thumb bit set.");
                result = false;
            }
        }

        if (entries == 0)
        {
            failures.Add("Expected RuntimeFunctions to contain at least one entry.");
            result = false;
        }

        return result;
    }

    /// <summary>
    /// Returns true if the CrossModuleInlineInfo section records that <paramref name="inlinerMethodName"/>
    /// inlined <paramref name="inlineeMethodName"/> and the inlinee is encoded as a cross-module
    /// reference (ILBody import index, not a local MethodDef RID). If a pair matches but the
    /// encoding is wrong, returns false with the mismatch described in <paramref name="diagnostic"/>.
    /// </summary>
    public static bool HasCrossModuleInlinedMethod(ReadyToRunReader reader, string inlinerMethodName, string inlineeMethodName, out string diagnostic)
    {
        if (!TryGetCrossModuleInliningInfoSection(reader, out var inliningInfo, out diagnostic))
            return false;

        var allPairs = new List<string>();
        foreach (var (inlinerName, inlineeName, inlineeKind) in inliningInfo.GetInliningPairs())
        {
            allPairs.Add($"{inlinerName} → {inlineeName} ({inlineeKind})");

            if (inlinerName.Contains(inlinerMethodName, StringComparison.OrdinalIgnoreCase) &&
                inlineeName.Contains(inlineeMethodName, StringComparison.OrdinalIgnoreCase))
            {
                if (inlineeKind == CrossModuleInliningInfoSection.InlineeReferenceKind.CrossModule)
                {
                    diagnostic = $"Found cross-module inlining '{inlinerName} → {inlineeName}'.";
                    return true;
                }

                diagnostic =
                    $"Found inlining pair '{inlinerName} → {inlineeName}' but the inlinee is not encoded " +
                    $"as a cross-module reference ({inlineeKind}). Expected ILBody import encoding.";
                return false;
            }
        }

        diagnostic =
            $"Expected cross-module inlining '{inlineeMethodName}' into '{inlinerMethodName}', but it was not found.\n" +
            $"Recorded inlining pairs:\n  {string.Join("\n  ", allPairs)}";
        return false;
    }

    /// <summary>
    /// Returns true if the CrossModuleInlineInfo section has an entry for an inlinee matching
    /// <paramref name="inlineeMethodName"/> with cross-module inliners whose resolved names
    /// contain each of the <paramref name="expectedInlinerNames"/>. This validates that
    /// cross-module inliner indices (encoded as absolute ILBody import indices) resolve
    /// to the correct method names.
    /// </summary>
    public static bool HasCrossModuleInliners(
        ReadyToRunReader reader,
        string inlineeMethodName,
        IEnumerable<string> expectedInlinerNames,
        out string diagnostic)
    {
        if (!TryGetCrossModuleInliningInfoSection(reader, out var inliningInfo, out diagnostic))
            return false;

        foreach (var entry in inliningInfo.GetEntries())
        {
            string inlineeName = inliningInfo.ResolveMethodName(entry.Inlinee);
            if (!inlineeName.Contains(inlineeMethodName, StringComparison.OrdinalIgnoreCase))
                continue;

            var crossModuleInlinerNames = new List<string>();
            foreach (var inliner in entry.Inliners)
            {
                if (inliner.IsCrossModule)
                    crossModuleInlinerNames.Add(inliningInfo.ResolveMethodName(inliner));
            }

            foreach (string expected in expectedInlinerNames)
            {
                if (!crossModuleInlinerNames.Any(n => n.Contains(expected, StringComparison.OrdinalIgnoreCase)))
                {
                    diagnostic =
                        $"Inlinee '{inlineeName}': expected a cross-module inliner matching '{expected}' " +
                        $"but found only:\n  {string.Join("\n  ", crossModuleInlinerNames)}";
                    return false;
                }
            }

            diagnostic =
                $"Inlinee '{inlineeName}' has all expected cross-module inliners: " +
                $"[{string.Join(", ", expectedInlinerNames)}]";
            return true;
        }

        var allEntries = new List<string>();
        foreach (var (inlinerName, inlineeName, _) in inliningInfo.GetInliningPairs())
            allEntries.Add($"{inlinerName} → {inlineeName}");

        diagnostic =
            $"No CrossModuleInlineInfo entry found for inlinee matching '{inlineeMethodName}'.\n" +
            $"All inlining pairs:\n  {string.Join("\n  ", allEntries)}";
        return false;
    }

    /// <summary>
    /// Returns true if any inlining info section (CrossModuleInlineInfo or InliningInfo2) records
    /// that <paramref name="inlinerMethodName"/> inlined <paramref name="inlineeMethodName"/>.
    /// Does not check whether the encoding is cross-module or local.
    /// </summary>
    public static bool HasInlinedMethod(ReadyToRunReader reader, string inlinerMethodName, string inlineeMethodName, out string diagnostic)
    {
        var foundPairs = new List<string>();

        if (reader.ReadyToRunHeader.Sections.ContainsKey(ReadyToRunSectionType.CrossModuleInlineInfo))
        {
            var inliningInfo = GetCrossModuleInliningInfoSection(reader);
            foreach (var (inlinerName, inlineeName, _) in inliningInfo.GetInliningPairs())
            {
                if (inlinerName.Contains(inlinerMethodName, StringComparison.OrdinalIgnoreCase) &&
                    inlineeName.Contains(inlineeMethodName, StringComparison.OrdinalIgnoreCase))
                {
                    diagnostic = $"Found inlining '{inlinerName} -> {inlineeName}' in CrossModuleInlineInfo.";
                    return true;
                }
                foundPairs.Add($"[CXMI] {inlinerName} -> {inlineeName}");
            }
        }

        foreach (var info2 in GetAllInliningInfo2Sections(reader))
        {
            foreach (var (inlinerName, inlineeName) in info2.GetInliningPairs())
            {
                if (inlinerName.Contains(inlinerMethodName, StringComparison.OrdinalIgnoreCase) &&
                    inlineeName.Contains(inlineeMethodName, StringComparison.OrdinalIgnoreCase))
                {
                    diagnostic = $"Found inlining '{inlinerName} -> {inlineeName}' in InliningInfo2.";
                    return true;
                }
                foundPairs.Add($"[II2] {inlinerName} -> {inlineeName}");
            }
        }

        string pairList = foundPairs.Count > 0 ? string.Join("\n  ", foundPairs) : "(none)";
        diagnostic =
            $"Inlining '{inlineeMethodName}' into '{inlinerMethodName}' not found.\n" +
            $"Inlining pairs in image:\n  {pairList}";
        return false;
    }

    private static CrossModuleInliningInfoSection GetCrossModuleInliningInfoSection(ReadyToRunReader reader)
    {
        Assert.True(
            reader.ReadyToRunHeader.Sections.TryGetValue(
                ReadyToRunSectionType.CrossModuleInlineInfo, out ReadyToRunSection section),
            "Expected CrossModuleInlineInfo section not found in R2R image.");

        int offset = reader.GetOffset(section.RelativeVirtualAddress);
        int endOffset = offset + section.Size;

        return new CrossModuleInliningInfoSection(reader, offset, endOffset);
    }

    private static bool TryGetCrossModuleInliningInfoSection(ReadyToRunReader reader, out CrossModuleInliningInfoSection inliningInfo, out string diagnostic)
    {
        if (!reader.ReadyToRunHeader.Sections.TryGetValue(
                ReadyToRunSectionType.CrossModuleInlineInfo, out ReadyToRunSection section))
        {
            inliningInfo = null!;
            diagnostic = "Expected CrossModuleInlineInfo section not found in R2R image.";
            return false;
        }

        int offset = reader.GetOffset(section.RelativeVirtualAddress);
        int endOffset = offset + section.Size;
        inliningInfo = new CrossModuleInliningInfoSection(reader, offset, endOffset);
        diagnostic = "";
        return true;
    }

    private static IEnumerable<InliningInfoSection2> GetAllInliningInfo2Sections(ReadyToRunReader reader)
    {
        // InliningInfo2 can appear in the global header
        if (reader.ReadyToRunHeader.Sections.TryGetValue(
                ReadyToRunSectionType.InliningInfo2, out ReadyToRunSection globalSection))
        {
            int offset = reader.GetOffset(globalSection.RelativeVirtualAddress);
            int endOffset = offset + globalSection.Size;
            yield return new InliningInfoSection2(reader, offset, endOffset);
        }

        // In composite images, InliningInfo2 is per-assembly
        if (reader.ReadyToRunAssemblyHeaders is not null)
        {
            for (int asmIndex = 0; asmIndex < reader.ReadyToRunAssemblyHeaders.Count; asmIndex++)
            {
                var asmHeader = reader.ReadyToRunAssemblyHeaders[asmIndex];
                if (asmHeader.Sections.TryGetValue(
                        ReadyToRunSectionType.InliningInfo2, out ReadyToRunSection asmSection))
                {
                    int offset = reader.GetOffset(asmSection.RelativeVirtualAddress);
                    int endOffset = offset + asmSection.Size;
                    uint ownerModuleIndex = reader.Composite
                        ? (uint)(asmIndex + reader.ComponentAssemblyIndexOffset)
                        : 0;
                    yield return new InliningInfoSection2(reader, offset, endOffset, ownerModuleIndex);
                }
            }
        }
    }

    /// <summary>
    /// Returns true if the R2R image contains a CrossModuleInlineInfo section with at least one entry.
    /// </summary>
    public static bool HasCrossModuleInliningInfo(ReadyToRunReader reader, out string diagnostic)
    {
        if (!TryGetCrossModuleInliningInfoSection(reader, out var inliningInfo, out diagnostic))
            return false;

        string dump = inliningInfo.ToString();
        if (dump.Length == 0)
        {
            diagnostic = "CrossModuleInlineInfo section is present but contains no entries.";
            return false;
        }

        diagnostic = $"CrossModuleInlineInfo contains entries:\n{dump}";
        return true;
    }

    /// <summary>
    /// Returns true if the R2R image contains exactly one [ASYNC] variant entry whose signature contains the given method name.
    /// Returns false if no match is found or if more than one [ASYNC] method signature matches the search token.
    /// Use a precise token (e.g. <c>".MethodName("</c>) to avoid unintended substring matches.
    /// </summary>
    public static bool HasAsyncVariant(ReadyToRunReader reader, string methodName, out string diagnostic)
    {
        var asyncSigs = GetAllMethods(reader)
            .Where(m => m.SignatureString.Contains("[ASYNC]", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.SignatureString)
            .ToList();

        var matchingSigs = asyncSigs
            .Where(s => s.Contains(methodName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingSigs.Count == 0)
        {
            diagnostic = $"Expected [ASYNC] variant for '{methodName}' not found. " +
                $"Async methods: [{string.Join(", ", asyncSigs)}]";
            return false;
        }

        if (matchingSigs.Count > 1)
        {
            diagnostic = $"Expected exactly one [ASYNC] variant matching '{methodName}', " +
                $"but found {matchingSigs.Count}: [{string.Join(", ", matchingSigs)}]";
            return false;
        }

        diagnostic = $"Found [ASYNC] variant for '{methodName}'.";
        return true;
    }

    /// <summary>
    /// Returns true if the R2R image contains a [RESUME] stub entry whose signature contains the given method name.
    /// </summary>
    public static bool HasResumptionStub(ReadyToRunReader reader, string methodName, out string diagnostic)
    {
        var resumeSigs = GetAllMethods(reader)
            .Where(m => m.SignatureString.Contains("[RESUME]", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.SignatureString)
            .ToList();

        bool found = resumeSigs.Any(s => s.Contains(methodName, StringComparison.OrdinalIgnoreCase));
        diagnostic = found
            ? $"Found [RESUME] stub for '{methodName}'."
            : $"Expected [RESUME] stub for '{methodName}' not found. " +
              $"Resume methods: [{string.Join(", ", resumeSigs)}]";
        return found;
    }

    /// <summary>
    /// Returns true if every [ASYNC] method with a ResumptionStubEntryPoint fixup is followed
    /// immediately in emitted code by its [RESUME] stub.
    /// </summary>
    public static bool AsyncMethodsWithResumptionStubsAreAdjacent(ReadyToRunReader reader, out string diagnostic)
    {
        var methodsByRva = GetAllMethods(reader)
            .Select(method => (Method: method, EntryPointRva: GetEntryPointRva(method)))
            .Where(entry => entry.EntryPointRva >= 0)
            .OrderBy(entry => entry.EntryPointRva)
            .ToList();

        var failures = new List<string>();
        int checkedMethodCount = 0;
        for (int i = 0; i < methodsByRva.Count; i++)
        {
            ReadyToRunMethod method = methodsByRva[i].Method;
            if (!HasSignaturePrefix(method, "[ASYNC]") ||
                !HasFixupKind(method, ReadyToRunFixupKind.ResumptionStubEntryPoint))
            {
                continue;
            }

            checkedMethodCount++;
            if (i + 1 == methodsByRva.Count)
            {
                failures.Add($"'{method.SignatureString}' at RVA 0x{methodsByRva[i].EntryPointRva:X} is the final method.");
                continue;
            }

            ReadyToRunMethod nextMethod = methodsByRva[i + 1].Method;
            if (!HasSignaturePrefix(nextMethod, "[RESUME]") ||
                StripAsyncMethodPrefix(nextMethod.SignatureString) != StripAsyncMethodPrefix(method.SignatureString))
            {
                failures.Add(
                    $"'{method.SignatureString}' at RVA 0x{methodsByRva[i].EntryPointRva:X} " +
                    $"is followed by '{nextMethod.SignatureString}' at RVA 0x{methodsByRva[i + 1].EntryPointRva:X}.");
            }
        }

        if (checkedMethodCount == 0)
        {
            diagnostic = "No [ASYNC] methods with ResumptionStubEntryPoint fixups were found.";
            return false;
        }

        if (failures.Count != 0)
        {
            diagnostic =
                $"Expected each [ASYNC] method with a ResumptionStubEntryPoint fixup to be followed by its [RESUME] stub, " +
                $"but found {failures.Count} mismatch(es):\n  {string.Join("\n  ", failures)}";
            return false;
        }

        diagnostic = $"Found {checkedMethodCount} [ASYNC] method(s), each followed by its [RESUME] stub.";
        return true;
    }

    /// <summary>
    /// Returns true if the R2R image contains at least one ContinuationLayout fixup.
    /// </summary>
    public static bool HasContinuationLayout(ReadyToRunReader reader, out string diagnostic)
        => HasFixupKind(reader, ReadyToRunFixupKind.ContinuationLayout, out diagnostic);

    /// <summary>
    /// Returns true if a method whose signature contains <paramref name="methodName"/>
    /// has at least one ContinuationLayout fixup.
    /// </summary>
    public static bool HasContinuationLayout(ReadyToRunReader reader, string methodName, out string diagnostic)
        => HasFixupKindOnMethod(reader, ReadyToRunFixupKind.ContinuationLayout, methodName, out diagnostic);

    /// <summary>
    /// Returns true if the R2R image contains at least one ResumptionStubEntryPoint fixup.
    /// </summary>
    public static bool HasResumptionStubFixup(ReadyToRunReader reader, out string diagnostic)
        => HasFixupKind(reader, ReadyToRunFixupKind.ResumptionStubEntryPoint, out diagnostic);

    /// <summary>
    /// Returns true if a method whose signature contains <paramref name="methodName"/>
    /// has at least one ResumptionStubEntryPoint fixup.
    /// </summary>
    public static bool HasResumptionStubFixup(ReadyToRunReader reader, string methodName, out string diagnostic)
        => HasFixupKindOnMethod(reader, ReadyToRunFixupKind.ResumptionStubEntryPoint, methodName, out diagnostic);

    /// <summary>
    /// Returns true if exactly one method whose signature contains <paramref name="methodName"/>
    /// has at least one fixup of <paramref name="kind"/>, and that method has exactly
    /// <paramref name="expectedCount"/> fixups of that kind.
    /// Returns false if no match is found, if more than one method matches the search token, or if
    /// the fixup count differs from <paramref name="expectedCount"/>.
    /// Use a precise token (e.g. <c>".MethodName("</c>) to avoid unintended substring matches.
    /// Useful for ensuring fixups are properly deduplicated.
    /// </summary>
    public static bool HasFixupKindCountOnMethod(ReadyToRunReader reader, ReadyToRunFixupKind kind, string methodName, int expectedCount, out string diagnostic)
    {
        var matchingMethods = new List<(string Signature, int Count)>();
        foreach (var method in GetAllMethods(reader))
        {
            if (method.Fixups is null)
                continue;
            if (!method.SignatureString.Contains(methodName, StringComparison.OrdinalIgnoreCase))
                continue;

            int count = 0;
            foreach (var cell in method.Fixups)
            {
                if (cell.Signature is not null && cell.Signature.FixupKind == kind)
                    count++;
            }

            // Only consider methods that have at least one fixup of this kind so we
            // don't false-fail on co-named thunks that legitimately have none.
            if (count > 0)
                matchingMethods.Add((method.SignatureString, count));
        }

        if (matchingMethods.Count == 0)
        {
            diagnostic = $"No method matching '{methodName}' was found with any '{kind}' fixup.";
            return false;
        }

        if (matchingMethods.Count > 1)
        {
            diagnostic = $"Expected exactly one method matching '{methodName}' with '{kind}' fixup, " +
                $"but found {matchingMethods.Count}: [{string.Join(", ", matchingMethods.Select(m => m.Signature))}]";
            return false;
        }

        var (signature, fixupCount) = matchingMethods[0];
        if (fixupCount != expectedCount)
        {
            diagnostic = $"Expected exactly {expectedCount} '{kind}' fixup(s) on method '{signature}', but found {fixupCount}.";
            return false;
        }

        diagnostic = $"Found exactly {expectedCount} '{kind}' fixup(s) on method '{signature}'.";
        return true;
    }

    /// <summary>
    /// Returns true if the R2R image contains at least one fixup of the given kind.
    /// </summary>
    public static bool HasFixupKind(ReadyToRunReader reader, ReadyToRunFixupKind kind, out string diagnostic)
    {
        var presentKinds = new HashSet<ReadyToRunFixupKind>();
        foreach (var method in GetAllMethods(reader))
        {
            if (method.Fixups is null)
                continue;
            foreach (var cell in method.Fixups)
            {
                if (cell.Signature is not null)
                    presentKinds.Add(cell.Signature.FixupKind);
            }
        }

        bool found = presentKinds.Contains(kind);
        diagnostic = found
            ? $"Found fixup kind '{kind}'."
            : $"Expected fixup kind '{kind}' not found. " +
              $"Present kinds: [{string.Join(", ", presentKinds)}]";
        return found;
    }

    private static bool HasFixupKind(ReadyToRunMethod method, ReadyToRunFixupKind kind)
    {
        if (method.Fixups is null)
            return false;

        foreach (var cell in method.Fixups)
        {
            if (cell.Signature is not null && cell.Signature.FixupKind == kind)
                return true;
        }

        return false;
    }

    private static bool HasSignaturePrefix(ReadyToRunMethod method, string prefix)
        => method.SignaturePrefixes is not null && method.SignaturePrefixes.Contains(prefix);

    private static int GetEntryPointRva(ReadyToRunMethod method)
    {
        foreach (RuntimeFunction runtimeFunction in method.RuntimeFunctions)
        {
            if (runtimeFunction.Id == method.EntryPointRuntimeFunctionId)
                return runtimeFunction.StartAddress;
        }

        return -1;
    }

    private static string StripAsyncMethodPrefix(string signature)
    {
        const string AsyncPrefix = "[ASYNC] ";
        const string ResumePrefix = "[RESUME] ";
        if (signature.StartsWith(AsyncPrefix, StringComparison.Ordinal))
            return signature.Substring(AsyncPrefix.Length);

        if (signature.StartsWith(ResumePrefix, StringComparison.Ordinal))
            return signature.Substring(ResumePrefix.Length);

        return signature;
    }

    /// <summary>
    /// Returns true if exactly one method whose signature contains <paramref name="methodName"/>
    /// has at least one fixup of the given kind.
    /// Fails if no match is found or if more than one method matches the search token.
    /// Use a precise token (e.g. <c>".MethodName("</c>) to avoid unintended substring matches.
    /// </summary>
    public static bool HasFixupKindOnMethod(ReadyToRunReader reader, ReadyToRunFixupKind kind, string methodName, out string diagnostic)
    {
        var matchingMethods = new List<string>();
        var methodsWithFixup = new List<string>();
        foreach (var method in GetAllMethods(reader))
        {
            if (method.Fixups is null)
                continue;

            bool hasKind = false;
            foreach (var cell in method.Fixups)
            {
                if (cell.Signature is not null && cell.Signature.FixupKind == kind)
                {
                    hasKind = true;
                    break;
                }
            }

            if (hasKind)
            {
                methodsWithFixup.Add(method.SignatureString);
                if (method.SignatureString.Contains(methodName, StringComparison.OrdinalIgnoreCase))
                    matchingMethods.Add(method.SignatureString);
            }
        }

        if (matchingMethods.Count == 0)
        {
            diagnostic =
                $"Expected fixup kind '{kind}' on method matching '{methodName}', but not found.\n" +
                $"Methods with '{kind}' fixups: [{string.Join(", ", methodsWithFixup)}]";
            return false;
        }

        if (matchingMethods.Count > 1)
        {
            diagnostic =
                $"Expected exactly one method matching '{methodName}' with fixup kind '{kind}', " +
                $"but found {matchingMethods.Count}: [{string.Join(", ", matchingMethods)}]";
            return false;
        }

        diagnostic = $"Found '{kind}' fixup on method matching '{methodName}'.";
        return true;
    }

    /// <summary>
    /// Returns true if the R2R image contains a compiled method with a matching declaring type and method name.
    /// Optionally checks method-level generic instantiation args.
    /// </summary>
    public static bool HasCompiledMethod(ReadyToRunReader reader, string declaringType, string methodName, out string diagnostic, string[]? instanceArgs = null)
    {
        List<ReadyToRunMethod> allMethods = GetAllMethods(reader);
        List<ReadyToRunMethod> matchingMethods = allMethods
            .Where(m => m.DeclaringType == declaringType && m.Name == methodName)
            .Where(m =>
            {
                if (instanceArgs is null)
                    return m.InstanceArgs is null;
                if (m.InstanceArgs is null || m.InstanceArgs.Length != instanceArgs.Length)
                    return false;
                for (int i = 0; i < instanceArgs.Length; i++)
                {
                    if (m.InstanceArgs[i] != instanceArgs[i])
                        return false;
                }
                return true;
            })
            .ToList();

        string expected = instanceArgs is null
            ? $"'{declaringType}.{methodName}'"
            : $"'{declaringType}.{methodName}<{string.Join(",", instanceArgs)}>'";

        if (matchingMethods.Count > 0)
        {
            diagnostic = $"Found compiled method {expected}: [{string.Join(", ", matchingMethods.Select(m => m.SignatureString))}]";
            return true;
        }

        diagnostic =
            $"Expected compiled method {expected} not found.\n" +
            $"All compiled methods ({allMethods.Count}):\n  {string.Join("\n  ", allMethods.Select(m => $"{m.DeclaringType}:{m.Name}"))}";
        return false;
    }

    /// <summary>
    /// Reads the raw IL byte stream of a method definition from a component MSIL file.
    /// </summary>
    private static bool TryGetMethodIL(string msilFilePath, string declaringType, string methodName, out byte[] il, out string diagnostic)
    {
        il = Array.Empty<byte>();

        if (!File.Exists(msilFilePath))
        {
            diagnostic = $"Component MSIL file not found: '{msilFilePath}'.";
            return false;
        }

        using var fileStream = new FileStream(msilFilePath, FileMode.Open, FileAccess.Read);
        using var peReader = new PEReader(fileStream);
        MetadataReader mr = peReader.GetMetadataReader();
        foreach (TypeDefinitionHandle typeHandle in mr.TypeDefinitions)
        {
            TypeDefinition type = mr.GetTypeDefinition(typeHandle);
            if (mr.GetString(type.Name) != declaringType)
                continue;

            foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
            {
                MethodDefinition method = mr.GetMethodDefinition(methodHandle);
                if (mr.GetString(method.Name) != methodName)
                    continue;

                int rva = method.RelativeVirtualAddress;
                if (rva == 0)
                {
                    diagnostic = $"Method '{declaringType}.{methodName}' has no IL body (RVA 0).";
                    return false;
                }

                il = peReader.GetMethodBody(rva).GetILBytes() ?? Array.Empty<byte>();
                diagnostic = string.Empty;
                return true;
            }
        }

        diagnostic = $"Method '{declaringType}.{methodName}' not found in '{msilFilePath}'.";
        return false;
    }

    /// <summary>
    /// Returns true if the method's IL body was stripped by crossgen2 (--strip-il-bodies).
    /// </summary>
    public static bool MethodILIsStripped(string msilFilePath, string declaringType, string methodName, out string diagnostic)
    {
        if (!TryGetMethodIL(msilFilePath, declaringType, methodName, out byte[] il, out diagnostic))
            return false;

        bool stripped = il.AsSpan().SequenceEqual((ReadOnlySpan<byte>)[0xFE, 0x24]);
        diagnostic = stripped
            ? $"IL of '{declaringType}.{methodName}' is stripped (invalid opcode 0xFE 0x24)."
            : $"Expected IL of '{declaringType}.{methodName}' to be stripped, but it is present ({il.Length} bytes: {BitConverter.ToString(il)}).";
        return stripped;
    }

    /// <summary>
    /// Returns true if the method's full IL body is present in the component MSIL file.
    /// </summary>
    public static bool MethodILIsPresent(string msilFilePath, string declaringType, string methodName, out string diagnostic)
    {
        if (!TryGetMethodIL(msilFilePath, declaringType, methodName, out byte[] il, out diagnostic))
            return false;

        bool present = !il.AsSpan().SequenceEqual((ReadOnlySpan<byte>)[0xFE, 0x24]);
        diagnostic = present
            ? $"IL of '{declaringType}.{methodName}' is present ({il.Length} bytes)."
            : $"Expected IL of '{declaringType}.{methodName}' to be present, but it was stripped (invalid opcode 0xFE 0x24).";
        return present;
    }
}

/// <summary>
/// Simple assembly resolver that looks in the same directory as the input image.
/// </summary>
internal sealed class SimpleAssemblyResolver : IAssemblyResolver
{
    private readonly TestPaths _paths;

    public SimpleAssemblyResolver(TestPaths paths)
    {
        _paths = paths;
    }

    public IAssemblyMetadata? FindAssembly(MetadataReader metadataReader, AssemblyReferenceHandle assemblyReferenceHandle, string parentFile)
    {
        var assemblyRef = metadataReader.GetAssemblyReference(assemblyReferenceHandle);
        string name = metadataReader.GetString(assemblyRef.Name);

        return FindAssembly(name, parentFile);
    }

    public IAssemblyMetadata? FindAssembly(string simpleName, string parentFile)
    {
        string? dir = Path.GetDirectoryName(parentFile);
        if (dir is null)
            return null;

        string candidate = Path.Combine(dir, simpleName + ".dll");
        if (!File.Exists(candidate))
            candidate = Path.Combine(_paths.RuntimePackDir, simpleName + ".dll");

        if (!File.Exists(candidate))
            return null;

        return new SimpleAssemblyMetadata(candidate);
    }
}

/// <summary>
/// Simple assembly metadata wrapper that loads the PE image into memory
/// to avoid holding file handles open (IAssemblyMetadata has no disposal contract,
/// and ReadyToRunReader caches these indefinitely).
/// </summary>
internal sealed class SimpleAssemblyMetadata : IAssemblyMetadata
{
    private readonly PEReader _peReader;

    public SimpleAssemblyMetadata(string path)
    {
        byte[] imageBytes = File.ReadAllBytes(path);
        _peReader = new PEReader(new MemoryStream(imageBytes));
    }

    public void GetSectionData(int relativeVirtualAddress, Action<BlobReader> action) => action(_peReader.GetSectionData(relativeVirtualAddress).GetReader());

    public MetadataReader MetadataReader => _peReader.GetMetadataReader();
}
