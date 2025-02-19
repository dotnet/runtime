// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Decoder.PETypes;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;
internal sealed class PEDecoder : IDisposable
{
    private readonly Stream _stream;
    private uint _peSigOffset;
    private ushort _optHeaderMagic;
    private IMAGE_EXPORT_DIRECTORY _exportDir;
    private bool _disposedValue;

    public bool IsValid { get; init; }

    /// <summary>
    /// Create PEDecoder with stream beginning at the base address of the module.
    /// </summary>
    public PEDecoder(Stream stream)
    {
        _stream = stream;

        IsValid = Initialize();
    }

    private bool Initialize()
    {
        using BinaryReader reader = new(_stream, Encoding.UTF8, leaveOpen: true);

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

    public bool TryGetRelativeSymbolAddress(string symbol, out ulong address)
    {
        address = 0;
        if (!IsValid)
            return false;

        using BinaryReader reader = new(_stream, Encoding.UTF8, leaveOpen: true);

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

                address = symbolRVA;
                return true;
            }
        }

        return false;
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _stream.Close();
            }

            _disposedValue = true;
        }
    }

    void IDisposable.Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
