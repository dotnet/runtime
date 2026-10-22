// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

// Natural-collection benchmark. No explicit collection is requested while measuring.
// Keep the logic identical to GcBench.java: same workloads, same random numbers, same checksums.
internal static class GcBench
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate ulong GetMetric(uint metric);

    private static readonly uint[] s_lxrMetrics = [5, 10, 11, 12];

    private static GetMetric? LoadMetric()
    {
        string? path = Environment.GetEnvironmentVariable("DOTNET_GCPath");
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        IntPtr module = NativeLibrary.Load(path);
        return Marshal.GetDelegateForFunctionPointer<GetMetric>(NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
    }

    private static long[] Snapshot(GetMetric? metric)
    {
        long[] values = new long[s_lxrMetrics.Length];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = metric is null ? -1 : (long)metric(s_lxrMetrics[i]);
        }
        return values;
    }

    private const int Buckets = 64 * 8;
    private const int ListLength = 64;
    private static ulong s_state;
    private static object? s_keep;

    private static ulong Next()
    {
        s_state ^= s_state >> 12;
        s_state ^= s_state << 25;
        s_state ^= s_state >> 27;
        return s_state * 0x2545F4914F6CDD1DUL;
    }

    private static int Rnd(int n) => (int)((Next() >> 33) % (ulong)n);

    private static void Record(long[] histogram, long ns)
    {
        if (ns < 1)
        {
            ns = 1;
        }
        int lg = 63 - System.Numerics.BitOperations.LeadingZeroCount((ulong)ns);
        int sub = lg >= 3 ? (int)((ns >> (lg - 3)) & 7) : 0;
        histogram[lg * 8 + sub]++;
    }

    private static double PercentileMicros(long[] histogram, double fraction)
    {
        long total = 0;
        foreach (long count in histogram)
        {
            total += count;
        }
        if (total == 0)
        {
            return 0;
        }
        long target = (long)Math.Ceiling(total * fraction);
        long seen = 0;
        for (int bucket = 0; bucket < histogram.Length; bucket++)
        {
            seen += histogram[bucket];
            if (seen >= target)
            {
                int lg = bucket / 8;
                int sub = bucket % 8;
                double upper = lg >= 3 ? (9 + sub) * (double)(1L << (lg - 3)) : (double)(1L << lg);
                return upper / 1000.0;
            }
        }
        return 0;
    }

    private sealed class Tree
    {
        public Tree? Left;
        public Tree? Right;

        public Tree(Tree? left, Tree? right)
        {
            Left = left;
            Right = right;
        }
    }

    private static Tree BottomUp(int depth) =>
        depth > 0 ? new Tree(BottomUp(depth - 1), BottomUp(depth - 1)) : new Tree(null, null);

    private static int Check(Tree t) => t.Left is null ? 1 : 1 + Check(t.Left) + Check(t.Right!);

    private static long BinaryTrees(int maxDepth, bool calibrate)
    {
        const int minDepth = 4;
        if (calibrate)
        {
            s_keep = BottomUp(maxDepth);
            return 0;
        }
        long total = Check(BottomUp(maxDepth + 1));
        Tree longLived = BottomUp(maxDepth);
        for (int depth = minDepth; depth <= maxDepth; depth += 2)
        {
            int iterations = 1 << (maxDepth - depth + minDepth);
            long sum = 0;
            for (int i = 0; i < iterations; i++)
            {
                sum += Check(BottomUp(depth));
            }
            total += sum + depth;
        }
        total += Check(longLived);
        return total;
    }

    private sealed class Item
    {
        public int V;
        public Item? Next;
        public byte[]? Data;
    }

    private sealed class Entry
    {
        public int Key;
        public Item? Items;
    }

    private static Entry BuildEntry(int key)
    {
        Entry e = new() { Key = key };
        Item? head = null;
        for (int i = 0; i < 4; i++)
        {
            Item it = new() { V = key + i, Data = new byte[48] };
            it.Data[0] = (byte)(key + i);
            it.Next = head;
            head = it;
        }
        e.Items = head;
        return e;
    }

    private static long Requests(int entries, long count, long[]? latency, bool calibrate)
    {
        Entry[] table = new Entry[entries];
        for (int i = 0; i < entries; i++)
        {
            table[i] = BuildEntry(i);
        }
        if (calibrate)
        {
            s_keep = table;
            return 0;
        }
        long cs = 0;
        for (long r = 0; r < count; r++)
        {
            long started = latency is not null ? Stopwatch.GetTimestamp() : 0;
            Item? head = null;
            for (int i = 0; i < 24; i++)
            {
                Item it = new() { V = i, Data = new byte[128] };
                it.Data[0] = (byte)i;
                it.Next = head;
                head = it;
            }
            for (Item? it = head; it is not null; it = it.Next)
            {
                cs += it.V + it.Data![0];
            }
            for (int k = 0; k < 8; k++)
            {
                Entry e = table[Rnd(entries)];
                for (Item? it = e.Items; it is not null; it = it.Next)
                {
                    cs += it.V + it.Data![0];
                }
            }
            if ((Next() & 7) == 0)
            {
                int slot = Rnd(entries);
                table[slot] = BuildEntry(slot);
            }
            if (latency is not null)
            {
                Record(latency, (long)((Stopwatch.GetTimestamp() - started) * (1e9 / Stopwatch.Frequency)));
            }
        }
        return cs;
    }

    private sealed class Node
    {
        public int V;
        public Node? Next;
    }

    private static long ListSuffix(int slots, long ops, bool calibrate)
    {
        Node?[] heads = new Node?[slots];
        for (int j = 0; j < slots; j++)
        {
            Node? head = null;
            for (int i = 0; i < ListLength; i++)
            {
                head = new Node { V = j + i, Next = head };
            }
            heads[j] = head;
        }
        if (calibrate)
        {
            s_keep = heads;
            return 0;
        }
        long cs = 0;
        for (long op = 0; op < ops; op++)
        {
            int j = Rnd(slots);
            int p = Rnd(ListLength);
            Node cur = heads[j]!;
            for (int i = 0; i < p; i++)
            {
                cur = cur.Next!;
            }
            Node prev = cur;
            for (int i = 0; i < ListLength - 1 - p; i++)
            {
                Node n = new() { V = (int)op ^ i };
                prev.Next = n;
                prev = n;
            }
            prev.Next = null;
            cs += cur.V;
        }
        return cs;
    }

    // PINHEAVY_NOPOH=1 replaces the pinned-object-heap arrays of the pin-heavy workload by ordinary arrays.
    private static readonly bool s_pinHeavyNoPoh = Environment.GetEnvironmentVariable("PINHEAVY_NOPOH") == "1";

    // P17 S7: pinned handles, `fixed` locals and pinned-object-heap arrays while allocating. Four threads, each with its
    // own random numbers, so the sum of the thread checksums does not depend on the thread schedule.
    private static unsafe long PinHeavyThread(int thread, int slots, long ops)
    {
        ulong state = 0x9E3779B97F4A7C15UL ^ (ulong)(thread + 1) * 0xD1B54A32D192ED03UL;
        ulong NextLocal()
        {
            state ^= state >> 12;
            state ^= state << 25;
            state ^= state >> 27;
            return state * 0x2545F4914F6CDD1DUL;
        }
        GCHandle[] handles = new GCHandle[slots];
        byte[]?[] pinnedArrays = new byte[]?[slots];
        long cs = 0;
        for (long op = 0; op < ops; op++)
        {
            ulong r = NextLocal();
            int slot = (int)((r >> 33) % (ulong)slots);
            int length = 256 + (int)((r >> 20) % 4096);
            switch ((r >> 8) & 3)
            {
                case 0:
                {
                    if (handles[slot].IsAllocated)
                    {
                        byte[] old = (byte[])handles[slot].Target!;
                        cs += old[0] + old[old.Length - 1];
                        handles[slot].Free();
                    }
                    byte[] buffer = new byte[length];
                    buffer[0] = (byte)op;
                    buffer[length - 1] = (byte)(op >> 3);
                    handles[slot] = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    break;
                }
                case 1:
                {
                    byte[] buffer = new byte[length];
                    buffer[length / 2] = (byte)op;
                    fixed (byte* pointer = buffer)
                    {
                        Node? head = null;
                        for (int i = 0; i < 16; i++)
                        {
                            head = new Node { V = i, Next = head };
                            cs += pointer[length / 2] + head.V;
                        }
                    }
                    break;
                }
                case 2:
                {
                    byte[]? old = pinnedArrays[slot];
                    if (old is not null)
                    {
                        cs += old[0] + old[old.Length - 1];
                    }
                    byte[] array = s_pinHeavyNoPoh ? new byte[length] : GC.AllocateArray<byte>(length, pinned: true);
                    array[0] = (byte)op;
                    array[length - 1] = (byte)(op >> 3);
                    pinnedArrays[slot] = array;
                    break;
                }
                default:
                {
                    Node? head = null;
                    for (int i = 0; i < 24; i++)
                    {
                        head = new Node { V = i, Next = head };
                    }
                    for (Node? n = head; n is not null; n = n.Next)
                    {
                        cs += n.V;
                    }
                    break;
                }
            }
        }
        for (int slot = 0; slot < slots; slot++)
        {
            if (handles[slot].IsAllocated)
            {
                handles[slot].Free();
            }
        }
        return cs;
    }

    private static long PinHeavy(int slots, long ops, bool calibrate)
    {
        const int Threads = 4;
        if (calibrate)
        {
            s_keep = new byte[Threads * slots * 2048];
            return 0;
        }
        long[] sums = new long[Threads];
        Thread[] threads = new Thread[Threads];
        for (int t = 0; t < Threads; t++)
        {
            int id = t;
            threads[t] = new Thread(() => sums[id] = PinHeavyThread(id, slots, ops / Threads));
            threads[t].Start();
        }
        long total = 0;
        for (int t = 0; t < Threads; t++)
        {
            threads[t].Join();
            total += sums[t];
        }
        return total;
    }

    private static long Run(string workload, int size, long ops, long[]? latency, bool calibrate) => workload switch
    {
        "binarytrees" => BinaryTrees(size, calibrate),
        "requests" => Requests(size, ops, latency, calibrate),
        "listsuffix" => ListSuffix(size, ops, calibrate),
        "pinheavy" => PinHeavy(size, ops, calibrate),
        _ => throw new ArgumentException(workload),
    };

    private static volatile bool s_stop;
    private static readonly long[] s_hiccup = new long[Buckets];
    private static long s_hiccupMax;

    private static long StatusKb(string key)
    {
        try
        {
            foreach (string line in File.ReadLines("/proc/self/status"))
            {
                if (line.StartsWith(key, StringComparison.Ordinal))
                {
                    return long.Parse(new string(Array.FindAll(line.ToCharArray(), char.IsDigit)), CultureInfo.InvariantCulture);
                }
            }
        }
        catch (IOException)
        {
            return -1;
        }
        return -1;
    }

    // P24 diagnostic variant: CPU time of every thread of the process, from /proc/self/task (user and system, ms).
    private static string ThreadCpu()
    {
        StringBuilder b = new("[");
        try
        {
            long tick = 10; // 100 ticks per second on Linux
            foreach (string dir in Directory.GetDirectories("/proc/self/task"))
            {
                string stat = File.ReadAllText(dir + "/stat");
                int open = stat.IndexOf('(');
                int close = stat.LastIndexOf(')');
                string name = stat.Substring(open + 1, close - open - 1);
                string[] f = stat.Substring(close + 2).Split(' ');
                long user = long.Parse(f[11], CultureInfo.InvariantCulture) * tick;
                long sys = long.Parse(f[12], CultureInfo.InvariantCulture) * tick;
                if (user + sys < 50)
                {
                    continue;
                }
                if (b.Length > 1)
                {
                    b.Append(',');
                }
                b.Append(CultureInfo.InvariantCulture, $"{{\"n\":\"{name}\",\"u\":{user},\"s\":{sys}}}");
            }
        }
        catch (IOException)
        {
        }
        b.Append(']');
        return b.ToString();
    }

    public static int Main(string[] a)
    {
        string workload = a[0];
        int size = int.Parse(a[1], CultureInfo.InvariantCulture);
        long ops = long.Parse(a[2], CultureInfo.InvariantCulture);
        string mode = a.Length > 3 ? a[3] : "run";
        if (mode == "calibrate")
        {
            Run(workload, size, ops, null, true);
            long live = GC.GetTotalMemory(true);
            Console.WriteLine($"{{\"runtime\":\"dotnet\",\"workload\":\"{workload}\",\"live_bytes\":{live}}}");
            GC.KeepAlive(s_keep);
            return 0;
        }
        // P24 diagnostic variant: GCBENCH_LATENCY=batch turns the concurrent trace of the LXR collector off (SetGcLatencyMode(0)).
        string? latency0 = Environment.GetEnvironmentVariable("GCBENCH_LATENCY");
        if (latency0 == "batch")
        {
            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Batch;
        }
        int warmSize = workload == "binarytrees" ? Math.Max(8, size - 4) : workload == "pinheavy" ? size : Math.Max(1024, size / 8);
        long warmOps = Math.Max(1, ops / 8);
        s_state = 0x9E3779B97F4A7C15UL;
        Run(workload, warmSize, warmOps, null, false);
        s_state = 0x9E3779B97F4A7C15UL;
        long[] latency = new long[Buckets];
        Thread meter = new(() =>
        {
            while (!s_stop)
            {
                long t = Stopwatch.GetTimestamp();
                Thread.Sleep(1);
                long d = (long)((Stopwatch.GetTimestamp() - t) * (1e9 / Stopwatch.Frequency));
                Record(s_hiccup, d);
                if (d > s_hiccupMax)
                {
                    s_hiccupMax = d;
                }
            }
        })
        { IsBackground = true };
        using Process process = Process.GetCurrentProcess();
        GetMetric? lxr = LoadMetric();
        long[] lxrBefore = Snapshot(lxr);
        int gcBefore = GC.CollectionCount(0);
        double pauseBefore = GC.GetTotalPauseDuration().TotalMilliseconds;
        meter.Start();
        List<string> events = new();
        Thread poller = new(() =>
        {
            if (lxr is null)
            {
                return;
            }
            uint[] ids = [5, 10, 11, 12, 43, 41, 42, 39, 40, 3, 26, 36, 13, 14, 21, 23, 24, 79, 80, 95, 96, 1, 2, 28,
                81, 82, 83, 91, 92, 93, 94, 97, 98, 99, 100, 103, 7, 86];
            string[] names = ["collections", "initial", "final", "full", "cause", "cycle_hint", "emergency_hint", "survival_e9", "mature_live_e3",
                "live_bytes", "committed_bytes", "admitted_bytes", "traced_objects", "traced_refs", "interruptions", "incs", "decs",
                "nursery_copied_objs", "nursery_copied_bytes", "mature_copied_objs", "mature_copied_bytes", "allocations", "allocated_bytes", "trace_phase",
                "nursery_in_place_pinned_bytes", "nursery_copy_failures", "nursery_forwarding_waits", "mature_eligible_blocks", "mature_selected_blocks",
                "mature_pinned_blocks", "mature_estimated_copy_bytes", "mature_copy_failures", "remembered_entries", "stale_entries", "replayed_entries",
                "invalid_slots", "blocks_released", "pause_young_bytes"];
            bool live = Environment.GetEnvironmentVariable("GCTRACE_LIVE") == "1";
            // Extra counters: GCTRACE_EXTRA="name=id,name=id".
            List<(string Name, uint Id)> extra = new();
            string? extraText = Environment.GetEnvironmentVariable("GCTRACE_EXTRA");
            if (!string.IsNullOrEmpty(extraText))
            {
                foreach (string item in extraText.Split(','))
                {
                    string[] parts = item.Split('=');
                    extra.Add((parts[0], uint.Parse(parts[1], CultureInfo.InvariantCulture)));
                }
            }
            // Phase timers: metric 240 is a sequence number (odd while the values are rewritten), 256 + i holds phase i.
            string[] phases = ["total", "lock_wait", "policy_drain", "pre_drain", "suspend_runtime", "suspend_ee", "suspend_prepare",
                "root_capture", "prepare_work", "release_epoch", "retire_sources", "reclaim_nursery", "sweep_blocks", "release_flip",
                "scan0", "scan1", "scan2", "scan3", "scan_before_roots", "scan_lease_finalizers", "scan_roots", "scan_handles",
                "scan_profiler_handles", "scan_dependent", "scan_syncblock", "scan_frozen", "scan_dh_initial", "scan_short_weak",
                "scan_queue_finalizers", "scan_trace_finalizers", "scan_dh_rescan", "scan_weak_ptr", "scan_weak_ptr_single",
                "scan_relocate_roots", "scan_relocate_handles", "scan_promotions_granted", "scan_heap_statistics",
                "resume_runtime", "stw_pause", "post_drain", "resume_gap", "publish_trace", "borrow", "give_back"];
            string[] groups = ["inc", "dec", "satb", "prepare_trace", "weak_closure", "epoch_closure", "nursery_sweep", "mature_sweep"];
            int published = phases.Length + 3 * groups.Length;
            ulong last = lxr(240);
            long begin = Stopwatch.GetTimestamp();
            ulong[] values = new ulong[published];
            while (!s_stop)
            {
                ulong now = lxr(240);
                if (now != last && (now & 1) == 0)
                {
                    ulong before = now;
                    for (int i = 0; i < published; i++)
                    {
                        values[i] = lxr((uint)(256 + i));
                    }
                    if (lxr(240) != before)
                    {
                        continue;
                    }
                    StringBuilder b = new();
                    b.Append(CultureInfo.InvariantCulture, $"{{\"t_ms\":{Stopwatch.GetElapsedTime(begin).TotalMilliseconds:F1},\"seq\":{now / 2},\"kind\":{lxr(241)},\"timestamps\":{lxr(242)}");
                    for (int i = 0; i < ids.Length; i++)
                    {
                        b.Append(CultureInfo.InvariantCulture, $",\"{names[i]}\":{lxr(ids[i])}");
                    }
                    foreach ((string name, uint id) in extra)
                    {
                        b.Append(CultureInfo.InvariantCulture, $",\"{name}\":{lxr(id)}");
                    }
                    for (int i = 0; i < phases.Length; i++)
                    {
                        b.Append(CultureInfo.InvariantCulture, $",\"{phases[i]}_us\":{values[i] / 1000.0:F1}");
                    }
                    for (int g = 0; g < groups.Length; g++)
                    {
                        int slot = phases.Length + 3 * g;
                        b.Append(CultureInfo.InvariantCulture,
                            $",\"{groups[g]}_wall_us\":{values[slot] / 1000.0:F1},\"{groups[g]}_busy_us\":{values[slot + 1] / 1000.0:F1},\"{groups[g]}_packets\":{values[slot + 2]}");
                    }
                    b.Append('}');
                    lock (events)
                    {
                        events.Add(b.ToString());
                        if (live)
                        {
                            Console.WriteLine("trace_event=" + b);
                        }
                    }
                    last = now;
                }
                Thread.Yield();
            }
        })
        { IsBackground = true };
        if (Environment.GetEnvironmentVariable("GCTRACE") == "1")
        {
            poller.Start();
        }
        TimeSpan cpu0 = process.TotalProcessorTime;
        long t0 = Stopwatch.GetTimestamp();
        long checksum = Run(workload, size, ops, latency, false);
        double elapsed = Stopwatch.GetElapsedTime(t0).TotalMilliseconds;
        double cpu = (process.TotalProcessorTime - cpu0).TotalMilliseconds;
        s_stop = true;
        meter.Join();
        if (poller.IsAlive)
        {
            poller.Join();
        }
        if (Environment.GetEnvironmentVariable("GCTRACE_LIVE") != "1")
        {
            lock (events)
            {
                foreach (string line in events)
                {
                    Console.WriteLine("trace_event=" + line);
                }
            }
        }
        int gcCount = GC.CollectionCount(0) - gcBefore;
        long[] lxrAfter = Snapshot(lxr);
        string lxrJson = lxr is null ? string.Empty : string.Create(CultureInfo.InvariantCulture,
            $"\"lxr_pauses\":{lxrAfter[0] - lxrBefore[0]},\"lxr_initial_mark\":{lxrAfter[1] - lxrBefore[1]}," +
            $"\"lxr_final_mark\":{lxrAfter[2] - lxrBefore[2]},\"lxr_full\":{lxrAfter[3] - lxrBefore[3]},");
        double pause = GC.GetTotalPauseDuration().TotalMilliseconds - pauseBefore;
        CultureInfo c = CultureInfo.InvariantCulture;
        Console.WriteLine(string.Create(c,
            $"{{\"runtime\":\"dotnet\",\"workload\":\"{workload}\",\"size\":{size},\"ops\":{ops},\"checksum\":{checksum},\"elapsed_ms\":{elapsed:F3},\"cpu_ms\":{cpu:F3}," +
            $"\"gc_count\":{gcCount},\"gc_ms\":{(long)pause},{lxrJson}\"hiccup_p50_us\":{PercentileMicros(s_hiccup, 0.5):F1},\"hiccup_p99_us\":{PercentileMicros(s_hiccup, 0.99):F1}," +
            $"\"hiccup_p999_us\":{PercentileMicros(s_hiccup, 0.999):F1},\"hiccup_max_us\":{s_hiccupMax / 1000.0:F1}," +
            $"\"req_p50_us\":{PercentileMicros(latency, 0.5):F1},\"req_p99_us\":{PercentileMicros(latency, 0.99):F1},\"req_p999_us\":{PercentileMicros(latency, 0.999):F1}," +
            $"\"peak_rss_kb\":{StatusKb("VmHWM")},\"threads\":{ThreadCpu()}}}"));
        return 0;
    }
}
