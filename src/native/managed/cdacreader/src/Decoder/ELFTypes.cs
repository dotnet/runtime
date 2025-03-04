// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;

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
    DT_NULL = 0, // Marks end of dynamic array.
    DT_NEEDED = 1, // String table offset of needed library.
    DT_PLTRELSZ = 2, // Size of relocation entries in PLT.
    DT_PLTGOT = 3, // Address associated with linkage table.
    DT_HASH = 4, // Address of symbolic hash table.
    DT_STRTAB = 5, // Address of dynamic string table.
    DT_SYMTAB = 6, // Address of dynamic symbol table.
    DT_RELA = 7, // Address of relocation table (Rela entries).
    DT_RELASZ = 8, // Size of Rela relocation table.
    DT_RELAENT = 9, // Size of a Rela relocation entry.
    DT_STRSZ = 10, // Total size of the string table.
    DT_SYMENT = 11, // Size of a symbol table entry.
    DT_INIT = 12, // Address of initialization function.
    DT_FINI = 13, // Address of termination function.
    DT_SONAME = 14, // String table offset of a shared objects name.
    DT_RPATH = 15, // String table offset of library search path.
    DT_SYMBOLIC = 16, // Changes symbol resolution algorithm.
    DT_REL = 17, // Address of relocation table (Rel entries).
    DT_RELSZ = 18, // Size of Rel relocation table.
    DT_RELENT = 19, // Size of a Rel relocation entry.
    DT_PLTREL = 20, // Type of relocation entry used for linking.
    DT_DEBUG = 21, // Reserved for debugger.
    DT_TEXTREL = 22, // Relocations exist for non-writable segments.
    DT_JMPREL = 23, // Address of relocations associated with PLT.
    DT_BIND_NOW = 24, // Process all relocations before execution.
    DT_INIT_ARRAY = 25, // Pointer to array of initialization functions.
    DT_FINI_ARRAY = 26, // Pointer to array of termination functions.
    DT_INIT_ARRAYSZ = 27, // Size of DT_INIT_ARRAY.
    DT_FINI_ARRAYSZ = 28, // Size of DT_FINI_ARRAY.
    DT_RUNPATH = 29, // String table offset of lib search path.
    DT_FLAGS = 30, // Flags.
    DT_ENCODING = 32, // Values from here to DT_LOOS follow the rules for the interpretation of the d_un union.
    DT_PREINIT_ARRAY = 32, // Pointer to array of preinit functions.
    DT_PREINIT_ARRAYSZ = 33, // Size of the DT_PREINIT_ARRAY array.

    DT_LOOS = 0x60000000, // Start of environment specific tags.
    DT_HIOS = 0x6FFFFFFF, // End of environment specific tags.
    DT_LOPROC = 0x70000000, // Start of processor specific tags.
    DT_HIPROC = 0x7FFFFFFF, // End of processor specific tags.

    DT_GNU_HASH = 0x6FFFFEF5, // Reference to the GNU hash table.
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
