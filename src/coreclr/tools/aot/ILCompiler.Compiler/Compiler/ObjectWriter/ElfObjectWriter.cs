// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Buffers.Binary;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using LibObjectFile;
using LibObjectFile.Elf;

namespace ILCompiler.ObjectWriter
{
    public sealed class ElfObjectWriter : UnixObjectWriter
    {
        private readonly ElfObjectFile _objectFile;
        private readonly Dictionary<int, Stream> _bssStreams = new();
        private readonly List<ElfSection> _sections = new();
        private readonly Dictionary<ElfSection, int> _elfSectionToSectionIndex = new();
        private readonly Dictionary<string, ElfGroupSection> _comdatNameToElfSection = new(StringComparer.Ordinal);
        private readonly Dictionary<ElfSection, ElfRelocationTable> _sectionToRelocationTable = new();
        private readonly List<ElfSection> _debugSections = new();

        // Symbol table
        private readonly Dictionary<string, uint> _symbolNameToIndex = new();
        private readonly ElfSymbolTable _symbolTable;

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

        protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, Stream sectionStream)
        {
            ElfSection elfSection;
            ElfGroupSection groupSection = null;
            string sectionName =
                section.Name == "rdata" ? ".rodata" :
                (section.Name.StartsWith('_') || section.Name.StartsWith('.') ? section.Name : "." + section.Name);
            int sectionIndex = _sections.Count;

            if (comdatName is not null &&
                !_comdatNameToElfSection.TryGetValue(comdatName, out groupSection))
            {
                groupSection = new ElfGroupSection
                {
                    GroupFlags = 1, // GRP_COMDAT
                    Link = _symbolTable,
                    // Info = <symbol index> of the COMDAT symbol, to be filled later
                };
                _comdatNameToElfSection.Add(comdatName, groupSection);
            }

            if (section.Type == SectionType.Uninitialized)
            {
                elfSection = new ElfBinarySection()
                {
                    Name = sectionName,
                    Type = ElfSectionType.NoBits,
                    Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Write,
                };

                _bssStreams[sectionIndex] = sectionStream;
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
                    Flags = section.Type switch
                    {
                        SectionType.Executable => ElfSectionFlags.Alloc | ElfSectionFlags.Executable,
                        SectionType.Writeable => ElfSectionFlags.Alloc | ElfSectionFlags.Write,
                        SectionType.Debug => sectionName == ".debug_str" ? ElfSectionFlags.Merge | ElfSectionFlags.Strings : 0,
                        _ => ElfSectionFlags.Alloc,
                    },
                    Alignment = 1
                };
            }

            if (section.Type == SectionType.Debug)
            {
                _debugSections.Add(elfSection);
            }

            _elfSectionToSectionIndex[elfSection] = sectionIndex;
            _sections.Add(elfSection);
            groupSection?.AddSection(elfSection);

            base.CreateSection(section, comdatName, symbolName ?? elfSection.Name.Value, sectionStream);
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
            ElfSection elfSection = _sections[sectionIndex];
            elfSection.Alignment = Math.Max(elfSection.Alignment, (uint)alignment);
        }

        protected internal override void EmitRelocation(
            int sectionIndex,
            long offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            long addend)
        {
            // We read the addend from the data and clear it. This is necessary
            // to produce correct addends in the `.rela` sections which override
            // the destination with the addend from relocation table.

            if (relocType == RelocType.IMAGE_REL_BASED_REL32 ||
                relocType == RelocType.IMAGE_REL_BASED_RELPTR32 ||
                relocType == RelocType.IMAGE_REL_TLSGD ||
                relocType == RelocType.IMAGE_REL_TPOFF ||
                relocType == RelocType.IMAGE_REL_BASED_HIGHLOW)
            {
                addend += BinaryPrimitives.ReadInt32LittleEndian(data);
                BinaryPrimitives.WriteInt32LittleEndian(data, 0);
            }
            else if (relocType == RelocType.IMAGE_REL_BASED_DIR64)
            {
                ulong a = BinaryPrimitives.ReadUInt64LittleEndian(data);
                addend += checked((long)a);
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

        protected override void EmitSymbolTable(
            IDictionary<string, SymbolDefinition> definedSymbols,
            SortedSet<string> undefinedSymbols)
        {
            uint symbolIndex = (uint)_symbolTable.Entries.Count;

            List<ElfSymbol> sortedSymbols = new(definedSymbols.Count);
            foreach ((string name, SymbolDefinition definition) in definedSymbols)
            {
                ElfSection elfSection = _sections[definition.SectionIndex];
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

            foreach (string externSymbol in undefinedSymbols)
            {
                if (!_symbolNameToIndex.ContainsKey(externSymbol))
                {
                    sortedSymbols.Add(new ElfSymbol
                    {
                        Name = externSymbol,
                        Bind = ElfSymbolBind.Global,
                    });
                }
            }

            sortedSymbols.Sort((symA, symB) => string.CompareOrdinal(symA.Name, symB.Name));
            foreach (ElfSymbol definedSymbol in sortedSymbols)
            {
                _symbolTable.Entries.Add(definedSymbol);
                _symbolNameToIndex[definedSymbol.Name] = symbolIndex;
                symbolIndex++;
            }

            // Update group sections links
            foreach ((string comdatName, ElfGroupSection groupSection) in _comdatNameToElfSection)
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
            ElfSection elfSection = _sections[sectionIndex];
            if (_sectionToRelocationTable.TryGetValue(elfSection, out ElfRelocationTable relocationTable))
            {
                foreach (SymbolicRelocation symbolicRelocation in relocationList)
                {
                    uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];

                    ElfRelocationType type = symbolicRelocation.Type switch
                    {
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

                    relocationTable.Entries.Add(new ElfRelocation
                    {
                        SymbolIndex = symbolIndex,
                        Type = type,
                        Offset = (ulong)symbolicRelocation.Offset,
                        Addend = symbolicRelocation.Addend
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
            ElfSection elfSection = _sections[sectionIndex];
            if (_sectionToRelocationTable.TryGetValue(elfSection, out ElfRelocationTable relocationTable))
            {
                foreach (SymbolicRelocation symbolicRelocation in relocationList)
                {
                    uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];

                    ElfRelocationType type = symbolicRelocation.Type switch
                    {
                        RelocType.IMAGE_REL_BASED_HIGHLOW => ElfRelocationType.R_X86_64_32,
                        RelocType.IMAGE_REL_BASED_DIR64 => ElfRelocationType.R_X86_64_64,
                        RelocType.IMAGE_REL_BASED_RELPTR32 => ElfRelocationType.R_X86_64_PC32,
                        RelocType.IMAGE_REL_BASED_REL32 => ElfRelocationType.R_X86_64_PLT32,
                        RelocType.IMAGE_REL_TLSGD => ElfRelocationType.R_X86_64_TLSGD,
                        RelocType.IMAGE_REL_TPOFF => ElfRelocationType.R_X86_64_TPOFF32,
                        _ => throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type)
                    };

                    long addend = symbolicRelocation.Addend;
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

        protected override void EmitDebugSections(IDictionary<string, SymbolDefinition> definedSymbols)
        {
            base.EmitDebugSections(definedSymbols);

            foreach (ElfSection debugSection in _debugSections)
            {
                AddElfSectionWithRelocationsIfNecessary(debugSection);
            }

            uint symbolIndex = (uint)_symbolTable.Entries.Count;
            foreach (ElfSection elfSection in _objectFile.Sections)
            {
                if (elfSection is ElfBinarySection)
                {
                    _symbolTable.Entries.Add(new ElfSymbol() { Type = ElfSymbolType.Section, Section = elfSection });
                    _symbolNameToIndex[elfSection.Name] = symbolIndex;
                    symbolIndex++;
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
            foreach (ElfGroupSection groupSection in _comdatNameToElfSection.Values)
            {
                _objectFile.AddSection(groupSection);
                for (int i = 0, sectionCount = groupSection.Sections.Count; i < sectionCount; i++)
                {
                    AddElfSectionWithRelocationsIfNecessary(groupSection.Sections[i], groupSection);
                }
            }

            foreach (ElfSection elfSection in _sections)
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
            foreach ((int bssSectionIndex, Stream bssStream) in _bssStreams)
            {
                _sections[bssSectionIndex].Size = (ulong)bssStream.Length;
            }

            _objectFile.AddSection(new ElfBinarySection(Stream.Null)
            {
                Name = ".note.GNU-stack",
                Type = ElfSectionType.ProgBits,
            });
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
            private readonly List<ElfSection> _sections = new(4);

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

                foreach (ElfSection section in _sections)
                {
                    if (section.Parent != Parent)
                    {
                        diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionInfoParent, $"Invalid parent for grouped section");
                    }
                }
            }

            protected override void Read(ElfReader reader) => throw new NotImplementedException();

            protected override void Write(ElfWriter writer)
            {
                writer.Write(GroupFlags);

                foreach (ElfSection section in _sections)
                {
                    writer.Write(section.SectionIndex);
                }
            }
        }
    }
}
