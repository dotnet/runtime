// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class SpanNonUnitStride
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int SumIncLtStride2(Span<int> a)
    {
        int sum = 0;
        for (int i = 0; i < a.Length; i += 2)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int SumIncLtStride3(Span<int> a)
    {
        int sum = 0;
        for (int i = 0; i < a.Length; i += 3)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int SumIncLeStride2(Span<int> a, int n)
    {
        int sum = 0;
        for (int i = 0; i <= n; i += 2)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int SumDecGtStride2(Span<int> a, int n)
    {
        int sum = 0;
        for (int i = n; i > 0; i -= 2)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int SumDecGeStride3(Span<int> a, int n)
    {
        int sum = 0;
        for (int i = n; i >= 0; i -= 3)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int SumIncLtStride2ConstLimit(Span<int> a)
    {
        int sum = 0;
        for (int i = 0; i < 50; i += 2)
        {
            sum += a[i];
        }
        return sum;
    }

    static int[] MakeArray(int n)
    {
        int[] a = new int[n];
        for (int i = 0; i < n; i++)
        {
            a[i] = i + 1;
        }
        return a;
    }

    static int ExpectedIncLt(int n, int stride)
    {
        int sum = 0;
        for (int i = 0; i < n; i += stride)
        {
            sum += i + 1;
        }
        return sum;
    }

    static int ExpectedIncLe(int n, int stride)
    {
        int sum = 0;
        for (int i = 0; i <= n; i += stride)
        {
            sum += i + 1;
        }
        return sum;
    }

    static int ExpectedDecGt(int n, int stride)
    {
        int sum = 0;
        for (int i = n; i > 0; i -= stride)
        {
            sum += i + 1;
        }
        return sum;
    }

    static int ExpectedDecGe(int n, int stride)
    {
        int sum = 0;
        for (int i = n; i >= 0; i -= stride)
        {
            sum += i + 1;
        }
        return sum;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(50)]
    [InlineData(99)]
    [InlineData(100)]
    public static void IncLtStride2(int n)
    {
        Span<int> span = MakeArray(n);
        int got = SumIncLtStride2(span);
        int want = ExpectedIncLt(n, 2);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(50)]
    [InlineData(99)]
    [InlineData(100)]
    public static void IncLtStride3(int n)
    {
        Span<int> span = MakeArray(n);
        int got = SumIncLtStride3(span);
        int want = ExpectedIncLt(n, 3);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(1, 100)]
    [InlineData(48, 100)]
    [InlineData(99, 100)]
    public static void IncLeStride2(int n, int len)
    {
        Span<int> span = MakeArray(len);
        int got = SumIncLeStride2(span, n);
        int want = ExpectedIncLe(n, 2);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 100)]
    [InlineData(50, 100)]
    [InlineData(99, 100)]
    public static void DecGtStride2(int n, int len)
    {
        Span<int> span = MakeArray(len);
        int got = SumDecGtStride2(span, n);
        int want = ExpectedDecGt(n, 2);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(1, 100)]
    [InlineData(50, 100)]
    [InlineData(99, 100)]
    public static void DecGeStride3(int n, int len)
    {
        Span<int> span = MakeArray(len);
        int got = SumDecGeStride3(span, n);
        int want = ExpectedDecGe(n, 3);
        Assert.Equal(want, got);
    }

    [Fact]
    public static void IncLtStride2ConstLimit()
    {
        Span<int> span = MakeArray(60);
        int got = SumIncLtStride2ConstLimit(span);
        int want = ExpectedIncLt(50, 2);
        Assert.Equal(want, got);
    }
}
