// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Buffers.Binary;
using System.Numerics;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.ObjectWriter
{
    public sealed class ElfObjectWriter : UnixObjectWriter
    {
        private readonly ushort _machine;
        private readonly ElfStringTable _stringTable = new();
        private readonly List<SectionDefinition> _sections = new();
        private readonly List<ElfSymbol> _symbols = new();
        private uint _localSymbolCount;
        private readonly Dictionary<string, SectionDefinition> _comdatNameToElfSection = new(StringComparer.Ordinal);

        // Symbol table
        private readonly Dictionary<string, uint> _symbolNameToIndex = new();

        private ElfObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
            _machine = factory.Target.Architecture switch
            {
                TargetArchitecture.X86 => ElfNative.EM_386,
                TargetArchitecture.X64 => ElfNative.EM_X86_64,
                TargetArchitecture.ARM64 => ElfNative.EM_AARCH64,
                _ => throw new NotSupportedException("Unsupported architecture")
            };

            // By convention the symbol table starts with empty symbol
            _symbols.Add(new ElfSymbol {});
        }

        protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, Stream sectionStream)
        {
            string sectionName =
                section.Name == "rdata" ? ".rodata" :
                (section.Name.StartsWith('_') || section.Name.StartsWith('.') ? section.Name : "." + section.Name);
            int sectionIndex = _sections.Count;
            uint type = 0;
            uint flags = 0;
            SectionDefinition groupSection = null;

            if (section.Type == SectionType.Uninitialized)
            {
                type = ElfNative.SHT_NOBITS;
                flags = ElfNative.SHF_ALLOC | ElfNative.SHF_WRITE;
            }
            else if (section == ObjectNodeSection.TLSSection)
            {
                type = ElfNative.SHT_PROGBITS;
                flags = ElfNative.SHF_ALLOC | ElfNative.SHF_WRITE | ElfNative.SHF_TLS;
            }
            else
            {
                type = section.Name == ".eh_frame" && _machine == ElfNative.EM_X86_64 ? ElfNative.SHT_IA_64_UNWIND : ElfNative.SHT_PROGBITS;
                flags = section.Type switch
                {
                    SectionType.Executable => ElfNative.SHF_ALLOC | ElfNative.SHF_EXECINSTR,
                    SectionType.Writeable => ElfNative.SHF_ALLOC | ElfNative.SHF_WRITE,
                    SectionType.Debug => sectionName == ".debug_str" ? ElfNative.SHF_MERGE | ElfNative.SHF_STRINGS : 0,
                    _ => ElfNative.SHF_ALLOC,
                };
            }

            if (comdatName is not null &&
                !_comdatNameToElfSection.TryGetValue(comdatName, out groupSection))
            {
                Span<byte> tempBuffer = stackalloc byte[sizeof(uint)];
                groupSection = new SectionDefinition
                {
                    Name = ".group",
                    Type = ElfNative.SHT_GROUP,
                    Alignment = 4,
                    Stream = new MemoryStream(5 * sizeof(uint)),
                };

                // Write group flags
                BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, /*ElfNative.GRP_COMDAT*/1u);
                groupSection.Stream.Write(tempBuffer);

                _comdatNameToElfSection.Add(comdatName, groupSection);
            }

            _sections.Add(new SectionDefinition
            {
                Name = sectionName,
                Type = type,
                Flags = flags,
                Stream = sectionStream,
                GroupSection = groupSection,
            });

            // Emit section symbol into symbol table (for COMDAT the defining symbol is section symbol)
            if (comdatName is null)
            {
                _symbolNameToIndex[sectionName] = (uint)_symbols.Count;
                _symbols.Add(new ElfSymbol
                {
                    Section = _sections[sectionIndex],
                    Info = ElfNative.STT_SECTION,
                });
            }

            base.CreateSection(section, comdatName, symbolName ?? sectionName, sectionStream);
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
            SectionDefinition elfSection = _sections[sectionIndex];
            elfSection.Alignment = Math.Max(elfSection.Alignment, alignment);
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
            List<ElfSymbol> sortedSymbols = new(definedSymbols.Count + undefinedSymbols.Count);
            foreach ((string name, SymbolDefinition definition) in definedSymbols)
            {
                var section = _sections[definition.SectionIndex];
                var type =
                    (section.Flags & ElfNative.SHF_TLS) == ElfNative.SHF_TLS ? ElfNative.STT_TLS :
                    definition.Size > 0 ? ElfNative.STT_FUNC : ElfNative.STT_NOTYPE;
                sortedSymbols.Add(new ElfSymbol
                {
                    Name = name,
                    Value = (ulong)definition.Value,
                    Size = (ulong)definition.Size,
                    Section = _sections[definition.SectionIndex],
                    Info = (byte)(type | (ElfNative.STB_GLOBAL << 4)),
                    Other = definition.Global ? ElfNative.STV_DEFAULT : ElfNative.STV_HIDDEN,
                });
            }

            foreach (string externSymbol in undefinedSymbols)
            {
                if (!_symbolNameToIndex.ContainsKey(externSymbol))
                {
                    sortedSymbols.Add(new ElfSymbol
                    {
                        Name = externSymbol,
                        Info = (ElfNative.STB_GLOBAL << 4),
                    });
                }
            }

            sortedSymbols.Sort((symA, symB) => string.CompareOrdinal(symA.Name, symB.Name));
            _localSymbolCount = (uint)_symbols.Count;
            _symbols.AddRange(sortedSymbols);
            uint symbolIndex = _localSymbolCount;
            foreach (ElfSymbol definedSymbol in sortedSymbols)
            {
                _symbolNameToIndex[definedSymbol.Name] = symbolIndex;
                symbolIndex++;
            }

            // Update group sections links
            foreach ((string comdatName, SectionDefinition groupSection) in _comdatNameToElfSection)
            {
                groupSection.Info = (uint)_symbolNameToIndex[comdatName];
            }
        }

        protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            switch (_machine)
            {
                case ElfNative.EM_386:
                    EmitRelocationsX86(sectionIndex, relocationList);
                    break;
                case ElfNative.EM_X86_64:
                    EmitRelocationsX64(sectionIndex, relocationList);
                    break;
                case ElfNative.EM_AARCH64:
                    EmitRelocationsARM64(sectionIndex, relocationList);
                    break;
                default:
                    Debug.Fail("Unsupported architecture");
                    break;
            }
        }

        private void EmitRelocationsX86(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            if (relocationList.Count > 0)
            {
                Span<byte> relocationEntry = stackalloc byte[12];
                var relocationStream = new MemoryStream(12 * relocationList.Count);
                _sections[sectionIndex].RelocationStream = relocationStream;
                foreach (SymbolicRelocation symbolicRelocation in relocationList)
                {
                    uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];
                    uint type = symbolicRelocation.Type switch
                    {
                        RelocType.IMAGE_REL_BASED_HIGHLOW => ElfNative.R_386_32,
                        RelocType.IMAGE_REL_BASED_RELPTR32 => ElfNative.R_386_PC32,
                        RelocType.IMAGE_REL_BASED_REL32 => ElfNative.R_386_PLT32,
                        RelocType.IMAGE_REL_TLSGD => ElfNative.R_386_TLS_GD,
                        RelocType.IMAGE_REL_TPOFF => ElfNative.R_386_TLS_TPOFF,
                        _ => throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type)
                    };

                    long addend = symbolicRelocation.Addend;
                    if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_REL32)
                    {
                        addend -= 4;
                    }

                    BinaryPrimitives.WriteUInt32LittleEndian(relocationEntry, (uint)symbolicRelocation.Offset);
                    BinaryPrimitives.WriteUInt32LittleEndian(relocationEntry.Slice(4), ((uint)symbolIndex << 8) | type);
                    BinaryPrimitives.WriteInt32LittleEndian(relocationEntry.Slice(8), (int)addend);
                    relocationStream.Write(relocationEntry);
                }
            }
        }

        private void EmitRelocationsX64(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            if (relocationList.Count > 0)
            {
                Span<byte> relocationEntry = stackalloc byte[24];
                var relocationStream = new MemoryStream(24 * relocationList.Count);
                _sections[sectionIndex].RelocationStream = relocationStream;
                foreach (SymbolicRelocation symbolicRelocation in relocationList)
                {
                    uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];
                    uint type = symbolicRelocation.Type switch
                    {
                        RelocType.IMAGE_REL_BASED_HIGHLOW => ElfNative.R_X86_64_32,
                        RelocType.IMAGE_REL_BASED_DIR64 => ElfNative.R_X86_64_64,
                        RelocType.IMAGE_REL_BASED_RELPTR32 => ElfNative.R_X86_64_PC32,
                        RelocType.IMAGE_REL_BASED_REL32 => ElfNative.R_X86_64_PLT32,
                        RelocType.IMAGE_REL_TLSGD => ElfNative.R_X86_64_TLSGD,
                        RelocType.IMAGE_REL_TPOFF => ElfNative.R_X86_64_TPOFF32,
                        _ => throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type)
                    };

                    long addend = symbolicRelocation.Addend;
                    if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_REL32)
                    {
                        addend -= 4;
                    }

                    BinaryPrimitives.WriteUInt64LittleEndian(relocationEntry, (ulong)symbolicRelocation.Offset);
                    BinaryPrimitives.WriteUInt64LittleEndian(relocationEntry.Slice(8), ((ulong)symbolIndex << 32) | type);
                    BinaryPrimitives.WriteInt64LittleEndian(relocationEntry.Slice(16), addend);
                    relocationStream.Write(relocationEntry);
                }
            }
        }

        private void EmitRelocationsARM64(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            if (relocationList.Count > 0)
            {
                Span<byte> relocationEntry = stackalloc byte[24];
                var relocationStream = new MemoryStream(24 * relocationList.Count);
                _sections[sectionIndex].RelocationStream = relocationStream;
                foreach (SymbolicRelocation symbolicRelocation in relocationList)
                {
                    uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];
                    uint type = symbolicRelocation.Type switch
                    {
                        RelocType.IMAGE_REL_BASED_DIR64 => ElfNative.R_AARCH64_ABS64,
                        RelocType.IMAGE_REL_BASED_HIGHLOW => ElfNative.R_AARCH64_ABS32,
                        RelocType.IMAGE_REL_BASED_RELPTR32 => ElfNative.R_AARCH64_PREL32,
                        RelocType.IMAGE_REL_BASED_ARM64_BRANCH26 => ElfNative.R_AARCH64_CALL26,
                        RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 => ElfNative.R_AARCH64_ADR_PREL_PG_HI21,
                        RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A => ElfNative.R_AARCH64_ADD_ABS_LO12_NC,
                        RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_HI12 => ElfNative.R_AARCH64_TLSLE_ADD_TPREL_HI12,
                        RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_LO12_NC => ElfNative.R_AARCH64_TLSLE_ADD_TPREL_LO12_NC,
                        RelocType.IMAGE_REL_AARCH64_TLSDESC_ADR_PAGE21 => ElfNative.R_AARCH64_TLSDESC_ADR_PAGE21,
                        RelocType.IMAGE_REL_AARCH64_TLSDESC_LD64_LO12 => ElfNative.R_AARCH64_TLSDESC_LD64_LO12,
                        RelocType.IMAGE_REL_AARCH64_TLSDESC_ADD_LO12 => ElfNative.R_AARCH64_TLSDESC_ADD_LO12,
                        RelocType.IMAGE_REL_AARCH64_TLSDESC_CALL => ElfNative.R_AARCH64_TLSDESC_CALL,
                        _ => throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type)
                    };

                    BinaryPrimitives.WriteUInt64LittleEndian(relocationEntry, (ulong)symbolicRelocation.Offset);
                    BinaryPrimitives.WriteUInt64LittleEndian(relocationEntry.Slice(8), ((ulong)symbolIndex << 32) | type);
                    BinaryPrimitives.WriteInt64LittleEndian(relocationEntry.Slice(16), symbolicRelocation.Addend);
                    relocationStream.Write(relocationEntry);
                }
            }
        }

        protected override void EmitSectionsAndLayout()
        {
        }

        protected override void EmitObjectFile(string objectFilePath)
        {
            using var outputFileStream = new FileStream(objectFilePath, FileMode.Create);
            switch (_machine)
            {
                case ElfNative.EM_386:
                    EmitObjectFile<uint>(outputFileStream);
                    break;
                default:
                    EmitObjectFile<ulong>(outputFileStream);
                    break;
            }
        }

        private void EmitObjectFile<TSize>(FileStream outputFileStream)
            where TSize : struct, IBinaryInteger<TSize>
        {
            ulong sectionHeaderOffset = (ulong)ElfHeader.GetSize<TSize>();
            uint sectionCount = 1; // NULL section
            bool hasSymTabExtendedIndices = false;
            Span<byte> tempBuffer = stackalloc byte[sizeof(uint)];

            _sections.AddRange(_comdatNameToElfSection.Values);
            _sections.Add(new SectionDefinition
            {
                Name = ".note.GNU-stack",
                Type = ElfNative.SHT_PROGBITS,
                Stream = Stream.Null,
            });

            foreach (var section in _sections)
            {
                _stringTable.ReserveString(section.Name);
                section.SectionIndex = (uint)sectionCount;
                if (section.Alignment > 0)
                {
                    sectionHeaderOffset = (ulong)((sectionHeaderOffset + (ulong)section.Alignment - 1) & ~(ulong)(section.Alignment - 1));
                }
                section.SectionOffset = sectionHeaderOffset;
                if (section.Type != ElfNative.SHT_NOBITS)
                {
                    sectionHeaderOffset += (ulong)section.Stream.Length;
                }
                sectionHeaderOffset += (ulong)section.RelocationStream.Length;
                sectionCount++;
                if (section.RelocationStream != Stream.Null)
                {
                    _stringTable.ReserveString(".rela" + section.Name);
                    sectionCount++;
                }

                // Write the section index into the section's group. We store all the groups
                // at the end so we can modify their contents in this loop safely.
                if (section.GroupSection is not null)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, section.SectionIndex);
                    section.GroupSection.Stream.Write(tempBuffer);
                }
            }

            // Reserve all symbol names
            foreach (var symbol in _symbols)
            {
                if (symbol.Name is not null)
                {
                    _stringTable.ReserveString(symbol.Name);
                }
            }
            _stringTable.ReserveString(".strtab");
            _stringTable.ReserveString(".symtab");
            if (sectionCount >= ElfNative.SHN_LORESERVE)
            {
                _stringTable.ReserveString(".symtab_shndx");
                hasSymTabExtendedIndices = true;
            }

            uint strTabSectionIndex = sectionCount;
            sectionHeaderOffset += _stringTable.Size;
            sectionCount++;
            uint symTabSectionIndex = sectionCount;
            sectionHeaderOffset += (ulong)(_symbols.Count * ElfSymbol.GetSize<TSize>());
            sectionCount++;
            if (hasSymTabExtendedIndices)
            {
                sectionHeaderOffset += (ulong)(_symbols.Count * sizeof(uint));
                sectionCount++;
            }

            ElfHeader elfHeader = new ElfHeader
            {
                Type = ElfNative.ET_REL,
                Machine = _machine,
                Version = ElfNative.EV_CURRENT,
                SegmentHeaderEntrySize = 0x38,
                SectionHeaderOffset = sectionHeaderOffset,
                SectionHeaderEntrySize = (ushort)ElfSectionHeader.GetSize<TSize>(),
                SectionHeaderEntryCount = sectionCount < ElfNative.SHN_LORESERVE ? (ushort)sectionCount : (ushort)0u,
                StringTableIndex = strTabSectionIndex < ElfNative.SHN_LORESERVE ? (ushort)strTabSectionIndex : (ushort)ElfNative.SHN_XINDEX,
            };
            elfHeader.Write<TSize>(outputFileStream);

            foreach (var section in _sections)
            {
                if (section.Type != ElfNative.SHT_NOBITS)
                {
                    outputFileStream.Position = (long)section.SectionOffset;
                    section.Stream.Position = 0;
                    section.Stream.CopyTo(outputFileStream);
                    if (section.RelocationStream != Stream.Null)
                    {
                        section.RelocationStream.Position = 0;
                        section.RelocationStream.CopyTo(outputFileStream);
                    }
                }
            }

            ulong stringTableOffset = (ulong)outputFileStream.Position;
            _stringTable.Write(outputFileStream);

            ulong symbolTableOffset = (ulong)outputFileStream.Position;
            foreach (var symbol in _symbols)
            {
                symbol.Write<TSize>(outputFileStream, _stringTable);
            }

            ulong symbolTableExtendedIndicesOffset = (ulong)outputFileStream.Position;
            if (hasSymTabExtendedIndices)
            {
                foreach (var symbol in _symbols)
                {
                    uint index = symbol.Section?.SectionIndex ?? 0;
                    BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, index >= ElfNative.SHN_LORESERVE ? index : 0);
                    outputFileStream.Write(tempBuffer);
                }
            }

            // Null section
            ElfSectionHeader nullSectionHeader = new ElfSectionHeader
            {
                NameIndex = 0,
                Type = ElfNative.SHT_NULL,
                Flags = 0u,
                Address = 0u,
                Offset = 0u,
                SectionSize = sectionCount >= ElfNative.SHN_LORESERVE ? sectionCount : 0u,
                Link = strTabSectionIndex >= ElfNative.SHN_LORESERVE ? strTabSectionIndex : 0u,
                Info = 0u,
                Alignment = 0u,
                EntrySize = 0u,
            };
            nullSectionHeader.Write<TSize>(outputFileStream);

            foreach (var section in _sections)
            {
                uint groupFlag = section.GroupSection is not null ? ElfNative.SHF_GROUP : 0u;

                ElfSectionHeader sectionHeader = new ElfSectionHeader
                {
                    NameIndex = _stringTable.GetStringOffset(section.Name),
                    Type = section.Type,
                    Flags = section.Flags | groupFlag,
                    Address = 0u,
                    Offset = section.SectionOffset,
                    SectionSize = (ulong)section.Stream.Length,
                    Link = section.Type == ElfNative.SHT_GROUP ? symTabSectionIndex : 0u,
                    Info = section.Info,
                    Alignment = (ulong)section.Alignment,
                    EntrySize = section.Type == ElfNative.SHT_GROUP ? (uint)sizeof(uint) : 0u,
                };
                sectionHeader.Write<TSize>(outputFileStream);

                if (section.Type != ElfNative.SHT_NOBITS &&
                    section.RelocationStream != Stream.Null)
                {
                    sectionHeader = new ElfSectionHeader
                    {
                        NameIndex = _stringTable.GetStringOffset(".rela" + section.Name),
                        Type = ElfNative.SHT_RELA,
                        Flags = groupFlag,
                        Address = 0u,
                        Offset = section.SectionOffset + sectionHeader.SectionSize,
                        SectionSize = (ulong)section.RelocationStream.Length,
                        Link = symTabSectionIndex,
                        Info = section.SectionIndex,
                        Alignment = 8u,
                        EntrySize = 24u,
                    };
                    sectionHeader.Write<TSize>(outputFileStream);
                }
            }

            // String table section
            ElfSectionHeader stringTableSectionHeader = new ElfSectionHeader
            {
                NameIndex = _stringTable.GetStringOffset(".strtab"),
                Type = ElfNative.SHT_STRTAB,
                Flags = 0u,
                Address = 0u,
                Offset = stringTableOffset,
                SectionSize = _stringTable.Size,
                Link = 0u,
                Info = 0u,
                Alignment = 0u,
                EntrySize = 0u,
            };
            stringTableSectionHeader.Write<TSize>(outputFileStream);

            // Symbol table section
            ElfSectionHeader symbolTableSectionHeader = new ElfSectionHeader
            {
                NameIndex = _stringTable.GetStringOffset(".symtab"),
                Type = ElfNative.SHT_SYMTAB,
                Flags = 0u,
                Address = 0u,
                Offset = symbolTableOffset,
                SectionSize = (ulong)(_symbols.Count * ElfSymbol.GetSize<TSize>()),
                Link = strTabSectionIndex,
                Info = _localSymbolCount,
                Alignment = 0u,
                EntrySize = (uint)ElfSymbol.GetSize<TSize>(),
            };
            symbolTableSectionHeader.Write<TSize>(outputFileStream);

            if (hasSymTabExtendedIndices)
            {
                ElfSectionHeader sectionHeader = new ElfSectionHeader
                {
                    NameIndex = _stringTable.GetStringOffset(".symtab_shndx"),
                    Type = ElfNative.SHT_SYMTAB_SHNDX,
                    Flags = 0u,
                    Address = 0u,
                    Offset = symbolTableExtendedIndicesOffset,
                    SectionSize = (ulong)(_symbols.Count * sizeof(uint)),
                    Link = symTabSectionIndex,
                    Info = 0u,
                    Alignment = 0u,
                    EntrySize = (uint)sizeof(uint),
                };
                sectionHeader.Write<TSize>(outputFileStream);
            }
        }

        public static void EmitObject(string objectFilePath, IReadOnlyCollection<DependencyNode> nodes, NodeFactory factory, ObjectWritingOptions options, IObjectDumper dumper, Logger logger)
        {
            using ElfObjectWriter writer = new ElfObjectWriter(factory, options);
            writer.EmitObject(objectFilePath, nodes, dumper, logger);
        }

        private sealed class SectionDefinition
        {
            public string Name { get; init; }
            public uint Type { get; init; }
            public ulong Flags { get; init; }
            public int Alignment { get; set; }
            public Stream Stream { get; init; }
            public Stream RelocationStream { get; set; } = Stream.Null;
            public SectionDefinition GroupSection { get; init; }
            public uint Info { get; set; }

            // Layout
            public uint SectionIndex { get; set; }
            public ulong SectionOffset { get; set; }
        }

        private sealed class ElfHeader
        {
            private static ReadOnlySpan<byte> Magic => new byte[] { 0x7f, 0x45, 0x4c, 0x46 };

            public ushort Type { get; set; }
            public ushort Machine { get; set; }
            public uint Version { get; set; }
            public ulong EntryPoint { get; set; }
            public ulong SegmentHeaderOffset { get; set; }
            public ulong SectionHeaderOffset { get; set; }
            public uint Flags { get; set; }
            public ushort SegmentHeaderEntrySize { get; set; }
            public ushort SegmentHeaderEntryCount { get; set; }
            public ushort SectionHeaderEntrySize { get; set; }
            public ushort SectionHeaderEntryCount { get; set; }
            public ushort StringTableIndex { get; set; }

            public static int GetSize<TSize>()
                where TSize : struct, IBinaryInteger<TSize>
            {
                return
                    Magic.Length +
                    1 + // Class
                    1 + // Endianness
                    1 + // Header version
                    1 + // ABI
                    1 + // ABI version
                    7 + // Padding
                    sizeof(ushort) + // Type
                    sizeof(ushort) + // Machine
                    sizeof(uint) + // Version
                    default(TSize).GetByteCount() + // Entry point
                    default(TSize).GetByteCount() + // Segment header offset
                    default(TSize).GetByteCount() + // Section header offset
                    sizeof(uint) + // Flags
                    sizeof(ushort) + // ELF Header size
                    sizeof(ushort) + // Segment header entry size
                    sizeof(ushort) + // Segment header entry count
                    sizeof(ushort) + // Section header entry size
                    sizeof(ushort) + // Section header entry count
                    sizeof(ushort); // String table index
            }

            public void Write<TSize>(FileStream stream)
                where TSize : struct, IBinaryInteger<TSize>
            {
                Span<byte> buffer = stackalloc byte[GetSize<TSize>()];

                buffer.Clear();
                Magic.CopyTo(buffer.Slice(0, Magic.Length));
                buffer[4] = typeof(TSize) == typeof(uint) ? ElfNative.ELFCLASS32 : ElfNative.ELFCLASS64;
                buffer[5] = ElfNative.ELFDATA2LSB;
                buffer[6] = 1;
                var tempBuffer = buffer.Slice(16);
                tempBuffer = tempBuffer.Slice(((IBinaryInteger<ushort>)Type).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(((IBinaryInteger<ushort>)Machine).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(((IBinaryInteger<uint>)Version).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(TSize.CreateChecked(EntryPoint).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(TSize.CreateChecked(SegmentHeaderOffset).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(TSize.CreateChecked(SectionHeaderOffset).WriteLittleEndian(tempBuffer));
                BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, Flags);
                BinaryPrimitives.WriteUInt16LittleEndian(tempBuffer.Slice(4), (ushort)buffer.Length);
                BinaryPrimitives.WriteUInt16LittleEndian(tempBuffer.Slice(6), SegmentHeaderEntrySize);
                BinaryPrimitives.WriteUInt16LittleEndian(tempBuffer.Slice(8), SegmentHeaderEntryCount);
                BinaryPrimitives.WriteUInt16LittleEndian(tempBuffer.Slice(10), SectionHeaderEntrySize);
                BinaryPrimitives.WriteUInt16LittleEndian(tempBuffer.Slice(12), SectionHeaderEntryCount);
                BinaryPrimitives.WriteUInt16LittleEndian(tempBuffer.Slice(14), StringTableIndex);

                stream.Write(buffer);
            }
        }

        private sealed class ElfSectionHeader
        {
            public uint NameIndex { get; set; }
            public uint Type { get; set; }
            public ulong Flags { get; set; }
            public ulong Address { get; set; }
            public ulong Offset { get; set; }
            public ulong SectionSize { get; set; }
            public uint Link { get; set; }
            public uint Info { get; set; }
            public ulong Alignment { get; set; }
            public ulong EntrySize { get; set; }

            public static int GetSize<TSize>()
                where TSize : struct, IBinaryInteger<TSize>
            {
                return
                    sizeof(uint) + // Name index
                    sizeof(uint) + // Type
                    default(TSize).GetByteCount() + // Flags
                    default(TSize).GetByteCount() + // Address
                    default(TSize).GetByteCount() + // Offset
                    default(TSize).GetByteCount() + // Size
                    sizeof(uint) + // Link
                    sizeof(uint) + // Info
                    default(TSize).GetByteCount() + // Alignment
                    default(TSize).GetByteCount(); // Entry size
            }

            public void Write<TSize>(FileStream stream)
                where TSize : struct, IBinaryInteger<TSize>
            {
                Span<byte> buffer = stackalloc byte[GetSize<TSize>()];
                var tempBuffer = buffer;

                tempBuffer = tempBuffer.Slice(((IBinaryInteger<uint>)NameIndex).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(((IBinaryInteger<uint>)Type).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(TSize.CreateChecked(Flags).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(TSize.CreateChecked(Address).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(TSize.CreateChecked(Offset).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(TSize.CreateChecked(SectionSize).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(((IBinaryInteger<uint>)Link).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(((IBinaryInteger<uint>)Info).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(TSize.CreateChecked(Alignment).WriteLittleEndian(tempBuffer));
                tempBuffer = tempBuffer.Slice(TSize.CreateChecked(EntrySize).WriteLittleEndian(tempBuffer));

                stream.Write(buffer);
            }
        }

        private sealed class ElfSymbol
        {
            public string Name { get; init; }
            public ulong Value { get; init; }
            public ulong Size { get; init; }
            public SectionDefinition Section { get; init; }
            public byte Info { get; init; }
            public byte Other { get; init; }

            public static int GetSize<TSize>()
                where TSize : struct, IBinaryInteger<TSize>
            {
                return typeof(TSize) == typeof(uint) ? 16 : 24;
            }

            public void Write<TSize>(FileStream stream, ElfStringTable stringTable)
                where TSize : struct, IBinaryInteger<TSize>
            {
                Span<byte> buffer = stackalloc byte[GetSize<TSize>()];
                ushort sectionIndex;

                sectionIndex = Section is { SectionIndex: >= ElfNative.SHN_LORESERVE } ?
                    (ushort)ElfNative.SHN_XINDEX :
                    (Section is not null ? (ushort)Section.SectionIndex : (ushort)0u);

                BinaryPrimitives.WriteUInt32LittleEndian(buffer, Name is not null ? stringTable.GetStringOffset(Name) : 0);
                if (typeof(TSize) == typeof(uint))
                {
                    TSize.CreateChecked(Value).WriteLittleEndian(buffer.Slice(4));
                    TSize.CreateChecked(Size).WriteLittleEndian(buffer.Slice(8));
                    buffer[12] = Info;
                    buffer[13] = Other;
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(14), sectionIndex);
                }
                else
                {
                    buffer[4] = Info;
                    buffer[5] = Other;
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(6), sectionIndex);
                    BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(8), Value);
                    BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(16), Size);
                }

                stream.Write(buffer);
            }
        }

        private sealed class ElfStringTable : StringTableBuilder
        {
            public ElfStringTable()
            {
                // Always start the table with empty string
                GetStringOffset("");
            }
        }
    }
}
