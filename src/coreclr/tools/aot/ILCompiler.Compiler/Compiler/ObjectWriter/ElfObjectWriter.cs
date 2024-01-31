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
using static ILCompiler.DependencyAnalysis.RelocType;
using static ILCompiler.ObjectWriter.EabiNative;
using static ILCompiler.ObjectWriter.ElfNative;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// ELF object file format writer for Linux/Unix targets.
    /// </summary>
    /// <remarks>
    /// ELF object format is described by the official specification hosted
    /// at https://refspecs.linuxfoundation.org/elf/elf.pdf. Different
    /// architectures specify the details in the ABI specification.
    ///
    /// Like COFF there are several quirks related to large number of sections
    /// (> 65279). Some of the fields in the ELF file header are moved to the
    /// first (NULL) section header. The symbol table that is normally a single
    /// section in the file is extended with a second .symtab_shndx section
    /// to accomodate the section indexes that don't fit within the regular
    /// section number field.
    /// </remarks>
    internal sealed class ElfObjectWriter : UnixObjectWriter
    {
        private readonly bool _useInlineRelocationAddends;
        private readonly ushort _machine;
        private readonly List<ElfSectionDefinition> _sections = new();
        private readonly List<ElfSymbol> _symbols = new();
        private uint _localSymbolCount;
        private readonly Dictionary<string, ElfSectionDefinition> _comdatNameToElfSection = new(StringComparer.Ordinal);

        // Symbol table
        private readonly Dictionary<string, uint> _symbolNameToIndex = new();

        private Dictionary<int, (SectionWriter ExidxSectionWriter, SectionWriter ExtabSectionWriter)> _armUnwindSections;
        private static readonly ObjectNodeSection ArmUnwindIndexSection = new ObjectNodeSection(".ARM.exidx", SectionType.UnwindData);
        private static readonly ObjectNodeSection ArmUnwindTableSection = new ObjectNodeSection(".ARM.extab", SectionType.ReadOnly);
        private static readonly ObjectNodeSection ArmAttributesSection = new ObjectNodeSection(".ARM.attributes", SectionType.ReadOnly);

        public ElfObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
            _machine = factory.Target.Architecture switch
            {
                TargetArchitecture.X86 => EM_386,
                TargetArchitecture.X64 => EM_X86_64,
                TargetArchitecture.ARM => EM_ARM,
                TargetArchitecture.ARM64 => EM_AARCH64,
                _ => throw new NotSupportedException("Unsupported architecture")
            };
            _useInlineRelocationAddends = _machine is EM_386 or EM_ARM;

            // By convention the symbol table starts with empty symbol
            _symbols.Add(new ElfSymbol {});
        }

        private protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, Stream sectionStream)
        {
            string sectionName =
                section.Name == "rdata" ? ".rodata" :
                (section.Name.StartsWith('_') || section.Name.StartsWith('.') ? section.Name : "." + section.Name);
            int sectionIndex = _sections.Count;
            uint type = 0;
            uint flags = 0;
            ElfSectionDefinition groupSection = null;

            if (section.Type == SectionType.Uninitialized)
            {
                type = SHT_NOBITS;
                flags = SHF_ALLOC | SHF_WRITE;
            }
            else if (section == ObjectNodeSection.TLSSection)
            {
                type = SHT_PROGBITS;
                flags = SHF_ALLOC | SHF_WRITE | SHF_TLS;
            }
            else if (section == ArmAttributesSection)
            {
                type = SHT_ARM_ATTRIBUTES;
            }
            else if (_machine == EM_ARM && section.Type == SectionType.UnwindData)
            {
                type = SHT_ARM_EXIDX;
                flags = SHF_ALLOC | SHF_LINK_ORDER;
            }
            else
            {
                type = section.Type == SectionType.UnwindData && _machine == EM_X86_64 ? SHT_IA_64_UNWIND : SHT_PROGBITS;
                flags = section.Type switch
                {
                    SectionType.Executable => SHF_ALLOC | SHF_EXECINSTR,
                    SectionType.Writeable => SHF_ALLOC | SHF_WRITE,
                    SectionType.Debug => sectionName == ".debug_str" ? SHF_MERGE | SHF_STRINGS : 0,
                    _ => SHF_ALLOC,
                };
            }

            if (comdatName is not null)
            {
                flags |= SHF_GROUP;
                if (!_comdatNameToElfSection.TryGetValue(comdatName, out groupSection))
                {
                    Span<byte> tempBuffer = stackalloc byte[sizeof(uint)];
                    groupSection = new ElfSectionDefinition
                    {
                        SectionHeader = new ElfSectionHeader
                        {
                            Type = SHT_GROUP,
                            Alignment = 4,
                            EntrySize = (uint)sizeof(uint),
                        },
                        Name = ".group",
                        Stream = new MemoryStream(5 * sizeof(uint)),
                    };

                    // Write group flags
                    BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, GRP_COMDAT);
                    groupSection.Stream.Write(tempBuffer);

                    _comdatNameToElfSection.Add(comdatName, groupSection);
                }
            }

            _sections.Add(new ElfSectionDefinition
            {
                SectionHeader = new ElfSectionHeader
                {
                    Type = type,
                    Flags = flags,
                },
                Name = sectionName,
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
                    Info = STT_SECTION,
                });
            }

            if (_machine is EM_ARM && section.Type is SectionType.Executable)
            {
                _symbols.Add(new ElfSymbol
                {
                    Section = _sections[sectionIndex],
                    Info = STT_NOTYPE,
                    Name = $"$t.{sectionIndex}"
                });
            }

            base.CreateSection(section, comdatName, symbolName ?? sectionName, sectionStream);
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
            ElfSectionDefinition elfSection = _sections[sectionIndex];
            elfSection.SectionHeader.Alignment = Math.Max(elfSection.SectionHeader.Alignment, (ulong)alignment);
        }

        protected internal override unsafe void EmitRelocation(
            int sectionIndex,
            long offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            long addend)
        {
            fixed (byte *pData = data)
            {
                addend -= relocType switch
                {
                    IMAGE_REL_BASED_REL32 => 4,
                    IMAGE_REL_BASED_THUMB_BRANCH24 => 4,
                    IMAGE_REL_BASED_THUMB_MOV32_PCREL => 12,
                    _ => 0
                };

                if (!_useInlineRelocationAddends)
                {
                    // We read the addend from the data and clear it. This is necessary
                    // to produce correct addends in the `.rela` sections which override
                    // the destination with the addend from relocation table.
                    long inlineValue = Relocation.ReadValue(relocType, (void*)pData);
                    if (inlineValue != 0)
                    {
                        addend += inlineValue;
                        Relocation.WriteValue(relocType, (void*)pData, 0);
                    }
                }
                else
                {
                    if (relocType is IMAGE_REL_BASED_THUMB_MOV32_PCREL or IMAGE_REL_BASED_THUMB_MOV32)
                    {
                        long inlineValue = Relocation.ReadValue(relocType, (void*)pData);
                        addend += inlineValue;
                        Debug.Assert(addend >= short.MinValue && addend <= short.MaxValue);
                        // This expands into two relocations where each of them has to have the
                        // same base 16-bit addend + PC difference between the two instructions.
                        addend = relocType is IMAGE_REL_BASED_THUMB_MOV32_PCREL ?
                             (long)((((uint)(addend + 4) & 0xffff) << 16) + (ushort)addend) :
                             (long)((((uint)(addend) & 0xffff) << 16) + (ushort)addend);
                        Relocation.WriteValue(relocType, (void*)pData, addend);
                    }
                    else if (addend != 0)
                    {
                        long inlineValue = Relocation.ReadValue(relocType, (void*)pData);
                        Relocation.WriteValue(relocType, (void*)pData, inlineValue + addend);
                    }
                    addend = 0;
                }
            }

            base.EmitRelocation(sectionIndex, offset, data, relocType, symbolName, addend);
        }

        private protected override void EmitSymbolTable(
            IDictionary<string, SymbolDefinition> definedSymbols,
            SortedSet<string> undefinedSymbols)
        {
            List<ElfSymbol> sortedSymbols = new(definedSymbols.Count + undefinedSymbols.Count);
            foreach ((string name, SymbolDefinition definition) in definedSymbols)
            {
                var section = _sections[definition.SectionIndex];
                var type =
                    (section.SectionHeader.Flags & SHF_TLS) == SHF_TLS ? STT_TLS :
                    definition.Size > 0 ? STT_FUNC : STT_NOTYPE;
                sortedSymbols.Add(new ElfSymbol
                {
                    Name = name,
                    Value = (ulong)definition.Value,
                    Size = (ulong)definition.Size,
                    Section = _sections[definition.SectionIndex],
                    Info = (byte)(type | (STB_GLOBAL << 4)),
                    Other = definition.Global ? STV_DEFAULT : STV_HIDDEN,
                });
            }

            foreach (string externSymbol in undefinedSymbols)
            {
                if (!_symbolNameToIndex.ContainsKey(externSymbol))
                {
                    sortedSymbols.Add(new ElfSymbol
                    {
                        Name = externSymbol,
                        Info = (STB_GLOBAL << 4),
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
            foreach ((string comdatName, ElfSectionDefinition groupSection) in _comdatNameToElfSection)
            {
                groupSection.SectionHeader.Info = (uint)_symbolNameToIndex[comdatName];
            }
        }

        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            switch (_machine)
            {
                case EM_386:
                    EmitRelocationsX86(sectionIndex, relocationList);
                    break;
                case EM_X86_64:
                    EmitRelocationsX64(sectionIndex, relocationList);
                    break;
                case EM_ARM:
                    EmitRelocationsARM(sectionIndex, relocationList);
                    break;
                case EM_AARCH64:
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
                Span<byte> relocationEntry = stackalloc byte[8];
                var relocationStream = new MemoryStream(8 * relocationList.Count);
                _sections[sectionIndex].RelocationStream = relocationStream;
                foreach (SymbolicRelocation symbolicRelocation in relocationList)
                {
                    uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];
                    uint type = symbolicRelocation.Type switch
                    {
                        IMAGE_REL_BASED_HIGHLOW => R_386_32,
                        IMAGE_REL_BASED_RELPTR32 => R_386_PC32,
                        IMAGE_REL_BASED_REL32 => R_386_PLT32,
                        IMAGE_REL_TLSGD => R_386_TLS_GD,
                        IMAGE_REL_TPOFF => R_386_TLS_TPOFF,
                        _ => throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type)
                    };

                    BinaryPrimitives.WriteUInt32LittleEndian(relocationEntry, (uint)symbolicRelocation.Offset);
                    BinaryPrimitives.WriteUInt32LittleEndian(relocationEntry.Slice(4), ((uint)symbolIndex << 8) | type);
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
                        IMAGE_REL_BASED_HIGHLOW => R_X86_64_32,
                        IMAGE_REL_BASED_DIR64 => R_X86_64_64,
                        IMAGE_REL_BASED_RELPTR32 => R_X86_64_PC32,
                        IMAGE_REL_BASED_REL32 => R_X86_64_PLT32,
                        IMAGE_REL_TLSGD => R_X86_64_TLSGD,
                        IMAGE_REL_TPOFF => R_X86_64_TPOFF32,
                        _ => throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type)
                    };

                    BinaryPrimitives.WriteUInt64LittleEndian(relocationEntry, (ulong)symbolicRelocation.Offset);
                    BinaryPrimitives.WriteUInt64LittleEndian(relocationEntry.Slice(8), ((ulong)symbolIndex << 32) | type);
                    BinaryPrimitives.WriteInt64LittleEndian(relocationEntry.Slice(16), symbolicRelocation.Addend);
                    relocationStream.Write(relocationEntry);
                }
            }
        }

        private void EmitRelocationsARM(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            if (relocationList.Count > 0)
            {
                Span<byte> relocationEntry = stackalloc byte[8];
                var relocationStream = new MemoryStream(8 * relocationList.Count);
                _sections[sectionIndex].RelocationStream = relocationStream;
                foreach (SymbolicRelocation symbolicRelocation in relocationList)
                {
                    uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];
                    uint type = symbolicRelocation.Type switch
                    {
                        IMAGE_REL_BASED_ABSOLUTE => R_ARM_NONE,
                        IMAGE_REL_BASED_HIGHLOW => R_ARM_ABS32,
                        IMAGE_REL_BASED_RELPTR32 => R_ARM_REL32,
                        IMAGE_REL_BASED_REL32 => R_ARM_REL32,
                        IMAGE_REL_BASED_THUMB_MOV32 => R_ARM_THM_MOVW_ABS_NC,
                        IMAGE_REL_BASED_THUMB_MOV32_PCREL => R_ARM_THM_MOVW_PREL_NC,
                        IMAGE_REL_BASED_THUMB_BRANCH24 => R_ARM_THM_CALL,
                        IMAGE_REL_ARM_PREL31 => R_ARM_PREL31,
                        _ => throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type)
                    };

                    BinaryPrimitives.WriteUInt32LittleEndian(relocationEntry, (uint)symbolicRelocation.Offset);
                    BinaryPrimitives.WriteUInt32LittleEndian(relocationEntry.Slice(4), ((uint)symbolIndex << 8) | type);
                    relocationStream.Write(relocationEntry);

                    if (symbolicRelocation.Type is IMAGE_REL_BASED_THUMB_MOV32 or IMAGE_REL_BASED_THUMB_MOV32_PCREL)
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(relocationEntry, (uint)(symbolicRelocation.Offset + 4));
                        BinaryPrimitives.WriteUInt32LittleEndian(relocationEntry.Slice(4), ((uint)symbolIndex << 8) | (type + 1));
                        relocationStream.Write(relocationEntry);
                    }
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
                        IMAGE_REL_BASED_DIR64 => R_AARCH64_ABS64,
                        IMAGE_REL_BASED_HIGHLOW => R_AARCH64_ABS32,
                        IMAGE_REL_BASED_RELPTR32 => R_AARCH64_PREL32,
                        IMAGE_REL_BASED_ARM64_BRANCH26 => R_AARCH64_CALL26,
                        IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 => R_AARCH64_ADR_PREL_PG_HI21,
                        IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A => R_AARCH64_ADD_ABS_LO12_NC,
                        IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_HI12 => R_AARCH64_TLSLE_ADD_TPREL_HI12,
                        IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_LO12_NC => R_AARCH64_TLSLE_ADD_TPREL_LO12_NC,
                        IMAGE_REL_AARCH64_TLSDESC_ADR_PAGE21 => R_AARCH64_TLSDESC_ADR_PAGE21,
                        IMAGE_REL_AARCH64_TLSDESC_LD64_LO12 => R_AARCH64_TLSDESC_LD64_LO12,
                        IMAGE_REL_AARCH64_TLSDESC_ADD_LO12 => R_AARCH64_TLSDESC_ADD_LO12,
                        IMAGE_REL_AARCH64_TLSDESC_CALL => R_AARCH64_TLSDESC_CALL,
                        _ => throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type)
                    };

                    BinaryPrimitives.WriteUInt64LittleEndian(relocationEntry, (ulong)symbolicRelocation.Offset);
                    BinaryPrimitives.WriteUInt64LittleEndian(relocationEntry.Slice(8), ((ulong)symbolIndex << 32) | type);
                    BinaryPrimitives.WriteInt64LittleEndian(relocationEntry.Slice(16), symbolicRelocation.Addend);
                    relocationStream.Write(relocationEntry);
                }
            }
        }

        private protected override void EmitSectionsAndLayout()
        {
            if (_machine == EM_ARM)
            {
                // Emit EABI attributes section
                // (Addenda to, and Errata in, the ABI for the Arm Architecture, 2023Q3)
                SectionWriter sectionWriter = GetOrCreateSection(ArmAttributesSection);
                EabiAttributesBuilder attributesBuilder = new EabiAttributesBuilder(sectionWriter);
                attributesBuilder.StartSection("aeabi");
                attributesBuilder.WriteAttribute(Tag_conformance, "2.09");
                attributesBuilder.WriteAttribute(Tag_CPU_name, "arm7tdmi");
                attributesBuilder.WriteAttribute(Tag_CPU_arch, 2); // Arm v4T / Arm7TDMI
                attributesBuilder.WriteAttribute(Tag_ARM_ISA_use, 1); // Yes
                attributesBuilder.WriteAttribute(Tag_THUMB_ISA_use, 3); // Yes, determined by Tag_CPU_arch / Tag_CPU_arch_profile
                attributesBuilder.WriteAttribute(Tag_ABI_PCS_R9_use, 0); // R9 is callee-saved register
                attributesBuilder.WriteAttribute(Tag_ABI_PCS_RW_data, 1); // PC-relative
                attributesBuilder.WriteAttribute(Tag_ABI_PCS_RO_data, 1); // PC-relative
                attributesBuilder.WriteAttribute(Tag_ABI_PCS_GOT_use, 2); // indirect
                attributesBuilder.WriteAttribute(Tag_ABI_FP_denormal, 1);
                attributesBuilder.WriteAttribute(Tag_ABI_FP_exceptions, 0); // Unused
                attributesBuilder.WriteAttribute(Tag_ABI_FP_number_model, 3); // IEEE 754
                attributesBuilder.WriteAttribute(Tag_ABI_align_needed, 1); // 8-byte
                attributesBuilder.WriteAttribute(Tag_ABI_align_preserved, 1); // 8-byte
                attributesBuilder.WriteAttribute(Tag_CPU_unaligned_access, 0); // None
                attributesBuilder.EndSection();
            }
        }

        private protected override void CreateEhSections()
        {
            // ARM creates the EHABI sections lazily in EmitUnwindInfo
            if (_machine is not EM_ARM)
            {
                base.CreateEhSections();
            }
        }

        private protected override void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            string currentSymbolName)
        {
            if (_machine is not EM_ARM)
            {
                base.EmitUnwindInfo(sectionWriter, nodeWithCodeInfo, currentSymbolName);
                return;
            }

            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode)
            {
                SectionWriter exidxSectionWriter;
                SectionWriter extabSectionWriter;

                if (ShouldShareSymbol((ObjectNode)nodeWithCodeInfo))
                {
                    exidxSectionWriter = GetOrCreateSection(ArmUnwindIndexSection, currentSymbolName, $"_unwind0{currentSymbolName}");
                    extabSectionWriter = GetOrCreateSection(ArmUnwindTableSection, currentSymbolName, $"_extab0{currentSymbolName}");
                    _sections[exidxSectionWriter.SectionIndex].LinkSection = _sections[sectionWriter.SectionIndex];
                }
                else
                {
                    _armUnwindSections ??= new();
                    if (_armUnwindSections.TryGetValue(sectionWriter.SectionIndex, out var unwindSections))
                    {
                        exidxSectionWriter = unwindSections.ExidxSectionWriter;
                        extabSectionWriter = unwindSections.ExtabSectionWriter;
                    }
                    else
                    {
                        string sectionName = _sections[sectionWriter.SectionIndex].Name;
                        exidxSectionWriter = GetOrCreateSection(new ObjectNodeSection($"{ArmUnwindIndexSection.Name}{sectionName}", ArmUnwindIndexSection.Type));
                        extabSectionWriter = GetOrCreateSection(new ObjectNodeSection($"{ArmUnwindTableSection.Name}{sectionName}", ArmUnwindTableSection.Type));
                        _sections[exidxSectionWriter.SectionIndex].LinkSection = _sections[sectionWriter.SectionIndex];
                        _armUnwindSections.Add(sectionWriter.SectionIndex, (exidxSectionWriter, extabSectionWriter));
                    }
                }

                long mainLsdaOffset = 0;
                Span<byte> unwindWord = stackalloc byte[4];
                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];
                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    byte[] blob = frameInfo.BlobData;

                    string framSymbolName = $"_fram{i}{currentSymbolName}";
                    string extabSymbolName = $"_extab{i}{currentSymbolName}";

                    sectionWriter.EmitSymbolDefinition(framSymbolName, start);

                    // Emit the index info
                    exidxSectionWriter.EmitSymbolReference(IMAGE_REL_ARM_PREL31, framSymbolName);
                    exidxSectionWriter.EmitSymbolReference(IMAGE_REL_ARM_PREL31, extabSymbolName);

                    Span<byte> armUnwindInfo = EabiUnwindConverter.ConvertCFIToEabi(blob);
                    string personalitySymbolName;

                    if (armUnwindInfo.Length <= 3)
                    {
                        personalitySymbolName = "__aeabi_unwind_cpp_pr0";
                        unwindWord[3] = 0x80;
                        unwindWord[2] = (byte)(armUnwindInfo.Length > 0 ? armUnwindInfo[0] : 0xB0);
                        unwindWord[1] = (byte)(armUnwindInfo.Length > 1 ? armUnwindInfo[1] : 0xB0);
                        unwindWord[0] = (byte)(armUnwindInfo.Length > 2 ? armUnwindInfo[2] : 0xB0);
                        armUnwindInfo = Span<byte>.Empty;
                    }
                    else
                    {
                        Debug.Assert(armUnwindInfo.Length <= 1024);
                        personalitySymbolName = "__aeabi_unwind_cpp_pr1";
                        unwindWord[3] = 0x81;
                        unwindWord[2] = (byte)(((armUnwindInfo.Length - 2) + 3) / 4);
                        unwindWord[1] = armUnwindInfo[0];
                        unwindWord[0] = armUnwindInfo[1];
                        armUnwindInfo = armUnwindInfo.Slice(2);
                    }

                    extabSectionWriter.EmitAlignment(4);
                    extabSectionWriter.EmitSymbolDefinition(extabSymbolName);

                    // ARM EHABI requires emitting a dummy relocation to the personality routine
                    // to tell the linker to preserve it.
                    extabSectionWriter.EmitRelocation(0, unwindWord, IMAGE_REL_BASED_ABSOLUTE, personalitySymbolName, 0);

                    // Emit the unwinding code. First word specifies the personality routine,
                    // format and first few bytes of the unwind code. For longer unwind codes
                    // the other words follow. They are padded with the "finish" instruction
                    // (0xB0).
                    extabSectionWriter.Write(unwindWord);
                    while (armUnwindInfo.Length > 0)
                    {
                        unwindWord[3] = (byte)(armUnwindInfo.Length > 0 ? armUnwindInfo[0] : 0xB0);
                        unwindWord[2] = (byte)(armUnwindInfo.Length > 1 ? armUnwindInfo[1] : 0xB0);
                        unwindWord[1] = (byte)(armUnwindInfo.Length > 2 ? armUnwindInfo[2] : 0xB0);
                        unwindWord[0] = (byte)(armUnwindInfo.Length > 3 ? armUnwindInfo[3] : 0xB0);
                        extabSectionWriter.Write(unwindWord);
                        armUnwindInfo = armUnwindInfo.Length > 3 ? armUnwindInfo.Slice(4) : Span<byte>.Empty;
                    }

                    // Emit our LSDA info directly into the exception table
                    EmitLsda(nodeWithCodeInfo, frameInfos, i, extabSectionWriter, ref mainLsdaOffset);
                }
            }
        }

        private protected override void EmitObjectFile(string objectFilePath)
        {
            using var outputFileStream = new FileStream(objectFilePath, FileMode.Create);
            switch (_machine)
            {
                case EM_386:
                case EM_ARM:
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
            ElfStringTable _stringTable = new();
            uint sectionCount = 1; // NULL section
            bool hasSymTabExtendedIndices = false;
            Span<byte> tempBuffer = stackalloc byte[sizeof(uint)];

            // Merge the group sections at the end of the section lists
            _sections.AddRange(_comdatNameToElfSection.Values);

            // Add marker for non-executable stack
            _sections.Add(new ElfSectionDefinition
            {
                SectionHeader = new ElfSectionHeader { Type = SHT_PROGBITS },
                Name = ".note.GNU-stack",
                Stream = Stream.Null,
            });

            // Reserve all symbol names
            foreach (var symbol in _symbols)
            {
                if (symbol.Name is not null)
                {
                    _stringTable.ReserveString(symbol.Name);
                }
            }

            // Layout the section content in the output file
            ulong currentOffset = (ulong)ElfHeader.GetSize<TSize>();
            foreach (var section in _sections)
            {
                _stringTable.ReserveString(section.Name);

                if (section.SectionHeader.Alignment > 0)
                {
                    currentOffset = (ulong)((currentOffset + (ulong)section.SectionHeader.Alignment - 1) & ~(ulong)(section.SectionHeader.Alignment - 1));
                }

                // Update section layout
                section.SectionIndex = (uint)sectionCount;
                section.SectionHeader.Offset = currentOffset;
                section.SectionHeader.Size = (ulong)section.Stream.Length;

                if (section.SectionHeader.Type != SHT_NOBITS)
                {
                    currentOffset += (ulong)section.Stream.Length;
                }
                currentOffset += (ulong)section.RelocationStream.Length;
                sectionCount++;

                if (section.RelocationStream != Stream.Null)
                {
                    _stringTable.ReserveString((_useInlineRelocationAddends ? ".rel" : ".rela") + section.Name);
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

            // Reserve names for the predefined sections
            _stringTable.ReserveString(".strtab");
            _stringTable.ReserveString(".symtab");
            if (sectionCount >= SHN_LORESERVE)
            {
                _stringTable.ReserveString(".symtab_shndx");
                hasSymTabExtendedIndices = true;
            }

            // Layout the string and symbol table
            uint strTabSectionIndex = sectionCount;
            currentOffset += _stringTable.Size;
            sectionCount++;
            uint symTabSectionIndex = sectionCount;
            currentOffset += (ulong)(_symbols.Count * ElfSymbol.GetSize<TSize>());
            sectionCount++;
            if (hasSymTabExtendedIndices)
            {
                currentOffset += (ulong)(_symbols.Count * sizeof(uint));
                sectionCount++;
            }

            // Update group section links
            foreach (ElfSectionDefinition groupSection in _comdatNameToElfSection.Values)
            {
                groupSection.SectionHeader.Link = symTabSectionIndex;
            }

            // Write the ELF file header
            ElfHeader elfHeader = new ElfHeader
            {
                Type = ET_REL,
                Machine = _machine,
                Version = EV_CURRENT,
                SegmentHeaderEntrySize = 0x38,
                SectionHeaderOffset = currentOffset,
                SectionHeaderEntrySize = (ushort)ElfSectionHeader.GetSize<TSize>(),
                SectionHeaderEntryCount = sectionCount < SHN_LORESERVE ? (ushort)sectionCount : (ushort)0u,
                StringTableIndex = strTabSectionIndex < SHN_LORESERVE ? (ushort)strTabSectionIndex : (ushort)SHN_XINDEX,
                // For ARM32 claim conformance with the EABI specification
                Flags = _machine is EM_ARM ? 0x05000000u : 0u,
            };
            elfHeader.Write<TSize>(outputFileStream);

            // Write the section contents and relocations
            foreach (var section in _sections)
            {
                if (section.SectionHeader.Type != SHT_NOBITS)
                {
                    outputFileStream.Position = (long)section.SectionHeader.Offset;
                    section.Stream.Position = 0;
                    section.Stream.CopyTo(outputFileStream);
                    if (section.RelocationStream != Stream.Null)
                    {
                        section.RelocationStream.Position = 0;
                        section.RelocationStream.CopyTo(outputFileStream);
                    }
                }
            }

            // Write the string and symbol table contents
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
                    BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, index >= SHN_LORESERVE ? index : 0);
                    outputFileStream.Write(tempBuffer);
                }
            }

            // Finally, write the section headers

            // Null section
            ElfSectionHeader nullSectionHeader = new ElfSectionHeader
            {
                NameIndex = 0,
                Type = SHT_NULL,
                Flags = 0u,
                Address = 0u,
                Offset = 0u,
                Size = sectionCount >= SHN_LORESERVE ? sectionCount : 0u,
                Link = strTabSectionIndex >= SHN_LORESERVE ? strTabSectionIndex : 0u,
                Info = 0u,
                Alignment = 0u,
                EntrySize = 0u,
            };
            nullSectionHeader.Write<TSize>(outputFileStream);

            // User sections and their relocations
            foreach (var section in _sections)
            {
                if (section.LinkSection != null)
                {
                    section.SectionHeader.Link = section.LinkSection.SectionIndex;
                }
                section.SectionHeader.NameIndex = _stringTable.GetStringOffset(section.Name);
                section.SectionHeader.Write<TSize>(outputFileStream);

                if (section.SectionHeader.Type != SHT_NOBITS &&
                    section.RelocationStream != Stream.Null)
                {
                    ElfSectionHeader relaSectionHeader = new ElfSectionHeader
                    {
                        NameIndex = _stringTable.GetStringOffset((_useInlineRelocationAddends ? ".rel" : ".rela") + section.Name),
                        Type = _useInlineRelocationAddends ? SHT_REL : SHT_RELA,
                        Flags = (section.GroupSection is not null ? SHF_GROUP : 0u) | SHF_INFO_LINK,
                        Address = 0u,
                        Offset = section.SectionHeader.Offset + section.SectionHeader.Size,
                        Size = (ulong)section.RelocationStream.Length,
                        Link = symTabSectionIndex,
                        Info = section.SectionIndex,
                        Alignment = 8u,
                        EntrySize = (ulong)default(TSize).GetByteCount() * (_useInlineRelocationAddends ? 2u : 3u),
                    };
                    relaSectionHeader.Write<TSize>(outputFileStream);
                }
            }

            // String table section
            ElfSectionHeader stringTableSectionHeader = new ElfSectionHeader
            {
                NameIndex = _stringTable.GetStringOffset(".strtab"),
                Type = SHT_STRTAB,
                Flags = 0u,
                Address = 0u,
                Offset = stringTableOffset,
                Size = _stringTable.Size,
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
                Type = SHT_SYMTAB,
                Flags = 0u,
                Address = 0u,
                Offset = symbolTableOffset,
                Size = (ulong)(_symbols.Count * ElfSymbol.GetSize<TSize>()),
                Link = strTabSectionIndex,
                Info = _localSymbolCount,
                Alignment = 0u,
                EntrySize = (uint)ElfSymbol.GetSize<TSize>(),
            };
            symbolTableSectionHeader.Write<TSize>(outputFileStream);

            // If the symbol table has references to sections with indexes higher than
            // SHN_LORESERVE (0xFF00) we need to write them down in a separate table
            // in the .symtab_shndx section.
            if (hasSymTabExtendedIndices)
            {
                ElfSectionHeader sectionHeader = new ElfSectionHeader
                {
                    NameIndex = _stringTable.GetStringOffset(".symtab_shndx"),
                    Type = SHT_SYMTAB_SHNDX,
                    Flags = 0u,
                    Address = 0u,
                    Offset = symbolTableExtendedIndicesOffset,
                    Size = (ulong)(_symbols.Count * sizeof(uint)),
                    Link = symTabSectionIndex,
                    Info = 0u,
                    Alignment = 0u,
                    EntrySize = (uint)sizeof(uint),
                };
                sectionHeader.Write<TSize>(outputFileStream);
            }
        }

        private sealed class ElfSectionDefinition
        {
            public required ElfSectionHeader SectionHeader { get; init; }
            public uint SectionIndex { get; set; }
            public required string Name { get; init; }
            public required Stream Stream { get; init; }
            public Stream RelocationStream { get; set; } = Stream.Null;
            public ElfSectionDefinition GroupSection { get; init; }
            public ElfSectionDefinition LinkSection { get; set; }
        }

        private sealed class ElfHeader
        {
            private static ReadOnlySpan<byte> Magic => new byte[] { 0x7F, 0x45, 0x4C, 0x46 };

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
                buffer[4] = typeof(TSize) == typeof(uint) ? ELFCLASS32 : ELFCLASS64;
                buffer[5] = ELFDATA2LSB;
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
            public ulong Size { get; set; }
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
                tempBuffer = tempBuffer.Slice(TSize.CreateChecked(Size).WriteLittleEndian(tempBuffer));
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
            public ElfSectionDefinition Section { get; init; }
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

                sectionIndex = Section is { SectionIndex: >= SHN_LORESERVE } ?
                    (ushort)SHN_XINDEX :
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
