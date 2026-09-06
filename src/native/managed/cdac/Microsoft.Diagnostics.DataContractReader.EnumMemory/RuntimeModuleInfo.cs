// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.EnumMemory;

internal sealed class RuntimeModuleInfo
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
    internal const string RuntimeModuleName = "coreclr.dll";
    internal static ReadOnlySpan<byte> ContractDescriptorSymbolName => "DotNetRuntimeContractDescriptor"u8;

    private readonly ICLRDataTarget _dataTarget;

    private RuntimeModuleInfo(
        ICLRDataTarget dataTarget,
        ulong imageBase,
        uint sizeOfHeaders,
        TargetDirectory exportDirectory,
        TargetDirectory resourceDirectory,
        TargetDirectory debugDirectory)
    {
        _dataTarget = dataTarget;
        ImageBase = imageBase;
        SizeOfHeaders = sizeOfHeaders;
        ExportDirectory = exportDirectory;
        ResourceDirectory = resourceDirectory;
        DebugDirectory = debugDirectory;
    }

    public ulong ImageBase { get; }
    public uint SizeOfHeaders { get; }
    public TargetDirectory ExportDirectory { get; }
    private TargetDirectory ResourceDirectory { get; }
    private TargetDirectory DebugDirectory { get; }

    public void EnumerateMemoryRegions(MemoryRegionEmitter emitter)
    {
        emitter.Add(ImageBase, SizeOfHeaders);
        AddDirectory(emitter, DebugDirectory);
        AddDirectory(emitter, ResourceDirectory);
        AddDirectory(emitter, ExportDirectory);
    }

    internal bool TryGetExport(
        ReadOnlySpan<byte> symbolName,
        out ulong address)
    {
        address = 0;
        if (ExportDirectory.Size < 40)
            return false;

        Span<byte> exportDirectory = stackalloc byte[40];
        if (!TryReadTarget(ExportDirectory.Address, exportDirectory))
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
                || !TryReadTarget(namePointerAddress, namePointer))
            {
                return false;
            }

            uint nameRva = BinaryPrimitives.ReadUInt32LittleEndian(namePointer);
            if (nameRva == 0 || !TryAdd(ImageBase, nameRva, out ulong nameAddress)
                || !TryReadTarget(nameAddress, targetName))
            {
                continue;
            }

            if (!targetName.AsSpan(0, symbolName.Length).SequenceEqual(symbolName)
                || targetName[^1] != 0)
            {
                continue;
            }

            if (!TryAdd(ImageBase, ordinalTableRva, nameIndex, sizeof(ushort), out ulong ordinalAddress)
                || !TryReadTarget(ordinalAddress, ordinalBuffer))
            {
                return false;
            }

            ushort ordinal = BinaryPrimitives.ReadUInt16LittleEndian(ordinalBuffer);
            if (ordinal >= functionCount
                || !TryAdd(ImageBase, functionTableRva, ordinal, sizeof(uint), out ulong functionAddress)
                || !TryReadTarget(functionAddress, functionRvaBuffer))
            {
                return false;
            }

            uint functionRva = BinaryPrimitives.ReadUInt32LittleEndian(functionRvaBuffer);
            return functionRva != 0 && TryAdd(ImageBase, functionRva, out address);
        }

        return false;
    }

    internal static unsafe bool TryCreate(
        ICLRDataTarget dataTarget,
        out RuntimeModuleInfo module)
    {
        module = null!;

        ulong imageBase = 0;
        if (dataTarget is ICLRRuntimeLocator runtimeLocator)
            runtimeLocator.GetRuntimeBase(&imageBase);
        else
            dataTarget.GetImageBase(RuntimeModuleName, &imageBase);

        if (imageBase == 0)
            return false;

        Span<byte> dosHeader = stackalloc byte[DosHeaderSize];
        if (!TryReadTarget(dataTarget, imageBase, dosHeader))
            return false;

        if (BinaryPrimitives.ReadUInt16LittleEndian(dosHeader) != DosSignature)
        {
            module = new(dataTarget, imageBase, 0, default, default, default);
            return true;
        }

        int peHeaderOffset = BinaryPrimitives.ReadInt32LittleEndian(dosHeader[0x3c..]);
        if (peHeaderOffset < 0 || !TryAdd(imageBase, (uint)peHeaderOffset, out ulong peHeaderAddress))
            return false;

        Span<byte> peHeader = stackalloc byte[PeHeaderSize];
        if (!TryReadTarget(dataTarget, peHeaderAddress, peHeader)
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
            dataTarget,
            imageBase,
            sizeOfHeaders,
            GetDirectory(peHeader, imageBase, dataDirectoryOffset, ExportDirectoryIndex),
            GetDirectory(peHeader, imageBase, dataDirectoryOffset, ResourceDirectoryIndex),
            GetDirectory(peHeader, imageBase, dataDirectoryOffset, DebugDirectoryIndex));
        return true;
    }

    private unsafe bool TryReadTarget(ulong address, Span<byte> buffer)
        => TryReadTarget(_dataTarget, address, buffer);

    private static unsafe bool TryReadTarget(
        ICLRDataTarget dataTarget,
        ulong address,
        Span<byte> buffer)
    {
        fixed (byte* bufferPointer = buffer)
        {
            uint bytesRead;
            return dataTarget.ReadVirtual(address, bufferPointer, (uint)buffer.Length, &bytesRead) >= 0
                && bytesRead == (uint)buffer.Length;
        }
    }

    private static void AddDirectory(MemoryRegionEmitter emitter, TargetDirectory directory)
    {
        if (directory.Address != 0 && directory.Size != 0)
            emitter.Add(directory.Address, directory.Size);
    }

    private static TargetDirectory GetDirectory(
        ReadOnlySpan<byte> peHeader,
        ulong imageBase,
        int dataDirectoryOffset,
        int directoryIndex)
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

    private static bool TryAdd(
        ulong baseAddress,
        uint tableRva,
        uint index,
        uint elementSize,
        out ulong address)
    {
        ulong tableOffset = (ulong)tableRva + ((ulong)index * elementSize);
        address = baseAddress + tableOffset;
        return tableOffset >= tableRva && address >= baseAddress;
    }

    internal readonly struct TargetDirectory(ulong address, uint size)
    {
        public ulong Address { get; } = address;
        public uint Size { get; } = size;
    }
}
