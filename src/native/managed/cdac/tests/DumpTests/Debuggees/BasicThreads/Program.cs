// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

/// <summary>
/// Debuggee app for cDAC dump tests.
/// Spawns threads with known names, ensures they are all alive, then crashes
/// so a dump is produced for analysis.
/// </summary>
internal static class Program
{
    // These constants are referenced by ThreadDumpTests to assert expected values.
    public const int SpawnedThreadCount = 5;
    public static readonly string[] ThreadNames = new[]
    {
        "cdac-test-thread-0",
        "cdac-test-thread-1",
        "cdac-test-thread-2",
        "cdac-test-thread-3",
        "cdac-test-thread-4",
    };

    private static void Main()
    {
        // Barrier ensures all threads are alive and named before we crash.
        // participantCount = SpawnedThreadCount + 1 (main thread)
        using Barrier barrier = new(SpawnedThreadCount + 1);

        Thread[] threads = new Thread[SpawnedThreadCount];
        for (int i = 0; i < SpawnedThreadCount; i++)
        {
            int index = i;
            threads[i] = new Thread(() =>
            {
                // Signal that this thread is alive and wait for all others.
                barrier.SignalAndWait();

                // Keep the thread alive until the process crashes.
                Thread.Sleep(Timeout.Infinite);
            })
            {
                Name = ThreadNames[index],
                IsBackground = true,
            };
            threads[i].Start();
        }

        // Wait until all spawned threads have reached the barrier.
        barrier.SignalAndWait();

        // All threads are alive and named. Crash to produce a dump.
        Environment.FailFast("cDAC dump test: BasicThreads debuggee intentional crash");
    }
}
