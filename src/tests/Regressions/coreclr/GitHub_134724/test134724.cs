// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;
using TestLibrary;

public class Program
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(20);

    [SkipOnCoreClr("This test is not compatible with GC stress.", RuntimeTestModes.AnyGCStress)]
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void TestEntryPoint()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        using var stop = new CancellationTokenSource(TestTimeout);
        using var ready = new ManualResetEventSlim();
        Exception workerError = null;

        var worker = new Thread(() =>
        {
            try
            {
                byte[][] ring = new byte[16][];
                for (int i = 0; i < ring.Length; i++)
                {
                    ring[i] = new byte[1024 * 1024];
                }

                object[] roots = new object[262_144];
                for (int i = 0; i < roots.Length; i++)
                {
                    roots[i] = new Node(i);
                }

                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
                ready.Set();

                for (int i = 0; i < 256 && !stop.IsCancellationRequested; i++)
                {
                    ring[i % ring.Length] = new byte[1024 * 1024];
                    GC.Collect(2, GCCollectionMode.Forced, blocking: false, compacting: false);
                    Thread.Sleep(4);
                }

                GC.KeepAlive(roots);
                GC.KeepAlive(ring);
            }
            catch (Exception exception)
            {
                workerError = exception;
                ready.Set();
            }
        })
        {
            IsBackground = true,
        };

        worker.Start();
        int nonzeroIntervals = 0;

        try
        {
            Assert.True(ready.Wait(TimeSpan.FromSeconds(5)), "Worker startup exceeded five seconds.");
            Assert.Null(workerError);

            for (int i = 0; i < 192; i++)
            {
                Assert.True(stopwatch.Elapsed < TestTimeout, "Test exceeded twenty seconds.");

                object anchor = Allocate();
                long allocatedBytes = MeasureEmptyInterval();
                GC.KeepAlive(anchor);

                if (allocatedBytes != 0)
                {
                    Console.WriteLine($"Iteration {i} reported {allocatedBytes} allocated bytes.");
                    nonzeroIntervals++;
                }
            }
        }
        finally
        {
            stop.Cancel();
            Assert.True(worker.Join(TimeSpan.FromSeconds(5)), "Worker shutdown exceeded five seconds.");
        }

        Assert.Null(workerError);
        Assert.Equal(0, nonzeroIntervals);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MeasureEmptyInterval()
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        Thread.SpinWait(100_000);
        long after = GC.GetAllocatedBytesForCurrentThread();

        return after - before;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Allocate()
    {
        return new byte[37];
    }

    private sealed class Node
    {
        public readonly int Value;

        public Node(int value)
        {
            Value = value;
        }
    }
}
