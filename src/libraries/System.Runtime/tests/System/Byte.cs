// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Tests.Common;

using Xunit;

public static class ByteTests
{
    [Fact]
    public static void TestCtorEmpty()
    {
        byte i = new byte();
        Assert.Equal(0, i);
    }
    
    [Fact]
    public static void TestCtorValue()
    {
        byte i = 41;
        Assert.Equal(41, i);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Assert.Equal(0xFF, byte.MaxValue);
    }

    [Fact]
    public static void TestMinValue()
    {
        Assert.Equal(0, byte.MinValue);
    }

    [Theory]
    [InlineData((byte)234, 0)]
    [InlineData(byte.MinValue, 1)]
    [InlineData((byte)0, 1)]
    [InlineData((byte)45, 1)]
    [InlineData((byte)123, 1)]
    [InlineData((byte)235, -1)]
    [InlineData(byte.MaxValue, -1)]
    public static void TestCompareTo(byte value, int expected)
    {
        byte i = 234;
        int result = CompareHelper.NormalizeCompare(i.CompareTo(value));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData((byte)234, 0)]
    [InlineData(byte.MinValue, 1)]
    [InlineData((byte)0, 1)]
    [InlineData((byte)45, 1)]
    [InlineData((byte)123, 1)]
    [InlineData((byte)235, -1)]
    [InlineData(byte.MaxValue, -1)]
    public static void TestCompareToObject(object obj, int expected)
    {
        IComparable comparable = (byte)234;
        int i = CompareHelper.NormalizeCompare(comparable.CompareTo(obj));
        Assert.Equal(expected, i);
    }

    [Fact]
    public static void TestCompareToObjectInvalid()
    {
        IComparable comparable = (byte)234;
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("a")); //Obj is not a byte
    }

    [Theory]
    [InlineData((byte)78, true)]
    [InlineData((byte)0, false)]
    public static void TestEqualsObject(object obj, bool expected)
    {
        byte i = 78;
        Assert.Equal(expected, i.Equals(obj));
    }

    [Theory]
    [InlineData((byte)78, true)]
    [InlineData((byte)0, false)]
    public static void TestEquals(byte i2, bool expected)
    {
        byte i = 78;
        Assert.Equal(expected, i.Equals(i2));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        byte i1 = 123;
        byte i2 = 65;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        byte i1 = 63;
        Assert.Equal("63", i1.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        byte i1 = 63;
        Assert.Equal("63", i1.ToString(numberFormat));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        byte i1 = 63;
        Assert.Equal("63", i1.ToString("G"));

        byte i2 = 82;
        Assert.Equal("82", i2.ToString("g"));

        byte i3 = 246;
        Assert.Equal(string.Format("{0:N}", 246.00), i3.ToString("N"));

        byte i4 = 0x24;
        Assert.Equal("24", i4.ToString("x"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        byte i1 = 63;
        Assert.Equal("63", i1.ToString("G", numberFormat));

        byte i2 = 82;
        Assert.Equal("82", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        numberFormat.NumberDecimalSeparator = ".";
        byte i3 = 24;
        Assert.Equal("24.00", i3.ToString("N", numberFormat));
    }

    public static IEnumerable<object[]> ParseValidData()
    {
        NumberFormatInfo defaultFormat = null;
        NumberStyles defaultStyle = NumberStyles.Integer;
        var emptyNfi = new NumberFormatInfo();

        var testNfi = new NumberFormatInfo();
        testNfi.CurrencySymbol = "$";

        yield return new object[] { "0", defaultStyle, defaultFormat, (byte)0 };
        yield return new object[] { "123", defaultStyle, defaultFormat, (byte)123 };
        yield return new object[] { "  123  ", defaultStyle, defaultFormat, (byte)123 };
        yield return new object[] { "255", defaultStyle, defaultFormat, (byte)255 };

        yield return new object[] { "12", NumberStyles.HexNumber, defaultFormat, (byte)0x12 };
        yield return new object[] { "10", NumberStyles.AllowThousands, defaultFormat, (byte)10 };

        yield return new object[] { "123", defaultStyle, emptyNfi, (byte)123 };

        yield return new object[] { "123", NumberStyles.Any, emptyNfi, (byte)123 };
        yield return new object[] { "12", NumberStyles.HexNumber, emptyNfi, (byte)0x12 };
        yield return new object[] { "ab", NumberStyles.HexNumber, emptyNfi, (byte)0xab };
        yield return new object[] { "$100", NumberStyles.Currency, testNfi, (byte)100 };
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

        yield return new object[] { "ab", defaultStyle, defaultFormat, typeof(FormatException) }; // Hex value
        yield return new object[] { "1E23", defaultStyle, defaultFormat, typeof(FormatException) }; // Exponent
        yield return new object[] { "(123)", defaultStyle, defaultFormat, typeof(FormatException) }; // Parentheses
        yield return new object[] { 100.ToString("C0"), defaultStyle, defaultFormat, typeof(FormatException) }; //Currency
        yield return new object[] { 1000.ToString("N0"), defaultStyle, defaultFormat, typeof(FormatException) }; //Thousands
        yield return new object[] { 67.90.ToString("F2"), defaultStyle, defaultFormat, typeof(FormatException) }; //Decimal

        yield return new object[] { "ab", NumberStyles.None, defaultFormat, typeof(FormatException) }; // Negative hex value
        yield return new object[] { "  123  ", NumberStyles.None, defaultFormat, typeof(FormatException) }; // Trailing and leading whitespace

        yield return new object[] { "67.90", defaultStyle, testNfi, typeof(FormatException) }; // Decimal

        yield return new object[] { "-1", defaultStyle, defaultFormat, typeof(OverflowException) }; // < min value
        yield return new object[] { "256", defaultStyle, defaultFormat, typeof(OverflowException) }; // > max value
        yield return new object[] { "(123)", NumberStyles.AllowParentheses, defaultFormat, typeof(OverflowException) }; // Parentheses = negative
    }

    [Theory, MemberData(nameof(ParseValidData))]
    public static void TestParse(string value, NumberStyles style, NumberFormatInfo nfi, byte expected)
    {
        byte i;
        //If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.Equal(true, byte.TryParse(value, out i));
            Assert.Equal(expected, i);

            Assert.Equal(expected, byte.Parse(value));
            
            //If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (nfi != null)
            {
                Assert.Equal(expected, byte.Parse(value, nfi));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.Equal(true, byte.TryParse(value, style, nfi ?? new NumberFormatInfo(), out i));
        Assert.Equal(expected, i);

        //If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (nfi == null)
        {
            Assert.Equal(expected, byte.Parse(value, style));
        }
        Assert.Equal(expected, byte.Parse(value, style, nfi ?? new NumberFormatInfo()));
    }

    [Theory, MemberData(nameof(ParseInvalidData))]
    public static void TestParseInvalid(string value, NumberStyles style, NumberFormatInfo nfi, Type exceptionType)
    {
        byte i;
        //If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.Equal(false, byte.TryParse(value, out i));
            Assert.Equal(default(byte), i);
            
            Assert.Throws(exceptionType, () => byte.Parse(value));

            //If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (nfi != null)
            {
                Assert.Throws(exceptionType, () => byte.Parse(value, nfi));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.Equal(false, byte.TryParse(value, style, nfi ?? new NumberFormatInfo(), out i));
        Assert.Equal(default(byte), i);

        //If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (nfi == null)
        {
            Assert.Throws(exceptionType, () => byte.Parse(value, style));
        }
        Assert.Throws(exceptionType, () => byte.Parse(value, style, nfi ?? new NumberFormatInfo()));
    }
}
