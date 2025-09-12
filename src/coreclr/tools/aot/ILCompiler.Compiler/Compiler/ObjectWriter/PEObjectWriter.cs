// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
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
        // PE layout computed ahead of writing the file. These are populated by
        // EmitSectionsAndLayout so that data directories (e.g. exception table)
        // can be filled prior to writing the optional header.
        private uint _peSizeOfHeaders;
        private uint _peSizeOfImage;
        private uint _peSizeOfCode;
        private uint _peSizeOfInitializedData;
        private uint _peSectionAlignment;
        private uint _peFileAlignment;
        private uint _pdataRva;
        private uint _pdataSize;
        // Grouping of object sections by base image section name. Populated by
        // EmitSectionsAndLayout so EmitObjectFile / EmitSections can consume
        // the grouping without recomputing it.
        private List<string> _groupOrder;
        private Dictionary<string, List<(SectionDefinition Section, int OriginalIndex)>> _groupMap;

        public PEObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
        }

        private protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, Stream sectionStream)
        {
            // COMDAT sections are not supported in PE files
            base.CreateSection(section, comdatName: null, symbolName, sectionStream);
        }

        private struct PEOptionalHeader
        {
            public bool IsPE32Plus { get; private set; }

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

            public void Write(FileStream stream, OptionalHeaderDataDirectories dataDirectories)
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

        private protected override void EmitSectionsAndLayout()
        {
            // Mirror layout logic previously performed inside EmitObjectFile so
            // that we can record section virtual addresses and sizes prior to
            // emitting the headers (so data directories can reference them).
            // Precompute grouping first so image header counts and the
            // header size calculation below reflect the final number of
            // image sections (base names without '$' suffix).
            _groupOrder = new List<string>();
            _groupMap = new Dictionary<string, List<(SectionDefinition Section, int OriginalIndex)>>(StringComparer.Ordinal);
            for (int i = 0; i < _sections.Count; i++)
            {
                var sec = _sections[i];
                string fullName = sec.Header.Name;
                int dollar = fullName.IndexOf('$');
                string baseName = dollar >= 0 ? fullName.Substring(0, dollar) : fullName;
                if (!_groupMap.TryGetValue(baseName, out var list))
                {
                    list = new List<(SectionDefinition, int)>();
                    _groupMap[baseName] = list;
                    _groupOrder.Add(baseName);
                }
                list.Add((sec, i));
            }

            // Build merged section definitions and grouped symbolic relocations
            var mergedSections = new List<SectionDefinition>();
            var groupedSymbolicRelocs = new List<List<SymbolicRelocation>>();

            foreach (string baseName in _groupOrder)
            {
                var contributions = _groupMap[baseName]
                    .OrderBy(c => c.Section.Header.Name, StringComparer.Ordinal)
                    .ToList();

                // Concatenate contribution streams
                long totalSize = 0;
                foreach (var (s, idx) in contributions)
                    totalSize += s.Stream.Length;

                var mergedStream = new MemoryStream((int)Math.Min(totalSize, int.MaxValue));

                uint contributionOffset = 0u;
                var groupSymbolic = new List<SymbolicRelocation>();

                foreach (var (s, origIdx) in contributions)
                {
                    s.Stream.Position = 0;
                    s.Stream.CopyTo(mergedStream);

                    // Adjust symbolic relocations from the original section into
                    // the group's address space
                    foreach (var symRel in _sectionIndexToRelocations[origIdx])
                    {
                        groupSymbolic.Add(new SymbolicRelocation(symRel.Offset + contributionOffset, symRel.Type, symRel.SymbolName, symRel.Addend));
                    }

                    contributionOffset += (uint)s.Stream.Length;
                }

                // Create a new header for the merged section using the first contribution as a template
                var template = contributions[0].Section.Header;
                var mergedHeader = new CoffSectionHeader
                {
                    Name = baseName,
                    SectionCharacteristics = contributions.Aggregate((SectionCharacteristics)0, (acc, t) => acc | t.Section.Header.SectionCharacteristics)
                };

                mergedSections.Add(new SectionDefinition(mergedHeader, mergedStream, new List<CoffRelocation>(), contributions[0].Section.ComdatName, contributions[0].Section.SymbolName, baseName));
                groupedSymbolicRelocs.Add(groupSymbolic);
            }

            // Replace section lists with grouped versions for subsequent layout & relocation emission
            _sections.Clear();
            _sections.AddRange(mergedSections);
            _sectionIndexToRelocations.Clear();
            _sectionIndexToRelocations.AddRange(groupedSymbolicRelocs);

            ushort numberOfSections = (ushort)_groupOrder.Count;
            bool isPE32Plus = _nodeFactory.Target.PointerSize == 8;
            ushort sizeOfOptionalHeader = (ushort)(isPE32Plus ? 0xF0 : 0xE0);

            // Determine file and section alignment following the same rules as
            // the image writer.
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
            uint sizeOfHeadersUnaligned = (uint)(0x80 + 4 + 20 + sizeOfOptionalHeader + 40 * numberOfSections);
            uint sizeOfHeaders = (uint)AlignmentHelper.AlignUp((int)sizeOfHeadersUnaligned, (int)fileAlignment);

            // Calculate layout for sections: raw file offsets and virtual addresses
            uint pointerToRawData = sizeOfHeaders;
            uint virtualAddress = (uint)AlignmentHelper.AlignUp((int)sizeOfHeaders, (int)sectionAlignment);

            uint sizeOfCode = 0;
            uint sizeOfInitializedData = 0;

            for (int i = 0; i < _sections.Count; i++)
            {
                var s = _sections[i];
                var h = s.Header;
                h.SizeOfRawData = (uint)s.Stream.Length;
                uint rawAligned = h.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData) ? 0u : (uint)AlignmentHelper.AlignUp((int)h.SizeOfRawData, (int)fileAlignment);

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

                if (h.Name == ".pdata")
                {
                    _pdataRva = h.VirtualAddress;
                    _pdataSize = h.VirtualSize != 0 ? h.VirtualSize : h.SizeOfRawData;
                }
            }

            uint sizeOfImage = (uint)AlignmentHelper.AlignUp((int)virtualAddress, (int)sectionAlignment);

            _peSizeOfHeaders = sizeOfHeaders;
            _peSizeOfImage = sizeOfImage;
            _peSizeOfCode = sizeOfCode;
            _peSizeOfInitializedData = sizeOfInitializedData;
        }

        private protected override void EmitObjectFile(string objectFilePath)
        {
            using var stream = new FileStream(objectFilePath, FileMode.Create, FileAccess.Write);

            // Write a minimal DOS stub and set the e_lfanew to 0x80 where we'll
            // place the PE headers.
            var dos = new byte[0x80];
            "MZ"u8.CopyTo(dos);
            BinaryPrimitives.WriteInt32LittleEndian(dos.AsSpan(0x3c), 0x80);
            stream.Write(dos);

            ushort numberOfSections = (ushort)_groupOrder.Count;
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

            // Write PE Signature at e_lfanew (0x80)
            stream.Position = 0x80;
            stream.Write("PE\0\0"u8);

            Characteristics characteristics = Characteristics.ExecutableImage | Characteristics.Dll;
            characteristics |= isPE32Plus ? Characteristics.LargeAddressAware : Characteristics.Bit32Machine;

            // COFF File Header (use the shared CoffHeader type)
            var coffHeader = new CoffHeader
            {
                Machine = _machine,
                NumberOfSections = (uint)numberOfSections,
                TimeDateStamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                PointerToSymbolTable = 0,
                NumberOfSymbols = 0,
                SizeOfOptionalHeader = sizeOfOptionalHeader,
                Characteristics = characteristics,
            };

            coffHeader.Write(stream);

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
            // Populate exception table with .pdata if present.
            if (_pdataSize != 0)
            {
                dataDirs.Set((int)ImageDirectoryEntry.Exception, _pdataRva, _pdataSize);
            }
            peOptional.Write(stream, dataDirs);

            CoffStringTable stringTable = new();
            int sectionIndex = 0;
            // Emit headers for each group (baseName order preserved)
            foreach (string baseName in _groupOrder)
            {
                var contributions = _groupMap[baseName];
                // The header we will emit for this image section is based on
                // the first contribution's header but needs to have the
                // section name without the '$' suffix.
                var groupHeader = contributions[0].Section.Header;
                string savedName = groupHeader.Name;
                groupHeader.Name = baseName;
                groupHeader.Write(stream, stringTable);

                // Restore the name to avoid affecting any other logic that
                // may rely on the original value stored in the SectionDefinition.
                groupHeader.Name = savedName;

                sectionIndex++;
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
