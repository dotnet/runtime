// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.IO;
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
    private readonly string[] _searchPaths;

    public string DumpPath { get; }

    private ClrMdDumpHost(string dumpPath, DataTarget dataTarget, string[] searchPaths)
    {
        DumpPath = dumpPath;
        _dataTarget = dataTarget;
        _searchPaths = searchPaths;
    }

    /// <summary>
    /// Open a crash dump and prepare it for cDAC analysis.
    /// </summary>
    /// <param name="dumpPath">Path to the crash dump file.</param>
    /// <param name="additionalSymbolPaths">
    /// Local directories to search for symbol files (e.g., System.Private.CoreLib,
    /// debuggee DLLs).
    /// </param>
    public static ClrMdDumpHost Open(string dumpPath, List<string> additionalSymbolPaths)
    {
        DataTarget dataTarget = DataTarget.LoadDump(dumpPath);

        return new ClrMdDumpHost(dumpPath, dataTarget, additionalSymbolPaths.ToArray());
    }

    /// <summary>
    /// Read memory from the dump at the specified address.
    /// Returns 0 on success, non-zero on failure.
    /// </summary>
    public int ReadFromTarget(ulong address, Span<byte> buffer)
    {
        int bytesRead = _dataTarget.DataReader.Read(address, buffer);
        if (bytesRead == buffer.Length)
            return 0; // success

        // If we couldn't read the full buffer, maybe it's in a PE image
        ModuleInfo? info = GetModuleForAddress(address);
        if (info is null || info.FileName is null)
        {
            return -1;
        }

        string? foundFile = FindFileOnDisk(info.FileName);
        if (foundFile is null)
        {
            return -1;
        }

        using FileStream fs = File.OpenRead(foundFile);
        using PEReader peReader = new PEReader(fs);

        int filled = bytesRead;
        ulong current = address + (ulong)bytesRead;
        while (filled < buffer.Length)
        {
            PEMemoryBlock block = peReader.GetSectionData((int)(current - info.ImageBase));
            if (block.Length == 0)
            {
                return -1;
            }

            int toCopy = Math.Min(block.Length, buffer.Length - filled);
            unsafe
            {
                new ReadOnlySpan<byte>(block.Pointer, toCopy).CopyTo(buffer.Slice(filled));
            }
            filled += toCopy;
            current += (ulong)toCopy;
        }

        return 0;
    }

    /// <summary>
    /// Get a thread's register context from the dump.
    /// Returns 0 on success, non-zero on failure.
    /// </summary>
    public int GetThreadContext(uint threadId, uint contextFlags, Span<byte> buffer)
    {
        return _dataTarget.DataReader.GetThreadContext(threadId, contextFlags, buffer) ? 0 : -1;
    }

    private ModuleInfo? GetModuleForAddress(ulong address)
    {
        foreach (ModuleInfo module in _dataTarget.DataReader.EnumerateModules())
        {
            if (address >= module.ImageBase && address < module.ImageBase + (ulong)module.ImageSize)
                return module;
        }
        return null;
    }

    /// <summary>
    /// Locate the DotNetRuntimeContractDescriptor symbol address in the dump.
    /// Uses ClrMD's built-in export resolution which handles PE, ELF, and Mach-O formats.
    /// </summary>
    public ulong FindContractDescriptorAddress()
    {
        foreach (ModuleInfo module in _dataTarget.DataReader.EnumerateModules())
        {
            string? fileName = module.FileName;
            if (fileName is null)
                continue;

            // Path.GetFileName doesn't handle Windows paths on a Linux/macOS host,
            // so split on both separators to extract the file name correctly when
            // analyzing cross-platform dumps.
            int lastSep = Math.Max(fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));
            string name = lastSep >= 0 ? fileName[(lastSep + 1)..] : fileName;
            if (!IsRuntimeModule(name))
                continue;

            ulong address = module.GetExportSymbolAddress("DotNetRuntimeContractDescriptor");
            if (address != 0)
            {
                // ClrMD may return addresses with spurious upper bits on 32-bit targets
                // (observed on ARM32 ELF). Mask to the target's pointer size.
                // https://github.com/microsoft/clrmd/issues/1407
                if (_dataTarget.DataReader.PointerSize == 4)
                    address &= 0xFFFF_FFFF;

                return address;
            }
        }

        throw new InvalidOperationException("Could not find DotNetRuntimeContractDescriptor export in any runtime module in the dump.");
    }

    private string? FindFileOnDisk(string modulePath)
    {
        // for local runs
        if (File.Exists(modulePath))
            return modulePath;
        int lastSep = Math.Max(modulePath.LastIndexOf('/'), modulePath.LastIndexOf('\\'));
        string fileName = lastSep >= 0 ? modulePath[(lastSep + 1)..] : modulePath;

        foreach (string searchPath in _searchPaths)
        {
            string candidate = Path.Combine(searchPath, fileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
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

    public void Dispose()
    {
        _dataTarget.Dispose();
    }
}
