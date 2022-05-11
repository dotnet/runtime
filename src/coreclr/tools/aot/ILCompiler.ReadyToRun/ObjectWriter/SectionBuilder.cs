// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using ILCompiler.DependencyAnalysis;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.PEWriter
{
    /// <summary>
    /// For a given symbol, this structure represents its target section and offset
    /// within the containing section.
    /// </summary>
    public struct SymbolTarget
    {
        /// <summary>
        /// Index of the section holding the symbol target.
        /// </summary>
        public readonly int SectionIndex;

        /// <summary>
        /// Offset of the symbol within the section.
        /// </summary>
        public readonly int Offset;

        public readonly int Size;

        /// <summary>
        /// Initialize symbol target with section and offset.
        /// </summary>
        /// <param name="sectionIndex">Section index where the symbol target resides</param>
        /// <param name="offset">Offset of the target within the section</param>
        public SymbolTarget(int sectionIndex, int offset, int size)
        {
            SectionIndex = sectionIndex;
            Offset = offset;
            Size = size;
        }
    }

    /// <summary>
    /// After placing an ObjectData within a section, we use this helper structure to record
    /// its relocation information for the final relocation pass.
    /// </summary>
    public struct PlacedObjectData
    {
        /// <summary>
        /// Offset of the ObjectData block within the section
        /// </summary>
        public readonly int Offset;

        /// <summary>
        /// Object data representing an array of relocations to fix up.
        /// </summary>
        public readonly ObjectNode.ObjectData Data;

        /// <summary>
        /// Array of relocations that need fixing up within the block.
        /// </summary>
        public Relocation[] Relocs => Data.Relocs;

        /// <summary>
        /// Initialize the list of relocations for a given object data item within the section.
        /// </summary>
        /// <param name="offset">Offset within the section</param>
        /// <param name="data">Object data block containing the list of relocations to fix up</param>
        public PlacedObjectData(int offset, ObjectNode.ObjectData data)
        {
            Offset = offset;
            Data = data;
        }
    }

    public struct SectionInfo
    {
        public readonly string SectionName;
        public readonly SectionCharacteristics Characteristics;

        public SectionInfo(string sectionName, SectionCharacteristics characteristics)
        {
            SectionName = sectionName;
            Characteristics = characteristics;
        }
    }

    /// <summary>
    /// Section represents a contiguous area of code or data with the same characteristics.
    /// </summary>
    public class Section
    {
        /// <summary>
        /// Index within the internal section table used by the section builder
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Section name
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Section characteristics
        /// </summary>
        public readonly SectionCharacteristics Characteristics;

        /// <summary>
        /// Alignment to apply when combining multiple builder sections into a single
        /// physical output section (typically when combining hot and cold code into
        /// the output code section).
        /// </summary>
        public readonly int Alignment;

        /// <summary>
        /// Blob builder representing the section content.
        /// </summary>
        public readonly BlobBuilder Content;

        /// <summary>
        /// All blocks requiring relocation resolution within the section
        /// </summary>
        public readonly List<PlacedObjectData> PlacedObjectDataToRelocate;

        /// <summary>
        /// RVA gets filled in during section serialization.
        /// </summary>
        public int RVAWhenPlaced;

        /// <summary>
        /// Output file position gets filled in during section serialization.
        /// </summary>
        public int FilePosWhenPlaced;

        /// <summary>
        /// Construct a new session object.
        /// </summary>
        /// <param name="index">Zero-based section index</param>
        /// <param name="name">Section name</param>
        /// <param name="characteristics">Section characteristics</param>
        /// <param name="alignment">Alignment for combining multiple logical sections</param>
        public Section(int index, string name, SectionCharacteristics characteristics, int alignment)
        {
            Index = index;
            Name = name;
            Characteristics = characteristics;
            Alignment = alignment;
            Content = new BlobBuilder();
            PlacedObjectDataToRelocate = new List<PlacedObjectData>();
            RVAWhenPlaced = 0;
            FilePosWhenPlaced = 0;
        }
    }

    /// <summary>
    /// This class represents a single export symbol in the PE file.
    /// </summary>
    public class ExportSymbol
    {
        /// <summary>
        /// Symbol identifier
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// When placed into the export section, RVA of the symbol name gets updated.
        /// </summary>
        public int NameRVAWhenPlaced;

        /// <summary>
        /// Export symbol ordinal
        /// </summary>
        public readonly int Ordinal;

        /// <summary>
        /// Symbol to export
        /// </summary>
        public readonly ISymbolNode Symbol;

        /// <summary>
        /// Construct the export symbol instance filling in its arguments
        /// </summary>
        /// <param name="name">Export symbol identifier</param>
        /// <param name="ordinal">Ordinal ID of the export symbol</param>
        /// <param name="symbol">Symbol to export</param>
        public ExportSymbol(string name, int ordinal, ISymbolNode symbol)
        {
            Name = name;
            Ordinal = ordinal;
            Symbol = symbol;
        }
    }

    /// <summary>
    /// Section builder is capable of accumulating blocks, using them to lay out sections
    /// and relocate the produced executable according to the block relocation information.
    /// </summary>
    public class SectionBuilder
    {
        /// <summary>
        /// Target OS / architecture.
        /// </summary>
        TargetDetails _target;

        /// <summary>
        /// Map from symbols to their target sections and offsets.
        /// </summary>
        Dictionary<ISymbolNode, SymbolTarget> _symbolMap;

        /// <summary>
        /// List of sections defined in the builder
        /// </summary>
        List<Section> _sections;

        /// <summary>
        /// Symbols to export from the PE file.
        /// </summary>
        List<ExportSymbol> _exportSymbols;

        /// <summary>
        /// Optional symbol representing an entrypoint override.
        /// </summary>
        ISymbolNode _entryPointSymbol;

        /// <summary>
        /// Export directory entry when available.
        /// </summary>
        DirectoryEntry _exportDirectoryEntry;

        /// <summary>
        /// Directory entry representing the extra relocation records.
        /// </summary>
        DirectoryEntry _relocationDirectoryEntry;

        /// <summary>
        /// Symbol representing the ready-to-run COR (MSIL) header table.
        /// Only present in single-file R2R executables. Composite R2R
        /// executables don't have a COR header and locate the ReadyToRun
        /// header directly using the well-known export symbol RTR_HEADER.
        /// </summary>
        ISymbolNode _corHeaderSymbol;

        /// <summary>
        /// Size of the ready-to-run header table in bytes.
        /// </summary>
        int _corHeaderSize;

        /// <summary>
        /// Symbol representing the debug directory.
        /// </summary>
        ISymbolNode _debugDirectorySymbol;

        /// <summary>
        /// Size of the debug directory in bytes.
        /// </summary>
        int _debugDirectorySize;

        /// <summary>
        /// Symbol representing the start of the win32 resources
        /// </summary>
        ISymbolNode _win32ResourcesSymbol;

        /// <summary>
        /// Size of the win32 resources
        /// </summary>
        int _win32ResourcesSize;

        /// <summary>
        /// Padding 4-byte sequence to use in code section. Typically corresponds
        /// to some interrupt to be thrown at "invalid" IP addresses.
        /// </summary>
        uint _codePadding;

        /// <summary>
        /// For PE files with exports, this is the "DLL name" string to store in the export directory table.
        /// </summary>
        string _dllNameForExportDirectoryTable;

        /// <summary>
        /// Construct an empty section builder without any sections or blocks.
        /// </summary>
        public SectionBuilder(TargetDetails target)
        {
            _target = target;
            _symbolMap = new Dictionary<ISymbolNode, SymbolTarget>();
            _sections = new List<Section>();
            _exportSymbols = new List<ExportSymbol>();
            _entryPointSymbol = null;
            _exportDirectoryEntry = default(DirectoryEntry);
            _relocationDirectoryEntry = default(DirectoryEntry);

            switch (_target.Architecture)
            {
                case TargetArchitecture.X86:
                case TargetArchitecture.X64:
                    // 4 times INT 3 (or debugger break)
                    _codePadding = 0xCCCCCCCCu;
                    break;

                case TargetArchitecture.ARM:
                    // 2 times undefined instruction used as debugger break
                    _codePadding = (_target.IsWindows ? 0xDEFEDEFEu : 0xDE01DE01u);
                    break;

                case TargetArchitecture.ARM64:
                    _codePadding = 0xD43E0000u;
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Add a new section. Section names must be unique.
        /// </summary>
        /// <param name="name">Section name</param>
        /// <param name="characteristics">Section characteristics</param>
        /// <param name="alignment">
        /// Alignment for composing multiple builder sections into one physical output section
        /// </param>
        /// <returns>Zero-based index of the added section</returns>
        public int AddSection(string name, SectionCharacteristics characteristics, int alignment)
        {
            int sectionIndex = _sections.Count;
            _sections.Add(new Section(sectionIndex, name, characteristics, alignment));
            return sectionIndex;
        }

        /// <summary>
        /// Try to look up a pre-existing section in the builder; returns null if not found.
        /// </summary>
        public Section FindSection(string name)
        {
            return _sections.FirstOrDefault((sec) => sec.Name == name);
        }

        /// <summary>
        /// Look up RVA for a given symbol. This assumes the section have already been placed.
        /// </summary>
        /// <param name="symbol">Symbol to look up</param>
        /// <returns>RVA of the symbol</returns>
        public int GetSymbolRVA(ISymbolNode symbol)
        {
            SymbolTarget symbolTarget = _symbolMap[symbol];
            Section section = _sections[symbolTarget.SectionIndex];
            Debug.Assert(section.RVAWhenPlaced != 0);
            return section.RVAWhenPlaced + symbolTarget.Offset;
        }

        /// <summary>
        /// Look up final file position for a given symbol. This assumes the section have already been placed.
        /// </summary>
        /// <param name="symbol">Symbol to look up</param>
        /// <returns>File position of the symbol, from the beginning of the emitted image</returns>
        public int GetSymbolFilePosition(ISymbolNode symbol)
        {
            SymbolTarget symbolTarget = _symbolMap[symbol];
            Section section = _sections[symbolTarget.SectionIndex];
            Debug.Assert(section.RVAWhenPlaced != 0);
            return section.FilePosWhenPlaced + symbolTarget.Offset;
        }

        /// <summary>
        /// Attach an export symbol to the output PE file.
        /// </summary>
        /// <param name="name">Export symbol identifier</param>
        /// <param name="ordinal">Ordinal ID of the export symbol</param>
        /// <param name="symbol">Symbol to export</param>
        public void AddExportSymbol(string name, int ordinal, ISymbolNode symbol)
        {
            _exportSymbols.Add(new ExportSymbol(
                name: name,
                ordinal: ordinal,
                symbol: symbol));
        }

        /// <summary>
        /// Record DLL name to emit in the export directory table.
        /// </summary>
        /// <param name="dllName">DLL name to emit</param>
        public void SetDllNameForExportDirectoryTable(string dllName)
        {
            _dllNameForExportDirectoryTable = dllName;
        }

        /// <summary>
        /// Override entry point for the app.
        /// </summary>
        /// <param name="symbol">Symbol representing the new entry point</param>
        public void SetEntryPoint(ISymbolNode symbol)
        {
            _entryPointSymbol = symbol;
        }

        public void SetCorHeader(ISymbolNode symbol, int headerSize)
        {
            _corHeaderSymbol = symbol;
            _corHeaderSize = headerSize;
        }

        public void SetDebugDirectory(ISymbolNode symbol, int size)
        {
            _debugDirectorySymbol = symbol;
            _debugDirectorySize = size;
        }

        public void SetWin32Resources(ISymbolNode symbol, int resourcesSize)
        {
            _win32ResourcesSymbol = symbol;
            _win32ResourcesSize = resourcesSize;
        }

        private NativeAotNameMangler _nameMangler;

        private NameMangler GetNameMangler()
        {
            if (_nameMangler == null)
            {
                // TODO-REFACTOR: why do we have two name manglers?
                _nameMangler = new NativeAotNameMangler();
                _nameMangler.CompilationUnitPrefix = "";
            }
            return _nameMangler;
        }

        /// <summary>
        /// Add an ObjectData block to a given section.
        /// </summary>
        /// <param name="data">Block to add</param>
        /// <param name="sectionIndex">Section index</param>
        /// <param name="name">Node name to emit in the map file</param>
        /// <param name="outputInfoBuilder">Optional output info to collect (used for creating maps and symbols)</param>
        public void AddObjectData(ObjectNode.ObjectData objectData, int sectionIndex, string name, OutputInfoBuilder outputInfoBuilder)
        {
            Section section = _sections[sectionIndex];

            // Calculate alignment padding - apparently ObjectDataBuilder can produce an alignment of 0
            int alignedOffset = section.Content.Count;
            if (objectData.Alignment > 1)
            {
                alignedOffset = (section.Content.Count + objectData.Alignment - 1) & -objectData.Alignment;
                int padding = alignedOffset - section.Content.Count;
                if (padding > 0)
                {
                    if ((section.Characteristics & SectionCharacteristics.ContainsCode) != 0)
                    {
                        uint cp = _codePadding;
                        while (padding >= sizeof(uint))
                        {
                            section.Content.WriteUInt32(cp);
                            padding -= sizeof(uint);
                        }
                        if (padding >= 2)
                        {
                            section.Content.WriteUInt16(unchecked((ushort)cp));
                            cp >>= 16;
                        }
                        if ((padding & 1) != 0)
                        {
                            section.Content.WriteByte(unchecked((byte)cp));
                        }
                    }
                    else
                    {
                        section.Content.WriteBytes(0, padding);
                    }
                }
            }

            if (outputInfoBuilder != null)
            {
                var node = new OutputNode(sectionIndex, alignedOffset, objectData.Data.Length, name);
                outputInfoBuilder.AddNode(node, objectData.DefinedSymbols[0]);
                if (objectData.Relocs != null)
                {
                    foreach (Relocation reloc in objectData.Relocs)
                    {
                        RelocType fileReloc = Relocation.GetFileRelocationType(reloc.RelocType);
                        if (fileReloc != RelocType.IMAGE_REL_BASED_ABSOLUTE)
                        {
                            outputInfoBuilder.AddRelocation(node, fileReloc);
                        }
                    }
                }
            }

            section.Content.WriteBytes(objectData.Data);

            if (objectData.DefinedSymbols != null)
            {
                foreach (ISymbolDefinitionNode symbol in objectData.DefinedSymbols)
                {
                    if (outputInfoBuilder != null)
                    {
                        Utf8StringBuilder sb = new Utf8StringBuilder();
                        symbol.AppendMangledName(GetNameMangler(), sb);
                        int sectionRelativeOffset = alignedOffset + symbol.Offset;
                        outputInfoBuilder.AddSymbol(new OutputSymbol(sectionIndex, sectionRelativeOffset, sb.ToString()));
                    }
                    _symbolMap.Add(symbol, new SymbolTarget(
                        sectionIndex: sectionIndex,
                        offset: alignedOffset + symbol.Offset,
                        size: objectData.Data.Length));
                }
            }

            if (objectData.Relocs != null && objectData.Relocs.Length != 0)
            {
                section.PlacedObjectDataToRelocate.Add(new PlacedObjectData(alignedOffset, objectData));
            }
        }

        public void AddSymbolForRange(ISymbolNode symbol, ISymbolNode firstNode, ISymbolNode secondNode)
        {
            SymbolTarget firstSymbolTarget = _symbolMap[firstNode];
            SymbolTarget secondSymbolTarget = _symbolMap[secondNode];
            Debug.Assert(firstSymbolTarget.SectionIndex == secondSymbolTarget.SectionIndex);
            Debug.Assert(firstSymbolTarget.Offset <= secondSymbolTarget.Offset);

            _symbolMap.Add(symbol, new SymbolTarget(
                sectionIndex: firstSymbolTarget.SectionIndex,
                offset: firstSymbolTarget.Offset,
                size: secondSymbolTarget.Offset - firstSymbolTarget.Offset + secondSymbolTarget.Size
                ));
        }

        /// <summary>
        /// Get the list of sections that need to be emitted to the output PE file.
        /// We filter out name duplicates as we'll end up merging builder sections with the same name
        /// into a single output physical section.
        /// </summary>
        public IEnumerable<SectionInfo> GetSections()
        {
            List<SectionInfo> sectionList = new List<SectionInfo>();
            foreach (Section section in _sections)
            {
                if (!sectionList.Any((sc) => sc.SectionName == section.Name))
                {
                    sectionList.Add(new SectionInfo(section.Name, section.Characteristics));
                }
            }

            return sectionList;
        }

        public void AddSections(OutputInfoBuilder outputInfoBuilder)
        {
            foreach (Section section in _sections)
            {
                outputInfoBuilder.AddSection(section);
            }
        }

        /// <summary>
        /// Traverse blocks within a single section and use them to calculate final layout
        /// of the given section.
        /// </summary>
        /// <param name="name">Section to serialize</param>
        /// <param name="sectionLocation">Logical section address within the output PE file</param>
        /// <returns></returns>
        public BlobBuilder SerializeSection(string name, SectionLocation sectionLocation)
        {
            if (name == R2RPEBuilder.RelocSectionName)
            {
                return SerializeRelocationSection(sectionLocation);
            }

            if (name == R2RPEBuilder.ExportDataSectionName)
            {
                return SerializeExportSection(sectionLocation);
            }

            BlobBuilder serializedSection = null;

            // Locate logical section index by name
            foreach (Section section in _sections.Where((sec) => sec.Name == name))
            {
                // Calculate alignment padding
                int alignedRVA = (sectionLocation.RelativeVirtualAddress + section.Alignment - 1) & -section.Alignment;
                int padding = alignedRVA - sectionLocation.RelativeVirtualAddress;
                if (padding > 0)
                {
                    if (serializedSection == null)
                    {
                        serializedSection = new BlobBuilder();
                    }
                    serializedSection.WriteBytes(0, padding);
                    sectionLocation = new SectionLocation(
                        sectionLocation.RelativeVirtualAddress + padding,
                        sectionLocation.PointerToRawData + padding);
                }

                // Place the section
                section.RVAWhenPlaced = sectionLocation.RelativeVirtualAddress;
                section.FilePosWhenPlaced = sectionLocation.PointerToRawData;

                if (section.Content.Count != 0)
                {
                    sectionLocation = new SectionLocation(
                        sectionLocation.RelativeVirtualAddress + section.Content.Count,
                        sectionLocation.PointerToRawData + section.Content.Count);

                    if (serializedSection == null)
                    {
                        serializedSection = section.Content;
                    }
                    else
                    {
                        serializedSection.LinkSuffix(section.Content);
                    }
                }
            }

            return serializedSection;
        }

        /// <summary>
        /// Emit the .reloc section based on file relocation information in the individual blocks.
        /// We rely on the fact that the .reloc section is emitted last so that, by the time
        /// it's getting serialized, all other sections that may contain relocations have already
        /// been laid out.
        /// </summary>
        private BlobBuilder SerializeRelocationSection(SectionLocation sectionLocation)
        {
            // There are 12 bits for the relative offset
            const int RelocationTypeShift = 12;
            const int MaxRelativeOffsetInBlock = (1 << RelocationTypeShift) - 1;

            // Even though the format doesn't dictate it, it seems customary
            // to align the base RVA's on 4K boundaries.
            const int BaseRVAAlignment = 1 << RelocationTypeShift;

            BlobBuilder builder = new BlobBuilder();
            int baseRVA = 0;
            List<ushort> offsetsAndTypes = null;

            Section relocSection = FindSection(R2RPEBuilder.RelocSectionName);
            if (relocSection != null)
            {
                relocSection.FilePosWhenPlaced = sectionLocation.PointerToRawData;
                relocSection.RVAWhenPlaced = sectionLocation.RelativeVirtualAddress;
                builder = relocSection.Content;
            }

            // Traverse relocations in all sections in their RVA order
            // By now, all "normal" sections with relocations should already have been laid out
            foreach (Section section in _sections.OrderBy((sec) => sec.RVAWhenPlaced))
            {
                foreach (PlacedObjectData placedObjectData in section.PlacedObjectDataToRelocate)
                {
                    for (int relocIndex = 0; relocIndex < placedObjectData.Relocs.Length; relocIndex++)
                    {
                        RelocType relocType = placedObjectData.Relocs[relocIndex].RelocType;
                        RelocType fileRelocType = Relocation.GetFileRelocationType(relocType);
                        if (fileRelocType != RelocType.IMAGE_REL_BASED_ABSOLUTE)
                        {
                            int relocationRVA = section.RVAWhenPlaced + placedObjectData.Offset + placedObjectData.Relocs[relocIndex].Offset;
                            if (offsetsAndTypes != null && relocationRVA - baseRVA > MaxRelativeOffsetInBlock)
                            {
                                // Need to flush relocation block as the current RVA is too far from base RVA
                                FlushRelocationBlock(builder, baseRVA, offsetsAndTypes);
                                offsetsAndTypes = null;
                            }
                            if (offsetsAndTypes == null)
                            {
                                // Create new relocation block
                                baseRVA = relocationRVA & -BaseRVAAlignment;
                                offsetsAndTypes = new List<ushort>();
                            }
                            ushort offsetAndType = (ushort)(((ushort)fileRelocType << RelocationTypeShift) | (relocationRVA - baseRVA));
                            offsetsAndTypes.Add(offsetAndType);
                        }
                    }
                }
            }

            if (offsetsAndTypes != null)
            {
                FlushRelocationBlock(builder, baseRVA, offsetsAndTypes);
            }

            if (builder.Count != 0)
            {
                _relocationDirectoryEntry = new DirectoryEntry(sectionLocation.RelativeVirtualAddress, builder.Count);
            }

            return builder;
        }

        /// <summary>
        /// Serialize a block of relocations into the .reloc section.
        /// </summary>
        /// <param name="builder">Output blob builder to receive the serialized relocation block</param>
        /// <param name="baseRVA">Base RVA of the relocation block</param>
        /// <param name="offsetsAndTypes">16-bit entries encoding offset relative to the base RVA (low 12 bits) and relocation type (top 4 bite)</param>
        private static void FlushRelocationBlock(BlobBuilder builder, int baseRVA, List<ushort> offsetsAndTypes)
        {
            // Ensure blocks are 4-byte aligned. This is required by kernel memory manager
            // on Windows 8.1 and earlier.
            if ((offsetsAndTypes.Count & 1) == 1)
            {
                offsetsAndTypes.Add(0);
            }

            // First, emit the block header: 4 bytes starting RVA,
            builder.WriteInt32(baseRVA);
            // followed by the total block size comprising this header
            // and following 16-bit entries.
            builder.WriteInt32(4 + 4 + 2 * offsetsAndTypes.Count);
            // Now serialize out the entries
            foreach (ushort offsetAndType in offsetsAndTypes)
            {
                builder.WriteUInt16(offsetAndType);
            }
        }

        /// <summary>
        /// Serialize the export symbol table into the export section.
        /// </summary>
        /// <param name="location">RVA and file location of the .edata section</param>
        private BlobBuilder SerializeExportSection(SectionLocation sectionLocation)
        {
            _exportSymbols.MergeSort((es1, es2) => StringComparer.Ordinal.Compare(es1.Name, es2.Name));

            BlobBuilder builder = new BlobBuilder();

            int minOrdinal = int.MaxValue;
            int maxOrdinal = int.MinValue;

            // First, emit the name table and store the name RVA's for the individual export symbols
            // Also, record the ordinal range.
            foreach (ExportSymbol symbol in _exportSymbols)
            {
                symbol.NameRVAWhenPlaced = sectionLocation.RelativeVirtualAddress + builder.Count;
                builder.WriteUTF8(symbol.Name);
                builder.WriteByte(0);

                if (symbol.Ordinal < minOrdinal)
                {
                    minOrdinal = symbol.Ordinal;
                }
                if (symbol.Ordinal > maxOrdinal)
                {
                    maxOrdinal = symbol.Ordinal;
                }
            }

            // Emit the DLL name
            int dllNameRVA = sectionLocation.RelativeVirtualAddress + builder.Count;
            builder.WriteUTF8(_dllNameForExportDirectoryTable);
            builder.WriteByte(0);

            int[] addressTable = new int[maxOrdinal - minOrdinal + 1];

            // Emit the name pointer table; it should be alphabetically sorted.
            // Also, we can now fill in the export address table as we've detected its size
            // in the previous pass.
            builder.Align(4);
            int namePointerTableRVA = sectionLocation.RelativeVirtualAddress + builder.Count;
            foreach (ExportSymbol symbol in _exportSymbols)
            {
                builder.WriteInt32(symbol.NameRVAWhenPlaced);
                SymbolTarget symbolTarget = _symbolMap[symbol.Symbol];
                Section symbolSection = _sections[symbolTarget.SectionIndex];
                Debug.Assert(symbolSection.RVAWhenPlaced != 0);
                addressTable[symbol.Ordinal - minOrdinal] = symbolSection.RVAWhenPlaced + symbolTarget.Offset;
            }

            // Emit the ordinal table
            int ordinalTableRVA = sectionLocation.RelativeVirtualAddress + builder.Count;
            foreach (ExportSymbol symbol in _exportSymbols)
            {
                builder.WriteUInt16((ushort)(symbol.Ordinal - minOrdinal));
            }

            // Emit the address table
            builder.Align(4);
            int addressTableRVA = sectionLocation.RelativeVirtualAddress + builder.Count;
            foreach (int addressTableEntry in addressTable)
            {
                builder.WriteInt32(addressTableEntry);
            }

            // Emit the export directory table
            builder.Align(4);
            int exportDirectoryTableRVA = sectionLocation.RelativeVirtualAddress + builder.Count;
            // +0x00: reserved
            builder.WriteInt32(0);
            // +0x04: TODO: time/date stamp
            builder.WriteInt32(0);
            // +0x08: major version
            builder.WriteInt16(0);
            // +0x0A: minor version
            builder.WriteInt16(0);
            // +0x0C: DLL name RVA
            builder.WriteInt32(dllNameRVA);
            // +0x10: ordinal base
            builder.WriteInt32(minOrdinal);
            // +0x14: number of entries in the address table
            builder.WriteInt32(addressTable.Length);
            // +0x18: number of name pointers
            builder.WriteInt32(_exportSymbols.Count);
            // +0x1C: export address table RVA
            builder.WriteInt32(addressTableRVA);
            // +0x20: name pointer RVV
            builder.WriteInt32(namePointerTableRVA);
            // +0x24: ordinal table RVA
            builder.WriteInt32(ordinalTableRVA);
            int exportDirectorySize = sectionLocation.RelativeVirtualAddress + builder.Count - exportDirectoryTableRVA;

            _exportDirectoryEntry = new DirectoryEntry(relativeVirtualAddress: exportDirectoryTableRVA, size: exportDirectorySize);

            return builder;
        }

        /// <summary>
        /// Update the PE file directories. Currently this is used to update the export symbol table
        /// when export symbols have been added to the section builder.
        /// </summary>
        /// <param name="directoriesBuilder">PE directory builder to update</param>
        public void UpdateDirectories(PEDirectoriesBuilder directoriesBuilder)
        {
            if (_corHeaderSymbol != null)
            {
                SymbolTarget symbolTarget = _symbolMap[_corHeaderSymbol];
                Section section = _sections[symbolTarget.SectionIndex];
                Debug.Assert(section.RVAWhenPlaced != 0);
                directoriesBuilder.CorHeaderTable = new DirectoryEntry(section.RVAWhenPlaced + symbolTarget.Offset, _corHeaderSize);
            }

            if (_win32ResourcesSymbol != null)
            {
                SymbolTarget symbolTarget = _symbolMap[_win32ResourcesSymbol];
                Section section = _sections[symbolTarget.SectionIndex];
                Debug.Assert(section.RVAWhenPlaced != 0);

                // Windows has a bug in its resource processing logic that occurs when
                // 1. A PE file is loaded as a data file
                // 2. The resource data found in the resources has an RVA which has a magnitude greater than the size of the section which holds the resources
                // 3. The offset of the start of the resource data from the start of the section is not zero.
                //
                // As it is impossible to effect condition 1 in the compiler, and changing condition 2 would require bloating the virtual size of the sections,
                // instead require that the resource data is located at offset 0 within the section.
                // We achieve that by sorting the Win32ResourcesNode as the first node.
                Debug.Assert(symbolTarget.Offset == 0);
                directoriesBuilder.ResourceTable = new DirectoryEntry(section.RVAWhenPlaced + symbolTarget.Offset, _win32ResourcesSize);
            }

            if (_exportDirectoryEntry.Size != 0)
            {
                directoriesBuilder.ExportTable = _exportDirectoryEntry;
            }

            int relocationTableRVA = directoriesBuilder.BaseRelocationTable.RelativeVirtualAddress;
            if (relocationTableRVA == 0)
            {
                relocationTableRVA = _relocationDirectoryEntry.RelativeVirtualAddress;
            }
            directoriesBuilder.BaseRelocationTable = new DirectoryEntry(
                relocationTableRVA,
                directoriesBuilder.BaseRelocationTable.Size + _relocationDirectoryEntry.Size);

            if (_entryPointSymbol != null)
            {
                SymbolTarget symbolTarget = _symbolMap[_entryPointSymbol];
                Section section = _sections[symbolTarget.SectionIndex];
                Debug.Assert(section.RVAWhenPlaced != 0);
                directoriesBuilder.AddressOfEntryPoint = section.RVAWhenPlaced + symbolTarget.Offset;
            }

            if (_debugDirectorySymbol != null)
            {
                SymbolTarget symbolTarget = _symbolMap[_debugDirectorySymbol];
                Section section = _sections[symbolTarget.SectionIndex];
                Debug.Assert(section.RVAWhenPlaced != 0);
                directoriesBuilder.DebugTable = new DirectoryEntry(section.RVAWhenPlaced + symbolTarget.Offset, _debugDirectorySize);
            }
        }

        /// <summary>
        /// Relocate the produced PE file and output the result into a given stream.
        /// </summary>
        /// <param name="peFile">Blob builder representing the complete PE file</param>
        /// <param name="defaultImageBase">Default load address for the image</param>
        /// <param name="corHeaderBuilder">COR header</param>
        /// <param name="corHeaderFileOffset">File position of the COR header</param>
        /// <param name="outputStream">Stream to receive the relocated PE file</param>
        public void RelocateOutputFile(
            BlobBuilder peFile,
            ulong defaultImageBase,
            Stream outputStream)
        {
            RelocationHelper relocationHelper = new RelocationHelper(outputStream, defaultImageBase, peFile);

            // Traverse relocations in all sections in their RVA order
            foreach (Section section in _sections.OrderBy((sec) => sec.RVAWhenPlaced))
            {
                int rvaToFilePosDelta = section.FilePosWhenPlaced - section.RVAWhenPlaced;
                foreach (PlacedObjectData placedObjectData in section.PlacedObjectDataToRelocate)
                {
                    foreach (Relocation relocation in placedObjectData.Relocs)
                    {
                        // Process a single relocation
                        int relocationRVA = section.RVAWhenPlaced + placedObjectData.Offset + relocation.Offset;
                        int relocationFilePos = relocationRVA + rvaToFilePosDelta;

                        // Flush parts of PE file before the relocation to the output stream
                        relocationHelper.CopyToFilePosition(relocationFilePos);

                        // Look up relocation target
                        SymbolTarget relocationTarget = _symbolMap[relocation.Target];
                        Section targetSection = _sections[relocationTarget.SectionIndex];
                        int targetRVA = targetSection.RVAWhenPlaced + relocationTarget.Offset;
                        int filePosWhenPlaced = targetSection.FilePosWhenPlaced + relocationTarget.Offset;

                        // If relocating to a node's size, switch out the target RVA with data length
                        if (relocation.RelocType == RelocType.IMAGE_REL_SYMBOL_SIZE)
                        {
                            targetRVA = relocationTarget.Size;
                        }

                        // Apply the relocation
                        relocationHelper.ProcessRelocation(relocation.RelocType, relocationRVA, targetRVA, filePosWhenPlaced);
                    }
                }
            }

            // Flush remaining PE file blocks after the last relocation
            relocationHelper.CopyRestOfFile();
        }
    }
}
