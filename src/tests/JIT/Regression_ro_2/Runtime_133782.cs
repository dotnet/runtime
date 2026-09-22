// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133782
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(124L, MultiplyInt64(100, 3));
        Assert.Equal(124L, MultiplyReversed(100, 3));
        Assert.Equal(124, MultiplyInt32(100, 3));
        Assert.Equal(124UL, MultiplyUInt64(100, 3));

        // Only the inner multiply is checked; the outer multiply may wrap.
        Assert.Equal(100L, MultiplyInt64(100, 1L << 61));
        Assert.Equal(100, MultiplyInt32(100, 1 << 29));
        Assert.Equal(100UL, MultiplyUInt64(100, 1UL << 62));

        Assert.Throws<OverflowException>(() => MultiplyInt64(100, 1L << 62));
        Assert.Throws<OverflowException>(() => MultiplyInt64(100, long.MinValue));
        Assert.Throws<OverflowException>(() => MultiplyReversed(100, 1L << 61));
        Assert.Throws<OverflowException>(() => MultiplyInt32(100, 1 << 30));
        Assert.Throws<OverflowException>(() => MultiplyUInt64(100, 1UL << 63));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long MultiplyInt64(long b, long i) => b + checked(i * 2) * 4;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long MultiplyReversed(long b, long i) => checked(i * 4) * 2 + b;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int MultiplyInt32(int b, int i) => b + checked(i * 2) * 4;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong MultiplyUInt64(ulong b, ulong i) => b + checked(i * 2) * 4;
}
