// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
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

    private static long Run(string workload, int size, long ops, long[]? latency, bool calibrate) => workload switch
    {
        "binarytrees" => BinaryTrees(size, calibrate),
        "requests" => Requests(size, ops, latency, calibrate),
        "listsuffix" => ListSuffix(size, ops, calibrate),
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
        int warmSize = workload == "binarytrees" ? Math.Max(8, size - 4) : Math.Max(1024, size / 8);
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
        TimeSpan cpu0 = process.TotalProcessorTime;
        long t0 = Stopwatch.GetTimestamp();
        long checksum = Run(workload, size, ops, latency, false);
        double elapsed = Stopwatch.GetElapsedTime(t0).TotalMilliseconds;
        double cpu = (process.TotalProcessorTime - cpu0).TotalMilliseconds;
        s_stop = true;
        meter.Join();
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
            $"\"peak_rss_kb\":{StatusKb("VmHWM")}}}"));
        return 0;
    }
}
