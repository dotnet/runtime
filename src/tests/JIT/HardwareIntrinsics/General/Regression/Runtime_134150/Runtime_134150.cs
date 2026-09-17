// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_134150
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void Single(bool positive)
    {
        float[] values =
        [
            0.0f, -0.0f, 1.0f, -1.0f,
            float.Epsilon, -float.Epsilon, float.PositiveInfinity, float.NegativeInfinity,
            BitConverter.Int32BitsToSingle(0x7FC00000), BitConverter.Int32BitsToSingle(unchecked((int)0xFFC00000)),
            BitConverter.Int32BitsToSingle(0x7F800001), BitConverter.Int32BitsToSingle(unchecked((int)0xFF800001)),
            float.MaxValue, float.MinValue, 2.0f, -2.0f
        ];

        for (int offset = 0; offset < values.Length; offset += Vector256<float>.Count)
        {
            Vector256<float> vector = Vector256.Create(values.AsSpan(offset));
            Vector256<int> result = (positive ? IsPositive(vector) : IsNegative(vector)).AsInt32();

            for (int i = 0; i < Vector256<float>.Count; i++)
            {
                bool negative = BitConverter.SingleToInt32Bits(values[offset + i]) < 0;
                Assert.Equal(negative != positive ? -1 : 0, result.GetElement(i));
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void Double(bool positive)
    {
        double[] values =
        [
            0.0, -0.0, 1.0, -1.0,
            double.Epsilon, -double.Epsilon, double.PositiveInfinity, double.NegativeInfinity,
            BitConverter.Int64BitsToDouble(0x7FF8000000000000), BitConverter.Int64BitsToDouble(unchecked((long)0xFFF8000000000000)),
            BitConverter.Int64BitsToDouble(0x7FF0000000000001), BitConverter.Int64BitsToDouble(unchecked((long)0xFFF0000000000001)),
            double.MaxValue, double.MinValue, 2.0, -2.0
        ];

        for (int offset = 0; offset < values.Length; offset += Vector256<double>.Count)
        {
            Vector256<double> vector = Vector256.Create(values.AsSpan(offset));
            Vector256<long> result = (positive ? IsPositive(vector) : IsNegative(vector)).AsInt64();

            for (int i = 0; i < Vector256<double>.Count; i++)
            {
                bool negative = BitConverter.DoubleToInt64Bits(values[offset + i]) < 0;
                Assert.Equal(negative != positive ? -1L : 0L, result.GetElement(i));
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<T> IsNegative<T>(Vector256<T> vector) => Vector256.IsNegative(vector);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<T> IsPositive<T>(Vector256<T> vector) => Vector256.IsPositive(vector);
}
