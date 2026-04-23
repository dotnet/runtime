// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// These cases exercise the relop simplification path in redundant branch
// elimination: a dominating relop does not directly imply the dominated
// relop, but AND of the two path predicates simplifies to a single relop
// (or the dominated relop can be reversed/rewritten).
public class RedundantBranchSimplify
{
    // if (a >= 100) { if (a <= 100) return 1; } => inner becomes (a == 100)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GeLe(int a)
    {
        if (a >= 100)
        {
            if (a <= 100)
            {
                return 1;
            }
            return 2;
        }
        return 3;
    }

    [Theory]
    [InlineData(99, 3)]
    [InlineData(100, 1)]
    [InlineData(101, 2)]
    public static void TestGeLe(int a, int expected) => Assert.Equal(expected, GeLe(a));

    // Shared successor is the TRUE successor of the dominated block: exercises
    // the polarity-reversal path (ReverseRelop when sharedSuccessor == blockTrueSucc).
    // Equivalent to `a >= 0` implemented as `a == 0 || a > 0`.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GreaterThanOrEqualZero(int a) => a == 0 || a > 0;

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(int.MinValue, false)]
    [InlineData(int.MaxValue, true)]
    public static void TestGreaterThanOrEqualZero(int a, bool expected) =>
        Assert.Equal(expected, GreaterThanOrEqualZero(a));

    // Operand-swapped variant: inner uses (100 <= a) rather than (a >= 100)
    // to exercise the swap path in the simplification.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GeLeSwapped(int a)
    {
        if (100 <= a)
        {
            if (a <= 100)
            {
                return 1;
            }
            return 2;
        }
        return 3;
    }

    [Theory]
    [InlineData(99, 3)]
    [InlineData(100, 1)]
    [InlineData(101, 2)]
    public static void TestGeLeSwapped(int a, int expected) => Assert.Equal(expected, GeLeSwapped(a));

    // Unsigned NE + LE: a regression guard for the unsigned relop AND table.
    // if (a != b) { if (a <= b) return 1; } where LE is unsigned-like (via uint).
    // Correct joint: a <_un b. If the simplification incorrectly yielded a <=_un b,
    // the a == b path would return the wrong answer.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NeLeUnsigned(uint a, uint b)
    {
        if (a != b)
        {
            if (a <= b)
            {
                return 1;
            }
            return 2;
        }
        return 3;
    }

    [Theory]
    [InlineData(0u, 0u, 3)]
    [InlineData(5u, 5u, 3)]
    [InlineData(4u, 5u, 1)]
    [InlineData(6u, 5u, 2)]
    [InlineData(0u, uint.MaxValue, 1)]
    [InlineData(uint.MaxValue, 0u, 2)]
    public static void TestNeLeUnsigned(uint a, uint b, int expected) =>
        Assert.Equal(expected, NeLeUnsigned(a, b));

    // Unsigned NE + GE analog.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NeGeUnsigned(uint a, uint b)
    {
        if (a != b)
        {
            if (a >= b)
            {
                return 1;
            }
            return 2;
        }
        return 3;
    }

    [Theory]
    [InlineData(0u, 0u, 3)]
    [InlineData(5u, 5u, 3)]
    [InlineData(4u, 5u, 2)]
    [InlineData(6u, 5u, 1)]
    [InlineData(0u, uint.MaxValue, 2)]
    [InlineData(uint.MaxValue, 0u, 1)]
    public static void TestNeGeUnsigned(uint a, uint b, int expected) =>
        Assert.Equal(expected, NeGeUnsigned(a, b));

    // Floating-point guard: NaN must be handled correctly. With NaN, both
    // `a >= 0.0` and `a <= 0.0` are false, so the inner should return 2 on the
    // NaN path. An unsound simplification (e.g., treating `GE & LE` as `EQ`
    // without accounting for unordered semantics) would miscompile this.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FloatGeLe(double a)
    {
        if (a >= 0.0)
        {
            if (a <= 0.0)
            {
                return 1;
            }
            return 2;
        }
        return 3;
    }

    [Theory]
    [InlineData(-1.0, 3)]
    [InlineData(0.0, 1)]
    [InlineData(1.0, 2)]
    [InlineData(double.NaN, 3)]
    [InlineData(double.PositiveInfinity, 2)]
    [InlineData(double.NegativeInfinity, 3)]
    public static void TestFloatGeLe(double a, int expected) => Assert.Equal(expected, FloatGeLe(a));
}