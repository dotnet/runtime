// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_98068
{
    [Fact]
    [InlineData(1e18)]
    public static void TestSinCosDouble(double d)
    {
        double sin1 = Math.Sin(d);
        double cos1 = Math.Cos(d);

        (double sin2, double cos2) = Math.SinCos(d);

        Assert.Equal(sin1, sin2);
        Assert.Equal(cos1, cos2);
    }

    [Fact]
    [InlineData(1e18f)]
    public static void TestSinCosSingle(float f)
    {
        float sin1 = MathF.Sin(f);
        float cos1 = MathF.Cos(f);

        (float sin2, float cos2) = MathF.SinCos(f);

        Assert.Equal(sin1, sin2);
        Assert.Equal(cos1, cos2);
    }
}
