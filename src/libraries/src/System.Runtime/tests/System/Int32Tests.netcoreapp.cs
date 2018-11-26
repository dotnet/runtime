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
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(int i, string format, IFormatProvider provider, string expected)
        {
            char[] actual;
            int charsWritten;

            // Just right
            actual = new char[expected.Length];
            Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(actual));

            // Longer than needed
            actual = new char[expected.Length + 1];
            Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(actual, 0, charsWritten));

            // Too short
            if (expected.Length > 0)
            {
                actual = new char[expected.Length - 1];
                Assert.False(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
                Assert.Equal(0, charsWritten);
            }

            if (format != null)
            {
                // Upper format
                actual = new char[expected.Length];
                Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToUpperInvariant(), provider));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected.ToUpperInvariant(), new string(actual));

                // Lower format
                actual = new char[expected.Length];
                Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToLowerInvariant(), provider));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected.ToLowerInvariant(), new string(actual));
            }
        }
    }
}
