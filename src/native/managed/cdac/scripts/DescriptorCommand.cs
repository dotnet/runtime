// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.DotNet.Diagnostics.CdacDumpInspect;

internal sealed class DescriptorCommand : Command
{
    private readonly Argument<string> _dumpPath = new("dump-path") { Description = "Path to a .NET crash dump" };

    public DescriptorCommand() : base("descriptor", "Print the raw contract descriptor (contracts, types, globals)")
    {
        Add(_dumpPath);
        SetAction(Run);
    }

    private int Run(ParseResult parse)
    {
        string dumpPath = parse.GetValue(_dumpPath)!;
        if (!File.Exists(dumpPath))
        {
            Console.Error.WriteLine($"Dump not found: {dumpPath}");
            return 1;
        }

        try
        {
            Execute(dumpPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }

        return 0;
    }

    private static void Execute(string dumpPath)
    {
        using DataTarget dt = DataTarget.LoadDump(dumpPath);
        ulong contractAddr = DumpHelpers.FindContractDescriptor(dt);

        Console.WriteLine($"Dump: {dumpPath}");
        Console.WriteLine($"Pointer size: {dt.DataReader.PointerSize}");

        List<ParsedDescriptor> descriptors = [];
        HashSet<ulong> visited = [];
        CollectDescriptors(dt, contractAddr, "Main", descriptors, visited);

        if (descriptors.Count == 0)
        {
            Console.Error.WriteLine("No descriptors found.");
            return;
        }

        foreach (ParsedDescriptor pd in descriptors)
        {
            Console.WriteLine();
            Console.WriteLine($"=== {pd.Name} Descriptor (0x{pd.Address:x}) ===");
            Console.WriteLine($"Version: {pd.Descriptor.Version}");
            Console.WriteLine($"Baseline: {pd.Descriptor.Baseline}");
            PrintDescriptorContents(pd);
        }

        if (descriptors.Count > 1)
            PrintMergeConflicts(descriptors);
    }

    private static void PrintDescriptorContents(ParsedDescriptor pd)
    {
        var descriptor = pd.Descriptor;

        if (descriptor.Contracts is { Count: > 0 })
        {
            Console.WriteLine($"\n  Contracts ({descriptor.Contracts.Count}):");
            foreach (var kvp in descriptor.Contracts.OrderBy(c => c.Key))
                Console.WriteLine($"    {kvp.Key} = {kvp.Value}");
        }

        if (descriptor.Types is { Count: > 0 })
        {
            Console.WriteLine($"\n  Types ({descriptor.Types.Count}):");
            foreach (var kvp in descriptor.Types.OrderBy(t => t.Key))
            {
                string size = kvp.Value.Size is not null ? $" (size: {kvp.Value.Size})" : "";
                int fieldCount = kvp.Value.Fields?.Count ?? 0;
                Console.WriteLine($"    {kvp.Key}{size} [{fieldCount} fields]");
                if (kvp.Value.Fields is not null)
                {
                    foreach (var field in kvp.Value.Fields.OrderBy(f => f.Key))
                        Console.WriteLine($"      {field.Key}: offset={field.Value.Offset}, type={field.Value.Type ?? "?"}");
                }
            }
        }

        if (descriptor.Globals is { Count: > 0 })
        {
            Console.WriteLine($"\n  Globals ({descriptor.Globals.Count}):");
            foreach (var kvp in descriptor.Globals.OrderBy(g => g.Key))
                PrintGlobalEntry(kvp.Key, kvp.Value, pd.PointerData);
        }

        if (descriptor.SubDescriptors is { Count: > 0 })
        {
            Console.WriteLine($"\n  Sub-descriptor references ({descriptor.SubDescriptors.Count}):");
            foreach (var kvp in descriptor.SubDescriptors.OrderBy(s => s.Key))
                PrintGlobalEntry(kvp.Key, kvp.Value, pd.PointerData);
        }
    }

    private static void PrintGlobalEntry(string name, ContractDescriptorParser.GlobalDescriptor g, ulong[] pointerData)
    {
        string val = ResolveGlobalValue(g, pointerData);
        string prefix = g.Type is not null ? $"type={g.Type}, " : "";
        Console.WriteLine($"    {name}: {prefix}indirect={g.Indirect}, value={val}");
    }

    private static string ResolveGlobalValue(ContractDescriptorParser.GlobalDescriptor g, ulong[] pointerData)
    {
        if (g.Indirect && g.NumericValue.HasValue)
        {
            ulong index = g.NumericValue.Value;
            if (index < (ulong)pointerData.Length)
                return $"0x{pointerData[index]:x} (pointer_data[{index}])";

            return $"invalid index {index}";
        }

        return g.NumericValue.HasValue ? $"0x{g.NumericValue.Value:x}" : g.StringValue ?? "?";
    }

    private static void CollectDescriptors(DataTarget dt, ulong contractAddr, string name,
        List<ParsedDescriptor> results, HashSet<ulong> visited)
    {
        if (!visited.Add(contractAddr))
        {
            Console.Error.WriteLine($"Warning: cycle detected at 0x{contractAddr:x}, skipping.");
            return;
        }

        if (!TryReadDescriptor(dt, contractAddr, out var descriptor, out ulong[] pointerData))
        {
            Console.Error.WriteLine($"Warning: failed to read descriptor at 0x{contractAddr:x}.");
            return;
        }

        results.Add(new ParsedDescriptor(name, contractAddr, descriptor, pointerData));

        if (descriptor.SubDescriptors is null)
            return;

        int ptrSize = dt.DataReader.PointerSize;
        Span<byte> bufPtr = stackalloc byte[ptrSize];

        foreach (var kvp in descriptor.SubDescriptors)
        {
            if (!kvp.Value.Indirect || !kvp.Value.NumericValue.HasValue)
                continue;

            ulong index = kvp.Value.NumericValue.Value;
            if (index >= (ulong)pointerData.Length)
                continue;

            ulong subDescPtrAddr = pointerData[index];
            if (subDescPtrAddr == 0)
                continue;

            if (dt.DataReader.Read(subDescPtrAddr, bufPtr) != bufPtr.Length)
                continue;

            ulong subDescAddr = ptrSize == 8 ? BitConverter.ToUInt64(bufPtr) : BitConverter.ToUInt32(bufPtr);
            if (subDescAddr == 0)
            {
                Console.Error.WriteLine($"Note: Sub-descriptor '{kvp.Key}' pointer is null (not populated at crash time).");
                continue;
            }

            CollectDescriptors(dt, subDescAddr, kvp.Key, results, visited);
        }
    }

    private static bool TryReadDescriptor(DataTarget dt, ulong contractAddr,
        out ContractDescriptorParser.ContractDescriptor descriptor, out ulong[] pointerData)
    {
        descriptor = null!;
        pointerData = [];

        int ptrSize = dt.DataReader.PointerSize;
        ulong addr = contractAddr;

        // Magic
        byte[] magic = new byte[8];
        if (dt.DataReader.Read(addr, magic) != magic.Length)
            return false;
        ReadOnlySpan<byte> magicLE = "DNCCDAC\0"u8;
        ReadOnlySpan<byte> magicBE = "\0CADCCND"u8;
        if (!magic.AsSpan().SequenceEqual(magicLE) && !magic.AsSpan().SequenceEqual(magicBE))
            return false;
        addr += 8;

        // Flags
        Span<byte> buf4 = stackalloc byte[4];
        if (dt.DataReader.Read(addr, buf4) != buf4.Length)
            return false;
        addr += 4;

        // Descriptor size
        if (dt.DataReader.Read(addr, buf4) != buf4.Length)
            return false;
        uint descriptorSize = BitConverter.ToUInt32(buf4);
        if (descriptorSize > 10 * 1024 * 1024)
            return false;
        addr += 4;

        // Descriptor pointer
        Span<byte> bufPtr = stackalloc byte[ptrSize];
        if (dt.DataReader.Read(addr, bufPtr) != bufPtr.Length)
            return false;
        ulong descriptorPtr = ptrSize == 8 ? BitConverter.ToUInt64(bufPtr) : BitConverter.ToUInt32(bufPtr);
        if (descriptorPtr == 0)
            return false;
        addr += (ulong)ptrSize;

        // Pointer data count
        if (dt.DataReader.Read(addr, buf4) != buf4.Length)
            return false;
        uint pointerDataCount = BitConverter.ToUInt32(buf4);
        if (pointerDataCount > 1024)
            return false;
        addr += 4;

        // Padding
        addr += 4;

        // Pointer data array address
        if (dt.DataReader.Read(addr, bufPtr) != bufPtr.Length)
            return false;
        ulong pointerDataAddr = ptrSize == 8 ? BitConverter.ToUInt64(bufPtr) : BitConverter.ToUInt32(bufPtr);
        if (pointerDataCount > 0 && pointerDataAddr == 0)
            return false;

        // Read pointer data entries
        pointerData = new ulong[pointerDataCount];
        for (uint i = 0; i < pointerDataCount; i++)
        {
            if (dt.DataReader.Read(pointerDataAddr + (ulong)i * (ulong)ptrSize, bufPtr) != bufPtr.Length)
                return false;
            pointerData[i] = ptrSize == 8 ? BitConverter.ToUInt64(bufPtr) : BitConverter.ToUInt32(bufPtr);
        }

        // Read and parse JSON
        byte[] jsonBytes = new byte[descriptorSize];
        int jsonRead = dt.DataReader.Read(descriptorPtr, jsonBytes);
        if (jsonRead != (int)descriptorSize)
            return false;

        try
        {
            descriptor = ContractDescriptorParser.ParseCompact(jsonBytes)!;
        }
        catch
        {
            return false;
        }

        return descriptor is not null;
    }

    private static void PrintMergeConflicts(List<ParsedDescriptor> descriptors)
    {
        List<string> conflicts = [];

        // Check duplicate contracts
        var contractSources = new Dictionary<string, List<(string Source, string Version)>>();
        foreach (ParsedDescriptor pd in descriptors)
        {
            foreach (var kvp in pd.Descriptor.Contracts ?? [])
            {
                if (!contractSources.TryGetValue(kvp.Key, out var list))
                {
                    list = [];
                    contractSources[kvp.Key] = list;
                }
                list.Add((pd.Name, kvp.Value));
            }
        }
        foreach (var kvp in contractSources.Where(c => c.Value.Count > 1).OrderBy(c => c.Key))
        {
            string details = string.Join(", ", kvp.Value.Select(v => $"{v.Source}={v.Version}"));
            conflicts.Add($"  Contract '{kvp.Key}': {details}");
        }

        // Check duplicate types
        var typeSources = new Dictionary<string, List<string>>();
        foreach (ParsedDescriptor pd in descriptors)
        {
            foreach (var kvp in pd.Descriptor.Types ?? [])
            {
                if (!typeSources.TryGetValue(kvp.Key, out var list))
                {
                    list = [];
                    typeSources[kvp.Key] = list;
                }
                list.Add(pd.Name);
            }
        }
        foreach (var kvp in typeSources.Where(t => t.Value.Count > 1).OrderBy(t => t.Key))
        {
            conflicts.Add($"  Type '{kvp.Key}': defined in {string.Join(", ", kvp.Value)}");
        }

        // Check duplicate globals
        var globalSources = new Dictionary<string, List<(string Source, string Value)>>();
        foreach (ParsedDescriptor pd in descriptors)
        {
            foreach (var kvp in pd.Descriptor.Globals ?? [])
            {
                if (!globalSources.TryGetValue(kvp.Key, out var list))
                {
                    list = [];
                    globalSources[kvp.Key] = list;
                }
                string val = ResolveGlobalValue(kvp.Value, pd.PointerData);
                list.Add((pd.Name, val));
            }
        }
        foreach (var kvp in globalSources.Where(g => g.Value.Count > 1).OrderBy(g => g.Key))
        {
            string details = string.Join(", ", kvp.Value.Select(v => $"{v.Source}={v.Value}"));
            conflicts.Add($"  Global '{kvp.Key}': {details}");
        }

        if (conflicts.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"=== Merge Conflicts ({conflicts.Count}) ===");
            foreach (string conflict in conflicts)
                Console.WriteLine(conflict);
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("=== No Merge Conflicts ===");
        }
    }
}
