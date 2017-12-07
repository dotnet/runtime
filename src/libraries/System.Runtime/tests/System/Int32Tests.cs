// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class Int32Tests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new int();
            Assert.Equal(0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            int i = 41;
            Assert.Equal(41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0x7FFFFFFF, int.MaxValue);
        }

        [Fact]
        public static void MinValue()
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
        [InlineData(-234, -234, 0)]
        [InlineData(-234, 234, -1)]
        [InlineData(-234, -432, 1)]
        [InlineData(234, null, 1)]
        public void CompareTo_Other_ReturnsExpected(int i, object value, int expected)
        {
            if (value is int intValue)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(intValue)));
            }

            Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData((long)234)]
        public void CompareTo_ObjectNotInt_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => 123.CompareTo(value));
        }

        [Theory]
        [InlineData(789, 789, true)]
        [InlineData(789, -789, false)]
        [InlineData(789, 0, false)]
        [InlineData(0, 0, true)]
        [InlineData(-789, -789, true)]
        [InlineData(-789, 789, false)]
        [InlineData(789, null, false)]
        [InlineData(789, "789", false)]
        [InlineData(789, (long)789, false)]
        public static void Equals(int i1, object obj, bool expected)
        {
            if (obj is int)
            {
                int i2 = (int)obj;
                Assert.Equal(expected, i1.Equals(i2));
                Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
            }
            Assert.Equal(expected, i1.Equals(obj));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsInt32()
        {
            Assert.Equal(TypeCode.Int32, 1.GetTypeCode());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "" })
                {
                    yield return new object[] { int.MinValue, defaultSpecifier, defaultFormat, "-2147483648" };
                    yield return new object[] { -4567, defaultSpecifier, defaultFormat, "-4567" };
                    yield return new object[] { 0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { 4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { int.MaxValue, defaultSpecifier, defaultFormat, "2147483647" };
                }

                yield return new object[] { 4567, "D", defaultFormat, "4567" };
                yield return new object[] { 4567, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { 4567, "D99\09", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };

                yield return new object[] { 0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { 2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
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
            yield return new object[] { -2468, "N", customFormat, "#2*468~00" };
            yield return new object[] { 2468, "N", customFormat, "2*468~00" };
            yield return new object[] { 123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { 123, "F", customFormat, "123~00" };
            yield return new object[] { 123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString(int i, string format, IFormatProvider provider, string expected)
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
            int i = 123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberFormatInfo samePositiveNegativeFormat = new NumberFormatInfo()
            {
                PositiveSign = "|",
                NegativeSign = "|"
            };

            NumberFormatInfo emptyPositiveFormat = new NumberFormatInfo() { PositiveSign = "" };
            NumberFormatInfo emptyNegativeFormat = new NumberFormatInfo() { NegativeSign = "" };

            // None
            yield return new object[] { "0", NumberStyles.None, null, 0 };
            yield return new object[] { "123", NumberStyles.None, null, 123 };
            yield return new object[] { "2147483647", NumberStyles.None, null, 2147483647 };
            yield return new object[] { "123\0\0", NumberStyles.None, null, 123 };

            // HexNumber
            yield return new object[] { "123", NumberStyles.HexNumber, null, 0x123 };
            yield return new object[] { "abc", NumberStyles.HexNumber, null, 0xabc };
            yield return new object[] { "ABC", NumberStyles.HexNumber, null, 0xabc };
            yield return new object[] { "12", NumberStyles.HexNumber, null, 0x12 };
            yield return new object[] { "80000000", NumberStyles.HexNumber, null, -2147483648 };
            yield return new object[] { "FFFFFFFF", NumberStyles.HexNumber, null, -1 };

            // Currency
            NumberFormatInfo currencyFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                CurrencyGroupSeparator = "|",
                NumberGroupSeparator = "/"
            };
            yield return new object[] { "$1|000", NumberStyles.Currency, currencyFormat, 1000 };
            yield return new object[] { "$1000", NumberStyles.Currency, currencyFormat, 1000 };
            yield return new object[] { "$   1000", NumberStyles.Currency, currencyFormat, 1000 };
            yield return new object[] { "1000", NumberStyles.Currency, currencyFormat, 1000 };
            yield return new object[] { "$(1000)", NumberStyles.Currency, currencyFormat, -1000};
            yield return new object[] { "($1000)", NumberStyles.Currency, currencyFormat, -1000 };
            yield return new object[] { "$-1000", NumberStyles.Currency, currencyFormat, -1000 };
            yield return new object[] { "-$1000", NumberStyles.Currency, currencyFormat, -1000 };

            NumberFormatInfo emptyCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "" };
            yield return new object[] { "100", NumberStyles.Currency, emptyCurrencyFormat, 100 };

            // If CurrencySymbol and Negative are the same, NegativeSign is preferred
            NumberFormatInfo sameCurrencyNegativeSignFormat = new NumberFormatInfo()
            {
                NegativeSign = "|",
                CurrencySymbol = "|"
            };
            yield return new object[] { "|1000", NumberStyles.AllowCurrencySymbol | NumberStyles.AllowLeadingSign, sameCurrencyNegativeSignFormat, -1000 };

            // Any
            yield return new object[] { "123", NumberStyles.Any, null, 123 };

            // AllowLeadingSign
            yield return new object[] { "-2147483648", NumberStyles.AllowLeadingSign, null, -2147483648 };
            yield return new object[] { "-123", NumberStyles.AllowLeadingSign, null, -123 };
            yield return new object[] { "+0", NumberStyles.AllowLeadingSign, null, 0 };
            yield return new object[] { "-0", NumberStyles.AllowLeadingSign, null, 0 };
            yield return new object[] { "+123", NumberStyles.AllowLeadingSign, null, 123 };
            
            // If PositiveSign and NegativeSign are the same, PositiveSign is preferred
            yield return new object[] { "|123", NumberStyles.AllowLeadingSign, samePositiveNegativeFormat, 123 };

            // Empty PositiveSign or NegativeSign
            yield return new object[] { "100", NumberStyles.AllowLeadingSign, emptyPositiveFormat, 100 };
            yield return new object[] { "100", NumberStyles.AllowLeadingSign, emptyNegativeFormat, 100 };

            // AllowTrailingSign
            yield return new object[] { "123", NumberStyles.AllowTrailingSign, null, 123 };
            yield return new object[] { "123+", NumberStyles.AllowTrailingSign, null, 123 };
            yield return new object[] { "123-", NumberStyles.AllowTrailingSign, null, -123 };

            // If PositiveSign and NegativeSign are the same, PositiveSign is preferred
            yield return new object[] { "123|", NumberStyles.AllowTrailingSign, samePositiveNegativeFormat, 123 };

            // Empty PositiveSign or NegativeSign
            yield return new object[] { "100", NumberStyles.AllowTrailingSign, emptyPositiveFormat, 100 };
            yield return new object[] { "100", NumberStyles.AllowTrailingSign, emptyNegativeFormat, 100 };

            // AllowLeadingWhite and AllowTrailingWhite
            yield return new object[] { "123  ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, 123 };
            yield return new object[] { "  123", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, 123 };
            yield return new object[] { "  123  ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, 123 };

            // AllowThousands
            NumberFormatInfo thousandsFormat = new NumberFormatInfo() { NumberGroupSeparator = "|" };
            yield return new object[] { "1000", NumberStyles.AllowThousands, thousandsFormat, 1000 };
            yield return new object[] { "1|0|0|0", NumberStyles.AllowThousands, thousandsFormat, 1000 };
            yield return new object[] { "1|||", NumberStyles.AllowThousands, thousandsFormat, 1 };

            NumberFormatInfo integerNumberSeparatorFormat = new NumberFormatInfo() { NumberGroupSeparator = "1" };
            yield return new object[] { "1111", NumberStyles.AllowThousands, integerNumberSeparatorFormat, 1111 };

            // AllowExponent
            yield return new object[] { "1E2", NumberStyles.AllowExponent, null, 100 };
            yield return new object[] { "1E+2", NumberStyles.AllowExponent, null, 100 };
            yield return new object[] { "1e2", NumberStyles.AllowExponent, null, 100 };
            yield return new object[] { "1E0", NumberStyles.AllowExponent, null, 1 };
            yield return new object[] { "(1E2)", NumberStyles.AllowExponent | NumberStyles.AllowParentheses, null, -100 };
            yield return new object[] { "-1E2", NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, null, -100 };

            NumberFormatInfo negativeFormat = new NumberFormatInfo() { PositiveSign = "|" };
            yield return new object[] { "1E|2", NumberStyles.AllowExponent, negativeFormat, 100 };

            // AllowParentheses
            yield return new object[] { "123", NumberStyles.AllowParentheses, null, 123 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, null, -123 };

            // AllowDecimalPoint
            NumberFormatInfo decimalFormat = new NumberFormatInfo() { NumberDecimalSeparator = "|" };
            yield return new object[] { "67|", NumberStyles.AllowDecimalPoint, decimalFormat, 67 };

            // NumberFormatInfo has a custom property with length > 1
            NumberFormatInfo integerCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "123" };
            yield return new object[] { "123123", NumberStyles.AllowCurrencySymbol, integerCurrencyFormat, 123 };

            NumberFormatInfo integerPositiveSignFormat = new NumberFormatInfo() { PositiveSign = "123" };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, integerPositiveSignFormat, 123 };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, int expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            int result;
            if ((style & ~NumberStyles.Integer) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(int.TryParse(value, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, int.Parse(value));
                }

                Assert.Equal(expected, int.Parse(value, provider));
            }
            
            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(int.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, int.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(int.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);
                
                Assert.Equal(expected, int.Parse(value, style));
                Assert.Equal(expected, int.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // String is null, empty or entirely whitespace
            yield return new object[] { null, NumberStyles.Integer, null, typeof(ArgumentNullException) };
            yield return new object[] { null, NumberStyles.Any, null, typeof(ArgumentNullException) };
            yield return new object[] { "", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "", NumberStyles.Any, null, typeof(FormatException) };
            yield return new object[] { " \t \n \r ", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { " \t \n \r ", NumberStyles.Any, null, typeof(FormatException) };

            // String is garbage
            yield return new object[] { "Garbage", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "Garbage", NumberStyles.Any, null, typeof(FormatException) };

            // String has leading zeros
            yield return new object[] { "\0\0123", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "\0\0123", NumberStyles.Any, null, typeof(FormatException) };

            // String has internal zeros
            yield return new object[] { "1\023", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "1\023", NumberStyles.Any, null, typeof(FormatException) };

            // Integer doesn't allow hex, exponents, paretheses, currency, thousands, decimal
            yield return new object[] { "abc", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "1E23", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "(123)", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { 1000.ToString("C0"), NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { 678.90.ToString("F2"), NumberStyles.Integer, null, typeof(FormatException) };

            // HexNumber
            yield return new object[] { "0xabc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "&habc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "G1", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "g1", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "+abc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "-abc", NumberStyles.HexNumber, null, typeof(FormatException) };

            // None doesn't allow hex or leading or trailing whitespace
            yield return new object[] { "abc", NumberStyles.None, null, typeof(FormatException) };
            yield return new object[] { "123   ", NumberStyles.None, null, typeof(FormatException) };
            yield return new object[] { "   123", NumberStyles.None, null, typeof(FormatException) };
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) };

            // AllowLeadingSign
            yield return new object[] { "+", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "-", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "+-123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "-+123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "- 123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "+ 123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };

            // AllowTrailingSign
            yield return new object[] { "123-+", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "123+-", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "123 -", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "123 +", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };

            // Parentheses has priority over CurrencySymbol and PositiveSign
            NumberFormatInfo currencyNegativeParenthesesFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "(",
                PositiveSign = "))"
            };
            yield return new object[] { "(100))", NumberStyles.AllowParentheses | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowTrailingSign, currencyNegativeParenthesesFormat, typeof(FormatException) };

            // AllowTrailingSign and AllowLeadingSign
            yield return new object[] { "+123+", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "+123-", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "-123+", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "-123-", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };

            // AllowLeadingSign and AllowParentheses
            yield return new object[] { "-(1000)", NumberStyles.AllowLeadingSign | NumberStyles.AllowParentheses, null, typeof(FormatException) };
            yield return new object[] { "(-1000)", NumberStyles.AllowLeadingSign | NumberStyles.AllowParentheses, null, typeof(FormatException) };

            // AllowLeadingWhite
            yield return new object[] { "1   ", NumberStyles.AllowLeadingWhite, null, typeof(FormatException) };
            yield return new object[] { "   1   ", NumberStyles.AllowLeadingWhite, null, typeof(FormatException) };

            // AllowTrailingWhite
            yield return new object[] { "   1       ", NumberStyles.AllowTrailingWhite, null, typeof(FormatException) };
            yield return new object[] { "   1", NumberStyles.AllowTrailingWhite, null, typeof(FormatException) };

            // AllowThousands
            NumberFormatInfo thousandsFormat = new NumberFormatInfo() { NumberGroupSeparator = "|" };
            yield return new object[] { "|||1", NumberStyles.AllowThousands, null, typeof(FormatException) };

            // AllowExponent
            yield return new object[] { "65E", NumberStyles.AllowExponent, null, typeof(FormatException) };
            yield return new object[] { "65E10", NumberStyles.AllowExponent, null, typeof(OverflowException) };
            yield return new object[] { "65E+10", NumberStyles.AllowExponent, null, typeof(OverflowException) };
            yield return new object[] { "65E-1", NumberStyles.AllowExponent, null, typeof(OverflowException) };

            // AllowDecimalPoint
            NumberFormatInfo decimalFormat = new NumberFormatInfo() { NumberDecimalSeparator = "." };
            yield return new object[] { (67.9).ToString(), NumberStyles.AllowDecimalPoint, null, typeof(OverflowException) };

            // Parsing integers doesn't allow NaN, PositiveInfinity or NegativeInfinity
            NumberFormatInfo doubleFormat = new NumberFormatInfo()
            {
                NaNSymbol = "NaN",
                PositiveInfinitySymbol = "Infinity",
                NegativeInfinitySymbol = "-Infinity"
            };
            yield return new object[] { "NaN", NumberStyles.Any, doubleFormat, typeof(FormatException) };
            yield return new object[] { "Infinity", NumberStyles.Any, doubleFormat, typeof(FormatException) };
            yield return new object[] { "-Infinity", NumberStyles.Any, doubleFormat, typeof(FormatException) };

            // NumberFormatInfo has a custom property with length > 1
            NumberFormatInfo integerCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "123" };
            yield return new object[] { "123", NumberStyles.AllowCurrencySymbol, integerCurrencyFormat, typeof(FormatException) };

            NumberFormatInfo integerPositiveSignFormat = new NumberFormatInfo() { PositiveSign = "123" };
            yield return new object[] { "123", NumberStyles.AllowLeadingSign, integerPositiveSignFormat, typeof(FormatException) };

            // Not in range of Int32
            yield return new object[] { "2147483648", NumberStyles.Any, null, typeof(OverflowException) };
            yield return new object[] { "2147483648", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "-2147483649", NumberStyles.Any, null, typeof(OverflowException) };
            yield return new object[] { "-2147483649", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "2147483649-", NumberStyles.AllowTrailingSign, null, typeof(OverflowException) };
            yield return new object[] { "(2147483649)", NumberStyles.AllowParentheses, null, typeof(OverflowException) };
            yield return new object[] { "2E10", NumberStyles.AllowExponent, null, typeof(OverflowException) };
            yield return new object[] { "800000000", NumberStyles.AllowHexSpecifier, null, typeof(OverflowException) };

            yield return new object[] { "9223372036854775808", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "-9223372036854775809", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "8000000000000000", NumberStyles.AllowHexSpecifier, null, typeof(OverflowException) };
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            int result;
            if ((style & ~NumberStyles.Integer) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(int.TryParse(value, out result));
                    Assert.Equal(default(int), result);

                    Assert.Throws(exceptionType, () => int.Parse(value));
                }

                Assert.Throws(exceptionType, () => int.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(int.TryParse(value, style, provider, out result));
            Assert.Equal(default(int), result);

            Assert.Throws(exceptionType, () => int.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(int.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(int), result);

                Assert.Throws(exceptionType, () => int.Parse(value, style));
                Assert.Throws(exceptionType, () => int.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            int result = 0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => int.TryParse("1", style, null, out result));
            Assert.Equal(default(int), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => int.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => int.Parse("1", style, null));
        }
    }
}
