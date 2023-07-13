// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// PGO enables an invariant GDV type test in a loop.
// We then clone the loop based on this test.
//
// DOTNET_TieredPGO=1

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

interface I
{
    public int F(int x, int y);
}

class Add : I 
{
    int I.F(int x, int y) => x + y;
}

class Mul : I
{
    int I.F(int x, int y) => x * y;
}

public class CloningForTypeTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static int BothTypeAndArray(I m, int[] xs, int[] ys, int from, int to)
    {
        int r = 0;

        // Cloning conditions for this loop should include a type test
        //
        for (int i = from; i < to; i++)
        {
            r += m.F(xs[i], ys[i]);
        }
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static int JustType(I m, int from, int to)
    {
        int r = 0;

        // Cloning conditions for this loop should only include a type test
        //
        for (int i = from; i < to; i++)
        {
            r += m.F(1, 1);
        }
        return r;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int[] xs = new int[] { 1, 2, 3, 4 };
        int[] ys = new int[] { 4, 3, 2, 1 };
        I m = new Add();

        int r0 = 0;
        int r1 = 0;

        for (int i = 0; i < 30; i++)
        {
            r0 += BothTypeAndArray(m, xs, ys, 0, 3);
            r1 += JustType(m, 0, 3);
            Thread.Sleep(15);
        }

        Thread.Sleep(50);

        for (int i = 0; i < 70; i++)
        {
            r0 += BothTypeAndArray(m, xs, ys, 0, 3);
            r1 += JustType(m, 0, 3);
        }
        
        return (r0 + r1) / 21;
    }
}
