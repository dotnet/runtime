// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public static class Runtime_98068
{
    [Fact]
    public static void Main(string[] args)
    {
        Vector128<double> result = Map(Vector128<double>.One, new FloatPoint(2.0, 3.0));
        Assert.Equal(2.0, result[0]);
        Assert.Equal(2.0, result[1]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<double> Map(Vector128<double> m0, FloatPoint point)
    {
        return m0 * Vector128.Create(point.X);
    }

    private struct FloatPoint(double x, double y)
    {
        public double X = x;
        public double Y = y;
    }
}
