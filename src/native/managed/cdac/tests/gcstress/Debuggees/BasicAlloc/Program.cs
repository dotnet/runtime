// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Exercises basic object allocation patterns: objects, strings, arrays.
/// </summary>
internal static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static object AllocAndHold()
    {
        object o = new object();
        string s = "hello world";
        int[] arr = new int[] { 1, 2, 3 };
        byte[] buf = new byte[256];
        GC.KeepAlive(o);
        GC.KeepAlive(s);
        GC.KeepAlive(arr);
        GC.KeepAlive(buf);
        return o;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ManyLiveRefs()
    {
        object r0 = new object();
        object r1 = new object();
        object r2 = new object();
        object r3 = new object();
        object r4 = new object();
        object r5 = new object();
        object r6 = new object();
        object r7 = new object();
        string r8 = "live-string";
        int[] r9 = new int[10];

        GC.KeepAlive(r0); GC.KeepAlive(r1);
        GC.KeepAlive(r2); GC.KeepAlive(r3);
        GC.KeepAlive(r4); GC.KeepAlive(r5);
        GC.KeepAlive(r6); GC.KeepAlive(r7);
        GC.KeepAlive(r8); GC.KeepAlive(r9);
    }

    static int Main()
    {
        for (int i = 0; i < 2; i++)
        {
            AllocAndHold();
            ManyLiveRefs();
        }
        return 100;
    }
}
