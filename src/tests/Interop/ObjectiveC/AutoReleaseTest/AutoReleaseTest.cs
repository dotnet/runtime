// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

internal static unsafe class ObjectiveC
{
    [DllImport(nameof(ObjectiveC))]
    public static extern IntPtr initObject();
    [DllImport(nameof(ObjectiveC))]
    public static extern void autoreleaseObject(IntPtr art);
    [DllImport(nameof(ObjectiveC))]
    public static extern int getNumReleaseCalls();
}

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class AutoReleaseTest
{
    [Fact]
    public static int TestEntryPoint()
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

            Assert.Equal(numReleaseCalls + 1, ObjectiveC.getNumReleaseCalls());
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
            Assert.Equal(numReleaseCalls + 1, ObjectiveC.getNumReleaseCalls());
        }
    }
}
