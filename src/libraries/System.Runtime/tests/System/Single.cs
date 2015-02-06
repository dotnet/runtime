// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class SingleTests
{
    [Fact]
    public static void TestCtor()
    {
        Single i = new Single();
        Assert.True(i == 0);

        i = 41;
        Assert.True(i == 41);

        i = (Single)41.3;
        Assert.True(i == (Single)41.3);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Single max = Single.MaxValue;

        Assert.True(max == (Single)3.40282346638528859e+38);
    }

    [Fact]
    public static void TestMinValue()
    {
        Single min = Single.MinValue;

        Assert.True(min == ((Single)(-3.40282346638528859e+38)));
    }

    [Fact]
    public static void TestEpsilon()
    {
        // Single Single.Epsilon
        Assert.Equal<Single>((Single)1.4e-45, Single.Epsilon);
    }

    [Fact]
    public static void TestIsInfinity()
    {
        // Boolean Single.IsInfinity(Single)
        Assert.True(Single.IsInfinity(Single.NegativeInfinity));
        Assert.True(Single.IsInfinity(Single.PositiveInfinity));
    }

    [Fact]
    public static void TestNaN()
    {
        // Single Single.NaN
        Assert.Equal<Single>((Single)0.0 / (Single)0.0, Single.NaN);
    }

    [Fact]
    public static void TestIsNaN()
    {
        // Boolean Single.IsNaN(Single)
        Assert.True(Single.IsNaN(Single.NaN));
    }

    [Fact]
    public static void TestNegativeInfinity()
    {
        // Single Single.NegativeInfinity
        Assert.Equal<Single>((Single)(-1.0) / (Single)0.0, Single.NegativeInfinity);
    }

    [Fact]
    public static void TestIsNegativeInfinity()
    {
        // Boolean Single.IsNegativeInfinity(Single)
        Assert.True(Single.IsNegativeInfinity(Single.NegativeInfinity));
    }

    [Fact]
    public static void TestPositiveInfinity()
    {
        // Single Single.PositiveInfinity
        Assert.Equal<Single>((Single)1.0 / (Single)0.0, Single.PositiveInfinity);
    }

    [Fact]
    public static void TestIsPositiveInfinity()
    {
        // Boolean Single.IsPositiveInfinity(Single)
        Assert.True(Single.IsPositiveInfinity(Single.PositiveInfinity));
    }

    [Fact]
    public static void TestCompareToObject()
    {
        Single i = 234;
        IComparable comparable = i;

        Assert.Equal(1, comparable.CompareTo(null));
        Assert.Equal(0, comparable.CompareTo((Single)234));

        Assert.True(comparable.CompareTo(Single.MinValue) > 0);
        Assert.True(comparable.CompareTo((Single)0) > 0);
        Assert.True(comparable.CompareTo((Single)(-123)) > 0);
        Assert.True(comparable.CompareTo((Single)123) > 0);
        Assert.True(comparable.CompareTo((Single)456) < 0);
        Assert.True(comparable.CompareTo(Single.MaxValue) < 0);

        Assert.Throws<ArgumentException>(() => comparable.CompareTo("a"));
    }

    [Fact]
    public static void TestCompareTo()
    {
        Single i = 234;

        Assert.Equal(0, i.CompareTo((Single)234));

        Assert.True(i.CompareTo(Single.MinValue) > 0);
        Assert.True(i.CompareTo((Single)0) > 0);
        Assert.True(i.CompareTo((Single)(-123)) > 0);
        Assert.True(i.CompareTo((Single)123) > 0);
        Assert.True(i.CompareTo((Single)456) < 0);
        Assert.True(i.CompareTo(Single.MaxValue) < 0);

        Assert.True(Single.NaN.CompareTo(Single.NaN) == 0);
        Assert.True(Single.NaN.CompareTo(0) < 0);
        Assert.True(i.CompareTo(Single.NaN) > 0);
    }

    [Fact]
    public static void TestEqualsObject()
    {
        Single i = 789;

        object obj1 = (Single)789;
        Assert.True(i.Equals(obj1));

        object obj2 = (Single)(-789);
        Assert.True(!i.Equals(obj2));

        object obj3 = (Single)0;
        Assert.True(!i.Equals(obj3));
    }

    [Fact]
    public static void TestEquals()
    {
        Single i = -911;

        Assert.True(i.Equals((Single)(-911)));

        Assert.True(!i.Equals((Single)911));
        Assert.True(!i.Equals((Single)0));
        Assert.True(Single.NaN.Equals(Single.NaN));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        Single i1 = 123;
        Single i2 = 654;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        Single i1 = 6310;
        Assert.Equal("6310", i1.ToString());

        Single i2 = -8249;
        Assert.Equal("-8249", i2.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        Single i1 = 6310;
        Assert.Equal("6310", i1.ToString(numberFormat));

        Single i2 = -8249;
        Assert.Equal("-8249", i2.ToString(numberFormat));

        Single i3 = -2468;

        // Changing the negative pattern doesn't do anything without also passing in a format string
        numberFormat.NumberNegativePattern = 0;
        Assert.Equal("-2468", i3.ToString(numberFormat));

        Assert.Equal("NaN", Single.NaN.ToString(NumberFormatInfo.InvariantInfo));
        Assert.Equal("Infinity", Single.PositiveInfinity.ToString(NumberFormatInfo.InvariantInfo));
        Assert.Equal("-Infinity", Single.NegativeInfinity.ToString(NumberFormatInfo.InvariantInfo));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        Single i1 = 6310;
        Assert.Equal("6310", i1.ToString("G"));

        Single i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g"));

        Single i3 = -2468;
        Assert.Equal("-2,468.00", i3.ToString("N"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        Single i1 = 6310;
        Assert.Equal("6310", i1.ToString("G", numberFormat));

        Single i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        Single i3 = -2468;
        Assert.Equal("(2*468.00)", i3.ToString("N", numberFormat));
    }

    [Fact]
    public static void TestParse()
    {
        Assert.Equal(123, Single.Parse("123"));
        Assert.Equal(-123, Single.Parse("-123"));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyle()
    {
        Assert.Equal<Single>(123.1f, Single.Parse("123.1", NumberStyles.AllowDecimalPoint));
        Assert.Equal(1000, Single.Parse("1,000", NumberStyles.AllowThousands));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal(123, Single.Parse("123", nfi));
        Assert.Equal(-123, Single.Parse("-123", nfi));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyleFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal<Single>(123.123f, Single.Parse("123.123", NumberStyles.Float, nfi));

        nfi.CurrencySymbol = "$";
        Assert.Equal(1000, Single.Parse("$1,000", NumberStyles.Currency, nfi));
        //TODO: Negative tests once we get better exception support
    }

    [Fact]
    public static void TestTryParse()
    {
        // Defaults AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowDecimalPoint | AllowExponent | AllowThousands

        Single i;
        Assert.True(Single.TryParse("123", out i));     // Simple
        Assert.Equal(123, i);

        Assert.True(Single.TryParse("-385", out i));    // LeadingSign
        Assert.Equal(-385, i);

        Assert.True(Single.TryParse(" 678 ", out i));   // Leading/Trailing whitespace
        Assert.Equal(678, i);

        Assert.True(Single.TryParse("678.90", out i)); // Decimal
        Assert.Equal((Single)678.90, i);

        Assert.True(Single.TryParse("1E23", out i));   // Exponent
        Assert.Equal((Single)1E23, i);

        Assert.True(Single.TryParse("1,000", out i));  // Thousands
        Assert.Equal(1000, i);

        Assert.False(Single.TryParse("$1000", out i));  // Currency
        Assert.False(Single.TryParse("abc", out i));    // Hex digits
        Assert.False(Single.TryParse("(135)", out i));  // Parentheses
    }

    [Fact]
    public static void TestTryParseNumberStyleFormatProvider()
    {
        Single i;
        var nfi = new NumberFormatInfo();
        Assert.True(Single.TryParse("123.123", NumberStyles.Any, nfi, out i));   // Simple positive
        Assert.Equal(123.123f, i);

        Assert.True(Single.TryParse("123", NumberStyles.Float, nfi, out i));   // Simple Hex
        Assert.Equal(123, i);

        nfi.CurrencySymbol = "$";
        Assert.True(Single.TryParse("$1,000", NumberStyles.Currency, nfi, out i)); // Currency/Thousands postive
        Assert.Equal(1000, i);

        Assert.False(Single.TryParse("abc", NumberStyles.None, nfi, out i));       // Hex Number negative

        Assert.False(Single.TryParse("678.90", NumberStyles.Integer, nfi, out i));  // Decimal
        Assert.False(Single.TryParse(" 678 ", NumberStyles.None, nfi, out i));      // Trailing/Leading whitespace negative

        Assert.True(Single.TryParse("(135)", NumberStyles.AllowParentheses, nfi, out i)); // Parenthese postive
        Assert.Equal(-135, i);

        Assert.True(Single.TryParse("Infinity", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(Single.IsPositiveInfinity(i));

        Assert.True(Single.TryParse("-Infinity", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(Single.IsNegativeInfinity(i));

        Assert.True(Single.TryParse("NaN", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(Single.IsNaN(i));
    }
}

