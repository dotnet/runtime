// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public class Decimal128Tests
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

            yield return new object[] { "-123", defaultStyle, null, Decimal128.Parse("-123") };
            yield return new object[] { "0", defaultStyle, null, Decimal128.Parse("0") };
            yield return new object[] { "123", defaultStyle, null, Decimal128.Parse("123") };
            yield return new object[] { "  123  ", defaultStyle, null, Decimal128.Parse("123") };
            yield return new object[] { (567.89).ToString(), defaultStyle, null, Decimal128.Parse("567.89") };
            yield return new object[] { (-567.89).ToString(), defaultStyle, null, Decimal128.Parse("-567.89") };
            yield return new object[] { "0.666666666666666666666666666666666650000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, Decimal128.Parse("0.66666666666666666666666666666666665") };

            yield return new object[] { "0." + new string('0', 6176) + "1", defaultStyle, invariantFormat, Decimal128.Parse("0") };
            yield return new object[] { "-0." + new string('0', 6176) + "1", defaultStyle, invariantFormat, Decimal128.Parse("0") };
            yield return new object[] { "0." + new string('0', 6175) + "1", defaultStyle, invariantFormat, Decimal128.Parse("1e-6176") };
            yield return new object[] { "-0." + new string('0', 6175) + "1", defaultStyle, invariantFormat, Decimal128.Parse("-1e-6176") };

            yield return new object[] { "0." + new string('0', 6174) + "12345", defaultStyle, invariantFormat, Decimal128.Parse("1.2345e-6175") };
            yield return new object[] { "-0." + new string('0', 6174) + "12345", defaultStyle, invariantFormat, Decimal128.Parse("-1.2345e-6175") };
            yield return new object[] { "0." + new string('0', 6174) + "12662", defaultStyle, invariantFormat, Decimal128.Parse("1.2662e-6175") };
            yield return new object[] { "-0." + new string('0', 6174) + "12662", defaultStyle, invariantFormat, Decimal128.Parse("-1.2662e-6175") };

            yield return new object[] { emptyFormat.NumberDecimalSeparator + "234", defaultStyle, null, Decimal128.Parse("2.34e-1") };
            yield return new object[] { "234" + emptyFormat.NumberDecimalSeparator, defaultStyle, null, Decimal128.Parse("2.34e2") };
            yield return new object[] { "7" + new string('0', 6144) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, Decimal128.Parse("7e6144") };
            yield return new object[] { "07" + new string('0', 6144) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, Decimal128.Parse("7e6144") };

            yield return new object[] { (123.1).ToString(), NumberStyles.AllowDecimalPoint, null, Decimal128.Parse("1.231e2") };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.AllowThousands, null, Decimal128.Parse("1e3") };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, Decimal128.Parse("123") };
            yield return new object[] { (123.567).ToString(), NumberStyles.Any, emptyFormat, Decimal128.Parse("1.23567e2") };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, Decimal128.Parse("123") };
            yield return new object[] { "$1000", NumberStyles.Currency, customFormat1, Decimal128.Parse("1e3") };
            yield return new object[] { "123.123", NumberStyles.Float, customFormat2, Decimal128.Parse("1.23123e2") };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, customFormat2, Decimal128.Parse("-123") };

            yield return new object[] { "NaN", NumberStyles.Any, invariantFormat, Decimal128.NaN };
            yield return new object[] { "+NaN", NumberStyles.Any, invariantFormat, Decimal128.NaN };
            yield return new object[] { "Infinity", NumberStyles.Any, invariantFormat, Decimal128.PositiveInfinity };
            yield return new object[] { "+Infinity", NumberStyles.Any, invariantFormat, Decimal128.PositiveInfinity };
            yield return new object[] { "1" + new string('0', 6145), NumberStyles.Any, invariantFormat, Decimal128.PositiveInfinity };
            yield return new object[] { "-Infinity", NumberStyles.Any, invariantFormat, Decimal128.NegativeInfinity };
            yield return new object[] { "-1" + new string('0', 6145), NumberStyles.Any, invariantFormat, Decimal128.NegativeInfinity };
        }


        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, Decimal128 expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Decimal128 result;
            if ((style & ~NumberStyles.Number) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(Decimal128.TryParse(value, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, Decimal128.Parse(value));
                }

                Assert.Equal(expected, Decimal128.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(Decimal128.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, Decimal128.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(Decimal128.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, Decimal128.Parse(value, style));
                Assert.Equal(expected, Decimal128.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        [Theory]
        [MemberData(nameof(Parse_Preserve_TrailingZero_TestData))]
        public static void Parse_Preserve_TrailingZero(string value, string expected)
        {
            Assert.Equal(expected, Decimal128.Parse(value).ToString(CultureInfo.InvariantCulture));
        }

        public static IEnumerable<object[]> Parse_Preserve_TrailingZero_TestData()
        {
            yield return new object[] { "0.00", "0.00" };
            yield return new object[] { "0." + new string('0', 6176), "0." + new string('0', 6176) };
            yield return new object[] { "0." + new string('0', 10000), "0." + new string('0', 6176) };
            yield return new object[] { "0." + new string('0', 10000) + "1234567", "0." + new string('0', 6176) };
            yield return new object[] { "0e-2", "0.00" };
            yield return new object[] { "0e-6176", "0." + new string('0', 6176) };
            yield return new object[] { "0e-10000", "0." + new string('0', 6176) };
            yield return new object[] { "0.123e-10000", "0." + new string('0', 6176) };
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
            Decimal128 result;
            if ((style & ~NumberStyles.Number) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(Decimal128.TryParse(value, out result));
                    Assert.Equal(default(Decimal128), result);

                    Assert.Throws(exceptionType, () => Decimal128.Parse(value));
                }

                Assert.Throws(exceptionType, () => Decimal128.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(Decimal128.TryParse(value, style, provider, out result));
            Assert.Equal(default(Decimal128), result);

            Assert.Throws(exceptionType, () => Decimal128.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(Decimal128.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(Decimal128), result);

                Assert.Throws(exceptionType, () => Decimal128.Parse(value, style));
                Assert.Throws(exceptionType, () => Decimal128.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "-123", 1, 3, NumberStyles.Number, null, Decimal128.Parse("123") };
            yield return new object[] { "-123", 0, 3, NumberStyles.Number, null, Decimal128.Parse("-12") };
            yield return new object[] { 1000.ToString("N0"), 0, 4, NumberStyles.AllowThousands, null, Decimal128.Parse("100") };
            yield return new object[] { 1000.ToString("N0"), 2, 3, NumberStyles.AllowThousands, null, Decimal128.Parse("0") };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, Decimal128.Parse("123") };
            yield return new object[] { "1234567890123456789012345.678456", 1, 4, NumberStyles.Number, new NumberFormatInfo() { NumberDecimalSeparator = "." }, Decimal128.Parse("2345") };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, Decimal128 expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            Decimal128 result;
            if ((style & ~NumberStyles.Number) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(Decimal128.TryParse(value.AsSpan(offset, count), out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, Decimal128.Parse(value.AsSpan(offset, count)));
                }

                Assert.Equal(expected, Decimal128.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, Decimal128.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(Decimal128.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }


        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => Decimal128.Parse(value.AsSpan(), style, provider));

                Assert.False(Decimal128.TryParse(value.AsSpan(), style, provider, out Decimal128 result));
                Assert.Equal(default, result);
            }
        }

        [Theory]
        [MemberData(nameof(Rounding_TestData))]
        public static void Rounding(string s1, string s2)
        {
            Assert.Equal(Decimal128.Parse(s1), Decimal128.Parse(s2));
        }

        public static IEnumerable<object[]> Rounding_TestData()
        {
            yield return new object[] { "12345678901234567890123456789012348", "12345678901234567890123456789012350" };
            yield return new object[] { "12345678901234567890123456789012343", "12345678901234567890123456789012340" };
            yield return new object[] { "12345678901234567890123456789012345", "12345678901234567890123456789012340" };
            yield return new object[] { "123456789012345678901234567890123850001", "123456789012345678901234567890123900000" };
            yield return new object[] { new string('9', 34) + "1", new string('9', 34) + "0" };
            yield return new object[] { new string('9', 34) + "5", "1" + new string('0', 35) };
            yield return new object[] { new string('9', 34) + "6", "1" + new string('0', 35) };
            yield return new object[] { new string('9', 34) + "001", new string('9', 34) + "000" };
        }

        [Fact]
        public static void MaxValue_Rounding()
        {
            Assert.Equal(Decimal128.MaxValue, Decimal128.Parse(new string('9', 34) + '4' + new string('0', 6110)));
            Assert.Equal(Decimal128.PositiveInfinity, Decimal128.Parse(new string('9', 34) + '5' + new string('0', 6110)));
            Assert.Equal(Decimal128.PositiveInfinity, Decimal128.Parse(new string('9', 34) + '5' + new string('0', 6109) + '1'));
        }

        public static IEnumerable<object[]> MaxValue_Rounding_TestData()
        {
            yield return new object[] { new string('9', 34) + '1' + new string('9', 6110), Decimal128.MaxValue };
            yield return new object[] { new string('9', 34) + '4' + new string('9', 6110), Decimal128.MaxValue };

            yield return new object[] { new string('9', 34) + '5' + new string('0', 6110), Decimal128.PositiveInfinity };
            yield return new object[] { new string('9', 34) + '5' + new string('0', 6109) + '1', Decimal128.PositiveInfinity };
            yield return new object[] { "1e6145", Decimal128.PositiveInfinity };
            yield return new object[] { "10e6144", Decimal128.PositiveInfinity };
            yield return new object[] { "100.3e6143", Decimal128.PositiveInfinity };

            yield return new object[] { '-' + new string('9', 34) + '5' + new string('0', 6110), Decimal128.NegativeInfinity };
            yield return new object[] { '-' + new string('9', 34) + '5' + new string('0', 6109) + '1', Decimal128.NegativeInfinity };
            yield return new object[] { "-1e6145", Decimal128.NegativeInfinity };
            yield return new object[] { "-10e6144", Decimal128.NegativeInfinity };
            yield return new object[] { "-100.3e6143", Decimal128.NegativeInfinity };
        }

        [Theory]
        [MemberData(nameof(CompareTo_Other_ReturnsExpected_TestData))]
        public static void CompareTo_Other_ReturnsExpected(Decimal128 d1, Decimal128 d2, int expected)
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
            yield return new object[] { Decimal128.Parse("-1e1"), Decimal128.Parse("-1e1"), 0 };
            yield return new object[] { Decimal128.Parse("-2e1"), Decimal128.Parse("-3e1"), 1 };
            yield return new object[] { Decimal128.Parse("3e1"), Decimal128.Parse("2e1"), 1 };
            yield return new object[] { Decimal128.Parse("1e6144"), Decimal128.Parse("1e6144"), 0 };
            yield return new object[] { Decimal128.Parse(new string('9', 33) + "e6111"), Decimal128.Parse(new string('9', 33) + "0e6110"), 0 };
            yield return new object[] { Decimal128.Parse(new string('9', 33) + new string('0', 6111)), Decimal128.Parse(new string('9', 32) + "8e6110"), 1 };
            yield return new object[] { Decimal128.Parse(new string('9', 25) + new string('0', 6111)), Decimal128.Parse(new string('9', 24) + "8e6110"), 1 };
            yield return new object[] { Decimal128.Parse("999e10"), Decimal128.Parse("997e170"), -1 };
            yield return new object[] { Decimal128.Parse("997e170"), Decimal128.Parse("999e10"), 1 };
            yield return new object[] { Decimal128.Parse("1e1"), Decimal128.Parse("-1e0"), 1 };
            yield return new object[] { Decimal128.Parse("1e1"), Decimal128.Parse("-1e1"), 1 };
            yield return new object[] { Decimal128.Parse("1e1"), Decimal128.NaN, 1 };
            yield return new object[] { Decimal128.Parse("1e1"), Decimal128.NegativeInfinity, 1 };
            yield return new object[] { Decimal128.Parse("1e1"), Decimal128.NegativeZero, 1 };
            yield return new object[] { Decimal128.PositiveInfinity, Decimal128.Parse("1e1"), 1 };
            yield return new object[] { Decimal128.PositiveInfinity, Decimal128.Parse("1e7000"), 0 };
            yield return new object[] { Decimal128.PositiveInfinity, Decimal128.NegativeInfinity, 1 };
            yield return new object[] { Decimal128.PositiveInfinity, Decimal128.PositiveInfinity, 0 };
            yield return new object[] { Decimal128.PositiveInfinity, Decimal128.NegativeZero, 1 };
            yield return new object[] { Decimal128.NegativeInfinity, Decimal128.NegativeInfinity, 0 };
            yield return new object[] { Decimal128.NaN, Decimal128.NaN, 0 };
            yield return new object[] { Decimal128.NegativeZero, Decimal128.NegativeInfinity, 1 };
            yield return new object[] { Decimal128.NegativeZero, Decimal128.Parse("0e1"), 0 };
            yield return new object[] { Decimal128.NegativeZero, Decimal128.NaN, 1 };
            yield return new object[] { Decimal128.Epsilon, Decimal128.Parse("1e-6176"), 0 };
            yield return new object[] { Decimal128.Parse("4e-6177"), Decimal128.Zero, 0 };
            yield return new object[] { Decimal128.Parse("5e-6177"), Decimal128.Zero, 0 };
            yield return new object[] { Decimal128.Parse("5.00001e-6177"), Decimal128.Epsilon, 0 };
            yield return new object[] { Decimal128.Parse("0.5" + new string('0', 300) + "1e-6176"), Decimal128.Epsilon, 0 };
            yield return new object[] { Decimal128.Parse("5." + new string('0', 300) + "1e-6177"), Decimal128.Epsilon, 0 };
            yield return new object[] { Decimal128.Parse("5." + new string('0', 300) + "1e-6178"), Decimal128.Zero, 0 };
            yield return new object[] { Decimal128.Parse("6e-6177"), Decimal128.Parse("1e-6176"), 0 };
            yield return new object[] { Decimal128.Parse("1" + new string('0', 43) + "1e-6220"), Decimal128.Epsilon, 0 };
            yield return new object[] { Decimal128.Parse("-1" + new string('0', 43) + "1e-6220"), Decimal128.Parse("-1e-6176"), 0 };
            for (int i = 1; i < 30; i++)
            {
                var d1 = Decimal128.Parse("1e" + i);
                var d2 = Decimal128.Parse("1" + new string('0', i));
                yield return new object[] { d1, d2, 0 };
            }
        }

        [Theory]
        [MemberData(nameof(GetHashCode_TestData))]
        public static void GetHashCodeTest(Decimal128 d1, Decimal128 d2)
        {
            Assert.Equal(d1.GetHashCode(), d2.GetHashCode());
        }

        public static IEnumerable<object[]> GetHashCode_TestData()
        {
            yield return new object[] { Decimal128.Zero, Decimal128.NegativeZero };
            yield return new object[] { Decimal128.Zero, Decimal128.Zero };
            yield return new object[] { Decimal128.NaN, Decimal128.NaN };
            yield return new object[] { Decimal128.Parse("0e20"), Decimal128.Parse("0e18") };
            yield return new object[] { Decimal128.Parse("1e7"), Decimal128.Parse("1e7") };
            yield return new object[] { Decimal128.Parse("1e7"), Decimal128.Parse("10e6") };
            yield return new object[] { Decimal128.PositiveInfinity, Decimal128.PositiveInfinity };
            yield return new object[] { Decimal128.NegativeInfinity, Decimal128.NegativeInfinity };
        }

        [Fact]
        public static void CompareToZero()
        {
            var zero = Decimal128.Parse("0e1");
            Assert.Equal(zero, Decimal128.Parse("0e20"));
            Assert.Equal(zero, Decimal128.Parse("1e-6177"));
            Assert.Equal(zero, Decimal128.Parse("234e-10000"));
            Assert.Equal(zero, Decimal128.Parse("-1e-6177"));
            Assert.Equal(zero, Decimal128.Parse("-234e-10000"));
            Assert.Equal(zero, Decimal128.Zero);
            Assert.Equal(zero, Decimal128.NegativeZero);
            Assert.Equal(Decimal128.Zero, Decimal128.NegativeZero);
        }
        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                yield return new object[] { Decimal128.Parse("-0"), "G", defaultFormat, "-0" };
                yield return new object[] { Decimal128.NegativeZero, "G", defaultFormat, "-0" };
                yield return new object[] { Decimal128.Parse("-0.0000"), "G", defaultFormat, "-0.0000" };
                yield return new object[] { Decimal128.Parse("0"), "G", defaultFormat, "0" };
                yield return new object[] { Decimal128.Zero, "G", defaultFormat, "0" };
                yield return new object[] { Decimal128.Parse("0.0000"), "G", defaultFormat, "0.0000" };
                yield return new object[] { Decimal128.Parse($"{Int128.MinValue}"), "G", defaultFormat, "-170141183460469231731687303715884100000" };
                yield return new object[] { Decimal128.Parse($"{Int128.MaxValue}"), "G", defaultFormat, "170141183460469231731687303715884100000" };
                yield return new object[] { Decimal128.Parse("3e6144"), "G", defaultFormat, "3" + new string('0', 6144) };
                yield return new object[] { Decimal128.Parse("-3e6144"), "G", defaultFormat, "-3" + new string('0', 6144) };
                yield return new object[] { Decimal128.Parse("-4567"), "G", defaultFormat, "-4567" };
                yield return new object[] { Decimal128.Parse("-4567.891"), "G", defaultFormat, "-4567.891" };
                yield return new object[] { Decimal128.Parse("0"), "G", defaultFormat, "0" };
                yield return new object[] { Decimal128.Parse("4567"), "G", defaultFormat, "4567" };
                yield return new object[] { Decimal128.Parse("4567.891"), "G", defaultFormat, "4567.891" };

                yield return new object[] { Decimal128.Parse("2468"), "N", defaultFormat, "2,468.00" };

                yield return new object[] { Decimal128.Parse("2467"), "[#-##-#]", defaultFormat, "[2-46-7]" };

                yield return new object[] { Decimal128.Parse("4e-6177"), "G", defaultFormat, "0." + new string('0', 6176) };
                yield return new object[] { Decimal128.Parse("5e-6177"), "G", defaultFormat, "0." + new string('0', 6176) };
                yield return new object[] { Decimal128.Parse("5.00000000000000000000000000000000000000001e-6177"), "G", defaultFormat, "0." + new string('0', 6175) + "1" };
                yield return new object[] { Decimal128.Parse("6e-6177"), "G", defaultFormat, "0." + new string('0', 6175) + "1" };
                yield return new object[] { Decimal128.Parse("-4e-6177"), "G", defaultFormat, "-0." + new string('0', 6176) };
                yield return new object[] { Decimal128.Parse("-5e-6177"), "G", defaultFormat, "-0." + new string('0', 6176) };
                yield return new object[] { Decimal128.Parse("-5.00000000000000000000000000000000000000001e-6177"), "G", defaultFormat, "-0." + new string('0', 6175) + "1" };
                yield return new object[] { Decimal128.Parse("-6e-6177"), "G", defaultFormat, "-0." + new string('0', 6175) + "1" };

            }
        }

        [Fact]
        public static void Test_ToString()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (object[] testdata in ToString_TestData())
                {
                    ToString((Decimal128)testdata[0], (string)testdata[1], (IFormatProvider)testdata[2], (string)testdata[3]);
                }
            }
        }

        private static void ToString(Decimal128 f, string format, IFormatProvider provider, string expected)
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
        [MemberData(nameof(PositiveInfinity_NonCanonicalEncodings128_TestData))]
        [MemberData(nameof(NegativeInfinity_NonCanonicalEncodings128_TestData))]
        [MemberData(nameof(NaN_NonCanonicalEncodings128_TestData))]
        public static void NaN_Infinity_NonCanonicalEncodings128_Compare(Decimal128 d, UInt128 encoding)
        {
            Decimal128 d2 = Unsafe.BitCast<UInt128, Decimal128>(encoding);
            Assert.Equal(0, d.CompareTo(d2));
            Assert.Equal(d.GetHashCode(), d2.GetHashCode());
        }

        public static IEnumerable<object[]> PositiveInfinity_NonCanonicalEncodings128_TestData()
        {
            UInt128 canonical = (UInt128)0x7800_0000_0000_0000UL << 64;

            yield return new object[] { Decimal128.PositiveInfinity, canonical };
            yield return new object[] { Decimal128.PositiveInfinity, canonical | (UInt128)1 };
            yield return new object[] { Decimal128.PositiveInfinity, canonical | ((UInt128)1 << 80) };
            yield return new object[] { Decimal128.PositiveInfinity, canonical | (UInt128)0x1234 };
            yield return new object[] { Decimal128.PositiveInfinity, canonical | (UInt128)0xFFFFF };
            yield return new object[] { Decimal128.PositiveInfinity, canonical | (((UInt128)1 << 110) - 1) };
        }

        public static IEnumerable<object[]> NegativeInfinity_NonCanonicalEncodings128_TestData()
        {
            UInt128 canonical = (UInt128)0xF800_0000_0000_0000UL << 64;

            yield return new object[] { Decimal128.NegativeInfinity, canonical };
            yield return new object[] { Decimal128.NegativeInfinity, canonical | (UInt128)1 };
            yield return new object[] { Decimal128.NegativeInfinity, canonical | ((UInt128)1 << 80) };
            yield return new object[] { Decimal128.NegativeInfinity, canonical | (UInt128)0x1234 };
            yield return new object[] { Decimal128.NegativeInfinity, canonical | (UInt128)0xFFFFF };
            yield return new object[] { Decimal128.NegativeInfinity, canonical | (((UInt128)1 << 110) - 1) };
        }

        public static IEnumerable<object[]> NaN_NonCanonicalEncodings128_TestData()
        {
            UInt128 canonical = (UInt128)0xFC00_0000_0000_0000UL << 64;

            yield return new object[] { Decimal128.NaN, canonical };
            yield return new object[] { Decimal128.NaN, canonical | (UInt128)1 };
            yield return new object[] { Decimal128.NaN, canonical | ((UInt128)1 << 80) };
            yield return new object[] { Decimal128.NaN, canonical | (UInt128)0x1234 };
            yield return new object[] { Decimal128.NaN, canonical | (UInt128)0xFFFFF };
            yield return new object[] { Decimal128.NaN, canonical | (((UInt128)1 << 110) - 1) };
        }
    }
}
