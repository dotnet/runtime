// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133560
{
    [Fact]
    public static void TestEntryPoint()
    {
        long value = Environment.TickCount64;
        ulong unsignedValue = (ulong)value;

        Assert.Equal(1L, PopCountInt64(value));
        Assert.Equal(55L, LeadingZeroCountInt64(value));
        Assert.Equal(8L, TrailingZeroCountInt64(value));
        Assert.Equal(1UL, PopCountUInt64(unsignedValue));
        Assert.Equal(55UL, LeadingZeroCountUInt64(unsignedValue));
        Assert.Equal(8UL, TrailingZeroCountUInt64(unsignedValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long PopCountInt64(long value) => long.PopCount((value - value) + 256);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long LeadingZeroCountInt64(long value) => long.LeadingZeroCount((value - value) + 256);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long TrailingZeroCountInt64(long value) => long.TrailingZeroCount((value - value) + 256);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong PopCountUInt64(ulong value) => ulong.PopCount((value - value) + 256);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong LeadingZeroCountUInt64(ulong value) => ulong.LeadingZeroCount((value - value) + 256);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong TrailingZeroCountUInt64(ulong value) => ulong.TrailingZeroCount((value - value) + 256);
}
