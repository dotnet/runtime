// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133762
{
    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public static void TestLocalLimit(int i)
    {
        int[] a = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        Assert.Throws<IndexOutOfRangeException>(() => LocalLimit(a, i, 30));
        Assert.Equal(55, LocalLimit(a, 0, 30));
        Assert.Equal(0, LocalLimit(a, 30, 30));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public static void TestArrayLengthLimit(int i)
    {
        int[] a = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int[] original = new int[30];
        Assert.Throws<IndexOutOfRangeException>(() => ArrayLengthLimit(original, a, i));
        Assert.Equal(55, ArrayLengthLimit(original, a, 0));
        Assert.Equal(0, ArrayLengthLimit(original, a, 30));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public static void TestIterator(int i)
    {
        int[] a = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        Assert.Throws<IndexOutOfRangeException>(() => Iterator(a, 0, i));
        Assert.Equal(55, Iterator(a, 0, 0));
        Assert.Equal(0, Iterator(a, 10, 0));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int LocalLimit(int[] a, int i, int n)
    {
        int sum = 0;
        if (i < n)
        {
            n = a.Length;
            do
            {
                sum += a[i];
                i++;
            } while (i < n);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int ArrayLengthLimit(int[] a, int[] replacement, int i)
    {
        int sum = 0;
        if (i < a.Length)
        {
            a = replacement;
            do
            {
                sum += a[i];
                i++;
            } while (i < a.Length);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Iterator(int[] a, int i, int replacement)
    {
        int sum = 0;
        if (i < 10)
        {
            i = replacement;
            do
            {
                sum += a[i];
                i++;
            } while (i < 10);
        }
        return sum;
    }
}
