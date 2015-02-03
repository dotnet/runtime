// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class UInt32Tests
{
    [Fact]
    public static void TestCtor()
    {
        UInt32 i = new UInt32();
        Assert.True(i == 0);

        i = 41;
        Assert.True(i == 41);
    }

    [Fact]
    public static void TestMaxValue()
    {
        UInt32 max = UInt32.MaxValue;

        Assert.True(max == (UInt32)0xFFFFFFFF);
    }

    [Fact]
    public static void TestMinValue()
    {
        UInt32 min = UInt32.MinValue;

        Assert.True(min == 0);
    }

    [Fact]
    public static void TestCompareToObject()
    {
        UInt32 i = 234;
        IComparable comparable = i;

        Assert.Equal(1, comparable.CompareTo(null));
        Assert.Equal(0, comparable.CompareTo((UInt32)234));

        Assert.True(comparable.CompareTo(UInt32.MinValue) > 0);
        Assert.True(comparable.CompareTo((UInt32)0) > 0);
        Assert.True(comparable.CompareTo((UInt32)(23)) > 0);
        Assert.True(comparable.CompareTo((UInt32)123) > 0);
        Assert.True(comparable.CompareTo((UInt32)456) < 0);
        Assert.True(comparable.CompareTo(UInt32.MaxValue) < 0);

        Assert.Throws<ArgumentException>(() => comparable.CompareTo("a"));
    }

    [Fact]
    public static void TestCompareTo()
    {
        UInt32 i = 234;

        Assert.Equal(0, i.CompareTo((UInt32)234));

        Assert.True(i.CompareTo(UInt32.MinValue) > 0);
        Assert.True(i.CompareTo((UInt32)0) > 0);
        Assert.True(i.CompareTo((UInt32)123) > 0);
        Assert.True(i.CompareTo((UInt32)456) < 0);
        Assert.True(i.CompareTo(UInt32.MaxValue) < 0);
    }

    [Fact]
    public static void TestEqualsObject()
    {
        UInt32 i = 789;

        object obj1 = (UInt32)789;
        Assert.True(i.Equals(obj1));

        object obj3 = (UInt32)0;
        Assert.True(!i.Equals(obj3));
    }

    [Fact]
    public static void TestEquals()
    {
        UInt32 i = 911;

        Assert.True(i.Equals((UInt32)911));
        Assert.True(!i.Equals((UInt32)0));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        UInt32 i1 = 123;
        UInt32 i2 = 654;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        UInt32 i1 = 6310;
        Assert.Equal("6310", i1.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        UInt32 i1 = 6310;
        Assert.Equal("6310", i1.ToString(numberFormat));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        UInt32 i1 = 6310;
        Assert.Equal("6310", i1.ToString("G"));

        UInt32 i2 = 8249;
        Assert.Equal("8249", i2.ToString("g"));

        UInt32 i3 = 2468;
        Assert.Equal("2,468.00", i3.ToString("N"));

        UInt32 i4 = 0x248;
        Assert.Equal("248", i4.ToString("x"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        UInt32 i1 = 6310;
        Assert.Equal("6310", i1.ToString("G", numberFormat));

        UInt32 i2 = 8249;
        Assert.Equal("8249", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        UInt32 i3 = 2468;
        Assert.Equal("2*468.00", i3.ToString("N", numberFormat));
    }

    [Fact]
    public static void TestParse()
    {
        Assert.Equal<UInt32>(123, UInt32.Parse("123"));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyle()
    {
        Assert.Equal<UInt32>(0x123, UInt32.Parse("123", NumberStyles.HexNumber));
        Assert.Equal<UInt32>(1000, UInt32.Parse("1,000", NumberStyles.AllowThousands));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal<UInt32>(123, UInt32.Parse("123", nfi));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyleFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal<UInt32>(0x123, UInt32.Parse("123", NumberStyles.HexNumber, nfi));

        nfi.CurrencySymbol = "$";
        Assert.Equal<UInt32>(1000, UInt32.Parse("$1,000", NumberStyles.Currency, nfi));
        //TODO: Negative tests once we get better exception support
    }

    [Fact]
    public static void TestTryParse()
    {
        // Defaults NumberStyles.Integer = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign

        UInt32 i;
        Assert.True(UInt32.TryParse("123", out i));     // Simple
        Assert.Equal<UInt32>(123, i);

        Assert.False(UInt32.TryParse("-385", out i));    // LeadingSign negative

        Assert.True(UInt32.TryParse(" 678 ", out i));   // Leading/Trailing whitespace
        Assert.Equal<UInt32>(678, i);

        Assert.False(UInt32.TryParse("$1000", out i));  // Currency
        Assert.False(UInt32.TryParse("1,000", out i));  // Thousands
        Assert.False(UInt32.TryParse("abc", out i));    // Hex digits
        Assert.False(UInt32.TryParse("678.90", out i)); // Decimal
        Assert.False(UInt32.TryParse("(135)", out i));  // Parentheses
        Assert.False(UInt32.TryParse("1E23", out i));   // Exponent
    }

    [Fact]
    public static void TestTryParseNumberStyleFormatProvider()
    {
        UInt32 i;
        var nfi = new NumberFormatInfo();
        Assert.True(UInt32.TryParse("123", NumberStyles.Any, nfi, out i));   // Simple positive
        Assert.Equal<UInt32>(123, i);

        Assert.True(UInt32.TryParse("123", NumberStyles.HexNumber, nfi, out i));   // Simple Hex
        Assert.Equal<UInt32>(0x123, i);

        nfi.CurrencySymbol = "$";
        Assert.True(UInt32.TryParse("$1,000", NumberStyles.Currency, nfi, out i)); // Currency/Thousands postive
        Assert.Equal<UInt32>(1000, i);

        Assert.False(UInt32.TryParse("abc", NumberStyles.None, nfi, out i));       // Hex Number negative
        Assert.True(UInt32.TryParse("abc", NumberStyles.HexNumber, nfi, out i));   // Hex Number positive
        Assert.Equal<UInt32>(0xabc, i);

        Assert.False(UInt32.TryParse("678.90", NumberStyles.Integer, nfi, out i));  // Decimal
        Assert.False(UInt32.TryParse(" 678 ", NumberStyles.None, nfi, out i));      // Trailing/Leading whitespace negative

        Assert.False(UInt32.TryParse("(135)", NumberStyles.AllowParentheses, nfi, out i)); // Parenthese negative
    }
}

