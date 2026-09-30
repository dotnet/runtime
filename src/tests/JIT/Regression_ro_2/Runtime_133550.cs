// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_133550
{
    [ConditionalTheory(typeof(Sse2), nameof(Sse2.IsSupported))]
    [InlineData(1.0, 2.0, 111.0)]
    [InlineData(2.0, 2.0, 111.0)]
    [InlineData(3.0, 2.0, 222.0)]
    [InlineData(double.NaN, 2.0, 111.0)]
    [InlineData(2.0, double.NaN, 111.0)]
    public static void Select128(double left, double right, double expected)
    {
        Assert.Equal(Vector128.Create((float)expected),
            Select(Vector128.Create((float)left), Vector128.Create((float)right),
                Vector128.Create(111.0f), Vector128.Create(222.0f)));
        Assert.Equal(Vector128.Create(expected),
            Select(Vector128.Create(left), Vector128.Create(right),
                Vector128.Create(111.0), Vector128.Create(222.0)));
    }

    [ConditionalTheory(typeof(Avx), nameof(Avx.IsSupported))]
    [InlineData(1.0, 2.0, 111.0)]
    [InlineData(2.0, 2.0, 111.0)]
    [InlineData(3.0, 2.0, 222.0)]
    [InlineData(double.NaN, 2.0, 111.0)]
    [InlineData(2.0, double.NaN, 111.0)]
    public static void Select256(double left, double right, double expected)
    {
        Assert.Equal(Vector256.Create((float)expected),
            Select(Vector256.Create((float)left), Vector256.Create((float)right),
                Vector256.Create(111.0f), Vector256.Create(222.0f)));
        Assert.Equal(Vector256.Create(expected),
            Select(Vector256.Create(left), Vector256.Create(right),
                Vector256.Create(111.0), Vector256.Create(222.0)));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> Select(Vector128<float> a, Vector128<float> b, Vector128<float> x, Vector128<float> y) =>
        Vector128.ConditionalSelect(Sse.CompareNotGreaterThan(a, b), x, y);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<double> Select(Vector128<double> a, Vector128<double> b, Vector128<double> x, Vector128<double> y) =>
        Vector128.ConditionalSelect(Sse2.CompareNotGreaterThan(a, b), x, y);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<float> Select(Vector256<float> a, Vector256<float> b, Vector256<float> x, Vector256<float> y) =>
        Vector256.ConditionalSelect(Avx.CompareNotGreaterThan(a, b), x, y);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<double> Select(Vector256<double> a, Vector256<double> b, Vector256<double> x, Vector256<double> y) =>
        Vector256.ConditionalSelect(Avx.CompareNotGreaterThan(a, b), x, y);
}
