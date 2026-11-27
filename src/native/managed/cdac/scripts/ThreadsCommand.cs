// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.Runtime;

using Exception = System.Exception;

namespace Microsoft.DotNet.Diagnostics.CdacDumpInspect;

internal sealed class ThreadsCommand : Command
{
    private readonly Argument<string> _dumpPath = new("dump-path") { Description = "Path to a .NET crash dump" };

    public ThreadsCommand() : base("threads", "List managed threads")
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
        var cdac = DumpHelpers.CreateCdacTarget(dt);

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
            catch (Exception ex)
            {
                Console.WriteLine($"Thread {idx}: Error reading at {threadAddr} - {ex.Message}");
                break;
            }
            idx++;
        }
    }
}
