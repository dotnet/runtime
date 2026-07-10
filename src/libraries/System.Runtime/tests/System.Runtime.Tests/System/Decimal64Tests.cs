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

        public static IEnumerable<object[]> UnaryNegation_TestData()
        {
            yield return new object[] { 0x31C0_0000_0000_0000UL, 0xB1C0_0000_0000_0000UL }; // +0 -> -0
            yield return new object[] { 0xB1C0_0000_0000_0000UL, 0x31C0_0000_0000_0000UL }; // -0 -> +0
            yield return new object[] { 0x7800_0000_0000_0000UL, 0xF800_0000_0000_0000UL }; // +Infinity -> -Infinity
            yield return new object[] { 0xF800_0000_0000_0000UL, 0x7800_0000_0000_0000UL }; // -Infinity -> +Infinity
            yield return new object[] { 0xFC00_0000_0000_0000UL, 0x7C00_0000_0000_0000UL }; // NaN -> sign-flipped NaN
            yield return new object[] { 0x7C00_0000_0000_0000UL, 0xFC00_0000_0000_0000UL };
        }

        [Theory]
        [MemberData(nameof(UnaryNegation_TestData))]
        public static void op_UnaryNegation(ulong value, ulong expected)
        {
            Decimal64 result = -Unsafe.BitCast<ulong, Decimal64>(value);
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(result));
        }

        [Theory]
        [InlineData("123")]
        [InlineData("-123")]
        [InlineData("4567.891")]
        [InlineData("-4567.891")]
        [InlineData("9.999999999999999e384")]
        [InlineData("1e-398")]
        public static void op_UnaryNegation_FiniteRoundTrips(string value)
        {
            Decimal64 d = Decimal64.Parse(value, CultureInfo.InvariantCulture);
            Decimal64 negated = -d;

            ulong dBits = Unsafe.BitCast<Decimal64, ulong>(d);
            ulong negatedBits = Unsafe.BitCast<Decimal64, ulong>(negated);

            Assert.Equal(dBits ^ 0x8000_0000_0000_0000UL, negatedBits); // only the sign bit differs
            Assert.Equal(dBits, Unsafe.BitCast<Decimal64, ulong>(-negated)); // double negation is identity
        }

        [Theory]
        [InlineData(0x31C0_0000_0000_0000UL)] // +0
        [InlineData(0xB1C0_0000_0000_0000UL)] // -0
        [InlineData(0x7800_0000_0000_0000UL)] // +Infinity
        [InlineData(0xF800_0000_0000_0000UL)] // -Infinity
        [InlineData(0xFC00_0000_0000_0000UL)] // NaN
        [InlineData(0x2FE8_6F26_FC0F_FFFFUL)] // arbitrary finite
        public static void op_UnaryPlus(ulong value)
        {
            Decimal64 d = Unsafe.BitCast<ulong, Decimal64>(value);
            Assert.Equal(value, Unsafe.BitCast<Decimal64, ulong>(+d));
        }
        public static IEnumerable<object[]> op_Addition_TestData()
        {
            yield return new object[] { 0xFC000000_00000000UL, 0x31C00000_00000001UL, 0xFC000000_00000000UL }; // NaN + 1 -> NaN
            yield return new object[] { 0x31C00000_00000001UL, 0xFC000000_00000000UL, 0xFC000000_00000000UL }; // 1 + NaN -> NaN
            yield return new object[] { 0x78000000_00000000UL, 0x31C00000_00000001UL, 0x78000000_00000000UL }; // +Inf + 1 -> +Inf
            yield return new object[] { 0x31C00000_00000001UL, 0x78000000_00000000UL, 0x78000000_00000000UL }; // 1 + +Inf -> +Inf
            yield return new object[] { 0x78000000_00000000UL, 0xF8000000_00000000UL, 0xFC000000_00000000UL }; // +Inf + -Inf -> NaN
            yield return new object[] { 0xF8000000_00000000UL, 0x78000000_00000000UL, 0xFC000000_00000000UL }; // -Inf + +Inf -> NaN
            yield return new object[] { 0x78000000_00000000UL, 0x78000000_00000000UL, 0x78000000_00000000UL }; // +Inf + +Inf -> +Inf
            yield return new object[] { 0xF8000000_00000000UL, 0xF8000000_00000000UL, 0xF8000000_00000000UL }; // -Inf + -Inf -> -Inf
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000000UL, 0x31C00000_00000000UL }; // +0 + +0 -> +0
            yield return new object[] { 0xB1C00000_00000000UL, 0xB1C00000_00000000UL, 0xB1C00000_00000000UL }; // -0 + -0 -> -0
            yield return new object[] { 0x31C00000_00000000UL, 0xB1C00000_00000000UL, 0x31C00000_00000000UL }; // +0 + -0 -> +0 (round-half-even)
            yield return new object[] { 0xB1C00000_00000000UL, 0x31C00000_00000000UL, 0x31C00000_00000000UL }; // -0 + +0 -> +0
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000000UL, 0x31C00000_00000001UL }; // 1 + 0 -> 1
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000001UL, 0x31C00000_00000001UL }; // 0 + 1 -> 1
            yield return new object[] { 0x78000000_00000002UL, 0x31C00000_00000001UL, 0x78000000_00000000UL }; // non-canonical +Inf + 1 -> canonical +Inf
            yield return new object[] { 0x31C00000_00000001UL, 0xF8000000_00000005UL, 0xF8000000_00000000UL }; // 1 + non-canonical -Inf -> canonical -Inf
            yield return new object[] { 0x78000000_0000000FUL, 0xF8000000_00000003UL, 0xFC000000_00000000UL }; // non-canonical +Inf + non-canonical -Inf -> NaN
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000002UL, 0x31C00000_00000003UL }; // 1 + 2 -> 3
            yield return new object[] { 0x31A00000_0000000FUL, 0x31A00000_00000019UL, 0x31A00000_00000028UL }; // 1.5 + 2.5 -> 4.0
            yield return new object[] { 0x31A00000_00000001UL, 0x31A00000_00000002UL, 0x31A00000_00000003UL }; // 0.1 + 0.2 -> 0.3
            yield return new object[] { 0x31C00000_00000001UL, 0xB1C00000_00000001UL, 0x31C00000_00000000UL }; // 1 + -1 -> +0
            yield return new object[] { 0xB1C00000_00000001UL, 0x31C00000_00000001UL, 0x31C00000_00000000UL }; // -1 + 1 -> +0
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x31C00000_00000001UL, 0x31E38D7E_A4C68000UL }; // all-nines + 1 (carry/overflow to next magnitude)
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x6C7386F2_6FC0FFFFUL, 0x31E71AFD_498D0000UL }; // big + big (round)
            yield return new object[] { 0x31C00000_00000001UL, 0x2FC00000_00000001UL, 0x2FE38D7E_A4C68000UL }; // 1 + 1e-P (alignment beyond precision)
            yield return new object[] { 0x31C00000_00000001UL, 0x2F200000_00000001UL, 0x2FE38D7E_A4C68000UL }; // 1 + tiny (sticky rounding)
            yield return new object[] { 0x5FE38D7E_A4C68000UL, 0x5FE38D7E_A4C68000UL, 0x5FE71AFD_498D0000UL }; // max-ish + max-ish (overflow to Inf)
            yield return new object[] { 0x31000000_0012D687UL, 0x31000000_0074CBB1UL, 0x31000000_0087A238UL }; // cohort / preferred exponent
            yield return new object[] { 0x31C00000_00000064UL, 0x31600000_00000001UL, 0x31600000_000186A1UL }; // 100 + 0.001 (exponent spread)
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0xEC7386F2_6FC0FFFEUL, 0x31C00000_00000001UL }; // cancellation leaving small
            yield return new object[] { 0xAF200000_000023D2UL, 0xAFA34A58_43C82F35UL, 0xEBE0E772_A5D1D81BUL };
            yield return new object[] { 0x19C00000_0006B3C8UL, 0xB2400000_000014EDUL, 0xB0D30829_C20FD000UL };
            yield return new object[] { 0x31200054_42297730UL, 0x2F000063_A23BC84AUL, 0x30ACDB58_73BFC300UL };
            yield return new object[] { 0x2E60007D_E4F31691UL, 0x328000E1_F407BFE1UL, 0x6C827A4C_6EB74510UL };
            yield return new object[] { 0x95400091_BAB57FB9UL, 0x14600009_ED261C68UL, 0x94D63C8D_4F42A1D5UL };
            yield return new object[] { 0xB1E00000_173C0E97UL, 0x17C00575_228BEDE5UL, 0xB10DD951_783BC580UL };
            yield return new object[] { 0x31C00000_3838FC8EUL, 0x33800000_0008C901UL, 0x3254745E_CA115476UL };
            yield return new object[] { 0x33E00000_00000006UL, 0xB3000000_000738AFUL, 0x33000000_038C4E51UL };
            yield return new object[] { 0xB120007E_4A0EE845UL, 0xB1A2CD7D_00E19992UL, 0xB19C06E2_29247E66UL };
            yield return new object[] { 0xB1800000_003142FFUL, 0x2F4004A5_E17CD24DUL, 0xB06B7839_F143220AUL };
            yield return new object[] { 0x30A1AE18_98FE6B6EUL, 0xB2400000_00034D87UL, 0xB107B037_771252EFUL };
            yield return new object[] { 0x1040016E_281289C6UL, 0x2E417F41_004E3595UL, 0x2E2EF88A_030E17D2UL };
            yield return new object[] { 0x2F2160D1_63A6598CUL, 0xAD988DFF_DCC311D5UL, 0x2F0DC82D_E47F6478UL };
            yield return new object[] { 0xAF800000_000149F7UL, 0x82600001_9308E202UL, 0xAE3E0297_BAE1D800UL };
            yield return new object[] { 0xB3600000_0000230CUL, 0xB8600000_00000003UL, 0xB68AA87B_EE538000UL };
            yield return new object[] { 0xAD80816C_363173A8UL, 0x2FE00718_1926DD3EUL, 0x2F9BB622_3FD03A30UL };
            yield return new object[] { 0xB2800000_00000000UL, 0x2F000000_007AF558UL, 0x2F000000_007AF558UL };
            yield return new object[] { 0xAE836513_54C06345UL, 0x8D000000_0000002AUL, 0xEB99F2C1_4F83E0B2UL };
            yield return new object[] { 0xAE543A07_C082FDB3UL, 0x35200000_00000007UL, 0x3358DE76_816D8000UL };
            yield return new object[] { 0x32600000_0000B48AUL, 0x31400000_00015465UL, 0x31402A08_F77A3865UL };
            yield return new object[] { 0x33400000_000E8F6AUL, 0x33C00000_00000287UL, 0x33400000_007148DAUL };
            yield return new object[] { 0xAFE00000_0025FA34UL, 0x31C00000_000001BBUL, 0x302FBD0F_C05A7EC7UL };
            yield return new object[] { 0x31200015_EE481751UL, 0xB06385FA_ED15C9E7UL, 0x6C211C84_0F44FE09UL };
            yield return new object[] { 0xB3A00000_0001B644UL, 0x31400000_88C9241BUL, 0xB263FC6A_AB408FFEUL };
            yield return new object[] { 0xCE000000_2C155ADEUL, 0x2E400000_00000CC1UL, 0xCD3A4698_81BB8300UL };
            yield return new object[] { 0x2E0059A2_1C73F798UL, 0xAEC00000_0000038FUL, 0x2E0059A1_E62735D8UL };
            yield return new object[] { 0xB4400000_00000000UL, 0xB3C00000_00000002UL, 0xB3C00000_00000002UL };
            yield return new object[] { 0x14200000_1E148863UL, 0xAF600000_00000003UL, 0xAD8AA87B_EE538000UL };
            yield return new object[] { 0xB3000000_093826EBUL, 0x27600000_00000008UL, 0xB2257EC2_9E692780UL };
            yield return new object[] { 0xBDA00000_0054E714UL, 0xAEC00000_00083858UL, 0xBC93C497_9C5DC800UL };
            yield return new object[] { 0xBBE00000_00000008UL, 0xB1000000_00000008UL, 0xBA1C6BF5_26340000UL };
            yield return new object[] { 0xD8600000_72FA8C0BUL, 0x30404C87_C53B7F29UL, 0xD7A6DA6F_8B62D8C0UL };
            yield return new object[] { 0x31E00000_00011A74UL, 0x31EB8DA8_BD290BE9UL, 0x31EB8DA8_BD2A265DUL };
            yield return new object[] { 0xAE8000BE_61899933UL, 0x2E8E68EB_22D62BD8UL, 0x2E8E682C_C14C92A5UL };
            yield return new object[] { 0x30A00002_3C81B477UL, 0xB3400000_0000019EUL, 0xB1AEB54E_DD5EBFA0UL };
            yield return new object[] { 0xAFC00016_E7160E75UL, 0x32600000_00000013UL, 0x30A6C00A_39129993UL };
            yield return new object[] { 0x328000C6_0667F8FFUL, 0x437376D5_46AB54AAUL, 0x437376D5_46AB54AAUL };
            yield return new object[] { 0x316008E3_5687A600UL, 0x32C00000_000000BCUL, 0x316019FC_8DDA0600UL };
            yield return new object[] { 0x32600011_10EB5AFDUL, 0xB1200000_70F0F514UL, 0x31DA0A71_1FB6021CUL };
            yield return new object[] { 0x2F600000_0000018DUL, 0x34000000_000001F2UL, 0x3271B148_9AFB4000UL };
            yield return new object[] { 0x3260573E_35DB7C8EUL, 0x328018AA_7862D5CEUL, 0x32614DE6_E9B7D69AUL };
            yield return new object[] { 0x22C00000_00000005UL, 0xB0A00000_000004FDUL, 0xAF24896C_BB60D000UL };
            yield return new object[] { 0xD4200014_54458F18UL, 0xB2C00000_0000001DUL, 0xD39F0516_A377FF00UL };
            yield return new object[] { 0x30200000_0000004CUL, 0x324199F5_8C48AAF6UL, 0x32300397_7AD6AD9CUL };
            yield return new object[] { 0xEB90A2E6_7C00A0EDUL, 0xB2C00000_000020F5UL, 0xB15DF968_23F85000UL };
            yield return new object[] { 0xB282B0EE_E060C24AUL, 0x30600000_00000008UL, 0xB27AE954_C3C796E4UL };
            yield return new object[] { 0x2E20004A_73ED6947UL, 0x9C6F546E_573DB6B7UL, 0x2DAB5C50_69E06570UL };
            yield return new object[] { 0xB1600000_00000054UL, 0x30800000_00000500UL, 0xB0800000_32115D00UL };
            yield return new object[] { 0xAA4042B6_7AA7818BUL, 0x31800000_00000055UL, 0x2FDE32B4_78974000UL };
            yield return new object[] { 0xB2E00000_00000366UL, 0xB2C00000_00001F1DUL, 0xB2C00000_00004119UL };
            yield return new object[] { 0x33000000_00000002UL, 0x33200000_02DF7470UL, 0x33000000_1CBA8C62UL };
            yield return new object[] { 0xB062FAF8_87469CE0UL, 0x30200000_00000045UL, 0xB05DCDB5_48C220B9UL };
            yield return new object[] { 0xB0200000_000E661FUL, 0xB1000000_00076DFBUL, 0xB020046D_AB3E759FUL };
            yield return new object[] { 0xA9800000_01ECEE93UL, 0x2DC04A74_C2BEB838UL, 0x2D9D159C_127FF5E0UL };
            yield return new object[] { 0x29200001_1579BC2EUL, 0xAFC058BC_B025DCC1UL, 0xEBE2A9B4_CECA3B64UL };
            yield return new object[] { 0xB3C00000_00000012UL, 0xD5600000_0000001EUL, 0xD3AAA87B_EE538000UL };
            yield return new object[] { 0xAF400000_000003E0UL, 0xB2E00000_1892B9E7UL, 0xB20EA590_A3724D80UL };
            yield return new object[] { 0x32400000_1E910857UL, 0xB120000E_041E212AUL, 0x31723815_132D90B0UL };
            yield return new object[] { 0x2FE00000_36F3932DUL, 0x32000000_00003529UL, 0x30A4D5BB_39132B9AUL };
            yield return new object[] { 0x32C00001_B3BF92FFUL, 0x2FC00000_02D2E250UL, 0x3219F8FD_F0BB7DC0UL };
        }

        [Theory]
        [MemberData(nameof(op_Addition_TestData))]
        public static void op_Addition(ulong left, ulong right, ulong expected)
        {
            Decimal64 result = Unsafe.BitCast<ulong, Decimal64>(left) + Unsafe.BitCast<ulong, Decimal64>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(result));
        }

        public static IEnumerable<object[]> op_Subtraction_TestData()
        {
            yield return new object[] { 0xFC000000_00000000UL, 0x31C00000_00000001UL, 0xFC000000_00000000UL }; // NaN + 1 -> NaN (sub)
            yield return new object[] { 0x31C00000_00000001UL, 0xFC000000_00000000UL, 0xFC000000_00000000UL }; // 1 + NaN -> NaN (sub)
            yield return new object[] { 0x78000000_00000000UL, 0x31C00000_00000001UL, 0x78000000_00000000UL }; // +Inf + 1 -> +Inf (sub)
            yield return new object[] { 0x31C00000_00000001UL, 0x78000000_00000000UL, 0xF8000000_00000000UL }; // 1 + +Inf -> +Inf (sub)
            yield return new object[] { 0x78000000_00000000UL, 0xF8000000_00000000UL, 0x78000000_00000000UL }; // +Inf + -Inf -> NaN (sub)
            yield return new object[] { 0xF8000000_00000000UL, 0x78000000_00000000UL, 0xF8000000_00000000UL }; // -Inf + +Inf -> NaN (sub)
            yield return new object[] { 0x78000000_00000000UL, 0x78000000_00000000UL, 0xFC000000_00000000UL }; // +Inf + +Inf -> +Inf (sub)
            yield return new object[] { 0xF8000000_00000000UL, 0xF8000000_00000000UL, 0xFC000000_00000000UL }; // -Inf + -Inf -> -Inf (sub)
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000000UL, 0x31C00000_00000000UL }; // +0 + +0 -> +0 (sub)
            yield return new object[] { 0xB1C00000_00000000UL, 0xB1C00000_00000000UL, 0x31C00000_00000000UL }; // -0 + -0 -> -0 (sub)
            yield return new object[] { 0x31C00000_00000000UL, 0xB1C00000_00000000UL, 0x31C00000_00000000UL }; // +0 + -0 -> +0 (round-half-even) (sub)
            yield return new object[] { 0xB1C00000_00000000UL, 0x31C00000_00000000UL, 0xB1C00000_00000000UL }; // -0 + +0 -> +0 (sub)
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000000UL, 0x31C00000_00000001UL }; // 1 + 0 -> 1 (sub)
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000001UL, 0xB1C00000_00000001UL }; // 0 + 1 -> 1 (sub)
            yield return new object[] { 0x78000000_00000002UL, 0x31C00000_00000001UL, 0x78000000_00000000UL }; // non-canonical +Inf + 1 -> canonical +Inf (sub)
            yield return new object[] { 0x31C00000_00000001UL, 0xF8000000_00000005UL, 0x78000000_00000000UL }; // 1 + non-canonical -Inf -> canonical -Inf (sub)
            yield return new object[] { 0x78000000_0000000FUL, 0xF8000000_00000003UL, 0x78000000_00000000UL }; // non-canonical +Inf + non-canonical -Inf -> NaN (sub)
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000002UL, 0xB1C00000_00000001UL }; // 1 + 2 -> 3 (sub)
            yield return new object[] { 0x31A00000_0000000FUL, 0x31A00000_00000019UL, 0xB1A00000_0000000AUL }; // 1.5 + 2.5 -> 4.0 (sub)
            yield return new object[] { 0x31A00000_00000001UL, 0x31A00000_00000002UL, 0xB1A00000_00000001UL }; // 0.1 + 0.2 -> 0.3 (sub)
            yield return new object[] { 0x31C00000_00000001UL, 0xB1C00000_00000001UL, 0x31C00000_00000002UL }; // 1 + -1 -> +0 (sub)
            yield return new object[] { 0xB1C00000_00000001UL, 0x31C00000_00000001UL, 0xB1C00000_00000002UL }; // -1 + 1 -> +0 (sub)
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x31C00000_00000001UL, 0x6C7386F2_6FC0FFFEUL }; // all-nines + 1 (carry/overflow to next magnitude) (sub)
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x6C7386F2_6FC0FFFFUL, 0x31C00000_00000000UL }; // big + big (round) (sub)
            yield return new object[] { 0x31C00000_00000001UL, 0x2FC00000_00000001UL, 0x6BF386F2_6FC0FFFFUL }; // 1 + 1e-P (alignment beyond precision) (sub)
            yield return new object[] { 0x31C00000_00000001UL, 0x2F200000_00000001UL, 0x2FE38D7E_A4C68000UL }; // 1 + tiny (sticky rounding) (sub)
            yield return new object[] { 0x5FE38D7E_A4C68000UL, 0x5FE38D7E_A4C68000UL, 0x5FE00000_00000000UL }; // max-ish + max-ish (overflow to Inf) (sub)
            yield return new object[] { 0x31000000_0012D687UL, 0x31000000_0074CBB1UL, 0xB1000000_0061F52AUL }; // cohort / preferred exponent (sub)
            yield return new object[] { 0x31C00000_00000064UL, 0x31600000_00000001UL, 0x31600000_0001869FUL }; // 100 + 0.001 (exponent spread) (sub)
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0xEC7386F2_6FC0FFFEUL, 0x31E71AFD_498D0000UL }; // cancellation leaving small (sub)
            yield return new object[] { 0xAF200000_000023D2UL, 0xAFA34A58_43C82F35UL, 0x6BE0E772_A5D1D809UL };
            yield return new object[] { 0x19C00000_0006B3C8UL, 0xB2400000_000014EDUL, 0x30D30829_C20FD000UL };
            yield return new object[] { 0x31200054_42297730UL, 0x2F000063_A23BC84AUL, 0x30ACDB58_73BFC300UL };
            yield return new object[] { 0x2E60007D_E4F31691UL, 0x328000E1_F407BFE1UL, 0xEC827A4C_6EB74510UL };
            yield return new object[] { 0x95400091_BAB57FB9UL, 0x14600009_ED261C68UL, 0x94D63C8D_5457B34BUL };
            yield return new object[] { 0xB1E00000_173C0E97UL, 0x17C00575_228BEDE5UL, 0xB10DD951_783BC580UL };
            yield return new object[] { 0x31C00000_3838FC8EUL, 0x33800000_0008C901UL, 0xB254745E_CA0E738AUL };
            yield return new object[] { 0x33E00000_00000006UL, 0xB3000000_000738AFUL, 0x33000000_039ABFAFUL };
            yield return new object[] { 0xB120007E_4A0EE845UL, 0xB1A2CD7D_00E19992UL, 0x319C06E1_E87B8102UL };
            yield return new object[] { 0xB1800000_003142FFUL, 0x2F4004A5_E17CD24DUL, 0xB06B7839_F14349F6UL };
            yield return new object[] { 0x30A1AE18_98FE6B6EUL, 0xB2400000_00034D87UL, 0x3107B113_ACA02511UL };
            yield return new object[] { 0x1040016E_281289C6UL, 0x2E417F41_004E3595UL, 0xAE2EF88A_030E17D2UL };
            yield return new object[] { 0x2F2160D1_63A6598CUL, 0xAD988DFF_DCC311D5UL, 0x2F0DC82D_E47F9A78UL };
            yield return new object[] { 0xAF800000_000149F7UL, 0x82600001_9308E202UL, 0xAE3E0297_BAE1D800UL };
            yield return new object[] { 0xB3600000_0000230CUL, 0xB8600000_00000003UL, 0x368AA87B_EE538000UL };
            yield return new object[] { 0xAD80816C_363173A8UL, 0x2FE00718_1926DD3EUL, 0xAF9BB622_3FD03A30UL };
            yield return new object[] { 0xB2800000_00000000UL, 0x2F000000_007AF558UL, 0xAF000000_007AF558UL };
            yield return new object[] { 0xAE836513_54C06345UL, 0x8D000000_0000002AUL, 0xEB99F2C1_4F83E0B2UL };
            yield return new object[] { 0xAE543A07_C082FDB3UL, 0x35200000_00000007UL, 0xB358DE76_816D8000UL };
            yield return new object[] { 0x32600000_0000B48AUL, 0x31400000_00015465UL, 0x31402A08_F7778F9BUL };
            yield return new object[] { 0x33400000_000E8F6AUL, 0x33C00000_00000287UL, 0xB3400000_00542A06UL };
            yield return new object[] { 0xAFE00000_0025FA34UL, 0x31C00000_000001BBUL, 0xB02FBD0F_C05B4139UL };
            yield return new object[] { 0x31200015_EE481751UL, 0xB06385FA_ED15C9E7UL, 0x6C21D0E9_71E2F337UL };
            yield return new object[] { 0xB3A00000_0001B644UL, 0x31400000_88C9241BUL, 0xB263FC6A_AB409002UL };
            yield return new object[] { 0xCE000000_2C155ADEUL, 0x2E400000_00000CC1UL, 0xCD3A4698_81BB8300UL };
            yield return new object[] { 0x2E0059A2_1C73F798UL, 0xAEC00000_0000038FUL, 0x2E0059A2_52C0B958UL };
            yield return new object[] { 0xB4400000_00000000UL, 0xB3C00000_00000002UL, 0x33C00000_00000002UL };
            yield return new object[] { 0x14200000_1E148863UL, 0xAF600000_00000003UL, 0x2D8AA87B_EE538000UL };
            yield return new object[] { 0xB3000000_093826EBUL, 0x27600000_00000008UL, 0xB2257EC2_9E692780UL };
            yield return new object[] { 0xBDA00000_0054E714UL, 0xAEC00000_00083858UL, 0xBC93C497_9C5DC800UL };
            yield return new object[] { 0xBBE00000_00000008UL, 0xB1000000_00000008UL, 0xBA1C6BF5_26340000UL };
            yield return new object[] { 0xD8600000_72FA8C0BUL, 0x30404C87_C53B7F29UL, 0xD7A6DA6F_8B62D8C0UL };
            yield return new object[] { 0x31E00000_00011A74UL, 0x31EB8DA8_BD290BE9UL, 0xB1EB8DA8_BD27F175UL };
            yield return new object[] { 0xAE8000BE_61899933UL, 0x2E8E68EB_22D62BD8UL, 0xAE8E69A9_845FC50BUL };
            yield return new object[] { 0x30A00002_3C81B477UL, 0xB3400000_0000019EUL, 0x31AEB54E_DD5EC060UL };
            yield return new object[] { 0xAFC00016_E7160E75UL, 0x32600000_00000013UL, 0xB0A6C00A_3912E66DUL };
            yield return new object[] { 0x328000C6_0667F8FFUL, 0x437376D5_46AB54AAUL, 0xC37376D5_46AB54AAUL };
            yield return new object[] { 0x316008E3_5687A600UL, 0x32C00000_000000BCUL, 0xB1600835_E0CABA00UL };
            yield return new object[] { 0x32600011_10EB5AFDUL, 0xB1200000_70F0F514UL, 0x31DA0A71_1FB69624UL };
            yield return new object[] { 0x2F600000_0000018DUL, 0x34000000_000001F2UL, 0xB271B148_9AFB4000UL };
            yield return new object[] { 0x3260573E_35DB7C8EUL, 0x328018AA_7862D5CEUL, 0xB2609F6A_7E00DD7EUL };
            yield return new object[] { 0x22C00000_00000005UL, 0xB0A00000_000004FDUL, 0x2F24896C_BB60D000UL };
            yield return new object[] { 0xD4200014_54458F18UL, 0xB2C00000_0000001DUL, 0xD39F0516_A377FF00UL };
            yield return new object[] { 0x30200000_0000004CUL, 0x324199F5_8C48AAF6UL, 0xB2300397_7AD6AD9CUL };
            yield return new object[] { 0xEB90A2E6_7C00A0EDUL, 0xB2C00000_000020F5UL, 0x315DF968_23F85000UL };
            yield return new object[] { 0xB282B0EE_E060C24AUL, 0x30600000_00000008UL, 0xB27AE954_C3C796E4UL };
            yield return new object[] { 0x2E20004A_73ED6947UL, 0x9C6F546E_573DB6B7UL, 0x2DAB5C50_69E06570UL };
            yield return new object[] { 0xB1600000_00000054UL, 0x30800000_00000500UL, 0xB0800000_32116700UL };
            yield return new object[] { 0xAA4042B6_7AA7818BUL, 0x31800000_00000055UL, 0xAFDE32B4_78974000UL };
            yield return new object[] { 0xB2E00000_00000366UL, 0xB2C00000_00001F1DUL, 0xB2C00000_000002DFUL };
            yield return new object[] { 0x33000000_00000002UL, 0x33200000_02DF7470UL, 0xB3000000_1CBA8C5EUL };
            yield return new object[] { 0xB062FAF8_87469CE0UL, 0x30200000_00000045UL, 0xB05DCDB5_48C220C7UL };
            yield return new object[] { 0xB0200000_000E661FUL, 0xB1000000_00076DFBUL, 0x3020046D_AB21A961UL };
            yield return new object[] { 0xA9800000_01ECEE93UL, 0x2DC04A74_C2BEB838UL, 0xAD9D159C_127FF5E0UL };
            yield return new object[] { 0x29200001_1579BC2EUL, 0xAFC058BC_B025DCC1UL, 0x6BE2A9B4_CECA3B64UL };
            yield return new object[] { 0xB3C00000_00000012UL, 0xD5600000_0000001EUL, 0x53AAA87B_EE538000UL };
            yield return new object[] { 0xAF400000_000003E0UL, 0xB2E00000_1892B9E7UL, 0x320EA590_A3724D80UL };
            yield return new object[] { 0x32400000_1E910857UL, 0xB120000E_041E212AUL, 0x31723815_5AF0BA50UL };
            yield return new object[] { 0x2FE00000_36F3932DUL, 0x32000000_00003529UL, 0xB0A4D5BB_39132466UL };
            yield return new object[] { 0x32C00001_B3BF92FFUL, 0x2FC00000_02D2E250UL, 0x3219F8FD_F0BB7DC0UL };
        }

        [Theory]
        [MemberData(nameof(op_Subtraction_TestData))]
        public static void op_Subtraction(ulong left, ulong right, ulong expected)
        {
            Decimal64 result = Unsafe.BitCast<ulong, Decimal64>(left) - Unsafe.BitCast<ulong, Decimal64>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(result));
        }

    }
}
