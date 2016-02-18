// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Tests.Common;

using Xunit;

public static class SByteTests
{
    [Fact]
    public static void TestCtorEmpty()
    {
        sbyte i = new sbyte();
        Assert.Equal(0, i);
    }

    [Fact]
    public static void TestCtorValue()
    {
        sbyte i = 41;
        Assert.Equal(41, i);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Assert.Equal(0x7F, sbyte.MaxValue);
    }

    [Fact]
    public static void TestMinValue()
    {
        Assert.Equal(-0x80, sbyte.MinValue);
    }
    
    [Theory]
    [InlineData((sbyte)114, 0)]
    [InlineData(sbyte.MinValue, 1)]
    [InlineData((sbyte)0, 1)]
    [InlineData((sbyte)45, 1)]
    [InlineData((sbyte)123, -1)]
    [InlineData(sbyte.MaxValue, -1)]
    public static void TestCompareTo(sbyte value, int expected)
    {
        sbyte i = 114;
        int result = CompareHelper.NormalizeCompare(i.CompareTo(value));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData((sbyte)114, 0)]
    [InlineData(sbyte.MinValue, 1)]
    [InlineData((sbyte)(-23), 1)]
    [InlineData((sbyte)0, 1)]
    [InlineData((sbyte)45, 1)]
    [InlineData((sbyte)123, -1)]
    [InlineData(sbyte.MaxValue, -1)]
    public static void TestCompareToObject(object obj, int expected)
    {
        IComparable comparable = (sbyte)114;
        int i = CompareHelper.NormalizeCompare(comparable.CompareTo(obj));
        Assert.Equal(expected, i);
    }

    [Fact]
    public static void TestCompareToObjectInvalid()
    {
        IComparable comparable = (sbyte)114;
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("a")); //Obj is not a sbyte
    }

    [Theory]
    [InlineData((sbyte)78, true)]
    [InlineData((sbyte)(-78), false)]
    [InlineData((sbyte)0, false)]
    public static void TestEqualsObject(object obj, bool expected)
    {
        sbyte i = 78;
        Assert.Equal(expected, i.Equals(obj));
    }

    [Theory]
    [InlineData((sbyte)78, true)]
    [InlineData((sbyte)(-78), false)]
    [InlineData((sbyte)0, false)]
    public static void TestEqualsObject(sbyte i2, bool expected)
    {
        sbyte i = 78;
        Assert.Equal(expected, i.Equals(i2));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        sbyte i1 = 123;
        sbyte i2 = 65;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        sbyte i1 = 63;
        Assert.Equal("63", i1.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        sbyte i1 = 63;
        Assert.Equal("63", i1.ToString(numberFormat));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        sbyte i1 = 63;
        Assert.Equal("63", i1.ToString("G"));

        sbyte i2 = 82;
        Assert.Equal("82", i2.ToString("g"));

        sbyte i3 = 46;
        Assert.Equal(string.Format("{0:N}", 46.00), i3.ToString("N"));

        sbyte i4 = 0x24;
        Assert.Equal("24", i4.ToString("x"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        sbyte i1 = 63;
        Assert.Equal("63", i1.ToString("G", numberFormat));

        sbyte i2 = 82;
        Assert.Equal("82", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        numberFormat.NumberDecimalSeparator = ".";
        sbyte i3 = 24;
        Assert.Equal("24.00", i3.ToString("N", numberFormat));
    }

    public static IEnumerable<object[]> ParseValidData()
    {
        NumberFormatInfo defaultFormat = null;
        NumberStyles defaultStyle = NumberStyles.Integer;
        var emptyNfi = new NumberFormatInfo();

        var testNfi = new NumberFormatInfo();
        testNfi.CurrencySymbol = "$";

        yield return new object[] { "-123", defaultStyle, defaultFormat, (sbyte)-123 };
        yield return new object[] { "0", defaultStyle, defaultFormat, (sbyte)0 };
        yield return new object[] { "123", defaultStyle, defaultFormat, (sbyte)123 };
        yield return new object[] { "  123  ", defaultStyle, defaultFormat, (sbyte)123 };
        yield return new object[] { "127", defaultStyle, defaultFormat, (sbyte)127 };

        yield return new object[] { "12", NumberStyles.HexNumber, defaultFormat, (sbyte)0x12 };
        yield return new object[] { "10", NumberStyles.AllowThousands, defaultFormat, (sbyte)10 };
        yield return new object[] { "(123)", NumberStyles.AllowParentheses, defaultFormat, (sbyte)-123 }; // Parentheses = negative

        yield return new object[] { "123", defaultStyle, emptyNfi, (sbyte)123 };

        yield return new object[] { "123", NumberStyles.Any, emptyNfi, (sbyte)123 };
        yield return new object[] { "12", NumberStyles.HexNumber, emptyNfi, (sbyte)0x12 };
        yield return new object[] { "$100", NumberStyles.Currency, testNfi, (sbyte)100 };
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

        yield return new object[] { "ab", NumberStyles.None, defaultFormat, typeof(FormatException) }; // Hex value
        yield return new object[] { "  123  ", NumberStyles.None, defaultFormat, typeof(FormatException) }; // Trailing and leading whitespace

        yield return new object[] { "67.90", defaultStyle, testNfi, typeof(FormatException) }; // Decimal

        yield return new object[] { "-129", defaultStyle, defaultFormat, typeof(OverflowException) }; // < min value
        yield return new object[] { "128", defaultStyle, defaultFormat, typeof(OverflowException) }; // > max value
    }

    [Theory, MemberData(nameof(ParseValidData))]
    public static void TestParse(string value, NumberStyles style, NumberFormatInfo nfi, sbyte expected)
    {
        sbyte i;
        //If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.Equal(true, sbyte.TryParse(value, out i));
            Assert.Equal(expected, i);

            Assert.Equal(expected, sbyte.Parse(value));

            //If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (nfi != null)
            {
                Assert.Equal(expected, sbyte.Parse(value, nfi));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.Equal(true, sbyte.TryParse(value, style, nfi ?? new NumberFormatInfo(), out i));
        Assert.Equal(expected, i);

        //If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (nfi == null)
        {
            Assert.Equal(expected, sbyte.Parse(value, style));
        }
        Assert.Equal(expected, sbyte.Parse(value, style, nfi ?? new NumberFormatInfo()));
    }

    [Theory, MemberData(nameof(ParseInvalidData))]
    public static void TestParseInvalid(string value, NumberStyles style, NumberFormatInfo nfi, Type exceptionType)
    {
        sbyte i;
        //If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.Equal(false, sbyte.TryParse(value, out i));
            Assert.Equal(default(sbyte), i);

            Assert.Throws(exceptionType, () => sbyte.Parse(value));

            //If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (nfi != null)
            {
                Assert.Throws(exceptionType, () => sbyte.Parse(value, nfi));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.Equal(false, sbyte.TryParse(value, style, nfi ?? new NumberFormatInfo(), out i));
        Assert.Equal(default(sbyte), i);

        //If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (nfi == null)
        {
            Assert.Throws(exceptionType, () => sbyte.Parse(value, style));
        }
        Assert.Throws(exceptionType, () => sbyte.Parse(value, style, nfi ?? new NumberFormatInfo()));
    }
}
