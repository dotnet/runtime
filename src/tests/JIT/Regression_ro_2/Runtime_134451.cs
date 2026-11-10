// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134451
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Test(new int[101], true));
    }

    // "x" wraps around, so "j += x" is not monotonic and "j" becomes negative.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int[] a, bool flag)
    {
        int j = 0, x = int.MaxValue - 10, sum = 0;
        if (a.Length != 101)
            return 0;
        while (j < 101)
        {
            if (flag)
                sum += a[j];
            x++;
            if (x >= int.MinValue + 1 && x <= 10)
                j += x;
        }
        return sum;
    }
}
