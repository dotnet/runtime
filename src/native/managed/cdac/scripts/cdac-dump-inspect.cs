// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Usage:
//   dotnet run -- descriptor <dump-path>   Print the raw contract descriptor
//   dotnet run -- threads <dump-path>      List managed threads
//   dotnet run -- stacks <dump-path>       Print managed stack traces

using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;

if (args.Length < 2)
{
    Console.WriteLine("Usage: cdac-dump-inspect <command> <dump-path>");
    Console.WriteLine("Commands:");
    Console.WriteLine("  descriptor   Print the raw contract descriptor (contracts, types, globals)");
    Console.WriteLine("  threads      List managed threads");
    Console.WriteLine("  stacks       Print managed stack traces for all threads");
    return 1;
}

string command = args[0];
string dumpPath = args[1];

if (!File.Exists(dumpPath))
{
    Console.Error.WriteLine($"Dump not found: {dumpPath}");
    return 1;
}

try
{
    switch (command)
    {
        case "descriptor":
            DumpDescriptor(dumpPath);
            break;
        case "threads":
            DumpThreads(dumpPath);
            break;
        case "stacks":
            DumpStacks(dumpPath);
            break;
        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            return 1;
    }
}
catch (System.Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

return 0;

// ---------------------------------------------------------------------------

static ulong FindContractDescriptor(DataTarget dt)
{
    foreach (ModuleInfo module in dt.DataReader.EnumerateModules())
    {
        string? fileName = module.FileName;
        if (fileName is null)
            continue;

        int lastSep = Math.Max(fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));
        string name = lastSep >= 0 ? fileName[(lastSep + 1)..] : fileName;
        if (!name.Contains("coreclr", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("dac", StringComparison.OrdinalIgnoreCase))
            continue;

        ulong addr = module.GetExportSymbolAddress("DotNetRuntimeContractDescriptor");
        if (addr != 0)
        {
            if (dt.DataReader.PointerSize == 4)
                addr &= 0xFFFF_FFFF;
            return addr;
        }
    }

    throw new InvalidOperationException("Could not find DotNetRuntimeContractDescriptor export.");
}

static ContractDescriptorTarget CreateCdacTarget(DataTarget dt)
{
    ulong contractAddr = FindContractDescriptor(dt);

    if (!ContractDescriptorTarget.TryCreate(
            contractAddr,
            dt.DataReader.Read,
            (ulong address, Span<byte> buffer) => throw new NotSupportedException("Read-only dump."),
            (uint threadId, uint contextFlags, Span<byte> buffer) =>
                dt.DataReader.GetThreadContext(threadId, contextFlags, buffer) ? 0 : -1,
            [CoreCLRContracts.Register],
            out ContractDescriptorTarget? target))
    {
        throw new InvalidOperationException("Failed to create cDAC target.");
    }

    return target;
}

static void DumpDescriptor(string dumpPath)
{
    using DataTarget dt = DataTarget.LoadDump(dumpPath);
    ulong contractAddr = FindContractDescriptor(dt);
    int ptrSize = dt.DataReader.PointerSize;

    Console.WriteLine($"Dump: {dumpPath}");
    Console.WriteLine($"Pointer size: {ptrSize}");
    Console.WriteLine($"Contract descriptor at: 0x{contractAddr:x}");

    ulong addr = contractAddr;

    byte[] magic = new byte[8];
    dt.DataReader.Read(addr, magic);
    Console.WriteLine($"Magic: {System.Text.Encoding.ASCII.GetString(magic).TrimEnd('\0')}");
    addr += 8;

    Span<byte> buf4 = stackalloc byte[4];
    dt.DataReader.Read(addr, buf4);
    uint flags = BitConverter.ToUInt32(buf4);
    int targetPtrSize = (flags & 0x2) == 0 ? 8 : 4;
    Console.WriteLine($"Flags: 0x{flags:x} (target pointer size: {targetPtrSize})");
    addr += 4;

    dt.DataReader.Read(addr, buf4);
    uint descriptorSize = BitConverter.ToUInt32(buf4);
    if (descriptorSize > 10 * 1024 * 1024)
    {
        Console.Error.WriteLine($"Descriptor size {descriptorSize} exceeds 10MB limit. Dump may be corrupted.");
        return;
    }
    addr += 4;

    Span<byte> bufPtr = stackalloc byte[ptrSize];
    dt.DataReader.Read(addr, bufPtr);
    ulong descriptorPtr = ptrSize == 8 ? BitConverter.ToUInt64(bufPtr) : BitConverter.ToUInt32(bufPtr);

    byte[] jsonBytes = new byte[descriptorSize];
    int jsonRead = dt.DataReader.Read(descriptorPtr, jsonBytes);

    var descriptor = ContractDescriptorParser.ParseCompact(jsonBytes.AsSpan(0, jsonRead));
    if (descriptor is null)
    {
        Console.WriteLine("Failed to parse contract descriptor JSON.");
        return;
    }

    Console.WriteLine($"Version: {descriptor.Version}");
    Console.WriteLine($"Baseline: {descriptor.Baseline}");

    if (descriptor.Contracts is { Count: > 0 })
    {
        Console.WriteLine($"\nContracts ({descriptor.Contracts.Count}):");
        foreach (var kvp in descriptor.Contracts.OrderBy(c => c.Key))
            Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
    }

    if (descriptor.Types is { Count: > 0 })
    {
        Console.WriteLine($"\nTypes ({descriptor.Types.Count}):");
        foreach (var kvp in descriptor.Types.OrderBy(t => t.Key))
        {
            string size = kvp.Value.Size is not null ? $" (size: {kvp.Value.Size})" : "";
            int fieldCount = kvp.Value.Fields?.Count ?? 0;
            Console.WriteLine($"  {kvp.Key}{size} [{fieldCount} fields]");
            if (kvp.Value.Fields is not null)
            {
                foreach (var field in kvp.Value.Fields.OrderBy(f => f.Key))
                    Console.WriteLine($"    {field.Key}: offset={field.Value.Offset}, type={field.Value.Type ?? "?"}");
            }
        }
    }

    if (descriptor.Globals is { Count: > 0 })
    {
        Console.WriteLine($"\nGlobals ({descriptor.Globals.Count}):");
        foreach (var kvp in descriptor.Globals.OrderBy(g => g.Key))
        {
            var g = kvp.Value;
            string val = g.NumericValue.HasValue ? $"0x{g.NumericValue.Value:x}" : g.StringValue ?? "?";
            string prefix = g.Type is not null ? $"type={g.Type}" : $"indirect={g.Indirect}";
            Console.WriteLine($"  {kvp.Key}: {prefix}, value={val}");
        }
    }
}

static void DumpThreads(string dumpPath)
{
    using DataTarget dt = DataTarget.LoadDump(dumpPath);
    var cdac = CreateCdacTarget(dt);

    Console.WriteLine($"Dump: {dumpPath}\n");

    IThread threadContract = cdac.Contracts.GetContract<IThread>();
    ThreadStoreData storeData = threadContract.GetThreadStoreData();
    Console.WriteLine($"Thread count: {storeData.ThreadCount}\n");

    int idx = 0;
    HashSet<ulong> visited = [];
    TargetPointer threadAddr = storeData.FirstThread;
    while (threadAddr != TargetPointer.Null)
    {
        if (!visited.Add(threadAddr.Value))
        {
            Console.WriteLine($"Cycle detected in thread list at {threadAddr}");
            break;
        }
        try
        {
            ThreadData td = threadContract.GetThreadData(threadAddr);
            Console.WriteLine($"Thread {idx}: OS ID=0x{td.OSId:x}, State=0x{(uint)td.State:x}, Addr={threadAddr}");
            threadAddr = td.NextThread;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Thread {idx}: Error reading at {threadAddr} - {ex.Message}");
            break;
        }
        idx++;
    }
}

static void DumpStacks(string dumpPath)
{
    using DataTarget dt = DataTarget.LoadDump(dumpPath);
    var cdac = CreateCdacTarget(dt);

    Console.WriteLine($"Dump: {dumpPath}\n");

    IThread threadContract = cdac.Contracts.GetContract<IThread>();
    IStackWalk stackWalk = cdac.Contracts.GetContract<IStackWalk>();
    IRuntimeTypeSystem rts = cdac.Contracts.GetContract<IRuntimeTypeSystem>();
    ThreadStoreData storeData = threadContract.GetThreadStoreData();

    int idx = 0;
    HashSet<ulong> visited = [];
    TargetPointer threadAddr = storeData.FirstThread;
    while (threadAddr != TargetPointer.Null)
    {
        if (!visited.Add(threadAddr.Value))
        {
            Console.WriteLine($"Cycle detected in thread list at {threadAddr}");
            break;
        }

        ThreadData td;
        try
        {
            td = threadContract.GetThreadData(threadAddr);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Thread {idx} ({threadAddr}): Error - {ex.Message}\n");
            break;
        }

        Console.WriteLine($"Thread {idx} (OS ID: 0x{td.OSId:x}):");

        try
        {
            foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(td))
            {
                try
                {
                    TargetPointer ip = stackWalk.GetInstructionPointer(frame);
                    TargetPointer mdPtr = stackWalk.GetMethodDescPtr(frame);
                    string frameName;

                    if (mdPtr != TargetPointer.Null)
                    {
                        try
                        {
                            MethodDescHandle mdHandle = rts.GetMethodDescHandle(mdPtr);
                            if (rts.IsNoMetadataMethod(mdHandle, out string methodName))
                            {
                                frameName = methodName;
                            }
                            else
                            {
                                TargetPointer mt = rts.GetMethodTable(mdHandle);
                                frameName = $"MD@0x{mdPtr.Value:x} (MT: 0x{mt.Value:x})";
                            }
                        }
                        catch
                        {
                            frameName = $"MethodDesc@0x{mdPtr.Value:x}";
                        }
                    }
                    else
                    {
                        TargetPointer frameAddr = stackWalk.GetFrameAddress(frame);
                        if (frameAddr != TargetPointer.Null)
                        {
                            try { frameName = $"[{stackWalk.GetFrameName(frameAddr)}]"; }
                            catch { frameName = $"[InternalFrame@0x{frameAddr.Value:x}]"; }
                        }
                        else
                        {
                            frameName = "[Native Frame]";
                        }
                    }

                    Console.WriteLine($"  0x{ip.Value:x16} {frameName}");
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine($"  <frame error: {ex.Message}>");
                }
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"  Stack walk failed: {ex.Message}");
        }

        Console.WriteLine();
        threadAddr = td.NextThread;
        idx++;
    }
}
