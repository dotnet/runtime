// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class Int32Tests
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
        [InlineData(-234, int.MinValue, 1)]
        [InlineData(int.MinValue, int.MinValue, 0)]
        [InlineData(234, -123, 1)]
        [InlineData(234, 0, 1)]
        [InlineData(234, 123, 1)]
        [InlineData(234, 456, -1)]
        [InlineData(234, int.MaxValue, -1)]
        [InlineData(-234, int.MaxValue, -1)]
        [InlineData(int.MaxValue, int.MaxValue, 0)]
        [InlineData(-234, -234, 0)]
        [InlineData(-234, 234, -1)]
        [InlineData(-234, -432, 1)]
        [InlineData(234, null, 1)]
        public void CompareTo_Other_ReturnsExpected(int i, object value, int expected)
        {
            if (value is int intValue)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(intValue)));
                Assert.Equal(-expected, Math.Sign(intValue.CompareTo(i)));
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
        public static void EqualsTest(int i1, object obj, bool expected)
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
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "", "R" })
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
                yield return new object[] { -4567, "D99\09", defaultFormat, "-000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };

                yield return new object[] { 0, "x", defaultFormat, "0" };
                yield return new object[] { 0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { -0x2468, "x", defaultFormat, "ffffdb98" };

                yield return new object[] { 0, "b", defaultFormat, "0" };
                yield return new object[] { 0x2468, "b", defaultFormat, "10010001101000" };
                yield return new object[] { -0x2468, "b", defaultFormat, "11111111111111111101101110011000" };

                yield return new object[] { 2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };

            }

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { 32, "C100", invariantFormat, "\u00A432.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { 32, "P100", invariantFormat, "3,200.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { 32, "D100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000032" };
            yield return new object[] { 32, "E100", invariantFormat, "3.2000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { 32, "F100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { 32, "N100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { 32, "X100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020" };
            yield return new object[] { 32, "B100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000100000" };

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
        public static void ToStringTest(int i, string format, IFormatProvider provider, string expected)
        {
            // Format is case insensitive
            string upperFormat = format.ToUpperInvariant();
            string lowerFormat = format.ToLowerInvariant();

            string upperExpected = expected.ToUpperInvariant();
            string lowerExpected = expected.ToLowerInvariant();

            bool isDefaultProvider = (provider == null || provider == NumberFormatInfo.CurrentInfo);
            if (string.IsNullOrEmpty(format) || format.ToUpperInvariant() is "G" or "R")
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
            yield return new object[] { "0000000000000000000000000000000000000000000000000000000000", NumberStyles.None, null, 0 };
            yield return new object[] { "0000000000000000000000000000000000000000000000000000000001", NumberStyles.None, null, 1 };
            yield return new object[] { "2147483647", NumberStyles.None, null, 2147483647 };
            yield return new object[] { "02147483647", NumberStyles.None, null, 2147483647 };
            yield return new object[] { "00000000000000000000000000000000000000000000000002147483647", NumberStyles.None, null, 2147483647 };
            yield return new object[] { "123\0\0", NumberStyles.None, null, 123 };

            // All lengths decimal
            foreach (bool neg in new[] { false, true })
            {
                string s = neg ? "-" : "";
                int result = 0;
                for (int i = 1; i <= 10; i++)
                {
                    result = result * 10 + (i % 10);
                    s += (i % 10).ToString();
                    yield return new object[] { s, NumberStyles.Integer, null, neg ? result * -1 : result };
                }
            }

            // All lengths hexadecimal
            {
                string s = "";
                int result = 0;
                for (int i = 1; i <= 8; i++)
                {
                    result = (result * 16) + (i % 16);
                    s += (i % 16).ToString("X");
                    yield return new object[] { s, NumberStyles.HexNumber, null, result };
                }
            }

            // All lengths binary
            {
                string s = "";
                int result = 0;
                for (int i = 1; i <= 32; i++)
                {
                    result = (result * 2) + (i % 2);
                    s += (i % 2).ToString("b");
                    yield return new object[] { s, NumberStyles.BinaryNumber, null, result };
                }
            }

            // HexNumber
            yield return new object[] { "123", NumberStyles.HexNumber, null, 0x123 };
            yield return new object[] { "abc", NumberStyles.HexNumber, null, 0xabc };
            yield return new object[] { "ABC", NumberStyles.HexNumber, null, 0xabc };
            yield return new object[] { "12", NumberStyles.HexNumber, null, 0x12 };
            yield return new object[] { "80000000", NumberStyles.HexNumber, null, int.MinValue };
            yield return new object[] { "FFFFFFFF", NumberStyles.HexNumber, null, -1 };

            // BinaryNumber
            yield return new object[] { "100100011", NumberStyles.BinaryNumber, null, 0b100100011 };
            yield return new object[] { "101010111100", NumberStyles.BinaryNumber, null, 0b101010111100 };
            yield return new object[] { "10010", NumberStyles.BinaryNumber, null, 0b10010 };
            yield return new object[] { "10000000000000000000000000000000", NumberStyles.BinaryNumber, null, int.MinValue };
            yield return new object[] { "11111111111111111111111111111111", NumberStyles.BinaryNumber, null, -1 };

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
            yield return new object[] { "$(1000)", NumberStyles.Currency, currencyFormat, -1000 };
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
            yield return new object[] { "  123", NumberStyles.AllowLeadingWhite, null, 123 };
            yield return new object[] { "  123  ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, 123 };
            yield return new object[] { "123  ", NumberStyles.AllowTrailingWhite, null, 123 };
            yield return new object[] { "123  \0\0", NumberStyles.AllowTrailingWhite, null, 123 };
            yield return new object[] { "   2147483647   ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, 2147483647 };
            yield return new object[] { "   -2147483648   ", NumberStyles.Integer, null, -2147483648 };
            foreach (char c in new[] { (char)0x9, (char)0xA, (char)0xB, (char)0xC, (char)0xD })
            {
                string cs = c.ToString();
                yield return new object[] { cs + cs + "123" + cs + cs, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, 123 };
            }
            yield return new object[] { "  0  ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, 0 };
            yield return new object[] { "  000000000  ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, 0 };

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

            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "1" }, 23123 };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "1" }, -23123 };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "123" }, 123 };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "123" }, -123 };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "12312" }, 3 };
            yield return new object[] { "123123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "12312" }, -3 };

            // Test trailing zeros
            yield return new object[] { "3.00", NumberStyles.Number, CultureInfo.InvariantCulture, 3 };
            yield return new object[] { "3.00000000", NumberStyles.Number, CultureInfo.InvariantCulture, 3 };
            yield return new object[] { "3.000000000", NumberStyles.Number, CultureInfo.InvariantCulture, 3 };
            yield return new object[] { "3.0000000000", NumberStyles.Number, CultureInfo.InvariantCulture, 3 };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, int expected)
        {
            int result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(int.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, int.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, int.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(int.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, int.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, int.Parse(value, provider));
            }

            // Full overloads
            Assert.True(int.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, int.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // String is null, empty or entirely whitespace
            yield return new object[] { null, NumberStyles.Integer, null, typeof(ArgumentNullException) };
            yield return new object[] { null, NumberStyles.Any, null, typeof(ArgumentNullException) };

            // String contains is null, empty or enitrely whitespace.
            foreach (NumberStyles style in new[] { NumberStyles.Integer, NumberStyles.HexNumber, NumberStyles.BinaryNumber, NumberStyles.Any })
            {
                yield return new object[] { null, style, null, typeof(ArgumentNullException) };
                yield return new object[] { "", style, null, typeof(FormatException) };
                yield return new object[] { " \t \n \r ", style, null, typeof(FormatException) };
                yield return new object[] { "   \0\0", style, null, typeof(FormatException) };
            }

            // Leading or trailing chars for which char.IsWhiteSpace is true but that's not valid for leading/trailing whitespace
            foreach (string c in new[] { "\x0085", "\x00A0", "\x1680", "\x2000", "\x2001", "\x2002", "\x2003", "\x2004", "\x2005", "\x2006", "\x2007", "\x2008", "\x2009", "\x200A", "\x2028", "\x2029", "\x202F", "\x205F", "\x3000" })
            {
                yield return new object[] { c + "123", NumberStyles.Integer, null, typeof(FormatException) };
                yield return new object[] { "123" + c, NumberStyles.Integer, null, typeof(FormatException) };
            }

            // String contains garbage
            foreach (NumberStyles style in new[] { NumberStyles.Integer, NumberStyles.HexNumber, NumberStyles.BinaryNumber, NumberStyles.Any })
            {
                yield return new object[] { "Garbage", style, null, typeof(FormatException) };
                yield return new object[] { "g", style, null, typeof(FormatException) };
                yield return new object[] { "g1", style, null, typeof(FormatException) };
                yield return new object[] { "1g", style, null, typeof(FormatException) };
                yield return new object[] { "123g", style, null, typeof(FormatException) };
                yield return new object[] { "g123", style, null, typeof(FormatException) };
                yield return new object[] { "214748364g", style, null, typeof(FormatException) };
            }

            // String has leading zeros
            yield return new object[] { "\0\0123", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "\0\0123", NumberStyles.Any, null, typeof(FormatException) };

            // String has internal zeros
            yield return new object[] { "1\023", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "1\023", NumberStyles.Any, null, typeof(FormatException) };

            // String has trailing zeros but with whitespace after
            yield return new object[] { "123\0\0   ", NumberStyles.Integer, null, typeof(FormatException) };

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

            // BinaryNumber
            yield return new object[] { "0b101010111100", NumberStyles.BinaryNumber, null, typeof(FormatException) };
            yield return new object[] { "G1", NumberStyles.BinaryNumber, null, typeof(FormatException) };
            yield return new object[] { "g1", NumberStyles.BinaryNumber, null, typeof(FormatException) };
            yield return new object[] { "+101", NumberStyles.BinaryNumber, null, typeof(FormatException) };
            yield return new object[] { "-010", NumberStyles.BinaryNumber, null, typeof(FormatException) };

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
            yield return new object[] { "67.9", NumberStyles.AllowDecimalPoint, decimalFormat, typeof(OverflowException) };

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

            // Only has a leading sign
            yield return new object[] { "+", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "-", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { " +", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { " -", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "+ ", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "- ", NumberStyles.Integer, null, typeof(FormatException) };

            // NumberFormatInfo has a custom property with length > 1
            NumberFormatInfo integerCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "123" };
            yield return new object[] { "123", NumberStyles.AllowCurrencySymbol, integerCurrencyFormat, typeof(FormatException) };
            yield return new object[] { "123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { PositiveSign = "123" }, typeof(FormatException) };
            yield return new object[] { "123", NumberStyles.AllowLeadingSign, new NumberFormatInfo() { NegativeSign = "123" }, typeof(FormatException) };

            // Decimals not in range of Int32
            foreach (string s in new[]
            {
                "2147483648", // int.MaxValue + 1
                "2147483650", // 10s digit incremented above int.MaxValue
                "10000000000", // extra digit after int.MaxValue

                "4294967296", // uint.MaxValue + 1
                "4294967300", // 100s digit incremented above uint.MaxValue

                "9223372036854775808", // long.MaxValue + 1
                "9223372036854775810", // 10s digit incremented above long.MaxValue
                "10000000000000000000", // extra digit after long.MaxValue

                "18446744073709551616", // ulong.MaxValue + 1
                "18446744073709551620", // 10s digit incremented above ulong.MaxValue
                "100000000000000000000", // extra digit after ulong.MaxValue

                "-2147483649", // int.MinValue - 1
                "-2147483650", // 10s digit decremented below int.MinValue
                "-10000000000", // extra digit after int.MinValue

                "-9223372036854775809", // long.MinValue - 1
                "-9223372036854775810", // 10s digit decremented below long.MinValue
                "-10000000000000000000", // extra digit after long.MinValue

                "100000000000000000000000000000000000000000000000000000000000000000000000000000000000000", // really big
                "-100000000000000000000000000000000000000000000000000000000000000000000000000000000000000" // really small
            })
            {
                foreach (NumberStyles styles in new[] { NumberStyles.Any, NumberStyles.Integer })
                {
                    yield return new object[] { s, styles, null, typeof(OverflowException) };
                    yield return new object[] { s + "   ", styles, null, typeof(OverflowException) };
                    yield return new object[] { s + "   " + "\0\0\0", styles, null, typeof(OverflowException) };

                    yield return new object[] { s + "g", styles, null, typeof(FormatException) };
                    yield return new object[] { s + "\0g", styles, null, typeof(FormatException) };
                    yield return new object[] { s + " g", styles, null, typeof(FormatException) };
                }
            }

            // Hexadecimals not in range of Int32
            foreach (string s in new[]
            {
                "100000000", // uint.MaxValue + 1
                "FFFFFFFF0", // extra digit after uint.MaxValue

                "10000000000000000", // ulong.MaxValue + 1
                "FFFFFFFFFFFFFFFF0", // extra digit after ulong.MaxValue

                "100000000000000000000000000000000000000000000000000000000000000000000000000000000000000" // really big
            })
            {
                yield return new object[] { s, NumberStyles.HexNumber, null, typeof(OverflowException) };
                yield return new object[] { s + "   ", NumberStyles.HexNumber, null, typeof(OverflowException) };
                yield return new object[] { s + "   " + "\0\0", NumberStyles.HexNumber, null, typeof(OverflowException) };

                yield return new object[] { s + "g", NumberStyles.HexNumber, null, typeof(FormatException) };
                yield return new object[] { s + "\0g", NumberStyles.HexNumber, null, typeof(FormatException) };
                yield return new object[] { s + " g", NumberStyles.HexNumber, null, typeof(FormatException) };
            }

            // Binary numbers not in range of Int32
            foreach (string s in new[]
            {
                "100000000000000000000000000000000", // uint.MaxValue + 1
                "111111111111111111111111111111110", // extra digit after uint.MaxValue

                "10000000000000000000000000000000000000000000000000000000000000000", // ulong.MaxValue + 1
                "11111111111111111111111111111111111111111111111111111111111111110", // extra digit after ulong.MaxValue

                "000000000000100000000000000000000000000000000000000000000000000000000000000000000000000000000000000" // really big
            })
            {
                yield return new object[] { s, NumberStyles.BinaryNumber, null, typeof(OverflowException) };
                yield return new object[] { s + "   ", NumberStyles.BinaryNumber, null, typeof(OverflowException) };
                yield return new object[] { s + "   " + "\0\0", NumberStyles.BinaryNumber, null, typeof(OverflowException) };

                yield return new object[] { s + "g", NumberStyles.BinaryNumber, null, typeof(FormatException) };
                yield return new object[] { s + "\0g", NumberStyles.BinaryNumber, null, typeof(FormatException) };
                yield return new object[] { s + " g", NumberStyles.BinaryNumber, null, typeof(FormatException) };
            }

            yield return new object[] { "2147483649-", NumberStyles.AllowTrailingSign, null, typeof(OverflowException) };
            yield return new object[] { "(2147483649)", NumberStyles.AllowParentheses, null, typeof(OverflowException) };
            yield return new object[] { "2E10", NumberStyles.AllowExponent, null, typeof(OverflowException) };
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            int result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(int.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => int.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => int.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(int.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => int.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => int.Parse(value, provider));
            }

            // Full overloads
            Assert.False(int.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => int.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.BinaryNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.HexNumber | NumberStyles.BinaryNumber)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00))]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style)
        {
            int result = 0;
            AssertExtensions.Throws<ArgumentException>("style", () => int.TryParse("1", style, null, out result));
            Assert.Equal(default(int), result);

            AssertExtensions.Throws<ArgumentException>("style", () => int.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>("style", () => int.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            NumberFormatInfo samePositiveNegativeFormat = new NumberFormatInfo()
            {
                PositiveSign = "|",
                NegativeSign = "|"
            };

            NumberFormatInfo emptyPositiveFormat = new NumberFormatInfo() { PositiveSign = "" };
            NumberFormatInfo emptyNegativeFormat = new NumberFormatInfo() { NegativeSign = "" };

            // None
            yield return new object[] { "2147483647", 1, 9, NumberStyles.None, null, 147483647 };
            yield return new object[] { "2147483647", 1, 1, NumberStyles.None, null, 1 };
            yield return new object[] { "123\0\0", 2, 2, NumberStyles.None, null, 3 };

            // Hex
            yield return new object[] { "abc", 0, 1, NumberStyles.HexNumber, null, 0xa };
            yield return new object[] { "ABC", 1, 1, NumberStyles.HexNumber, null, 0xB };
            yield return new object[] { "FFFFFFFF", 6, 2, NumberStyles.HexNumber, null, 0xFF };
            yield return new object[] { "FFFFFFFF", 0, 1, NumberStyles.HexNumber, null, 0xF };

            // Binary
            yield return new object[] { "10101", 0, 4, NumberStyles.BinaryNumber, null, 0b1010 };
            yield return new object[] { "001010", 2, 3, NumberStyles.BinaryNumber, null, 0b101 };
            yield return new object[] { "FFFFFF11", 6, 2, NumberStyles.BinaryNumber, null, 0b11 };
            yield return new object[] { "1FFFFFFF", 0, 1, NumberStyles.BinaryNumber, null, 0b1 };

            // Currency
            yield return new object[] { "-$1000", 1, 5, NumberStyles.Currency, new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                CurrencyGroupSeparator = "|",
                NumberGroupSeparator = "/"
            }, 1000 };

            NumberFormatInfo emptyCurrencyFormat = new NumberFormatInfo() { CurrencySymbol = "" };
            yield return new object[] { "100", 1, 2, NumberStyles.Currency, emptyCurrencyFormat, 0 };
            yield return new object[] { "100", 0, 1, NumberStyles.Currency, emptyCurrencyFormat, 1 };

            // If CurrencySymbol and Negative are the same, NegativeSign is preferred
            NumberFormatInfo sameCurrencyNegativeSignFormat = new NumberFormatInfo()
            {
                NegativeSign = "|",
                CurrencySymbol = "|"
            };
            yield return new object[] { "1000", 1, 3, NumberStyles.AllowCurrencySymbol | NumberStyles.AllowLeadingSign, sameCurrencyNegativeSignFormat, 0 };
            yield return new object[] { "|1000", 0, 2, NumberStyles.AllowCurrencySymbol | NumberStyles.AllowLeadingSign, sameCurrencyNegativeSignFormat, -1 };

            // Any
            yield return new object[] { "123", 0, 2, NumberStyles.Any, null, 12 };

            // AllowLeadingSign
            yield return new object[] { "-2147483648", 0, 10, NumberStyles.AllowLeadingSign, null, -214748364 };

            // AllowTrailingSign
            yield return new object[] { "123-", 0, 3, NumberStyles.AllowTrailingSign, null, 123 };

            // AllowExponent
            yield return new object[] { "1E2", 0, 1, NumberStyles.AllowExponent, null, 1 };
            yield return new object[] { "1E+2", 3, 1, NumberStyles.AllowExponent, null, 2 };
            yield return new object[] { "(1E2)", 1, 3, NumberStyles.AllowExponent | NumberStyles.AllowParentheses, null, 1E2 };
            yield return new object[] { "-1E2", 1, 3, NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, null, 1E2 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, int expected)
        {
            int result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(int.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, int.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(int.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                int result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(int.TryParse(value.AsSpan(), out result));
                    Assert.Equal(0, result);
                }

                Assert.Throws(exceptionType, () => int.Parse(value.AsSpan(), style, provider));

                Assert.False(int.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(0, result);
            }
        }

        [Theory]
        [InlineData("N")]
        [InlineData("F")]
        public static void ToString_N_F_EmptyNumberGroup_Success(string specifier)
        {
            var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
            nfi.NumberGroupSizes = new int[0];
            nfi.NumberGroupSeparator = ",";
            Assert.Equal("1234", 1234.ToString($"{specifier}0", nfi));
        }

        [Fact]
        public static void ToString_P_EmptyPercentGroup_Success()
        {
            var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
            nfi.PercentGroupSizes = new int[0];
            nfi.PercentSymbol = "%";
            Assert.Equal("123400 %", 1234.ToString("P0", nfi));
        }

        [Fact]
        public static void ToString_C_EmptyPercentGroup_Success()
        {
            var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
            nfi.CurrencyGroupSizes = new int[0];
            nfi.CurrencySymbol = "$";
            Assert.Equal("$1234", 1234.ToString("C0", nfi));
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(int i, string format, IFormatProvider provider, string expected) =>
            NumberFormatTestHelper.TryFormatNumberTest(i, format, provider, expected);

        [Fact]
        public static void TestNegativeNumberParsingWithHyphen()
        {
            // CLDR data for Swedish culture has negative sign U+2212. This test ensure parsing with the hyphen with such cultures will succeed.
            CultureInfo ci = CultureInfo.GetCultureInfo("sv-SE");
            Assert.Equal(-158, int.Parse("-158", NumberStyles.Integer, ci));
        }

        [Fact]
        public static void TestParseLenientData()
        {
            NumberFormatInfo nfi = new CultureInfo("en-US").NumberFormat;

            string [] negativeSigns = new string []
            {
                "\u2012", // Figure Dash
                "\u207B", // Superscript Minus
                "\u208B", // Subscript Minus
                "\u2212", // Minus Sign
                "\u2796", // Heavy Minus Sign
                "\uFE63", // Small Hyphen-Minus
            };

            foreach (string negativeSign in negativeSigns)
            {
                nfi.NegativeSign = negativeSign;
                Assert.Equal(negativeSign, nfi.NegativeSign);
                Assert.Equal(-9670, int.Parse("-9670", NumberStyles.Integer, nfi));
            }
        }

    }
}
