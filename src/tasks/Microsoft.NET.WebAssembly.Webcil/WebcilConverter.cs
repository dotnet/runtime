// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

    // Intersting stuff we know about the webcil file we're writing
    public record WCFileInfo(
        // The header of the webcil file
        WebcilHeader Header,
        // The section directory of the webcil file
        ImmutableArray<WebcilSectionHeader> SectionHeaders,
        // The file offset of the sections, following the section directory
        FilePosition SectionStart
    );

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
            // if wrapping in WASM, write the webcil payload to memory because we need to discover the length

            // webcil is about the same size as the PE file
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
        CopySections(outputStream, inputStream, peInfo.SectionHeaders);
        if (wcInfo.Header.pe_debug_size != 0 && wcInfo.Header.pe_debug_rva != 0)
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
        header.id[0] = (byte)'W';
        header.id[1] = (byte)'b';
        header.id[2] = (byte)'I';
        header.id[3] = (byte)'L';
        header.version_major = Internal.Constants.WC_VERSION_MAJOR;
        header.version_minor = Internal.Constants.WC_VERSION_MINOR;
        header.coff_sections = (ushort)coffHeader.NumberOfSections;
        header.reserved0 = 0;
        header.pe_cli_header_rva = (uint)peHeader.CorHeaderTableDirectory.RelativeVirtualAddress;
        header.pe_cli_header_size = (uint)peHeader.CorHeaderTableDirectory.Size;
        header.pe_debug_rva = (uint)peHeader.DebugTableDirectory.RelativeVirtualAddress;
        header.pe_debug_size = (uint)peHeader.DebugTableDirectory.Size;

        // current logical position in the output file
        FilePosition pos = SizeOfHeader();
        // position of the current section in the output file
        // initially it's after all the section headers
        FilePosition curSectionPos = pos + sizeof(WebcilSectionHeader) * coffHeader.NumberOfSections;
        // The first WC section is immediately after the section directory
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
            curSectionPos += sectionHeader.SizeOfRawData;
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

    private static void WriteHeader(Stream s, WebcilHeader header)
    {
        WriteStructure(s, header);
    }

    private static void WriteSectionHeaders(Stream s, ImmutableArray<WebcilSectionHeader> sectionsHeaders)
    {
        // FIXME: fixup endianness
        if (!BitConverter.IsLittleEndian)
            throw new NotImplementedException();
        foreach (var sectionHeader in sectionsHeaders)
        {
            WriteSectionHeader(s, sectionHeader);
        }
    }

    private static void WriteSectionHeader(Stream s, WebcilSectionHeader sectionHeader)
    {
        WriteStructure(s, sectionHeader);
    }

#if NETCOREAPP2_1_OR_GREATER
    private static void WriteStructure<T>(Stream s, T structure)
        where T : unmanaged
    {
        // FIXME: fixup endianness
        if (!BitConverter.IsLittleEndian)
            throw new NotImplementedException();
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
        // FIXME: fixup endianness
        if (!BitConverter.IsLittleEndian)
            throw new NotImplementedException();
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

    private static void CopySections(Stream outStream, FileStream inputStream, ImmutableArray<SectionHeader> peSections)
    {
        // endianness: ok, we're just copying from one stream to another
        foreach (var peHeader in peSections)
        {
            var buffer = new byte[peHeader.SizeOfRawData];
            inputStream.Seek(peHeader.PointerToRawData, SeekOrigin.Begin);
            ReadExactly(inputStream, buffer);
            outStream.Write(buffer, 0, buffer.Length);
        }
    }

#if NETCOREAPP2_1_OR_GREATER
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
                FilePosition pos = section.PointerToRawData + ((int)relativeVirtualAddress - section.VirtualAddress);
                return pos;
            }
        }

        throw new InvalidOperationException("relative virtual address not in any section");
    }

    // Given a physical file offset, return the section and the offset within the section.
    private (WebcilSectionHeader section, int offset) GetSectionFromFileOffset(ImmutableArray<WebcilSectionHeader> peSections, FilePosition fileOffset)
    {
        foreach (var section in peSections)
        {
            if (fileOffset.Position >= section.PointerToRawData && fileOffset.Position < section.PointerToRawData + section.SizeOfRawData)
            {
                return (section, fileOffset.Position - section.PointerToRawData);
            }
        }

        throw new InvalidOperationException($"file offset not in any section (Webcil) for {InputPath}");
    }

    private void GetSectionFromFileOffset(ImmutableArray<SectionHeader> sections, FilePosition fileOffset)
    {
        foreach (var section in sections)
        {
            if (fileOffset.Position >= section.PointerToRawData && fileOffset.Position < section.PointerToRawData + section.SizeOfRawData)
            {
                return;
            }
        }

        throw new InvalidOperationException($"file offset {fileOffset.Position} not in any section (PE) for {InputPath}");
    }

    // Make a new set of debug directory entries that
    // have their data pointers adjusted to be relative to the start of the webcil file.
    // This is necessary because the debug directory entires in the PE file are relative to the start of the PE file,
    // and a PE header is bigger than a webcil header.
    private ImmutableArray<DebugDirectoryEntry> FixupDebugDirectoryEntries(PEFileInfo peInfo, WCFileInfo wcInfo)
    {
        int dataPointerAdjustment = peInfo.SectionStart.Position - wcInfo.SectionStart.Position;
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
                // the "DataPointer" field is a file offset in the PE file, adjust the entry wit the corresponding offset in the Webcil file
                var newDataPointer = entry.DataPointer - dataPointerAdjustment;
                newEntry = new DebugDirectoryEntry(entry.Stamp, entry.MajorVersion, entry.MinorVersion, entry.Type, entry.DataSize, entry.DataRelativeVirtualAddress, newDataPointer);
                GetSectionFromFileOffset(peInfo.SectionHeaders, entry.DataPointer);
                // validate that the new entry is in some section
                GetSectionFromFileOffset(wcInfo.SectionHeaders, newDataPointer);
            }
            newEntries.Add(newEntry);
        }
        return newEntries.MoveToImmutable();
    }

    private static void OverwriteDebugDirectoryEntries(Stream s, WCFileInfo wcInfo, ImmutableArray<DebugDirectoryEntry> entries)
    {
        FilePosition debugDirectoryPos = GetPositionOfRelativeVirtualAddress(wcInfo.SectionHeaders, wcInfo.Header.pe_debug_rva);
        using var writer = new BinaryWriter(s, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Seek(debugDirectoryPos.Position, SeekOrigin.Begin);
        foreach (var entry in entries)
        {
            WriteDebugDirectoryEntry(writer, entry);
        }
        // TODO check that we overwrite with the same size as the original

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
