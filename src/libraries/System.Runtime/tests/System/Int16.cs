// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

public static class Int16Tests
{
    [Fact]
    public static void TestCtor_Empty()
    {
        var i = new short();
        Assert.Equal(0, i);
    }

    [Fact]
    public static void TestCtor_Value()
    {
        short i = 41;
        Assert.Equal(41, i);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Assert.Equal(0x7FFF, short.MaxValue);
    }

    [Fact]
    public static void TestMinValue()
    {
        Assert.Equal(unchecked((short)0x8000), short.MinValue);
    }

    [Theory]
    [InlineData((short)234, (short)234, 0)]
    [InlineData((short)234, short.MinValue, 1)]
    [InlineData((short)234, (short)-123, 1)]
    [InlineData((short)234, (short)0, 1)]
    [InlineData((short)234, (short)123, 1)]
    [InlineData((short)234, (short)456, -1)]
    [InlineData((short)234, short.MaxValue, -1)]
    [InlineData((short)234, null, 1)]
    public static void TestCompareTo(short i, object obj, int expected)
    {
        if (obj is short)
        {
            Assert.Equal(expected, Math.Sign(i.CompareTo((short)obj)));
        }
        IComparable comparable = i;
        Assert.Equal(expected, Math.Sign(comparable.CompareTo(obj)));
    }

    [Fact]
    public static void TestCompareTo_Invalid()
    {
        IComparable comparable = (short)234;
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("a")); // Obj is not a short
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo(234)); // Obj is not a short
    }

    [Theory]
    [InlineData((short)789, (short)789, true)]
    [InlineData((short)789, (short)-789, false)]
    [InlineData((short)789, (short)0, false)]
    [InlineData((short)0, (short)0, true)]
    [InlineData((short)789, null, false)]
    [InlineData((short)789, "789", false)]
    [InlineData((short)789, 789, false)]
    public static void TestEquals(short i1, object obj, bool expected)
    {
        if (obj is short)
        {
            short i2 = (short)obj;
            Assert.Equal(expected, i1.Equals(i2));
            Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
        }
        Assert.Equal(expected, i1.Equals(obj));
    }

    public static IEnumerable<object[]> ToString_TestData()
    {
        NumberFormatInfo emptyFormat = NumberFormatInfo.CurrentInfo;
        yield return new object[] { short.MinValue, "G", emptyFormat, "-32768" };
        yield return new object[] { (short)-4567, "G", emptyFormat, "-4567" };
        yield return new object[] { (short)0, "G", emptyFormat, "0" };
        yield return new object[] { (short)4567, "G", emptyFormat, "4567" };
        yield return new object[] { short.MaxValue, "G", emptyFormat, "32767" };

        yield return new object[] { (short)0x2468, "x", emptyFormat, "2468" };
        yield return new object[] { (short)-0x2468, "x", emptyFormat, "DB98" };

        yield return new object[] { (short)2468, "N", emptyFormat, string.Format("{0:N}", 2468.00) };

        NumberFormatInfo customFormat = new NumberFormatInfo();
        customFormat.NegativeSign = "#";
        customFormat.NumberDecimalSeparator = "~";
        customFormat.NumberGroupSeparator = "*";
        yield return new object[] { (short)-2468, "N", customFormat, "#2*468~00" };
        yield return new object[] { (short)2468, "N", customFormat, "2*468~00" };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public static void TestToString(short i, string format, IFormatProvider provider, string expected)
    {
        // Format is case insensitive
        string upperFormat = format.ToUpperInvariant();
        string lowerFormat = format.ToLowerInvariant();

        string upperExpected = expected.ToUpperInvariant();
        string lowerExpected = expected.ToLowerInvariant();

        bool isDefaultProvider = (provider == null || provider == NumberFormatInfo.CurrentInfo);
        if (string.IsNullOrEmpty(format) || format.ToUpperInvariant() == "G")
        {
            if (isDefaultProvider)
            {
                Assert.Equal(upperExpected, i.ToString());
                Assert.Equal(upperExpected, i.ToString((IFormatProvider)null));
            }
            Assert.Equal(upperExpected, i.ToString(provider));
        }
        if (isDefaultProvider)
        {
            Assert.Equal(upperExpected, i.ToString(upperFormat));
            Assert.Equal(lowerExpected, i.ToString(lowerFormat));
            Assert.Equal(upperExpected, i.ToString(upperFormat, null));
            Assert.Equal(lowerExpected, i.ToString(lowerFormat, null));
        }
        Assert.Equal(upperExpected, i.ToString(upperFormat, provider));
        Assert.Equal(lowerExpected, i.ToString(lowerFormat, provider));
    }

    [Fact]
    public static void TestToString_Invalid()
    {
        short i = 123;
        Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
        Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
    }

    public static IEnumerable<object[]> Parse_Valid_TestData()
    {
        NumberFormatInfo nullFormat = null;
        NumberStyles defaultStyle = NumberStyles.Integer;
        NumberFormatInfo emptyFormat = new NumberFormatInfo();

        NumberFormatInfo customFormat = new NumberFormatInfo();
        customFormat.CurrencySymbol = "$";

        yield return new object[] { "-32768", defaultStyle, nullFormat, (short)-32768 };
        yield return new object[] { "-123", defaultStyle, nullFormat, (short)-123 };
        yield return new object[] { "0", defaultStyle, nullFormat, (short)0 };
        yield return new object[] { "123", defaultStyle, nullFormat, (short)123 };
        yield return new object[] { "  123  ", defaultStyle, nullFormat, (short)123 };
        yield return new object[] { "32767", defaultStyle, nullFormat, (short)32767 };

        yield return new object[] { "123", NumberStyles.HexNumber, nullFormat, (short)0x123 };
        yield return new object[] { "abc", NumberStyles.HexNumber, nullFormat, (short)0xabc };
        yield return new object[] { "1000", NumberStyles.AllowThousands, nullFormat, (short)1000 };
        yield return new object[] { "(123)", NumberStyles.AllowParentheses, nullFormat, (short)-123 }; // Parentheses = negative

        yield return new object[] { "123", defaultStyle, emptyFormat, (short)123 };

        yield return new object[] { "123", NumberStyles.Any, emptyFormat, (short)123 };
        yield return new object[] { "12", NumberStyles.HexNumber, emptyFormat, (short)0x12 };
        yield return new object[] { "$1,000", NumberStyles.Currency, customFormat, (short)1000 };
    }

    [Theory]
    [MemberData(nameof(Parse_Valid_TestData))]
    public static void TestParse(string value, NumberStyles style, IFormatProvider provider, short expected)
    {
        short result;
        // If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.True(short.TryParse(value, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, short.Parse(value));

            // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (provider != null)
            {
                Assert.Equal(expected, short.Parse(value, provider));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.True(short.TryParse(value, style, provider ?? new NumberFormatInfo(), out result));
        Assert.Equal(expected, result);

        // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (provider == null)
        {
            Assert.Equal(expected, short.Parse(value, style));
        }
        Assert.Equal(expected, short.Parse(value, style, provider ?? new NumberFormatInfo()));
    }

    public static IEnumerable<object[]> Parse_Invalid_TestData()
    {
        NumberFormatInfo nullFormat = null;
        NumberStyles defaultStyle = NumberStyles.Integer;

        NumberFormatInfo customFormat = new NumberFormatInfo();
        customFormat.CurrencySymbol = "$";
        customFormat.NumberDecimalSeparator = ".";

        yield return new object[] { null, defaultStyle, nullFormat, typeof(ArgumentNullException) };
        yield return new object[] { "", defaultStyle, nullFormat, typeof(FormatException) };
        yield return new object[] { " ", defaultStyle, nullFormat, typeof(FormatException) };
        yield return new object[] { "Garbage", defaultStyle, nullFormat, typeof(FormatException) };

        yield return new object[] { "abc", defaultStyle, nullFormat, typeof(FormatException) }; // Hex value
        yield return new object[] { "1E23", defaultStyle, nullFormat, typeof(FormatException) }; // Exponent
        yield return new object[] { "(123)", defaultStyle, nullFormat, typeof(FormatException) }; // Parentheses
        yield return new object[] { 1000.ToString("C0"), defaultStyle, nullFormat, typeof(FormatException) }; // Currency
        yield return new object[] { 1000.ToString("N0"), defaultStyle, nullFormat, typeof(FormatException) }; // Thousands
        yield return new object[] { 678.90.ToString("F2"), defaultStyle, nullFormat, typeof(FormatException) }; // Decimal

        yield return new object[] { "abc", NumberStyles.None, nullFormat, typeof(FormatException) }; // Negative hex value
        yield return new object[] { "  123  ", NumberStyles.None, nullFormat, typeof(FormatException) }; // Trailing and leading whitespace

        yield return new object[] { "67.90", defaultStyle, customFormat, typeof(FormatException) }; // Decimal

        yield return new object[] { "-32769", defaultStyle, nullFormat, typeof(OverflowException) }; // < min value
        yield return new object[] { "32768", defaultStyle, nullFormat, typeof(OverflowException) }; // > max value

        yield return new object[] { "2147483648", defaultStyle, nullFormat, typeof(OverflowException) }; // Internally, Parse pretends we are inputting an Int32, so this overflows

        yield return new object[] { "FFFFFFFF", NumberStyles.HexNumber, nullFormat, typeof(OverflowException) }; // Hex number < 0
        yield return new object[] { "10000", NumberStyles.HexNumber, nullFormat, typeof(OverflowException) }; // Hex number > max value
    }

    [Theory]
    [MemberData(nameof(Parse_Invalid_TestData))]
    public static void TestParse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
        short result;
        // If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.False(short.TryParse(value, out result));
            Assert.Equal(default(short), result);

            Assert.Throws(exceptionType, () => short.Parse(value));

            // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (provider != null)
            {
                Assert.Throws(exceptionType, () => short.Parse(value, provider));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.False(short.TryParse(value, style, provider ?? new NumberFormatInfo(), out result));
        Assert.Equal(default(short), result);

        // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (provider == null)
        {
            Assert.Throws(exceptionType, () => short.Parse(value, style));
        }
        Assert.Throws(exceptionType, () => short.Parse(value, style, provider ?? new NumberFormatInfo()));
    }
}
