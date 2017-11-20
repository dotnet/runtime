// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class SByteTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new sbyte();
            Assert.Equal(0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            sbyte i = 41;
            Assert.Equal(41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0x7F, sbyte.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(-0x80, sbyte.MinValue);
        }

        [Theory]
        [InlineData((sbyte)114, (sbyte)114, 0)]
        [InlineData((sbyte)114, sbyte.MinValue, 1)]
        [InlineData((sbyte)114, (sbyte)-123, 1)]
        [InlineData((sbyte)114, (sbyte)0, 1)]
        [InlineData((sbyte)114, (sbyte)123, -1)]
        [InlineData((sbyte)114, sbyte.MaxValue, -1)]
        [InlineData((sbyte)114, null, 1)]
        public void CompareTo_Other_ReturnsExpected(sbyte i, object value, int expected)
        {
            if (value is sbyte sbyteValue)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(sbyteValue)));
            }

            Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData(234)]
        public void CompareTo_ObjectNotSByte_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((sbyte)123).CompareTo(value));
        }

        [Theory]
        [InlineData((sbyte)78, (sbyte)78, true)]
        [InlineData((sbyte)78, (sbyte)-78, false)]
        [InlineData((sbyte)78, (sbyte)0, false)]
        [InlineData((sbyte)0, (sbyte)0, true)]
        [InlineData((sbyte)-78, (sbyte)-78, true)]
        [InlineData((sbyte)-78, (sbyte)78, false)]
        [InlineData((sbyte)78, null, false)]
        [InlineData((sbyte)78, "78", false)]
        [InlineData((sbyte)78, 78, false)]
        public static void Equals(sbyte i1, object obj, bool expected)
        {
            if (obj is sbyte)
            {
                sbyte i2 = (sbyte)obj;
                Assert.Equal(expected, i1.Equals(i2));
                Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
            }
            Assert.Equal(expected, i1.Equals(obj));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsSByte()
        {
            Assert.Equal(TypeCode.SByte, ((sbyte)1).GetTypeCode());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                yield return new object[] { sbyte.MinValue, "G", defaultFormat, "-128" };
                yield return new object[] { (sbyte)-123, "G", defaultFormat, "-123" };
                yield return new object[] { (sbyte)0, "G", defaultFormat, "0" };
                yield return new object[] { (sbyte)123, "G", defaultFormat, "123" };
                yield return new object[] { sbyte.MaxValue, "G", defaultFormat, "127" };

                yield return new object[] { (sbyte)123, "D", defaultFormat, "123" };
                yield return new object[] { (sbyte)123, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000123" };
                yield return new object[] { (sbyte)(-123), "D99", defaultFormat, "-000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000123" };

                yield return new object[] { (sbyte)0x24, "x", defaultFormat, "24" };
                yield return new object[] { (sbyte)24, "N", defaultFormat, string.Format("{0:N}", 24.00) };
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
            yield return new object[] { (sbyte)-24, "N", customFormat, "#24~00" };
            yield return new object[] { (sbyte)24, "N", customFormat, "24~00" };
            yield return new object[] { (sbyte)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (sbyte)123, "F", customFormat, "123~00" };
            yield return new object[] { (sbyte)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString(sbyte i, string format, IFormatProvider provider, string expected)
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
            IComparable comparable = (sbyte)123;
            AssertExtensions.Throws<ArgumentException>(null, () => comparable.CompareTo("a")); // Obj is not a sbyte
            AssertExtensions.Throws<ArgumentException>(null, () => comparable.CompareTo(234)); // Obj is not a sbyte
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

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Integer;
            NumberFormatInfo emptyFormat = new NumberFormatInfo();

            NumberFormatInfo customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";

            yield return new object[] { "-123", defaultStyle, null, (sbyte)-123 };
            yield return new object[] { "0", defaultStyle, null, (sbyte)0 };
            yield return new object[] { "123", defaultStyle, null, (sbyte)123 };
            yield return new object[] { "+123", defaultStyle, null, (sbyte)123 };
            yield return new object[] { "  123  ", defaultStyle, null, (sbyte)123 };
            yield return new object[] { "127", defaultStyle, null, (sbyte)127 };

            yield return new object[] { "12", NumberStyles.HexNumber, null, (sbyte)0x12 };
            yield return new object[] { "10", NumberStyles.AllowThousands, null, (sbyte)10 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, null, (sbyte)-123 }; // Parentheses = negative

            yield return new object[] { "123", defaultStyle, emptyFormat, (sbyte)123 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, (sbyte)123 };
            yield return new object[] { "12", NumberStyles.HexNumber, emptyFormat, (sbyte)0x12 };
            yield return new object[] { "a", NumberStyles.HexNumber, null, (sbyte)0xa };
            yield return new object[] { "A", NumberStyles.HexNumber, null, (sbyte)0xa };
            yield return new object[] { "$100", NumberStyles.Currency, customFormat, (sbyte)100 };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, sbyte expected)
        {
            sbyte result;
            // If no style is specified, use the (String) or (String, IFormatProvider) overload
            if (style == NumberStyles.Integer)
            {
                Assert.True(sbyte.TryParse(value, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, sbyte.Parse(value));

                // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
                if (provider != null)
                {
                    Assert.Equal(expected, sbyte.Parse(value, provider));
                }
            }

            // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
            Assert.True(sbyte.TryParse(value, style, provider ?? new NumberFormatInfo(), out result));
            Assert.Equal(expected, result);

            // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
            if (provider == null)
            {
                Assert.Equal(expected, sbyte.Parse(value, style));
            }
            Assert.Equal(expected, sbyte.Parse(value, style, provider ?? new NumberFormatInfo()));
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

            yield return new object[] { "ab", defaultStyle, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "1E23", defaultStyle, null, typeof(FormatException) }; // Exponent
            yield return new object[] { "(123)", defaultStyle, null, typeof(FormatException) }; // Parentheses
            yield return new object[] { 100.ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency
            yield return new object[] { 1000.ToString("N0"), defaultStyle, null, typeof(FormatException) }; // Thousands
            yield return new object[] { 67.90.ToString("F2"), defaultStyle, null, typeof(FormatException) }; // Decimal
            yield return new object[] { "+-123", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "-+123", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "+abc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "-abc", NumberStyles.HexNumber, null, typeof(FormatException) };

            yield return new object[] { "- 123", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "+ 123", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "ab", NumberStyles.None, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) }; // Trailing and leading whitespace

            yield return new object[] { "67.90", defaultStyle, customFormat, typeof(FormatException) }; // Decimal

            yield return new object[] { "-129", defaultStyle, null, typeof(OverflowException) }; // < min value
            yield return new object[] { "128", defaultStyle, null, typeof(OverflowException) }; // > max value
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            sbyte result;
            // If no style is specified, use the (String) or (String, IFormatProvider) overload
            if (style == NumberStyles.Integer)
            {
                Assert.False(sbyte.TryParse(value, out result));
                Assert.Equal(default(sbyte), result);

                Assert.Throws(exceptionType, () => sbyte.Parse(value));

                // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
                if (provider != null)
                {
                    Assert.Throws(exceptionType, () => sbyte.Parse(value, provider));
                }
            }

            // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
            Assert.False(sbyte.TryParse(value, style, provider ?? new NumberFormatInfo(), out result));
            Assert.Equal(default(sbyte), result);

            // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => sbyte.Parse(value, style));
            }
            Assert.Throws(exceptionType, () => sbyte.Parse(value, style, provider ?? new NumberFormatInfo()));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            sbyte result = 0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => sbyte.TryParse("1", style, null, out result));
            Assert.Equal(default(sbyte), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => sbyte.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => sbyte.Parse("1", style, null));
        }
    }
}
