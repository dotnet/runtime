// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
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
        internal const int DosHeaderSize = 0x80;

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

        // PE layout computed ahead of writing the file. These are populated by
        // EmitSectionsAndLayout so that data directories (e.g. exception table)
        // can be filled prior to writing the optional header.
        private uint _peSizeOfHeaders;
        private uint _peSizeOfImage;
        private uint _peSizeOfCode;
        private uint _peSizeOfInitializedData;
        private uint _peSectionAlignment;
        private uint _peFileAlignment;

        // Relocations that we can resolve at emit time (ie not file-based relocations).
        private List<List<SymbolicRelocation>> _resolvableRelocations = [];

        private int _pdataSectionIndex;
        private int _debugSectionIndex;
        private int _exportSectionIndex;
        private int _baseRelocSectionIndex;

        // Base relocation (.reloc) bookkeeping
        private readonly SortedDictionary<uint, List<ushort>> _baseRelocMap = new();

        // Emitted Symbol Table info
        private sealed record PESymbol(string Name, uint Offset);
        private readonly Dictionary<string, PESymbol> _definedPESymbols = new();
        private readonly List<PESymbol> _exportedPESymbols = new();
        private readonly int _sectionAlignment;

        private HashSet<string> _exportedSymbolNames = new();

        public PEObjectWriter(NodeFactory factory, ObjectWritingOptions options, int sectionAlignment, OutputInfoBuilder outputInfoBuilder)
            : base(factory, options, outputInfoBuilder)
        {
            _sectionAlignment = sectionAlignment;
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

            if (_sectionAlignment != 0)
            {
                UpdateSectionAlignment(sectionIndex, _sectionAlignment);
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

            public PEOptionalHeader(Machine machine)
            {
                IsPE32Plus = machine is Machine.Amd64 or Machine.Arm64;

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
                    WriteLittleEndian(stream, LoaderFlags);
                    WriteLittleEndian(stream, NumberOfRvaAndSizes);

                    // Data directories start at offset 0x60 for PE32
                    dataDirectories.WriteTo(stream, (int)NumberOfRvaAndSizes);
                }
                else
                {
                    WriteLittleEndian(stream, SizeOfStackReserve);
                    WriteLittleEndian(stream, SizeOfStackCommit);
                    WriteLittleEndian(stream, SizeOfHeapReserve);
                    WriteLittleEndian(stream, SizeOfHeapCommit);
                    WriteLittleEndian(stream, LoaderFlags);
                    WriteLittleEndian(stream, NumberOfRvaAndSizes);

                    // Data directories start after NumberOfRvaAndSizes for PE32+
                    dataDirectories.WriteTo(stream, (int)NumberOfRvaAndSizes);
                }
            }
        }

        private sealed class OptionalHeaderDataDirectories
        {
            private readonly (uint VirtualAddress, uint Size)[] _entries;

            public OptionalHeaderDataDirectories()
            {
                _entries = new (uint, uint)[16];
            }

            public void Set(int index, uint virtualAddress, uint size)
            {
                if ((uint)index >= (uint)_entries.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                _entries[index] = (virtualAddress, size);
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
            foreach (var (symbolName, symbolDefinition) in definedSymbols)
            {
                int sectionIdx = symbolDefinition.SectionIndex;
                if (sectionIdx < 0 || sectionIdx >= _sections.Count)
                    continue;

                uint targetRva = _sections[sectionIdx].Header.VirtualAddress + (uint)symbolDefinition.Value;
                PESymbol sym = new(symbolName, targetRva);
                _definedPESymbols.Add(symbolName, sym);

                if (_exportedSymbolNames.Contains(symbolName))
                {
                    _exportedPESymbols.Add(sym);
                }
            }
        }

        private protected override void EmitSectionsAndLayout()
        {
            // Compute layout for sections directly from the _sections list
            // without merging or grouping by name suffixes.
            ushort numberOfSections = (ushort)_sections.Count;
            bool isPE32Plus = _nodeFactory.Target.PointerSize == 8;
            ushort sizeOfOptionalHeader = (ushort)(isPE32Plus ? 0xF0 : 0xE0);

            int fileAlignment = 0x200;
            bool isWindowsOr32bit = _nodeFactory.Target.IsWindows || !isPE32Plus;
            if (isWindowsOr32bit)
            {
                fileAlignment = 0x1000;
            }

            uint sectionAlignment = (uint)PEHeaderConstants.SectionAlignment;
            if (!isWindowsOr32bit)
            {
                sectionAlignment = (uint)fileAlignment;
            }

            _peFileAlignment = (uint)fileAlignment;
            _peSectionAlignment = sectionAlignment;

            // Compute headers size and align to file alignment
            uint sizeOfHeadersUnaligned = (uint)(DosHeaderSize + 4 + 20 + sizeOfOptionalHeader + 40 * numberOfSections);
            uint sizeOfHeaders = (uint)AlignmentHelper.AlignUp((int)sizeOfHeadersUnaligned, (int)fileAlignment);

            // Calculate layout for sections: raw file offsets and virtual addresses
            uint pointerToRawData = sizeOfHeaders;
            uint virtualAddress = (uint)AlignmentHelper.AlignUp((int)sizeOfHeaders, (int)sectionAlignment);

            uint sizeOfCode = 0;
            uint sizeOfInitializedData = 0;

            for (int i = 0; i < _sections.Count; i++)
            {
                _resolvableRelocations.Add([]);
                SectionDefinition s = _sections[i];
                CoffSectionHeader h = s.Header;
                h.SizeOfRawData = (uint)s.Stream.Length;
                uint rawAligned = h.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData) ? 0u : (uint)AlignmentHelper.AlignUp((int)h.SizeOfRawData, (int)fileAlignment);

                uint offsetToStart = pointerToRawData;

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

                h.VirtualAddress = virtualAddress;
                h.VirtualSize = Math.Max(h.VirtualSize, h.SizeOfRawData);
                virtualAddress += (uint)AlignmentHelper.AlignUp((int)h.VirtualSize, (int)sectionAlignment);

                if (h.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsCode))
                    sizeOfCode += h.SizeOfRawData;
                else if (!h.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData))
                    sizeOfInitializedData += h.SizeOfRawData;

                _outputSectionLayout.Add(new OutputSection(h.Name, h.VirtualAddress, h.PointerToRawData, h.SizeOfRawData));

                if (h.Name == ".pdata")
                {
                    _pdataSectionIndex = i;
                }
                else if (h.Name == ".debug")
                {
                    _debugSectionIndex = i;
                }
            }

            // If we have exports, create an export data section (.edata) and place it after other sections.
            if (_exportedSymbolNames.Count > 0)
            {
                uint edataRva = virtualAddress;
                ExportDirectory exportDir = new(_exportedPESymbols, moduleName: "UNKNOWN", edataRva);
                SectionDefinition edataSection = exportDir.Section;
                CoffSectionHeader edataHeader = edataSection.Header;

                // Compute raw and virtual sizes and update pointers
                edataHeader.SizeOfRawData = (uint)edataSection.Stream.Length;
                uint edataRawAligned = (uint)AlignmentHelper.AlignUp((int)edataHeader.SizeOfRawData, (int)fileAlignment);
                edataHeader.PointerToRawData = pointerToRawData;
#pragma warning disable IDE0059 // Unnecessary assignment. We don't want to remove this and forget it if we add more sections after .edata.
                pointerToRawData += edataRawAligned;
#pragma warning restore IDE0059 // Unnecessary assignment
                edataHeader.SizeOfRawData = edataRawAligned;

                edataHeader.VirtualAddress = virtualAddress;
                edataHeader.VirtualSize = Math.Max(edataHeader.VirtualSize, (uint)edataSection.Stream.Length);
                virtualAddress += (uint)AlignmentHelper.AlignUp((int)edataHeader.VirtualSize, (int)sectionAlignment);

                // Add to sections and bookkeeping
                _sections.Add(edataSection);
                _sectionIndexToRelocations.Add(new List<SymbolicRelocation>());
                _resolvableRelocations.Add([]);

                sizeOfInitializedData += edataHeader.SizeOfRawData;

                // Set export directory fields for header
                _exportSectionIndex = _sections.Count - 1;
            }

            uint sizeOfImage = (uint)AlignmentHelper.AlignUp((int)virtualAddress, (int)sectionAlignment);

            _peSizeOfHeaders = sizeOfHeaders;
            _peSizeOfImage = sizeOfImage;
            _peSizeOfCode = sizeOfCode;
            _peSizeOfInitializedData = sizeOfInitializedData;
        }

        private protected override unsafe void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            foreach (var reloc in relocationList)
            {
                RelocType fileRelocType = Relocation.GetFileRelocationType(reloc.Type);

                if (fileRelocType != reloc.Type)
                {
                    _resolvableRelocations[sectionIndex].Add(reloc);
                }
                else
                {
                    // Gather file-level relocations that need to go into the .reloc
                    // section. We collect entries grouped by 4KB page (page RVA ->
                    // list of (type<<12 | offsetInPage) WORD entries).
                    uint targetRva = _sections[sectionIndex].Header.VirtualAddress + (uint)reloc.Offset;
                    uint pageRva = targetRva & ~0xFFFu;
                    ushort offsetInPage = (ushort)(targetRva & 0xFFFu);
                    ushort entry = (ushort)(((ushort)fileRelocType << 12) | offsetInPage);

                    if (!_baseRelocMap.TryGetValue(pageRva, out var list))
                    {
                        list = new List<ushort>();
                        _baseRelocMap.Add(pageRva, list);
                    }
                    list.Add(entry);
                }
            }
        }

        private void AddRelocSection()
        {
            var ms = new MemoryStream();

            foreach (var kv in _baseRelocMap)
            {
                uint pageRva = kv.Key;
                List<ushort> entries = kv.Value;
                entries.Sort();

                int entriesSize = entries.Count * 2;
                int sizeOfBlock = 8 + entriesSize;
                // Pad to 4-byte alignment as customary
                if ((sizeOfBlock & 3) != 0)
                    sizeOfBlock += 2;

                byte[] headerBuf = new byte[8];
                BinaryPrimitives.WriteUInt32LittleEndian(headerBuf.AsSpan(0, 4), pageRva);
                BinaryPrimitives.WriteUInt32LittleEndian(headerBuf.AsSpan(4, 4), (uint)sizeOfBlock);
                ms.Write(headerBuf, 0, headerBuf.Length);

                // Emit entries
                foreach (ushort e in entries)
                {
                    byte[] w = new byte[2];
                    BinaryPrimitives.WriteUInt16LittleEndian(w.AsSpan(), e);
                    ms.Write(w, 0, 2);
                }

                // Ensure block is 4-byte aligned by padding a WORD if needed
                if (((entriesSize) & 3) != 0)
                {
                    byte[] pad = new byte[2];
                    BinaryPrimitives.WriteUInt16LittleEndian(pad.AsSpan(), 0);
                    ms.Write(pad, 0, 2);
                }
            }

            // Create a new section for .reloc and compute its raw/virtual layout
            var relocHeader = new CoffSectionHeader
            {
                Name = ".reloc",
                SectionCharacteristics = SectionCharacteristics.MemRead | SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemDiscardable,
            };

            var relocSection = new SectionDefinition(relocHeader, ms, new List<CoffRelocation>(), null, null, ".reloc");

            // Compute pointer to raw data: find last used raw end and align
            uint lastRawEnd = 0;
            uint lastVEnd = 0;
            foreach (var s in _sections)
            {
                if (s.Header.PointerToRawData != 0)
                {
                    lastRawEnd = Math.Max(lastRawEnd, s.Header.PointerToRawData + s.Header.SizeOfRawData);
                }
                uint vEnd = s.Header.VirtualAddress + (uint)AlignmentHelper.AlignUp((int)s.Header.VirtualSize, (int)_peSectionAlignment);
                lastVEnd = Math.Max(lastVEnd, vEnd);
            }

            uint pointerToRawData = lastRawEnd == 0 ? (uint)AlignmentHelper.AlignUp((int)_peSizeOfHeaders, (int)_peFileAlignment) : lastRawEnd;
            relocHeader.PointerToRawData = pointerToRawData;
            relocHeader.SizeOfRawData = (uint)AlignmentHelper.AlignUp((int)ms.Length, (int)_peFileAlignment);
            relocHeader.VirtualAddress = (uint)AlignmentHelper.AlignUp((int)lastVEnd, (int)_peSectionAlignment);
            relocHeader.VirtualSize = (uint)ms.Length;

            _sections.Add(relocSection);
            _sectionIndexToRelocations.Add(new List<SymbolicRelocation>());
            _resolvableRelocations.Add([]);

            _outputSectionLayout.Add(new OutputSection(relocHeader.Name, relocHeader.VirtualAddress, relocHeader.PointerToRawData, relocHeader.SizeOfRawData));
            _baseRelocSectionIndex = _sections.Count - 1;
        }

        private protected override void EmitObjectFile(Stream outputFileStream)
        {
            if (_baseRelocMap.Count > 0)
            {
                AddRelocSection();
            }

            outputFileStream.Write(DosHeader);
            Debug.Assert(DosHeader.Length == DosHeaderSize);
            outputFileStream.Write("PE\0\0"u8);

            ushort numberOfSections = (ushort)_sections.Count;
            bool isPE32Plus = _nodeFactory.Target.PointerSize == 8;
            ushort sizeOfOptionalHeader = (ushort)(isPE32Plus ? 0xF0 : 0xE0);

            int fileAlignment = 0x200;
            bool isWindowsOr32bit = _nodeFactory.Target.IsWindows || !isPE32Plus;
            if (isWindowsOr32bit)
            {
                // To minimize wasted VA space on 32-bit systems (regardless of OS),
                // align file to page boundaries (presumed to be 4K)
                //
                // On Windows we use 4K file alignment (regardless of ptr size),
                // per requirements of memory mapping API (MapViewOfFile3, et al).
                // The alternative could be using the same approach as on Unix, but that would result in PEs
                // incompatible with OS loader. While that is not a problem on Unix, we do not want that on Windows.
                fileAlignment = 0x1000;
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
                sectionAlignment = (uint)fileAlignment;
            }

            // Use layout computed in EmitSectionsAndLayout
            uint sizeOfHeaders = _peSizeOfHeaders;
            uint sizeOfCode = _peSizeOfCode;
            uint sizeOfInitializedData = _peSizeOfInitializedData;
            uint sizeOfImage = _peSizeOfImage;

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
                Machine = _machine,
                NumberOfSections = (uint)numberOfSections,
                TimeDateStamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), // TODO: Make deterministic
                PointerToSymbolTable = 0,
                NumberOfSymbols = 0,
                SizeOfOptionalHeader = sizeOfOptionalHeader,
                Characteristics = characteristics,
            };

            coffHeader.Write(outputFileStream);

            var peOptional = new PEOptionalHeader(_machine)
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
            peOptional.SectionAlignment = sectionAlignment;
            peOptional.FileAlignment = (uint)fileAlignment;

            // Create data directories object and pass it to the optional header writer.
            // Entries are zeroed by default; callers may populate particular directories
            // before writing if needed.
            var dataDirs = new OptionalHeaderDataDirectories();
            // Populate data directories if present.
            if (_pdataSectionIndex != 0)
            {
                dataDirs.Set((int)ImageDirectoryEntry.Exception, (uint)_outputSectionLayout[_pdataSectionIndex].VirtualAddress, (uint)_outputSectionLayout[_pdataSectionIndex].Length);
            }
            if (_exportSectionIndex != 0)
            {
                dataDirs.Set((int)ImageDirectoryEntry.Export, (uint)_outputSectionLayout[_exportSectionIndex].VirtualAddress, (uint)_outputSectionLayout[_exportSectionIndex].Length);
            }
            if (_baseRelocSectionIndex != 0)
            {
                dataDirs.Set((int)ImageDirectoryEntry.BaseRelocation, (uint)_outputSectionLayout[_baseRelocSectionIndex].VirtualAddress, (uint)_outputSectionLayout[_baseRelocSectionIndex].Length);
            }
            if (_debugSectionIndex != 0)
            {
                dataDirs.Set((int)ImageDirectoryEntry.Debug, (uint)_outputSectionLayout[_debugSectionIndex].VirtualAddress, (uint)_outputSectionLayout[_debugSectionIndex].Length);
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
                    if (_resolvableRelocations[i] is not [])
                    {
                        // Resolve all relocations we can't represent in a PE as we write out the data.
                        MemoryStream stream = new((int)section.Stream.Length);
                        section.Stream.Position = 0;
                        section.Stream.CopyTo(stream);
                        ResolveNonImageBaseRelocations(i, _resolvableRelocations[i], stream);
                        stream.Position = 0;
                        section.Stream.CopyTo(outputFileStream);
                    }
                    else
                    {
                        section.Stream.Position = 0;
                        section.Stream.CopyTo(outputFileStream);
                    }
                }
            }

            // TODO: calculate PE checksum
            // TODO: calculate deterministic timestamp
        }

        private unsafe void ResolveNonImageBaseRelocations(int sectionIndex, List<SymbolicRelocation> symbolicRelocations, MemoryStream stream)
        {
            foreach (SymbolicRelocation reloc in symbolicRelocations)
            {
                switch (reloc.Type)
                {
                    case RelocType.IMAGE_REL_BASED_ADDR32NB:
                        fixed (byte* pData = GetRelocDataSpan(reloc))
                        {
                            long rva = _sections[sectionIndex].Header.VirtualAddress + reloc.Offset;
                            Relocation.WriteValue(reloc.Type, pData, rva);
                            WriteRelocDataSpan(reloc, pData);
                        }
                        break;
                    case RelocType.IMAGE_REL_BASED_REL32:
                    case RelocType.IMAGE_REL_BASED_RELPTR32:
                    {
                        PESymbol definedSymbol = _definedPESymbols[reloc.SymbolName];
                        fixed (byte* pData = GetRelocDataSpan(reloc))
                        {
                            long adjustedAddend = reloc.Addend;

                            adjustedAddend -= 4;

                            adjustedAddend += definedSymbol.Offset;
                            adjustedAddend += Relocation.ReadValue(reloc.Type, pData);
                            adjustedAddend -= reloc.Offset;

                            Relocation.WriteValue(reloc.Type, pData, adjustedAddend);
                            WriteRelocDataSpan(reloc, pData);
                        }
                        break;
                    }
                    case RelocType.IMAGE_REL_FILE_ABSOLUTE:
                        fixed (byte* pData = GetRelocDataSpan(reloc))
                        {
                            long rva = _sections[sectionIndex].Header.PointerToRawData + reloc.Offset;
                            Relocation.WriteValue(reloc.Type, pData, rva);
                            WriteRelocDataSpan(reloc, pData);
                        }
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported relocation: {reloc.Type}");
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

        private sealed class ExportDirectory
        {
            public SectionDefinition Section { get; }
            public int ExportDirectoryOffset { get; }
            public int ExportDirectorySize { get; }

            public ExportDirectory(IEnumerable<PESymbol> exportedPESymbols, string moduleName, uint edataRva)
            {
                var exports = exportedPESymbols.OrderBy(e => e.Name, StringComparer.Ordinal).ToArray();

                using var ms = new MemoryStream();

                // Emit names
                var nameRvas = new uint[exports.Length];
                for (int i = 0; i < exports.Length; i++)
                {
                    nameRvas[i] = edataRva + (uint)ms.Position;
                    ms.Write(System.Text.Encoding.UTF8.GetBytes(exports[i].Name));
                    ms.WriteByte(0);
                }

                // Module name
                uint moduleNameRva = edataRva + (uint)ms.Position;
                ms.Write(System.Text.Encoding.UTF8.GetBytes(moduleName));
                ms.WriteByte(0);

                // Align to 4
                while (ms.Position % 4 != 0)
                    ms.WriteByte(0);

                // Name pointer table
                uint namePointerTableRva = edataRva + (uint)ms.Position;
                int minOrdinal = 1;
                int count = exports.Length;
                int maxOrdinal = minOrdinal + count - 1;
                var addressTable = new int[maxOrdinal - minOrdinal + 1];

                for (int i = 0; i < exports.Length; i++)
                {
                    // name RVA
                    WriteLittleEndian(ms, (int)nameRvas[i]);
                    addressTable[i] = (int)exports[i].Offset;
                }

                // Ordinal table
                uint ordinalTableRva = edataRva + (uint)ms.Position;
                Span<byte> tmp2 = stackalloc byte[2];
                for (int i = 0; i < exports.Length; i++)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(tmp2, (ushort)(i));
                    ms.Write(tmp2);
                }

                // Address table
                while (ms.Position % 4 != 0)
                    ms.WriteByte(0);

                uint addressTableRva = edataRva + (uint)ms.Position;
                Span<byte> tmp4a = stackalloc byte[4];
                for (int i = 0; i < addressTable.Length; i++)
                {
                    WriteLittleEndian(ms, addressTable[i]);
                }

                // Export directory table
                while (ms.Position % 4 != 0)
                    ms.WriteByte(0);
                uint exportDirectoryTableRva = edataRva + (uint)ms.Position;

                // +0x00: reserved
                WriteLittleEndian(ms, 0);
                // +0x04: time/date stamp
                WriteLittleEndian(ms, 0);
                // +0x08: major version
                WriteLittleEndian<ushort>(ms, 0);
                // +0x0A: minor version
                WriteLittleEndian<ushort>(ms, 0);
                // +0x0C: DLL name RVA
                WriteLittleEndian(ms, (int)moduleNameRva);
                // +0x10: ordinal base
                WriteLittleEndian(ms, minOrdinal);
                // +0x14: number of entries in the address table
                WriteLittleEndian(ms, addressTable.Length);
                // +0x18: number of name pointers
                WriteLittleEndian(ms, exports.Length);
                // +0x1C: export address table RVA
                WriteLittleEndian(ms, (int)addressTableRva);
                // +0x20: name pointer RVA
                WriteLittleEndian(ms, (int)namePointerTableRva);
                // +0x24: ordinal table RVA
                WriteLittleEndian(ms, (int)ordinalTableRva);

                int exportDirectorySize = (int)(ms.Position - exportDirectoryTableRva);
                int exportDirectoryOffset = (int)exportDirectoryTableRva - (int)edataRva;

                var header = new CoffSectionHeader
                {
                    Name = ".edata",
                    SectionCharacteristics = SectionCharacteristics.MemRead | SectionCharacteristics.ContainsInitializedData
                };
                Section = new SectionDefinition(header, ms, new List<CoffRelocation>(), null, null, ".edata");
                ExportDirectoryOffset = exportDirectoryOffset;
                ExportDirectorySize = exportDirectorySize;
            }
        }
    }
}
