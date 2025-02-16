// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;
internal sealed class PEDecoder
{
    private struct IMAGE_OPTIONAL_HEADER32
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
            for (int i=0; i<NumberOfRvaAndSizes; i++)
                DataDirectory[i] = new IMAGE_DATA_DIRECTORY(reader);
        }
    }

    private struct IMAGE_OPTIONAL_HEADER64
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

    private struct IMAGE_DATA_DIRECTORY
    {
        public uint VirtualAddress;
        public uint Size;

        public IMAGE_DATA_DIRECTORY(BinaryReader reader)
        {
            VirtualAddress = reader.ReadUInt32();
            Size = reader.ReadUInt32();
        }
    }

    private struct IMAGE_EXPORT_DIRECTORY
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

        public override readonly string ToString()
        {
            return $"Characteristics: {Characteristics}, TimeDateStamp: {TimeDateStamp}, MajorVersion: {MajorVersion}, MinorVersion: {MinorVersion}, " +
                    $"Name: {Name}, Base: {Base}, NumberOfFunctions: {NumberOfFunctions}, NumberOfNames: {NumberOfNames}, " +
                    $"AddressOfFunctions: {AddressOfFunctions}, AddressOfNames: {AddressOfNames}, AddressOfNameOrdinals: {AddressOfNameOrdinals}";
        }
    }

    private readonly ICLRDataTarget _dataTarget;
    private readonly ulong _baseAddress;
    private uint _peSigOffset;
    private ushort _optHeaderMagic;
    private IMAGE_EXPORT_DIRECTORY _exportDir;

    public bool IsValid { get; init; }

    public PEDecoder(ICLRDataTarget dataTarget, ulong baseAddress)
    {
        _dataTarget = dataTarget;
        _baseAddress = baseAddress;

        IsValid = Initialize();
    }

    private bool Initialize()
    {
        DataTargetStream stream = new(_dataTarget, _baseAddress);
        BinaryReader reader = new(stream);

        ushort dosMagic = reader.ReadUInt16();
        if (dosMagic != 0x5A4D) // "MZ"
            return false;

        // PE Header offset is at 0x3C in DOS header
        reader.BaseStream.Seek(0x3C, SeekOrigin.Begin);
        _peSigOffset = reader.ReadUInt32();

        // Read PE signature
        reader.BaseStream.Seek(_peSigOffset, SeekOrigin.Begin);
        uint peSig = reader.ReadUInt32();
        if (peSig != 0x00004550) // "PE00"
            return false;

        // Seek to beginning of opt header and read magic
        reader.BaseStream.Seek(_peSigOffset + 0x18, SeekOrigin.Begin);
        _optHeaderMagic = reader.ReadUInt16();

        // Seek back to beginning of opt header and parse
        reader.BaseStream.Seek(_peSigOffset + 0x18, SeekOrigin.Begin);
        uint rva;
        switch (_optHeaderMagic)
        {
            case 0x10B: // PE32
                IMAGE_OPTIONAL_HEADER32 optHeader32 = new(reader);
                rva = optHeader32.DataDirectory[0].VirtualAddress;
                break;
            case 0x20B: // PE32+
                IMAGE_OPTIONAL_HEADER64 optHeader64 = new(reader);
                rva = optHeader64.DataDirectory[0].VirtualAddress;
                break;
            // unknown type, invalid
            default:
                return false;
        }

        // Seek to export directory and parse
        reader.BaseStream.Seek(rva, SeekOrigin.Begin);
        _exportDir = new IMAGE_EXPORT_DIRECTORY(reader);

        Console.WriteLine(_exportDir.ToString());

        return true;
    }

    public TargetPointer GetSymbolAddress(string symbol)
    {
        if (!IsValid)
            return TargetPointer.Null;

        Console.WriteLine($"GetSymbolAddress({symbol})");
        DataTargetStream stream = new(_dataTarget, _baseAddress);
        BinaryReader reader = new(stream, Encoding.ASCII);

        for (int nameIndex = 0; nameIndex < _exportDir.NumberOfNames; nameIndex++)
        {
            // Seek to address of names
            reader.BaseStream.Seek(_exportDir.AddressOfNames + sizeof(uint) * nameIndex, SeekOrigin.Begin);
            uint namePointerRVA = reader.ReadUInt32();

            // Seek to name RVA and read name
            reader.BaseStream.Seek(namePointerRVA, SeekOrigin.Begin);
            string name = reader.ReadZString();
            Console.WriteLine($"Name: {name}");
            if (name == symbol)
            {
                // // Seek to address of ordinals
                // reader.BaseStream.Seek(_exportDir.AddressOfNameOrdinals + (uint)(nameIndex * 2), SeekOrigin.Begin);
                // ushort ordinal = reader.ReadUInt16();

                // // Seek to address of functions
                // reader.BaseStream.Seek(_exportDir.AddressOfFunctions + (uint)(ordinal * 4), SeekOrigin.Begin);
                // uint functionRva = reader.ReadUInt32();

                // // Return the function RVA
                // return new TargetPointer(_baseAddress + functionRva);
            }
        }

        return TargetPointer.Null;
    }
}
