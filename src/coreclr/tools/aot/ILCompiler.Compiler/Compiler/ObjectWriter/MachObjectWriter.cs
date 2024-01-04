// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using static ILCompiler.DependencyAnalysis.RelocType;
using static ILCompiler.ObjectWriter.MachNative;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Mach-O object file format writer for Apple macOS and iOS-like targets.
    /// </summary>
    /// <remarks>
    /// Old version of the Mach-O file format specification is mirrored at
    /// https://github.com/aidansteele/osx-abi-macho-file-format-reference.
    ///
    /// There are some notable differences when compared to ELF or COFF:
    /// - The maximum number of sections in object file is limited to 255.
    /// - Sections are subdivided by their symbols and treated by the
    ///   linker as subsections (often referred to as atoms by the linker).
    ///
    /// The consequences of these design decisions is the COMDAT sections are
    /// modeled in entirely different way. Dead code elimination works on the
    /// atom level, so relative relocations within the same section have to be
    /// preserved.
    ///
    /// Debug information uses the standard DWARF format. It is, however, not
    /// linked into the intermediate executable files. Instead the linker creates
    /// a map between the final executable and the object files. Debuggers like
    /// lldb then use this map to read the debug information from the object
    /// file directly. As a consequence the DWARF information is not generated
    /// with relocations for the DWARF sections themselves since it's never
    /// needed.
    ///
    /// While Mach-O uses the DWARF exception handling information for unwind
    /// tables it also supports a compact representation for common prolog types.
    /// Unofficial reference of the format can be found at
    /// https://faultlore.com/blah/compact-unwinding/. It's necessary to emit
    /// at least the stub entries pointing to the DWARF information but due
    /// to limits in the linked file format it's advisable to use the compact
    /// encoding whenever possible.
    ///
    /// The Apple linker is extremely picky in which relocation types are allowed
    /// inside the DWARF sections, both for debugging and exception handling.
    /// </remarks>
    internal sealed class MachObjectWriter : UnixObjectWriter
    {
        private sealed record CompactUnwindCode(string PcStartSymbolName, uint PcLength, uint Code, string LsdaSymbolName = null, string PersonalitySymbolName = null);

        private readonly TargetOS _targetOS;
        private readonly uint _cpuType;
        private readonly uint _cpuSubType;
        private readonly List<MachSection> _sections = new();

        // Exception handling sections
        private MachSection _compactUnwindSection;
        private MemoryStream _compactUnwindStream;
        private readonly List<CompactUnwindCode> _compactUnwindCodes = new();
        private readonly uint _compactUnwindDwarfCode;

        // Symbol table
        private readonly Dictionary<string, uint> _symbolNameToIndex = new();
        private readonly List<MachSymbol> _symbolTable = new();
        private readonly MachDynamicLinkEditSymbolTable _dySymbolTable = new();

        public MachObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
            switch (factory.Target.Architecture)
            {
                case TargetArchitecture.ARM64:
                    _cpuType = CPU_TYPE_ARM64;
                    _cpuSubType = CPU_SUBTYPE_ARM64_ALL;
                    _compactUnwindDwarfCode = 0x3_00_00_00u;
                    break;
                case TargetArchitecture.X64:
                    _cpuType = CPU_TYPE_X86_64;
                    _cpuSubType = CPU_SUBTYPE_X86_64_ALL;
                    _compactUnwindDwarfCode = 0x4_00_00_00u;
                    break;
                default:
                    throw new NotSupportedException("Unsupported architecture");
            }

            _targetOS = factory.Target.OperatingSystem;
        }

        private protected override void EmitSectionsAndLayout()
        {
            // Layout sections. At this point we don't really care if the file offsets are correct
            // but we need to compute the virtual addresses to populate the symbol table.
            uint fileOffset = 0;
            LayoutSections(ref fileOffset, out _, out _);

            // Generate section base symbols. The section symbols are used for PC relative relocations
            // to subtract the base of the section, and in DWARF to emit section relative relocations.
            byte sectionIndex = 0;
            foreach (MachSection section in _sections)
            {
                var machSymbol = new MachSymbol
                {
                    Name = $"lsection{sectionIndex}",
                    Section = section,
                    Value = section.VirtualAddress,
                    Descriptor = 0,
                    Type = N_SECT,
                };
                _symbolTable.Add(machSymbol);
                _symbolNameToIndex[machSymbol.Name] = sectionIndex;
                sectionIndex++;
            }
        }

        private void LayoutSections(ref uint fileOffset, out uint segmentFileSize, out ulong segmentSize)
        {
            ulong virtualAddress = 0;
            byte sectionIndex = 1;

            segmentFileSize = 0;
            segmentSize = 0;
            foreach (MachSection section in _sections)
            {
                uint alignment = 1u << (int)section.Log2Alignment;

                fileOffset = (fileOffset + alignment - 1) & ~(alignment - 1);
                virtualAddress = (virtualAddress + alignment - 1) & ~(alignment - 1);

                if (section.IsInFile)
                {
                    section.FileOffset = fileOffset;
                    fileOffset += (uint)section.Size;
                    segmentFileSize = Math.Max(segmentFileSize, fileOffset);
                }
                else
                {
                    // The offset is unused for virtual sections.
                    section.FileOffset = 0;
                }

                section.VirtualAddress = virtualAddress;
                virtualAddress += section.Size;

                section.SectionIndex = sectionIndex;
                sectionIndex++;

                segmentSize = Math.Max(segmentSize, virtualAddress);
            }

            // ...and the relocation tables
            foreach (MachSection section in _sections)
            {
                section.RelocationOffset = fileOffset;
                fileOffset += section.NumberOfRelocationEntries * 8;
            }
        }

        private protected override void EmitObjectFile(string objectFilePath)
        {
            _sections.Add(_compactUnwindSection);

            // Segment + sections
            uint loadCommandsCount = 1;
            uint loadCommandsSize = (uint)(MachSegment64Header.HeaderSize + _sections.Count * MachSection.HeaderSize);
            // Symbol table
            loadCommandsCount += 2;
            loadCommandsSize += (uint)(MachSymbolTableCommandHeader.HeaderSize + MachDynamicLinkEditSymbolTable.HeaderSize);
            // Build version
            loadCommandsCount++;
            loadCommandsSize += (uint)MachBuildVersionCommandHeader.HeaderSize;

            // We added the compact unwinding section, debug sections, and relocations,
            // so re-run the layout and this time calculate with the correct file offsets.
            uint fileOffset = (uint)MachHeader64.HeaderSize + loadCommandsSize;
            uint segmentFileOffset = fileOffset;
            LayoutSections(ref fileOffset, out uint segmentFileSize, out ulong segmentSize);

            using var outputFileStream = new FileStream(objectFilePath, FileMode.Create);

            MachHeader64 machHeader = new MachHeader64
            {
                CpuType = _cpuType,
                CpuSubType = _cpuSubType,
                FileType = MH_OBJECT,
                NumberOfCommands = loadCommandsCount,
                SizeOfCommands = loadCommandsSize,
                Flags = MH_SUBSECTIONS_VIA_SYMBOLS,
                Reserved = 0,
            };
            machHeader.Write(outputFileStream);

            MachSegment64Header machSegment64Header = new MachSegment64Header
            {
                Name = "",
                InitialProtection = VM_PROT_READ | VM_PROT_WRITE | VM_PROT_EXECUTE,
                MaximumProtection = VM_PROT_READ | VM_PROT_WRITE | VM_PROT_EXECUTE,
                Address = 0,
                Size = segmentSize,
                FileOffset = segmentFileOffset,
                FileSize = segmentFileSize,
                NumberOfSections = (uint)_sections.Count,
            };
            machSegment64Header.Write(outputFileStream);

            foreach (MachSection section in _sections)
            {
                section.WriteHeader(outputFileStream);
            }

            MachStringTable stringTable = new();
            foreach (MachSymbol symbol in _symbolTable)
            {
                stringTable.ReserveString(symbol.Name);
            }

            uint symbolTableOffset = fileOffset;
            uint stringTableOffset = symbolTableOffset + ((uint)_symbolTable.Count * 16u);
            MachSymbolTableCommandHeader symbolTableHeader = new MachSymbolTableCommandHeader
            {
                SymbolTableOffset = symbolTableOffset,
                NumberOfSymbols = (uint)_symbolTable.Count,
                StringTableOffset = stringTableOffset,
                StringTableSize = stringTable.Size,
            };
            symbolTableHeader.Write(outputFileStream);
            _dySymbolTable.Write(outputFileStream);

            // Build version
            MachBuildVersionCommandHeader buildVersion = new MachBuildVersionCommandHeader
            {
                SdkVersion = 0x10_00_00u, // 16.0.0
            };
            switch (_targetOS)
            {
                case TargetOS.OSX:
                    buildVersion.Platform = PLATFORM_MACOS;
                    buildVersion.MinimumPlatformVersion = 0x0A_0C_00; // 10.12.0
                    break;

                case TargetOS.MacCatalyst:
                    buildVersion.Platform = PLATFORM_MACCATALYST;
                    buildVersion.MinimumPlatformVersion = _cpuType switch
                    {
                        CPU_TYPE_X86_64 => 0x0D_05_00u, // 13.5.0
                        _ => 0x0E_02_00u, // 14.2.0
                    };
                    break;

                case TargetOS.iOS:
                case TargetOS.iOSSimulator:
                case TargetOS.tvOS:
                case TargetOS.tvOSSimulator:
                    buildVersion.Platform = _targetOS switch
                    {
                        TargetOS.iOS => PLATFORM_IOS,
                        TargetOS.iOSSimulator => PLATFORM_IOSSIMULATOR,
                        TargetOS.tvOS => PLATFORM_TVOS,
                        TargetOS.tvOSSimulator => PLATFORM_TVOSSIMULATOR,
                        _ => 0,
                    };
                    buildVersion.MinimumPlatformVersion = 0x0B_00_00; // 11.0.0
                    break;
            }
            buildVersion.Write(outputFileStream);

            // Write section contents
            foreach (MachSection section in _sections)
            {
                if (section.IsInFile)
                {
                    outputFileStream.Position = (long)section.FileOffset;
                    section.Stream.Position = 0;
                    section.Stream.CopyTo(outputFileStream);
                }
            }

            // Write relocations
            foreach (MachSection section in _sections)
            {
                if (section.NumberOfRelocationEntries > 0)
                {
                    foreach (MachRelocation relocation in section.Relocations)
                    {
                        relocation.Write(outputFileStream);
                    }
                }
            }

            // Write string and symbol table
            outputFileStream.Position = symbolTableOffset;
            foreach (MachSymbol symbol in _symbolTable)
            {
                symbol.Write(outputFileStream, stringTable);
            }
            stringTable.Write(outputFileStream);
        }

        private protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, Stream sectionStream)
        {
            string segmentName = section.Name switch
            {
                "rdata" => "__TEXT",
                ".eh_frame" => "__TEXT",
                _ => section.Type switch
                {
                    SectionType.Executable => "__TEXT",
                    SectionType.Debug => "__DWARF",
                    _ => "__DATA"
                }
            };

            string sectionName = section.Name switch
            {
                "text" => "__text",
                "data" => "__data",
                "rdata" => "__const",
                "bss" => "__bss",
                ".eh_frame" => "__eh_frame",
                ".debug_info" => "__debug_info",
                ".debug_abbrev" => "__debug_abbrev",
                ".debug_ranges" => "__debug_ranges",
                ".debug_aranges" => "__debug_aranges",
                ".debug_str" => "__debug_str",
                ".debug_line" => "__debug_line",
                ".debug_loc" => "__debug_loc",
                _ => section.Name
            };

            uint flags = section.Name switch
            {
                "bss" => S_ZEROFILL,
                ".eh_frame" => S_COALESCED,
                _ => section.Type == SectionType.Uninitialized ? S_ZEROFILL : S_REGULAR
            };

            flags |= section.Name switch
            {
                ".dotnet_eh_table" => S_ATTR_DEBUG,
                ".eh_frame" => S_ATTR_LIVE_SUPPORT | S_ATTR_STRIP_STATIC_SYMS | S_ATTR_NO_TOC,
                _ => section.Type switch
                {
                    SectionType.Executable => S_ATTR_SOME_INSTRUCTIONS | S_ATTR_PURE_INSTRUCTIONS,
                    SectionType.Debug => S_ATTR_DEBUG,
                    _ => 0
                }
            };

            MachSection machSection = new MachSection(segmentName, sectionName, sectionStream)
            {
                Log2Alignment = 1,
                Flags = flags,
            };

            int sectionIndex = _sections.Count;
            _sections.Add(machSection);

            base.CreateSection(section, comdatName, symbolName ?? $"lsection{sectionIndex}", sectionStream);
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
            MachSection machSection = _sections[sectionIndex];
            Debug.Assert(BitOperations.IsPow2(alignment));
            machSection.Log2Alignment = Math.Max(machSection.Log2Alignment, (uint)BitOperations.Log2((uint)alignment));
        }

        protected internal override unsafe void EmitRelocation(
            int sectionIndex,
            long offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            long addend)
        {
            // Mach-O doesn't use relocations between DWARF sections, so embed the offsets directly
            if (relocType is IMAGE_REL_BASED_DIR64 or IMAGE_REL_BASED_HIGHLOW &&
                _sections[sectionIndex].IsDwarfSection)
            {
                // DWARF section to DWARF section relocation
                if (symbolName.StartsWith('.'))
                {
                    switch (relocType)
                    {
                        case IMAGE_REL_BASED_DIR64:
                            BinaryPrimitives.WriteInt64LittleEndian(data, addend);
                            break;
                        case IMAGE_REL_BASED_HIGHLOW:
                            BinaryPrimitives.WriteUInt32LittleEndian(data, (uint)addend);
                            break;
                        default:
                            throw new NotSupportedException("Unsupported relocation in debug section");
                    }
                    return;
                }
                // DWARF section to code/data section relocation
                else
                {
                    Debug.Assert(IsSectionSymbolName(symbolName));
                    Debug.Assert(relocType == IMAGE_REL_BASED_DIR64);
                    int targetSectionIndex = (int)_symbolNameToIndex[symbolName];
                    BinaryPrimitives.WriteUInt64LittleEndian(data, _sections[targetSectionIndex].VirtualAddress + (ulong)addend);
                    base.EmitRelocation(sectionIndex, offset, data, relocType, symbolName, addend);
                }

                return;
            }

            switch (relocType)
            {
                case IMAGE_REL_BASED_ARM64_PAGEBASE_REL21:
                case IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A:
                    // Addend is handled through ARM64_RELOC_ADDEND
                    break;

                case IMAGE_REL_BASED_RELPTR32:
                    if (_cpuType == CPU_TYPE_ARM64 || sectionIndex == EhFrameSectionIndex)
                    {
                        // On ARM64 we need to represent PC relative relocations as
                        // subtraction and the PC offset is baked into the addend.
                        // On x64, ld64 requires X86_64_RELOC_SUBTRACTOR + X86_64_RELOC_UNSIGNED
                        // for DWARF .eh_frame section.
                        BinaryPrimitives.WriteInt32LittleEndian(
                            data,
                            BinaryPrimitives.ReadInt32LittleEndian(data) +
                            (int)(addend - offset));
                    }
                    else
                    {
                        addend += 4;
                        if (addend != 0)
                        {
                            BinaryPrimitives.WriteInt32LittleEndian(
                                data,
                                BinaryPrimitives.ReadInt32LittleEndian(data) +
                                (int)addend);
                        }
                    }
                    addend = 0;
                    break;

                default:
                    if (addend != 0)
                    {
                        fixed (byte *pData = data)
                        {
                            long inlineValue = Relocation.ReadValue(relocType, (void*)pData);
                            Relocation.WriteValue(relocType, (void*)pData, inlineValue + addend);
                            addend = 0;
                        }
                    }
                    break;
            }

            base.EmitRelocation(sectionIndex, offset, data, relocType, symbolName, addend);
        }

        private protected override void EmitSymbolTable(
            IDictionary<string, SymbolDefinition> definedSymbols,
            SortedSet<string> undefinedSymbols)
        {
            // We already emitted symbols for all non-debug sections in EmitSectionsAndLayout,
            // these symbols are local and we need to account for them.
            uint symbolIndex = (uint)_symbolTable.Count;
            _dySymbolTable.LocalSymbolsIndex = 0;
            _dySymbolTable.LocalSymbolsCount = symbolIndex;

            // Sort and insert all defined symbols
            var sortedDefinedSymbols = new List<MachSymbol>(definedSymbols.Count);
            foreach ((string name, SymbolDefinition definition) in definedSymbols)
            {
                MachSection section = _sections[definition.SectionIndex];
                sortedDefinedSymbols.Add(new MachSymbol
                {
                    Name = name,
                    Section = section,
                    Value = section.VirtualAddress + (ulong)definition.Value,
                    Descriptor = 0,
                    Type = N_SECT | N_EXT,
                });
            }
            sortedDefinedSymbols.Sort((symA, symB) => string.CompareOrdinal(symA.Name, symB.Name));
            foreach (MachSymbol definedSymbol in sortedDefinedSymbols)
            {
                _symbolTable.Add(definedSymbol);
                _symbolNameToIndex[definedSymbol.Name] = symbolIndex;
                symbolIndex++;
            }

            _dySymbolTable.ExternalSymbolsIndex = _dySymbolTable.LocalSymbolsCount;
            _dySymbolTable.ExternalSymbolsCount = (uint)definedSymbols.Count;

            uint savedSymbolIndex = symbolIndex;
            foreach (string externSymbol in undefinedSymbols)
            {
                if (!_symbolNameToIndex.ContainsKey(externSymbol))
                {
                    var machSymbol = new MachSymbol
                    {
                        Name = externSymbol,
                        Section = null,
                        Value = 0,
                        Descriptor = 0,
                        Type = N_UNDF | N_EXT,
                    };
                    _symbolTable.Add(machSymbol);
                    _symbolNameToIndex[externSymbol] = symbolIndex;
                    symbolIndex++;
                }
            }

            _dySymbolTable.UndefinedSymbolsIndex = _dySymbolTable.LocalSymbolsCount + _dySymbolTable.ExternalSymbolsCount;
            _dySymbolTable.UndefinedSymbolsCount = symbolIndex - savedSymbolIndex;

            EmitCompactUnwindTable(definedSymbols);
        }

        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            if (_cpuType == CPU_TYPE_ARM64)
            {
                EmitRelocationsArm64(sectionIndex, relocationList);
            }
            else
            {
                EmitRelocationsX64(sectionIndex, relocationList);
            }
        }

        private void EmitRelocationsX64(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            ICollection<MachRelocation> sectionRelocations = _sections[sectionIndex].Relocations;

            relocationList.Reverse();
            foreach (SymbolicRelocation symbolicRelocation in relocationList)
            {
                uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];

                if (symbolicRelocation.Type == IMAGE_REL_BASED_DIR64)
                {
                    bool isExternal = !IsSectionSymbolName(symbolicRelocation.SymbolName);
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = isExternal ? symbolIndex : symbolIndex + 1,
                            Length = 8,
                            RelocationType = X86_64_RELOC_UNSIGNED,
                            IsExternal = isExternal,
                            IsPCRelative = false,
                        });
                }
                else if (symbolicRelocation.Type == IMAGE_REL_BASED_RELPTR32 && sectionIndex == EhFrameSectionIndex)
                {
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = (uint)sectionIndex,
                            Length = 4,
                            RelocationType = X86_64_RELOC_SUBTRACTOR,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = X86_64_RELOC_UNSIGNED,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                }
                else if (symbolicRelocation.Type is IMAGE_REL_BASED_RELPTR32 or IMAGE_REL_BASED_REL32)
                {
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = X86_64_RELOC_BRANCH,
                            IsExternal = true,
                            IsPCRelative = true,
                        });
                }
                else
                {
                    throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type);
                }
            }
        }

        private void EmitRelocationsArm64(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            ICollection<MachRelocation> sectionRelocations = _sections[sectionIndex].Relocations;

            relocationList.Reverse();
            foreach (SymbolicRelocation symbolicRelocation in relocationList)
            {
                uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];

                if (symbolicRelocation.Type == IMAGE_REL_BASED_ARM64_BRANCH26)
                {
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = ARM64_RELOC_BRANCH26,
                            IsExternal = true,
                            IsPCRelative = true,
                        });
                }
                else if (symbolicRelocation.Type is IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 or IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A)
                {
                    if (symbolicRelocation.Addend != 0)
                    {
                        sectionRelocations.Add(
                            new MachRelocation
                            {
                                Address = (int)symbolicRelocation.Offset,
                                SymbolOrSectionIndex = (uint)symbolicRelocation.Addend,
                                Length = 4,
                                RelocationType = ARM64_RELOC_ADDEND,
                                IsExternal = false,
                                IsPCRelative = false,
                            });
                    }

                    byte type = symbolicRelocation.Type switch
                    {
                        IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 => ARM64_RELOC_PAGE21,
                        IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A => ARM64_RELOC_PAGEOFF12,
                        _ => 0
                    };

                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = type,
                            IsExternal = true,
                            IsPCRelative = symbolicRelocation.Type != IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A,
                        });
                }
                else if (symbolicRelocation.Type == IMAGE_REL_BASED_DIR64)
                {
                    bool isExternal = !IsSectionSymbolName(symbolicRelocation.SymbolName);
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = isExternal ? symbolIndex : symbolIndex + 1,
                            Length = 8,
                            RelocationType = ARM64_RELOC_UNSIGNED,
                            IsExternal = isExternal,
                            IsPCRelative = false,
                        });
                }
                else if (symbolicRelocation.Type == IMAGE_REL_BASED_RELPTR32)
                {
                    // This one is tough... needs to be represented by ARM64_RELOC_SUBTRACTOR + ARM64_RELOC_UNSIGNED.
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = (uint)sectionIndex,
                            Length = 4,
                            RelocationType = ARM64_RELOC_SUBTRACTOR,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = ARM64_RELOC_UNSIGNED,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                }
                else
                {
                    throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type);
                }
            }
        }

        private void EmitCompactUnwindTable(IDictionary<string, SymbolDefinition> definedSymbols)
        {
            _compactUnwindStream = new MemoryStream(32 * _compactUnwindCodes.Count);
            // Preset the size of the compact unwind section which is not generated yet
            _compactUnwindStream.SetLength(32 * _compactUnwindCodes.Count);

            _compactUnwindSection = new MachSection("__LD", "__compact_unwind", _compactUnwindStream)
            {
                Log2Alignment = 3,
                Flags = S_REGULAR | S_ATTR_DEBUG,
            };

            IList<MachSymbol> symbols = _symbolTable;
            Span<byte> tempBuffer = stackalloc byte[8];
            foreach (var cu in _compactUnwindCodes)
            {
                EmitCompactUnwindSymbol(cu.PcStartSymbolName);
                BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, cu.PcLength);
                BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer.Slice(4), cu.Code);
                _compactUnwindStream.Write(tempBuffer);
                EmitCompactUnwindSymbol(cu.PersonalitySymbolName);
                EmitCompactUnwindSymbol(cu.LsdaSymbolName);
            }

            void EmitCompactUnwindSymbol(string symbolName)
            {
                Span<byte> tempBuffer = stackalloc byte[8];
                if (symbolName is not null)
                {
                    SymbolDefinition symbol = definedSymbols[symbolName];
                    MachSection section = _sections[symbol.SectionIndex];
                    BinaryPrimitives.WriteUInt64LittleEndian(tempBuffer, section.VirtualAddress + (ulong)symbol.Value);
                    _compactUnwindSection.Relocations.Add(
                        new MachRelocation
                        {
                            Address = (int)_compactUnwindStream.Position,
                            SymbolOrSectionIndex = (byte)(1 + symbol.SectionIndex), // 1-based
                            Length = 8,
                            RelocationType = ARM64_RELOC_UNSIGNED,
                            IsExternal = false,
                            IsPCRelative = false,
                        }
                    );
                }
                _compactUnwindStream.Write(tempBuffer);
            }
        }

        private protected override string ExternCName(string name) => "_" + name;

        // This represents the following DWARF code:
        //   DW_CFA_advance_loc: 4
        //   DW_CFA_def_cfa_offset: +16
        //   DW_CFA_offset: W29 -16
        //   DW_CFA_offset: W30 -8
        //   DW_CFA_advance_loc: 4
        //   DW_CFA_def_cfa_register: W29
        // which is generated for the following frame prolog/epilog:
        //   stp fp, lr, [sp, #-10]!
        //   mov fp, sp
        //   ...
        //   ldp fp, lr, [sp], #0x10
        //   ret
        private static ReadOnlySpan<byte> DwarfArm64EmptyFrame => new byte[]
        {
            0x04, 0x00, 0xFF, 0xFF, 0x10, 0x00, 0x00, 0x00,
            0x04, 0x02, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x02, 0x1E, 0x00, 0x08, 0x00, 0x00, 0x00,
            0x08, 0x01, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private protected override bool EmitCompactUnwinding(string startSymbolName, ulong length, string lsdaSymbolName, byte[] blob)
        {
            uint encoding = _compactUnwindDwarfCode;

            if (_cpuType == CPU_TYPE_ARM64)
            {
                if (blob.AsSpan().SequenceEqual(DwarfArm64EmptyFrame))
                {
                    // Frame-based encoding, no saved registers
                    encoding = 0x04000000;
                }
            }

            _compactUnwindCodes.Add(new CompactUnwindCode(
                PcStartSymbolName: startSymbolName,
                PcLength: (uint)length,
                Code: encoding | (encoding != _compactUnwindDwarfCode && lsdaSymbolName is not null ? 0x40000000u : 0), // UNWIND_HAS_LSDA
                LsdaSymbolName: encoding != _compactUnwindDwarfCode ? lsdaSymbolName : null
            ));

            return encoding != _compactUnwindDwarfCode;
        }

        private protected override bool UseFrameNames => true;

        private static bool IsSectionSymbolName(string symbolName) => symbolName.StartsWith('l');

        private struct MachHeader64
        {
            public uint CpuType { get; set; }
            public uint CpuSubType { get; set; }
            public uint FileType { get; set; }
            public uint NumberOfCommands { get; set; }
            public uint SizeOfCommands { get; set; }
            public uint Flags { get; set; }
            public uint Reserved { get; set; }

            public static int HeaderSize => 32;

            public void Write(FileStream stream)
            {
                Span<byte> buffer = stackalloc byte[HeaderSize];

                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(0, 4), MH_MAGIC_64);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4, 4), (uint)CpuType);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(8, 4), CpuSubType);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(12, 4), FileType);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(16, 4), NumberOfCommands);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(20, 4), SizeOfCommands);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(24, 4), Flags);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(28, 4), Reserved);

                stream.Write(buffer);
            }
        }

        public struct MachSegment64Header
        {
            public string Name { get; set; }
            public ulong Address { get; set; }
            public ulong Size { get; set; }
            public ulong FileOffset { get; set; }
            public ulong FileSize { get; set; }
            public uint MaximumProtection { get; set; }
            public uint InitialProtection { get; set; }
            public uint NumberOfSections { get; set; }
            public uint Flags { get; set; }

            public static int HeaderSize => 72;

            public void Write(FileStream stream)
            {
                Span<byte> buffer = stackalloc byte[HeaderSize];

                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(0, 4), LC_SEGMENT_64);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4, 4), (uint)(HeaderSize + NumberOfSections * MachSection.HeaderSize));
                bool encoded = Encoding.UTF8.TryGetBytes(Name, buffer.Slice(8, 16), out _);
                Debug.Assert(encoded);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(24, 8), Address);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(32, 8), Size);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(40, 8), FileOffset);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(48, 8), FileSize);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(56, 4), MaximumProtection);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(60, 4), InitialProtection);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(64, 4), NumberOfSections);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(68, 4), Flags);

                stream.Write(buffer);
            }
        }

        private sealed class MachSection
        {
            private Stream dataStream;
            private List<MachRelocation> relocationCollection;

            public string SectionName { get; private init; }
            public string SegmentName { get; private init; }
            public ulong VirtualAddress { get; set; }
            public ulong Size => (ulong)dataStream.Length;
            public uint FileOffset { get; set; }
            public uint Log2Alignment { get; set; }
            public uint RelocationOffset { get; set; }
            public uint NumberOfRelocationEntries => relocationCollection is null ? 0u : (uint)relocationCollection.Count;
            public uint Flags { get; set; }

            public uint Type => Flags & 0xFF;
            public bool IsInFile => Size > 0 && Type != S_ZEROFILL && Type != S_GB_ZEROFILL && Type != S_THREAD_LOCAL_ZEROFILL;

            public bool IsDwarfSection { get; }

            public IList<MachRelocation> Relocations => relocationCollection ??= new List<MachRelocation>();
            public Stream Stream => dataStream;
            public byte SectionIndex { get; set; }

            public static int HeaderSize => 80; // 64-bit section

            public MachSection(string segmentName, string sectionName, Stream stream)
            {
                ArgumentNullException.ThrowIfNull(segmentName);
                ArgumentNullException.ThrowIfNull(sectionName);

                this.SegmentName = segmentName;
                this.SectionName = sectionName;
                this.IsDwarfSection = segmentName == "__DWARF";
                this.dataStream = stream;
                this.relocationCollection = null;
            }

            public void WriteHeader(FileStream stream)
            {
                Span<byte> buffer = stackalloc byte[HeaderSize];

                buffer.Clear();
                bool encoded = Encoding.UTF8.TryGetBytes(SectionName, buffer.Slice(0, 16), out _);
                Debug.Assert(encoded);
                encoded = Encoding.UTF8.TryGetBytes(SegmentName, buffer.Slice(16, 16), out _);
                Debug.Assert(encoded);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(32, 8), VirtualAddress);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(40, 8), Size);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(48, 4), FileOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(52, 4), Log2Alignment);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(56, 4), RelocationOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(60, 4), NumberOfRelocationEntries);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(64, 4), Flags);

                stream.Write(buffer);
            }
        }

        private sealed class MachRelocation
        {
            public int Address { get; init; }
            public uint SymbolOrSectionIndex { get; init; }
            public bool IsPCRelative { get; init; }
            public bool IsExternal { get; init; }
            public byte Length { get; init; }
            public byte RelocationType { get; init; }

            public void Write(FileStream stream)
            {
                Span<byte> relocationBuffer = stackalloc byte[8];
                uint info = SymbolOrSectionIndex;
                info |= IsPCRelative ? 0x1_00_00_00u : 0u;
                info |= Length switch { 1 => 0u << 25, 2 => 1u << 25, 4 => 2u << 25, _ => 3u << 25 };
                info |= IsExternal ? 0x8_00_00_00u : 0u;
                info |= (uint)RelocationType << 28;
                BinaryPrimitives.WriteInt32LittleEndian(relocationBuffer, Address);
                BinaryPrimitives.WriteUInt32LittleEndian(relocationBuffer.Slice(4), info);
                stream.Write(relocationBuffer);
            }
        }

        private sealed class MachSymbol
        {
            public string Name { get; init; } = string.Empty;
            public byte Type { get; init; }
            public MachSection Section { get; init; }
            public ushort Descriptor { get; init; }
            public ulong Value { get; init; }

            public void Write(FileStream stream, MachStringTable stringTable)
            {
                Span<byte> buffer = stackalloc byte[16];
                uint nameIndex = stringTable.GetStringOffset(Name);

                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(0, 4), nameIndex);
                buffer[4] = Type;
                buffer[5] = (byte)(Section?.SectionIndex ?? 0);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(6, 2), Descriptor);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(8), Value);

                stream.Write(buffer);
            }
        }

        private sealed class MachSymbolTableCommandHeader
        {
            public uint SymbolTableOffset { get; set; }
            public uint NumberOfSymbols { get; set; }
            public uint StringTableOffset { get; set; }
            public uint StringTableSize { get; set; }

            public static int HeaderSize => 24;

            public void Write(FileStream stream)
            {
                Span<byte> buffer = stackalloc byte[HeaderSize];

                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(0, 4), LC_SYMTAB);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4, 4), (uint)HeaderSize);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(8, 4), SymbolTableOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(12, 4), NumberOfSymbols);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(16, 4), StringTableOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(20, 4), StringTableSize);

                stream.Write(buffer);
            }
        }

        private sealed class MachDynamicLinkEditSymbolTable
        {
            public uint LocalSymbolsIndex { get; set; }
            public uint LocalSymbolsCount { get; set; }
            public uint ExternalSymbolsIndex { get; set; }
            public uint ExternalSymbolsCount { get; set; }
            public uint UndefinedSymbolsIndex { get; set; }
            public uint UndefinedSymbolsCount { get; set; }
            public uint TableOfContentsOffset { get; set; }
            public uint TableOfContentsCount { get; set; }
            public uint ModuleTableOffset { get; set; }
            public uint ModuleTableCount { get; set; }
            public uint ExternalReferenceTableOffset { get; set; }
            public uint ExternalReferenceTableCount { get; set; }
            public uint IndirectSymbolTableOffset { get; set; }
            public uint IndirectSymbolTableCount { get; set; }
            public uint ExternalRelocationTableOffset { get; set; }
            public uint ExternalRelocationTableCount { get; set; }
            public uint LocalRelocationTableOffset { get; set; }
            public uint LocalRelocationTableCount { get; set; }

            public static int HeaderSize => 80;

            public void Write(FileStream stream)
            {
                Span<byte> buffer = stackalloc byte[HeaderSize];

                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(0, 4), LC_DYSYMTAB);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4, 4), (uint)HeaderSize);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(8, 4), LocalSymbolsIndex);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(12, 4), LocalSymbolsCount);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(16, 4), ExternalSymbolsIndex);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(20, 4), ExternalSymbolsCount);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(24, 4), UndefinedSymbolsIndex);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(28, 4), UndefinedSymbolsCount);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(32, 4), TableOfContentsOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(36, 4), TableOfContentsCount);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(40, 4), ModuleTableOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(44, 4), ModuleTableCount);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(48, 4), ExternalReferenceTableOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(52, 4), ExternalReferenceTableCount);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(56, 4), IndirectSymbolTableOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(60, 4), IndirectSymbolTableCount);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(64, 4), ExternalRelocationTableOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(68, 4), ExternalRelocationTableCount);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(72, 4), LocalRelocationTableOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(76, 4), LocalRelocationTableCount);

                stream.Write(buffer);
            }
        }

        private struct MachBuildVersionCommandHeader
        {
            public uint Platform;
            public uint MinimumPlatformVersion { get; set; }
            public uint SdkVersion { get; set; }

            public static int HeaderSize => 24;

            public void Write(FileStream stream)
            {
                Span<byte> buffer = stackalloc byte[HeaderSize];

                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(0, 4), LC_BUILD_VERSION);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4, 4), (uint)HeaderSize);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(8, 4), Platform);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(12, 4), MinimumPlatformVersion);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(16, 4), SdkVersion);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(20, 4), 0); // No tools

                stream.Write(buffer);
            }
        }

        private sealed class MachStringTable : StringTableBuilder
        {
            public MachStringTable()
            {
                // Always start the table with empty string
                GetStringOffset("");
            }
        }
    }
}
