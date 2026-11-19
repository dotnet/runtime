// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_137675
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int Test(int[][] rows, long k, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += rows[(int)k][i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int TestSpan(Span<int> span, long k, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += span[(int)k + i];
        }
        return sum;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int[][] rows = { new int[10] };
        Assert.Equal(0, Test(rows, 1L << 32, 10));
        Assert.Equal(0, TestSpan(new int[10], 1L << 32, 10));
    }
}
