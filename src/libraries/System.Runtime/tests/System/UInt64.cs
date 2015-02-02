// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class UInt64Tests
{
    [Fact]
    public static void TestCtor()
    {
        UInt64 i = new UInt64();
        Assert.True(i == 0);

        i = 41;
        Assert.True(i == 41);
    }

    [Fact]
    public static void TestMaxValue()
    {
        UInt64 max = UInt64.MaxValue;

        Assert.True(max == (UInt64)0xFFFFFFFFFFFFFFFF);
    }

    [Fact]
    public static void TestMinValue()
    {
        UInt64 min = UInt64.MinValue;

        Assert.True(min == 0);
    }

    [Fact]
    public static void TestCompareToObject()
    {
        UInt64 i = 234;
        IComparable comparable = i;

        Assert.Equal(1, comparable.CompareTo(null));
        Assert.Equal(0, comparable.CompareTo((UInt64)234));

        Assert.True(comparable.CompareTo(UInt64.MinValue) > 0);
        Assert.True(comparable.CompareTo((UInt64)0) > 0);
        Assert.True(comparable.CompareTo((UInt64)(23)) > 0);
        Assert.True(comparable.CompareTo((UInt64)123) > 0);
        Assert.True(comparable.CompareTo((UInt64)456) < 0);
        Assert.True(comparable.CompareTo(UInt64.MaxValue) < 0);

        Assert.Throws<ArgumentException>(() => comparable.CompareTo("a"));
    }

    [Fact]
    public static void TestCompareTo()
    {
        UInt64 i = 234;

        Assert.Equal(0, i.CompareTo((UInt64)234));

        Assert.True(i.CompareTo(UInt64.MinValue) > 0);
        Assert.True(i.CompareTo((UInt64)0) > 0);
        Assert.True(i.CompareTo((UInt64)123) > 0);
        Assert.True(i.CompareTo((UInt64)456) < 0);
        Assert.True(i.CompareTo(UInt64.MaxValue) < 0);
    }

    [Fact]
    public static void TestEqualsObject()
    {
        UInt64 i = 789;

        object obj1 = (UInt64)789;
        Assert.True(i.Equals(obj1));

        object obj3 = (UInt64)0;
        Assert.True(!i.Equals(obj3));
    }

    [Fact]
    public static void TestEquals()
    {
        UInt64 i = 911;

        Assert.True(i.Equals((UInt64)911));
        Assert.True(!i.Equals((UInt64)0));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        UInt64 i1 = 123;
        UInt64 i2 = 654;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        UInt64 i1 = 6310;
        Assert.Equal("6310", i1.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        UInt64 i1 = 6310;
        Assert.Equal("6310", i1.ToString(numberFormat));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        UInt64 i1 = 6310;
        Assert.Equal("6310", i1.ToString("G"));

        UInt64 i2 = 8249;
        Assert.Equal("8249", i2.ToString("g"));

        UInt64 i3 = 2468;
        Assert.Equal("2,468.00", i3.ToString("N"));

        UInt64 i4 = 0x248;
        Assert.Equal("248", i4.ToString("x"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        UInt64 i1 = 6310;
        Assert.Equal("6310", i1.ToString("G", numberFormat));

        UInt64 i2 = 8249;
        Assert.Equal("8249", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        UInt64 i3 = 2468;
        Assert.Equal("2*468.00", i3.ToString("N", numberFormat));
    }

    [Fact]
    public static void TestParse()
    {
        Assert.Equal<UInt64>(123, UInt64.Parse("123"));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyle()
    {
        Assert.Equal<UInt64>(0x123, UInt64.Parse("123", NumberStyles.HexNumber));
        Assert.Equal<UInt64>(1000, UInt64.Parse("1,000", NumberStyles.AllowThousands));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal<UInt64>(123, UInt64.Parse("123", nfi));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyleFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal<UInt64>(0x123, UInt64.Parse("123", NumberStyles.HexNumber, nfi));

        nfi.CurrencySymbol = "$";
        Assert.Equal<UInt64>(1000, UInt64.Parse("$1,000", NumberStyles.Currency, nfi));
        //TODO: Negative tests once we get better exception support
    }

    [Fact]
    public static void TestTryParse()
    {
        // Defaults NumberStyles.Integer = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign

        UInt64 i;
        Assert.True(UInt64.TryParse("123", out i));     // Simple
        Assert.Equal<UInt64>(123, i);

        Assert.False(UInt64.TryParse("-385", out i));    // LeadingSign negative

        Assert.True(UInt64.TryParse(" 678 ", out i));   // Leading/Trailing whitespace
        Assert.Equal<UInt64>(678, i);

        Assert.False(UInt64.TryParse("$1000", out i));  // Currency
        Assert.False(UInt64.TryParse("1,000", out i));  // Thousands
        Assert.False(UInt64.TryParse("abc", out i));    // Hex digits
        Assert.False(UInt64.TryParse("678.90", out i)); // Decimal
        Assert.False(UInt64.TryParse("(135)", out i));  // Parentheses
        Assert.False(UInt64.TryParse("1E23", out i));   // Exponent
    }

    [Fact]
    public static void TestTryParseNumberStyleFormatProvider()
    {
        UInt64 i;
        var nfi = new NumberFormatInfo();
        Assert.True(UInt64.TryParse("123", NumberStyles.Any, nfi, out i));   // Simple positive
        Assert.Equal<UInt64>(123, i);

        Assert.True(UInt64.TryParse("123", NumberStyles.HexNumber, nfi, out i));   // Simple Hex
        Assert.Equal<UInt64>(0x123, i);

        nfi.CurrencySymbol = "$";
        Assert.True(UInt64.TryParse("$1,000", NumberStyles.Currency, nfi, out i)); // Currency/Thousands postive
        Assert.Equal<UInt64>(1000, i);

        Assert.False(UInt64.TryParse("abc", NumberStyles.None, nfi, out i));       // Hex Number negative
        Assert.True(UInt64.TryParse("abc", NumberStyles.HexNumber, nfi, out i));   // Hex Number positive
        Assert.Equal<UInt64>(0xabc, i);

        Assert.False(UInt64.TryParse("678.90", NumberStyles.Integer, nfi, out i));  // Decimal
        Assert.False(UInt64.TryParse(" 678 ", NumberStyles.None, nfi, out i));      // Trailing/Leading whitespace negative

        Assert.False(UInt64.TryParse("(135)", NumberStyles.AllowParentheses, nfi, out i)); // Parenthese negative
    }
}

