// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
    private static readonly EventWaitHandle s_gcThreadReady = new(false, EventResetMode.ManualReset);
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
        s_gcThreadReady.WaitOne();

        Environment.SetEnvironmentVariable(GCThreadIdVariable, Volatile.Read(ref s_gcThreadId).ToString());
        Environment.SetEnvironmentVariable(MapRWAttemptEventVariable, s_mapRWAttemptEventName);
        Environment.SetEnvironmentVariable(StartGCEventVariable, s_startGCEventName);
        Environment.SetEnvironmentVariable(FatalErrorThreadIdVariable, GetCurrentThreadId().ToString());

        RunDynamicMethod();
        return 1;
    }

    private static void RunGC()
    {
        Volatile.Write(ref s_gcThreadId, GetCurrentThreadId());
        IntPtr startGCHandle = s_startGC.SafeWaitHandle.DangerousGetHandle();
        IntPtr readyHandle = s_gcThreadReady.SafeWaitHandle.DangerousGetHandle();
        int maxGeneration = GC.MaxGeneration;

        // Avoid JITting managed wait helpers while the other thread holds the code-allocation lock.
        if (!SetEvent(startGCHandle) || WaitForSingleObject(startGCHandle, 0) != 0)
        {
            throw new InvalidOperationException("Failed to warm the native event operations.");
        }
        s_startGC.Reset();
        // Prepare the collection wrappers without performing the full GC needed by the test.
        foreach (MethodInfo method in typeof(GC).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.Name is nameof(GC.Collect) or "_Collect")
            {
                RuntimeHelpers.PrepareMethod(method.MethodHandle);
            }
        }

        if (!SetEvent(readyHandle) || WaitForSingleObject(startGCHandle, uint.MaxValue) != 0)
        {
            throw new InvalidOperationException("Failed to synchronize the GC thread.");
        }

        GC.Collect(maxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
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

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetEvent(IntPtr handle);

    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
}
