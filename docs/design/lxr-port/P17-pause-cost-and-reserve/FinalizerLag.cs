// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// P17 S6: how late does a finalizer run, and what does it cost? Natural collections only: nothing in the measured part
// forces a collection. Every object stores the time at which it stopped being used; its finalizer reports the delay.
//
// usage: FinalizerLag <objects> <ring> <payloadBytes> <disposeEvery> [threads] [plain]
//   objects       finalizable objects to create in the measured part
//   ring          objects that stay referenced (a live set); an object that leaves the ring stops being used
//   payloadBytes  size of the array that each object owns
//   disposeEvery  every n-th object is disposed (GC.SuppressFinalize) when it stops being used; 0 = none
//   threads       producer threads (default 1)
//   plain         use objects without a finalizer (the control for the memory cost of the registry)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

internal static class FinalizerLag
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate ulong GetMetric(uint metric);

    private class Base
    {
        internal long _stopped;
        internal readonly byte[] _payload;

        internal Base(int payload) => _payload = new byte[payload];
    }

    // The control: the same object without a finalizer.
    private sealed class PlainItem : Base
    {
        internal PlainItem(int payload) : base(payload)
        {
        }
    }

    private sealed class Item : Base
    {
        internal Item(int payload) : base(payload)
        {
        }

        ~Item()
        {
            long now = Stopwatch.GetTimestamp();
            long delay = _stopped == 0 ? 0 : now - _stopped;
            Interlocked.Increment(ref s_finalized);
            Record(delay);
        }
    }

    private static readonly string[] s_phases =
    [
        "total", "lock_wait", "policy_drain", "pre_drain", "suspend_runtime", "suspend_ee", "suspend_prepare",
        "root_capture", "prepare_work", "release_epoch", "retire_sources", "reclaim_nursery", "sweep_blocks",
        "release_flip", "scan0", "scan1", "scan2", "scan3", "scan_before_roots", "registry_capture",
        "scan_roots", "scan_handles", "scan_profiler_handles", "scan_dependent", "scan_syncblock", "scan_frozen",
        "scan_dh_initial", "scan_short_weak", "registry_decide", "registry_trace", "scan_dh_rescan",
        "scan_weak_ptr", "scan_weak_ptr_single", "scan_relocate_roots", "scan_relocate_handles",
        "scan_promotions_granted", "scan_heap_statistics", "resume_runtime", "stw_pause", "post_drain", "resume_gap",
        "publish_trace", "borrow", "give_back",
    ];

    private static readonly string[] s_groups =
        ["increments", "decrements", "satb", "prepare_trace", "weak_closure", "epoch_closure", "nursery_sweep", "mature_sweep"];

    private static long s_finalized;
    private static long s_suppressed;
    private static readonly long[] s_buckets = new long[64];
    private static long s_delaySum;
    private static long s_delayMax;

    // Bucket b holds delays in [2^b, 2^(b+1)) microseconds (bucket 0: below 2 microseconds).
    private static void Record(long ticks)
    {
        long micro = ticks * 1_000_000 / Stopwatch.Frequency;
        int bucket = micro < 2 ? 0 : 63 - System.Numerics.BitOperations.LeadingZeroCount((ulong)micro);
        Interlocked.Increment(ref s_buckets[bucket]);
        Interlocked.Add(ref s_delaySum, micro);
        long max = Interlocked.Read(ref s_delayMax);
        while (micro > max && Interlocked.CompareExchange(ref s_delayMax, micro, max) != max)
        {
            max = Interlocked.Read(ref s_delayMax);
        }
    }

    private static double Percentile(long[] buckets, double fraction)
    {
        long total = 0;
        foreach (long count in buckets)
        {
            total += count;
        }
        if (total == 0)
        {
            return 0;
        }
        long target = (long)Math.Ceiling(total * fraction);
        long seen = 0;
        for (int bucket = 0; bucket < buckets.Length; bucket++)
        {
            seen += buckets[bucket];
            if (seen >= target)
            {
                // The upper edge of the bucket, in milliseconds.
                return Math.Pow(2, bucket + 1) / 1000.0;
            }
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Produce(int count, Base?[] ring, int first, int ringPart, int payload, int disposeEvery, long seed, bool plain)
    {
        long counter = seed;
        for (int index = 0; index < count; index++)
        {
            Base item = plain ? new PlainItem(payload) : new Item(payload);
            if (ringPart == 0)
            {
                item._stopped = Stopwatch.GetTimestamp();
                if (!plain && disposeEvery > 0 && ++counter % disposeEvery == 0)
                {
                    GC.SuppressFinalize(item);
                    Interlocked.Increment(ref s_suppressed);
                }
                continue;
            }
            int slot = first + index % ringPart;
            Base? old = ring[slot];
            if (old is not null)
            {
                old._stopped = Stopwatch.GetTimestamp();
                if (!plain && disposeEvery > 0 && ++counter % disposeEvery == 0)
                {
                    GC.SuppressFinalize(old);
                    Interlocked.Increment(ref s_suppressed);
                }
            }
            ring[slot] = item;
        }
    }

    private static int Main(string[] args)
    {
        int objects = int.Parse(args[0]);
        int ring = int.Parse(args[1]);
        int payload = int.Parse(args[2]);
        int disposeEvery = int.Parse(args[3]);
        int threads = args.Length > 4 ? int.Parse(args[4]) : 1;
        bool plain = args.Length > 5 && args[5] == "plain";
        GetMetric? metric = null;
        string? module = Environment.GetEnvironmentVariable("DOTNET_GCPath");
        if (!string.IsNullOrEmpty(module))
        {
            nint handle = NativeLibrary.Load(module);
            metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(NativeLibrary.GetExport(handle, "GC_LxrPrototype_GetMetric"));
        }
        Base?[] live = new Base?[Math.Max(ring, 1)];
        int perThread = objects / threads;
        long start = Stopwatch.GetTimestamp();
        Thread[] workers = new Thread[threads];
        for (int thread = 0; thread < threads; thread++)
        {
            int first = ring == 0 ? 0 : thread * (ring / threads);
            int part = ring == 0 ? 0 : ring / threads;
            long seed = thread;
            workers[thread] = new Thread(() => Produce(perThread, live, first, part, payload, disposeEvery, seed, plain));
            workers[thread].Start();
        }
        foreach (Thread worker in workers)
        {
            worker.Join();
        }
        double elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        // Snapshot of the measured part: nothing has forced a collection yet.
        long finalizedAtEnd = Interlocked.Read(ref s_finalized);
        long[] buckets = new long[64];
        for (int index = 0; index < buckets.Length; index++)
        {
            buckets[index] = Interlocked.Read(ref s_buckets[index]);
        }
        long delaySum = Interlocked.Read(ref s_delaySum);
        long delayMax = Interlocked.Read(ref s_delayMax);
        double pauseMs = GCSettings.IsServerGC ? 0 : GC.GetTotalPauseDuration().TotalMilliseconds;
        long peakRss;
        using (Process process = Process.GetCurrentProcess())
        {
            peakRss = process.PeakWorkingSet64;
        }
        Dictionary<string, object?> record = new()
        {
            ["objects"] = (long)perThread * threads,
            ["ring"] = ring,
            ["payload"] = payload,
            ["dispose_every"] = disposeEvery,
            ["threads"] = threads,
            ["plain"] = plain,
            ["elapsed_ms"] = Math.Round(elapsedMs, 1),
            ["finalized_at_end"] = finalizedAtEnd,
            ["delay_mean_ms"] = finalizedAtEnd == 0 ? 0 : Math.Round(delaySum / 1000.0 / finalizedAtEnd, 2),
            ["delay_p50_ms"] = Percentile(buckets, 0.50),
            ["delay_p95_ms"] = Percentile(buckets, 0.95),
            ["delay_p99_ms"] = Percentile(buckets, 0.99),
            ["delay_max_ms"] = delayMax / 1000.0,
            ["peak_rss_mib"] = Math.Round(peakRss / 1048576.0, 1),
            ["collections_gen0"] = GC.CollectionCount(0),
            ["gc_pause_total_ms"] = Math.Round(pauseMs, 1),
        };
        if (metric is not null)
        {
            record["pauses"] = metric(5);
            record["initial_marks"] = metric(10);
            record["final_marks"] = metric(11);
            record["full_marks"] = metric(12);
            record["tracing_pauses"] = metric(10) + metric(12);
            record["registry_counted"] = metric(450);
            record["registry_registered"] = metric(451);
            record["registry_decided"] = metric(454);
            record["registry_dropped"] = metric(455);
            record["registry_walked"] = metric(457);
            record["pending_at_end"] = metric(25);
            if (metric(244) == 1)
            {
                // S0 phase timers, summed over all collections (ms). Scan1 is the tracing-pause decision step.
                Dictionary<string, double> phases = new();
                for (int index = 0; index < s_phases.Length; index++)
                {
                    ulong value = metric((uint)(340 + index));
                    if (value != 0 && value != ulong.MaxValue)
                    {
                        phases[s_phases[index]] = Math.Round(value / 1e6, 1);
                    }
                }
                for (int group = 0; group < s_groups.Length; group++)
                {
                    ulong busy = metric((uint)(340 + s_phases.Length + 3 * group + 1));
                    if (busy != 0 && busy != ulong.MaxValue)
                    {
                        phases["group_" + s_groups[group] + "_busy"] = Math.Round(busy / 1e6, 1);
                    }
                }
                record["phases_ms"] = phases;
            }
        }
        else
        {
            record["tracing_pauses"] = GC.CollectionCount(2);
        }
        // Drain: not part of the measured part. It tells that no finalizer was lost.
        for (int attempt = 0; attempt < 20; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        long created = (long)perThread * threads;
        long inRing = 0;
        foreach (Base? item in live)
        {
            inRing += item is null ? 0 : 1;
        }
        long finalized = Interlocked.Read(ref s_finalized);
        record["finalized_after_drain"] = finalized;
        record["suppressed"] = Interlocked.Read(ref s_suppressed);
        // Every object is in the ring, finalized, or had its finalization suppressed. Anything else is lost.
        record["lost"] = created - inRing - finalized - Interlocked.Read(ref s_suppressed);
        GC.KeepAlive(live);
        Console.WriteLine(JsonSerializer.Serialize(record));
        return 0;
    }
}
