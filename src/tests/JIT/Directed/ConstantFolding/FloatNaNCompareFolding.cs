// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

// Regression coverage for folding floating-point comparisons against a constant
// NaN in gtFoldExprSpecialFloating. Every ordered comparison with NaN is false and
// '!=' is true. The other operand is an opaque argument, so the fold goes through
// the special (variable operand + constant NaN) path rather than full constant
// folding. The branch helpers additionally exercise the unordered relop imports
// (bne.un / bge.un / ...) that carry GTF_RELOP_NAN_UN.
public class FloatNaNCompareFolding
{
    [Theory]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public static void Double(double x)
    {
        Assert.False(x == double.NaN);
        Assert.True(x != double.NaN);
        Assert.False(x < double.NaN);
        Assert.False(x > double.NaN);
        Assert.False(x <= double.NaN);
        Assert.False(x >= double.NaN);

        Assert.False(double.NaN == x);
        Assert.True(double.NaN != x);
        Assert.False(double.NaN < x);
        Assert.False(double.NaN > x);
        Assert.False(double.NaN <= x);
        Assert.False(double.NaN >= x);

        Assert.False(EqBranch(x));
        Assert.True(NeBranch(x));
        Assert.False(LtBranch(x));
        Assert.False(GtBranch(x));
        Assert.False(LeBranch(x));
        Assert.False(GeBranch(x));
    }

    [Theory]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(0.0f)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.NaN)]
    public static void Single(float x)
    {
        Assert.False(x == float.NaN);
        Assert.True(x != float.NaN);
        Assert.False(x < float.NaN);
        Assert.False(x > float.NaN);
        Assert.False(x <= float.NaN);
        Assert.False(x >= float.NaN);

        Assert.False(float.NaN == x);
        Assert.True(float.NaN != x);
        Assert.False(float.NaN < x);
        Assert.False(float.NaN > x);
        Assert.False(float.NaN <= x);
        Assert.False(float.NaN >= x);

        Assert.False(EqBranch(x));
        Assert.True(NeBranch(x));
        Assert.False(LtBranch(x));
        Assert.False(GtBranch(x));
        Assert.False(LeBranch(x));
        Assert.False(GeBranch(x));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EqBranch(double x) { if (x == double.NaN) { return true; } return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool NeBranch(double x) { if (x != double.NaN) { return true; } return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LtBranch(double x) { if (x < double.NaN) { return true; } return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GtBranch(double x) { if (x > double.NaN) { return true; } return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LeBranch(double x) { if (x <= double.NaN) { return true; } return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GeBranch(double x) { if (x >= double.NaN) { return true; } return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EqBranch(float x) { if (x == float.NaN) { return true; } return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool NeBranch(float x) { if (x != float.NaN) { return true; } return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LtBranch(float x) { if (x < float.NaN) { return true; } return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GtBranch(float x) { if (x > float.NaN) { return true; } return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LeBranch(float x) { if (x <= float.NaN) { return true; } return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GeBranch(float x) { if (x >= float.NaN) { return true; } return false; }
}
