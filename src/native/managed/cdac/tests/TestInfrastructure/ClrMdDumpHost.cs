// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.DataContractReader.TestInfrastructure;

/// <summary>
/// Wraps a ClrMD DataTarget to provide the memory read callback and symbol lookup
/// needed to create a <see cref="ContractDescriptorTarget"/> from a crash dump.
/// </summary>
public sealed class ClrMdDumpHost : IDisposable
{
    private static readonly string[] s_runtimeModuleNames =
    {
        "coreclr.dll",
        "libcoreclr.so",
        "libcoreclr.dylib",
    };

    public readonly record struct ManagedModuleImage(ulong Base, ulong Size, string FileName, uint TimeStamp, uint ImageSize);

    private readonly record struct ModuleEntry(ManagedModuleImage Image, Lazy<string?> FilePath);

    private readonly DataTarget _dataTarget;
    private readonly string[] _searchPaths;
    private readonly List<ModuleEntry> _modules = [];

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
        try
        {
            int bytesRead = _dataTarget.DataReader.Read(address, buffer);
            if (bytesRead == buffer.Length)
                return 0; // success

            ModuleEntry? module = FindModule(address + (ulong)bytesRead);
            if (module is null)
            {
                return -1;
            }

            string? filePath = module.Value.FilePath.Value;
            if (filePath is null)
            {
                return -1;
            }

            using FileStream fs = File.OpenRead(filePath);
            using PEReader peReader = new PEReader(fs);

            int filled = bytesRead;
            ulong current = address + (ulong)bytesRead;
            while (filled < buffer.Length)
            {
                long rvaLong = (long)(current - module.Value.Image.Base);
                if (rvaLong < 0 || rvaLong > int.MaxValue)
                    return -1;

                PEMemoryBlock block = peReader.GetSectionData((int)rvaLong);
                if (block.Length == 0)
                    return -1;

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
        catch
        {
            return -1;
        }
    }

    private ModuleEntry? FindModule(ulong address)
    {
        foreach (ModuleEntry entry in _modules)
        {
            if (address >= entry.Image.Base && address < entry.Image.Base + entry.Image.Size)
                return entry;
        }
        return null;
    }

    public void RegisterManagedModules(IEnumerable<ManagedModuleImage> modules)
    {
        _modules.Clear();
        foreach (ManagedModuleImage module in modules)
        {
            _modules.Add(new ModuleEntry(module, new Lazy<string?>(() => ResolveFile(module))));
        }
    }

    /// <summary>
    /// Get a thread's register context from the dump.
    /// Returns 0 on success, non-zero on failure.
    /// </summary>
    public int GetThreadContext(uint threadId, uint contextFlags, Span<byte> buffer)
    {
        return _dataTarget.DataReader.GetThreadContext(threadId, contextFlags, buffer) ? 0 : -1;
    }

    private string? ResolveFile(ManagedModuleImage module)
    {
        foreach (string candidate in EnumerateCandidatePaths(module.FileName))
        {
            if (File.Exists(candidate) && KeyMatches(candidate, module.TimeStamp, module.ImageSize))
                return candidate;
        }
        return null;
    }

    private IEnumerable<string> EnumerateCandidatePaths(string modulePath)
    {
        // The path recorded in the runtime (may be absolute; only meaningful on the collecting host).
        yield return modulePath;

        int lastSep = Math.Max(modulePath.LastIndexOf('/'), modulePath.LastIndexOf('\\'));
        string fileName = lastSep >= 0 ? modulePath[(lastSep + 1)..] : modulePath;
        foreach (string searchPath in _searchPaths)
            yield return Path.Combine(searchPath, fileName);
    }

    private static bool KeyMatches(string path, uint expectedTimeStamp, uint expectedImageSize)
    {
        // A zero key means the module didn't report identity info; accept a name match in that case.
        if (expectedTimeStamp == 0 && expectedImageSize == 0)
            return true;
        try
        {
            using FileStream fs = File.OpenRead(path);
            using PEReader peReader = new PEReader(fs);
            PEHeaders headers = peReader.PEHeaders;
            uint actualTimeStamp = (uint)headers.CoffHeader.TimeDateStamp;
            uint actualImageSize = (uint)(headers.PEHeader?.SizeOfImage ?? 0);
            return actualTimeStamp == expectedTimeStamp && actualImageSize == expectedImageSize;
        }
        catch
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
