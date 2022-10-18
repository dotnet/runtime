// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines the type of a section.
    /// </summary>
    public enum ElfSectionType : uint
    {
        /// <summary>
        /// Section header table entry unused
        /// </summary>
        Null = ElfNative.SHT_NULL,

        /// <summary>
        /// Program data
        /// </summary>
        ProgBits = ElfNative.SHT_PROGBITS,

        /// <summary>
        /// Symbol table
        /// </summary>
        SymbolTable = ElfNative.SHT_SYMTAB,

        /// <summary>
        /// String table
        /// </summary>
        StringTable = ElfNative.SHT_STRTAB,

        /// <summary>
        /// Relocation entries with addends
        /// </summary>
        RelocationAddends = ElfNative.SHT_RELA,

        /// <summary>
        /// Symbol hash table
        /// </summary>
        SymbolHashTable = ElfNative.SHT_HASH,

        /// <summary>
        /// Dynamic linking information 
        /// </summary>
        DynamicLinking = ElfNative.SHT_DYNAMIC,

        /// <summary>
        /// Notes
        /// </summary>
        Note = ElfNative.SHT_NOTE,

        /// <summary>
        /// Program space with no data (bss)
        /// </summary>
        NoBits = ElfNative.SHT_NOBITS,

        /// <summary>
        /// Relocation entries, no addends
        /// </summary>
        Relocation = ElfNative.SHT_REL,

        /// <summary>
        /// Reserved
        /// </summary>
        Shlib = ElfNative.SHT_SHLIB,

        /// <summary>
        /// Dynamic linker symbol table
        /// </summary>
        DynamicLinkerSymbolTable = ElfNative.SHT_DYNSYM,

        /// <summary>
        /// Array of constructors
        /// </summary>
        InitArray = ElfNative.SHT_INIT_ARRAY,

        /// <summary>
        /// Array of destructors
        /// </summary>
        FiniArray = ElfNative.SHT_FINI_ARRAY,

        /// <summary>
        /// Array of pre-constructors
        /// </summary>
        PreInitArray = ElfNative.SHT_PREINIT_ARRAY,

        /// <summary>
        /// Section group
        /// </summary>
        Group = ElfNative.SHT_GROUP,

        /// <summary>
        /// Extended section indices
        /// </summary>
        SymbolTableSectionHeaderIndices = ElfNative.SHT_SYMTAB_SHNDX,

        /// <summary>
        /// Object attributes.
        /// </summary>
        GnuAttributes = ElfNative.SHT_GNU_ATTRIBUTES,

        /// <summary>
        /// GNU-style hash table.
        /// </summary>
        GnuHash = ElfNative.SHT_GNU_HASH,

        /// <summary>
        /// Prelink library list
        /// </summary>
        GnuLibList = ElfNative.SHT_GNU_LIBLIST,

        /// <summary>
        /// Checksum for DSO content.
        /// </summary>
        Checksum = ElfNative.SHT_CHECKSUM,

        /// <summary>
        /// Version definition section.
        /// </summary>
        GnuVersionDefinition = ElfNative.SHT_GNU_verdef,

        /// <summary>
        /// Version needs section.
        /// </summary>
        GnuVersionNeedsSection = ElfNative.SHT_GNU_verneed,

        /// <summary>
        /// Version symbol table.
        /// </summary>
        GnuVersionSymbolTable = ElfNative.SHT_GNU_versym,
    }
}