// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ILCompiler.DependencyAnalysis;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem.TypesDebugInfo;
using ILCompiler.DependencyAnalysis.Wasm;
using System.Linq;

namespace ILCompiler.ObjectWriter
{
    internal sealed partial class WasmRelocatableObjectWriter : WasmObjectWriter
    {
        private WasmDataSection _dataSection;
        private DwarfBuilder _dwarfBuilder;

        // https://github.com/WebAssembly/tool-conventions/blob/main/Linking.md
        private enum SymbolKind : byte
        {
            Function = 0, // SYMTAB_FUNCTION
            Data = 1, // SYMTAB_DATA
            Global = 2, // SYMTAB_GLOBAL
            Section = 3, // SYMTAB_SECTION
            Tag = 4, // SYMTAB_EVENT
            Table = 5, // SYMTAB_TABLE
        }

        private const uint UndefinedSymbol = 0x10; // WASM_SYM_UNDEFINED
        private const uint LocalSymbol = 0x2; // WASM_SYM_BINDING_LOCAL
        private const uint ExportedSymbol = 0x20; // WASM_SYM_EXPORTED
        private const byte SegmentInfoSubsection = 5; // WASM_SEGMENT_INFO
        private const byte SymbolTableSubsection = 8; // WASM_SYMBOL_TABLE
        private const uint LinkingVersion = 2;

        private enum WasmRelocationType : byte
        {
            FunctionIndexLeb = 0, // R_WASM_FUNCTION_INDEX_LEB
            TableIndexSleb = 1, // R_WASM_TABLE_INDEX_SLEB
            TableIndexI32 = 2, // R_WASM_TABLE_INDEX_I32
            MemoryAddrLeb = 3, // R_WASM_MEMORY_ADDR_LEB
            MemoryAddrSleb = 4, // R_WASM_MEMORY_ADDR_SLEB
            MemoryAddrI32 = 5, // R_WASM_MEMORY_ADDR_I32
            TypeIndexLeb = 6, // R_WASM_TYPE_INDEX_LEB
            GlobalIndexLeb = 7, // R_WASM_GLOBAL_INDEX_LEB
            FunctionOffsetI32 = 8, // R_WASM_FUNCTION_OFFSET_I32
            SectionOffsetI32 = 9, // R_WASM_SECTION_OFFSET_I32
            MemoryAddrRelSleb = 11, // R_WASM_MEMORY_ADDR_REL_SLEB
            MemoryAddrI64 = 16, // R_WASM_MEMORY_ADDR_I64
            TableIndexI64 = 19, // R_WASM_TABLE_INDEX_I64
            MemoryAddrLocrelI32 = 23, // R_WASM_MEMORY_ADDR_LOCREL_I32
        }

        private readonly Dictionary<Utf8String, int> _linkingSymbolIndices = new();
        private readonly Dictionary<int, List<SymbolicRelocation>> _relocations = new();
        private readonly Dictionary<int, long> _functionOffsets = new();

        public WasmRelocatableObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder = null) : base(factory, options, outputInfoBuilder)
        {
        }

        private protected override ObjectNodeSection GetEmitSection(ObjectNodeSection section)
        {
            if (section == ObjectNodeSection.TextSection ||
                section == ObjectNodeSection.ManagedCodeUnixContentSection ||
                section == ObjectNodeSection.ManagedCodeWindowsContentSection)
            {
                return ObjectNodeSection.WasmCodeSection;
            }

            return section;
        }

        private protected override void EmitSectionsAndLayout()
        {
            List<IWasmDataSegment> dataSegments = new();
            foreach (SectionDataEmitter section in _sections.Sections)
            {
                if (section is WasmDataSegmentEmitter dataSegment)
                {
                    dataSegments.Add(dataSegment);
                }
            }

            if (dataSegments.Count == 0)
            {
                return;
            }

            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.DataCountSection);
            writer.WriteULEB128((ulong)dataSegments.Count);
            _dataSection = new WasmDataSection(dataSegments, new Utf8String("data"));
        }

        private protected override void EmitObjectFile(Stream outputFileStream)
        {
            Debug.Assert(outputFileStream.CanSeek, $"EmitObjectFile requires seekable output stream");

            FinalizeSectionEntryCounts();
            _dataSection?.AssignSegmentLayout();

            List<IWasmSection> sections = new();
            foreach (int index in SectionEmitOrder)
            {
                if (_sections[index] is IWasmSection section)
                {
                    sections.Add(section);
                }
            }

            int dataSectionIndex = sections.Count;
            if (_dataSection is not null)
            {
                sections.Add(_dataSection);
            }

            foreach (SectionDataEmitter section in _sections.Sections)
            {
                if (section is WasmCustomSection customSection)
                {
                    sections.Add(customSection);
                }
            }

            Dictionary<int, int> fileSectionIndices = new();
            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i] is SectionDataEmitter section)
                {
                    fileSectionIndices.Add(section.SectionIndex, i);
                }
            }

            using MemoryStream linking = CreateLinkingSection(fileSectionIndices);
            sections.Add(new WasmCustomSection(linking, new Utf8String("linking"), sections.Count));
            Dictionary<int, (string Name, MemoryStream Entries, int Count)> relocationSections = new();
            try
            {
                foreach ((int sectionIndex, List<SymbolicRelocation> relocations) in _relocations)
                {
                    SectionDataEmitter section = _sections[sectionIndex];
                    (int contentOffset, int targetSectionIndex, string name) = section switch
                    {
                        WasmCustomSection customSection => (
                            0,
                            fileSectionIndices[sectionIndex],
                            $"reloc.{customSection.SectionName}"),
                        WasmSection wasmSection => (
                            wasmSection.ContentPrefixSize,
                            fileSectionIndices[sectionIndex],
                            "reloc.CODE"),
                        WasmDataSegmentEmitter segment => (
                            _dataSection.GetSegmentContentOffset(segment),
                            dataSectionIndex,
                            "reloc.DATA"),
                        _ => throw new UnreachableException(),
                    };
                    if (!relocationSections.TryGetValue(targetSectionIndex, out var relocationSection))
                    {
                        relocationSection = (name, new MemoryStream(), 0);
                        relocationSections.Add(targetSectionIndex, relocationSection);
                    }
                    int relocationCount = WriteRelocationEntries(section, contentOffset, relocations, relocationSection.Entries);
                    relocationSections[targetSectionIndex] = (relocationSection.Name, relocationSection.Entries, relocationSection.Count + relocationCount);
                }

                foreach ((int targetSectionIndex, var relocationSection) in relocationSections)
                {
                    if (relocationSection.Count == 0)
                    {
                        continue;
                    }

                    sections.Add(new WasmRelocationSection(
                        relocationSection.Entries,
                        new Utf8String(relocationSection.Name),
                        sections.Count,
                        targetSectionIndex,
                        relocationSection.Count));
                }

                EmitWasmHeader(outputFileStream);
                foreach (IWasmSection section in sections)
                {
                    section.EmitToStream(outputFileStream);
                }
            }
            finally
            {
                foreach (var relocationSection in relocationSections.Values)
                {
                    relocationSection.Entries.Dispose();
                }
            }
        }

        private MemoryStream CreateLinkingSection(Dictionary<int, int> fileSectionIndices)
        {
            MemoryStream linking = new();
            WriteULEB128(linking, LinkingVersion); // version: the version of linking metadata contained in this section.
            using MemoryStream symbols = new();
            using MemoryStream entries = new();

            foreach (WasmIndexSpace indexSpace in new[] { WasmIndexSpace.Function, WasmIndexSpace.Global, WasmIndexSpace.Table, WasmIndexSpace.Tag })
            {
                foreach (WasmSymbol symbol in _wasmSymbolManager.GetDefinitions(indexSpace))
                {
                    WriteIndexedSymbol(entries, symbol);
                }
            }

            foreach ((Utf8String name, SymbolDefinition definition) in _definedSymbols)
            {
                if (_wasmSymbolManager.TryGetSymbol(name, out WasmSymbol symbol) && symbol.IndexSpace == WasmIndexSpace.Function)
                {
                    if (!_linkingSymbolIndices.ContainsKey(name))
                    {
                        WriteIndexedSymbol(entries, symbol);
                    }
                    continue;
                }

                if (_sections[definition.SectionIndex] is not WasmDataSegmentEmitter segment)
                {
                    continue;
                }

                AddSymbolIndex(name);
                entries.WriteByte((byte)SymbolKind.Data); // kind: the symbol type (SYMTAB_DATA).
                WriteULEB128(entries, 0); // flags: a bitfield containing flags for this symbol.
                WriteName(entries, name); // name_len/name_data: the UTF-8 symbol name.
                WriteULEB128(entries, (ulong)_dataSection.GetSegmentIndex(segment)); // index: the index of the data segment.
                WriteULEB128(entries, checked((ulong)definition.Value)); // offset: the offset within the segment.
                WriteULEB128(entries, (ulong)definition.Size); // size: the size of the symbol.
            }

            foreach (Utf8String name in GetUndefinedSymbols())
            {
                if (_linkingSymbolIndices.ContainsKey(name))
                {
                    continue;
                }

                AddSymbolIndex(name);
                entries.WriteByte((byte)SymbolKind.Data); // kind: the symbol type (SYMTAB_DATA).
                WriteULEB128(entries, UndefinedSymbol); // flags: a bitfield containing flags for this symbol.
                WriteName(entries, name); // name_len/name_data: the UTF-8 symbol name.
            }

            foreach (SectionDataEmitter section in _sections.Sections)
            {
                if (section is WasmCustomSection)
                {
                    AddSymbolIndex(section.SectionName);
                    entries.WriteByte((byte)SymbolKind.Section);
                    WriteULEB128(entries, LocalSymbol);
                    WriteULEB128(entries, (ulong)fileSectionIndices[section.SectionIndex]);
                }
            }

            WriteULEB128(symbols, (ulong)_linkingSymbolIndices.Count); // count: number of syminfo entries in infos.
            entries.Position = 0;
            entries.CopyTo(symbols); // infos: sequence of syminfo entries.
            WriteSubsection(linking, SymbolTableSubsection, symbols);

            if (_dataSection is not null)
            {
                using MemoryStream segments = new();
                WriteULEB128(segments, (ulong)_dataSection.SegmentCount); // count: number of segment entries in segments.
                foreach (WasmDataSegmentEmitter segment in _dataSection.Segments)
                {
                    WriteName(segments, segment.SectionName); // name_len/name_data: the UTF-8 segment name.
                    WriteULEB128(segments, (ulong)BitOperations.Log2((uint)segment.MemoryAlignment)); // alignment: required segment alignment, encoded as a power of 2.
                    WriteULEB128(segments, 0); // flags: a bitfield containing flags for this segment.
                }
                WriteSubsection(linking, SegmentInfoSubsection, segments);
            }

            return linking;
        }

        private void AddSymbolIndex(Utf8String name) =>
            _linkingSymbolIndices.Add(name, _linkingSymbolIndices.Count);

        private void WriteIndexedSymbol(Stream entries, WasmSymbol symbol)
        {
            AddSymbolIndex(symbol.Name);
            SymbolKind kind = symbol.IndexSpace switch
            {
                WasmIndexSpace.Function => SymbolKind.Function,
                WasmIndexSpace.Global => SymbolKind.Global,
                WasmIndexSpace.Table => SymbolKind.Table,
                WasmIndexSpace.Tag => SymbolKind.Tag,
                _ => throw new UnreachableException(),
            };
            uint flags = symbol.IsImport ? UndefinedSymbol : 0;
            if (!symbol.IsImport && _definedSymbols.TryGetValue(symbol.Name, out SymbolDefinition definition) && definition.Global)
            {
                flags |= ExportedSymbol;
            }
            entries.WriteByte((byte)kind); // kind: the symbol type.
            WriteULEB128(entries, flags); // flags: a bitfield containing flags for this symbol.
            WriteULEB128(entries, (ulong)symbol.Index); // index: the index of the Wasm object corresponding to the symbol.
            if (!symbol.IsImport)
            {
                WriteName(entries, symbol.Name); // name_len/name_data: the optional UTF-8 symbol name, omitted for imports.
            }
        }

        private static void WriteULEB128(Stream stream, ulong value)
        {
            Span<byte> buffer = stackalloc byte[10];
            int size = DwarfHelper.WriteULEB128(buffer, value);
            stream.Write(buffer.Slice(0, size));
        }

        private static void WriteSLEB128(Stream stream, long value)
        {
            Span<byte> buffer = stackalloc byte[10];
            int size = DwarfHelper.WriteSLEB128(buffer, value);
            stream.Write(buffer.Slice(0, size));
        }

        private static void WriteName(Stream stream, Utf8String name)
        {
            WriteULEB128(stream, (ulong)name.Length); // name_len: the length of name_data in bytes.
            stream.Write(name.AsSpan()); // name_data: UTF-8 encoding of the name.
        }

        private static void WriteSubsection(Stream stream, byte kind, MemoryStream payload)
        {
            stream.WriteByte(kind); // type: code identifying the type of subsection.
            WriteULEB128(stream, (ulong)payload.Length); // payload_len: size of this subsection in bytes.
            payload.Position = 0;
            payload.CopyTo(stream); // payload_data: content of this subsection, of length payload_len.
        }

        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            if (relocationList.Count > 0)
            {
                _relocations.Add(sectionIndex, relocationList);
            }
        }

        private unsafe int WriteRelocationEntries(SectionDataEmitter section, int contentOffset, List<SymbolicRelocation> relocs, MemoryStream payload)
        {
            using Stream originalStream = section.ContentReadStream;
            MemoryStream sectionStream = new((int)originalStream.Length);
            originalStream.Position = 0;
            originalStream.CopyTo(sectionStream);
            byte[] relocScratchBuffer = new byte[Relocation.MaxSize];
            int relocationCount = 0;
            WasmSection codeSection = _sections.GetSection<WasmSection>(ObjectNodeSection.WasmCodeSection.Name);
            int codeSectionPrefixSize = codeSection.ContentPrefixSize;

            foreach (SymbolicRelocation reloc in relocs)
            {
                int size = Relocation.GetSize(reloc.Type);

                sectionStream.Position = reloc.Offset;
                sectionStream.ReadExactly(relocScratchBuffer.AsSpan(0, size));
                fixed (byte* pData = relocScratchBuffer)
                {
                    long addend = reloc.Addend + Relocation.ReadValue(reloc.Type, pData);
                    if (reloc.Type is RelocType.WASM_METHOD_RELATIVE_VIRTUAL_IP_I32 or RelocType.WASM_METHOD_FRAME_IDENTITY_SLEB)
                    {
                        Relocation.WriteValue(reloc.Type, pData, addend);
                        sectionStream.Position = reloc.Offset;
                        sectionStream.Write(relocScratchBuffer.AsSpan(0, size));
                        continue;
                    }

                    bool isFunction = _wasmSymbolManager.TryGetSymbol(reloc.SymbolName, out WasmSymbol symbol) &&
                        symbol.IndexSpace == WasmIndexSpace.Function;
                    bool isSection = _definedSymbols.TryGetValue(reloc.SymbolName, out SymbolDefinition definition) &&
                        _sections[definition.SectionIndex] is WasmCustomSection;
                    WasmRelocationType type = reloc.Type switch
                    {
                        RelocType.WASM_FUNCTION_INDEX_LEB => WasmRelocationType.FunctionIndexLeb,
                        RelocType.WASM_TYPE_INDEX_LEB => WasmRelocationType.TypeIndexLeb,
                        RelocType.WASM_GLOBAL_INDEX_LEB => WasmRelocationType.GlobalIndexLeb,
                        RelocType.WASM_TABLE_INDEX_SLEB => WasmRelocationType.TableIndexSleb,
                        RelocType.WASM_TABLE_INDEX_I32 => WasmRelocationType.TableIndexI32,
                        RelocType.WASM_TABLE_INDEX_I64 => WasmRelocationType.TableIndexI64,
                        RelocType.WASM_MEMORY_ADDR_LEB => WasmRelocationType.MemoryAddrLeb,
                        RelocType.WASM_MEMORY_ADDR_SLEB => WasmRelocationType.MemoryAddrSleb,
                        RelocType.WASM_MEMORY_ADDR_REL_SLEB when isFunction => WasmRelocationType.TableIndexSleb,
                        RelocType.WASM_MEMORY_ADDR_REL_SLEB => WasmRelocationType.MemoryAddrRelSleb,
                        RelocType.IMAGE_REL_BASED_HIGHLOW when isFunction && section is WasmCustomSection => WasmRelocationType.FunctionOffsetI32,
                        RelocType.IMAGE_REL_BASED_HIGHLOW when isSection => WasmRelocationType.SectionOffsetI32,
                        RelocType.IMAGE_REL_BASED_HIGHLOW when isFunction => WasmRelocationType.TableIndexI32,
                        RelocType.IMAGE_REL_BASED_HIGHLOW => WasmRelocationType.MemoryAddrI32,
                        RelocType.IMAGE_REL_BASED_DIR64 => WasmRelocationType.MemoryAddrI64,
                        RelocType.IMAGE_REL_BASED_RELPTR32 => WasmRelocationType.MemoryAddrLocrelI32,
                        _ => throw new NotSupportedException($"Relocation type {reloc.Type} for symbol '{reloc.SymbolName}' in section {section.SectionName} not yet implemented"),
                    };
                    Utf8String symbolName = reloc.SymbolName;
                    if (isFunction && type != WasmRelocationType.FunctionOffsetI32 && addend != 0)
                    {
                        // The JIT encodes funclet references as parent + ordinal, but the linker can reorder table entries.
                        Utf8String funcletName = GetFuncletName(symbolName, checked((int)addend - 1));
                        symbol = _wasmSymbolManager.GetSymbol(funcletName);
                        Debug.Assert(symbol.IndexSpace == WasmIndexSpace.Function);
                        symbolName = funcletName;
                        addend = 0;
                    }
                    bool hasAddend = type is WasmRelocationType.MemoryAddrLeb or WasmRelocationType.MemoryAddrSleb or
                        WasmRelocationType.MemoryAddrI32 or WasmRelocationType.MemoryAddrI64 or
                        WasmRelocationType.FunctionOffsetI32 or WasmRelocationType.SectionOffsetI32 or
                        WasmRelocationType.MemoryAddrRelSleb or WasmRelocationType.MemoryAddrLocrelI32;
                    if (!hasAddend && addend != 0)
                    {
                        throw new NotSupportedException($"Nonzero addend for {reloc.Type} relocation to '{reloc.SymbolName}'");
                    }

                    relocationCount++;
                    payload.WriteByte((byte)type); // type: the relocation type.
                    WriteULEB128(payload, checked((ulong)(contentOffset + reloc.Offset))); // offset: offset of the value to rewrite, relative to the section's contents.
                    // index: the symbol index, or the type index for R_WASM_TYPE_INDEX_LEB.
                    WriteULEB128(payload, (ulong)(type == WasmRelocationType.TypeIndexLeb
                        ? symbol.Index : _linkingSymbolIndices[symbolName]));
                    if (hasAddend)
                    {
                        WriteSLEB128(payload, addend); // addend: addend to add to the address.
                    }

                    long value = type switch
                    {
                        WasmRelocationType.FunctionIndexLeb or WasmRelocationType.TypeIndexLeb or WasmRelocationType.GlobalIndexLeb => symbol.Index,
                        WasmRelocationType.FunctionOffsetI32 => codeSectionPrefixSize + _functionOffsets[symbol.Index] + addend,
                        WasmRelocationType.SectionOffsetI32 => addend,
                        _ => 0,
                    };
                    Relocation.WriteValue(reloc.Type, pData, value);
                    sectionStream.Position = reloc.Offset;
                    sectionStream.Write(relocScratchBuffer.AsSpan(0, size));
                }
            }

            section.ContentReadStream = sectionStream;
            return relocationCount;
        }

        // COMDAT groups are not supported yet.
        private protected override bool UsesSubsectionsViaSymbols => true;

        private protected override SectionDataEmitter CreateDataSection(
            ObjectNodeSection section,
            int sectionIndex,
            Stream sectionStream)
        {
            return new WasmDataSegmentEmitter(
                sectionStream,
                new Utf8String(section.Name),
                sectionIndex);
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
            if (_sections[sectionIndex] is WasmDataSegmentEmitter dataSegment)
            {
                dataSegment.UpdateAlignment(alignment);
            }
        }
        private protected override void WriteGlobalSection()
        {
        }

        private protected override void WriteImports()
        {
            WriteImport(new WasmImport("env", WasmWellKnownGlobalSymbolNode.StackPointerName, import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Mut)));
            WriteImport(new WasmImport("env", "__indirect_function_table", new WasmTableImportType()));
            WriteImport(new WasmImport("env", "memory", new WasmMemoryImportType(WasmLimitType.HasMin, 0)));
        }

        private protected override void WriteExports()
        {
            WasmSection codeSection = GetOrCreateSection<WasmSection>(ObjectNodeSection.WasmCodeSection, out _);
            int codeSectionIndex = codeSection.SectionIndex;
            foreach (var symbol in _definedSymbols)
            {
                // We only export methods for now
                if (!symbol.Value.Global || symbol.Value.SectionIndex != codeSectionIndex)
                    continue;

                WasmSymbol methodEntry = _wasmSymbolManager.GetSymbol(symbol.Key);
                Debug.Assert(methodEntry.IndexSpace == WasmIndexSpace.Function);
                WriteFunctionExport(symbol.Key.ToString(), methodEntry.Index);
            }
        }

        private protected override void WriteElements()
        {
        }

        private protected override ITypesDebugInfoWriter CreateDebugInfoBuilder()
        {
            return _dwarfBuilder = new DwarfBuilder(
                _nodeFactory.NameMangler,
                _nodeFactory.Target,
                _options.HasFlag(ObjectWritingOptions.UseDwarf5));
        }

        private protected override void EmitDebugFunctionInfo(
            uint methodTypeIndex,
            Utf8String methodDisplayName,
            Utf8String methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode)
        {
            EmitDebugMethodInfo(methodTypeIndex, methodName, methodSymbol, debugNode);
        }

        private protected override void EmitDebugThunkInfo(
            Utf8String methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode)
        {
            EmitDebugMethodInfo(0, methodName, methodSymbol, debugNode);
        }

        private void EmitDebugMethodInfo(
            uint methodTypeIndex,
            Utf8String methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode)
        {
            // RyuJIT includes a five-byte padded body-size field in each function's frame extent.
            const int FunctionBodySizePrefixSize = 5;
            FrameInfo[] frameInfos = ((INodeWithCodeInfo)debugNode).FrameInfos;
            List<NativeSequencePoint> sequencePoints = new(debugNode.GetNativeSequencePoints());
            int sequencePointIndex = 0;
            for (int functionOrdinal = 0; functionOrdinal < frameInfos.Length; functionOrdinal++)
            {
                FrameInfo frameInfo = frameInfos[functionOrdinal];
                int bodyStart = checked(frameInfo.StartOffset + FunctionBodySizePrefixSize);
                int bodyEnd = frameInfo.EndOffset;
                int bodySize = bodyEnd - bodyStart;
                Debug.Assert(bodySize > 0);
                Utf8String functionName = functionOrdinal == 0 ? methodName : GetFuncletName(methodName, functionOrdinal - 1);
                int functionIndex = _wasmSymbolManager.GetSymbol(functionName).Index;
                // Folded methods share the same function index and body offset.
                _functionOffsets[functionIndex] = checked(methodSymbol.Value + bodyStart);
                _dwarfBuilder.EmitSectionInfo(functionName, (ulong)bodySize);
                // Wasm variable locations are not described yet.
                _dwarfBuilder.EmitSubprogramInfo(methodName, functionName, 0, bodySize, methodTypeIndex, [], []);

                List<NativeSequencePoint> functionPoints = new();
                while (sequencePointIndex < sequencePoints.Count && sequencePoints[sequencePointIndex].NativeOffset < bodyEnd)
                {
                    NativeSequencePoint point = sequencePoints[sequencePointIndex++];
                    Debug.Assert(point.NativeOffset >= frameInfo.StartOffset);
                    // JIT offsets include the body length; DWARF function offsets start at the locals vector.
                    functionPoints.Add(new NativeSequencePoint(
                        Math.Max(0, point.NativeOffset - bodyStart),
                        point.FileName,
                        point.LineNumber,
                        point.ColNumber));
                }
                if (functionPoints.Count > 0)
                {
                    _dwarfBuilder.EmitLineInfo(
                        functionIndex,
                        functionName,
                        0,
                        functionPoints,
                        bodySize);
                }
            }
        }

        private protected override void EmitDebugSections(IDictionary<Utf8String, SymbolDefinition> definedSymbols)
        {
            SectionWriter info = GetOrCreateSection(new ObjectNodeSection(".debug_info", SectionType.Debug));
            SectionWriter strings = GetOrCreateSection(new ObjectNodeSection(".debug_str", SectionType.Debug));
            SectionWriter abbrev = GetOrCreateSection(new ObjectNodeSection(".debug_abbrev", SectionType.Debug));
            SectionWriter loc = GetOrCreateSection(new ObjectNodeSection(".debug_loc", SectionType.Debug));
            SectionWriter ranges = GetOrCreateSection(new ObjectNodeSection(".debug_ranges", SectionType.Debug));
            SectionWriter lines = GetOrCreateSection(new ObjectNodeSection(".debug_line", SectionType.Debug));
            SectionWriter aranges = GetOrCreateSection(new ObjectNodeSection(".debug_aranges", SectionType.Debug));
            _dwarfBuilder.Write(info, strings, abbrev, loc, ranges, lines, aranges, _ => (default, 0));
        }

        private protected override void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            Utf8String currentSymbolName)
        {
        }

        private protected override void CreateEhSections()
        {
        }
    }
}
