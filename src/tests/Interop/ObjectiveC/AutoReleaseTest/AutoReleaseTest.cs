// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using TestLibrary;

internal static class ObjectiveC
{
    [DllImport(nameof(ObjectiveC))]
    internal static extern IntPtr initObject();
    [DllImport(nameof(ObjectiveC))]
    internal static extern void autoreleaseObject(IntPtr art);
    [DllImport(nameof(ObjectiveC))]
    internal static extern int getNumReleaseCalls();
}

public class AutoReleaseTest
{
    public static int Main()
    {
        AppContext.SetSwitch("System.Threading.ThreadPool.EnableDispatchAutoreleasePool", true);
        try
        {
            TestAutoRelease();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }

    private static void TestAutoRelease()
    {
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
