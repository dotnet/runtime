// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

// JIT is able to replace "x / 2" with "x * 0.5" where 2 is a power of two float
// Make sure this optimization doesn't change the results

public class Program
{
    private static int resultCode = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        // Some corner cases
        var testValues = new List<double>(new []
            {
                0, 0.01, 1.333, 1/3.0, 0.5, 1, 2, 3, 4,
                MathF.PI, MathF.E, Math.PI, Math.E,
                float.MinValue, double.MinValue,
                float.MaxValue, double.MaxValue,
                int.MaxValue, long.MaxValue,
                int.MinValue, long.MinValue,
                float.NegativeInfinity, double.NegativeInfinity,
                float.PositiveInfinity, double.PositiveInfinity,
                float.NaN, double.NaN
            });

        foreach (double value in testValues)
        {
            TestPowOfTwo_Single((float)value);
            TestPowOfTwo_Single(-(float)value);

            TestPowOfTwo_Double(value);
            TestPowOfTwo_Double(-value);

            TestNotPowOfTwo_Single((float)value);
            TestNotPowOfTwo_Single(-(float)value);

            TestNotPowOfTwo_Double(value);
            TestNotPowOfTwo_Double(-value);
        }

        return resultCode;
    }

    private static void TestPowOfTwo_Single(float x)
    {
        // TestPowOfTwo_Single should contain 19 'mul' and 19 'div' instructions
        AssertEquals(expected: x / ConstToVar(2), actual: x / 2);
        AssertEquals(expected: x / ConstToVar(4), actual: x / 4);
        AssertEquals(expected: x / ConstToVar(8), actual: x / 8);
        AssertEquals(expected: x / ConstToVar(16), actual: x / 16);

        AssertEquals(expected: x / ConstToVar(134217728), actual: x / 134217728);
        AssertEquals(expected: x / ConstToVar(268435456), actual: x / 268435456);
        AssertEquals(expected: x / ConstToVar(536870912), actual: x / 536870912);
        AssertEquals(expected: x / ConstToVar(1073741824), actual: x / 1073741824);

        AssertEquals(expected: x / ConstToVar(0.5f), actual: x / 0.5f);
        AssertEquals(expected: x / ConstToVar(0.25f), actual: x / 0.25f);
        AssertEquals(expected: x / ConstToVar(0.125f), actual: x / 0.125f);
        AssertEquals(expected: x / ConstToVar(0.0625f), actual: x / 0.0625f);

        AssertEquals(expected: x / ConstToVar(0.0009765625f), actual: x / 0.0009765625f);
        AssertEquals(expected: x / ConstToVar(0.00048828125f), actual: x / 0.00048828125f);
        AssertEquals(expected: x / ConstToVar(0.00024414062f), actual: x / 0.00024414062f);
        AssertEquals(expected: x / ConstToVar(0.00012207031f), actual: x / 0.00012207031f);

        AssertEquals(expected: x / ConstToVar(-1073741824), actual: x / -1073741824);
        AssertEquals(expected: x / ConstToVar(-0.00012207031f), actual: x / -0.00012207031f);
        AssertEquals(expected: x / ConstToVar(-2147483648), actual: x / -2147483648);
    }


    private static void TestPowOfTwo_Double(double x)
    {
        // TestPowOfTwo_Double should contain 19 'mul' and 19 'div' instructions
        AssertEquals(expected: x / ConstToVar(2), actual: x / 2);
        AssertEquals(expected: x / ConstToVar(4), actual: x / 4);
        AssertEquals(expected: x / ConstToVar(8), actual: x / 8);
        AssertEquals(expected: x / ConstToVar(16), actual: x / 16);

        AssertEquals(expected: x / ConstToVar(9007199254740992), actual: x / 9007199254740992);
        AssertEquals(expected: x / ConstToVar(18014398509481984), actual: x / 18014398509481984);
        AssertEquals(expected: x / ConstToVar(36028797018963970), actual: x / 36028797018963970);
        AssertEquals(expected: x / ConstToVar(72057594037927940), actual: x / 72057594037927940);

        AssertEquals(expected: x / ConstToVar(0.5), actual: x / 0.5);
        AssertEquals(expected: x / ConstToVar(0.25), actual: x / 0.25);
        AssertEquals(expected: x / ConstToVar(0.125), actual: x / 0.125);
        AssertEquals(expected: x / ConstToVar(0.0625), actual: x / 0.0625);

        AssertEquals(expected: x / ConstToVar(0.00390625), actual: x / 0.00390625);
        AssertEquals(expected: x / ConstToVar(0.001953125), actual: x / 0.001953125);
        AssertEquals(expected: x / ConstToVar(0.00048828125), actual: x / 0.00048828125);
        AssertEquals(expected: x / ConstToVar(0.0001220703125), actual: x / 0.0001220703125);

        AssertEquals(expected: x / ConstToVar(-1073741824), actual: x / -1073741824);
        AssertEquals(expected: x / ConstToVar(-0.00012207031f), actual: x / -0.00012207031f);
        AssertEquals(expected: x / ConstToVar(-2147483648), actual: x / -2147483648);
    }

    private static void TestNotPowOfTwo_Single(float x)
    {
        // TestNotPowOfTwo_Single should not contain 'mul' instructions (the optimization should not be applied here)
        AssertEquals(expected: x / ConstToVar(3), actual: x / 3);
        AssertEquals(expected: x / ConstToVar(9), actual: x / 9);
        AssertEquals(expected: x / ConstToVar(2.5f), actual: x / 2.5f);
        AssertEquals(expected: x / ConstToVar(0.51f), actual: x / 0.51f);

        AssertEquals(expected: x / ConstToVar(-3), actual: x / -3);
        AssertEquals(expected: x / ConstToVar(-9), actual: x / -9);
        AssertEquals(expected: x / ConstToVar(-2.5f), actual: x / -2.5f);
        AssertEquals(expected: x / ConstToVar(-0.51f), actual: x / -0.51f);

        AssertEquals(expected: x / ConstToVar(0.0f), actual: x / 0.0f);
        AssertEquals(expected: x / ConstToVar(1.0f), actual: x / 1.0f);
        AssertEquals(expected: x / ConstToVar(-0.0f), actual: x / -0.0f);
        AssertEquals(expected: x / ConstToVar(-1.0f), actual: x / -1.0f);

        AssertEquals(expected: x / ConstToVar(float.Epsilon), actual: x / float.Epsilon);
        AssertEquals(expected: x / ConstToVar(float.MinValue), actual: x / float.MinValue);
        AssertEquals(expected: x / ConstToVar(float.MaxValue), actual: x / float.MaxValue);
        AssertEquals(expected: x / ConstToVar(float.PositiveInfinity), actual: x / float.PositiveInfinity);
        AssertEquals(expected: x / ConstToVar(float.NegativeInfinity), actual: x / float.NegativeInfinity);
    }

    private static void TestNotPowOfTwo_Double(double x)
    {
        // TestNotPowOfTwo_Double should not contain 'mul' instructions (the optimization should not be applied here)
        AssertEquals(expected: x / ConstToVar(3), actual: x / 3);
        AssertEquals(expected: x / ConstToVar(9), actual: x / 9);
        AssertEquals(expected: x / ConstToVar(2.5), actual: x / 2.5);
        AssertEquals(expected: x / ConstToVar(0.51), actual: x / 0.51);

        AssertEquals(expected: x / ConstToVar(-3), actual: x / -3);
        AssertEquals(expected: x / ConstToVar(-9), actual: x / -9);
        AssertEquals(expected: x / ConstToVar(-2.5), actual: x / -2.5);
        AssertEquals(expected: x / ConstToVar(-0.51), actual: x / -0.51);

        AssertEquals(expected: x / ConstToVar(0.00024414062), actual: x / 0.00024414062);
        AssertEquals(expected: x / ConstToVar(0.00012207031), actual: x / 0.00012207031);

        AssertEquals(expected: x / ConstToVar(0.0), actual: x / 0.0);
        AssertEquals(expected: x / ConstToVar(1.0), actual: x / 1.0);
        AssertEquals(expected: x / ConstToVar(-0.0), actual: x / -0.0);
        AssertEquals(expected: x / ConstToVar(-1.0), actual: x / -1.0);

        AssertEquals(expected: x / ConstToVar(double.Epsilon), actual: x / double.Epsilon);
        AssertEquals(expected: x / ConstToVar(double.MinValue), actual: x / double.MinValue);
        AssertEquals(expected: x / ConstToVar(double.MaxValue), actual: x / double.MaxValue);
        AssertEquals(expected: x / ConstToVar(double.PositiveInfinity), actual: x / double.PositiveInfinity);
        AssertEquals(expected: x / ConstToVar(double.NegativeInfinity), actual: x / double.NegativeInfinity);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertEquals(float expected, float actual)
    {
        if (Single.IsNaN(expected) && Single.IsNaN(actual))
        {
            // There can be multiple configurations for NaN values
            // verifying that these values are NaNs should be enough
            return;
        }

        int expectedi = BitConverter.SingleToInt32Bits(expected);
        int actuali = BitConverter.SingleToInt32Bits(actual);
        if (expectedi != actuali)
        {
            resultCode--;
            Console.WriteLine($"AssertEquals: {expected} != {actual}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertEquals(double expected, double actual)
    {
        if (Double.IsNaN(expected) && Double.IsNaN(actual))
        {
            // There can be multiple configurations for NaN values
            // verifying that these values are NaNs should be enough
            return;
        }

        long expectedi = BitConverter.DoubleToInt64Bits(expected);
        long actuali = BitConverter.DoubleToInt64Bits(actual);
        if (expectedi != actuali)
        {
            resultCode--;
            Console.WriteLine($"AssertEquals: {expected} != {actual}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ConstToVar<T>(T v) => v;
}
