// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.DotNet.Diagnostics.CdacDumpInspect;

internal static class DumpHelpers
{
    private static readonly string[] s_coreClrModuleNames = ["coreclr.dll", "libcoreclr.so", "libcoreclr.dylib"];

    public static ulong FindContractDescriptor(DataTarget dt, out ulong runtimeImageBase)
    {
        runtimeImageBase = 0;
        // Prefer CoreCLR modules, retaining the first other export for NativeAOT application images.
        ulong fallback = 0;
        ulong fallbackImageBase = 0;
        foreach (ModuleInfo module in dt.DataReader.EnumerateModules())
        {
            ulong addr = module.GetExportSymbolAddress("DotNetRuntimeContractDescriptor");
            if (addr == 0)
                continue;

            ulong imageBase = module.ImageBase;
            if (dt.DataReader.PointerSize == 4)
            {
                addr &= 0xFFFF_FFFF;
                imageBase &= 0xFFFF_FFFF;
            }

            string? fileName = module.FileName;
            if (fileName is not null)
            {
                int lastSep = Math.Max(fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));
                string name = lastSep >= 0 ? fileName[(lastSep + 1)..] : fileName;
                if (s_coreClrModuleNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    runtimeImageBase = imageBase;
                    return addr;
                }
            }

            if (fallback == 0)
            {
                fallback = addr;
                fallbackImageBase = imageBase;
            }
        }

        if (fallback != 0)
        {
            runtimeImageBase = fallbackImageBase;
            return fallback;
        }

        throw new InvalidOperationException("Could not find DotNetRuntimeContractDescriptor export.");
    }

    public static ContractDescriptorTarget CreateCdacTarget(DataTarget dt)
    {
        ulong contractAddr = FindContractDescriptor(dt, out ulong runtimeImageBase);

        return ContractDescriptorTarget.Create(
            contractAddr,
            (ulong address, Span<byte> buffer) => dt.DataReader.Read(address, buffer) == buffer.Length ? 0 : -1,
            (ulong address, Span<byte> buffer) => -1,
            (uint threadId, uint contextFlags, Span<byte> buffer) =>
                dt.DataReader.GetThreadContext(threadId, contextFlags, buffer) ? 0 : -1,
            (uint threadId, ReadOnlySpan<byte> context) => -1,
            (ulong _, out ulong _) => throw new NotImplementedException("Scripts do not provide AllocVirtual"),
            [CoreCLRContracts.Register],
            runtimeImageBase);
    }
}
