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

    private readonly DataTarget _dataTarget;
    private readonly string[] _searchPaths;
    private readonly List<ResolvedModule> _modules = new();

    public string DumpPath { get; }

    /// <summary>
    /// Describes a managed module's loaded image mapping, sourced from the cDAC Loader contract:
    /// the loaded (converted) image base, the image size, the on-disk assembly path, and the PE
    /// identity key (COFF <c>TimeDateStamp</c> and optional-header <c>SizeOfImage</c>) used to
    /// verify that the on-disk file matches the module captured in the dump.
    /// </summary>
    public readonly record struct ManagedModuleImage(ulong Base, ulong Size, string FileName, uint TimeStamp, uint ImageSize);

    /// <summary>A module mapping whose on-disk file has already been located and key-verified.</summary>
    private readonly record struct ResolvedModule(ulong Base, ulong Size, string? FilePath);

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

            // The dump didn't fully cover the range. Serve the remaining bytes from the owning
            // module's on-disk image -- exactly as the legacy DAC and dotnet-dump do for PE/COR
            // headers and ECMA metadata, which heap/mini dumps do not capture. Module ownership is
            // resolved solely from the cDAC Loader contract (see RegisterManagedModules); ClrMD's
            // raw module list is deliberately not consulted, because it registers managed R2R
            // modules at their flat/file base and cannot represent the separate loaded mapping.
            ResolvedModule? module = FindModule(address + (ulong)bytesRead);
            if (module is null || module.Value.FilePath is null)
            {
                return -1;
            }

            return FillFromImage(module.Value.FilePath, module.Value.Base, address, buffer, bytesRead);
        }
        catch
        {
            return -1;
        }
    }

    private ResolvedModule? FindModule(ulong address)
    {
        foreach (ResolvedModule module in _modules)
        {
            if (address >= module.Base && address < module.Base + module.Size)
                return module;
        }
        return null;
    }

    private static int FillFromImage(string foundFile, ulong moduleBase, ulong address, Span<byte> buffer, int bytesRead)
    {
        using FileStream fs = File.OpenRead(foundFile);
        using PEReader peReader = new PEReader(fs);

        int sizeOfHeaders = peReader.PEHeaders.PEHeader?.SizeOfHeaders ?? 0;
        PEMemoryBlock wholeImage = default;
        bool wholeImageLoaded = false;

        int filled = bytesRead;
        ulong current = address + (ulong)bytesRead;
        while (filled < buffer.Length)
        {
            long rvaLong = (long)(current - moduleBase);
            if (rvaLong < 0 || rvaLong > int.MaxValue)
            {
                return -1;
            }
            int rva = (int)rvaLong;

            // GetSectionData maps a (loaded-layout) RVA to the raw section bytes in the on-disk
            // file, so this works whether the requested address is in the flat or loaded layout.
            PEMemoryBlock block = peReader.GetSectionData(rva);
            if (block.Length > 0)
            {
                int toCopy = Math.Min(block.Length, buffer.Length - filled);
                unsafe
                {
                    new ReadOnlySpan<byte>(block.Pointer, toCopy).CopyTo(buffer.Slice(filled));
                }
                filled += toCopy;
                current += (ulong)toCopy;
            }
            else if (rva >= 0 && rva < sizeOfHeaders)
            {
                // PE header region (before the first section): GetSectionData doesn't cover it.
                // For a loaded image the headers sit at file offset == RVA, so serve them from
                // the raw file image (needed to read a module's PE/COR headers when the dump
                // doesn't capture them, e.g. cdac-lite Normal dumps without the legacy DAC).
                if (!wholeImageLoaded)
                {
                    wholeImage = peReader.GetEntireImage();
                    wholeImageLoaded = true;
                }
                int available = Math.Min(sizeOfHeaders, wholeImage.Length) - rva;
                int toCopy = Math.Min(available, buffer.Length - filled);
                if (toCopy <= 0)
                    return -1;
                unsafe
                {
                    new ReadOnlySpan<byte>(wholeImage.Pointer + rva, toCopy).CopyTo(buffer.Slice(filled));
                }
                filled += toCopy;
                current += (ulong)toCopy;
            }
            else
            {
                return -1;
            }
        }

        return 0;
    }

    /// <summary>
    /// Registers managed module images sourced from the cDAC Loader contract so that reads of
    /// loaded-layout addresses (e.g. ECMA metadata, which heap/mini dumps do not capture) resolve
    /// to the on-disk assembly. Mirrors dotnet/diagnostics' ManagedModuleService, which sources
    /// loaded bases from the runtime rather than from ClrMD's raw module list. Each module's
    /// on-disk file is located and verified against its PE identity key (TimeDateStamp +
    /// SizeOfImage) here, once, so a stale or mismatched same-named assembly is never used.
    /// </summary>
    public void RegisterManagedModules(IEnumerable<ManagedModuleImage> modules)
    {
        _modules.Clear();
        foreach (ManagedModuleImage module in modules)
        {
            _modules.Add(new ResolvedModule(module.Base, module.Size, ResolveFile(module)));
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

    /// <summary>
    /// Locates the on-disk file for a module and verifies it matches the module captured in the
    /// dump via its PE identity key (COFF TimeDateStamp + optional-header SizeOfImage) -- the same
    /// key dotnet-dump/ClrMD use for symbol-store lookups. Searches the module's recorded path
    /// first, then the configured symbol paths, and returns the first candidate whose key matches.
    /// Returns null if no matching file is found.
    /// </summary>
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
