// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    /// Get a thread's register context from the dump.
    /// Returns 0 on success, non-zero on failure.
    /// </summary>
    public int GetThreadContext(uint threadId, uint contextFlags, Span<byte> buffer)
    {
        return _dataTarget.DataReader.GetThreadContext(threadId, contextFlags, buffer) ? 0 : -1;
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

    public void Dispose()
    {
        _dataTarget.Dispose();
    }
}
