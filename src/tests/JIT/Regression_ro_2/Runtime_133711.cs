// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133711
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void NegativeConstants(int testCase)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Int32Log2(testCase, 42));
        Assert.Throws<ArgumentOutOfRangeException>(() => Int64Log2(testCase, 42));
        Assert.Throws<ArgumentOutOfRangeException>(() => NativeIntLog2(testCase, 42));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Int32Log2(int testCase, int value)
    {
        return testCase switch
        {
            0 => int.Log2(-1),
            1 => int.Log2(int.MinValue),
            2 => value + int.Log2(-1),
            _ => value + int.Log2(int.MinValue),
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long Int64Log2(int testCase, long value)
    {
        return testCase switch
        {
            0 => long.Log2(-1L),
            1 => long.Log2(long.MinValue),
            2 => value + long.Log2(-1L),
            _ => value + long.Log2(long.MinValue),
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static nint NativeIntLog2(int testCase, nint value)
    {
        return testCase switch
        {
            0 => nint.Log2(-1),
            1 => nint.Log2(nint.MinValue),
            2 => value + nint.Log2(-1),
            _ => value + nint.Log2(nint.MinValue),
        };
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void NonNegativeConstants()
    {
        Assert.Equal(0, int.Log2(0));
        Assert.Equal(0, int.Log2(1));
        Assert.Equal(30, int.Log2(int.MaxValue));
        Assert.Equal(0L, long.Log2(0L));
        Assert.Equal(0L, long.Log2(1L));
        Assert.Equal(62L, long.Log2(long.MaxValue));
        Assert.Equal((nint)0, nint.Log2(0));
        Assert.Equal((nint)0, nint.Log2(1));
        Assert.Equal((nint)(IntPtr.Size * 8 - 2), nint.Log2(nint.MaxValue));

        Assert.Equal(0U, uint.Log2(0U));
        Assert.Equal(0U, uint.Log2(1U));
        Assert.Equal(31U, uint.Log2(uint.MaxValue));
        Assert.Equal(0UL, ulong.Log2(0UL));
        Assert.Equal(0UL, ulong.Log2(1UL));
        Assert.Equal(63UL, ulong.Log2(ulong.MaxValue));
        Assert.Equal((nuint)0, nuint.Log2(0));
        Assert.Equal((nuint)0, nuint.Log2(1));
        Assert.Equal((nuint)(IntPtr.Size * 8 - 1), nuint.Log2(nuint.MaxValue));
    }
}
