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

    private volatile static int _runningThreads;

    public static int Main()
    {
        Thread[] threadMap = new Thread[ThreadCount];
        for (int threadIndex = 0; threadIndex < ThreadCount; threadIndex++)
        {
            Thread thread = new Thread(new ThreadStart(CrashInParallel));
            threadMap[threadIndex] = thread;
            thread.Start();
        }
        for (;;)
        {
            Thread.Sleep(1000);
        }
        return 0;
    }

    private static void CrashInParallel()
    {
        int threadIndex = ++_runningThreads;
        string failFastMessage = string.Format("Parallel crash in thread {0}!\n", threadIndex);
        while (_runningThreads != ThreadCount)
        {
        }
        // Now all the worker threads should be running, fire!
        Environment.FailFast(failFastMessage);
    }
}