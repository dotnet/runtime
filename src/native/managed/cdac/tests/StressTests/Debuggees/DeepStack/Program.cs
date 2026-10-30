// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Exercises deep recursion with live GC references at each frame level.
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
    static void NestedWithMultipleRefs(int depth)
    {
        object a = new object();
        string b = $"depth-{depth}";
        int[] c = new int[depth + 1];
        if (depth > 0)
            NestedWithMultipleRefs(depth - 1);
        GC.KeepAlive(a);
        GC.KeepAlive(b);
        GC.KeepAlive(c);
    }

    static int Main()
    {
        for (int i = 0; i < 2; i++)
        {
            NestedCall(10);
            NestedWithMultipleRefs(8);
        }
        return 100;
    }
}
