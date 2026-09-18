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
        Assert.Equal(10, Signed(arr, 4, 2, true));
        Assert.Equal(14, Signed(arr, 4, 4, false));
        Assert.Equal(-1, Signed(arr, 4, 8, false));
        Assert.Throws<IndexOutOfRangeException>(() => Signed(arr, -5, -3, false));
        Assert.Throws<IndexOutOfRangeException>(() => SignedOffset(arr, -5, -3, false));
        Assert.Throws<IndexOutOfRangeException>(() => SignedReversed(arr, -5, -3, false));
        Assert.Throws<IndexOutOfRangeException>(() => Equal(arr, -3, -3, false));
        Assert.Throws<IndexOutOfRangeException>(() => Unsigned(arr, -3, -5, false));

        Assert.Equal(0, Subtract(0, 0));
        Assert.Equal(7, Subtract(10, 3));
        Assert.Equal(int.MaxValue, Subtract(int.MaxValue, 0));
        Assert.Equal(0, Subtract(int.MaxValue, int.MaxValue));
        Assert.Equal(-1, Subtract(10, 11));
        Assert.Equal(-1, Subtract(10, -1));
        Assert.Equal(-1, Subtract(int.MaxValue, int.MinValue));
        Assert.Equal(-1, Subtract(int.MinValue, int.MaxValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Signed(int[] arr, int n, int k, bool cond)
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

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SignedOffset(int[] arr, int n, int k, bool cond)
    {
        if (cond)
        {
            int[] tmp = new int[n];
            s_sink = tmp[0];
        }

        int i = 0;
        if (k >= n - 1)
        {
            i = k + 1;
        }

        return i < arr.Length ? arr[i] : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SignedReversed(int[] arr, int n, int k, bool cond)
    {
        if (cond)
        {
            int[] tmp = new int[n];
            s_sink = tmp[0];
            int[] other = new int[k];
            s_sink = other[0];
        }

        int i = 0;
        if (k >= n)
        {
            i = k;
        }

        return i < arr.Length ? arr[i] : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Equal(int[] arr, int n, int k, bool cond)
    {
        if (cond)
        {
            int[] tmp = new int[n];
            s_sink = tmp[0];
        }

        int i = 0;
        if (k == n)
        {
            i = k;
        }

        return i < arr.Length ? arr[i] : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Unsigned(int[] arr, int n, int k, bool cond)
    {
        if (cond)
        {
            int[] tmp = new int[n];
            s_sink = tmp[0];
        }

        int i = 0;
        if ((uint)k < (uint)n)
        {
            i = k;
        }

        return i < arr.Length ? arr[i] : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Subtract(int length, int start)
    {
        if (length < 0 || (uint)start > (uint)length)
        {
            return -1;
        }

        int remaining = length - start;
        return remaining < 0 ? -2 : remaining;
    }
}
