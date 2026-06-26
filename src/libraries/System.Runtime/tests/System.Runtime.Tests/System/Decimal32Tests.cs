// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
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
            yield return new object[] { Decimal32.Parse("-1e1"), Decimal32.Parse("-10"), 0 };
            yield return new object[] { Decimal32.Parse("-2"), Decimal32.Parse("-3"), 1 };
            yield return new object[] { Decimal32.Parse("3"), Decimal32.Parse("2"), 1 };
            yield return new object[] { Decimal32.Parse("1e90"), Decimal32.Parse("1e90"), 0 };
            yield return new object[] { Decimal32.Parse("9.99999e95"), Decimal32.Parse("9.99999e95"), 0 };
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
                yield return new object[] { Decimal32.Parse("-0.0000"), "G", defaultFormat, "-0.0000" };
                yield return new object[] { Decimal32.Parse("0"), "G", defaultFormat, "0" };
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
    }
}
