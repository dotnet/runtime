// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Issues with stack spill ordering around some GDVs
// Compile with <DebugType>None</DebugType>

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

class P
{
  virtual public (double x, double y) XY() => (0, 0);
}

class P1 : P
{
  override public (double x, double y) XY() => (1, 2);
}

public class Runtime_95349
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Problem(P p, int n, (double x, double y) tuple)
    {
        int wn = 0;
        for (int i = 0; i < n; i++)
        {
            (double x, double y) tupleTmp = tuple;
            tuple = p.XY();
            (_, double f) = tupleTmp;
            wn = Wn(wn, f, tuple.y);
        }

        return wn;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Wn(int wn, double f, double y)
    {
        wn += (f == -1) ? 1 : 0;
        return wn;
    }

    [Fact]
    public static void Test()
    {
        P p = new P1();
        int n = 100_000;
        for (int i = 0; i < 100; i++)
        {
            _ = Problem(p, n, (-1, -1));
            Thread.Sleep(30);
        }

        int r = Problem(p, n, (-1, -1));
        Console.WriteLine($"r = {r} (expected 1)");
        Assert.Equal(1, r);
    }
}
