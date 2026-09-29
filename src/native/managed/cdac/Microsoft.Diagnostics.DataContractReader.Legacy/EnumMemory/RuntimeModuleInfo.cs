// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;

/// <summary>
/// Describes a runtime module's PE headers, directories, and exports.
/// </summary>
public sealed class RuntimeModuleInfo
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
    private const ushort Pe32Magic = 0x10b;
    private const ushort Pe32PlusMagic = 0x20b;
    private const uint PeSignature = 0x00004550;

    /// <summary>
    /// Reads memory from the target process.
    /// </summary>
    /// <param name="address">The target address to read.</param>
    /// <param name="buffer">The buffer to fill with target memory.</param>
    /// <returns><see langword="true"/> if the entire buffer was read; otherwise, <see langword="false"/>.</returns>
    public delegate bool TryReadMemory(ulong address, Span<byte> buffer);

    private RuntimeModuleInfo(ulong imageBase, uint sizeOfHeaders, TargetDirectory exportDirectory, TargetDirectory resourceDirectory, TargetDirectory debugDirectory)
    {
        ImageBase = imageBase;
        SizeOfHeaders = sizeOfHeaders;
        ExportDirectory = exportDirectory;
        ResourceDirectory = resourceDirectory;
        DebugDirectory = debugDirectory;
    }

    internal ulong ImageBase { get; }
    internal uint SizeOfHeaders { get; }
    private TargetDirectory ExportDirectory { get; }
    private TargetDirectory ResourceDirectory { get; }
    private TargetDirectory DebugDirectory { get; }

    internal IEnumerable<TargetSpan> EnumerateMemoryRegions()
    {
        if (ImageBase != 0 && SizeOfHeaders != 0)
            yield return new TargetSpan(ImageBase, SizeOfHeaders);
        if (DebugDirectory.Address != 0 && DebugDirectory.Size != 0)
            yield return new TargetSpan(DebugDirectory.Address, DebugDirectory.Size);
        if (ResourceDirectory.Address != 0 && ResourceDirectory.Size != 0)
            yield return new TargetSpan(ResourceDirectory.Address, ResourceDirectory.Size);
        if (ExportDirectory.Address != 0 && ExportDirectory.Size != 0)
            yield return new TargetSpan(ExportDirectory.Address, ExportDirectory.Size);
    }

    internal static bool TryCreate(Target target, out RuntimeModuleInfo module)
    {
        module = null!;
        if (!target.TryGetRuntimeImageBase(out TargetPointer imageBase))
            return false;

        try
        {
            return TryCreate(imageBase, (address, buffer) =>
            {
                target.ReadBuffer(address, buffer);
                return true;
            }, out module);
        }
        catch (VirtualReadException)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads the runtime module's PE headers and directories.
    /// </summary>
    /// <param name="imageBase">The runtime module's image base address.</param>
    /// <param name="readMemory">The callback used to read target memory.</param>
    /// <param name="module">When this method returns, contains the module information on success, or <see langword="null"/> on failure.</param>
    /// <returns><see langword="true"/> if the headers were read successfully or the image is not PE; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreate(ulong imageBase, TryReadMemory readMemory, out RuntimeModuleInfo module)
    {
        module = null!;
        Span<byte> dosHeader = stackalloc byte[DosHeaderSize];
        if (!readMemory(imageBase, dosHeader))
            return false;

        if (BinaryPrimitives.ReadUInt16LittleEndian(dosHeader) != DosSignature)
        {
            module = new(imageBase, 0, default, default, default);
            return true;
        }

        int peHeaderOffset = BinaryPrimitives.ReadInt32LittleEndian(dosHeader[0x3c..]);
        if (peHeaderOffset < 0 || !TryAdd(imageBase, (uint)peHeaderOffset, out ulong peHeaderAddress))
            return false;

        Span<byte> peHeader = stackalloc byte[PeHeaderSize];
        if (!readMemory(peHeaderAddress, peHeader)
            || BinaryPrimitives.ReadUInt32LittleEndian(peHeader) != PeSignature)
        {
            return false;
        }

        int dataDirectoryOffset = BinaryPrimitives.ReadUInt16LittleEndian(peHeader[OptionalHeaderOffset..]) switch
        {
            Pe32Magic => Pe32DataDirectoryOffset,
            Pe32PlusMagic => Pe32PlusDataDirectoryOffset,
            _ => -1,
        };
        if (dataDirectoryOffset < 0)
            return false;

        uint sizeOfHeaders = BinaryPrimitives.ReadUInt32LittleEndian(peHeader[SizeOfHeadersOffset..]);
        if (sizeOfHeaders == 0)
            return false;

        module = new(
            imageBase,
            sizeOfHeaders,
            GetDirectory(peHeader, imageBase, dataDirectoryOffset, ExportDirectoryIndex),
            GetDirectory(peHeader, imageBase, dataDirectoryOffset, ResourceDirectoryIndex),
            GetDirectory(peHeader, imageBase, dataDirectoryOffset, DebugDirectoryIndex));
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

    private static TargetDirectory GetDirectory(ReadOnlySpan<byte> peHeader, ulong imageBase, int dataDirectoryOffset, int directoryIndex)
    {
        int entryOffset = dataDirectoryOffset + (directoryIndex * 2 * sizeof(uint));
        uint rva = BinaryPrimitives.ReadUInt32LittleEndian(peHeader[entryOffset..]);
        uint size = BinaryPrimitives.ReadUInt32LittleEndian(peHeader[(entryOffset + sizeof(uint))..]);
        return rva != 0 && size != 0 && TryAdd(imageBase, rva, out ulong address)
            ? new(address, size)
            : default;
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
}
