// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134455
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Test(-1, 1));
        Assert.Equal(0, Test(-1, 0));
        Assert.Equal(0, Test(50, 0));
    }

    // "x >>> (y & 3)" is a large positive value for negative "x".
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int x, int y)
    {
        int[] a = new int[51];
        if (x <= 50)
        {
            int v = x >>> (y & 3);
            if (v >= 0)
                return a[v];
        }
        return 0;
    }
}
