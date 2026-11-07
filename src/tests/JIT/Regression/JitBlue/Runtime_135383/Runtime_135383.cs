// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_135383
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Use(ref float f)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Use(ref double d)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> CreateScalarFromStackFloat(Vector128<float> a, Vector128<float> b)
    {
        float f = 0;
        Use(ref f);
        f = (a + b).ToScalar();
        return Vector128.CreateScalar(f);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<double> CreateScalarFromStackDouble(Vector128<double> a, Vector128<double> b)
    {
        double d = 0;
        Use(ref d);
        d = (a + b).ToScalar();
        return Vector128.CreateScalar(d);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Vector128<float> f = CreateScalarFromStackFloat(Vector128.Create(1f, 2f, 3f, 4f), Vector128.Create(1f));
        Assert.Equal(Vector128.Create(2f, 0f, 0f, 0f), f);

        Vector128<double> d = CreateScalarFromStackDouble(Vector128.Create(1d, 2d), Vector128.Create(1d));
        Assert.Equal(Vector128.Create(2d, 0d), d);
    }
}
