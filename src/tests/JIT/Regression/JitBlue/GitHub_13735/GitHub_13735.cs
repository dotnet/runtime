// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_13735
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int GetRandom()
    {
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Print(int x) { }

    static void SampleA()
    {
        var a = GetRandom();
        var b = GetRandom();
        var c = GetRandom();
        var d = GetRandom();
        var e = GetRandom();
        var f = GetRandom();
        var g = GetRandom();
        var h = GetRandom();
        for (var x = 0; x < 100; ++x)
        {
            var xa = GetRandom();
            var xb = GetRandom();
            var xc = GetRandom();
            var xd = GetRandom();
            var xe = GetRandom();
            var xf = GetRandom();
            Print(xa);
            Print(xb);
            Print(xc);
            Print(xd);
            Print(xe);
            Print(xf);
        }
        Print(a);
        Print(b);
        Print(c);
        Print(d);
        Print(e);
        Print(f);
        Print(g);
        Print(h);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        SampleA();
        return 100;
    }
}
