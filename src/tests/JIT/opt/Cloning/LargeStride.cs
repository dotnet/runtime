// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class LargeStride
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int ArrayLengthStride60(int[] a)
    {
        int sum = 0;
        for (int i = 0; i < a.Length; i += 60)
            sum += a[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int ArrayLengthStride256(int[] a)
    {
        int sum = 0;
        for (int i = 0; i < a.Length; i += 256)
            sum += a[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int VarLimitStride100(int[] a, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i += 100)
            sum += a[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int ConstLimitStride200(int[] a)
    {
        int sum = 0;
        for (int i = 0; i < 1000; i += 200)
            sum += a[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int DecGtStride100(int[] a, int n)
    {
        int sum = 0;
        for (int i = n; i > 0; i -= 100)
            sum += a[i];
        return sum;
    }

    static int[] Make(int n)
    {
        int[] a = new int[n];
        for (int i = 0; i < n; i++) a[i] = i + 1;
        return a;
    }

    static int ExpectedIncLt(int n, int stride)
    {
        int sum = 0;
        for (int i = 0; i < n; i += stride) sum += i + 1;
        return sum;
    }

    static int ExpectedDecGt(int n, int stride)
    {
        int sum = 0;
        for (int i = n; i > 0; i -= stride) sum += i + 1;
        return sum;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(59)]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(300)]
    public static void ArrayLengthStride60Test(int n)
    {
        int[] a = Make(n);
        Assert.Equal(ExpectedIncLt(n, 60), ArrayLengthStride60(a));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(1000)]
    public static void ArrayLengthStride256Test(int n)
    {
        int[] a = Make(n);
        Assert.Equal(ExpectedIncLt(n, 256), ArrayLengthStride256(a));
    }

    [Theory]
    [InlineData(0, 1000)]
    [InlineData(99, 1000)]
    [InlineData(100, 1000)]
    [InlineData(500, 1000)]
    public static void VarLimitStride100Test(int n, int len)
    {
        int[] a = Make(len);
        Assert.Equal(ExpectedIncLt(n, 100), VarLimitStride100(a, n));
    }

    [Fact]
    public static void ConstLimitStride200Test()
    {
        int[] a = Make(1000);
        Assert.Equal(ExpectedIncLt(1000, 200), ConstLimitStride200(a));
    }

    [Theory]
    [InlineData(1, 1000)]
    [InlineData(99, 1000)]
    [InlineData(100, 1000)]
    [InlineData(500, 1000)]
    public static void DecGtStride100Test(int n, int len)
    {
        int[] a = Make(len);
        Assert.Equal(ExpectedDecGt(n, 100), DecGtStride100(a, n));
    }
}
