// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.PortableExecutable;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.DotNet.Diagnostics.CdacDumpInspect;

internal static class DumpHelpers
{
    private static readonly string[] s_coreClrModuleNames = ["coreclr.dll", "libcoreclr.so", "libcoreclr.dylib"];

    public static ulong FindContractDescriptor(DataTarget dt)
    {
        // First pass: look in known CoreCLR modules.
        // Second pass: check all remaining modules (covers NativeAOT where the export is in the app binary).
        ulong fallback = 0;
        foreach (ModuleInfo module in dt.DataReader.EnumerateModules())
        {
            ulong addr = module.GetExportSymbolAddress("DotNetRuntimeContractDescriptor");
            if (addr == 0)
                continue;

            if (dt.DataReader.PointerSize == 4)
                addr &= 0xFFFF_FFFF;

            string? fileName = module.FileName;
            if (fileName is not null)
            {
                int lastSep = Math.Max(fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));
                string name = lastSep >= 0 ? fileName[(lastSep + 1)..] : fileName;
                if (s_coreClrModuleNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                    return addr;
            }

            if (fallback == 0)
                fallback = addr;
        }

        if (fallback != 0)
            return fallback;

        throw new InvalidOperationException("Could not find DotNetRuntimeContractDescriptor export.");
    }

    public static ContractDescriptorTarget CreateCdacTarget(DataTarget dt)
    {
        ulong contractAddr = FindContractDescriptor(dt);

        if (!ContractDescriptorTarget.TryCreate(
                contractAddr,
                (ulong address, Span<byte> buffer) => ReadWithImageFallback(dt, address, buffer),
                (ulong address, Span<byte> buffer) => -1,
                (uint threadId, uint contextFlags, Span<byte> buffer) =>
                    dt.DataReader.GetThreadContext(threadId, contextFlags, buffer) ? 0 : -1,
                (uint threadId, ReadOnlySpan<byte> context) => -1,
                (ulong size, out ulong allocatedAddress) => { allocatedAddress = 0; return -1; },
                [CoreCLRContracts.Register],
                out ContractDescriptorTarget? target))
        {
            throw new InvalidOperationException("Failed to create cDAC target.");
        }

        return target!;
    }

    /// <summary>
    /// Reads memory from the dump, falling back to the on-disk PE image when the dump
    /// does not contain the requested bytes. Minidumps (and the DAC's Normal dumps) omit
    /// most module content — R2R code/metadata in particular — so the reader must re-read
    /// it from the module file recorded in the dump. Returns 0 on success, -1 on failure.
    /// </summary>
    private static int ReadWithImageFallback(DataTarget dt, ulong address, Span<byte> buffer)
    {
        try
        {
            int bytesRead = dt.DataReader.Read(address, buffer);
            if (bytesRead == buffer.Length)
                return 0;

            ModuleInfo? info = GetModuleForAddress(dt, address);
            if (info?.FileName is null)
                return -1;

            string? foundFile = FindFileOnDisk(info.FileName);
            if (foundFile is null)
                return -1;

            using FileStream fs = File.OpenRead(foundFile);
            using PEReader peReader = new(fs);

            int sizeOfHeaders = peReader.PEHeaders.PEHeader?.SizeOfHeaders ?? 0;
            PEMemoryBlock wholeImage = default;
            bool wholeImageLoaded = false;

            int filled = bytesRead;
            ulong current = address + (ulong)bytesRead;
            while (filled < buffer.Length)
            {
                int rva = (int)(current - info.ImageBase);
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
                    // the raw file image. Needed to read a module's PE/COR headers to locate ECMA
                    // metadata when the headers aren't captured in the dump.
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
        catch
        {
            return -1;
        }
    }

    private static ModuleInfo? GetModuleForAddress(DataTarget dt, ulong address)
    {
        foreach (ModuleInfo module in dt.DataReader.EnumerateModules())
        {
            if (address >= module.ImageBase && address < module.ImageBase + (ulong)module.ImageSize)
                return module;
        }

        return null;
    }

    private static string? FindFileOnDisk(string modulePath)
    {
        // For local runs the path recorded in the dump usually still exists on disk.
        if (File.Exists(modulePath))
            return modulePath;

        // Otherwise look next to the executing tool (module file copied alongside).
        int lastSep = Math.Max(modulePath.LastIndexOf('/'), modulePath.LastIndexOf('\\'));
        string fileName = lastSep >= 0 ? modulePath[(lastSep + 1)..] : modulePath;
        string candidate = Path.Combine(AppContext.BaseDirectory, fileName);
        return File.Exists(candidate) ? candidate : null;
    }
}
