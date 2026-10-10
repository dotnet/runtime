// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests for merging same-direction constant compares, e.g. "x > 10 && x > 100" -> "x > 100"
// in OptBoolsDsc::optOptimizeRangeTests.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class SameDirectionRangeTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool GtGt(int x) => x > 10 && x > 100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool GtGtRev(int x) => x > 100 && x > 10;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool LtLt(int x) => x < 50 && x < 20;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool LeLt(int x) => x <= 50 && x < 51;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool GeGt(int x) => x >= 5 && x > 4;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool GtGtOr(int x) => x > 10 || x > 100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool LtLtOr(int x) => x < 50 || x < 20;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Swapped(int x) => 10 < x && 100 < x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool GtGtMax(int x) => x > int.MaxValue - 1 && x > 5;

    // Negative/zero-adjacent constants are not merged yet; this just checks correctness is preserved.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool LtZero(int x) => x < 0 && x < 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool UGtGt(uint x) => x > 10u && x > 100u;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ULtLt(uint x) => x < 50u && x < 20u;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool LongGtGt(long x) => x > 10 && x > 100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Branchy(int x)
    {
        if (x > 10 && x > 100)
        {
            return 1;
        }
        return 0;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int[] values = { int.MinValue, -1000, -1, 0, 1, 4, 5, 6, 9, 10, 11, 19, 20, 21, 49, 50, 51, 99, 100, 101,
                         int.MaxValue - 2, int.MaxValue - 1, int.MaxValue };

        foreach (int x in values)
        {
            uint ux = (uint)x;
            long lx = x;

            Assert.Equal(x > 10 && x > 100, GtGt(x));
            Assert.Equal(x > 100 && x > 10, GtGtRev(x));
            Assert.Equal(x < 50 && x < 20, LtLt(x));
            Assert.Equal(x <= 50 && x < 51, LeLt(x));
            Assert.Equal(x >= 5 && x > 4, GeGt(x));
            Assert.Equal(x > 10 || x > 100, GtGtOr(x));
            Assert.Equal(x < 50 || x < 20, LtLtOr(x));
            Assert.Equal(10 < x && 100 < x, Swapped(x));
            Assert.Equal(x > int.MaxValue - 1 && x > 5, GtGtMax(x));
            Assert.Equal(x < 0 && x < 5, LtZero(x));
            Assert.Equal(ux > 10u && ux > 100u, UGtGt(ux));
            Assert.Equal(ux < 50u && ux < 20u, ULtLt(ux));
            Assert.Equal(lx > 10 && lx > 100, LongGtGt(lx));
            Assert.Equal((x > 10 && x > 100) ? 1 : 0, Branchy(x));
        }
    }
}
