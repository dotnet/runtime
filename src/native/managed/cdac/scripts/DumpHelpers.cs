// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                (ulong address, Span<byte> buffer) => dt.DataReader.Read(address, buffer) == buffer.Length ? 0 : -1,
                (ulong address, Span<byte> buffer) => -1,
                (uint threadId, uint contextFlags, Span<byte> buffer) =>
                    dt.DataReader.GetThreadContext(threadId, contextFlags, buffer) ? 0 : -1,
                [CoreCLRContracts.Register],
                out ContractDescriptorTarget? target))
        {
            throw new InvalidOperationException("Failed to create cDAC target.");
        }

        return target;
    }
}
