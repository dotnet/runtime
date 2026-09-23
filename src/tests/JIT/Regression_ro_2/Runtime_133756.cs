// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133756
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, int.MaxValue)]
    [InlineData(5, -10)]
    [InlineData(6, int.MaxValue - 14)]
    public static void TestInt32(int n, int expected)
    {
        Assert.Equal(expected, SumInt32(n));
    }

    [Theory]
    [InlineData(0, 0L)]
    [InlineData(1, 0L)]
    [InlineData(2, long.MaxValue)]
    [InlineData(5, -10L)]
    [InlineData(6, long.MaxValue - 14)]
    public static void TestInt64(int n, long expected)
    {
        Assert.Equal(expected, SumInt64(n));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SumInt32(int n)
    {
        int sum = 0;
        int i = 0;
        for (int c = 0; c < n; c++)
        {
            sum += i * 0x55555555; // Derived IV step is -1.
            sum += i << 31; // Derived IV step is int.MinValue.
            i += 3;
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long SumInt64(int n)
    {
        long sum = 0;
        long i = 0;
        for (int c = 0; c < n; c++)
        {
            sum += i * 0x5555555555555555; // Derived IV step is -1.
            sum += i << 63; // Derived IV step is long.MinValue.
            i += 3;
        }

        return sum;
    }
}
