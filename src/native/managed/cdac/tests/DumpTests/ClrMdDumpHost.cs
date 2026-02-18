// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Wraps a ClrMD DataTarget to provide the memory read callback and symbol lookup
/// needed to create a <see cref="ContractDescriptorTarget"/> from a crash dump.
/// </summary>
internal sealed class ClrMdDumpHost : IDisposable
{
    private static readonly string[] s_runtimeModuleNames =
    {
        "coreclr.dll",
        "libcoreclr.so",
        "libcoreclr.dylib",
    };

    private readonly DataTarget _dataTarget;

    public string DumpPath { get; }

    private ClrMdDumpHost(string dumpPath, DataTarget dataTarget)
    {
        DumpPath = dumpPath;
        _dataTarget = dataTarget;
    }

    /// <summary>
    /// Open a crash dump and prepare it for cDAC analysis.
    /// </summary>
    public static ClrMdDumpHost Open(string dumpPath)
    {
        DataTarget dataTarget = DataTarget.LoadDump(dumpPath);
        return new ClrMdDumpHost(dumpPath, dataTarget);
    }

    /// <summary>
    /// Read memory from the dump at the specified address.
    /// Returns 0 on success, non-zero on failure.
    /// </summary>
    public int ReadFromTarget(ulong address, Span<byte> buffer)
    {
        int bytesRead = _dataTarget.DataReader.Read(address, buffer);
        return bytesRead == buffer.Length ? 0 : -1;
    }

    /// <summary>
    /// Locate the DotNetRuntimeContractDescriptor symbol address in the dump.
    /// </summary>
    public ulong FindContractDescriptorAddress()
    {
        // Find the native coreclr module via DataReader (not ClrRuntime, which only lists managed assemblies)
        foreach (ModuleInfo module in _dataTarget.DataReader.EnumerateModules())
        {
            string? fileName = module.FileName;
            if (fileName is null)
                continue;

            string name = System.IO.Path.GetFileName(fileName);
            if (!IsRuntimeModule(name))
                continue;

            ulong address = FindPEExport(module.ImageBase, "DotNetRuntimeContractDescriptor");
            if (address != 0)
                return address;
        }

        throw new InvalidOperationException("Could not find DotNetRuntimeContractDescriptor export in any runtime module in the dump.");
    }

    private static bool IsRuntimeModule(string fileName)
    {
        foreach (string name in s_runtimeModuleNames)
        {
            if (fileName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private ulong FindPEExport(ulong imageBase, string symbolName)
    {
        // Read the DOS header to find the PE header
        Span<byte> dosHeader = stackalloc byte[64];
        if (ReadFromTarget(imageBase, dosHeader) != 0)
            return 0;

        // e_lfanew is at offset 0x3C
        int peOffset = MemoryMarshal.Read<int>(dosHeader.Slice(0x3C));

        // Read PE signature + COFF header
        Span<byte> peHeader = stackalloc byte[4 + 20];
        if (ReadFromTarget(imageBase + (ulong)peOffset, peHeader) != 0)
            return 0;

        if (MemoryMarshal.Read<int>(peHeader) != 0x00004550) // "PE\0\0"
            return 0;

        // Determine PE32 or PE32+ to find export directory offset
        uint optionalHeaderOffset = (uint)peOffset + 4 + 20;
        Span<byte> magic = stackalloc byte[2];
        if (ReadFromTarget(imageBase + optionalHeaderOffset, magic) != 0)
            return 0;

        ushort peMagic = MemoryMarshal.Read<ushort>(magic);
        uint exportDirRvaOffset = peMagic switch
        {
            0x10b => optionalHeaderOffset + 96,  // PE32
            0x20b => optionalHeaderOffset + 112, // PE32+
            _ => 0,
        };
        if (exportDirRvaOffset == 0)
            return 0;

        // Read export directory RVA
        Span<byte> exportDirEntry = stackalloc byte[8];
        if (ReadFromTarget(imageBase + exportDirRvaOffset, exportDirEntry) != 0)
            return 0;

        uint exportRva = MemoryMarshal.Read<uint>(exportDirEntry);
        if (exportRva == 0)
            return 0;

        // Read export directory header (40 bytes)
        Span<byte> exportDir = stackalloc byte[40];
        if (ReadFromTarget(imageBase + exportRva, exportDir) != 0)
            return 0;

        uint numberOfNames = MemoryMarshal.Read<uint>(exportDir.Slice(24));
        uint addressOfFunctions = MemoryMarshal.Read<uint>(exportDir.Slice(28));
        uint addressOfNames = MemoryMarshal.Read<uint>(exportDir.Slice(32));
        uint addressOfNameOrdinals = MemoryMarshal.Read<uint>(exportDir.Slice(36));

        // Search the name pointer table for the symbol
        for (uint i = 0; i < numberOfNames; i++)
        {
            Span<byte> nameRvaBytes = stackalloc byte[4];
            if (ReadFromTarget(imageBase + addressOfNames + i * 4, nameRvaBytes) != 0)
                continue;

            uint nameRva = MemoryMarshal.Read<uint>(nameRvaBytes);

            Span<byte> nameBytes = stackalloc byte[64];
            if (ReadFromTarget(imageBase + nameRva, nameBytes) != 0)
                continue;

            int nullIndex = nameBytes.IndexOf((byte)0);
            if (nullIndex < 0)
                continue;

            string name = System.Text.Encoding.ASCII.GetString(nameBytes.Slice(0, nullIndex));
            if (name != symbolName)
                continue;

            Span<byte> ordinalBytes = stackalloc byte[2];
            if (ReadFromTarget(imageBase + addressOfNameOrdinals + i * 2, ordinalBytes) != 0)
                return 0;

            ushort ordinal = MemoryMarshal.Read<ushort>(ordinalBytes);

            Span<byte> funcRvaBytes = stackalloc byte[4];
            if (ReadFromTarget(imageBase + addressOfFunctions + ordinal * 4u, funcRvaBytes) != 0)
                return 0;

            uint funcRva = MemoryMarshal.Read<uint>(funcRvaBytes);
            return imageBase + funcRva;
        }

        return 0;
    }

    public void Dispose()
    {
        _dataTarget?.Dispose();
    }
}
