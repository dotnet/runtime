// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.Decoder.PETypes;

internal struct IMAGE_DOS_HEADER
{
    public ushort e_magic;
    public ushort e_cblp;
    public ushort e_cp;
    public ushort e_crlc;
    public ushort e_cparhdr;
    public ushort e_minalloc;
    public ushort e_maxalloc;
    public ushort e_ss;
    public ushort e_sp;
    public ushort e_csum;
    public ushort e_ip;
    public ushort e_cs;
    public ushort e_lfarlc;
    public ushort e_ovno;
    public ushort[] e_res1;
    public ushort e_oemid;
    public ushort e_oeminfo;
    public ushort[] e_res2;
    public int e_lfanew;

    public IMAGE_DOS_HEADER(BinaryReader reader)
    {
        e_magic = reader.ReadUInt16();
        e_cblp = reader.ReadUInt16();
        e_cp = reader.ReadUInt16();
        e_crlc = reader.ReadUInt16();
        e_cparhdr = reader.ReadUInt16();
        e_minalloc = reader.ReadUInt16();
        e_maxalloc = reader.ReadUInt16();
        e_ss = reader.ReadUInt16();
        e_sp = reader.ReadUInt16();
        e_csum = reader.ReadUInt16();
        e_ip = reader.ReadUInt16();
        e_cs = reader.ReadUInt16();
        e_lfarlc = reader.ReadUInt16();
        e_ovno = reader.ReadUInt16();

        e_res1 = new ushort[4];
        for (int i = 0; i < 4; i++)
            e_res1[i] = reader.ReadUInt16();

        e_oemid = reader.ReadUInt16();
        e_oeminfo = reader.ReadUInt16();

        e_res2 = new ushort[10];
        for (int i = 0; i < 10; i++)
            e_res2[i] = reader.ReadUInt16();

        e_lfanew = reader.ReadInt32();
    }
}

internal struct IMAGE_NT_HEADERS
{
    public uint Signature;
    public IMAGE_FILE_HEADER FileHeader;
    public IMAGE_OPTIONAL_HEADER32 OptionalHeader;

    public IMAGE_NT_HEADERS(BinaryReader reader)
    {
        Signature = reader.ReadUInt32();
        FileHeader = new IMAGE_FILE_HEADER(reader);
        OptionalHeader = new IMAGE_OPTIONAL_HEADER32(reader);
    }
}

internal struct IMAGE_FILE_HEADER
{
    public ushort Machine;
    public ushort NumberOfSections;
    public uint TimeDateStamp;
    public uint PointerToSymbolTable;
    public uint NumberOfSymbols;
    public ushort SizeOfOptionalHeader;
    public ushort Characteristics;

    public IMAGE_FILE_HEADER(BinaryReader reader)
    {
        Machine = reader.ReadUInt16();
        NumberOfSections = reader.ReadUInt16();
        TimeDateStamp = reader.ReadUInt32();
        PointerToSymbolTable = reader.ReadUInt32();
        NumberOfSymbols = reader.ReadUInt32();
        SizeOfOptionalHeader = reader.ReadUInt16();
        Characteristics = reader.ReadUInt16();
    }
}

internal struct IMAGE_OPTIONAL_HEADER32
{
    public ushort Magic;
    public byte MajorLinkerVersion;
    public byte MinorLinkerVersion;
    public uint SizeOfCode;
    public uint SizeOfInitializedData;
    public uint SizeOfUninitializedData;
    public uint AddressOfEntryPoint;
    public uint BaseOfCode;
    public uint BaseOfData;
    public uint ImageBase;
    public uint SectionAlignment;
    public uint FileAlignment;
    public ushort MajorOperatingSystemVersion;
    public ushort MinorOperatingSystemVersion;
    public ushort MajorImageVersion;
    public ushort MinorImageVersion;
    public ushort MajorSubsystemVersion;
    public ushort MinorSubsystemVersion;
    public uint Win32VersionValue;
    public uint SizeOfImage;
    public uint SizeOfHeaders;
    public uint CheckSum;
    public ushort Subsystem;
    public ushort DllCharacteristics;
    public uint SizeOfStackReserve;
    public uint SizeOfStackCommit;
    public uint SizeOfHeapReserve;
    public uint SizeOfHeapCommit;
    public uint LoaderFlags;
    public uint NumberOfRvaAndSizes;
    public IMAGE_DATA_DIRECTORY[] DataDirectory;

    public IMAGE_OPTIONAL_HEADER32(BinaryReader reader)
    {
        Magic = reader.ReadUInt16();
        MajorLinkerVersion = reader.ReadByte();
        MinorLinkerVersion = reader.ReadByte();
        SizeOfCode = reader.ReadUInt32();
        SizeOfInitializedData = reader.ReadUInt32();
        SizeOfUninitializedData = reader.ReadUInt32();
        AddressOfEntryPoint = reader.ReadUInt32();
        BaseOfCode = reader.ReadUInt32();
        BaseOfData = reader.ReadUInt32();
        ImageBase = reader.ReadUInt32();
        SectionAlignment = reader.ReadUInt32();
        FileAlignment = reader.ReadUInt32();
        MajorOperatingSystemVersion = reader.ReadUInt16();
        MinorOperatingSystemVersion = reader.ReadUInt16();
        MajorImageVersion = reader.ReadUInt16();
        MinorImageVersion = reader.ReadUInt16();
        MajorSubsystemVersion = reader.ReadUInt16();
        MinorSubsystemVersion = reader.ReadUInt16();
        Win32VersionValue = reader.ReadUInt32();
        SizeOfImage = reader.ReadUInt32();
        SizeOfHeaders = reader.ReadUInt32();
        CheckSum = reader.ReadUInt32();
        Subsystem = reader.ReadUInt16();
        DllCharacteristics = reader.ReadUInt16();
        SizeOfStackReserve = reader.ReadUInt32();
        SizeOfStackCommit = reader.ReadUInt32();
        SizeOfHeapReserve = reader.ReadUInt32();
        SizeOfHeapCommit = reader.ReadUInt32();
        LoaderFlags = reader.ReadUInt32();
        NumberOfRvaAndSizes = reader.ReadUInt32();

        DataDirectory = new IMAGE_DATA_DIRECTORY[16];
        for (int i = 0; i < 16; i++)
            DataDirectory[i] = new IMAGE_DATA_DIRECTORY(reader);
    }
}

internal struct IMAGE_DATA_DIRECTORY
{
    public uint VirtualAddress;
    public uint Size;

    public IMAGE_DATA_DIRECTORY(BinaryReader reader)
    {
        VirtualAddress = reader.ReadUInt32();
        Size = reader.ReadUInt32();
    }
}

internal struct IMAGE_EXPORT_DIRECTORY
{
    public uint Characteristics;
    public uint TimeDateStamp;
    public ushort MajorVersion;
    public ushort MinorVersion;
    public uint Name;
    public uint Base;
    public uint NumberOfFunctions;
    public uint NumberOfNames;
    public uint AddressOfFunctions;
    public uint AddressOfNames;
    public uint AddressOfNameOrdinals;

    public IMAGE_EXPORT_DIRECTORY(BinaryReader reader)
    {
        Characteristics = reader.ReadUInt32();
        TimeDateStamp = reader.ReadUInt32();
        MajorVersion = reader.ReadUInt16();
        MinorVersion = reader.ReadUInt16();
        Name = reader.ReadUInt32();
        Base = reader.ReadUInt32();
        NumberOfFunctions = reader.ReadUInt32();
        NumberOfNames = reader.ReadUInt32();
        AddressOfFunctions = reader.ReadUInt32();
        AddressOfNames = reader.ReadUInt32();
        AddressOfNameOrdinals = reader.ReadUInt32();
    }
}
