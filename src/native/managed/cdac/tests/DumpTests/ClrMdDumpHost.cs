// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.PortableExecutable;
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
    /// Locate module metadata from the PE file on disk when it cannot be read from dump memory.
    /// Uses ClrMD's <see cref="IFileLocator"/> to find the PE file by matching the module name,
    /// timestamp, and image size recorded in the dump, then extracts the raw ECMA-335 metadata.
    /// </summary>
    public bool TryGetReadOnlyMetadata(string? modulePath, out byte[] metadata)
    {
        metadata = [];
        if (modulePath is null)
            return false;

        // First, try reading directly from the module path on disk.
        // For local dumps, the module path recorded in the target process
        // typically points to a valid file on the same machine.
        if (TryReadMetadataFromPE(modulePath, out metadata))
            return true;

        // Fall back to ClrMD's FileLocator, which can use symbol servers
        // and local caches to find PE files by timestamp and image size.
        string targetFileName = GetPortableFileName(modulePath);

        foreach (ModuleInfo module in _dataTarget.DataReader.EnumerateModules())
        {
            if (module.FileName is null)
                continue;

            string moduleFileName = GetPortableFileName(module.FileName);
            if (!moduleFileName.Equals(targetFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            // Try the module's dump-recorded file path directly
            if (TryReadMetadataFromPE(module.FileName, out metadata))
                return true;

            // Try FileLocator (symbol server / local cache)
            string? localPath = _dataTarget.FileLocator?.FindPEImage(
                module.FileName,
                module.IndexTimeStamp,
                module.IndexFileSize,
                checkProperties: false);

            if (localPath is not null && TryReadMetadataFromPE(localPath, out metadata))
                return true;
        }

        return false;
    }

    private static bool TryReadMetadataFromPE(string path, out byte[] metadata)
    {
        metadata = [];
        if (!File.Exists(path))
            return false;

        try
        {
            using FileStream fs = File.OpenRead(path);
            using PEReader peReader = new(fs, PEStreamOptions.PrefetchEntireImage);
            if (!peReader.HasMetadata)
                return false;

            var metadataBlock = peReader.GetMetadata();
            metadata = new byte[metadataBlock.Length];
            metadataBlock.GetContent().CopyTo(metadata);
            return metadata.Length > 0;
        }
        catch (System.Exception ex) when (ex is BadImageFormatException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
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

            string name = GetPortableFileName(fileName);
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

    /// <summary>
    /// Extracts the file name from a path, handling both Windows and Unix separators
    /// for cross-platform dump analysis.
    /// </summary>
    private static string GetPortableFileName(string path)
    {
        int lastSep = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        return lastSep >= 0 ? path[(lastSep + 1)..] : path;
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
