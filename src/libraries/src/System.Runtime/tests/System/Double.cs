// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.Tests.Common;

using Xunit;

public static class DoubleTests
{
    [Fact]
    public static void TestCtorEmpty()
    {
        double i = new double();
        Assert.Equal(0, i);
    }

    [Fact]
    public static void TestCtorValue()
    {
        double i = 41;
        Assert.Equal(41, i);

        i = 41.3;
        Assert.Equal(41.3, i);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Assert.Equal((double)1.7976931348623157E+308, double.MaxValue);
    }

    [Fact]
    public static void TestMinValue()
    {
        Assert.Equal((double)(-1.7976931348623157E+308), double.MinValue);
    }

    [Fact]
    public static void TestEpsilon()
    {
        Assert.Equal((double)4.9406564584124654E-324, double.Epsilon);
    }

    [Fact]
    public static void TestIsInfinity()
    {
        Assert.True(double.IsInfinity(double.NegativeInfinity));
        Assert.True(double.IsInfinity(double.PositiveInfinity));
    }

    [Fact]
    public static void TestNaN()
    {
        Assert.Equal((double)0.0 / (double)0.0, double.NaN);
    }

    [Fact]
    public static void TestIsNaN()
    {
        Assert.True(double.IsNaN(double.NaN));
    }

    [Fact]
    public static void TestNegativeInfinity()
    {
        Assert.Equal((double)(-1.0) / (double)0.0, double.NegativeInfinity);
    }

    [Fact]
    public static void TestIsNegativeInfinity()
    {
        Assert.True(double.IsNegativeInfinity(double.NegativeInfinity));
    }

    [Fact]
    public static void TestPositiveInfinity()
    {
        Assert.Equal((double)1.0 / (double)0.0, double.PositiveInfinity);
    }

    [Fact]
    public static void TestIsPositiveInfinity()
    {
        Assert.True(double.IsPositiveInfinity(double.PositiveInfinity));
    }

    [Theory]
    [InlineData((double)234, (double)234, 0)]
    [InlineData((double)234, double.MinValue, 1)]
    [InlineData((double)234, (double)(-123), 1)]
    [InlineData((double)234, (double)0, 1)]
    [InlineData((double)234, (double)123, 1)]
    [InlineData((double)234, (double)456, -1)]
    [InlineData((double)234, double.MaxValue, -1)]
    [InlineData((double)234, double.NaN, 1)]
    [InlineData(double.NaN, double.NaN, 0)]
    [InlineData(double.NaN, 0, -1)]
    public static void TestCompareTo(double i, double value, int expected)
    {
        int result = CompareHelper.NormalizeCompare(i.CompareTo(value));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData((double)234, 0)]
    [InlineData(double.MinValue, 1)]
    [InlineData((double)(-123), 1)]
    [InlineData((double)0, 1)]
    [InlineData((double)123, 1)]
    [InlineData((double)456, -1)]
    [InlineData(double.MaxValue, -1)]
    public static void TestCompareToObject(object obj, int expected)
    {
        IComparable comparable = (double)234;
        int i = CompareHelper.NormalizeCompare(comparable.CompareTo(obj));
        Assert.Equal(expected, i);
    }

    [Fact]
    public static void TestCompareToObjectInvalid()
    {
        IComparable comparable = (double)234;
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("a")); //Obj is not a double
    }

    [Theory]
    [InlineData((double)789, true)]
    [InlineData((double)(-789), false)]
    [InlineData((double)0, false)]
    public static void TestEqualsObject(object obj, bool expected)
    {
        double i = 789;
        Assert.Equal(expected, i.Equals(obj));
    }

    [Theory]
    [InlineData((double)789, (double)789, true)]
    [InlineData((double)789, (double)(-789), false)]
    [InlineData((double)789, (double)0, false)]
    [InlineData(double.NaN, double.NaN, true)]
    public static void TestEquals(double i1, double i2, bool expected)
    {
        Assert.Equal(expected, i1.Equals(i2));
    }


    [Fact]
    public static void TestGetHashCode()
    {
        double i1 = 123;
        double i2 = 654;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        double i1 = 6310;
        Assert.Equal("6310", i1.ToString());

        double i2 = -8249;
        Assert.Equal("-8249", i2.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        double i1 = 6310;
        Assert.Equal("6310", i1.ToString(numberFormat));

        double i2 = -8249;
        Assert.Equal("-8249", i2.ToString(numberFormat));

        double i3 = -2468;

        // Changing the negative pattern doesn't do anything without also passing in a format string
        numberFormat.NumberNegativePattern = 0;
        Assert.Equal("-2468", i3.ToString(numberFormat));

        Assert.Equal("NaN", double.NaN.ToString(NumberFormatInfo.InvariantInfo));
        Assert.Equal("Infinity", double.PositiveInfinity.ToString(NumberFormatInfo.InvariantInfo));
        Assert.Equal("-Infinity", double.NegativeInfinity.ToString(NumberFormatInfo.InvariantInfo));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        double i1 = 6310;
        Assert.Equal("6310", i1.ToString("G"));

        double i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g"));

        double i3 = -2468;
        Assert.Equal(string.Format("{0:N}", -2468.00), i3.ToString("N"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        double i1 = 6310;
        Assert.Equal("6310", i1.ToString("G", numberFormat));

        double i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        double i3 = -2468;
        Assert.Equal("(2*468.00)", i3.ToString("N", numberFormat));
    }

    [Fact]
    public static void TestParse()
    {
        Assert.Equal(123, double.Parse("123"));
        Assert.Equal(-123, double.Parse("-123"));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyle()
    {
        Assert.Equal((double)123.1, double.Parse(string.Format("{0}", 123.1), NumberStyles.AllowDecimalPoint));
        Assert.Equal(1000, double.Parse(string.Format("{0}", 1000), NumberStyles.AllowThousands));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal(123, double.Parse("123", nfi));
        Assert.Equal(-123, double.Parse("-123", nfi));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyleFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        nfi.NumberDecimalSeparator = ".";
        Assert.Equal((double)123.123, double.Parse("123.123", NumberStyles.Float, nfi));

        nfi.CurrencySymbol = "$";
        nfi.CurrencyGroupSeparator = ",";
        Assert.Equal(1000, double.Parse("$1,000", NumberStyles.Currency, nfi));
        //TODO: Negative tests once we get better exception support
    }

    [Fact]
    public static void TestTryParse()
    {
        // Defaults AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowDecimalPoint | AllowExponent | AllowThousands

        double i;
        Assert.True(double.TryParse("123", out i));     // Simple
        Assert.Equal(123, i);

        Assert.True(double.TryParse("-385", out i));    // LeadingSign
        Assert.Equal(-385, i);

        Assert.True(double.TryParse(" 678 ", out i));   // Leading/Trailing whitespace
        Assert.Equal(678, i);

        Assert.True(double.TryParse((678.90).ToString("F2"), out i)); // Decimal
        Assert.Equal((double)678.90, i);

        Assert.True(double.TryParse("1E23", out i));   // Exponent
        Assert.Equal((double)1E23, i);

        Assert.True(double.TryParse((1000).ToString("N0"), out i));  // Thousands
        Assert.Equal(1000, i);

        var nfi = new NumberFormatInfo() { CurrencyGroupSeparator = "" };
        Assert.False(double.TryParse((1000).ToString("C0", nfi), out i));  // Currency
        Assert.False(double.TryParse("abc", out i));    // Hex digits
        Assert.False(double.TryParse("(135)", out i));  // Parentheses
    }

    [Fact]
    public static void TestTryParseNumberStyleFormatProvider()
    {
        double i;
        var nfi = new NumberFormatInfo();
        nfi.NumberDecimalSeparator = ".";
        Assert.True(double.TryParse("123.123", NumberStyles.Any, nfi, out i));   // Simple positive
        Assert.Equal((double)123.123, i);

        Assert.True(double.TryParse("123", NumberStyles.Float, nfi, out i));   // Simple Hex
        Assert.Equal(123, i);

        nfi.CurrencySymbol = "$";
        nfi.CurrencyGroupSeparator = ",";
        Assert.True(double.TryParse("$1,000", NumberStyles.Currency, nfi, out i)); // Currency/Thousands postive
        Assert.Equal(1000, i);

        Assert.False(double.TryParse("abc", NumberStyles.None, nfi, out i));       // Hex Number negative

        Assert.False(double.TryParse("678.90", NumberStyles.Integer, nfi, out i));  // Decimal
        Assert.False(double.TryParse(" 678 ", NumberStyles.None, nfi, out i));      // Trailing/Leading whitespace negative

        Assert.True(double.TryParse("(135)", NumberStyles.AllowParentheses, nfi, out i)); // Parenthese postive
        Assert.Equal(-135, i);

        Assert.True(double.TryParse("Infinity", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(double.IsPositiveInfinity(i));

        Assert.True(double.TryParse("-Infinity", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(double.IsNegativeInfinity(i));

        Assert.True(double.TryParse("NaN", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(double.IsNaN(i));
    }
}
