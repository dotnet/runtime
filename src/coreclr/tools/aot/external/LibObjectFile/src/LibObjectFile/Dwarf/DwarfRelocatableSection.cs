// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using LibObjectFile.Elf;

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfRelocatableSection : DwarfSection
    {
        protected DwarfRelocatableSection()
        {
            Relocations = new List<DwarfRelocation>();
        }


        public List<DwarfRelocation> Relocations { get; }
    }

    public static class DwarfRelocationSectionExtensions
    {
        public static void CopyRelocationsTo(this DwarfRelocatableSection dwarfRelocSection, DwarfElfContext elfContext, ElfRelocationTable relocTable)
        {
            if (elfContext == null) throw new ArgumentNullException(nameof(elfContext));
            if (relocTable == null) throw new ArgumentNullException(nameof(relocTable));

            switch (elfContext.Elf.Arch.Value)
            {
                case ElfArch.X86_64:
                    CopyRelocationsX86_64(dwarfRelocSection, elfContext, relocTable);
                    break;
                default:
                    throw new NotImplementedException($"The relocation for architecture {relocTable.Parent.Arch} is not supported/implemented.");
            }

        }

        private static void CopyRelocationsX86_64(DwarfRelocatableSection dwarfRelocSection, DwarfElfContext elfContext, ElfRelocationTable relocTable)
        {
            relocTable.Entries.Clear();
            foreach (var reloc in dwarfRelocSection.Relocations)
            {
                var relocType = reloc.Size == DwarfAddressSize.Bit64 ? ElfRelocationType.R_X86_64_64 : ElfRelocationType.R_X86_64_32;
                switch (reloc.Target)
                {
                    case DwarfRelocationTarget.Code:
                        relocTable.Entries.Add(new ElfRelocation(reloc.Offset, relocType, (uint)elfContext.CodeSectionSymbolIndex, (long) reloc.Addend));
                        break;
                    case DwarfRelocationTarget.DebugString:
                        relocTable.Entries.Add(new ElfRelocation(reloc.Offset, relocType, (uint)elfContext.StringTableSymbolIndex, (long)reloc.Addend));
                        break;
                    case DwarfRelocationTarget.DebugAbbrev:
                        relocTable.Entries.Add(new ElfRelocation(reloc.Offset, relocType, (uint)elfContext.AbbreviationTableSymbolIndex, (long)reloc.Addend));
                        break;
                    case DwarfRelocationTarget.DebugInfo:
                        relocTable.Entries.Add(new ElfRelocation(reloc.Offset, relocType, (uint)elfContext.InfoSectionSymbolIndex, (long)reloc.Addend));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}