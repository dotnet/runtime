// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

public static class DecimalTests
{
    [Fact]
    public static void TestEquals()
    {
        // Boolean Decimal.Equals(Decimal)
        Assert.True(Decimal.Zero.Equals(Decimal.Zero));
        Assert.False(Decimal.Zero.Equals(Decimal.One));
        Assert.True(Decimal.MaxValue.Equals(Decimal.MaxValue));
        Assert.True(Decimal.MinValue.Equals(Decimal.MinValue));
        Assert.False(Decimal.MaxValue.Equals(Decimal.MinValue));
        Assert.False(Decimal.MinValue.Equals(Decimal.MaxValue));
    }

    [Fact]
    public static void TestEqualsDecDec()
    {
        // Boolean Decimal.Equals(Decimal, Decimal)
        Assert.True(Decimal.Equals(Decimal.Zero, Decimal.Zero));
        Assert.False(Decimal.Equals(Decimal.Zero, Decimal.One));
        Assert.True(Decimal.Equals(Decimal.MaxValue, Decimal.MaxValue));
        Assert.True(Decimal.Equals(Decimal.MinValue, Decimal.MinValue));
        Assert.False(Decimal.Equals(Decimal.MinValue, Decimal.MaxValue));
        Assert.False(Decimal.Equals(Decimal.MaxValue, Decimal.MinValue));
    }

    [Fact]
    public static void TestEqualsObj()
    {
        // Boolean Decimal.Equals(Object)
        Assert.True(Decimal.Zero.Equals((object)Decimal.Zero));
        Assert.False(Decimal.Zero.Equals((object)Decimal.One));
        Assert.True(Decimal.MaxValue.Equals((object)Decimal.MaxValue));
        Assert.True(Decimal.MinValue.Equals((object)Decimal.MinValue));
        Assert.False(Decimal.MinValue.Equals((object)Decimal.MaxValue));
        Assert.False(Decimal.MaxValue.Equals((object)Decimal.MinValue));
        Assert.False(Decimal.One.Equals(null));
        Assert.False(Decimal.One.Equals("one"));
        Assert.False(Decimal.One.Equals((object)1));
    }

    [Fact]
    public static void Testop_Equality()
    {
        // Boolean Decimal.op_Equality(Decimal, Decimal)
        Assert.True(Decimal.Zero == Decimal.Zero);
        Assert.False(Decimal.Zero == Decimal.One);
        Assert.True(Decimal.MaxValue == Decimal.MaxValue);
        Assert.True(Decimal.MinValue == Decimal.MinValue);
        Assert.False(Decimal.MinValue == Decimal.MaxValue);
        Assert.False(Decimal.MaxValue == Decimal.MinValue);
    }

    [Fact]
    public static void Testop_GreaterThan()
    {
        // Boolean Decimal.op_GreaterThan(Decimal, Decimal)
        Assert.False(Decimal.Zero > Decimal.Zero);
        Assert.False(Decimal.Zero > Decimal.One);
        Assert.True(Decimal.One > Decimal.Zero);
        Assert.False(Decimal.MaxValue > Decimal.MaxValue);
        Assert.False(Decimal.MinValue > Decimal.MinValue);
        Assert.False(Decimal.MinValue > Decimal.MaxValue);
        Assert.True(Decimal.MaxValue > Decimal.MinValue);
    }

    [Fact]
    public static void Testop_GreaterThanOrEqual()
    {
        // Boolean Decimal.op_GreaterThanOrEqual(Decimal, Decimal)
        Assert.True(Decimal.Zero >= Decimal.Zero);
        Assert.False(Decimal.Zero >= Decimal.One);
        Assert.True(Decimal.One >= Decimal.Zero);
        Assert.True(Decimal.MaxValue >= Decimal.MaxValue);
        Assert.True(Decimal.MinValue >= Decimal.MinValue);
        Assert.False(Decimal.MinValue >= Decimal.MaxValue);
        Assert.True(Decimal.MaxValue >= Decimal.MinValue);
    }

    [Fact]
    public static void Testop_Inequality()
    {
        // Boolean Decimal.op_Inequality(Decimal, Decimal)
        Assert.False(Decimal.Zero != Decimal.Zero);
        Assert.True(Decimal.Zero != Decimal.One);
        Assert.True(Decimal.One != Decimal.Zero);
        Assert.False(Decimal.MaxValue != Decimal.MaxValue);
        Assert.False(Decimal.MinValue != Decimal.MinValue);
        Assert.True(Decimal.MinValue != Decimal.MaxValue);
        Assert.True(Decimal.MaxValue != Decimal.MinValue);
    }

    [Fact]
    public static void Testop_LessThan()
    {
        // Boolean Decimal.op_LessThan(Decimal, Decimal)
        Assert.False(Decimal.Zero < Decimal.Zero);
        Assert.True(Decimal.Zero < Decimal.One);
        Assert.False(Decimal.One < Decimal.Zero);
        Assert.True(5m < 15m);
        decimal d5 = 5;
        decimal d3 = 3;
        Assert.False(d5 < d3);
        Assert.False(Decimal.MaxValue < Decimal.MaxValue);
        Assert.False(Decimal.MinValue < Decimal.MinValue);
        Assert.True(Decimal.MinValue < Decimal.MaxValue);
        Assert.False(Decimal.MaxValue < Decimal.MinValue);
    }

    [Fact]
    public static void Testop_LessThanOrEqual()
    {
        // Boolean Decimal.op_LessThanOrEqual(Decimal, Decimal)
        Assert.True(Decimal.Zero <= Decimal.Zero);
        Assert.True(Decimal.Zero <= Decimal.One);
        Assert.False(Decimal.One <= Decimal.Zero);
        Assert.True(Decimal.MaxValue <= Decimal.MaxValue);
        Assert.True(Decimal.MinValue <= Decimal.MinValue);
        Assert.True(Decimal.MinValue <= Decimal.MaxValue);
        Assert.False(Decimal.MaxValue <= Decimal.MinValue);
    }

    [Fact]
    public static void TestToByte()
    {
        // Byte Decimal.ToByte(Decimal)
        Assert.Equal(0, Decimal.ToByte(0));
        Assert.Equal(1, Decimal.ToByte(1));
        Assert.Equal(255, Decimal.ToByte(255));

        Assert.Throws<OverflowException>(() => Decimal.ToByte(256));
    }

    private static void VerifyAdd<T>(Decimal d1, Decimal d2, Decimal expected = Decimal.Zero) where T : Exception
    {
        bool expectFailure = typeof(T) != typeof(Exception);

        try
        {
            Decimal result1 = Decimal.Add(d1, d2);
            Decimal result2 = d1 + d2;

            Assert.False(expectFailure, "Expected an exception to be thrown");
            Assert.Equal(result1, result2);
            Assert.Equal(expected, result1);
        }
        catch (T)
        {
            Assert.True(expectFailure, "Didn't expect an exception to be thrown");
        }
    }
    [Fact]
    public static void TestAdd()
    {
        // Decimal Decimal.Add(Decimal, Decimal)
        // Decimal Decimal.op_Addition(Decimal, Decimal)
        VerifyAdd<Exception>(1, 1, 2);
        VerifyAdd<Exception>(-1, 1, 0);
        VerifyAdd<Exception>(1, -1, 0);
        VerifyAdd<Exception>(Decimal.MaxValue, Decimal.Zero, Decimal.MaxValue);
        VerifyAdd<Exception>(Decimal.MinValue, Decimal.Zero, Decimal.MinValue);
        VerifyAdd<Exception>(79228162514264337593543950330m, 5, Decimal.MaxValue);
        VerifyAdd<Exception>(79228162514264337593543950330m, -5, 79228162514264337593543950325m);
        VerifyAdd<Exception>(-79228162514264337593543950330m, -5, Decimal.MinValue);
        VerifyAdd<Exception>(-79228162514264337593543950330m, 5, -79228162514264337593543950325m);
        VerifyAdd<Exception>(1234.5678m, 0.00009m, 1234.56789m);
        VerifyAdd<Exception>(-1234.5678m, 0.00009m, -1234.56771m);
        VerifyAdd<Exception>(0.1111111111111111111111111111m,
                             0.1111111111111111111111111111m,
                             0.2222222222222222222222222222m);
        VerifyAdd<Exception>(0.5555555555555555555555555555m,
                             0.5555555555555555555555555555m,
                             1.1111111111111111111111111110m);

        // Exceptions
        VerifyAdd<OverflowException>(Decimal.MaxValue, Decimal.MaxValue);
        VerifyAdd<OverflowException>(79228162514264337593543950330m, 6);
        VerifyAdd<OverflowException>(-79228162514264337593543950330m, -6, Decimal.MinValue);
    }

    [Fact]
    public static void TestCeiling()
    {
        // Decimal Decimal.Ceiling(Decimal)
        Assert.Equal<Decimal>(123, Decimal.Ceiling((Decimal)123));
        Assert.Equal<Decimal>(124, Decimal.Ceiling((Decimal)123.123));
        Assert.Equal<Decimal>(-123, Decimal.Ceiling((Decimal)(-123.123)));
        Assert.Equal<Decimal>(124, Decimal.Ceiling((Decimal)123.567));
        Assert.Equal<Decimal>(-123, Decimal.Ceiling((Decimal)(-123.567)));
    }

    private static void VerifyDivide<T>(Decimal d1, Decimal d2, Decimal expected = Decimal.Zero) where T : Exception
    {
        bool expectFailure = typeof(T) != typeof(Exception);

        try
        {
            Decimal result1 = Decimal.Divide(d1, d2);
            Decimal result2 = d1 / d2;

            Assert.False(expectFailure, "Expected an exception to be thrown");
            Assert.Equal(result1, result2);
            Assert.Equal(expected, result1);
        }
        catch (T)
        {
            Assert.True(expectFailure, "Didn't expect an exception to be thrown");
        }
    }
    [Fact]
    public static void TestDivide()
    {
        // Decimal Decimal.Divide(Decimal, Decimal)
        // Decimal Decimal.op_Division(Decimal, Decimal)

        // Vanilla cases
        VerifyDivide<Exception>(Decimal.One, Decimal.One, Decimal.One);
        VerifyDivide<Exception>(Decimal.MaxValue, Decimal.MinValue, Decimal.MinusOne);
        VerifyDivide<Exception>(0.9214206543486529434634231456m, Decimal.MaxValue, Decimal.Zero);
        VerifyDivide<Exception>(38214206543486529434634231456m, 0.49214206543486529434634231456m, 77648730371625094566866001277m);
        VerifyDivide<Exception>(-78228162514264337593543950335m, Decimal.MaxValue, -0.987378225516463811113412343m);
        VerifyDivide<Exception>(5m + 10m, 2m, 7.5m);
        VerifyDivide<Exception>(10m, 2m, 5m);

        // Tests near MaxValue (VSWhidbey #389382)
        VerifyDivide<Exception>(792281625142643375935439503.4m, 0.1m, 7922816251426433759354395034m);
        VerifyDivide<Exception>(79228162514264337593543950.34m, 0.1m, 792281625142643375935439503.4m);
        VerifyDivide<Exception>(7922816251426433759354395.034m, 0.1m, 79228162514264337593543950.34m);
        VerifyDivide<Exception>(792281625142643375935439.5034m, 0.1m, 7922816251426433759354395.034m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 10m, 7922816251426433759354395033.5m);
        VerifyDivide<Exception>(79228162514264337567774146561m, 10m, 7922816251426433756777414656.1m);
        VerifyDivide<Exception>(79228162514264337567774146560m, 10m, 7922816251426433756777414656m);
        VerifyDivide<Exception>(79228162514264337567774146559m, 10m, 7922816251426433756777414655.9m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.1m, 72025602285694852357767227577m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.01m, 78443725261647859003508861718m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.001m, 79149013500763574019524425909.091m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.0001m, 79220240490215316061937756559.344m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.00001m, 79227370240561931974224208092.919m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.000001m, 79228083286181051412492537842.462m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.0000001m, 79228154591448878448656105469.389m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.00000001m, 79228161721982720373716746597.833m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.000000001m, 79228162435036175158507775176.492m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.0000000001m, 79228162506341521342909798200.709m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.00000000001m, 79228162513472055968409229775.316m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.000000000001m, 79228162514185109431029765225.569m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.0000000000001m, 79228162514256414777292524693.522m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.00000000000001m, 79228162514263545311918807699.547m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.000000000000001m, 79228162514264258365381436070.742m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.0000000000000001m, 79228162514264329670727698908.567m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.00000000000000001m, 79228162514264336801262325192.357m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.000000000000000001m, 79228162514264337514315787820.736m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.0000000000000000001m, 79228162514264337585621134083.574m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.00000000000000000001m, 79228162514264337592751668709.857m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.000000000000000000001m, 79228162514264337593464722172.486m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.0000000000000000000001m, 79228162514264337593536027518.749m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.00000000000000000000001m, 79228162514264337593543158053.375m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.000000000000000000000001m, 79228162514264337593543871106.837m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.0000000000000000000000001m, 79228162514264337593543942412.184m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.00000000000000000000000001m, 79228162514264337593543949542.718m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.000000000000000000000000001m, 79228162514264337593543950255.772m);
        VerifyDivide<Exception>(7922816251426433759354395033.5m, 0.9999999999999999999999999999m, 7922816251426433759354395034m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 10000000m, 7922816251426433759354.3950335m);
        VerifyDivide<Exception>(7922816251426433759354395033.5m, 1.000001m, 7922808328618105141249253784.2m);
        VerifyDivide<Exception>(7922816251426433759354395033.5m, 1.0000000000000000000000000001m, 7922816251426433759354395032.7m);
        VerifyDivide<Exception>(7922816251426433759354395033.5m, 1.0000000000000000000000000002m, 7922816251426433759354395031.9m);
        VerifyDivide<Exception>(7922816251426433759354395033.5m, 0.9999999999999999999999999999m, 7922816251426433759354395034m);
        VerifyDivide<Exception>(79228162514264337593543950335m, 1.0000000000000000000000000001m, 79228162514264337593543950327m);
        Decimal boundary7 = new Decimal((int)429u, (int)2133437386u, 0, false, 0);
        Decimal boundary71 = new Decimal((int)429u, (int)2133437387u, 0, false, 0);
        Decimal maxValueBy7 = Decimal.MaxValue * 0.0000001m;
        VerifyDivide<Exception>(maxValueBy7, 1m, maxValueBy7);
        VerifyDivide<Exception>(maxValueBy7, 1m, maxValueBy7);
        VerifyDivide<Exception>(maxValueBy7, 0.0000001m, Decimal.MaxValue);
        VerifyDivide<Exception>(boundary7, 1m, boundary7);
        VerifyDivide<Exception>(boundary7, 0.000000100000000000000000001m, 91630438009337286849083695.62m);
        VerifyDivide<Exception>(boundary71, 0.000000100000000000000000001m, 91630438052286959809083695.62m);
        VerifyDivide<Exception>(7922816251426433759354.3950335m, 1m, 7922816251426433759354.3950335m);
        VerifyDivide<Exception>(7922816251426433759354.3950335m, 0.0000001m, 79228162514264337593543950335m);

        //[] DivideByZero exceptions
        VerifyDivide<DivideByZeroException>(Decimal.One, Decimal.Zero);
        VerifyDivide<DivideByZeroException>(Decimal.Zero, Decimal.Zero);
        VerifyDivide<DivideByZeroException>(-5.00m, (-1m) * Decimal.Zero);
        VerifyDivide<DivideByZeroException>(0.0m, -0.00m);

        //[] Overflow exceptions
        VerifyDivide<OverflowException>(79228162514264337593543950335m, -0.9999999999999999999999999m);
        VerifyDivide<OverflowException>(792281625142643.37593543950335m, 0.0000000000000079228162514264337593543950335m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.1m);
        VerifyDivide<OverflowException>(7922816251426433759354395034m, 0.1m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.9m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.99m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.9999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.99999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.9999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.99999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.9999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.99999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.9999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.99999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.9999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.99999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.999999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.9999999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.99999999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.999999999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.9999999999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.99999999999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.999999999999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.9999999999999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.99999999999999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.999999999999999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, 0.9999999999999999999999999999m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, -0.1m);
        VerifyDivide<OverflowException>(79228162514264337593543950335m, -0.9999999999999999999999999m);
        VerifyDivide<OverflowException>(Decimal.MaxValue / 2, 0.5m);
    }

    [Fact]
    public static void TestFloor()
    {
        // Decimal Decimal.Floor(Decimal)
        Assert.Equal<Decimal>(123, Decimal.Floor((Decimal)123));
        Assert.Equal<Decimal>(123, Decimal.Floor((Decimal)123.123));
        Assert.Equal<Decimal>(-124, Decimal.Floor((Decimal)(-123.123)));
        Assert.Equal<Decimal>(123, Decimal.Floor((Decimal)123.567));
        Assert.Equal<Decimal>(-124, Decimal.Floor((Decimal)(-123.567)));
    }

    [Fact]
    public static void TestMaxValue()
    {
        // Decimal Decimal.MaxValue
        Assert.Equal(Decimal.MaxValue, 79228162514264337593543950335m);
    }

    [Fact]
    public static void TestMinusOne()
    {
        // Decimal Decimal.MinusOne
        Assert.Equal(Decimal.MinusOne, -1);
    }

    [Fact]
    public static void TestZero()
    {
        // Decimal Decimal.Zero
        Assert.Equal(Decimal.Zero, 0);
    }

    [Fact]
    public static void TestOne()
    {
        // Decimal Decimal.One
        Assert.Equal(Decimal.One, 1);
    }

    [Fact]
    public static void TestMinValue()
    {
        // Decimal Decimal.MinValue
        Assert.Equal(Decimal.MinValue, -79228162514264337593543950335m);
    }

    private static void VerifyMultiply<T>(Decimal d1, Decimal d2, Decimal expected = Decimal.Zero) where T : Exception
    {
        bool expectFailure = typeof(T) != typeof(Exception);

        try
        {
            Decimal result1 = Decimal.Multiply(d1, d2);
            Decimal result2 = d1 * d2;

            Assert.False(expectFailure, "Expected an exception to be thrown");
            Assert.Equal(result1, result2);
            Assert.Equal(expected, result1);
        }
        catch (T)
        {
            Assert.True(expectFailure, "Didn't expect an exception to be thrown");
        }
    }

    [Fact]
    public static void TestMultiply()
    {
        // Decimal Decimal.Multiply(Decimal, Decimal)
        // Decimal Decimal.op_Multiply(Decimal, Decimal)

        VerifyMultiply<Exception>(Decimal.One, Decimal.One, Decimal.One);
        VerifyMultiply<Exception>(7922816251426433759354395033.5m, new Decimal(10), Decimal.MaxValue);
        VerifyMultiply<Exception>(0.2352523523423422342354395033m, 56033525474612414574574757495m, 13182018677937129120135020796m);
        VerifyMultiply<Exception>(46161363632634613634.093453337m, 461613636.32634613634083453337m, 21308714924243214928823669051m);
        VerifyMultiply<Exception>(0.0000000000000345435353453563m, .0000000000000023525235234234m, 0.0000000000000000000000000001m);

        // Near MaxValue
        VerifyMultiply<Exception>(79228162514264337593543950335m, 0.9m, 71305346262837903834189555302m);
        VerifyMultiply<Exception>(79228162514264337593543950335m, 0.99m, 78435880889121694217608510832m);
        VerifyMultiply<Exception>(79228162514264337593543950335m, 0.9999999999999999999999999999m, 79228162514264337593543950327m);
        VerifyMultiply<Exception>(-79228162514264337593543950335m, 0.9m, -71305346262837903834189555302m);
        VerifyMultiply<Exception>(-79228162514264337593543950335m, 0.99m, -78435880889121694217608510832m);
        VerifyMultiply<Exception>(-79228162514264337593543950335m, 0.9999999999999999999999999999m, -79228162514264337593543950327m);

        // Exceptions
        VerifyMultiply<OverflowException>(Decimal.MaxValue, Decimal.MinValue);
        VerifyMultiply<OverflowException>(Decimal.MinValue, 1.1m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.1m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.01m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.0001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.00001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.0000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.00000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.0000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.00000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.0000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.00000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.0000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.00000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.000000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.0000000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.00000000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.000000000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.0000000000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.00000000000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.000000000000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.0000000000000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.00000000000000000000000001m);
        VerifyMultiply<OverflowException>(79228162514264337593543950335m, 1.000000000000000000000000001m);
        VerifyMultiply<OverflowException>(Decimal.MaxValue / 2, 2m);
    }

    [Fact]
    public static void TestNegate()
    {
        // Decimal Decimal.Negate(Decimal)
        Assert.Equal(0, Decimal.Negate(0));
        Assert.Equal(1, Decimal.Negate(-1));
        Assert.Equal(-1, Decimal.Negate(1));
    }

    [Fact]
    public static void Testop_Decrement()
    {
        // Decimal Decimal.op_Decrement(Decimal)
        Decimal d = 12345;
        Assert.Equal(12344, --d);
        d = 12345.678m;
        Assert.Equal(12344.678m, --d);
        d = -12345;
        Assert.Equal(-12346, --d);
        d = -12345.678m;
        Assert.Equal(-12346.678m, --d);
    }

    [Fact]
    public static void Testop_Increment()
    {
        // Decimal Decimal.op_Increment(Decimal)
        Decimal d = 12345;
        Assert.Equal(12346, ++d);
        d = 12345.678m;
        Assert.Equal(12346.678m, ++d);
        d = -12345;
        Assert.Equal(-12344m, ++d);
        d = -12345.678m;
        Assert.Equal(-12344.678m, ++d);
    }

    [Fact]
    public static void TestParse()
    {
        // Boolean Decimal.TryParse(String, NumberStyles, IFormatProvider, Decimal)
        Assert.Equal(123, Decimal.Parse("123"));
        Assert.Equal(-123, Decimal.Parse("-123"));
        Assert.Equal(123.123m, Decimal.Parse((123.123).ToString()));
        Assert.Equal(-123.123m, Decimal.Parse((-123.123).ToString()));

        Decimal d;
        Assert.True(Decimal.TryParse("79228162514264337593543950335", out d));
        Assert.Equal(Decimal.MaxValue, d);

        Assert.True(Decimal.TryParse("-79228162514264337593543950335", out d));
        Assert.Equal(Decimal.MinValue, d);

        var nfi = new NumberFormatInfo() { NumberGroupSeparator = "," };
        Assert.True(Decimal.TryParse("79,228,162,514,264,337,593,543,950,335", NumberStyles.AllowThousands, nfi, out d));
        Assert.Equal(Decimal.MaxValue, d);

        Assert.False(Decimal.TryParse("ysaidufljasdf", out d));
        Assert.False(Decimal.TryParse("79228162514264337593543950336", out d));
    }

    private static void VerifyRemainder(Decimal d1, Decimal d2, Decimal expectedResult)
    {
        Decimal result1 = Decimal.Remainder(d1, d2);
        Decimal result2 = d1 % d2;

        Assert.Equal(result1, result2);
        Assert.Equal(expectedResult, result1);
    }

    [Fact]
    public static void TestRemainder()
    {
        // Decimal Decimal.Remainder(Decimal, Decimal)
        // Decimal Decimal.op_Modulus(Decimal, Decimal)
        Decimal NegativeZero = new Decimal(0, 0, 0, true, 0);
        VerifyRemainder(5m, 3m, 2m);
        VerifyRemainder(5m, -3m, 2m);
        VerifyRemainder(-5m, 3m, -2m);
        VerifyRemainder(-5m, -3m, -2m);
        VerifyRemainder(3m, 5m, 3m);
        VerifyRemainder(3m, -5m, 3m);
        VerifyRemainder(-3m, 5m, -3m);
        VerifyRemainder(-3m, -5m, -3m);
        VerifyRemainder(10m, -3m, 1m);
        VerifyRemainder(-10m, 3m, -1m);
        VerifyRemainder(-2.0m, 0.5m, -0.0m);
        VerifyRemainder(2.3m, 0.531m, 0.176m);
        VerifyRemainder(0.00123m, 3242m, 0.00123m);
        VerifyRemainder(3242m, 0.00123m, 0.00044m);
        VerifyRemainder(17.3m, 3m, 2.3m);
        VerifyRemainder(8.55m, 2.25m, 1.80m);
        VerifyRemainder(0.00m, 3m, 0.00m);
        VerifyRemainder(NegativeZero, 2.2m, NegativeZero);

        // [] Max/Min
        VerifyRemainder(Decimal.MaxValue, Decimal.MaxValue, 0m);
        VerifyRemainder(Decimal.MaxValue, Decimal.MinValue, 0m);
        VerifyRemainder(Decimal.MaxValue, 1, 0m);
        VerifyRemainder(Decimal.MaxValue, 2394713m, 1494647m);
        VerifyRemainder(Decimal.MaxValue, -32768m, 32767m);
        VerifyRemainder(-0.00m, Decimal.MaxValue, -0.00m);
        VerifyRemainder(1.23984m, Decimal.MaxValue, 1.23984m);
        VerifyRemainder(2398412.12983m, Decimal.MaxValue, 2398412.12983m);
        VerifyRemainder(-0.12938m, Decimal.MaxValue, -0.12938m);

        VerifyRemainder(Decimal.MinValue, Decimal.MinValue, NegativeZero);
        VerifyRemainder(Decimal.MinValue, Decimal.MaxValue, NegativeZero);
        VerifyRemainder(Decimal.MinValue, 1, NegativeZero);
        VerifyRemainder(Decimal.MinValue, 2394713m, -1494647m);
        VerifyRemainder(Decimal.MinValue, -32768m, -32767m); // ASURT #90921
        VerifyRemainder(0.0m, Decimal.MinValue, 0.0m);
        VerifyRemainder(1.23984m, Decimal.MinValue, 1.23984m);
        VerifyRemainder(2398412.12983m, Decimal.MinValue, 2398412.12983m);
        VerifyRemainder(-0.12938m, Decimal.MinValue, -0.12938m);

        VerifyRemainder(57675350989891243676868034225m, 7m, 5m); // VSWhidbey #325142
        VerifyRemainder(-57675350989891243676868034225m, 7m, -5m);
        VerifyRemainder(57675350989891243676868034225m, -7m, 5m);
        VerifyRemainder(-57675350989891243676868034225m, -7m, -5m);

        // VSWhidbey #389382
        VerifyRemainder(792281625142643375935439503.4m, 0.1m, 0.0m);
        VerifyRemainder(79228162514264337593543950.34m, 0.1m, 0.04m);
        VerifyRemainder(7922816251426433759354395.034m, 0.1m, 0.034m);
        VerifyRemainder(792281625142643375935439.5034m, 0.1m, 0.0034m);
        VerifyRemainder(79228162514264337593543950335m, 10m, 5m);
        VerifyRemainder(79228162514264337567774146561m, 10m, 1m);
        VerifyRemainder(79228162514264337567774146560m, 10m, 0m);
        VerifyRemainder(79228162514264337567774146559m, 10m, 9m);
    }

    private static void VerifySubtract<T>(Decimal d1, Decimal d2, Decimal expected = Decimal.Zero) where T : Exception
    {
        bool expectFailure = typeof(T) != typeof(Exception);

        try
        {
            Decimal result1 = Decimal.Subtract(d1, d2);
            Decimal result2 = d1 - d2;

            Assert.False(expectFailure, "Expected an exception to be thrown");
            Assert.Equal(result1, result2);
            Assert.Equal(expected, result1);
        }
        catch (T)
        {
            Assert.True(expectFailure, "Didn't expect an exception to be thrown");
        }
    }

    [Fact]
    public static void TestSubtract()
    {
        // Decimal Decimal.Subtract(Decimal, Decimal)
        // Decimal Decimal.op_Subtraction(Decimal, Decimal)

        VerifySubtract<Exception>(1, 1, 0);
        VerifySubtract<Exception>(-1, 1, -2);
        VerifySubtract<Exception>(1, -1, 2);
        VerifySubtract<Exception>(Decimal.MaxValue, Decimal.Zero, Decimal.MaxValue);
        VerifySubtract<Exception>(Decimal.MinValue, Decimal.Zero, Decimal.MinValue);
        VerifySubtract<Exception>(79228162514264337593543950330m, -5, Decimal.MaxValue);
        VerifySubtract<Exception>(79228162514264337593543950330m, 5, 79228162514264337593543950325m);
        VerifySubtract<Exception>(-79228162514264337593543950330m, 5, Decimal.MinValue);
        VerifySubtract<Exception>(-79228162514264337593543950330m, -5, -79228162514264337593543950325m);
        VerifySubtract<Exception>(1234.5678m, 0.00009m, 1234.56771m);
        VerifySubtract<Exception>(-1234.5678m, 0.00009m, -1234.56789m);
        VerifySubtract<Exception>(0.1111111111111111111111111111m, 0.1111111111111111111111111111m, 0);
        VerifySubtract<Exception>(0.2222222222222222222222222222m,
                             0.1111111111111111111111111111m,
                             0.1111111111111111111111111111m);
        VerifySubtract<Exception>(1.1111111111111111111111111110m,
                             0.5555555555555555555555555555m,
                             0.5555555555555555555555555555m);
    }

    [Fact]
    public static void TestTruncate()
    {
        // Decimal Decimal.Truncate(Decimal)
        Assert.Equal<Decimal>(123, Decimal.Truncate((Decimal)123));
        Assert.Equal<Decimal>(123, Decimal.Truncate((Decimal)123.123));
        Assert.Equal<Decimal>(-123, Decimal.Truncate((Decimal)(-123.123)));
        Assert.Equal<Decimal>(123, Decimal.Truncate((Decimal)123.567));
        Assert.Equal<Decimal>(-123, Decimal.Truncate((Decimal)(-123.567)));
    }

    [Fact]
    public static void TestRound()
    {
        // Decimal Decimal.Truncate(Decimal)
        // Assert.AreEqual<Decimal>(123, Decimal.Round((Decimal)123, 2));
        // Assert.AreEqual<Decimal>((Decimal)123.123, Decimal.Round((Decimal)123.123, 3));
        // Assert.AreEqual<Decimal>((Decimal)(-123.1), Decimal.Round((Decimal)(-123.123), 1));
        // Assert.AreEqual<Decimal>(124, Decimal.Round((Decimal)123.567, 0));
        // Assert.AreEqual<Decimal>((Decimal)(-123.567), Decimal.Round((Decimal)(-123.567), 4));
    }

    [Fact]
    public static void TestCompare()
    {
        // Int32 Decimal.Compare(Decimal, Decimal)
        Assert.True(Decimal.Compare(Decimal.Zero, Decimal.Zero) == 0);
        Assert.True(Decimal.Compare(Decimal.Zero, Decimal.One) < 0);
        Assert.True(Decimal.Compare(Decimal.One, Decimal.Zero) > 0);
        Assert.True(Decimal.Compare(Decimal.MinusOne, Decimal.Zero) < 0);
        Assert.True(Decimal.Compare(Decimal.Zero, Decimal.MinusOne) > 0);
        Assert.True(Decimal.Compare(5, 3) > 0);
        Assert.True(Decimal.Compare(5, 5) == 0);
        Assert.True(Decimal.Compare(5, 9) < 0);
        Assert.True(Decimal.Compare(-123.123m, 123.123m) < 0);
        Assert.True(Decimal.Compare(Decimal.MaxValue, Decimal.MaxValue) == 0);
        Assert.True(Decimal.Compare(Decimal.MinValue, Decimal.MinValue) == 0);
        Assert.True(Decimal.Compare(Decimal.MinValue, Decimal.MaxValue) < 0);
        Assert.True(Decimal.Compare(Decimal.MaxValue, Decimal.MinValue) > 0);
    }

    [Fact]
    public static void TestCompareTo()
    {
        // Int32 Decimal.CompareTo(Decimal)
        Decimal d = 456;
        Assert.True(d.CompareTo(456m) == 0);
        Assert.True(d.CompareTo(457m) < 0);
        Assert.True(d.CompareTo(455m) > 0);
    }

    [Fact]
    public static void TestSystemIComparableCompareTo()
    {
        // Int32 Decimal.System.IComparable.CompareTo(Object)
        IComparable d = (Decimal)248;
        Assert.True(d.CompareTo(248m) == 0);
        Assert.True(d.CompareTo(249m) < 0);
        Assert.True(d.CompareTo(247m) > 0);
        Assert.True(d.CompareTo(null) > 0);

        Assert.Throws<ArgumentException>(() => d.CompareTo("248"));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        // Int32 Decimal.GetHashCode()
        Assert.NotEqual(Decimal.MinusOne.GetHashCode(), Decimal.One.GetHashCode());
    }

    [Fact]
    public static void TestToSingle()
    {
        // Single Decimal.ToSingle(Decimal)
        Single s = 12345.12f;
        Assert.Equal(s, Decimal.ToSingle((Decimal)s));
        Assert.Equal(-s, Decimal.ToSingle((Decimal)(-s)));

        s = 1e20f;
        Assert.Equal(s, Decimal.ToSingle((Decimal)s));
        Assert.Equal(-s, Decimal.ToSingle((Decimal)(-s)));

        s = 1e27f;
        Assert.Equal(s, Decimal.ToSingle((Decimal)s));
        Assert.Equal(-s, Decimal.ToSingle((Decimal)(-s)));
    }

    [Fact]
    public static void TestToDouble()
    {
        Double d = Decimal.ToDouble(new Decimal(0, 0, 1, false, 0));

        // Double Decimal.ToDouble(Decimal)
        Double dbl = 123456789.123456;
        Assert.Equal(dbl, Decimal.ToDouble((Decimal)dbl));
        Assert.Equal(-dbl, Decimal.ToDouble((Decimal)(-dbl)));

        dbl = 1e20;
        Assert.Equal(dbl, Decimal.ToDouble((Decimal)dbl));
        Assert.Equal(-dbl, Decimal.ToDouble((Decimal)(-dbl)));

        dbl = 1e27;
        Assert.Equal(dbl, Decimal.ToDouble((Decimal)dbl));
        Assert.Equal(-dbl, Decimal.ToDouble((Decimal)(-dbl)));

        dbl = Int64.MaxValue;
        // Need to pass in the Int64.MaxValue to ToDouble and not dbl because the conversion to double is a little lossy and we want precision
        Assert.Equal(dbl, Decimal.ToDouble((Decimal)Int64.MaxValue));
        Assert.Equal(-dbl, Decimal.ToDouble((Decimal)(-Int64.MaxValue)));
    }

    [Fact]
    public static void TestToInt16()
    {
        // Int16 Decimal.ToInt16(Decimal)
        Assert.Equal(Int16.MaxValue, Decimal.ToInt16((Decimal)Int16.MaxValue));
        Assert.Equal(Int16.MinValue, Decimal.ToInt16((Decimal)Int16.MinValue));
    }

    [Fact]
    public static void TestToInt32()
    {
        // Int32 Decimal.ToInt32(Decimal)
        Assert.Equal(Int32.MaxValue, Decimal.ToInt32((Decimal)Int32.MaxValue));
        Assert.Equal(Int32.MinValue, Decimal.ToInt32((Decimal)Int32.MinValue));
    }

    public static IEnumerable<object[]> DecimalTestData
    {
        get
        {
            return new[]
            {
                new object[] {1M, new int[] { 0x00000001, 0x00000000, 0x00000000, 0x00000000 } },
                new object[] {100000000000000M, new int[] { 0x107A4000, 0x00005AF3, 0x00000000, 0x00000000 } },
                new object[] {100000000000000.00000000000000M, new int[] { 0x10000000, 0x3E250261, 0x204FCE5E, 0x000E0000 } },
                new object[] {1.0000000000000000000000000000M, new int[] { 0x10000000, 0x3E250261, 0x204FCE5E, 0x001C0000 } },
                new object[] {123456789M, new int[] { 0x075BCD15, 0x00000000, 0x00000000, 0x00000000 } },
                new object[] {0.123456789M, new int[] { 0x075BCD15, 0x00000000, 0x00000000, 0x00090000 } },
                new object[] {0.000000000123456789M, new int[] { 0x075BCD15, 0x00000000, 0x00000000, 0x00120000 } },
                new object[] {0.000000000000000000123456789M, new int[] { 0x075BCD15, 0x00000000, 0x00000000, 0x001B0000 } },
                new object[] {4294967295M, new int[] { unchecked((int)0xFFFFFFFF), 0x00000000, 0x00000000, 0x00000000 } },
                new object[] {18446744073709551615M, new int[] { unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), 0x00000000, 0x00000000 } },
                new object[] {Decimal.MaxValue, new int[] { unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), 0x00000000 } },
                new object[] {Decimal.MinValue, new int[] { unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0x80000000) } },
                new object[] {-7.9228162514264337593543950335M, new int[] { unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0x801C0000) } }
            };
        }
    }

    [Theory, MemberData("DecimalTestData")]
    public static void TestGetBits(Decimal input, int[] expectedBits)
    {
        int[] actualsBits = Decimal.GetBits(input);

        Assert.Equal(expectedBits, actualsBits);

        bool sign = (actualsBits[3] & 0x80000000) != 0;
        byte scale = (byte)((actualsBits[3] >> 16) & 0x7F);
        Decimal newValue = new Decimal(actualsBits[0], actualsBits[1], actualsBits[2], sign, scale);

        Assert.Equal(input, newValue);
    }

    [Fact]
    public static void TestToInt64()
    {
        // Int64 Decimal.ToInt64(Decimal)
        Assert.Equal(Int64.MaxValue, Decimal.ToInt64((Decimal)Int64.MaxValue));
        Assert.Equal(Int64.MinValue, Decimal.ToInt64((Decimal)Int64.MinValue));
    }

    [Fact]
    public static void TestToSByte()
    {
        // SByte Decimal.ToSByte(Decimal)
        Assert.Equal(SByte.MaxValue, Decimal.ToSByte((Decimal)SByte.MaxValue));
        Assert.Equal(SByte.MinValue, Decimal.ToSByte((Decimal)SByte.MinValue));
    }

    [Fact]
    public static void TestToUInt16()
    {
        // UInt16 Decimal.ToUInt16(Decimal)
        Assert.Equal(UInt16.MaxValue, Decimal.ToUInt16((Decimal)UInt16.MaxValue));
        Assert.Equal(UInt16.MinValue, Decimal.ToUInt16((Decimal)UInt16.MinValue));
    }

    [Fact]
    public static void TestToUInt32()
    {
        // UInt32 Decimal.ToUInt32(Decimal)
        Assert.Equal(UInt32.MaxValue, Decimal.ToUInt32((Decimal)UInt32.MaxValue));
        Assert.Equal(UInt32.MinValue, Decimal.ToUInt32((Decimal)UInt32.MinValue));
    }

    [Fact]
    public static void TestToUInt64()
    {
        // UInt64 Decimal.ToUInt64(Decimal)
        Assert.Equal(UInt64.MaxValue, Decimal.ToUInt64((Decimal)UInt64.MaxValue));
        Assert.Equal(UInt64.MinValue, Decimal.ToUInt64((Decimal)UInt64.MinValue));
    }

    [Fact]
    public static void TestToString()
    {
        // String Decimal.ToString()
        Decimal d1 = 6310.23m;
        Assert.Equal(string.Format("{0}", 6310.23), d1.ToString());

        Decimal d2 = -8249.000003m;
        Assert.Equal(string.Format("{0}", -8249.000003), d2.ToString());

        Assert.Equal("79228162514264337593543950335", Decimal.MaxValue.ToString());
        Assert.Equal("-79228162514264337593543950335", Decimal.MinValue.ToString());
    }

    [Fact]
    public static void Testctor()
    {
        Decimal d;
        // Void Decimal..ctor(Double)
        d = new Decimal((Double)123456789.123456);
        Assert.Equal<Decimal>(d, (Decimal)123456789.123456);

        // Void Decimal..ctor(Int32)
        d = new Decimal((Int32)Int32.MaxValue);
        Assert.Equal<Decimal>(d, Int32.MaxValue);

        // Void Decimal..ctor(Int64)
        d = new Decimal((Int64)Int64.MaxValue);
        Assert.Equal<Decimal>(d, Int64.MaxValue);

        // Void Decimal..ctor(Single)
        d = new Decimal((Single)123.123);
        Assert.Equal<Decimal>(d, (Decimal)123.123);

        // Void Decimal..ctor(UInt32)
        d = new Decimal((UInt32)UInt32.MaxValue);
        Assert.Equal<Decimal>(d, UInt32.MaxValue);

        // Void Decimal..ctor(UInt64)
        d = new Decimal((UInt64)UInt64.MaxValue);
        Assert.Equal<Decimal>(d, UInt64.MaxValue);

        // Void Decimal..ctor(Int32, Int32, Int32, Boolean, Byte)
        d = new Decimal(1, 1, 1, false, 0);
        Decimal d2 = 3;
        d2 += UInt32.MaxValue;
        d2 += UInt64.MaxValue;
        Assert.Equal(d, d2);

        // Void Decimal..ctor(Int32[])
        d = new Decimal(new Int32[] { 1, 1, 1, 0 });
        Assert.Equal(d, d2);
    }

    [Fact]
    public static void TestNumberBufferLimit()
    {
        Decimal dE = 1234567890123456789012345.6785m;
        string s1 = "1234567890123456789012345.678456";
        var nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        Decimal d1 = Decimal.Parse(s1, nfi);
        Assert.Equal(d1, dE);
        return;
    }
}

