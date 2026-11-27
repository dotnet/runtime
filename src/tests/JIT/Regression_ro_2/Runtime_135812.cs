// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_135812;

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Jump threading in redundant branch opts bypasses a block with a phi for y, leaving
// stale SSA that strength reduction must not treat as loop-invariant.
public static class Runtime_135812
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int Test(int[] a, int k, int m, bool c, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            int y;
            if (c)
                y = k;
            else
                y = m;

            if (c)
                sum += a[2 * i + y];
        }
        return sum;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int[] a = new int[200];
        for (int i = 0; i < a.Length; i++)
        {
            a[i] = i;
        }

        Assert.Equal(2550, Test(a, 2, 5, true, 50));
    }
}
