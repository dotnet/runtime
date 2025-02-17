// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.Decoder.PETypes;

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

        DataDirectory = new IMAGE_DATA_DIRECTORY[NumberOfRvaAndSizes];
        for (int i = 0; i < NumberOfRvaAndSizes; i++)
            DataDirectory[i] = new IMAGE_DATA_DIRECTORY(reader);
    }
}

internal struct IMAGE_OPTIONAL_HEADER64
{
    public ushort Magic;
    public byte MajorLinkerVersion;
    public byte MinorLinkerVersion;
    public uint SizeOfCode;
    public uint SizeOfInitializedData;
    public uint SizeOfUninitializedData;
    public uint AddressOfEntryPoint;
    public uint BaseOfCode;
    public ulong ImageBase;
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
    public ulong SizeOfStackReserve;
    public ulong SizeOfStackCommit;
    public ulong SizeOfHeapReserve;
    public ulong SizeOfHeapCommit;
    public uint LoaderFlags;
    public uint NumberOfRvaAndSizes;
    public IMAGE_DATA_DIRECTORY[] DataDirectory;

    public IMAGE_OPTIONAL_HEADER64(BinaryReader reader)
    {
        Magic = reader.ReadUInt16();
        MajorLinkerVersion = reader.ReadByte();
        MinorLinkerVersion = reader.ReadByte();
        SizeOfCode = reader.ReadUInt32();
        SizeOfInitializedData = reader.ReadUInt32();
        SizeOfUninitializedData = reader.ReadUInt32();
        AddressOfEntryPoint = reader.ReadUInt32();
        BaseOfCode = reader.ReadUInt32();
        ImageBase = reader.ReadUInt64();
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
        SizeOfStackReserve = reader.ReadUInt64();
        SizeOfStackCommit = reader.ReadUInt64();
        SizeOfHeapReserve = reader.ReadUInt64();
        SizeOfHeapCommit = reader.ReadUInt64();
        LoaderFlags = reader.ReadUInt32();
        NumberOfRvaAndSizes = reader.ReadUInt32();

        DataDirectory = new IMAGE_DATA_DIRECTORY[NumberOfRvaAndSizes];
        for (int i = 0; i < NumberOfRvaAndSizes; i++)
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
