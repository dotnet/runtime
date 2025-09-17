// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test119403
{
    [Fact]
    public static void TestEntryPoint()
    {
        TrashStack();
        Problem();
    }

    static void Problem()
    {
        try
        {
            SubProblem("s", null);
        }
        catch (Exception e) when (ForceGC())
        {
            Console.WriteLine("Caught");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ForceGC()
    {
        GC.Collect();
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TrashStack()
    {
        Span<int> span = stackalloc int[128];
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = 0xBADBAD;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SubProblem(string? x, string? y)
    {
        int z = y.Length;
        string s = x;
        Foo(ref s);
        return z;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Foo(ref string s)
    {
    }
}
