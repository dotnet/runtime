// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;

internal struct Mach64Header
{
    public const uint LE_MAGIC = 0xfeedfacf; // 64-bit Mach-O file
    public const uint BE_MAGIC = 0xcffaedfe; // 64-bit reversed Mach-O file

    public uint magic;          // mach magic number identifier
    public uint cpuType;        // cpu specifier
    public uint cpuSubType;     // machine specifier
    public uint fileType;       // type of file
    public uint nCmds;          // number of load commands
    public uint sizeOfCmds;     // the size of all the load commands
    public uint flags;          // flags
    public uint reserved;       // reserved

    public Mach64Header(BinaryReader reader)
    {
        magic = reader.ReadUInt32();
        cpuType = reader.ReadUInt32();
        cpuSubType = reader.ReadUInt32();
        fileType = reader.ReadUInt32();
        nCmds = reader.ReadUInt32();
        sizeOfCmds = reader.ReadUInt32();
        flags = reader.ReadUInt32();
        reserved = reader.ReadUInt32();
    }
}

internal struct Mach64LoadCommand
{
    public enum Type
    {
        LC_SYMTAB = 0x2,
        LC_DYNSYM = 0xb,
        LC_SEGMENT_64 = 0x19,
    }

    public uint cmd;            // type of load command
    public uint cmdSize;        // total size of command in bytes

    public Mach64LoadCommand(BinaryReader reader)
    {
        cmd = reader.ReadUInt32();
        cmdSize = reader.ReadUInt32();
    }
}

internal struct Mach64SymTabCommand
{
    public uint cmd;            // type of load command
    public uint cmdSize;        // total size of command in bytes
    public uint symoff;         // symbol table offset
    public uint nsyms;          // number of symbol table entries
    public uint stroff;         // string table offset
    public uint strsize;        // string table size

    public Mach64SymTabCommand(BinaryReader reader)
    {
        cmd = reader.ReadUInt32();
        cmdSize = reader.ReadUInt32();
        symoff = reader.ReadUInt32();
        nsyms = reader.ReadUInt32();
        stroff = reader.ReadUInt32();
        strsize = reader.ReadUInt32();
    }
}

internal struct Mach64DySymTabCommand
{
    public uint cmd;            // LC_DYSYMTAB
    public uint cmdsize;        // sizeof(struct dysymtab_command)
    public uint ilocalsym;      // index to local symbols
    public uint nlocalsym;      // number of local symbols

    public uint iextdefsym;     // index to externally defined symbols
    public uint nextdefsym;     // number of externally defined symbols

    public uint iundefsym;      // index to undefined symbols
    public uint nundefsym;      // number of undefined symbols
    public uint tocoff;         // file offset to table of contents
    public uint ntoc;           // number of entries in table of contents
    public uint modtaboff;      // file offset to module table
    public uint nmodtab;        // number of module table entries
    public uint extrefsymoff;   // offset to referenced symbol table
    public uint nextrefsyms;    // number of referenced symbol table entries
    public uint indirectsymoff; // file offset to the indirect symbol table
    public uint nindirectsyms;  // number of indirect symbol table entries
    public uint extreloff;      // offset to external relocation entries
    public uint nextrel;        // number of external relocation entries
    public uint locreloff;      // offset to local relocation entries
    public uint nlocrel;        // number of local relocation entries

    public Mach64DySymTabCommand(BinaryReader reader)
    {
        cmd = reader.ReadUInt32();
        cmdsize = reader.ReadUInt32();
        ilocalsym = reader.ReadUInt32();
        nlocalsym = reader.ReadUInt32();
        iextdefsym = reader.ReadUInt32();
        nextdefsym = reader.ReadUInt32();
        iundefsym = reader.ReadUInt32();
        nundefsym = reader.ReadUInt32();
        tocoff = reader.ReadUInt32();
        ntoc = reader.ReadUInt32();
        modtaboff = reader.ReadUInt32();
        nmodtab = reader.ReadUInt32();
        extrefsymoff = reader.ReadUInt32();
        nextrefsyms = reader.ReadUInt32();
        indirectsymoff = reader.ReadUInt32();
        nindirectsyms = reader.ReadUInt32();
        extreloff = reader.ReadUInt32();
        nextrel = reader.ReadUInt32();
        locreloff = reader.ReadUInt32();
        nlocrel = reader.ReadUInt32();
    }
}

internal struct Mach64SegmentCommand
{
    public const string SEG_TEXT = "__TEXT";
    public uint cmd;            // LC_SEGMENT_64
    public uint cmdsize;        // includes sizeof section_64 structs
    public string segname;      // segment name
    public ulong vmaddr;        // memory address of this segment
    public ulong vmsize;        // memory size of this segment
    public ulong fileoff;       // file offset of this segment
    public ulong filesize;      // amount to map from the file
    public uint maxprot;        // maximum VM protection
    public uint initprot;       // initial VM protection
    public uint nsects;         // number of sections in segment
    public uint flags;          // flags

    public Mach64SegmentCommand(BinaryReader reader)
    {
        cmd = reader.ReadUInt32();
        cmdsize = reader.ReadUInt32();
        segname = new string(reader.ReadChars(16)).TrimEnd('\0');
        vmaddr = reader.ReadUInt64();
        vmsize = reader.ReadUInt64();
        fileoff = reader.ReadUInt64();
        filesize = reader.ReadUInt64();
        maxprot = reader.ReadUInt32();
        initprot = reader.ReadUInt32();
        nsects = reader.ReadUInt32();
        flags = reader.ReadUInt32();
    }
}

internal struct NList64
{
    public uint n_strx;        // index into the string table
    public byte n_type;        // type flag, see below
    public byte n_sect;        // section number or NO_SECT
    public ushort n_desc;      // see <mach-o/stab.h>
    public ulong n_value;      // value of this symbol (or stab offset)

    public NList64(BinaryReader reader)
    {
        n_strx = reader.ReadUInt32();
        n_type = reader.ReadByte();
        n_sect = reader.ReadByte();
        n_desc = reader.ReadUInt16();
        n_value = reader.ReadUInt64();
    }
}
