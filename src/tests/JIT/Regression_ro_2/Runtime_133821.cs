// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133821
{
    private static int s_sink;

    [Fact]
    public static void TestEntryPoint()
    {
        int[] arr = { 10, 11, 12, 13, 14, 15, 16, 17 };
        Assert.Equal(10, Test(arr, 4, 2, true));
        Assert.Equal(14, Test(arr, 4, 4, false));
        Assert.Equal(-1, Test(arr, 4, 8, false));
        Assert.Throws<IndexOutOfRangeException>(() => Test(arr, -5, -3, false));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int[] arr, int n, int k, bool cond)
    {
        if (cond)
        {
            int[] tmp = new int[n];
            s_sink = tmp[0];
        }

        int i = 0;
        if (k >= n)
        {
            i = k;
        }

        return i < arr.Length ? arr[i] : -1;
    }
}
