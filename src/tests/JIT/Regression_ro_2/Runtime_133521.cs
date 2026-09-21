// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_133521
{
    [ConditionalTheory(typeof(Sse2), nameof(Sse2.IsSupported))]
    [InlineData(1.0, 1.0)]
    [InlineData(1.0, 2.0)]
    [InlineData(double.NaN, 1.0)]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ScalarComparison(double left, double right)
    {
        Vector128<float> a = Vector128.Create((float)left, 2.0f, -0.0f, float.NaN);
        Vector128<float> b = Vector128.Create((float)right);
        Vector128<double> c = Vector128.Create(left, 2.0);
        Vector128<double> d = Vector128.Create(right);
        uint expectedSingle = left == right ? 0u : uint.MaxValue;
        ulong expectedDouble = left == right ? 0ul : ulong.MaxValue;
        Vector128<uint> expectedScalarSingle = Vector128.Create(expectedSingle, ~0x40000000u, ~0x80000000u, ~BitConverter.SingleToUInt32Bits(float.NaN));
        Vector128<ulong> expectedScalarDouble = Vector128.Create(expectedDouble, ~0x4000000000000000ul);

        Assert.Equal(expectedScalarSingle, (~Sse.CompareScalarEqual(a, b)).AsUInt32());
        Assert.Equal(expectedScalarDouble, (~Sse2.CompareScalarEqual(c, d)).AsUInt64());

        if (Avx.IsSupported)
        {
            bool unordered = double.IsNaN(left) || double.IsNaN(right);
            Assert.Equal(expectedScalarSingle.WithElement(0, unordered ? 0u : expectedSingle),
                (~Avx.CompareScalar(a, b, FloatComparisonMode.UnorderedEqualNonSignaling)).AsUInt32());
            Assert.Equal(expectedScalarDouble.WithElement(0, unordered ? 0ul : expectedDouble),
                (~Avx.CompareScalar(c, d, FloatComparisonMode.UnorderedEqualNonSignaling)).AsUInt64());
        }
    }

    [ConditionalTheory(typeof(Fma), nameof(Fma.IsSupported))]
    [InlineData(0, -11)]
    [InlineData(1, -13)]
    [InlineData(2, 13)]
    [InlineData(3, 11)]
    public static void ScalarFmaNegation(int operation, int expected)
    {
        Vector128<float> a = Vector128.Create(2.0f, 3.0f, -0.0f, float.NaN);
        Vector128<double> b = Vector128.Create(2.0, -0.0);
        Assert.Equal(Vector128.Create((float)expected, -3.0f, 0.0f, BitConverter.UInt32BitsToSingle(BitConverter.SingleToUInt32Bits(float.NaN) ^ 0x80000000u)).AsUInt32(),
            NegatedScalarFma(a, Vector128.Create(6.0f), Vector128.Create(1.0f), operation).AsUInt32());
        Assert.Equal(Vector128.Create((double)expected, 0.0).AsUInt64(),
            NegatedScalarFma(b, Vector128.Create(6.0), Vector128.Create(1.0), operation).AsUInt64());

        if (operation == 0)
        {
            Assert.Equal(Vector128.Create(-13.0f, 3.0f, -0.0f, float.NaN).AsUInt32(),
                NegatedOtherScalarFma(a, Vector128.Create(6.0f), Vector128.Create(1.0f)).AsUInt32());
            Assert.Equal(Vector128.Create(-13.0, -0.0).AsUInt64(),
                NegatedOtherScalarFma(b, Vector128.Create(6.0), Vector128.Create(1.0)).AsUInt64());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> NegatedScalarFma(Vector128<float> a, Vector128<float> b, Vector128<float> c, int operation) => operation switch
    {
        0 => Fma.MultiplyAddScalar(-a, b, c),
        1 => Fma.MultiplySubtractScalar(-a, b, c),
        2 => Fma.MultiplyAddNegatedScalar(-a, b, c),
        3 => Fma.MultiplySubtractNegatedScalar(-a, b, c),
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<double> NegatedScalarFma(Vector128<double> a, Vector128<double> b, Vector128<double> c, int operation) => operation switch
    {
        0 => Fma.MultiplyAddScalar(-a, b, c),
        1 => Fma.MultiplySubtractScalar(-a, b, c),
        2 => Fma.MultiplyAddNegatedScalar(-a, b, c),
        3 => Fma.MultiplySubtractNegatedScalar(-a, b, c),
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> NegatedOtherScalarFma(Vector128<float> a, Vector128<float> b, Vector128<float> c) =>
        Fma.MultiplyAddScalar(a, -b, -c);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<double> NegatedOtherScalarFma(Vector128<double> a, Vector128<double> b, Vector128<double> c) =>
        Fma.MultiplyAddScalar(a, -b, -c);
}
