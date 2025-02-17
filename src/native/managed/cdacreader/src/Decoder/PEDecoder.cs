// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Diagnostics.DataContractReader.Decoder.PETypes;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;
internal sealed class PEDecoder
{
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
        using BinaryReader reader = new(new DataTargetStream(_dataTarget, _baseAddress));

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

        return true;
    }

    public TargetPointer GetSymbolAddress(string symbol)
    {
        if (!IsValid)
            return TargetPointer.Null;

        using BinaryReader reader = new(new DataTargetStream(_dataTarget, _baseAddress));

        for (int nameIndex = 0; nameIndex < _exportDir.NumberOfNames; nameIndex++)
        {
            // Seek to address of names
            reader.BaseStream.Seek(_exportDir.AddressOfNames + sizeof(uint) * nameIndex, SeekOrigin.Begin);
            uint namePointerRVA = reader.ReadUInt32();

            // Seek to name RVA and read name
            reader.BaseStream.Seek(namePointerRVA, SeekOrigin.Begin);
            string name = reader.ReadZString();

            if (name == symbol)
            {
                // If the name matches, we should be able to get the ordinal using the ordinal
                // table with the same index. This table contains 16-bit values.
                reader.BaseStream.Seek(_exportDir.AddressOfNameOrdinals + sizeof(ushort) * nameIndex, SeekOrigin.Begin);
                ushort ordinalForNamedExport = reader.ReadUInt16();

                // Use the ordinal to index into the address table. This table contains 32-bit RVA values.
                reader.BaseStream.Seek(_exportDir.AddressOfFunctions + sizeof(uint) * ordinalForNamedExport, SeekOrigin.Begin);
                uint symbolRVA = reader.ReadUInt32();

                return new TargetPointer(_baseAddress + symbolRVA);
            }
        }

        return TargetPointer.Null;
    }
}
