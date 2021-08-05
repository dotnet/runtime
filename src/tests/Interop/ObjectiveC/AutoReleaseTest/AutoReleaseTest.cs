// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using TestLibrary;

internal static unsafe class ObjectiveC
{
    [DllImport(nameof(ObjectiveC))]
    public static extern IntPtr initObject();
    [DllImport(nameof(ObjectiveC))]
    public static extern void autoreleaseObject(IntPtr art);
    [DllImport(nameof(ObjectiveC))]
    public static extern int getNumReleaseCalls();
}

public class AutoReleaseTest
{
    public static int Main()
    {
        try
        {
            ValidateNewManagedThreadAutoRelease();
            ValidateThreadPoolAutoRelease();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }

    private static void ValidateNewManagedThreadAutoRelease()
    {
        Console.WriteLine($"Running {nameof(ValidateNewManagedThreadAutoRelease)}...");
        using (AutoResetEvent evt = new AutoResetEvent(false))
        {
            int numReleaseCalls = ObjectiveC.getNumReleaseCalls();

            RunScenario(evt);

            // Trigger the GC and wait to clean up the allocated managed Thread instance.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.AreEqual(numReleaseCalls + 1, ObjectiveC.getNumReleaseCalls());
        }

        static void RunScenario(AutoResetEvent evt)
        {
            IntPtr obj = ObjectiveC.initObject();
            var thread = new Thread(_ =>
            {
                ObjectiveC.autoreleaseObject(obj);
                evt.Set();
            });
            thread.Start();

            evt.WaitOne();
            thread.Join();
        }
    }

    private static void ValidateThreadPoolAutoRelease()
    {
        Console.WriteLine($"Running {nameof(ValidateThreadPoolAutoRelease)}...");
        using (AutoResetEvent evt = new AutoResetEvent(false))
        {
            int numReleaseCalls = ObjectiveC.getNumReleaseCalls();
            IntPtr obj = ObjectiveC.initObject();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                ObjectiveC.autoreleaseObject(obj);
                evt.Set();
            });
            evt.WaitOne();
            // Wait 60 ms after the signal to ensure that the thread has finished the work item and has drained the thread's autorelease pool.
            Thread.Sleep(60);
            Assert.AreEqual(numReleaseCalls + 1, ObjectiveC.getNumReleaseCalls());
        }
    }
}
