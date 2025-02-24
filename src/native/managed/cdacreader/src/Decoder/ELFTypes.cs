// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.IO;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.Decoder.PETypes;

internal enum HeaderType : uint
{
    PT_NULL = 0, // Unused segment.
    PT_LOAD = 1, // Loadable segment.
    PT_DYNAMIC = 2, // Dynamic linking information.
    PT_INTERP = 3, // Interpreter pathname.
    PT_NOTE = 4, // Auxiliary information.
    PT_SHLIB = 5, // Reserved.
    PT_PHDR = 6, // The program header table itself.
    PT_TLS = 7, // The thread-local storage template.
    PT_LOOS = 0x60000000, // Lowest operating system-specific pt entry type.
    PT_HIOS = 0x6fffffff, // Highest operating system-specific pt entry type.
    PT_LOPROC = 0x70000000, // Lowest processor-specific program hdr entry type.
    PT_HIPROC = 0x7fffffff, // Highest processor-specific program hdr entry type.
}

internal enum DynamicType : uint
{
  DT_NULL         = 0,        // Marks end of dynamic array.
  DT_NEEDED       = 1,        // String table offset of needed library.
  DT_PLTRELSZ     = 2,        // Size of relocation entries in PLT.
  DT_PLTGOT       = 3,        // Address associated with linkage table.
  DT_HASH         = 4,        // Address of symbolic hash table.
  DT_STRTAB       = 5,        // Address of dynamic string table.
  DT_SYMTAB       = 6,        // Address of dynamic symbol table.
  DT_RELA         = 7,        // Address of relocation table (Rela entries).
  DT_RELASZ       = 8,        // Size of Rela relocation table.
  DT_RELAENT      = 9,        // Size of a Rela relocation entry.
  DT_STRSZ        = 10,       // Total size of the string table.
  DT_SYMENT       = 11,       // Size of a symbol table entry.
  DT_INIT         = 12,       // Address of initialization function.
  DT_FINI         = 13,       // Address of termination function.
  DT_SONAME       = 14,       // String table offset of a shared objects name.
  DT_RPATH        = 15,       // String table offset of library search path.
  DT_SYMBOLIC     = 16,       // Changes symbol resolution algorithm.
  DT_REL          = 17,       // Address of relocation table (Rel entries).
  DT_RELSZ        = 18,       // Size of Rel relocation table.
  DT_RELENT       = 19,       // Size of a Rel relocation entry.
  DT_PLTREL       = 20,       // Type of relocation entry used for linking.
  DT_DEBUG        = 21,       // Reserved for debugger.
  DT_TEXTREL      = 22,       // Relocations exist for non-writable segments.
  DT_JMPREL       = 23,       // Address of relocations associated with PLT.
  DT_BIND_NOW     = 24,       // Process all relocations before execution.
  DT_INIT_ARRAY   = 25,       // Pointer to array of initialization functions.
  DT_FINI_ARRAY   = 26,       // Pointer to array of termination functions.
  DT_INIT_ARRAYSZ = 27,       // Size of DT_INIT_ARRAY.
  DT_FINI_ARRAYSZ = 28,       // Size of DT_FINI_ARRAY.
  DT_RUNPATH      = 29,       // String table offset of lib search path.
  DT_FLAGS        = 30,       // Flags.
  DT_ENCODING     = 32,       // Values from here to DT_LOOS follow the rules
                              // for the interpretation of the d_un union.

  DT_PREINIT_ARRAY = 32,      // Pointer to array of preinit functions.
  DT_PREINIT_ARRAYSZ = 33,    // Size of the DT_PREINIT_ARRAY array.

  DT_LOOS         = 0x60000000, // Start of environment specific tags.
  DT_HIOS         = 0x6FFFFFFF, // End of environment specific tags.
  DT_LOPROC       = 0x70000000, // Start of processor specific tags.
  DT_HIPROC       = 0x7FFFFFFF, // End of processor specific tags.

  DT_GNU_HASH     = 0x6FFFFEF5, // Reference to the GNU hash table.
  DT_RELACOUNT    = 0x6FFFFFF9, // ELF32_Rela count.
  DT_RELCOUNT     = 0x6FFFFFFA, // ELF32_Rel count.

  DT_FLAGS_1      = 0X6FFFFFFB, // Flags_1.
  DT_VERSYM       = 0x6FFFFFF0, // The address of .gnu.version section.
  DT_VERDEF       = 0X6FFFFFFC, // The address of the version definition table.
  DT_VERDEFNUM    = 0X6FFFFFFD, // The number of entries in DT_VERDEF.
  DT_VERNEED      = 0X6FFFFFFE, // The address of the version Dependency table.
  DT_VERNEEDNUM   = 0X6FFFFFFF, // The number of entries in DT_VERNEED.

  // Mips specific dynamic table entry tags.
  DT_MIPS_RLD_VERSION   = 0x70000001, // 32 bit version number for runtime
                                      // linker interface.
  DT_MIPS_TIME_STAMP    = 0x70000002, // Time stamp.
  DT_MIPS_ICHECKSUM     = 0x70000003, // Checksum of external strings
                                      // and common sizes.
  DT_MIPS_IVERSION      = 0x70000004, // Index of version string
                                      // in string table.
  DT_MIPS_FLAGS         = 0x70000005, // 32 bits of flags.
  DT_MIPS_BASE_ADDRESS  = 0x70000006, // Base address of the segment.
  DT_MIPS_MSYM          = 0x70000007, // Address of .msym section.
  DT_MIPS_CONFLICT      = 0x70000008, // Address of .conflict section.
  DT_MIPS_LIBLIST       = 0x70000009, // Address of .liblist section.
  DT_MIPS_LOCAL_GOTNO   = 0x7000000a, // Number of local global offset
                                      // table entries.
  DT_MIPS_CONFLICTNO    = 0x7000000b, // Number of entries
                                      // in the .conflict section.
  DT_MIPS_LIBLISTNO     = 0x70000010, // Number of entries
                                      // in the .liblist section.
  DT_MIPS_SYMTABNO      = 0x70000011, // Number of entries
                                      // in the .dynsym section.
  DT_MIPS_UNREFEXTNO    = 0x70000012, // Index of first external dynamic symbol
                                      // not referenced locally.
  DT_MIPS_GOTSYM        = 0x70000013, // Index of first dynamic symbol
                                      // in global offset table.
  DT_MIPS_HIPAGENO      = 0x70000014, // Number of page table entries
                                      // in global offset table.
  DT_MIPS_RLD_MAP       = 0x70000016, // Address of run time loader map,
                                      // used for debugging.
  DT_MIPS_DELTA_CLASS       = 0x70000017, // Delta C++ class definition.
  DT_MIPS_DELTA_CLASS_NO    = 0x70000018, // Number of entries
                                          // in DT_MIPS_DELTA_CLASS.
  DT_MIPS_DELTA_INSTANCE    = 0x70000019, // Delta C++ class instances.
  DT_MIPS_DELTA_INSTANCE_NO = 0x7000001A, // Number of entries
                                          // in DT_MIPS_DELTA_INSTANCE.
  DT_MIPS_DELTA_RELOC       = 0x7000001B, // Delta relocations.
  DT_MIPS_DELTA_RELOC_NO    = 0x7000001C, // Number of entries
                                          // in DT_MIPS_DELTA_RELOC.
  DT_MIPS_DELTA_SYM         = 0x7000001D, // Delta symbols that Delta
                                          // relocations refer to.
  DT_MIPS_DELTA_SYM_NO      = 0x7000001E, // Number of entries
                                          // in DT_MIPS_DELTA_SYM.
  DT_MIPS_DELTA_CLASSSYM    = 0x70000020, // Delta symbols that hold
                                          // class declarations.
  DT_MIPS_DELTA_CLASSSYM_NO = 0x70000021, // Number of entries
                                          // in DT_MIPS_DELTA_CLASSSYM.
  DT_MIPS_CXX_FLAGS         = 0x70000022, // Flags indicating information
                                          // about C++ flavor.
  DT_MIPS_PIXIE_INIT        = 0x70000023, // Pixie information.
  DT_MIPS_SYMBOL_LIB        = 0x70000024, // Address of .MIPS.symlib
  DT_MIPS_LOCALPAGE_GOTIDX  = 0x70000025, // The GOT index of the first PTE
                                          // for a segment
  DT_MIPS_LOCAL_GOTIDX      = 0x70000026, // The GOT index of the first PTE
                                          // for a local symbol
  DT_MIPS_HIDDEN_GOTIDX     = 0x70000027, // The GOT index of the first PTE
                                          // for a hidden symbol
  DT_MIPS_PROTECTED_GOTIDX  = 0x70000028, // The GOT index of the first PTE
                                          // for a protected symbol
  DT_MIPS_OPTIONS           = 0x70000029, // Address of `.MIPS.options'.
  DT_MIPS_INTERFACE         = 0x7000002A, // Address of `.interface'.
  DT_MIPS_DYNSTR_ALIGN      = 0x7000002B, // Unknown.
  DT_MIPS_INTERFACE_SIZE    = 0x7000002C, // Size of the .interface section.
  DT_MIPS_RLD_TEXT_RESOLVE_ADDR = 0x7000002D, // Size of rld_text_resolve
                                              // function stored in the GOT.
  DT_MIPS_PERF_SUFFIX       = 0x7000002E, // Default suffix of DSO to be added
                                          // by rld on dlopen() calls.
  DT_MIPS_COMPACT_SIZE      = 0x7000002F, // Size of compact relocation
                                          // section (O32).
  DT_MIPS_GP_VALUE          = 0x70000030, // GP value for auxiliary GOTs.
  DT_MIPS_AUX_DYNAMIC       = 0x70000031, // Address of auxiliary .dynamic.
  DT_MIPS_PLTGOT            = 0x70000032, // Address of the base of the PLTGOT.
  DT_MIPS_RWPLT             = 0x70000034, // Points to the base
                                          // of a writable PLT.
  DT_MIPS_RLD_MAP_REL       = 0x70000035  // Relative offset of run time loader
                                          // map, used for debugging.
};

internal struct Elf_Ehdr<T>
    where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
{
    public byte[] e_ident;
    public ushort e_type;
    public ushort e_machine;
    public uint e_version;
    public T e_entry;
    public T e_phoff;
    public T e_shoff;
    public uint e_flags;
    public ushort e_ehsize;
    public ushort e_phentsize;
    public ushort e_phnum;
    public ushort e_shentsize;
    public ushort e_shnum;
    public ushort e_shstrndx;

    public Elf_Ehdr(BinaryReader reader)
    {
        e_ident = reader.ReadBytes(16);
        e_type = reader.ReadUInt16();
        e_machine = reader.ReadUInt16();
        e_version = reader.ReadUInt32();
        e_entry = reader.Read<T>();
        e_phoff = reader.Read<T>();
        e_shoff = reader.Read<T>();
        e_flags = reader.ReadUInt32();
        e_ehsize = reader.ReadUInt16();
        e_phentsize = reader.ReadUInt16();
        e_phnum = reader.ReadUInt16();
        e_shentsize = reader.ReadUInt16();
        e_shnum = reader.ReadUInt16();
        e_shstrndx = reader.ReadUInt16();
    }
}

internal struct Elf_Phdr<T>
    where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
{
    public uint p_type;
    public uint p_flags;
    public T p_offset;
    public T p_vaddr;
    public T p_paddr;
    public T p_filesz;
    public T p_memsz;
    public T p_align;

    public readonly HeaderType Type { get => (HeaderType)p_type; }

    public Elf_Phdr(BinaryReader reader)
    {
        T t = default;
        p_type = reader.Read<uint>();
        if (t.GetByteCount() == 8)
        {
            // on 64 bit platforms, p_flags is the second element
            p_flags = reader.ReadUInt32();
        }
        p_offset = reader.Read<T>();
        p_vaddr = reader.Read<T>();
        p_paddr = reader.Read<T>();
        p_filesz = reader.Read<T>();
        p_memsz = reader.Read<T>();
        if (t.GetByteCount() == 4)
        {
            // on 32 bit platforms, p_flags is located after p_memsz
            p_flags = reader.ReadUInt32();
        }
        p_align = reader.Read<T>();
    }
}

internal struct Elf_Dyn<T>
    where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
{
    public T d_tag;
    public T d_val;

    public readonly DynamicType Type { get => (DynamicType)Convert.ToUInt32(d_tag); }

    public Elf_Dyn(BinaryReader reader)
    {
        d_tag = reader.Read<T>();
        d_val = reader.Read<T>();
    }
}

internal struct Elf_Sym<T>
    where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
{
    public uint st_name;
    public T st_value;
    public T st_size;
    public char st_info;
    public char st_other;
    public ushort st_shndx;

    public Elf_Sym(BinaryReader reader)
    {
        T t = default;
        // field ordering is different on 32/64 bit platforms.
        if (t.GetByteCount() == 4)
        {
            st_name = reader.ReadUInt32();
            st_value = reader.Read<T>();
            st_size = reader.Read<T>();
            st_info = reader.ReadChar();
            st_other = reader.ReadChar();
            st_shndx = reader.ReadUInt16();
        }
        else if (t.GetByteCount() == 8)
        {
            st_name = reader.ReadUInt32();
            st_info = reader.ReadChar();
            st_other = reader.ReadChar();
            st_shndx = reader.ReadUInt16();
            st_value = reader.Read<T>();
            st_size = reader.Read<T>();
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    public static int GetPackedSize()
    {
        T t = default;
        if (t.GetByteCount() == 4) return 16;
        if (t.GetByteCount() == 8) return 24;
        throw new InvalidOperationException();
    }
}

internal struct GnuHashTable(BinaryReader reader)
{
    public int bucketCount = reader.ReadInt32();
    public int symbolOffset = reader.ReadInt32();
    public int bloomSize = reader.ReadInt32();
    public int bloomShift = reader.ReadInt32();
}
