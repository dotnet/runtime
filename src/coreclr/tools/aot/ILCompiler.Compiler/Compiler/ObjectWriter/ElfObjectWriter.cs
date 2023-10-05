// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Linq;
using System.Numerics;
using System.Buffers;
using System.Buffers.Binary;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using Internal.JitInterface;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

using Melanzana.MachO;
using LibObjectFile;
using LibObjectFile.Elf;
using LibObjectFile.Dwarf;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    public sealed class ElfObjectWriter : UnixObjectWriter
    {
        private ElfObjectFile _objectFile;
        private int _sectionIndex;
        private Dictionary<int, Stream> _bssStreams = new();
        private Dictionary<int, ElfSection> _sectionIndexToElfSection = new();
        private Dictionary<ElfSection, int> _elfSectionToSectionIndex = new();
        private Dictionary<string, ElfGroupSection> _comdatNameToElfSection = new(StringComparer.Ordinal);
        private Dictionary<ElfSection, ElfRelocationTable> _sectionToRelocationTable = new();

        // Symbol table
        private Dictionary<string, uint> _symbolNameToIndex = new();
        private ElfSymbolTable _symbolTable;

        private ElfObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
            ElfArch arch = factory.Target.Architecture switch
            {
                TargetArchitecture.X64 => ElfArch.X86_64,
                TargetArchitecture.ARM64 => ElfArch.AARCH64,
                _ => throw new NotSupportedException("Unsupported architecture")
            };

            _objectFile = new ElfObjectFile(arch);

            var stringSection = new ElfStringTable();
            _symbolTable = new ElfSymbolTable { Link = stringSection };
        }

        protected override void CreateSection(ObjectNodeSection section, Stream sectionStream)
        {
            ElfSection elfSection;
            ElfGroupSection groupSection = null;
            string sectionName =
                section.Name == "rdata" ? ".rodata" :
                (section.Name.StartsWith("_") || section.Name.StartsWith(".") ? section.Name : "." + section.Name);

            if (section.ComdatName != null &&
                !_comdatNameToElfSection.TryGetValue(section.ComdatName, out groupSection))
            {
                groupSection = new ElfGroupSection
                {
                    Link = _symbolTable,
                    // Info = <symbol index> of the COMDAT symbol, to be filled later
                };
                groupSection.GroupFlags = 1; // GRP_COMDAT
                _comdatNameToElfSection.Add(section.ComdatName, groupSection);
            }

            if (section.Type == SectionType.Uninitialized)
            {
                elfSection = new ElfBinarySection()
                {
                    Name = sectionName,
                    Type = ElfSectionType.NoBits,
                    Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Write,
                };

                _bssStreams[_sectionIndex] = sectionStream;
            }
            else if (section == ObjectNodeSection.TLSSection)
            {
                elfSection = new ElfBinarySection(sectionStream)
                {
                    Name = sectionName,
                    Type = ElfSectionType.ProgBits,
                    Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Write | ElfSectionFlags.Tls
                };
            }
            else
            {
                elfSection = new ElfBinarySection(sectionStream)
                {
                    Name = sectionName,
                    Type = section.Name == ".eh_frame" && _objectFile.Arch == ElfArch.X86_64 ?
                        (ElfSectionType)ElfNative.SHT_IA_64_UNWIND : ElfSectionType.ProgBits,
                    Flags =
                        section.Type == SectionType.Executable ? ElfSectionFlags.Alloc | ElfSectionFlags.Executable :
                        (section.Type == SectionType.Writeable ? ElfSectionFlags.Alloc | ElfSectionFlags.Write :
                        ElfSectionFlags.Alloc)
                };
            }

            _elfSectionToSectionIndex[elfSection] = _sectionIndex;
            _sectionIndexToElfSection[_sectionIndex++] = elfSection;
            groupSection?.AddSection(elfSection);
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
            var elfSection = _sectionIndexToElfSection[sectionIndex];
            elfSection.Alignment = Math.Max(elfSection.Alignment, (uint)alignment);
        }

        protected internal override void EmitRelocation(
            int sectionIndex,
            int offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            int addend)
        {
            // We read the addend from the data and clear it. This is necessary
            // to produce correct addends in the `.rela` sections which override
            // the destination with the addend from relocation table.

            if (relocType == RelocType.IMAGE_REL_BASED_REL32 ||
                relocType == RelocType.IMAGE_REL_BASED_RELPTR32 ||
                relocType == RelocType.IMAGE_REL_TLSGD ||
                relocType == RelocType.IMAGE_REL_TPOFF)
            {
                addend += BinaryPrimitives.ReadInt32LittleEndian(data);
                BinaryPrimitives.WriteInt32LittleEndian(data, 0);
            }
            else if (relocType == RelocType.IMAGE_REL_BASED_DIR64)
            {
                var a = BinaryPrimitives.ReadUInt64LittleEndian(data);
                addend += checked((int)a);
                BinaryPrimitives.WriteUInt64LittleEndian(data, 0);
            }
            else if (relocType == RelocType.IMAGE_REL_BASED_ARM64_BRANCH26 ||
                     relocType == RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 ||
                     relocType == RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A ||
                     relocType == RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_HI12 ||
                     relocType == RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_LO12_NC ||
                     relocType == RelocType.IMAGE_REL_AARCH64_TLSDESC_ADR_PAGE21 ||
                     relocType == RelocType.IMAGE_REL_AARCH64_TLSDESC_LD64_LO12 ||
                     relocType == RelocType.IMAGE_REL_AARCH64_TLSDESC_ADD_LO12 ||
                     relocType == RelocType.IMAGE_REL_AARCH64_TLSDESC_CALL)
            {
                // NOTE: Zero addend in code is currently always used for these.
                // R2R object writer has the same assumption.
            }
            else
            {
                throw new NotSupportedException($"Unsupported relocation: {relocType}");
            }

            base.EmitRelocation(sectionIndex, offset, data, relocType, symbolName, addend);
        }

        protected override void EmitSymbolTable()
        {
            uint symbolIndex = (uint)_symbolTable.Entries.Count;

            var definedSymbols = GetDefinedSymbols();
            var sortedSymbols = new List<ElfSymbol>(definedSymbols.Count);
            foreach (var (name, definition) in definedSymbols)
            {
                var elfSection = _sectionIndexToElfSection[definition.SectionIndex];
                sortedSymbols.Add(new ElfSymbol
                {
                    Name = name,
                    Bind = ElfSymbolBind.Global,
                    Section = elfSection,
                    Value = (ulong)definition.Value,
                    Type =
                        elfSection.Flags.HasFlag(ElfSectionFlags.Tls) ? ElfSymbolType.Tls :
                        definition.Size > 0 ? ElfSymbolType.Function : 0,
                    Size = (ulong)definition.Size,
                    Visibility = definition.Global ? ElfSymbolVisibility.Default : ElfSymbolVisibility.Hidden,
                });
            }

            foreach (var externSymbol in GetUndefinedSymbols())
            {
                sortedSymbols.Add(new ElfSymbol
                {
                    Name = externSymbol,
                    Bind = ElfSymbolBind.Global,
                });
            }

            sortedSymbols.Sort((symA, symB) => string.CompareOrdinal(symA.Name, symB.Name));
            foreach (var definedSymbol in sortedSymbols)
            {
                _symbolTable.Entries.Add(definedSymbol);
                _symbolNameToIndex[definedSymbol.Name] = symbolIndex;
                symbolIndex++;
            }

            // Update group sections links
            foreach (var (comdatName, groupSection) in _comdatNameToElfSection)
            {
                groupSection.Info = new ElfSectionLink(_symbolNameToIndex[comdatName]);
            }
        }

        protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            switch ((ElfArch)_objectFile.Arch)
            {
                case ElfArch.X86_64:
                    EmitRelocationsX64(sectionIndex, relocationList);
                    break;
                case ElfArch.AARCH64:
                    EmitRelocationsARM64(sectionIndex, relocationList);
                    break;
                default:
                    Debug.Fail("Unsupported architecture");
                    break;
            }
        }

        private void EmitRelocationsARM64(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            var elfSection = _sectionIndexToElfSection[sectionIndex];
            if (_sectionToRelocationTable.TryGetValue(elfSection, out var relocationTable))
            {
                foreach (var symbolicRelocation in relocationList)
                {
                    uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];

                    ElfRelocationType type = symbolicRelocation.Type switch {
                        RelocType.IMAGE_REL_BASED_DIR64 => ElfRelocationType.R_AARCH64_ABS64,
                        RelocType.IMAGE_REL_BASED_HIGHLOW => ElfRelocationType.R_AARCH64_ABS32,
                        RelocType.IMAGE_REL_BASED_RELPTR32 => ElfRelocationType.R_AARCH64_PREL32,
                        RelocType.IMAGE_REL_BASED_ARM64_BRANCH26 => ElfRelocationType.R_AARCH64_CALL26,
                        RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 => ElfRelocationType.R_AARCH64_ADR_PREL_PG_HI21,
                        RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A => ElfRelocationType.R_AARCH64_ADD_ABS_LO12_NC,
                        RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_HI12 => ElfRelocationType.R_AARCH64_TLSLE_ADD_TPREL_HI12,
                        RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_LO12_NC => ElfRelocationType.R_AARCH64_TLSLE_ADD_TPREL_LO12_NC,
                        RelocType.IMAGE_REL_AARCH64_TLSDESC_ADR_PAGE21 => ElfRelocationType.R_AARCH64_TLSDESC_ADR_PAGE21,
                        RelocType.IMAGE_REL_AARCH64_TLSDESC_LD64_LO12 => ElfRelocationType.R_AARCH64_TLSDESC_LD64_LO12,
                        RelocType.IMAGE_REL_AARCH64_TLSDESC_ADD_LO12 => ElfRelocationType.R_AARCH64_TLSDESC_ADD_LO12,
                        RelocType.IMAGE_REL_AARCH64_TLSDESC_CALL => ElfRelocationType.R_AARCH64_TLSDESC_CALL,
                        _ => throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type)
                    };

                    var addend = symbolicRelocation.Addend;

                    relocationTable.Entries.Add(new ElfRelocation
                    {
                        SymbolIndex = symbolIndex,
                        Type = type,
                        Offset = (ulong)symbolicRelocation.Offset,
                        Addend = addend
                    });
                }
            }
            else
            {
                Debug.Assert(relocationList.Count == 0);
            }
        }

        private void EmitRelocationsX64(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            var elfSection = _sectionIndexToElfSection[sectionIndex];
            if (_sectionToRelocationTable.TryGetValue(elfSection, out var relocationTable))
            {
                foreach (var symbolicRelocation in relocationList)
                {
                    uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];

                    ElfRelocationType type = symbolicRelocation.Type switch {
                        RelocType.IMAGE_REL_BASED_DIR64 => ElfRelocationType.R_X86_64_64,
                        RelocType.IMAGE_REL_BASED_RELPTR32 => ElfRelocationType.R_X86_64_PC32,
                        RelocType.IMAGE_REL_BASED_REL32 => ElfRelocationType.R_X86_64_PLT32,
                        RelocType.IMAGE_REL_TLSGD => ElfRelocationType.R_X86_64_TLSGD,
                        RelocType.IMAGE_REL_TPOFF => ElfRelocationType.R_X86_64_TPOFF32,
                        _ => throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type)
                    };

                    var addend = symbolicRelocation.Addend;
                    if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_REL32)
                    {
                        addend -= 4;
                    }

                    relocationTable.Entries.Add(new ElfRelocation
                    {
                        SymbolIndex = symbolIndex,
                        Type = type,
                        Offset = (ulong)symbolicRelocation.Offset,
                        Addend = addend
                    });
                }
            }
            else
            {
                Debug.Assert(relocationList.Count == 0);
            }
        }

        protected override ulong GetSectionVirtualAddress(int sectionIndex)
        {
            // Use file offset
            return _sectionIndexToElfSection[sectionIndex].Offset;
        }

        protected override void EmitDebugSections(DwarfFile dwarfFile)
        {
            ElfRelocationType reloc32, reloc64;

            switch ((ElfArch)_objectFile.Arch)
            {
                case ElfArch.X86_64:
                    reloc32 = ElfRelocationType.R_X86_64_32;
                    reloc64 = ElfRelocationType.R_X86_64_64;
                    break;
                case ElfArch.AARCH64:
                    reloc32 = ElfRelocationType.R_AARCH64_ABS32;
                    reloc64 = ElfRelocationType.R_AARCH64_ABS64;
                    break;
                default:
                    throw new NotSupportedException("Unsupported architecture");
            }

            Dictionary<ElfSection, uint> sectionToSymbolIndex = new();
            ulong debugRangesSize = 0;
            var debugRangesStream = new MemoryStream();
            var debugRangesSection = new ElfBinarySection(debugRangesStream) { Name = ".debug_ranges", Type = ElfSectionType.ProgBits, Alignment = 8 };
            var debugRangesRelocation = new ElfRelocationTable { Name = ".rela.debug_ranges", Link = _symbolTable, Info = debugRangesSection, Alignment = 8 };

            var unit = dwarfFile.InfoSection.Units[0];
            var rootDIE = (DwarfDIECompileUnit)unit.Root;

            dwarfFile.AddressRangeTable.AddressSize = unit.AddressSize;
            dwarfFile.AddressRangeTable.Unit = unit;

            // Point DW_AT_RANGES to beginning of our .debug_ranges section
            rootDIE.Ranges = 0;

            foreach (var elfSection in _objectFile.Sections)
            {
                // NOTE: We could exclude some extra sections, like .dotnet_eh_table, .eh_frame,
                // __modules and .note.GNU-stack but it doesn't really matter if we include them.
                if (elfSection is ElfBinarySection &&
                    elfSection.Size > 0 &&
                    /*!elfSection.Name.Value.Equals(".dotnet_eh_table", StringComparison.Ordinal) &&*/
                    !elfSection.Name.Value.Equals(".eh_frame", StringComparison.Ordinal))
                {
                    dwarfFile.AddressRangeTable.Ranges.Add(new DwarfAddressRange(0, elfSection.Offset, elfSection.Size));
                    rootDIE.LowPC ??= elfSection.Offset;
                    //rootDIE.HighPC = (int)elfSection.Size;

                    // Create a symbol for the section so we can make relocations relative to it.
                    uint sectionSymbolIndex;
                    sectionToSymbolIndex[elfSection] = sectionSymbolIndex = (uint)_symbolTable.Entries.Count;
                    _symbolTable.Entries.Add(new ElfSymbol() { Type = ElfSymbolType.Section, Section = elfSection });

                    // Emit entry in .debug_ranges
                    debugRangesRelocation.Entries.Add(new ElfRelocation(debugRangesSize, reloc64, sectionSymbolIndex, 0));
                    debugRangesRelocation.Entries.Add(new ElfRelocation(debugRangesSize + 8, reloc64, sectionSymbolIndex, (long)elfSection.Size));
                    debugRangesSize += 16;
                }
            }

            debugRangesSize += 16; // NULL entry
            debugRangesStream.SetLength((long)debugRangesSize);

            var outputContext = new DwarfWriterContext
            {
                IsLittleEndian = _objectFile.Encoding == ElfEncoding.Lsb,
                EnableRelocation = true,
                AddressSize = _objectFile.FileClass == ElfFileClass.Is64 ? DwarfAddressSize.Bit64 : DwarfAddressSize.Bit32,
                DebugLineStream = new MemoryStream(),
                DebugAbbrevStream = new MemoryStream(),
                DebugStringStream = new MemoryStream(),
                DebugAddressRangeStream = new MemoryStream(),
                DebugInfoStream = new MemoryStream(),
                DebugLocationStream = new MemoryStream(),
            };

            dwarfFile.Write(outputContext);

            var debugInfoSection = new ElfBinarySection(outputContext.DebugInfoStream) { Name = ".debug_info", Type = ElfSectionType.ProgBits, Alignment = 8 };
            var debugInfoRelocation = new ElfRelocationTable { Name = ".rela.debug_info", Link = _symbolTable, Info = debugInfoSection, Alignment = 8 };
            var debugAbbrevSection = new ElfBinarySection(outputContext.DebugAbbrevStream) { Name = ".debug_abbrev", Type = ElfSectionType.ProgBits, Alignment = 8 };
            var debugAddressRangeSection = new ElfBinarySection(outputContext.DebugAddressRangeStream) { Name = ".debug_aranges", Type = ElfSectionType.ProgBits, Alignment = 8 };
            var debugAddressRangeRelocation = new ElfRelocationTable { Name = ".rela.debug_aranges", Link = _symbolTable, Info = debugAddressRangeSection, Alignment = 8 };
            var debugStringSection = new ElfBinarySection(outputContext.DebugStringStream) { Name = ".debug_str", Type = ElfSectionType.ProgBits, Alignment = 8 };
            var debugLineSection = new ElfBinarySection(outputContext.DebugLineStream) { Name = ".debug_line", Type = ElfSectionType.ProgBits, Alignment = 8 };
            var debugLineRelocation = new ElfRelocationTable { Name = ".rela.debug_line", Link = _symbolTable, Info = debugLineSection, Alignment = 8 };
            var debugLocationSection = new ElfBinarySection(outputContext.DebugLocationStream) { Name = ".debug_loc", Type = ElfSectionType.ProgBits, Alignment = 8 };
            var debugLocationRelocation = new ElfRelocationTable { Name = ".rela.debug_loc", Link = _symbolTable, Info = debugLocationSection, Alignment = 8 };

            _objectFile.AddSection(debugInfoSection);
            _objectFile.AddSection(debugInfoRelocation);
            _objectFile.AddSection(debugAbbrevSection);
            _objectFile.AddSection(debugAddressRangeSection);
            _objectFile.AddSection(debugAddressRangeRelocation);
            _objectFile.AddSection(debugStringSection);
            _objectFile.AddSection(debugLineSection);
            _objectFile.AddSection(debugLineRelocation);
            _objectFile.AddSection(debugLocationSection);
            _objectFile.AddSection(debugLocationRelocation);
            _objectFile.AddSection(debugRangesSection);
            _objectFile.AddSection(debugRangesRelocation);

            uint stringSectionIndex = (uint)_symbolTable.Entries.Count;
            _symbolTable.Entries.Add(new ElfSymbol() { Type = ElfSymbolType.Section, Section = debugStringSection });
            uint abbrevSectionIndex = (uint)_symbolTable.Entries.Count;
            _symbolTable.Entries.Add(new ElfSymbol() { Type = ElfSymbolType.Section, Section = debugAbbrevSection });
            uint infoSectionIndex = (uint)_symbolTable.Entries.Count;
            _symbolTable.Entries.Add(new ElfSymbol() { Type = ElfSymbolType.Section, Section = debugInfoSection });
            uint rangesSectionIndex = (uint)_symbolTable.Entries.Count;
            _symbolTable.Entries.Add(new ElfSymbol() { Type = ElfSymbolType.Section, Section = debugRangesSection });

            CopyRelocations(dwarfFile.InfoSection, debugInfoRelocation);
            CopyRelocations(dwarfFile.AddressRangeTable, debugAddressRangeRelocation);
            CopyRelocations(dwarfFile.LineSection, debugLineRelocation);
            CopyRelocations(dwarfFile.LocationSection, debugLocationRelocation);

            // Add relocation for DW_AT_RANGES
            var rangesAttribute = rootDIE.FindAttributeByKey(DwarfAttributeKind.Ranges);
            debugInfoRelocation.Entries.Add(new ElfRelocation(rangesAttribute.Offset, reloc64, rangesSectionIndex, 0));

            void CopyRelocations(DwarfRelocatableSection dwarfRelocSection, ElfRelocationTable relocTable)
            {
                List<DwarfRelocation> codeRelocations = null;

                relocTable.Entries.Clear();

                foreach (var reloc in dwarfRelocSection.Relocations)
                {
                    if (reloc.Target != DwarfRelocationTarget.Code)
                    {
                        var relocType = reloc.Size == DwarfAddressSize.Bit64 ? reloc64 : reloc32;

                        switch (reloc.Target)
                        {
                            case DwarfRelocationTarget.DebugString:
                                relocTable.Entries.Add(new ElfRelocation(reloc.Offset, relocType, stringSectionIndex, (long)reloc.Addend));
                                break;
                            case DwarfRelocationTarget.DebugAbbrev:
                                relocTable.Entries.Add(new ElfRelocation(reloc.Offset, relocType, abbrevSectionIndex, (long)reloc.Addend));
                                break;
                            case DwarfRelocationTarget.DebugInfo:
                                relocTable.Entries.Add(new ElfRelocation(reloc.Offset, relocType, infoSectionIndex, (long)reloc.Addend));
                                break;
                            default:
                                Debug.Fail("Unknown relocation");
                                break;
                        }
                    }
                    else
                    {
                        codeRelocations ??= new();
                        codeRelocations.Add(reloc);
                    }
                }

                if (codeRelocations is not null)
                {
                    int sectionIndex = 0;
                    ElfSection currentSection = _objectFile.Sections[0];
                    uint symbolIndex = 0;

                    // Sort the code relocations by their offset, and then walk the sections
                    // and relocations at the same time to point them to matching section.
                    codeRelocations.Sort((a, b) => (int)a.Addend - (int)b.Addend);

                    foreach (var reloc in codeRelocations)
                    {
                        if (reloc.Addend >= currentSection.Offset + currentSection.Size)
                        {
                            while (reloc.Addend >= currentSection.Offset + currentSection.Size)
                            {
                                currentSection = _objectFile.Sections[++sectionIndex];
                            }

                            Debug.Assert(currentSection.Flags.HasFlag(ElfSectionFlags.Executable));
                            symbolIndex = sectionToSymbolIndex[currentSection];
                        }

                        var relocType = reloc.Size == DwarfAddressSize.Bit64 ? reloc64 : reloc32;
                        relocTable.Entries.Add(new ElfRelocation(reloc.Offset, relocType, symbolIndex, (long)(reloc.Addend - currentSection.Offset)));
                    }
                }
            }
        }

        private void AddElfSectionWithRelocationsIfNecessary(ElfSection elfSection, ElfGroupSection groupSection = null)
        {
            int sectionIndex = _elfSectionToSectionIndex[elfSection];
            elfSection.Flags |= groupSection is not null ? ElfSectionFlags.Group : 0;
            _objectFile.AddSection(elfSection);

            if (SectionHasRelocations(sectionIndex))
            {
                var elfRelocationTable = new ElfRelocationTable
                {
                    Name = ".rela" + elfSection.Name,
                    Link = _symbolTable,
                    Info = elfSection,
                    Alignment = 8,
                    Flags = groupSection is not null ? ElfSectionFlags.Group : 0
                };

                _sectionToRelocationTable[elfSection] = elfRelocationTable;
                _objectFile.AddSection(elfRelocationTable);
                groupSection?.AddSection(elfRelocationTable);
            }
        }

        protected override void EmitSectionsAndLayout()
        {
            foreach (var groupSection in _comdatNameToElfSection.Values)
            {
                _objectFile.AddSection(groupSection);
                for (int i = 0, sectionCount = groupSection.Sections.Count; i < sectionCount; i++)
                {
                    AddElfSectionWithRelocationsIfNecessary(groupSection.Sections[i], groupSection);
                }
            }

            foreach (var elfSection in _sectionIndexToElfSection.Values)
            {
                // If the section was not already added as part of COMDAT group,
                // add it now.
                if (elfSection.Parent is null)
                {
                    AddElfSectionWithRelocationsIfNecessary(elfSection);
                }
            }

            _objectFile.AddSection(_symbolTable.Link.Section);
            _objectFile.AddSection(_symbolTable);
            _objectFile.AddSection(new ElfSectionHeaderStringTable());
            foreach (var (bssSectionIndex, bssStream) in _bssStreams)
            {
                _sectionIndexToElfSection[bssSectionIndex].Size = (ulong)bssStream.Length;
            }

            _objectFile.AddSection(new ElfBinarySection(Stream.Null)
            {
                Name = ".note.GNU-stack",
                Type = ElfSectionType.ProgBits,
            });

            // NOTE: We need the layout to find the offset and size of the __managedcode section
            // but also to report correct method addresses in the debug information. Beyond this
            // point, GetSectionVirtualAddress must return correct address for any code section.
            var elfDiagnostics = new DiagnosticBag();
            _objectFile.UpdateLayout(elfDiagnostics);
            Debug.Assert(!elfDiagnostics.HasErrors);
        }

        protected override void EmitObjectFile(string objectFilePath)
        {
            using (var outputFileStream = new FileStream(objectFilePath, FileMode.Create))
            {
                _objectFile.Write(outputFileStream);
            }
        }

        public static void EmitObject(string objectFilePath, IReadOnlyCollection<DependencyNode> nodes, NodeFactory factory, ObjectWritingOptions options, IObjectDumper dumper, Logger logger)
        {
            using ElfObjectWriter writer = new ElfObjectWriter(factory, options);
            writer.EmitObject(objectFilePath, nodes, dumper, logger);
        }

        public sealed class ElfGroupSection : ElfSection
        {
            private List<ElfSection> _sections = new(4);

            public ElfGroupSection()
            {
                Name = ".group";
                Type = ElfSectionType.Group;
                Alignment = 4;
            }

            public uint GroupFlags { get; set; }
            public IReadOnlyList<ElfSection> Sections => _sections;
            public override ulong TableEntrySize => 4;

            public void AddSection(ElfSection section) => _sections.Add(section);

            public override void UpdateLayout(DiagnosticBag diagnostics)
            {
                Size = (ulong)(4u + (_sections.Count * 4u));
            }

            public override void Verify(DiagnosticBag diagnostics)
            {
                base.Verify(diagnostics);

                foreach (var section in _sections)
                {
                    if (section.Parent != this.Parent)
                    {
                        diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionInfoParent, $"Invalid parent for grouped section");
                    }
                }
            }

            protected override void Read(ElfReader reader) => throw new NotImplementedException();

            protected override void Write(ElfWriter writer)
            {
                writer.Write((uint)GroupFlags);
                foreach (var section in _sections)
                {
                    writer.Write((uint)section.SectionIndex);
                }
            }
        }
    }
}
