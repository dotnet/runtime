// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.NET.WebAssembly.Webcil;


public sealed partial class WebcilReader : IDisposable
{
    // WISH:
    // This should be implemented in terms of System.Reflection.Internal.MemoryBlockProvider like the PEReader,
    // but the memory block classes are internal to S.R.M.

    private readonly Stream _stream;
    private WebcilHeader _header;
    private DirectoryEntry _corHeaderMetadataDirectory;
    private MetadataReaderProvider? _metadataReaderProvider;
    private ImmutableArray<WebcilSectionHeader>? _sections;

    private string? InputPath { get; }

    private readonly long _webcilInWasmOffset;

    public WebcilReader(Stream stream)
    {
        this._stream = stream;
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("Stream must be readable and seekable", nameof(stream));
        }
        if (TryReadWasmWrapper(out var webcilInWasmOffset)) {
            _webcilInWasmOffset = webcilInWasmOffset;
            _stream.Seek(_webcilInWasmOffset, SeekOrigin.Begin);
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

    public WebcilReader (Stream stream, string inputPath) : this(stream)
    {
        InputPath = inputPath;
    }

    private unsafe bool ReadHeader()
    {
        WebcilHeader header;
        var buffer = new byte[Marshal.SizeOf<WebcilHeader>()];
        if (_stream.Read(buffer, 0, buffer.Length) != buffer.Length)
        {
            return false;
        }
        fixed (byte* p = buffer)
        {
            header = *(WebcilHeader*)p;
        }
        if (!BitConverter.IsLittleEndian)
        {
            header.version_major = BinaryPrimitives.ReverseEndianness(header.version_major);
            header.version_minor = BinaryPrimitives.ReverseEndianness(header.version_minor);
            header.coff_sections = BinaryPrimitives.ReverseEndianness(header.coff_sections);
            header.pe_cli_header_rva = BinaryPrimitives.ReverseEndianness(header.pe_cli_header_rva);
            header.pe_cli_header_size = BinaryPrimitives.ReverseEndianness(header.pe_cli_header_size);
            header.pe_debug_rva = BinaryPrimitives.ReverseEndianness(header.pe_debug_rva);
            header.pe_debug_rva = BinaryPrimitives.ReverseEndianness(header.pe_debug_size);
        }
        if (header.id[0] != 'W' || header.id[1] != 'b'
            || header.id[2] != 'I' || header.id[3] != 'L'
            || header.version_major != Internal.Constants.WC_VERSION_MAJOR
            || header.version_minor != Internal.Constants.WC_VERSION_MINOR)
        {
            return false;
        }
        _header = header;
        return true;
    }

    private unsafe bool ReadCorHeader()
    {
        // we can't construct CorHeader because it's constructor is internal
        // but we don't care, really, we only want the metadata directory entry
        var pos = TranslateRVA(_header.pe_cli_header_rva);
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
            long pos = TranslateRVA((uint)_corHeaderMetadataDirectory.RelativeVirtualAddress);
            if (_stream.Seek(pos, SeekOrigin.Begin) != pos)
            {
                throw new BadImageFormatException("Could not seek to metadata in ", InputPath);
            }
            _metadataReaderProvider = MetadataReaderProvider.FromMetadataStream(_stream, MetadataStreamOptions.LeaveOpen);
        }
        return _metadataReaderProvider;
    }

    public MetadataReader GetMetadataReader() => GetMetadataReaderProvider().GetMetadataReader();

    public ImmutableArray<DebugDirectoryEntry> ReadDebugDirectory()
    {
        var debugRVA = _header.pe_debug_rva;
        if (debugRVA == 0)
        {
            return ImmutableArray<DebugDirectoryEntry>.Empty;
        }
        var debugSize = _header.pe_debug_size;
        if (debugSize == 0)
        {
            return ImmutableArray<DebugDirectoryEntry>.Empty;
        }
        var debugOffset = TranslateRVA(debugRVA);
        _stream.Seek(debugOffset, SeekOrigin.Begin);
        var buffer = new byte[debugSize];
        if (_stream.Read(buffer, 0, buffer.Length) != buffer.Length)
        {
            throw new BadImageFormatException("Could not read debug directory", InputPath);
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
        var pos = entry.DataPointer + _webcilInWasmOffset;
        var buffer = new byte[entry.DataSize];
        if (_stream.Seek(pos, SeekOrigin.Begin) != pos)
        {
            throw new BadImageFormatException("Could not seek to CodeView debug directory data", nameof(_stream));
        }
        if (_stream.Read(buffer, 0, buffer.Length) != buffer.Length)
        {
            throw new BadImageFormatException("Could not read CodeView debug directory data", nameof(_stream));
        }
        unsafe
        {
            fixed (byte* p = buffer)
            {
                return DecodeCodeViewDebugDirectoryData(new BlobReader(p, buffer.Length));
            }
        }
    }

    private static CodeViewDebugDirectoryData DecodeCodeViewDebugDirectoryData(BlobReader reader)
    {
        // FIXME: copy-pasted from PEReader.DecodeCodeViewDebugDirectoryData

        if (reader.ReadByte() != (byte)'R' ||
            reader.ReadByte() != (byte)'S' ||
            reader.ReadByte() != (byte)'D' ||
            reader.ReadByte() != (byte)'S')
        {
            throw new BadImageFormatException("Unexpected CodeView data signature");
        }

        Guid guid = reader.ReadGuid();
        int age = reader.ReadInt32();
        string path = ReadUtf8NullTerminated(ref reader)!;

        return MakeCodeViewDebugDirectoryData(guid, age, path);
    }

    private static string? ReadUtf8NullTerminated(ref BlobReader reader) => Reflection.ReadUtf8NullTerminated(ref reader);

    private static CodeViewDebugDirectoryData MakeCodeViewDebugDirectoryData(Guid guid, int age, string path) => Reflection.MakeCodeViewDebugDirectoryData(guid, age, path);

    private static PdbChecksumDebugDirectoryData MakePdbChecksumDebugDirectoryData(string algorithmName, ImmutableArray<byte> checksum) => Reflection.MakePdbChecksumDebugDirectoryData(algorithmName, checksum);

    public MetadataReaderProvider ReadEmbeddedPortablePdbDebugDirectoryData(DebugDirectoryEntry entry)
    {
        var pos = entry.DataPointer + _webcilInWasmOffset;
        var buffer = new byte[entry.DataSize];
        if (_stream.Seek(pos, SeekOrigin.Begin) != pos)
        {
            throw new BadImageFormatException("Could not seek to Embedded Portable PDB debug directory data", nameof(_stream));
        }
        if (_stream.Read(buffer, 0, buffer.Length) != buffer.Length)
        {
            throw new BadImageFormatException("Could not read Embedded Portable PDB debug directory data", nameof(_stream));
        }
        unsafe
        {
            fixed (byte* p = buffer)
            {
                return DecodeEmbeddedPortablePdbDirectoryData(new BlobReader(p, buffer.Length));
            }
        }
    }

    private const uint PortablePdbVersions_DebugDirectoryEmbeddedSignature = 0x4244504d;
    private static MetadataReaderProvider DecodeEmbeddedPortablePdbDirectoryData(BlobReader reader)
    {
        // FIXME: inspired by PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData
        // but not using its internal utility classes.

        if (reader.ReadUInt32() != PortablePdbVersions_DebugDirectoryEmbeddedSignature)
        {
            throw new BadImageFormatException("Unexpected embedded portable PDB data signature");
        }

        int decompressedSize = reader.ReadInt32();

        byte[] decompressedBuffer;

        byte[] compressedBuffer = reader.ReadBytes(reader.RemainingBytes);

        using (var compressedStream = new MemoryStream(compressedBuffer, writable: false))
        using (var deflateStream = new System.IO.Compression.DeflateStream(compressedStream, System.IO.Compression.CompressionMode.Decompress, leaveOpen: true))
        {
#if NETCOREAPP1_1_OR_GREATER
            decompressedBuffer = GC.AllocateUninitializedArray<byte>(decompressedSize);
#else
            decompressedBuffer = new byte[decompressedSize];
#endif
            using (var decompressedStream = new MemoryStream(decompressedBuffer, writable: true))
            {
                deflateStream.CopyTo(decompressedStream);
            }
        }


        return MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(decompressedBuffer, writable: false));

    }

    public PdbChecksumDebugDirectoryData ReadPdbChecksumDebugDirectoryData(DebugDirectoryEntry entry)
    {
        if (entry.Type != DebugDirectoryEntryType.PdbChecksum)
        {
            throw new ArgumentException($"expected debug directory entry type {nameof(DebugDirectoryEntryType.PdbChecksum)}", nameof(entry));
        }

        var pos = entry.DataPointer + _webcilInWasmOffset;
        var buffer = new byte[entry.DataSize];
        if (_stream.Seek(pos, SeekOrigin.Begin) != pos)
        {
            throw new BadImageFormatException("Could not seek to CodeView debug directory data", nameof(_stream));
        }
        if (_stream.Read(buffer, 0, buffer.Length) != buffer.Length)
        {
            throw new BadImageFormatException("Could not read CodeView debug directory data", nameof(_stream));
        }
        unsafe
        {
            fixed (byte* p = buffer)
            {
                return DecodePdbChecksumDebugDirectoryData(new BlobReader(p, buffer.Length));
            }
        }
    }

    private static PdbChecksumDebugDirectoryData DecodePdbChecksumDebugDirectoryData(BlobReader reader)
    {
        var algorithmName = ReadUtf8NullTerminated(ref reader);
        byte[]? checksum = reader.ReadBytes(reader.RemainingBytes);
        if (string.IsNullOrEmpty(algorithmName) || checksum == null || checksum.Length == 0)
        {
            throw new BadImageFormatException("Invalid PdbChecksum data format");
        }

        return MakePdbChecksumDebugDirectoryData(algorithmName!, ImmutableArray.Create(checksum));
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
                return section.PointerToRawData + (rva - section.VirtualAddress) + _webcilInWasmOffset;
            }
        }
        throw new BadImageFormatException("RVA not found in any section", nameof(_stream));
    }

    private static long SectionDirectoryOffset => Marshal.SizeOf<WebcilHeader>();

    private unsafe ImmutableArray<WebcilSectionHeader> ReadSections()
    {
        WebcilSectionHeader secheader;
        var sections = ImmutableArray.CreateBuilder<WebcilSectionHeader>(_header.coff_sections);
        var buffer = new byte[Marshal.SizeOf<WebcilSectionHeader>()];
        _stream.Seek(SectionDirectoryOffset + _webcilInWasmOffset, SeekOrigin.Begin);
        for (int i = 0; i < _header.coff_sections; i++)
        {
            if (_stream.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new BadImageFormatException("Stream does not contain a valid Webcil file", nameof(_stream));
            }
            fixed (byte* p = buffer)
            {
                secheader = (*(WebcilSectionHeader*)p);
            }
            if (!BitConverter.IsLittleEndian)
            {
                sections.Add
                (
                    new WebcilSectionHeader
                    (
                        virtualSize: BinaryPrimitives.ReverseEndianness(secheader.VirtualSize),
                        virtualAddress: BinaryPrimitives.ReverseEndianness(secheader.VirtualAddress),
                        sizeOfRawData: BinaryPrimitives.ReverseEndianness(secheader.SizeOfRawData),
                        pointerToRawData: BinaryPrimitives.ReverseEndianness(secheader.PointerToRawData)
                    )
                );
            }
            else
            {
                sections.Add(secheader);
            }
        }
        return sections.MoveToImmutable();
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    private bool TryReadWasmWrapper(out long webcilInWasmOffset)
    {
        webcilInWasmOffset = 0;
        using var reader = new WasmWrapperModuleReader(_stream);
        if (!reader.IsWasmModule)
            return false;
        if (!reader.Visit())
            return false;
        if (!reader.HasWebcil)
            return false;
        webcilInWasmOffset = reader.WebcilPayloadOffset;
        return true;
    }

    private sealed class WasmWrapperModuleReader : WasmModuleReader
    {
        internal bool HasWebcil {get; private set;}
        internal long WebcilPayloadOffset {get; private set; }
        public WasmWrapperModuleReader(Stream stream) : base (stream)
        {
        }

        protected override bool VisitSection (WasmModuleReader.Section sec, out bool shouldStop)
        {
            shouldStop = false;
            if (sec != WasmModuleReader.Section.Data)
                return true;
            shouldStop = true;

            uint numSegments = ReadULEB128();
            if (numSegments != 2)
                return false;

            // skip the first segment
            if (!TryReadPassiveDataSegment (out long _, out long _))
                return false;

            if (!TryReadPassiveDataSegment (out long _, out long segmentStart))
                return false;

            HasWebcil = true;
            WebcilPayloadOffset = segmentStart;
            return true;
        }
    }
}
