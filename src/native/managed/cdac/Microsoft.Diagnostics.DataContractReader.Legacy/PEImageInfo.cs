// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Describes a module's PE headers, directories, and exports.
/// </summary>
public sealed class PEImageInfo
{
    private const int DosHeaderSize = 64;
    private const int PeHeaderSize = 264;
    private const int OptionalHeaderOffset = 24;
    private const int SizeOfHeadersOffset = OptionalHeaderOffset + 60;
    private const int Pe32DataDirectoryOffset = OptionalHeaderOffset + 96;
    private const int Pe32PlusDataDirectoryOffset = OptionalHeaderOffset + 112;
    private const int ExportDirectoryIndex = 0;
    private const int ResourceDirectoryIndex = 2;
    private const int DebugDirectoryIndex = 6;
    private const ushort DosSignature = 0x5a4d;
    private const uint PeSignature = 0x00004550;
    private const int SectionHeaderSize = 40;
    private const int DebugDirectoryEntrySize = 28;

    private readonly uint _imageSize;
    private readonly bool _isMapped;
    private readonly ImageSection[] _sections;

    /// <summary>
    /// Reads memory from the target process.
    /// </summary>
    /// <param name="address">The target address to read.</param>
    /// <param name="buffer">The buffer to fill with target memory.</param>
    /// <returns><see langword="true"/> if the entire buffer was read; otherwise, <see langword="false"/>.</returns>
    public delegate bool TryReadMemory(ulong address, Span<byte> buffer);

    private PEImageInfo(ulong imageBase, uint imageSize, uint sizeOfHeaders, bool isMapped, ImageSection[] sections)
    {
        ImageBase = imageBase;
        _imageSize = imageSize;
        SizeOfHeaders = sizeOfHeaders;
        _isMapped = isMapped;
        _sections = sections;
    }

    internal ulong ImageBase { get; }
    internal uint SizeOfHeaders { get; }
    private TargetDirectory ExportDirectory { get; set; }
    private TargetDirectory ResourceDirectory { get; set; }
    private TargetDirectory DebugDirectory { get; set; }

    internal IEnumerable<TargetSpan> EnumerateMemoryRegions(bool includeExportsAndResources = true)
    {
        if (ImageBase != 0 && SizeOfHeaders != 0)
            yield return new TargetSpan(ImageBase, SizeOfHeaders);
        if (DebugDirectory.Address != 0 && DebugDirectory.Size != 0)
            yield return new TargetSpan(DebugDirectory.Address, DebugDirectory.Size);
        if (includeExportsAndResources && ResourceDirectory.Address != 0 && ResourceDirectory.Size != 0)
            yield return new TargetSpan(ResourceDirectory.Address, ResourceDirectory.Size);
        if (includeExportsAndResources && ExportDirectory.Address != 0 && ExportDirectory.Size != 0)
            yield return new TargetSpan(ExportDirectory.Address, ExportDirectory.Size);
    }

    internal IEnumerable<DebugEntry> EnumerateDebugEntries(TryReadMemory readMemory)
    {
        if (DebugDirectory.Size < DebugDirectoryEntrySize)
            yield break;

        // IMAGE_DEBUG_DIRECTORY entries refer to payloads outside the directory itself.
        // https://learn.microsoft.com/windows/win32/debug/pe-format#debug-directory-image-only
        byte[] entry = new byte[DebugDirectoryEntrySize];
        for (uint offset = 0; DebugDirectory.Size - offset >= DebugDirectoryEntrySize; offset += DebugDirectoryEntrySize)
        {
            if (!readMemory(DebugDirectory.Address + offset, entry))
                throw new InvalidOperationException("Could not read the PE debug directory entry.");

            DebugDirectoryEntryType type = (DebugDirectoryEntryType)BinaryPrimitives.ReadInt32LittleEndian(entry.AsSpan(12));
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(entry.AsSpan(16));
            uint rva = BinaryPrimitives.ReadUInt32LittleEndian(entry.AsSpan(20));
            if (size == 0 || rva == 0)
                continue;
            if (!TryResolveRva(rva, size, out ulong address))
                throw new InvalidOperationException("The PE debug payload is outside the image.");

            yield return new DebugEntry(type, new TargetSpan(address, size));
        }
    }

    /// <summary>
    /// Reads a module's PE headers and directories.
    /// </summary>
    /// <param name="imageBase">The module's image base address.</param>
    /// <param name="imageSize">The maximum readable image size.</param>
    /// <param name="isMapped"><see langword="true"/> for a loaded image; otherwise, <see langword="false"/> for a flat image.</param>
    /// <param name="readMemory">The callback used to read target memory.</param>
    /// <param name="module">When this method returns, contains the module information on success, or <see langword="null"/> on failure.</param>
    /// <returns><see langword="true"/> if the headers were read successfully or the image is not PE; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreate(ulong imageBase, uint imageSize, bool isMapped, TryReadMemory readMemory, out PEImageInfo module)
    {
        module = null!;
        Span<byte> dosHeader = stackalloc byte[DosHeaderSize];
        if (imageSize < DosHeaderSize || !TryAdd(imageBase, imageSize, out _) || !readMemory(imageBase, dosHeader))
            return false;

        if (BinaryPrimitives.ReadUInt16LittleEndian(dosHeader) != DosSignature)
        {
            module = new(imageBase, imageSize, 0, isMapped, []);
            return true;
        }

        int peHeaderOffset = BinaryPrimitives.ReadInt32LittleEndian(dosHeader[0x3c..]);
        if (peHeaderOffset < 0 || (uint)peHeaderOffset > imageSize || imageSize - (uint)peHeaderOffset < PeHeaderSize
            || !TryAdd(imageBase, (uint)peHeaderOffset, out ulong peHeaderAddress))
            return false;

        Span<byte> peHeader = stackalloc byte[PeHeaderSize];
        if (!readMemory(peHeaderAddress, peHeader)
            || BinaryPrimitives.ReadUInt32LittleEndian(peHeader) != PeSignature)
        {
            return false;
        }

        int dataDirectoryOffset = (PEMagic)BinaryPrimitives.ReadUInt16LittleEndian(peHeader[OptionalHeaderOffset..]) switch
        {
            PEMagic.PE32 => Pe32DataDirectoryOffset,
            PEMagic.PE32Plus => Pe32PlusDataDirectoryOffset,
            _ => -1,
        };
        if (dataDirectoryOffset < 0)
            return false;

        uint sizeOfHeaders = BinaryPrimitives.ReadUInt32LittleEndian(peHeader[SizeOfHeadersOffset..]);
        if (sizeOfHeaders == 0 || sizeOfHeaders > imageSize)
            return false;

        ImageSection[] sections = [];
        if (!isMapped)
        {
            ushort count = BinaryPrimitives.ReadUInt16LittleEndian(peHeader[6..]);
            ushort optionalHeaderSize = BinaryPrimitives.ReadUInt16LittleEndian(peHeader[20..]);
            ulong sectionOffset = (ulong)peHeaderOffset + OptionalHeaderOffset + optionalHeaderSize;
            if (optionalHeaderSize < dataDirectoryOffset - OptionalHeaderOffset + (DebugDirectoryIndex + 1) * 8
                || sectionOffset > sizeOfHeaders || (ulong)count * SectionHeaderSize > sizeOfHeaders - sectionOffset)
                return false;

            sections = new ImageSection[count];
            Span<byte> section = stackalloc byte[SectionHeaderSize];
            for (int i = 0; i < count; i++)
            {
                if (!readMemory(imageBase + sectionOffset + (uint)i * SectionHeaderSize, section))
                    return false;
                sections[i] = new(
                    BinaryPrimitives.ReadUInt32LittleEndian(section[12..]),
                    BinaryPrimitives.ReadUInt32LittleEndian(section[20..]),
                    BinaryPrimitives.ReadUInt32LittleEndian(section[16..]));
            }
        }

        module = new(imageBase, imageSize, sizeOfHeaders, isMapped, sections);
        module.ExportDirectory = module.GetDirectory(peHeader, dataDirectoryOffset, ExportDirectoryIndex);
        module.ResourceDirectory = module.GetDirectory(peHeader, dataDirectoryOffset, ResourceDirectoryIndex);
        module.DebugDirectory = module.GetDirectory(peHeader, dataDirectoryOffset, DebugDirectoryIndex);
        return true;
    }

    /// <summary>
    /// Finds an exported symbol in the runtime module.
    /// </summary>
    /// <param name="readMemory">The callback used to read target memory.</param>
    /// <param name="symbolName">The UTF-8 symbol name without a null terminator.</param>
    /// <param name="address">When this method returns successfully, contains the exported symbol's address.</param>
    /// <returns><see langword="true"/> if the symbol was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetExport(TryReadMemory readMemory, ReadOnlySpan<byte> symbolName, out ulong address)
    {
        address = 0;
        if (ExportDirectory.Size < 40)
            return false;

        Span<byte> exportDirectory = stackalloc byte[40];
        if (!readMemory(ExportDirectory.Address, exportDirectory))
            return false;

        uint functionCount = BinaryPrimitives.ReadUInt32LittleEndian(exportDirectory[20..]);
        uint nameCount = BinaryPrimitives.ReadUInt32LittleEndian(exportDirectory[24..]);
        uint functionTableRva = BinaryPrimitives.ReadUInt32LittleEndian(exportDirectory[28..]);
        uint nameTableRva = BinaryPrimitives.ReadUInt32LittleEndian(exportDirectory[32..]);
        uint ordinalTableRva = BinaryPrimitives.ReadUInt32LittleEndian(exportDirectory[36..]);
        if (functionCount > ExportDirectory.Size / sizeof(uint)
            || nameCount > ExportDirectory.Size / sizeof(uint))
        {
            return false;
        }

        byte[] targetName = new byte[symbolName.Length + 1];
        Span<byte> namePointer = stackalloc byte[sizeof(uint)];
        Span<byte> ordinalBuffer = stackalloc byte[sizeof(ushort)];
        Span<byte> functionRvaBuffer = stackalloc byte[sizeof(uint)];
        for (uint nameIndex = 0; nameIndex < nameCount; nameIndex++)
        {
            if (!TryAdd(ImageBase, nameTableRva, nameIndex, sizeof(uint), out ulong namePointerAddress)
                || !readMemory(namePointerAddress, namePointer))
            {
                return false;
            }

            uint nameRva = BinaryPrimitives.ReadUInt32LittleEndian(namePointer);
            if (nameRva == 0 || !TryAdd(ImageBase, nameRva, out ulong nameAddress)
                || !readMemory(nameAddress, targetName))
            {
                continue;
            }

            if (!targetName.AsSpan(0, symbolName.Length).SequenceEqual(symbolName)
                || targetName[^1] != 0)
            {
                continue;
            }

            if (!TryAdd(ImageBase, ordinalTableRva, nameIndex, sizeof(ushort), out ulong ordinalAddress)
                || !readMemory(ordinalAddress, ordinalBuffer))
            {
                return false;
            }

            ushort ordinal = BinaryPrimitives.ReadUInt16LittleEndian(ordinalBuffer);
            if (ordinal >= functionCount
                || !TryAdd(ImageBase, functionTableRva, ordinal, sizeof(uint), out ulong functionAddress)
                || !readMemory(functionAddress, functionRvaBuffer))
            {
                return false;
            }

            uint functionRva = BinaryPrimitives.ReadUInt32LittleEndian(functionRvaBuffer);
            return functionRva != 0 && TryAdd(ImageBase, functionRva, out address);
        }

        return false;
    }

    private TargetDirectory GetDirectory(ReadOnlySpan<byte> peHeader, int dataDirectoryOffset, int directoryIndex)
    {
        int entryOffset = dataDirectoryOffset + (directoryIndex * 2 * sizeof(uint));
        uint rva = BinaryPrimitives.ReadUInt32LittleEndian(peHeader[entryOffset..]);
        uint size = BinaryPrimitives.ReadUInt32LittleEndian(peHeader[(entryOffset + sizeof(uint))..]);
        return rva != 0 && size != 0 && TryResolveRva(rva, size, out ulong address)
            ? new(address, size)
            : default;
    }

    private bool TryResolveRva(uint rva, uint size, out ulong address)
    {
        address = 0;
        uint offset = rva;
        if (!_isMapped && rva >= SizeOfHeaders)
        {
            bool found = false;
            foreach (ImageSection section in _sections)
            {
                if (rva < section.VirtualAddress)
                    continue;
                uint delta = rva - section.VirtualAddress;
                if (delta > section.RawDataSize || size > section.RawDataSize - delta
                    || section.RawDataOffset > uint.MaxValue - delta)
                    continue;
                offset = section.RawDataOffset + delta;
                found = true;
                break;
            }
            if (!found)
                return false;
        }
        else if (!_isMapped && size > SizeOfHeaders - rva)
        {
            return false;
        }

        return offset <= _imageSize && size <= _imageSize - offset
            && TryAdd(ImageBase, offset, out address) && TryAdd(address, size, out _);
    }

    private static bool TryAdd(ulong baseAddress, uint offset, out ulong address)
    {
        address = baseAddress + offset;
        return address >= baseAddress;
    }

    private static bool TryAdd(ulong baseAddress, uint tableRva, uint index, uint elementSize, out ulong address)
    {
        ulong tableOffset = (ulong)tableRva + ((ulong)index * elementSize);
        address = baseAddress + tableOffset;
        return tableOffset >= tableRva && address >= baseAddress;
    }

    private readonly struct TargetDirectory(ulong address, uint size)
    {
        public ulong Address { get; } = address;
        public uint Size { get; } = size;
    }

    private readonly record struct ImageSection(uint VirtualAddress, uint RawDataOffset, uint RawDataSize);

    internal readonly record struct DebugEntry(DebugDirectoryEntryType Type, TargetSpan Data);
}
