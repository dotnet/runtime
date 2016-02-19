// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Tests.Common;

using Xunit;

public static class UInt64Tests
{
    [Fact]
    public static void TestCtorEmpty()
    {
        ulong i = new ulong();
        Assert.Equal((ulong)0, i);
    }

    [Fact]
    public static void TestCtorValue()
    {
        ulong i = 41;
        Assert.Equal((ulong)41, i);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Assert.Equal(0xFFFFFFFFFFFFFFFF, ulong.MaxValue);
    }

    [Fact]
    public static void TestMinValue()
    {
        Assert.Equal((ulong)0, ulong.MinValue);
    }

    [Theory]
    [InlineData((ulong)234, 0)]
    [InlineData(ulong.MinValue, 1)]
    [InlineData((ulong)0, 1)]
    [InlineData((ulong)45, 1)]
    [InlineData((ulong)123, 1)]
    [InlineData((ulong)456, -1)]
    [InlineData(ulong.MaxValue, -1)]
    public static void TestCompareTo(ulong value, int expected)
    {
        ulong i = 234;
        int result = CompareHelper.NormalizeCompare(i.CompareTo(value));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData((ulong)234, 0)]
    [InlineData(ulong.MinValue, 1)]
    [InlineData((ulong)0, 1)]
    [InlineData((ulong)45, 1)]
    [InlineData((ulong)123, 1)]
    [InlineData((ulong)456, -1)]
    [InlineData(ulong.MaxValue, -1)]
    public static void TestCompareToObject(object obj, int expected)
    {
        IComparable comparable = (ulong)234;
        int i = CompareHelper.NormalizeCompare(comparable.CompareTo(obj));
        Assert.Equal(expected, i);
    }

    [Fact]
    public static void TestCompareToObjectInvalid()
    {
        IComparable comparable = (ulong)234;
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("a")); //Obj is not a byte
    }

    [Theory]
    [InlineData((ulong)789, true)]
    [InlineData((ulong)0, false)]
    public static void TestEqualsObject(object obj, bool expected)
    {
        ulong i = 789;
        Assert.Equal(expected, i.Equals(obj));
    }

    [Theory]
    [InlineData((ulong)789, true)]
    [InlineData((ulong)0, false)]
    public static void TestEquals(ulong i2, bool expected)
    {
        ulong i = 789;
        Assert.Equal(expected, i.Equals(i2));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        ulong i1 = 123;
        ulong i2 = 654;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        ulong i1 = 6310;
        Assert.Equal("6310", i1.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        ulong i1 = 6310;
        Assert.Equal("6310", i1.ToString(numberFormat));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        ulong i1 = 6310;
        Assert.Equal("6310", i1.ToString("G"));

        ulong i2 = 8249;
        Assert.Equal("8249", i2.ToString("g"));

        ulong i3 = 2468;
        Assert.Equal(string.Format("{0:N}", 2468.00), i3.ToString("N"));

        ulong i4 = 0x248;
        Assert.Equal("248", i4.ToString("x"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        ulong i1 = 6310;
        Assert.Equal("6310", i1.ToString("G", numberFormat));

        ulong i2 = 8249;
        Assert.Equal("8249", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        ulong i3 = 2468;
        Assert.Equal("2*468.00", i3.ToString("N", numberFormat));
    }


    public static IEnumerable<object[]> ParseValidData()
    {
        NumberFormatInfo defaultFormat = null;
        NumberStyles defaultStyle = NumberStyles.Integer;
        var emptyNfi = new NumberFormatInfo();

        var testNfi = new NumberFormatInfo();
        testNfi.CurrencySymbol = "$";
        
        yield return new object[] { "0", defaultStyle, defaultFormat, (ulong)0 };
        yield return new object[] { "123", defaultStyle, defaultFormat, (ulong)123 };
        yield return new object[] { "  123  ", defaultStyle, defaultFormat, (ulong)123 };
        yield return new object[] { "18446744073709551615", defaultStyle, defaultFormat, (ulong)18446744073709551615 };

        yield return new object[] { "12", NumberStyles.HexNumber, defaultFormat, (ulong)0x12 };
        yield return new object[] { "1000", NumberStyles.AllowThousands, defaultFormat, (ulong)1000 };

        yield return new object[] { "123", defaultStyle, emptyNfi, (ulong)123 };

        yield return new object[] { "123", NumberStyles.Any, emptyNfi, (ulong)123 };
        yield return new object[] { "12", NumberStyles.HexNumber, emptyNfi, (ulong)0x12 };
        yield return new object[] { "abc", NumberStyles.HexNumber, emptyNfi, (ulong)0xabc };
        yield return new object[] { "$1,000", NumberStyles.Currency, testNfi, (ulong)1000 };
    }

    public static IEnumerable<object[]> ParseInvalidData()
    {
        NumberFormatInfo defaultFormat = null;
        NumberStyles defaultStyle = NumberStyles.Integer;
        var emptyNfi = new NumberFormatInfo();

        var testNfi = new NumberFormatInfo();
        testNfi.CurrencySymbol = "$";
        testNfi.NumberDecimalSeparator = ".";

        yield return new object[] { null, defaultStyle, defaultFormat, typeof(ArgumentNullException) };
        yield return new object[] { "", defaultStyle, defaultFormat, typeof(FormatException) };
        yield return new object[] { " ", defaultStyle, defaultFormat, typeof(FormatException) };
        yield return new object[] { "Garbage", defaultStyle, defaultFormat, typeof(FormatException) };

        yield return new object[] { "abc", defaultStyle, defaultFormat, typeof(FormatException) }; // Hex value
        yield return new object[] { "1E23", defaultStyle, defaultFormat, typeof(FormatException) }; // Exponent
        yield return new object[] { "(123)", defaultStyle, defaultFormat, typeof(FormatException) }; // Parentheses
        yield return new object[] { 100.ToString("C0"), defaultStyle, defaultFormat, typeof(FormatException) }; //Currency
        yield return new object[] { 1000.ToString("N0"), defaultStyle, defaultFormat, typeof(FormatException) }; //Thousands
        yield return new object[] { 678.90.ToString("F2"), defaultStyle, defaultFormat, typeof(FormatException) }; //Decimal

        yield return new object[] { "abc", NumberStyles.None, defaultFormat, typeof(FormatException) }; // Negative hex value
        yield return new object[] { "  123  ", NumberStyles.None, defaultFormat, typeof(FormatException) }; // Trailing and leading whitespace

        yield return new object[] { "678.90", defaultStyle, testNfi, typeof(FormatException) }; // Decimal

        yield return new object[] { "-1", defaultStyle, defaultFormat, typeof(OverflowException) }; // < min value
        yield return new object[] { "18446744073709551616", defaultStyle, defaultFormat, typeof(OverflowException) }; // > max value
        yield return new object[] { "(123)", NumberStyles.AllowParentheses, defaultFormat, typeof(OverflowException) }; // Parentheses = negative
    }

    [Theory, MemberData(nameof(ParseValidData))]
    public static void TestParse(string value, NumberStyles style, NumberFormatInfo nfi, ulong expected)
    {
        ulong i;
        //If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.Equal(true, ulong.TryParse(value, out i));
            Assert.Equal(expected, i);

            Assert.Equal(expected, ulong.Parse(value));

            //If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (nfi != null)
            {
                Assert.Equal(expected, ulong.Parse(value, nfi));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.Equal(true, ulong.TryParse(value, style, nfi ?? new NumberFormatInfo(), out i));
        Assert.Equal(expected, i);

        //If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (nfi == null)
        {
            Assert.Equal(expected, ulong.Parse(value, style));
        }
        Assert.Equal(expected, ulong.Parse(value, style, nfi ?? new NumberFormatInfo()));
    }

    [Theory, MemberData(nameof(ParseInvalidData))]
    public static void TestParseInvalid(string value, NumberStyles style, NumberFormatInfo nfi, Type exceptionType)
    {
        ulong i;
        //If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.Equal(false, ulong.TryParse(value, out i));
            Assert.Equal(default(ulong), i);

            Assert.Throws(exceptionType, () => ulong.Parse(value));

            //If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (nfi != null)
            {
                Assert.Throws(exceptionType, () => ulong.Parse(value, nfi));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.Equal(false, ulong.TryParse(value, style, nfi ?? new NumberFormatInfo(), out i));
        Assert.Equal(default(ulong), i);

        //If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (nfi == null)
        {
            Assert.Throws(exceptionType, () => ulong.Parse(value, style));
        }
        Assert.Throws(exceptionType, () => ulong.Parse(value, style, nfi ?? new NumberFormatInfo()));
    }
}
