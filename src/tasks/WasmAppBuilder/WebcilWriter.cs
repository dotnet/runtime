// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using Microsoft.Build.Utilities;

using Microsoft.WebAssembly.Build.Tasks.WebCil;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Reads a .NET assembly in a normal PE COFF file and writes it out as a Webcil file
/// </summary>
public class WebcilWriter
{
    private readonly string _inputPath;
    private readonly string _outputPath;

    private TaskLoggingHelper Log { get; }
    public WebcilWriter(string inputPath, string outputPath, TaskLoggingHelper logger)
    {
        _inputPath = inputPath;
        _outputPath = outputPath;
        Log = logger;
    }

    public void Write()
    {
        Log.LogMessage($"Writing Webcil (input {_inputPath}) output to {_outputPath}");

        using var inputStream = File.Open(_inputPath, FileMode.Open, FileAccess.Read);
        ImmutableArray<CoffSectionHeaderBuilder> sectionsHeaders;
        ImmutableArray<SectionHeader> peSections;
        WCHeader header;
        using (var peReader = new PEReader(inputStream, PEStreamOptions.LeaveOpen))
        {
            // DumpPE(peReader);
            FillHeader(peReader, out header, out peSections, out sectionsHeaders);
        }

        using var outputStream = File.Open(_outputPath, FileMode.Create, FileAccess.Write);
        WriteHeader(outputStream, header);
        WriteSectionHeaders(outputStream, sectionsHeaders);
        CopySections(outputStream, inputStream, peSections);
    }


    private record struct FilePosition(int Position)
    {
        public static implicit operator FilePosition(int position) => new(position);

        public static FilePosition operator +(FilePosition left, int right) => new(left.Position + right);
    }

    private static unsafe int SizeOfHeader()
    {
        return sizeof(WCHeader);
    }

    public static unsafe void FillHeader(PEReader peReader, out WCHeader header, out ImmutableArray<SectionHeader> peSections, out ImmutableArray<CoffSectionHeaderBuilder> sectionsHeaders)
    {
        var headers = peReader.PEHeaders;
        var peHeader = headers.PEHeader!;
        var coffHeader = headers.CoffHeader!;
        var sections = headers.SectionHeaders;
        header.id[0] = (byte)'W';
        header.id[1] = (byte)'C';
        header.version = Constants.WC_VERSION;
        header.reserved0 = 0;
        header.coff_sections = (ushort)coffHeader.NumberOfSections;
        header.reserved1 = 0;
        header.pe_cli_header_rva = (uint)peHeader.CorHeaderTableDirectory.RelativeVirtualAddress;
        header.pe_cli_header_size = (uint)peHeader.CorHeaderTableDirectory.Size;

        // current logical position in the output file
        FilePosition pos = SizeOfHeader();
        // position of the current section in the output file
        // initially it's after all the section headers
        FilePosition curSectionPos = pos + sizeof(CoffSectionHeaderBuilder) * coffHeader.NumberOfSections;

        // TODO: write the sections, but adjust the raw data ptr to the offset after the WCHeader.
        ImmutableArray<CoffSectionHeaderBuilder>.Builder headerBuilder = ImmutableArray.CreateBuilder<CoffSectionHeaderBuilder>(coffHeader.NumberOfSections);
        foreach (var sectionHeader in sections)
        {
            var newHeader = new CoffSectionHeaderBuilder
            (
                virtualSize: sectionHeader.VirtualSize,
                virtualAddress: sectionHeader.VirtualAddress,
                sizeOfRawData: sectionHeader.SizeOfRawData,
                pointerToRawData: curSectionPos.Position
            );

            pos += sizeof(CoffSectionHeaderBuilder);
            curSectionPos += sectionHeader.SizeOfRawData;
            headerBuilder.Add(newHeader);
        }

        peSections = sections;
        sectionsHeaders = headerBuilder.ToImmutable();
    }

    private static void WriteHeader(Stream s, WCHeader header)
    {
        WriteStructure(s, header);
    }

    private static void WriteSectionHeaders(Stream s, ImmutableArray<CoffSectionHeaderBuilder> sectionsHeaders)
    {
        // FIXME: fixup endianness
        if (!BitConverter.IsLittleEndian)
            throw new NotImplementedException();
        foreach (var sectionHeader in sectionsHeaders)
        {
            WriteSectionHeader(s, sectionHeader);
        }
    }

    private static void WriteSectionHeader(Stream s, CoffSectionHeaderBuilder sectionHeader)
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

    private static void CopySections(Stream outStream, Stream inputStream, ImmutableArray<SectionHeader> peSections)
    {
        // endianness: ok, we're just copying from one stream to another
        foreach (var peHeader in peSections)
        {
            inputStream.Seek(peHeader.PointerToRawData, SeekOrigin.Begin);
            inputStream.CopyTo(outStream, peHeader.SizeOfRawData);
        }
    }

}
