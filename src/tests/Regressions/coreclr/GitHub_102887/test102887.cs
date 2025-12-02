// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public class Test102887
{
    delegate void DispatchQueueWork(IntPtr args);
    [DllImport("nativetest102887")]
    private static extern void StartDispatchQueueThread(DispatchQueueWork start);

    [DllImport("nativetest102887")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SupportsSendingSignalsToDispatchQueueThread();

    static volatile int s_cnt;
    static ManualResetEvent s_workStarted = new ManualResetEvent(false);

    private static void RunOnDispatchQueueThread(IntPtr args)
    {
        s_workStarted.Set();
        while (true)
        {
            s_cnt++;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // Skip the test if the current OS doesn't support sending signals to the dispatch queue threads
        if (SupportsSendingSignalsToDispatchQueueThread())
        {
            Console.WriteLine("Sending signals to dispatch queue thread is supported, testing it now");
            StartDispatchQueueThread(RunOnDispatchQueueThread);
            s_workStarted.WaitOne();

            for (int i = 0; i < 100; i++)
            {
                GC.Collect(2);
            }
        }
    }
}

