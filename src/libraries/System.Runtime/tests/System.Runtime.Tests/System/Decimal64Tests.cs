// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public class Decimal64Tests
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

            yield return new object[] { "-123", defaultStyle, null, Decimal64.Parse("-123") };
            yield return new object[] { "0", defaultStyle, null, Decimal64.Zero };
            yield return new object[] { "123", defaultStyle, null, Decimal64.Parse("123") };
            yield return new object[] { "  123  ", defaultStyle, null, Decimal64.Parse("123") };
            yield return new object[] { (567.89).ToString(), defaultStyle, null, Decimal64.Parse("56789e-2") };
            yield return new object[] { (-567.89).ToString(), defaultStyle, null, Decimal64.Parse("-56789e-2") };
            yield return new object[] { "0.6666666666666666500000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, Decimal64.Parse("0.66666666666666665") };
            yield return new object[] { new string('9', 17), defaultStyle, invariantFormat, Decimal64.Parse(new string('9', 17)) };

            yield return new object[] { "0." + new string('0', 398) + "1", defaultStyle, invariantFormat, Decimal64.Zero };
            yield return new object[] { "-0." + new string('0', 398) + "1", defaultStyle, invariantFormat, Decimal64.NegativeZero };
            yield return new object[] { "0." + new string('0', 397) + "1", defaultStyle, invariantFormat, Decimal64.Parse("1e-398") };
            yield return new object[] { "-0." + new string('0', 397) + "1", defaultStyle, invariantFormat, Decimal64.Parse("-1e-398") };

            yield return new object[] { "0." + new string('0', 396) + "12345", defaultStyle, invariantFormat, Decimal64.Parse("1.2345e-397") };
            yield return new object[] { "-0." + new string('0', 396) + "12345", defaultStyle, invariantFormat, Decimal64.Parse("-1.2345e-397") };
            yield return new object[] { "0." + new string('0', 396) + "12662", defaultStyle, invariantFormat, Decimal64.Parse("1.2662e-397") };
            yield return new object[] { "-0." + new string('0', 396) + "12662", defaultStyle, invariantFormat, Decimal64.Parse("-1.2662e-397") };

            yield return new object[] { emptyFormat.NumberDecimalSeparator + "234", defaultStyle, null, Decimal64.Parse("0.234") };
            yield return new object[] { "234" + emptyFormat.NumberDecimalSeparator, defaultStyle, null, Decimal64.Parse("234") };
            yield return new object[] { "7" + new string('0', 384) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, Decimal64.Parse("7e384") };
            yield return new object[] { "07" + new string('0', 384) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, Decimal64.Parse("7e384") };

            yield return new object[] { (123.1).ToString(), NumberStyles.AllowDecimalPoint, null, Decimal64.Parse("123.1") };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.AllowThousands, null, Decimal64.Parse("1000") };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, Decimal64.Parse("123") };
            yield return new object[] { (123.567).ToString(), NumberStyles.Any, emptyFormat, Decimal64.Parse("123567e-3") };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, Decimal64.Parse("123") };
            yield return new object[] { "$1000", NumberStyles.Currency, customFormat1, Decimal64.Parse("1000") };
            yield return new object[] { "123.123", NumberStyles.Float, customFormat2, Decimal64.Parse("123123e-3") };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, customFormat2, Decimal64.Parse("-123") };

            yield return new object[] { "NaN", NumberStyles.Any, invariantFormat, Decimal64.NaN };
            yield return new object[] { "+NaN", NumberStyles.Any, invariantFormat, Decimal64.NaN };
            yield return new object[] { "Infinity", NumberStyles.Any, invariantFormat, Decimal64.PositiveInfinity };
            yield return new object[] { "+Infinity", NumberStyles.Any, invariantFormat, Decimal64.PositiveInfinity };
            yield return new object[] { "1" + new string('0', 385), NumberStyles.Any, invariantFormat, Decimal64.PositiveInfinity };
            yield return new object[] { "-Infinity", NumberStyles.Any, invariantFormat, Decimal64.NegativeInfinity };
            yield return new object[] { "-1" + new string('0', 385), NumberStyles.Any, invariantFormat, Decimal64.NegativeInfinity };
        }


        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, Decimal64 expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Decimal64 result;
            if ((style & ~NumberStyles.Number) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(Decimal64.TryParse(value, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, Decimal64.Parse(value));
                }

                Assert.Equal(expected, Decimal64.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(Decimal64.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, Decimal64.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(Decimal64.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, Decimal64.Parse(value, style));
                Assert.Equal(expected, Decimal64.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [MemberData(nameof(Parse_Preserve_TrailingZero_TestData))]
        public static void Parse_Preserve_TrailingZero(string value, string expected)
        {
            Assert.Equal(expected, Decimal64.Parse(value).ToString(CultureInfo.InvariantCulture));
        }

        public static IEnumerable<object[]> Parse_Preserve_TrailingZero_TestData()
        {
            yield return new object[] { "0.00", "0.00" };
            yield return new object[] { "0." + new string('0', 398), "0." + new string('0', 398) };
            yield return new object[] { "0." + new string('0', 1000), "0." + new string('0', 398) };
            yield return new object[] { "0." + new string('0', 1000) + "1234567", "0." + new string('0', 398) };
            yield return new object[] { "0e-2", "0.00" };
            yield return new object[] { "0e-398", "0." + new string('0', 398) };
            yield return new object[] { "0e-10000", "0." + new string('0', 398) };
            yield return new object[] { "0.123e-10000", "0." + new string('0', 398) };
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
            Decimal64 result;
            if ((style & ~NumberStyles.Number) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(Decimal64.TryParse(value, out result));
                    Assert.Equal(default(Decimal64), result);

                    Assert.Throws(exceptionType, () => Decimal64.Parse(value));
                }

                Assert.Throws(exceptionType, () => Decimal64.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(Decimal64.TryParse(value, style, provider, out result));
            Assert.Equal(default(Decimal64), result);

            Assert.Throws(exceptionType, () => Decimal64.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(Decimal64.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(Decimal64), result);

                Assert.Throws(exceptionType, () => Decimal64.Parse(value, style));
                Assert.Throws(exceptionType, () => Decimal64.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "-123", 1, 3, NumberStyles.Number, null, Decimal64.Parse("123") };
            yield return new object[] { "-123", 0, 3, NumberStyles.Number, null, Decimal64.Parse("-12") };
            yield return new object[] { 1000.ToString("N0"), 0, 4, NumberStyles.AllowThousands, null, Decimal64.Parse("100") };
            yield return new object[] { 1000.ToString("N0"), 2, 3, NumberStyles.AllowThousands, null, Decimal64.Parse("0") };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, Decimal64.Parse("123") };
            yield return new object[] { "1234567890123456789012345.678456", 1, 4, NumberStyles.Number, new NumberFormatInfo() { NumberDecimalSeparator = "." }, Decimal64.Parse("2345") };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, Decimal64 expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Decimal64 result;
            if ((style & ~NumberStyles.Number) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(Decimal64.TryParse(value.AsSpan(offset, count), out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, Decimal64.Parse(value.AsSpan(offset, count)));
                }

                Assert.Equal(expected, Decimal64.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, Decimal64.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(Decimal64.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }


        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => Decimal64.Parse(value.AsSpan(), style, provider));

                Assert.False(Decimal64.TryParse(value.AsSpan(), style, provider, out Decimal64 result));
                Assert.Equal(default, result);
            }
        }

        [Theory]
        [MemberData(nameof(Rounding_TestData))]
        public static void Rounding(string s1, string s2)
        {
            Assert.Equal(Decimal64.Parse(s1), Decimal64.Parse(s2));
        }

        public static IEnumerable<object[]> Rounding_TestData()
        {
            yield return new object[] { "12345678912345678", "12345678912345680" };
            yield return new object[] { "12345678912345671", "12345678912345670" };
            yield return new object[] { "12345678912345675", "12345678912345680" };
            yield return new object[] { "12345678912345685", "12345678912345680" };
            yield return new object[] { "123456789123456850001", "123456789123456900000" };
            yield return new object[] { "99999999999999991", "99999999999999990" };
            yield return new object[] { "99999999999999995", "100000000000000000" };
            yield return new object[] { "99999999999999996", "100000000000000000" };
            yield return new object[] { "9999999999999999001", "9999999999999999000" };
        }

        [Theory]
        [MemberData(nameof(MaxValue_Rounding_TestData))]
        public static void MaxValue_Rounding(string value, Decimal64 expected)
        {
            Assert.Equal(expected, Decimal64.Parse(value));
        }

        public static IEnumerable<object[]> MaxValue_Rounding_TestData()
        {
            yield return new object[] { new string('9', 16) + '4' + new string('0', 368), Decimal64.MaxValue };
            yield return new object[] { new string('9', 16) + '5' + new string('0', 368), Decimal64.PositiveInfinity };
            yield return new object[] { new string('9', 16) + '5' + new string('0', 367) + '1', Decimal64.PositiveInfinity };

            yield return new object[] { new string('9', 16) + '1' + new string('9', 368), Decimal64.MaxValue };
            yield return new object[] { new string('9', 16) + '4' + new string('9', 368), Decimal64.MaxValue };

            yield return new object[] { new string('9', 16) + '5' + new string('0', 368), Decimal64.PositiveInfinity };
            yield return new object[] { new string('9', 16) + '5' + new string('0', 367) + '1', Decimal64.PositiveInfinity };
            yield return new object[] { "1e397", Decimal64.PositiveInfinity };
            yield return new object[] { "10e395", Decimal64.PositiveInfinity };
            yield return new object[] { "100.3e394", Decimal64.PositiveInfinity };

            yield return new object[] { '-' + new string('9', 16) + '5' + new string('0', 368), Decimal64.NegativeInfinity };
            yield return new object[] { '-' + new string('9', 16) + '5' + new string('0', 367) + '1', Decimal64.NegativeInfinity };
            yield return new object[] { "-1e397", Decimal64.NegativeInfinity };
            yield return new object[] { "-10e395", Decimal64.NegativeInfinity };
            yield return new object[] { "-100.3e394", Decimal64.NegativeInfinity };
        }

        [Theory]
        [MemberData(nameof(CompareTo_Other_ReturnsExpected_TestData))]
        public static void CompareTo_Other_ReturnsExpected(Decimal64 d1, Decimal64 d2, int expected)
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
            yield return new object[] { Decimal64.Parse("0e5"), Decimal64.Parse("1e1"), -1 };
            yield return new object[] { Decimal64.Parse("0e5"), Decimal64.Parse("-1e1"), 1 };
            yield return new object[] { Decimal64.Parse("0e-5"), Decimal64.Parse("1e1"), -1 };
            yield return new object[] { Decimal64.Parse("0e-5"), Decimal64.Parse("-1e1"), 1 };
            yield return new object[] { Decimal64.Parse("-1e1"), Decimal64.Parse("-10"), 0 };
            yield return new object[] { Decimal64.Parse("-2"), Decimal64.Parse("-3"), 1 };
            yield return new object[] { Decimal64.Parse("3"), Decimal64.Parse("2"), 1 };
            yield return new object[] { Decimal64.Parse("1e384"), Decimal64.Parse("1e384"), 0 };
            yield return new object[] { Decimal64.Parse("9.999999999999999e369"), Decimal64.Parse("9.999999999999999e369"), 0 };
            yield return new object[] { Decimal64.Parse("999e10"), Decimal64.Parse("997e70"), -1 };
            yield return new object[] { Decimal64.Parse("997e70"), Decimal64.Parse("999e10"), 1 };
            yield return new object[] { Decimal64.Parse("1"), Decimal64.Parse("-1"), 1 };
            yield return new object[] { Decimal64.Parse("10"), Decimal64.Parse("-1"), 1 };
            yield return new object[] { Decimal64.Parse("10"), Decimal64.NaN, 1 };
            yield return new object[] { Decimal64.Parse("10"), Decimal64.NegativeInfinity, 1 };
            yield return new object[] { Decimal64.Parse("10"), Decimal64.NegativeZero, 1 };
            yield return new object[] { Decimal64.PositiveInfinity, Decimal64.Parse("1e20"), 1 };
            yield return new object[] { Decimal64.PositiveInfinity, Decimal64.Parse("1e1500"), 0 };
            yield return new object[] { Decimal64.PositiveInfinity, Decimal64.NegativeInfinity, 1 };
            yield return new object[] { Decimal64.PositiveInfinity, Decimal64.PositiveInfinity, 0 };
            yield return new object[] { Decimal64.PositiveInfinity, Decimal64.NegativeZero, 1 };
            yield return new object[] { Decimal64.NegativeInfinity, Decimal64.NegativeInfinity, 0 };
            yield return new object[] { Decimal64.NaN, Decimal64.NaN, 0 };
            yield return new object[] { Decimal64.NegativeZero, Decimal64.NegativeInfinity, 1 };
            yield return new object[] { Decimal64.NegativeZero, Decimal64.Parse("0e20"), 0 };
            yield return new object[] { Decimal64.NegativeZero, Decimal64.NaN, 1 };
            yield return new object[] { Decimal64.Epsilon, Decimal64.Parse("1e-398"), 0 };
            yield return new object[] { Decimal64.Parse("4e-399"), Decimal64.Zero, 0 };
            yield return new object[] { Decimal64.Parse("5e-399"), Decimal64.Zero, 0 };
            yield return new object[] { Decimal64.Parse("5.00001e-399"), Decimal64.Epsilon, 0 };
            yield return new object[] { Decimal64.Parse("0.5" + new string('0', 200) + "1e-398"), Decimal64.Epsilon, 0 };
            yield return new object[] { Decimal64.Parse("0.5" + new string('0', 200) + "1e-399 "), Decimal64.Epsilon, -1 };
            yield return new object[] { Decimal64.Parse("5." + new string('0', 300) + "1e-399"), Decimal64.Epsilon, 0 };
            yield return new object[] { Decimal64.Parse("6e-399"), Decimal64.Parse("1e-398"), 0 };
            yield return new object[] { Decimal64.Parse("1" + new string('0', 21) + "1e-420"), Decimal64.Epsilon, 0 };
            yield return new object[] { Decimal64.Parse("-1" + new string('0', 21) + "1e-420"), Decimal64.Parse("-1e-398"), 0 };
            for (int i = 1; i < 16; i++)
            {
                var d1 = Decimal64.Parse("1e" + i);
                var d2 = Decimal64.Parse("1" + new string('0', i));
                yield return new object[] { d1, d2, 0 };
            }
        }

        [Theory]
        [MemberData(nameof(GetHashCode_TestData))]
        public static void GetHashCodeTest(Decimal64 d1, Decimal64 d2)
        {
            Assert.Equal(d1.GetHashCode(), d2.GetHashCode());
        }

        public static IEnumerable<object[]> GetHashCode_TestData()
        {
            yield return new object[] { Decimal64.Zero, Decimal64.NegativeZero };
            yield return new object[] { Decimal64.Zero, Decimal64.Zero };
            yield return new object[] { Decimal64.NaN, Decimal64.NaN };
            yield return new object[] { Decimal64.Parse("0e20"), Decimal64.Parse("0e18") };
            yield return new object[] { Decimal64.Parse("1e7"), Decimal64.Parse("1e7") };
            yield return new object[] { Decimal64.Parse("1e7"), Decimal64.Parse("1e7") };
            yield return new object[] { Decimal64.PositiveInfinity, Decimal64.PositiveInfinity };
            yield return new object[] { Decimal64.NegativeInfinity, Decimal64.NegativeInfinity };
        }

        [Fact]
        public static void CompareToZero()
        {
            var zero = Decimal64.Parse("0e1");
            Assert.Equal(zero, Decimal64.Parse("0e20"));
            Assert.Equal(zero, Decimal64.Parse("1e-399"));
            Assert.Equal(zero, Decimal64.Parse("234e-1000"));
            Assert.Equal(zero, Decimal64.Parse("-1e-399"));
            Assert.Equal(zero, Decimal64.Parse("-234e-1000"));
            Assert.Equal(zero, Decimal64.Zero);
            Assert.Equal(zero, Decimal64.NegativeZero);
            Assert.Equal(Decimal64.Zero, Decimal64.NegativeZero);
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                yield return new object[] { Decimal64.Parse("-0"), "G", defaultFormat, "-0" };
                yield return new object[] { Decimal64.NegativeZero, "G", defaultFormat, "-0" };
                yield return new object[] { Decimal64.Parse("-0.0000"), "G", defaultFormat, "-0.0000" };
                yield return new object[] { Decimal64.Parse("0"), "G", defaultFormat, "0" };
                yield return new object[] { Decimal64.Zero, "G", defaultFormat, "0" };
                yield return new object[] { Decimal64.Parse("0.0000"), "G", defaultFormat, "0.0000" };
                yield return new object[] { Decimal64.Parse($"{long.MinValue}"), "G", defaultFormat, "-9223372036854776000" };
                yield return new object[] { Decimal64.Parse($"{long.MaxValue}"), "G", defaultFormat, "9223372036854776000" };
                yield return new object[] { Decimal64.Parse("3e384"), "G", defaultFormat, "3" + new string('0', 384) };
                yield return new object[] { Decimal64.Parse("-3e384"), "G", defaultFormat, "-3" + new string('0', 384) };
                yield return new object[] { Decimal64.Parse("-4567e0"), "G", defaultFormat, "-4567" };
                yield return new object[] { Decimal64.Parse("-4567891e-3"), "G", defaultFormat, "-4567.891" };
                yield return new object[] { Decimal64.Parse("0e0"), "G", defaultFormat, "0" };
                yield return new object[] { Decimal64.Parse("4567e0"), "G", defaultFormat, "4567" };
                yield return new object[] { Decimal64.Parse("4567891e-3"), "G", defaultFormat, "4567.891" };

                yield return new object[] { Decimal64.Parse("2468e0"), "N", defaultFormat, "2,468.00" };

                yield return new object[] { Decimal64.Parse("2467e0"), "[#-##-#]", defaultFormat, "[2-46-7]" };
                yield return new object[] { Decimal64.Parse("4e-399"), "G", defaultFormat, "0." + new string('0', 398) };
                yield return new object[] { Decimal64.Parse("5e-399"), "G", defaultFormat, "0." + new string('0', 398) };
                yield return new object[] { Decimal64.Parse("5.00000000000000000000000001e-399"), "G", defaultFormat, "0." + new string('0', 397) + "1" };
                yield return new object[] { Decimal64.Parse("6e-399"), "G", defaultFormat, "0." + new string('0', 397) + "1" };
                yield return new object[] { Decimal64.Parse("-4e-399"), "G", defaultFormat, "-0." + new string('0', 398) };
                yield return new object[] { Decimal64.Parse("-5e-399"), "G", defaultFormat, "-0." + new string('0', 398) };
                yield return new object[] { Decimal64.Parse("-5.00000000000000000000000001e-399"), "G", defaultFormat, "-0." + new string('0', 397) + "1" };
                yield return new object[] { Decimal64.Parse("-6e-399"), "G", defaultFormat, "-0." + new string('0', 397) + "1" };

            }
        }

        [Fact]
        public static void Test_ToString()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (object[] testdata in ToString_TestData())
                {
                    ToString((Decimal64)testdata[0], (string)testdata[1], (IFormatProvider)testdata[2], (string)testdata[3]);
                }
            }
        }

        private static void ToString(Decimal64 f, string format, IFormatProvider provider, string expected)
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
        [MemberData(nameof(PositiveInfinity_NonCanonicalEncodings64_TestData))]
        [MemberData(nameof(NegativeInfinity_NonCanonicalEncodings64_TestData))]
        [MemberData(nameof(NaN_NonCanonicalEncodings64_TestData))]
        public static void NaN_Infinity_NonCanonicalEncodings64_Compare(Decimal64 d, ulong encoding)
        {
            Decimal64 d2 = Unsafe.BitCast<ulong, Decimal64>(encoding);
            Assert.Equal(0, d.CompareTo(d2));
            Assert.Equal(d.GetHashCode(), d2.GetHashCode());
        }

        public static IEnumerable<object[]> PositiveInfinity_NonCanonicalEncodings64_TestData()
        {
            const ulong canonical = 0x7800_0000_0000_0000UL;

            yield return new object[] { Decimal64.PositiveInfinity, canonical };
            yield return new object[] { Decimal64.PositiveInfinity, canonical | 0x0000_0000_0000_0001UL };
            yield return new object[] { Decimal64.PositiveInfinity, canonical | 0x0200_0000_0000_0000UL };
            yield return new object[] { Decimal64.PositiveInfinity, canonical | 0x0000_0000_0000_1234UL };
            yield return new object[] { Decimal64.PositiveInfinity, canonical | 0x0000_0000_000F_FFFFUL };
            yield return new object[] { Decimal64.PositiveInfinity, canonical | 0x03FF_FFFF_FFFF_FFFFUL };
        }

        public static IEnumerable<object[]> NegativeInfinity_NonCanonicalEncodings64_TestData()
        {
            const ulong canonical = 0xF800_0000_0000_0000UL;

            yield return new object[] { Decimal64.NegativeInfinity, canonical };
            yield return new object[] { Decimal64.NegativeInfinity, canonical | 0x0000_0000_0000_0001UL };
            yield return new object[] { Decimal64.NegativeInfinity, canonical | 0x0200_0000_0000_0000UL };
            yield return new object[] { Decimal64.NegativeInfinity, canonical | 0x0000_0000_0000_1234UL };
            yield return new object[] { Decimal64.NegativeInfinity, canonical | 0x0000_0000_000F_FFFFUL };
            yield return new object[] { Decimal64.NegativeInfinity, canonical | 0x03FF_FFFF_FFFF_FFFFUL };
        }

        public static IEnumerable<object[]> NaN_NonCanonicalEncodings64_TestData()
        {
            const ulong canonical = 0xFC00_0000_0000_0000UL;

            yield return new object[] { Decimal64.NaN, canonical };
            yield return new object[] { Decimal64.NaN, canonical | 0x0000_0000_0000_0001UL };
            yield return new object[] { Decimal64.NaN, canonical | 0x0200_0000_0000_0000UL };
            yield return new object[] { Decimal64.NaN, canonical | 0x0000_0000_0000_1234UL };
            yield return new object[] { Decimal64.NaN, canonical | 0x0000_0000_000F_FFFFUL };
            yield return new object[] { Decimal64.NaN, canonical | 0x03FF_FFFF_FFFF_FFFFUL };
        }

        public static IEnumerable<object[]> Parse_AllowTrailingInvalidCharacters_TestData()
        {
            NumberStyles style = NumberStyles.Float | NumberStyles.AllowTrailingInvalidCharacters;

            // Trailing invalid characters after a valid number
            yield return new object[] { "123abc", style, CultureInfo.InvariantCulture, Decimal64.Parse("123", CultureInfo.InvariantCulture), 3 };
            yield return new object[] { "12.5xyz", style, CultureInfo.InvariantCulture, Decimal64.Parse("12.5", CultureInfo.InvariantCulture), 4 };
            yield return new object[] { "+7e2!!", style, CultureInfo.InvariantCulture, Decimal64.Parse("7e2", CultureInfo.InvariantCulture), 4 };
            yield return new object[] { "-8.0#", style, CultureInfo.InvariantCulture, Decimal64.Parse("-8.0", CultureInfo.InvariantCulture), 4 };

            // No trailing invalid characters
            yield return new object[] { "123", style, CultureInfo.InvariantCulture, Decimal64.Parse("123", CultureInfo.InvariantCulture), 3 };

            // Special values with trailing invalid characters
            yield return new object[] { "Infinityabc", style, CultureInfo.InvariantCulture, Decimal64.PositiveInfinity, 8 };
            yield return new object[] { "-Infinityxyz", style, CultureInfo.InvariantCulture, Decimal64.NegativeInfinity, 9 };
            yield return new object[] { "NaNabc", style, CultureInfo.InvariantCulture, Decimal64.NaN, 3 };
        }

        [Theory]
        [MemberData(nameof(Parse_AllowTrailingInvalidCharacters_TestData))]
        public static void Parse_AllowTrailingInvalidCharacters(string value, NumberStyles style, IFormatProvider provider, Decimal64 expected, int expectedCharsConsumed)
        {
            Assert.True(Decimal64.TryParse(value, style, provider, out Decimal64 result, out int charsConsumed));
            Assert.Equal(expected, result);
            Assert.Equal(expectedCharsConsumed, charsConsumed);

            Assert.True(Decimal64.TryParse(value.AsSpan(), style, provider, out result, out charsConsumed));
            Assert.Equal(expected, result);
            Assert.Equal(expectedCharsConsumed, charsConsumed);

            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);
            Assert.True(Decimal64.TryParse(utf8Bytes.AsSpan(), style, provider, out result, out int bytesConsumed));
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
            Assert.False(Decimal64.TryParse(value, style, provider, out Decimal64 result, out int charsConsumed));
            Assert.Equal(0, charsConsumed);

            Assert.False(Decimal64.TryParse(value.AsSpan(), style, provider, out result, out charsConsumed));
            Assert.Equal(0, charsConsumed);
        }
    }
}
