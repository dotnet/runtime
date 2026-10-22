// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// P21 (D1): store-heavy micro benchmark. Each loop stores heap references into fields of heap objects and does not
// allocate, so no collection runs while it measures. The stores overwrite non-null values (the old value is logged by
// LXR at most once per field and epoch).
// usage: StoreBench [rounds] [repeats]
internal static class StoreBench
{
    private sealed class Node
    {
        internal Node? Next;
        internal Node? Other;
    }

    private struct Pair
    {
        internal Node? A;
        internal long Padding;
        internal Node? B;
    }

    private const int Count = 1024;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ScalarStores(Node[] holders, Node[] values, int rounds)
    {
        for (int round = 0; round < rounds; round++)
        {
            for (int index = 0; index < holders.Length; index++)
            {
                Node holder = holders[index];
                Node value = values[(index + round) & (Count - 1)];
                holder.Next = value;
                holder.Other = value;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ArrayStores(Node?[] target, Node[] values, int rounds)
    {
        for (int round = 0; round < rounds; round++)
        {
            for (int index = 0; index < target.Length; index++)
            {
                target[index] = values[(index + round) & (Count - 1)];
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StructCopies(Pair[] target, Pair[] source, int rounds)
    {
        for (int round = 0; round < rounds; round++)
        {
            for (int index = 0; index < target.Length; index++)
            {
                target[index] = source[(index + round) & (Count - 1)];
            }
        }
    }

    private static double Measure(Action<int> body, int rounds, int repeats, long storesPerRound)
    {
        body(2);
        double best = double.MaxValue;
        for (int repeat = 0; repeat < repeats; repeat++)
        {
            long start = Stopwatch.GetTimestamp();
            body(rounds);
            double seconds = (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;
            best = Math.Min(best, seconds * 1e9 / (storesPerRound * rounds));
        }
        return best;
    }

    private static int Main(string[] args)
    {
        int rounds = args.Length > 0 ? int.Parse(args[0]) : 20000;
        int repeats = args.Length > 1 ? int.Parse(args[1]) : 5;
        Node[] holders = new Node[Count];
        Node[] values = new Node[Count];
        Node?[] arrayTarget = new Node?[Count];
        Pair[] pairSource = new Pair[Count];
        Pair[] pairTarget = new Pair[Count];
        for (int index = 0; index < Count; index++)
        {
            holders[index] = new Node();
            values[index] = new Node();
            pairSource[index].A = values[index];
            pairSource[index].B = values[(index + 1) & (Count - 1)];
        }

        // Make everything older than the nursery before the loops.
        GC.Collect();
        GC.Collect();

        double scalar = Measure(r => ScalarStores(holders, values, r), rounds, repeats, 2L * Count);
        double array = Measure(r => ArrayStores(arrayTarget, values, r), rounds, repeats, Count);
        double structCopy = Measure(r => StructCopies(pairTarget, pairSource, r), rounds, repeats, 2L * Count);
        Console.WriteLine($"{{\"scalar_ns_per_store\":{scalar:F3},\"array_ns_per_store\":{array:F3},\"struct_copy_ns_per_reference\":{structCopy:F3}}}");
        GC.KeepAlive(holders);
        GC.KeepAlive(values);
        GC.KeepAlive(arrayTarget);
        GC.KeepAlive(pairSource);
        GC.KeepAlive(pairTarget);
        return 0;
    }
}
