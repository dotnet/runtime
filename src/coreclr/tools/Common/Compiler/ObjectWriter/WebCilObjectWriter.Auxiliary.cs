// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if READYTORUN
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.Text;

namespace ILCompiler.ObjectWriter;

internal sealed partial class WebCilObjectWriter
{
    private IReadOnlyCollection<DependencyNode> _partitionNodes;
    private int[] _physicalFunctionIndices;
    private int[] _primaryFunctionIndices;
    private int _activationSignature;
    private List<AuxiliaryDataRange> _auxiliaryDataRanges;
    private WasmImportLayout _importLayout;

    private readonly struct AuxiliaryDataRange
    {
        public readonly int Offset;
        public readonly int Length;
        public readonly bool EntryPoints;

        public AuxiliaryDataRange(int offset, int length, bool entryPoints)
        {
            Offset = offset;
            Length = length;
            EntryPoints = entryPoints;
        }
    }

    public override void EmitObject(Stream stream, IReadOnlyCollection<DependencyNode> nodes, IObjectDumper dumper, Logger logger)
    {
        if (_nodeFactory.UsesWasmAuxiliaryModules)
        {
            _importLayout = PrepareImportLayout(nodes);
            nodes = _importLayout.OrderSignatureBlobs(nodes);
        }
        _partitionNodes = nodes;
        base.EmitObject(stream, nodes, dumper, logger);
    }

    private WasmImportLayout PrepareImportLayout(IReadOnlyCollection<DependencyNode> nodes)
    {
        WasmImportLayout layout = new();
        HashSet<ISymbolNode> signatureArrays = new();
        foreach (DependencyNode dependency in nodes)
        {
            if (dependency is Import import)
            {
                layout.AddImport(import);
                signatureArrays.Add(import.Table.SignatureArray);
            }
        }
        foreach (DependencyNode dependency in nodes)
        {
            if (dependency is not ObjectNode node || node.ShouldSkipEmittingObjectNode(_nodeFactory))
                continue;
            bool cold = node is MethodWithGCInfo method && _nodeFactory.IsAuxiliaryMethod(method);
            if (node is MethodWithGCInfo body)
            {
                foreach (ISymbolNode fixup in body.Fixups)
                    layout.AddUse(fixup, cold);
            }
            // Signature-array references follow their owning import cells, not the resident container.
            // Other resident data, including portable import-thunk records, must remain usable immediately.
            if (node is ISymbolNode symbol && signatureArrays.Contains(symbol))
                continue;
            if (node.GetData(_nodeFactory, relocsOnly: true).Relocs is Relocation[] relocations)
            {
                foreach (Relocation relocation in relocations)
                    layout.AddUse(relocation.Target, cold);
            }
        }
        layout.Complete(symbol => _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, symbol));
        return layout;
    }

    private List<AuxiliaryDataRange> GetAuxiliaryDataRanges()
    {
        List<AuxiliaryDataRange> ranges = new();
        int primaryCount = 0;
        int totalCount = 0;
        MethodWithGCInfo lastMethod = null;
        foreach (MethodWithGCInfo method in _nodeFactory.EnumerateCompiledMethods())
        {
            if (!_nodeFactory.IsAuxiliaryMethod(method))
                primaryCount += method.FrameInfos.Length;
            totalCount += method.FrameInfos.Length;
            lastMethod = method;
        }

        int finalUnwindOffset = lastMethod is null ? -1 :
            lastMethod.GCInfoNode.CalculateFuncletOffsets(_nodeFactory)[lastMethod.FrameInfos.Length - 1];
        foreach (DependencyNode dependency in _partitionNodes)
        {
            if (dependency is not ObjectNode node || node is not ISymbolNode symbol ||
                !_definedSymbols.TryGetValue(GetMangledName(symbol), out SymbolDefinition definition) ||
                _sections[definition.SectionIndex] is not WebcilSection section)
                continue;

            int offset = checked((int)section.Header.VirtualAddress + (int)definition.Value);
            int fixupSize = node switch
            {
                MethodEntryPointTableNode { IsAuxiliary: true } table => table.Offset,
                InstanceEntryPointTableNode { IsAuxiliary: true } table => table.Offset,
                _ => -1,
            };
            if (fixupSize >= 0)
            {
                Add(offset - fixupSize, fixupSize, entryPoints: false);
                Add(offset, definition.Size, entryPoints: true);
            }
            else if (node is RuntimeFunctionsTableNode)
            {
                // The final function and its unwind data remain readable before any sidecar arrives.
                Add(offset + primaryCount * 8, Math.Max(totalCount - primaryCount - 1, 0) * 8, entryPoints: false);
            }
            else if (node is RuntimeFunctionsGCInfoNode gcInfo)
            {
                foreach ((int start, int length) in gcInfo.AuxiliaryDataRanges)
                {
                    if (finalUnwindOffset < start || finalUnwindOffset >= start + length)
                        Add(offset + start, length, entryPoints: false);
                }
            }
            else if (node is EHInfoNode ehInfo)
            {
                foreach ((int start, int length) in ehInfo.AuxiliaryDataRanges)
                    Add(offset + start, length, entryPoints: false);
            }
        }

        AddExclusiveImports();
        ranges.Sort(static (left, right) => left.Offset.CompareTo(right.Offset));
        List<AuxiliaryDataRange> merged = new();
        foreach (AuxiliaryDataRange range in ranges)
        {
            if (merged.Count > 0)
            {
                AuxiliaryDataRange previous = merged[merged.Count - 1];
                if (previous.Offset + previous.Length > range.Offset)
                    throw new InvalidDataException("Overlapping auxiliary WebAssembly data ownership.");
                if (previous.EntryPoints == range.EntryPoints && previous.Offset + previous.Length == range.Offset)
                {
                    merged[merged.Count - 1] = new AuxiliaryDataRange(previous.Offset,
                        checked(previous.Length + range.Length), range.EntryPoints);
                    continue;
                }
            }
            merged.Add(range);
        }
        return merged;

        void Add(int offset, int length, bool entryPoints)
        {
            if (length > 0)
                ranges.Add(new AuxiliaryDataRange(offset, length, entryPoints));
        }

        void AddExclusiveImports()
        {
            foreach (Import import in _importLayout.ColdImports)
            {
                AddSymbolRange(import, import.Table.EntrySize);
                if (_definedSymbols.TryGetValue(GetMangledName(import.Table.SignatureArray), out SymbolDefinition array))
                {
                    WebcilSection section = (WebcilSection)_sections[array.SectionIndex];
                    Add(checked((int)section.Header.VirtualAddress + (int)array.Value +
                        import.ImportSignature.OffsetFromBeginningOfArray), sizeof(uint), entryPoints: false);
                }
            }
            foreach (ISymbolNode signature in _importLayout.ColdSignatures)
                AddSymbolRange(signature, length: 0, prefixLength: ((Signature)signature).Offset);
        }

        void AddSymbolRange(ISymbolNode symbol, int length, int prefixLength = 0)
        {
            if (_definedSymbols.TryGetValue(GetMangledName(symbol), out SymbolDefinition definition))
            {
                WebcilSection section = (WebcilSection)_sections[definition.SectionIndex];
                Add(checked((int)section.Header.VirtualAddress + (int)definition.Value - prefixLength),
                    length == 0 ? checked(definition.Size + prefixLength) : length, entryPoints: false);
            }
        }
    }

    private static bool ContainsAuxiliaryData(List<AuxiliaryDataRange> ranges, int offset)
    {
        int low = 0;
        int high = ranges.Count - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            AuxiliaryDataRange range = ranges[middle];
            if (offset < range.Offset)
                high = middle - 1;
            else if (offset - range.Offset >= range.Length)
                low = middle + 1;
            else
                return true;
        }
        return false;
    }

    private void EmitPartitionedObjectFile(Stream primaryOutput, WebcilPayloadDataSegment payloadSegment, WebcilSection[] sections)
    {
        if (primaryOutput is not FileStream file)
            throw new InvalidOperationException("Partitioned WebAssembly output requires a file stream.");

        WriteMemoryImport((ulong)payloadSegment.ContentSize);
        FinalizeSectionEntryCounts();

        using MemoryStream encodedPayload = new();
        payloadSegment.EmitToStream(encodedPayload);
        byte[] payload = encodedPayload.ToArray().AsSpan(payloadSegment.HeaderSize).ToArray();
        List<AuxiliaryDataRange> ranges = _auxiliaryDataRanges;
        byte[] primaryPayload = (byte[])payload.Clone();
        foreach (AuxiliaryDataRange range in ranges)
            primaryPayload.AsSpan(range.Offset, range.Length).Clear();

        int metadataOffset = checked((int)ResolveSymbolRVA(sections,
            _definedSymbols[GetMangledName(_nodeFactory.AuxiliaryModuleMetadata)]));
        int primaryCount = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(metadataOffset + 4));
        int coldCount = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(metadataOffset + 24));
        int managedCount = checked(primaryCount + coldCount);
        int[] tableFunctions = GetTableSlotFunctionIndices();
        HashSet<int> hotFunctions = new();
        HashSet<int> coldFunctions = new();
        for (int slot = 0; slot < managedCount; slot++)
            (slot < primaryCount ? hotFunctions : coldFunctions).Add(tableFunctions[slot]);
        HashSet<int> managedHelperFunctions = new();
        foreach (MethodWithGCInfo method in _nodeFactory.EnumerateCompiledMethods())
        {
            if (!method.IsJitHelper)
                continue;

            int firstSlot = _nodeFactory.RuntimeFunctionsTable.GetIndex(method);
            for (int frame = 0; frame < method.FrameInfos.Length; frame++)
                managedHelperFunctions.Add(tableFunctions[firstSlot + frame]);
        }

        int functionImports = _wasmSymbolManager.GetImportCount(WasmIndexSpace.Function);
        List<WasmSymbol> functions = new();
        foreach (WasmSymbol symbol in _wasmSymbolManager.GetDefinitions(WasmIndexSpace.Function))
        {
            if (!symbol.IsImport)
                functions.Add(symbol);
        }

        List<int> functionTypes = new();
        Stream typeIndices = _sections.GetSection<WasmFunctionSection>(WasmObjectNodeSection.FunctionSection.Name).ContentReadStream;
        typeIndices.Position = 0;
        while (typeIndices.Position < typeIndices.Length)
            functionTypes.Add(checked((int)DwarfHelper.ReadULEB128(typeIndices, out _).Value));

        WriteModule(primaryOutput, physicalModule: 0);
        string stem = Path.Combine(Path.GetDirectoryName(file.Name), Path.GetFileNameWithoutExtension(file.Name));
        using (FileStream entryPointsOutput = File.Create(stem + ".entrypoints.wasm"))
            WriteModule(entryPointsOutput, physicalModule: 1);
        using (FileStream coldOutput = File.Create(stem + ".1.wasm"))
            WriteModule(coldOutput, physicalModule: 2);
        using (FileStream manifestFile = File.Create(stem + ".auxiliary.json"))
        using (Utf8JsonWriter manifest = new(manifestFile))
        {
            manifest.WriteStartObject();
            manifest.WriteString("entrypoints", Path.GetFileName(stem) + ".entrypoints.wasm");
            manifest.WriteStartArray("modules");
            manifest.WriteStringValue(Path.GetFileName(stem) + ".1.wasm");
            manifest.WriteEndArray();
            manifest.WriteEndObject();
        }
        _physicalFunctionIndices = null;

        void WriteModule(Stream output, int physicalModule)
        {
            List<int> kept = new();
            _physicalFunctionIndices = new int[functionImports + functions.Count];
            Array.Fill(_physicalFunctionIndices, -1);
            for (int index = 0; index < functionImports; index++)
                _physicalFunctionIndices[index] = index;
            if (physicalModule != 1)
            {
                for (int ordinal = 0; ordinal < functions.Count; ordinal++)
                {
                    int index = functions[ordinal].Index;
                    if (physicalModule != 0 && _wasmStubNames.Contains(functions[ordinal].Name))
                        continue;
                    bool managed = hotFunctions.Contains(index) || coldFunctions.Contains(index);
                    // Helper copies share the primary's logical slots, frame identities and metadata.
                    if (!managed || managedHelperFunctions.Contains(index) ||
                        (physicalModule == 0 ? hotFunctions : coldFunctions).Contains(index))
                    {
                        _physicalFunctionIndices[index] = functionImports + kept.Count;
                        kept.Add(ordinal);
                    }
                }
                if (physicalModule == 0)
                    _primaryFunctionIndices = _physicalFunctionIndices;
            }

            int codeSectionIndex = _sections.GetSectionIndex(ObjectNodeSection.WasmCodeSection.Name);
            Stream originalCode = _sections[codeSectionIndex].ContentReadStream;
            originalCode.Position = 0;
            using MemoryStream resolvedCode = new();
            if (physicalModule != 1)
            {
                ValidateDirectCalls(originalCode, codeSectionIndex);
                originalCode.Position = 0;
                if (_resolvableRelocations.TryGetValue(codeSectionIndex, out List<SymbolicRelocation> relocations))
                    ResolveRelocations(codeSectionIndex, originalCode, resolvedCode, relocations, shrink: true);
                else
                    originalCode.CopyTo(resolvedCode);
            }
            resolvedCode.Position = 0;
            List<CodeBlob> codeBlobs = physicalModule == 1 ? new() : ParseCodeBlobs(resolvedCode);
            List<AuxiliaryDataRange> moduleRanges = new();
            foreach (AuxiliaryDataRange range in ranges)
            {
                if (physicalModule != 0 && range.EntryPoints == (physicalModule == 1))
                    moduleRanges.Add(range);
            }

            EmitWasmHeader(output);
            _sections.GetSection<WasmExternallyCountedSection>(ObjectNodeSection.WasmTypeSection.Name).EmitToStream(output);
            _sections.GetSection<WasmImportSection>(WasmObjectNodeSection.ImportSection.Name).EmitToStream(output);
            WriteRawSection(output, WasmSectionType.Function, section =>
            {
                WriteUnsigned(section, kept.Count + (physicalModule == 0 ? 0 : 1));
                foreach (int ordinal in kept)
                    WriteUnsigned(section, functionTypes[ordinal]);
                if (physicalModule != 0)
                    WriteUnsigned(section, _activationSignature);
            });
            if (physicalModule == 0)
                _sections.GetSection<WasmGlobalSection>(WasmObjectNodeSection.GlobalSection.Name).EmitToStream(output);
            WriteRawSection(output, WasmSectionType.Export, section =>
            {
                WriteUnsigned(section, physicalModule == 0 ? _wasmStubNames.Count + 2 : 1);
                if (physicalModule == 0)
                {
                    WriteExport(section, "table", 1, 0);
                    WriteExport(section, "webcilVersion", 3, _definedGlobals["webcilVersion"].Index);
                    foreach (Utf8String name in _wasmStubNames)
                        WriteExport(section, name.ToString(), 0, _physicalFunctionIndices[_wasmSymbolManager.GetSymbol(name).Index]);
                }
                else
                    WriteExport(section, "ActivateModule", 0, functionImports + kept.Count);
            });
            if (physicalModule != 1)
            {
                WriteRawSection(output, WasmSectionType.Element, section =>
                {
                    if (physicalModule == 0)
                    {
                        WriteUnsigned(section, 2);
                        WriteElements(section, 0, primaryCount, passive: false);
                        WriteElements(section, managedCount, tableFunctions.Length - managedCount, passive: false);
                    }
                    else
                    {
                        WriteUnsigned(section, 1);
                        WriteElements(section, primaryCount, coldCount, passive: true);
                    }
                });
            }
            WriteRawSection(output, WasmSectionType.DataCount, section =>
                WriteUnsigned(section, physicalModule == 0 ? 2 : moduleRanges.Count));
            WriteRawSection(output, WasmSectionType.Code, section =>
            {
                WriteUnsigned(section, kept.Count + (physicalModule == 0 ? 0 : 1));
                foreach (int ordinal in kept)
                {
                    if (functions[ordinal].Name.ToString() == "getAuxiliaryModuleMetadata")
                    {
                        WriteBody(section, new WasmFunctionBody(new WasmFuncType(new([]), new([WasmValueType.I32])),
                            [Global.Get(ImageBaseGlobalIndex), I32.Const(metadataOffset), I32.Add]));
                    }
                    else
                    {
                        CodeBlob blob = codeBlobs[ordinal];
                        WriteUnsigned(section, checked((int)blob.Size));
                        section.Write(resolvedCode.GetBuffer().AsSpan(checked((int)blob.Start), checked((int)blob.Size)));
                    }
                }
                if (physicalModule != 0)
                    WriteBody(section, CreateActivationBody(moduleRanges, metadataOffset,
                        physicalModule == 1, primaryCount, coldCount));
            });
            WriteRawSection(output, WasmSectionType.Data, section =>
            {
                if (physicalModule == 0)
                {
                    WriteUnsigned(section, 2);
                    section.WriteByte(1);
                    WriteUnsigned(section, 8);
                    Span<byte> size = stackalloc byte[8];
                    BinaryPrimitives.WriteInt32LittleEndian(size, primaryPayload.Length);
                    BinaryPrimitives.WriteInt32LittleEndian(size.Slice(4), MethodCount);
                    section.Write(size);
                    section.WriteByte(0);
                    section.WriteByte(0x23);
                    WriteUnsigned(section, ImageBaseGlobalIndex);
                    section.WriteByte(0x0B);
                    WriteUnsigned(section, primaryPayload.Length);
                    section.Write(primaryPayload);
                }
                else
                {
                    WriteUnsigned(section, moduleRanges.Count);
                    foreach (AuxiliaryDataRange range in moduleRanges)
                    {
                        section.WriteByte(1);
                        WriteUnsigned(section, range.Length);
                        section.Write(payload.AsSpan(range.Offset, range.Length));
                    }
                }
            }, alignedSuffixLength: physicalModule == 0 ? primaryPayload.Length : 0);
            if (_emitNameSection)
            {
                List<WasmSymbol> names = new();
                foreach (int ordinal in kept)
                {
                    WasmSymbol symbol = functions[ordinal];
                    names.Add(symbol with { Index = _physicalFunctionIndices[symbol.Index] });
                }
                if (physicalModule != 0)
                    names.Add(new WasmSymbol(new Utf8String("ActivateModule"), WasmIndexSpace.Function,
                        functionImports + kept.Count, IsImport: false, HasExplicitName: false));
                new WasmNameSection(names).EmitToStream(output);
            }

            void WriteElements(Stream section, int start, int count, bool passive)
            {
                section.WriteByte(passive ? (byte)1 : (byte)0);
                if (passive)
                    section.WriteByte(0);
                else
                {
                    section.WriteByte(0x23);
                    WriteUnsigned(section, TableBaseGlobalIndex);
                    section.WriteByte(0x41);
                    WriteSigned(section, start);
                    section.WriteByte(0x6A);
                    section.WriteByte(0x0B);
                }
                WriteUnsigned(section, count);
                for (int slot = start; slot < start + count; slot++)
                {
                    int index = _physicalFunctionIndices[tableFunctions[slot]];
                    if (index < 0)
                        throw new InvalidDataException("Auxiliary element references an unavailable WebAssembly function.");
                    WriteUnsigned(section, index);
                }
            }
        }
    }

    private void ValidateDirectCalls(Stream code, int sectionIndex)
    {
        if (!_resolvableRelocations.TryGetValue(sectionIndex, out List<SymbolicRelocation> relocations))
            return;
        code.Position = 0;
        List<CodeBlob> blobs = ParseCodeBlobs(code);
        int ordinal = 0;
        int imports = _wasmSymbolManager.GetImportCount(WasmIndexSpace.Function);
        foreach (SymbolicRelocation relocation in relocations)
        {
            while (ordinal < blobs.Count && relocation.Offset >= blobs[ordinal].End)
                ordinal++;
            if (ordinal >= blobs.Count || _physicalFunctionIndices[imports + ordinal] < 0 ||
                relocation.Type != RelocType.WASM_FUNCTION_INDEX_LEB)
                continue;
            WasmSymbol target = _wasmSymbolManager.GetSymbol(relocation.SymbolName);
            if (_physicalFunctionIndices[target.Index] < 0)
                throw new InvalidDataException($"Direct call across auxiliary WebAssembly partitions: {target.Name}.");
        }
    }

    private WasmFunctionBody CreateActivationBody(List<AuxiliaryDataRange> ranges, int metadataOffset,
        bool entryPoints, int tableStart, int tableCount)
    {
        int status = metadataOffset + (entryPoints
            ? AuxiliaryModuleMetadataNode.EntryPointsStatusOffset : AuxiliaryModuleMetadataNode.ModuleStatusOffset);
        List<WasmExpr> instructions = new()
        {
            Global.Get(ImageBaseGlobalIndex), I32.Load((ulong)status), I32.Eqz, Block.If(WasmBlockType.Empty),
        };
        for (int index = 0; index < ranges.Count; index++)
        {
            AuxiliaryDataRange range = ranges[index];
            instructions.Add(Global.Get(ImageBaseGlobalIndex));
            instructions.Add(I32.Const(range.Offset));
            instructions.Add(I32.Add);
            instructions.Add(I32.Const(0));
            instructions.Add(I32.Const(range.Length));
            instructions.Add(Memory.Init(index));
        }
        foreach (PendingBaseReloc relocation in _pendingBaseRelocs)
        {
            WebcilSection section = (WebcilSection)_sections[relocation.SectionIndex];
            int offset = checked((int)section.Header.VirtualAddress + (int)relocation.Offset);
            if (!ContainsAuxiliaryData(ranges, offset))
                continue;
            int baseGlobal = relocation.FileRelocType switch
            {
                RelocType.IMAGE_REL_BASED_HIGHLOW => ImageBaseGlobalIndex,
                RelocType.IMAGE_REL_BASED_WASM32_TABLE => TableBaseGlobalIndex,
                _ => throw new InvalidDataException($"Unsupported auxiliary base relocation: {relocation.FileRelocType}."),
            };
            instructions.Add(Global.Get(ImageBaseGlobalIndex));
            instructions.Add(Global.Get(ImageBaseGlobalIndex));
            instructions.Add(I32.Load((ulong)offset));
            instructions.Add(Global.Get(baseGlobal));
            instructions.Add(I32.Add);
            instructions.Add(I32.Store((ulong)offset));
        }
        if (!entryPoints)
        {
            instructions.Add(Global.Get(TableBaseGlobalIndex));
            instructions.Add(I32.Const(tableStart));
            instructions.Add(I32.Add);
            instructions.Add(I32.Const(0));
            instructions.Add(I32.Const(tableCount));
            instructions.Add(Table.Init(0));
        }
        instructions.Add(Global.Get(ImageBaseGlobalIndex));
        instructions.Add(I32.Const(1));
        instructions.Add(I32.Store((ulong)status));
        instructions.Add(Block.End);
        return new WasmFunctionBody(new WasmFuncType(new([]), new([])), instructions.ToArray());
    }

    private static void WriteBody(Stream stream, WasmFunctionBody body)
    {
        byte[] bytes = new byte[body.EncodeSize()];
        body.Encode(bytes);
        stream.Write(bytes);
    }

    private static void WriteExport(Stream stream, string name, byte kind, int index)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(name);
        WriteUnsigned(stream, bytes.Length);
        stream.Write(bytes);
        stream.WriteByte(kind);
        WriteUnsigned(stream, index);
    }

    private static void WriteRawSection(Stream output, WasmSectionType type, Action<MemoryStream> emit,
        int alignedSuffixLength = 0)
    {
        using MemoryStream content = new();
        emit(content);
        if (alignedSuffixLength != 0)
        {
            long alignedOffset = output.Position + 1 + DwarfHelper.SizeOfULEB128((ulong)content.Length) +
                content.Length - alignedSuffixLength;
            new PaddingWasmSection(WebcilSectionAlignment, alignedOffset).EmitToStream(output);
        }
        output.WriteByte((byte)type);
        WriteUnsigned(output, checked((int)content.Length));
        content.Position = 0;
        content.CopyTo(output);
    }

    private static void WriteUnsigned(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[10];
        int length = DwarfHelper.WriteULEB128(bytes, checked((ulong)value));
        stream.Write(bytes.Slice(0, length));
    }

    private static void WriteSigned(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[10];
        int length = DwarfHelper.WriteSLEB128(bytes, value);
        stream.Write(bytes.Slice(0, length));
    }
}
#endif
