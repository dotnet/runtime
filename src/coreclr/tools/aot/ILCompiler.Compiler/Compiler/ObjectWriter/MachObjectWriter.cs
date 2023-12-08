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
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Melanzana.MachO;

namespace ILCompiler.ObjectWriter
{
    internal sealed class MachObjectWriter : UnixObjectWriter
    {
        private sealed record CompactUnwindCode(string PcStartSymbolName, uint PcLength, uint Code, string LsdaSymbolName = null, string PersonalitySymbolName = null);

        private readonly TargetOS _targetOS;
        private readonly MachObjectFile _objectFile;
        private readonly MachSegment _segment;

        // Exception handling sections
        private MachSection _compactUnwindSection;
        private readonly List<CompactUnwindCode> _compactUnwindCodes = new();
        private readonly uint _compactUnwindDwarfCode;

        // Symbol table
        private readonly Dictionary<string, uint> _symbolNameToIndex = new();
        private MachSymbolTable _symbolTable;
        private MachDynamicLinkEditSymbolTable _dySymbolTable;

        public MachObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
            _objectFile = new MachObjectFile
            {
                FileType = MachFileType.Object,
                IsLittleEndian = true,
                Flags = MachHeaderFlags.SubsectionsViaSymbols
            };

            switch (factory.Target.Architecture)
            {
                case TargetArchitecture.ARM64:
                    _objectFile.CpuType = MachCpuType.Arm64;
                    _objectFile.CpuSubType = (uint)MachArm64CpuSubType.All;
                    _compactUnwindDwarfCode = 0x3_00_00_00u;
                    break;
                case TargetArchitecture.X64:
                    _objectFile.CpuType = MachCpuType.X86_64;
                    _objectFile.CpuSubType = (uint)X8664CpuSubType.All;
                    _compactUnwindDwarfCode = 0x4_00_00_00u;
                    break;
                default:
                    throw new NotSupportedException("Unsupported architecture");
            }

            // Mach-O object files have single segment with no name
            _segment = new MachSegment(_objectFile, "")
            {
                InitialProtection = MachVmProtection.Execute | MachVmProtection.Read | MachVmProtection.Write,
                MaximumProtection = MachVmProtection.Execute | MachVmProtection.Read | MachVmProtection.Write,
            };
            _objectFile.LoadCommands.Add(_segment);

            _targetOS = factory.Target.OperatingSystem;
        }

        protected override void EmitSectionsAndLayout()
        {
            _compactUnwindSection = new MachSection(_objectFile, "__LD", "__compact_unwind")
            {
                Log2Alignment = 3,
                Type = MachSectionType.Regular,
                Attributes = MachSectionAttributes.Debug,
                // Preset the size of the compact unwind section which is not generated yet
                Size = 32u * (ulong)_compactUnwindCodes.Count,
            };

            // Insert all the load commands to ensure we have correct layout
            _symbolTable = new MachSymbolTable(_objectFile);
            _dySymbolTable = new MachDynamicLinkEditSymbolTable();
            _objectFile.LoadCommands.Add(_symbolTable);
            _objectFile.LoadCommands.Add(_dySymbolTable);

            var sdkVersion = new Version(16, 0, 0);
            switch (_targetOS)
            {
                case TargetOS.OSX:
                    _objectFile.LoadCommands.Add(new MachVersionMinMacOS
                    {
                        MinimumPlatformVersion = new Version(10, 12, 0),
                        SdkVersion = sdkVersion,
                    });
                    break;

                case TargetOS.MacCatalyst:
                    _objectFile.LoadCommands.Add(new MachBuildVersion
                    {
                        TargetPlatform = MachPlatform.MacCatalyst,
                        MinimumPlatformVersion = _objectFile.CpuType switch {
                            MachCpuType.X86_64 => new Version(13, 5, 0),
                            _ => new Version(14, 2, 0),
                        },
                        SdkVersion = sdkVersion,
                    });
                    break;

                case TargetOS.iOS:
                case TargetOS.iOSSimulator:
                case TargetOS.tvOS:
                case TargetOS.tvOSSimulator:
                    _objectFile.LoadCommands.Add(new MachBuildVersion
                    {
                        TargetPlatform = _targetOS switch
                        {
                            TargetOS.iOS => MachPlatform.IOS,
                            TargetOS.iOSSimulator => MachPlatform.IOSSimulator,
                            TargetOS.tvOS => MachPlatform.TvOS,
                            TargetOS.tvOSSimulator => MachPlatform.TvOSSimulator,
                            _ => 0,
                        },
                        MinimumPlatformVersion = new Version(11, 0, 0),
                        SdkVersion = sdkVersion,
                    });
                    break;
            }

            // Layout the sections
            _objectFile.UpdateLayout();

            // Generate section base symbols. The section symbols are used for PC relative relocations
            // to subtract the base of the section, and in DWARF to emit section relative relocations.
            uint sectionIndex = 0;
            foreach (MachSection machSection in _segment.Sections)
            {
                var machSymbol = new MachSymbol
                {
                    Name = $"lsection{sectionIndex}",
                    Section = machSection,
                    Value = machSection.VirtualAddress,
                    Descriptor = 0,
                    Type = MachSymbolType.Section,
                };
                _symbolTable.Symbols.Add(machSymbol);
                _symbolNameToIndex[machSymbol.Name] = sectionIndex;
                sectionIndex++;
            }
        }

        protected override void EmitObjectFile(string objectFilePath)
        {
            _segment.Sections.Add(_compactUnwindSection);

            // Update layout again to account for symbol table and relocation tables
            _objectFile.UpdateLayout();

            using (var outputFileStream = new FileStream(objectFilePath, FileMode.Create))
            {
                MachWriter.Write(outputFileStream, _objectFile);
            }
        }

        protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, Stream sectionStream)
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

            MachSectionAttributes attributes = section.Name switch
            {
                ".dotnet_eh_table" => MachSectionAttributes.Debug,
                ".eh_frame" => MachSectionAttributes.LiveSupport | MachSectionAttributes.StripStaticSymbols | MachSectionAttributes.NoTableOfContents,
                _ => section.Type switch
                {
                    SectionType.Executable => MachSectionAttributes.SomeInstructions | MachSectionAttributes.PureInstructions,
                    SectionType.Debug => MachSectionAttributes.Debug,
                    _ => 0
                }
            };

            MachSectionType type = section.Name switch
            {
                "bss" => MachSectionType.ZeroFill,
                ".eh_frame" => MachSectionType.Coalesced,
                _ => section.Type == SectionType.Uninitialized ? MachSectionType.ZeroFill : MachSectionType.Regular
            };

            MachSection machSection = new MachSection(_objectFile, segmentName, sectionName, sectionStream)
            {
                Log2Alignment = 1,
                Type = type,
                Attributes = attributes,
            };

            int sectionIndex = _segment.Sections.Count;
            _segment.Sections.Add(machSection);

            base.CreateSection(section, comdatName, symbolName ?? $"lsection{sectionIndex}", sectionStream);
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
            MachSection machSection = _segment.Sections[sectionIndex];
            Debug.Assert(BitOperations.IsPow2(alignment));
            machSection.Log2Alignment = Math.Max(machSection.Log2Alignment, (uint)BitOperations.Log2((uint)alignment));
        }

        protected internal override void EmitRelocation(
            int sectionIndex,
            long offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            long addend)
        {
            if (relocType is RelocType.IMAGE_REL_BASED_DIR64 or RelocType.IMAGE_REL_BASED_HIGHLOW)
            {
                // Mach-O doesn't use relocations between DWARF sections, so embed the offsets directly
                MachSection machSection = _segment.Sections[sectionIndex];
                if (machSection.Attributes.HasFlag(MachSectionAttributes.Debug) &&
                    machSection.SegmentName == "__DWARF")
                {
                    // DWARF section to DWARF section relocation
                    if (symbolName.StartsWith('.'))
                    {
                        switch (relocType)
                        {
                            case RelocType.IMAGE_REL_BASED_DIR64:
                                BinaryPrimitives.WriteInt64LittleEndian(data, addend);
                                break;
                            case RelocType.IMAGE_REL_BASED_HIGHLOW:
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
                        Debug.Assert(relocType == RelocType.IMAGE_REL_BASED_DIR64);
                        int targetSectionIndex = (int)_symbolNameToIndex[symbolName];
                        BinaryPrimitives.WriteUInt64LittleEndian(data, _segment.Sections[targetSectionIndex].VirtualAddress + (ulong)addend);
                        base.EmitRelocation(sectionIndex, offset, data, relocType, symbolName, addend);
                    }

                    return;
                }
            }

            // For most relocations we write the addend directly into the
            // data. The exceptions are IMAGE_REL_BASED_ARM64_PAGEBASE_REL21
            // and IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A.

            if (relocType == RelocType.IMAGE_REL_BASED_ARM64_BRANCH26)
            {
                Debug.Assert(_objectFile.CpuType == MachCpuType.Arm64);
                Debug.Assert(addend == 0);
            }
            else if (relocType == RelocType.IMAGE_REL_BASED_DIR64)
            {
                if (addend != 0)
                {
                    BinaryPrimitives.WriteInt64LittleEndian(
                        data,
                        BinaryPrimitives.ReadInt64LittleEndian(data) +
                        addend);
                    addend = 0;
                }
            }
            else if (relocType == RelocType.IMAGE_REL_BASED_RELPTR32)
            {
                if (_objectFile.CpuType == MachCpuType.Arm64)
                {
                    // On ARM64 we need to represent PC relative relocations as
                    // subtraction and the PC offset is baked into the addend.
                    BinaryPrimitives.WriteInt32LittleEndian(
                        data,
                        BinaryPrimitives.ReadInt32LittleEndian(data) +
                        (int)(addend - offset));
                }
                else if (sectionIndex == EhFrameSectionIndex)
                {
                    // ld64 requires X86_64_RELOC_SUBTRACTOR + X86_64_RELOC_UNSIGNED
                    // for DWARF CFI sections
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
            }
            else if (relocType == RelocType.IMAGE_REL_BASED_REL32)
            {
                Debug.Assert(_objectFile.CpuType != MachCpuType.Arm64);
                if (addend != 0)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(
                        data,
                        BinaryPrimitives.ReadInt32LittleEndian(data) +
                        (int)addend);
                    addend = 0;
                }
            }

            base.EmitRelocation(sectionIndex, offset, data, relocType, symbolName, addend);
        }

        protected override void EmitSymbolTable(
            IDictionary<string, SymbolDefinition> definedSymbols,
            SortedSet<string> undefinedSymbols)
        {
            // We already emitted symbols for all non-debug sections in EmitSectionsAndLayout,
            // these symbols are local and we need to account for them.
            uint symbolIndex = (uint)_symbolTable.Symbols.Count;
            _dySymbolTable.LocalSymbolsIndex = 0;
            _dySymbolTable.LocalSymbolsCount = symbolIndex;

            // Sort and insert all defined symbols
            var sortedDefinedSymbols = new List<MachSymbol>(definedSymbols.Count);
            foreach ((string name, SymbolDefinition definition) in definedSymbols)
            {
                MachSection section = _segment.Sections[definition.SectionIndex];
                sortedDefinedSymbols.Add(new MachSymbol
                {
                    Name = name,
                    Section = section,
                    Value = section.VirtualAddress + (ulong)definition.Value,
                    Descriptor = 0,
                    Type = MachSymbolType.Section | MachSymbolType.External,
                });
            }
            sortedDefinedSymbols.Sort((symA, symB) => string.CompareOrdinal(symA.Name, symB.Name));
            foreach (MachSymbol definedSymbol in sortedDefinedSymbols)
            {
                _symbolTable.Symbols.Add(definedSymbol);
                _symbolNameToIndex[definedSymbol.Name] = symbolIndex;
                symbolIndex++;
            }

            _dySymbolTable.ExternalSymbolsIndex = _dySymbolTable.LocalSymbolsCount;
            _dySymbolTable.ExternalSymbolsCount = (uint)definedSymbols.Count;

            uint savedSymbolIndex = symbolIndex;
            foreach (string externSymbol in undefinedSymbols)
            {
                var machSymbol = new MachSymbol
                {
                    Name = externSymbol,
                    Section = null,
                    Value = 0,
                    Descriptor = 0,
                    Type = MachSymbolType.Undefined | MachSymbolType.External,
                };
                _symbolTable.Symbols.Add(machSymbol);
                _symbolNameToIndex[externSymbol] = symbolIndex;
                symbolIndex++;
            }

            _dySymbolTable.UndefinedSymbolsIndex = _dySymbolTable.LocalSymbolsCount + _dySymbolTable.ExternalSymbolsCount;
            _dySymbolTable.UndefinedSymbolsCount = symbolIndex - savedSymbolIndex;

            EmitCompactUnwindTable(definedSymbols);
        }

        protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            if (_objectFile.CpuType == MachCpuType.Arm64)
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
            ICollection<MachRelocation> sectionRelocations = _segment.Sections[sectionIndex].Relocations;

            relocationList.Reverse();
            foreach (SymbolicRelocation symbolicRelocation in relocationList)
            {
                uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];

                if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_DIR64)
                {
                    bool isExternal = !IsSectionSymbolName(symbolicRelocation.SymbolName);
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = isExternal ? symbolIndex : symbolIndex + 1,
                            Length = 8,
                            RelocationType = MachRelocationType.X86_64Unsigned,
                            IsExternal = isExternal,
                            IsPCRelative = false,
                        });
                }
                else if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_RELPTR32 && sectionIndex == EhFrameSectionIndex)
                {
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = (uint)sectionIndex,
                            Length = 4,
                            RelocationType = MachRelocationType.X86_64Subtractor,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = MachRelocationType.X86_64Unsigned,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                }
                else if (symbolicRelocation.Type is RelocType.IMAGE_REL_BASED_RELPTR32 or RelocType.IMAGE_REL_BASED_REL32)
                {
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = MachRelocationType.X86_64Branch,
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
            ICollection<MachRelocation> sectionRelocations = _segment.Sections[sectionIndex].Relocations;

            relocationList.Reverse();
            foreach (SymbolicRelocation symbolicRelocation in relocationList)
            {
                uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];

                if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_ARM64_BRANCH26)
                {
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = MachRelocationType.Arm64Branch26,
                            IsExternal = true,
                            IsPCRelative = true,
                        });
                }
                else if (symbolicRelocation.Type is RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 or RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A)
                {
                    if (symbolicRelocation.Addend != 0)
                    {
                        sectionRelocations.Add(
                            new MachRelocation
                            {
                                Address = (int)symbolicRelocation.Offset,
                                SymbolOrSectionIndex = (uint)symbolicRelocation.Addend,
                                Length = 4,
                                RelocationType = MachRelocationType.Arm64Addend,
                                IsExternal = false,
                                IsPCRelative = false,
                            });
                    }

                    MachRelocationType type = symbolicRelocation.Type switch
                    {
                        RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 => MachRelocationType.Arm64Page21,
                        RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A => MachRelocationType.Arm64PageOffset21,
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
                            IsPCRelative = symbolicRelocation.Type != RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A,
                        });
                }
                else if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_DIR64)
                {
                    bool isExternal = !IsSectionSymbolName(symbolicRelocation.SymbolName);
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = isExternal ? symbolIndex : symbolIndex + 1,
                            Length = 8,
                            RelocationType = MachRelocationType.Arm64Unsigned,
                            IsExternal = isExternal,
                            IsPCRelative = false,
                        });
                }
                else if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_RELPTR32)
                {
                    // This one is tough... needs to be represented by ARM64_RELOC_SUBTRACTOR + ARM64_RELOC_UNSIGNED.
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = (uint)sectionIndex,
                            Length = 4,
                            RelocationType = MachRelocationType.Arm64Subtractor,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = (int)symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = MachRelocationType.Arm64Unsigned,
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
            using Stream compactUnwindStream = _compactUnwindSection.GetWriteStream();

            IList<MachSymbol> symbols = _symbolTable.Symbols;
            Span<byte> tempBuffer = stackalloc byte[8];
            foreach (var cu in _compactUnwindCodes)
            {
                EmitCompactUnwindSymbol(cu.PcStartSymbolName);
                BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, cu.PcLength);
                BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer.Slice(4), cu.Code);
                compactUnwindStream.Write(tempBuffer);
                EmitCompactUnwindSymbol(cu.PersonalitySymbolName);
                EmitCompactUnwindSymbol(cu.LsdaSymbolName);
            }

            void EmitCompactUnwindSymbol(string symbolName)
            {
                Span<byte> tempBuffer = stackalloc byte[8];
                if (symbolName != null)
                {
                    SymbolDefinition symbol = definedSymbols[symbolName];
                    MachSection section = _segment.Sections[symbol.SectionIndex];
                    BinaryPrimitives.WriteUInt64LittleEndian(tempBuffer, section.VirtualAddress + (ulong)symbol.Value);
                    _compactUnwindSection.Relocations.Add(
                        new MachRelocation
                        {
                            Address = (int)compactUnwindStream.Position,
                            SymbolOrSectionIndex = (byte)(1 + symbol.SectionIndex), // 1-based
                            Length = 8,
                            RelocationType = MachRelocationType.Arm64Unsigned,
                            IsExternal = false,
                            IsPCRelative = false,
                        }
                    );
                }
                compactUnwindStream.Write(tempBuffer);
            }
        }

        protected override string ExternCName(string name) => "_" + name;

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

        protected override bool EmitCompactUnwinding(string startSymbolName, ulong length, string lsdaSymbolName, byte[] blob)
        {
            uint encoding = _compactUnwindDwarfCode;

            if (_objectFile.CpuType == MachCpuType.Arm64)
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
                Code: encoding | (encoding != _compactUnwindDwarfCode && lsdaSymbolName != null ? 0x40000000u : 0), // UNWIND_HAS_LSDA
                LsdaSymbolName: encoding != _compactUnwindDwarfCode ? lsdaSymbolName : null
            ));

            return encoding != _compactUnwindDwarfCode;
        }

        private static bool IsSectionSymbolName(string symbolName) => symbolName.StartsWith('l');
    }
}
