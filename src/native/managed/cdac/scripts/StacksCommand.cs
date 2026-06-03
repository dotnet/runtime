// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.Runtime;

using Exception = System.Exception;

namespace Microsoft.DotNet.Diagnostics.CdacDumpInspect;

internal sealed class StacksCommand : Command
{
    private readonly Argument<string> _dumpPath = new("dump-path") { Description = "Path to a .NET crash dump" };

    public StacksCommand() : base("stacks", "Print managed stack traces for all threads")
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
            catch (Exception ex)
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
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  <frame error: {ex.Message}>");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Stack walk failed: {ex.Message}");
            }

            Console.WriteLine();
            threadAddr = td.NextThread;
            idx++;
        }
    }
}
