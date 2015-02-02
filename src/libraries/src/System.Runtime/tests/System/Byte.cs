// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class ByteTests
{
    [Fact]
    public static void TestCtor()
    {
        Byte i = new Byte();
        Assert.True(i == 0);

        i = 41;
        Assert.True(i == 41);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Byte max = Byte.MaxValue;

        Assert.True(max == (Byte)0xFF);
    }

    [Fact]
    public static void TestMinValue()
    {
        Byte min = Byte.MinValue;

        Assert.True(min == 0);
    }

    [Fact]
    public static void TestCompareToObject()
    {
        Byte i = 234;
        IComparable comparable = i;

        Assert.Equal(1, comparable.CompareTo(null));
        Assert.Equal(0, comparable.CompareTo((Byte)234));

        Assert.True(comparable.CompareTo(Byte.MinValue) > 0);
        Assert.True(comparable.CompareTo((Byte)0) > 0);
        Assert.True(comparable.CompareTo((Byte)(23)) > 0);
        Assert.True(comparable.CompareTo((Byte)123) > 0);
        Assert.True(comparable.CompareTo((Byte)45) > 0);
        Assert.True(comparable.CompareTo(Byte.MaxValue) < 0);

        Assert.Throws<ArgumentException>(() => comparable.CompareTo("a"));
    }

    [Fact]
    public static void TestCompareTo()
    {
        Byte i = 234;

        Assert.Equal(0, i.CompareTo((Byte)234));

        Assert.True(i.CompareTo(Byte.MinValue) > 0);
        Assert.True(i.CompareTo((Byte)0) > 0);
        Assert.True(i.CompareTo((Byte)123) > 0);
        Assert.True(i.CompareTo((Byte)45) > 0);
        Assert.True(i.CompareTo(Byte.MaxValue) < 0);
    }

    [Fact]
    public static void TestEqualsObject()
    {
        Byte i = 78;

        object obj1 = (Byte)78;
        Assert.True(i.Equals(obj1));

        object obj3 = (Byte)0;
        Assert.True(!i.Equals(obj3));
    }

    [Fact]
    public static void TestEquals()
    {
        Byte i = 91;

        Assert.True(i.Equals((Byte)91));
        Assert.True(!i.Equals((Byte)0));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        Byte i1 = 123;
        Byte i2 = 65;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        Byte i1 = 63;
        Assert.Equal("63", i1.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        Byte i1 = 63;
        Assert.Equal("63", i1.ToString(numberFormat));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        Byte i1 = 63;
        Assert.Equal("63", i1.ToString("G"));

        Byte i2 = 82;
        Assert.Equal("82", i2.ToString("g"));

        Byte i3 = 246;
        Assert.Equal("246.00", i3.ToString("N"));

        Byte i4 = 0x24;
        Assert.Equal("24", i4.ToString("x"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new System.Globalization.NumberFormatInfo();

        Byte i1 = 63;
        Assert.Equal("63", i1.ToString("G", numberFormat));

        Byte i2 = 82;
        Assert.Equal("82", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        Byte i3 = 24;
        Assert.Equal("24.00", i3.ToString("N", numberFormat));
    }

    [Fact]
    public static void TestParse()
    {
        Assert.Equal<Byte>(123, Byte.Parse("123"));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyle()
    {
        Assert.Equal<Byte>(0x12, Byte.Parse("12", NumberStyles.HexNumber));
        Assert.Equal<Byte>(10, Byte.Parse("10", NumberStyles.AllowThousands));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal<Byte>(123, Byte.Parse("123", nfi));
        //TODO: Negative tests once we get better exceptions
    }

    [Fact]
    public static void TestParseNumberStyleFormatProvider()
    {
        var nfi = new NumberFormatInfo();
        Assert.Equal<Byte>(0x12, Byte.Parse("12", NumberStyles.HexNumber, nfi));

        nfi.CurrencySymbol = "$";
        Assert.Equal<Byte>(100, Byte.Parse("$100", NumberStyles.Currency, nfi));
        //TODO: Negative tests once we get better exception support
    }

    [Fact]
    public static void TestTryParse()
    {
        // Defaults NumberStyles.Integer = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign

        Byte i;
        Assert.True(Byte.TryParse("123", out i));     // Simple
        Assert.Equal<Byte>(123, i);

        Assert.False(Byte.TryParse("-38", out i));    // LeadingSign negative

        Assert.True(Byte.TryParse(" 67 ", out i));   // Leading/Trailing whitespace
        Assert.Equal<Byte>(67, i);

        Assert.False(Byte.TryParse("$100", out i));  // Currency
        Assert.False(Byte.TryParse("1,000", out i));  // Thousands
        Assert.False(Byte.TryParse("ab", out i));    // Hex digits
        Assert.False(Byte.TryParse("67.90", out i)); // Decimal
        Assert.False(Byte.TryParse("(135)", out i));  // Parentheses
        Assert.False(Byte.TryParse("1E23", out i));   // Exponent
    }

    [Fact]
    public static void TestTryParseNumberStyleFormatProvider()
    {
        Byte i;
        var nfi = new NumberFormatInfo();
        Assert.True(Byte.TryParse("123", NumberStyles.Any, nfi, out i));   // Simple positive
        Assert.Equal<Byte>(123, i);

        Assert.True(Byte.TryParse("12", NumberStyles.HexNumber, nfi, out i));   // Simple Hex
        Assert.Equal<Byte>(0x12, i);

        nfi.CurrencySymbol = "$";
        Assert.True(Byte.TryParse("$100", NumberStyles.Currency, nfi, out i)); // Currency/Thousands postive
        Assert.Equal<Byte>(100, i);

        Assert.False(Byte.TryParse("ab", NumberStyles.None, nfi, out i));       // Hex Number negative
        Assert.True(Byte.TryParse("ab", NumberStyles.HexNumber, nfi, out i));   // Hex Number positive
        Assert.Equal<Byte>(0xab, i);

        Assert.False(Byte.TryParse("67.90", NumberStyles.Integer, nfi, out i));  // Decimal
        Assert.False(Byte.TryParse(" 67 ", NumberStyles.None, nfi, out i));      // Trailing/Leading whitespace negative

        Assert.False(Byte.TryParse("(135)", NumberStyles.AllowParentheses, nfi, out i)); // Parenthese negative
    }
}

