// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_134480
{
    [ConditionalTheory(typeof(Avx), nameof(Avx.IsSupported))]
    [InlineData(1.0f, 1.0f)]
    [InlineData(1.0f, 2.0f)]
    [InlineData(float.NaN, 1.0f)]
    [InlineData(1.0f, float.NaN)]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void CompareWithZero(float left, float right)
    {
        Vector256<float> a = Vector256.Create(left).WithElement(0, 1.0f);
        Vector256<float> b = Vector256.Create(right).WithElement(0, 1.0f);
        int expected = !float.IsNaN(left) && !float.IsNaN(right) && left != right ? -1 : 0;
        Vector256<int> mask = Vector256.Create(expected).WithElement(0, 0);

        Assert.Equal(mask.GetLower(), Vector128.Equals(
            Avx.Compare(a.GetLower(), b.GetLower(), FloatComparisonMode.UnorderedEqualNonSignaling).AsInt32(), Vector128<int>.Zero));
        Assert.Equal(~mask.GetLower(), Vector128.Equals(
            Avx.Compare(a.GetLower(), b.GetLower(), FloatComparisonMode.OrderedNotEqualNonSignaling).AsInt32(), Vector128<int>.Zero));
        Assert.Equal(mask, Vector256.Equals(
            Avx.Compare(a, b, FloatComparisonMode.UnorderedEqualNonSignaling).AsInt32(), Vector256<int>.Zero));
        Assert.Equal(~mask, Vector256.Equals(
            Avx.Compare(a, b, FloatComparisonMode.OrderedNotEqualNonSignaling).AsInt32(), Vector256<int>.Zero));

        if (Avx512F.IsSupported)
        {
            Vector512<double> c = Vector512.Create((double)left).WithElement(0, 1.0);
            Vector512<double> d = Vector512.Create((double)right).WithElement(0, 1.0);
            Vector512<long> mask512 = Vector512.Create((long)expected).WithElement(0, 0);
            Assert.Equal(mask512, Vector512.Equals(
                Avx512F.Compare(c, d, FloatComparisonMode.UnorderedEqualNonSignaling).AsInt64(), Vector512<long>.Zero));
            Assert.Equal(~mask512, Vector512.Equals(
                Avx512F.Compare(c, d, FloatComparisonMode.OrderedNotEqualNonSignaling).AsInt64(), Vector512<long>.Zero));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    public static void DoubleNot(int value)
    {
        CheckDoubleNot<byte>((byte)value);
        CheckDoubleNot<short>((short)value);
        CheckDoubleNot<int>(value);
        CheckDoubleNot<long>(value);
    }

    private static void CheckDoubleNot<T>(T value) where T : unmanaged
    {
        Vector512<T> a = Vector512.Create(value).WithElement(0, default);
        Vector512<T> b = a.WithElement(1, default);
        Vector512<T> expected = Vector512.Equals(a, b) & Vector512.Equals(a, Vector512<T>.Zero);
        Assert.Equal(expected.GetLower().GetLower(), DoubleNot128(a.GetLower().GetLower(), b.GetLower().GetLower()));
        Assert.Equal(expected.GetLower(), DoubleNot256(a.GetLower(), b.GetLower()));
        Assert.Equal(expected, DoubleNot512(a, b));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<T> DoubleNot128<T>(Vector128<T> a, Vector128<T> b) where T : unmanaged =>
        Vector128.Equals(~(Vector128.Equals(a, b) & Vector128.Equals(a, Vector128<T>.Zero)), Vector128<T>.Zero);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<T> DoubleNot256<T>(Vector256<T> a, Vector256<T> b) where T : unmanaged =>
        Vector256.Equals(~(Vector256.Equals(a, b) & Vector256.Equals(a, Vector256<T>.Zero)), Vector256<T>.Zero);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector512<T> DoubleNot512<T>(Vector512<T> a, Vector512<T> b) where T : unmanaged =>
        Vector512.Equals(~(Vector512.Equals(a, b) & Vector512.Equals(a, Vector512<T>.Zero)), Vector512<T>.Zero);
}
