// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.DumpCollect;

internal static class ContractDescriptorLocator
{
    private const string RuntimeModuleName = "coreclr.dll";

    private static ReadOnlySpan<byte> ContractDescriptorSymbolName => "DotNetRuntimeContractDescriptor"u8;

    internal static unsafe bool TryGetFromPE(ICLRDataTarget dataTarget, out ulong contractAddress)
    {
        const int DosHeaderSize = 64;
        const int PeHeaderSize = 144;
        const int OptionalHeaderOffset = 24;
        const int Pe32ExportDirectoryOffset = OptionalHeaderOffset + 96;
        const int Pe32PlusExportDirectoryOffset = OptionalHeaderOffset + 112;
        const ushort Pe32Magic = 0x10b;
        const ushort Pe32PlusMagic = 0x20b;
        const uint PeSignature = 0x00004550;

        contractAddress = 0;

        ulong runtimeBase;
        if (dataTarget.GetImageBase(RuntimeModuleName, &runtimeBase) < 0 || runtimeBase == 0)
            return false;

        Span<byte> dosHeader = stackalloc byte[DosHeaderSize];
        if (!TryReadTarget(dataTarget, runtimeBase, dosHeader))
            return false;

        int peHeaderOffset = BinaryPrimitives.ReadInt32LittleEndian(dosHeader[0x3c..]);
        if (peHeaderOffset < 0 || !TryAdd(runtimeBase, (uint)peHeaderOffset, out ulong peHeaderAddress))
            return false;

        Span<byte> peHeader = stackalloc byte[PeHeaderSize];
        if (!TryReadTarget(dataTarget, peHeaderAddress, peHeader)
            || BinaryPrimitives.ReadUInt32LittleEndian(peHeader) != PeSignature)
        {
            return false;
        }

        ushort optionalHeaderMagic = BinaryPrimitives.ReadUInt16LittleEndian(peHeader[OptionalHeaderOffset..]);
        int exportDirectoryOffset = optionalHeaderMagic switch
        {
            Pe32Magic => Pe32ExportDirectoryOffset,
            Pe32PlusMagic => Pe32PlusExportDirectoryOffset,
            _ => -1,
        };
        if (exportDirectoryOffset < 0)
            return false;

        uint exportDirectoryRva = BinaryPrimitives.ReadUInt32LittleEndian(peHeader[exportDirectoryOffset..]);
        uint exportDirectorySize = BinaryPrimitives.ReadUInt32LittleEndian(peHeader[(exportDirectoryOffset + sizeof(uint))..]);
        if (exportDirectoryRva == 0 || exportDirectorySize < 40
            || !TryAdd(runtimeBase, exportDirectoryRva, out ulong exportDirectoryAddress))
        {
            return false;
        }

        Span<byte> exportDirectory = stackalloc byte[40];
        if (!TryReadTarget(dataTarget, exportDirectoryAddress, exportDirectory))
            return false;

        uint functionCount = BinaryPrimitives.ReadUInt32LittleEndian(exportDirectory[20..]);
        uint nameCount = BinaryPrimitives.ReadUInt32LittleEndian(exportDirectory[24..]);
        uint functionTableRva = BinaryPrimitives.ReadUInt32LittleEndian(exportDirectory[28..]);
        uint nameTableRva = BinaryPrimitives.ReadUInt32LittleEndian(exportDirectory[32..]);
        uint ordinalTableRva = BinaryPrimitives.ReadUInt32LittleEndian(exportDirectory[36..]);
        if (functionCount > exportDirectorySize / sizeof(uint) || nameCount > exportDirectorySize / sizeof(uint))
            return false;

        ReadOnlySpan<byte> symbolName = ContractDescriptorSymbolName;
        byte[] targetName = new byte[symbolName.Length + 1];
        Span<byte> namePointer = stackalloc byte[sizeof(uint)];
        Span<byte> ordinalBuffer = stackalloc byte[sizeof(ushort)];
        Span<byte> functionRvaBuffer = stackalloc byte[sizeof(uint)];
        for (uint nameIndex = 0; nameIndex < nameCount; nameIndex++)
        {
            if (!TryAdd(runtimeBase, nameTableRva, nameIndex, sizeof(uint), out ulong namePointerAddress)
                || !TryReadTarget(dataTarget, namePointerAddress, namePointer))
            {
                return false;
            }

            uint nameRva = BinaryPrimitives.ReadUInt32LittleEndian(namePointer);
            if (nameRva == 0 || !TryAdd(runtimeBase, nameRva, out ulong nameAddress)
                || !TryReadTarget(dataTarget, nameAddress, targetName))
            {
                continue;
            }

            if (!targetName.AsSpan(0, symbolName.Length).SequenceEqual(symbolName)
                || targetName[^1] != 0)
            {
                continue;
            }

            if (!TryAdd(runtimeBase, ordinalTableRva, nameIndex, sizeof(ushort), out ulong ordinalAddress)
                || !TryReadTarget(dataTarget, ordinalAddress, ordinalBuffer))
            {
                return false;
            }

            ushort ordinal = BinaryPrimitives.ReadUInt16LittleEndian(ordinalBuffer);
            if (ordinal >= functionCount
                || !TryAdd(runtimeBase, functionTableRva, ordinal, sizeof(uint), out ulong functionAddress)
                || !TryReadTarget(dataTarget, functionAddress, functionRvaBuffer))
            {
                return false;
            }

            uint functionRva = BinaryPrimitives.ReadUInt32LittleEndian(functionRvaBuffer);
            return functionRva != 0 && TryAdd(runtimeBase, functionRva, out contractAddress);
        }

        return false;
    }

    private static unsafe bool TryReadTarget(ICLRDataTarget dataTarget, ulong address, Span<byte> buffer)
    {
        fixed (byte* bufferPointer = buffer)
        {
            uint bytesRead;
            return dataTarget.ReadVirtual(address, bufferPointer, (uint)buffer.Length, &bytesRead) >= 0
                && bytesRead == (uint)buffer.Length;
        }
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
}
