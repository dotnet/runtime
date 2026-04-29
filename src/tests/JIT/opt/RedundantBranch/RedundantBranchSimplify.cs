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

    // NaN must be handled correctly.
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
    public static void TestFloatGeLe(double a, int expected) => Assert.Equal(expected, FloatGeLe(a));

    // Special FP values are passed via separate Fact methods because the
    // XUnitWrapperGenerator that emits the merged test runner does not
    // qualify identifiers like `NaN` in InlineData arguments.
    [Fact]
    public static void TestFloatGeLeNaN() => Assert.Equal(3, FloatGeLe(double.NaN));

    [Fact]
    public static void TestFloatGeLePositiveInfinity() => Assert.Equal(2, FloatGeLe(double.PositiveInfinity));

    [Fact]
    public static void TestFloatGeLeNegativeInfinity() => Assert.Equal(3, FloatGeLe(double.NegativeInfinity));
}