// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class SByteTests
{
    [Fact]
    public static void TestCtor()
    {
        SByte i = new SByte();
        Assert.True(i == 0);

        i = 41;
        Assert.True(i == 41);
    }

    [Fact]
    public static void TestMaxValue()
    {
        SByte max = SByte.MaxValue;

        Assert.True(max == (SByte)0x7F);
    }

    [Fact]
    public static void TestMinValue()
    {
        SByte min = SByte.MinValue;

        Assert.True(min == (SByte)(-0x80));
    }

    [Fact]
    public static void TestCompareToObject()
    {
        SByte i = 114;
        IComparable comparable = i;

        Assert.Equal(1, comparable.CompareTo(null));
        Assert.Equal(0, comparable.CompareTo((SByte)114));

        Assert.True(comparable.CompareTo(SByte.MinValue) > 0);
        Assert.True(comparable.CompareTo((SByte)0) > 0);
        Assert.True(comparable.CompareTo((SByte)(-23)) > 0);
        Assert.True(comparable.CompareTo((SByte)123) < 0);
        Assert.True(comparable.CompareTo((SByte)45) > 0);
        Assert.True(comparable.CompareTo(SByte.MaxValue) < 0);

        Assert.Throws<ArgumentException>(() => comparable.CompareTo("a"));
    }

    [Fact]
    public static void TestCompareTo()
    {
        SByte i = 114;

        Assert.Equal(0, i.CompareTo((SByte)114));

        Assert.True(i.CompareTo(SByte.MinValue) > 0);
        Assert.True(i.CompareTo((SByte)0) > 0);
        Assert.True(i.CompareTo((SByte)123) < 0);
        Assert.True(i.CompareTo((SByte)45) > 0);
        Assert.True(i.CompareTo(SByte.MaxValue) < 0);
    }

    [Fact]
    public static void TestEqualsObject()
    {
        SByte i = 78;

        object obj1 = (SByte)78;
        Assert.True(i.Equals(obj1));

        object obj3 = (SByte)0;
        Assert.True(!i.Equals(obj3));
    }

    [Fact]
    public static void TestEquals()
    {
        SByte i = 91;

        Assert.True(i.Equals((SByte)91));
        Assert.True(!i.Equals((SByte)0));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        SByte i1 = 123;
        SByte i2 = 65;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        SByte i1 = 63;
        Assert.Equal("63", i1.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        SByte i1 = 63;
        Assert.Equal("63", i1.ToString(numberFormat));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        SByte i1 = 63;
        Assert.Equal("63", i1.ToString("G"));

        SByte i2 = 82;
        Assert.Equal("82", i2.ToString("g"));

        SByte i3 = 46;
        Assert.Equal(string.Format("{0:N}", 46.00), i3.ToString("N"));

        SByte i4 = 0x24;
        Assert.Equal("24", i4.ToString("x"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        SByte i1 = 63;
        Assert.Equal("63", i1.ToString("G", numberFormat));

        SByte i2 = 82;
        Assert.Equal("82", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        numberFormat.NumberDecimalSeparator = ".";
        SByte i3 = 24;
        Assert.Equal("24.00", i3.ToString("N", numberFormat));
    }

    [Fact]
    public static void TestParse()
    {
        Assert.Equal<SByte>(123, SByte.Parse("123"));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyle()
    {
        Assert.Equal<SByte>(0x12, SByte.Parse("12", NumberStyles.HexNumber));
        Assert.Equal<SByte>(10, SByte.Parse("10", NumberStyles.AllowThousands));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal<SByte>(123, SByte.Parse("123", nfi));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyleFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal<SByte>(0x12, SByte.Parse("12", NumberStyles.HexNumber, nfi));

        nfi.CurrencySymbol = "$";
        Assert.Equal<SByte>(100, SByte.Parse("$100", NumberStyles.Currency, nfi));
        //TODO: Negative tests once we get better exception support
    }

    [Fact]
    public static void TestTryParse()
    {
        // Defaults NumberStyles.Integer = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign

        SByte i;
        Assert.True(SByte.TryParse("123", out i));     // Simple
        Assert.Equal<SByte>(123, i);

        Assert.True(SByte.TryParse("-38", out i));    // LeadingSign negative
        Assert.Equal(-38, i);

        Assert.True(SByte.TryParse(" 67 ", out i));   // Leading/Trailing whitespace
        Assert.Equal<SByte>(67, i);

        Assert.False(SByte.TryParse((100).ToString("C0"), out i));  // Currency
        Assert.False(SByte.TryParse((1000).ToString("N0"), out i));  // Thousands
        Assert.False(SByte.TryParse("ab", out i));    // Hex digits
        Assert.False(SByte.TryParse((67.90).ToString("F2"), out i)); // Decimal
        Assert.False(SByte.TryParse("(35)", out i));  // Parentheses
        Assert.False(SByte.TryParse("1E23", out i));   // Exponent
    }

    [Fact]
    public static void TestTryParseNumberStyleFormatProvider()
    {
        SByte i;
        var nfi = new NumberFormatInfo();
        Assert.True(SByte.TryParse("123", NumberStyles.Any, nfi, out i));   // Simple positive
        Assert.Equal<SByte>(123, i);

        Assert.True(SByte.TryParse("12", NumberStyles.HexNumber, nfi, out i));   // Simple Hex
        Assert.Equal<SByte>(0x12, i);

        nfi.CurrencySymbol = "$";
        Assert.True(SByte.TryParse("$100", NumberStyles.Currency, nfi, out i)); // Currency/Thousands postive
        Assert.Equal<SByte>(100, i);

        Assert.False(SByte.TryParse("ab", NumberStyles.None, nfi, out i));       // Hex Number negative
        Assert.True(SByte.TryParse("2b", NumberStyles.HexNumber, nfi, out i));   // Hex Number positive
        Assert.Equal<SByte>(0x2b, i);

        nfi.NumberDecimalSeparator = ".";
        Assert.False(SByte.TryParse("67.90", NumberStyles.Integer, nfi, out i));  // Decimal
        Assert.False(SByte.TryParse(" 67 ", NumberStyles.None, nfi, out i));      // Trailing/Leading whitespace negative

        Assert.True(SByte.TryParse("(35)", NumberStyles.AllowParentheses, nfi, out i)); // Parenthese
        Assert.Equal(-35, i);
    }
}

