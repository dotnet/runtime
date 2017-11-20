// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class Int16Tests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new short();
            Assert.Equal(0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            short i = 41;
            Assert.Equal(41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0x7FFF, short.MaxValue);
        }

        [Fact]
        public static void MinValue()
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
        [InlineData((short)-234, (short)-234, 0)]
        [InlineData((short)-234, (short)234, -1)]
        [InlineData((short)-234, (short)-432, 1)]
        [InlineData((short)234, null, 1)]
        public void CompareTo_Other_ReturnsExpected(short i, object value, int expected)
        {
            if (value is short shortValue)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(shortValue)));
            }

            Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData(234)]
        public void CompareTo_ObjectNotShort_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((short)123).CompareTo(value));
        }

        [Theory]
        [InlineData((short)789, (short)789, true)]
        [InlineData((short)789, (short)-789, false)]
        [InlineData((short)789, (short)0, false)]
        [InlineData((short)0, (short)0, true)]
        [InlineData((short)-789, (short)-789, true)]
        [InlineData((short)-789, (short)789, false)]
        [InlineData((short)789, null, false)]
        [InlineData((short)789, "789", false)]
        [InlineData((short)789, 789, false)]
        public static void Equals(short i1, object obj, bool expected)
        {
            if (obj is short)
            {
                short i2 = (short)obj;
                Assert.Equal(expected, i1.Equals(i2));
                Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
            }
            Assert.Equal(expected, i1.Equals(obj));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsInt16()
        {
            Assert.Equal(TypeCode.Int16, ((short)1).GetTypeCode());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                yield return new object[] { short.MinValue, "G", defaultFormat, "-32768" };
                yield return new object[] { (short)-4567, "G", defaultFormat, "-4567" };
                yield return new object[] { (short)0, "G", defaultFormat, "0" };
                yield return new object[] { (short)4567, "G", defaultFormat, "4567" };
                yield return new object[] { short.MaxValue, "G", defaultFormat, "32767" };

                yield return new object[] { (short)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (short)4567, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };

                yield return new object[] { (short)0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { (short)-0x2468, "x", defaultFormat, "db98" };

                yield return new object[] { (short)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
            }

            var customFormat = new NumberFormatInfo()
            {
                NegativeSign = "#",
                NumberDecimalSeparator = "~",
                NumberGroupSeparator = "*",
                PositiveSign = "&",
                NumberDecimalDigits = 2,
                PercentSymbol = "@",
                PercentGroupSeparator = ",",
                PercentDecimalSeparator = ".",
                PercentDecimalDigits = 5
            };
            yield return new object[] { (short)-2468, "N", customFormat, "#2*468~00" };
            yield return new object[] { (short)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (short)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (short)123, "F", customFormat, "123~00" };
            yield return new object[] { (short)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString(short i, string format, IFormatProvider provider, string expected)
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
            short i = 123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Integer;
            NumberFormatInfo emptyFormat = new NumberFormatInfo();

            NumberFormatInfo customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";

            yield return new object[] { "-32768", defaultStyle, null, (short)-32768 };
            yield return new object[] { "-123", defaultStyle, null, (short)-123 };
            yield return new object[] { "0", defaultStyle, null, (short)0 };
            yield return new object[] { "123", defaultStyle, null, (short)123 };
            yield return new object[] { "+123", defaultStyle, null, (short)123 };
            yield return new object[] { "  123  ", defaultStyle, null, (short)123 };
            yield return new object[] { "32767", defaultStyle, null, (short)32767 };

            yield return new object[] { "123", NumberStyles.HexNumber, null, (short)0x123 };
            yield return new object[] { "abc", NumberStyles.HexNumber, null, (short)0xabc };
            yield return new object[] { "ABC", NumberStyles.HexNumber, null, (short)0xabc };
            yield return new object[] { "1000", NumberStyles.AllowThousands, null, (short)1000 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, null, (short)-123 }; // Parentheses = negative

            yield return new object[] { "123", defaultStyle, emptyFormat, (short)123 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, (short)123 };
            yield return new object[] { "12", NumberStyles.HexNumber, emptyFormat, (short)0x12 };
            yield return new object[] { "$1,000", NumberStyles.Currency, customFormat, (short)1000 };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, short expected)
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

            yield return new object[] { "-32769", defaultStyle, null, typeof(OverflowException) }; // < min value
            yield return new object[] { "32768", defaultStyle, null, typeof(OverflowException) }; // > max value

            yield return new object[] { "2147483648", defaultStyle, null, typeof(OverflowException) }; // Internally, Parse pretends we are inputting an Int32, so this overflows

            yield return new object[] { "FFFFFFFF", NumberStyles.HexNumber, null, typeof(OverflowException) }; // Hex number < 0
            yield return new object[] { "10000", NumberStyles.HexNumber, null, typeof(OverflowException) }; // Hex number > max value
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
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

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            short result = 0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => short.TryParse("1", style, null, out result));
            Assert.Equal(default(short), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => short.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => short.Parse("1", style, null));
        }
    }
}
