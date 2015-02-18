// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class DoubleTests
{
    [Fact]
    public static void TestCtor()
    {
        Double i = new Double();
        Assert.True(i == 0);

        i = 41;
        Assert.True(i == 41);

        i = (Double)41.3;
        Assert.True(i == (Double)41.3);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Double max = Double.MaxValue;

        Assert.True(max == (Double)1.7976931348623157E+308);
    }

    [Fact]
    public static void TestMinValue()
    {
        Double min = Double.MinValue;

        Assert.True(min == ((Double)(-1.7976931348623157E+308)));
    }

    [Fact]
    public static void TestEpsilon()
    {
        // Double Double.Epsilon
        Assert.Equal<Double>((Double)4.9406564584124654E-324, Double.Epsilon);
    }

    [Fact]
    public static void TestIsInfinity()
    {
        // Boolean Double.IsInfinity(Double)
        Assert.True(Double.IsInfinity(Double.NegativeInfinity));
        Assert.True(Double.IsInfinity(Double.PositiveInfinity));
    }

    [Fact]
    public static void TestNaN()
    {
        // Double Double.NaN
        Assert.Equal<Double>((Double)0.0 / (Double)0.0, Double.NaN);
    }

    [Fact]
    public static void TestIsNaN()
    {
        // Boolean Double.IsNaN(Double)
        Assert.True(Double.IsNaN(Double.NaN));
    }

    [Fact]
    public static void TestNegativeInfinity()
    {
        // Double Double.NegativeInfinity
        Assert.Equal<Double>((Double)(-1.0) / (Double)0.0, Double.NegativeInfinity);
    }

    [Fact]
    public static void TestIsNegativeInfinity()
    {
        // Boolean Double.IsNegativeInfinity(Double)
        Assert.True(Double.IsNegativeInfinity(Double.NegativeInfinity));
    }

    [Fact]
    public static void TestPositiveInfinity()
    {
        // Double Double.PositiveInfinity
        Assert.Equal<Double>((Double)1.0 / (Double)0.0, Double.PositiveInfinity);
    }

    [Fact]
    public static void TestIsPositiveInfinity()
    {
        // Boolean Double.IsPositiveInfinity(Double)
        Assert.True(Double.IsPositiveInfinity(Double.PositiveInfinity));
    }

    [Fact]
    public static void TestCompareToObject()
    {
        Double i = 234;
        IComparable comparable = i;

        Assert.Equal(1, comparable.CompareTo(null));
        Assert.Equal(0, comparable.CompareTo((Double)234));

        Assert.True(comparable.CompareTo(Double.MinValue) > 0);
        Assert.True(comparable.CompareTo((Double)0) > 0);
        Assert.True(comparable.CompareTo((Double)(-123)) > 0);
        Assert.True(comparable.CompareTo((Double)123) > 0);
        Assert.True(comparable.CompareTo((Double)456) < 0);
        Assert.True(comparable.CompareTo(Double.MaxValue) < 0);

        Assert.Throws<ArgumentException>(() => comparable.CompareTo("a"));
    }

    [Fact]
    public static void TestCompareTo()
    {
        Double i = 234;

        Assert.Equal(0, i.CompareTo((Double)234));

        Assert.True(i.CompareTo(Double.MinValue) > 0);
        Assert.True(i.CompareTo((Double)0) > 0);
        Assert.True(i.CompareTo((Double)(-123)) > 0);
        Assert.True(i.CompareTo((Double)123) > 0);
        Assert.True(i.CompareTo((Double)456) < 0);
        Assert.True(i.CompareTo(Double.MaxValue) < 0);

        Assert.True(Double.NaN.CompareTo(Double.NaN) == 0);
        Assert.True(Double.NaN.CompareTo(0) < 0);
        Assert.True(i.CompareTo(Double.NaN) > 0);
    }

    [Fact]
    public static void TestEqualsObject()
    {
        Double i = 789;

        object obj1 = (Double)789;
        Assert.True(i.Equals(obj1));

        object obj2 = (Double)(-789);
        Assert.True(!i.Equals(obj2));

        object obj3 = (Double)0;
        Assert.True(!i.Equals(obj3));
    }

    [Fact]
    public static void TestEquals()
    {
        Double i = -911;

        Assert.True(i.Equals((Double)(-911)));

        Assert.True(!i.Equals((Double)911));
        Assert.True(!i.Equals((Double)0));
        Assert.True(Double.NaN.Equals(Double.NaN));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        Double i1 = 123;
        Double i2 = 654;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        Double i1 = 6310;
        Assert.Equal("6310", i1.ToString());

        Double i2 = -8249;
        Assert.Equal("-8249", i2.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        Double i1 = 6310;
        Assert.Equal("6310", i1.ToString(numberFormat));

        Double i2 = -8249;
        Assert.Equal("-8249", i2.ToString(numberFormat));

        Double i3 = -2468;

        // Changing the negative pattern doesn't do anything without also passing in a format string
        numberFormat.NumberNegativePattern = 0;
        Assert.Equal("-2468", i3.ToString(numberFormat));

        Assert.Equal("NaN", Double.NaN.ToString(NumberFormatInfo.InvariantInfo));
        Assert.Equal("Infinity", Double.PositiveInfinity.ToString(NumberFormatInfo.InvariantInfo));
        Assert.Equal("-Infinity", Double.NegativeInfinity.ToString(NumberFormatInfo.InvariantInfo));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        Double i1 = 6310;
        Assert.Equal("6310", i1.ToString("G"));

        Double i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g"));

        Double i3 = -2468;
        Assert.Equal(string.Format("{0:N}", -2468.00), i3.ToString("N"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        Double i1 = 6310;
        Assert.Equal("6310", i1.ToString("G", numberFormat));

        Double i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        Double i3 = -2468;
        Assert.Equal("(2*468.00)", i3.ToString("N", numberFormat));
    }

    [Fact]
    public static void TestParse()
    {
        Assert.Equal(123, Double.Parse("123"));
        Assert.Equal(-123, Double.Parse("-123"));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyle()
    {
        Assert.Equal<Double>((Double)123.1, Double.Parse(string.Format("{0}", 123.1), NumberStyles.AllowDecimalPoint));
        Assert.Equal(1000, Double.Parse(string.Format("{0}", 1000), NumberStyles.AllowThousands));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal(123, Double.Parse("123", nfi));
        Assert.Equal(-123, Double.Parse("-123", nfi));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyleFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        nfi.NumberDecimalSeparator = ".";
        Assert.Equal<Double>((Double)123.123, Double.Parse("123.123", NumberStyles.Float, nfi));

        nfi.CurrencySymbol = "$";
        nfi.CurrencyGroupSeparator = ",";
        Assert.Equal(1000, Double.Parse("$1,000", NumberStyles.Currency, nfi));
        //TODO: Negative tests once we get better exception support
    }

    [Fact]
    public static void TestTryParse()
    {
        // Defaults AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowDecimalPoint | AllowExponent | AllowThousands

        Double i;
        Assert.True(Double.TryParse("123", out i));     // Simple
        Assert.Equal(123, i);

        Assert.True(Double.TryParse("-385", out i));    // LeadingSign
        Assert.Equal(-385, i);

        Assert.True(Double.TryParse(" 678 ", out i));   // Leading/Trailing whitespace
        Assert.Equal(678, i);

        Assert.True(Double.TryParse((678.90).ToString("F2"), out i)); // Decimal
        Assert.Equal((Double)678.90, i);

        Assert.True(Double.TryParse("1E23", out i));   // Exponent
        Assert.Equal((Double)1E23, i);

        Assert.True(Double.TryParse((1000).ToString("N0"), out i));  // Thousands
        Assert.Equal(1000, i);

        var nfi = new NumberFormatInfo() { CurrencyGroupSeparator = "" };
        Assert.False(Double.TryParse((1000).ToString("C0", nfi), out i));  // Currency
        Assert.False(Double.TryParse("abc", out i));    // Hex digits
        Assert.False(Double.TryParse("(135)", out i));  // Parentheses
    }

    [Fact]
    public static void TestTryParseNumberStyleFormatProvider()
    {
        Double i;
        var nfi = new NumberFormatInfo();
        nfi.NumberDecimalSeparator = ".";
        Assert.True(Double.TryParse("123.123", NumberStyles.Any, nfi, out i));   // Simple positive
        Assert.Equal((Double)123.123, i);

        Assert.True(Double.TryParse("123", NumberStyles.Float, nfi, out i));   // Simple Hex
        Assert.Equal(123, i);

        nfi.CurrencySymbol = "$";
        nfi.CurrencyGroupSeparator = ",";
        Assert.True(Double.TryParse("$1,000", NumberStyles.Currency, nfi, out i)); // Currency/Thousands postive
        Assert.Equal(1000, i);

        Assert.False(Double.TryParse("abc", NumberStyles.None, nfi, out i));       // Hex Number negative

        Assert.False(Double.TryParse("678.90", NumberStyles.Integer, nfi, out i));  // Decimal
        Assert.False(Double.TryParse(" 678 ", NumberStyles.None, nfi, out i));      // Trailing/Leading whitespace negative

        Assert.True(Double.TryParse("(135)", NumberStyles.AllowParentheses, nfi, out i)); // Parenthese postive
        Assert.Equal(-135, i);

        Assert.True(Double.TryParse("Infinity", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(Double.IsPositiveInfinity(i));

        Assert.True(Double.TryParse("-Infinity", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(Double.IsNegativeInfinity(i));

        Assert.True(Double.TryParse("NaN", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(Double.IsNaN(i));
    }
}

