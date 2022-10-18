// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.Utils;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Extensions for <see cref="ElfSection"/>
    /// </summary>
    public static class ElfSectionExtension
    {
        /// <summary>
        /// Configure a section default name, type and flags with <see cref="ElfSectionSpecialType"/>
        /// </summary>
        /// <typeparam name="TElfSection">The type of the section to configure</typeparam>
        /// <param name="section">The section to configure</param>
        /// <param name="sectionSpecialType">The special type</param>
        /// <returns>The section configured</returns>
        public static TElfSection ConfigureAs<TElfSection>(this TElfSection section, ElfSectionSpecialType sectionSpecialType) where TElfSection : ElfSection
        {
            switch (sectionSpecialType)
            {
                case ElfSectionSpecialType.None:
                    break;
                case ElfSectionSpecialType.Bss:
                    section.Name = ".bss";
                    section.Type = ElfSectionType.NoBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Write;
                    break;
                case ElfSectionSpecialType.Comment:
                    section.Name = ".comment";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Data:
                    section.Name = ".data";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Write;
                    break;
                case ElfSectionSpecialType.Data1:
                    section.Name = ".data1";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Write;
                    break;
                case ElfSectionSpecialType.Debug:
                    section.Name = ".debug";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Dynamic:
                    section.Name = ".dynamic";
                    section.Type = ElfSectionType.DynamicLinking;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.DynamicStringTable:
                    section.Name = ".dynstr";
                    section.Type = ElfSectionType.StringTable;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.DynamicSymbolTable:
                    section.Name = ".dynsym";
                    section.Type = ElfSectionType.DynamicLinkerSymbolTable;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.Fini:
                    section.Name = ".fini";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Executable;
                    break;
                case ElfSectionSpecialType.Got:
                    section.Name = ".got";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Hash:
                    section.Name = ".hash";
                    section.Type = ElfSectionType.SymbolHashTable;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Init:
                    section.Name = ".init";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Executable;
                    break;
                case ElfSectionSpecialType.Interp:
                    section.Name = ".interp";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.Line:
                    section.Name = ".line";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Note:
                    section.Name = ".note";
                    section.Type = ElfSectionType.Note;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Plt:
                    section.Name = ".plt";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Relocation:
                    section.Name = ElfRelocationTable.DefaultName;
                    section.Type = ElfSectionType.Relocation;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.RelocationAddends:
                    section.Name = ElfRelocationTable.DefaultNameWithAddends;
                    section.Type = ElfSectionType.RelocationAddends;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.ReadOnlyData:
                    section.Name = ".rodata";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.ReadOnlyData1:
                    section.Name = ".rodata1";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.SectionHeaderStringTable:
                    section.Name = ".shstrtab";
                    section.Type = ElfSectionType.StringTable;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.StringTable:
                    section.Name = ElfStringTable.DefaultName;
                    section.Type = ElfSectionType.StringTable;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.SymbolTable:
                    section.Name = ElfSymbolTable.DefaultName;
                    section.Type = ElfSectionType.SymbolTable;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Text:
                    section.Name = ".text";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Executable;
                    break;
                default:
                    throw ThrowHelper.InvalidEnum(sectionSpecialType);
            }
            return section;
        }
    }
}