// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Tests.Common;

using Xunit;

public static class Int32Tests
{
    [Fact]
    public static void TestCtorEmpty()
    {
        int i = new int();
        Assert.Equal(0, i);
    }

    [Fact]
    public static void TestCtorValue()
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
    [InlineData(234, 0)]
    [InlineData(int.MinValue, 1)]
    [InlineData(-123, 1)]
    [InlineData(0, 1)]
    [InlineData(45, 1)]
    [InlineData(123, 1)]
    [InlineData(456, -1)]
    [InlineData(int.MaxValue, -1)]
    public static void TestCompareTo(int value, int expected)
    {
        int i = 234;
        int result = CompareHelper.NormalizeCompare(i.CompareTo(value));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(234, 0)]
    [InlineData(int.MinValue, 1)]
    [InlineData(-123, 1)]
    [InlineData(0, 1)]
    [InlineData(45, 1)]
    [InlineData(123, 1)]
    [InlineData(456, -1)]
    [InlineData(int.MaxValue, -1)]
    public static void TestCompareToObject(object obj, int expected)
    {
        IComparable comparable = 234;
        int i = CompareHelper.NormalizeCompare(comparable.CompareTo(obj));
        Assert.Equal(expected, i);
    }

    [Fact]
    public static void TestCompareToObjectInvalid()
    {
        IComparable comparable = 234;
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("a")); //Obj is not a int
    }

    [Theory]
    [InlineData(789, true)]
    [InlineData(-789, false)]
    [InlineData(0, false)]
    public static void TestEqualsObject(object obj, bool expected)
    {
        int i = 789;
        Assert.Equal(expected, i.Equals(obj));
    }

    [Theory]
    [InlineData(789, true)]
    [InlineData(-789, false)]
    [InlineData(0, false)]
    public static void TestEquals(int i2, bool expected)
    {
        int i = 789;
        Assert.Equal(expected, i.Equals(i2));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        int i1 = 123;
        int i2 = 654;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        int i1 = 6310;
        Assert.Equal("6310", i1.ToString());

        int i2 = -8249;
        Assert.Equal("-8249", i2.ToString());
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        int i1 = 6310;
        Assert.Equal("6310", i1.ToString(numberFormat));

        int i2 = -8249;
        Assert.Equal("-8249", i2.ToString(numberFormat));

        int i3 = -2468;

        // Changing the negative pattern doesn't do anything without also passing in a format string
        numberFormat.NumberNegativePattern = 0;
        Assert.Equal("-2468", i3.ToString(numberFormat));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        int i1 = 6310;
        Assert.Equal("6310", i1.ToString("G"));

        int i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g"));

        int i3 = -2468;
        Assert.Equal(string.Format("{0:N}", -2468.00), i3.ToString("N"));

        int i4 = 0x248;
        Assert.Equal("248", i4.ToString("x"));
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        int i1 = 6310;
        Assert.Equal("6310", i1.ToString("G", numberFormat));

        int i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        int i3 = -2468;
        Assert.Equal("(2*468.00)", i3.ToString("N", numberFormat));
    }

    public static IEnumerable<object[]> ParseValidData()
    {
        NumberFormatInfo defaultFormat = null;
        NumberStyles defaultStyle = NumberStyles.Integer;
        var emptyNfi = new NumberFormatInfo();

        var testNfi = new NumberFormatInfo();
        testNfi.CurrencySymbol = "$";

        yield return new object[] { "-2147483648", defaultStyle, defaultFormat, -2147483648 };
        yield return new object[] { "0", defaultStyle, defaultFormat, 0 };
        yield return new object[] { "123", defaultStyle, defaultFormat, 123 };
        yield return new object[] { "  123  ", defaultStyle, defaultFormat, 123 };
        yield return new object[] { "2147483647", defaultStyle, defaultFormat, 2147483647 };

        yield return new object[] { "123", NumberStyles.HexNumber, defaultFormat, 0x123 };
        yield return new object[] { "abc", NumberStyles.HexNumber, defaultFormat, 0xabc };
        yield return new object[] { "1000", NumberStyles.AllowThousands, defaultFormat, 1000 };
        yield return new object[] { "(123)", NumberStyles.AllowParentheses, defaultFormat, -123 }; // Parentheses = negative

        yield return new object[] { "123", defaultStyle, emptyNfi, 123 };

        yield return new object[] { "123", NumberStyles.Any, emptyNfi, 123 };
        yield return new object[] { "12", NumberStyles.HexNumber, emptyNfi, 0x12 };
        yield return new object[] { "$1,000", NumberStyles.Currency, testNfi, 1000 };
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
        yield return new object[] { 1000.ToString("C0"), defaultStyle, defaultFormat, typeof(FormatException) }; //Currency
        yield return new object[] { 1000.ToString("N0"), defaultStyle, defaultFormat, typeof(FormatException) }; //Thousands
        yield return new object[] { 678.90.ToString("F2"), defaultStyle, defaultFormat, typeof(FormatException) }; //Decimal

        yield return new object[] { "abc", NumberStyles.None, defaultFormat, typeof(FormatException) }; // Negative hex value
        yield return new object[] { "  123  ", NumberStyles.None, defaultFormat, typeof(FormatException) }; // Trailing and leading whitespace

        yield return new object[] { "67.90", defaultStyle, testNfi, typeof(FormatException) }; // Decimal
        
        yield return new object[] { "-2147483649", defaultStyle, defaultFormat, typeof(OverflowException) }; // > max value
        yield return new object[] { "2147483648", defaultStyle, defaultFormat, typeof(OverflowException) }; // < min value
    }

    [Theory, MemberData(nameof(ParseValidData))]
    public static void TestParse(string value, NumberStyles style, NumberFormatInfo nfi, int expected)
    {
        int i;
        //If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.Equal(true, int.TryParse(value, out i));
            Assert.Equal(expected, i);

            Assert.Equal(expected, int.Parse(value));

            //If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (nfi != null)
            {
                Assert.Equal(expected, int.Parse(value, nfi));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.Equal(true, int.TryParse(value, style, nfi ?? new NumberFormatInfo(), out i));
        Assert.Equal(expected, i);

        //If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (nfi == null)
        {
            Assert.Equal(expected, int.Parse(value, style));
        }
        Assert.Equal(expected, int.Parse(value, style, nfi ?? new NumberFormatInfo()));
    }

    [Theory, MemberData(nameof(ParseInvalidData))]
    public static void TestParseInvalid(string value, NumberStyles style, NumberFormatInfo nfi, Type exceptionType)
    {
        int i;
        //If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.Equal(false, int.TryParse(value, out i));
            Assert.Equal(default(int), i);

            Assert.Throws(exceptionType, () => int.Parse(value));

            //If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (nfi != null)
            {
                Assert.Throws(exceptionType, () => int.Parse(value, nfi));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.Equal(false, int.TryParse(value, style, nfi ?? new NumberFormatInfo(), out i));
        Assert.Equal(default(int), i);

        //If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (nfi == null)
        {
            Assert.Throws(exceptionType, () => int.Parse(value, style));
        }
        Assert.Throws(exceptionType, () => int.Parse(value, style, nfi ?? new NumberFormatInfo()));
    }
}
