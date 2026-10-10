// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using Xunit;

namespace SIMDTests.CoverageTests;

// This is a set of simple smoke tests to help test the coverage of
// the Vector<T> API surface implementation.
public class Coverage
{
    [Fact]
    public static void CreateTest() => Assert.Equal(42, Vector.Create(42)[Vector<int>.Count - 1]);

    [Fact]
    public static void AbsIntTest() => Assert.Equal(42, Vector.Abs(new Vector<int>(-42))[0]);

    [Fact]
    public static void AbsFloatTest() => Assert.Equal(42.0f, Vector.Abs(new Vector<float>(-42.0f))[0]);

    [Fact]
    public static void AddTest() => Assert.Equal(42, (new Vector<int>(40) + new Vector<int>(2))[0]);

    [Fact]
    public static void SubtractTest() => Assert.Equal(42, (new Vector<int>(44) - new Vector<int>(2))[0]);

    [Fact]
    public static void MultiplyTest() => Assert.Equal(42, (new Vector<int>(21) * new Vector<int>(2))[0]);

    [Fact]
    public static void MultiplyScalarTest() => Assert.Equal(42, (new Vector<int>(21) * 2)[0]);

    [Fact]
    public static void ScalarMultiplyTest() => Assert.Equal(42, (2 * new Vector<int>(21))[0]);

    [Fact]
    public static void DivideFloatTest() =>
        Assert.Equal(42.0f, (new Vector<float>(84.0f) / new Vector<float>(2.0f))[0]);

    [Fact]
    public static void DivideScalarFloatTest() => Assert.Equal(42.0f, (new Vector<float>(84.0f) / 2.0f)[0]);

    [Fact]
    public static void BitwiseAndTest() => Assert.Equal(2, (new Vector<int>(6) & new Vector<int>(3))[0]);

    [Fact]
    public static void BitwiseOrTest() => Assert.Equal(7, (new Vector<int>(6) | new Vector<int>(3))[0]);

    [Fact]
    public static void XorTest() => Assert.Equal(5, (new Vector<int>(6) ^ new Vector<int>(3))[0]);

    [Fact]
    public static void AndNotTest() => Assert.Equal(5, Vector.AndNot(new Vector<int>(7), new Vector<int>(2))[0]);

    [Fact]
    public static void NegateTest() => Assert.Equal(-42, Vector.Negate(new Vector<int>(42))[0]);

    [Fact]
    public static void NegateFloatTest() => Assert.Equal(-42.0f, Vector.Negate(new Vector<float>(42.0f))[0]);

    [Fact]
    public static void OnesComplementTest() => Assert.Equal(-1, Vector.OnesComplement(new Vector<int>(0))[0]);

    [Fact]
    public static void OnesComplementFloatTest() =>
        Assert.Equal(-1, BitConverter.SingleToInt32Bits(Vector.OnesComplement(new Vector<float>(0.0f))[0]));

    [Fact]
    public static void OnesComplementOperatorTest() => Assert.Equal(-1, (~new Vector<int>(0))[0]);

    [Fact]
    public static void UnaryNegationOperatorTest() => Assert.Equal(-42, (-new Vector<int>(42))[0]);

    [Fact]
    public static void UnaryPlusOperatorTest() => Assert.Equal(42, (+new Vector<int>(42))[0]);

    [Fact]
    public static void ShiftLeftTest() => Assert.Equal(42, Vector.ShiftLeft(new Vector<int>(21), 1)[0]);

    [Fact]
    public static void ShiftLeftOperatorTest() => Assert.Equal(42, (new Vector<int>(21) << 1)[0]);

    [Fact]
    public static void ShiftRightArithmeticTest() =>
        Assert.Equal(-42, Vector.ShiftRightArithmetic(new Vector<int>(-84), 1)[0]);

    [Fact]
    public static void ShiftRightArithmeticOperatorTest() => Assert.Equal(-42, (new Vector<int>(-84) >> 1)[0]);

    [Fact]
    public static void ShiftRightLogicalTest() =>
        Assert.Equal(1u, Vector.ShiftRightLogical(new Vector<uint>(0x80000000), 31)[0]);

    [Fact]
    public static void ShiftRightLogicalOperatorTest() => Assert.Equal(1u, (new Vector<uint>(0x80000000) >>> 31)[0]);

    [Fact]
    public static void AddSaturateTest() =>
        Assert.Equal(int.MaxValue, Vector.AddSaturate(new Vector<int>(int.MaxValue), new Vector<int>(1))[0]);

    [Fact]
    public static void IsFiniteSingleTest() => Assert.NotEqual(0.0f, Vector.IsFinite(new Vector<float>(1.0f))[0]);

    [Fact]
    public static void IsFiniteDoubleTest() => Assert.NotEqual(0.0, Vector.IsFinite(new Vector<double>(1.0))[0]);

    [Fact]
    public static void IsIntegerSingleTest() => Assert.NotEqual(0.0f, Vector.IsInteger(new Vector<float>(1.0f))[0]);

    [Fact]
    public static void IsIntegerDoubleTest() => Assert.NotEqual(0.0, Vector.IsInteger(new Vector<double>(1.0))[0]);

    [Fact]
    public static void IsEvenIntegerSingleTest() =>
        Assert.NotEqual(0.0f, Vector.IsEvenInteger(new Vector<float>(2.0f))[0]);

    [Fact]
    public static void IsEvenIntegerDoubleTest() =>
        Assert.NotEqual(0.0, Vector.IsEvenInteger(new Vector<double>(2.0))[0]);

    [Fact]
    public static void IsOddIntegerSingleTest() =>
        Assert.NotEqual(0.0f, Vector.IsOddInteger(new Vector<float>(3.0f))[0]);

    [Fact]
    public static void IsOddIntegerDoubleTest() =>
        Assert.NotEqual(0.0, Vector.IsOddInteger(new Vector<double>(3.0))[0]);

    [Fact]
    public static void SubtractSaturateTest() =>
        Assert.Equal(int.MinValue, Vector.SubtractSaturate(new Vector<int>(int.MinValue), new Vector<int>(1))[0]);

    [Fact]
    public static void CeilingTest() => Assert.Equal(2.0f, Vector.Ceiling(new Vector<float>(1.25f))[0]);

    [Fact]
    public static void FloorTest() => Assert.Equal(1.0f, Vector.Floor(new Vector<float>(1.75f))[0]);

    [Fact]
    public static void RoundTest() => Assert.Equal(2.0f, Vector.Round(new Vector<float>(1.75f))[0]);

    [Fact]
    public static void TruncateTest() => Assert.Equal(-1.0f, Vector.Truncate(new Vector<float>(-1.75f))[0]);

    [Fact]
    public static void SquareRootTest() => Assert.Equal(4.0f, Vector.SquareRoot(new Vector<float>(16.0f))[0]);

    [Fact]
    public static void FusedMultiplyAddTest() =>
        Assert.Equal(
            10.0f,
            Vector.FusedMultiplyAdd(new Vector<float>(2.0f), new Vector<float>(3.0f), new Vector<float>(4.0f))[0]
        );

    [Fact]
    public static void ConditionalSelectTest()
    {
        Vector<int> condition = Vector.GreaterThan(new Vector<int>(2), new Vector<int>(1));
        Assert.Equal(2, Vector.ConditionalSelect(condition, new Vector<int>(2), new Vector<int>(3))[0]);
    }

    [Fact]
    public static void EqualsVectorTest() => Assert.Equal(-1, Vector.Equals(new Vector<int>(2), new Vector<int>(2))[0]);

    [Fact]
    public static void EqualsAllTest() => Assert.True(new Vector<int>(2) == new Vector<int>(2));

    [Fact]
    public static void NotEqualsAnyTest() => Assert.True(new Vector<int>(2) != new Vector<int>(3));

    [Fact]
    public static void GreaterThanTest() =>
        Assert.Equal(-1, Vector.GreaterThan(new Vector<int>(2), new Vector<int>(1))[0]);

    [Fact]
    public static void GreaterThanOrEqualTest() =>
        Assert.Equal(-1, Vector.GreaterThanOrEqual(new Vector<int>(2), new Vector<int>(2))[0]);

    [Fact]
    public static void LessThanTest() => Assert.Equal(-1, Vector.LessThan(new Vector<int>(1), new Vector<int>(2))[0]);

    [Fact]
    public static void LessThanOrEqualTest() =>
        Assert.Equal(-1, Vector.LessThanOrEqual(new Vector<int>(2), new Vector<int>(2))[0]);

    [Fact]
    public static void MaxTest() => Assert.Equal(3, Vector.Max(new Vector<int>(2), new Vector<int>(3))[0]);

    [Fact]
    public static void MinTest() => Assert.Equal(2, Vector.Min(new Vector<int>(2), new Vector<int>(3))[0]);

    [Fact]
    public static void MaxFloatTest() =>
        Assert.Equal(3.0f, Vector.Max(new Vector<float>(2.0f), new Vector<float>(3.0f))[0]);

    [Fact]
    public static void MinFloatTest() =>
        Assert.Equal(2.0f, Vector.Min(new Vector<float>(2.0f), new Vector<float>(3.0f))[0]);

    [Fact]
    public static void MaxMagnitudeTest() =>
        Assert.Equal(-4.0f, Vector.MaxMagnitude(new Vector<float>(-4.0f), new Vector<float>(3.0f))[0]);

    [Fact]
    public static void MinMagnitudeTest() =>
        Assert.Equal(3.0f, Vector.MinMagnitude(new Vector<float>(-4.0f), new Vector<float>(3.0f))[0]);

    [Fact]
    public static void MaxNumberTest() =>
        Assert.Equal(3.0f, Vector.MaxNumber(new Vector<float>(2.0f), new Vector<float>(3.0f))[0]);

    [Fact]
    public static void MinNumberTest() =>
        Assert.Equal(2.0f, Vector.MinNumber(new Vector<float>(2.0f), new Vector<float>(3.0f))[0]);

    [Fact]
    public static void ConvertToDoubleInt64Test() =>
        Assert.Equal(42.0, Vector.ConvertToDouble(new Vector<long>(42))[0]);

    [Fact]
    public static void ConvertToDoubleUInt64Test() =>
        Assert.Equal(42.0, Vector.ConvertToDouble(new Vector<ulong>(42))[0]);

    [Fact]
    public static void ConvertToSingleInt32Test() =>
        Assert.Equal(42.0f, Vector.ConvertToSingle(new Vector<int>(42))[0]);

    [Fact]
    public static void ConvertToSingleUInt32Test() =>
        Assert.Equal(42.0f, Vector.ConvertToSingle(new Vector<uint>(42))[0]);

    [Fact]
    public static void ConvertToInt32Test() => Assert.Equal(42, Vector.ConvertToInt32(new Vector<float>(42.75f))[0]);

    [Fact]
    public static void ConvertToUInt32Test() => Assert.Equal(42u, Vector.ConvertToUInt32(new Vector<float>(42.75f))[0]);

    [Fact]
    public static void ConvertToInt64Test() => Assert.Equal(42L, Vector.ConvertToInt64(new Vector<double>(42.75))[0]);

    [Fact]
    public static void ConvertToUInt64Test() =>
        Assert.Equal(42UL, Vector.ConvertToUInt64(new Vector<double>(42.75))[0]);

    [Fact]
    public static void GetElementTest() =>
        Assert.Equal(42, Vector.GetElement(new Vector<int>(42), Vector<int>.Count - 1));

    [Fact]
    public static void AsVector128Test() => Assert.Equal(42, new Vector<int>(42).AsVector128()[0]);

    [Fact]
    public static void AsVector128ConstantTest() => Assert.Equal(42, new Vector<int>(42).AsVector128()[0]);

    [Fact]
    public static void FromVector128Test() => Assert.Equal(42, Vector128.Create(42).AsVector()[0]);

    [Fact]
    public static void NarrowInt16Test() =>
        Assert.Equal((short)43, Vector.Narrow(new Vector<int>(42), new Vector<int>(43))[Vector<int>.Count]);

    [Fact]
    public static void NarrowSingleTest() =>
        Assert.Equal(43.0f, Vector.Narrow(new Vector<double>(42.0), new Vector<double>(43.0))[Vector<double>.Count]);

    [Fact]
    public static void NarrowSaturateTest()
    {
        Vector<short> result = Vector.NarrowWithSaturation(
            new Vector<int>(int.MaxValue),
            new Vector<int>(int.MinValue)
        );
        Assert.True((result[0] == short.MaxValue) && (result[Vector<int>.Count] == short.MinValue));
    }

    [Fact]
    public static void NarrowSaturateUnsignedTest()
    {
        Vector<ushort> result = Vector.NarrowWithSaturation(new Vector<uint>(uint.MaxValue), new Vector<uint>(0));
        Assert.True((result[0] == ushort.MaxValue) && (result[Vector<uint>.Count] == 0));
    }

    [Fact]
    public static void WidenLowerInt64Test() => Assert.Equal(42L, Vector.WidenLower(new Vector<int>(42))[0]);

    [Fact]
    public static void WidenUpperInt64Test() => Assert.Equal(42L, Vector.WidenUpper(new Vector<int>(42))[0]);

    [Fact]
    public static void WidenLowerUInt64Test() => Assert.Equal(42UL, Vector.WidenLower(new Vector<uint>(42))[0]);

    [Fact]
    public static void WidenUpperUInt64Test() => Assert.Equal(42UL, Vector.WidenUpper(new Vector<uint>(42))[0]);

    [Fact]
    public static void WidenLowerDoubleTest() => Assert.Equal(42.0, Vector.WidenLower(new Vector<float>(42.0f))[0]);

    [Fact]
    public static void WidenUpperDoubleTest() => Assert.Equal(42.0, Vector.WidenUpper(new Vector<float>(42.0f))[0]);

    [Fact]
    public static void SumTest() => Assert.Equal(3 * Vector<int>.Count, Vector.Sum(new Vector<int>(3)));

    [Fact]
    public static void SumUIntTest() => Assert.Equal(3u * (uint)Vector<uint>.Count, Vector.Sum(new Vector<uint>(3)));

    [Fact]
    public static void SumFloatTest() =>
        Assert.Equal(3.0f * Vector<float>.Count, Vector.Sum(new Vector<float>(3.0f)));

    [Fact]
    public static void DotFloatTest() =>
        Assert.Equal(6.0f * Vector<float>.Count, Vector.Dot(new Vector<float>(2.0f), new Vector<float>(3.0f)));

    [Fact]
    public static void AlternatingTest() => Assert.Equal(2, Vector.CreateAlternatingSequence(1, 2)[1]);

    [Fact]
    public static void GeometricTest()
    {
        Vector<int> sequence = Vector.CreateGeometricSequence(1, 2);
        int expected = 1;

        for (int index = 0; index < Vector<int>.Count; index++)
        {
            Assert.Equal(expected, sequence[index]);
            expected *= 2;
        }
    }

    [Fact]
    public static void GeometricByteTest()
    {
        Vector<byte> sequence = Vector.CreateGeometricSequence((byte)200, (byte)2);
        byte expected = 200;

        for (int index = 0; index < Vector<byte>.Count; index++)
        {
            Assert.Equal(expected, sequence[index]);
            expected = unchecked((byte)(expected * 2));
        }
    }

    [Fact]
    public static void GeometricSingleTest()
    {
        const float initial = 1.0059024f;
        const float multiplier = 1.0064822f;
        Vector<float> sequence = Vector.CreateGeometricSequence(initial, multiplier);

        for (int index = 0; index < Vector<float>.Count; index++)
        {
            Assert.Equal(initial * MathF.Pow(multiplier, index), sequence[index]);
        }
    }

    [Fact]
    public static void GeometricDoubleTest()
    {
        const double initial = 1e-154;
        const double multiplier = 1e-50;
        Vector<double> sequence = Vector.CreateGeometricSequence(initial, multiplier);

        for (int index = 0; index < Vector<double>.Count; index++)
        {
            Assert.Equal(initial * Math.Pow(multiplier, index), sequence[index]);
        }
    }

    [Fact]
    public static void ConcatTest()
    {
        Vector<int> result =
            Vector.ConcatLowerLower(Vector.CreateSequence(0, 1), Vector.CreateSequence(100, 1));
        Assert.Equal(0, result[0]);
        Assert.Equal(100, result[Vector<int>.Count / 2]);
    }

    [Fact]
    public static void ConcatLowerUpperTest()
    {
        Vector<int> result =
            Vector.ConcatLowerUpper(Vector.CreateSequence(0, 1), Vector.CreateSequence(100, 1));
        Assert.Equal(0, result[0]);
        Assert.Equal(100 + (Vector<int>.Count / 2), result[Vector<int>.Count / 2]);
    }

    [Fact]
    public static void ConcatUpperLowerTest()
    {
        Vector<int> result =
            Vector.ConcatUpperLower(Vector.CreateSequence(0, 1), Vector.CreateSequence(100, 1));
        Assert.Equal(Vector<int>.Count / 2, result[0]);
        Assert.Equal(100, result[Vector<int>.Count / 2]);
    }

    [Fact]
    public static void ConcatUpperUpperTest()
    {
        Vector<int> result =
            Vector.ConcatUpperUpper(Vector.CreateSequence(0, 1), Vector.CreateSequence(100, 1));
        Assert.Equal(Vector<int>.Count / 2, result[0]);
        Assert.Equal(100 + (Vector<int>.Count / 2), result[Vector<int>.Count / 2]);
    }

    [Fact]
    public static void ZipTest() => Assert.Equal(2, Vector.ZipLower(new Vector<int>(1), new Vector<int>(2))[1]);

    [Fact]
    public static void ZipUpperTest()
    {
        Vector<int> result = Vector.ZipUpper(Vector.CreateSequence(0, 1), Vector.CreateSequence(100, 1));
        Assert.Equal(Vector<int>.Count / 2, result[0]);
        Assert.Equal(100 + (Vector<int>.Count / 2), result[1]);
    }

    [Fact]
    public static void UnzipTest() =>
        Assert.Equal(2, Vector.UnzipEven(new Vector<int>(1), new Vector<int>(2))[Vector<int>.Count / 2]);

    [Fact]
    public static void UnzipOddTest()
    {
        Vector<int> result = Vector.UnzipOdd(Vector.CreateSequence(0, 1), Vector.CreateSequence(100, 1));
        Assert.Equal(1, result[0]);
        Assert.Equal(101, result[Vector<int>.Count / 2]);
    }

    [Fact]
    public static void ReverseTest() =>
        Assert.Equal(Vector<int>.Count - 1, Vector.Reverse(Vector.CreateSequence(0, 1))[0]);

    [Fact]
    public static void WithElementTest() =>
        Assert.Equal(42, Vector.WithElement(Vector<int>.Zero, Vector<int>.Count - 1, 42)[Vector<int>.Count - 1]);

    [Fact]
    public static void WithElementConstantTest() => Assert.Equal(42, Vector.WithElement(Vector<int>.Zero, 1, 42)[1]);

    [Fact]
    public static void PiTest() => Assert.Equal(3, (int)Vector<float>.Pi[0]);

    [Fact]
    public static void NaNSingleTest() =>
        Assert.Equal(BitConverter.SingleToInt32Bits(float.NaN), BitConverter.SingleToInt32Bits(Vector<float>.NaN[0]));

    [Fact]
    public static void NaNDoubleTest() => Assert.Equal(BitConverter.DoubleToInt64Bits(double.NaN),
                                                       BitConverter.DoubleToInt64Bits(Vector<double>.NaN[0]));

    [Fact]
    public static void NegativeOneTest() => Assert.Equal(-1, Vector<int>.NegativeOne[0]);

    [Fact]
    public static void AsVector256Test() => Assert.Equal(1, Vector256<int>.One.AsVector()[0]);

    [Fact]
    public static void DivideIntTest() => Assert.Equal(42, (new Vector<int>(84) / new Vector<int>(2))[0]);

    [Fact]
    public static void DotLongTest() =>
        Assert.Equal(6 * Vector<long>.Count, (int)Vector.Dot(new Vector<long>(2), new Vector<long>(3)));

    [Fact]
    public static void SequenceLongTest() =>
        Assert.Equal(1, (int)Vector.CreateSequence(1L, DateTime.Now.Ticks | 1)[0]);
}
