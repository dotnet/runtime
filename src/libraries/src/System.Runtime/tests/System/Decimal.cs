// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        Assert.True(decimal.Zero.Equals(decimal.Zero));
        Assert.False(decimal.Zero.Equals(decimal.One));
        Assert.True(decimal.MaxValue.Equals(decimal.MaxValue));
        Assert.True(decimal.MinValue.Equals(decimal.MinValue));
        Assert.False(decimal.MaxValue.Equals(decimal.MinValue));
        Assert.False(decimal.MinValue.Equals(decimal.MaxValue));
    }

    [Fact]
    public static void TestEqualsDecDec()
    {
        // Boolean Decimal.Equals(Decimal, Decimal)
        Assert.True(decimal.Equals(decimal.Zero, decimal.Zero));
        Assert.False(decimal.Equals(decimal.Zero, decimal.One));
        Assert.True(decimal.Equals(decimal.MaxValue, decimal.MaxValue));
        Assert.True(decimal.Equals(decimal.MinValue, decimal.MinValue));
        Assert.False(decimal.Equals(decimal.MinValue, decimal.MaxValue));
        Assert.False(decimal.Equals(decimal.MaxValue, decimal.MinValue));
    }

    [Fact]
    public static void TestEqualsObj()
    {
        // Boolean Decimal.Equals(Object)
        Assert.True(decimal.Zero.Equals((object)decimal.Zero));
        Assert.False(decimal.Zero.Equals((object)decimal.One));
        Assert.True(decimal.MaxValue.Equals((object)decimal.MaxValue));
        Assert.True(decimal.MinValue.Equals((object)decimal.MinValue));
        Assert.False(decimal.MinValue.Equals((object)decimal.MaxValue));
        Assert.False(decimal.MaxValue.Equals((object)decimal.MinValue));
        Assert.False(decimal.One.Equals(null));
        Assert.False(decimal.One.Equals("one"));
        Assert.False(decimal.One.Equals((object)1));
    }

    [Fact]
    public static void Testop_Equality()
    {
        // Boolean Decimal.op_Equality(Decimal, Decimal)
        Assert.True(decimal.Zero == decimal.Zero);
        Assert.False(decimal.Zero == decimal.One);
        Assert.True(decimal.MaxValue == decimal.MaxValue);
        Assert.True(decimal.MinValue == decimal.MinValue);
        Assert.False(decimal.MinValue == decimal.MaxValue);
        Assert.False(decimal.MaxValue == decimal.MinValue);
    }

    [Fact]
    public static void Testop_GreaterThan()
    {
        // Boolean Decimal.op_GreaterThan(Decimal, Decimal)
        Assert.False(decimal.Zero > decimal.Zero);
        Assert.False(decimal.Zero > decimal.One);
        Assert.True(decimal.One > decimal.Zero);
        Assert.False(decimal.MaxValue > decimal.MaxValue);
        Assert.False(decimal.MinValue > decimal.MinValue);
        Assert.False(decimal.MinValue > decimal.MaxValue);
        Assert.True(decimal.MaxValue > decimal.MinValue);
    }

    [Fact]
    public static void Testop_GreaterThanOrEqual()
    {
        // Boolean Decimal.op_GreaterThanOrEqual(Decimal, Decimal)
        Assert.True(decimal.Zero >= decimal.Zero);
        Assert.False(decimal.Zero >= decimal.One);
        Assert.True(decimal.One >= decimal.Zero);
        Assert.True(decimal.MaxValue >= decimal.MaxValue);
        Assert.True(decimal.MinValue >= decimal.MinValue);
        Assert.False(decimal.MinValue >= decimal.MaxValue);
        Assert.True(decimal.MaxValue >= decimal.MinValue);
    }

    [Fact]
    public static void Testop_Inequality()
    {
        // Boolean Decimal.op_Inequality(Decimal, Decimal)
        Assert.False(decimal.Zero != decimal.Zero);
        Assert.True(decimal.Zero != decimal.One);
        Assert.True(decimal.One != decimal.Zero);
        Assert.False(decimal.MaxValue != decimal.MaxValue);
        Assert.False(decimal.MinValue != decimal.MinValue);
        Assert.True(decimal.MinValue != decimal.MaxValue);
        Assert.True(decimal.MaxValue != decimal.MinValue);
    }

    [Fact]
    public static void Testop_LessThan()
    {
        // Boolean Decimal.op_LessThan(Decimal, Decimal)
        Assert.False(decimal.Zero < decimal.Zero);
        Assert.True(decimal.Zero < decimal.One);
        Assert.False(decimal.One < decimal.Zero);
        Assert.True(5m < 15m);
        decimal d5 = 5;
        decimal d3 = 3;
        Assert.False(d5 < d3);
        Assert.False(decimal.MaxValue < decimal.MaxValue);
        Assert.False(decimal.MinValue < decimal.MinValue);
        Assert.True(decimal.MinValue < decimal.MaxValue);
        Assert.False(decimal.MaxValue < decimal.MinValue);
    }

    [Fact]
    public static void Testop_LessThanOrEqual()
    {
        // Boolean Decimal.op_LessThanOrEqual(Decimal, Decimal)
        Assert.True(decimal.Zero <= decimal.Zero);
        Assert.True(decimal.Zero <= decimal.One);
        Assert.False(decimal.One <= decimal.Zero);
        Assert.True(decimal.MaxValue <= decimal.MaxValue);
        Assert.True(decimal.MinValue <= decimal.MinValue);
        Assert.True(decimal.MinValue <= decimal.MaxValue);
        Assert.False(decimal.MaxValue <= decimal.MinValue);
    }

    [Fact]
    public static void TestToByte()
    {
        // Byte Decimal.ToByte(Decimal)
        Assert.Equal(0, decimal.ToByte(0));
        Assert.Equal(1, decimal.ToByte(1));
        Assert.Equal(255, decimal.ToByte(255));

        Assert.Throws<OverflowException>(() => decimal.ToByte(256));
    }

    private static void VerifyAdd<T>(decimal d1, decimal d2, decimal expected = decimal.Zero) where T : Exception
    {
        bool expectFailure = typeof(T) != typeof(Exception);

        try
        {
            decimal result1 = decimal.Add(d1, d2);
            decimal result2 = d1 + d2;

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
        VerifyAdd<Exception>(decimal.MaxValue, decimal.Zero, decimal.MaxValue);
        VerifyAdd<Exception>(decimal.MinValue, decimal.Zero, decimal.MinValue);
        VerifyAdd<Exception>(79228162514264337593543950330m, 5, decimal.MaxValue);
        VerifyAdd<Exception>(79228162514264337593543950330m, -5, 79228162514264337593543950325m);
        VerifyAdd<Exception>(-79228162514264337593543950330m, -5, decimal.MinValue);
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
        VerifyAdd<OverflowException>(decimal.MaxValue, decimal.MaxValue);
        VerifyAdd<OverflowException>(79228162514264337593543950330m, 6);
        VerifyAdd<OverflowException>(-79228162514264337593543950330m, -6, decimal.MinValue);
    }

    [Fact]
    public static void TestCeiling()
    {
        // Decimal Decimal.Ceiling(Decimal)
        Assert.Equal<Decimal>(123, decimal.Ceiling((decimal)123));
        Assert.Equal<Decimal>(124, decimal.Ceiling((decimal)123.123));
        Assert.Equal<Decimal>(-123, decimal.Ceiling((decimal)(-123.123)));
        Assert.Equal<Decimal>(124, decimal.Ceiling((decimal)123.567));
        Assert.Equal<Decimal>(-123, decimal.Ceiling((decimal)(-123.567)));
    }

    private static void VerifyDivide<T>(decimal d1, decimal d2, decimal expected = decimal.Zero) where T : Exception
    {
        bool expectFailure = typeof(T) != typeof(Exception);

        try
        {
            decimal result1 = decimal.Divide(d1, d2);
            decimal result2 = d1 / d2;

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
        VerifyDivide<Exception>(decimal.One, decimal.One, decimal.One);
        VerifyDivide<Exception>(decimal.MaxValue, decimal.MinValue, decimal.MinusOne);
        VerifyDivide<Exception>(0.9214206543486529434634231456m, decimal.MaxValue, decimal.Zero);
        VerifyDivide<Exception>(38214206543486529434634231456m, 0.49214206543486529434634231456m, 77648730371625094566866001277m);
        VerifyDivide<Exception>(-78228162514264337593543950335m, decimal.MaxValue, -0.987378225516463811113412343m);
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
        decimal boundary7 = new decimal((int)429u, (int)2133437386u, 0, false, 0);
        decimal boundary71 = new decimal((int)429u, (int)2133437387u, 0, false, 0);
        decimal maxValueBy7 = decimal.MaxValue * 0.0000001m;
        VerifyDivide<Exception>(maxValueBy7, 1m, maxValueBy7);
        VerifyDivide<Exception>(maxValueBy7, 1m, maxValueBy7);
        VerifyDivide<Exception>(maxValueBy7, 0.0000001m, decimal.MaxValue);
        VerifyDivide<Exception>(boundary7, 1m, boundary7);
        VerifyDivide<Exception>(boundary7, 0.000000100000000000000000001m, 91630438009337286849083695.62m);
        VerifyDivide<Exception>(boundary71, 0.000000100000000000000000001m, 91630438052286959809083695.62m);
        VerifyDivide<Exception>(7922816251426433759354.3950335m, 1m, 7922816251426433759354.3950335m);
        VerifyDivide<Exception>(7922816251426433759354.3950335m, 0.0000001m, 79228162514264337593543950335m);

        //[] DivideByZero exceptions
        VerifyDivide<DivideByZeroException>(decimal.One, decimal.Zero);
        VerifyDivide<DivideByZeroException>(decimal.Zero, decimal.Zero);
        VerifyDivide<DivideByZeroException>(-5.00m, (-1m) * decimal.Zero);
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
        VerifyDivide<OverflowException>(decimal.MaxValue / 2, 0.5m);
    }

    [Fact]
    public static void TestFloor()
    {
        // Decimal Decimal.Floor(Decimal)
        Assert.Equal<Decimal>(123, decimal.Floor((decimal)123));
        Assert.Equal<Decimal>(123, decimal.Floor((decimal)123.123));
        Assert.Equal<Decimal>(-124, decimal.Floor((decimal)(-123.123)));
        Assert.Equal<Decimal>(123, decimal.Floor((decimal)123.567));
        Assert.Equal<Decimal>(-124, decimal.Floor((decimal)(-123.567)));
    }

    [Fact]
    public static void TestMaxValue()
    {
        // Decimal Decimal.MaxValue
        Assert.Equal(decimal.MaxValue, 79228162514264337593543950335m);
    }

    [Fact]
    public static void TestMinusOne()
    {
        // Decimal Decimal.MinusOne
        Assert.Equal(decimal.MinusOne, -1);
    }

    [Fact]
    public static void TestZero()
    {
        // Decimal Decimal.Zero
        Assert.Equal(decimal.Zero, 0);
    }

    [Fact]
    public static void TestOne()
    {
        // Decimal Decimal.One
        Assert.Equal(decimal.One, 1);
    }

    [Fact]
    public static void TestMinValue()
    {
        // Decimal Decimal.MinValue
        Assert.Equal(decimal.MinValue, -79228162514264337593543950335m);
    }

    private static void VerifyMultiply<T>(decimal d1, decimal d2, decimal expected = decimal.Zero) where T : Exception
    {
        bool expectFailure = typeof(T) != typeof(Exception);

        try
        {
            decimal result1 = decimal.Multiply(d1, d2);
            decimal result2 = d1 * d2;

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

        VerifyMultiply<Exception>(decimal.One, decimal.One, decimal.One);
        VerifyMultiply<Exception>(7922816251426433759354395033.5m, new decimal(10), decimal.MaxValue);
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
        VerifyMultiply<OverflowException>(decimal.MaxValue, decimal.MinValue);
        VerifyMultiply<OverflowException>(decimal.MinValue, 1.1m);
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
        VerifyMultiply<OverflowException>(decimal.MaxValue / 2, 2m);
    }

    [Fact]
    public static void TestNegate()
    {
        // Decimal Decimal.Negate(Decimal)
        Assert.Equal(0, decimal.Negate(0));
        Assert.Equal(1, decimal.Negate(-1));
        Assert.Equal(-1, decimal.Negate(1));
    }

    [Fact]
    public static void Testop_Decrement()
    {
        // Decimal Decimal.op_Decrement(Decimal)
        decimal d = 12345;
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
        decimal d = 12345;
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
        Assert.Equal(123, decimal.Parse("123"));
        Assert.Equal(-123, decimal.Parse("-123"));
        Assert.Equal(123.123m, decimal.Parse((123.123).ToString()));
        Assert.Equal(-123.123m, decimal.Parse((-123.123).ToString()));

        decimal d;
        Assert.True(decimal.TryParse("79228162514264337593543950335", out d));
        Assert.Equal(decimal.MaxValue, d);

        Assert.True(decimal.TryParse("-79228162514264337593543950335", out d));
        Assert.Equal(decimal.MinValue, d);

        var nfi = new NumberFormatInfo() { NumberGroupSeparator = "," };
        Assert.True(decimal.TryParse("79,228,162,514,264,337,593,543,950,335", NumberStyles.AllowThousands, nfi, out d));
        Assert.Equal(decimal.MaxValue, d);

        Assert.False(decimal.TryParse("ysaidufljasdf", out d));
        Assert.False(decimal.TryParse("79228162514264337593543950336", out d));
    }

    private static void VerifyRemainder(decimal d1, decimal d2, decimal expectedResult)
    {
        decimal result1 = decimal.Remainder(d1, d2);
        decimal result2 = d1 % d2;

        Assert.Equal(result1, result2);
        Assert.Equal(expectedResult, result1);
    }

    [Fact]
    public static void TestRemainder()
    {
        // Decimal Decimal.Remainder(Decimal, Decimal)
        // Decimal Decimal.op_Modulus(Decimal, Decimal)
        decimal NegativeZero = new decimal(0, 0, 0, true, 0);
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
        VerifyRemainder(decimal.MaxValue, decimal.MaxValue, 0m);
        VerifyRemainder(decimal.MaxValue, decimal.MinValue, 0m);
        VerifyRemainder(decimal.MaxValue, 1, 0m);
        VerifyRemainder(decimal.MaxValue, 2394713m, 1494647m);
        VerifyRemainder(decimal.MaxValue, -32768m, 32767m);
        VerifyRemainder(-0.00m, decimal.MaxValue, -0.00m);
        VerifyRemainder(1.23984m, decimal.MaxValue, 1.23984m);
        VerifyRemainder(2398412.12983m, decimal.MaxValue, 2398412.12983m);
        VerifyRemainder(-0.12938m, decimal.MaxValue, -0.12938m);

        VerifyRemainder(decimal.MinValue, decimal.MinValue, NegativeZero);
        VerifyRemainder(decimal.MinValue, decimal.MaxValue, NegativeZero);
        VerifyRemainder(decimal.MinValue, 1, NegativeZero);
        VerifyRemainder(decimal.MinValue, 2394713m, -1494647m);
        VerifyRemainder(decimal.MinValue, -32768m, -32767m); // ASURT #90921
        VerifyRemainder(0.0m, decimal.MinValue, 0.0m);
        VerifyRemainder(1.23984m, decimal.MinValue, 1.23984m);
        VerifyRemainder(2398412.12983m, decimal.MinValue, 2398412.12983m);
        VerifyRemainder(-0.12938m, decimal.MinValue, -0.12938m);

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

    private static void VerifySubtract<T>(decimal d1, decimal d2, decimal expected = decimal.Zero) where T : Exception
    {
        bool expectFailure = typeof(T) != typeof(Exception);

        try
        {
            decimal result1 = decimal.Subtract(d1, d2);
            decimal result2 = d1 - d2;

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
        VerifySubtract<Exception>(decimal.MaxValue, decimal.Zero, decimal.MaxValue);
        VerifySubtract<Exception>(decimal.MinValue, decimal.Zero, decimal.MinValue);
        VerifySubtract<Exception>(79228162514264337593543950330m, -5, decimal.MaxValue);
        VerifySubtract<Exception>(79228162514264337593543950330m, 5, 79228162514264337593543950325m);
        VerifySubtract<Exception>(-79228162514264337593543950330m, 5, decimal.MinValue);
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
        Assert.Equal<Decimal>(123, decimal.Truncate((decimal)123));
        Assert.Equal<Decimal>(123, decimal.Truncate((decimal)123.123));
        Assert.Equal<Decimal>(-123, decimal.Truncate((decimal)(-123.123)));
        Assert.Equal<Decimal>(123, decimal.Truncate((decimal)123.567));
        Assert.Equal<Decimal>(-123, decimal.Truncate((decimal)(-123.567)));
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
        Assert.True(decimal.Compare(decimal.Zero, decimal.Zero) == 0);
        Assert.True(decimal.Compare(decimal.Zero, decimal.One) < 0);
        Assert.True(decimal.Compare(decimal.One, decimal.Zero) > 0);
        Assert.True(decimal.Compare(decimal.MinusOne, decimal.Zero) < 0);
        Assert.True(decimal.Compare(decimal.Zero, decimal.MinusOne) > 0);
        Assert.True(decimal.Compare(5, 3) > 0);
        Assert.True(decimal.Compare(5, 5) == 0);
        Assert.True(decimal.Compare(5, 9) < 0);
        Assert.True(decimal.Compare(-123.123m, 123.123m) < 0);
        Assert.True(decimal.Compare(decimal.MaxValue, decimal.MaxValue) == 0);
        Assert.True(decimal.Compare(decimal.MinValue, decimal.MinValue) == 0);
        Assert.True(decimal.Compare(decimal.MinValue, decimal.MaxValue) < 0);
        Assert.True(decimal.Compare(decimal.MaxValue, decimal.MinValue) > 0);
    }

    [Fact]
    public static void TestCompareTo()
    {
        // Int32 Decimal.CompareTo(Decimal)
        decimal d = 456;
        Assert.True(d.CompareTo(456m) == 0);
        Assert.True(d.CompareTo(457m) < 0);
        Assert.True(d.CompareTo(455m) > 0);
    }

    [Fact]
    public static void TestSystemIComparableCompareTo()
    {
        // Int32 Decimal.System.IComparable.CompareTo(Object)
        IComparable d = (decimal)248;
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
        Assert.NotEqual(decimal.MinusOne.GetHashCode(), decimal.One.GetHashCode());
    }

    [Fact]
    public static void TestToSingle()
    {
        // Single Decimal.ToSingle(Decimal)
        Single s = 12345.12f;
        Assert.Equal(s, decimal.ToSingle((decimal)s));
        Assert.Equal(-s, decimal.ToSingle((decimal)(-s)));

        s = 1e20f;
        Assert.Equal(s, decimal.ToSingle((decimal)s));
        Assert.Equal(-s, decimal.ToSingle((decimal)(-s)));

        s = 1e27f;
        Assert.Equal(s, decimal.ToSingle((decimal)s));
        Assert.Equal(-s, decimal.ToSingle((decimal)(-s)));
    }

    [Fact]
    public static void TestToDouble()
    {
        Double d = decimal.ToDouble(new decimal(0, 0, 1, false, 0));

        // Double Decimal.ToDouble(Decimal)
        Double dbl = 123456789.123456;
        Assert.Equal(dbl, decimal.ToDouble((decimal)dbl));
        Assert.Equal(-dbl, decimal.ToDouble((decimal)(-dbl)));

        dbl = 1e20;
        Assert.Equal(dbl, decimal.ToDouble((decimal)dbl));
        Assert.Equal(-dbl, decimal.ToDouble((decimal)(-dbl)));

        dbl = 1e27;
        Assert.Equal(dbl, decimal.ToDouble((decimal)dbl));
        Assert.Equal(-dbl, decimal.ToDouble((decimal)(-dbl)));

        dbl = long.MaxValue;
        // Need to pass in the Int64.MaxValue to ToDouble and not dbl because the conversion to double is a little lossy and we want precision
        Assert.Equal(dbl, decimal.ToDouble((decimal)long.MaxValue));
        Assert.Equal(-dbl, decimal.ToDouble((decimal)(-long.MaxValue)));
    }

    [Fact]
    public static void TestToInt16()
    {
        // Int16 Decimal.ToInt16(Decimal)
        Assert.Equal(short.MaxValue, decimal.ToInt16((decimal)short.MaxValue));
        Assert.Equal(short.MinValue, decimal.ToInt16((decimal)short.MinValue));
    }

    [Fact]
    public static void TestToInt32()
    {
        // Int32 Decimal.ToInt32(Decimal)
        Assert.Equal(int.MaxValue, decimal.ToInt32((decimal)int.MaxValue));
        Assert.Equal(int.MinValue, decimal.ToInt32((decimal)int.MinValue));
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
                new object[] { decimal.MaxValue, new int[] { unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), 0x00000000 } },
                new object[] { decimal.MinValue, new int[] { unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0x80000000) } },
                new object[] {-7.9228162514264337593543950335M, new int[] { unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFFF), unchecked((int)0x801C0000) } }
            };
        }
    }

    [Theory, MemberData(nameof(DecimalTestData))]
    public static void TestGetBits(decimal input, int[] expectedBits)
    {
        int[] actualsBits = decimal.GetBits(input);

        Assert.Equal(expectedBits, actualsBits);

        bool sign = (actualsBits[3] & 0x80000000) != 0;
        byte scale = (byte)((actualsBits[3] >> 16) & 0x7F);
        decimal newValue = new decimal(actualsBits[0], actualsBits[1], actualsBits[2], sign, scale);

        Assert.Equal(input, newValue);
    }

    [Fact]
    public static void TestToInt64()
    {
        // Int64 Decimal.ToInt64(Decimal)
        Assert.Equal(long.MaxValue, decimal.ToInt64((decimal)long.MaxValue));
        Assert.Equal(long.MinValue, decimal.ToInt64((decimal)long.MinValue));
    }

    [Fact]
    public static void TestToSByte()
    {
        // SByte Decimal.ToSByte(Decimal)
        Assert.Equal(sbyte.MaxValue, decimal.ToSByte((decimal)sbyte.MaxValue));
        Assert.Equal(sbyte.MinValue, decimal.ToSByte((decimal)sbyte.MinValue));
    }

    [Fact]
    public static void TestToUInt16()
    {
        // UInt16 Decimal.ToUInt16(Decimal)
        Assert.Equal(ushort.MaxValue, decimal.ToUInt16((decimal)ushort.MaxValue));
        Assert.Equal(ushort.MinValue, decimal.ToUInt16((decimal)ushort.MinValue));
    }

    [Fact]
    public static void TestToUInt32()
    {
        // UInt32 Decimal.ToUInt32(Decimal)
        Assert.Equal(uint.MaxValue, decimal.ToUInt32((decimal)uint.MaxValue));
        Assert.Equal(uint.MinValue, decimal.ToUInt32((decimal)uint.MinValue));
    }

    [Fact]
    public static void TestToUInt64()
    {
        // UInt64 Decimal.ToUInt64(Decimal)
        Assert.Equal(ulong.MaxValue, decimal.ToUInt64((decimal)ulong.MaxValue));
        Assert.Equal(ulong.MinValue, decimal.ToUInt64((decimal)ulong.MinValue));
    }

    [Fact]
    public static void TestToString()
    {
        // String Decimal.ToString()
        decimal d1 = 6310.23m;
        Assert.Equal(string.Format("{0}", 6310.23), d1.ToString());

        decimal d2 = -8249.000003m;
        Assert.Equal(string.Format("{0}", -8249.000003), d2.ToString());

        Assert.Equal("79228162514264337593543950335", decimal.MaxValue.ToString());
        Assert.Equal("-79228162514264337593543950335", decimal.MinValue.ToString());
    }

    [Fact]
    public static void Testctor()
    {
        decimal d;
        // Void Decimal..ctor(Double)
        d = new decimal((Double)123456789.123456);
        Assert.Equal<Decimal>(d, (decimal)123456789.123456);

        // Void Decimal..ctor(Int32)
        d = new decimal((int)int.MaxValue);
        Assert.Equal<Decimal>(d, int.MaxValue);

        // Void Decimal..ctor(Int64)
        d = new decimal((long)long.MaxValue);
        Assert.Equal<Decimal>(d, long.MaxValue);

        // Void Decimal..ctor(Single)
        d = new decimal((Single)123.123);
        Assert.Equal<Decimal>(d, (decimal)123.123);

        // Void Decimal..ctor(UInt32)
        d = new decimal((uint)uint.MaxValue);
        Assert.Equal<Decimal>(d, uint.MaxValue);

        // Void Decimal..ctor(UInt64)
        d = new decimal((ulong)ulong.MaxValue);
        Assert.Equal<Decimal>(d, ulong.MaxValue);

        // Void Decimal..ctor(Int32, Int32, Int32, Boolean, Byte)
        d = new decimal(1, 1, 1, false, 0);
        decimal d2 = 3;
        d2 += uint.MaxValue;
        d2 += ulong.MaxValue;
        Assert.Equal(d, d2);

        // Void Decimal..ctor(Int32[])
        d = new decimal(new int[] { 1, 1, 1, 0 });
        Assert.Equal(d, d2);
    }

    [Fact]
    public static void TestNumberBufferLimit()
    {
        decimal dE = 1234567890123456789012345.6785m;
        string s1 = "1234567890123456789012345.678456";
        var nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        decimal d1 = decimal.Parse(s1, nfi);
        Assert.Equal(d1, dE);
        return;
    }
}
