// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public static class Int64Tests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new long();
            Assert.Equal(0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            long i = 41;
            Assert.Equal(41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0x7FFFFFFFFFFFFFFF, long.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(unchecked((long)0x8000000000000000), long.MinValue);
        }

        [Theory]
        [InlineData((long)234, (long)234, 0)]
        [InlineData((long)234, long.MinValue, 1)]
        [InlineData((long)234, (long)-123, 1)]
        [InlineData((long)234, (long)0, 1)]
        [InlineData((long)234, (long)123, 1)]
        [InlineData((long)234, (long)456, -1)]
        [InlineData((long)234, long.MaxValue, -1)]
        [InlineData((long)-234, (long)-234, 0)]
        [InlineData((long)-234, (long)234, -1)]
        [InlineData((long)-234, (long)-432, 1)]
        [InlineData((long)234, null, 1)]
        public static void CompareTo(long i, object value, long expected)
        {
            if (value is long)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo((long)value)));
            }
            IComparable comparable = i;
            Assert.Equal(expected, Math.Sign(comparable.CompareTo(value)));
        }

        [Fact]
        public static void CompareTo_ObjectNotLong_ThrowsArgumentException()
        {
            IComparable comparable = (long)234;
            AssertExtensions.Throws<ArgumentException>(null, () => comparable.CompareTo("a")); // Obj is not a long
            AssertExtensions.Throws<ArgumentException>(null, () => comparable.CompareTo(234)); // Obj is not a long
        }

        [Theory]
        [InlineData((long)789, (long)789, true)]
        [InlineData((long)789, (long)-789, false)]
        [InlineData((long)789, (long)0, false)]
        [InlineData((long)0, (long)0, true)]
        [InlineData((long)-789, (long)-789, true)]
        [InlineData((long)-789, (long)789, false)]
        [InlineData((long)789, null, false)]
        [InlineData((long)789, "789", false)]
        [InlineData((long)789, 789, false)]
        public static void Equals(long i1, object obj, bool expected)
        {
            if (obj is long)
            {
                long i2 = (long)obj;
                Assert.Equal(expected, i1.Equals(i2));
                Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
            }
            Assert.Equal(expected, i1.Equals(obj));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            NumberFormatInfo emptyFormat = NumberFormatInfo.CurrentInfo;
            yield return new object[] { long.MinValue, "G", emptyFormat, "-9223372036854775808" };
            yield return new object[] { (long)-4567, "G", emptyFormat, "-4567" };
            yield return new object[] { (long)0, "G", emptyFormat, "0" };
            yield return new object[] { (long)4567, "G", emptyFormat, "4567" };
            yield return new object[] { long.MaxValue, "G", emptyFormat, "9223372036854775807" };

            yield return new object[] { (long)0x2468, "x", emptyFormat, "2468" };
            yield return new object[] { (long)2468, "N", emptyFormat, string.Format("{0:N}", 2468.00) };

            NumberFormatInfo customFormat = new NumberFormatInfo();
            customFormat.NegativeSign = "#";
            customFormat.NumberDecimalSeparator = "~";
            customFormat.NumberGroupSeparator = "*";
            yield return new object[] { (long)-2468, "N", customFormat, "#2*468~00" };
            yield return new object[] { (long)2468, "N", customFormat, "2*468~00" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString(long i, string format, IFormatProvider provider, string expected)
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
        public static void ToString_InvalidFormat_ThrowsFormatException()
        {
            long i = 123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Integer;
            NumberFormatInfo emptyFormat = new NumberFormatInfo();

            NumberFormatInfo customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";

            yield return new object[] { "-9223372036854775808", defaultStyle, null, -9223372036854775808 };
            yield return new object[] { "-123", defaultStyle, null, (long)-123 };
            yield return new object[] { "0", defaultStyle, null, (long)0 };
            yield return new object[] { "123", defaultStyle, null, (long)123 };
            yield return new object[] { "+123", defaultStyle, null, (long)123 };
            yield return new object[] { "  123  ", defaultStyle, null, (long)123 };
            yield return new object[] { "9223372036854775807", defaultStyle, null, 9223372036854775807 };

            yield return new object[] { "123", NumberStyles.HexNumber, null, (long)0x123 };
            yield return new object[] { "abc", NumberStyles.HexNumber, null, (long)0xabc };
            yield return new object[] { "ABC", NumberStyles.HexNumber, null, (long)0xabc };
            yield return new object[] { "1000", NumberStyles.AllowThousands, null, (long)1000 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, null, (long)-123 }; // Parentheses = negative

            yield return new object[] { "123", defaultStyle, emptyFormat, (long)123 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, (long)123 };
            yield return new object[] { "12", NumberStyles.HexNumber, emptyFormat, (long)0x12 };
            yield return new object[] { "$1,000", NumberStyles.Currency, customFormat, (long)1000 };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, long expected)
        {
            long result;
            // If no style is specified, use the (String) or (String, IFormatProvider) overload
            if (style == NumberStyles.Integer)
            {
                Assert.True(long.TryParse(value, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, long.Parse(value));

                // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
                if (provider != null)
                {
                    Assert.Equal(expected, long.Parse(value, provider));
                }
            }

            // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
            Assert.True(long.TryParse(value, style, provider ?? new NumberFormatInfo(), out result));
            Assert.Equal(expected, result);

            // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
            if (provider == null)
            {
                Assert.Equal(expected, long.Parse(value, style));
            }
            Assert.Equal(expected, long.Parse(value, style, provider ?? new NumberFormatInfo()));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Integer;

            NumberFormatInfo customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";
            customFormat.NumberDecimalSeparator = ".";

            yield return new object[] { null, defaultStyle, null, typeof(ArgumentNullException) };
            yield return new object[] { "", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { " \t \n \r ", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "Garbage", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "abc", defaultStyle, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "1E23", defaultStyle, null, typeof(FormatException) }; // Exponent
            yield return new object[] { "(123)", defaultStyle, null, typeof(FormatException) }; // Parentheses
            yield return new object[] { 1000.ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency
            yield return new object[] { 1000.ToString("N0"), defaultStyle, null, typeof(FormatException) }; // Thousands
            yield return new object[] { 678.90.ToString("F2"), defaultStyle, null, typeof(FormatException) }; // Decimal
            yield return new object[] { "+-123", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "-+123", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "+abc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "-abc", NumberStyles.HexNumber, null, typeof(FormatException) };

            yield return new object[] { "- 123", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "+ 123", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "abc", NumberStyles.None, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) }; // Trailing and leading whitespace

            yield return new object[] { "67.90", defaultStyle, customFormat, typeof(FormatException) }; // Decimal

            yield return new object[] { "-9223372036854775809", defaultStyle, null, typeof(OverflowException) }; // < min value
            yield return new object[] { "9223372036854775808", defaultStyle, null, typeof(OverflowException) }; // > max value
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            long result;
            // If no style is specified, use the (String) or (String, IFormatProvider) overload
            if (style == NumberStyles.Integer)
            {
                Assert.False(long.TryParse(value, out result));
                Assert.Equal(default(long), result);

                Assert.Throws(exceptionType, () => long.Parse(value));

                // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
                if (provider != null)
                {
                    Assert.Throws(exceptionType, () => long.Parse(value, provider));
                }
            }

            // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
            Assert.False(long.TryParse(value, style, provider ?? new NumberFormatInfo(), out result));
            Assert.Equal(default(long), result);

            // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => long.Parse(value, style));
            }
            Assert.Throws(exceptionType, () => long.Parse(value, style, provider ?? new NumberFormatInfo()));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00))]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style)
        {
            long result = 0;
            Assert.Throws<ArgumentException>(() => long.TryParse("1", style, null, out result));
            Assert.Equal(default(long), result);

            Assert.Throws<ArgumentException>(() => long.Parse("1", style));
            Assert.Throws<ArgumentException>(() => long.Parse("1", style, null));
        }
    }
}
