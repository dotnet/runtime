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

        public static IEnumerable<object[]> Zero_NonCanonicalEncodings_TestData()
        {
            // A finite encoding whose significand exceeds MaxSignificand (9_999_999_999_999_999) is non-canonical and represents zero.
            const ulong PositiveZero = 0x31C0_0000_0000_0000;
            const ulong NegativeZero = 0xB1C0_0000_0000_0000;

            yield return new object[] { PositiveZero, 0x6C77_FFFF_FFFF_FFFFUL };
            yield return new object[] { PositiveZero, 0x6C73_86F2_6FC1_0000UL };
            yield return new object[] { NegativeZero, 0xEC77_FFFF_FFFF_FFFFUL };
            yield return new object[] { NegativeZero, 0xEC73_86F2_6FC1_0000UL };
        }

        [Theory]
        [MemberData(nameof(Zero_NonCanonicalEncodings_TestData))]
        public static void Finite_NonCanonicalEncodings_BehaveAsZero(ulong canonicalZero, ulong encoding)
        {
            Decimal64 zero = Unsafe.BitCast<ulong, Decimal64>(canonicalZero);
            Decimal64 nc = Unsafe.BitCast<ulong, Decimal64>(encoding);
            Decimal64 one = Unsafe.BitCast<ulong, Decimal64>(0x31C0_0000_0000_0001UL);
            Decimal64 inf = Decimal64.PositiveInfinity;

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

            static ulong Bits(Decimal64 value) => Unsafe.BitCast<Decimal64, ulong>(value);
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
            yield return new object[] { 0x78000000_00000000UL, 0xF8000000_00000000UL, 0x7C000000_00000000UL }; // +Inf + -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xF8000000_00000000UL, 0x78000000_00000000UL, 0x7C000000_00000000UL }; // -Inf + +Inf -> +QNaN (canonical invalid-operation result)
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
            yield return new object[] { 0x78000000_0000000FUL, 0xF8000000_00000003UL, 0x7C000000_00000000UL }; // non-canonical +Inf + non-canonical -Inf -> +QNaN (canonical invalid-operation result)
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
            yield return new object[] { 0xFC000000_00000000UL, 0x31C00000_00000001UL, 0xFC000000_00000000UL }; // NaN - 1 -> NaN
            yield return new object[] { 0x31C00000_00000001UL, 0xFC000000_00000000UL, 0xFC000000_00000000UL }; // 1 - NaN -> NaN
            yield return new object[] { 0x78000000_00000000UL, 0x31C00000_00000001UL, 0x78000000_00000000UL }; // +Inf - 1 -> +Inf
            yield return new object[] { 0x31C00000_00000001UL, 0x78000000_00000000UL, 0xF8000000_00000000UL }; // 1 - +Inf -> -Inf
            yield return new object[] { 0x78000000_00000000UL, 0xF8000000_00000000UL, 0x78000000_00000000UL }; // +Inf - -Inf -> +Inf
            yield return new object[] { 0xF8000000_00000000UL, 0x78000000_00000000UL, 0xF8000000_00000000UL }; // -Inf - +Inf -> -Inf
            yield return new object[] { 0x78000000_00000000UL, 0x78000000_00000000UL, 0x7C000000_00000000UL }; // +Inf - +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xF8000000_00000000UL, 0xF8000000_00000000UL, 0x7C000000_00000000UL }; // -Inf - -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000000UL, 0x31C00000_00000000UL }; // +0 - +0 -> +0
            yield return new object[] { 0xB1C00000_00000000UL, 0xB1C00000_00000000UL, 0x31C00000_00000000UL }; // -0 - -0 -> +0 (round-half-even)
            yield return new object[] { 0x31C00000_00000000UL, 0xB1C00000_00000000UL, 0x31C00000_00000000UL }; // +0 - -0 -> +0
            yield return new object[] { 0xB1C00000_00000000UL, 0x31C00000_00000000UL, 0xB1C00000_00000000UL }; // -0 - +0 -> -0
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000000UL, 0x31C00000_00000001UL }; // 1 - 0 -> 1
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000001UL, 0xB1C00000_00000001UL }; // 0 - 1 -> -1
            yield return new object[] { 0x78000000_00000002UL, 0x31C00000_00000001UL, 0x78000000_00000000UL }; // non-canonical +Inf - 1 -> canonical +Inf
            yield return new object[] { 0x31C00000_00000001UL, 0xF8000000_00000005UL, 0x78000000_00000000UL }; // 1 - non-canonical -Inf -> canonical +Inf
            yield return new object[] { 0x78000000_0000000FUL, 0xF8000000_00000003UL, 0x78000000_00000000UL }; // non-canonical +Inf - non-canonical -Inf -> canonical +Inf
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000002UL, 0xB1C00000_00000001UL }; // 1 - 2 -> -1
            yield return new object[] { 0x31A00000_0000000FUL, 0x31A00000_00000019UL, 0xB1A00000_0000000AUL }; // 1.5 - 2.5 -> -1.0
            yield return new object[] { 0x31A00000_00000001UL, 0x31A00000_00000002UL, 0xB1A00000_00000001UL }; // 0.1 - 0.2 -> -0.1
            yield return new object[] { 0x31C00000_00000001UL, 0xB1C00000_00000001UL, 0x31C00000_00000002UL }; // 1 - -1 -> 2
            yield return new object[] { 0xB1C00000_00000001UL, 0x31C00000_00000001UL, 0xB1C00000_00000002UL }; // -1 - 1 -> -2
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x31C00000_00000001UL, 0x6C7386F2_6FC0FFFEUL }; // all-nines - 1 -> 9_999_999_999_999_998
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x6C7386F2_6FC0FFFFUL, 0x31C00000_00000000UL }; // big - big -> +0
            yield return new object[] { 0x31C00000_00000001UL, 0x2FC00000_00000001UL, 0x6BF386F2_6FC0FFFFUL }; // 1 - 1e-16 -> 0.9999999999999999 (alignment beyond precision)
            yield return new object[] { 0x31C00000_00000001UL, 0x2F200000_00000001UL, 0x2FE38D7E_A4C68000UL }; // 1 - 1e-21 -> 1.000000000000000 (sticky rounding)
            yield return new object[] { 0x5FE38D7E_A4C68000UL, 0x5FE38D7E_A4C68000UL, 0x5FE00000_00000000UL }; // max-ish - max-ish -> +0 (preferred exponent retained)
            yield return new object[] { 0x31000000_0012D687UL, 0x31000000_0074CBB1UL, 0xB1000000_0061F52AUL }; // cohort / preferred exponent
            yield return new object[] { 0x31C00000_00000064UL, 0x31600000_00000001UL, 0x31600000_0001869FUL }; // 100 - 0.001 -> 99.999 (exponent spread)
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0xEC7386F2_6FC0FFFEUL, 0x31E71AFD_498D0000UL }; // opposite signs subtract as add: 9_999_999_999_999_999 - -9_999_999_999_999_998 -> 2.0e16 (carry/rounding)
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

        public static IEnumerable<object[]> op_Equality_TestData()
        {
            yield return new object[] { 0xFC000000_00000000UL, 0xFC000000_00000000UL, false }; // NaN == NaN -> false
            yield return new object[] { 0xFC000000_00000000UL, 0x31C00000_00000001UL, false }; // NaN == 1 -> false
            yield return new object[] { 0x31C00000_00000001UL, 0xFC000000_00000000UL, false }; // 1 == NaN -> false
            yield return new object[] { 0xFC000000_00000000UL, 0x78000000_00000000UL, false }; // NaN == +Inf -> false
            yield return new object[] { 0xFE000000_00000000UL, 0xFE000000_00000000UL, false }; // sNaN == sNaN -> false
            yield return new object[] { 0xFE000000_00000000UL, 0x31C00000_00000001UL, false }; // sNaN == 1 -> false
            yield return new object[] { 0x78000000_00000000UL, 0x78000000_00000000UL, true }; // +Inf == +Inf -> true
            yield return new object[] { 0xF8000000_00000000UL, 0xF8000000_00000000UL, true }; // -Inf == -Inf -> true
            yield return new object[] { 0x78000000_00000000UL, 0xF8000000_00000000UL, false }; // +Inf == -Inf -> false
            yield return new object[] { 0x78000000_00000000UL, 0x31C00000_00000001UL, false }; // +Inf == 1 -> false
            yield return new object[] { 0x78000000_00000002UL, 0x78000000_00000000UL, true }; // non-canonical +Inf == +Inf -> true
            yield return new object[] { 0xF8000000_00000005UL, 0xF8000000_00000000UL, true }; // non-canonical -Inf == -Inf -> true
            yield return new object[] { 0x31C00000_00000000UL, 0xB1C00000_00000000UL, true }; // +0 == -0 -> true
            yield return new object[] { 0xB1C00000_00000000UL, 0x31C00000_00000000UL, true }; // -0 == +0 -> true
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000000UL, true }; // +0 == +0 -> true
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000001UL, true }; // 1 == 1 -> true
            yield return new object[] { 0x31C00000_00000001UL, 0xB1C00000_00000001UL, false }; // 1 == -1 -> false
            yield return new object[] { 0xB1C00000_00000001UL, 0x31C00000_00000001UL, false }; // -1 == 1 -> false
            yield return new object[] { 0x31C00000_00000001UL, 0x31A00000_0000000AUL, true }; // 1 == 1.0 -> true (cohort)
            yield return new object[] { 0x31A00000_0000000AUL, 0x31C00000_00000001UL, true }; // 1.0 == 1 -> true (cohort)
            yield return new object[] { 0x31800000_00000064UL, 0x31A00000_0000000AUL, true }; // 1.00 == 1.0 -> true (cohort)
            yield return new object[] { 0x31C00000_00000064UL, 0x32000000_00000001UL, true }; // 100 == 1e2 -> true (cohort)
            yield return new object[] { 0x31C00000_0000000AUL, 0x31C00000_00000001UL, false }; // 10 == 1 -> false
            yield return new object[] { 0x5FE38D7E_A4C68000UL, 0x5FE38D7E_A4C68000UL, true }; // large == large -> true
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x6C7386F2_6FC0FFFFUL, true }; // all-nines == all-nines -> true
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x6C7386F2_6FC0FFFEUL, false }; // all-nines == near -> false
            yield return new object[] { 0x31A00000_00000001UL, 0x31A00000_00000001UL, true }; // 0.1 == 0.1 -> true
            yield return new object[] { 0xB1A00000_00000001UL, 0x31A00000_00000001UL, false }; // -0.1 == 0.1 -> false
            yield return new object[] { 0x32600000_00000000UL, 0x31C00000_00000000UL, true }; // +0 (exp 5) == +0 -> true (zero cohort)
            yield return new object[] { 0xB1A008C2_84977883UL, 0x2E800000_0088FC67UL, false };
            yield return new object[] { 0x32E00000_001FC878UL, 0x32C00000_013DD4B0UL, true };
            yield return new object[] { 0x2EA0391E_4F49DE8FUL, 0x9F400000_00001DF5UL, false };
            yield return new object[] { 0xAEC00000_001D0824UL, 0xAE800000_0B572E10UL, true };
            yield return new object[] { 0xAF800000_2C48DC3AUL, 0xAF600001_BAD89A44UL, true };
            yield return new object[] { 0xB2A00000_001E3059UL, 0xB2600000_0BCAE2C4UL, true };
            yield return new object[] { 0x33600000_00000006UL, 0x33200000_00000258UL, true };
            yield return new object[] { 0xB0600000_00010528UL, 0xB0400000_000A3390UL, true };
            yield return new object[] { 0x31A00000_000003D9UL, 0x31400000_000F07A8UL, true };
            yield return new object[] { 0xB1000000_000014DFUL, 0xA2A00000_000493D9UL, false };
            yield return new object[] { 0xB1A2045D_952633E9UL, 0xB1942BA7_D37E071AUL, true };
            yield return new object[] { 0x18C00000_0000AEAEUL, 0x31C00000_3838FC8EUL, false };
            yield return new object[] { 0x33800000_0008C901UL, 0x33E00000_00000006UL, false };
            yield return new object[] { 0xB3000000_000738AFUL, 0xB2C00000_02D2245CUL, true };
            yield return new object[] { 0xB1C03D4D_B6F5E4F8UL, 0xB197F25B_780D70E0UL, true };
            yield return new object[] { 0x30800000_000003D6UL, 0x2F400664_5DF1A27CUL, false };
            yield return new object[] { 0x30A1AE18_98FE6B6EUL, 0xB2400000_00034D87UL, false };
            yield return new object[] { 0x1040016E_281289C6UL, 0x30000000_00000005UL, false };
            yield return new object[] { 0x2F2160D1_63A6598CUL, 0x2F0DC82D_E47F7F78UL, true };
            yield return new object[] { 0x30E00000_00000C4AUL, 0xB2200000_000025CFUL, false };
            yield return new object[] { 0xB2203A12_8BBE5CE9UL, 0xB20244B9_756FA11AUL, true };
            yield return new object[] { 0xB0E00013_1E45601FUL, 0xB0C000BF_2EB5C136UL, true };
            yield return new object[] { 0x2FC00001_6D25307DUL, 0x34000000_00000007UL, false };
            yield return new object[] { 0x2E406792_5AAB2BB2UL, 0x30604337_B2B054C2UL, false };
            yield return new object[] { 0x6B7B4EB4_CA653CC8UL, 0x31600000_0000DC60UL, false };
            yield return new object[] { 0xB3400000_00000160UL, 0x2DC77033_1BF19A20UL, false };
            yield return new object[] { 0xCC001DE4_66BAF9CBUL, 0xAF600000_0023E448UL, false };
            yield return new object[] { 0x2FA00000_00000008UL, 0x33C00000_00000287UL, false };
            yield return new object[] { 0xAFE00000_0025FA34UL, 0xAF604373_91C90C36UL, false };
            yield return new object[] { 0xB3C00000_00000127UL, 0xA21F2123_A600AF10UL, false };
            yield return new object[] { 0xAEC369E6_4F6A2F25UL, 0xEBAA22FF_1A25D772UL, true };
            yield return new object[] { 0x2F800000_00004BEFUL, 0x2F400000_001DA95CUL, true };
            yield return new object[] { 0x21400000_000001A3UL, 0x21200000_0000105EUL, true };
            yield return new object[] { 0x2EC00000_000E96B9UL, 0x2E600000_38FCC2A8UL, true };
            yield return new object[] { 0xB22014D6_070C597FUL, 0x3169A927_D9913FE9UL, false };
            yield return new object[] { 0xAF600000_00000003UL, 0xAF000000_00000BB8UL, true };
            yield return new object[] { 0x30400011_8446412EUL, 0xBDA00000_0054E714UL, false };
            yield return new object[] { 0xAEC00000_00083858UL, 0xAEA00000_00523370UL, true };
            yield return new object[] { 0xC5E3830C_56EBAA74UL, 0xF1731E7B_6534A888UL, true };
            yield return new object[] { 0xD8600000_72FA8C0BUL, 0xD8400004_7DC9786EUL, true };
            yield return new object[] { 0xB1D51B94_C03688D2UL, 0xB0000000_0007D313UL, false };
            yield return new object[] { 0xB1C00000_000002AAUL, 0x2FA00000_05391305UL, false };
            yield return new object[] { 0xB0000000_038654B6UL, 0xAFE00000_233F4F1CUL, true };
            yield return new object[] { 0x2EE00001_EA1CAC6FUL, 0x2E80077A_80019198UL, true };
            yield return new object[] { 0x5660DA0F_B478C74AUL, 0x5648849D_0CB7C8E4UL, true };
            yield return new object[] { 0xAFC00000_0000DB6BUL, 0xAF600000_035919F8UL, true };
            yield return new object[] { 0x2F000000_000001EFUL, 0x2EA00000_00078D98UL, true };
            yield return new object[] { 0x2E62F469_D9824BA0UL, 0x4DF4BD37_15B124F2UL, false };
            yield return new object[] { 0xB1000000_00014173UL, 0xB0A00000_04E7A938UL, true };
            yield return new object[] { 0xAE6004BD_94F49D11UL, 0xAE402F67_D18E22AAUL, true };
            yield return new object[] { 0x33600000_00B80D1DUL, 0x3260573E_35DB7C8EUL, false };
            yield return new object[] { 0x328018AA_7862D5CEUL, 0x22C00000_00000005UL, false };
            yield return new object[] { 0xB0A00000_000004FDUL, 0xB0400000_00137C48UL, true };
            yield return new object[] { 0x31C00000_000BC1FDUL, 0x31800000_0497C6D4UL, true };
            yield return new object[] { 0x58C11505_F3B6E6F2UL, 0x324199F5_8C48AAF6UL, false };
            yield return new object[] { 0xEB90A2E6_7C00A0EDUL, 0xB27AFF08_C857C807UL, false };
            yield return new object[] { 0x30600000_00000008UL, 0x30200000_00000320UL, true };
            yield return new object[] { 0xB2E00000_00000057UL, 0xB2800000_000153D8UL, true };
            yield return new object[] { 0xB1600000_05307358UL, 0x1CA00000_3594B625UL, false };
            yield return new object[] { 0xAA4042B6_7AA7818BUL, 0xAA229B20_CA8B0F6EUL, true };
        }

        [Theory]
        [MemberData(nameof(op_Equality_TestData))]
        public static void op_Equality(ulong left, ulong right, bool expected)
        {
            Decimal64 l = Unsafe.BitCast<ulong, Decimal64>(left);
            Decimal64 r = Unsafe.BitCast<ulong, Decimal64>(right);
            Assert.Equal(expected, l == r);
            Assert.Equal(!expected, l != r);
        }

        public static IEnumerable<object[]> op_Comparison_TestData()
        {
            yield return new object[] { 0xFC000000_00000000UL, 0x31C00000_00000001UL, false, false, false, false }; // NaN vs 1 -> unordered
            yield return new object[] { 0x31C00000_00000001UL, 0xFC000000_00000000UL, false, false, false, false }; // 1 vs NaN -> unordered
            yield return new object[] { 0xFC000000_00000000UL, 0xFC000000_00000000UL, false, false, false, false }; // NaN vs NaN -> unordered
            yield return new object[] { 0xFE000000_00000000UL, 0x31C00000_00000001UL, false, false, false, false }; // sNaN vs 1 -> unordered
            yield return new object[] { 0xFC000000_00000000UL, 0x78000000_00000000UL, false, false, false, false }; // NaN vs +Inf -> unordered
            yield return new object[] { 0x78000000_00000000UL, 0x31C00000_00000001UL, false, true, false, true }; // +Inf vs 1
            yield return new object[] { 0x31C00000_00000001UL, 0x78000000_00000000UL, true, false, true, false }; // 1 vs +Inf
            yield return new object[] { 0xF8000000_00000000UL, 0x31C00000_00000001UL, true, false, true, false }; // -Inf vs 1
            yield return new object[] { 0x31C00000_00000001UL, 0xF8000000_00000000UL, false, true, false, true }; // 1 vs -Inf
            yield return new object[] { 0xF8000000_00000000UL, 0x78000000_00000000UL, true, false, true, false }; // -Inf vs +Inf
            yield return new object[] { 0x78000000_00000000UL, 0x78000000_00000000UL, false, false, true, true }; // +Inf vs +Inf (equal)
            yield return new object[] { 0xF8000000_00000000UL, 0xF8000000_00000000UL, false, false, true, true }; // -Inf vs -Inf (equal)
            yield return new object[] { 0x78000000_00000002UL, 0x78000000_00000000UL, false, false, true, true }; // non-canonical +Inf vs +Inf (equal)
            yield return new object[] { 0x31C00000_00000000UL, 0xB1C00000_00000000UL, false, false, true, true }; // +0 vs -0 (equal)
            yield return new object[] { 0xB1C00000_00000000UL, 0x31C00000_00000000UL, false, false, true, true }; // -0 vs +0 (equal)
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000001UL, true, false, true, false }; // 0 vs 1
            yield return new object[] { 0xB1C00000_00000001UL, 0x31C00000_00000000UL, true, false, true, false }; // -1 vs 0
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000002UL, true, false, true, false }; // 1 vs 2
            yield return new object[] { 0x31C00000_00000002UL, 0x31C00000_00000001UL, false, true, false, true }; // 2 vs 1
            yield return new object[] { 0xB1C00000_00000001UL, 0x31C00000_00000001UL, true, false, true, false }; // -1 vs 1
            yield return new object[] { 0x31C00000_00000001UL, 0xB1C00000_00000001UL, false, true, false, true }; // 1 vs -1
            yield return new object[] { 0xB1C00000_00000001UL, 0xB1C00000_00000002UL, false, true, false, true }; // -1 vs -2
            yield return new object[] { 0x31C00000_00000001UL, 0x31A00000_0000000AUL, false, false, true, true }; // 1 vs 1.0 (cohort equal)
            yield return new object[] { 0x31A00000_0000000AUL, 0x31C00000_00000001UL, false, false, true, true }; // 1.0 vs 1 (cohort equal)
            yield return new object[] { 0x31C00000_00000064UL, 0x32000000_00000001UL, false, false, true, true }; // 100 vs 1e2 (cohort equal)
            yield return new object[] { 0x31C00000_0000000AUL, 0x31C00000_00000001UL, false, true, false, true }; // 10 vs 1
            yield return new object[] { 0x31A00000_00000001UL, 0x31A00000_00000002UL, true, false, true, false }; // 0.1 vs 0.2
            yield return new object[] { 0x31000000_0098967FUL, 0x31000000_0098967EUL, false, true, false, true }; // close values
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x6C7386F2_6FC0FFFEUL, false, true, false, true }; // all-nines vs near
            yield return new object[] { 0x5FE38D7E_A4C68000UL, 0x5FE05AF3_107A4000UL, false, true, false, true }; // large magnitudes
            yield return new object[] { 0xDFE38D7E_A4C68000UL, 0x5FE38D7E_A4C68000UL, true, false, true, false }; // -large vs +large
            yield return new object[] { 0xB2400000_000014EDUL, 0x31C00000_002A5135UL, true, false, true, false };
            yield return new object[] { 0x2F000063_A23BC84AUL, 0x2E60007D_E4F31691UL, false, true, false, true };
            yield return new object[] { 0x328000E1_F407BFE1UL, 0xB1000000_000014DFUL, false, true, false, true };
            yield return new object[] { 0xA2200001_2C5C9406UL, 0xB1A2045D_952633E9UL, false, true, false, true };
            yield return new object[] { 0x2F600004_B07A97FAUL, 0x31C00000_3838FC8EUL, true, false, true, false };
            yield return new object[] { 0x33800000_0008C901UL, 0x33E00000_00000006UL, false, true, false, true };
            yield return new object[] { 0xB3000000_000738AFUL, 0xEC2B57F6_AEDFA2F6UL, true, false, true, false };
            yield return new object[] { 0xB1A2CD7D_00E19992UL, 0xB1800000_003142FFUL, true, false, true, false };
            yield return new object[] { 0x2F4004A5_E17CD24DUL, 0x30A1AE18_98FE6B6EUL, true, false, true, false };
            yield return new object[] { 0xB2400000_00034D87UL, 0xB0A00000_000000C7UL, true, false, true, false };
            yield return new object[] { 0x30000000_00000005UL, 0x2F2160D1_63A6598CUL, true, false, true, false };
            yield return new object[] { 0xAD988DFF_DCC311D5UL, 0xB0000691_D863649AUL, false, true, false, true };
            yield return new object[] { 0x30627FCF_B909892FUL, 0x3058FE1D_3A5F5BD6UL, false, false, true, true };
            yield return new object[] { 0xB3600000_0000230CUL, 0xB3400000_00015E78UL, false, false, true, true };
            yield return new object[] { 0x2FC00001_6D25307DUL, 0x34000000_00000007UL, true, false, true, false };
            yield return new object[] { 0x2E406792_5AAB2BB2UL, 0x30604337_B2B054C2UL, true, false, true, false };
            yield return new object[] { 0x6B7B4EB4_CA653CC8UL, 0xAF400000_0000008FUL, false, true, false, true };
            yield return new object[] { 0xB3C00000_00000001UL, 0x2DC77033_1BF19A20UL, true, false, true, false };
            yield return new object[] { 0xCC001DE4_66BAF9CBUL, 0xAF600000_0023E448UL, true, false, true, false };
            yield return new object[] { 0x2FA00000_00000008UL, 0x33C00000_00000287UL, true, false, true, false };
            yield return new object[] { 0xAFE00000_0025FA34UL, 0xAF604373_91C90C36UL, false, true, false, true };
            yield return new object[] { 0xB3C00000_00000127UL, 0xA21F2123_A600AF10UL, true, false, true, false };
            yield return new object[] { 0xAEC369E6_4F6A2F25UL, 0xCE000000_2C155ADEUL, false, true, false, true };
            yield return new object[] { 0x2E400000_00000CC1UL, 0x2EE00000_006399AAUL, true, false, true, false };
            yield return new object[] { 0xAE2000E8_7DB2F031UL, 0xB22014D6_070C597FUL, false, true, false, true };
            yield return new object[] { 0x14200000_1E148863UL, 0x13E0000B_C00546ACUL, false, false, true, true };
            yield return new object[] { 0xB3E00000_00000004UL, 0xB3800000_00000FA0UL, false, false, true, true };
            yield return new object[] { 0x30600000_00000192UL, 0x2DB14774_E29D36EFUL, false, true, false, true };
            yield return new object[] { 0xBA404693_038D50F9UL, 0xB040009D_38654929UL, true, false, true, false };
            yield return new object[] { 0xB4000000_00000009UL, 0x30C00000_000001EBUL, true, false, true, false };
            yield return new object[] { 0x32800029_6783D33DUL, 0xB1C00000_000002AAUL, false, true, false, true };
            yield return new object[] { 0x2FA00000_05391305UL, 0x2F400014_66F24B88UL, false, false, true, true };
            yield return new object[] { 0xB3800000_00006C63UL, 0xB3600000_00043BDEUL, false, false, true, true };
            yield return new object[] { 0x2EE00001_EA1CAC6FUL, 0x5660DA0F_B478C74AUL, true, false, true, false };
            yield return new object[] { 0xB7C00000_03E9AF8BUL, 0x328000C6_0667F8FFUL, true, false, true, false };
            yield return new object[] { 0x437376D5_46AB54AAUL, 0x2E62F469_D9824BA0UL, false, true, false, true };
            yield return new object[] { 0x09200001_FEBFFAC0UL, 0xB1200000_70F0F514UL, false, true, false, true };
            yield return new object[] { 0x2F600000_0000018DUL, 0x2F200000_00009B14UL, false, false, true, true };
            yield return new object[] { 0x31C00000_0093B3B1UL, 0x32200000_03650DEAUL, true, false, true, false };
            yield return new object[] { 0x32C00000_0072C76FUL, 0x22C00000_00000005UL, false, true, false, true };
            yield return new object[] { 0xB0A00000_000004FDUL, 0x31C00000_000BC1FDUL, true, false, true, false };
            yield return new object[] { 0xB2000000_03255E52UL, 0xB1A0000C_49F87050UL, false, false, true, true };
            yield return new object[] { 0xB2600000_00012FDAUL, 0x31200000_007A3E03UL, true, false, true, false };
            yield return new object[] { 0xB18041EB_25020508UL, 0x2FE00040_A971BF9CUL, true, false, true, false };
            yield return new object[] { 0x33800000_0000B296UL, 0x30E00000_0000034FUL, false, true, false, true };
            yield return new object[] { 0xB1600000_05307358UL, 0x1CA00000_3594B625UL, true, false, true, false };
            yield return new object[] { 0xAA4042B6_7AA7818BUL, 0xAA229B20_CA8B0F6EUL, false, false, true, true };
            yield return new object[] { 0xB3000000_38B20AF3UL, 0xAE800003_9C24FB35UL, true, false, true, false };
            yield return new object[] { 0xB0A00000_2F4E733BUL, 0x31800000_00013536UL, true, false, true, false };
            yield return new object[] { 0xB062FAF8_87469CE0UL, 0x30200000_00000045UL, true, false, true, false };
            yield return new object[] { 0xB0200000_000E661FUL, 0xAFE3054C_CC540881UL, false, true, false, true };
            yield return new object[] { 0xA9800000_01ECEE93UL, 0x25200000_0412E566UL, true, false, true, false };
            yield return new object[] { 0xAF800002_24F7B6CAUL, 0x2E800000_2F26BC28UL, true, false, true, false };
            yield return new object[] { 0x18000001_F084CCA2UL, 0xADE00A2A_BF8CFA22UL, false, true, false, true };
            yield return new object[] { 0xB2E00000_1892B9E7UL, 0x31726749_CD4380CAUL, true, false, true, false };
            yield return new object[] { 0xB120000E_041E212AUL, 0x34000000_00000336UL, true, false, true, false };
            yield return new object[] { 0x32000000_00003529UL, 0x32A00000_25E00AC4UL, true, false, true, false };
            yield return new object[] { 0x2FC00000_02D2E250UL, 0x30A00000_00000001UL, false, true, false, true };
            yield return new object[] { 0xB0200000_045F9E3BUL, 0xC8A00002_48B351F0UL, false, true, false, true };
            yield return new object[] { 0xAE305989_14E871DFUL, 0xB1400000_00000035UL, false, true, false, true };
        }

        [Theory]
        [MemberData(nameof(op_Comparison_TestData))]
        public static void op_Comparison(ulong left, ulong right, bool lessThan, bool greaterThan, bool lessThanOrEqual, bool greaterThanOrEqual)
        {
            Decimal64 l = Unsafe.BitCast<ulong, Decimal64>(left);
            Decimal64 r = Unsafe.BitCast<ulong, Decimal64>(right);
            Assert.Equal(lessThan, l < r);
            Assert.Equal(greaterThan, l > r);
            Assert.Equal(lessThanOrEqual, l <= r);
            Assert.Equal(greaterThanOrEqual, l >= r);
        }

        public static IEnumerable<object[]> op_Multiply_TestData()
        {
            yield return new object[] { 0xFC000000_00000000UL, 0x31C00000_00000001UL, 0xFC000000_00000000UL }; // NaN * 1 -> NaN
            yield return new object[] { 0x31C00000_00000001UL, 0xFC000000_00000000UL, 0xFC000000_00000000UL }; // 1 * NaN -> NaN
            yield return new object[] { 0xFC000000_00000000UL, 0x78000000_00000000UL, 0xFC000000_00000000UL }; // NaN * +Inf -> NaN
            yield return new object[] { 0x78000000_00000000UL, 0x31C00000_00000000UL, 0x7C000000_00000000UL }; // +Inf * +0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x31C00000_00000000UL, 0x78000000_00000000UL, 0x7C000000_00000000UL }; // +0 * +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xF8000000_00000000UL, 0x31C00000_00000000UL, 0x7C000000_00000000UL }; // -Inf * +0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x78000000_00000000UL, 0xB1C00000_00000000UL, 0x7C000000_00000000UL }; // +Inf * -0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x78000000_00000000UL, 0x31C00000_00000001UL, 0x78000000_00000000UL }; // +Inf * 1 -> +Inf
            yield return new object[] { 0x78000000_00000000UL, 0xB1C00000_00000001UL, 0xF8000000_00000000UL }; // +Inf * -1 -> -Inf
            yield return new object[] { 0xF8000000_00000000UL, 0x31C00000_00000002UL, 0xF8000000_00000000UL }; // -Inf * 2 -> -Inf
            yield return new object[] { 0xF8000000_00000000UL, 0xB1C00000_00000002UL, 0x78000000_00000000UL }; // -Inf * -2 -> +Inf
            yield return new object[] { 0x78000000_00000000UL, 0x78000000_00000000UL, 0x78000000_00000000UL }; // +Inf * +Inf -> +Inf
            yield return new object[] { 0x78000000_00000000UL, 0xF8000000_00000000UL, 0xF8000000_00000000UL }; // +Inf * -Inf -> -Inf
            yield return new object[] { 0xF8000000_00000000UL, 0xF8000000_00000000UL, 0x78000000_00000000UL }; // -Inf * -Inf -> +Inf
            yield return new object[] { 0x78000000_00000002UL, 0x31C00000_00000001UL, 0x78000000_00000000UL }; // non-canonical +Inf * 1 -> canonical +Inf
            yield return new object[] { 0x31C00000_00000001UL, 0xF8000000_00000005UL, 0xF8000000_00000000UL }; // 1 * non-canonical -Inf -> canonical -Inf
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000000UL, 0x31C00000_00000000UL }; // +0 * +0 -> +0
            yield return new object[] { 0xB1C00000_00000000UL, 0xB1C00000_00000000UL, 0x31C00000_00000000UL }; // -0 * -0 -> +0
            yield return new object[] { 0x31C00000_00000000UL, 0xB1C00000_00000000UL, 0xB1C00000_00000000UL }; // +0 * -0 -> -0
            yield return new object[] { 0x31C00000_00000001UL, 0xB1C00000_00000000UL, 0xB1C00000_00000000UL }; // 1 * -0 -> -0
            yield return new object[] { 0xB1C00000_00000001UL, 0x31C00000_00000000UL, 0xB1C00000_00000000UL }; // -1 * +0 -> -0
            yield return new object[] { 0x32600000_00000000UL, 0x32200000_00000000UL, 0x32C00000_00000000UL }; // 0e5 * 0e3 -> 0e8 (preferred exp)
            yield return new object[] { 0x31200000_00000000UL, 0x31C00000_0000000CUL, 0x31200000_00000000UL }; // 0e-5 * 12 -> 0e-5 (preferred exp)
            yield return new object[] { 0x31C00000_00000002UL, 0x31C00000_00000005UL, 0x31C00000_0000000AUL }; // 2 * 5 -> 10 (exp 0, trailing zero retained)
            yield return new object[] { 0x31C00000_00000002UL, 0x31A00000_00000005UL, 0x31A00000_0000000AUL }; // 2 * 0.5 -> 1.0
            yield return new object[] { 0x31A00000_0000000FUL, 0x31C00000_00000002UL, 0x31A00000_0000001EUL }; // 1.5 * 2 -> 3.0
            yield return new object[] { 0x31A00000_00000001UL, 0x31A00000_00000001UL, 0x31800000_00000001UL }; // 0.1 * 0.1 -> 0.01
            yield return new object[] { 0x31A00000_0000000CUL, 0x31A00000_0000000CUL, 0x31800000_00000090UL }; // 1.2 * 1.2 -> 1.44
            yield return new object[] { 0xB1C00000_00000003UL, 0x31C00000_00000007UL, 0xB1C00000_00000015UL }; // -3 * 7 -> -21
            yield return new object[] { 0x31C00000_0000000CUL, 0x31C00000_0000000CUL, 0x31C00000_00000090UL }; // 12 * 12 -> 144
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x6C7386F2_6FC0FFFFUL, 0x6CF386F2_6FC0FFFEUL }; // all-nines squared (round)
            yield return new object[] { 0x2FE4BCA8_DBB35555UL, 0x2FE650E1_24EF1C71UL, 0x2FE86BD6_DBE97B41UL }; // full-precision * full-precision (round)
            yield return new object[] { 0x31C38D7E_A4C68000UL, 0x31C38D7E_A4C68000UL, 0x33A38D7E_A4C68000UL }; // 10^(P-1) * 10^(P-1) (trailing zeros beyond precision)
            yield return new object[] { 0x5FFFF973_CAFA8000UL, 0x5FFFF973_CAFA8000UL, 0x78000000_00000000UL }; // huge * huge -> +Inf (overflow)
            yield return new object[] { 0xDFFFF973_CAFA8000UL, 0x5FFFF973_CAFA8000UL, 0xF8000000_00000000UL }; // -huge * huge -> -Inf (overflow)
            yield return new object[] { 0x01C00000_00000001UL, 0x01C00000_00000001UL, 0x00000000_00000000UL }; // tiny * tiny -> underflow
            yield return new object[] { 0x01E00000_00000001UL, 0x31200000_00000001UL, 0x01400000_00000001UL }; // small * small (subnormal-ish)
            yield return new object[] { 0x662B86F2_6FC0FFFFUL, 0x662B86F2_6FC0FFFFUL, 0x606386F2_6FC0FFFEUL }; // wide product just below min quantum (normal after single rounding)
            yield return new object[] { 0x65EB86F2_6FC0FFFFUL, 0x65F386F2_6FC0FFFFUL, 0x00000918_4E72A000UL }; // wide product deep in the subnormal range
            yield return new object[] { 0x607386F2_6FC0FFFFUL, 0x607386F2_6FC0FFFFUL, 0x00000000_00000000UL }; // wide product underflow to zero/epsilon
            yield return new object[] { 0x5FE00000_00000000UL, 0x5FE00000_00000005UL, 0x5FE00000_00000000UL }; // zero * finite, preferred exponent far above max quantum (clamped)
            yield return new object[] { 0xDFE00000_00000000UL, 0x5FE00000_00000005UL, 0xDFE00000_00000000UL }; // -zero * finite, preferred exponent clamped (sign preserved)
            yield return new object[] { 0x31E00000_00000000UL, 0x5FE00000_00000005UL, 0x5FE00000_00000000UL }; // zero * finite, preferred exponent one above max quantum (clamped)
            yield return new object[] { 0x31C00000_00000000UL, 0x5FE00000_00000005UL, 0x5FE00000_00000000UL }; // zero * finite, preferred exponent exactly at max quantum (no clamp)
            yield return new object[] { 0xAF200000_000023D2UL, 0xAFA34A58_43C82F35UL, 0x2D7E2C4D_3A5C54BAUL };
            yield return new object[] { 0x19C00000_0006B3C8UL, 0xB2400000_000014EDUL, 0x9A400000_8C401028UL };
            yield return new object[] { 0x31200054_42297730UL, 0x2F000063_A23BC84AUL, 0x2F658071_C3516735UL };
            yield return new object[] { 0x2E60007D_E4F31691UL, 0x328000E1_F407BFE1UL, 0x3032A47C_68E29A50UL };
            yield return new object[] { 0x95400091_BAB57FB9UL, 0x14600009_ED261C68UL, 0x80000000_00000000UL };
            yield return new object[] { 0xB1E00000_173C0E97UL, 0x17C00575_228BEDE5UL, 0x98A84F6B_C6DCC819UL };
            yield return new object[] { 0x31C00000_3838FC8EUL, 0x33800000_0008C901UL, 0x3381EDEC_DAF47A8EUL };
            yield return new object[] { 0x33E00000_00000006UL, 0xB3000000_000738AFUL, 0xB5200000_002B541AUL };
            yield return new object[] { 0xB120007E_4A0EE845UL, 0xB1A2CD7D_00E19992UL, 0x326F33B7_70DDF2ECUL };
            yield return new object[] { 0xB1800000_003142FFUL, 0x2F4004A5_E17CD24DUL, 0xAF85DC8E_E7E2C5B1UL };
            yield return new object[] { 0x30A1AE18_98FE6B6EUL, 0xB2400000_00034D87UL, 0xB1C3A2F6_E824594EUL };
            yield return new object[] { 0x1040016E_281289C6UL, 0x2E417F41_004E3595UL, 0x0E378B2A_D1798B46UL };
            yield return new object[] { 0x2F2160D1_63A6598CUL, 0xAD988DFF_DCC311D5UL, 0xACC98682_F248B25DUL };
            yield return new object[] { 0xAF800000_000149F7UL, 0x82600001_9308E202UL, 0x0022077B_4806A1EEUL };
            yield return new object[] { 0xB3600000_0000230CUL, 0xB8600000_00000003UL, 0x3A000000_00006924UL };
            yield return new object[] { 0xAD80816C_363173A8UL, 0x2FE00718_1926DD3EUL, 0xAD23F181_FCCBE5B2UL };
            yield return new object[] { 0xB2800000_00000000UL, 0x2F000000_007AF558UL, 0xAFC00000_00000000UL };
            yield return new object[] { 0xAE836513_54C06345UL, 0x8D000000_0000002AUL, 0x09EE421D_FD8E6DBBUL };
            yield return new object[] { 0xAE543A07_C082FDB3UL, 0x35200000_00000007UL, 0xB1CE289F_06C217FDUL };
            yield return new object[] { 0x32600000_0000B48AUL, 0x31400000_00015465UL, 0x31E00000_F00E8272UL };
            yield return new object[] { 0x33400000_000E8F6AUL, 0x33C00000_00000287UL, 0x35400000_24CC74E6UL };
            yield return new object[] { 0xAFE00000_0025FA34UL, 0x31C00000_000001BBUL, 0xAFE00000_41B7F7FCUL };
            yield return new object[] { 0x31200015_EE481751UL, 0xB06385FA_ED15C9E7UL, 0xEC412FEE_DE7C30F6UL };
            yield return new object[] { 0xB3A00000_0001B644UL, 0x31400000_88C9241BUL, 0xB320EA2C_7933C92CUL };
            yield return new object[] { 0xCE000000_2C155ADEUL, 0x2E400000_00000CC1UL, 0xCA800232_3C5BE95EUL };
            yield return new object[] { 0x2E0059A2_1C73F798UL, 0xAEC00000_0000038FUL, 0xAB3FE596_B9AB0231UL };
            yield return new object[] { 0xB4400000_00000000UL, 0xB3C00000_00000002UL, 0x36400000_00000000UL };
            yield return new object[] { 0x14200000_1E148863UL, 0xAF600000_00000003UL, 0x91C00000_5A3D9929UL };
            yield return new object[] { 0xB3000000_093826EBUL, 0x27600000_00000008UL, 0xA8A00000_49C13758UL };
            yield return new object[] { 0xBDA00000_0054E714UL, 0xAEC00000_00083858UL, 0x3AA002B9_E85BCEE0UL };
            yield return new object[] { 0xBBE00000_00000008UL, 0xB1000000_00000008UL, 0x3B200000_00000040UL };
            yield return new object[] { 0xD8600000_72FA8C0BUL, 0x30404C87_C53B7F29UL, 0xD7E5C449_A7AF6E2BUL };
            yield return new object[] { 0x31E00000_00011A74UL, 0x31EB8DA8_BD290BE9UL, 0x32A85A9F_C97EAEF6UL };
            yield return new object[] { 0xAE8000BE_61899933UL, 0x2E8E68EB_22D62BD8UL, 0xACCBC85B_083C3191UL };
            yield return new object[] { 0x30A00002_3C81B477UL, 0xB3400000_0000019EUL, 0xB220039D_D9C1D872UL };
            yield return new object[] { 0xAFC00016_E7160E75UL, 0x32600000_00000013UL, 0xB06001B3_26A312AFUL };
            yield return new object[] { 0x328000C6_0667F8FFUL, 0x437376D5_46AB54AAUL, 0x45B08DF4_524121DDUL };
            yield return new object[] { 0x316008E3_5687A600UL, 0x32C00000_000000BCUL, 0x326686F3_8B9DE800UL };
            yield return new object[] { 0x32600011_10EB5AFDUL, 0xB1200000_70F0F514UL, 0xB264EF2F_0CD8F588UL };
            yield return new object[] { 0x2F600000_0000018DUL, 0x34000000_000001F2UL, 0x31A00000_0003044AUL };
            yield return new object[] { 0x3260573E_35DB7C8EUL, 0x328018AA_7862D5CEUL, 0x34A93E11_AF666CB4UL };
            yield return new object[] { 0x22C00000_00000005UL, 0xB0A00000_000004FDUL, 0xA1A00000_000018F1UL };
            yield return new object[] { 0xD4200014_54458F18UL, 0xB2C00000_0000001DUL, 0x5520024D_8BE135B8UL };
            yield return new object[] { 0x30200000_0000004CUL, 0x324199F5_8C48AAF6UL, 0x30CC2BB0_908EACE7UL };
            yield return new object[] { 0xEB90A2E6_7C00A0EDUL, 0xB2C00000_000020F5UL, 0x2FDB8907_874A27EDUL };
            yield return new object[] { 0xB282B0EE_E060C24AUL, 0x30600000_00000008UL, 0xB1358777_03061250UL };
            yield return new object[] { 0x2E20004A_73ED6947UL, 0x9C6F546E_573DB6B7UL, 0x9A44E6EC_D9584235UL };
            yield return new object[] { 0xB1600000_00000054UL, 0x30800000_00000500UL, 0xB0200000_0001A400UL };
            yield return new object[] { 0xAA4042B6_7AA7818BUL, 0x31800000_00000055UL, 0xAA162696_B99E0327UL };
            yield return new object[] { 0xB2E00000_00000366UL, 0xB2C00000_00001F1DUL, 0x33E00000_0069BC8EUL };
            yield return new object[] { 0x33000000_00000002UL, 0x33200000_02DF7470UL, 0x34600000_05BEE8E0UL };
            yield return new object[] { 0xB062FAF8_87469CE0UL, 0x30200000_00000045UL, 0xAEF4907F_A5673A70UL };
            yield return new object[] { 0xB0200000_000E661FUL, 0xB1000000_00076DFBUL, 0x2F60006A_FA725365UL };
            yield return new object[] { 0xA9800000_01ECEE93UL, 0x2DC04A74_C2BEB838UL, 0xA6496549_D0B127DBUL };
            yield return new object[] { 0x29200001_1579BC2EUL, 0xAFC058BC_B025DCC1UL, 0xA83022F2_73C51F9FUL };
            yield return new object[] { 0xB3C00000_00000012UL, 0xD5600000_0000001EUL, 0x57600000_0000021CUL };
            yield return new object[] { 0xAF400000_000003E0UL, 0xB2E00000_1892B9E7UL, 0x3060005F_38905F20UL };
            yield return new object[] { 0x32400000_1E910857UL, 0xB120000E_041E212AUL, 0xB22AF7B6_BA92F217UL };
            yield return new object[] { 0x2FE00000_36F3932DUL, 0x32000000_00003529UL, 0x30200B69_3A7AE335UL };
            yield return new object[] { 0x32C00001_B3BF92FFUL, 0x2FC00000_02D2E250UL, 0x310C4DF4_84C9C542UL };
        }

        [Theory]
        [MemberData(nameof(op_Multiply_TestData))]
        public static void op_Multiply(ulong left, ulong right, ulong expected)
        {
            Decimal64 result = Unsafe.BitCast<ulong, Decimal64>(left) * Unsafe.BitCast<ulong, Decimal64>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(result));
        }

        public static IEnumerable<object[]> op_Division_TestData()
        {
            yield return new object[] { 0xFC000000_00000000UL, 0x31C00000_00000001UL, 0xFC000000_00000000UL }; // NaN / 1 -> NaN
            yield return new object[] { 0x31C00000_00000001UL, 0xFC000000_00000000UL, 0xFC000000_00000000UL }; // 1 / NaN -> NaN
            yield return new object[] { 0xFC000000_00000000UL, 0x78000000_00000000UL, 0xFC000000_00000000UL }; // NaN / +Inf -> NaN
            yield return new object[] { 0x78000000_00000000UL, 0x78000000_00000000UL, 0x7C000000_00000000UL }; // +Inf / +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x78000000_00000000UL, 0xF8000000_00000000UL, 0x7C000000_00000000UL }; // +Inf / -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xF8000000_00000000UL, 0x78000000_00000000UL, 0x7C000000_00000000UL }; // -Inf / +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xF8000000_00000000UL, 0xF8000000_00000000UL, 0x7C000000_00000000UL }; // -Inf / -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x78000000_00000000UL, 0x31C00000_00000001UL, 0x78000000_00000000UL }; // +Inf / 1 -> +Inf
            yield return new object[] { 0x78000000_00000000UL, 0xB1C00000_00000001UL, 0xF8000000_00000000UL }; // +Inf / -1 -> -Inf
            yield return new object[] { 0xF8000000_00000000UL, 0x31C00000_00000002UL, 0xF8000000_00000000UL }; // -Inf / 2 -> -Inf
            yield return new object[] { 0xF8000000_00000000UL, 0xB1C00000_00000002UL, 0x78000000_00000000UL }; // -Inf / -2 -> +Inf
            yield return new object[] { 0x78000000_00000000UL, 0x31C00000_00000000UL, 0x78000000_00000000UL }; // +Inf / +0 -> +Inf
            yield return new object[] { 0xF8000000_00000000UL, 0x31C00000_00000000UL, 0xF8000000_00000000UL }; // -Inf / +0 -> -Inf
            yield return new object[] { 0x78000000_00000000UL, 0xB1C00000_00000000UL, 0xF8000000_00000000UL }; // +Inf / -0 -> -Inf
            yield return new object[] { 0x31C00000_00000001UL, 0x78000000_00000000UL, 0x00000000_00000000UL }; // 1 / +Inf -> +0 (Etiny)
            yield return new object[] { 0xB1C00000_00000001UL, 0x78000000_00000000UL, 0x80000000_00000000UL }; // -1 / +Inf -> -0 (Etiny)
            yield return new object[] { 0x31C00000_00000005UL, 0xF8000000_00000000UL, 0x80000000_00000000UL }; // 5 / -Inf -> -0 (Etiny)
            yield return new object[] { 0x33000000_00000005UL, 0x78000000_00000000UL, 0x00000000_00000000UL }; // 5e10 / +Inf -> +0 (Etiny, dividend exp ignored)
            yield return new object[] { 0x31C00000_00000000UL, 0x78000000_00000000UL, 0x00000000_00000000UL }; // +0 / +Inf -> +0 (Etiny)
            yield return new object[] { 0xB1C00000_00000000UL, 0x78000000_00000000UL, 0x80000000_00000000UL }; // -0 / +Inf -> -0 (Etiny)
            yield return new object[] { 0x31C00000_00000005UL, 0x31C00000_00000000UL, 0x78000000_00000000UL }; // 5 / +0 -> +Inf
            yield return new object[] { 0xB1C00000_00000005UL, 0x31C00000_00000000UL, 0xF8000000_00000000UL }; // -5 / +0 -> -Inf
            yield return new object[] { 0x31C00000_00000005UL, 0xB1C00000_00000000UL, 0xF8000000_00000000UL }; // 5 / -0 -> -Inf
            yield return new object[] { 0xB1C00000_00000005UL, 0xB1C00000_00000000UL, 0x78000000_00000000UL }; // -5 / -0 -> +Inf
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000000UL, 0x7C000000_00000000UL }; // +0 / +0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0xB1C00000_00000000UL, 0xB1C00000_00000000UL, 0x7C000000_00000000UL }; // -0 / -0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x31C00000_00000000UL, 0xB1C00000_00000000UL, 0x7C000000_00000000UL }; // +0 / -0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000005UL, 0x31C00000_00000000UL }; // +0 / 5 -> +0 (ideal exp)
            yield return new object[] { 0xB1C00000_00000000UL, 0x31C00000_00000005UL, 0xB1C00000_00000000UL }; // -0 / 5 -> -0 (ideal exp, sign xor)
            yield return new object[] { 0x32000000_00000000UL, 0x31C00000_00000005UL, 0x32000000_00000000UL }; // 0e2 / 5 -> 0e2 (ideal exp)
            yield return new object[] { 0x31C00000_00000000UL, 0x32200000_00000005UL, 0x31600000_00000000UL }; // 0 / 5e3 -> 0e-3 (ideal exp)
            yield return new object[] { 0x32600000_00000000UL, 0x31200000_00000005UL, 0x33000000_00000000UL }; // 0e5 / 5e-5 -> 0e10 (ideal exp)
            yield return new object[] { 0x5FE00000_00000000UL, 0x31200000_00000005UL, 0x5FE00000_00000000UL }; // zero dividend, ideal exp far above max quantum (clamped)
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000002UL, 0x31A00000_00000005UL }; // 1 / 2 -> 0.5 (exact)
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000008UL, 0x31600000_0000007DUL }; // 1 / 8 -> 0.125 (exact)
            yield return new object[] { 0x31C00000_00000064UL, 0x31C00000_00000004UL, 0x31C00000_00000019UL }; // 100 / 4 -> 25 (exact)
            yield return new object[] { 0x31C00000_0000000AUL, 0x31C00000_00000002UL, 0x31C00000_00000005UL }; // 10 / 2 -> 5 (exact)
            yield return new object[] { 0x31C00000_00000006UL, 0x31C00000_00000003UL, 0x31C00000_00000002UL }; // 6 / 3 -> 2 (exact)
            yield return new object[] { 0x31C00000_00000005UL, 0x31C00000_00000005UL, 0x31C00000_00000001UL }; // 5 / 5 -> 1 (exact)
            yield return new object[] { 0x31800000_00000064UL, 0x31C00000_00000004UL, 0x31800000_00000019UL }; // 1.00 / 4 -> 0.25 (exact, ideal exp preserved)
            yield return new object[] { 0x31C00000_00000007UL, 0x31C00000_00000002UL, 0x31A00000_00000023UL }; // 7 / 2 -> 3.5 (exact)
            yield return new object[] { 0xB1C00000_0000000FUL, 0x31C00000_00000004UL, 0xB1800000_00000177UL }; // -15 / 4 -> -3.75 (exact, sign)
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000003UL, 0x2FCBD7A6_25405555UL }; // 1 / 3 -> repeating (round)
            yield return new object[] { 0x31C00000_00000002UL, 0x31C00000_00000003UL, 0x2FD7AF4C_4A80AAABUL }; // 2 / 3 -> repeating (round)
            yield return new object[] { 0x31C00000_000F4240UL, 0x31C00000_00000007UL, 0x30851347_34894925UL }; // 1000000 / 7 (round)
            yield return new object[] { 0x31C00000_0000000AUL, 0x31C00000_00000003UL, 0x2FEBD7A6_25405555UL }; // 10 / 3 -> repeating (round)
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x31C00000_00000007UL, 0x31C51347_34894924UL }; // all-nines / 7 (round)
            yield return new object[] { 0x31C00000_00000001UL, 0x6C7386F2_6FC0FFFFUL, 0x2DE38D7E_A4C68000UL }; // 1 / all-nines (round)
            yield return new object[] { 0x31C00000_00000002UL, 0x31C00000_00000007UL, 0x2FCA268E_69129249UL }; // 2 / 7 (round)
            yield return new object[] { 0x2FE4BCA8_DBB35555UL, 0x2FE650E1_24EF1C71UL, 0x2FDAA535_D3D0C001UL }; // full-precision / full-precision (round)
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x2FE3F28C_B71571C7UL, 0x33A00000_00000009UL }; // all-nines / near-one full precision (round)
            yield return new object[] { 0x5FFFF973_CAFA8000UL, 0x31200000_00000001UL, 0x78000000_00000000UL }; // huge / tiny -> +Inf (overflow)
            yield return new object[] { 0xDFFFF973_CAFA8000UL, 0x31200000_00000001UL, 0xF8000000_00000000UL }; // -huge / tiny -> -Inf (overflow)
            yield return new object[] { 0x01C00000_00000001UL, 0x32600000_00000001UL, 0x01200000_00000001UL }; // tiny / large -> underflow
            yield return new object[] { 0x31C00000_00000001UL, 0x5FC00000_00000003UL, 0x01CBD7A6_25405555UL }; // 1 / huge -> tiny subnormal (round)
            yield return new object[] { 0x01E00000_00000001UL, 0x32600000_00000001UL, 0x01400000_00000001UL }; // small / large (subnormal-ish)
            yield return new object[] { 0x78000000_00000002UL, 0x31C00000_00000001UL, 0x78000000_00000000UL }; // non-canonical +Inf / 1 -> canonical +Inf
            yield return new object[] { 0x31C00000_00000001UL, 0xF8000000_00000005UL, 0x80000000_00000000UL }; // 1 / non-canonical -Inf -> canonical -0 (Etiny)
            yield return new object[] { 0xAF200000_000023D2UL, 0xAFA34A58_43C82F35UL, 0x6B7B2CEB_297ED958UL };
            yield return new object[] { 0x19C00000_0006B3C8UL, 0xB2400000_000014EDUL, 0x979D2147_81117476UL };
            yield return new object[] { 0x31200054_42297730UL, 0x2F000063_A23BC84AUL, 0x31FE0B6E_C9E4B663UL };
            yield return new object[] { 0x2E60007D_E4F31691UL, 0x328000E1_F407BFE1UL, 0x2BB3CB6E_24C9CCB5UL };
            yield return new object[] { 0x95400091_BAB57FB9UL, 0x14600009_ED261C68UL, 0xB0E5373B_9B321342UL };
            yield return new object[] { 0xB1E00000_173C0E97UL, 0x17C00575_228BEDE5UL, 0xC9771439_895B2784UL };
            yield return new object[] { 0x31C00000_3838FC8EUL, 0x33800000_0008C901UL, 0x2E85D20C_CE429AB3UL };
            yield return new object[] { 0x33E00000_00000006UL, 0xB3000000_000738AFUL, 0xB024810D_4C0C6020UL };
            yield return new object[] { 0xB120007E_4A0EE845UL, 0xB1A2CD7D_00E19992UL, 0x2EF86D56_8CCEE558UL };
            yield return new object[] { 0xB1800000_003142FFUL, 0x2F4004A5_E17CD24DUL, 0xB156717A_82887347UL };
            yield return new object[] { 0x30A1AE18_98FE6B6EUL, 0xB2400000_00034D87UL, 0xAF67C2FF_FFD9495DUL };
            yield return new object[] { 0x1040016E_281289C6UL, 0x2E417F41_004E3595UL, 0x118D4239_24D52B06UL };
            yield return new object[] { 0x2F2160D1_63A6598CUL, 0xAD988DFF_DCC311D5UL, 0xB153F0C6_047336B7UL };
            yield return new object[] { 0xAF800000_000149F7UL, 0x82600001_9308E202UL, 0x5C64702C_F70E8C82UL };
            yield return new object[] { 0xB3600000_0000230CUL, 0xB8600000_00000003UL, 0x2B4A9FFE_D84EEAABUL };
            yield return new object[] { 0xAD80816C_363173A8UL, 0x2FE00718_1926DD3EUL, 0xADA67B3F_728271F8UL };
            yield return new object[] { 0xB2800000_00000000UL, 0x2F000000_007AF558UL, 0xB5400000_00000000UL };
            yield return new object[] { 0xAE836513_54C06345UL, 0x8D000000_0000002AUL, 0x5308153A_3780EC5BUL };
            yield return new object[] { 0xAE543A07_C082FDB3UL, 0x35200000_00000007UL, 0xAADCE52F_A54D6A6DUL };
            yield return new object[] { 0x32600000_0000B48AUL, 0x31400000_00015465UL, 0x30F2D7CB_58E04973UL };
            yield return new object[] { 0x33400000_000E8F6AUL, 0x33C00000_00000287UL, 0x2FC53D5A_B1B4EF3CUL };
            yield return new object[] { 0xAFE00000_0025FA34UL, 0x31C00000_000001BBUL, 0xAE73F5C4_5E9939EFUL };
            yield return new object[] { 0x31200015_EE481751UL, 0xB06385FA_ED15C9E7UL, 0xEC01BE15_983D5BE2UL };
            yield return new object[] { 0xB3A00000_0001B644UL, 0x31400000_88C9241BUL, 0xB1B15E7C_6BD1FB18UL };
            yield return new object[] { 0xCE000000_2C155ADEUL, 0x2E400000_00000CC1UL, 0xD0480C36_8CE138FBUL };
            yield return new object[] { 0x2E0059A2_1C73F798UL, 0xAEC00000_0000038FUL, 0xB083D7E6_4738794BUL };
            yield return new object[] { 0xB4400000_00000000UL, 0xB3C00000_00000002UL, 0x32400000_00000000UL };
            yield return new object[] { 0x14200000_1E148863UL, 0xAF600000_00000003UL, 0x96800000_0A06D821UL };
            yield return new object[] { 0xB3000000_093826EBUL, 0x27600000_00000008UL, 0xBD000004_806B00BFUL };
            yield return new object[] { 0xBDA00000_0054E714UL, 0xAEC00000_00083858UL, 0x3EE3AB63_2787AAABUL };
            yield return new object[] { 0xBBE00000_00000008UL, 0xB1000000_00000008UL, 0x3CA00000_00000001UL };
            yield return new object[] { 0xD8600000_72FA8C0BUL, 0x30404C87_C53B7F29UL, 0xD76824FD_5DC6387AUL };
            yield return new object[] { 0x31E00000_00011A74UL, 0x31EB8DA8_BD290BE9UL, 0x2E87E644_72962839UL };
            yield return new object[] { 0xAE8000BE_61899933UL, 0x2E8E68EB_22D62BD8UL, 0xAF672984_2954D3E5UL };
            yield return new object[] { 0x30A00002_3C81B477UL, 0xB3400000_0000019EUL, 0xAE283E16_28D505EEUL };
            yield return new object[] { 0xAFC00016_E7160E75UL, 0x32600000_00000013UL, 0xAE72649C_3A014DDBUL };
            yield return new object[] { 0x328000C6_0667F8FFUL, 0x437376D5_46AB54AAUL, 0x1E8583E6_67068B52UL };
            yield return new object[] { 0x316008E3_5687A600UL, 0x32C00000_000000BCUL, 0x2FD277AE_2C929D9EUL };
            yield return new object[] { 0x32600011_10EB5AFDUL, 0xB1200000_70F0F514UL, 0xB14DBE35_89D2645BUL };
            yield return new object[] { 0x2F600000_0000018DUL, 0x34000000_000001F2UL, 0x2B3C5263_B59E7BE3UL };
            yield return new object[] { 0x3260573E_35DB7C8EUL, 0x328018AA_7862D5CEUL, 0x2FCC90DF_5F1B1DBCUL };
            yield return new object[] { 0x22C00000_00000005UL, 0xB0A00000_000004FDUL, 0xA1ADE90F_5674DD52UL };
            yield return new object[] { 0xD4200014_54458F18UL, 0xB2C00000_0000001DUL, 0x526AB24E_6D558412UL };
            yield return new object[] { 0x30200000_0000004CUL, 0x324199F5_8C48AAF6UL, 0x2C25FD76_8E35F895UL };
            yield return new object[] { 0xEB90A2E6_7C00A0EDUL, 0xB2C00000_000020F5UL, 0x2CE3DE44_E337731CUL };
            yield return new object[] { 0xB282B0EE_E060C24AUL, 0x30600000_00000008UL, 0xECE9A3A9_F4B97C9DUL };
            yield return new object[] { 0x2E20004A_73ED6947UL, 0x9C6F546E_573DB6B7UL, 0xC11A5413_6BDEAB49UL };
            yield return new object[] { 0xB1600000_00000054UL, 0x30800000_00000500UL, 0xB1E00000_00010059UL };
            yield return new object[] { 0xAA4042B6_7AA7818BUL, 0x31800000_00000055UL, 0xAA1EA890_D66400D2UL };
            yield return new object[] { 0xB2E00000_00000366UL, 0xB2C00000_00001F1DUL, 0x2FE3E16B_F510BDF3UL };
            yield return new object[] { 0x33000000_00000002UL, 0x33200000_02DF7470UL, 0x2ECEBDEE_F33FAD55UL };
            yield return new object[] { 0xB062FAF8_87469CE0UL, 0x30200000_00000045UL, 0xB1C451C1_3AC6CD18UL };
            yield return new object[] { 0xB0200000_000E661FUL, 0xB1000000_00076DFBUL, 0x2F06E2A3_F0EE7805UL };
            yield return new object[] { 0xA9800000_01ECEE93UL, 0x2DC04A74_C2BEB838UL, 0xAACE04F2_43140E20UL };
            yield return new object[] { 0x29200001_1579BC2EUL, 0xAFC058BC_B025DCC1UL, 0xA8B0F380_7A2A5151UL };
            yield return new object[] { 0xB3C00000_00000012UL, 0xD5600000_0000001EUL, 0x10000000_00000006UL };
            yield return new object[] { 0xAF400000_000003E0UL, 0xB2E00000_1892B9E7UL, 0x2B888C6C_1C593DF8UL };
            yield return new object[] { 0x32400000_1E910857UL, 0xB120000E_041E212AUL, 0xB0BE43D2_A6ACD64EUL };
            yield return new object[] { 0x2FE00000_36F3932DUL, 0x32000000_00003529UL, 0x2E58114F_9968A5F7UL };
            yield return new object[] { 0x32C00001_B3BF92FFUL, 0x2FC00000_02D2E250UL, 0x33257B7B_B885F394UL };
        }

        [Theory]
        [MemberData(nameof(op_Division_TestData))]
        public static void op_Division(ulong left, ulong right, ulong expected)
        {
            Decimal64 result = Unsafe.BitCast<ulong, Decimal64>(left) / Unsafe.BitCast<ulong, Decimal64>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(result));
        }

        public static IEnumerable<object[]> op_Increment_TestData()
        {
            yield return new object[] { 0xFC000000_00000000UL, 0xFC000000_00000000UL }; // NaN
            yield return new object[] { 0x78000000_00000000UL, 0x78000000_00000000UL }; // +Inf
            yield return new object[] { 0xF8000000_00000000UL, 0xF8000000_00000000UL }; // -Inf
            yield return new object[] { 0x78000000_00000002UL, 0x78000000_00000000UL }; // non-canonical +Inf (canonicalizes)
            yield return new object[] { 0x31C00000_00000000UL, 0x31C00000_00000001UL }; // +0 -> 1
            yield return new object[] { 0xB1C00000_00000000UL, 0x31C00000_00000001UL }; // -0 -> 1
            yield return new object[] { 0x32600000_00000000UL, 0x31C00000_00000001UL }; // 0e5 (preferred exponent min(exp,0))
            yield return new object[] { 0x31200000_00000000UL, 0x31200000_000186A0UL }; // 0e-5
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000002UL }; // 1 -> 2
            yield return new object[] { 0xB1C00000_00000001UL, 0x31C00000_00000000UL }; // -1 -> 0
            yield return new object[] { 0x31C00000_00000002UL, 0x31C00000_00000003UL }; // 2
            yield return new object[] { 0xB1C00000_00000002UL, 0xB1C00000_00000001UL }; // -2
            yield return new object[] { 0x31C00000_0000000AUL, 0x31C00000_0000000BUL }; // 10
            yield return new object[] { 0x31A00000_00000005UL, 0x31A00000_0000000FUL }; // 0.5 -> 1.5
            yield return new object[] { 0xB1A00000_00000005UL, 0x31A00000_00000005UL }; // -0.5
            yield return new object[] { 0x31A00000_00000019UL, 0x31A00000_00000023UL }; // 2.5
            yield return new object[] { 0x31A00000_00000001UL, 0x31A00000_0000000BUL }; // 0.1 -> 1.1
            yield return new object[] { 0x31800000_00000177UL, 0x31800000_000001DBUL }; // 3.75
            yield return new object[] { 0xB1800000_00000019UL, 0x31800000_0000004BUL }; // -0.25
            yield return new object[] { 0x31C00000_00000009UL, 0x31C00000_0000000AUL }; // 9 -> 10
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x31E38D7E_A4C68000UL }; // all-nines (overflows precision, rounds)
            yield return new object[] { 0x31C38D7E_A4C68000UL, 0x31C38D7E_A4C68001UL }; // 10^(P-1)
            yield return new object[] { 0x31C38D7E_A4C67FFFUL, 0x31C38D7E_A4C68000UL }; // (P-1)-nines
            yield return new object[] { 0x34000000_00000001UL, 0x32238D7E_A4C68000UL }; // 1e(P+2) (1 below quantum, no visible change)
            yield return new object[] { 0x5FFFF973_CAFA8000UL, 0x5FFFF973_CAFA8000UL }; // near-max (1 negligible)
            yield return new object[] { 0xDFFFF973_CAFA8000UL, 0xDFFFF973_CAFA8000UL }; // near -max (1 negligible)
            yield return new object[] { 0x01C00000_00000001UL, 0x2FE38D7E_A4C68000UL }; // tiny positive subnormal (++ ~ 1)
            yield return new object[] { 0x81C00000_00000001UL, 0x2FE38D7E_A4C68000UL }; // tiny negative subnormal (++ ~ 1)
            yield return new object[] { 0x31200000_00000001UL, 0x31200000_000186A1UL }; // 1e-5
            yield return new object[] { 0x2FE4BCA8_DBB35555UL, 0x2FE84A27_8079D555UL }; // 1.333... full precision
            yield return new object[] { 0x6BFB86F2_6FC0FFFFUL, 0x3003E871_B540C000UL }; // 9.999... full precision (carry)
            yield return new object[] { 0xDE000D7B_C1739419UL, 0xDDC54457_9125D9C4UL };
            yield return new object[] { 0x33A00000_00000035UL, 0x31F2D452_694F4000UL };
            yield return new object[] { 0x32A00000_001AACBFUL, 0x31C00FE6_3FF64981UL };
            yield return new object[] { 0x31400000_0000C292UL, 0x31400000_0000E9A2UL };
            yield return new object[] { 0xB0E00000_00002125UL, 0x30E00000_0098755BUL };
            yield return new object[] { 0xB4200000_00000004UL, 0xB24E35FA_931A0000UL };
            yield return new object[] { 0x31C00000_06E71450UL, 0x31C00000_06E71451UL };
            yield return new object[] { 0xB2E00000_05A136AAUL, 0xEC798E4D_53BD6A00UL };
            yield return new object[] { 0x2FA00000_03D3544BUL, 0x2FE38D7E_A4D04B15UL };
            yield return new object[] { 0xB0C00000_0002002AUL, 0x30C00000_05F3E0D6UL };
            yield return new object[] { 0xB2604C77_D2CE2737UL, 0xB23DDECE_5887517CUL };
            yield return new object[] { 0x32600000_01C6C941UL, 0x31C002B5_F2D6CEA1UL };
            yield return new object[] { 0x30E00000_33939110UL, 0x30E00000_342C2790UL };
            yield return new object[] { 0x32C00000_5AC09507UL, 0x320568C5_11FA0FC0UL };
            yield return new object[] { 0xB06D3F1C_656F52FAUL, 0xB06D3F05_1CF86AFAUL };
            yield return new object[] { 0x31E00000_00000043UL, 0x31C00000_0000029FUL };
            yield return new object[] { 0xAE00C898_D85C6FF7UL, 0x6BF386F2_6FC0FFFEUL };
            yield return new object[] { 0xBE8CD924_B6560750UL, 0xBE8CD924_B6560750UL };
            yield return new object[] { 0x3102EDC1_D68C47BEUL, 0x3102EDC1_D69B89FEUL };
            yield return new object[] { 0xB0E00000_00000009UL, 0x30E00000_00989677UL };
            yield return new object[] { 0xB0600586_0DF1C1B6UL, 0xB060056E_C57AD9B6UL };
            yield return new object[] { 0x33000000_00000016UL, 0x31C00033_39059801UL };
            yield return new object[] { 0xB0C0007E_EB0B6EA0UL, 0xB0C0007E_E5158DA0UL };
            yield return new object[] { 0x2F400000_00000897UL, 0x2FE38D7E_A4C68000UL };
            yield return new object[] { 0x32A00000_000003E0UL, 0x31C00002_4F473001UL };
            yield return new object[] { 0xB2A00000_00000004UL, 0xB1C00000_026259FFUL };
            yield return new object[] { 0x3820253B_3AF58D7CUL, 0x37EE8B23_07EB4470UL };
            yield return new object[] { 0x32A00000_0060035AUL, 0x31C0393A_6F686901UL };
            yield return new object[] { 0x2EC00000_1B1D8BA6UL, 0x2FE38D7E_A4C68000UL };
            yield return new object[] { 0xB2800000_22850479UL, 0xB1C20EBA_2F7F503FUL };
            yield return new object[] { 0xB2200000_30F3774BUL, 0xB1C000BF_3709FCF7UL };
            yield return new object[] { 0xAF200000_042E8A44UL, 0x6BF386F2_6FC0FD42UL };
            yield return new object[] { 0x302012E4_B193AA00UL, 0x30201BFD_00064A00UL };
            yield return new object[] { 0xAEE00001_BE122682UL, 0x6BF386F2_6FC0FD14UL };
            yield return new object[] { 0x07600000_00000000UL, 0x2FE38D7E_A4C68000UL };
            yield return new object[] { 0xB2600000_0000D120UL, 0xB1C00001_3F1973FFUL };
            yield return new object[] { 0x2EC00008_5153EF36UL, 0x2FE38D7E_A4C68024UL };
            yield return new object[] { 0x2F200000_00000045UL, 0x2FE38D7E_A4C68000UL };
            yield return new object[] { 0x2FCDCB8B_5E0C05CAUL, 0x2FE4EEA6_2E2E1A2EUL };
            yield return new object[] { 0xBEE000E4_BBA70FA7UL, 0xEF9AE6E2_2DD36B70UL };
            yield return new object[] { 0x5B600000_000003D2UL, 0x7672BEDB_B1E74000UL };
            yield return new object[] { 0x2F600000_00004436UL, 0x2FE38D7E_A4C68002UL };
            yield return new object[] { 0x30600000_00833DA9UL, 0x30600017_48FA25A9UL };
            yield return new object[] { 0x2F200000_1228ADE2UL, 0x2FE38D7E_A4C68131UL };
            yield return new object[] { 0xAF600000_2AD1A374UL, 0x6BF386F2_6FB609D2UL };
            yield return new object[] { 0xB1600000_18006133UL, 0xB1600000_18005D4BUL };
            yield return new object[] { 0x6B5A3F19_333501CCUL, 0x2FE38D7E_A4C68000UL };
            yield return new object[] { 0x32E00000_001FC878UL, 0x31C7666B_545EB001UL };
            yield return new object[] { 0x31800000_B171BD11UL, 0x31800000_B171BD75UL };
            yield return new object[] { 0x2EE00001_723C5118UL, 0x2FE38D7E_A4C6803EUL };
            yield return new object[] { 0x9F400000_00001DF5UL, 0x2FE38D7E_A4C68000UL };
            yield return new object[] { 0xAEC00000_001D0824UL, 0x2FE38D7E_A4C68000UL };
            yield return new object[] { 0xAFA34A58_43C82F35UL, 0x6BF332B6_68F9C814UL };
            yield return new object[] { 0x19C00000_0006B3C8UL, 0x2FE38D7E_A4C68000UL };
            yield return new object[] { 0xB2400000_000014EDUL, 0xB1C00000_033169CFUL };
            yield return new object[] { 0x31200054_42297730UL, 0x31200054_422AFDD0UL };
            yield return new object[] { 0x2F000063_A23BC84AUL, 0x2FE38D7E_A4C72728UL };
            yield return new object[] { 0x2E60007D_E4F31691UL, 0x2FE38D7E_A4C68001UL };
            yield return new object[] { 0x328000E1_F407BFE1UL, 0x6C827A4C_6EB74510UL };
            yield return new object[] { 0x95400091_BAB57FB9UL, 0x2FE38D7E_A4C68000UL };
        }

        [Theory]
        [MemberData(nameof(op_Increment_TestData))]
        public static void op_Increment(ulong value, ulong expected)
        {
            Decimal64 result = Unsafe.BitCast<ulong, Decimal64>(value);
            result++;
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(result));
        }

        public static IEnumerable<object[]> op_Decrement_TestData()
        {
            yield return new object[] { 0xFC000000_00000000UL, 0xFC000000_00000000UL }; // NaN
            yield return new object[] { 0x78000000_00000000UL, 0x78000000_00000000UL }; // +Inf
            yield return new object[] { 0xF8000000_00000000UL, 0xF8000000_00000000UL }; // -Inf
            yield return new object[] { 0x78000000_00000002UL, 0x78000000_00000000UL }; // non-canonical +Inf (canonicalizes)
            yield return new object[] { 0x31C00000_00000000UL, 0xB1C00000_00000001UL }; // +0 -> -1
            yield return new object[] { 0xB1C00000_00000000UL, 0xB1C00000_00000001UL }; // -0 -> -1
            yield return new object[] { 0x32600000_00000000UL, 0xB1C00000_00000001UL }; // 0e5 (preferred exponent min(exp,0))
            yield return new object[] { 0x31200000_00000000UL, 0xB1200000_000186A0UL }; // 0e-5
            yield return new object[] { 0x31C00000_00000001UL, 0x31C00000_00000000UL }; // 1 -> 0
            yield return new object[] { 0xB1C00000_00000001UL, 0xB1C00000_00000002UL }; // -1 -> -2
            yield return new object[] { 0x31C00000_00000002UL, 0x31C00000_00000001UL }; // 2
            yield return new object[] { 0xB1C00000_00000002UL, 0xB1C00000_00000003UL }; // -2
            yield return new object[] { 0x31C00000_0000000AUL, 0x31C00000_00000009UL }; // 10
            yield return new object[] { 0x31A00000_00000005UL, 0xB1A00000_00000005UL }; // 0.5 -> -0.5
            yield return new object[] { 0xB1A00000_00000005UL, 0xB1A00000_0000000FUL }; // -0.5
            yield return new object[] { 0x31A00000_00000019UL, 0x31A00000_0000000FUL }; // 2.5
            yield return new object[] { 0x31A00000_00000001UL, 0xB1A00000_00000009UL }; // 0.1 -> -0.9
            yield return new object[] { 0x31800000_00000177UL, 0x31800000_00000113UL }; // 3.75
            yield return new object[] { 0xB1800000_00000019UL, 0xB1800000_0000007DUL }; // -0.25
            yield return new object[] { 0x31C00000_00000009UL, 0x31C00000_00000008UL }; // 9 -> 8
            yield return new object[] { 0x6C7386F2_6FC0FFFFUL, 0x6C7386F2_6FC0FFFEUL }; // all-nines (decrement in last place)
            yield return new object[] { 0x31C38D7E_A4C68000UL, 0x31C38D7E_A4C67FFFUL }; // 10^(P-1)
            yield return new object[] { 0x31C38D7E_A4C67FFFUL, 0x31C38D7E_A4C67FFEUL }; // (P-1)-nines
            yield return new object[] { 0x34000000_00000001UL, 0x32238D7E_A4C68000UL }; // 1e(P+2) (1 below quantum, no visible change)
            yield return new object[] { 0x5FFFF973_CAFA8000UL, 0x5FFFF973_CAFA8000UL }; // near-max (1 negligible)
            yield return new object[] { 0xDFFFF973_CAFA8000UL, 0xDFFFF973_CAFA8000UL }; // near -max (1 negligible)
            yield return new object[] { 0x01C00000_00000001UL, 0xAFE38D7E_A4C68000UL }; // tiny positive subnormal (-- ~ -1)
            yield return new object[] { 0x81C00000_00000001UL, 0xAFE38D7E_A4C68000UL }; // tiny negative subnormal (-- ~ -1)
            yield return new object[] { 0x31200000_00000001UL, 0xB1200000_0001869FUL }; // 1e-5
            yield return new object[] { 0x2FE4BCA8_DBB35555UL, 0x2FE12F2A_36ECD555UL }; // 1.333... full precision
            yield return new object[] { 0x6BFB86F2_6FC0FFFFUL, 0x2FFFF973_CAFA7FFFUL }; // 9.999... full precision
            yield return new object[] { 0xDE000D7B_C1739419UL, 0xDDC54457_9125D9C4UL };
            yield return new object[] { 0x33A00000_00000035UL, 0x31F2D452_694F4000UL };
            yield return new object[] { 0x32A00000_001AACBFUL, 0x31C00FE6_3FF6497FUL };
            yield return new object[] { 0x31400000_0000C292UL, 0x31400000_00009B82UL };
            yield return new object[] { 0xB0E00000_00002125UL, 0xB0E00000_0098B7A5UL };
            yield return new object[] { 0xB4200000_00000004UL, 0xB24E35FA_931A0000UL };
            yield return new object[] { 0x31C00000_06E71450UL, 0x31C00000_06E7144FUL };
            yield return new object[] { 0xB2E00000_05A136AAUL, 0xEC798E4D_53BD6A00UL };
            yield return new object[] { 0x2FA00000_03D3544BUL, 0xEBF386F2_6F5F112CUL };
            yield return new object[] { 0xB0C00000_0002002AUL, 0xB0C00000_05F7E12AUL };
            yield return new object[] { 0xB2604C77_D2CE2737UL, 0xB23DDECE_5887517CUL };
            yield return new object[] { 0x32600000_01C6C941UL, 0x31C002B5_F2D6CE9FUL };
            yield return new object[] { 0x30E00000_33939110UL, 0x30E00000_32FAFA90UL };
            yield return new object[] { 0x32C00000_5AC09507UL, 0x320568C5_11FA0FC0UL };
            yield return new object[] { 0xB06D3F1C_656F52FAUL, 0xB06D3F33_ADE63AFAUL };
            yield return new object[] { 0x31E00000_00000043UL, 0x31C00000_0000029DUL };
            yield return new object[] { 0xAE00C898_D85C6FF7UL, 0xAFE38D7E_A4C68000UL };
            yield return new object[] { 0xBE8CD924_B6560750UL, 0xBE8CD924_B6560750UL };
            yield return new object[] { 0x3102EDC1_D68C47BEUL, 0x3102EDC1_D67D057EUL };
            yield return new object[] { 0xB0E00000_00000009UL, 0xB0E00000_00989689UL };
            yield return new object[] { 0xB0600586_0DF1C1B6UL, 0xB060059D_5668A9B6UL };
            yield return new object[] { 0x33000000_00000016UL, 0x31C00033_390597FFUL };
            yield return new object[] { 0xB0C0007E_EB0B6EA0UL, 0xB0C0007E_F1014FA0UL };
            yield return new object[] { 0x2F400000_00000897UL, 0xAFE38D7E_A4C68000UL };
            yield return new object[] { 0x32A00000_000003E0UL, 0x31C00002_4F472FFFUL };
            yield return new object[] { 0xB2A00000_00000004UL, 0xB1C00000_02625A01UL };
            yield return new object[] { 0x3820253B_3AF58D7CUL, 0x37EE8B23_07EB4470UL };
            yield return new object[] { 0x32A00000_0060035AUL, 0x31C0393A_6F6868FFUL };
            yield return new object[] { 0x2EC00000_1B1D8BA6UL, 0xEBF386F2_6FC0FFFBUL };
            yield return new object[] { 0xB2800000_22850479UL, 0xB1C20EBA_2F7F5041UL };
            yield return new object[] { 0xB2200000_30F3774BUL, 0xB1C000BF_3709FCF9UL };
            yield return new object[] { 0xAF200000_042E8A44UL, 0xAFE38D7E_A4C68046UL };
            yield return new object[] { 0x302012E4_B193AA00UL, 0x302009CC_63210A00UL };
            yield return new object[] { 0xAEE00001_BE122682UL, 0xAFE38D7E_A4C6804BUL };
            yield return new object[] { 0x07600000_00000000UL, 0xAFE38D7E_A4C68000UL };
            yield return new object[] { 0xB2600000_0000D120UL, 0xB1C00001_3F197401UL };
            yield return new object[] { 0x2EC00008_5153EF36UL, 0xEBF386F2_6FC0FE9BUL };
            yield return new object[] { 0x2F200000_00000045UL, 0xAFE38D7E_A4C68000UL };
            yield return new object[] { 0x2FCDCB8B_5E0C05CAUL, 0xAFD5BB67_11B4FA36UL };
            yield return new object[] { 0xBEE000E4_BBA70FA7UL, 0xEF9AE6E2_2DD36B70UL };
            yield return new object[] { 0x5B600000_000003D2UL, 0x7672BEDB_B1E74000UL };
            yield return new object[] { 0x2F600000_00004436UL, 0xEBF386F2_6FC0FFEFUL };
            yield return new object[] { 0x30600000_00833DA9UL, 0xB0600017_47F3AA57UL };
            yield return new object[] { 0x2F200000_1228ADE2UL, 0xEBF386F2_6FC0F419UL };
            yield return new object[] { 0xAF600000_2AD1A374UL, 0xAFE38D7E_A4C7989EUL };
            yield return new object[] { 0xB1600000_18006133UL, 0xB1600000_1800651BUL };
            yield return new object[] { 0x6B5A3F19_333501CCUL, 0xAFE38D7E_A4C68000UL };
            yield return new object[] { 0x32E00000_001FC878UL, 0x31C7666B_545EAFFFUL };
            yield return new object[] { 0x31800000_B171BD11UL, 0x31800000_B171BCADUL };
            yield return new object[] { 0x2EE00001_723C5118UL, 0xEBF386F2_6FC0FD93UL };
            yield return new object[] { 0x9F400000_00001DF5UL, 0xAFE38D7E_A4C68000UL };
            yield return new object[] { 0xAEC00000_001D0824UL, 0xAFE38D7E_A4C68000UL };
            yield return new object[] { 0xAFA34A58_43C82F35UL, 0xAFE395EB_0BDA6BFEUL };
            yield return new object[] { 0x19C00000_0006B3C8UL, 0xAFE38D7E_A4C68000UL };
            yield return new object[] { 0xB2400000_000014EDUL, 0xB1C00000_033169D1UL };
            yield return new object[] { 0x31200054_42297730UL, 0x31200054_4227F090UL };
            yield return new object[] { 0x2F000063_A23BC84AUL, 0xEBF386F2_6FBA786CUL };
            yield return new object[] { 0x2E60007D_E4F31691UL, 0xEBF386F2_6FC0FFFBUL };
            yield return new object[] { 0x328000E1_F407BFE1UL, 0x6C827A4C_6EB74510UL };
            yield return new object[] { 0x95400091_BAB57FB9UL, 0xAFE38D7E_A4C68000UL };
        }

        [Theory]
        [MemberData(nameof(op_Decrement_TestData))]
        public static void op_Decrement(ulong value, ulong expected)
        {
            Decimal64 result = Unsafe.BitCast<ulong, Decimal64>(value);
            result--;
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(result));
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

            // Special values always consume surrounding whitespace (independent of AllowLeadingWhite/AllowTrailingWhite) before stopping on the first non-whitespace invalid character
            yield return new object[] { "Infinity   ", style, CultureInfo.InvariantCulture, Decimal64.PositiveInfinity, 11 };
            yield return new object[] { "Infinity  x", style, CultureInfo.InvariantCulture, Decimal64.PositiveInfinity, 10 };
            yield return new object[] { "+Infinity  x", style, CultureInfo.InvariantCulture, Decimal64.PositiveInfinity, 11 };
            yield return new object[] { "-Infinity  x", style, CultureInfo.InvariantCulture, Decimal64.NegativeInfinity, 11 };
            yield return new object[] { "NaN  x", style, CultureInfo.InvariantCulture, Decimal64.NaN, 5 };

            // AllowTrailingWhite has no effect on special values; the surrounding whitespace is still consumed
            yield return new object[] { "Infinity  x", (NumberStyles.Float & ~NumberStyles.AllowTrailingWhite) | NumberStyles.AllowTrailingInvalidCharacters, CultureInfo.InvariantCulture, Decimal64.PositiveInfinity, 10 };
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

            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);
            Assert.False(Decimal64.TryParse(utf8Bytes.AsSpan(), style, provider, out result, out int bytesConsumed));
            Assert.Equal(0, bytesConsumed);
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal64Arithmetic), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void op_Arithmetic_IntelReferenceVectors(string operation, ulong left, ulong right, ulong expected)
        {
            Decimal64 l = Unsafe.BitCast<ulong, Decimal64>(left);
            Decimal64 r = Unsafe.BitCast<ulong, Decimal64>(right);

            Decimal64 result = operation switch
            {
                "add" => l + r,
                "sub" => l - r,
                "mul" => l * r,
                "div" => l / r,
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal64Comparison), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void op_Comparison_IntelReferenceVectors(string operation, ulong left, ulong right, bool expected)
        {
            Decimal64 l = Unsafe.BitCast<ulong, Decimal64>(left);
            Decimal64 r = Unsafe.BitCast<ulong, Decimal64>(right);

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
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal64Unary), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void UnaryOperation_IntelReferenceVectors(string operation, ulong value, ulong expected)
        {
            Decimal64 v = Unsafe.BitCast<ulong, Decimal64>(value);

            Decimal64 result = operation switch
            {
                "abs" => Decimal64.Abs(v),
                "negate" => -v,
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal64BinaryValue), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void BinaryValueOperation_IntelReferenceVectors(string operation, ulong left, ulong right, ulong expected)
        {
            Decimal64 l = Unsafe.BitCast<ulong, Decimal64>(left);
            Decimal64 r = Unsafe.BitCast<ulong, Decimal64>(right);

            Decimal64 result = operation switch
            {
                "copySign" => Decimal64.CopySign(l, r),
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal64Predicate), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void Predicate_IntelReferenceVectors(string operation, ulong value, bool expected)
        {
            Decimal64 v = Unsafe.BitCast<ulong, Decimal64>(value);

            bool result = operation switch
            {
                "isNaN" => Decimal64.IsNaN(v),
                "isInf" => Decimal64.IsInfinity(v),
                "isFinite" => Decimal64.IsFinite(v),
                "isSigned" => Decimal64.IsNegative(v),
                "isNormal" => Decimal64.IsNormal(v),
                "isSubnormal" => Decimal64.IsSubnormal(v),
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, result);
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal(0x31C0000000000001UL, Unsafe.BitCast<Decimal64, ulong>(Decimal64.One));
        }

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(0xB1C0000000000001UL, Unsafe.BitCast<Decimal64, ulong>(Decimal64.NegativeOne));
        }

        [Fact]
        public static void ETest()
        {
            Assert.Equal(0x2FE9A8434EC8E225UL, Unsafe.BitCast<Decimal64, ulong>(Decimal64.E)); // +2.718281828459045
        }

        [Fact]
        public static void PiTest()
        {
            Assert.Equal(0x2FEB29430A256D21UL, Unsafe.BitCast<Decimal64, ulong>(Decimal64.Pi)); // +3.141592653589793
        }

        [Fact]
        public static void TauTest()
        {
            Assert.Equal(0x2FF65286144ADA42UL, Unsafe.BitCast<Decimal64, ulong>(Decimal64.Tau)); // +6.283185307179586
        }

        public static IEnumerable<object[]> Abs_TestData()
        {
            yield return new object[] { 0x7C00000000000000UL, 0x7C00000000000000UL }; // Abs(NaN)
            yield return new object[] { 0xFC00000000000000UL, 0x7C00000000000000UL }; // Abs(-NaN)
            yield return new object[] { 0x7800000000000000UL, 0x7800000000000000UL }; // Abs(+Inf)
            yield return new object[] { 0xF800000000000000UL, 0x7800000000000000UL }; // Abs(-Inf)
            yield return new object[] { 0x31C0000000000000UL, 0x31C0000000000000UL }; // Abs(0)
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL }; // Abs(-0)
            yield return new object[] { 0x31C0000000000001UL, 0x31C0000000000001UL }; // Abs(1)
            yield return new object[] { 0xB1C0000000000001UL, 0x31C0000000000001UL }; // Abs(-1)
            yield return new object[] { 0x31C0000000000002UL, 0x31C0000000000002UL }; // Abs(2)
            yield return new object[] { 0xB1C0000000000002UL, 0x31C0000000000002UL }; // Abs(-2)
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL }; // Abs(3)
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL }; // Abs(-3)
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000005UL }; // Abs(5)
            yield return new object[] { 0xB1C0000000000005UL, 0x31C0000000000005UL }; // Abs(-5)
            yield return new object[] { 0x31C000000000000AUL, 0x31C000000000000AUL }; // Abs(10)
            yield return new object[] { 0xB1C000000000000AUL, 0x31C000000000000AUL }; // Abs(-10)
            yield return new object[] { 0x31A0000000000005UL, 0x31A0000000000005UL }; // Abs(0.5)
            yield return new object[] { 0xB1A0000000000005UL, 0x31A0000000000005UL }; // Abs(-0.5)
            yield return new object[] { 0x31A0000000000019UL, 0x31A0000000000019UL }; // Abs(2.5)
            yield return new object[] { 0x31A000000000000FUL, 0x31A000000000000FUL }; // Abs(1.5)
            yield return new object[] { 0x31C0000000000064UL, 0x31C0000000000064UL }; // Abs(100)
            yield return new object[] { 0x31C00000000003E8UL, 0x31C00000000003E8UL }; // Abs(1000)
            yield return new object[] { 0x31A0000000000001UL, 0x31A0000000000001UL }; // Abs(0.1)
            yield return new object[] { 0x31C0000000000009UL, 0x31C0000000000009UL }; // Abs(9)
            yield return new object[] { 0x3220000000000001UL, 0x3220000000000001UL }; // Abs(1E+3)
            yield return new object[] { 0x31A000000000000AUL, 0x31A000000000000AUL }; // Abs(1.0)
            yield return new object[] { 0x31800000000000C8UL, 0x31800000000000C8UL }; // Abs(2.00)
            yield return new object[] { 0x31E0000000000002UL, 0x31E0000000000002UL }; // Abs(2E+1)
            yield return new object[] { 0x0000000000000001UL, 0x0000000000000001UL }; // Abs(epsilon)
            yield return new object[] { 0x8000000000000001UL, 0x0000000000000001UL }; // Abs(-epsilon)
            yield return new object[] { 0x00038D7EA4C67FFFUL, 0x00038D7EA4C67FFFUL }; // Abs(largest_subnormal)
            yield return new object[] { 0x80038D7EA4C67FFFUL, 0x00038D7EA4C67FFFUL }; // Abs(-largest_subnormal)
            yield return new object[] { 0x01E0000000000001UL, 0x01E0000000000001UL }; // Abs(smallest_normal)
            yield return new object[] { 0x81E0000000000001UL, 0x01E0000000000001UL }; // Abs(-smallest_normal)
            yield return new object[] { 0x77FB86F26FC0FFFFUL, 0x77FB86F26FC0FFFFUL }; // Abs(maxvalue)
            yield return new object[] { 0xF7FB86F26FC0FFFFUL, 0x77FB86F26FC0FFFFUL }; // Abs(-maxvalue)
        }

        [Theory]
        [MemberData(nameof(Abs_TestData))]
        public static void AbsTest(ulong value, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.Abs(Unsafe.BitCast<ulong, Decimal64>(value))));
        }

        public static IEnumerable<object[]> Classification_TestData()
        {
            yield return new object[] { 0x7C00000000000000UL, false, false, true, false, true, false, false, false }; // NaN
            yield return new object[] { 0xFC00000000000000UL, false, false, true, true, false, false, false, false }; // -NaN
            yield return new object[] { 0x7800000000000000UL, false, true, false, false, true, false, true, true }; // +Inf
            yield return new object[] { 0xF800000000000000UL, false, true, false, true, false, true, false, true }; // -Inf
            yield return new object[] { 0x31C0000000000000UL, true, false, false, false, true, false, false, true }; // 0
            yield return new object[] { 0xB1C0000000000000UL, true, false, false, true, false, false, false, true }; // -0
            yield return new object[] { 0x31C0000000000001UL, true, false, false, false, true, false, false, true }; // 1
            yield return new object[] { 0xB1C0000000000001UL, true, false, false, true, false, false, false, true }; // -1
            yield return new object[] { 0x31C0000000000002UL, true, false, false, false, true, false, false, true }; // 2
            yield return new object[] { 0xB1C0000000000002UL, true, false, false, true, false, false, false, true }; // -2
            yield return new object[] { 0x31C0000000000003UL, true, false, false, false, true, false, false, true }; // 3
            yield return new object[] { 0xB1C0000000000003UL, true, false, false, true, false, false, false, true }; // -3
            yield return new object[] { 0x31C0000000000005UL, true, false, false, false, true, false, false, true }; // 5
            yield return new object[] { 0xB1C0000000000005UL, true, false, false, true, false, false, false, true }; // -5
            yield return new object[] { 0x31C000000000000AUL, true, false, false, false, true, false, false, true }; // 10
            yield return new object[] { 0xB1C000000000000AUL, true, false, false, true, false, false, false, true }; // -10
            yield return new object[] { 0x31A0000000000005UL, true, false, false, false, true, false, false, true }; // 0.5
            yield return new object[] { 0xB1A0000000000005UL, true, false, false, true, false, false, false, true }; // -0.5
            yield return new object[] { 0x31A0000000000019UL, true, false, false, false, true, false, false, true }; // 2.5
            yield return new object[] { 0x31A000000000000FUL, true, false, false, false, true, false, false, true }; // 1.5
            yield return new object[] { 0x31C0000000000064UL, true, false, false, false, true, false, false, true }; // 100
            yield return new object[] { 0x31C00000000003E8UL, true, false, false, false, true, false, false, true }; // 1000
            yield return new object[] { 0x31A0000000000001UL, true, false, false, false, true, false, false, true }; // 0.1
            yield return new object[] { 0x31C0000000000009UL, true, false, false, false, true, false, false, true }; // 9
            yield return new object[] { 0x3220000000000001UL, true, false, false, false, true, false, false, true }; // 1E+3
            yield return new object[] { 0x31A000000000000AUL, true, false, false, false, true, false, false, true }; // 1.0
            yield return new object[] { 0x31800000000000C8UL, true, false, false, false, true, false, false, true }; // 2.00
            yield return new object[] { 0x31E0000000000002UL, true, false, false, false, true, false, false, true }; // 2E+1
            yield return new object[] { 0x0000000000000001UL, true, false, false, false, true, false, false, true }; // epsilon
            yield return new object[] { 0x8000000000000001UL, true, false, false, true, false, false, false, true }; // -epsilon
            yield return new object[] { 0x00038D7EA4C67FFFUL, true, false, false, false, true, false, false, true }; // largest_subnormal
            yield return new object[] { 0x80038D7EA4C67FFFUL, true, false, false, true, false, false, false, true }; // -largest_subnormal
            yield return new object[] { 0x01E0000000000001UL, true, false, false, false, true, false, false, true }; // smallest_normal
            yield return new object[] { 0x81E0000000000001UL, true, false, false, true, false, false, false, true }; // -smallest_normal
            yield return new object[] { 0x77FB86F26FC0FFFFUL, true, false, false, false, true, false, false, true }; // maxvalue
            yield return new object[] { 0xF7FB86F26FC0FFFFUL, true, false, false, true, false, false, false, true }; // -maxvalue
        }

        [Theory]
        [MemberData(nameof(Classification_TestData))]
        public static void ClassificationTest(ulong value, bool isFinite, bool isInfinity, bool isNaN, bool isNegative, bool isPositive, bool isNegativeInfinity, bool isPositiveInfinity, bool isRealNumber)
        {
            Decimal64 d = Unsafe.BitCast<ulong, Decimal64>(value);
            Assert.Equal(isFinite, Decimal64.IsFinite(d));
            Assert.Equal(isInfinity, Decimal64.IsInfinity(d));
            Assert.Equal(isNaN, Decimal64.IsNaN(d));
            Assert.Equal(isNegative, Decimal64.IsNegative(d));
            Assert.Equal(isPositive, Decimal64.IsPositive(d));
            Assert.Equal(isNegativeInfinity, Decimal64.IsNegativeInfinity(d));
            Assert.Equal(isPositiveInfinity, Decimal64.IsPositiveInfinity(d));
            Assert.Equal(isRealNumber, Decimal64.IsRealNumber(d));
        }

        public static IEnumerable<object[]> IsNormalIsSubnormal_TestData()
        {
            yield return new object[] { 0x7C00000000000000UL, false, false }; // NaN
            yield return new object[] { 0xFC00000000000000UL, false, false }; // -NaN
            yield return new object[] { 0x7800000000000000UL, false, false }; // +Inf
            yield return new object[] { 0xF800000000000000UL, false, false }; // -Inf
            yield return new object[] { 0x31C0000000000000UL, false, false }; // 0
            yield return new object[] { 0xB1C0000000000000UL, false, false }; // -0
            yield return new object[] { 0x31C0000000000001UL, true, false }; // 1
            yield return new object[] { 0xB1C0000000000001UL, true, false }; // -1
            yield return new object[] { 0x31C0000000000002UL, true, false }; // 2
            yield return new object[] { 0xB1C0000000000002UL, true, false }; // -2
            yield return new object[] { 0x31C0000000000003UL, true, false }; // 3
            yield return new object[] { 0xB1C0000000000003UL, true, false }; // -3
            yield return new object[] { 0x31C0000000000005UL, true, false }; // 5
            yield return new object[] { 0xB1C0000000000005UL, true, false }; // -5
            yield return new object[] { 0x31C000000000000AUL, true, false }; // 10
            yield return new object[] { 0xB1C000000000000AUL, true, false }; // -10
            yield return new object[] { 0x31A0000000000005UL, true, false }; // 0.5
            yield return new object[] { 0xB1A0000000000005UL, true, false }; // -0.5
            yield return new object[] { 0x31A0000000000019UL, true, false }; // 2.5
            yield return new object[] { 0x31A000000000000FUL, true, false }; // 1.5
            yield return new object[] { 0x31C0000000000064UL, true, false }; // 100
            yield return new object[] { 0x31C00000000003E8UL, true, false }; // 1000
            yield return new object[] { 0x31A0000000000001UL, true, false }; // 0.1
            yield return new object[] { 0x31C0000000000009UL, true, false }; // 9
            yield return new object[] { 0x3220000000000001UL, true, false }; // 1E+3
            yield return new object[] { 0x31A000000000000AUL, true, false }; // 1.0
            yield return new object[] { 0x31800000000000C8UL, true, false }; // 2.00
            yield return new object[] { 0x31E0000000000002UL, true, false }; // 2E+1
            yield return new object[] { 0x0000000000000001UL, false, true }; // epsilon
            yield return new object[] { 0x8000000000000001UL, false, true }; // -epsilon
            yield return new object[] { 0x00038D7EA4C67FFFUL, false, true }; // largest_subnormal
            yield return new object[] { 0x80038D7EA4C67FFFUL, false, true }; // -largest_subnormal
            yield return new object[] { 0x01E0000000000001UL, true, false }; // smallest_normal
            yield return new object[] { 0x81E0000000000001UL, true, false }; // -smallest_normal
            yield return new object[] { 0x77FB86F26FC0FFFFUL, true, false }; // maxvalue
            yield return new object[] { 0xF7FB86F26FC0FFFFUL, true, false }; // -maxvalue
        }

        [Theory]
        [MemberData(nameof(IsNormalIsSubnormal_TestData))]
        public static void IsNormalIsSubnormalTest(ulong value, bool isNormal, bool isSubnormal)
        {
            Decimal64 d = Unsafe.BitCast<ulong, Decimal64>(value);
            Assert.Equal(isNormal, Decimal64.IsNormal(d));
            Assert.Equal(isSubnormal, Decimal64.IsSubnormal(d));
        }

        public static IEnumerable<object[]> IsInteger_TestData()
        {
            yield return new object[] { 0x7C00000000000000UL, false, false, false }; // NaN
            yield return new object[] { 0xFC00000000000000UL, false, false, false }; // -NaN
            yield return new object[] { 0x7800000000000000UL, false, false, false }; // +Inf
            yield return new object[] { 0xF800000000000000UL, false, false, false }; // -Inf
            yield return new object[] { 0x31C0000000000000UL, true, true, false }; // 0
            yield return new object[] { 0xB1C0000000000000UL, true, true, false }; // -0
            yield return new object[] { 0x31C0000000000001UL, true, false, true }; // 1
            yield return new object[] { 0xB1C0000000000001UL, true, false, true }; // -1
            yield return new object[] { 0x31C0000000000002UL, true, true, false }; // 2
            yield return new object[] { 0xB1C0000000000002UL, true, true, false }; // -2
            yield return new object[] { 0x31C0000000000003UL, true, false, true }; // 3
            yield return new object[] { 0xB1C0000000000003UL, true, false, true }; // -3
            yield return new object[] { 0x31C0000000000005UL, true, false, true }; // 5
            yield return new object[] { 0xB1C0000000000005UL, true, false, true }; // -5
            yield return new object[] { 0x31C000000000000AUL, true, true, false }; // 10
            yield return new object[] { 0xB1C000000000000AUL, true, true, false }; // -10
            yield return new object[] { 0x31A0000000000005UL, false, false, false }; // 0.5
            yield return new object[] { 0xB1A0000000000005UL, false, false, false }; // -0.5
            yield return new object[] { 0x31A0000000000019UL, false, false, false }; // 2.5
            yield return new object[] { 0x31A000000000000FUL, false, false, false }; // 1.5
            yield return new object[] { 0x31C0000000000064UL, true, true, false }; // 100
            yield return new object[] { 0x31C00000000003E8UL, true, true, false }; // 1000
            yield return new object[] { 0x31A0000000000001UL, false, false, false }; // 0.1
            yield return new object[] { 0x31C0000000000009UL, true, false, true }; // 9
            yield return new object[] { 0x3220000000000001UL, true, true, false }; // 1E+3
            yield return new object[] { 0x31A000000000000AUL, true, false, true }; // 1.0
            yield return new object[] { 0x31800000000000C8UL, true, true, false }; // 2.00
            yield return new object[] { 0x31E0000000000002UL, true, true, false }; // 2E+1
            yield return new object[] { 0x0000000000000001UL, false, false, false }; // epsilon
            yield return new object[] { 0x8000000000000001UL, false, false, false }; // -epsilon
            yield return new object[] { 0x00038D7EA4C67FFFUL, false, false, false }; // largest_subnormal
            yield return new object[] { 0x80038D7EA4C67FFFUL, false, false, false }; // -largest_subnormal
            yield return new object[] { 0x01E0000000000001UL, false, false, false }; // smallest_normal
            yield return new object[] { 0x81E0000000000001UL, false, false, false }; // -smallest_normal
            yield return new object[] { 0x77FB86F26FC0FFFFUL, true, true, false }; // maxvalue
            yield return new object[] { 0xF7FB86F26FC0FFFFUL, true, true, false }; // -maxvalue
        }

        [Theory]
        [MemberData(nameof(IsInteger_TestData))]
        public static void IsIntegerTest(ulong value, bool isInteger, bool isEvenInteger, bool isOddInteger)
        {
            Decimal64 d = Unsafe.BitCast<ulong, Decimal64>(value);
            Assert.Equal(isInteger, Decimal64.IsInteger(d));
            Assert.Equal(isEvenInteger, Decimal64.IsEvenInteger(d));
            Assert.Equal(isOddInteger, Decimal64.IsOddInteger(d));
        }

        public static IEnumerable<object[]> MaxMagnitude_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000005UL };
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000003UL, 0x31C0000000000005UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000005UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000005UL };
            yield return new object[] { 0xB1C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000005UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000003UL, 0x7C00000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0x7C00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xF800000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0xB1C0000000000005UL, 0x7800000000000000UL, 0x7800000000000000UL };
        }

        [Theory]
        [MemberData(nameof(MaxMagnitude_TestData))]
        public static void MaxMagnitudeTest(ulong x, ulong y, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.MaxMagnitude(Unsafe.BitCast<ulong, Decimal64>(x), Unsafe.BitCast<ulong, Decimal64>(y))));
        }

        public static IEnumerable<object[]> MinMagnitude_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0xB1C0000000000000UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0xB1C0000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000003UL, 0x7C00000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0x7C00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xF800000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0xB1C0000000000005UL, 0x7800000000000000UL, 0xB1C0000000000005UL };
        }

        [Theory]
        [MemberData(nameof(MinMagnitude_TestData))]
        public static void MinMagnitudeTest(ulong x, ulong y, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.MinMagnitude(Unsafe.BitCast<ulong, Decimal64>(x), Unsafe.BitCast<ulong, Decimal64>(y))));
        }

        public static IEnumerable<object[]> MaxMagnitudeNumber_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000005UL };
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000003UL, 0x31C0000000000005UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000005UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000005UL };
            yield return new object[] { 0xB1C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000005UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x7C00000000000000UL, 0x31C0000000000003UL };
            yield return new object[] { 0x7C00000000000000UL, 0xF800000000000000UL, 0xF800000000000000UL };
            yield return new object[] { 0xB1C0000000000005UL, 0x7800000000000000UL, 0x7800000000000000UL };
        }

        [Theory]
        [MemberData(nameof(MaxMagnitudeNumber_TestData))]
        public static void MaxMagnitudeNumberTest(ulong x, ulong y, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.MaxMagnitudeNumber(Unsafe.BitCast<ulong, Decimal64>(x), Unsafe.BitCast<ulong, Decimal64>(y))));
        }

        public static IEnumerable<object[]> MinMagnitudeNumber_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0xB1C0000000000000UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0xB1C0000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x7C00000000000000UL, 0x31C0000000000003UL };
            yield return new object[] { 0x7C00000000000000UL, 0xF800000000000000UL, 0xF800000000000000UL };
            yield return new object[] { 0xB1C0000000000005UL, 0x7800000000000000UL, 0xB1C0000000000005UL };
        }

        [Theory]
        [MemberData(nameof(MinMagnitudeNumber_TestData))]
        public static void MinMagnitudeNumberTest(ulong x, ulong y, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.MinMagnitudeNumber(Unsafe.BitCast<ulong, Decimal64>(x), Unsafe.BitCast<ulong, Decimal64>(y))));
        }

        public static IEnumerable<object[]> MultiplyAddEstimate_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000002UL, 0x31C0000000000011UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000002UL, 0xB1C000000000000DUL };
            yield return new object[] { 0x31A0000000000001UL, 0x31A0000000000001UL, 0x31C0000000000001UL, 0x3180000000000065UL };
            yield return new object[] { 0x31C0000000000002UL, 0x31A0000000000019UL, 0xB1A0000000000005UL, 0x31A000000000002DUL };
            yield return new object[] { 0x31C000000000000AUL, 0x31C000000000000AUL, 0x31C0000000000001UL, 0x31C0000000000065UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000002UL, 0x31C0000000000003UL, 0x7C00000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x31C0000000000000UL, 0x31C0000000000001UL, 0x7C00000000000000UL };
            yield return new object[] { 0x31C0000000000002UL, 0x31C0000000000003UL, 0xF800000000000000UL, 0xF800000000000000UL };
        }

        [Theory]
        [MemberData(nameof(MultiplyAddEstimate_TestData))]
        public static void MultiplyAddEstimateTest(ulong left, ulong right, ulong addend, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.MultiplyAddEstimate(Unsafe.BitCast<ulong, Decimal64>(left), Unsafe.BitCast<ulong, Decimal64>(right), Unsafe.BitCast<ulong, Decimal64>(addend))));
        }

        public static IEnumerable<object[]> Max_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000005UL };
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000003UL, 0x31C0000000000005UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000005UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xB1C0000000000005UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31A000000000000AUL, 0x3180000000000064UL, 0x3180000000000064UL };
            yield return new object[] { 0x31E0000000000002UL, 0x31C0000000000014UL, 0x31C0000000000014UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x31C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000003UL, 0x7C00000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0x7C00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0xFC00000000000000UL, 0xB1C0000000000003UL, 0xFC00000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0xFC00000000000000UL, 0xFC00000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xFC00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x31C0000000000003UL, 0x7800000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0xF800000000000000UL, 0x31C0000000000003UL };
            yield return new object[] { 0xF800000000000000UL, 0x7800000000000000UL, 0x7800000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x7C00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xF800000000000000UL, 0x7C00000000000000UL };
        }

        [Theory]
        [MemberData(nameof(Max_TestData))]
        public static void MaxTest(ulong x, ulong y, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.Max(Unsafe.BitCast<ulong, Decimal64>(x), Unsafe.BitCast<ulong, Decimal64>(y))));
        }

        public static IEnumerable<object[]> Min_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000005UL };
            yield return new object[] { 0xB1C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000005UL };
            yield return new object[] { 0xB1C0000000000005UL, 0xB1C0000000000003UL, 0xB1C0000000000005UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31A000000000000AUL, 0x3180000000000064UL, 0x3180000000000064UL };
            yield return new object[] { 0x31E0000000000002UL, 0x31C0000000000014UL, 0x31C0000000000014UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0xB1C0000000000000UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0xB1C0000000000000UL };
            yield return new object[] { 0x31C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000003UL, 0x7C00000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0x7C00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0xFC00000000000000UL, 0xB1C0000000000003UL, 0xFC00000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0xFC00000000000000UL, 0xFC00000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xFC00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xF800000000000000UL, 0xF800000000000000UL };
            yield return new object[] { 0xF800000000000000UL, 0x7800000000000000UL, 0xF800000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x7C00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xF800000000000000UL, 0x7C00000000000000UL };
        }

        [Theory]
        [MemberData(nameof(Min_TestData))]
        public static void MinTest(ulong x, ulong y, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.Min(Unsafe.BitCast<ulong, Decimal64>(x), Unsafe.BitCast<ulong, Decimal64>(y))));
        }

        public static IEnumerable<object[]> MaxNative_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000005UL };
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000003UL, 0x31C0000000000005UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000005UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xB1C0000000000005UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31A000000000000AUL, 0x3180000000000064UL, 0x3180000000000064UL };
            yield return new object[] { 0x31E0000000000002UL, 0x31C0000000000014UL, 0x31C0000000000014UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0xB1C0000000000000UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x31C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x7C00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0xFC00000000000000UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xFC00000000000000UL, 0xFC00000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xFC00000000000000UL, 0xFC00000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x31C0000000000003UL, 0x7800000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0xF800000000000000UL, 0x31C0000000000003UL };
            yield return new object[] { 0xF800000000000000UL, 0x7800000000000000UL, 0x7800000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x7C00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xF800000000000000UL, 0xF800000000000000UL };
        }

        [Theory]
        [MemberData(nameof(MaxNative_TestData))]
        public static void MaxNativeTest(ulong x, ulong y, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.MaxNative(Unsafe.BitCast<ulong, Decimal64>(x), Unsafe.BitCast<ulong, Decimal64>(y))));
        }

        public static IEnumerable<object[]> MinNative_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000005UL };
            yield return new object[] { 0xB1C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000005UL };
            yield return new object[] { 0xB1C0000000000005UL, 0xB1C0000000000003UL, 0xB1C0000000000005UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31A000000000000AUL, 0x3180000000000064UL, 0x3180000000000064UL };
            yield return new object[] { 0x31E0000000000002UL, 0x31C0000000000014UL, 0x31C0000000000014UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0xB1C0000000000000UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x31C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x7C00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0xFC00000000000000UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xFC00000000000000UL, 0xFC00000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xFC00000000000000UL, 0xFC00000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xF800000000000000UL, 0xF800000000000000UL };
            yield return new object[] { 0xF800000000000000UL, 0x7800000000000000UL, 0xF800000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x7C00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xF800000000000000UL, 0xF800000000000000UL };
        }

        [Theory]
        [MemberData(nameof(MinNative_TestData))]
        public static void MinNativeTest(ulong x, ulong y, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.MinNative(Unsafe.BitCast<ulong, Decimal64>(x), Unsafe.BitCast<ulong, Decimal64>(y))));
        }

        public static IEnumerable<object[]> MaxNumber_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000005UL };
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000003UL, 0x31C0000000000005UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000005UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xB1C0000000000005UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31A000000000000AUL, 0x3180000000000064UL, 0x3180000000000064UL };
            yield return new object[] { 0x31E0000000000002UL, 0x31C0000000000014UL, 0x31C0000000000014UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x31C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x7C00000000000000UL, 0x31C0000000000003UL };
            yield return new object[] { 0xFC00000000000000UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xFC00000000000000UL, 0x31C0000000000003UL };
            yield return new object[] { 0x7C00000000000000UL, 0xFC00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x31C0000000000003UL, 0x7800000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0xF800000000000000UL, 0x31C0000000000003UL };
            yield return new object[] { 0xF800000000000000UL, 0x7800000000000000UL, 0x7800000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x7C00000000000000UL, 0x7800000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xF800000000000000UL, 0xF800000000000000UL };
        }

        [Theory]
        [MemberData(nameof(MaxNumber_TestData))]
        public static void MaxNumberTest(ulong x, ulong y, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.MaxNumber(Unsafe.BitCast<ulong, Decimal64>(x), Unsafe.BitCast<ulong, Decimal64>(y))));
        }

        public static IEnumerable<object[]> MinNumber_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000005UL };
            yield return new object[] { 0xB1C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000005UL };
            yield return new object[] { 0xB1C0000000000005UL, 0xB1C0000000000003UL, 0xB1C0000000000005UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31A000000000000AUL, 0x3180000000000064UL, 0x3180000000000064UL };
            yield return new object[] { 0x31E0000000000002UL, 0x31C0000000000014UL, 0x31C0000000000014UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0xB1C0000000000000UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0xB1C0000000000000UL };
            yield return new object[] { 0x31C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x7C00000000000000UL, 0x31C0000000000003UL };
            yield return new object[] { 0xFC00000000000000UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xFC00000000000000UL, 0x31C0000000000003UL };
            yield return new object[] { 0x7C00000000000000UL, 0xFC00000000000000UL, 0x7C00000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xF800000000000000UL, 0xF800000000000000UL };
            yield return new object[] { 0xF800000000000000UL, 0x7800000000000000UL, 0xF800000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x7C00000000000000UL, 0x7800000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xF800000000000000UL, 0xF800000000000000UL };
        }

        [Theory]
        [MemberData(nameof(MinNumber_TestData))]
        public static void MinNumberTest(ulong x, ulong y, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.MinNumber(Unsafe.BitCast<ulong, Decimal64>(x), Unsafe.BitCast<ulong, Decimal64>(y))));
        }

        public static IEnumerable<object[]> CopySign_TestData()
        {
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000003UL, 0x31C0000000000005UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000005UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0xB1C0000000000005UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xB1C0000000000005UL, 0xB1C0000000000003UL, 0xB1C0000000000005UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xB1C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x31A000000000000AUL, 0x3180000000000064UL, 0x31A000000000000AUL };
            yield return new object[] { 0x31E0000000000002UL, 0x31C0000000000014UL, 0x31E0000000000002UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0xB1C0000000000000UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x31C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000003UL, 0x7C00000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0x7C00000000000000UL, 0x31C0000000000003UL };
            yield return new object[] { 0xFC00000000000000UL, 0xB1C0000000000003UL, 0xFC00000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0xFC00000000000000UL, 0xB1C0000000000003UL };
            yield return new object[] { 0x7C00000000000000UL, 0xFC00000000000000UL, 0xFC00000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x31C0000000000003UL, 0x7800000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0xF800000000000000UL, 0xB1C0000000000003UL };
            yield return new object[] { 0xF800000000000000UL, 0x7800000000000000UL, 0x7800000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x7C00000000000000UL, 0x7800000000000000UL };
            yield return new object[] { 0x7C00000000000000UL, 0xF800000000000000UL, 0xFC00000000000000UL };
        }

        [Theory]
        [MemberData(nameof(CopySign_TestData))]
        public static void CopySignTest(ulong value, ulong sign, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.CopySign(Unsafe.BitCast<ulong, Decimal64>(value), Unsafe.BitCast<ulong, Decimal64>(sign))));
        }

        public static IEnumerable<object[]> Sign_TestData()
        {
            yield return new object[] { 0x31C0000000000000UL, 0 };
            yield return new object[] { 0xB1C0000000000000UL, 0 };
            yield return new object[] { 0x31C0000000000001UL, 1 };
            yield return new object[] { 0xB1C0000000000001UL, -1 };
            yield return new object[] { 0x31C0000000000005UL, 1 };
            yield return new object[] { 0xB1C0000000000005UL, -1 };
            yield return new object[] { 0x31A0000000000005UL, 1 };
            yield return new object[] { 0xB1A0000000000005UL, -1 };
            yield return new object[] { 0x31A000000000000AUL, 1 };
            yield return new object[] { 0x31E0000000000002UL, 1 };
            yield return new object[] { 0x3160000000000001UL, 1 };
            yield return new object[] { 0x7800000000000000UL, 1 };
            yield return new object[] { 0xF800000000000000UL, -1 };
        }

        [Theory]
        [MemberData(nameof(Sign_TestData))]
        public static void SignTest(ulong value, int expected)
        {
            Assert.Equal(expected, Decimal64.Sign(Unsafe.BitCast<ulong, Decimal64>(value)));
        }

        [Fact]
        public static void SignNaNTest()
        {
            Assert.Throws<ArithmeticException>(() => Decimal64.Sign(Decimal64.NaN));
            Assert.Throws<ArithmeticException>(() => Decimal64.Sign(-Decimal64.NaN));
        }

        public static IEnumerable<object[]> Clamp_TestData()
        {
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C0000000000005UL };
            yield return new object[] { 0xB1C0000000000005UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C0000000000001UL };
            yield return new object[] { 0x31C000000000000FUL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C000000000000AUL };
            yield return new object[] { 0x31C0000000000001UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C0000000000001UL };
            yield return new object[] { 0x31C000000000000AUL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C000000000000AUL };
            yield return new object[] { 0x31A0000000000019UL, 0x31C0000000000002UL, 0x31C0000000000003UL, 0x31A0000000000019UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x7C00000000000000UL };
            yield return new object[] { 0x7800000000000000UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C000000000000AUL };
            yield return new object[] { 0xF800000000000000UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C0000000000001UL };
        }

        [Theory]
        [MemberData(nameof(Clamp_TestData))]
        public static void ClampTest(ulong value, ulong min, ulong max, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.Clamp(Unsafe.BitCast<ulong, Decimal64>(value), Unsafe.BitCast<ulong, Decimal64>(min), Unsafe.BitCast<ulong, Decimal64>(max))));
        }

        [Fact]
        public static void ClampMinGreaterThanMaxTest()
        {
            Assert.Throws<ArgumentException>(() => Decimal64.Clamp(Decimal64.One, Unsafe.BitCast<ulong, Decimal64>(0x31C000000000000AUL), Unsafe.BitCast<ulong, Decimal64>(0x31C0000000000001UL)));
        }

        public static IEnumerable<object[]> ClampNative_TestData()
        {
            yield return new object[] { 0x31C0000000000005UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C0000000000005UL };
            yield return new object[] { 0xB1C0000000000005UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C0000000000001UL };
            yield return new object[] { 0x31C000000000000FUL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C000000000000AUL };
            yield return new object[] { 0x31C0000000000001UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C0000000000001UL };
            yield return new object[] { 0x31C000000000000AUL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C000000000000AUL };
            yield return new object[] { 0x31A0000000000019UL, 0x31C0000000000002UL, 0x31C0000000000003UL, 0x31A0000000000019UL };
            yield return new object[] { 0xB1C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x31C0000000000000UL, 0xB1C0000000000000UL, 0x31C0000000000000UL, 0x31C0000000000000UL };
            yield return new object[] { 0x31C0000000000003UL, 0xB1C0000000000003UL, 0x31C0000000000003UL, 0x31C0000000000003UL };
            yield return new object[] { 0x7C00000000000000UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C0000000000001UL };
            yield return new object[] { 0x7800000000000000UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C000000000000AUL };
            yield return new object[] { 0xF800000000000000UL, 0x31C0000000000001UL, 0x31C000000000000AUL, 0x31C0000000000001UL };
        }

        [Theory]
        [MemberData(nameof(ClampNative_TestData))]
        public static void ClampNativeTest(ulong value, ulong min, ulong max, ulong expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal64, ulong>(Decimal64.ClampNative(Unsafe.BitCast<ulong, Decimal64>(value), Unsafe.BitCast<ulong, Decimal64>(min), Unsafe.BitCast<ulong, Decimal64>(max))));
        }

        [Fact]
        public static void ClampNativeMinGreaterThanMaxTest()
        {
            Assert.Throws<ArgumentException>(() => Decimal64.ClampNative(Decimal64.One, Unsafe.BitCast<ulong, Decimal64>(0x31C000000000000AUL), Unsafe.BitCast<ulong, Decimal64>(0x31C0000000000001UL)));
        }
    }
}
