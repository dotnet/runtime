// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class Int32Tests
{
    [Fact]
    public static void TestCtor()
    {
        Int32 i = new Int32();
        Assert.True(i == 0);

        i = 41;
        Assert.True(i == 41);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Int32 max = Int32.MaxValue;

        Assert.True(max == (Int32)0x7FFFFFFF);
    }

    [Fact]
    public static void TestMinValue()
    {
        Int32 min = Int32.MinValue;

        Assert.True(min == unchecked((Int32)0x80000000));
    }

    [Fact]
    public static void TestCompareToObject()
    {
        Int32 i = 234;
        IComparable comparable = i;

        Assert.Equal(1, comparable.CompareTo(null));
        Assert.Equal(0, comparable.CompareTo((Int32)234));

        Assert.True(comparable.CompareTo(Int32.MinValue) > 0);
        Assert.True(comparable.CompareTo((Int32)0) > 0);
        Assert.True(comparable.CompareTo((Int32)(-123)) > 0);
        Assert.True(comparable.CompareTo((Int32)123) > 0);
        Assert.True(comparable.CompareTo((Int32)456) < 0);
        Assert.True(comparable.CompareTo(Int32.MaxValue) < 0);

        Assert.Throws<ArgumentException>(() => comparable.CompareTo("a"));
    }

    [Fact]
    public static void TestCompareTo()
    {
        Int32 i = 234;

        Assert.Equal(0, i.CompareTo((Int32)234));

        Assert.True(i.CompareTo(Int32.MinValue) > 0);
        Assert.True(i.CompareTo((Int32)0) > 0);
        Assert.True(i.CompareTo((Int32)(-123)) > 0);
        Assert.True(i.CompareTo((Int32)123) > 0);
        Assert.True(i.CompareTo((Int32)456) < 0);
        Assert.True(i.CompareTo(Int32.MaxValue) < 0);
    }

    [Fact]
    public static void TestEqualsObject()
    {
        Int32 i = 789;

        object obj1 = (Int32)789;
        Assert.True(i.Equals(obj1));

        object obj2 = (Int32)(-789);
        Assert.True(!i.Equals(obj2));

        object obj3 = (Int32)0;
        Assert.True(!i.Equals(obj3));
    }

    [Fact]
    public static void TestEquals()
    {
        Int32 i = -911;

        Assert.True(i.Equals((Int32)(-911)));

        Assert.True(!i.Equals((Int32)911));
        Assert.True(!i.Equals((Int32)0));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        Int32 i1 = 123;
        Int32 i2 = 654;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        Int32 i1 = 6310;
        Assert.Equal("6310", i1.ToString());

        Int32 i2 = -8249;
        Assert.Equal("-8249", i2.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        Int32 i1 = 6310;
        Assert.Equal("6310", i1.ToString(numberFormat));

        Int32 i2 = -8249;
        Assert.Equal("-8249", i2.ToString(numberFormat));

        Int32 i3 = -2468;

        // Changing the negative pattern doesn't do anything without also passing in a format string
        numberFormat.NumberNegativePattern = 0;
        Assert.Equal("-2468", i3.ToString(numberFormat));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        Int32 i1 = 6310;
        Assert.Equal("6310", i1.ToString("G"));

        Int32 i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g"));

        Int32 i3 = -2468;
        Assert.Equal("-2,468.00", i3.ToString("N"));

        Int32 i4 = 0x248;
        Assert.Equal("248", i4.ToString("x"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        Int32 i1 = 6310;
        Assert.Equal("6310", i1.ToString("G", numberFormat));

        Int32 i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        Int32 i3 = -2468;
        Assert.Equal("(2*468.00)", i3.ToString("N", numberFormat));
    }

    [Fact]
    public static void TestParse()
    {
        Assert.Equal(123, Int32.Parse("123"));
        Assert.Equal(-123, Int32.Parse("-123"));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyle()
    {
        Assert.Equal(0x123, Int32.Parse("123", NumberStyles.HexNumber));
        Assert.Equal(1000, Int32.Parse("1,000", NumberStyles.AllowThousands));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal(123, Int32.Parse("123", nfi));
        Assert.Equal(-123, Int32.Parse("-123", nfi));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyleFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal(0x123, Int32.Parse("123", NumberStyles.HexNumber, nfi));

        nfi.CurrencySymbol = "$";
        Assert.Equal(1000, Int32.Parse("$1,000", NumberStyles.Currency, nfi));
        //TODO: Negative tests once we get better exception support
    }

    [Fact]
    public static void TestTryParse()
    {
        // Defaults NumberStyles.Integer = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign

        Int32 i;
        Assert.True(Int32.TryParse("123", out i));     // Simple
        Assert.Equal(123, i);

        Assert.True(Int32.TryParse("-385", out i));    // LeadingSign
        Assert.Equal(-385, i);

        Assert.True(Int32.TryParse(" 678 ", out i));   // Leading/Trailing whitespace
        Assert.Equal(678, i);

        Assert.False(Int32.TryParse("$1000", out i));  // Currency
        Assert.False(Int32.TryParse("1,000", out i));  // Thousands
        Assert.False(Int32.TryParse("abc", out i));    // Hex digits
        Assert.False(Int32.TryParse("678.90", out i)); // Decimal
        Assert.False(Int32.TryParse("(135)", out i));  // Parentheses
        Assert.False(Int32.TryParse("1E23", out i));   // Exponent
    }

    [Fact]
    public static void TestTryParseNumberStyleFormatProvider()
    {
        Int32 i;
        var nfi = new NumberFormatInfo();
        Assert.True(Int32.TryParse("123", NumberStyles.Any, nfi, out i));   // Simple positive
        Assert.Equal(123, i);

        Assert.True(Int32.TryParse("123", NumberStyles.HexNumber, nfi, out i));   // Simple Hex
        Assert.Equal(0x123, i);

        nfi.CurrencySymbol = "$";
        Assert.True(Int32.TryParse("$1,000", NumberStyles.Currency, nfi, out i)); // Currency/Thousands postive
        Assert.Equal(1000, i);

        Assert.False(Int32.TryParse("abc", NumberStyles.None, nfi, out i));       // Hex Number negative
        Assert.True(Int32.TryParse("abc", NumberStyles.HexNumber, nfi, out i));   // Hex Number positive
        Assert.Equal(0xabc, i);

        Assert.False(Int32.TryParse("678.90", NumberStyles.Integer, nfi, out i));  // Decimal
        Assert.False(Int32.TryParse(" 678 ", NumberStyles.None, nfi, out i));      // Trailing/Leading whitespace negative

        Assert.True(Int32.TryParse("(135)", NumberStyles.AllowParentheses, nfi, out i)); // Parenthese postive
        Assert.Equal(-135, i);
    }
}

