// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class OffsetLimit
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int SpanLengthMinus4(Span<int> a)
    {
        int sum = 0;
        for (int i = 0; i < a.Length - 4; i++)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int SpanLengthMinusVector128Count(Span<int> a)
    {
        int sum = 0;
        for (int i = 0; i < a.Length - Vector128<int>.Count; i += Vector128<int>.Count)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int ArrayLengthMinus3(int[] a)
    {
        int sum = 0;
        for (int i = 0; i < a.Length - 3; i++)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int ArrayLengthPlus0Stride2(int[] a)
    {
        int sum = 0;
        for (int i = 0; i < a.Length + 0; i += 2)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int VarMinusK(int[] a, int n)
    {
        int sum = 0;
        for (int i = 0; i < n - 4; i++)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int DecGtArrayLengthMinusK(int[] a, int initVal)
    {
        int sum = 0;
        for (int i = initVal; i > a.Length - 5; i--)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int IncLeSpanLengthMinusK(Span<int> a)
    {
        int sum = 0;
        for (int i = 0; i <= a.Length - 5; i += 2)
        {
            sum += a[i];
        }
        return sum;
    }

    static int[] MakeArray(int n)
    {
        int[] a = new int[n];
        for (int i = 0; i < n; i++) a[i] = i + 1;
        return a;
    }

    static int ExpectedInc(int from, int toExclusive, int stride, int[] src)
    {
        int sum = 0;
        for (int i = from; i < toExclusive; i += stride) sum += src[i];
        return sum;
    }

    static int ExpectedIncLe(int from, int toInclusive, int stride, int[] src)
    {
        int sum = 0;
        for (int i = from; i <= toInclusive; i += stride) sum += src[i];
        return sum;
    }

    static int ExpectedDecGt(int from, int toExclusive, int stride, int[] src)
    {
        int sum = 0;
        for (int i = from; i > toExclusive; i -= stride) sum += src[i];
        return sum;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(100)]
    public static void SpanLengthMinus4Test(int n)
    {
        int[] a = MakeArray(n);
        int got = SpanLengthMinus4(a);
        int want = ExpectedInc(0, Math.Max(0, n - 4), 1, a);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(100)]
    public static void SpanLengthMinusVector128CountTest(int n)
    {
        int[] a = MakeArray(n);
        int got = SpanLengthMinusVector128Count(a);
        int stride = Vector128<int>.Count;
        int want = ExpectedInc(0, Math.Max(0, n - stride), stride, a);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(50)]
    [InlineData(100)]
    public static void ArrayLengthMinus3Test(int n)
    {
        int[] a = MakeArray(n);
        int got = ArrayLengthMinus3(a);
        int want = ExpectedInc(0, Math.Max(0, n - 3), 1, a);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(50)]
    [InlineData(99)]
    public static void ArrayLengthPlus0Stride2Test(int n)
    {
        int[] a = MakeArray(n);
        int got = ArrayLengthPlus0Stride2(a);
        int want = ExpectedInc(0, n, 2, a);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(3, 100)]
    [InlineData(50, 100)]
    [InlineData(100, 100)]
    public static void VarMinusKTest(int n, int len)
    {
        int[] a = MakeArray(len);
        int got = VarMinusK(a, n);
        int want = ExpectedInc(0, Math.Max(0, n - 4), 1, a);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(99, 100)]
    [InlineData(50, 100)]
    [InlineData(5, 10)]
    public static void DecGtArrayLengthMinusKTest(int initVal, int len)
    {
        int[] a = MakeArray(len);
        int got = DecGtArrayLengthMinusK(a, initVal);
        int want = ExpectedDecGt(initVal, len - 5, 1, a);
        Assert.Equal(want, got);
    }

    // When the array is shorter than the constant offset, `a.Length - K` is
    // negative and the decreasing loop steps past index 0. The source loop
    // throws IndexOutOfRangeException; the `arr.Length + offset >= 0` guard
    // in loop cloning must keep the fast (bounds-check-free) clone from
    // silently accepting these accesses.
    [Theory]
    [InlineData(2, 3)]
    [InlineData(0, 1)]
    public static void DecGtArrayLengthMinusKShortArrayTest(int initVal, int len)
    {
        int[] a = MakeArray(len);
        Assert.Throws<IndexOutOfRangeException>(() => DecGtArrayLengthMinusK(a, initVal));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(99)]
    [InlineData(100)]
    public static void IncLeSpanLengthMinusKTest(int n)
    {
        int[] a = MakeArray(n);
        int got = IncLeSpanLengthMinusK(a);
        int want = ExpectedIncLe(0, n - 5, 2, a);
        Assert.Equal(want, got);
    }
}
