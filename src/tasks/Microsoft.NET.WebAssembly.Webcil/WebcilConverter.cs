// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Microsoft.NET.WebAssembly.Webcil;

/// <summary>
/// Reads a .NET assembly in a normal PE COFF file and writes it out as a Webcil file
/// </summary>
public class WebcilConverter
{

    // Interesting stuff we've learned about the input PE file
    public record PEFileInfo(
        // The sections in the PE file
        ImmutableArray<SectionHeader> SectionHeaders,
        // The location of the debug directory entries
        DirectoryEntry DebugTableDirectory,
        // The file offset of the sections, following the section directory
        FilePosition SectionStart,
        // The debug directory entries
        ImmutableArray<DebugDirectoryEntry> DebugDirectoryEntries
        );

    // Interesting stuff we know about the Webcil file we're writing
    public record WCFileInfo(
        // The header of the Webcil file
        WebcilHeader Header,
        // The section directory of the Webcil file
        ImmutableArray<WebcilSectionHeader> SectionHeaders,
        // The file offset of the sections, following the section directory
        FilePosition SectionStart
    );

    private const int SectionAlignment = 16;

    private readonly string _inputPath;
    private readonly string _outputPath;

    private string InputPath => _inputPath;

    public bool WrapInWebAssembly { get; set; } = true;

    private WebcilConverter(string inputPath, string outputPath)
    {
        _inputPath = inputPath;
        _outputPath = outputPath;
    }

    public static WebcilConverter FromPortableExecutable(string inputPath, string outputPath)
        => new WebcilConverter(inputPath, outputPath);

    public void ConvertToWebcil()
    {
        using var inputStream = File.Open(_inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        PEFileInfo peInfo;
        WCFileInfo wcInfo;
        using (var peReader = new PEReader(inputStream, PEStreamOptions.LeaveOpen))
        {
            GatherInfo(peReader, out wcInfo, out peInfo);
        }

        using var outputStream = File.Open(_outputPath, FileMode.Create, FileAccess.Write);
        if (!WrapInWebAssembly)
        {
            WriteConversionTo(outputStream, inputStream, peInfo, wcInfo);
        }
        else
        {
            // if wrapping in WASM, write the Webcil payload to memory because we need to discover the length

            // Webcil is about the same size as the PE file
            using var memoryStream = new MemoryStream(checked((int)inputStream.Length));
            WriteConversionTo(memoryStream, inputStream, peInfo, wcInfo);
            memoryStream.Flush();
            var wrapper = new WebcilWasmWrapper(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            wrapper.WriteWasmWrappedWebcil(outputStream);
        }
    }

    public void WriteConversionTo(Stream outputStream, FileStream inputStream, PEFileInfo peInfo, WCFileInfo wcInfo)
    {
        WriteHeader(outputStream, wcInfo.Header);
        WriteSectionHeaders(outputStream, wcInfo.SectionHeaders);
        CopySections(outputStream, inputStream, peInfo.SectionHeaders, wcInfo.SectionHeaders);
        if (wcInfo.Header.PeDebugSize != 0 && wcInfo.Header.PeDebugRva != 0)
        {
            var wcDebugDirectoryEntries = FixupDebugDirectoryEntries(peInfo, wcInfo);
            OverwriteDebugDirectoryEntries(outputStream, wcInfo, wcDebugDirectoryEntries);
        }
    }

    public record struct FilePosition(int Position)
    {
        public static implicit operator FilePosition(int position) => new(position);

        public static FilePosition operator +(FilePosition left, int right) => new(left.Position + right);
    }

    private static unsafe int SizeOfHeader()
    {
        return sizeof(WebcilHeader);
    }

    public unsafe void GatherInfo(PEReader peReader, out WCFileInfo wcInfo, out PEFileInfo peInfo)
    {
        var headers = peReader.PEHeaders;
        var peHeader = headers.PEHeader!;
        var coffHeader = headers.CoffHeader!;
        var sections = headers.SectionHeaders;
        WebcilHeader header;
        header.Id[0] = (byte)'W';
        header.Id[1] = (byte)'b';
        header.Id[2] = (byte)'I';
        header.Id[3] = (byte)'L';
        header.VersionMajor = Internal.Constants.WC_VERSION_MAJOR;
        header.VersionMinor = Internal.Constants.WC_VERSION_MINOR;
        header.CoffSections = (ushort)coffHeader.NumberOfSections;
        header.Reserved0 = 0;
        header.PeCliHeaderRva = (uint)peHeader.CorHeaderTableDirectory.RelativeVirtualAddress;
        header.PeCliHeaderSize = (uint)peHeader.CorHeaderTableDirectory.Size;
        header.PeDebugRva = (uint)peHeader.DebugTableDirectory.RelativeVirtualAddress;
        header.PeDebugSize = (uint)peHeader.DebugTableDirectory.Size;

        // current logical position in the output file
        FilePosition pos = SizeOfHeader();
        // position of the current section in the output file
        // initially it's after all the section headers, aligned to 16-byte boundary
        FilePosition curSectionPos = AlignTo((pos + sizeof(WebcilSectionHeader) * coffHeader.NumberOfSections).Position, SectionAlignment);
        // The first WC section is at the aligned position after the section directory
        FilePosition firstWCSection = curSectionPos;

        FilePosition firstPESection = 0;

        ImmutableArray<WebcilSectionHeader>.Builder headerBuilder = ImmutableArray.CreateBuilder<WebcilSectionHeader>(coffHeader.NumberOfSections);
        foreach (var sectionHeader in sections)
        {
            // The first section is the one with the lowest file offset
            if (firstPESection.Position == 0)
            {
                firstPESection = sectionHeader.PointerToRawData;
            }
            else
            {
                firstPESection = Math.Min(firstPESection.Position, sectionHeader.PointerToRawData);
            }

            var newHeader = new WebcilSectionHeader
            (
                virtualSize: sectionHeader.VirtualSize,
                virtualAddress: sectionHeader.VirtualAddress,
                sizeOfRawData: sectionHeader.SizeOfRawData,
                pointerToRawData: curSectionPos.Position
            );

            pos += sizeof(WebcilSectionHeader);
            curSectionPos = AlignTo((curSectionPos + sectionHeader.SizeOfRawData).Position, SectionAlignment);
            headerBuilder.Add(newHeader);
        }

        ImmutableArray<DebugDirectoryEntry> debugDirectoryEntries = peReader.ReadDebugDirectory();

        peInfo = new PEFileInfo(SectionHeaders: sections,
                                DebugTableDirectory: peHeader.DebugTableDirectory,
                                SectionStart: firstPESection,
                                DebugDirectoryEntries: debugDirectoryEntries);

        wcInfo = new WCFileInfo(Header: header,
                                SectionHeaders: headerBuilder.MoveToImmutable(),
                                SectionStart: firstWCSection);
    }

    private static void WriteHeader(Stream s, WebcilHeader webcilHeader)
    {
        if (!BitConverter.IsLittleEndian)
        {
            webcilHeader.VersionMajor = BinaryPrimitives.ReverseEndianness(webcilHeader.VersionMajor);
            webcilHeader.VersionMinor = BinaryPrimitives.ReverseEndianness(webcilHeader.VersionMinor);
            webcilHeader.CoffSections = BinaryPrimitives.ReverseEndianness(webcilHeader.CoffSections);
            webcilHeader.PeCliHeaderRva = BinaryPrimitives.ReverseEndianness(webcilHeader.PeCliHeaderRva);
            webcilHeader.PeCliHeaderSize = BinaryPrimitives.ReverseEndianness(webcilHeader.PeCliHeaderSize);
            webcilHeader.PeDebugRva = BinaryPrimitives.ReverseEndianness(webcilHeader.PeDebugRva);
            webcilHeader.PeDebugSize = BinaryPrimitives.ReverseEndianness(webcilHeader.PeDebugSize);
        }
        WriteStructure(s, webcilHeader);
    }

    private static void WriteSectionHeaders(Stream s, ImmutableArray<WebcilSectionHeader> sectionsHeaders)
    {
        foreach (var sectionHeader in sectionsHeaders)
        {
            WriteSectionHeader(s, sectionHeader);
        }
    }

    private static void WriteSectionHeader(Stream s, WebcilSectionHeader sectionHeader)
    {
        if (!BitConverter.IsLittleEndian)
        {
            sectionHeader = new WebcilSectionHeader
            (
                virtualSize: BinaryPrimitives.ReverseEndianness(sectionHeader.VirtualSize),
                virtualAddress: BinaryPrimitives.ReverseEndianness(sectionHeader.VirtualAddress),
                sizeOfRawData: BinaryPrimitives.ReverseEndianness(sectionHeader.SizeOfRawData),
                pointerToRawData: BinaryPrimitives.ReverseEndianness(sectionHeader.PointerToRawData)
            );
        }
        WriteStructure(s, sectionHeader);
    }

#if NET
    private static void WriteStructure<T>(Stream s, T structure)
        where T : unmanaged
    {
        unsafe
        {
            byte* p = (byte*)&structure;
            s.Write(new ReadOnlySpan<byte>(p, sizeof(T)));
        }
    }
#else
    private static void WriteStructure<T>(Stream s, T structure)
        where T : unmanaged
    {
        int size = Marshal.SizeOf<T>();
        byte[] buffer = new byte[size];
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structure, ptr, false);
            Marshal.Copy(ptr, buffer, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        s.Write(buffer, 0, size);
    }
#endif

    private static void CopySections(Stream outStream, FileStream inputStream, ImmutableArray<SectionHeader> peSections, ImmutableArray<WebcilSectionHeader> wcSections)
    {
        // endianness: ok, we're just copying from one stream to another
        for (int i = 0; i < peSections.Length; i++)
        {
            // Write zero padding to reach the aligned section position
            int paddingNeeded = wcSections[i].PointerToRawData - (int)outStream.Position;
            if (paddingNeeded > 0)
            {
                outStream.Write(new byte[paddingNeeded], 0, paddingNeeded);
            }
            var buffer = new byte[peSections[i].SizeOfRawData];
            inputStream.Seek(peSections[i].PointerToRawData, SeekOrigin.Begin);
            ReadExactly(inputStream, buffer);
            outStream.Write(buffer, 0, buffer.Length);
        }
    }

#if NET
    private static void ReadExactly(FileStream s, Span<byte> buffer)
    {
        s.ReadExactly(buffer);
    }
#else
    private static void ReadExactly(FileStream s, byte[] buffer)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = s.Read(buffer, offset, buffer.Length - offset);
            if (read == 0)
                throw new EndOfStreamException();
            offset += read;
        }
    }
#endif

    private static FilePosition GetPositionOfRelativeVirtualAddress(ImmutableArray<WebcilSectionHeader> wcSections, uint relativeVirtualAddress)
    {
        foreach (var section in wcSections)
        {
            if (relativeVirtualAddress >= section.VirtualAddress && relativeVirtualAddress < section.VirtualAddress + section.VirtualSize)
            {
                uint offsetInSection = relativeVirtualAddress - (uint)section.VirtualAddress;
                if (offsetInSection >= (uint)section.SizeOfRawData)
                {
                    throw new InvalidOperationException(
                        $"relative virtual address 0x{relativeVirtualAddress:X} is in virtual tail of section (offset {offsetInSection} >= SizeOfRawData {section.SizeOfRawData})");
                }
                FilePosition pos = section.PointerToRawData + (int)offsetInSection;
                return pos;
            }
        }

        throw new InvalidOperationException("relative virtual address not in any section");
    }

    // Make a new set of debug directory entries that
    // have their data pointers adjusted to be relative to the start of the Webcil file.
    // This is necessary because the debug directory entries in the PE file are relative to the start of the PE file,
    // and section offsets may differ between PE and Webcil due to header sizes and alignment.
    private static ImmutableArray<DebugDirectoryEntry> FixupDebugDirectoryEntries(PEFileInfo peInfo, WCFileInfo wcInfo)
    {
        ImmutableArray<DebugDirectoryEntry> entries = peInfo.DebugDirectoryEntries;
        ImmutableArray<DebugDirectoryEntry>.Builder newEntries = ImmutableArray.CreateBuilder<DebugDirectoryEntry>(entries.Length);
        foreach (var entry in entries)
        {
            DebugDirectoryEntry newEntry;
            if (entry.Type == DebugDirectoryEntryType.Reproducible || entry.DataPointer == 0 || entry.DataSize == 0)
            {
                // this entry doesn't have an associated data pointer, so just copy it
                newEntry = entry;
            }
            else
            {
                // the "DataPointer" field is a file offset in the PE file, translate it to the corresponding offset in the Webcil file
                var newDataPointer = TranslateFileOffset(peInfo.SectionHeaders, wcInfo.SectionHeaders, entry.DataPointer);
                newEntry = new DebugDirectoryEntry(entry.Stamp, entry.MajorVersion, entry.MinorVersion, entry.Type, entry.DataSize, entry.DataRelativeVirtualAddress, newDataPointer);
            }
            newEntries.Add(newEntry);
        }
        return newEntries.MoveToImmutable();
    }

    private static int TranslateFileOffset(ImmutableArray<SectionHeader> peSections, ImmutableArray<WebcilSectionHeader> wcSections, int peFileOffset)
    {
        for (int i = 0; i < peSections.Length; i++)
        {
            var peSection = peSections[i];
            if (peFileOffset >= peSection.PointerToRawData && peFileOffset < peSection.PointerToRawData + peSection.SizeOfRawData)
            {
                int offsetInSection = peFileOffset - peSection.PointerToRawData;
                return wcSections[i].PointerToRawData + offsetInSection;
            }
        }

        throw new InvalidOperationException($"file offset {peFileOffset} not in any PE section");
    }

    private static int AlignTo(int value, int alignment) => (value + (alignment - 1)) & ~(alignment - 1);

    private static void OverwriteDebugDirectoryEntries(Stream s, WCFileInfo wcInfo, ImmutableArray<DebugDirectoryEntry> entries)
    {
        FilePosition debugDirectoryPos = GetPositionOfRelativeVirtualAddress(wcInfo.SectionHeaders, wcInfo.Header.PeDebugRva);
        using var writer = new BinaryWriter(s, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Seek(debugDirectoryPos.Position, SeekOrigin.Begin);
        foreach (var entry in entries)
        {
            WriteDebugDirectoryEntry(writer, entry);
        }
        writer.Flush();
        long bytesWritten = s.Position - debugDirectoryPos.Position;
        if (bytesWritten != wcInfo.Header.PeDebugSize)
        {
            throw new InvalidOperationException(
                $"Debug directory size mismatch: wrote {bytesWritten} bytes, expected {wcInfo.Header.PeDebugSize}");
        }

        // restore the stream position
        writer.Seek(0, SeekOrigin.End);
    }

    private static void WriteDebugDirectoryEntry(BinaryWriter writer, DebugDirectoryEntry entry)
    {
        writer.Write((uint)0); // Characteristics
        writer.Write(entry.Stamp);
        writer.Write(entry.MajorVersion);
        writer.Write(entry.MinorVersion);
        writer.Write((uint)entry.Type);
        writer.Write(entry.DataSize);
        writer.Write(entry.DataRelativeVirtualAddress);
        writer.Write(entry.DataPointer);
    }
}
