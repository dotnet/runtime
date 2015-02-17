// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class UInt16Tests
{
    [Fact]
    public static void TestCtor()
    {
        UInt16 i = new UInt16();
        Assert.True(i == 0);

        i = 41;
        Assert.True(i == 41);
    }

    [Fact]
    public static void TestMaxValue()
    {
        UInt16 max = UInt16.MaxValue;

        Assert.True(max == (UInt16)0xFFFF);
    }

    [Fact]
    public static void TestMinValue()
    {
        UInt16 min = UInt16.MinValue;

        Assert.True(min == 0);
    }

    [Fact]
    public static void TestCompareToObject()
    {
        UInt16 i = 234;
        IComparable comparable = i;

        Assert.Equal(1, comparable.CompareTo(null));
        Assert.Equal(0, comparable.CompareTo((UInt16)234));

        Assert.True(comparable.CompareTo(UInt16.MinValue) > 0);
        Assert.True(comparable.CompareTo((UInt16)0) > 0);
        Assert.True(comparable.CompareTo((UInt16)(23)) > 0);
        Assert.True(comparable.CompareTo((UInt16)123) > 0);
        Assert.True(comparable.CompareTo((UInt16)456) < 0);
        Assert.True(comparable.CompareTo(UInt16.MaxValue) < 0);

        Assert.Throws<ArgumentException>(() => comparable.CompareTo("a"));
    }

    [Fact]
    public static void TestCompareTo()
    {
        UInt16 i = 234;

        Assert.Equal(0, i.CompareTo((UInt16)234));

        Assert.True(i.CompareTo(UInt16.MinValue) > 0);
        Assert.True(i.CompareTo((UInt16)0) > 0);
        Assert.True(i.CompareTo((UInt16)123) > 0);
        Assert.True(i.CompareTo((UInt16)456) < 0);
        Assert.True(i.CompareTo(UInt16.MaxValue) < 0);
    }

    [Fact]
    public static void TestEqualsObject()
    {
        UInt16 i = 789;

        object obj1 = (UInt16)789;
        Assert.True(i.Equals(obj1));

        object obj3 = (UInt16)0;
        Assert.True(!i.Equals(obj3));
    }

    [Fact]
    public static void TestEquals()
    {
        UInt16 i = 911;

        Assert.True(i.Equals((UInt16)911));
        Assert.True(!i.Equals((UInt16)0));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        UInt16 i1 = 123;
        UInt16 i2 = 654;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        UInt16 i1 = 6310;
        Assert.Equal("6310", i1.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        UInt16 i1 = 6310;
        Assert.Equal("6310", i1.ToString(numberFormat));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        UInt16 i1 = 6310;
        Assert.Equal("6310", i1.ToString("G"));

        UInt16 i2 = 8249;
        Assert.Equal("8249", i2.ToString("g"));

        UInt16 i3 = 2468;
        Assert.Equal(string.Format("{0:N}", 2468.00), i3.ToString("N"));

        UInt16 i4 = 0x248;
        Assert.Equal("248", i4.ToString("x"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        UInt16 i1 = 6310;
        Assert.Equal("6310", i1.ToString("G", numberFormat));

        UInt16 i2 = 8249;
        Assert.Equal("8249", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        UInt16 i3 = 2468;
        Assert.Equal("2*468.00", i3.ToString("N", numberFormat));
    }

    [Fact]
    public static void TestParse()
    {
        Assert.Equal<UInt16>(123, UInt16.Parse("123"));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyle()
    {
        Assert.Equal<UInt16>(0x123, UInt16.Parse("123", NumberStyles.HexNumber));

        Assert.Equal<UInt16>(1000, UInt16.Parse((1000).ToString("N0"), NumberStyles.AllowThousands));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal<UInt16>(123, UInt16.Parse("123", nfi));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyleFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal<UInt16>(0x123, UInt16.Parse("123", NumberStyles.HexNumber, nfi));

        nfi.CurrencySymbol = "$";
        nfi.CurrencyGroupSeparator = ",";
        Assert.Equal<UInt16>(1000, UInt16.Parse("$1,000", NumberStyles.Currency, nfi));
        //TODO: Negative tests once we get better exception support
    }

    [Fact]
    public static void TestTryParse()
    {
        // Defaults NumberStyles.Integer = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign

        UInt16 i;
        Assert.True(UInt16.TryParse("123", out i));     // Simple
        Assert.Equal<UInt16>(123, i);

        Assert.False(UInt16.TryParse("-385", out i));    // LeadingSign negative

        Assert.True(UInt16.TryParse(" 678 ", out i));   // Leading/Trailing whitespace
        Assert.Equal<UInt16>(678, i);

        var nfi = new NumberFormatInfo() { CurrencyGroupSeparator = "" };
        Assert.False(UInt16.TryParse((1000).ToString("C0", nfi), out i));  // Currency
        Assert.False(UInt16.TryParse((1000).ToString("N0"), out i));  // Thousands
        Assert.False(UInt16.TryParse("abc", out i));    // Hex digits
        Assert.False(UInt16.TryParse((678.90).ToString("F2"), out i)); // Decimal
        Assert.False(UInt16.TryParse("(135)", out i));  // Parentheses
        Assert.False(UInt16.TryParse("1E23", out i));   // Exponent
    }

    [Fact]
    public static void TestTryParseNumberStyleFormatProvider()
    {
        UInt16 i;
        var nfi = new NumberFormatInfo();
        Assert.True(UInt16.TryParse("123", NumberStyles.Any, nfi, out i));   // Simple positive
        Assert.Equal<UInt16>(123, i);

        Assert.True(UInt16.TryParse("123", NumberStyles.HexNumber, nfi, out i));   // Simple Hex
        Assert.Equal<UInt16>(0x123, i);

        nfi.CurrencySymbol = "$";
        nfi.CurrencyGroupSeparator = ",";
        Assert.True(UInt16.TryParse("$1,000", NumberStyles.Currency, nfi, out i)); // Currency/Thousands postive
        Assert.Equal<UInt16>(1000, i);

        Assert.False(UInt16.TryParse("abc", NumberStyles.None, nfi, out i));       // Hex Number negative
        Assert.True(UInt16.TryParse("abc", NumberStyles.HexNumber, nfi, out i));   // Hex Number positive
        Assert.Equal<UInt16>(0xabc, i);

        nfi.NumberDecimalSeparator = ".";
        Assert.False(UInt16.TryParse("678.90", NumberStyles.Integer, nfi, out i));  // Decimal
        Assert.False(UInt16.TryParse(" 678 ", NumberStyles.None, nfi, out i));      // Trailing/Leading whitespace negative

        Assert.False(UInt16.TryParse("(135)", NumberStyles.AllowParentheses, nfi, out i)); // Parenthese negative
    }
}

