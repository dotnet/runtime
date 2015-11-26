// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.Tests.Common;

using Xunit;

public static class SingleTests
{
    [Fact]
    public static void TestCtorEmpty()
    {
        float i = new float();
        Assert.Equal(0, i);
    }

    [Fact]
    public static void TestCtorValue()
    {
        float i = 41;
        Assert.Equal(41, i);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Assert.Equal((float)3.40282346638528859e+38, float.MaxValue);
    }

    [Fact]
    public static void TestMinValue()
    {
        Assert.Equal((float)(-3.40282346638528859e+38), float.MinValue);
    }

    [Fact]
    public static void TestEpsilon()
    {
        Assert.Equal((float)1.4e-45, float.Epsilon);
    }

    [Fact]
    public static void TestIsInfinity()
    {
        Assert.True(float.IsInfinity(float.NegativeInfinity));
        Assert.True(float.IsInfinity(float.PositiveInfinity));
    }

    [Fact]
    public static void TestNaN()
    {
        Assert.Equal((float)0.0 / (float)0.0, float.NaN);
    }

    [Fact]
    public static void TestIsNaN()
    {
        Assert.True(float.IsNaN(float.NaN));
    }

    [Fact]
    public static void TestNegativeInfinity()
    {
        Assert.Equal((float)(-1.0) / (float)0.0, float.NegativeInfinity);
    }

    [Fact]
    public static void TestIsNegativeInfinity()
    {
        Assert.True(float.IsNegativeInfinity(float.NegativeInfinity));
    }

    [Fact]
    public static void TestPositiveInfinity()
    {
        Assert.Equal((float)1.0 / (float)0.0, float.PositiveInfinity);
    }

    [Fact]
    public static void TestIsPositiveInfinity()
    {
        Assert.True(float.IsPositiveInfinity(float.PositiveInfinity));
    }

    [Theory]
    [InlineData((float)234, (float)234, 0)]
    [InlineData((float)234, float.MinValue, 1)]
    [InlineData((float)234, (float)(-123), 1)]
    [InlineData((float)234, (float)0, 1)]
    [InlineData((float)234, (float)123, 1)]
    [InlineData((float)234, (float)456, -1)]
    [InlineData((float)234, float.MaxValue, -1)]
    [InlineData((float)234, float.NaN, 1)]
    [InlineData(float.NaN, float.NaN, 0)]
    [InlineData(float.NaN, 0, -1)]
    public static void TestCompareTo(float i, float value, int expected)
    {
        int result = CompareHelper.NormalizeCompare(i.CompareTo(value));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData((float)234, 0)]
    [InlineData(float.MinValue, 1)]
    [InlineData((float)(-123), 1)]
    [InlineData((float)0, 1)]
    [InlineData((float)123, 1)]
    [InlineData((float)456, -1)]
    [InlineData(float.MaxValue, -1)]
    public static void TestCompareToObject(object obj, int expected)
    {
        IComparable comparable = (float)234;
        int i = CompareHelper.NormalizeCompare(comparable.CompareTo(obj));
        Assert.Equal(expected, i);
    }

    [Fact]
    public static void TestCompareToObjectInvalid()
    {
        IComparable comparable = (float)234;
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("a")); //Obj is not a float
    }
    
    [Theory]
    [InlineData((float)789, true)]
    [InlineData((float)(-789), false)]
    [InlineData((float)0, false)]
    public static void TestEqualsObject(object obj, bool expected)
    {
        float i = 789;
        Assert.Equal(expected, i.Equals(obj));
    }

    [Theory]
    [InlineData((float)789, (float)789, true)]
    [InlineData((float)789, (float)(-789), false)]
    [InlineData((float)789, (float)0, false)]
    [InlineData(float.NaN, float.NaN, true)]
    public static void TestEquals(float i1, float i2, bool expected)
    {
        Assert.Equal(expected, i1.Equals(i2));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        float i1 = 123;
        float i2 = 654;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        float i1 = 6310;
        Assert.Equal("6310", i1.ToString());

        float i2 = -8249;
        Assert.Equal("-8249", i2.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        float i1 = 6310;
        Assert.Equal("6310", i1.ToString(numberFormat));

        float i2 = -8249;
        Assert.Equal("-8249", i2.ToString(numberFormat));

        float i3 = -2468;

        // Changing the negative pattern doesn't do anything without also passing in a format string
        numberFormat.NumberNegativePattern = 0;
        Assert.Equal("-2468", i3.ToString(numberFormat));

        Assert.Equal("NaN", float.NaN.ToString(NumberFormatInfo.InvariantInfo));
        Assert.Equal("Infinity", float.PositiveInfinity.ToString(NumberFormatInfo.InvariantInfo));
        Assert.Equal("-Infinity", float.NegativeInfinity.ToString(NumberFormatInfo.InvariantInfo));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        float i1 = 6310;
        Assert.Equal("6310", i1.ToString("G"));

        float i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g"));

        float i3 = -2468;
        Assert.Equal(string.Format("{0:N}", -2468.00), i3.ToString("N"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        float i1 = 6310;
        Assert.Equal("6310", i1.ToString("G", numberFormat));

        float i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        float i3 = -2468;
        Assert.Equal("(2*468.00)", i3.ToString("N", numberFormat));
    }

    [Fact]
    public static void TestParse()
    {
        Assert.Equal(123, float.Parse("123"));
        Assert.Equal(-123, float.Parse("-123"));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyle()
    {
        Assert.Equal(123.1f, float.Parse((123.1).ToString("F"), NumberStyles.AllowDecimalPoint));
        Assert.Equal(1000, float.Parse((1000).ToString("N0"), NumberStyles.AllowThousands));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal(123, float.Parse("123", nfi));
        Assert.Equal(-123, float.Parse("-123", nfi));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyleFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        nfi.NumberDecimalSeparator = ".";
        Assert.Equal(123.123f, float.Parse("123.123", NumberStyles.Float, nfi));

        nfi.CurrencySymbol = "$";
        nfi.CurrencyGroupSeparator = ",";
        Assert.Equal(1000, float.Parse("$1,000", NumberStyles.Currency, nfi));
        //TODO: Negative tests once we get better exception support
    }

    [Fact]
    public static void TestTryParse()
    {
        // Defaults AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowDecimalPoint | AllowExponent | AllowThousands

        float i;
        Assert.True(float.TryParse("123", out i));     // Simple
        Assert.Equal(123, i);

        Assert.True(float.TryParse("-385", out i));    // LeadingSign
        Assert.Equal(-385, i);

        Assert.True(float.TryParse(" 678 ", out i));   // Leading/Trailing whitespace
        Assert.Equal(678, i);

        Assert.True(float.TryParse((678.90).ToString("F2"), out i)); // Decimal
        Assert.Equal((float)678.90, i);

        Assert.True(float.TryParse("1E23", out i));   // Exponent
        Assert.Equal((float)1E23, i);

        Assert.True(float.TryParse((1000).ToString("N0"), out i));  // Thousands
        Assert.Equal(1000, i);

        var nfi = new NumberFormatInfo() { CurrencyGroupSeparator = "" };
        Assert.False(float.TryParse((1000).ToString("C0", nfi), out i));  // Currency
        Assert.False(float.TryParse("abc", out i));    // Hex digits
        Assert.False(float.TryParse("(135)", out i));  // Parentheses
    }

    [Fact]
    public static void TestTryParseNumberStyleFormatProvider()
    {
        float i;
        var nfi = new NumberFormatInfo();
        nfi.NumberDecimalSeparator = ".";
        Assert.True(float.TryParse("123.123", NumberStyles.Any, nfi, out i));   // Simple positive
        Assert.Equal(123.123f, i);

        Assert.True(float.TryParse("123", NumberStyles.Float, nfi, out i));   // Simple Hex
        Assert.Equal(123, i);

        nfi.CurrencySymbol = "$";
        nfi.CurrencyGroupSeparator = ",";
        Assert.True(float.TryParse("$1,000", NumberStyles.Currency, nfi, out i)); // Currency/Thousands postive
        Assert.Equal(1000, i);

        Assert.False(float.TryParse("abc", NumberStyles.None, nfi, out i));       // Hex Number negative

        Assert.False(float.TryParse("678.90", NumberStyles.Integer, nfi, out i));  // Decimal
        Assert.False(float.TryParse(" 678 ", NumberStyles.None, nfi, out i));      // Trailing/Leading whitespace negative

        Assert.True(float.TryParse("(135)", NumberStyles.AllowParentheses, nfi, out i)); // Parenthese postive
        Assert.Equal(-135, i);

        Assert.True(float.TryParse("Infinity", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(float.IsPositiveInfinity(i));

        Assert.True(float.TryParse("-Infinity", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(float.IsNegativeInfinity(i));

        Assert.True(float.TryParse("NaN", NumberStyles.Any, NumberFormatInfo.InvariantInfo, out i));
        Assert.True(float.IsNaN(i));
    }
}
