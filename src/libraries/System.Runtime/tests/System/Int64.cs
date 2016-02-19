// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Tests.Common;

using Xunit;

public static class Int64Tests
{
    [Fact]
    public static void TestCtorEmpty()
    {
        long i = new long();
        Assert.Equal(0, i);
    }

    [Fact]
    public static void TestCtorValue()
    {
        long i = 41;
        Assert.Equal(41, i);
    }

    [Fact]
    public static void TestMaxValue()
    {
        Assert.Equal(0x7FFFFFFFFFFFFFFF, long.MaxValue);
    }

    [Fact]
    public static void TestMinValue()
    {
        Assert.Equal(unchecked((long)0x8000000000000000), long.MinValue);
    }

    [Theory]
    [InlineData((long)234, 0)]
    [InlineData(long.MinValue, 1)]
    [InlineData((long)(-123), 1)]
    [InlineData((long)0, 1)]
    [InlineData((long)45, 1)]
    [InlineData((long)123, 1)]
    [InlineData((long)456, -1)]
    [InlineData(long.MaxValue, -1)]
    public static void TestCompareTo(long value, long expected)
    {
        long i = 234;
        long result = CompareHelper.NormalizeCompare(i.CompareTo(value));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData((long)234, 0)]
    [InlineData(long.MinValue, 1)]
    [InlineData((long)(-123), 1)]
    [InlineData((long)0, 1)]
    [InlineData((long)45, 1)]
    [InlineData((long)123, 1)]
    [InlineData((long)456, -1)]
    [InlineData(long.MaxValue, -1)]
    public static void TestCompareToObject(object obj, long expected)
    {
        IComparable comparable = (long)234;
        long i = CompareHelper.NormalizeCompare(comparable.CompareTo(obj));
        Assert.Equal(expected, i);
    }

    [Fact]
    public static void TestCompareToObjectInvalid()
    {
        IComparable comparable = (long)234;
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("a")); //Obj is not a long
    }

    [Theory]
    [InlineData((long)789, true)]
    [InlineData((long)(-789), false)]
    [InlineData((long)0, false)]
    public static void TestEqualsObject(object obj, bool expected)
    {
        long i = 789;
        Assert.Equal(expected, i.Equals(obj));
    }

    [Theory]
    [InlineData((long)789, true)]
    [InlineData((long)(-789), false)]
    [InlineData((long)0, false)]
    public static void TestEquals(long i2, bool expected)
    {
        long i = 789;
        Assert.Equal(expected, i.Equals(i2));
    }

    [Fact]
    public static void TestGetHashCode()
    {
        long i1 = 123;
        long i2 = 654;

        Assert.NotEqual(0, i1.GetHashCode());
        Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
    }

    [Fact]
    public static void TestToString()
    {
        long i1 = 6310;
        Assert.Equal("6310", i1.ToString());

        long i2 = -8249;
        Assert.Equal("-8249", i2.ToString());

        Assert.Equal(long.MaxValue.ToString(), "9223372036854775807");
        Assert.Equal(long.MinValue.ToString(), "-9223372036854775808");
    }

    [Fact]
    public static void TestToStringFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        long i1 = 6310;
        Assert.Equal("6310", i1.ToString(numberFormat));

        long i2 = -8249;
        Assert.Equal("-8249", i2.ToString(numberFormat));

        long i3 = -2468;

        // Changing the negative pattern doesn't do anything without also passing in a format string
        numberFormat.NumberNegativePattern = 0;
        Assert.Equal("-2468", i3.ToString(numberFormat));
    }

    [Fact]
    public static void TestToStringFormat()
    {
        long i1 = 6310;
        Assert.Equal("6310", i1.ToString("G"));

        long i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g"));

        long i3 = -2468;
        Assert.Equal(string.Format("{0:N}", -2468.00), i3.ToString("N"));

        long i4 = 0x248;
        Assert.Equal("248", i4.ToString("x"));

        Assert.Equal(long.MinValue.ToString("X"), "8000000000000000");
        Assert.Equal(long.MaxValue.ToString("X"), "7FFFFFFFFFFFFFFF");
    }

    [Fact]
    public static void TestToStringFormatFormatProvider()
    {
        var numberFormat = new NumberFormatInfo();

        long i1 = 6310;
        Assert.Equal("6310", i1.ToString("G", numberFormat));

        long i2 = -8249;
        Assert.Equal("-8249", i2.ToString("g", numberFormat));

        numberFormat.NegativeSign = "xx"; // setting it to trash to make sure it doesn't show up
        numberFormat.NumberGroupSeparator = "*";
        numberFormat.NumberNegativePattern = 0;
        long i3 = -2468;
        Assert.Equal("(2*468.00)", i3.ToString("N", numberFormat));
    }

    public static IEnumerable<object[]> ParseValidData()
    {
        NumberFormatInfo defaultFormat = null;
        NumberStyles defaultStyle = NumberStyles.Integer;
        var emptyNfi = new NumberFormatInfo();

        var testNfi = new NumberFormatInfo();
        testNfi.CurrencySymbol = "$";

        yield return new object[] { "-9223372036854775808", defaultStyle, defaultFormat, -9223372036854775808 };
        yield return new object[] { "-123", defaultStyle, defaultFormat, (long)-123 };
        yield return new object[] { "0", defaultStyle, defaultFormat, (long)0 };
        yield return new object[] { "123", defaultStyle, defaultFormat, (long)123 };
        yield return new object[] { "  123  ", defaultStyle, defaultFormat, (long)123 };
        yield return new object[] { "9223372036854775807", defaultStyle, defaultFormat, 9223372036854775807 };

        yield return new object[] { "123", NumberStyles.HexNumber, defaultFormat, (long)0x123 };
        yield return new object[] { "abc", NumberStyles.HexNumber, defaultFormat, (long)0xabc };
        yield return new object[] { "1000", NumberStyles.AllowThousands, defaultFormat, (long)1000 };
        yield return new object[] { "(123)", NumberStyles.AllowParentheses, defaultFormat, (long)-123 }; // Parentheses = negative

        yield return new object[] { "123", defaultStyle, emptyNfi, (long)123 };

        yield return new object[] { "123", NumberStyles.Any, emptyNfi, (long)123 };
        yield return new object[] { "12", NumberStyles.HexNumber, emptyNfi, (long)0x12 };
        yield return new object[] { "$1,000", NumberStyles.Currency, testNfi, (long)1000 };
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
        
        yield return new object[] { "-9223372036854775809", defaultStyle, defaultFormat, typeof(OverflowException) }; // < min value
        yield return new object[] { "9223372036854775808", defaultStyle, defaultFormat, typeof(OverflowException) }; // > max value
    }

    [Theory, MemberData(nameof(ParseValidData))]
    public static void TestParse(string value, NumberStyles style, NumberFormatInfo nfi, long expected)
    {
        long i;
        //If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.Equal(true, long.TryParse(value, out i));
            Assert.Equal(expected, i);

            Assert.Equal(expected, long.Parse(value));

            //If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (nfi != null)
            {
                Assert.Equal(expected, long.Parse(value, nfi));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.Equal(true, long.TryParse(value, style, nfi ?? new NumberFormatInfo(), out i));
        Assert.Equal(expected, i);

        //If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (nfi == null)
        {
            Assert.Equal(expected, long.Parse(value, style));
        }
        Assert.Equal(expected, long.Parse(value, style, nfi ?? new NumberFormatInfo()));
    }

    [Theory, MemberData(nameof(ParseInvalidData))]
    public static void TestParseInvalid(string value, NumberStyles style, NumberFormatInfo nfi, Type exceptionType)
    {
        long i;
        //If no style is specified, use the (String) or (String, IFormatProvider) overload
        if (style == NumberStyles.Integer)
        {
            Assert.Equal(false, long.TryParse(value, out i));
            Assert.Equal(default(long), i);

            Assert.Throws(exceptionType, () => long.Parse(value));

            //If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
            if (nfi != null)
            {
                Assert.Throws(exceptionType, () => long.Parse(value, nfi));
            }
        }

        // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
        Assert.Equal(false, long.TryParse(value, style, nfi ?? new NumberFormatInfo(), out i));
        Assert.Equal(default(long), i);

        //If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
        if (nfi == null)
        {
            Assert.Throws(exceptionType, () => long.Parse(value, style));
        }
        Assert.Throws(exceptionType, () => long.Parse(value, style, nfi ?? new NumberFormatInfo()));
    }
}
