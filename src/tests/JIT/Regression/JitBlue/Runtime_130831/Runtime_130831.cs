// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_130831;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_130831
{
    // Compare the exact bit pattern: Double/Single.Equals treat -0.0 and +0.0 as equal,
    // so a plain Assert.Equal would not observe a wrong-signed-zero result.
    [Fact]
    [SkipOnMono("https://github.com/dotnet/runtime/issues/131130", TestPlatforms.Any)]
    public static void TestEntryPoint()
    {
        Assert.Equal(BitConverter.DoubleToInt64Bits(-0.0), BitConverter.DoubleToInt64Bits(MinNegZeroConst(+0.0)));
        Assert.Equal(BitConverter.DoubleToInt64Bits(-0.0), BitConverter.DoubleToInt64Bits(MinNumberZeroConst(-0.0)));

        Assert.Equal(BitConverter.SingleToInt32Bits(-0.0f), BitConverter.SingleToInt32Bits(MinNegZeroConst(+0.0f)));
        Assert.Equal(BitConverter.SingleToInt32Bits(-0.0f), BitConverter.SingleToInt32Bits(MinNumberZeroConst(-0.0f)));
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
