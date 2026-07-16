// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_130831;

using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_130831
{
    // xunit's Assert.Equal for float/double compares bitwise, so it distinguishes -0.0 from +0.0.
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(-0.0, MinNegZeroConst(+0.0));
        Assert.Equal(-0.0, MinNumberZeroConst(-0.0));

        Assert.Equal(-0.0f, MinNegZeroConst(+0.0f));
        Assert.Equal(-0.0f, MinNumberZeroConst(-0.0f));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double MinNegZeroConst(double value) => double.Min(value, -0.0);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double MinNumberZeroConst(double value) => double.MinNumber(value, +0.0);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static float MinNegZeroConst(float value) => float.Min(value, -0.0f);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static float MinNumberZeroConst(float value) => float.MinNumber(value, +0.0f);
}
