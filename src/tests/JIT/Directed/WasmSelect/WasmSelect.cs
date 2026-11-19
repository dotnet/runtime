// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class WasmSelect
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void SelectValues(bool condition)
    {
        Assert.Equal(condition ? 18 : 12, SelectInt(condition, 11, 21));
        Assert.Equal(condition ? 0x100000012L : 0x20000000cL, SelectLong(condition, 0x10000000bL, 0x200000015L));
        Assert.Equal(condition ? 18.5f : 12.5f, SelectFloat(condition, 11.5f, 21.5f));
        Assert.Equal(condition ? 18.5 : 12.5, SelectDouble(condition, 11.5, 21.5));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SelectInt(bool condition, int x, int y) => condition ? x + 7 : y - 9;

    [Theory]
    [InlineData(5, 2, 18)]
    [InlineData(2, 5, 12)]
    [InlineData(2, 2, 12)]
    public static void SelectWithComparison(int left, int right, int expected) =>
        Assert.Equal(expected, SelectCompare(left, right, 11, 21));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SelectCompare(int left, int right, int x, int y) => left > right ? x + 7 : y - 9;

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 1)]
    [InlineData(4, 1)]
    [InlineData(6, 1)]
    public static void NormalizeComparisonResult(int value, int expected) =>
        Assert.Equal(expected, SelectBooleanValue(value));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SelectBooleanValue(int value)
    {
        const int Mask = 6;
        int result;
        if ((value & Mask) != 0)
        {
            result = 1;
        }
        else
        {
            result = 0;
        }

        // Keep both assignments as stores to the same local until if-conversion.
        return ReadResult(ref result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadResult(ref int result) => result;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SelectLong(bool condition, long x, long y) => condition ? x + 7 : y - 9;

    [Theory]
    [InlineData(false, 0x200000015L)]
    [InlineData(true, 0x10000000bL)]
    public static void SelectLongValue(bool condition, long expected) =>
        Assert.Equal(expected, SelectLongBits(condition, 0x10000000bL, 0x200000015L));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SelectLongBits(bool condition, long x, long y) => condition ? x : y;

    [Theory]
    [InlineData(false, 0L)]
    [InlineData(true, long.MinValue)]
    public static void SelectToStackLocal(bool condition, long expected)
    {
        Assert.Equal(expected, BitConverter.DoubleToInt64Bits(SelectStackLocal(condition)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double SelectStackLocal(bool condition)
    {
        long bits;
        if (condition)
        {
            bits = long.MinValue;
        }
        else
        {
            bits = 0;
        }
        return Unsafe.As<long, double>(ref bits);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float SelectFloat(bool condition, float x, float y) => condition ? x + 7 : y - 9;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double SelectDouble(bool condition, double x, double y) => condition ? x + 7 : y - 9;

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, int.MinValue)]
    public static void PreserveFloatBits(bool condition, int expected)
    {
        Assert.Equal(expected, BitConverter.SingleToInt32Bits(SelectFloatBits(condition, -0.0f, 0.0f)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float SelectFloatBits(bool condition, float x, float y) => condition ? x : y;

    [Theory]
    [InlineData(false, false, 33)]
    [InlineData(false, true, 33)]
    [InlineData(true, false, 22)]
    [InlineData(true, true, 11)]
    public static void SelectNestedValues(bool outer, bool inner, int expected)
    {
        Assert.Equal(expected, SelectNested(outer, inner, 10, 20, 30));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SelectNested(bool outer, bool inner, int x, int y, int z) =>
        outer ? (inner ? x + 1 : y + 2) : z + 3;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void PreserveNaNBits(bool condition)
    {
        const long bits = 0x7ff8000000001234L;
        double nan = BitConverter.Int64BitsToDouble(bits);
        double result = SelectDoubleBits(condition, nan, -0.0);
        Assert.Equal(condition ? bits : long.MinValue, BitConverter.DoubleToInt64Bits(result));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double SelectDoubleBits(bool condition, double x, double y) => condition ? x : y;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void DoNotEvaluateThrowingArm(bool condition)
    {
        int[] values = [42];
        Assert.Equal(42, SelectArrayValue(condition, values, 0));
        if (!condition)
        {
            Assert.Equal(42, SelectArrayValue(condition, values, 2));
        }
        else
        {
            Assert.Throws<IndexOutOfRangeException>(() => SelectArrayValue(condition, values, 2));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SelectArrayValue(bool condition, int[] values, int index) => condition ? values[index] : 42;

    [Fact]
    public static void EvaluateThrowingCondition()
    {
        int[] values = [1, -1];
        Assert.Equal(18, SelectWithArrayCondition(values, 0, 11, 21));
        Assert.Equal(12, SelectWithArrayCondition(values, 1, 11, 21));
        Assert.Throws<IndexOutOfRangeException>(() => SelectWithArrayCondition(values, 2, 11, 21));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SelectWithArrayCondition(int[] values, int index, int x, int y) =>
        values[index] > 0 ? x + 7 : y - 9;

    [Fact]
    public static void PreserveExceptionOrder()
    {
        int[] values = [1];
        Assert.Equal(20, SelectBeforeDivision(values, 0, 5));
        Assert.Throws<IndexOutOfRangeException>(() => SelectBeforeDivision(values, 1, 0));
        Assert.Throws<DivideByZeroException>(() => SelectBeforeDivision(values, 0, 0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SelectBeforeDivision(int[] values, int index, int divisor) =>
        (values[index] > 0 ? 18 : 12) + 10 / divisor;
}
