// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.WebAssembly.Diagnostics.Webcil;


public sealed class WebcilReader : IDisposable
{
    // TODO: WISH:
    // This should be implemented in terms of System.Reflection.Internal.MemoryBlockProvider like the PEReader,
    // but the memory block classes are internal to S.R.M.

    private readonly Stream _stream;
    private WCHeader? _header;
    private DirectoryEntry? _corHeaderMetadataDirectory;
    private MetadataReaderProvider _metadataReaderProvider;
    private ImmutableArray<CoffSectionHeaderBuilder>? _sections;

    public WebcilReader(Stream stream)
    {
        this._stream = stream;
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("Stream must be readable and seekable", nameof(stream));
        }
        if (!ReadHeader())
        {
            throw new BadImageFormatException("Stream does not contain a valid Webcil file", nameof(stream));
        }
        if (!ReadCorHeader())
        {
            throw new BadImageFormatException("Stream does not contain a valid COR header in the Webcil file", nameof(stream));
        }
    }

    private unsafe bool ReadHeader()
    {
        WCHeader header;
        var buffer = new byte[Marshal.SizeOf<WCHeader>()];
        if (_stream.Read(buffer, 0, buffer.Length) != buffer.Length)
        {
            return false;
        }
        fixed (byte* p = buffer)
        {
            header = *(WCHeader*)p;
        }
        if (header.id[0] != 'W' || header.id[1] != 'C' || header.version != Constants.WC_VERSION)
        {
            return false;
        }
        _header = header;
        if (!BitConverter.IsLittleEndian)
        {
            throw new NotImplementedException("TODO: implement big endian support");
        }
        return true;
    }

    private unsafe bool ReadCorHeader()
    {
        // we can't construct CorHeader because it's constructor is internal
        // but we don't care, really, we only want the metadata directory entry
        var pos = TranslateRVA(_header.Value.pe_cli_header_rva);
        if (_stream.Seek(pos, SeekOrigin.Begin) != pos)
        {
            return false;
        }
        using var reader = new BinaryReader(_stream, System.Text.Encoding.UTF8, leaveOpen: true);
        reader.ReadInt32(); // byte count
        reader.ReadUInt16(); // major version
        reader.ReadUInt16(); // minor version
        _corHeaderMetadataDirectory = new DirectoryEntry(reader.ReadInt32(), reader.ReadInt32());
        return true;
    }

    public MetadataReaderProvider GetMetadataReaderProvider()
    {
        // FIXME threading
        if (_metadataReaderProvider == null)
        {
            long pos = TranslateRVA((uint)_corHeaderMetadataDirectory.Value.RelativeVirtualAddress);
            if (_stream.Seek(pos, SeekOrigin.Begin) != pos)
            {
                throw new BadImageFormatException("Could not seek to metadata", nameof(_stream));
            }
            _metadataReaderProvider = MetadataReaderProvider.FromMetadataStream(_stream, MetadataStreamOptions.LeaveOpen);
        }
        return _metadataReaderProvider;
    }

    public MetadataReader GetMetadataReader() => GetMetadataReaderProvider().GetMetadataReader();

    public ImmutableArray<DebugDirectoryEntry> ReadDebugDirectory()
    {
        var debugRVA = _header.Value.pe_debug_rva;
        if (debugRVA == 0)
        {
            return ImmutableArray<DebugDirectoryEntry>.Empty;
        }
        var debugSize = _header.Value.pe_debug_size;
        if (debugSize == 0)
        {
            return ImmutableArray<DebugDirectoryEntry>.Empty;
        }
        var debugOffset = TranslateRVA(debugRVA);
        _stream.Seek(debugOffset, SeekOrigin.Begin);
        var buffer = new byte[debugSize];
        if (_stream.Read(buffer, 0, buffer.Length) != buffer.Length)
        {
            throw new BadImageFormatException("Could not read debug directory", nameof(_stream));
        }
        unsafe
        {
            fixed (byte* p = buffer)
            {
                return ReadDebugDirectoryEntries(new BlobReader(p, buffer.Length));
            }
        }
    }

    // FIXME: copied from DebugDirectoryEntry.Size
    internal const int DebugDirectoryEntrySize =
        sizeof(uint) +   // Characteristics
        sizeof(uint) +   // TimeDataStamp
        sizeof(uint) +   // Version
        sizeof(uint) +   // Type
        sizeof(uint) +   // SizeOfData
        sizeof(uint) +   // AddressOfRawData
        sizeof(uint);    // PointerToRawData


    // FIXME: copy-pasted from PEReader
    private static ImmutableArray<DebugDirectoryEntry> ReadDebugDirectoryEntries(BlobReader reader)
    {
        int entryCount = reader.Length / DebugDirectoryEntrySize;
        var builder = ImmutableArray.CreateBuilder<DebugDirectoryEntry>(entryCount);
        for (int i = 0; i < entryCount; i++)
        {
            // Reserved, must be zero.
            int characteristics = reader.ReadInt32();
            if (characteristics != 0)
            {
                throw new BadImageFormatException();
            }

            uint stamp = reader.ReadUInt32();
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();

            var type = (DebugDirectoryEntryType)reader.ReadInt32();

            int dataSize = reader.ReadInt32();
            int dataRva = reader.ReadInt32();
            int dataPointer = reader.ReadInt32();

            builder.Add(new DebugDirectoryEntry(stamp, majorVersion, minorVersion, type, dataSize, dataRva, dataPointer));
        }

        return builder.MoveToImmutable();
    }

    public CodeViewDebugDirectoryData ReadCodeViewDebugDirectoryData(DebugDirectoryEntry entry)
    {
        // TODO: implement by copying PEReader.DecodeCodeViewDebugDirectoryData
        throw new NotImplementedException("FIXME: implement ReadCodeViewDebugDirectoryData");
    }

    public MetadataReaderProvider ReadEmbeddedPortablePdbDebugDirectoryData(DebugDirectoryEntry entry)
    {
        // TODO: implement by copying PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData
        throw new NotImplementedException("FIXME: Implement ReadEmbeddedPortablePdbDebugDirectoryData");
    }

    private long TranslateRVA(uint rva)
    {
        if (_sections == null)
        {
            _sections = ReadSections();
        }
        foreach (var section in _sections.Value)
        {
            if (rva >= section.VirtualAddress && rva < section.VirtualAddress + section.VirtualSize)
            {
                return section.PointerToRawData + (rva - section.VirtualAddress);
            }
        }
        throw new BadImageFormatException("RVA not found in any section", nameof(_stream));
    }

    private static long SectionDirectoryOffset => Marshal.SizeOf<WCHeader>();

    private unsafe ImmutableArray<CoffSectionHeaderBuilder> ReadSections()
    {
        var sections = ImmutableArray.CreateBuilder<CoffSectionHeaderBuilder>(_header.Value.coff_sections);
        var buffer = new byte[Marshal.SizeOf<CoffSectionHeaderBuilder>()];
        _stream.Seek(SectionDirectoryOffset, SeekOrigin.Begin);
        for (int i = 0; i < _header.Value.coff_sections; i++)
        {
            if (_stream.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new BadImageFormatException("Stream does not contain a valid Webcil file", nameof(_stream));
            }
            fixed (byte* p = buffer)
            {
                // FIXME endianness
                sections.Add(*(CoffSectionHeaderBuilder*)p);
            }
        }
        return sections.MoveToImmutable();
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}
