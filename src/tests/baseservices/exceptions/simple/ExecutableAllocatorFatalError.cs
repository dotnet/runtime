// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;

public static class ExecutableAllocatorFatalError
{
    private const string FatalErrorThreadIdVariable = "DOTNET_ExecutableAllocatorFatalErrorThreadId";
    private const string GCThreadIdVariable = "DOTNET_ExecutableAllocatorGCThreadId";
    private const string MapRWAttemptEventVariable = "DOTNET_ExecutableAllocatorMapRWAttemptEvent";
    private const string StartGCEventVariable = "DOTNET_ExecutableAllocatorStartGCEvent";

    private static readonly string s_mapRWAttemptEventName = $"ExecutableAllocatorMapRWAttempt-{Environment.ProcessId}";
    private static readonly string s_startGCEventName = $"ExecutableAllocatorStartGC-{Environment.ProcessId}";
    private static readonly ManualResetEventSlim s_gcThreadReady = new();
    private static readonly EventWaitHandle s_mapRWAttempt = new(false, EventResetMode.ManualReset, s_mapRWAttemptEventName);
    private static readonly EventWaitHandle s_startGC = new(false, EventResetMode.ManualReset, s_startGCEventName);
    private static uint s_gcThreadId;

    public static int Main()
    {
        RunDynamicMethod();

        var gcThread = new Thread(RunGC)
        {
            IsBackground = true
        };
        gcThread.Start();
        s_gcThreadReady.Wait();

        Environment.SetEnvironmentVariable(FatalErrorThreadIdVariable, GetCurrentThreadId().ToString());
        Environment.SetEnvironmentVariable(GCThreadIdVariable, Volatile.Read(ref s_gcThreadId).ToString());
        Environment.SetEnvironmentVariable(MapRWAttemptEventVariable, s_mapRWAttemptEventName);
        Environment.SetEnvironmentVariable(StartGCEventVariable, s_startGCEventName);

        RunDynamicMethod();
        return 1;
    }

    private static void RunGC()
    {
        Volatile.Write(ref s_gcThreadId, GetCurrentThreadId());
        s_gcThreadReady.Set();
        s_startGC.WaitOne();

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static void RunDynamicMethod()
    {
        var method = new DynamicMethod("ExecutableAllocatorFatalErrorMethod", typeof(int), Type.EmptyTypes);
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ret);
        _ = ((Func<int>)method.CreateDelegate(typeof(Func<int>)))();
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
