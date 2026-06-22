// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// Exercises concurrent threads with GC references, exercising multi-threaded
/// stack walks and GC ref enumeration.
/// </summary>
internal static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void NestedCall(int depth)
    {
        object o = new object();
        if (depth > 0)
            NestedCall(depth - 1);
        GC.KeepAlive(o);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThreadWork(int id)
    {
        object threadLocal = new object();
        string threadName = $"thread-{id}";
        NestedCall(5);
        GC.KeepAlive(threadLocal);
        GC.KeepAlive(threadName);
    }

    static int Main()
    {
        for (int iteration = 0; iteration < 2; iteration++)
        {
            ManualResetEventSlim ready = new ManualResetEventSlim(false);
            ManualResetEventSlim go = new ManualResetEventSlim(false);
            Thread t = new Thread(() =>
            {
                ready.Set();
                go.Wait();
                ThreadWork(1);
            });
            t.Start();
            ready.Wait();
            go.Set();
            ThreadWork(0);
            t.Join();
        }
        return 100;
    }
}
