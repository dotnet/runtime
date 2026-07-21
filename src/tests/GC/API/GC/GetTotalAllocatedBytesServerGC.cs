// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for a Server GC accounting bug in GC.GetTotalAllocatedBytes().
//
// GCHeap::GetTotalAllocatedBytes() summed each heap's cumulative total_alloc_bytes
// over the *currently-active* heaps [0, n_heaps). Under DATAS (Dynamic Adaptation To
// Application Sizes, default-on for Server GC) the active heap count grows and shrinks
// at run time as load changes. Because a decommissioned heap keeps its (real, frozen)
// total_alloc_bytes, excluding it from the sum made the returned value DROP whenever the
// heap count shrank -- i.e. GC.GetTotalAllocatedBytes() became non-monotonic. The
// precise overload has no smoothing, so it exposed the drop directly (surfacing as random
// zero / hallucinated allocation deltas in tools that difference two reads).
//
// This test drives Server GC with an oscillating allocation load (bursts grow the heap
// count, quiet periods let DATAS shrink it) while forcing frequent blocking gen2 GCs --
// the points at which DATAS re-evaluates and decommissions heaps -- and repeatedly reads
// the precise cumulative total. Because the value is a cumulative counter, it must never
// decrease. A shrink-induced drop equals an entire heap's cumulative allocation, which is
// far larger than the small, by-design fluctuation of the precise value (per-thread unused
// allocation-context bytes); the tolerance below absorbs the latter while catching the bug.
//
// Only meaningful under Server GC (the .csproj sets DOTNET_gcServer=1). Under Workstation
// GC there is a single heap and no heap-count oscillation, so the check is a trivial pass.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;

public class GetTotalAllocatedBytesServerGC
{
    [Fact]
    public static void TestEntryPoint()
    {
        TimeSpan budget = TimeSpan.FromSeconds(20);

        // The precise total legitimately fluctuates by a small amount (it subtracts each
        // thread's currently-unused allocation-context bytes). The bug drops a whole heap's
        // cumulative counter, which is orders of magnitude larger. Allow generous slack for
        // the benign fluctuation so the test cannot false-fail on a correct runtime.
        const long Tolerance = 64L * 1024 * 1024;

        int burstThreads = Math.Max(2, Environment.ProcessorCount);
        using var cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;

        // Load controller: alternate a heavy BURST (many allocators -> DATAS grows the heap
        // count) with a QUIET period (near-zero allocation -> DATAS shrinks it).
        var controller = new Thread(() =>
        {
            while (!token.IsCancellationRequested)
            {
                var burst = new List<Thread>();
                using var burstCts = new CancellationTokenSource();
                for (int i = 0; i < burstThreads; i++)
                {
                    var t = new Thread(() =>
                    {
                        var rnd = new Random(Environment.CurrentManagedThreadId);
                        object local = null;
                        while (!burstCts.IsCancellationRequested)
                        {
                            for (int j = 0; j < 2000; j++)
                                local = new byte[rnd.Next(16, 4096)];
                        }
                        GC.KeepAlive(local);
                    })
                    { IsBackground = true };
                    t.Start();
                    burst.Add(t);
                }

                bool cancelled = token.WaitHandle.WaitOne(1500);

                // Always cancel and join the burst threads so no allocator outlives the
                // BURST -- this guarantees the following QUIET period is truly allocation-free.
                burstCts.Cancel();
                foreach (var t in burst)
                    t.Join(2000);

                if (cancelled)
                    break;

                // QUIET: near-zero allocation so DATAS decides to shrink the heap count.
                if (token.WaitHandle.WaitOne(3000))
                    break;
            }
        })
        { IsBackground = true, Name = "load-controller" };
        controller.Start();

        long maxDrop = 0;
        long prevInitial = 0, prevFinal = 0;
        long prev = GC.GetTotalAllocatedBytes(precise: true);
        var sw = Stopwatch.StartNew();
        try
        {
            while (sw.Elapsed < budget)
            {
                // A blocking gen2 GC is where DATAS re-evaluates (and can decommission) heaps.
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                long cur = GC.GetTotalAllocatedBytes(precise: true);
                if (cur < prev)
                {
                    long drop = prev - cur;
                    if (drop > maxDrop)
                    {
                        maxDrop = drop;
                        prevInitial = prev;
                        prevFinal = cur;
                    }
                }
                prev = cur;
            }
        }
        finally
        {
            cts.Cancel();
            controller.Join(3000);
        }

        Assert.True(
            maxDrop <= Tolerance,
            $"GC.GetTotalAllocatedBytes(precise: true) is a cumulative counter and must not " +
            $"decrease, but it dropped by {maxDrop:N0} bytes (from {prevInitial:N0} to {prevFinal:N0}), " +
            $"exceeding the {Tolerance:N0} byte tolerance. Server GC = {System.Runtime.GCSettings.IsServerGC}, " +
            $"processors = {Environment.ProcessorCount}.");
    }
}
