// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_133022;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public static class Runtime_133022
{
    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void TestEntryPoint()
    {
        Vector128<double> nan128 = Vector128.Create(double.NaN, double.PositiveInfinity);
        Vector128<double> otherNan128 = Opaque(Vector128.Create(double.PositiveInfinity, double.NaN));
        Vector128<double> propagatedNaN128 = Vector128.Create(double.NaN);
        Vector128<double> numberNaN128 = Vector128.Create(double.PositiveInfinity);

        AssertEqual(propagatedNaN128, Vector128.Min(nan128, otherNan128));
        AssertEqual(propagatedNaN128, Vector128.Min(otherNan128, nan128));
        AssertEqual(propagatedNaN128, Vector128.Max(nan128, otherNan128));
        AssertEqual(propagatedNaN128, Vector128.Max(otherNan128, nan128));
        AssertEqual(numberNaN128, Vector128.MinNumber(nan128, otherNan128));
        AssertEqual(numberNaN128, Vector128.MinNumber(otherNan128, nan128));
        AssertEqual(numberNaN128, Vector128.MaxNumber(nan128, otherNan128));
        AssertEqual(numberNaN128, Vector128.MaxNumber(otherNan128, nan128));

        Vector128<double> zero128 = Vector128.Create(-1.0, -0.0);
        Vector128<double> otherZero128 = Opaque(Vector128.Create(-0.0, +0.0));
        Vector128<double> minZero128 = Vector128.Create(-1.0, -0.0);
        Vector128<double> maxZero128 = Vector128.Create(-0.0, +0.0);

        AssertEqual(minZero128, Vector128.Min(zero128, otherZero128));
        AssertEqual(minZero128, Vector128.Min(otherZero128, zero128));
        AssertEqual(maxZero128, Vector128.Max(zero128, otherZero128));
        AssertEqual(maxZero128, Vector128.Max(otherZero128, zero128));
        AssertEqual(minZero128, Vector128.MinNumber(zero128, otherZero128));
        AssertEqual(minZero128, Vector128.MinNumber(otherZero128, zero128));
        AssertEqual(maxZero128, Vector128.MaxNumber(zero128, otherZero128));
        AssertEqual(maxZero128, Vector128.MaxNumber(otherZero128, zero128));

        Vector128<float> nanSingle128 = Vector128.Create(float.NaN, float.PositiveInfinity, 1.0f, 2.0f);
        Vector128<float> otherNanSingle128 = Opaque(Vector128.Create(float.PositiveInfinity, float.NaN, 2.0f, 1.0f));

        AssertEqual(Vector128.Create(float.NaN, float.NaN, 1.0f, 1.0f), Vector128.Min(nanSingle128, otherNanSingle128));
        AssertEqual(Vector128.Create(float.NaN, float.NaN, 2.0f, 2.0f), Vector128.Max(nanSingle128, otherNanSingle128));
        AssertEqual(Vector128.Create(float.PositiveInfinity, float.PositiveInfinity, 1.0f, 1.0f),
                    Vector128.MinNumber(nanSingle128, otherNanSingle128));
        AssertEqual(Vector128.Create(float.PositiveInfinity, float.PositiveInfinity, 2.0f, 2.0f),
                    Vector128.MaxNumber(nanSingle128, otherNanSingle128));

        Vector128<float> zeroSingle128 = Vector128.Create(-1.0f, -0.0f, -2.0f, +0.0f);
        Vector128<float> otherZeroSingle128 = Opaque(Vector128.Create(-0.0f, +0.0f, -3.0f, -0.0f));

        AssertEqual(Vector128.Create(-1.0f, -0.0f, -3.0f, -0.0f), Vector128.Min(zeroSingle128, otherZeroSingle128));
        AssertEqual(Vector128.Create(-0.0f, +0.0f, -2.0f, +0.0f), Vector128.Max(zeroSingle128, otherZeroSingle128));
        AssertEqual(Vector128.Create(-1.0f, -0.0f, -3.0f, -0.0f),
                    Vector128.MinNumber(zeroSingle128, otherZeroSingle128));
        AssertEqual(Vector128.Create(-0.0f, +0.0f, -2.0f, +0.0f),
                    Vector128.MaxNumber(zeroSingle128, otherZeroSingle128));

        float positiveNaN = BitConverter.Int32BitsToSingle(0x7FC0_0001);
        float negativeNaN = BitConverter.Int32BitsToSingle(unchecked((int)0xFFC0_0001));
        Vector128<float> mixedSingle128 = Vector128.Create(-1.0f, -2.0f, -0.0f, +0.0f);
        Vector128<float> otherMixedSingle128 = Opaque(Vector128.Create(positiveNaN, negativeNaN, +0.0f, -0.0f));
        Vector128<float> minMixedSingle128 = Vector128.Create(float.NaN, float.NaN, -0.0f, -0.0f);
        Vector128<float> maxMixedSingle128 = Vector128.Create(float.NaN, float.NaN, +0.0f, +0.0f);

        AssertEqualIgnoringNaNBits(minMixedSingle128, Vector128.Min(mixedSingle128, otherMixedSingle128));
        AssertEqualIgnoringNaNBits(minMixedSingle128, Vector128.Min(otherMixedSingle128, mixedSingle128));
        AssertEqualIgnoringNaNBits(maxMixedSingle128, Vector128.Max(mixedSingle128, otherMixedSingle128));
        AssertEqualIgnoringNaNBits(maxMixedSingle128, Vector128.Max(otherMixedSingle128, mixedSingle128));

        Vector256<double> nan256 = Vector256.Create(double.NaN, double.PositiveInfinity, 1.0, 2.0);
        Vector256<double> otherNan256 = Opaque(Vector256.Create(double.PositiveInfinity, double.NaN, 2.0, 1.0));
        Vector256<double> propagatedNaN256 = Vector256.Create(double.NaN, double.NaN, 1.0, 1.0);
        Vector256<double> minNumberNaN256 = Vector256.Create(double.PositiveInfinity, double.PositiveInfinity, 1.0, 1.0);
        Vector256<double> maxNumberNaN256 = Vector256.Create(double.PositiveInfinity, double.PositiveInfinity, 2.0, 2.0);

        AssertEqual(propagatedNaN256, Vector256.Min(nan256, otherNan256));
        AssertEqual(propagatedNaN256, Vector256.Min(otherNan256, nan256));
        AssertEqual(Vector256.Create(double.NaN, double.NaN, 2.0, 2.0), Vector256.Max(nan256, otherNan256));
        AssertEqual(Vector256.Create(double.NaN, double.NaN, 2.0, 2.0), Vector256.Max(otherNan256, nan256));
        AssertEqual(minNumberNaN256, Vector256.MinNumber(nan256, otherNan256));
        AssertEqual(minNumberNaN256, Vector256.MinNumber(otherNan256, nan256));
        AssertEqual(maxNumberNaN256, Vector256.MaxNumber(nan256, otherNan256));
        AssertEqual(maxNumberNaN256, Vector256.MaxNumber(otherNan256, nan256));

        Vector256<double> zero256 = Vector256.Create(-1.0, -0.0, -2.0, +0.0);
        Vector256<double> otherZero256 = Opaque(Vector256.Create(-0.0, +0.0, -3.0, -0.0));
        Vector256<double> minZero256 = Vector256.Create(-1.0, -0.0, -3.0, -0.0);
        Vector256<double> maxZero256 = Vector256.Create(-0.0, +0.0, -2.0, +0.0);

        AssertEqual(minZero256, Vector256.Min(zero256, otherZero256));
        AssertEqual(minZero256, Vector256.Min(otherZero256, zero256));
        AssertEqual(maxZero256, Vector256.Max(zero256, otherZero256));
        AssertEqual(maxZero256, Vector256.Max(otherZero256, zero256));
        AssertEqual(minZero256, Vector256.MinNumber(zero256, otherZero256));
        AssertEqual(minZero256, Vector256.MinNumber(otherZero256, zero256));
        AssertEqual(maxZero256, Vector256.MaxNumber(zero256, otherZero256));
        AssertEqual(maxZero256, Vector256.MaxNumber(otherZero256, zero256));

        AssertEqual(Vector128.Create(double.NaN), MinConstants128());
        AssertEqual(Vector128.Create(double.NaN), MaxConstants128());
        AssertEqual(Vector128.Create(1.0), MinWithoutSpecialValues(Opaque(Vector128.Create(3.0, 1.0))));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<double> Opaque(Vector128<double> value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Opaque(Vector128<float> value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<double> Opaque(Vector256<double> value) => value;

    private static void AssertEqual(Vector128<double> expected, Vector128<double> actual)
        => Assert.Equal(expected.AsUInt64(), actual.AsUInt64());

    private static void AssertEqual(Vector128<float> expected, Vector128<float> actual)
        => Assert.Equal(expected.AsUInt32(), actual.AsUInt32());

    private static void AssertEqualIgnoringNaNBits(Vector128<float> expected, Vector128<float> actual)
    {
        for (int index = 0; index < Vector128<float>.Count; index++)
        {
            float expectedElement = expected.GetElement(index);
            float actualElement = actual.GetElement(index);

            if (float.IsNaN(expectedElement))
            {
                Assert.True(float.IsNaN(actualElement));
            }
            else
            {
                Assert.Equal(BitConverter.SingleToInt32Bits(expectedElement),
                             BitConverter.SingleToInt32Bits(actualElement));
            }
        }
    }

    private static void AssertEqual(Vector256<double> expected, Vector256<double> actual)
        => Assert.Equal(expected.AsUInt64(), actual.AsUInt64());

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<double> MinConstants128()
    {
        Vector128<double> left = Vector128.Create(double.NaN, 3.0);
        Vector128<double> right = Vector128.Create(3.0, double.NaN);

        return Vector128.Min(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<double> MaxConstants128()
    {
        Vector128<double> left = Vector128.Create(double.NaN, 3.0);
        Vector128<double> right = Vector128.Create(3.0, double.NaN);

        return Vector128.Max(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<double> MinWithoutSpecialValues(Vector128<double> value)
        => Vector128.Min(Vector128.Create(1.0, 2.0), value);
}
