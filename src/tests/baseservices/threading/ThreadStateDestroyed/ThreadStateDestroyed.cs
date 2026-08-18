// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

// A native library runs managed code on an OS thread, then runs managed code again from a
// thread-destruction callback that fires after the runtime has already torn down its per-thread
// state. The runtime should fail fast instead of attaching another managed thread.

public static unsafe class ThreadStateDestroyed
{
    private const string NativeLib = "ThreadStateDestroyedNative";

    [DllImport(NativeLib)]
    private static extern void RunCallbackOnThreadAndDuringItsDestruction(delegate* unmanaged<void> callback);

    private static int s_callbackCount;
    private static Thread s_firstThread;
    private static bool s_secondCallbackGotNewThread;

    [UnmanagedCallersOnly]
    private static void Callback()
    {
        int count = Interlocked.Increment(ref s_callbackCount);
        Thread current = Thread.CurrentThread;

        if (count == 1)
        {
            s_firstThread = current;
            Console.WriteLine("[managed] callback #1 ran; the runtime attached a Thread to the OS thread.");
        }
        else
        {
            // Managed thread ids can be recycled. We need to check that it's actually a new Thread object.
            s_secondCallbackGotNewThread = !ReferenceEquals(current, s_firstThread);
            Console.WriteLine($"[managed] callback #{count} ran; attached to a new Thread: {s_secondCallbackGotNewThread}.");
        }
    }

    public static int Main()
    {
        RunCallbackOnThreadAndDuringItsDestruction(&Callback);

        // Only reachable when the runtime did not fail fast.
        if (s_callbackCount != 2)
        {
            Console.WriteLine($"[managed] Expected exactly 2 callbacks but got {s_callbackCount}.");
            return 102;
        }

        if (!s_secondCallbackGotNewThread)
        {
            Console.WriteLine("[managed] The second callback reused the existing Thread.");
            return 103;
        }

        Console.WriteLine("[managed] FAIL: the runtime attached a second Thread instead of failing fast.");
        return 101;
    }
}
