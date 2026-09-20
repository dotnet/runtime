// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Morph distributes negation over division: NEG(DIV(a, C)) => DIV(a, -C).
// That is only valid when -C is exactly representable, which is not the case for
// C == int.MinValue/long.MinValue (negating those wraps back to themselves, silently
// dropping the negation).
public class Runtime_133517
{
    private static readonly int s_intMin = int.MinValue;
    private static readonly long s_longMin = long.MinValue;
    private static readonly nint s_nintMin = nint.MinValue;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NegDivIntMin(int x) => -(x / int.MinValue);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long NegDivLongMin(long x) => -(x / long.MinValue);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static nint NegDivNIntMin(nint x) => -(x / nint.MinValue);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NegDivMinusOne(int x) => -(x / -1);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NegDivOne(int x) => -(x / 1);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NegDivPow2(int x) => -(x / -4);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long NegDivNonPow2(long x) => -(x / 3);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NegMulIntMin(int x) => -(x * int.MinValue);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long NegMulLongMin(long x) => -(x * long.MinValue);

    // Same folds, but with the dividend coming from a static readonly field, which the JIT
    // may turn into a constant and hand to the importer/value numbering folding paths.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NegDivIntMinFolded() => -(s_intMin / int.MinValue);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long NegDivLongMinFolded() => -(s_longMin / long.MinValue);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static nint NegDivNIntMinFolded() => -(s_nintMin / nint.MinValue);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NegDivMinusOneFolded() => -(s_intMin / -1);

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(-1, NegDivIntMin(int.MinValue));
        Assert.Equal(0, NegDivIntMin(int.MaxValue));
        Assert.Equal(0, NegDivIntMin(-1));
        Assert.Equal(0, NegDivIntMin(0));

        Assert.Equal(-1L, NegDivLongMin(long.MinValue));
        Assert.Equal(0L, NegDivLongMin(long.MaxValue));
        Assert.Equal(0L, NegDivLongMin(-1));
        Assert.Equal(0L, NegDivLongMin(0));

        Assert.Equal((nint)(-1), NegDivNIntMin(nint.MinValue));
        Assert.Equal((nint)0, NegDivNIntMin(nint.MaxValue));
        Assert.Equal((nint)0, NegDivNIntMin(-1));

        Assert.Equal(-1, NegDivIntMinFolded());
        Assert.Equal(-1L, NegDivLongMinFolded());
        Assert.Equal((nint)(-1), NegDivNIntMinFolded());

        // -(x / -1) must still throw for int.MinValue.
        Assert.Throws<OverflowException>(() => { _ = NegDivMinusOne(int.MinValue); });
        Assert.Throws<OverflowException>(() => { _ = NegDivMinusOneFolded(); });
        Assert.Equal(7, NegDivMinusOne(7));

        // -(x / 1) must not start throwing for int.MinValue.
        Assert.Equal(int.MinValue, NegDivOne(int.MinValue));
        Assert.Equal(-7, NegDivOne(7));

        Assert.Equal(2, NegDivPow2(9));
        Assert.Equal(-2, NegDivPow2(-9));
        Assert.Equal(-3L, NegDivNonPow2(11));
        Assert.Equal(3L, NegDivNonPow2(-11));

        Assert.Equal(int.MinValue, NegMulIntMin(1));
        Assert.Equal(int.MinValue, NegMulIntMin(-1));
        Assert.Equal(0, NegMulIntMin(2));
        Assert.Equal(long.MinValue, NegMulLongMin(1));
        Assert.Equal(long.MinValue, NegMulLongMin(-1));
        Assert.Equal(0L, NegMulLongMin(2));
    }
}
