// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
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
        private int _bssSectionIndex;
        private Stream _bssStream;
        private Dictionary<int, ElfSection> _sectionIndexToElfSection = new();
        private Dictionary<ElfSection, ElfRelocationTable> _sectionToRelocationTable = new();

        // Symbol table
        private Dictionary<string, uint> _symbolNameToIndex = new();
        private ElfSymbolTable _symbolTable;

        private ElfObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
            if (factory.Target.Architecture != TargetArchitecture.X64)
            {
                throw new NotSupportedException("Unsupported architecture");
            }

            _objectFile = new ElfObjectFile(ElfArch.X86_64);

            var stringSection = new ElfStringTable();
            _objectFile.AddSection(stringSection);
            _symbolTable = new ElfSymbolTable { Link = stringSection };
            _objectFile.AddSection(_symbolTable);
        }

        protected override void CreateSection(ObjectNodeSection section, out Stream sectionStream)
        {
            if (section.Name == "bss")
            {
                Debug.Assert(_bssStream == null);

                _bssSectionIndex = _sectionIndex;
                _bssStream = sectionStream = new MemoryStream();
                ElfSection elfSection = new ElfBinarySection()
                {
                    Name = ".bss",
                    Type = ElfSectionType.NoBits,
                    Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Write,
                };

                _sectionIndexToElfSection[_sectionIndex++] = elfSection;
                _objectFile.AddSection(elfSection);
            }
            else
            {
                string sectionName =
                    section.Name == "rdata" ? ".rodata" :
                    (section.Name.StartsWith("_") || section.Name.StartsWith(".") ? section.Name : "." + section.Name);

                sectionStream = new MemoryStream();
                ElfSection elfSection = new ElfBinarySection(sectionStream)
                {
                    Name = sectionName,
                    Type = section.Name == ".eh_frame" ? (ElfSectionType)ElfNative.SHT_IA_64_UNWIND : ElfSectionType.ProgBits,
                    Flags =
                        section.Type == SectionType.Executable ? ElfSectionFlags.Alloc | ElfSectionFlags.Executable :
                        (section.Type == SectionType.Writeable ? ElfSectionFlags.Alloc | ElfSectionFlags.Write :
                        ElfSectionFlags.Alloc)
                };

                var elfRelocationTable = new ElfRelocationTable
                {
                    Name = ".rela" + sectionName,
                    Link = _symbolTable,
                    Info = elfSection,
                    Alignment = 8,
                };

                _sectionIndexToElfSection[_sectionIndex++] = elfSection;
                _sectionToRelocationTable[elfSection] = elfRelocationTable;
                _objectFile.AddSection(elfSection);
                _objectFile.AddSection(elfRelocationTable);
            }
        }

        protected override void EmitRelocation(
            int sectionIndex,
            List<SymbolicRelocation> relocationList,
            int offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            int addend)
        {
            if (relocType == RelocType.IMAGE_REL_BASED_REL32 ||
                relocType == RelocType.IMAGE_REL_BASED_RELPTR32)
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
            relocationList.Add(new SymbolicRelocation(offset, relocType, symbolName, addend));
        }

        protected override void EmitSymbolTable()
        {
            uint symbolIndex = (uint)_symbolTable.Entries.Count;

            var definedSymbols = GetDefinedSymbols();
            var sortedSymbols = new List<ElfSymbol>(definedSymbols.Count);
            foreach (var (name, definition) in definedSymbols)
            {
                sortedSymbols.Add(new ElfSymbol
                {
                    Name = name,
                    Bind = ElfSymbolBind.Global,
                    Section = _sectionIndexToElfSection[definition.SectionIndex],
                    Value = (ulong)definition.Value,
                    Type = definition.Size > 0 ? ElfSymbolType.Function : 0,
                    Size = (ulong)definition.Size,
                    Visibility = ElfSymbolVisibility.Hidden,
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
        }

        protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
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

        protected override void UpdateSectionAlignment(int sectionIndex, int alignment, out bool isExecutable)
        {
            var elfSection = _sectionIndexToElfSection[sectionIndex];
            elfSection.Alignment = Math.Max(elfSection.Alignment, (uint)alignment);
            isExecutable = elfSection.Flags.HasFlag(ElfSectionFlags.Executable);
        }

        protected override void EmitDebugSections(DwarfFile dwarfFile)
        {
            // TODO: Pretty much broken in all possible ways since we have multiple
            // code sections and no LowPC relocations

            var elfDiagnostics = new DiagnosticBag();
            _objectFile.UpdateLayout(elfDiagnostics);

            foreach (var unit in dwarfFile.InfoSection.Units)
            {
                dwarfFile.AddressRangeTable.AddressSize = unit.AddressSize;
                dwarfFile.AddressRangeTable.Unit = unit;
                dwarfFile.AddressRangeTable.Ranges.Add(new DwarfAddressRange(0, 0, _objectFile.Layout.TotalSize));
            }

            var dwarfElfContext = new DwarfElfContext(_objectFile);
            dwarfFile.WriteToElf(dwarfElfContext);
        }

        protected override void EmitSectionsAndLayout()
        {
            _objectFile.AddSection(new ElfSectionHeaderStringTable());
            if (_bssStream != null)
            {
                _sectionIndexToElfSection[_bssSectionIndex].Size = (ulong)_bssStream.Length;
            }
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
    }
}
