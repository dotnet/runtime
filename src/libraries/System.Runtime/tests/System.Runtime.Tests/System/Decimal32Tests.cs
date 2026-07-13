// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public class Decimal32Tests
    {
        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Number;
            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;

            NumberFormatInfo emptyFormat = NumberFormatInfo.CurrentInfo;

            var customFormat1 = new NumberFormatInfo();
            customFormat1.CurrencySymbol = "$";
            customFormat1.CurrencyGroupSeparator = ",";

            var customFormat2 = new NumberFormatInfo();
            customFormat2.NumberDecimalSeparator = ".";

            yield return new object[] { "-123", defaultStyle, null, Decimal32.Parse("-123") };
            yield return new object[] { "0", defaultStyle, null, Decimal32.Zero };
            yield return new object[] { "123", defaultStyle, null, Decimal32.Parse("123") };
            yield return new object[] { "  123  ", defaultStyle, null, Decimal32.Parse("123") };
            yield return new object[] { (567.89).ToString(), defaultStyle, null, Decimal32.Parse("56789e-2") };
            yield return new object[] { (-567.89).ToString(), defaultStyle, null, Decimal32.Parse("-56789e-2") };
            yield return new object[] { "0.6666666500000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, Decimal32.Parse("0.66666665") };

            yield return new object[] { "0." + new string('0', 101) + "1", defaultStyle, invariantFormat, Decimal32.Zero };
            yield return new object[] { "-0." + new string('0', 101) + "1", defaultStyle, invariantFormat, Decimal32.NegativeZero };
            yield return new object[] { "0." + new string('0', 100) + "1", defaultStyle, invariantFormat, Decimal32.Parse("1e-101") };
            yield return new object[] { "-0." + new string('0', 100) + "1", defaultStyle, invariantFormat, Decimal32.Parse("-1e-101") };

            yield return new object[] { "0." + new string('0', 99) + "12345", defaultStyle, invariantFormat, Decimal32.Parse("1.2345e-100") };
            yield return new object[] { "-0." + new string('0', 99) + "12345", defaultStyle, invariantFormat, Decimal32.Parse("-1.2345e-100") };
            yield return new object[] { "0." + new string('0', 99) + "12562", defaultStyle, invariantFormat, Decimal32.Parse("1.2562e-100") };
            yield return new object[] { "-0." + new string('0', 99) + "12562", defaultStyle, invariantFormat, Decimal32.Parse("-1.2562e-100") };

            yield return new object[] { emptyFormat.NumberDecimalSeparator + "234", defaultStyle, null, Decimal32.Parse("0.234") };
            yield return new object[] { "234" + emptyFormat.NumberDecimalSeparator, defaultStyle, null, Decimal32.Parse("234") };
            yield return new object[] { "7" + new string('0', 96) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, Decimal32.Parse("7e96") };
            yield return new object[] { "07" + new string('0', 96) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, Decimal32.Parse("7e96") };

            yield return new object[] { (123.1).ToString(), NumberStyles.AllowDecimalPoint, null, Decimal32.Parse("123.1") };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.AllowThousands, null, Decimal32.Parse("1000") };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, Decimal32.Parse("123") };
            yield return new object[] { (123.567).ToString(), NumberStyles.Any, emptyFormat, Decimal32.Parse("123567e-3") };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, Decimal32.Parse("123") };
            yield return new object[] { "$1000", NumberStyles.Currency, customFormat1, Decimal32.Parse("1000") };
            yield return new object[] { "123.123", NumberStyles.Float, customFormat2, Decimal32.Parse("123123e-3") };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, customFormat2, Decimal32.Parse("-123") };

            yield return new object[] { "NaN", NumberStyles.Any, invariantFormat, Decimal32.NaN };
            yield return new object[] { "+NaN", NumberStyles.Any, invariantFormat, Decimal32.NaN };
            yield return new object[] { "Infinity", NumberStyles.Any, invariantFormat, Decimal32.PositiveInfinity };
            yield return new object[] { "+Infinity", NumberStyles.Any, invariantFormat, Decimal32.PositiveInfinity };
            yield return new object[] { "1" + new string('0', 97), NumberStyles.Any, invariantFormat, Decimal32.PositiveInfinity };
            yield return new object[] { "-Infinity", NumberStyles.Any, invariantFormat, Decimal32.NegativeInfinity };
            yield return new object[] { "-1" + new string('0', 97), NumberStyles.Any, invariantFormat, Decimal32.NegativeInfinity };
        }


        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, Decimal32 expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Decimal32 result;
            if ((style & ~NumberStyles.Number) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(Decimal32.TryParse(value, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, Decimal32.Parse(value));
                }

                Assert.Equal(expected, Decimal32.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(Decimal32.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, Decimal32.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(Decimal32.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, Decimal32.Parse(value, style));
                Assert.Equal(expected, Decimal32.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [MemberData(nameof(Parse_Preserve_TrailingZero_TestData))]
        public static void Parse_Preserve_TrailingZero(string value, string expected)
        {
            Assert.Equal(expected, Decimal32.Parse(value).ToString(CultureInfo.InvariantCulture));
        }

        public static IEnumerable<object[]> Parse_Preserve_TrailingZero_TestData()
        {
            yield return new object[] { "0.00", "0.00" };
            yield return new object[] { "0." + new string('0', 101), "0." + new string('0', 101) };
            yield return new object[] { "0." + new string('0', 1000), "0." + new string('0', 101) };
            yield return new object[] { "0." + new string('0', 1000) + "1234567", "0." + new string('0', 101) };
            yield return new object[] { "0e-2", "0.00" };
            yield return new object[] { "0e-101", "0." + new string('0', 101) };
            yield return new object[] { "0e-1000", "0." + new string('0', 101) };
            yield return new object[] { "0.123e-1000", "0." + new string('0', 101) };
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Number;

            yield return new object[] { null, defaultStyle, null, typeof(ArgumentNullException) };
            yield return new object[] { "", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { " ", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "Garbage", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "+Garbage", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "+", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "+  ", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "ab", defaultStyle, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "(123)", defaultStyle, null, typeof(FormatException) }; // Parentheses
            yield return new object[] { 100.ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency

            yield return new object[] { (123.456m).ToString(), NumberStyles.Integer, null, typeof(FormatException) }; // Decimal
            yield return new object[] { "  " + (123.456m).ToString(), NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { (123.456m).ToString() + "   ", NumberStyles.None, null, typeof(FormatException) }; // Trailing space
            yield return new object[] { "1E23", NumberStyles.None, null, typeof(FormatException) }; // Exponent

            yield return new object[] { "ab", NumberStyles.None, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) }; // Trailing and leading whitespace
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Decimal32 result;
            if ((style & ~NumberStyles.Number) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(Decimal32.TryParse(value, out result));
                    Assert.Equal(default(Decimal32), result);

                    Assert.Throws(exceptionType, () => Decimal32.Parse(value));
                }

                Assert.Throws(exceptionType, () => Decimal32.Parse(value, provider));
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

            yield return new object[] { "-123", 1, 3, NumberStyles.Number, null, Decimal32.Parse("123") };
            yield return new object[] { "-123", 0, 3, NumberStyles.Number, null, Decimal32.Parse("-12") };
            yield return new object[] { 1000.ToString("N0"), 0, 4, NumberStyles.AllowThousands, null, Decimal32.Parse("100") };
            yield return new object[] { 1000.ToString("N0"), 2, 3, NumberStyles.AllowThousands, null, Decimal32.Parse("0") };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, Decimal32.Parse("123") };
            yield return new object[] { "1234567890123456789012345.678456", 1, 4, NumberStyles.Number, new NumberFormatInfo() { NumberDecimalSeparator = "." }, Decimal32.Parse("2345") };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, Decimal32 expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Decimal32 result;
            if ((style & ~NumberStyles.Number) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(Decimal32.TryParse(value.AsSpan(offset, count), out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, Decimal32.Parse(value.AsSpan(offset, count)));
                }

                Assert.Equal(expected, Decimal32.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, Decimal32.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(Decimal32.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => Decimal32.Parse(value.AsSpan(), style, provider));

                Assert.False(Decimal32.TryParse(value.AsSpan(), style, provider, out Decimal32 result));
                Assert.Equal(default, result);
            }
        }

        [Theory]
        [MemberData(nameof(Rounding_TestData))]
        public static void Rounding(string s1, string s2)
        {
            Assert.Equal(Decimal32.Parse(s1), Decimal32.Parse(s2));
        }

        public static IEnumerable<object[]> Rounding_TestData()
        {
            yield return new object[] { "12345678", "12345680" };
            yield return new object[] { "12345671", "12345670" };
            yield return new object[] { "12345675", "12345680" };
            yield return new object[] { "12345685", "12345680" };
            yield return new object[] { "123456850001", "123456900000" };
            yield return new object[] { "99999991", "99999990" };
            yield return new object[] { "99999995", "100000000" };
            yield return new object[] { "99999996", "100000000" };
            yield return new object[] { "9999999001", "9999999000" };
        }

        [Theory]
        [MemberData(nameof(MaxValue_Rounding_TestData))]
        public static void MaxValue_Rounding(string value, Decimal32 expected)
        {
            Assert.Equal(expected, Decimal32.Parse(value));
        }

        public static IEnumerable<object[]> MaxValue_Rounding_TestData()
        {
            yield return new object[] { new string('9', 7) + '1' + new string('9', 89), Decimal32.MaxValue };
            yield return new object[] { new string('9', 7) + '4' + new string('9', 89), Decimal32.MaxValue };

            yield return new object[] { new string('9', 7) + '5' + new string('0', 89), Decimal32.PositiveInfinity };
            yield return new object[] { new string('9', 7) + '5' + new string('0', 88) + '1', Decimal32.PositiveInfinity };
            yield return new object[] { "1e97", Decimal32.PositiveInfinity };
            yield return new object[] { "10e96", Decimal32.PositiveInfinity };
            yield return new object[] { "100.3e95", Decimal32.PositiveInfinity };
            
            yield return new object[] { '-' + new string('9', 7) + '5' + new string('0', 89), Decimal32.NegativeInfinity };
            yield return new object[] { '-' + new string('9', 7) + '5' + new string('0', 88) + '1', Decimal32.NegativeInfinity };
            yield return new object[] { "-1e97", Decimal32.NegativeInfinity };
            yield return new object[] { "-10e96", Decimal32.NegativeInfinity };
            yield return new object[] { "-100.3e95", Decimal32.NegativeInfinity };
        }

        [Theory]
        [MemberData(nameof(CompareTo_Other_ReturnsExpected_TestData))]
        public static void CompareTo_Other_ReturnsExpected(Decimal32 d1, Decimal32 d2, int expected)
        {
            Assert.Equal(expected, d1.CompareTo(d2));
            if (expected == 0)
            {
                Assert.Equal(d1, d2);
                Assert.Equal(d2, d1);
            }
            else
            {
                Assert.Equal(-expected, d2.CompareTo(d1));
                Assert.NotEqual(d1, d2);
                Assert.NotEqual(d2, d1);
            }
        }

        public static IEnumerable<object[]> CompareTo_Other_ReturnsExpected_TestData()
        {
            yield return new object[] { Decimal32.Parse("0e5"), Decimal32.Parse("1e1"), -1 };
            yield return new object[] { Decimal32.Parse("0e5"), Decimal32.Parse("-1e1"), 1 };
            yield return new object[] { Decimal32.Parse("0e-5"), Decimal32.Parse("1e1"), -1 };
            yield return new object[] { Decimal32.Parse("0e-5"), Decimal32.Parse("-1e1"), 1 };
            yield return new object[] { Decimal32.Parse("-1e1"), Decimal32.Parse("-10"), 0 };
            yield return new object[] { Decimal32.Parse("-2"), Decimal32.Parse("-3"), 1 };
            yield return new object[] { Decimal32.Parse("3"), Decimal32.Parse("2"), 1 };
            yield return new object[] { Decimal32.Parse("1e90"), Decimal32.Parse("1e90"), 0 };
            yield return new object[] { Decimal32.Parse("9.99999e95"), Decimal32.Parse("9.99999e95"), 0 };
            yield return new object[] { Decimal32.Parse("999e10"), Decimal32.Parse("997e70"), -1 };
            yield return new object[] { Decimal32.Parse("997e70"), Decimal32.Parse("999e10"), 1 };
            yield return new object[] { Decimal32.Parse("1"), Decimal32.Parse("-1"), 1 };
            yield return new object[] { Decimal32.Parse("10"), Decimal32.Parse("-1"), 1 };
            yield return new object[] { Decimal32.Parse("10"), Decimal32.NaN, 1 };
            yield return new object[] { Decimal32.Parse("10"), Decimal32.NegativeInfinity, 1 };
            yield return new object[] { Decimal32.Parse("10"), Decimal32.NegativeZero, 1 };
            yield return new object[] { Decimal32.PositiveInfinity, Decimal32.Parse("10"), 1 };
            yield return new object[] { Decimal32.PositiveInfinity, Decimal32.Parse("10e150"), 0 };
            yield return new object[] { Decimal32.PositiveInfinity, Decimal32.NegativeInfinity, 1 };
            yield return new object[] { Decimal32.PositiveInfinity, Decimal32.PositiveInfinity, 0 };
            yield return new object[] { Decimal32.PositiveInfinity, Decimal32.NegativeZero, 1 };
            yield return new object[] { Decimal32.NegativeInfinity, Decimal32.NegativeInfinity, 0 };
            yield return new object[] { Decimal32.NaN, Decimal32.NaN, 0 };
            yield return new object[] { Decimal32.NegativeZero, Decimal32.NegativeInfinity, 1 };
            yield return new object[] { Decimal32.NegativeZero, Decimal32.Parse("0e20"), 0 };
            yield return new object[] { Decimal32.NegativeZero, Decimal32.NaN, 1 };
            yield return new object[] { Decimal32.Epsilon, Decimal32.Parse("1e-101"), 0 };
            yield return new object[] { Decimal32.Parse("4e-102"), Decimal32.Zero, 0 };
            yield return new object[] { Decimal32.Parse("5e-102"), Decimal32.Zero, 0 };
            yield return new object[] { Decimal32.Parse("5.00001e-102"), Decimal32.Epsilon, 0 };
            yield return new object[] { Decimal32.Parse("0.5" + new string('0', 100) + "1e-101"), Decimal32.Epsilon, 0 };
            yield return new object[] { Decimal32.Parse("0.5" + new string('0', 100) + "1e-102"), Decimal32.Epsilon, -1 };
            yield return new object[] { Decimal32.Parse("5." + new string('0', 300) + "1e-102"), Decimal32.Epsilon, 0 };
            yield return new object[] { Decimal32.Parse("6e-102"), Decimal32.Parse("1e-101"), 0 };
            yield return new object[] { Decimal32.Parse("1000000001e-110"), Decimal32.Epsilon, 0 };
            yield return new object[] { Decimal32.Parse("-1000000001e-110"), Decimal32.Parse("-1e-101"), 0 };
            for (int i = 1; i < 7; i++)
            {
                var d1 = Decimal32.Parse("1e" + i);
                var d2 = Decimal32.Parse("1" + new string('0', i));
                yield return new object[] { d1, d2, 0 };
            }
        }

        [Theory]
        [MemberData(nameof(GetHashCode_TestData))]
        public static void GetHashCodeTest(Decimal32 d1, Decimal32 d2)
        {
            Assert.Equal(d1.GetHashCode(), d2.GetHashCode());
        }

        [Fact]
        public static void CompareToZero()
        {
            var zero = Decimal32.Parse("0e1");
            Assert.Equal(zero, Decimal32.Parse("0e20"));
            Assert.Equal(zero, Decimal32.Parse("1e-102"));
            Assert.Equal(zero, Decimal32.Parse("234e-1000"));
            Assert.Equal(zero, Decimal32.Parse("-1e-102"));
            Assert.Equal(zero, Decimal32.Parse("-234e-1000"));
            Assert.Equal(zero, Decimal32.Zero);
            Assert.Equal(zero, Decimal32.NegativeZero);
            Assert.Equal(Decimal32.Zero, Decimal32.NegativeZero);
        }

        public static IEnumerable<object[]> GetHashCode_TestData()
        {
            yield return new object[] { Decimal32.Zero, Decimal32.NegativeZero };
            yield return new object[] { Decimal32.Zero, Decimal32.Zero };
            yield return new object[] { Decimal32.NaN, Decimal32.NaN };
            yield return new object[] { Decimal32.Parse("0e20"), Decimal32.Parse("0e18") };
            yield return new object[] { Decimal32.Parse("1e7"), Decimal32.Parse("1e7") };
            yield return new object[] { Decimal32.Parse("1e7"), Decimal32.Parse("10e6") };
            yield return new object[] { Decimal32.PositiveInfinity, Decimal32.PositiveInfinity };
            yield return new object[] { Decimal32.NegativeInfinity, Decimal32.NegativeInfinity };
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                yield return new object[] { Decimal32.Parse("-0"), "G", defaultFormat, "-0" };
                yield return new object[] { Decimal32.NegativeZero, "G", defaultFormat, "-0" };
                yield return new object[] { Decimal32.Parse("-0.0000"), "G", defaultFormat, "-0.0000" };
                yield return new object[] { Decimal32.Parse("0"), "G", defaultFormat, "0" };
                yield return new object[] { Decimal32.Zero, "G", defaultFormat, "0" };
                yield return new object[] { Decimal32.Parse("0.0000"), "G", defaultFormat, "0.0000" };
                yield return new object[] { Decimal32.Parse($"{int.MinValue}"), "G", defaultFormat, "-2147484000" };
                yield return new object[] { Decimal32.Parse($"{int.MaxValue}"), "G", defaultFormat, "2147484000" };
                yield return new object[] { Decimal32.Parse("3" + new string('0', 96)), "G", defaultFormat, "3" + new string('0', 96) };
                yield return new object[] { Decimal32.Parse("-3" + new string('0', 96)), "G", defaultFormat, "-3" + new string('0', 96) };
                yield return new object[] { Decimal32.Parse("-4567"), "G", defaultFormat, "-4567" };
                yield return new object[] { Decimal32.Parse("-4567.891"), "G", defaultFormat, "-4567.891" };
                yield return new object[] { Decimal32.Parse("0"), "G", defaultFormat, "0" };
                yield return new object[] { Decimal32.Parse("4567"), "G", defaultFormat, "4567" };
                yield return new object[] { Decimal32.Parse("4567.891"), "G", defaultFormat, "4567.891" };

                yield return new object[] { Decimal32.Parse("2468"), "N", defaultFormat, "2,468.00" };

                yield return new object[] { Decimal32.Parse("2467"), "[#-##-#]", defaultFormat, "[2-46-7]" };
                yield return new object[] { Decimal32.Parse("4e-102"), "G", defaultFormat, "0." + new string('0', 101) };
                yield return new object[] { Decimal32.Parse("5e-102"), "G", defaultFormat, "0." + new string('0', 101) };
                yield return new object[] { Decimal32.Parse("5.000000000000001e-102"), "G", defaultFormat, "0." + new string('0', 100) + "1" };
                yield return new object[] { Decimal32.Parse("6e-102"), "G", defaultFormat, "0." + new string('0', 100) + "1" };
                yield return new object[] { Decimal32.Parse("-4e-102"), "G", defaultFormat, "-0." + new string('0', 101) };
                yield return new object[] { Decimal32.Parse("-5e-102"), "G", defaultFormat, "-0." + new string('0', 101) };
                yield return new object[] { Decimal32.Parse("-5.000000000000001e-102"), "G", defaultFormat, "-0." + new string('0', 100) + "1" };
                yield return new object[] { Decimal32.Parse("-6e-102"), "G", defaultFormat, "-0." + new string('0', 100) + "1" };

            }
        }

        [Fact]
        public static void Test_ToString()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (object[] testdata in ToString_TestData())
                {
                    ToString((Decimal32)testdata[0], (string)testdata[1], (IFormatProvider)testdata[2], (string)testdata[3]);
                }
            }
        }

        private static void ToString(Decimal32 f, string format, IFormatProvider provider, string expected)
        {
            bool isDefaultProvider = provider == null;
            if (string.IsNullOrEmpty(format) || format.ToUpperInvariant() == "G")
            {
                if (isDefaultProvider)
                {
                    Assert.Equal(expected, f.ToString());
                    Assert.Equal(expected, f.ToString((IFormatProvider)null));
                }
                Assert.Equal(expected, f.ToString(provider));
            }
            if (isDefaultProvider)
            {
                Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant())); // If format is upper case, then exponents are printed in upper case
                Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant())); // If format is lower case, then exponents are printed in lower case
                Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant(), null));
                Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant(), null));
            }
            Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant(), provider));
            Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant(), provider));
        }

        [Theory]
        [MemberData(nameof(PositiveInfinity_NonCanonicalEncodings_TestData))]
        [MemberData(nameof(NegativeInfinity_NonCanonicalEncodings_TestData))]
        [MemberData(nameof(NaN_NonCanonicalEncodings_TestData))]
        public static void NaN_Infinity_NonCanonicalEncodings_Compare(Decimal32 d, uint encoding)
        {
            Decimal32 d2 = Unsafe.BitCast<uint, Decimal32>(encoding);
            Assert.Equal(0, d.CompareTo(d2));
            Assert.Equal(d.GetHashCode(), d2.GetHashCode());
        }

        public static IEnumerable<object[]> PositiveInfinity_NonCanonicalEncodings_TestData()
        {
            const uint canonical = 0x7800_0000;

            yield return new object[] { Decimal32.PositiveInfinity, canonical };
            yield return new object[] { Decimal32.PositiveInfinity, canonical | 0x0000_0001U };
            yield return new object[] { Decimal32.PositiveInfinity, canonical | 0x0200_0000U };
            yield return new object[] { Decimal32.PositiveInfinity, canonical | 0x0000_1234U };
            yield return new object[] { Decimal32.PositiveInfinity, canonical | 0x000F_FFFFU };
            yield return new object[] { Decimal32.PositiveInfinity, canonical | 0x03FF_FFFFU };
        }

        public static IEnumerable<object[]> NegativeInfinity_NonCanonicalEncodings_TestData()
        {
            const uint canonical = 0xF800_0000;

            yield return new object[] { Decimal32.NegativeInfinity, canonical };
            yield return new object[] { Decimal32.NegativeInfinity, canonical | 0x0000_0001U };
            yield return new object[] { Decimal32.NegativeInfinity, canonical | 0x0200_0000U };
            yield return new object[] { Decimal32.NegativeInfinity, canonical | 0x0000_1234U };
            yield return new object[] { Decimal32.NegativeInfinity, canonical | 0x000F_FFFFU };
            yield return new object[] { Decimal32.NegativeInfinity, canonical | 0x03FF_FFFFU };
        }

        public static IEnumerable<object[]> NaN_NonCanonicalEncodings_TestData()
        {
            const uint canonical = 0xFC00_0000;

            yield return new object[] { Decimal32.NaN, canonical };
            yield return new object[] { Decimal32.NaN, canonical | 0x0000_0001U };
            yield return new object[] { Decimal32.NaN, canonical | 0x0200_0000U };
            yield return new object[] { Decimal32.NaN, canonical | 0x0000_1234U };
            yield return new object[] { Decimal32.NaN, canonical | 0x000F_FFFFU };
            yield return new object[] { Decimal32.NaN, canonical | 0x03FF_FFFFU };
        }

        public static IEnumerable<object[]> Zero_NonCanonicalEncodings_TestData()
        {
            // A finite encoding whose significand exceeds MaxSignificand (9_999_999) is non-canonical and represents zero.
            const uint PositiveZero = 0x3280_0000;
            const uint NegativeZero = 0xB280_0000;

            yield return new object[] { PositiveZero, 0x6CBF_FFFFU }; // significand 0x9F_FFFF
            yield return new object[] { PositiveZero, 0x6CB8_9680U }; // significand 0x98_9680 (== 10_000_000)
            yield return new object[] { NegativeZero, 0xECBF_FFFFU };
            yield return new object[] { NegativeZero, 0xECB8_9680U };
        }

        [Theory]
        [MemberData(nameof(Zero_NonCanonicalEncodings_TestData))]
        public static void Finite_NonCanonicalEncodings_BehaveAsZero(uint canonicalZero, uint encoding)
        {
            Decimal32 zero = Unsafe.BitCast<uint, Decimal32>(canonicalZero);
            Decimal32 nc = Unsafe.BitCast<uint, Decimal32>(encoding);
            Decimal32 one = Unsafe.BitCast<uint, Decimal32>(0x3280_0001U);
            Decimal32 inf = Decimal32.PositiveInfinity;

            // A non-canonical finite encoding compares, hashes, and equals as zero.
            Assert.Equal(0, zero.CompareTo(nc));
            Assert.True(zero == nc);
            Assert.Equal(zero.GetHashCode(), nc.GetHashCode());

            // Arithmetic treats the non-canonical operand identically to canonical zero.
            Assert.Equal(Bits(zero + one), Bits(nc + one));
            Assert.Equal(Bits(zero - one), Bits(nc - one));
            Assert.Equal(Bits(one + zero), Bits(one + nc));
            Assert.Equal(Bits(zero * one), Bits(nc * one));
            Assert.Equal(Bits(one * zero), Bits(one * nc));
            Assert.Equal(Bits(zero / one), Bits(nc / one));

            // Division-by-zero and invalid operations must trigger for non-canonical zero.
            Assert.Equal(Bits(one / zero), Bits(one / nc)); // finite / 0 -> Infinity
            Assert.Equal(Bits(zero / zero), Bits(nc / nc)); // 0 / 0 -> NaN
            Assert.Equal(Bits(inf * zero), Bits(inf * nc)); // Infinity * 0 -> NaN

            static uint Bits(Decimal32 value) => Unsafe.BitCast<Decimal32, uint>(value);
        }

        public static IEnumerable<object[]> UnaryNegation_TestData()
        {
            yield return new object[] { 0x3280_0000U, 0xB280_0000U }; // +0 -> -0
            yield return new object[] { 0xB280_0000U, 0x3280_0000U }; // -0 -> +0
            yield return new object[] { 0x7800_0000U, 0xF800_0000U }; // +Infinity -> -Infinity
            yield return new object[] { 0xF800_0000U, 0x7800_0000U }; // -Infinity -> +Infinity
            yield return new object[] { 0xFC00_0000U, 0x7C00_0000U }; // NaN -> sign-flipped NaN
            yield return new object[] { 0x7C00_0000U, 0xFC00_0000U };
        }

        [Theory]
        [MemberData(nameof(UnaryNegation_TestData))]
        public static void op_UnaryNegation(uint value, uint expected)
        {
            Decimal32 result = -Unsafe.BitCast<uint, Decimal32>(value);
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(result));
        }

        [Theory]
        [InlineData("123")]
        [InlineData("-123")]
        [InlineData("4567.891")]
        [InlineData("-4567.891")]
        [InlineData("9.999999e96")]
        [InlineData("1e-101")]
        public static void op_UnaryNegation_FiniteRoundTrips(string value)
        {
            Decimal32 d = Decimal32.Parse(value, CultureInfo.InvariantCulture);
            Decimal32 negated = -d;

            uint dBits = Unsafe.BitCast<Decimal32, uint>(d);
            uint negatedBits = Unsafe.BitCast<Decimal32, uint>(negated);

            Assert.Equal(dBits ^ 0x8000_0000U, negatedBits); // only the sign bit differs
            Assert.Equal(dBits, Unsafe.BitCast<Decimal32, uint>(-negated)); // double negation is identity
        }

        [Theory]
        [InlineData(0x3280_0000U)] // +0
        [InlineData(0xB280_0000U)] // -0
        [InlineData(0x7800_0000U)] // +Infinity
        [InlineData(0xF800_0000U)] // -Infinity
        [InlineData(0xFC00_0000U)] // NaN
        [InlineData(0x00F4_9F87U)] // +9.999999e-90 (arbitrary finite)
        public static void op_UnaryPlus(uint value)
        {
            Decimal32 d = Unsafe.BitCast<uint, Decimal32>(value);
            Assert.Equal(value, Unsafe.BitCast<Decimal32, uint>(+d));
        }
        public static IEnumerable<object[]> op_Addition_TestData()
        {
            yield return new object[] { 0xFC000000U, 0x32800001U, 0xFC000000U }; // NaN + 1 -> NaN
            yield return new object[] { 0x32800001U, 0xFC000000U, 0xFC000000U }; // 1 + NaN -> NaN
            yield return new object[] { 0x78000000U, 0x32800001U, 0x78000000U }; // +Inf + 1 -> +Inf
            yield return new object[] { 0x32800001U, 0x78000000U, 0x78000000U }; // 1 + +Inf -> +Inf
            yield return new object[] { 0x78000000U, 0xF8000000U, 0x7C000000U }; // +Inf + -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xF8000000U, 0x78000000U, 0x7C000000U }; // -Inf + +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x78000000U, 0x78000000U, 0x78000000U }; // +Inf + +Inf -> +Inf
            yield return new object[] { 0xF8000000U, 0xF8000000U, 0xF8000000U }; // -Inf + -Inf -> -Inf
            yield return new object[] { 0x32800000U, 0x32800000U, 0x32800000U }; // +0 + +0 -> +0
            yield return new object[] { 0xB2800000U, 0xB2800000U, 0xB2800000U }; // -0 + -0 -> -0
            yield return new object[] { 0x32800000U, 0xB2800000U, 0x32800000U }; // +0 + -0 -> +0 (round-half-even)
            yield return new object[] { 0xB2800000U, 0x32800000U, 0x32800000U }; // -0 + +0 -> +0
            yield return new object[] { 0x32800001U, 0x32800000U, 0x32800001U }; // 1 + 0 -> 1
            yield return new object[] { 0x32800000U, 0x32800001U, 0x32800001U }; // 0 + 1 -> 1
            yield return new object[] { 0x78000002U, 0x32800001U, 0x78000000U }; // non-canonical +Inf + 1 -> canonical +Inf
            yield return new object[] { 0x32800001U, 0xF8000005U, 0xF8000000U }; // 1 + non-canonical -Inf -> canonical -Inf
            yield return new object[] { 0x7800000FU, 0xF8000003U, 0x7C000000U }; // non-canonical +Inf + non-canonical -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x32800001U, 0x32800002U, 0x32800003U }; // 1 + 2 -> 3
            yield return new object[] { 0x3200000FU, 0x32000019U, 0x32000028U }; // 1.5 + 2.5 -> 4.0
            yield return new object[] { 0x32000001U, 0x32000002U, 0x32000003U }; // 0.1 + 0.2 -> 0.3
            yield return new object[] { 0x32800001U, 0xB2800001U, 0x32800000U }; // 1 + -1 -> +0
            yield return new object[] { 0xB2800001U, 0x32800001U, 0x32800000U }; // -1 + 1 -> +0
            yield return new object[] { 0x6CB8967FU, 0x32800001U, 0x330F4240U }; // all-nines + 1 (carry/overflow to next magnitude)
            yield return new object[] { 0x6CB8967FU, 0x6CB8967FU, 0x331E8480U }; // big + big (round)
            yield return new object[] { 0x32800001U, 0x2F000001U, 0x2F8F4240U }; // 1 + 1e-P (alignment beyond precision)
            yield return new object[] { 0x32800001U, 0x2C800001U, 0x2F8F4240U }; // 1 + tiny (sticky rounding)
            yield return new object[] { 0x5F8F4240U, 0x5F8F4240U, 0x5F9E8480U }; // max-ish + max-ish (overflow to Inf)
            yield return new object[] { 0x2F92D687U, 0x2FF4CBB1U, 0x6BE7A238U }; // cohort / preferred exponent
            yield return new object[] { 0x32800064U, 0x31000001U, 0x310186A1U }; // 100 + 0.001 (exponent spread)
            yield return new object[] { 0x6CB8967FU, 0xECB8967EU, 0x32800001U }; // cancellation leaving small
            yield return new object[] { 0x2F2C8C35U, 0x2A00002DU, 0x2F2C8C35U };
            yield return new object[] { 0xB8002104U, 0xB98043D6U, 0xB89A82E5U };
            yield return new object[] { 0xA8800010U, 0x34802213U, 0x6CC51A38U };
            yield return new object[] { 0x318496C5U, 0xA71C20DCU, 0x312DE3B2U };
            yield return new object[] { 0x2E830F1EU, 0xA9000005U, 0x2E1E972CU };
            yield return new object[] { 0xAD00006CU, 0xB7011BA9U, 0xB66ECE04U };
            yield return new object[] { 0xAC000017U, 0x380B7DD1U, 0x37F2EA2AU };
            yield return new object[] { 0xBC800009U, 0xA600170BU, 0xEE695440U };
            yield return new object[] { 0x1C8C3773U, 0x4C800FA6U, 0x4B3D2070U };
            yield return new object[] { 0xB2000007U, 0xEAD57BF3U, 0xAF6ACFC0U };
            yield return new object[] { 0xBA000010U, 0x2F0ED152U, 0xB7986A00U };
            yield return new object[] { 0x6A41D272U, 0xA8800006U, 0x6A41D271U };
            yield return new object[] { 0xAC0000EBU, 0x28800027U, 0xAA23DBB0U };
            yield return new object[] { 0xAB8B81BBU, 0xB0000044U, 0xADE7C2CBU };
            yield return new object[] { 0x390001B3U, 0xB885B1AEU, 0xB885A0B0U };
            yield return new object[] { 0x3B800006U, 0x27007EF8U, 0x38DB8D80U };
            yield return new object[] { 0x27034AB4U, 0x3280024CU, 0x30D9B8C0U };
            yield return new object[] { 0x3B800094U, 0x3685C05CU, 0x39969540U };
            yield return new object[] { 0xC0001DD4U, 0x330000DDU, 0xBEF48420U };
            yield return new object[] { 0x378000FAU, 0x9A02AE5DU, 0x35A625A0U };
            yield return new object[] { 0x278014E4U, 0x3806111AU, 0x37BCAB04U };
            yield return new object[] { 0xB20ACE79U, 0x3204CA4CU, 0xB206042DU };
            yield return new object[] { 0xB98025B8U, 0xB2000002U, 0xEE1356C0U };
            yield return new object[] { 0xB780002BU, 0x3B80007DU, 0x399312D0U };
            yield return new object[] { 0xDA81A30AU, 0x38002124U, 0xDA105E64U };
            yield return new object[] { 0xB5800001U, 0x36000006U, 0x3580003BU };
            yield return new object[] { 0xB8800004U, 0x35297F01U, 0xB5B8E2B3U };
            yield return new object[] { 0xAF00252CU, 0xBF000006U, 0xBC5B8D80U };
            yield return new object[] { 0x92EA6215U, 0x38800002U, 0x359E8480U };
            yield return new object[] { 0xAE800008U, 0xAE00009FU, 0xAE0000EFU };
            yield return new object[] { 0x2A0025A7U, 0x35001CE0U, 0x33F0CB00U };
            yield return new object[] { 0x45800056U, 0x32000030U, 0x70C339C0U };
            yield return new object[] { 0x360DAA53U, 0xB20D6A8EU, 0x6D68A73EU };
            yield return new object[] { 0xAF800202U, 0xC000857DU, 0xBF3424D4U };
            yield return new object[] { 0xE1000004U, 0x2D00003DU, 0x2ADD1420U };
            yield return new object[] { 0xBF805674U, 0x6E377AE8U, 0xBEA1C550U };
            yield return new object[] { 0x2E0087F8U, 0x2F0E50D7U, 0x6BAF35FFU };
            yield return new object[] { 0x32800061U, 0x4D80016BU, 0x4BB763B0U };
            yield return new object[] { 0xB1FD9564U, 0x33800008U, 0xB1FC5CE4U };
            yield return new object[] { 0xA9000C89U, 0xC0000CD9U, 0xBEB22FA8U };
            yield return new object[] { 0x37001CE3U, 0x58095C61U, 0x57DD9BCAU };
            yield return new object[] { 0x2E8018F2U, 0xB98002B6U, 0xB7E9E560U };
            yield return new object[] { 0x400B2B74U, 0xAE0003ACU, 0x3FEFB288U };
            yield return new object[] { 0x2B000676U, 0xD1000009U, 0xF3895440U };
            yield return new object[] { 0xDD8E1313U, 0x33325AAEU, 0xF74CBEBEU };
            yield return new object[] { 0x3500008BU, 0x38800030U, 0x36493E01U };
            yield return new object[] { 0x338DB8E1U, 0xB9000023U, 0xB6B567DFU };
            yield return new object[] { 0xB700030FU, 0x2A807402U, 0xB57779F0U };
            yield return new object[] { 0xA68542F3U, 0x35000005U, 0x324C4B40U };
            yield return new object[] { 0x261B9CA7U, 0xB7000005U, 0xB44C4B40U };
            yield return new object[] { 0x33000007U, 0xB180033BU, 0x3180181DU };
            yield return new object[] { 0x0D80010EU, 0xBC000016U, 0xB9A191C0U };
            yield return new object[] { 0x310000DCU, 0xAC0583C6U, 0x2F2191C0U };
            yield return new object[] { 0x297CCD10U, 0x2A000006U, 0x297CCF68U };
            yield return new object[] { 0x15000001U, 0xA7010BBCU, 0xA6689570U };
            yield return new object[] { 0xAB8002D0U, 0xAA84EC44U, 0xAA860584U };
            yield return new object[] { 0xB4000180U, 0x38002093U, 0x36FF3E38U };
            yield return new object[] { 0x2A015FAAU, 0x3A000006U, 0x375B8D80U };
            yield return new object[] { 0x388002C5U, 0x2A04F49DU, 0x36EC2F50U };
            yield return new object[] { 0x35003DFEU, 0x35BB999DU, 0x35BB9FD0U };
        }

        [Theory]
        [MemberData(nameof(op_Addition_TestData))]
        public static void op_Addition(uint left, uint right, uint expected)
        {
            Decimal32 result = Unsafe.BitCast<uint, Decimal32>(left) + Unsafe.BitCast<uint, Decimal32>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(result));
        }

        public static IEnumerable<object[]> op_Subtraction_TestData()
        {
            yield return new object[] { 0xFC000000U, 0x32800001U, 0xFC000000U }; // NaN - 1 -> NaN
            yield return new object[] { 0x32800001U, 0xFC000000U, 0xFC000000U }; // 1 - NaN -> NaN
            yield return new object[] { 0x78000000U, 0x32800001U, 0x78000000U }; // +Inf - 1 -> +Inf
            yield return new object[] { 0x32800001U, 0x78000000U, 0xF8000000U }; // 1 - +Inf -> -Inf
            yield return new object[] { 0x78000000U, 0xF8000000U, 0x78000000U }; // +Inf - -Inf -> +Inf
            yield return new object[] { 0xF8000000U, 0x78000000U, 0xF8000000U }; // -Inf - +Inf -> -Inf
            yield return new object[] { 0x78000000U, 0x78000000U, 0x7C000000U }; // +Inf - +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xF8000000U, 0xF8000000U, 0x7C000000U }; // -Inf - -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x32800000U, 0x32800000U, 0x32800000U }; // +0 - +0 -> +0
            yield return new object[] { 0xB2800000U, 0xB2800000U, 0x32800000U }; // -0 - -0 -> +0 (round-half-even)
            yield return new object[] { 0x32800000U, 0xB2800000U, 0x32800000U }; // +0 - -0 -> +0
            yield return new object[] { 0xB2800000U, 0x32800000U, 0xB2800000U }; // -0 - +0 -> -0
            yield return new object[] { 0x32800001U, 0x32800000U, 0x32800001U }; // 1 - 0 -> 1
            yield return new object[] { 0x32800000U, 0x32800001U, 0xB2800001U }; // 0 - 1 -> -1
            yield return new object[] { 0x78000002U, 0x32800001U, 0x78000000U }; // non-canonical +Inf - 1 -> canonical +Inf
            yield return new object[] { 0x32800001U, 0xF8000005U, 0x78000000U }; // 1 - non-canonical -Inf -> canonical +Inf
            yield return new object[] { 0x7800000FU, 0xF8000003U, 0x78000000U }; // non-canonical +Inf - non-canonical -Inf -> canonical +Inf
            yield return new object[] { 0x32800001U, 0x32800002U, 0xB2800001U }; // 1 - 2 -> -1
            yield return new object[] { 0x3200000FU, 0x32000019U, 0xB200000AU }; // 1.5 - 2.5 -> -1.0
            yield return new object[] { 0x32000001U, 0x32000002U, 0xB2000001U }; // 0.1 - 0.2 -> -0.1
            yield return new object[] { 0x32800001U, 0xB2800001U, 0x32800002U }; // 1 - -1 -> 2
            yield return new object[] { 0xB2800001U, 0x32800001U, 0xB2800002U }; // -1 - 1 -> -2
            yield return new object[] { 0x6CB8967FU, 0x32800001U, 0x6CB8967EU }; // all-nines - 1 -> 9_999_998
            yield return new object[] { 0x6CB8967FU, 0x6CB8967FU, 0x32800000U }; // big - big -> +0
            yield return new object[] { 0x32800001U, 0x2F000001U, 0x6BD8967FU }; // 1 - 1e-7 -> 0.9999999 (alignment beyond precision)
            yield return new object[] { 0x32800001U, 0x2C800001U, 0x2F8F4240U }; // 1 - 1e-12 -> 1.000000 (sticky rounding)
            yield return new object[] { 0x5F8F4240U, 0x5F8F4240U, 0x5F800000U }; // max-ish - max-ish -> +0 (preferred exponent retained)
            yield return new object[] { 0x2F92D687U, 0x2FF4CBB1U, 0xAFE1F52AU }; // cohort / preferred exponent
            yield return new object[] { 0x32800064U, 0x31000001U, 0x3101869FU }; // 100 - 0.001 -> 99.999 (exponent spread)
            yield return new object[] { 0x6CB8967FU, 0xECB8967EU, 0x331E8480U }; // opposite signs subtract as add: 9_999_999 - -9_999_998 -> 2.0e7 (carry/rounding)
            yield return new object[] { 0x2F2C8C35U, 0x2A00002DU, 0x2F2C8C35U };
            yield return new object[] { 0xB8002104U, 0xB98043D6U, 0x389A7C4BU };
            yield return new object[] { 0xA8800010U, 0x34802213U, 0xECC51A38U };
            yield return new object[] { 0x318496C5U, 0xA71C20DCU, 0x312DE3B2U };
            yield return new object[] { 0x2E830F1EU, 0xA9000005U, 0x2E1E972CU };
            yield return new object[] { 0xAD00006CU, 0xB7011BA9U, 0x366ECE04U };
            yield return new object[] { 0xAC000017U, 0x380B7DD1U, 0xB7F2EA2AU };
            yield return new object[] { 0xBC800009U, 0xA600170BU, 0xEE695440U };
            yield return new object[] { 0x1C8C3773U, 0x4C800FA6U, 0xCB3D2070U };
            yield return new object[] { 0xB2000007U, 0xEAD57BF3U, 0xAF6ACFC0U };
            yield return new object[] { 0xBA000010U, 0x2F0ED152U, 0xB7986A00U };
            yield return new object[] { 0x6A41D272U, 0xA8800006U, 0x6A41D273U };
            yield return new object[] { 0xAC0000EBU, 0x28800027U, 0xAA23DBB0U };
            yield return new object[] { 0xAB8B81BBU, 0xB0000044U, 0x2DE7C235U };
            yield return new object[] { 0x390001B3U, 0xB885B1AEU, 0x3885C2ACU };
            yield return new object[] { 0x3B800006U, 0x27007EF8U, 0x38DB8D80U };
            yield return new object[] { 0x27034AB4U, 0x3280024CU, 0xB0D9B8C0U };
            yield return new object[] { 0x3B800094U, 0x3685C05CU, 0x39969540U };
            yield return new object[] { 0xC0001DD4U, 0x330000DDU, 0xBEF48420U };
            yield return new object[] { 0x378000FAU, 0x9A02AE5DU, 0x35A625A0U };
            yield return new object[] { 0x278014E4U, 0x3806111AU, 0xB7BCAB04U };
            yield return new object[] { 0xB20ACE79U, 0x3204CA4CU, 0xB20F98C5U };
            yield return new object[] { 0xB98025B8U, 0xB2000002U, 0xEE1356C0U };
            yield return new object[] { 0xB780002BU, 0x3B80007DU, 0xB99312D0U };
            yield return new object[] { 0xDA81A30AU, 0x38002124U, 0xDA105E64U };
            yield return new object[] { 0xB5800001U, 0x36000006U, 0xB580003DU };
            yield return new object[] { 0xB8800004U, 0x35297F01U, 0xB5C12F4DU };
            yield return new object[] { 0xAF00252CU, 0xBF000006U, 0x3C5B8D80U };
            yield return new object[] { 0x92EA6215U, 0x38800002U, 0xB59E8480U };
            yield return new object[] { 0xAE800008U, 0xAE00009FU, 0x2E00004FU };
            yield return new object[] { 0x2A0025A7U, 0x35001CE0U, 0xB3F0CB00U };
            yield return new object[] { 0x45800056U, 0x32000030U, 0x70C339C0U };
            yield return new object[] { 0x360DAA53U, 0xB20D6A8EU, 0x6D68A73EU };
            yield return new object[] { 0xAF800202U, 0xC000857DU, 0x3F3424D4U };
            yield return new object[] { 0xE1000004U, 0x2D00003DU, 0xAADD1420U };
            yield return new object[] { 0xBF805674U, 0x6E377AE8U, 0xBEA1C550U };
            yield return new object[] { 0x2E0087F8U, 0x2F0E50D7U, 0xEBAF1ACDU };
            yield return new object[] { 0x32800061U, 0x4D80016BU, 0xCBB763B0U };
            yield return new object[] { 0xB1FD9564U, 0x33800008U, 0xB1FECDE4U };
            yield return new object[] { 0xA9000C89U, 0xC0000CD9U, 0x3EB22FA8U };
            yield return new object[] { 0x37001CE3U, 0x58095C61U, 0xD7DD9BCAU };
            yield return new object[] { 0x2E8018F2U, 0xB98002B6U, 0x37E9E560U };
            yield return new object[] { 0x400B2B74U, 0xAE0003ACU, 0x3FEFB288U };
            yield return new object[] { 0x2B000676U, 0xD1000009U, 0x73895440U };
            yield return new object[] { 0xDD8E1313U, 0x33325AAEU, 0xF74CBEBEU };
            yield return new object[] { 0x3500008BU, 0x38800030U, 0xB6493DFFU };
            yield return new object[] { 0x338DB8E1U, 0xB9000023U, 0x36B567E1U };
            yield return new object[] { 0xB700030FU, 0x2A807402U, 0xB57779F0U };
            yield return new object[] { 0xA68542F3U, 0x35000005U, 0xB24C4B40U };
            yield return new object[] { 0x261B9CA7U, 0xB7000005U, 0x344C4B40U };
            yield return new object[] { 0x33000007U, 0xB180033BU, 0x31801E93U };
            yield return new object[] { 0x0D80010EU, 0xBC000016U, 0x39A191C0U };
            yield return new object[] { 0x310000DCU, 0xAC0583C6U, 0x2F2191C0U };
            yield return new object[] { 0x297CCD10U, 0x2A000006U, 0x297CCAB8U };
            yield return new object[] { 0x15000001U, 0xA7010BBCU, 0x26689570U };
            yield return new object[] { 0xAB8002D0U, 0xAA84EC44U, 0x2A83D304U };
            yield return new object[] { 0xB4000180U, 0x38002093U, 0xB6FF3E38U };
            yield return new object[] { 0x2A015FAAU, 0x3A000006U, 0xB75B8D80U };
            yield return new object[] { 0x388002C5U, 0x2A04F49DU, 0x36EC2F50U };
            yield return new object[] { 0x35003DFEU, 0x35BB999DU, 0xB5BB936AU };
        }

        [Theory]
        [MemberData(nameof(op_Subtraction_TestData))]
        public static void op_Subtraction(uint left, uint right, uint expected)
        {
            Decimal32 result = Unsafe.BitCast<uint, Decimal32>(left) - Unsafe.BitCast<uint, Decimal32>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(result));
        }

        public static IEnumerable<object[]> op_Equality_TestData()
        {
            yield return new object[] { 0xFC000000U, 0xFC000000U, false }; // NaN == NaN -> false
            yield return new object[] { 0xFC000000U, 0x32800001U, false }; // NaN == 1 -> false
            yield return new object[] { 0x32800001U, 0xFC000000U, false }; // 1 == NaN -> false
            yield return new object[] { 0xFC000000U, 0x78000000U, false }; // NaN == +Inf -> false
            yield return new object[] { 0xFE000000U, 0xFE000000U, false }; // sNaN == sNaN -> false
            yield return new object[] { 0xFE000000U, 0x32800001U, false }; // sNaN == 1 -> false
            yield return new object[] { 0x78000000U, 0x78000000U, true }; // +Inf == +Inf -> true
            yield return new object[] { 0xF8000000U, 0xF8000000U, true }; // -Inf == -Inf -> true
            yield return new object[] { 0x78000000U, 0xF8000000U, false }; // +Inf == -Inf -> false
            yield return new object[] { 0x78000000U, 0x32800001U, false }; // +Inf == 1 -> false
            yield return new object[] { 0x78000002U, 0x78000000U, true }; // non-canonical +Inf == +Inf -> true
            yield return new object[] { 0xF8000005U, 0xF8000000U, true }; // non-canonical -Inf == -Inf -> true
            yield return new object[] { 0x32800000U, 0xB2800000U, true }; // +0 == -0 -> true
            yield return new object[] { 0xB2800000U, 0x32800000U, true }; // -0 == +0 -> true
            yield return new object[] { 0x32800000U, 0x32800000U, true }; // +0 == +0 -> true
            yield return new object[] { 0x32800001U, 0x32800001U, true }; // 1 == 1 -> true
            yield return new object[] { 0x32800001U, 0xB2800001U, false }; // 1 == -1 -> false
            yield return new object[] { 0xB2800001U, 0x32800001U, false }; // -1 == 1 -> false
            yield return new object[] { 0x32800001U, 0x3200000AU, true }; // 1 == 1.0 -> true (cohort)
            yield return new object[] { 0x3200000AU, 0x32800001U, true }; // 1.0 == 1 -> true (cohort)
            yield return new object[] { 0x31800064U, 0x3200000AU, true }; // 1.00 == 1.0 -> true (cohort)
            yield return new object[] { 0x32800064U, 0x33800001U, true }; // 100 == 1e2 -> true (cohort)
            yield return new object[] { 0x3280000AU, 0x32800001U, false }; // 10 == 1 -> false
            yield return new object[] { 0x5F8F4240U, 0x5F8F4240U, true }; // large == large -> true
            yield return new object[] { 0x6CB8967FU, 0x6CB8967FU, true }; // all-nines == all-nines -> true
            yield return new object[] { 0x6CB8967FU, 0x6CB8967EU, false }; // all-nines == near -> false
            yield return new object[] { 0x32000001U, 0x32000001U, true }; // 0.1 == 0.1 -> true
            yield return new object[] { 0xB2000001U, 0x32000001U, false }; // -0.1 == 0.1 -> false
            yield return new object[] { 0x35000000U, 0x32800000U, true }; // +0 (exp 5) == +0 -> true (zero cohort)
            yield return new object[] { 0x2F2C8C35U, 0xBD800001U, false };
            yield return new object[] { 0x2A80BFA3U, 0x2E80F146U, false };
            yield return new object[] { 0xEDA7AB71U, 0x33800054U, false };
            yield return new object[] { 0xA71C20DCU, 0x2C8C18CBU, false };
            yield return new object[] { 0xB48003B6U, 0xB3817318U, true };
            yield return new object[] { 0xB980002FU, 0x4CC66EF1U, false };
            yield return new object[] { 0x3171BC43U, 0x28803E6EU, false };
            yield return new object[] { 0xB9855F56U, 0xB935B95CU, true };
            yield return new object[] { 0xA80003C8U, 0xA7017A20U, true };
            yield return new object[] { 0x08000005U, 0x070001F4U, true };
            yield return new object[] { 0x3A000116U, 0xBA000010U, false };
            yield return new object[] { 0x2F0ED152U, 0x8E003D25U, false };
            yield return new object[] { 0x2E80ADBAU, 0x29800030U, false };
            yield return new object[] { 0xAD80005DU, 0xAC802454U, true };
            yield return new object[] { 0x33415C3BU, 0xB80006F3U, false };
            yield return new object[] { 0x3B800006U, 0x58802513U, false };
            yield return new object[] { 0x27034AB4U, 0x26A0EB08U, true };
            yield return new object[] { 0x30DA8988U, 0xB187B561U, false };
            yield return new object[] { 0xB308524FU, 0xB2D33716U, true };
            yield return new object[] { 0xB0001165U, 0xAF80ADF2U, true };
            yield return new object[] { 0x168020CDU, 0x158CD014U, true };
            yield return new object[] { 0x278014E4U, 0xAD00004EU, false };
            yield return new object[] { 0xB6F4EDE0U, 0xBB000007U, false };
            yield return new object[] { 0xB2000002U, 0xB10000C8U, true };
            yield return new object[] { 0xA987E16CU, 0xA94ECE38U, true };
            yield return new object[] { 0xA9006A4FU, 0xA8842716U, true };
            yield return new object[] { 0x28000036U, 0x27001518U, true };
            yield return new object[] { 0xAE800293U, 0xAD81016CU, true };
            yield return new object[] { 0x36000006U, 0xCA87FEFCU, false };
            yield return new object[] { 0x33011190U, 0xAC001D41U, false };
            yield return new object[] { 0x5C800033U, 0xAA006FD9U, false };
            yield return new object[] { 0xBA800003U, 0xB980012CU, true };
            yield return new object[] { 0x3A80001BU, 0x39006978U, true };
            yield return new object[] { 0x3409DBACU, 0x33E294B8U, true };
            yield return new object[] { 0x6DD212C6U, 0xB2002198U, false };
            yield return new object[] { 0x4652D371U, 0x2F010631U, false };
            yield return new object[] { 0xB48002F6U, 0xB3812818U, true };
            yield return new object[] { 0xB98003C0U, 0xB80EA600U, true };
            yield return new object[] { 0x49800395U, 0x48816634U, true };
            yield return new object[] { 0x34800368U, 0x2E800341U, false };
            yield return new object[] { 0xB80000CEU, 0xB7005078U, true };
            yield return new object[] { 0x39800003U, 0xB300195BU, false };
            yield return new object[] { 0x33800008U, 0x33000050U, true };
            yield return new object[] { 0x41800251U, 0x4080E7A4U, true };
            yield return new object[] { 0x358001C7U, 0xAF000009U, false };
            yield return new object[] { 0xB2000F58U, 0xB70EF86AU, false };
            yield return new object[] { 0x400B2B74U, 0x3FEFB288U, true };
            yield return new object[] { 0x28B41FEFU, 0xD1000009U, false };
            yield return new object[] { 0xDD8E1313U, 0x30001D6CU, false };
            yield return new object[] { 0xB06FD858U, 0xA888673CU, false };
            yield return new object[] { 0xB80004B6U, 0xB700011AU, false };
            yield return new object[] { 0x2A807402U, 0x3180012EU, false };
            yield return new object[] { 0x180002F8U, 0x17801DB0U, true };
            yield return new object[] { 0x94800019U, 0x938009C4U, true };
            yield return new object[] { 0xB000F1D7U, 0xAF5E77FCU, true };
            yield return new object[] { 0xAA800003U, 0xB7000051U, false };
            yield return new object[] { 0x3180001AU, 0xB60002EEU, false };
            yield return new object[] { 0x07D3490CU, 0x2B80D4D0U, false };
            yield return new object[] { 0xAF000001U, 0xAE000064U, true };
            yield return new object[] { 0xAB8002D0U, 0xB0800377U, false };
        }

        [Theory]
        [MemberData(nameof(op_Equality_TestData))]
        public static void op_Equality(uint left, uint right, bool expected)
        {
            Decimal32 l = Unsafe.BitCast<uint, Decimal32>(left);
            Decimal32 r = Unsafe.BitCast<uint, Decimal32>(right);
            Assert.Equal(expected, l == r);
            Assert.Equal(!expected, l != r);
        }

        public static IEnumerable<object[]> op_Comparison_TestData()
        {
            yield return new object[] { 0xFC000000U, 0x32800001U, false, false, false, false }; // NaN vs 1 -> unordered
            yield return new object[] { 0x32800001U, 0xFC000000U, false, false, false, false }; // 1 vs NaN -> unordered
            yield return new object[] { 0xFC000000U, 0xFC000000U, false, false, false, false }; // NaN vs NaN -> unordered
            yield return new object[] { 0xFE000000U, 0x32800001U, false, false, false, false }; // sNaN vs 1 -> unordered
            yield return new object[] { 0xFC000000U, 0x78000000U, false, false, false, false }; // NaN vs +Inf -> unordered
            yield return new object[] { 0x78000000U, 0x32800001U, false, true, false, true }; // +Inf vs 1
            yield return new object[] { 0x32800001U, 0x78000000U, true, false, true, false }; // 1 vs +Inf
            yield return new object[] { 0xF8000000U, 0x32800001U, true, false, true, false }; // -Inf vs 1
            yield return new object[] { 0x32800001U, 0xF8000000U, false, true, false, true }; // 1 vs -Inf
            yield return new object[] { 0xF8000000U, 0x78000000U, true, false, true, false }; // -Inf vs +Inf
            yield return new object[] { 0x78000000U, 0x78000000U, false, false, true, true }; // +Inf vs +Inf (equal)
            yield return new object[] { 0xF8000000U, 0xF8000000U, false, false, true, true }; // -Inf vs -Inf (equal)
            yield return new object[] { 0x78000002U, 0x78000000U, false, false, true, true }; // non-canonical +Inf vs +Inf (equal)
            yield return new object[] { 0x32800000U, 0xB2800000U, false, false, true, true }; // +0 vs -0 (equal)
            yield return new object[] { 0xB2800000U, 0x32800000U, false, false, true, true }; // -0 vs +0 (equal)
            yield return new object[] { 0x32800000U, 0x32800001U, true, false, true, false }; // 0 vs 1
            yield return new object[] { 0xB2800001U, 0x32800000U, true, false, true, false }; // -1 vs 0
            yield return new object[] { 0x32800001U, 0x32800002U, true, false, true, false }; // 1 vs 2
            yield return new object[] { 0x32800002U, 0x32800001U, false, true, false, true }; // 2 vs 1
            yield return new object[] { 0xB2800001U, 0x32800001U, true, false, true, false }; // -1 vs 1
            yield return new object[] { 0x32800001U, 0xB2800001U, false, true, false, true }; // 1 vs -1
            yield return new object[] { 0xB2800001U, 0xB2800002U, false, true, false, true }; // -1 vs -2
            yield return new object[] { 0x32800001U, 0x3200000AU, false, false, true, true }; // 1 vs 1.0 (cohort equal)
            yield return new object[] { 0x3200000AU, 0x32800001U, false, false, true, true }; // 1.0 vs 1 (cohort equal)
            yield return new object[] { 0x32800064U, 0x33800001U, false, false, true, true }; // 100 vs 1e2 (cohort equal)
            yield return new object[] { 0x3280000AU, 0x32800001U, false, true, false, true }; // 10 vs 1
            yield return new object[] { 0x32000001U, 0x32000002U, true, false, true, false }; // 0.1 vs 0.2
            yield return new object[] { 0x6BF8967FU, 0x6BF8967EU, false, true, false, true }; // close values
            yield return new object[] { 0x6CB8967FU, 0x6CB8967EU, false, true, false, true }; // all-nines vs near
            yield return new object[] { 0x5F8F4240U, 0x5F8186A0U, false, true, false, true }; // large magnitudes
            yield return new object[] { 0xDF8F4240U, 0x5F8F4240U, true, false, true, false }; // -large vs +large
            yield return new object[] { 0x2F2C8C35U, 0xBD800001U, false, true, false, true };
            yield return new object[] { 0x2A80BFA3U, 0x2E80F146U, true, false, true, false };
            yield return new object[] { 0xEDA7AB71U, 0x33800054U, true, false, true, false };
            yield return new object[] { 0xA71C20DCU, 0x2C8C18CBU, true, false, true, false };
            yield return new object[] { 0xB48003B6U, 0xB3817318U, false, false, true, true };
            yield return new object[] { 0xB980002FU, 0x4CC66EF1U, true, false, true, false };
            yield return new object[] { 0x3171BC43U, 0x28803E6EU, false, true, false, true };
            yield return new object[] { 0xB9855F56U, 0xB935B95CU, false, false, true, true };
            yield return new object[] { 0xA80003C8U, 0xA7017A20U, false, false, true, true };
            yield return new object[] { 0x08000005U, 0x39801340U, true, false, true, false };
            yield return new object[] { 0xCA000C33U, 0x29000379U, true, false, true, false };
            yield return new object[] { 0x33000001U, 0x31800012U, false, true, false, true };
            yield return new object[] { 0x36000003U, 0x340C7831U, true, false, true, false };
            yield return new object[] { 0xAE808F1FU, 0x3782B3D3U, true, false, true, false };
            yield return new object[] { 0xB885B1AEU, 0xB838F0CCU, false, false, true, true };
            yield return new object[] { 0x2A80014CU, 0x29800003U, false, true, false, true };
            yield return new object[] { 0x3280024CU, 0x3A083505U, true, false, true, false };
            yield return new object[] { 0x3685C05CU, 0x31000002U, false, true, false, true };
            yield return new object[] { 0x300090CAU, 0x6543374AU, false, true, false, true };
            yield return new object[] { 0xB4800019U, 0xAD847994U, true, false, true, false };
            yield return new object[] { 0xAD00004EU, 0x33001329U, true, false, true, false };
            yield return new object[] { 0xB98025B8U, 0xB88EBBE0U, false, false, true, true };
            yield return new object[] { 0xAF8E5E1FU, 0xEBCFAD36U, false, false, true, true };
            yield return new object[] { 0xA987E16CU, 0xA94ECE38U, false, false, true, true };
            yield return new object[] { 0xA9006A4FU, 0x30000002U, true, false, true, false };
            yield return new object[] { 0xB2950EABU, 0x27802665U, true, false, true, false };
            yield return new object[] { 0xBA800028U, 0xAA05B8ADU, true, false, true, false };
            yield return new object[] { 0xB8013769U, 0xB779A504U, false, false, true, true };
            yield return new object[] { 0xB680005AU, 0xB6000384U, false, false, true, true };
            yield return new object[] { 0x2B000471U, 0xAE800008U, false, true, false, true };
            yield return new object[] { 0xAE00009FU, 0xDD9AF653U, false, true, false, true };
            yield return new object[] { 0x3A800006U, 0x3A00003CU, false, false, true, true };
            yield return new object[] { 0xB2002198U, 0xB9000008U, false, true, false, true };
            yield return new object[] { 0xA8000116U, 0x2E80D42FU, true, false, true, false };
            yield return new object[] { 0x8C801CF2U, 0x8B711150U, false, false, true, true };
            yield return new object[] { 0x49800395U, 0x48816634U, false, false, true, true };
            yield return new object[] { 0x34800368U, 0x2E800341U, false, true, false, true };
            yield return new object[] { 0xB80000CEU, 0xB7005078U, false, false, true, true };
            yield return new object[] { 0x39800003U, 0xB300195BU, false, true, false, true };
            yield return new object[] { 0x33800008U, 0xB7000028U, false, true, false, true };
            yield return new object[] { 0xBF033649U, 0x35F38F19U, true, false, true, false };
            yield return new object[] { 0x58095C61U, 0xB0BD637DU, false, true, false, true };
            yield return new object[] { 0x6BB0F7CAU, 0x36013C4DU, true, false, true, false };
            yield return new object[] { 0x530001F8U, 0xD1000009U, false, true, false, true };
            yield return new object[] { 0xDD8E1313U, 0x30001D6CU, true, false, true, false };
            yield return new object[] { 0xB06FD858U, 0xA888673CU, true, false, true, false };
            yield return new object[] { 0xB80004B6U, 0xB700011AU, true, false, true, false };
            yield return new object[] { 0x2A807402U, 0x3180012EU, true, false, true, false };
            yield return new object[] { 0x180002F8U, 0x2E000002U, true, false, true, false };
            yield return new object[] { 0xCC801631U, 0xB180033BU, true, false, true, false };
            yield return new object[] { 0x0D80010EU, 0xBB800002U, false, true, false, true };
            yield return new object[] { 0x310000DCU, 0xB60002EEU, false, true, false, true };
            yield return new object[] { 0x07D3490CU, 0x2B0EF5D7U, true, false, true, false };
            yield return new object[] { 0xAF000001U, 0xAE000064U, false, false, true, true };
            yield return new object[] { 0xAB8002D0U, 0xB0800377U, false, true, false, true };
            yield return new object[] { 0xB6802125U, 0xEDA8258FU, false, true, false, true };
            yield return new object[] { 0xAC000001U, 0xAB0DD4BDU, false, true, false, true };
            yield return new object[] { 0x34011190U, 0x2A00159EU, false, true, false, true };
            yield return new object[] { 0xAD01FF37U, 0x37837CF9U, true, false, true, false };
            yield return new object[] { 0xB58001C6U, 0x2D000A26U, true, false, true, false };
        }

        [Theory]
        [MemberData(nameof(op_Comparison_TestData))]
        public static void op_Comparison(uint left, uint right, bool lessThan, bool greaterThan, bool lessThanOrEqual, bool greaterThanOrEqual)
        {
            Decimal32 l = Unsafe.BitCast<uint, Decimal32>(left);
            Decimal32 r = Unsafe.BitCast<uint, Decimal32>(right);
            Assert.Equal(lessThan, l < r);
            Assert.Equal(greaterThan, l > r);
            Assert.Equal(lessThanOrEqual, l <= r);
            Assert.Equal(greaterThanOrEqual, l >= r);
        }

        public static IEnumerable<object[]> op_Multiply_TestData()
        {
            yield return new object[] { 0xFC000000U, 0x32800001U, 0xFC000000U }; // NaN * 1 -> NaN
            yield return new object[] { 0x32800001U, 0xFC000000U, 0xFC000000U }; // 1 * NaN -> NaN
            yield return new object[] { 0xFC000000U, 0x78000000U, 0xFC000000U }; // NaN * +Inf -> NaN
            yield return new object[] { 0x78000000U, 0x32800000U, 0x7C000000U }; // +Inf * +0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x32800000U, 0x78000000U, 0x7C000000U }; // +0 * +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xF8000000U, 0x32800000U, 0x7C000000U }; // -Inf * +0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x78000000U, 0xB2800000U, 0x7C000000U }; // +Inf * -0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x78000000U, 0x32800001U, 0x78000000U }; // +Inf * 1 -> +Inf
            yield return new object[] { 0x78000000U, 0xB2800001U, 0xF8000000U }; // +Inf * -1 -> -Inf
            yield return new object[] { 0xF8000000U, 0x32800002U, 0xF8000000U }; // -Inf * 2 -> -Inf
            yield return new object[] { 0xF8000000U, 0xB2800002U, 0x78000000U }; // -Inf * -2 -> +Inf
            yield return new object[] { 0x78000000U, 0x78000000U, 0x78000000U }; // +Inf * +Inf -> +Inf
            yield return new object[] { 0x78000000U, 0xF8000000U, 0xF8000000U }; // +Inf * -Inf -> -Inf
            yield return new object[] { 0xF8000000U, 0xF8000000U, 0x78000000U }; // -Inf * -Inf -> +Inf
            yield return new object[] { 0x78000002U, 0x32800001U, 0x78000000U }; // non-canonical +Inf * 1 -> canonical +Inf
            yield return new object[] { 0x32800001U, 0xF8000005U, 0xF8000000U }; // 1 * non-canonical -Inf -> canonical -Inf
            yield return new object[] { 0x32800000U, 0x32800000U, 0x32800000U }; // +0 * +0 -> +0
            yield return new object[] { 0xB2800000U, 0xB2800000U, 0x32800000U }; // -0 * -0 -> +0
            yield return new object[] { 0x32800000U, 0xB2800000U, 0xB2800000U }; // +0 * -0 -> -0
            yield return new object[] { 0x32800001U, 0xB2800000U, 0xB2800000U }; // 1 * -0 -> -0
            yield return new object[] { 0xB2800001U, 0x32800000U, 0xB2800000U }; // -1 * +0 -> -0
            yield return new object[] { 0x35000000U, 0x34000000U, 0x36800000U }; // 0e5 * 0e3 -> 0e8 (preferred exp)
            yield return new object[] { 0x30000000U, 0x3280000CU, 0x30000000U }; // 0e-5 * 12 -> 0e-5 (preferred exp)
            yield return new object[] { 0x32800002U, 0x32800005U, 0x3280000AU }; // 2 * 5 -> 10 (exp 0, trailing zero retained)
            yield return new object[] { 0x32800002U, 0x32000005U, 0x3200000AU }; // 2 * 0.5 -> 1.0
            yield return new object[] { 0x3200000FU, 0x32800002U, 0x3200001EU }; // 1.5 * 2 -> 3.0
            yield return new object[] { 0x32000001U, 0x32000001U, 0x31800001U }; // 0.1 * 0.1 -> 0.01
            yield return new object[] { 0x3200000CU, 0x3200000CU, 0x31800090U }; // 1.2 * 1.2 -> 1.44
            yield return new object[] { 0xB2800003U, 0x32800007U, 0xB2800015U }; // -3 * 7 -> -21
            yield return new object[] { 0x3280000CU, 0x3280000CU, 0x32800090U }; // 12 * 12 -> 144
            yield return new object[] { 0x6CB8967FU, 0x6CB8967FU, 0x6D98967EU }; // all-nines squared (round)
            yield return new object[] { 0x2F945855U, 0x2F9B2071U, 0x2FA42B41U }; // full-precision * full-precision (round)
            yield return new object[] { 0x328F4240U, 0x328F4240U, 0x358F4240U }; // 10^(P-1) * 10^(P-1) (trailing zeros beyond precision)
            yield return new object[] { 0x77E95440U, 0x77E95440U, 0x78000000U }; // huge * huge -> +Inf (overflow)
            yield return new object[] { 0xF7E95440U, 0x77E95440U, 0xF8000000U }; // -huge * huge -> -Inf (overflow)
            yield return new object[] { 0x02800001U, 0x02800001U, 0x00000000U }; // tiny * tiny -> underflow
            yield return new object[] { 0x03000001U, 0x30000001U, 0x00800001U }; // small * small (subnormal-ish)
            yield return new object[] { 0x6618967FU, 0x6638967FU, 0x6078967EU }; // wide product just below min quantum (normal after single rounding)
            yield return new object[] { 0x65B8967FU, 0x65D8967FU, 0x00002710U }; // wide product deep in the subnormal range
            yield return new object[] { 0x60B8967FU, 0x60B8967FU, 0x00000000U }; // wide product underflow to zero/epsilon
            yield return new object[] { 0x5F800000U, 0x5F800005U, 0x5F800000U }; // zero * finite, preferred exponent far above max quantum (clamped)
            yield return new object[] { 0xDF800000U, 0x5F800005U, 0xDF800000U }; // -zero * finite, preferred exponent clamped (sign preserved)
            yield return new object[] { 0x33000000U, 0x5F800005U, 0x5F800000U }; // zero * finite, preferred exponent one above max quantum (clamped)
            yield return new object[] { 0x32800000U, 0x5F800005U, 0x5F800000U }; // zero * finite, preferred exponent exactly at max quantum (no clamp)
            yield return new object[] { 0x2F2C8C35U, 0x2A00002DU, 0x27940BE5U };
            yield return new object[] { 0xB8002104U, 0xB98043D6U, 0x4016657EU };
            yield return new object[] { 0xA8800010U, 0x34802213U, 0xAA822130U };
            yield return new object[] { 0x318496C5U, 0xA71C20DCU, 0xA8D497F0U };
            yield return new object[] { 0x2E830F1EU, 0xA9000005U, 0xA50F4B96U };
            yield return new object[] { 0xAD00006CU, 0xB7011BA9U, 0x31F7AB4CU };
            yield return new object[] { 0xAC000017U, 0x380B7DD1U, 0xB21A6E2EU };
            yield return new object[] { 0xBC800009U, 0xA600170BU, 0x3000CF63U };
            yield return new object[] { 0x1C8C3773U, 0x4C800FA6U, 0x3830F090U };
            yield return new object[] { 0xB2000007U, 0xEAD57BF3U, 0x2B68A390U };
            yield return new object[] { 0xBA000010U, 0x2F0ED152U, 0xB717B550U };
            yield return new object[] { 0x6A41D272U, 0xA8800006U, 0x9FCDE4ABU };
            yield return new object[] { 0xAC0000EBU, 0x28800027U, 0xA20023CDU };
            yield return new object[] { 0xAB8B81BBU, 0xB0000044U, 0x29CE3EF8U };
            yield return new object[] { 0x390001B3U, 0xB885B1AEU, 0xC018C4E8U };
            yield return new object[] { 0x3B800006U, 0x27007EF8U, 0x3002F9D0U };
            yield return new object[] { 0x27034AB4U, 0x3280024CU, 0x28135B18U };
            yield return new object[] { 0x3B800094U, 0x3685C05CU, 0x40551EEBU };
            yield return new object[] { 0xC0001DD4U, 0x330000DDU, 0xC099C004U };
            yield return new object[] { 0x378000FAU, 0x9A02AE5DU, 0x9FC30715U };
            yield return new object[] { 0x278014E4U, 0x3806111AU, 0x2EA071FDU };
            yield return new object[] { 0xB20ACE79U, 0x3204CA4CU, 0xB421ECD8U };
            yield return new object[] { 0xB98025B8U, 0xB2000002U, 0x39004B70U };
            yield return new object[] { 0xB780002BU, 0x3B80007DU, 0xC08014FFU };
            yield return new object[] { 0xDA81A30AU, 0x38002124U, 0xF8000000U };
            yield return new object[] { 0xB5800001U, 0x36000006U, 0xB9000006U };
            yield return new object[] { 0xB8800004U, 0x35297F01U, 0xBB909934U };
            yield return new object[] { 0xAF00252CU, 0xBF000006U, 0x3B80DF08U };
            yield return new object[] { 0x92EA6215U, 0x38800002U, 0x991546D1U };
            yield return new object[] { 0xAE800008U, 0xAE00009FU, 0x2A0004F8U };
            yield return new object[] { 0x2A0025A7U, 0x35001CE0U, 0x2D6CB89DU };
            yield return new object[] { 0x45800056U, 0x32000030U, 0x45001020U };
            yield return new object[] { 0x360DAA53U, 0xB20D6A8EU, 0xB87826E0U };
            yield return new object[] { 0xAF800202U, 0xC000857DU, 0x3D9ACD4CU };
            yield return new object[] { 0xE1000004U, 0x2D00003DU, 0x8007CED9U };
            yield return new object[] { 0xBF805674U, 0x6E377AE8U, 0xC821868CU };
            yield return new object[] { 0x2E0087F8U, 0x2F0E50D7U, 0x2CB1D493U };
            yield return new object[] { 0x32800061U, 0x4D80016BU, 0x4D80898BU };
            yield return new object[] { 0xB1FD9564U, 0x33800008U, 0xB3647783U };
            yield return new object[] { 0xA9000C89U, 0xC0000CD9U, 0x37101AD0U };
            yield return new object[] { 0x37001CE3U, 0x58095C61U, 0x5E453939U };
            yield return new object[] { 0x2E8018F2U, 0xB98002B6U, 0xB5C3A00CU };
            yield return new object[] { 0x400B2B74U, 0xAE0003ACU, 0xBCE8FEDCU };
            yield return new object[] { 0x2B000676U, 0xD1000009U, 0xC9803A26U };
            yield return new object[] { 0xDD8E1313U, 0x33325AAEU, 0xF8000000U };
            yield return new object[] { 0x3500008BU, 0x38800030U, 0x3B001A10U };
            yield return new object[] { 0x338DB8E1U, 0xB9000023U, 0xBAB00714U };
            yield return new object[] { 0xB700030FU, 0x2A807402U, 0xAFA37B69U };
            yield return new object[] { 0xA68542F3U, 0x35000005U, 0xA91A4EBFU };
            yield return new object[] { 0x261B9CA7U, 0xB7000005U, 0xEAAA0F43U };
            yield return new object[] { 0x33000007U, 0xB180033BU, 0xB200169DU };
            yield return new object[] { 0x0D80010EU, 0xBC000016U, 0x97001734U };
            yield return new object[] { 0x310000DCU, 0xAC0583C6U, 0xAB795304U };
            yield return new object[] { 0x297CCD10U, 0x2A000006U, 0x214AE170U };
            yield return new object[] { 0x15000001U, 0xA7010BBCU, 0x89810BBCU };
            yield return new object[] { 0xAB8002D0U, 0xAA84EC44U, 0x24A371EAU };
            yield return new object[] { 0xB4000180U, 0x38002093U, 0xB9B0DC80U };
            yield return new object[] { 0x2A015FAAU, 0x3A000006U, 0x31883DFCU };
            yield return new object[] { 0x388002C5U, 0x2A04F49DU, 0x31232278U };
            yield return new object[] { 0x35003DFEU, 0x35BB999DU, 0x3A5E95D5U };
        }

        [Theory]
        [MemberData(nameof(op_Multiply_TestData))]
        public static void op_Multiply(uint left, uint right, uint expected)
        {
            Decimal32 result = Unsafe.BitCast<uint, Decimal32>(left) * Unsafe.BitCast<uint, Decimal32>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(result));
        }

        public static IEnumerable<object[]> op_Division_TestData()
        {
            yield return new object[] { 0xFC000000U, 0x32800001U, 0xFC000000U }; // NaN / 1 -> NaN
            yield return new object[] { 0x32800001U, 0xFC000000U, 0xFC000000U }; // 1 / NaN -> NaN
            yield return new object[] { 0xFC000000U, 0x78000000U, 0xFC000000U }; // NaN / +Inf -> NaN
            yield return new object[] { 0x78000000U, 0x78000000U, 0x7C000000U }; // +Inf / +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x78000000U, 0xF8000000U, 0x7C000000U }; // +Inf / -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xF8000000U, 0x78000000U, 0x7C000000U }; // -Inf / +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xF8000000U, 0xF8000000U, 0x7C000000U }; // -Inf / -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x78000000U, 0x32800001U, 0x78000000U }; // +Inf / 1 -> +Inf
            yield return new object[] { 0x78000000U, 0xB2800001U, 0xF8000000U }; // +Inf / -1 -> -Inf
            yield return new object[] { 0xF8000000U, 0x32800002U, 0xF8000000U }; // -Inf / 2 -> -Inf
            yield return new object[] { 0xF8000000U, 0xB2800002U, 0x78000000U }; // -Inf / -2 -> +Inf
            yield return new object[] { 0x78000000U, 0x32800000U, 0x78000000U }; // +Inf / +0 -> +Inf
            yield return new object[] { 0xF8000000U, 0x32800000U, 0xF8000000U }; // -Inf / +0 -> -Inf
            yield return new object[] { 0x78000000U, 0xB2800000U, 0xF8000000U }; // +Inf / -0 -> -Inf
            yield return new object[] { 0x32800001U, 0x78000000U, 0x00000000U }; // 1 / +Inf -> +0 (Etiny)
            yield return new object[] { 0xB2800001U, 0x78000000U, 0x80000000U }; // -1 / +Inf -> -0 (Etiny)
            yield return new object[] { 0x32800005U, 0xF8000000U, 0x80000000U }; // 5 / -Inf -> -0 (Etiny)
            yield return new object[] { 0x37800005U, 0x78000000U, 0x00000000U }; // 5e10 / +Inf -> +0 (Etiny, dividend exp ignored)
            yield return new object[] { 0x32800000U, 0x78000000U, 0x00000000U }; // +0 / +Inf -> +0 (Etiny)
            yield return new object[] { 0xB2800000U, 0x78000000U, 0x80000000U }; // -0 / +Inf -> -0 (Etiny)
            yield return new object[] { 0x32800005U, 0x32800000U, 0x78000000U }; // 5 / +0 -> +Inf
            yield return new object[] { 0xB2800005U, 0x32800000U, 0xF8000000U }; // -5 / +0 -> -Inf
            yield return new object[] { 0x32800005U, 0xB2800000U, 0xF8000000U }; // 5 / -0 -> -Inf
            yield return new object[] { 0xB2800005U, 0xB2800000U, 0x78000000U }; // -5 / -0 -> +Inf
            yield return new object[] { 0x32800000U, 0x32800000U, 0x7C000000U }; // +0 / +0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xB2800000U, 0xB2800000U, 0x7C000000U }; // -0 / -0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x32800000U, 0xB2800000U, 0x7C000000U }; // +0 / -0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x32800000U, 0x32800005U, 0x32800000U }; // +0 / 5 -> +0 (ideal exp)
            yield return new object[] { 0xB2800000U, 0x32800005U, 0xB2800000U }; // -0 / 5 -> -0 (ideal exp, sign xor)
            yield return new object[] { 0x33800000U, 0x32800005U, 0x33800000U }; // 0e2 / 5 -> 0e2 (ideal exp)
            yield return new object[] { 0x32800000U, 0x34000005U, 0x31000000U }; // 0 / 5e3 -> 0e-3 (ideal exp)
            yield return new object[] { 0x35000000U, 0x30000005U, 0x37800000U }; // 0e5 / 5e-5 -> 0e10 (ideal exp)
            yield return new object[] { 0x5F800000U, 0x30000005U, 0x5F800000U }; // zero dividend, ideal exp far above max quantum (clamped)
            yield return new object[] { 0x32800001U, 0x32800002U, 0x32000005U }; // 1 / 2 -> 0.5 (exact)
            yield return new object[] { 0x32800001U, 0x32800008U, 0x3100007DU }; // 1 / 8 -> 0.125 (exact)
            yield return new object[] { 0x32800064U, 0x32800004U, 0x32800019U }; // 100 / 4 -> 25 (exact)
            yield return new object[] { 0x3280000AU, 0x32800002U, 0x32800005U }; // 10 / 2 -> 5 (exact)
            yield return new object[] { 0x32800006U, 0x32800003U, 0x32800002U }; // 6 / 3 -> 2 (exact)
            yield return new object[] { 0x32800005U, 0x32800005U, 0x32800001U }; // 5 / 5 -> 1 (exact)
            yield return new object[] { 0x31800064U, 0x32800004U, 0x31800019U }; // 1.00 / 4 -> 0.25 (exact, ideal exp preserved)
            yield return new object[] { 0x32800007U, 0x32800002U, 0x32000023U }; // 7 / 2 -> 3.5 (exact)
            yield return new object[] { 0xB280000FU, 0x32800004U, 0xB1800177U }; // -15 / 4 -> -3.75 (exact, sign)
            yield return new object[] { 0x32800001U, 0x32800003U, 0x2F32DCD5U }; // 1 / 3 -> repeating (round)
            yield return new object[] { 0x32800002U, 0x32800003U, 0x2F65B9ABU }; // 2 / 3 -> repeating (round)
            yield return new object[] { 0x328F4240U, 0x32800007U, 0x3215CC5BU }; // 1000000 / 7 (round)
            yield return new object[] { 0x3280000AU, 0x32800003U, 0x2FB2DCD5U }; // 10 / 3 -> repeating (round)
            yield return new object[] { 0x6CB8967FU, 0x32800007U, 0x3295CC5BU }; // all-nines / 7 (round)
            yield return new object[] { 0x32800001U, 0x6CB8967FU, 0x2C0F4240U }; // 1 / all-nines (round)
            yield return new object[] { 0x32800002U, 0x32800007U, 0x2F2B98B7U }; // 2 / 7 (round)
            yield return new object[] { 0x2F945855U, 0x2F9B2071U, 0x2F7270E1U }; // full-precision / full-precision (round)
            yield return new object[] { 0x6CB8967FU, 0x2F90F447U, 0x35800009U }; // all-nines / near-one full precision (round)
            yield return new object[] { 0x77E95440U, 0x30000001U, 0x78000000U }; // huge / tiny -> +Inf (overflow)
            yield return new object[] { 0xF7E95440U, 0x30000001U, 0xF8000000U }; // -huge / tiny -> -Inf (overflow)
            yield return new object[] { 0x02800001U, 0x35000001U, 0x00000001U }; // tiny / large -> underflow
            yield return new object[] { 0x32800001U, 0x5F000003U, 0x02B2DCD5U }; // 1 / huge -> tiny subnormal (round)
            yield return new object[] { 0x03000001U, 0x35000001U, 0x00800001U }; // small / large (subnormal-ish)
            yield return new object[] { 0x78000002U, 0x32800001U, 0x78000000U }; // non-canonical +Inf / 1 -> canonical +Inf
            yield return new object[] { 0x32800001U, 0xF8000005U, 0x80000000U }; // 1 / non-canonical -Inf -> canonical -0 (Etiny)
            yield return new object[] { 0x2F2C8C35U, 0x2A00002DU, 0x36E2FEAFU };
            yield return new object[] { 0xB8002104U, 0xB98043D6U, 0x2DCA43A5U };
            yield return new object[] { 0xA8800010U, 0x34802213U, 0xA21BFCF7U };
            yield return new object[] { 0x318496C5U, 0xA71C20DCU, 0xB998E4C6U };
            yield return new object[] { 0x2E830F1EU, 0xA9000005U, 0xB7861E3CU };
            yield return new object[] { 0xAD00006CU, 0xB7011BA9U, 0x2416B197U };
            yield return new object[] { 0xAC000017U, 0x380B7DD1U, 0xA12E99C7U };
            yield return new object[] { 0xBC800009U, 0xA600170BU, 0x449747B2U };
            yield return new object[] { 0x1C8C3773U, 0x4C800FA6U, 0x009E7EEAU };
            yield return new object[] { 0xB2000007U, 0xEAD57BF3U, 0x336D077CU };
            yield return new object[] { 0xBA000010U, 0x2F0ED152U, 0xB8192411U };
            yield return new object[] { 0x6A41D272U, 0xA8800006U, 0xB315A313U };
            yield return new object[] { 0xAC0000EBU, 0x28800027U, 0xB35BF1A9U };
            yield return new object[] { 0xAB8B81BBU, 0xB0000044U, 0x2D10EBF5U };
            yield return new object[] { 0x390001B3U, 0xB885B1AEU, 0xAE91C985U };
            yield return new object[] { 0x3B800006U, 0x27007EF8U, 0x421C2AA7U };
            yield return new object[] { 0x27034AB4U, 0x3280024CU, 0x2537FBB0U };
            yield return new object[] { 0x3B800094U, 0x3685C05CU, 0x32BBE9F9U };
            yield return new object[] { 0xC0001DD4U, 0x330000DDU, 0xBD34B8E4U };
            yield return new object[] { 0x378000FAU, 0x9A02AE5DU, 0xCB95B5D7U };
            yield return new object[] { 0x278014E4U, 0x3806111AU, 0x1E148643U };
            yield return new object[] { 0xB20ACE79U, 0x3204CA4CU, 0xAFA26C55U };
            yield return new object[] { 0xB98025B8U, 0xB2000002U, 0x3A0012DCU };
            yield return new object[] { 0xB780002BU, 0x3B80007DU, 0xAD000158U };
            yield return new object[] { 0xDA81A30AU, 0x38002124U, 0xD2934B2BU };
            yield return new object[] { 0xB5800001U, 0x36000006U, 0xAE996E6BU };
            yield return new object[] { 0xB8800004U, 0x35297F01U, 0xB0167191U };
            yield return new object[] { 0xAF00252CU, 0xBF000006U, 0x22800632U };
            yield return new object[] { 0x92EA6215U, 0x38800002U, 0x8CB5310AU };
            yield return new object[] { 0xAE800008U, 0xAE00009FU, 0x2F4CC617U };
            yield return new object[] { 0x2A0025A7U, 0x35001CE0U, 0x2493E5A9U };
            yield return new object[] { 0x45800056U, 0x32000030U, 0x431B56B3U };
            yield return new object[] { 0x360DAA53U, 0xB20D6A8EU, 0xB38F8AC7U };
            yield return new object[] { 0xAF800202U, 0xC000857DU, 0x1E16F36FU };
            yield return new object[] { 0xE1000004U, 0x2D00003DU, 0x8914FBCEU };
            yield return new object[] { 0xBF805674U, 0x6E377AE8U, 0xB5220489U };
            yield return new object[] { 0x2E0087F8U, 0x2F0E50D7U, 0x2DB89C87U };
            yield return new object[] { 0x32800061U, 0x4D80016BU, 0x1428C630U };
            yield return new object[] { 0xB1FD9564U, 0x33800008U, 0xB08FB2ACU };
            yield return new object[] { 0xA9000C89U, 0xC0000CD9U, 0x6614E05DU };
            yield return new object[] { 0x37001CE3U, 0x58095C61U, 0x0D9264B8U };
            yield return new object[] { 0x2E8018F2U, 0xB98002B6U, 0xE92C6841U };
            yield return new object[] { 0x400B2B74U, 0xAE0003ACU, 0xC2F6D3B7U };
            yield return new object[] { 0x2B000676U, 0xD1000009U, 0x8A9C0AD2U };
            yield return new object[] { 0xDD8E1313U, 0x33325AAEU, 0xD9AAA65CU };
            yield return new object[] { 0x3500008BU, 0x38800030U, 0x2C2C2FD9U };
            yield return new object[] { 0x338DB8E1U, 0xB9000023U, 0xAC83EBAEU };
            yield return new object[] { 0xB700030FU, 0x2A807402U, 0xBB283AFDU };
            yield return new object[] { 0xA68542F3U, 0x35000005U, 0xA38A85E6U };
            yield return new object[] { 0x261B9CA7U, 0xB7000005U, 0xA18585BBU };
            yield return new object[] { 0x33000007U, 0xB180033BU, 0xEBE127C9U };
            yield return new object[] { 0x0D80010EU, 0xBC000016U, 0x8192BA09U };
            yield return new object[] { 0x310000DCU, 0xAC0583C6U, 0xB2DCE222U };
            yield return new object[] { 0x297CCD10U, 0x2A000006U, 0x3194CCD8U };
            yield return new object[] { 0x15000001U, 0xA7010BBCU, 0x9B16433AU };
            yield return new object[] { 0xAB8002D0U, 0xAA84EC44U, 0x2F220D78U };
            yield return new object[] { 0xB4000180U, 0x38002093U, 0xAAC643C5U };
            yield return new object[] { 0x2A015FAAU, 0x3A000006U, 0x2196E511U };
            yield return new object[] { 0x388002C5U, 0x2A04F49DU, 0x3CA14FCDU };
            yield return new object[] { 0x35003DFEU, 0x35BB999DU, 0x2DBDFF39U };
        }

        [Theory]
        [MemberData(nameof(op_Division_TestData))]
        public static void op_Division(uint left, uint right, uint expected)
        {
            Decimal32 result = Unsafe.BitCast<uint, Decimal32>(left) / Unsafe.BitCast<uint, Decimal32>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(result));
        }

        public static IEnumerable<object[]> op_Increment_TestData()
        {
            yield return new object[] { 0xFC000000U, 0xFC000000U }; // NaN
            yield return new object[] { 0x78000000U, 0x78000000U }; // +Inf
            yield return new object[] { 0xF8000000U, 0xF8000000U }; // -Inf
            yield return new object[] { 0x78000002U, 0x78000000U }; // non-canonical +Inf (canonicalizes)
            yield return new object[] { 0x32800000U, 0x32800001U }; // +0 -> 1
            yield return new object[] { 0xB2800000U, 0x32800001U }; // -0 -> 1
            yield return new object[] { 0x35000000U, 0x32800001U }; // 0e5 (preferred exponent min(exp,0))
            yield return new object[] { 0x30000000U, 0x300186A0U }; // 0e-5
            yield return new object[] { 0x32800001U, 0x32800002U }; // 1 -> 2
            yield return new object[] { 0xB2800001U, 0x32800000U }; // -1 -> 0
            yield return new object[] { 0x32800002U, 0x32800003U }; // 2
            yield return new object[] { 0xB2800002U, 0xB2800001U }; // -2
            yield return new object[] { 0x3280000AU, 0x3280000BU }; // 10
            yield return new object[] { 0x32000005U, 0x3200000FU }; // 0.5 -> 1.5
            yield return new object[] { 0xB2000005U, 0x32000005U }; // -0.5
            yield return new object[] { 0x32000019U, 0x32000023U }; // 2.5
            yield return new object[] { 0x32000001U, 0x3200000BU }; // 0.1 -> 1.1
            yield return new object[] { 0x31800177U, 0x318001DBU }; // 3.75
            yield return new object[] { 0xB1800019U, 0x3180004BU }; // -0.25
            yield return new object[] { 0x32800009U, 0x3280000AU }; // 9 -> 10
            yield return new object[] { 0x6CB8967FU, 0x330F4240U }; // all-nines (overflows precision, rounds)
            yield return new object[] { 0x328F4240U, 0x328F4241U }; // 10^(P-1)
            yield return new object[] { 0x328F423FU, 0x328F4240U }; // (P-1)-nines
            yield return new object[] { 0x37000001U, 0x340F4240U }; // 1e(P+2) (1 below quantum, no visible change)
            yield return new object[] { 0x77E95440U, 0x77E95440U }; // near-max (1 negligible)
            yield return new object[] { 0xF7E95440U, 0xF7E95440U }; // near -max (1 negligible)
            yield return new object[] { 0x02800001U, 0x2F8F4240U }; // tiny positive subnormal (++ ~ 1)
            yield return new object[] { 0x82800001U, 0x2F8F4240U }; // tiny negative subnormal (++ ~ 1)
            yield return new object[] { 0x30000001U, 0x300186A1U }; // 1e-5
            yield return new object[] { 0x2F945855U, 0x2FA39A95U }; // 1.333... full precision
            yield return new object[] { 0x6BF8967FU, 0x3010C8E0U }; // 9.999... full precision (carry)
            yield return new object[] { 0x2F2C8C35U, 0x2F93B6ACU };
            yield return new object[] { 0x2A00002DU, 0x2F8F4240U };
            yield return new object[] { 0xB8002104U, 0xEDA0F7A0U };
            yield return new object[] { 0xB98043D6U, 0xB89A7F98U };
            yield return new object[] { 0xA8800010U, 0x2F8F4240U };
            yield return new object[] { 0x34802213U, 0x6CC51A38U };
            yield return new object[] { 0x318496C5U, 0x31849729U };
            yield return new object[] { 0xA71C20DCU, 0x2F8F4240U };
            yield return new object[] { 0x2E830F1EU, 0x2F8F4A15U };
            yield return new object[] { 0xA9000005U, 0x2F8F4240U };
            yield return new object[] { 0xAD00006CU, 0x2F8F4240U };
            yield return new object[] { 0xB7011BA9U, 0xB66ECE04U };
            yield return new object[] { 0xAC000017U, 0x2F8F4240U };
            yield return new object[] { 0x380B7DD1U, 0x37F2EA2AU };
            yield return new object[] { 0xBC800009U, 0xEE695440U };
            yield return new object[] { 0xA600170BU, 0x2F8F4240U };
            yield return new object[] { 0x1C8C3773U, 0x2F8F4240U };
            yield return new object[] { 0x4C800FA6U, 0x4B3D2070U };
            yield return new object[] { 0xB2000007U, 0x32000003U };
            yield return new object[] { 0xEAD57BF3U, 0x2F8F4240U };
            yield return new object[] { 0xBA000010U, 0xB7986A00U };
            yield return new object[] { 0x2F0ED152U, 0x2F90BD95U };
            yield return new object[] { 0x6A41D272U, 0x2F8F4240U };
            yield return new object[] { 0xA8800006U, 0x2F8F4240U };
            yield return new object[] { 0xAC0000EBU, 0x2F8F4240U };
            yield return new object[] { 0x28800027U, 0x2F8F4240U };
            yield return new object[] { 0xAB8B81BBU, 0x2F8F4240U };
            yield return new object[] { 0xB0000044U, 0x3001865CU };
            yield return new object[] { 0x390001B3U, 0x37426030U };
            yield return new object[] { 0xB885B1AEU, 0xB838F0CCU };
            yield return new object[] { 0x3B800006U, 0x38DB8D80U };
            yield return new object[] { 0x27007EF8U, 0x2F8F4240U };
            yield return new object[] { 0x27034AB4U, 0x2F8F4240U };
            yield return new object[] { 0x3280024CU, 0x3280024DU };
            yield return new object[] { 0x3B800094U, 0x39969540U };
            yield return new object[] { 0x3685C05CU, 0x36398398U };
            yield return new object[] { 0xC0001DD4U, 0xBEF48420U };
            yield return new object[] { 0x330000DDU, 0x328008A3U };
            yield return new object[] { 0x378000FAU, 0x35A625A0U };
            yield return new object[] { 0x9A02AE5DU, 0x2F8F4240U };
            yield return new object[] { 0x278014E4U, 0x2F8F4240U };
            yield return new object[] { 0x3806111AU, 0x37BCAB04U };
            yield return new object[] { 0xB20ACE79U, 0xB20ACE6FU };
            yield return new object[] { 0x3204CA4CU, 0x3204CA56U };
            yield return new object[] { 0xB98025B8U, 0xEE1356C0U };
            yield return new object[] { 0xB2000002U, 0x32000008U };
            yield return new object[] { 0xB780002BU, 0xB5419CE0U };
            yield return new object[] { 0x3B80007DU, 0x399312D0U };
            yield return new object[] { 0xDA81A30AU, 0xDA105E64U };
            yield return new object[] { 0x38002124U, 0x6DA174A0U };
            yield return new object[] { 0xB5800001U, 0xB28F423FU };
            yield return new object[] { 0x36000006U, 0x335B8D80U };
            yield return new object[] { 0xB8800004U, 0xB5BD0900U };
            yield return new object[] { 0x35297F01U, 0x35297F01U };
            yield return new object[] { 0xAF00252CU, 0x6BD87154U };
            yield return new object[] { 0xBF000006U, 0xBC5B8D80U };
            yield return new object[] { 0x92EA6215U, 0x2F8F4240U };
            yield return new object[] { 0x38800002U, 0x359E8480U };
            yield return new object[] { 0xAE800008U, 0x6BD8967FU };
            yield return new object[] { 0xAE00009FU, 0x6BD8967EU };
        }

        [Theory]
        [MemberData(nameof(op_Increment_TestData))]
        public static void op_Increment(uint value, uint expected)
        {
            Decimal32 result = Unsafe.BitCast<uint, Decimal32>(value);
            result++;
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(result));
        }

        public static IEnumerable<object[]> op_Decrement_TestData()
        {
            yield return new object[] { 0xFC000000U, 0xFC000000U }; // NaN
            yield return new object[] { 0x78000000U, 0x78000000U }; // +Inf
            yield return new object[] { 0xF8000000U, 0xF8000000U }; // -Inf
            yield return new object[] { 0x78000002U, 0x78000000U }; // non-canonical +Inf (canonicalizes)
            yield return new object[] { 0x32800000U, 0xB2800001U }; // +0 -> -1
            yield return new object[] { 0xB2800000U, 0xB2800001U }; // -0 -> -1
            yield return new object[] { 0x35000000U, 0xB2800001U }; // 0e5 (preferred exponent min(exp,0))
            yield return new object[] { 0x30000000U, 0xB00186A0U }; // 0e-5
            yield return new object[] { 0x32800001U, 0x32800000U }; // 1 -> 0
            yield return new object[] { 0xB2800001U, 0xB2800002U }; // -1 -> -2
            yield return new object[] { 0x32800002U, 0x32800001U }; // 2
            yield return new object[] { 0xB2800002U, 0xB2800003U }; // -2
            yield return new object[] { 0x3280000AU, 0x32800009U }; // 10
            yield return new object[] { 0x32000005U, 0xB2000005U }; // 0.5 -> -0.5
            yield return new object[] { 0xB2000005U, 0xB200000FU }; // -0.5
            yield return new object[] { 0x32000019U, 0x3200000FU }; // 2.5
            yield return new object[] { 0x32000001U, 0xB2000009U }; // 0.1 -> -0.9
            yield return new object[] { 0x31800177U, 0x31800113U }; // 3.75
            yield return new object[] { 0xB1800019U, 0xB180007DU }; // -0.25
            yield return new object[] { 0x32800009U, 0x32800008U }; // 9 -> 8
            yield return new object[] { 0x6CB8967FU, 0x6CB8967EU }; // all-nines (decrement in last place)
            yield return new object[] { 0x328F4240U, 0x328F423FU }; // 10^(P-1)
            yield return new object[] { 0x328F423FU, 0x328F423EU }; // (P-1)-nines
            yield return new object[] { 0x37000001U, 0x340F4240U }; // 1e(P+2) (1 below quantum, no visible change)
            yield return new object[] { 0x77E95440U, 0x77E95440U }; // near-max (1 negligible)
            yield return new object[] { 0xF7E95440U, 0xF7E95440U }; // near -max (1 negligible)
            yield return new object[] { 0x02800001U, 0xAF8F4240U }; // tiny positive subnormal (-- ~ -1)
            yield return new object[] { 0x82800001U, 0xAF8F4240U }; // tiny negative subnormal (-- ~ -1)
            yield return new object[] { 0x30000001U, 0xB001869FU }; // 1e-5
            yield return new object[] { 0x2F945855U, 0x2F851615U }; // 1.333... full precision
            yield return new object[] { 0x6BF8967FU, 0x6BE9543FU }; // 9.999... full precision
            yield return new object[] { 0x2F2C8C35U, 0xAF6C0A4BU };
            yield return new object[] { 0x2A00002DU, 0xAF8F4240U };
            yield return new object[] { 0xB8002104U, 0xEDA0F7A0U };
            yield return new object[] { 0xB98043D6U, 0xB89A7F98U };
            yield return new object[] { 0xA8800010U, 0xAF8F4240U };
            yield return new object[] { 0x34802213U, 0x6CC51A38U };
            yield return new object[] { 0x318496C5U, 0x31849661U };
            yield return new object[] { 0xA71C20DCU, 0xAF8F4240U };
            yield return new object[] { 0x2E830F1EU, 0xEBD84830U };
            yield return new object[] { 0xA9000005U, 0xAF8F4240U };
            yield return new object[] { 0xAD00006CU, 0xAF8F4240U };
            yield return new object[] { 0xB7011BA9U, 0xB66ECE04U };
            yield return new object[] { 0xAC000017U, 0xAF8F4240U };
            yield return new object[] { 0x380B7DD1U, 0x37F2EA2AU };
            yield return new object[] { 0xBC800009U, 0xEE695440U };
            yield return new object[] { 0xA600170BU, 0xAF8F4240U };
            yield return new object[] { 0x1C8C3773U, 0xAF8F4240U };
            yield return new object[] { 0x4C800FA6U, 0x4B3D2070U };
            yield return new object[] { 0xB2000007U, 0xB2000011U };
            yield return new object[] { 0xEAD57BF3U, 0xAF8F4240U };
            yield return new object[] { 0xBA000010U, 0xB7986A00U };
            yield return new object[] { 0x2F0ED152U, 0xEBC9C52EU };
            yield return new object[] { 0x6A41D272U, 0xAF8F4240U };
            yield return new object[] { 0xA8800006U, 0xAF8F4240U };
            yield return new object[] { 0xAC0000EBU, 0xAF8F4240U };
            yield return new object[] { 0x28800027U, 0xAF8F4240U };
            yield return new object[] { 0xAB8B81BBU, 0xAF8F4240U };
            yield return new object[] { 0xB0000044U, 0xB00186E4U };
            yield return new object[] { 0x390001B3U, 0x37426030U };
            yield return new object[] { 0xB885B1AEU, 0xB838F0CCU };
            yield return new object[] { 0x3B800006U, 0x38DB8D80U };
            yield return new object[] { 0x27007EF8U, 0xAF8F4240U };
            yield return new object[] { 0x27034AB4U, 0xAF8F4240U };
            yield return new object[] { 0x3280024CU, 0x3280024BU };
            yield return new object[] { 0x3B800094U, 0x39969540U };
            yield return new object[] { 0x3685C05CU, 0x36398398U };
            yield return new object[] { 0xC0001DD4U, 0xBEF48420U };
            yield return new object[] { 0x330000DDU, 0x328008A1U };
            yield return new object[] { 0x378000FAU, 0x35A625A0U };
            yield return new object[] { 0x9A02AE5DU, 0xAF8F4240U };
            yield return new object[] { 0x278014E4U, 0xAF8F4240U };
            yield return new object[] { 0x3806111AU, 0x37BCAB04U };
            yield return new object[] { 0xB20ACE79U, 0xB20ACE83U };
            yield return new object[] { 0x3204CA4CU, 0x3204CA42U };
            yield return new object[] { 0xB98025B8U, 0xEE1356C0U };
            yield return new object[] { 0xB2000002U, 0xB200000CU };
            yield return new object[] { 0xB780002BU, 0xB5419CE0U };
            yield return new object[] { 0x3B80007DU, 0x399312D0U };
            yield return new object[] { 0xDA81A30AU, 0xDA105E64U };
            yield return new object[] { 0x38002124U, 0x6DA174A0U };
            yield return new object[] { 0xB5800001U, 0xB28F4241U };
            yield return new object[] { 0x36000006U, 0x335B8D80U };
            yield return new object[] { 0xB8800004U, 0xB5BD0900U };
            yield return new object[] { 0x35297F01U, 0x35297F01U };
            yield return new object[] { 0xAF00252CU, 0xAF8F45F8U };
            yield return new object[] { 0xBF000006U, 0xBC5B8D80U };
            yield return new object[] { 0x92EA6215U, 0xAF8F4240U };
            yield return new object[] { 0x38800002U, 0x359E8480U };
            yield return new object[] { 0xAE800008U, 0xAF8F4240U };
            yield return new object[] { 0xAE00009FU, 0xAF8F4240U };
        }

        [Theory]
        [MemberData(nameof(op_Decrement_TestData))]
        public static void op_Decrement(uint value, uint expected)
        {
            Decimal32 result = Unsafe.BitCast<uint, Decimal32>(value);
            result--;
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(result));
        }

        public static IEnumerable<object[]> Parse_AllowTrailingInvalidCharacters_TestData()
        {
            NumberStyles style = NumberStyles.Float | NumberStyles.AllowTrailingInvalidCharacters;

            // Trailing invalid characters after a valid number
            yield return new object[] { "123abc", style, CultureInfo.InvariantCulture, Decimal32.Parse("123", CultureInfo.InvariantCulture), 3 };
            yield return new object[] { "12.5xyz", style, CultureInfo.InvariantCulture, Decimal32.Parse("12.5", CultureInfo.InvariantCulture), 4 };
            yield return new object[] { "+7e2!!", style, CultureInfo.InvariantCulture, Decimal32.Parse("7e2", CultureInfo.InvariantCulture), 4 };
            yield return new object[] { "-8.0#", style, CultureInfo.InvariantCulture, Decimal32.Parse("-8.0", CultureInfo.InvariantCulture), 4 };

            // No trailing invalid characters
            yield return new object[] { "123", style, CultureInfo.InvariantCulture, Decimal32.Parse("123", CultureInfo.InvariantCulture), 3 };

            // Special values with trailing invalid characters
            yield return new object[] { "Infinityabc", style, CultureInfo.InvariantCulture, Decimal32.PositiveInfinity, 8 };
            yield return new object[] { "-Infinityxyz", style, CultureInfo.InvariantCulture, Decimal32.NegativeInfinity, 9 };
            yield return new object[] { "NaNabc", style, CultureInfo.InvariantCulture, Decimal32.NaN, 3 };

            // Special values always consume surrounding whitespace (independent of AllowLeadingWhite/AllowTrailingWhite) before stopping on the first non-whitespace invalid character
            yield return new object[] { "Infinity   ", style, CultureInfo.InvariantCulture, Decimal32.PositiveInfinity, 11 };
            yield return new object[] { "Infinity  x", style, CultureInfo.InvariantCulture, Decimal32.PositiveInfinity, 10 };
            yield return new object[] { "+Infinity  x", style, CultureInfo.InvariantCulture, Decimal32.PositiveInfinity, 11 };
            yield return new object[] { "-Infinity  x", style, CultureInfo.InvariantCulture, Decimal32.NegativeInfinity, 11 };
            yield return new object[] { "NaN  x", style, CultureInfo.InvariantCulture, Decimal32.NaN, 5 };

            // AllowTrailingWhite has no effect on special values; the surrounding whitespace is still consumed
            yield return new object[] { "Infinity  x", (NumberStyles.Float & ~NumberStyles.AllowTrailingWhite) | NumberStyles.AllowTrailingInvalidCharacters, CultureInfo.InvariantCulture, Decimal32.PositiveInfinity, 10 };
        }

        [Theory]
        [MemberData(nameof(Parse_AllowTrailingInvalidCharacters_TestData))]
        public static void Parse_AllowTrailingInvalidCharacters(string value, NumberStyles style, IFormatProvider provider, Decimal32 expected, int expectedCharsConsumed)
        {
            Assert.True(Decimal32.TryParse(value, style, provider, out Decimal32 result, out int charsConsumed));
            Assert.Equal(expected, result);
            Assert.Equal(expectedCharsConsumed, charsConsumed);

            Assert.True(Decimal32.TryParse(value.AsSpan(), style, provider, out result, out charsConsumed));
            Assert.Equal(expected, result);
            Assert.Equal(expectedCharsConsumed, charsConsumed);

            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);
            Assert.True(Decimal32.TryParse(utf8Bytes.AsSpan(), style, provider, out result, out int bytesConsumed));
            Assert.Equal(expected, result);
            Assert.Equal(expectedCharsConsumed, bytesConsumed);
        }

        public static IEnumerable<object[]> Parse_AllowTrailingInvalidCharacters_Invalid_TestData()
        {
            NumberStyles style = NumberStyles.Float | NumberStyles.AllowTrailingInvalidCharacters;

            yield return new object[] { "", style, CultureInfo.InvariantCulture };
            yield return new object[] { "abc", style, CultureInfo.InvariantCulture };
            yield return new object[] { ".abc", style, CultureInfo.InvariantCulture };
        }

        [Theory]
        [MemberData(nameof(Parse_AllowTrailingInvalidCharacters_Invalid_TestData))]
        public static void Parse_AllowTrailingInvalidCharacters_Invalid(string value, NumberStyles style, IFormatProvider provider)
        {
            Assert.False(Decimal32.TryParse(value, style, provider, out Decimal32 result, out int charsConsumed));
            Assert.Equal(0, charsConsumed);

            Assert.False(Decimal32.TryParse(value.AsSpan(), style, provider, out result, out charsConsumed));
            Assert.Equal(0, charsConsumed);

            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);
            Assert.False(Decimal32.TryParse(utf8Bytes.AsSpan(), style, provider, out result, out int bytesConsumed));
            Assert.Equal(0, bytesConsumed);
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal32Arithmetic), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void op_Arithmetic_IntelReferenceVectors(string operation, uint left, uint right, uint expected)
        {
            Decimal32 l = Unsafe.BitCast<uint, Decimal32>(left);
            Decimal32 r = Unsafe.BitCast<uint, Decimal32>(right);

            Decimal32 result = operation switch
            {
                "add" => l + r,
                "sub" => l - r,
                "mul" => l * r,
                "div" => l / r,
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal32Comparison), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void op_Comparison_IntelReferenceVectors(string operation, uint left, uint right, bool expected)
        {
            Decimal32 l = Unsafe.BitCast<uint, Decimal32>(left);
            Decimal32 r = Unsafe.BitCast<uint, Decimal32>(right);

            bool result = operation switch
            {
                "quiet_equal" => l == r,
                "quiet_not_equal" => l != r,
                "quiet_less" => l < r,
                "quiet_greater" => l > r,
                "quiet_less_equal" => l <= r,
                "quiet_greater_equal" => l >= r,
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, result);
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal32Unary), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void UnaryOperation_IntelReferenceVectors(string operation, uint value, uint expected)
        {
            Decimal32 v = Unsafe.BitCast<uint, Decimal32>(value);

            Decimal32 result = operation switch
            {
                "abs" => Decimal32.Abs(v),
                "negate" => -v,
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal32BinaryValue), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void BinaryValueOperation_IntelReferenceVectors(string operation, uint left, uint right, uint expected)
        {
            Decimal32 l = Unsafe.BitCast<uint, Decimal32>(left);
            Decimal32 r = Unsafe.BitCast<uint, Decimal32>(right);

            Decimal32 result = operation switch
            {
                "copySign" => Decimal32.CopySign(l, r),
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal32Predicate), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void Predicate_IntelReferenceVectors(string operation, uint value, bool expected)
        {
            Decimal32 v = Unsafe.BitCast<uint, Decimal32>(value);

            bool result = operation switch
            {
                "isNaN" => Decimal32.IsNaN(v),
                "isInf" => Decimal32.IsInfinity(v),
                "isFinite" => Decimal32.IsFinite(v),
                "isSigned" => Decimal32.IsNegative(v),
                "isNormal" => Decimal32.IsNormal(v),
                "isSubnormal" => Decimal32.IsSubnormal(v),
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, result);
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal(0x32800001U, Unsafe.BitCast<Decimal32, uint>(Decimal32.One));
        }

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(0xB2800001U, Unsafe.BitCast<Decimal32, uint>(Decimal32.NegativeOne));
        }

        [Fact]
        public static void ETest()
        {
            Assert.Equal(0x2FA97A4AU, Unsafe.BitCast<Decimal32, uint>(Decimal32.E)); // +2.718282
        }

        [Fact]
        public static void PiTest()
        {
            Assert.Equal(0x2FAFEFD9U, Unsafe.BitCast<Decimal32, uint>(Decimal32.Pi)); // +3.141593
        }

        [Fact]
        public static void TauTest()
        {
            Assert.Equal(0x2FDFDFB1U, Unsafe.BitCast<Decimal32, uint>(Decimal32.Tau)); // +6.283185
        }

        public static IEnumerable<object[]> Abs_TestData()
        {
            yield return new object[] { 0x7C000000U, 0x7C000000U }; // Abs(NaN)
            yield return new object[] { 0xFC000000U, 0x7C000000U }; // Abs(-NaN)
            yield return new object[] { 0x78000000U, 0x78000000U }; // Abs(+Inf)
            yield return new object[] { 0xF8000000U, 0x78000000U }; // Abs(-Inf)
            yield return new object[] { 0x32800000U, 0x32800000U }; // Abs(0)
            yield return new object[] { 0xB2800000U, 0x32800000U }; // Abs(-0)
            yield return new object[] { 0x32800001U, 0x32800001U }; // Abs(1)
            yield return new object[] { 0xB2800001U, 0x32800001U }; // Abs(-1)
            yield return new object[] { 0x32800002U, 0x32800002U }; // Abs(2)
            yield return new object[] { 0xB2800002U, 0x32800002U }; // Abs(-2)
            yield return new object[] { 0x32800003U, 0x32800003U }; // Abs(3)
            yield return new object[] { 0xB2800003U, 0x32800003U }; // Abs(-3)
            yield return new object[] { 0x32800005U, 0x32800005U }; // Abs(5)
            yield return new object[] { 0xB2800005U, 0x32800005U }; // Abs(-5)
            yield return new object[] { 0x3280000AU, 0x3280000AU }; // Abs(10)
            yield return new object[] { 0xB280000AU, 0x3280000AU }; // Abs(-10)
            yield return new object[] { 0x32000005U, 0x32000005U }; // Abs(0.5)
            yield return new object[] { 0xB2000005U, 0x32000005U }; // Abs(-0.5)
            yield return new object[] { 0x32000019U, 0x32000019U }; // Abs(2.5)
            yield return new object[] { 0x3200000FU, 0x3200000FU }; // Abs(1.5)
            yield return new object[] { 0x32800064U, 0x32800064U }; // Abs(100)
            yield return new object[] { 0x328003E8U, 0x328003E8U }; // Abs(1000)
            yield return new object[] { 0x32000001U, 0x32000001U }; // Abs(0.1)
            yield return new object[] { 0x32800009U, 0x32800009U }; // Abs(9)
            yield return new object[] { 0x34000001U, 0x34000001U }; // Abs(1E+3)
            yield return new object[] { 0x3200000AU, 0x3200000AU }; // Abs(1.0)
            yield return new object[] { 0x318000C8U, 0x318000C8U }; // Abs(2.00)
            yield return new object[] { 0x33000002U, 0x33000002U }; // Abs(2E+1)
            yield return new object[] { 0x00000001U, 0x00000001U }; // Abs(epsilon)
            yield return new object[] { 0x80000001U, 0x00000001U }; // Abs(-epsilon)
            yield return new object[] { 0x000F423FU, 0x000F423FU }; // Abs(largest_subnormal)
            yield return new object[] { 0x800F423FU, 0x000F423FU }; // Abs(-largest_subnormal)
            yield return new object[] { 0x03000001U, 0x03000001U }; // Abs(smallest_normal)
            yield return new object[] { 0x83000001U, 0x03000001U }; // Abs(-smallest_normal)
            yield return new object[] { 0x77F8967FU, 0x77F8967FU }; // Abs(maxvalue)
            yield return new object[] { 0xF7F8967FU, 0x77F8967FU }; // Abs(-maxvalue)
        }

        [Theory]
        [MemberData(nameof(Abs_TestData))]
        public static void AbsTest(uint value, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.Abs(Unsafe.BitCast<uint, Decimal32>(value))));
        }

        public static IEnumerable<object[]> Classification_TestData()
        {
            yield return new object[] { 0x7C000000U, false, false, true, false, true, false, false, false }; // NaN
            yield return new object[] { 0xFC000000U, false, false, true, true, false, false, false, false }; // -NaN
            yield return new object[] { 0x78000000U, false, true, false, false, true, false, true, true }; // +Inf
            yield return new object[] { 0xF8000000U, false, true, false, true, false, true, false, true }; // -Inf
            yield return new object[] { 0x32800000U, true, false, false, false, true, false, false, true }; // 0
            yield return new object[] { 0xB2800000U, true, false, false, true, false, false, false, true }; // -0
            yield return new object[] { 0x32800001U, true, false, false, false, true, false, false, true }; // 1
            yield return new object[] { 0xB2800001U, true, false, false, true, false, false, false, true }; // -1
            yield return new object[] { 0x32800002U, true, false, false, false, true, false, false, true }; // 2
            yield return new object[] { 0xB2800002U, true, false, false, true, false, false, false, true }; // -2
            yield return new object[] { 0x32800003U, true, false, false, false, true, false, false, true }; // 3
            yield return new object[] { 0xB2800003U, true, false, false, true, false, false, false, true }; // -3
            yield return new object[] { 0x32800005U, true, false, false, false, true, false, false, true }; // 5
            yield return new object[] { 0xB2800005U, true, false, false, true, false, false, false, true }; // -5
            yield return new object[] { 0x3280000AU, true, false, false, false, true, false, false, true }; // 10
            yield return new object[] { 0xB280000AU, true, false, false, true, false, false, false, true }; // -10
            yield return new object[] { 0x32000005U, true, false, false, false, true, false, false, true }; // 0.5
            yield return new object[] { 0xB2000005U, true, false, false, true, false, false, false, true }; // -0.5
            yield return new object[] { 0x32000019U, true, false, false, false, true, false, false, true }; // 2.5
            yield return new object[] { 0x3200000FU, true, false, false, false, true, false, false, true }; // 1.5
            yield return new object[] { 0x32800064U, true, false, false, false, true, false, false, true }; // 100
            yield return new object[] { 0x328003E8U, true, false, false, false, true, false, false, true }; // 1000
            yield return new object[] { 0x32000001U, true, false, false, false, true, false, false, true }; // 0.1
            yield return new object[] { 0x32800009U, true, false, false, false, true, false, false, true }; // 9
            yield return new object[] { 0x34000001U, true, false, false, false, true, false, false, true }; // 1E+3
            yield return new object[] { 0x3200000AU, true, false, false, false, true, false, false, true }; // 1.0
            yield return new object[] { 0x318000C8U, true, false, false, false, true, false, false, true }; // 2.00
            yield return new object[] { 0x33000002U, true, false, false, false, true, false, false, true }; // 2E+1
            yield return new object[] { 0x00000001U, true, false, false, false, true, false, false, true }; // epsilon
            yield return new object[] { 0x80000001U, true, false, false, true, false, false, false, true }; // -epsilon
            yield return new object[] { 0x000F423FU, true, false, false, false, true, false, false, true }; // largest_subnormal
            yield return new object[] { 0x800F423FU, true, false, false, true, false, false, false, true }; // -largest_subnormal
            yield return new object[] { 0x03000001U, true, false, false, false, true, false, false, true }; // smallest_normal
            yield return new object[] { 0x83000001U, true, false, false, true, false, false, false, true }; // -smallest_normal
            yield return new object[] { 0x77F8967FU, true, false, false, false, true, false, false, true }; // maxvalue
            yield return new object[] { 0xF7F8967FU, true, false, false, true, false, false, false, true }; // -maxvalue
        }

        [Theory]
        [MemberData(nameof(Classification_TestData))]
        public static void ClassificationTest(uint value, bool isFinite, bool isInfinity, bool isNaN, bool isNegative, bool isPositive, bool isNegativeInfinity, bool isPositiveInfinity, bool isRealNumber)
        {
            Decimal32 d = Unsafe.BitCast<uint, Decimal32>(value);
            Assert.Equal(isFinite, Decimal32.IsFinite(d));
            Assert.Equal(isInfinity, Decimal32.IsInfinity(d));
            Assert.Equal(isNaN, Decimal32.IsNaN(d));
            Assert.Equal(isNegative, Decimal32.IsNegative(d));
            Assert.Equal(isPositive, Decimal32.IsPositive(d));
            Assert.Equal(isNegativeInfinity, Decimal32.IsNegativeInfinity(d));
            Assert.Equal(isPositiveInfinity, Decimal32.IsPositiveInfinity(d));
            Assert.Equal(isRealNumber, Decimal32.IsRealNumber(d));
        }

        public static IEnumerable<object[]> IsNormalIsSubnormal_TestData()
        {
            yield return new object[] { 0x7C000000U, false, false }; // NaN
            yield return new object[] { 0xFC000000U, false, false }; // -NaN
            yield return new object[] { 0x78000000U, false, false }; // +Inf
            yield return new object[] { 0xF8000000U, false, false }; // -Inf
            yield return new object[] { 0x32800000U, false, false }; // 0
            yield return new object[] { 0xB2800000U, false, false }; // -0
            yield return new object[] { 0x32800001U, true, false }; // 1
            yield return new object[] { 0xB2800001U, true, false }; // -1
            yield return new object[] { 0x32800002U, true, false }; // 2
            yield return new object[] { 0xB2800002U, true, false }; // -2
            yield return new object[] { 0x32800003U, true, false }; // 3
            yield return new object[] { 0xB2800003U, true, false }; // -3
            yield return new object[] { 0x32800005U, true, false }; // 5
            yield return new object[] { 0xB2800005U, true, false }; // -5
            yield return new object[] { 0x3280000AU, true, false }; // 10
            yield return new object[] { 0xB280000AU, true, false }; // -10
            yield return new object[] { 0x32000005U, true, false }; // 0.5
            yield return new object[] { 0xB2000005U, true, false }; // -0.5
            yield return new object[] { 0x32000019U, true, false }; // 2.5
            yield return new object[] { 0x3200000FU, true, false }; // 1.5
            yield return new object[] { 0x32800064U, true, false }; // 100
            yield return new object[] { 0x328003E8U, true, false }; // 1000
            yield return new object[] { 0x32000001U, true, false }; // 0.1
            yield return new object[] { 0x32800009U, true, false }; // 9
            yield return new object[] { 0x34000001U, true, false }; // 1E+3
            yield return new object[] { 0x3200000AU, true, false }; // 1.0
            yield return new object[] { 0x318000C8U, true, false }; // 2.00
            yield return new object[] { 0x33000002U, true, false }; // 2E+1
            yield return new object[] { 0x00000001U, false, true }; // epsilon
            yield return new object[] { 0x80000001U, false, true }; // -epsilon
            yield return new object[] { 0x000F423FU, false, true }; // largest_subnormal
            yield return new object[] { 0x800F423FU, false, true }; // -largest_subnormal
            yield return new object[] { 0x03000001U, true, false }; // smallest_normal
            yield return new object[] { 0x83000001U, true, false }; // -smallest_normal
            yield return new object[] { 0x77F8967FU, true, false }; // maxvalue
            yield return new object[] { 0xF7F8967FU, true, false }; // -maxvalue
        }

        [Theory]
        [MemberData(nameof(IsNormalIsSubnormal_TestData))]
        public static void IsNormalIsSubnormalTest(uint value, bool isNormal, bool isSubnormal)
        {
            Decimal32 d = Unsafe.BitCast<uint, Decimal32>(value);
            Assert.Equal(isNormal, Decimal32.IsNormal(d));
            Assert.Equal(isSubnormal, Decimal32.IsSubnormal(d));
        }

        public static IEnumerable<object[]> IsInteger_TestData()
        {
            yield return new object[] { 0x7C000000U, false, false, false }; // NaN
            yield return new object[] { 0xFC000000U, false, false, false }; // -NaN
            yield return new object[] { 0x78000000U, false, false, false }; // +Inf
            yield return new object[] { 0xF8000000U, false, false, false }; // -Inf
            yield return new object[] { 0x32800000U, true, true, false }; // 0
            yield return new object[] { 0xB2800000U, true, true, false }; // -0
            yield return new object[] { 0x32800001U, true, false, true }; // 1
            yield return new object[] { 0xB2800001U, true, false, true }; // -1
            yield return new object[] { 0x32800002U, true, true, false }; // 2
            yield return new object[] { 0xB2800002U, true, true, false }; // -2
            yield return new object[] { 0x32800003U, true, false, true }; // 3
            yield return new object[] { 0xB2800003U, true, false, true }; // -3
            yield return new object[] { 0x32800005U, true, false, true }; // 5
            yield return new object[] { 0xB2800005U, true, false, true }; // -5
            yield return new object[] { 0x3280000AU, true, true, false }; // 10
            yield return new object[] { 0xB280000AU, true, true, false }; // -10
            yield return new object[] { 0x32000005U, false, false, false }; // 0.5
            yield return new object[] { 0xB2000005U, false, false, false }; // -0.5
            yield return new object[] { 0x32000019U, false, false, false }; // 2.5
            yield return new object[] { 0x3200000FU, false, false, false }; // 1.5
            yield return new object[] { 0x32800064U, true, true, false }; // 100
            yield return new object[] { 0x328003E8U, true, true, false }; // 1000
            yield return new object[] { 0x32000001U, false, false, false }; // 0.1
            yield return new object[] { 0x32800009U, true, false, true }; // 9
            yield return new object[] { 0x34000001U, true, true, false }; // 1E+3
            yield return new object[] { 0x3200000AU, true, false, true }; // 1.0
            yield return new object[] { 0x318000C8U, true, true, false }; // 2.00
            yield return new object[] { 0x33000002U, true, true, false }; // 2E+1
            yield return new object[] { 0x00000001U, false, false, false }; // epsilon
            yield return new object[] { 0x80000001U, false, false, false }; // -epsilon
            yield return new object[] { 0x000F423FU, false, false, false }; // largest_subnormal
            yield return new object[] { 0x800F423FU, false, false, false }; // -largest_subnormal
            yield return new object[] { 0x03000001U, false, false, false }; // smallest_normal
            yield return new object[] { 0x83000001U, false, false, false }; // -smallest_normal
            yield return new object[] { 0x77F8967FU, true, true, false }; // maxvalue
            yield return new object[] { 0xF7F8967FU, true, true, false }; // -maxvalue
        }

        [Theory]
        [MemberData(nameof(IsInteger_TestData))]
        public static void IsIntegerTest(uint value, bool isInteger, bool isEvenInteger, bool isOddInteger)
        {
            Decimal32 d = Unsafe.BitCast<uint, Decimal32>(value);
            Assert.Equal(isInteger, Decimal32.IsInteger(d));
            Assert.Equal(isEvenInteger, Decimal32.IsEvenInteger(d));
            Assert.Equal(isOddInteger, Decimal32.IsOddInteger(d));
        }

        public static IEnumerable<object[]> MaxMagnitude_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800005U };
            yield return new object[] { 0x32800005U, 0x32800003U, 0x32800005U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0x32800005U };
            yield return new object[] { 0x32800003U, 0xB2800005U, 0xB2800005U };
            yield return new object[] { 0xB2800003U, 0xB2800005U, 0xB2800005U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0x32800000U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x7C000000U, 0x32800003U, 0x7C000000U };
            yield return new object[] { 0x32800003U, 0x7C000000U, 0x7C000000U };
            yield return new object[] { 0x7C000000U, 0xF8000000U, 0x7C000000U };
            yield return new object[] { 0xB2800005U, 0x78000000U, 0x78000000U };
        }

        [Theory]
        [MemberData(nameof(MaxMagnitude_TestData))]
        public static void MaxMagnitudeTest(uint x, uint y, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.MaxMagnitude(Unsafe.BitCast<uint, Decimal32>(x), Unsafe.BitCast<uint, Decimal32>(y))));
        }

        public static IEnumerable<object[]> MinMagnitude_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800003U };
            yield return new object[] { 0x32800005U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xB2800005U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0xB2800005U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0xB2800003U, 0x32800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0xB2800000U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0xB2800000U };
            yield return new object[] { 0x7C000000U, 0x32800003U, 0x7C000000U };
            yield return new object[] { 0x32800003U, 0x7C000000U, 0x7C000000U };
            yield return new object[] { 0x7C000000U, 0xF8000000U, 0x7C000000U };
            yield return new object[] { 0xB2800005U, 0x78000000U, 0xB2800005U };
        }

        [Theory]
        [MemberData(nameof(MinMagnitude_TestData))]
        public static void MinMagnitudeTest(uint x, uint y, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.MinMagnitude(Unsafe.BitCast<uint, Decimal32>(x), Unsafe.BitCast<uint, Decimal32>(y))));
        }

        public static IEnumerable<object[]> MaxMagnitudeNumber_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800005U };
            yield return new object[] { 0x32800005U, 0x32800003U, 0x32800005U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0x32800005U };
            yield return new object[] { 0x32800003U, 0xB2800005U, 0xB2800005U };
            yield return new object[] { 0xB2800003U, 0xB2800005U, 0xB2800005U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0x32800000U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x7C000000U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x7C000000U, 0x32800003U };
            yield return new object[] { 0x7C000000U, 0xF8000000U, 0xF8000000U };
            yield return new object[] { 0xB2800005U, 0x78000000U, 0x78000000U };
        }

        [Theory]
        [MemberData(nameof(MaxMagnitudeNumber_TestData))]
        public static void MaxMagnitudeNumberTest(uint x, uint y, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.MaxMagnitudeNumber(Unsafe.BitCast<uint, Decimal32>(x), Unsafe.BitCast<uint, Decimal32>(y))));
        }

        public static IEnumerable<object[]> MinMagnitudeNumber_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800003U };
            yield return new object[] { 0x32800005U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xB2800005U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0xB2800005U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0xB2800003U, 0x32800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0xB2800000U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0xB2800000U };
            yield return new object[] { 0x7C000000U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x7C000000U, 0x32800003U };
            yield return new object[] { 0x7C000000U, 0xF8000000U, 0xF8000000U };
            yield return new object[] { 0xB2800005U, 0x78000000U, 0xB2800005U };
        }

        [Theory]
        [MemberData(nameof(MinMagnitudeNumber_TestData))]
        public static void MinMagnitudeNumberTest(uint x, uint y, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.MinMagnitudeNumber(Unsafe.BitCast<uint, Decimal32>(x), Unsafe.BitCast<uint, Decimal32>(y))));
        }

        public static IEnumerable<object[]> MultiplyAddEstimate_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800002U, 0x32800011U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0x32800002U, 0xB280000DU };
            yield return new object[] { 0x32000001U, 0x32000001U, 0x32800001U, 0x31800065U };
            yield return new object[] { 0x32800002U, 0x32000019U, 0xB2000005U, 0x3200002DU };
            yield return new object[] { 0x3280000AU, 0x3280000AU, 0x32800001U, 0x32800065U };
            yield return new object[] { 0x7C000000U, 0x32800002U, 0x32800003U, 0x7C000000U };
            yield return new object[] { 0x78000000U, 0x32800000U, 0x32800001U, 0x7C000000U };
            yield return new object[] { 0x32800002U, 0x32800003U, 0xF8000000U, 0xF8000000U };
        }

        [Theory]
        [MemberData(nameof(MultiplyAddEstimate_TestData))]
        public static void MultiplyAddEstimateTest(uint left, uint right, uint addend, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.MultiplyAddEstimate(Unsafe.BitCast<uint, Decimal32>(left), Unsafe.BitCast<uint, Decimal32>(right), Unsafe.BitCast<uint, Decimal32>(addend))));
        }

        public static IEnumerable<object[]> Max_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800005U };
            yield return new object[] { 0x32800005U, 0x32800003U, 0x32800005U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0x32800005U };
            yield return new object[] { 0x32800003U, 0xB2800005U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0xB2800005U, 0xB2800003U };
            yield return new object[] { 0xB2800005U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x3200000AU, 0x31800064U, 0x31800064U };
            yield return new object[] { 0x33000002U, 0x32800014U, 0x32800014U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0x32800000U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x32800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x7C000000U, 0x32800003U, 0x7C000000U };
            yield return new object[] { 0x32800003U, 0x7C000000U, 0x7C000000U };
            yield return new object[] { 0xFC000000U, 0xB2800003U, 0xFC000000U };
            yield return new object[] { 0x32800003U, 0xFC000000U, 0xFC000000U };
            yield return new object[] { 0x7C000000U, 0xFC000000U, 0x7C000000U };
            yield return new object[] { 0x78000000U, 0x32800003U, 0x78000000U };
            yield return new object[] { 0x32800003U, 0xF8000000U, 0x32800003U };
            yield return new object[] { 0xF8000000U, 0x78000000U, 0x78000000U };
            yield return new object[] { 0x78000000U, 0x7C000000U, 0x7C000000U };
            yield return new object[] { 0x7C000000U, 0xF8000000U, 0x7C000000U };
        }

        [Theory]
        [MemberData(nameof(Max_TestData))]
        public static void MaxTest(uint x, uint y, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.Max(Unsafe.BitCast<uint, Decimal32>(x), Unsafe.BitCast<uint, Decimal32>(y))));
        }

        public static IEnumerable<object[]> Min_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800003U };
            yield return new object[] { 0x32800005U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xB2800005U, 0xB2800005U };
            yield return new object[] { 0xB2800003U, 0xB2800005U, 0xB2800005U };
            yield return new object[] { 0xB2800005U, 0xB2800003U, 0xB2800005U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0xB2800003U, 0x32800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x3200000AU, 0x31800064U, 0x31800064U };
            yield return new object[] { 0x33000002U, 0x32800014U, 0x32800014U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0xB2800000U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0xB2800000U };
            yield return new object[] { 0x32800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x7C000000U, 0x32800003U, 0x7C000000U };
            yield return new object[] { 0x32800003U, 0x7C000000U, 0x7C000000U };
            yield return new object[] { 0xFC000000U, 0xB2800003U, 0xFC000000U };
            yield return new object[] { 0x32800003U, 0xFC000000U, 0xFC000000U };
            yield return new object[] { 0x7C000000U, 0xFC000000U, 0x7C000000U };
            yield return new object[] { 0x78000000U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0xF8000000U, 0xF8000000U };
            yield return new object[] { 0xF8000000U, 0x78000000U, 0xF8000000U };
            yield return new object[] { 0x78000000U, 0x7C000000U, 0x7C000000U };
            yield return new object[] { 0x7C000000U, 0xF8000000U, 0x7C000000U };
        }

        [Theory]
        [MemberData(nameof(Min_TestData))]
        public static void MinTest(uint x, uint y, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.Min(Unsafe.BitCast<uint, Decimal32>(x), Unsafe.BitCast<uint, Decimal32>(y))));
        }

        public static IEnumerable<object[]> MaxNative_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800005U };
            yield return new object[] { 0x32800005U, 0x32800003U, 0x32800005U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0x32800005U };
            yield return new object[] { 0x32800003U, 0xB2800005U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0xB2800005U, 0xB2800003U };
            yield return new object[] { 0xB2800005U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x3200000AU, 0x31800064U, 0x31800064U };
            yield return new object[] { 0x33000002U, 0x32800014U, 0x32800014U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0xB2800000U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x32800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x7C000000U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x7C000000U, 0x7C000000U };
            yield return new object[] { 0xFC000000U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xFC000000U, 0xFC000000U };
            yield return new object[] { 0x7C000000U, 0xFC000000U, 0xFC000000U };
            yield return new object[] { 0x78000000U, 0x32800003U, 0x78000000U };
            yield return new object[] { 0x32800003U, 0xF8000000U, 0x32800003U };
            yield return new object[] { 0xF8000000U, 0x78000000U, 0x78000000U };
            yield return new object[] { 0x78000000U, 0x7C000000U, 0x7C000000U };
            yield return new object[] { 0x7C000000U, 0xF8000000U, 0xF8000000U };
        }

        [Theory]
        [MemberData(nameof(MaxNative_TestData))]
        public static void MaxNativeTest(uint x, uint y, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.MaxNative(Unsafe.BitCast<uint, Decimal32>(x), Unsafe.BitCast<uint, Decimal32>(y))));
        }

        public static IEnumerable<object[]> MinNative_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800003U };
            yield return new object[] { 0x32800005U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xB2800005U, 0xB2800005U };
            yield return new object[] { 0xB2800003U, 0xB2800005U, 0xB2800005U };
            yield return new object[] { 0xB2800005U, 0xB2800003U, 0xB2800005U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0xB2800003U, 0x32800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x3200000AU, 0x31800064U, 0x31800064U };
            yield return new object[] { 0x33000002U, 0x32800014U, 0x32800014U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0xB2800000U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x32800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x7C000000U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x7C000000U, 0x7C000000U };
            yield return new object[] { 0xFC000000U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xFC000000U, 0xFC000000U };
            yield return new object[] { 0x7C000000U, 0xFC000000U, 0xFC000000U };
            yield return new object[] { 0x78000000U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0xF8000000U, 0xF8000000U };
            yield return new object[] { 0xF8000000U, 0x78000000U, 0xF8000000U };
            yield return new object[] { 0x78000000U, 0x7C000000U, 0x7C000000U };
            yield return new object[] { 0x7C000000U, 0xF8000000U, 0xF8000000U };
        }

        [Theory]
        [MemberData(nameof(MinNative_TestData))]
        public static void MinNativeTest(uint x, uint y, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.MinNative(Unsafe.BitCast<uint, Decimal32>(x), Unsafe.BitCast<uint, Decimal32>(y))));
        }

        public static IEnumerable<object[]> MaxNumber_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800005U };
            yield return new object[] { 0x32800005U, 0x32800003U, 0x32800005U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0x32800005U };
            yield return new object[] { 0x32800003U, 0xB2800005U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0xB2800005U, 0xB2800003U };
            yield return new object[] { 0xB2800005U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x3200000AU, 0x31800064U, 0x31800064U };
            yield return new object[] { 0x33000002U, 0x32800014U, 0x32800014U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0x32800000U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x32800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x7C000000U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x7C000000U, 0x32800003U };
            yield return new object[] { 0xFC000000U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xFC000000U, 0x32800003U };
            yield return new object[] { 0x7C000000U, 0xFC000000U, 0x7C000000U };
            yield return new object[] { 0x78000000U, 0x32800003U, 0x78000000U };
            yield return new object[] { 0x32800003U, 0xF8000000U, 0x32800003U };
            yield return new object[] { 0xF8000000U, 0x78000000U, 0x78000000U };
            yield return new object[] { 0x78000000U, 0x7C000000U, 0x78000000U };
            yield return new object[] { 0x7C000000U, 0xF8000000U, 0xF8000000U };
        }

        [Theory]
        [MemberData(nameof(MaxNumber_TestData))]
        public static void MaxNumberTest(uint x, uint y, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.MaxNumber(Unsafe.BitCast<uint, Decimal32>(x), Unsafe.BitCast<uint, Decimal32>(y))));
        }

        public static IEnumerable<object[]> MinNumber_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800003U };
            yield return new object[] { 0x32800005U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xB2800005U, 0xB2800005U };
            yield return new object[] { 0xB2800003U, 0xB2800005U, 0xB2800005U };
            yield return new object[] { 0xB2800005U, 0xB2800003U, 0xB2800005U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0xB2800003U, 0x32800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x3200000AU, 0x31800064U, 0x31800064U };
            yield return new object[] { 0x33000002U, 0x32800014U, 0x32800014U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0xB2800000U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0xB2800000U };
            yield return new object[] { 0x32800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x7C000000U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x7C000000U, 0x32800003U };
            yield return new object[] { 0xFC000000U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0x32800003U, 0xFC000000U, 0x32800003U };
            yield return new object[] { 0x7C000000U, 0xFC000000U, 0x7C000000U };
            yield return new object[] { 0x78000000U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0xF8000000U, 0xF8000000U };
            yield return new object[] { 0xF8000000U, 0x78000000U, 0xF8000000U };
            yield return new object[] { 0x78000000U, 0x7C000000U, 0x78000000U };
            yield return new object[] { 0x7C000000U, 0xF8000000U, 0xF8000000U };
        }

        [Theory]
        [MemberData(nameof(MinNumber_TestData))]
        public static void MinNumberTest(uint x, uint y, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.MinNumber(Unsafe.BitCast<uint, Decimal32>(x), Unsafe.BitCast<uint, Decimal32>(y))));
        }

        public static IEnumerable<object[]> CopySign_TestData()
        {
            yield return new object[] { 0x32800003U, 0x32800005U, 0x32800003U };
            yield return new object[] { 0x32800005U, 0x32800003U, 0x32800005U };
            yield return new object[] { 0xB2800003U, 0x32800005U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0xB2800005U, 0xB2800003U };
            yield return new object[] { 0xB2800003U, 0xB2800005U, 0xB2800003U };
            yield return new object[] { 0xB2800005U, 0xB2800003U, 0xB2800005U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0xB2800003U };
            yield return new object[] { 0xB2800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x32800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x3200000AU, 0x31800064U, 0x3200000AU };
            yield return new object[] { 0x33000002U, 0x32800014U, 0x33000002U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0xB2800000U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x32800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x7C000000U, 0x32800003U, 0x7C000000U };
            yield return new object[] { 0x32800003U, 0x7C000000U, 0x32800003U };
            yield return new object[] { 0xFC000000U, 0xB2800003U, 0xFC000000U };
            yield return new object[] { 0x32800003U, 0xFC000000U, 0xB2800003U };
            yield return new object[] { 0x7C000000U, 0xFC000000U, 0xFC000000U };
            yield return new object[] { 0x78000000U, 0x32800003U, 0x78000000U };
            yield return new object[] { 0x32800003U, 0xF8000000U, 0xB2800003U };
            yield return new object[] { 0xF8000000U, 0x78000000U, 0x78000000U };
            yield return new object[] { 0x78000000U, 0x7C000000U, 0x78000000U };
            yield return new object[] { 0x7C000000U, 0xF8000000U, 0xFC000000U };
        }

        [Theory]
        [MemberData(nameof(CopySign_TestData))]
        public static void CopySignTest(uint value, uint sign, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.CopySign(Unsafe.BitCast<uint, Decimal32>(value), Unsafe.BitCast<uint, Decimal32>(sign))));
        }

        public static IEnumerable<object[]> Sign_TestData()
        {
            yield return new object[] { 0x32800000U, 0 };
            yield return new object[] { 0xB2800000U, 0 };
            yield return new object[] { 0x32800001U, 1 };
            yield return new object[] { 0xB2800001U, -1 };
            yield return new object[] { 0x32800005U, 1 };
            yield return new object[] { 0xB2800005U, -1 };
            yield return new object[] { 0x32000005U, 1 };
            yield return new object[] { 0xB2000005U, -1 };
            yield return new object[] { 0x3200000AU, 1 };
            yield return new object[] { 0x33000002U, 1 };
            yield return new object[] { 0x31000001U, 1 };
            yield return new object[] { 0x78000000U, 1 };
            yield return new object[] { 0xF8000000U, -1 };
        }

        [Theory]
        [MemberData(nameof(Sign_TestData))]
        public static void SignTest(uint value, int expected)
        {
            Assert.Equal(expected, Decimal32.Sign(Unsafe.BitCast<uint, Decimal32>(value)));
        }

        [Fact]
        public static void SignNaNTest()
        {
            Assert.Throws<ArithmeticException>(() => Decimal32.Sign(Decimal32.NaN));
            Assert.Throws<ArithmeticException>(() => Decimal32.Sign(-Decimal32.NaN));
        }

        public static IEnumerable<object[]> Clamp_TestData()
        {
            yield return new object[] { 0x32800005U, 0x32800001U, 0x3280000AU, 0x32800005U };
            yield return new object[] { 0xB2800005U, 0x32800001U, 0x3280000AU, 0x32800001U };
            yield return new object[] { 0x3280000FU, 0x32800001U, 0x3280000AU, 0x3280000AU };
            yield return new object[] { 0x32800001U, 0x32800001U, 0x3280000AU, 0x32800001U };
            yield return new object[] { 0x3280000AU, 0x32800001U, 0x3280000AU, 0x3280000AU };
            yield return new object[] { 0x32000019U, 0x32800002U, 0x32800003U, 0x32000019U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x7C000000U, 0x32800001U, 0x3280000AU, 0x7C000000U };
            yield return new object[] { 0x78000000U, 0x32800001U, 0x3280000AU, 0x3280000AU };
            yield return new object[] { 0xF8000000U, 0x32800001U, 0x3280000AU, 0x32800001U };
        }

        [Theory]
        [MemberData(nameof(Clamp_TestData))]
        public static void ClampTest(uint value, uint min, uint max, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.Clamp(Unsafe.BitCast<uint, Decimal32>(value), Unsafe.BitCast<uint, Decimal32>(min), Unsafe.BitCast<uint, Decimal32>(max))));
        }

        [Fact]
        public static void ClampMinGreaterThanMaxTest()
        {
            Assert.Throws<ArgumentException>(() => Decimal32.Clamp(Decimal32.One, Unsafe.BitCast<uint, Decimal32>(0x3280000AU), Unsafe.BitCast<uint, Decimal32>(0x32800001U)));
        }

        public static IEnumerable<object[]> ClampNative_TestData()
        {
            yield return new object[] { 0x32800005U, 0x32800001U, 0x3280000AU, 0x32800005U };
            yield return new object[] { 0xB2800005U, 0x32800001U, 0x3280000AU, 0x32800001U };
            yield return new object[] { 0x3280000FU, 0x32800001U, 0x3280000AU, 0x3280000AU };
            yield return new object[] { 0x32800001U, 0x32800001U, 0x3280000AU, 0x32800001U };
            yield return new object[] { 0x3280000AU, 0x32800001U, 0x3280000AU, 0x3280000AU };
            yield return new object[] { 0x32000019U, 0x32800002U, 0x32800003U, 0x32000019U };
            yield return new object[] { 0xB2800000U, 0x32800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x32800000U, 0xB2800000U, 0x32800000U, 0x32800000U };
            yield return new object[] { 0x32800003U, 0xB2800003U, 0x32800003U, 0x32800003U };
            yield return new object[] { 0x7C000000U, 0x32800001U, 0x3280000AU, 0x32800001U };
            yield return new object[] { 0x78000000U, 0x32800001U, 0x3280000AU, 0x3280000AU };
            yield return new object[] { 0xF8000000U, 0x32800001U, 0x3280000AU, 0x32800001U };
        }

        [Theory]
        [MemberData(nameof(ClampNative_TestData))]
        public static void ClampNativeTest(uint value, uint min, uint max, uint expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal32, uint>(Decimal32.ClampNative(Unsafe.BitCast<uint, Decimal32>(value), Unsafe.BitCast<uint, Decimal32>(min), Unsafe.BitCast<uint, Decimal32>(max))));
        }

        [Fact]
        public static void ClampNativeMinGreaterThanMaxTest()
        {
            Assert.Throws<ArgumentException>(() => Decimal32.ClampNative(Decimal32.One, Unsafe.BitCast<uint, Decimal32>(0x3280000AU), Unsafe.BitCast<uint, Decimal32>(0x32800001U)));
        }
    }
}
