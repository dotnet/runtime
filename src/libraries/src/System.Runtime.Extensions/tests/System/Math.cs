// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public static unsafe class MathTests
{
    [Fact]
    public static void TestCos()
    {
        double d;

        d = Math.Cos(1.0);
        IsEqual(d, 0.54030230586814);
        d = Math.Cos(0.0);
        IsEqual(d, 1.0);
        d = Math.Cos(-1.0);
        IsEqual(d, 0.54030230586814);
        d = Math.Cos(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Cos(Double.PositiveInfinity);
        IsEqual(d, Double.NaN);
        d = Math.Cos(Double.NegativeInfinity);
        IsEqual(d, Double.NaN);
    }

    [Fact]
    public static void TestSin()
    {
        double d;

        d = Math.Sin(1.0);
        IsEqual(d, 0.841470984807897);
        d = Math.Sin(0.0);
        IsEqual(d, 0.0);
        d = Math.Sin(-1.0);
        IsEqual(d, -0.841470984807897);
        d = Math.Sin(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Sin(Double.PositiveInfinity);
        IsEqual(d, Double.NaN);
        d = Math.Sin(Double.NegativeInfinity);
        IsEqual(d, Double.NaN);
    }

    [Fact]
    public static void TestTan()
    {
        double d;

        d = Math.Tan(1.0);
        IsEqual(d, 1.5574077246549);
        d = Math.Tan(0.0);
        IsEqual(d, 0.0);
        d = Math.Tan(-1.0);
        IsEqual(d, -1.5574077246549);
        d = Math.Tan(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Tan(Double.PositiveInfinity);
        IsEqual(d, Double.NaN);
        d = Math.Tan(Double.NegativeInfinity);
        IsEqual(d, Double.NaN);
    }

    [Fact]
    public static void TestCosh()
    {
        double d;

        d = Math.Cosh(1.0);
        IsEqual(d, 1.54308063481524);
        d = Math.Cosh(0.0);
        IsEqual(d, 1.0);
        d = Math.Cosh(-1.0);
        IsEqual(d, 1.54308063481524);
        d = Math.Cosh(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Cosh(Double.PositiveInfinity);
        IsEqual(d, Double.PositiveInfinity);
        d = Math.Cosh(Double.NegativeInfinity);
        IsEqual(d, Double.PositiveInfinity);
    }

    [Fact]
    public static void TestSinh()
    {
        double d;

        d = Math.Sinh(1.0);
        IsEqual(d, 1.1752011936438);
        d = Math.Sinh(0.0);
        IsEqual(d, 0.0);
        d = Math.Sinh(-1.0);
        IsEqual(d, -1.1752011936438);
        d = Math.Sinh(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Sinh(Double.PositiveInfinity);
        IsEqual(d, Double.PositiveInfinity);
        d = Math.Sinh(Double.NegativeInfinity);
        IsEqual(d, Double.NegativeInfinity);
    }

    [Fact]
    public static void TestTanh()
    {
        double d;

        d = Math.Tanh(1.0);
        IsEqual(d, 0.761594155955765);
        d = Math.Tanh(0.0);
        IsEqual(d, 0.0);
        d = Math.Tanh(-1.0);
        IsEqual(d, -0.761594155955765);
        d = Math.Tanh(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Tanh(Double.PositiveInfinity);
        IsEqual(d, 1.0);
        d = Math.Tanh(Double.NegativeInfinity);
        IsEqual(d, -1.0);
    }

    [Fact]
    public static void TestACos()
    {
        double d;

        d = Math.Acos(1.0);
        IsEqual(d, 0.0);
        d = Math.Acos(0.0);
        IsEqual(d, 1.5707963267949);
        d = Math.Acos(-1.0);
        IsEqual(d, 3.14159265358979);
        d = Math.Acos(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Acos(Double.PositiveInfinity);
        IsEqual(d, Double.NaN);
        d = Math.Acos(Double.NegativeInfinity);
        IsEqual(d, Double.NaN);
    }

    [Fact]
    public static void TestASin()
    {
        double d;

        d = Math.Asin(1.0);
        IsEqual(d, 1.5707963267949);
        d = Math.Asin(0.0);
        IsEqual(d, 0.0);
        d = Math.Asin(-1.0);
        IsEqual(d, -1.5707963267949);
        d = Math.Asin(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Asin(Double.PositiveInfinity);
        IsEqual(d, Double.NaN);
        d = Math.Asin(Double.NegativeInfinity);
        IsEqual(d, Double.NaN);
    }

    [Fact]
    public static void TestATan()
    {
        double d;

        d = Math.Atan(1.0);
        IsEqual(d, 0.785398163397448);
        d = Math.Atan(0.0);
        IsEqual(d, 0.0);
        d = Math.Atan(-1.0);
        IsEqual(d, -0.785398163397448);
        d = Math.Atan(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Atan(Double.PositiveInfinity);
        IsEqual(d, 1.5707963267949);
        d = Math.Atan(Double.NegativeInfinity);
        IsEqual(d, -1.5707963267949);
    }

    [Fact]
    public static void TestATan2()
    {
        double d;

        d = Math.Atan2(0.0, 0.0);
        IsEqual(d, 0.0);
        d = Math.Atan2(1.0, 0.0);
        IsEqual(d, 1.5707963267949);
        d = Math.Atan2(2.0, 3.0);
        IsEqual(d, 0.588002603547568);
        d = Math.Atan2(0.0, 3.0);
        IsEqual(d, 0.0);
        d = Math.Atan2(-2.0, 3.0);
        IsEqual(d, -0.588002603547568);
        d = Math.Atan2(Double.NaN, 1.0);
        IsEqual(d, Double.NaN);
        d = Math.Atan2(1.0, Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Atan2(Double.PositiveInfinity, 1.0);
        IsEqual(d, 1.5707963267949);
        d = Math.Atan2(Double.NegativeInfinity, 1.0);
        IsEqual(d, -1.5707963267949);
        d = Math.Atan2(1.0, Double.PositiveInfinity);
        IsEqual(d, 0.0);
        d = Math.Atan2(1.0, Double.NegativeInfinity);
        IsEqual(d, 3.14159265358979);
    }

    [Fact]
    public static void TestCeiling()
    {
        double d;

        d = Math.Ceiling(1.1);
        IsEqual(d, 2.0);
        d = Math.Ceiling(1.9);
        IsEqual(d, 2.0);
        d = Math.Ceiling(-1.1);
        IsEqual(d, -1.0);
        d = Math.Ceiling(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Ceiling(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Ceiling(Double.NaN);
        IsEqual(d, Double.NaN);
    }

    [Fact]
    public static void TestFloor()
    {
        double d;

        d = Math.Floor(1.1);
        IsEqual(d, 1.0);
        d = Math.Floor(1.9);
        IsEqual(d, 1.0);
        d = Math.Floor(-1.1);
        IsEqual(d, -2.0);
        d = Math.Floor(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Floor(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Floor(Double.NaN);
        IsEqual(d, Double.NaN);
    }

    [Fact]
    public static void TestRound()
    {
        double d;

        d = Math.Round(0.0);
        IsEqual(d, 0.0);

        d = Math.Round(1.4);
        IsEqual(d, 1.0);

        d = Math.Round(1.5);
        IsEqual(d, 2.0);

        d = Math.Round(2e16);
        IsEqual(d, 2e16);

        d = Math.Round(-0.0);
        IsEqual(d, 0.0);

        d = Math.Round(-1.4);
        IsEqual(d, -1.0);

        d = Math.Round(-1.5);
        IsEqual(d, -2.0);

        d = Math.Round(-2e16);
        IsEqual(d, -2e16);
    }

    [Fact]
    public static void TestRoundDigits()
    {
        double d;

        d = Math.Round(3.42156, 3, MidpointRounding.AwayFromZero);
        IsEqual(d, 3.422);

        d = Math.Round(-3.42156, 3, MidpointRounding.AwayFromZero);
        IsEqual(d, -3.422);

        d = Math.Round(0.0, 3, MidpointRounding.AwayFromZero);
        IsEqual(d, 0.0);

        d = Math.Round(Double.NaN, 3, MidpointRounding.AwayFromZero);
        IsEqual(d, Double.NaN);

        d = Math.Round(Double.PositiveInfinity, 3, MidpointRounding.AwayFromZero);
        IsEqual(d, Double.PositiveInfinity);

        d = Math.Round(Double.NegativeInfinity, 3, MidpointRounding.AwayFromZero);
        IsEqual(d, Double.NegativeInfinity);

        Decimal dec;
        dec = Math.Round((Decimal)3.42156, 3, MidpointRounding.AwayFromZero);
        Assert.Equal(dec, (Decimal)3.422);

        dec = Math.Round((Decimal)(-3.42156), 3, MidpointRounding.AwayFromZero);
        Assert.Equal(dec, (Decimal)(-3.422));

        dec = Math.Round(Decimal.Zero, 3, MidpointRounding.AwayFromZero);
        Assert.Equal(dec, Decimal.Zero);
    }

    [Fact]
    public static void TestSqrt()
    {
        double d;

        d = Math.Sqrt(3.0);
        IsEqual(d, 1.73205080756888);
        d = Math.Sqrt(0.0);
        IsEqual(d, 0.0);
        d = Math.Sqrt(-3.0);
        IsEqual(d, Double.NaN);
        d = Math.Sqrt(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Sqrt(Double.PositiveInfinity);
        IsEqual(d, Double.PositiveInfinity);
        d = Math.Sqrt(Double.NegativeInfinity);
        IsEqual(d, Double.NaN);
    }

    [Fact]
    public static void TestLog()
    {
        double d;

        d = Math.Log(3.0);
        IsEqual(d, 1.09861228866811);
        d = Math.Log(0.0);
        IsEqual(d, Double.NegativeInfinity);
        d = Math.Log(-3.0);
        IsEqual(d, Double.NaN);
        d = Math.Log(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Log(Double.PositiveInfinity);
        IsEqual(d, Double.PositiveInfinity);
        d = Math.Log(Double.NegativeInfinity);
        IsEqual(d, Double.NaN);
    }

    [Fact]
    public static void TestLog10()
    {
        double d;

        d = Math.Log10(3.0);
        IsEqual(d, 0.477121254719662);
        d = Math.Log10(0.0);
        IsEqual(d, Double.NegativeInfinity);
        d = Math.Log10(-3.0);
        IsEqual(d, Double.NaN);
        d = Math.Log10(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Log10(Double.PositiveInfinity);
        IsEqual(d, Double.PositiveInfinity);
        d = Math.Log10(Double.NegativeInfinity);
        IsEqual(d, Double.NaN);
    }

    [Fact]
    public static void TestPow()
    {
        double d;

        d = Math.Pow(0.0, 0.0);
        IsEqual(d, 1.0);
        d = Math.Pow(1.0, 0.0);
        IsEqual(d, 1.0);
        d = Math.Pow(2.0, 3.0);
        IsEqual(d, 8.0);
        d = Math.Pow(0.0, 3.0);
        IsEqual(d, 0.0);
        d = Math.Pow(-2.0, 3.0);
        IsEqual(d, -8.0);
        d = Math.Pow(Double.NaN, 1.0);
        IsEqual(d, Double.NaN);
        d = Math.Pow(1.0, Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Pow(Double.PositiveInfinity, 1.0);
        IsEqual(d, Double.PositiveInfinity);
        d = Math.Pow(Double.NegativeInfinity, 1.0);
        IsEqual(d, Double.NegativeInfinity);
        d = Math.Pow(1.0, Double.PositiveInfinity);
        IsEqual(d, 1.0);
        d = Math.Pow(1.0, Double.NegativeInfinity);
        IsEqual(d, 1.0);
        d = Math.Pow(-1.0, Double.PositiveInfinity);
        IsEqual(d, Double.NaN);
        d = Math.Pow(-1.0, Double.NegativeInfinity);
        IsEqual(d, Double.NaN);
        d = Math.Pow(1.1, Double.PositiveInfinity);
        IsEqual(d, Double.PositiveInfinity);
        d = Math.Pow(1.1, Double.NegativeInfinity);
        IsEqual(d, 0.0);
    }

    [Fact]
    public static void TestFAbs()
    {
        float d;

        d = Math.Abs(3.0f);
        IsEqual(d, 3.0);
        d = Math.Abs(0.0f);
        IsEqual(d, 0.0);
        d = Math.Abs(-3.0f);
        IsEqual(d, 3.0);
        d = Math.Abs(Single.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Abs(Single.PositiveInfinity);
        IsEqual(d, Double.PositiveInfinity);
        d = Math.Abs(Single.NegativeInfinity);
        IsEqual(d, Double.PositiveInfinity);
    }

    [Fact]
    public static void TestAbs()
    {
        double d;

        d = Math.Abs(3.0);
        IsEqual(d, 3.0);
        d = Math.Abs(0.0);
        IsEqual(d, 0.0);
        d = Math.Abs(-3.0);
        IsEqual(d, 3.0);
        d = Math.Abs(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Abs(Double.PositiveInfinity);
        IsEqual(d, Double.PositiveInfinity);
        d = Math.Abs(Double.NegativeInfinity);
        IsEqual(d, Double.PositiveInfinity);
    }

    [Fact]
    public static void TestExp()
    {
        double d;

        d = Math.Exp(3.0);
        IsEqual(d, 20.0855369231877);
        d = Math.Exp(0.0);
        IsEqual(d, 1.0);
        d = Math.Exp(-3.0);
        IsEqual(d, 0.0497870683678639);
        d = Math.Exp(Double.NaN);
        IsEqual(d, Double.NaN);
        d = Math.Exp(Double.PositiveInfinity);
        IsEqual(d, Double.PositiveInfinity);
        d = Math.Exp(Double.NegativeInfinity);
        IsEqual(d, 0.0);
    }

    [Fact]
    public static void TestDecimalAbs()
    {
        Decimal d;

        d = Math.Abs(new Decimal(3.0));
        IsEqual(Decimal.ToDouble(d), 3.0);
        d = Math.Abs(new Decimal(-3.0));
        IsEqual(Decimal.ToDouble(d), 3.0);
    }

    [Fact]
    public static void TestDecimalMin()
    {
        Decimal d;

        d = Math.Min(new Decimal(3.0), new Decimal(-2.0));
        Assert.Equal(d, new Decimal(-2.0));
    }

    [Fact]
    public static void TestDecimalMax()
    {
        Decimal d;

        d = Math.Max(new Decimal(3.0), new Decimal(-2.0));
        Assert.Equal(d, new Decimal(3.0));
    }

    private static void IsEqual(double d1, double d2)
    {
        if (d1 == d2)
            return;
        if (Double.IsNaN(d1) && Double.IsNaN(d2))
            return;
        double delta = d2 - d1;
        if (delta < 0.0)
            delta = -delta;

        if (d1 == 0.0 && delta < 0.00000001)
            return;
        double dividend = d1;
        if (dividend < 0)
            dividend = -dividend;
        if ((delta / dividend) < 0.0001)
            return;

        throw new Exception("Doubles not equal: " + d1 + " != " + d2);
    }
}

