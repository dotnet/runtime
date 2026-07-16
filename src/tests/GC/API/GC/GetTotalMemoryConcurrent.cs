// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// GC.GetTotalMemory(false) must never return a negative value.
//
// The value bottoms out in ApproxTotalBytesInUse, which for gen0 computes gen0_size - gen0_frag
// as an unsigned quantity. gen0 fragmentation (free-list + free-object space) is a per-generation
// total that spans every gen0 region, so gen0_size must span every gen0 region as well. When gen0
// retains a region past the ephemeral one (for example a region held in place by a pinned object),
// its span has to be included; otherwise fragmentation exceeds the counted span, the subtraction
// underflows, and the managed API surfaces the wrapped value as a negative long.
//
// This test drives concurrent allocation with a large, continuously-refreshed set of pinned
// objects (to force such retained gen0 regions) while repeatedly probing GetTotalMemory on another
// thread. Without the accounting fix it returns a negative value in well under a second; with the
// fix the value stays non-negative.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public class GetTotalMemoryConcurrent
{
    private static volatile bool s_stop;

    [Fact]
    public static void TestEntryPoint()
    {
        // A few seconds is far more than enough: the unfixed runtime fails almost immediately.
        // Even a single allocating thread is sufficient to build the region layout that triggers
        // the miscount, so this does not depend on the number of processors.
        const int DurationSeconds = 8;
        int workers = Math.Max(2, Math.Min(8, Environment.ProcessorCount));

        var threads = new Thread[workers];
        for (int i = 0; i < workers; i++)
        {
            threads[i] = new Thread(PinChurn) { IsBackground = true };
            threads[i].Start();
        }

        long probes = 0;
        long minObserved = long.MaxValue;
        long negative = 0;
        var sw = Stopwatch.StartNew();
        try
        {
            while (sw.Elapsed.TotalSeconds < DurationSeconds)
            {
                long total = GC.GetTotalMemory(false);
                probes++;
                if (total < minObserved)
                {
                    minObserved = total;
                }

                if (total < 0)
                {
                    negative = total;
                    break;
                }
            }
        }
        finally
        {
            s_stop = true;
            foreach (Thread t in threads)
            {
                t.Join(TimeSpan.FromSeconds(5));
            }
        }

        Console.WriteLine($"probes={probes}, min observed={minObserved}, negative={negative}");
        Assert.True(negative >= 0, $"GC.GetTotalMemory(false) returned a negative value: {negative}");
    }

    // Keeps a large ring of pinned tiny objects alive, refreshing the oldest one each iteration, and
    // floods gen0 with throwaway garbage in between. The pins prevent their gen0 regions from being
    // compacted, so those regions are swept and retained with large free lists while the live set
    // stays small - exactly the shape that makes gen0 fragmentation exceed the counted gen0 span.
    private static void PinChurn()
    {
        var rng = new Random(Environment.CurrentManagedThreadId);
        const int RingSize = 4096;
        var ring = new GCHandle[RingSize];
        int slot = 0;
        long sink = 0;

        try
        {
            while (!s_stop)
            {
                if (ring[slot].IsAllocated)
                {
                    ring[slot].Free();
                }
                ring[slot] = GCHandle.Alloc(new byte[24], GCHandleType.Pinned);
                slot = (slot + 1) % RingSize;

                for (int j = 0; j < 96; j++)
                {
                    byte[] junk = new byte[rng.Next(8, 256)];
                    sink += junk.Length;
                }
            }
        }
        finally
        {
            for (int i = 0; i < RingSize; i++)
            {
                if (ring[i].IsAllocated)
                {
                    ring[i].Free();
                }
            }
        }

        if (sink == long.MaxValue)
        {
            Console.WriteLine(sink);
        }
    }
}
