// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// Runtime stability in the presence of concurrent fatal errors

public class ParallelCrash
{
    private const int ThreadCount = 10;

    private volatile static int s_runningThreads;
    private static bool s_crashMainThread;
    private static bool s_crashWorkerThreads;

    public static int Main(string[] args)
    {
        s_crashMainThread = true;
        s_crashWorkerThreads = true;
        if (args.Length > 0)
        {
            s_crashMainThread = (args[0] != "2");
            s_crashWorkerThreads = (args[0] != "1");
        }
        
        for (int threadIndex = ThreadCount; --threadIndex >= 0;)
        {
            new Thread(CrashInParallel).Start();
        }
        if (s_crashMainThread)
        {
            Environment.FailFast("Parallel crash in main thread");
        }
        for (;;)
        {
            Thread.Sleep(50);
        }
        return 0;
    }

    private static void CrashInParallel()
    {
        int threadIndex = Interlocked.Increment(ref s_runningThreads);
        string failFastMessage = string.Format("Parallel crash in thread {0}!\n", threadIndex);
        while (s_runningThreads != ThreadCount)
        {
        }
        // Now all the worker threads should be running, fire!
        if (s_crashWorkerThreads)
        {
            Environment.FailFast(failFastMessage);
        }
        for (;;)
        {
            Thread.Sleep(50);
        }
    }
}