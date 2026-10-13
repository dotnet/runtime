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

        // https://github.com/WebAssembly/tool-conventions/blob/main/Linking.md
        private enum SymbolKind : byte
        {
            Function = 0, // SYMTAB_FUNCTION
            Data = 1, // SYMTAB_DATA
            Global = 2, // SYMTAB_GLOBAL
            Tag = 4, // SYMTAB_EVENT
            Table = 5, // SYMTAB_TABLE
        }

        private const uint UndefinedSymbol = 0x10; // WASM_SYM_UNDEFINED
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
            MemoryAddrRelSleb = 11, // R_WASM_MEMORY_ADDR_REL_SLEB
            MemoryAddrI64 = 16, // R_WASM_MEMORY_ADDR_I64
            TableIndexI64 = 19, // R_WASM_TABLE_INDEX_I64
            MemoryAddrLocrelI32 = 23, // R_WASM_MEMORY_ADDR_LOCREL_I32
        }

        private readonly Dictionary<Utf8String, int> _linkingSymbolIndices = new();
        private readonly Dictionary<int, List<SymbolicRelocation>> _relocations = new();

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
            Dictionary<int, int> fileSectionIndices = new();
            foreach (int index in SectionEmitOrder)
            {
                if (_sections[index] is IWasmSection section)
                {
                    fileSectionIndices.Add(index, sections.Count);
                    sections.Add(section);
                }
            }

            int dataSectionIndex = sections.Count;
            if (_dataSection is not null)
            {
                sections.Add(_dataSection);
            }

            using MemoryStream linking = CreateLinkingSection();
            sections.Add(new WasmCustomSection(linking, new Utf8String("linking"), sections.Count));
            Dictionary<int, (string Name, MemoryStream Entries, int Count)> relocationSections = new();
            try
            {
                foreach ((int sectionIndex, List<SymbolicRelocation> relocations) in _relocations)
                {
                    SectionDataEmitter section = _sections[sectionIndex];
                    (int contentOffset, int targetSectionIndex, string name) = section switch
                    {
                        WasmSection wasmSection => (
                            wasmSection.ContentSize - (int)wasmSection.ContentReadStream.Length,
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
                    WriteRelocationEntries(section, contentOffset, relocations, relocationSection.Entries);
                    relocationSections[targetSectionIndex] = (relocationSection.Name, relocationSection.Entries, relocationSection.Count + relocations.Count);
                }

                foreach ((int targetSectionIndex, var relocationSection) in relocationSections)
                {
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

        private MemoryStream CreateLinkingSection()
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

        private unsafe void WriteRelocationEntries(SectionDataEmitter section, int contentOffset, List<SymbolicRelocation> relocs, MemoryStream payload)
        {
            using Stream originalStream = section.ContentReadStream;
            MemoryStream sectionStream = new((int)originalStream.Length);
            originalStream.Position = 0;
            originalStream.CopyTo(sectionStream);
            byte[] relocScratchBuffer = new byte[Relocation.MaxSize];

            foreach (SymbolicRelocation reloc in relocs)
            {
                int size = Relocation.GetSize(reloc.Type);
                bool isFunction = _wasmSymbolManager.TryGetSymbol(reloc.SymbolName, out WasmSymbol symbol) &&
                    symbol.IndexSpace == WasmIndexSpace.Function;
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
                    RelocType.IMAGE_REL_BASED_HIGHLOW when isFunction => WasmRelocationType.TableIndexI32,
                    RelocType.IMAGE_REL_BASED_HIGHLOW => WasmRelocationType.MemoryAddrI32,
                    RelocType.IMAGE_REL_BASED_DIR64 => WasmRelocationType.MemoryAddrI64,
                    RelocType.IMAGE_REL_BASED_RELPTR32 => WasmRelocationType.MemoryAddrLocrelI32,
                    _ => throw new NotSupportedException($"Relocation type {reloc.Type} for symbol '{reloc.SymbolName}' in section {section.SectionName} not yet implemented"),
                };

                sectionStream.Position = reloc.Offset;
                sectionStream.ReadExactly(relocScratchBuffer.AsSpan(0, size));
                fixed (byte* pData = relocScratchBuffer)
                {
                    long addend = reloc.Addend + Relocation.ReadValue(reloc.Type, pData);
                    bool hasAddend = type is WasmRelocationType.MemoryAddrLeb or WasmRelocationType.MemoryAddrSleb or
                        WasmRelocationType.MemoryAddrI32 or WasmRelocationType.MemoryAddrI64 or
                        WasmRelocationType.MemoryAddrRelSleb or WasmRelocationType.MemoryAddrLocrelI32;
                    if (!hasAddend && addend != 0)
                    {
                        throw new NotSupportedException($"Nonzero addend for {reloc.Type} relocation to '{reloc.SymbolName}'");
                    }

                    payload.WriteByte((byte)type); // type: the relocation type.
                    WriteULEB128(payload, checked((ulong)(contentOffset + reloc.Offset))); // offset: offset of the value to rewrite, relative to the section's contents.
                    // index: the symbol index, or the type index for R_WASM_TYPE_INDEX_LEB.
                    WriteULEB128(payload, (ulong)(type == WasmRelocationType.TypeIndexLeb
                        ? symbol.Index : _linkingSymbolIndices[reloc.SymbolName]));
                    if (hasAddend)
                    {
                        WriteSLEB128(payload, addend); // addend: addend to add to the address.
                    }

                    long value = type is WasmRelocationType.FunctionIndexLeb or WasmRelocationType.TypeIndexLeb or WasmRelocationType.GlobalIndexLeb
                        ? symbol.Index : 0;
                    Relocation.WriteValue(reloc.Type, pData, value);
                    sectionStream.Position = reloc.Offset;
                    sectionStream.Write(relocScratchBuffer.AsSpan(0, size));
                }
            }

            section.ContentReadStream = sectionStream;
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

        // ObjectWriter.Aot.cs methods
        private protected override void EmitUnwindInfo(SectionWriter sectionWriter, INodeWithCodeInfo nodeWithCodeInfo, Utf8String currentSymbolName)
        {
        }

        private protected override ITypesDebugInfoWriter CreateDebugInfoBuilder()
        {
            return null;
        }

        private protected override void EmitDebugFunctionInfo(uint methodTypeIndex, Utf8String methodDisplayName, Utf8String methodName, SymbolDefinition methodSymbol, INodeWithDebugInfo debugNode)
        {
        }

        private protected override void EmitDebugSections(IDictionary<Utf8String, SymbolDefinition> definedSymbols)
        {
        }

        private protected override void CreateEhSections()
        {
        }
    }
}
