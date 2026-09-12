// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_133718;

public class Runtime_133718
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void TestEntryPoint(bool nanFirst)
    {
        double[] values = new double[1];
        Assert.Throws<IndexOutOfRangeException>(() => Min(values, values.Length, nanFirst));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double Min(double[] values, int index, bool nanFirst) =>
        nanFirst
            ? Math.Min(double.NaN, values[index])
            : Math.Min(values[index], double.NaN);
}
