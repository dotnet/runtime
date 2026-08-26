// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

// Regression test for https://github.com/dotnet/runtime/issues/126903
// Verifies that aggressive GC does not corrupt the heap under emulated large pages.
// The large pages emulation mode (DOTNET_GCLargePages=2) exercises the same
// GC code paths as real large pages without requiring OS-level large page setup.
public class AggressiveCollectLargePages
{
    const int DurationMs = 3000;
    const int WriterCount = 4;

    [Fact]
    public static int TestEntryPoint()
    {
        var dict = new ConcurrentDictionary<int, byte[]>();
        var cts = new CancellationTokenSource(DurationMs);
        var token = cts.Token;
        int errors = 0;

        Thread[] writers = new Thread[WriterCount];
        for (int t = 0; t < WriterCount; t++)
        {
            int tid = t;
            writers[t] = new Thread(() =>
            {
                try
                {
                    int i = tid * 1_000_000;
                    while (!token.IsCancellationRequested)
                    {
                        dict[i] = new byte[100];
                        i++;
                        if ((i % 1000) == 0)
                        {
                            dict.Clear();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Writer {tid} caught: {ex.GetType().Name}: {ex.Message}");
                    Interlocked.Increment(ref errors);
                }
            });
            writers[t].IsBackground = true;
            writers[t].Start();
        }

        Thread gcThread = new Thread(() =>
        {
            while (!token.IsCancellationRequested)
            {
                CreateGarbage();
                GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);
                Thread.Sleep(50);
            }
        });
        gcThread.IsBackground = true;
        gcThread.Start();

        gcThread.Join();
        for (int t = 0; t < WriterCount; t++)
        {
            writers[t].Join();
        }

        if (errors > 0)
        {
            Console.WriteLine($"FAIL: {errors} writer(s) hit exceptions (heap corruption).");
            return 101;
        }

        Console.WriteLine("PASS: No heap corruption detected.");
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CreateGarbage()
    {
        byte[][] small = new byte[500][];
        for (int i = 0; i < small.Length; i++)
        {
            small[i] = new byte[4000];
        }
        byte[] large = new byte[8 * 1024 * 1024];
        GC.KeepAlive(small);
        GC.KeepAlive(large);
    }
}
