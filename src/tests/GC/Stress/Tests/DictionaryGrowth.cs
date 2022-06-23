// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test causes a lot of ephemeral and background GCs to happen
// It's a repro case for a race condition in background GC

using System;
using System.Collections.Generic;
using System.Threading;

internal class Program
{
    static void Run()
    {
        for (int iter = 0; iter < 1000_000; iter++)
        {
            var d = new Dictionary<string, ValueTuple<int, int>>();
            for (int i = 0; i < 10 * 1000; i++)
            {
                d[i.ToString()] = new ValueTuple<int, int>(i, -1);
            }
        }
    }

    static void Main(string[] args)
    {
        int startTick = System.Environment.TickCount;
        const int threadCount = 4;
        Thread[] ta = new Thread[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            ThreadStart ts = new ThreadStart(Run);
            Thread t = new Thread(ts);
            t.Start();
            ta[i] = t;
        }
        for (int i = 0; i < threadCount; i++)
        {
            ta[i].Join();
        }
        double elapsed = 0.001*(System.Environment.TickCount - startTick);
        Console.WriteLine("{0} seconds", elapsed);
    }
}
