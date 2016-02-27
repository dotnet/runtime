// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Threading;

public class FinalizeTimeout
{
    public static int Main(string[] args)
    {
        Console.WriteLine("Main start");

        // Run the finalizer at least once to have its code be jitted
        BlockingFinalizerOnShutdown finalizableObject;
        do
        {
            finalizableObject = new BlockingFinalizerOnShutdown();
        } while (!BlockingFinalizerOnShutdown.finalizerCompletedOnce);

        // Start a bunch of threads that allocate continuously, to increase the chance that when Main returns, one of the
        // threads will be blocked for shutdown while holding one of the GC locks
        for (int i = 0; i < Environment.ProcessorCount; ++i)
        {
            var t = new Thread(ThreadMain);
            t.IsBackground = true;
            t.Start();
        }

        // Wait a second to give the threads a chance to actually start running
        Thread.Sleep(1000);

        Console.WriteLine("Main end");

        // Create another finalizable object, and immediately return from Main to have finalization occur during shutdown
        finalizableObject = new BlockingFinalizerOnShutdown() { isLastObject = true };
        return 100;
    }

    private static void ThreadMain()
    {
        byte[] b;
        while (true)
            b = new byte[1024];
    }

    private class BlockingFinalizerOnShutdown
    {
        public static bool finalizerCompletedOnce = false;
        public bool isLastObject = false;

        ~BlockingFinalizerOnShutdown()
        {
            if (finalizerCompletedOnce && !isLastObject)
                return;

            Console.WriteLine("Finalizer start");

            // Allocate in the finalizer for long enough to try allocation after one of the background threads blocks for
            // shutdown while holding one of the GC locks, to deadlock the finalizer. The main thread should eventually time
            // out waiting for the finalizer thread to complete, and the process should exit cleanly.
            TimeSpan timeout = isLastObject ? TimeSpan.FromMilliseconds(500) : TimeSpan.Zero;
            TimeSpan elapsed = TimeSpan.Zero;
            var start = DateTime.Now;
            int i = -1;
            object o;
            do
            {
                o = new object();
            } while ((++i & 0xff) != 0 || (elapsed = DateTime.Now - start) < timeout);

            Console.WriteLine("Finalizer end");
            finalizerCompletedOnce = true;
        }
    }
}
