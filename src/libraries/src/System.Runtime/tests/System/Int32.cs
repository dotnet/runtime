// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

public static class Int32Tests
{
    [Fact]
    public static void TestCtor_Empty()
    {
        var i = new int();
        Assert.Equal(0, i);
    }

    [Fact]
    public static void TestCtor_Value()
    {
        int i = 41;
        Assert.Equal(41, i);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Assert.Equal(0x7FFFFFFF, int.MaxValue);
    }

    [Fact]
    public static void TestMinValue()
    {
        Assert.Equal(unchecked((int)0x80000000), int.MinValue);
    }

    [Theory]
    [InlineData(234, 234, 0)]
    [InlineData(234, int.MinValue, 1)]
    [InlineData(234, -123, 1)]
    [InlineData(234, 0, 1)]
    [InlineData(234, 123, 1)]
    [InlineData(234, 456, -1)]
    [InlineData(234, int.MaxValue, -1)]
    [InlineData(234, null, 1)]
    public static void TestCompareTo(int i, object value, int expected)
    {
        if (value is int)
        {
            Assert.Equal(expected, Math.Sign(i.CompareTo((int)value)));
        }
        IComparable comparable = i;
        Assert.Equal(expected, Math.Sign(comparable.CompareTo(value)));
    }

    [Fact]
    public static void TestCompareTo_Invalid()
    {
        IComparable comparable = 234;
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("a")); // Obj is not an int
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo((long)234)); // Obj is not an int
    }

    [Theory]
    [InlineData(789, 789, true)]
    [InlineData(789, -789, false)]
    [InlineData(789, 0, false)]
    [InlineData(0, 0, true)]
    [InlineData(789, null, false)]
    [InlineData(789, "789", false)]
    [InlineData(789, (long)789, false)]
    public static void TestEquals(int i1, object obj, bool expected)
    {
        if (obj is int)
        {
            int i2 = (int)obj;
            Assert.Equal(expected, i1.Equals(i2));
            Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
        }
        Assert.Equal(expected, i1.Equals(obj));
    }

    public static IEnumerable<object[]> ToString_TestData()
    {
        NumberFormatInfo emptyFormat = NumberFormatInfo.CurrentInfo;
        yield return new object[] { int.MinValue, "G", emptyFormat, "-2147483648" };
        yield return new object[] { -4567, "G", emptyFormat, "-4567" };
        yield return new object[] { 0, "G", emptyFormat, "0" };
        yield return new object[] { 4567, "G", emptyFormat, "4567" };
        yield return new object[] { int.MaxValue, "G", emptyFormat, "2147483647" };

        yield return new object[] { 0x2468, "x", emptyFormat, "2468" };
        yield return new object[] { 2468, "N", emptyFormat, string.Format("{0:N}", 2468.00) };

        NumberFormatInfo customFormat = new NumberFormatInfo();
        customFormat.NegativeSign = "#";
        customFormat.NumberDecimalSeparator = "~";
        customFormat.NumberGroupSeparator = "*";
        yield return new object[] { -2468, "N", customFormat, "#2*468~00" };
        yield return new object[] { 2468, "N", customFormat, "2*468~00" };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public static void TestToString(int i, string format, IFormatProvider provider, string expected)
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
        int i = 123;
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

        yield return new object[] { "-2147483648", defaultStyle, nullFormat, -2147483648 };
        yield return new object[] { "-123", defaultStyle, nullFormat, -123 };
        yield return new object[] { "0", defaultStyle, nullFormat, 0 };
        yield return new object[] { "123", defaultStyle, nullFormat, 123 };
        yield return new object[] { "  123  ", defaultStyle, nullFormat, 123 };
        yield return new object[] { "2147483647", defaultStyle, nullFormat, 2147483647 };

        yield return new object[] { "123", NumberStyles.HexNumber, nullFormat, 0x123 };
        yield return new object[] { "abc", NumberStyles.HexNumber, nullFormat, 0xabc };
        yield return new object[] { "1000", NumberStyles.AllowThousands, nullFormat, 1000 };
        yield return new object[] { "(123)", NumberStyles.AllowParentheses, nullFormat, -123 }; // Parentheses = negative

        yield return new object[] { "123", defaultStyle, emptyFormat, 123 };

        yield return new object[] { "123", NumberStyles.Any, emptyFormat, 123 };
        yield return new object[] { "12", NumberStyles.HexNumber, emptyFormat, 0x12 };
        yield return new object[] { "$1,000", NumberStyles.Currency, customFormat, 1000 };
    }

    [Theory]
    [MemberData(nameof(Parse_Valid_TestData))]
    public static void TestParse(string value, NumberStyles style, IFormatProvider provider, int expected)
    {
        int result;
        // If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.True(int.TryParse(value, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, int.Parse(value));

            // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (provider != null)
            {
                Assert.Equal(expected, int.Parse(value, provider));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.True(int.TryParse(value, style, provider ?? new NumberFormatInfo(), out result));
        Assert.Equal(expected, result);

        // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (provider == null)
        {
            Assert.Equal(expected, int.Parse(value, style));
        }
        Assert.Equal(expected, int.Parse(value, style, provider ?? new NumberFormatInfo()));
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

        yield return new object[] { "-2147483649", defaultStyle, nullFormat, typeof(OverflowException) }; // > max value
        yield return new object[] { "2147483648", defaultStyle, nullFormat, typeof(OverflowException) }; // < min value
    }

    [Theory]
    [MemberData(nameof(Parse_Invalid_TestData))]
    public static void TestParse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
        int result;
        // If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.False(int.TryParse(value, out result));
            Assert.Equal(default(int), result);

            Assert.Throws(exceptionType, () => int.Parse(value));

            // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (provider != null)
            {
                Assert.Throws(exceptionType, () => int.Parse(value, provider));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.False(int.TryParse(value, style, provider ?? new NumberFormatInfo(), out result));
        Assert.Equal(default(int), result);

        // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (provider == null)
        {
            Assert.Throws(exceptionType, () => int.Parse(value, style));
        }
        Assert.Throws(exceptionType, () => int.Parse(value, style, provider ?? new NumberFormatInfo()));
    }
}
