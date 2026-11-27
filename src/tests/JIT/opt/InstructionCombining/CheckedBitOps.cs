// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Correctness coverage for the xarch BMI lowerings when the `- 1` subtraction is a checked
// (overflowing) operation. `blsr` (AND(X, SUB(X, 1))), `blsmsk` (XOR(X, SUB(X, 1))) and `bzhi`
// (AND(X, SUB(LSH(1, Y), 1))) all fold the subtraction away, so they must not fire when it is a
// checked subtraction that would throw -- otherwise the observable OverflowException is dropped.
public static class CheckedBitOps
{
    // blsr: x & (x - 1). Overflows for x == INT_MIN / LONG_MIN.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ResetLowestChecked(int x) => x & checked(x - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ResetLowestChecked(long x) => x & checked(x - 1);

    // blsmsk: x ^ (x - 1). Overflows for x == INT_MIN / LONG_MIN.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MaskUpToChecked(int x) => x ^ checked(x - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long MaskUpToChecked(long x) => x ^ checked(x - 1);

    // bzhi: x & ((1 << y) - 1). Overflows when (1 << y) == INT_MIN (y == 31) / LONG_MIN (y == 63).
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ZeroHighChecked(int x, int y) => x & checked((1 << y) - 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ZeroHighChecked(long x, int y) => x & checked((1L << y) - 1);

    [Fact]
    public static void Test()
    {
        Assert.Throws<OverflowException>(() => ResetLowestChecked(int.MinValue));
        Assert.Throws<OverflowException>(() => ResetLowestChecked(long.MinValue));

        Assert.Throws<OverflowException>(() => MaskUpToChecked(int.MinValue));
        Assert.Throws<OverflowException>(() => MaskUpToChecked(long.MinValue));

        Assert.Throws<OverflowException>(() => ZeroHighChecked(0x1234, 31));
        Assert.Throws<OverflowException>(() => ZeroHighChecked(0x1234L, 63));

        // The non-overflowing inputs must still compute the expected result.
        Assert.Equal(0b1100 & 0b1011, ResetLowestChecked(0b1100));
        Assert.Equal(0b1100 ^ 0b1011, MaskUpToChecked(0b1100));
        Assert.Equal(0xFF & ((1 << 4) - 1), ZeroHighChecked(0xFF, 4));
    }
}
