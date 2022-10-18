// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibObjectFile.Elf;

namespace LibObjectFile.Dwarf
{
    public class DwarfElfContext
    {
        private readonly int _codeSectionSymbolIndex;
        private int _infoSectionSymbolIndex;
        private int _abbreviationTableSymbolIndex;
        private int _lineTableSymbolIndex;
        private int _stringTableSymbolIndex;
        private int _locationSectionSymbolIndex;
        private readonly ElfSymbolTable _symbolTable;

        public DwarfElfContext(ElfObjectFile elf)
        {
            Elf = elf ?? throw new ArgumentNullException(nameof(elf));

            var relocContext = new ElfRelocationContext();

            var codeSection = elf.Sections.OfType<ElfBinarySection>().FirstOrDefault(s => s.Name == ".text");
            
            _symbolTable = elf.Sections.OfType<ElfSymbolTable>().FirstOrDefault();
            var mapSectionToSymbolIndex = new Dictionary<ElfSection, int>();
            if (_symbolTable != null)
            {
                for (var i = 0; i < _symbolTable.Entries.Count; i++)
                {
                    var entry = _symbolTable.Entries[i];

                    if (entry.Type == ElfSymbolType.Section && entry.Section.Section != null)
                    {
                        mapSectionToSymbolIndex[entry.Section.Section] = i;
                    }
                }

                if (codeSection != null)
                {
                    if (!mapSectionToSymbolIndex.TryGetValue(codeSection, out _codeSectionSymbolIndex))
                    {
                        _codeSectionSymbolIndex = _symbolTable.Entries.Count;
                        _symbolTable.Entries.Add(new ElfSymbol()
                        {
                            Type = ElfSymbolType.Section,
                            Section = codeSection,
                        });
                    }
                }
            }

            foreach (var section in elf.Sections)
            {
                switch (section.Name.Value)
                {
                    case ".debug_info":
                        InfoSection = ((ElfBinarySection)section);
                        mapSectionToSymbolIndex.TryGetValue(InfoSection, out _infoSectionSymbolIndex);
                        break;
                    case ".debug_abbrev":
                        AbbreviationTable = ((ElfBinarySection)section);
                        mapSectionToSymbolIndex.TryGetValue(AbbreviationTable, out _abbreviationTableSymbolIndex);
                        break;
                    case ".debug_aranges":
                        AddressRangeTable = ((ElfBinarySection)section);
                        break;
                    case ".debug_str":
                        StringTable = ((ElfBinarySection)section);
                        mapSectionToSymbolIndex.TryGetValue(StringTable, out _stringTableSymbolIndex);
                        break;
                    case ".debug_line":
                        LineTable = ((ElfBinarySection)section);
                        mapSectionToSymbolIndex.TryGetValue(LineTable, out _lineTableSymbolIndex);
                        break;
                    case ".debug_loc":
                        LocationSection = ((ElfBinarySection)section);
                        mapSectionToSymbolIndex.TryGetValue(LocationSection, out _locationSectionSymbolIndex);
                        break;

                    case ".rela.debug_aranges":
                    case ".rel.debug_aranges":
                        RelocAddressRangeTable = (ElfRelocationTable)section;
                        RelocAddressRangeTable.Relocate(relocContext);
                        break;

                    case ".rela.debug_line":
                    case ".rel.debug_line":
                        RelocLineTable = (ElfRelocationTable)section;
                        RelocLineTable.Relocate(relocContext);
                        break;

                    case ".rela.debug_info":
                    case ".rel.debug_info":
                        RelocInfoSection = (ElfRelocationTable)section;
                        RelocInfoSection.Relocate(relocContext);
                        break;

                    case ".rela.debug_loc":
                    case ".rel.debug_loc":
                        RelocLocationSection = (ElfRelocationTable)section;
                        RelocLocationSection.Relocate(relocContext);
                        break;
                }
            }
        }
        
        public ElfObjectFile Elf { get; }

        public bool IsLittleEndian => Elf.Encoding == ElfEncoding.Lsb;

        public DwarfAddressSize AddressSize => Elf.FileClass == ElfFileClass.Is64 ? DwarfAddressSize.Bit64 : DwarfAddressSize.Bit32;

        public ElfBinarySection InfoSection { get; private set; }

        public ElfRelocationTable RelocInfoSection { get; set; }

        public ElfBinarySection AbbreviationTable { get; set; }

        public ElfBinarySection AddressRangeTable { get; private set; }

        public ElfRelocationTable RelocAddressRangeTable { get; set; }

        public ElfBinarySection StringTable { get; set; }

        public ElfBinarySection LineTable { get; set; }

        public ElfRelocationTable RelocLineTable { get; set; }

        public ElfBinarySection LocationSection { get; private set; }

        public ElfRelocationTable RelocLocationSection { get; set; }

        public int CodeSectionSymbolIndex => _codeSectionSymbolIndex;

        public int InfoSectionSymbolIndex => _infoSectionSymbolIndex;

        public int StringTableSymbolIndex => _stringTableSymbolIndex;

        public int AbbreviationTableSymbolIndex => _abbreviationTableSymbolIndex;

        public int LineTableSymbolIndex => _lineTableSymbolIndex;

        public int LocationSectionSymbolIndex => _locationSectionSymbolIndex;

        public ElfBinarySection GetOrCreateInfoSection()
        {
            return InfoSection ??= GetOrCreateDebugSection(".debug_info", true, out _infoSectionSymbolIndex);
        }

        public ElfRelocationTable GetOrCreateRelocInfoSection()
        {
            return RelocInfoSection ??= GetOrCreateRelocationTable(InfoSection);
        }

        public ElfBinarySection GetOrCreateAbbreviationTable()
        {
            return AbbreviationTable ??= GetOrCreateDebugSection(".debug_abbrev", true, out _abbreviationTableSymbolIndex);
        }
        
        public ElfBinarySection GetOrCreateAddressRangeTable()
        {
            return AddressRangeTable ??= GetOrCreateDebugSection(".debug_aranges", false, out _);
        }

        public ElfRelocationTable GetOrCreateRelocAddressRangeTable()
        {
            return RelocAddressRangeTable ??= GetOrCreateRelocationTable(AddressRangeTable);
        }

        public ElfBinarySection GetOrCreateLineSection()
        {
            return LineTable ??= GetOrCreateDebugSection(".debug_line", true, out _lineTableSymbolIndex);
        }

        public ElfRelocationTable GetOrCreateRelocLineSection()
        {
            return RelocLineTable ??= GetOrCreateRelocationTable(LineTable);
        }

        public ElfBinarySection GetOrCreateStringTable()
        {
            return StringTable ??= GetOrCreateDebugSection(".debug_str", true, out _stringTableSymbolIndex);
        }

        public ElfBinarySection GetOrCreateLocationSection()
        {
            return LocationSection ??= GetOrCreateDebugSection(".debug_loc", true, out _locationSectionSymbolIndex);
        }

        public ElfRelocationTable GetOrCreateRelocLocationSection()
        {
            return RelocLocationSection ??= GetOrCreateRelocationTable(LocationSection);
        }

        public void RemoveStringTable()
        {
            if (StringTable != null)
            {
                Elf.RemoveSection(StringTable);
                StringTable = null;
            }
        }

        public void RemoveAbbreviationTable()
        {
            if (AbbreviationTable != null)
            {
                Elf.RemoveSection(AbbreviationTable);
                AbbreviationTable = null;
            }
        }

        public void RemoveLineTable()
        {
            if (LineTable != null)
            {
                Elf.RemoveSection(LineTable);
                LineTable = null;
            }

            RemoveRelocLineTable();
        }

        public void RemoveRelocLineTable()
        {
            if (RelocLineTable != null)
            {
                Elf.RemoveSection(RelocLineTable);
                RelocLineTable = null;
            }
        }

        public void RemoveAddressRangeTable()
        {
            if (AddressRangeTable != null)
            {
                Elf.RemoveSection(AddressRangeTable);
                AddressRangeTable = null;
            }

            RemoveRelocAddressRangeTable();
        }

        public void RemoveRelocAddressRangeTable()
        {
            if (RelocAddressRangeTable != null)
            {
                Elf.RemoveSection(RelocAddressRangeTable);
                RelocAddressRangeTable = null;
            }
        }

        public void RemoveInfoSection()
        {
            if (InfoSection != null)
            {
                Elf.RemoveSection(InfoSection);
                InfoSection = null;
            }

            RemoveRelocInfoSection();
        }

        public void RemoveRelocInfoSection()
        {
            if (RelocInfoSection != null)
            {
                Elf.RemoveSection(RelocInfoSection);
                RelocInfoSection = null;
            }
        }

        public void RemoveLocationSection()
        {
            if (LocationSection != null)
            {
                Elf.RemoveSection(LocationSection);
                LocationSection = null;
            }

            RemoveRelocLocationSection();
        }

        public void RemoveRelocLocationSection()
        {
            if (RelocLocationSection != null)
            {
                Elf.RemoveSection(RelocLocationSection);
                RelocLocationSection = null;
            }
        }

        private ElfBinarySection GetOrCreateDebugSection(string name, bool createSymbol, out int symbolIndex)
        {
            var newSection = new ElfBinarySection()
            {
                Name = name, 
                Alignment = 1, 
                Type = ElfSectionType.ProgBits,
                Stream = new MemoryStream(),
            };

            Elf.AddSection(newSection);
            symbolIndex = 0;

            if (createSymbol && _symbolTable != null)
            {
                symbolIndex = _symbolTable.Entries.Count;
                _symbolTable.Entries.Add(new ElfSymbol()
                {
                    Type = ElfSymbolType.Section,
                    Section = newSection,
                });
            }

            return newSection;
        }

        private ElfRelocationTable GetOrCreateRelocationTable(ElfBinarySection section)
        {
            var newSection = new ElfRelocationTable()
            {
                Name = $".rela{section.Name}", 
                Alignment = (ulong)AddressSize, 
                Flags = ElfSectionFlags.InfoLink, 
                Type = ElfSectionType.RelocationAddends,
                Info = section,
                Link = _symbolTable,
            };
            Elf.AddSection(newSection);
            return newSection;
        }
    }
}