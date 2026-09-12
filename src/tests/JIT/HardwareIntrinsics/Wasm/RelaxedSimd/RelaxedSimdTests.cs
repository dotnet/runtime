// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;
using Xunit;

public sealed class RelaxedSimdTests
{
    public static bool IsNotSupported => !RelaxedSimd.IsSupported;

    [Fact]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(RelaxedSimd))]
    public static void RelaxedSimdIsSupportedReflects()
    {
        MethodInfo? methodInfo = typeof(RelaxedSimd).GetProperty(nameof(RelaxedSimd.IsSupported))?.GetGetMethod();
        Assert.NotNull(methodInfo);
        Assert.Equal(RelaxedSimd.IsSupported, methodInfo.Invoke(null, null));
    }

    [ConditionalFact(typeof(RelaxedSimdTests), nameof(IsNotSupported))]
    public static void UnsupportedMethodThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(
            () => RelaxedSimd.ConvertToInt32Native(Vector128.Create(1.0f)));
    }

    [ConditionalFact(typeof(RelaxedSimd), nameof(RelaxedSimd.IsSupported))]
    public static void ConvertToIntegerNativeInRangeMatchesExpected()
    {
        Assert.Equal(
            Vector128.Create(1, -2, 3, -4),
            RelaxedSimd.ConvertToInt32Native(Vector128.Create(1.75f, -2.25f, 3.0f, -4.99f)));
        Assert.Equal(
            Vector128.Create(5, -6, 0, 0),
            RelaxedSimd.ConvertToInt32Native(Vector128.Create(5.75, -6.25)));
        Assert.Equal(
            Vector128.Create(1u, 2u, 3u, 4u),
            RelaxedSimd.ConvertToUInt32Native(Vector128.Create(1.75f, 2.25f, 3.0f, 4.99f)));
        Assert.Equal(
            Vector128.Create(5u, 6u, 0u, 0u),
            RelaxedSimd.ConvertToUInt32Native(Vector128.Create(5.75, 6.25)));
    }

    [ConditionalFact(typeof(RelaxedSimd), nameof(RelaxedSimd.IsSupported))]
    public static void DotProductNativeByteSByteMatchesScalar()
    {
        Vector128<sbyte> signed = Vector128.Create((sbyte)-1, 2, -3, 4, -5, 6, -7, 8, -9, 10, -11, 12, -13, 14, -15, 16);
        Vector128<byte> unsigned = Vector128.Create((byte)2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3);

        Vector128<short> actual = RelaxedSimd.DotProductNative(signed, unsigned);

        for (int i = 0; i < 8; i++)
        {
            short expected = (short)(signed[2 * i] * unsigned[2 * i] + signed[2 * i + 1] * unsigned[2 * i + 1]);
            Assert.Equal(expected, actual[i]);
        }
    }

    [ConditionalFact(typeof(RelaxedSimd), nameof(RelaxedSimd.IsSupported))]
    public static void DotProductAddNativeByteSByteMatchesScalar()
    {
        Vector128<sbyte> signed = Vector128.Create((sbyte)-1, 2, -3, 4, -5, 6, -7, 8, -9, 10, -11, 12, -13, 14, -15, 16);
        Vector128<byte> unsigned = Vector128.Create((byte)2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3, 2, 3);
        Vector128<int> accumulator = Vector128.Create(100, 200, 300, 400);

        Vector128<int> actual = RelaxedSimd.DotProductAddNative(signed, unsigned, accumulator);

        for (int i = 0; i < 4; i++)
        {
            int expected = accumulator[i];
            for (int j = 0; j < 4; j++)
            {
                expected += signed[4 * i + j] * unsigned[4 * i + j];
            }
            Assert.Equal(expected, actual[i]);
        }
    }

    [ConditionalFact(typeof(RelaxedSimd), nameof(RelaxedSimd.IsSupported))]
    public static void MultiplyAddFloatMatchesScalarApproximately()
    {
        Vector128<float> left = Vector128.Create(1.5f, 2.25f, -3.125f, 4.0f);
        Vector128<float> right = Vector128.Create(2.0f, -1.5f, 0.5f, 6.25f);
        Vector128<float> addend = Vector128.Create(0.5f, 1.0f, -0.25f, -2.0f);

        Vector128<float> actual = RelaxedSimd.MultiplyAddEstimate(left, right, addend);
        Vector128<float> unfused = (left * right) + addend;

        const float RelativeTolerance = 1e-5f;
        for (int i = 0; i < 4; i++)
        {
            float tolerance = Math.Max(Math.Abs(unfused[i]), 1.0f) * RelativeTolerance;
            Assert.True(Math.Abs(actual[i] - unfused[i]) <= tolerance,
                $"lane {i}: relaxed FMA {actual[i]} differs from unfused {unfused[i]} by more than {tolerance}");
        }
    }

    [ConditionalFact(typeof(RelaxedSimd), nameof(RelaxedSimd.IsSupported))]
    public static void LaneSelectNativeAllOnesAllZerosBehavesLikeConditionalSelect()
    {
        Vector128<byte> left = Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        Vector128<byte> right = Vector128.Create((byte)17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
        Vector128<byte> mask = Vector128.Create((byte)0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00,
                                                          0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00);

        Vector128<byte> actual = RelaxedSimd.LaneSelectNative(left, right, mask);
        Vector128<byte> expected = Vector128.ConditionalSelect(mask, left, right);

        Assert.Equal(expected, actual);
    }

    [ConditionalFact(typeof(RelaxedSimd), nameof(RelaxedSimd.IsSupported))]
    public static void SwizzleNativeInRangeMatchesVector128Shuffle()
    {
        Vector128<byte> value = Vector128.Create((byte)10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160);
        Vector128<byte> indices = Vector128.Create((byte)15, 0, 14, 1, 13, 2, 12, 3, 11, 4, 10, 5, 9, 6, 8, 7);

        Vector128<byte> actual = RelaxedSimd.SwizzleNative(value, indices);
        Vector128<byte> expected = Vector128.Shuffle(value, indices);

        Assert.Equal(expected, actual);
    }
}
