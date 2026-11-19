// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

// ImmutableSortedDictionary.Node.Search is compiled into a self-recursive fast tail call ("pop ...; jmp Search"). When a GC suspension
// hijacks the thread in the epilogue of such a call, the return address must still be found in the right stack slot. Getting
// this wrong corrupts the caller's frame, so the failure mode is a crash (rare per GC, so we keep triggering GCs for a while).
internal class Program
{
    private const int KeyCount = 100_000;
    private const int CollectingThreads = 2;
    private const int FillingThreads = 2;
    private static readonly TimeSpan s_duration = TimeSpan.FromSeconds(10);

    private static volatile bool s_stop;

    private static int Main()
    {
        Thread[] threads = new Thread[CollectingThreads];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                while (!s_stop)
                {
                    GC.Collect(0);
                }
            });
            threads[i].Start();
        }

        string[] keys = new string[KeyCount];
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i] = "k" + i.ToString("D6", CultureInfo.InvariantCulture);
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        Thread[] fillers = new Thread[FillingThreads - 1];
        for (int i = 0; i < fillers.Length; i++)
        {
            fillers[i] = new Thread(() => FillUntilDone(keys, stopwatch));
            fillers[i].Start();
        }

        FillUntilDone(keys, stopwatch);

        foreach (Thread thread in fillers)
        {
            thread.Join();
        }

        s_stop = true;
        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        return 100;
    }

    private static void FillUntilDone(string[] keys, Stopwatch stopwatch)
    {
        while (stopwatch.Elapsed < s_duration)
        {
            Fill(keys);
        }
    }

    // ContainsKey calls Node.Search, and the caller keeps using its frame after the call returns.
    private static ImmutableSortedDictionary<string, object> Fill(string[] keys)
    {
        var map = ImmutableSortedDictionary.CreateBuilder<string, object>(StringComparer.Ordinal);
        foreach (string key in keys)
        {
            if (map.ContainsKey(key))
            {
                throw new InvalidOperationException("duplicate key");
            }

            map.Add(key, new object());
        }

        return map.ToImmutable();
    }
}
