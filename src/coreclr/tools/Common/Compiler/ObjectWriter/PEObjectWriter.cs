// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// PE image file writer built on top of the COFF object writer infrastructure.
    /// This writer reuses sections prepared by the base class and emits a minimal
    /// PE/PE+ image containing those sections.
    /// </summary>
    internal sealed class PEObjectWriter : CoffObjectWriter
    {
        /// <summary>
        /// Number of low-order RVA bits that must match file position on Linux.
        /// </summary>
        /// <remarks>
        /// This is because the CoreCLR runtime on Linux requires the 12-16 low-order bits of section RVAs
        /// (the number of bits corresponds to the page size) to be identical to the file offset,
        /// otherwise memory mapping of the file fails.
        /// </remarks>
        private const int RVABitsToMatchFilePos = 16;
        internal const int DosHeaderSize = 0x80;
        private const int NoSectionIndex = -1;

        private static ReadOnlySpan<byte> DosHeader => // DosHeaderSize
        [
            0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00,
            0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,

            0x80, 0x00, 0x00, 0x00, // NT Header offset (0x80 == DosHeader.Length)

            0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09, 0xcd,
            0x21, 0xb8, 0x01, 0x4c, 0xcd, 0x21, 0x54, 0x68,
            0x69, 0x73, 0x20, 0x70, 0x72, 0x6f, 0x67, 0x72,
            0x61, 0x6d, 0x20, 0x63, 0x61, 0x6e, 0x6e, 0x6f,
            0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6e,
            0x20, 0x69, 0x6e, 0x20, 0x44, 0x4f, 0x53, 0x20,
            0x6d, 0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d, 0x0a,
            0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ];

        private static ObjectNodeSection ExportDirectorySection = new ObjectNodeSection("edata", SectionType.ReadOnly);
        private static ObjectNodeSection BaseRelocSection = new ObjectNodeSection("reloc", SectionType.ReadOnly);

        private uint _peSectionAlignment;
        private uint _peFileAlignment;
        private readonly string _outputPath;
        private readonly int _requestedSectionAlignment;
        private readonly int? _coffTimestamp;

        // Relocations that we can resolve at emit time (ie not file-based relocations).
        private Dictionary<int, List<SymbolicRelocation>> _resolvableRelocations = [];

        private int _pdataSectionIndex = NoSectionIndex;
        private int _debugSectionIndex = NoSectionIndex;
        private int _exportSectionIndex = NoSectionIndex;
        private int _baseRelocSectionIndex = NoSectionIndex;
        private int _corMetaSectionIndex = NoSectionIndex;

        // Base relocation (.reloc) bookkeeping
        private readonly SortedDictionary<uint, List<ushort>> _baseRelocMap = new();
        private Dictionary<string, SymbolDefinition> _definedSymbols = [];

        private HashSet<string> _exportedSymbolNames = new();
        private long _coffHeaderOffset;

        public PEObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder, string outputPath, int sectionAlignment, int? coffTimestamp)
            : base(factory, options, outputInfoBuilder)
        {
            _outputPath = outputPath;
            _requestedSectionAlignment = sectionAlignment;
            _coffTimestamp = coffTimestamp;
        }

        public void AddExportedSymbol(string symbol)
        {
            if (!string.IsNullOrEmpty(symbol))
            {
                _exportedSymbolNames.Add(symbol);
            }
        }

        private protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, int sectionIndex, Stream sectionStream)
        {
            // COMDAT sections are not supported in PE files
            base.CreateSection(section, comdatName: null, symbolName, sectionIndex, sectionStream);

            if (_requestedSectionAlignment != 0)
            {
                UpdateSectionAlignment(sectionIndex, _requestedSectionAlignment);
            }
        }

        private struct PEOptionalHeader
        {
            public bool IsPE32Plus { get; }

            // Standard fields
            public byte MajorLinkerVersion { get; set; }
            public byte MinorLinkerVersion { get; set; }
            public uint SizeOfCode { get; set; }
            public uint SizeOfInitializedData { get; set; }
            public uint SizeOfUninitializedData { get; set; }
            public uint AddressOfEntryPoint { get; set; }
            public uint BaseOfCode { get; set; }
            public uint BaseOfData { get; set; }

            // Windows-specific fields
            public ulong ImageBase { get; set; }
            public uint SectionAlignment { get; set; }
            public uint FileAlignment { get; set; }
            public ushort MajorOperatingSystemVersion { get; set; }
            public ushort MinorOperatingSystemVersion { get; set; }
            public ushort MajorImageVersion { get; set; }
            public ushort MinorImageVersion { get; set; }
            public ushort MajorSubsystemVersion { get; set; }
            public ushort MinorSubsystemVersion { get; set; }
            public Subsystem Subsystem { get; set; }
            public DllCharacteristics DllCharacteristics { get; set; }
            public uint Win32VersionValue { get; set; }
            public uint SizeOfImage { get; set; }
            public uint SizeOfHeaders { get; set; }
            public uint CheckSum { get; set; }
            public ulong SizeOfStackReserve { get; set; }
            public ulong SizeOfStackCommit { get; set; }
            public ulong SizeOfHeapReserve { get; set; }
            public ulong SizeOfHeapCommit { get; set; }
            public uint LoaderFlags { get; set; }
            public uint NumberOfRvaAndSizes { get; set; }

            public PEOptionalHeader(bool isPe32Plus)
            {
                IsPE32Plus = isPe32Plus;

                // Defaults taken from PETargetExtensions (PEHeaderConstants / PE32/PE64 constants)
                MajorLinkerVersion = PEHeaderConstants.MajorLinkerVersion;
                MinorLinkerVersion = PEHeaderConstants.MinorLinkerVersion;

                SizeOfCode = 0;
                SizeOfInitializedData = 0;
                SizeOfUninitializedData = 0;
                AddressOfEntryPoint = 0;
                BaseOfCode = 0;
                BaseOfData = 0;

                ImageBase = IsPE32Plus ? PE64HeaderConstants.ExeImageBase : PE32HeaderConstants.ImageBase;
                // Use PETargetExtensions defaults for alignments and versions
                SectionAlignment = (uint)PEHeaderConstants.SectionAlignment;
                FileAlignment = 0x200;

                MajorOperatingSystemVersion = PEHeaderConstants.MajorOperatingSystemVersion;
                MinorOperatingSystemVersion = PEHeaderConstants.MinorOperatingSystemVersion;

                MajorImageVersion = PEHeaderConstants.MajorImageVersion;
                MinorImageVersion = PEHeaderConstants.MinorImageVersion;

                MajorSubsystemVersion = PEHeaderConstants.MajorSubsystemVersion;
                MinorSubsystemVersion = PEHeaderConstants.MinorSubsystemVersion;

                Subsystem = Subsystem.WindowsCui;
                DllCharacteristics = DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible | DllCharacteristics.NoSeh | DllCharacteristics.TerminalServerAware;

                Win32VersionValue = 0;
                SizeOfImage = 0;
                SizeOfHeaders = 0;
                CheckSum = 0;

                // Use PE32/PE64 per-target defaults for stack/heap
                SizeOfStackReserve = IsPE32Plus ? PE64HeaderConstants.SizeOfStackReserve : PE32HeaderConstants.SizeOfStackReserve;
                SizeOfStackCommit = IsPE32Plus ? PE64HeaderConstants.SizeOfStackCommit : PE32HeaderConstants.SizeOfStackCommit;
                SizeOfHeapReserve = IsPE32Plus ? PE64HeaderConstants.SizeOfHeapReserve : PE32HeaderConstants.SizeOfHeapReserve;
                SizeOfHeapCommit = IsPE32Plus ? PE64HeaderConstants.SizeOfHeapCommit : PE32HeaderConstants.SizeOfHeapCommit;

                LoaderFlags = 0;
                NumberOfRvaAndSizes = 16u;
            }

            public void Write(Stream stream, OptionalHeaderDataDirectories dataDirectories)
            {
                // Write the optional header fields directly to the stream in the
                // correct order for PE32 / PE32+ without allocating a giant buffer.

                // Magic
                WriteLittleEndian(stream, (ushort)(IsPE32Plus ? 0x20b : 0x10b));

                // Linker versions
                stream.WriteByte(MajorLinkerVersion);
                stream.WriteByte(MinorLinkerVersion);

                // SizeOfCode
                WriteLittleEndian(stream, SizeOfCode);

                // SizeOfInitializedData
                WriteLittleEndian(stream, SizeOfInitializedData);

                // SizeOfUninitializedData
                WriteLittleEndian(stream, SizeOfUninitializedData);

                // AddressOfEntryPoint
                WriteLittleEndian(stream, AddressOfEntryPoint);

                // BaseOfCode
                WriteLittleEndian(stream, BaseOfCode);

                if (!IsPE32Plus)
                {
                    WriteLittleEndian(stream, BaseOfData);
                    WriteLittleEndian(stream, (uint)ImageBase);
                }
                else
                {
                    WriteLittleEndian(stream, ImageBase);
                }

                // SectionAlignment and FileAlignment
                WriteLittleEndian(stream, SectionAlignment);
                WriteLittleEndian(stream, FileAlignment);

                // Versioning
                WriteLittleEndian(stream, MajorOperatingSystemVersion);
                WriteLittleEndian(stream, MinorOperatingSystemVersion);
                WriteLittleEndian(stream, MajorImageVersion);
                WriteLittleEndian(stream, MinorImageVersion);
                WriteLittleEndian(stream, MajorSubsystemVersion);
                WriteLittleEndian(stream, MinorSubsystemVersion);

                // Win32VersionValue
                WriteLittleEndian<int>(stream, 0);

                // SizeOfImage
                WriteLittleEndian(stream, SizeOfImage);

                // SizeOfHeaders
                WriteLittleEndian(stream, SizeOfHeaders);

                // CheckSum
                WriteLittleEndian(stream, CheckSum);

                // Subsystem
                WriteLittleEndian(stream, (ushort)Subsystem);

                // DllCharacteristics
                WriteLittleEndian(stream, (ushort)DllCharacteristics);

                if (!IsPE32Plus)
                {
                    WriteLittleEndian(stream, (uint)SizeOfStackReserve);
                    WriteLittleEndian(stream, (uint)SizeOfStackCommit);
                    WriteLittleEndian(stream, (uint)SizeOfHeapReserve);
                    WriteLittleEndian(stream, (uint)SizeOfHeapCommit);
                }
                else
                {
                    WriteLittleEndian(stream, SizeOfStackReserve);
                    WriteLittleEndian(stream, SizeOfStackCommit);
                    WriteLittleEndian(stream, SizeOfHeapReserve);
                    WriteLittleEndian(stream, SizeOfHeapCommit);
                }

                WriteLittleEndian(stream, LoaderFlags);
                WriteLittleEndian(stream, NumberOfRvaAndSizes);

                // Data directories start after NumberOfRvaAndSizes for PE32+
                dataDirectories.WriteTo(stream, (int)NumberOfRvaAndSizes);
            }
        }

        private sealed class OptionalHeaderDataDirectories
        {
            private readonly (uint VirtualAddress, uint Size)[] _entries;

            public OptionalHeaderDataDirectories()
            {
                _entries = new (uint, uint)[16];
            }

            public void SetIfNonEmpty(int index, uint virtualAddress, uint size)
            {
                if ((uint)index >= (uint)_entries.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (size != 0)
                {
                    _entries[index] = (virtualAddress, size);
                }
            }

            public void WriteTo(Stream stream, int count)
            {
                int max = Math.Min(count, _entries.Length);
                for (int i = 0; i < max; i++)
                {
                    WriteLittleEndian(stream, _entries[i].VirtualAddress);
                    WriteLittleEndian(stream, _entries[i].Size);
                }
            }
        }

        // Named image directory indices for the PE optional header. This avoids
        // using magic numbers when populating data directories.
        private enum ImageDirectoryEntry
        {
            Export = 0,
            Import = 1,
            Resource = 2,
            Exception = 3,
            Security = 4,
            BaseRelocation = 5,
            Debug = 6,
            Architecture = 7,
            GlobalPtr = 8,
            TLS = 9,
            LoadConfig = 10,
            BoundImport = 11,
            IAT = 12,
            DelayImport = 13,
            CLRRuntimeHeader = 14,
            Reserved = 15,
        }

        private protected override void EmitSymbolTable(IDictionary<string, SymbolDefinition> definedSymbols, SortedSet<string> undefinedSymbols)
        {
            if (undefinedSymbols.Count > 0)
            {
                throw new NotSupportedException("PEObjectWriter does not support undefined symbols");
            }

            // Grab the defined symbols to resolve relocs during emit.
            _definedSymbols = new Dictionary<string, SymbolDefinition>(definedSymbols);
        }

        private protected override void EmitSectionsAndLayout()
        {
            SectionWriter exportDirectory = GetOrCreateSection(ExportDirectorySection);

            EmitExportDirectory(exportDirectory);

            // Grab section indicies.
            _pdataSectionIndex = GetOrCreateSection(ObjectNodeSection.PDataSection).SectionIndex;
            _debugSectionIndex = GetOrCreateSection(ObjectNodeSection.DebugDirectorySection).SectionIndex;
            _corMetaSectionIndex = GetOrCreateSection(ObjectNodeSection.CorMetaSection).SectionIndex;
            _exportSectionIndex = exportDirectory.SectionIndex;

            // Create the reloc section last. We write page offsets into it based on the virtual addresses of other sections
            // and we write it after the initial layout. Therefore, we need to have it after all other sections that it may reference,
            // as we can't move the emitted values later.
            _baseRelocSectionIndex = GetOrCreateSection(BaseRelocSection).SectionIndex;

            uint fileAlignment = 0x200;
            bool isWindowsOr32bit = _nodeFactory.Target.IsWindows || _nodeFactory.Target.PointerSize != 8;
            if (isWindowsOr32bit)
            {
                // To minimize wasted VA space on 32-bit systems (regardless of OS),
                // align file to page boundaries (presumed to be 4K)
                //
                // On Windows we use 4K file alignment (regardless of ptr size),
                // per requirements of memory mapping API (MapViewOfFile3, et al).
                // The alternative could be using the same approach as on Unix, but that would result in PEs
                // incompatible with OS loader. While that is not a problem on Unix, we do not want that on Windows.
                fileAlignment = PEHeaderConstants.SectionAlignment;
            }

            uint sectionAlignment = (uint)PEHeaderConstants.SectionAlignment;
            if (!isWindowsOr32bit)
            {
                // On 64bit Linux, we must match the bottom 12 bits of section RVA's to their file offsets. For this reason
                // we need the same alignment for both.
                //
                // In addition to that we specify section RVAs to be at least 64K apart, which is > page on most systems.
                // It ensures that the sections will not overlap when mapped from a singlefile bundle, which introduces a sub-page skew.
                //
                // Such format would not be accepted by OS loader on Windows, but it is not a problem on Unix.
                sectionAlignment = fileAlignment;
            }

            if (_requestedSectionAlignment != 0)
            {
                fileAlignment = (uint)_requestedSectionAlignment;
                sectionAlignment = (uint)_requestedSectionAlignment;
            }

            _peFileAlignment = fileAlignment;
            _peSectionAlignment = sectionAlignment;
            LayoutSections(recordFinalLayout: false, out _, out _, out _, out _, out _);
        }

        private void LayoutSections(bool recordFinalLayout, out ushort numberOfSections, out uint sizeOfHeaders, out uint sizeOfImage, out uint sizeOfInitializedData, out uint sizeOfCode)
        {
            bool isPE32Plus = _nodeFactory.Target.PointerSize == 8;
            ushort sizeOfOptionalHeader = (ushort)(isPE32Plus ? 0xF0 : 0xE0);

            numberOfSections = (ushort)_sections.Count;

            ushort numNonEmptySections = 0;
            foreach (SectionDefinition section in _sections)
            {
                // Only count sections with data or that contain uninitialized data
                if (section.Header.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData))
                {
                    numNonEmptySections++;
                }
                else if (section.Stream.Length != 0)
                {
                    numNonEmptySections++;
                }
            }

            numberOfSections = numNonEmptySections;

            // Compute headers size and align to file alignment
            uint sizeOfHeadersUnaligned = (uint)(DosHeaderSize + 4 + 20 + sizeOfOptionalHeader + 40 * numberOfSections);
            sizeOfHeaders = (uint)AlignmentHelper.AlignUp((int)sizeOfHeadersUnaligned, (int)_peFileAlignment);

            // Calculate layout for sections: raw file offsets and virtual addresses
            uint pointerToRawData = sizeOfHeaders;
            uint virtualAddress = (uint)AlignmentHelper.AlignUp((int)sizeOfHeaders, (int)_peSectionAlignment);

            sizeOfCode = 0;
            sizeOfInitializedData = 0;

            bool firstSection = true;
            foreach (SectionDefinition s in _sections)
            {
                CoffSectionHeader h = s.Header;
                h.SizeOfRawData = (uint)s.Stream.Length;
                uint requestedAlignment = GetSectionAlignment(h);
                uint rawAligned = h.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData)
                    ? 0u
                    : (uint)AlignmentHelper.AlignUp((int)h.SizeOfRawData, (int)uint.Max(requestedAlignment, _peFileAlignment));

                if (rawAligned != 0)
                {
                    h.PointerToRawData = pointerToRawData;
                    pointerToRawData += rawAligned;
                    h.SizeOfRawData = rawAligned;
                }
                else
                {
                    h.PointerToRawData = 0;
                }

                uint sectionAlignment = uint.Max(requestedAlignment, _peSectionAlignment);
                if (!_nodeFactory.Target.IsWindows)
                {
                    const int RVAAlign = 1 << RVABitsToMatchFilePos;
                    if (!firstSection)
                    {
                        // when assembly is stored in a singlefile bundle, an additional skew is introduced
                        // as the streams inside the bundle are not necessarily page aligned as we do not
                        // know the actual page size on the target system.
                        // We may need one page gap of unused VA space before the next section starts.
                        // We will assume the page size is <= RVAAlign
                        virtualAddress += RVAAlign;
                    }

                    virtualAddress = (uint)AlignmentHelper.AlignUp((int)virtualAddress, RVAAlign);

                    uint rvaAdjust = (h.PointerToRawData - virtualAddress) & (RVAAlign - 1);
                    virtualAddress += rvaAdjust;
                }
                else
                {
                    virtualAddress = (uint)AlignmentHelper.AlignUp((int)virtualAddress, (int)sectionAlignment);
                }

                uint virtualSize = (uint)AlignmentHelper.AlignUp((int)h.SizeOfRawData, (int)_peSectionAlignment);

                h.VirtualAddress = virtualAddress;
                h.VirtualSize = virtualSize;

                virtualAddress += virtualSize;

                if (h.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsCode))
                    sizeOfCode += h.SizeOfRawData;
                else if (!h.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData))
                    sizeOfInitializedData += h.SizeOfRawData;

                if (recordFinalLayout)
                {
                    // Use the stream length so we don't include any space that's appended just for alignment purposes.
                    // To ensure that we match the section indexes in _sections, we don't skip empty sections here
                    // even though we omit them in EmitObjectFile.
                    _outputSectionLayout.Add(new OutputSection(h.Name, h.VirtualAddress, h.PointerToRawData, (uint)s.Stream.Length));
                }
                firstSection = false;
            }

            sizeOfImage = (uint)AlignmentHelper.AlignUp((int)virtualAddress, (int)_peSectionAlignment);
        }

        private protected override unsafe void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            foreach (var reloc in relocationList)
            {
                if (!_resolvableRelocations.TryGetValue(sectionIndex, out List<SymbolicRelocation> resolvable))
                {
                    _resolvableRelocations[sectionIndex] = resolvable = [];
                }
                resolvable.Add(reloc);
                if (Relocation.GetFileRelocationType(reloc.Type) == reloc.Type)
                {
                    // Gather file-level relocations that need to go into the .reloc
                    // section. We collect entries grouped by 4KB page (page RVA ->
                    // list of (type<<12 | offsetInPage) WORD entries).
                    uint targetRva = _sections[sectionIndex].Header.VirtualAddress + (uint)reloc.Offset;
                    uint pageRva = targetRva & ~(PEHeaderConstants.SectionAlignment - 1);
                    ushort offsetInPage = (ushort)(targetRva & (PEHeaderConstants.SectionAlignment - 1));
                    ushort entry = (ushort)(((ushort)reloc.Type << 12) | offsetInPage);

                    if (!_baseRelocMap.TryGetValue(pageRva, out var list))
                    {
                        list = new List<ushort>();
                        _baseRelocMap.Add(pageRva, list);
                    }
                    list.Add(entry);
                }
            }
        }

        private void EmitExportDirectory(SectionWriter sectionWriter)
        {
            if (_exportedSymbolNames.Count == 0)
            {
                // No exports to emit.
                return;
            }

            List<string> exports = [.._exportedSymbolNames];

            exports.Sort(StringComparer.Ordinal);
            string moduleName = Path.GetFileName(_outputPath);
            const int minOrdinal = 1;

            StringTableBuilder exportsStringTable = new();

            exportsStringTable.ReserveString(moduleName);
            foreach (var exportName in exports)
            {
                exportsStringTable.ReserveString(exportName);
            }

            string exportsStringTableSymbol = GenerateSymbolNameForReloc("exportsStringTable");
            string addressTableSymbol = GenerateSymbolNameForReloc("addressTable");
            string namePointerTableSymbol = GenerateSymbolNameForReloc("namePointerTable");
            string ordinalPointerTableSymbol = GenerateSymbolNameForReloc("ordinalPointerTable");

            Debug.Assert(sectionWriter.Position == 0);

            // +0x00: reserved
            sectionWriter.WriteLittleEndian(0);
            // +0x04: time/date stamp
            sectionWriter.WriteLittleEndian(0);
            // +0x08: major version
            sectionWriter.WriteLittleEndian<ushort>(0);
            // +0x0A: minor version
            sectionWriter.WriteLittleEndian<ushort>(0);
            // +0x0C: DLL name RVA
            sectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_ADDR32NB, exportsStringTableSymbol, exportsStringTable.GetStringOffset(moduleName));
            // +0x10: ordinal base
            sectionWriter.WriteLittleEndian(minOrdinal);
            // +0x14: number of entries in the address table
            sectionWriter.WriteLittleEndian(exports.Count);
            // +0x18: number of name pointers
            sectionWriter.WriteLittleEndian(exports.Count);
            // +0x1C: export address table RVA
            sectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_ADDR32NB, addressTableSymbol);
            // +0x20: name pointer RVA
            sectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_ADDR32NB, namePointerTableSymbol);
            // +0x24: ordinal table RVA
            sectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_ADDR32NB, ordinalPointerTableSymbol);


            sectionWriter.EmitAlignment(4);
            sectionWriter.EmitSymbolDefinition(addressTableSymbol);
            foreach (var exportName in exports)
            {
                sectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_ADDR32NB, exportName);
            }

            sectionWriter.EmitAlignment(4);
            sectionWriter.EmitSymbolDefinition(namePointerTableSymbol);

            foreach (var exportName in exports)
            {
                sectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_ADDR32NB, exportsStringTableSymbol, exportsStringTable.GetStringOffset(exportName));
            }

            sectionWriter.EmitAlignment(4);
            sectionWriter.EmitSymbolDefinition(ordinalPointerTableSymbol);
            for (int i = 0; i < exports.Count; i++)
            {
                sectionWriter.WriteLittleEndian(checked((ushort)i));
            }

            sectionWriter.EmitSymbolDefinition(exportsStringTableSymbol);
            MemoryStream ms = new();
            exportsStringTable.Write(ms);
            sectionWriter.Write(ms.ToArray());

            string GenerateSymbolNameForReloc(string name)
            {
                return $"{_nodeFactory.NameMangler.CompilationUnitPrefix}__ExportDirectory__{name}";
            }
        }

        private void EmitRelocSectionData()
        {
            var writer = GetOrCreateSection(BaseRelocSection);
            Debug.Assert(writer.SectionIndex == _sections.Count - 1, "The .reloc section must be the last section we emit.");

            foreach (var kv in _baseRelocMap)
            {
                uint pageRva = kv.Key;
                List<ushort> entries = kv.Value;
                entries.Sort();

                int entriesSize = entries.Count * 2;
                int sizeOfBlock = 8 + entriesSize;
                sizeOfBlock = AlignmentHelper.AlignUp(sizeOfBlock, 4);

                writer.WriteLittleEndian(pageRva);
                writer.WriteLittleEndian((uint)sizeOfBlock);

                // Emit entries
                foreach (ushort e in entries)
                {
                    writer.WriteLittleEndian(e);
                }

                // Ensure block is 4-byte aligned
                writer.EmitAlignment(4);
            }

            CoffSectionHeader relocHeader = _sections[_baseRelocSectionIndex].Header;

            relocHeader.SectionCharacteristics |= SectionCharacteristics.MemDiscardable;
        }

        private protected override void EmitObjectFile(Stream outputFileStream)
        {
            if (_baseRelocMap.Count > 0)
            {
                EmitRelocSectionData();
            }

            // redo layout in case we made any additions during inital layout.
            LayoutSections(recordFinalLayout: true, out ushort numberOfSections, out uint sizeOfHeaders, out uint sizeOfImage, out uint sizeOfInitializedData, out uint sizeOfCode);

            outputFileStream.Write(DosHeader);
            Debug.Assert(DosHeader.Length == DosHeaderSize);
            outputFileStream.Write("PE\0\0"u8);

            bool isPE32Plus = _nodeFactory.Target.PointerSize == 8;
            ushort sizeOfOptionalHeader = (ushort)(isPE32Plus ? 0xF0 : 0xE0);

            Characteristics characteristics = Characteristics.ExecutableImage | Characteristics.Dll;
            characteristics |= isPE32Plus ? Characteristics.LargeAddressAware : Characteristics.Bit32Machine;

            Machine machine = _machine;
#if READYTORUN
            // On R2R, we encode the target OS into the machine bits to ensure we don't try running
            // linux or mac R2R code on Windows, or vice versa.
            machine = (Machine) ((ushort)machine ^ (ushort)_nodeFactory.Target.MachineOSOverrideFromTarget());
#endif

            // COFF File Header
            var coffHeader = new CoffHeader
            {
                Machine = machine,
                NumberOfSections = (uint)numberOfSections,
                TimeDateStamp = (uint)(_coffTimestamp ?? 0),
                PointerToSymbolTable = 0,
                NumberOfSymbols = 0,
                SizeOfOptionalHeader = sizeOfOptionalHeader,
                Characteristics = characteristics,
            };

            _coffHeaderOffset = outputFileStream.Position;

            coffHeader.Write(outputFileStream);

            var peOptional = new PEOptionalHeader(isPE32Plus)
            {
                SizeOfCode = sizeOfCode,
                SizeOfInitializedData = sizeOfInitializedData,
                AddressOfEntryPoint = 0u,
                BaseOfCode = 0u,
                BaseOfData = 0u,
                SizeOfImage = sizeOfImage,
                SizeOfHeaders = sizeOfHeaders,
                NumberOfRvaAndSizes = 16u,
            };

            // Set DLL characteristics similar to PEHeaderProvider.Create
            DllCharacteristics dllCharacteristics =
                DllCharacteristics.DynamicBase |
                DllCharacteristics.NxCompatible |
                DllCharacteristics.TerminalServerAware;
            if (isPE32Plus)
            {
                dllCharacteristics |= DllCharacteristics.HighEntropyVirtualAddressSpace;
            }
            else
            {
                dllCharacteristics |= DllCharacteristics.NoSeh;
            }

            peOptional.DllCharacteristics = dllCharacteristics;
            peOptional.SectionAlignment = _peSectionAlignment;
            peOptional.FileAlignment = _peFileAlignment;

            // Create data directories object and pass it to the optional header writer.
            // Entries are zeroed by default; callers may populate particular directories
            // before writing if needed.
            var dataDirs = new OptionalHeaderDataDirectories();
            // Populate data directories if present.
            if (_pdataSectionIndex != NoSectionIndex)
            {
                dataDirs.SetIfNonEmpty((int)ImageDirectoryEntry.Exception, (uint)_outputSectionLayout[_pdataSectionIndex].VirtualAddress, (uint)_outputSectionLayout[_pdataSectionIndex].Length);
            }
            if (_exportSectionIndex != NoSectionIndex)
            {
                dataDirs.SetIfNonEmpty((int)ImageDirectoryEntry.Export, (uint)_outputSectionLayout[_exportSectionIndex].VirtualAddress, (uint)_outputSectionLayout[_exportSectionIndex].Length);
            }
            if (_baseRelocSectionIndex != NoSectionIndex)
            {
                dataDirs.SetIfNonEmpty((int)ImageDirectoryEntry.BaseRelocation, (uint)_outputSectionLayout[_baseRelocSectionIndex].VirtualAddress, (uint)_outputSectionLayout[_baseRelocSectionIndex].Length);
            }
            if (_debugSectionIndex != NoSectionIndex)
            {
                dataDirs.SetIfNonEmpty((int)ImageDirectoryEntry.Debug, (uint)_outputSectionLayout[_debugSectionIndex].VirtualAddress, (uint)_outputSectionLayout[_debugSectionIndex].Length);
            }
            if (_corMetaSectionIndex != NoSectionIndex)
            {
                dataDirs.SetIfNonEmpty((int)ImageDirectoryEntry.CLRRuntimeHeader, (uint)_outputSectionLayout[_corMetaSectionIndex].VirtualAddress, (uint)_outputSectionLayout[_corMetaSectionIndex].Length);
            }
            peOptional.Write(outputFileStream, dataDirs);

            CoffStringTable stringTable = new();

            // Emit headers for each section in the order they appear in _sections
            for (int i = 0; i < _sections.Count; i++)
            {
                var hdr = _sections[i].Header;
                if (hdr.VirtualSize == 0 && !hdr.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData))
                {
                    // Don't emit empty sections into the binary
                    continue;
                }

                // The alignment flags aren't valid in PE image files, only in COFF object files.
                hdr.SectionCharacteristics &= ~SectionCharacteristics.AlignMask;
                hdr.Write(outputFileStream, stringTable);
            }

            // Write section content
            for (int i = 0; i < _sections.Count; i++)
            {
                SectionDefinition section = _sections[i];
                if (section.Header.VirtualSize != 0 && !section.Header.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData))
                {
                    Debug.Assert(outputFileStream.Position <= section.Header.PointerToRawData);
                    outputFileStream.Position = section.Header.PointerToRawData; // Pad to alignment
                    if (_resolvableRelocations.TryGetValue(i, out List<SymbolicRelocation> relocationsToResolve))
                    {
                        // Resolve all relocations we can't represent in a PE as we write out the data.
                        MemoryStream stream = new((int)section.Stream.Length);
                        section.Stream.Position = 0;
                        section.Stream.CopyTo(stream);
                        ResolveRelocations(i, relocationsToResolve, (long)peOptional.ImageBase, stream);
                        stream.Position = 0;
                        stream.CopyTo(outputFileStream);
                    }
                    else
                    {
                        section.Stream.Position = 0;
                        section.Stream.CopyTo(outputFileStream);
                    }
                }
            }

            // Align the output file size with the image (including trailing padding for section and file alignment).
            Debug.Assert(outputFileStream.Position <= sizeOfImage);
            outputFileStream.SetLength(sizeOfImage);
        }

        private protected override void EmitChecksumsForObject(Stream outputFileStream, List<ChecksumsToCalculate> checksumRelocations, ReadOnlySpan<byte> originalOutput)
        {
            base.EmitChecksumsForObject(outputFileStream, checksumRelocations, originalOutput);

            if (_coffTimestamp is null)
            {
                // If we were not provided a deterministic timestamp, generate one from a hash of the content.
                outputFileStream.Seek(_coffHeaderOffset + CoffHeader.TimeDateStampOffset(bigObj: false), SeekOrigin.Begin);
                using BinaryWriter writer = new(outputFileStream, Encoding.UTF8, leaveOpen: true);
                writer.Write(BlobContentId.FromHash(SHA256.HashData(originalOutput)).Stamp);
            }
        }

        private unsafe void ResolveRelocations(int sectionIndex, List<SymbolicRelocation> symbolicRelocations, long imageBase, MemoryStream stream)
        {
            foreach (SymbolicRelocation reloc in symbolicRelocations)
            {
                SymbolDefinition definedSymbol = _definedSymbols[reloc.SymbolName];
                uint relocOffset = checked((uint)(_sections[sectionIndex].Header.VirtualAddress + reloc.Offset));
                uint relocLength = (uint)Relocation.GetSize(reloc.Type);
                uint symbolImageOffset = checked((uint)(_sections[definedSymbol.SectionIndex].Header.VirtualAddress + definedSymbol.Value));

                fixed (byte* pData = GetRelocDataSpan(reloc))
                {
                    long addend = Relocation.ReadValue(reloc.Type, pData);
                    switch (reloc.Type)
                    {
                        case RelocType.IMAGE_REL_BASED_ABSOLUTE:
                            // No action required
                            break;

                        case RelocType.IMAGE_REL_BASED_THUMB_MOV32:
                        case RelocType.IMAGE_REL_BASED_DIR64:
                        case RelocType.IMAGE_REL_BASED_HIGHLOW:
                            // Write the ImageBase-relative value to be relocated at load time.
                            Relocation.WriteValue(reloc.Type, pData, symbolImageOffset + imageBase + addend);
                        break;
                        case RelocType.IMAGE_REL_BASED_ADDR32NB:
                            Relocation.WriteValue(reloc.Type, pData, symbolImageOffset + addend);
                            break;
                        case RelocType.IMAGE_REL_BASED_REL32:
                        case RelocType.IMAGE_REL_BASED_RELPTR32:
                            Relocation.WriteValue(reloc.Type, pData, symbolImageOffset - (relocOffset + relocLength) + addend);
                            break;
                        case RelocType.IMAGE_REL_FILE_ABSOLUTE:
                            long fileOffset = _sections[definedSymbol.SectionIndex].Header.PointerToRawData + definedSymbol.Value;
                            Relocation.WriteValue(reloc.Type, pData, fileOffset + addend);
                            break;
                        case RelocType.IMAGE_REL_BASED_THUMB_MOV32_PCREL:
                            const uint offsetCorrection = 12;
                            Relocation.WriteValue(reloc.Type, pData, symbolImageOffset - (relocOffset + offsetCorrection) + addend);
                            break;
                        case RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21:
                        {
                            if (addend != 0)
                            {
                                throw new NotSupportedException();
                            }
                            int sourcePageRVA = (int)(relocOffset & ~0xFFF);
                            long delta = (symbolImageOffset - sourcePageRVA >> 12) & 0x1f_ffff;
                            Relocation.WriteValue(reloc.Type, pData, delta);
                            break;
                        }
                        case RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A:
                            if (addend != 0)
                            {
                                throw new NotSupportedException();
                            }
                            Relocation.WriteValue(reloc.Type, pData, symbolImageOffset & 0xfff);
                            break;
                        case RelocType.IMAGE_REL_BASED_LOONGARCH64_PC:
                        {
                            if (addend != 0)
                            {
                                throw new NotSupportedException();
                            }
                            long delta = (symbolImageOffset - (relocOffset & ~0xfff) + ((symbolImageOffset & 0x800) << 1));
                            Relocation.WriteValue(reloc.Type, pData, delta);
                            break;
                        }
                        case RelocType.IMAGE_REL_BASED_LOONGARCH64_JIR:
                        case RelocType.IMAGE_REL_BASED_RISCV64_PC:
                        {
                            if (addend != 0)
                            {
                                throw new NotSupportedException();
                            }
                            long delta = symbolImageOffset - relocOffset;
                            Relocation.WriteValue(reloc.Type, pData, delta);
                            break;
                        }
                        default:
                            throw new NotSupportedException($"Unsupported relocation: {reloc.Type}");
                    }
                    WriteRelocDataSpan(reloc, pData);
                }

                Span<byte> GetRelocDataSpan(SymbolicRelocation reloc)
                {
                    stream.Position = reloc.Offset;
                    Span<byte> data = new byte[Relocation.GetSize(reloc.Type)];
                    stream.ReadExactly(data);
                    return data;
                }

                void WriteRelocDataSpan(SymbolicRelocation reloc, byte* data)
                {
                    stream.Position = reloc.Offset;
                    stream.Write(new Span<byte>(data, Relocation.GetSize(reloc.Type)));
                }
            }
        }

        private static unsafe void WriteLittleEndian<T>(Stream stream, T value)
            where T : IBinaryInteger<T>
        {
            Span<byte> buffer = stackalloc byte[sizeof(T)];
            value.WriteLittleEndian(buffer);
            stream.Write(buffer);
        }
    }
}
