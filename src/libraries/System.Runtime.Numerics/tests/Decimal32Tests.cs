
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Numerics.Tests
{
    public partial class Decimal32Tests
    {
        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Float | NumberStyles.AllowThousands;

            NumberFormatInfo emptyFormat = NumberFormatInfo.CurrentInfo;

            var dollarSignCommaSeparatorFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                CurrencyGroupSeparator = ","
            };

            var decimalSeparatorFormat = new NumberFormatInfo()
            {
                NumberDecimalSeparator = "."
            };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;

            //                                                          Decimal32(sign, q, c) = -1^sign * 10^q * c
            yield return new object[] { "-123", defaultStyle, null, new Decimal32(true, 0, 123) };
            yield return new object[] { "0", defaultStyle, null, Decimal32.Zero }; // TODO what kind of zero do we want to store here?
            yield return new object[] { "123", defaultStyle, null, new Decimal32(false, 0, 123) };
            yield return new object[] { "  123  ", defaultStyle, null, new Decimal32(false, 0, 123) };
            yield return new object[] { "567.89", defaultStyle, null, new Decimal32(false, -2, 56789) };
            yield return new object[] { "-567.89", defaultStyle, null, new Decimal32(true, -2, 56789) };
            yield return new object[] { "1E23", defaultStyle, null, new Decimal32(false, 23, 1) };

            yield return new object[] { emptyFormat.NumberDecimalSeparator + "234", defaultStyle, null, new Decimal32(false, -3, 234) };
            yield return new object[] { "234" + emptyFormat.NumberDecimalSeparator, defaultStyle, null, new Decimal32(false, 0, 234) };
            yield return new object[] { new string('0', 72) + "3" + new string('0', 38) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, new Decimal32(false, 32, 3000000) }; // could be wrong
            yield return new object[] { new string('0', 73) + "3" + new string('0', 38) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, new Decimal32(false, 32, 3000000) }; // could be wrong

            // Trailing zeros add sig figs and should be accounted for
            yield return new object[] { "1.000", defaultStyle, null, new Decimal32(false, -3, 1000) };
            yield return new object[] { "1.0000000", defaultStyle, null, new Decimal32(false, -6, 1000000) };

            // 10^7 + 5. Not exactly representable
            yield return new object[] { "10000005.0", defaultStyle, invariantFormat, new Decimal32(false, 1, 1000000) };
            yield return new object[] { "10000005.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, new Decimal32(false, 1, 1000001) };
            yield return new object[] { "10000005.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, new Decimal32(false, 1, 1000001) };
            yield return new object[] { "10000005.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, new Decimal32(false, 1, 1000001) };
            yield return new object[] { "5.005", defaultStyle, invariantFormat, new Decimal32(false, -3, 5005) };
            yield return new object[] { "5.050", defaultStyle, invariantFormat, new Decimal32(false, -3, 5050) };
            yield return new object[] { "5.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005", defaultStyle, invariantFormat, new Decimal32(false, -6, 5000000) };
            yield return new object[] { "5.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005", defaultStyle, invariantFormat, new Decimal32(false, -6, 5000000) };
            yield return new object[] { "5.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005", defaultStyle, invariantFormat, new Decimal32(false, -6, 5000000) };
            yield return new object[] { "5.005000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, new Decimal32(false, -6, 5005000) };
            yield return new object[] { "5.0050000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, new Decimal32(false, -6, 5005000) };
            yield return new object[] { "5.0050000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, new Decimal32(false, -6, 5005000) };

            yield return new object[] { "5005.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, new Decimal32(false, -3, 5005000) };
            yield return new object[] { "50050.0", defaultStyle, invariantFormat, new Decimal32(false, -1, 500500) };
            yield return new object[] { "5005", defaultStyle, invariantFormat, new Decimal32(false, 0, 5005) };
            yield return new object[] { "050050", defaultStyle, invariantFormat, new Decimal32(false, 0, 50050) };
            yield return new object[] { "0.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, Decimal32.Zero };
            yield return new object[] { "0.005", defaultStyle, invariantFormat, new Decimal32(false, -3, 5) };
            yield return new object[] { "0.0500", defaultStyle, invariantFormat, new Decimal32(false, -4, 500) };
            yield return new object[] { "6250000000000000000000000000000000e-12", defaultStyle, invariantFormat, new Decimal32(false, 15, 6250000) };
            yield return new object[] { "6250000e0", defaultStyle, invariantFormat, new Decimal32(false, 0, 6250000) };
            yield return new object[] { "6250100e-5", defaultStyle, invariantFormat, new Decimal32(false, -5, 6250100) };
            yield return new object[] { "625010.00e-4", defaultStyle, invariantFormat, new Decimal32(false, -5, 6250100) };
            yield return new object[] { "62500e-4", defaultStyle, invariantFormat, new Decimal32(false, -4, 62500) };
            yield return new object[] { "62500", defaultStyle, invariantFormat, new Decimal32(false, 0, 62500) };

            yield return new object[] { "123.1", NumberStyles.AllowDecimalPoint, null, new Decimal32(false, -1, 1231) };
            yield return new object[] { "1,000", NumberStyles.AllowThousands, null, new Decimal32(false, 0, 1000) };
            yield return new object[] { "1,000.0", NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, null, new Decimal32(false, -1, 10000) };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, new Decimal32(false, 0, 123) };
            yield return new object[] { "123.567", NumberStyles.Any, emptyFormat, new Decimal32(false, -3, 123567) };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, new Decimal32(false, 0, 123) };
            yield return new object[] { "$1,000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, new Decimal32(false, 0, 1000) };
            yield return new object[] { "$1000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, new Decimal32(false, 0, 1000) };
            yield return new object[] { "123.123", NumberStyles.Float, decimalSeparatorFormat, new Decimal32(false, -3, 123123) };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, decimalSeparatorFormat, new Decimal32(true, 0, 123) }; // TODO HalfTests and SingleTests have this output -123, but I'm not exactly sure why

            yield return new object[] { "NaN", NumberStyles.Any, invariantFormat, Decimal32.NaN };
            yield return new object[] { "Infinity", NumberStyles.Any, invariantFormat, Decimal32.PositiveInfinity };
            yield return new object[] { "-Infinity", NumberStyles.Any, invariantFormat, Decimal32.NegativeInfinity };
        }

        private static void AssertEqualAndSameQuantum(Decimal32 expected, Decimal32 result)
        {
            Assert.Equal(expected, result);
            Assert.True(Decimal32.SameQuantum(expected, result));
        }

        private static void AssertEqualAndSameQuantumOrBothNan(Decimal32 expected, Decimal32 result)
        {
            if (!(Decimal32.IsNaN(expected) && Decimal32.IsNaN(result)))
            {
                AssertEqualAndSameQuantum(expected, result);
            }
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, Decimal32 expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Decimal32 result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(Decimal32.TryParse(value, out result));
                    AssertEqualAndSameQuantum(expected, result);

                    AssertEqualAndSameQuantum(expected, Decimal32.Parse(value));
                }

                Assert.True(Decimal32.TryParse(value, provider: provider, out result));
                AssertEqualAndSameQuantum(expected, result);

                AssertEqualAndSameQuantum(expected, Decimal32.Parse(value, provider: provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(Decimal32.TryParse(value, style, provider, out result));

            AssertEqualAndSameQuantumOrBothNan(expected, result);
            AssertEqualAndSameQuantumOrBothNan(expected, Decimal32.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(Decimal32.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                AssertEqualAndSameQuantum(expected, result);

                AssertEqualAndSameQuantum(expected, Decimal32.Parse(value));
                AssertEqualAndSameQuantum(expected, Decimal32.Parse(value, style));
                AssertEqualAndSameQuantum(expected, Decimal32.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Float;

            var dollarSignDecimalSeparatorFormat = new NumberFormatInfo();
            dollarSignDecimalSeparatorFormat.CurrencySymbol = "$";
            dollarSignDecimalSeparatorFormat.NumberDecimalSeparator = ".";

            yield return new object[] { null, defaultStyle, null, typeof(ArgumentNullException) };
            yield return new object[] { "", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { " ", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "Garbage", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "ab", defaultStyle, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "(123)", defaultStyle, null, typeof(FormatException) }; // Parentheses
            yield return new object[] { (100.0f).ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency

            yield return new object[] { (123.456f).ToString(), NumberStyles.Integer, null, typeof(FormatException) }; // Decimal
            yield return new object[] { "  " + (123.456f).ToString(), NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { (123.456f).ToString() + "   ", NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { "1E23", NumberStyles.None, null, typeof(FormatException) }; // Exponent

            yield return new object[] { "ab", NumberStyles.None, null, typeof(FormatException) }; // Negative hex value
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) }; // Trailing and leading whitespace
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Decimal32 result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(Decimal32.TryParse(value, out result));
                    Assert.Equal(default(Decimal32), result);

                    Assert.Throws(exceptionType, () => Decimal32.Parse(value));
                }

                Assert.False(Decimal32.TryParse(value, provider: provider, out result));
                Assert.Equal(default(Decimal32), result);

                Assert.Throws(exceptionType, () => Decimal32.Parse(value, provider: provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(Decimal32.TryParse(value, style, provider, out result));
            Assert.Equal(default(Decimal32), result);

            Assert.Throws(exceptionType, () => Decimal32.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(Decimal32.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(Decimal32), result);

                Assert.Throws(exceptionType, () => Decimal32.Parse(value, style));
                Assert.Throws(exceptionType, () => Decimal32.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            const NumberStyles DefaultStyle = NumberStyles.Float | NumberStyles.AllowThousands;

            yield return new object[] { "-123", 1, 3, DefaultStyle, null, new Decimal32(false, 0, 123) };
            yield return new object[] { "-123", 0, 3, DefaultStyle, null, new Decimal32(true, 0, 12) };
            yield return new object[] { "1E23", 0, 3, DefaultStyle, null, new Decimal32(false, 2, 1) };
            yield return new object[] { "123", 0, 2, NumberStyles.Float, new NumberFormatInfo(), new Decimal32(false, 0, 12) };
            yield return new object[] { "$1,000", 1, 3, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$", CurrencyGroupSeparator = "," }, new Decimal32(false, 0, 10) };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, new Decimal32(false, 0, 123) };
            yield return new object[] { "-Infinity", 1, 8, NumberStyles.Any, NumberFormatInfo.InvariantInfo, Decimal32.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, Decimal32 expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Decimal32 result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(Decimal32.TryParse(value.AsSpan(offset, count), out result));
                    AssertEqualAndSameQuantum(expected, result);

                    AssertEqualAndSameQuantum(expected, Decimal32.Parse(value.AsSpan(offset, count)));
                }

                Assert.True(Decimal32.TryParse(value.AsSpan(offset, count), provider: provider, out result));
                AssertEqualAndSameQuantum(expected, result);

                AssertEqualAndSameQuantum(expected, Decimal32.Parse(value.AsSpan(offset, count), provider: provider));
            }

            AssertEqualAndSameQuantumOrBothNan(expected, Decimal32.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(Decimal32.TryParse(value.AsSpan(offset, count), style, provider, out result));
            AssertEqualAndSameQuantumOrBothNan(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => Decimal32.Parse(value.AsSpan(), style, provider));

                Assert.False(Decimal32.TryParse(value.AsSpan(), style, provider, out Decimal32 result));
                Assert.Equal(default(Decimal32), result);
            }
        }
    }
}

