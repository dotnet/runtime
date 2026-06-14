// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class LongInductionVar
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long SumLongIvArrLen(int[] a)
    {
        long sum = 0;
        for (long i = 0; i < a.Length; i++)
        {
            sum += a[(int)i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long SumLongIvLongLen(int[] a)
    {
        long sum = 0;
        for (long i = 0; i < a.LongLength; i++)
        {
            sum += a[(int)i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long SumLongIvParamLimit(int[] a, long limit)
    {
        long sum = 0;
        for (long i = 0; i < limit; i++)
        {
            sum += a[(int)i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long SumLongIvConstLimit(int[] a)
    {
        long sum = 0;
        for (long i = 0; i < 50L; i++)
        {
            sum += a[(int)i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long SumLongIvArrLenMinus3(int[] a)
    {
        long sum = 0;
        for (long i = 0; i < a.Length - 3; i++)
        {
            sum += a[(int)i];
        }
        return sum;
    }

    static int[] MakeArray(int n)
    {
        var a = new int[n];
        for (int i = 0; i < n; i++)
        {
            a[i] = i + 1;
        }
        return a;
    }

    static long ExpectedSum(int len)
    {
        long sum = 0;
        for (int i = 0; i < len; i++)
        {
            sum += i + 1;
        }
        return sum;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public static void LongIvArrLen(int n)
    {
        int[] a = MakeArray(n);
        long got = SumLongIvArrLen(a);
        long want = ExpectedSum(n);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public static void LongIvLongLen(int n)
    {
        int[] a = MakeArray(n);
        long got = SumLongIvLongLen(a);
        long want = ExpectedSum(n);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public static void LongIvParamLimit(int n)
    {
        int[] a = MakeArray(n);
        long got = SumLongIvParamLimit(a, n);
        long want = ExpectedSum(n);
        Assert.Equal(want, got);
    }

    [Fact]
    public static void LongIvConstLimit()
    {
        int[] a = MakeArray(60);
        long got = SumLongIvConstLimit(a);
        long want = ExpectedSum(50);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(100)]
    public static void LongIvArrLenMinus3(int n)
    {
        int[] a = MakeArray(n);
        long got = SumLongIvArrLenMinus3(a);
        long want = ExpectedSum(n - 3);
        Assert.Equal(want, got);
    }
}
