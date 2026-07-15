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
            yield return new object[] { Decimal128.Parse("0e5"), Decimal128.Parse("1e1"), -1 };
            yield return new object[] { Decimal128.Parse("0e5"), Decimal128.Parse("-1e1"), 1 };
            yield return new object[] { Decimal128.Parse("0e-5"), Decimal128.Parse("1e1"), -1 };
            yield return new object[] { Decimal128.Parse("0e-5"), Decimal128.Parse("-1e1"), 1 };
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
            yield return new object[] { Decimal128.Parse("0.5" + new string('0', 300) + "1e-6178"), Decimal128.Epsilon, -1 };
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

        public static IEnumerable<object[]> Zero_NonCanonicalEncodings_TestData()
        {
            // A finite encoding whose significand exceeds MaxSignificand (10^34 - 1) is non-canonical and represents zero.
            // For Decimal128 every large-coefficient (G0G1 == 11) finite encoding is non-canonical.
            UInt128 positiveZero = new UInt128(0x3040_0000_0000_0000, 0);
            UInt128 negativeZero = new UInt128(0xB040_0000_0000_0000, 0);

            yield return new object[] { positiveZero, new UInt128(0x6C10_7FFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF) };
            yield return new object[] { positiveZero, new UInt128(0x6C10_0000_0000_0000, 0x0000_0000_0000_0001) };
            yield return new object[] { negativeZero, new UInt128(0xEC10_7FFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF) };
            yield return new object[] { negativeZero, new UInt128(0xEC10_0000_0000_0000, 0x0000_0000_0000_0001) };
        }

        [Theory]
        [MemberData(nameof(Zero_NonCanonicalEncodings_TestData))]
        public static void Finite_NonCanonicalEncodings_BehaveAsZero(UInt128 canonicalZero, UInt128 encoding)
        {
            Decimal128 zero = Unsafe.BitCast<UInt128, Decimal128>(canonicalZero);
            Decimal128 nc = Unsafe.BitCast<UInt128, Decimal128>(encoding);
            Decimal128 one = Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x3040_0000_0000_0000, 1));
            Decimal128 inf = Decimal128.PositiveInfinity;

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

            static UInt128 Bits(Decimal128 value) => Unsafe.BitCast<Decimal128, UInt128>(value);
        }

        public static IEnumerable<object[]> UnaryNegation_TestData()
        {
            yield return new object[] { new UInt128(0x3040_0000_0000_0000, 0), new UInt128(0xB040_0000_0000_0000, 0) }; // +0 -> -0
            yield return new object[] { new UInt128(0xB040_0000_0000_0000, 0), new UInt128(0x3040_0000_0000_0000, 0) }; // -0 -> +0
            yield return new object[] { new UInt128(0x7800_0000_0000_0000, 0), new UInt128(0xF800_0000_0000_0000, 0) }; // +Infinity -> -Infinity
            yield return new object[] { new UInt128(0xF800_0000_0000_0000, 0), new UInt128(0x7800_0000_0000_0000, 0) }; // -Infinity -> +Infinity
            yield return new object[] { new UInt128(0xFC00_0000_0000_0000, 0), new UInt128(0x7C00_0000_0000_0000, 0) }; // NaN -> sign-flipped NaN
            yield return new object[] { new UInt128(0x7C00_0000_0000_0000, 0), new UInt128(0xFC00_0000_0000_0000, 0) };
        }

        [Theory]
        [MemberData(nameof(UnaryNegation_TestData))]
        public static void op_UnaryNegation(UInt128 value, UInt128 expected)
        {
            Decimal128 result = -Unsafe.BitCast<UInt128, Decimal128>(value);
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData("123")]
        [InlineData("-123")]
        [InlineData("4567.891")]
        [InlineData("-4567.891")]
        [InlineData("9.999999999999999999999999999999999e6144")]
        [InlineData("1e-6176")]
        public static void op_UnaryNegation_FiniteRoundTrips(string value)
        {
            Decimal128 d = Decimal128.Parse(value, CultureInfo.InvariantCulture);
            Decimal128 negated = -d;

            UInt128 dBits = Unsafe.BitCast<Decimal128, UInt128>(d);
            UInt128 negatedBits = Unsafe.BitCast<Decimal128, UInt128>(negated);

            Assert.Equal(dBits ^ new UInt128(0x8000_0000_0000_0000, 0), negatedBits); // only the sign bit differs
            Assert.Equal(dBits, Unsafe.BitCast<Decimal128, UInt128>(-negated)); // double negation is identity
        }

        [Theory]
        [MemberData(nameof(UnaryNegation_TestData))]
        public static void op_UnaryPlus(UInt128 value, UInt128 _)
        {
            Decimal128 d = Unsafe.BitCast<UInt128, Decimal128>(value);
            Assert.Equal(value, Unsafe.BitCast<Decimal128, UInt128>(+d));
        }
        public static IEnumerable<object[]> op_Addition_TestData()
        {
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // NaN + 1 -> NaN
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // 1 + NaN -> NaN
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000) }; // +Inf + 1 -> +Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // 1 + +Inf -> +Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // +Inf + -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // -Inf + +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // +Inf + +Inf -> +Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) }; // -Inf + -Inf -> -Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) }; // +0 + +0 -> +0
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) }; // -0 + -0 -> -0
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) }; // +0 + -0 -> +0 (round-half-even)
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) }; // -0 + +0 -> +0
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001) }; // 1 + 0 -> 1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000001) }; // 0 + 1 -> 1
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000) }; // non-canonical +Inf + 1 -> canonical +Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xF800000000000000, 0x0000000000000005), new UInt128(0xF800000000000000, 0x0000000000000000) }; // 1 + non-canonical -Inf -> canonical -Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x000000000000000F), new UInt128(0xF800000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // non-canonical +Inf + non-canonical -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000003) }; // 1 + 2 -> 3
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000F), new UInt128(0x303E000000000000, 0x0000000000000019), new UInt128(0x303E000000000000, 0x0000000000000028) }; // 1.5 + 2.5 -> 4.0
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), new UInt128(0x303E000000000000, 0x0000000000000002), new UInt128(0x303E000000000000, 0x0000000000000003) }; // 0.1 + 0.2 -> 0.3
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000000) }; // 1 + -1 -> +0
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000000) }; // -1 + 1 -> +0
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3042314DC6448D93, 0x38C15B0A00000000) }; // all-nines + 1 (carry/overflow to next magnitude)
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3042629B8C891B26, 0x7182B61400000000) }; // big + big (round)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x2FFC000000000000, 0x0000000000000001), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) }; // 1 + 1e-P (alignment beyond precision)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x2FF2000000000000, 0x0000000000000001), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) }; // 1 + tiny (sticky rounding)
            yield return new object[] { new UInt128(0x5FFE314DC6448D93, 0x38C15B0A00000000), new UInt128(0x5FFE314DC6448D93, 0x38C15B0A00000000), new UInt128(0x5FFE629B8C891B26, 0x7182B61400000000) }; // max-ish + max-ish (overflow to Inf)
            yield return new object[] { new UInt128(0x3034000000000000, 0x000000000012D687), new UInt128(0x3034000000000000, 0x000000000074CBB1), new UInt128(0x3034000000000000, 0x000000000087A238) }; // cohort / preferred exponent
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000064), new UInt128(0x303A000000000000, 0x0000000000000001), new UInt128(0x303A000000000000, 0x00000000000186A1) }; // 100 + 0.001 (exponent spread)
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0xB041ED09BEAD87C0, 0x378D8E63FFFFFFFE), new UInt128(0x3040000000000000, 0x0000000000000001) }; // cancellation leaving small
            yield return new object[] { new UInt128(0x300C000000000000, 0x0386DF690C6EF5BE), new UInt128(0xB018000000000000, 0x00022FCFF31AA0B9), new UInt128(0xB00C000000000021, 0x5A87EFD0027FEA82) };
            yield return new object[] { new UInt128(0x3016000000000000, 0x00000000025C222D), new UInt128(0xAFEE0000105989B6, 0x14E871DF7FB4E7FF), new UInt128(0xAFEE0000038E860F, 0x754522FCF5E4E7FF) };
            yield return new object[] { new UInt128(0xAFE4004BC3F60664, 0xA2D28316972FB16C), new UInt128(0x3024000000000000, 0x336683344D857468), new UInt128(0x3006B69C6D6AD20B, 0x43BBF7251F590CE0) };
            yield return new object[] { new UInt128(0xB026000000000000, 0x0000000000000009), new UInt128(0xD36C269369438B15, 0x4317B7B7F941F96B), new UInt128(0xD36B81C21CA36ED4, 0x9EED2D2FBC93BE2E) };
            yield return new object[] { new UInt128(0xB030000000000000, 0x00000006C1105459), new UInt128(0xB01E100AA3FBDB37, 0x590C5A8BE72C6FA0), new UInt128(0xB01E100AA3FBDB38, 0xEBA096260A44A9A0) };
            yield return new object[] { new UInt128(0x4592000000000000, 0x0125EB92A4B6C61E), new UInt128(0xB03E000000000000, 0x00000001A8B80287), new UInt128(0x457197E56F0AC19C, 0x7790248FB22C0000) };
            yield return new object[] { new UInt128(0xB018000000000000, 0x000016E8FC21185B), new UInt128(0xB052000000000000, 0x00000000EFF4031C), new UInt128(0xB022C67C0F8B01FB, 0xA2D1A6E0AB03AD2F) };
            yield return new object[] { new UInt128(0x3020000000000000, 0x0000009AA0434574), new UInt128(0x3048000000000000, 0x0000000000001ED7), new UInt128(0x302000000000A72E, 0xE1796A54B2B34574) };
            yield return new object[] { new UInt128(0xB058000000000000, 0x00000000000003DF), new UInt128(0x300C000000000000, 0x000179DCB4BE3FF7), new UInt128(0xB01BE899C8FE6687, 0x7271EA097D860D73) };
            yield return new object[] { new UInt128(0x9EE405C739C662F4, 0x5752242E1DB1EC20), new UInt128(0x3004000000000000, 0x0000BD66F041ECD5), new UInt128(0x2FDE66ACD270BB66, 0x6BD8D5D09E080000) };
            yield return new object[] { new UInt128(0xAFE6000119C10D0F, 0xC98968772E7E6EF7), new UInt128(0xB0020000000002DA, 0x7B7ACAB57EE654B5), new UInt128(0xAFEC426FDE55CADB, 0xA69EFD5C6E732E83) };
            yield return new object[] { new UInt128(0xB00E000000000000, 0x00000000004E6990), new UInt128(0x3852000000000000, 0x000000000000031D), new UInt128(0x381588F38AEA0C31, 0x8456F6DC80000000) };
            yield return new object[] { new UInt128(0x3008000000000000, 0x000000014204609D), new UInt128(0xB020000000000000, 0x000000000000CCF5), new UInt128(0xB008000000000000, 0x00BA6845C8B3EF63) };
            yield return new object[] { new UInt128(0x8462000000000000, 0x00000000EAC878D0), new UInt128(0xB034000000000000, 0x0000000000000110), new UInt128(0xAFF6861B3A0224EC, 0x9A5FD8E800000000) };
            yield return new object[] { new UInt128(0xB02400012DE50EC6, 0xBBBA78C47B74A0E0), new UInt128(0x302200000000012F, 0x1F7FF8F741AE5567), new UInt128(0xB022000BCAF29294, 0x35C8BEB590DFF359) };
            yield return new object[] { new UInt128(0x3038000000000000, 0x0000000000000057), new UInt128(0xB03C000000000000, 0x000000110FC8CD30), new UInt128(0xB038000000000000, 0x000006AA2A702669) };
            yield return new object[] { new UInt128(0x3030000000000000, 0x17E95893CB288412), new UInt128(0xC6E2000000000000, 0x52ABCE0A9DE14E35), new UInt128(0xC6C525B4F0614608, 0xC7F6706318188000) };
            yield return new object[] { new UInt128(0x0870000000000000, 0x000000DD250AAC57), new UInt128(0xB00000000000092C, 0x7754F6DC4C1D7D08), new UInt128(0xAFEAD5977D016087, 0x955141749AFF4000) };
            yield return new object[] { new UInt128(0xB036000000000000, 0x000000130041C7D7), new UInt128(0xB016000000000000, 0x002262E6BEFE8EBF), new UInt128(0xB016000002A30D1F, 0x4AC08D62A1158EBF) };
            yield return new object[] { new UInt128(0x3036000000000000, 0x00000001B6848ADB), new UInt128(0x301E000000000000, 0x00000000000A8089), new UInt128(0x301E00000000018E, 0xD45E584B23DF3089) };
            yield return new object[] { new UInt128(0xC3AE000000000000, 0x04CF4E8A302A8BDC), new UInt128(0xAFFA00000000002E, 0x3420D6865B6A6E4E), new UInt128(0xC38EAAE0CE12AD01, 0x0D9220AFD4DC0000) };
            yield return new object[] { new UInt128(0xD258000000000000, 0x000000000091C9EF), new UInt128(0x300A000000000000, 0x0000167364209915), new UInt128(0xD223D711ABE4492C, 0x8AD6F30498000000) };
            yield return new object[] { new UInt128(0x301C000000000000, 0x000000000439E2E9), new UInt128(0xB014000000000032, 0x708772352DE98CF2), new UInt128(0xB014000000000032, 0x7087719018B9DF62) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000064), new UInt128(0xB03A000000000000, 0x00003B2E17E3E88B), new UInt128(0xB03A000000000000, 0x00003B2E17E56F2B) };
            yield return new object[] { new UInt128(0xB044000000000000, 0x00000000023F5ABE), new UInt128(0xB03A000000006CA7, 0xBAF42939DF4E7BA0), new UInt128(0xB03A000000006CA7, 0xBAF42CA7CB24A660) };
            yield return new object[] { new UInt128(0xC6E6000000000000, 0x00000001B8D3E89E), new UInt128(0x301C000000000007, 0x0964D911A0BBD8F4), new UInt128(0xC6B76CA4E932F6AB, 0xE7ED87915E000000) };
            yield return new object[] { new UInt128(0x305E000000000000, 0x000000000000A71E), new UInt128(0x3026000000000000, 0x000000795A3C873F), new UInt128(0x30261517D8F9A995, 0x6CE399493A3C873F) };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x0000000005C867C0), new UInt128(0x302E000000000000, 0x0000000000572AE6), new UInt128(0x301A000000000000, 0x00CAF3EBEC4E7040) };
            yield return new object[] { new UInt128(0xB650000000D0224F, 0xFD6F80D1270DDE07), new UInt128(0xB010000000000000, 0x0000021670F50113), new UInt128(0xB6427C0EBBAA80B2, 0x896A25B17B2F1D80) };
            yield return new object[] { new UInt128(0xB02C000000000006, 0x909F1381718DE51E), new UInt128(0xB01E00000000002D, 0xCE88CD810A4F6601), new UInt128(0xB01E000003E9BAA6, 0xE85BD302FF518901) };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x00000000000018F2), new UInt128(0x303C000000000000, 0x000000000046CBDD), new UInt128(0x301A00000000623F, 0xE9B2FED12E21E70E) };
            yield return new object[] { new UInt128(0x5F400000000005E8, 0x2C8923BCBDB9FD3E), new UInt128(0xB034000000000000, 0x00000000000C3C2B), new UInt128(0x5F2A89880B37B794, 0xE2748F76B8143000) };
            yield return new object[] { new UInt128(0xD95C0000000051B5, 0x62B3BD63621B218F), new UInt128(0xB01E000000000000, 0x0000000000000000), new UInt128(0xD948BE3E155B3E8C, 0x9858E4AB87085C00) };
            yield return new object[] { new UInt128(0xB0280000000006F8, 0x435BE30981A1CB95), new UInt128(0x54FE000000000000, 0x00015E8772D1DBC6), new UInt128(0x54D8BE05AF221D3F, 0x366FDDE421700000) };
            yield return new object[] { new UInt128(0x5C72000037D35F7D, 0xF4E8427727658DEF), new UInt128(0x3042000000000000, 0x0001692E610A8ED8), new UInt128(0x5C68552EE79591D2, 0xE66B107D55B2CF60) };
            yield return new object[] { new UInt128(0x305C000000000000, 0x000000000000FE36), new UInt128(0x3014000000000000, 0x000000D1D108B375), new UInt128(0x302340DBFBE65F62, 0x49AC0F7DC0016004) };
            yield return new object[] { new UInt128(0xB042000000000000, 0x000000000000A932), new UInt128(0x303E000000000000, 0x000002503A034940), new UInt128(0x303E000000000000, 0x0000025039C131B8) };
            yield return new object[] { new UInt128(0xB00E000000000000, 0x587FADD919E02966), new UInt128(0x3014000000000000, 0x0721E25C46337978), new UInt128(0x300E00000000001B, 0x83DC8A991F32535A) };
            yield return new object[] { new UInt128(0xAFFA4821D7034A02, 0xB8ACC25DF6F4CF86), new UInt128(0xB044000000000000, 0x00E1766220EE98D3), new UInt128(0xB02338E458FDFD49, 0x227DFD5961604959) };
            yield return new object[] { new UInt128(0x3024000000000000, 0x000000000007C72E), new UInt128(0x1758000000000000, 0x000000041F72D9AA), new UInt128(0x2FECFB527C560A41, 0x002750E0E0000000) };
            yield return new object[] { new UInt128(0x301A0000000061C8, 0xEEFC10C4D3DADBE3), new UInt128(0xB0100003A1125F48, 0x953E3B9231F49265), new UInt128(0xB01000030BDD259F, 0x3627E5CE5E2F5285) };
            yield return new object[] { new UInt128(0x301000004BB9B532, 0xE5CB89F41922BD32), new UInt128(0xDE64000000000000, 0x0000CCBC550D3404), new UInt128(0xDE3E6EFCC8409C2A, 0x0D9E80DD47A00000) };
            yield return new object[] { new UInt128(0xB024000000000000, 0x000000000116C396), new UInt128(0x3028000000005351, 0x729900C58D0BD408), new UInt128(0x3024000000208BD0, 0xC3C44D2B17880F8A) };
            yield return new object[] { new UInt128(0x300200000220753C, 0x8CA178C667F5ED50), new UInt128(0xD382000000000000, 0x0A69C7380DBC5B46), new UInt128(0xD36371F3777B420E, 0x8D9DF2C029C60000) };
            yield return new object[] { new UInt128(0x300C045E788BFD23, 0xFDC576761B96F039), new UInt128(0xB01A000000000000, 0x0AE0ABB2D104776E), new UInt128(0x300C045E78858158, 0xF6A8BBEE1210C539) };
            yield return new object[] { new UInt128(0xCD32000000000000, 0x00000062ADC4C04D), new UInt128(0xB040000000000000, 0xD235D4832BF52B34), new UInt128(0xCD06D0F5E02D285C, 0x39CE6BD79D400000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x00017F274F195D94), new UInt128(0x2FFA000000000000, 0x022F53F33F61565B), new UInt128(0xB01ACFB53C9B53FE, 0x7F30F461021FFFF0) };
            yield return new object[] { new UInt128(0xB044000000000000, 0x0000000000000046), new UInt128(0x300A00000005E4A6, 0x3DBCC6C875ABB5B6), new UInt128(0xB00A00585A32650B, 0xAF75BA9F4A544A4A) };
            yield return new object[] { new UInt128(0x3066000000000000, 0x0000000000000015), new UInt128(0x3014000000008E7B, 0xA150E858F5A1264C), new UInt128(0x30266789B9F65C81, 0xF732098AA3786387) };
            yield return new object[] { new UInt128(0x21A60000000048E8, 0xDC7FE586D3707752), new UInt128(0xB024000000000023, 0x2E0D31C20845EFD5), new UInt128(0xB00B3FF58FC5004C, 0xC2033C5B68BF2000) };
            yield return new object[] { new UInt128(0xAFF403A1D73F5ACD, 0x96A762C7BAA70840), new UInt128(0x3042000000000000, 0x000000019FF4DE39), new UInt128(0x3013581237E839A6, 0x10386C6F634043B0) };
            yield return new object[] { new UInt128(0x303C000000000000, 0x00113925FCD09CDA), new UInt128(0x3000000000007140, 0x43CE4908B69E66F9), new UInt128(0x3018EF0539CB08CE, 0x80E380BA007D13FC) };
            yield return new object[] { new UInt128(0x3004000000000000, 0x00003C65D7E4774B), new UInt128(0x301825B99EFDAE69, 0x303766A98D4C34B5), new UInt128(0x3017794035E8D01B, 0xE22A029F84FB127A) };
            yield return new object[] { new UInt128(0xB0220000004AE762, 0xF65E345A6F0E69F9), new UInt128(0x301A000000000000, 0x00000000000DC8E9), new UInt128(0xB01A000B6DEE89B7, 0xBFDD0C9222FDC5A7) };
            yield return new object[] { new UInt128(0x3052000000000000, 0x000000000000002A), new UInt128(0x300000001C3D6C1F, 0x26F3E82E5A4692F0), new UInt128(0x3012CF1373ECB904, 0x67A96D19B1D3BCB5) };
            yield return new object[] { new UInt128(0xB048000000000000, 0x0000000000000280), new UInt128(0xAFFC000000000000, 0x0AE997D86F01F581), new UInt128(0xB00B3B8B5B5056E1, 0x6B3BE0524EDF1EC2) };
            yield return new object[] { new UInt128(0xB018000000120CBF, 0x23F4CD9940BD2B9A), new UInt128(0xDFCC005830554042, 0x149E930F0D337664), new UInt128(0xDFC7587CCD030220, 0x8B6E72CB910676A0) };
            yield return new object[] { new UInt128(0x300E000000000000, 0x0112E59C50FC31A5), new UInt128(0x2FEC16CD5779F833, 0xFD5BB8EB3F0C6FD3), new UInt128(0x2FED944C58617AA0, 0x5472BD3EF2FE6FD3) };
            yield return new object[] { new UInt128(0x3D3E000000000000, 0x00000000E4F7F4D6), new UInt128(0xD270000000000000, 0x00000815ABADD29E), new UInt128(0xD247B64511B61A22, 0x8091E2DEA6C00000) };
            yield return new object[] { new UInt128(0x3034000000000000, 0x00181648272743F6), new UInt128(0x569600000002FBFF, 0x4C2693B7C7D3A38C), new UInt128(0x5684B1E1C8F82556, 0xA82AFFAEA5447800) };
        }

        [Theory]
        [MemberData(nameof(op_Addition_TestData))]
        public static void op_Addition(UInt128 left, UInt128 right, UInt128 expected)
        {
            Decimal128 result = Unsafe.BitCast<UInt128, Decimal128>(left) + Unsafe.BitCast<UInt128, Decimal128>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        public static IEnumerable<object[]> op_Subtraction_TestData()
        {
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // NaN - 1 -> NaN
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // 1 - NaN -> NaN
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000) }; // +Inf - 1 -> +Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) }; // 1 - +Inf -> -Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // +Inf - -Inf -> +Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) }; // -Inf - +Inf -> -Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // +Inf - +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // -Inf - -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) }; // +0 - +0 -> +0
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) }; // -0 - -0 -> +0 (round-half-even)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) }; // +0 - -0 -> +0
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) }; // -0 - +0 -> -0
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001) }; // 1 - 0 -> 1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xB040000000000000, 0x0000000000000001) }; // 0 - 1 -> -1
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000) }; // non-canonical +Inf - 1 -> canonical +Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xF800000000000000, 0x0000000000000005), new UInt128(0x7800000000000000, 0x0000000000000000) }; // 1 - non-canonical -Inf -> canonical +Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x000000000000000F), new UInt128(0xF800000000000000, 0x0000000000000003), new UInt128(0x7800000000000000, 0x0000000000000000) }; // non-canonical +Inf - non-canonical -Inf -> canonical +Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0xB040000000000000, 0x0000000000000001) }; // 1 - 2 -> -1
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000F), new UInt128(0x303E000000000000, 0x0000000000000019), new UInt128(0xB03E000000000000, 0x000000000000000A) }; // 1.5 - 2.5 -> -1.0
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), new UInt128(0x303E000000000000, 0x0000000000000002), new UInt128(0xB03E000000000000, 0x0000000000000001) }; // 0.1 - 0.2 -> -0.1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000002) }; // 1 - -1 -> 2
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xB040000000000000, 0x0000000000000002) }; // -1 - 1 -> -2
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFE) }; // all-nines - 1
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3040000000000000, 0x0000000000000000) }; // big - big -> +0
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x2FFC000000000000, 0x0000000000000001), new UInt128(0x2FFDED09BEAD87C0, 0x378D8E63FFFFFFFF) }; // 1 - 1e-P -> 0.999... (alignment beyond precision)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x2FF2000000000000, 0x0000000000000001), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) }; // 1 - tiny -> 1.000... (sticky rounding)
            yield return new object[] { new UInt128(0x5FFE314DC6448D93, 0x38C15B0A00000000), new UInt128(0x5FFE314DC6448D93, 0x38C15B0A00000000), new UInt128(0x5FFE000000000000, 0x0000000000000000) }; // max-ish - max-ish -> +0 (preferred exponent retained)
            yield return new object[] { new UInt128(0x3034000000000000, 0x000000000012D687), new UInt128(0x3034000000000000, 0x000000000074CBB1), new UInt128(0xB034000000000000, 0x000000000061F52A) }; // cohort / preferred exponent
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000064), new UInt128(0x303A000000000000, 0x0000000000000001), new UInt128(0x303A000000000000, 0x000000000001869F) }; // 100 - 0.001 -> 99.999 (exponent spread)
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0xB041ED09BEAD87C0, 0x378D8E63FFFFFFFE), new UInt128(0x3042629B8C891B26, 0x7182B61400000000) }; // opposite signs subtract as add (magnitudes add, carry/rounding)
            yield return new object[] { new UInt128(0x300C000000000000, 0x0386DF690C6EF5BE), new UInt128(0xB018000000000000, 0x00022FCFF31AA0B9), new UInt128(0x300C000000000021, 0x6195AEA21B5DD5FE) };
            yield return new object[] { new UInt128(0x3016000000000000, 0x00000000025C222D), new UInt128(0xAFEE0000105989B6, 0x14E871DF7FB4E7FF), new UInt128(0x2FEE00001D248D5C, 0xB48BC0C20984E7FF) };
            yield return new object[] { new UInt128(0xAFE4004BC3F60664, 0xA2D28316972FB16C), new UInt128(0x3024000000000000, 0x336683344D857468), new UInt128(0xB006B69C6D6AD20B, 0x43BC6455A5EEF320) };
            yield return new object[] { new UInt128(0xB026000000000000, 0x0000000000000009), new UInt128(0xD36C269369438B15, 0x4317B7B7F941F96B), new UInt128(0x536B81C21CA36ED4, 0x9EED2D2FBC93BE2E) };
            yield return new object[] { new UInt128(0xB030000000000000, 0x00000006C1105459), new UInt128(0xB01E100AA3FBDB37, 0x590C5A8BE72C6FA0), new UInt128(0x301E100AA3FBDB35, 0xC6781EF1C41435A0) };
            yield return new object[] { new UInt128(0x4592000000000000, 0x0125EB92A4B6C61E), new UInt128(0xB03E000000000000, 0x00000001A8B80287), new UInt128(0x457197E56F0AC19C, 0x7790248FB22C0000) };
            yield return new object[] { new UInt128(0xB018000000000000, 0x000016E8FC21185B), new UInt128(0xB052000000000000, 0x00000000EFF4031C), new UInt128(0x3022C67C0F8B01FB, 0xA2D1A6E08CFC52D1) };
            yield return new object[] { new UInt128(0x3020000000000000, 0x0000009AA0434574), new UInt128(0x3048000000000000, 0x0000000000001ED7), new UInt128(0xB02000000000A72E, 0xE179691F722CBA8C) };
            yield return new object[] { new UInt128(0xB058000000000000, 0x00000000000003DF), new UInt128(0x300C000000000000, 0x000179DCB4BE3FF7), new UInt128(0xB01BE899C8FE6687, 0x7271EA098279F28D) };
            yield return new object[] { new UInt128(0x9EE405C739C662F4, 0x5752242E1DB1EC20), new UInt128(0x3004000000000000, 0x0000BD66F041ECD5), new UInt128(0xAFDE66ACD270BB66, 0x6BD8D5D09E080000) };
            yield return new object[] { new UInt128(0xAFE6000119C10D0F, 0xC98968772E7E6EF7), new UInt128(0xB0020000000002DA, 0x7B7ACAB57EE654B5), new UInt128(0x2FEC426FDDC588C8, 0xACDFBD26F1F0E17D) };
            yield return new object[] { new UInt128(0xB00E000000000000, 0x00000000004E6990), new UInt128(0x3852000000000000, 0x000000000000031D), new UInt128(0xB81588F38AEA0C31, 0x8456F6DC80000000) };
            yield return new object[] { new UInt128(0x3008000000000000, 0x000000014204609D), new UInt128(0xB020000000000000, 0x000000000000CCF5), new UInt128(0x3008000000000000, 0x00BA68484CBCB09D) };
            yield return new object[] { new UInt128(0x8462000000000000, 0x00000000EAC878D0), new UInt128(0xB034000000000000, 0x0000000000000110), new UInt128(0x2FF6861B3A0224EC, 0x9A5FD8E800000000) };
            yield return new object[] { new UInt128(0xB02400012DE50EC6, 0xBBBA78C47B74A0E0), new UInt128(0x302200000000012F, 0x1F7FF8F741AE5567), new UInt128(0xB022000BCAF294F2, 0x74C8B0A4143C9E27) };
            yield return new object[] { new UInt128(0x3038000000000000, 0x0000000000000057), new UInt128(0xB03C000000000000, 0x000000110FC8CD30), new UInt128(0x3038000000000000, 0x000006AA2A702717) };
            yield return new object[] { new UInt128(0x3030000000000000, 0x17E95893CB288412), new UInt128(0xC6E2000000000000, 0x52ABCE0A9DE14E35), new UInt128(0x46C525B4F0614608, 0xC7F6706318188000) };
            yield return new object[] { new UInt128(0x0870000000000000, 0x000000DD250AAC57), new UInt128(0xB00000000000092C, 0x7754F6DC4C1D7D08), new UInt128(0x2FEAD5977D016087, 0x955141749AFF4000) };
            yield return new object[] { new UInt128(0xB036000000000000, 0x000000130041C7D7), new UInt128(0xB016000000000000, 0x002262E6BEFE8EBF), new UInt128(0xB016000002A30D1F, 0x4A7BC79523187141) };
            yield return new object[] { new UInt128(0x3036000000000000, 0x00000001B6848ADB), new UInt128(0x301E000000000000, 0x00000000000A8089), new UInt128(0x301E00000000018E, 0xD45E584B23CA2F77) };
            yield return new object[] { new UInt128(0xC3AE000000000000, 0x04CF4E8A302A8BDC), new UInt128(0xAFFA00000000002E, 0x3420D6865B6A6E4E), new UInt128(0xC38EAAE0CE12AD01, 0x0D9220AFD4DC0000) };
            yield return new object[] { new UInt128(0xD258000000000000, 0x000000000091C9EF), new UInt128(0x300A000000000000, 0x0000167364209915), new UInt128(0xD223D711ABE4492C, 0x8AD6F30498000000) };
            yield return new object[] { new UInt128(0x301C000000000000, 0x000000000439E2E9), new UInt128(0xB014000000000032, 0x708772352DE98CF2), new UInt128(0x3014000000000032, 0x708772DA43193A82) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000064), new UInt128(0xB03A000000000000, 0x00003B2E17E3E88B), new UInt128(0x303A000000000000, 0x00003B2E17E261EB) };
            yield return new object[] { new UInt128(0xB044000000000000, 0x00000000023F5ABE), new UInt128(0xB03A000000006CA7, 0xBAF42939DF4E7BA0), new UInt128(0x303A000000006CA7, 0xBAF425CBF37850E0) };
            yield return new object[] { new UInt128(0xC6E6000000000000, 0x00000001B8D3E89E), new UInt128(0x301C000000000007, 0x0964D911A0BBD8F4), new UInt128(0xC6B76CA4E932F6AB, 0xE7ED87915E000000) };
            yield return new object[] { new UInt128(0x305E000000000000, 0x000000000000A71E), new UInt128(0x3026000000000000, 0x000000795A3C873F), new UInt128(0x30261517D8F9A995, 0x6CE3985685C378C1) };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x0000000005C867C0), new UInt128(0x302E000000000000, 0x0000000000572AE6), new UInt128(0xB01A000000000000, 0x00CAF3EBF7DF3FC0) };
            yield return new object[] { new UInt128(0xB650000000D0224F, 0xFD6F80D1270DDE07), new UInt128(0xB010000000000000, 0x0000021670F50113), new UInt128(0xB6427C0EBBAA80B2, 0x896A25B17B2F1D80) };
            yield return new object[] { new UInt128(0xB02C000000000006, 0x909F1381718DE51E), new UInt128(0xB01E00000000002D, 0xCE88CD810A4F6601), new UInt128(0xB01E000003E9BA4B, 0x4B4A3800EAB2BCFF) };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x00000000000018F2), new UInt128(0x303C000000000000, 0x000000000046CBDD), new UInt128(0xB01A00000000623F, 0xE9B2FED12E2218F2) };
            yield return new object[] { new UInt128(0x5F400000000005E8, 0x2C8923BCBDB9FD3E), new UInt128(0xB034000000000000, 0x00000000000C3C2B), new UInt128(0x5F2A89880B37B794, 0xE2748F76B8143000) };
            yield return new object[] { new UInt128(0xD95C0000000051B5, 0x62B3BD63621B218F), new UInt128(0xB01E000000000000, 0x0000000000000000), new UInt128(0xD948BE3E155B3E8C, 0x9858E4AB87085C00) };
            yield return new object[] { new UInt128(0xB0280000000006F8, 0x435BE30981A1CB95), new UInt128(0x54FE000000000000, 0x00015E8772D1DBC6), new UInt128(0xD4D8BE05AF221D3F, 0x366FDDE421700000) };
            yield return new object[] { new UInt128(0x5C72000037D35F7D, 0xF4E8427727658DEF), new UInt128(0x3042000000000000, 0x0001692E610A8ED8), new UInt128(0x5C68552EE79591D2, 0xE66B107D55B2CF60) };
            yield return new object[] { new UInt128(0x305C000000000000, 0x000000000000FE36), new UInt128(0x3014000000000000, 0x000000D1D108B375), new UInt128(0x302340DBFBE65F62, 0x49AC0F7DBFFE9FFC) };
            yield return new object[] { new UInt128(0xB042000000000000, 0x000000000000A932), new UInt128(0x303E000000000000, 0x000002503A034940), new UInt128(0xB03E000000000000, 0x000002503A4560C8) };
            yield return new object[] { new UInt128(0xB00E000000000000, 0x587FADD919E02966), new UInt128(0x3014000000000000, 0x0721E25C46337978), new UInt128(0xB00E00000000001C, 0x34DBE64B52F2A626) };
            yield return new object[] { new UInt128(0xAFFA4821D7034A02, 0xB8ACC25DF6F4CF86), new UInt128(0xB044000000000000, 0x00E1766220EE98D3), new UInt128(0x302338E458FDFD49, 0x227DE2BCB01BB6A7) };
            yield return new object[] { new UInt128(0x3024000000000000, 0x000000000007C72E), new UInt128(0x1758000000000000, 0x000000041F72D9AA), new UInt128(0x2FECFB527C560A41, 0x002750E0E0000000) };
            yield return new object[] { new UInt128(0x301A0000000061C8, 0xEEFC10C4D3DADBE3), new UInt128(0xB0100003A1125F48, 0x953E3B9231F49265), new UInt128(0x30100004364798F1, 0xF454915605B9D245) };
            yield return new object[] { new UInt128(0x301000004BB9B532, 0xE5CB89F41922BD32), new UInt128(0xDE64000000000000, 0x0000CCBC550D3404), new UInt128(0x5E3E6EFCC8409C2A, 0x0D9E80DD47A00000) };
            yield return new object[] { new UInt128(0xB024000000000000, 0x000000000116C396), new UInt128(0x3028000000005351, 0x729900C58D0BD408), new UInt128(0xB024000000208BD0, 0xC3C44D2B19B596B6) };
            yield return new object[] { new UInt128(0x300200000220753C, 0x8CA178C667F5ED50), new UInt128(0xD382000000000000, 0x0A69C7380DBC5B46), new UInt128(0x536371F3777B420E, 0x8D9DF2C029C60000) };
            yield return new object[] { new UInt128(0x300C045E788BFD23, 0xFDC576761B96F039), new UInt128(0xB01A000000000000, 0x0AE0ABB2D104776E), new UInt128(0x300C045E789278EF, 0x04E230FE251D1B39) };
            yield return new object[] { new UInt128(0xCD32000000000000, 0x00000062ADC4C04D), new UInt128(0xB040000000000000, 0xD235D4832BF52B34), new UInt128(0xCD06D0F5E02D285C, 0x39CE6BD79D400000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x00017F274F195D94), new UInt128(0x2FFA000000000000, 0x022F53F33F61565B), new UInt128(0xB01ACFB53C9B53FE, 0x7F30F46102200010) };
            yield return new object[] { new UInt128(0xB044000000000000, 0x0000000000000046), new UInt128(0x300A00000005E4A6, 0x3DBCC6C875ABB5B6), new UInt128(0xB00A00585A3E2E58, 0x2AEF483035ABB5B6) };
            yield return new object[] { new UInt128(0x3066000000000000, 0x0000000000000015), new UInt128(0x3014000000008E7B, 0xA150E858F5A1264C), new UInt128(0x30266789B9F65C81, 0xF72D419F5C879C79) };
            yield return new object[] { new UInt128(0x21A60000000048E8, 0xDC7FE586D3707752), new UInt128(0xB024000000000023, 0x2E0D31C20845EFD5), new UInt128(0x300B3FF58FC5004C, 0xC2033C5B68BF2000) };
            yield return new object[] { new UInt128(0xAFF403A1D73F5ACD, 0x96A762C7BAA70840), new UInt128(0x3042000000000000, 0x000000019FF4DE39), new UInt128(0xB013581237E839A6, 0x1243E09E4EBFBC50) };
            yield return new object[] { new UInt128(0x303C000000000000, 0x00113925FCD09CDA), new UInt128(0x3000000000007140, 0x43CE4908B69E66F9), new UInt128(0x3018EF0539CB08CE, 0x80E37FC0F5D2EC04) };
            yield return new object[] { new UInt128(0x3004000000000000, 0x00003C65D7E4774B), new UInt128(0x301825B99EFDAE69, 0x303766A98D4C34B5), new UInt128(0xB017794035E8D01B, 0xE22A029F84F90BAA) };
            yield return new object[] { new UInt128(0xB0220000004AE762, 0xF65E345A6F0E69F9), new UInt128(0x301A000000000000, 0x00000000000DC8E9), new UInt128(0xB01A000B6DEE89B7, 0xBFDD0C9223195779) };
            yield return new object[] { new UInt128(0x3052000000000000, 0x000000000000002A), new UInt128(0x300000001C3D6C1F, 0x26F3E82E5A4692F0), new UInt128(0x3012CF1373ECB903, 0x7515293A4E2C434B) };
            yield return new object[] { new UInt128(0xB048000000000000, 0x0000000000000280), new UInt128(0xAFFC000000000000, 0x0AE997D86F01F581), new UInt128(0xB00B3B8B5B5056E1, 0x6B3BE02DB120E13E) };
            yield return new object[] { new UInt128(0xB018000000120CBF, 0x23F4CD9940BD2B9A), new UInt128(0xDFCC005830554042, 0x149E930F0D337664), new UInt128(0x5FC7587CCD030220, 0x8B6E72CB910676A0) };
            yield return new object[] { new UInt128(0x300E000000000000, 0x0112E59C50FC31A5), new UInt128(0x2FEC16CD5779F833, 0xFD5BB8EB3F0C6FD3), new UInt128(0x2FED66B1A96D8A38, 0x59BB4B6874E5902D) };
            yield return new object[] { new UInt128(0x3D3E000000000000, 0x00000000E4F7F4D6), new UInt128(0xD270000000000000, 0x00000815ABADD29E), new UInt128(0x5247B64511B61A22, 0x8091E2DEA6C00000) };
            yield return new object[] { new UInt128(0x3034000000000000, 0x00181648272743F6), new UInt128(0x569600000002FBFF, 0x4C2693B7C7D3A38C), new UInt128(0xD684B1E1C8F82556, 0xA82AFFAEA5447800) };
        }

        [Theory]
        [MemberData(nameof(op_Subtraction_TestData))]
        public static void op_Subtraction(UInt128 left, UInt128 right, UInt128 expected)
        {
            Decimal128 result = Unsafe.BitCast<UInt128, Decimal128>(left) - Unsafe.BitCast<UInt128, Decimal128>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        public static IEnumerable<object[]> op_Equality_TestData()
        {
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000), false }; // NaN == NaN -> false
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), false }; // NaN == 1 -> false
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xFC00000000000000, 0x0000000000000000), false }; // 1 == NaN -> false
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), false }; // NaN == +Inf -> false
            yield return new object[] { new UInt128(0xFE00000000000000, 0x0000000000000000), new UInt128(0xFE00000000000000, 0x0000000000000000), false }; // sNaN == sNaN -> false
            yield return new object[] { new UInt128(0xFE00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), false }; // sNaN == 1 -> false
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), true }; // +Inf == +Inf -> true
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), true }; // -Inf == -Inf -> true
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), false }; // +Inf == -Inf -> false
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), false }; // +Inf == 1 -> false
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000002), new UInt128(0x7800000000000000, 0x0000000000000000), true }; // non-canonical +Inf == +Inf -> true
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000005), new UInt128(0xF800000000000000, 0x0000000000000000), true }; // non-canonical -Inf == -Inf -> true
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), true }; // +0 == -0 -> true
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), true }; // -0 == +0 -> true
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), true }; // +0 == +0 -> true
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000001), true }; // 1 == 1 -> true
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xB040000000000000, 0x0000000000000001), false }; // 1 == -1 -> false
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000001), false }; // -1 == 1 -> false
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x303E000000000000, 0x000000000000000A), true }; // 1 == 1.0 -> true (cohort)
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001), true }; // 1.0 == 1 -> true (cohort)
            yield return new object[] { new UInt128(0x303C000000000000, 0x0000000000000064), new UInt128(0x303E000000000000, 0x000000000000000A), true }; // 1.00 == 1.0 -> true (cohort)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000064), new UInt128(0x3044000000000000, 0x0000000000000001), true }; // 100 == 1e2 -> true (cohort)
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001), false }; // 10 == 1 -> false
            yield return new object[] { new UInt128(0x5FFE314DC6448D93, 0x38C15B0A00000000), new UInt128(0x5FFE314DC6448D93, 0x38C15B0A00000000), true }; // large == large -> true
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), true }; // all-nines == all-nines -> true
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFE), false }; // all-nines == near -> false
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), new UInt128(0x303E000000000000, 0x0000000000000001), true }; // 0.1 == 0.1 -> true
            yield return new object[] { new UInt128(0xB03E000000000000, 0x0000000000000001), new UInt128(0x303E000000000000, 0x0000000000000001), false }; // -0.1 == 0.1 -> false
            yield return new object[] { new UInt128(0x304A000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), true }; // +0 (exp 5) == +0 -> true (zero cohort)
            yield return new object[] { new UInt128(0x301A000000000003, 0x9BCF4E2EDCF12EF7), new UInt128(0x3018000000000024, 0x16190DD4A16BD5A6), true };
            yield return new object[] { new UInt128(0xABF20063BD39CCEE, 0x436C8968918D808D), new UInt128(0x303E000000000000, 0x00230AA75BEE8E14), false };
            yield return new object[] { new UInt128(0x300C00002B873DBB, 0xBEBE1FC587469CE0), new UInt128(0x2FF00016B43C9B4F, 0x23150FD733E05182), false };
            yield return new object[] { new UInt128(0xB026000000000000, 0x0000076D2FCDB6B3), new UInt128(0x3000000000000000, 0x0003114CF7F46793), false };
            yield return new object[] { new UInt128(0x3030000000000030, 0x6A45B64B7AA1C513), new UInt128(0x302C0000000012E9, 0x833B357BE730FB6C), true };
            yield return new object[] { new UInt128(0x289C000000000000, 0x000000000004EDA8), new UInt128(0x2896000000000000, 0x0000000013405840), true };
            yield return new object[] { new UInt128(0xB008000000000000, 0x0AC06CB3BC9AF0A1), new UInt128(0xB004000000000004, 0x332A7635AC85FEE4), true };
            yield return new object[] { new UInt128(0xD3FA000000000000, 0x0000000000000F1B), new UInt128(0xB02800044597400B, 0x08FB25BCBB359B1B), false };
            yield return new object[] { new UInt128(0x3038000000000000, 0x0157E2AA499D25FA), new UInt128(0xB048000000000000, 0x000000000000003B), false };
            yield return new object[] { new UInt128(0x1A30000000000000, 0x0970216EDBCE4CB6), new UInt128(0x1A2C000000000003, 0xAFCD0F4DDC95F718), true };
            yield return new object[] { new UInt128(0xB032000002F2A152, 0x9B60F2A4EEAF5036), new UInt128(0x305E000000000000, 0x000000000003CF69), false };
            yield return new object[] { new UInt128(0xAFFC000000000000, 0x000119FF4AE64AB6), new UInt128(0xAFF6000000000000, 0x044D8D3C9393D6F0), true };
            yield return new object[] { new UInt128(0xB024000000000000, 0x0000000000383395), new UInt128(0xB01E000000000000, 0x00000000DB897E08), true };
            yield return new object[] { new UInt128(0x3016000000000000, 0x00000000025C222D), new UInt128(0xAFEE0000105989B6, 0x14E871DF7FB4E7FF), false };
            yield return new object[] { new UInt128(0xAFE4004BC3F60664, 0xA2D28316972FB16C), new UInt128(0x3024000000000000, 0x336683344D857468), false };
            yield return new object[] { new UInt128(0xB026000000000000, 0x0000000000000009), new UInt128(0xD38C000000000000, 0x0134998469438B15), false };
            yield return new object[] { new UInt128(0xB030000000000000, 0x00000006C1105459), new UInt128(0xB040000000000000, 0x0000000000000272), false };
            yield return new object[] { new UInt128(0x30360000000000BC, 0x438EB99271BCAFC8), new UInt128(0xB040000000000000, 0x00000000DBE2C593), false };
            yield return new object[] { new UInt128(0x9B3E004F2DD1B479, 0xFC21185B344542C4), new UInt128(0xB052000000000000, 0x00000000EFF4031C), false };
            yield return new object[] { new UInt128(0x3020000000000000, 0x0000009AA0434574), new UInt128(0x301C000000000000, 0x00003C669A472150), true };
            yield return new object[] { new UInt128(0x3048000000000000, 0x000000003A904488), new UInt128(0x3046000000000000, 0x0000000249A2AD50), true };
            yield return new object[] { new UInt128(0x303C0000000000E8, 0xFEE00455BE372834), new UInt128(0x3036000000038E23, 0x9B10EEEF07750B20), true };
            yield return new object[] { new UInt128(0xB012000000000000, 0x0000000002BA9121), new UInt128(0xB00C000000000000, 0x0000000AA8C6E8E8), true };
            yield return new object[] { new UInt128(0x3004000000000000, 0x0000BD66F041ECD5), new UInt128(0x3002000000000000, 0x0007660562934052), true };
            yield return new object[] { new UInt128(0xADE0000000000000, 0x0002A69155BE30BC), new UInt128(0xADDE000000000000, 0x001A81AD596DE758), true };
            yield return new object[] { new UInt128(0xDF96000000000000, 0x000000DF56F88A23), new UInt128(0xDF90000000000000, 0x0003686BBADB98B8), true };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x0000000000000034), new UInt128(0xB016000000000000, 0x0000000000001450), true };
            yield return new object[] { new UInt128(0x334C0000001FE2B8, 0xADA8450705F0175D), new UInt128(0x334800000C749023, 0xD5BAF6BE51C92054), true };
            yield return new object[] { new UInt128(0x8462000000000000, 0x00000000EAC878D0), new UInt128(0xB044000000000000, 0x0766DC043F42224C), false };
            yield return new object[] { new UInt128(0xB02400012DE50EC6, 0xBBBA78C47B74A0E0), new UInt128(0x302200000000012F, 0x1F7FF8F741AE5567), false };
            yield return new object[] { new UInt128(0x3038000000000000, 0x0000000000000057), new UInt128(0xB03C000000000000, 0x000000110FC8CD30), false };
            yield return new object[] { new UInt128(0x3030000000000000, 0x17E95893CB288412), new UInt128(0x004A00000000001C, 0x8D893304C576A4DE), false };
            yield return new object[] { new UInt128(0xB030000000000000, 0x0000000002CEAE78), new UInt128(0xB02A000000000000, 0x0000000AF75984C0), true };
            yield return new object[] { new UInt128(0xB004000000000000, 0x0000000FAFB5B374), new UInt128(0xDF860003F338CEEF, 0xDDFFE3EAF7A5A1FD), false };
            yield return new object[] { new UInt128(0xB026000000000000, 0x00009E4E44EAB8B0), new UInt128(0x9244000000000000, 0x000004781F6323F3), false };
            yield return new object[] { new UInt128(0xAFFA00000000002E, 0x3420D6865B6A6E4E), new UInt128(0xD258000000000000, 0x000000000091C9EF), false };
            yield return new object[] { new UInt128(0x300A000000000000, 0x0000167364209915), new UInt128(0x301C000000000000, 0x000000000439E2E9), false };
            yield return new object[] { new UInt128(0xB014000000000032, 0x708772352DE98CF2), new UInt128(0xB0120000000001F8, 0x654A7613CB1F8174), true };
            yield return new object[] { new UInt128(0xA7B80000000002FC, 0x39DED1B856E65FE1), new UInt128(0xB0240000000047EB, 0x1D33A9AEB44C6F89), false };
            yield return new object[] { new UInt128(0xB03A000000006CA7, 0xBAF42939DF4E7BA0), new UInt128(0x3012000000008D6C, 0x822E872520B2A4C8), false };
            yield return new object[] { new UInt128(0x1530000000000000, 0x0000000005217896), new UInt128(0x152C000000000000, 0x0000000201131A98), true };
            yield return new object[] { new UInt128(0xB042000000000000, 0x00000000002D231B), new UInt128(0xB040000000000000, 0x0000000001C35F0E), true };
            yield return new object[] { new UInt128(0x300800000041C9DA, 0x0AE55CDA1CDDE16F), new UInt128(0x300600000291E284, 0x6CF5A08520AACE56), true };
            yield return new object[] { new UInt128(0xB024000000000000, 0x00000000007F4CCA), new UInt128(0xB020000000000000, 0x0000000031B9FEE8), true };
            yield return new object[] { new UInt128(0x3050000000000000, 0x000000001572A68A), new UInt128(0x30040058AA3F10A7, 0x9EFB87DE8539A6CD), false };
            yield return new object[] { new UInt128(0xB006000000000000, 0x0000041BDB3C92D4), new UInt128(0xB004000000000000, 0x00002916905DBC48), true };
            yield return new object[] { new UInt128(0x3022000000000002, 0x18ABFEB8FC5D88D3), new UInt128(0x5F400000000005E8, 0x2C8923BCBDB9FD3E), false };
            yield return new object[] { new UInt128(0xB034000000000000, 0x00000000000C3C2B), new UInt128(0xB032000000000000, 0x00000000007A59AE), true };
            yield return new object[] { new UInt128(0xB0040315621B218F, 0xE4D69AF338BACC8F), new UInt128(0xB0021ED5D50F4F9E, 0xF0620D80374BFD96), true };
            yield return new object[] { new UInt128(0xB02E000000000000, 0x000000440D74407B), new UInt128(0xB02A000000000000, 0x00001A954169300C), true };
            yield return new object[] { new UInt128(0x302600393862E7E8, 0x73749E8BE815E588), new UInt128(0x3020DF844259E402, 0xFF8B528295889B40), true };
            yield return new object[] { new UInt128(0x3038000000000000, 0x096997BACFE09F53), new UInt128(0x3032000000000024, 0xC478B1BC056E5C38), true };
            yield return new object[] { new UInt128(0x3040000000000000, 0x00000000F4E84277), new UInt128(0x302E000000029939, 0x82A8BC0D5A4BB09F), false };
            yield return new object[] { new UInt128(0x305C000000000000, 0x000000000000FE36), new UInt128(0x3058000000000000, 0x0000000000634D18), true };
            yield return new object[] { new UInt128(0xB00E000000000000, 0x000000008DD9B33D), new UInt128(0xAFF80000004473DF, 0xDFFE9B3EC48B6A65), false };
            yield return new object[] { new UInt128(0xB012000000000000, 0x000000013AA4FC9F), new UInt128(0x3014000000000000, 0x00003245B752F717), false };
            yield return new object[] { new UInt128(0x3002000000000000, 0x081624F7721E25C3), new UInt128(0x2FFE000000000003, 0x28A670A893C6C02C), true };
            yield return new object[] { new UInt128(0xDDC8000000000000, 0x00855569DF51C986), new UInt128(0x3024000000000000, 0x000000000007C72E), false };
            yield return new object[] { new UInt128(0x1758000000000000, 0x000000041F72D9AA), new UInt128(0x301A0000000061C8, 0xEEFC10C4D3DADBE3), false };
            yield return new object[] { new UInt128(0xB0100003A1125F48, 0x953E3B9231F49265), new UInt128(0xB00C016AEB2D385A, 0x4C4F451B83892F74), true };
        }

        [Theory]
        [MemberData(nameof(op_Equality_TestData))]
        public static void op_Equality(UInt128 left, UInt128 right, bool expected)
        {
            Decimal128 l = Unsafe.BitCast<UInt128, Decimal128>(left);
            Decimal128 r = Unsafe.BitCast<UInt128, Decimal128>(right);
            Assert.Equal(expected, l == r);
            Assert.Equal(!expected, l != r);
        }

        public static IEnumerable<object[]> op_Comparison_TestData()
        {
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), false, false, false, false }; // NaN vs 1 -> unordered
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xFC00000000000000, 0x0000000000000000), false, false, false, false }; // 1 vs NaN -> unordered
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000), false, false, false, false }; // NaN vs NaN -> unordered
            yield return new object[] { new UInt128(0xFE00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), false, false, false, false }; // sNaN vs 1 -> unordered
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), false, false, false, false }; // NaN vs +Inf -> unordered
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), false, true, false, true }; // +Inf vs 1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000), true, false, true, false }; // 1 vs +Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), true, false, true, false }; // -Inf vs 1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xF800000000000000, 0x0000000000000000), false, true, false, true }; // 1 vs -Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), true, false, true, false }; // -Inf vs +Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), false, false, true, true }; // +Inf vs +Inf (equal)
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), false, false, true, true }; // -Inf vs -Inf (equal)
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000002), new UInt128(0x7800000000000000, 0x0000000000000000), false, false, true, true }; // non-canonical +Inf vs +Inf (equal)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), false, false, true, true }; // +0 vs -0 (equal)
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), false, false, true, true }; // -0 vs +0 (equal)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), true, false, true, false }; // 0 vs 1
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000000), true, false, true, false }; // -1 vs 0
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000002), true, false, true, false }; // 1 vs 2
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000001), false, true, false, true }; // 2 vs 1
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000001), true, false, true, false }; // -1 vs 1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xB040000000000000, 0x0000000000000001), false, true, false, true }; // 1 vs -1
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0xB040000000000000, 0x0000000000000002), false, true, false, true }; // -1 vs -2
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x303E000000000000, 0x000000000000000A), false, false, true, true }; // 1 vs 1.0 (cohort equal)
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001), false, false, true, true }; // 1.0 vs 1 (cohort equal)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000064), new UInt128(0x3044000000000000, 0x0000000000000001), false, false, true, true }; // 100 vs 1e2 (cohort equal)
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001), false, true, false, true }; // 10 vs 1
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), new UInt128(0x303E000000000000, 0x0000000000000002), true, false, true, false }; // 0.1 vs 0.2
            yield return new object[] { new UInt128(0x3034000000000000, 0x000000000098967F), new UInt128(0x3034000000000000, 0x000000000098967E), false, true, false, true }; // close values
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFE), false, true, false, true }; // all-nines vs near
            yield return new object[] { new UInt128(0x5FFE314DC6448D93, 0x38C15B0A00000000), new UInt128(0x5FFE04EE2D6D415B, 0x85ACEF8100000000), false, true, false, true }; // large magnitudes
            yield return new object[] { new UInt128(0xDFFE314DC6448D93, 0x38C15B0A00000000), new UInt128(0x5FFE314DC6448D93, 0x38C15B0A00000000), true, false, true, false }; // -large vs +large
            yield return new object[] { new UInt128(0x2FFE289FF22ABDCC, 0xB2563F2BA4A187B8), new UInt128(0x3032000000002117, 0xA8AB1AC92768DFCC), true, false, true, false };
            yield return new object[] { new UInt128(0xB02A000000000000, 0x0000000003E03EDC), new UInt128(0x3034000000000000, 0x5A65FF2A16E631E0), true, false, true, false };
            yield return new object[] { new UInt128(0xB032000000000A9F, 0x4028DF83A3FBDB37), new UInt128(0x3054000000000000, 0x000000002DF6D94F), true, false, true, false };
            yield return new object[] { new UInt128(0xB03E000000000000, 0x00000001A8B80287), new UInt128(0xB03C000000000000, 0x0000001097301946), false, false, true, true };
            yield return new object[] { new UInt128(0x305C000000000000, 0x00000000003673BF), new UInt128(0x3028000000000000, 0x00000007C09C1527), false, true, false, true };
            yield return new object[] { new UInt128(0xAFFE00000000019B, 0x341186BF3F1DA827), new UInt128(0xB03E000000000000, 0x0005DCB5F7ED6B34), false, true, false, true };
            yield return new object[] { new UInt128(0x300C000000000000, 0x000179DCB4BE3FF7), new UInt128(0xB012000000000000, 0x0000000002BA9121), false, true, false, true };
            yield return new object[] { new UInt128(0x0D04000000000000, 0x001C8C6C22C81050), new UInt128(0x304E000000000000, 0x0000000000001216), true, false, true, false };
            yield return new object[] { new UInt128(0xB008000000000000, 0x0000B26F886C7769), new UInt128(0xB01A000000000000, 0x0000000000000034), true, false, true, false };
            yield return new object[] { new UInt128(0x3036000000000000, 0x0000042026A018FC), new UInt128(0xB020000000000000, 0x000000000000CCF5), false, true, false, true };
            yield return new object[] { new UInt128(0x8462000000000000, 0x00000000EAC878D0), new UInt128(0xB044000000000000, 0x0766DC043F42224C), false, true, false, true };
            yield return new object[] { new UInt128(0xB02400012DE50EC6, 0xBBBA78C47B74A0E0), new UInt128(0x302200000000012F, 0x1F7FF8F741AE5567), true, false, true, false };
            yield return new object[] { new UInt128(0x3038000000000000, 0x0000000000000057), new UInt128(0xB03C000000000000, 0x000000110FC8CD30), false, true, false, true };
            yield return new object[] { new UInt128(0x3030000000000000, 0x17E95893CB288412), new UInt128(0x004A00000000001C, 0x8D893304C576A4DE), false, true, false, true };
            yield return new object[] { new UInt128(0xB030000000000000, 0x0000000002CEAE78), new UInt128(0x2FEA538FE26D7D1B, 0xD726661AA55DF804), true, false, true, false };
            yield return new object[] { new UInt128(0xDF8E0000001FA271, 0xF338CEEFDDFFE3EA), new UInt128(0xB040000000000000, 0x0B6848AD2793AAE4), true, false, true, false };
            yield return new object[] { new UInt128(0x9244000000000000, 0x000004781F6323F3), new UInt128(0x30220000000009CA, 0xBA5D608B3420D686), true, false, true, false };
            yield return new object[] { new UInt128(0x3034000000000000, 0x000000A44C289F0F), new UInt128(0xB01C000000000000, 0x0000000000023EFC), false, true, false, true };
            yield return new object[] { new UInt128(0xB02600000FD5551A, 0x94F228ED26B2F66C), new UInt128(0xB020003DD9546FD5, 0xD1EFDE5F2B1295E0), false, false, true, true };
            yield return new object[] { new UInt128(0x9D10000000000000, 0x7E51E27048F9F398), new UInt128(0xB04C000000000000, 0x000000000000ECB9), false, true, false, true };
            yield return new object[] { new UInt128(0xB044000000000000, 0x00000000023F5ABE), new UInt128(0xB0280000082CA932, 0x5E4B1837B8D3E89E), false, true, false, true };
            yield return new object[] { new UInt128(0x301C000000000007, 0x0964D911A0BBD8F4), new UInt128(0x305E000000000000, 0x000000000000A71E), true, false, true, false };
            yield return new object[] { new UInt128(0x3026000000000000, 0x000000795A3C873F), new UInt128(0x300800000041C9DA, 0x0AE55CDA1CDDE16F), false, true, false, true };
            yield return new object[] { new UInt128(0xB650000000D0224F, 0xFD6F80D1270DDE07), new UInt128(0x301800007AEF1309, 0xE6818A3F216A34FF), true, false, true, false };
            yield return new object[] { new UInt128(0x3006000A9EFB87DE, 0x8539A6CD7BF62403), new UInt128(0xB006000000000000, 0x0000041BDB3C92D4), false, true, false, true };
            yield return new object[] { new UInt128(0x301C000000000315, 0xFC5D88D34D795D12), new UInt128(0x5F400000000005E8, 0x2C8923BCBDB9FD3E), true, false, true, false };
            yield return new object[] { new UInt128(0xB034000000000000, 0x00000000000C3C2B), new UInt128(0xB02E000000000000, 0x00001C5D7E76C0D0), false, true, false, true };
            yield return new object[] { new UInt128(0x3028000000000000, 0x00000000155EEEE3), new UInt128(0xC63C000000000000, 0x0000000000000016), false, true, false, true };
            yield return new object[] { new UInt128(0x3010125D89F7C323, 0x37C02ED0435BE309), new UInt128(0xB012000000000000, 0x000000000009536A), false, true, false, true };
            yield return new object[] { new UInt128(0x3038000000000000, 0x096997BACFE09F53), new UInt128(0x3040000000000000, 0x00000000F4E84277), false, true, false, true };
            yield return new object[] { new UInt128(0x3042000000000000, 0x0001692E610A8ED8), new UInt128(0x303E000000000000, 0x008D161DE81FCC60), false, false, true, true };
            yield return new object[] { new UInt128(0x2A96000000000000, 0x0000000000398706), new UInt128(0xB042000000000000, 0x000000000000A932), false, true, false, true };
            yield return new object[] { new UInt128(0x303E000000000000, 0x000002503A034940), new UInt128(0xB026000000000000, 0x0000000000587FAD), false, true, false, true };
            yield return new object[] { new UInt128(0x3014000000000000, 0x0721E25C46337978), new UInt128(0xAFFA4821D7034A02, 0xB8ACC25DF6F4CF86), false, true, false, true };
            yield return new object[] { new UInt128(0xB044000000000000, 0x00E1766220EE98D3), new UInt128(0xB042000000000000, 0x08CE9FD54951F83E), false, false, true, true };
            yield return new object[] { new UInt128(0x8840000000000531, 0x049414502C46E143), new UInt128(0xB03C000000000000, 0x00000000005D61A7), false, true, false, true };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x0000031F76932CF2), new UInt128(0xB00A00000022492E, 0xC74AC5D28F7C2B9E), false, true, false, true };
            yield return new object[] { new UInt128(0x301A000000000000, 0x000000696040784C), new UInt128(0x3014000000000000, 0x00019B9FFBD5E8E0), false, false, true, true };
            yield return new object[] { new UInt128(0xB0040000000022D8, 0x1DA76F3AF6929B87), new UInt128(0x30240000F05E5FAC, 0x7D46A35E53517B13), true, false, true, false };
            yield return new object[] { new UInt128(0x300200000220753C, 0x8CA178C667F5ED50), new UInt128(0xB036000000000000, 0x0000000000010401), false, true, false, true };
            yield return new object[] { new UInt128(0xB050000000000000, 0x00000000001B96F0), new UInt128(0xC4DA000000000000, 0x002332BFAE0ABB23), false, true, false, true };
            yield return new object[] { new UInt128(0xCD32000000000000, 0x00000062ADC4C04D), new UInt128(0xD02A000000000000, 0x00000000000014F6), false, true, false, true };
            yield return new object[] { new UInt128(0xB040000000000000, 0x00017F274F195D94), new UInt128(0x2FFA000000000000, 0x022F53F33F61565B), true, false, true, false };
            yield return new object[] { new UInt128(0xB044000000000000, 0x0000000000000046), new UInt128(0x2FFA000000000000, 0x000ED37C5E4A6F24), true, false, true, false };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x000000148D08DAD9), new UInt128(0x2FFA000000000000, 0x1FBD55F356C051B7), true, false, true, false };
            yield return new object[] { new UInt128(0x304A000000000000, 0x00000003CD7FC505), new UInt128(0x3046000000000000, 0x0000017C45E8F5F4), false, false, true, true };
            yield return new object[] { new UInt128(0x46D4000000000000, 0x0000000014D6A755), new UInt128(0x2FE6000001F28868, 0xEDF78B1F515A1856), false, true, false, true };
            yield return new object[] { new UInt128(0xB02C000956B6687B, 0x36CEC180F9999542), new UInt128(0x2FFC001E242C92DD, 0xC816BBACBE275F77), true, false, true, false };
            yield return new object[] { new UInt128(0x2FEC879CB69E66F9, 0x5CF87F593585DB26), new UInt128(0x40CE000000000000, 0x0000000FC627A4F5), true, false, true, false };
            yield return new object[] { new UInt128(0xB02C000000000000, 0x0020BB76731068C7), new UInt128(0x3020000000007B27, 0xC72B0E98A7E6BDA9), true, false, true, false };
            yield return new object[] { new UInt128(0x823000000000002A, 0xA9473055B2F65315), new UInt128(0x300800363B6D91C5, 0x9ECF8577D4668EFE), true, false, true, false };
            yield return new object[] { new UInt128(0xAFE400013C7FC403, 0xE8DFAB61815BDD62), new UInt128(0xAFFC000000000000, 0x0AE997D86F01F581), false, true, false, true };
            yield return new object[] { new UInt128(0xB018000000120CBF, 0x23F4CD9940BD2B9A), new UInt128(0xDFCC005830554042, 0x149E930F0D337664), false, true, false, true };
            yield return new object[] { new UInt128(0x300E000000000000, 0x0112E59C50FC31A5), new UInt128(0xB0180000000007E1, 0xF8A6349B7A2C9257), false, true, false, true };
            yield return new object[] { new UInt128(0x300E000000000000, 0x0000001427C344A7), new UInt128(0x300C000000000000, 0x000000C98DA0AE86), false, false, true, true };
            yield return new object[] { new UInt128(0xD270000000000000, 0x00000815ABADD29E), new UInt128(0xD26C000000000000, 0x000328770FE645B8), false, false, true, true };
            yield return new object[] { new UInt128(0xAFE8000619369D80, 0xCF00A8D8FFBDB9B9), new UInt128(0xAFE6003CFC222708, 0x16069879FD69413A), false, false, true, true };
            yield return new object[] { new UInt128(0x3052000000000000, 0x0000001410258185), new UInt128(0x3050000000000000, 0x000000C8A1770F32), false, false, true, true };
            yield return new object[] { new UInt128(0x2FF6000000000000, 0x08D41FB605523418), new UInt128(0x304A000000000000, 0x00003055B53A1C43), true, false, true, false };
            yield return new object[] { new UInt128(0xB036000000000000, 0x0000000000000012), new UInt128(0xB00E000000000061, 0xC2DBBF40A99DB992), false, true, false, true };
        }

        [Theory]
        [MemberData(nameof(op_Comparison_TestData))]
        public static void op_Comparison(UInt128 left, UInt128 right, bool lessThan, bool greaterThan, bool lessThanOrEqual, bool greaterThanOrEqual)
        {
            Decimal128 l = Unsafe.BitCast<UInt128, Decimal128>(left);
            Decimal128 r = Unsafe.BitCast<UInt128, Decimal128>(right);
            Assert.Equal(lessThan, l < r);
            Assert.Equal(greaterThan, l > r);
            Assert.Equal(lessThanOrEqual, l <= r);
            Assert.Equal(greaterThanOrEqual, l >= r);
        }

        public static IEnumerable<object[]> op_Multiply_TestData()
        {
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // NaN * 1 -> NaN
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // 1 * NaN -> NaN
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // NaN * +Inf -> NaN
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // +Inf * +0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // +0 * +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // -Inf * +0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // +Inf * -0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000) }; // +Inf * 1 -> +Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0xF800000000000000, 0x0000000000000000) }; // +Inf * -1 -> -Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0xF800000000000000, 0x0000000000000000) }; // -Inf * 2 -> -Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000002), new UInt128(0x7800000000000000, 0x0000000000000000) }; // -Inf * -2 -> +Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // +Inf * +Inf -> +Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) }; // +Inf * -Inf -> -Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // -Inf * -Inf -> +Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000) }; // non-canonical +Inf * 1 -> canonical +Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xF800000000000000, 0x0000000000000005), new UInt128(0xF800000000000000, 0x0000000000000000) }; // 1 * non-canonical -Inf -> canonical -Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) }; // +0 * +0 -> +0
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) }; // -0 * -0 -> +0
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) }; // +0 * -0 -> -0
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) }; // 1 * -0 -> -0
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) }; // -1 * +0 -> -0
            yield return new object[] { new UInt128(0x304A000000000000, 0x0000000000000000), new UInt128(0x3046000000000000, 0x0000000000000000), new UInt128(0x3050000000000000, 0x0000000000000000) }; // 0e5 * 0e3 -> 0e8 (preferred exp)
            yield return new object[] { new UInt128(0x3036000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x000000000000000C), new UInt128(0x3036000000000000, 0x0000000000000000) }; // 0e-5 * 12 -> 0e-5 (preferred exp)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x000000000000000A) }; // 2 * 5 -> 10 (exp 0, trailing zero retained)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x303E000000000000, 0x0000000000000005), new UInt128(0x303E000000000000, 0x000000000000000A) }; // 2 * 0.5 -> 1.0
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000F), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x303E000000000000, 0x000000000000001E) }; // 1.5 * 2 -> 3.0
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), new UInt128(0x303E000000000000, 0x0000000000000001), new UInt128(0x303C000000000000, 0x0000000000000001) }; // 0.1 * 0.1 -> 0.01
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000C), new UInt128(0x303E000000000000, 0x000000000000000C), new UInt128(0x303C000000000000, 0x0000000000000090) }; // 1.2 * 1.2 -> 1.44
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000007), new UInt128(0xB040000000000000, 0x0000000000000015) }; // -3 * 7 -> -21
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000C), new UInt128(0x3040000000000000, 0x000000000000000C), new UInt128(0x3040000000000000, 0x0000000000000090) }; // 12 * 12 -> 144
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3085ED09BEAD87C0, 0x378D8E63FFFFFFFE) }; // all-nines squared (round)
            yield return new object[] { new UInt128(0x2FFE41BD085B676E, 0xF657240D55555555), new UInt128(0x2FFE57A6B5CF3493, 0xF31EDABC71C71C71), new UInt128(0x2FFE74DE47BEF0C5, 0x442923A5ED097B41) }; // full-precision * full-precision (round)
            yield return new object[] { new UInt128(0x3040314DC6448D93, 0x38C15B0A00000000), new UInt128(0x3040314DC6448D93, 0x38C15B0A00000000), new UInt128(0x3082314DC6448D93, 0x38C15B0A00000000) }; // 10^(P-1) * 10^(P-1) (trailing zeros beyond precision)
            yield return new object[] { new UInt128(0x5FFFBBBBF868FA2C, 0xFECC335A00000000), new UInt128(0x5FFFBBBBF868FA2C, 0xFECC335A00000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // huge * huge -> +Inf (overflow)
            yield return new object[] { new UInt128(0xDFFFBBBBF868FA2C, 0xFECC335A00000000), new UInt128(0x5FFFBBBBF868FA2C, 0xFECC335A00000000), new UInt128(0xF800000000000000, 0x0000000000000000) }; // -huge * huge -> -Inf (overflow)
            yield return new object[] { new UInt128(0x0040000000000000, 0x0000000000000001), new UInt128(0x0040000000000000, 0x0000000000000001), new UInt128(0x0000000000000000, 0x0000000000000000) }; // tiny * tiny -> underflow
            yield return new object[] { new UInt128(0x0042000000000000, 0x0000000000000001), new UInt128(0x3036000000000000, 0x0000000000000001), new UInt128(0x0038000000000000, 0x0000000000000001) }; // small * small (subnormal-ish)
            yield return new object[] { new UInt128(0x181DED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x181DED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x003DED09BEAD87C0, 0x378D8E63FFFFFFFE) }; // wide product just below min quantum (normal after single rounding)
            yield return new object[] { new UInt128(0x17FBED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x17FDED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x0000007E37BE2022, 0xC0914B2680000000) }; // wide product deep in the subnormal range
            yield return new object[] { new UInt128(0x0041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x0041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x0000000000000000, 0x0000000000000000) }; // wide product underflow to zero/epsilon
            yield return new object[] { new UInt128(0x5FFE000000000000, 0x0000000000000000), new UInt128(0x5FFE000000000000, 0x0000000000000005), new UInt128(0x5FFE000000000000, 0x0000000000000000) }; // zero * finite, preferred exponent far above max quantum (clamped)
            yield return new object[] { new UInt128(0xDFFE000000000000, 0x0000000000000000), new UInt128(0x5FFE000000000000, 0x0000000000000005), new UInt128(0xDFFE000000000000, 0x0000000000000000) }; // -zero * finite, preferred exponent clamped (sign preserved)
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000000), new UInt128(0x5FFE000000000000, 0x0000000000000005), new UInt128(0x5FFE000000000000, 0x0000000000000000) }; // zero * finite, preferred exponent one above max quantum (clamped)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x5FFE000000000000, 0x0000000000000005), new UInt128(0x5FFE000000000000, 0x0000000000000000) }; // zero * finite, preferred exponent exactly at max quantum (no clamp)
            yield return new object[] { new UInt128(0x300C000000000000, 0x0386DF690C6EF5BE), new UInt128(0xB018000000000000, 0x00022FCFF31AA0B9), new UInt128(0xAFE407B65F3E6C11, 0x97E447D9A512564E) };
            yield return new object[] { new UInt128(0x3016000000000000, 0x00000000025C222D), new UInt128(0xAFEE0000105989B6, 0x14E871DF7FB4E7FF), new UInt128(0xAFC862C63C9DC466, 0xEEA20CD21BC0A580) };
            yield return new object[] { new UInt128(0xAFE4004BC3F60664, 0xA2D28316972FB16C), new UInt128(0x3024000000000000, 0x336683344D857468), new UInt128(0xAFE86D9DF48A21EF, 0xA1A8BB1C44333B9B) };
            yield return new object[] { new UInt128(0xB026000000000000, 0x0000000000000009), new UInt128(0xD36C269369438B15, 0x4317B7B7F941F96B), new UInt128(0x53535B2EB35FE3BF, 0x5BD57577C351C4C3) };
            yield return new object[] { new UInt128(0xB030000000000000, 0x00000006C1105459), new UInt128(0xB01E100AA3FBDB37, 0x590C5A8BE72C6FA0), new UInt128(0x3021D1590788E3E1, 0xE8D4D9049C75FA8E) };
            yield return new object[] { new UInt128(0x4592000000000000, 0x0125EB92A4B6C61E), new UInt128(0xB03E000000000000, 0x00000001A8B80287), new UInt128(0xC590000001E7A16F, 0x190B7B1E5F7EB5D2) };
            yield return new object[] { new UInt128(0xB018000000000000, 0x000016E8FC21185B), new UInt128(0xB052000000000000, 0x00000000EFF4031C), new UInt128(0x302A000000001579, 0x59BA71B822A3BAF4) };
            yield return new object[] { new UInt128(0x3020000000000000, 0x0000009AA0434574), new UInt128(0x3048000000000000, 0x0000000000001ED7), new UInt128(0x3028000000000000, 0x0012A0A47AA2EC6C) };
            yield return new object[] { new UInt128(0xB058000000000000, 0x00000000000003DF), new UInt128(0x300C000000000000, 0x000179DCB4BE3FF7), new UInt128(0xB024000000000000, 0x05B6BD5FAC799D29) };
            yield return new object[] { new UInt128(0x9EE405C739C662F4, 0x5752242E1DB1EC20), new UInt128(0x3004000000000000, 0x0000BD66F041ECD5), new UInt128(0x9EC27854D8F6C60F, 0x6949AA79C4640D7C) };
            yield return new object[] { new UInt128(0xAFE6000119C10D0F, 0xC98968772E7E6EF7), new UInt128(0xB0020000000002DA, 0x7B7ACAB57EE654B5), new UInt128(0x2FCC39EEA2704D09, 0x18025365B6B0A095) };
            yield return new object[] { new UInt128(0xB00E000000000000, 0x00000000004E6990), new UInt128(0x3852000000000000, 0x000000000000031D), new UInt128(0xB820000000000000, 0x00000000F41EA550) };
            yield return new object[] { new UInt128(0x3008000000000000, 0x000000014204609D), new UInt128(0xB020000000000000, 0x000000000000CCF5), new UInt128(0xAFE8000000000000, 0x000101CFAB2D9241) };
            yield return new object[] { new UInt128(0x8462000000000000, 0x00000000EAC878D0), new UInt128(0xB034000000000000, 0x0000000000000110), new UInt128(0x0456000000000000, 0x000000F975005D00) };
            yield return new object[] { new UInt128(0xB02400012DE50EC6, 0xBBBA78C47B74A0E0), new UInt128(0x302200000000012F, 0x1F7FF8F741AE5567), new UInt128(0xB0290194CFA11AFA, 0x7F52F9881B76CEAC) };
            yield return new object[] { new UInt128(0x3038000000000000, 0x0000000000000057), new UInt128(0xB03C000000000000, 0x000000110FC8CD30), new UInt128(0xB034000000000000, 0x000005CC5D3DBB50) };
            yield return new object[] { new UInt128(0x3030000000000000, 0x17E95893CB288412), new UInt128(0xC6E2000000000000, 0x52ABCE0A9DE14E35), new UInt128(0xC6DA329B18E3DD29, 0xFE1B6F4B3BC384C8) };
            yield return new object[] { new UInt128(0x0870000000000000, 0x000000DD250AAC57), new UInt128(0xB00000000000092C, 0x7754F6DC4C1D7D08), new UInt128(0x8832CADF15269ABF, 0x15479B9D126F162C) };
            yield return new object[] { new UInt128(0xB036000000000000, 0x000000130041C7D7), new UInt128(0xB016000000000000, 0x002262E6BEFE8EBF), new UInt128(0x300C0000028D5FF6, 0x213D8511873F5B69) };
            yield return new object[] { new UInt128(0x3036000000000000, 0x00000001B6848ADB), new UInt128(0x301E000000000000, 0x00000000000A8089), new UInt128(0x3014000000000000, 0x0011FD5A5EE9CF33) };
            yield return new object[] { new UInt128(0xC3AE000000000000, 0x04CF4E8A302A8BDC), new UInt128(0xAFFA00000000002E, 0x3420D6865B6A6E4E), new UInt128(0x437291A3FAE81214, 0x39CAD6C0814BA89C) };
            yield return new object[] { new UInt128(0xD258000000000000, 0x000000000091C9EF), new UInt128(0x300A000000000000, 0x0000167364209915), new UInt128(0xD22200000000000C, 0xC91145CEC785679B) };
            yield return new object[] { new UInt128(0x301C000000000000, 0x000000000439E2E9), new UInt128(0xB014000000000032, 0x708772352DE98CF2), new UInt128(0xAFF00000D529E131, 0x40A8C42321E0EC42) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000064), new UInt128(0xB03A000000000000, 0x00003B2E17E3E88B), new UInt128(0x303A000000000000, 0x00171E015506D64C) };
            yield return new object[] { new UInt128(0xB044000000000000, 0x00000000023F5ABE), new UInt128(0xB03A000000006CA7, 0xBAF42939DF4E7BA0), new UInt128(0x303E00F43340869F, 0x30B182BD001600C0) };
            yield return new object[] { new UInt128(0xC6E6000000000000, 0x00000001B8D3E89E), new UInt128(0x301C000000000007, 0x0964D911A0BBD8F4), new UInt128(0xC6C2000C1DF87810, 0x66C04A42A1A90698) };
            yield return new object[] { new UInt128(0x305E000000000000, 0x000000000000A71E), new UInt128(0x3026000000000000, 0x000000795A3C873F), new UInt128(0x3044000000000000, 0x004F38160F51F262) };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x0000000005C867C0), new UInt128(0x302E000000000000, 0x0000000000572AE6), new UInt128(0xB008000000000000, 0x0001F8135552B680) };
            yield return new object[] { new UInt128(0xB650000000D0224F, 0xFD6F80D1270DDE07), new UInt128(0xB010000000000000, 0x0000021670F50113), new UInt128(0x362B1CC33D11D8E8, 0x94F27B03FC307F88) };
            yield return new object[] { new UInt128(0xB02C000000000006, 0x909F1381718DE51E), new UInt128(0xB01E00000000002D, 0xCE88CD810A4F6601), new UInt128(0x301A3273BF0089BE, 0x1BF92234A5C2B6C3) };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x00000000000018F2), new UInt128(0x303C000000000000, 0x000000000046CBDD), new UInt128(0xB016000000000000, 0x00000006E6096EEA) };
            yield return new object[] { new UInt128(0x5F400000000005E8, 0x2C8923BCBDB9FD3E), new UInt128(0xB034000000000000, 0x00000000000C3C2B), new UInt128(0xDF34000048457EDD, 0x4C4221E35480116A) };
            yield return new object[] { new UInt128(0xD95C0000000051B5, 0x62B3BD63621B218F), new UInt128(0xB01E000000000000, 0x0000000000000000), new UInt128(0x593A000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB0280000000006F8, 0x435BE30981A1CB95), new UInt128(0x54FE000000000000, 0x00015E8772D1DBC6), new UInt128(0xD4EE3E8B2A46B875, 0x59B96E9577A90909) };
            yield return new object[] { new UInt128(0x5C72000037D35F7D, 0xF4E8427727658DEF), new UInt128(0x3042000000000000, 0x0001692E610A8ED8), new UInt128(0x5C87524835F626B9, 0xBFC37B63EBCC127C) };
            yield return new object[] { new UInt128(0x305C000000000000, 0x000000000000FE36), new UInt128(0x3014000000000000, 0x000000D1D108B375), new UInt128(0x3030000000000000, 0x00D059A8B9E3F0AE) };
            yield return new object[] { new UInt128(0xB042000000000000, 0x000000000000A932), new UInt128(0x303E000000000000, 0x000002503A034940), new UInt128(0xB040000000000000, 0x018769F77FFF8E80) };
            yield return new object[] { new UInt128(0xB00E000000000000, 0x587FADD919E02966), new UInt128(0x3014000000000000, 0x0721E25C46337978), new UInt128(0xAFE8A196A59F5076, 0x7DBBCD2DBAAE1214) };
            yield return new object[] { new UInt128(0xAFFA4821D7034A02, 0xB8ACC25DF6F4CF86), new UInt128(0xB044000000000000, 0x00E1766220EE98D3), new UInt128(0x301FC9C3F9A45974, 0xEDCE05153B8B06AB) };
            yield return new object[] { new UInt128(0x3024000000000000, 0x000000000007C72E), new UInt128(0x1758000000000000, 0x000000041F72D9AA), new UInt128(0x173C000000000000, 0x00201153E17C428C) };
            yield return new object[] { new UInt128(0x301A0000000061C8, 0xEEFC10C4D3DADBE3), new UInt128(0xB0100003A1125F48, 0x953E3B9231F49265), new UInt128(0xB0124176B78A0C87, 0x2603353137A081D0) };
            yield return new object[] { new UInt128(0x301000004BB9B532, 0xE5CB89F41922BD32), new UInt128(0xDE64000000000000, 0x0000CCBC550D3404), new UInt128(0xDE47041BDCF88924, 0x3862B211EE51A96E) };
            yield return new object[] { new UInt128(0xB024000000000000, 0x000000000116C396), new UInt128(0x3028000000005351, 0x729900C58D0BD408), new UInt128(0xB00C005ABA1A4E2A, 0x87C41908E82054B0) };
            yield return new object[] { new UInt128(0x300200000220753C, 0x8CA178C667F5ED50), new UInt128(0xD382000000000000, 0x0A69C7380DBC5B46), new UInt128(0xD35AF381555D3933, 0xA140AE1DFC803624) };
            yield return new object[] { new UInt128(0x300C045E788BFD23, 0xFDC576761B96F039), new UInt128(0xB01A000000000000, 0x0AE0ABB2D104776E), new UInt128(0xB0075673706B72C0, 0xB77852E5EF9F5ABF) };
            yield return new object[] { new UInt128(0xCD32000000000000, 0x00000062ADC4C04D), new UInt128(0xB040000000000000, 0xD235D4832BF52B34), new UInt128(0x4D320051074B49F1, 0xC6902C3B50F4FEA4) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x00017F274F195D94), new UInt128(0x2FFA000000000000, 0x022F53F33F61565B), new UInt128(0xAFFA0345247B8835, 0x00326DAB8487FB9C) };
            yield return new object[] { new UInt128(0xB044000000000000, 0x0000000000000046), new UInt128(0x300A00000005E4A6, 0x3DBCC6C875ABB5B6), new UInt128(0xB00E0000019C8574, 0xE19E5AD02CF3AFC4) };
            yield return new object[] { new UInt128(0x3066000000000000, 0x0000000000000015), new UInt128(0x3014000000008E7B, 0xA150E858F5A1264C), new UInt128(0x303A0000000BB024, 0x3BA30F4C2638243C) };
            yield return new object[] { new UInt128(0x21A60000000048E8, 0xDC7FE586D3707752), new UInt128(0xB024000000000023, 0x2E0D31C20845EFD5), new UInt128(0xA1A06E29F26C86CE, 0x9B6E54620B903F9B) };
            yield return new object[] { new UInt128(0xAFF403A1D73F5ACD, 0x96A762C7BAA70840), new UInt128(0x3042000000000000, 0x000000019FF4DE39), new UInt128(0xB006FD79CCA0632A, 0x1CF2022B79252C0A) };
            yield return new object[] { new UInt128(0x303C000000000000, 0x00113925FCD09CDA), new UInt128(0x3000000000007140, 0x43CE4908B69E66F9), new UInt128(0x30087FD4C96325E5, 0x83F8D90398C3CF2C) };
            yield return new object[] { new UInt128(0x3004000000000000, 0x00003C65D7E4774B), new UInt128(0x301825B99EFDAE69, 0x303766A98D4C34B5), new UInt128(0x2FF6FA8670030F05, 0xFFC0D3475619D770) };
            yield return new object[] { new UInt128(0xB0220000004AE762, 0xF65E345A6F0E69F9), new UInt128(0x301A000000000000, 0x00000000000DC8E9), new UInt128(0xAFFC040888F86C54, 0x7D3BF0CDB88DFBA1) };
            yield return new object[] { new UInt128(0x3052000000000000, 0x000000000000002A), new UInt128(0x300000001C3D6C1F, 0x26F3E82E5A4692F0), new UInt128(0x30120004A213BD1C, 0x6404179ACF941B60) };
            yield return new object[] { new UInt128(0xB048000000000000, 0x0000000000000280), new UInt128(0xAFFC000000000000, 0x0AE997D86F01F581), new UInt128(0x300400000000001B, 0x47FB9D1584E5C280) };
            yield return new object[] { new UInt128(0xB018000000120CBF, 0x23F4CD9940BD2B9A), new UInt128(0xDFCC005830554042, 0x149E930F0D337664), new UInt128(0x5FD24B2B8DCF44E4, 0xF9328AD297D2D135) };
            yield return new object[] { new UInt128(0x300E000000000000, 0x0112E59C50FC31A5), new UInt128(0x2FEC16CD5779F833, 0xFD5BB8EB3F0C6FD3), new UInt128(0x2FDAB06F5E049A66, 0x55F91067350E15D9) };
            yield return new object[] { new UInt128(0x3D3E000000000000, 0x00000000E4F7F4D6), new UInt128(0xD270000000000000, 0x00000815ABADD29E), new UInt128(0xDF6E00000000073B, 0x218ADE0A1E7EA814) };
            yield return new object[] { new UInt128(0x3034000000000000, 0x00181648272743F6), new UInt128(0x569600000002FBFF, 0x4C2693B7C7D3A38C), new UInt128(0x5698789A280DEAE7, 0x8FCFA699FD055CA3) };
        }

        [Theory]
        [MemberData(nameof(op_Multiply_TestData))]
        public static void op_Multiply(UInt128 left, UInt128 right, UInt128 expected)
        {
            Decimal128 result = Unsafe.BitCast<UInt128, Decimal128>(left) * Unsafe.BitCast<UInt128, Decimal128>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        public static IEnumerable<object[]> op_Division_TestData()
        {
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // NaN / 1 -> NaN
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // 1 / NaN -> NaN
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // NaN / +Inf -> NaN
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // +Inf / +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // +Inf / -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // -Inf / +Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // -Inf / -Inf -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000) }; // +Inf / 1 -> +Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0xF800000000000000, 0x0000000000000000) }; // +Inf / -1 -> -Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0xF800000000000000, 0x0000000000000000) }; // -Inf / 2 -> -Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000002), new UInt128(0x7800000000000000, 0x0000000000000000) }; // -Inf / -2 -> +Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // +Inf / +0 -> +Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) }; // -Inf / +0 -> -Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) }; // +Inf / -0 -> -Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x0000000000000000, 0x0000000000000000) }; // 1 / +Inf -> +0 (Etiny)
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x8000000000000000, 0x0000000000000000) }; // -1 / +Inf -> -0 (Etiny)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x8000000000000000, 0x0000000000000000) }; // 5 / -Inf -> -0 (Etiny)
            yield return new object[] { new UInt128(0x3054000000000000, 0x0000000000000005), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x0000000000000000, 0x0000000000000000) }; // 5e10 / +Inf -> +0 (Etiny, dividend exp ignored)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x0000000000000000, 0x0000000000000000) }; // +0 / +Inf -> +0 (Etiny)
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x8000000000000000, 0x0000000000000000) }; // -0 / +Inf -> -0 (Etiny)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // 5 / +0 -> +Inf
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) }; // -5 / +0 -> -Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) }; // 5 / -0 -> -Inf
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // -5 / -0 -> +Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // +0 / +0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // -0 / -0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // +0 / -0 -> +QNaN (canonical invalid-operation result)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000000) }; // +0 / 5 -> +0 (ideal exp)
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000000) }; // -0 / 5 -> -0 (ideal exp, sign xor)
            yield return new object[] { new UInt128(0x3044000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3044000000000000, 0x0000000000000000) }; // 0e2 / 5 -> 0e2 (ideal exp)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3046000000000000, 0x0000000000000005), new UInt128(0x303A000000000000, 0x0000000000000000) }; // 0 / 5e3 -> 0e-3 (ideal exp)
            yield return new object[] { new UInt128(0x304A000000000000, 0x0000000000000000), new UInt128(0x3036000000000000, 0x0000000000000005), new UInt128(0x3054000000000000, 0x0000000000000000) }; // 0e5 / 5e-5 -> 0e10 (ideal exp)
            yield return new object[] { new UInt128(0x5FFE000000000000, 0x0000000000000000), new UInt128(0x3036000000000000, 0x0000000000000005), new UInt128(0x5FFE000000000000, 0x0000000000000000) }; // zero dividend, ideal exp far above max quantum (clamped)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x303E000000000000, 0x0000000000000005) }; // 1 / 2 -> 0.5 (exact)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000008), new UInt128(0x303A000000000000, 0x000000000000007D) }; // 1 / 8 -> 0.125 (exact)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000064), new UInt128(0x3040000000000000, 0x0000000000000004), new UInt128(0x3040000000000000, 0x0000000000000019) }; // 100 / 4 -> 25 (exact)
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000005) }; // 10 / 2 -> 5 (exact)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000006), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000002) }; // 6 / 3 -> 2 (exact)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000001) }; // 5 / 5 -> 1 (exact)
            yield return new object[] { new UInt128(0x303C000000000000, 0x0000000000000064), new UInt128(0x3040000000000000, 0x0000000000000004), new UInt128(0x303C000000000000, 0x0000000000000019) }; // 1.00 / 4 -> 0.25 (exact, ideal exp preserved)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000007), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x303E000000000000, 0x0000000000000023) }; // 7 / 2 -> 3.5 (exact)
            yield return new object[] { new UInt128(0xB040000000000000, 0x000000000000000F), new UInt128(0x3040000000000000, 0x0000000000000004), new UInt128(0xB03C000000000000, 0x0000000000000177) }; // -15 / 4 -> -3.75 (exact, sign)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x2FFCA45894E48295, 0x67D9DA2155555555) }; // 1 / 3 -> repeating (round)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x2FFD48B129C9052A, 0xCFB3B442AAAAAAAB) }; // 2 / 3 -> repeating (round)
            yield return new object[] { new UInt128(0x3040000000000000, 0x00000000000F4240), new UInt128(0x3040000000000000, 0x0000000000000007), new UInt128(0x3008466F1B3D5C89, 0x2C81EFC524924925) }; // 1000000 / 7 (round)
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x2FFEA45894E48295, 0x67D9DA2155555555) }; // 10 / 3 -> repeating (round)
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3040000000000000, 0x0000000000000007), new UInt128(0x3040466F1B3D5C89, 0x2C81EFC524924924) }; // all-nines / 7 (round)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x2FBA314DC6448D93, 0x38C15B0A00000000) }; // 1 / all-nines (round)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000007), new UInt128(0x2FFC8CDE367AB912, 0x5903DF8A49249249) }; // 2 / 7 (round)
            yield return new object[] { new UInt128(0x2FFE41BD085B676E, 0xF657240D55555555), new UInt128(0x2FFE57A6B5CF3493, 0xF31EDABC71C71C71), new UInt128(0x2FFD71C74F0225D0, 0x29AA2ACB00000001) }; // full-precision / full-precision (round)
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x2FFE36C831A180DC, 0x77F348B5C71C71C7), new UInt128(0x3082000000000000, 0x0000000000000009) }; // all-nines / near-one full precision (round)
            yield return new object[] { new UInt128(0x5FFFBBBBF868FA2C, 0xFECC335A00000000), new UInt128(0x3036000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000) }; // huge / tiny -> +Inf (overflow)
            yield return new object[] { new UInt128(0xDFFFBBBBF868FA2C, 0xFECC335A00000000), new UInt128(0x3036000000000000, 0x0000000000000001), new UInt128(0xF800000000000000, 0x0000000000000000) }; // -huge / tiny -> -Inf (overflow)
            yield return new object[] { new UInt128(0x0040000000000000, 0x0000000000000001), new UInt128(0x304A000000000000, 0x0000000000000001), new UInt128(0x0036000000000000, 0x0000000000000001) }; // tiny / large -> underflow
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x5FFC000000000000, 0x0000000000000003), new UInt128(0x0040A45894E48295, 0x67D9DA2155555555) }; // 1 / huge -> tiny subnormal (round)
            yield return new object[] { new UInt128(0x0042000000000000, 0x0000000000000001), new UInt128(0x304A000000000000, 0x0000000000000001), new UInt128(0x0038000000000000, 0x0000000000000001) }; // small / large (subnormal-ish)
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7800000000000000, 0x0000000000000000) }; // non-canonical +Inf / 1 -> canonical +Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0xF800000000000000, 0x0000000000000005), new UInt128(0x8000000000000000, 0x0000000000000000) }; // 1 / non-canonical -Inf -> canonical -0 (Etiny)
            yield return new object[] { new UInt128(0x300C000000000000, 0x0386DF690C6EF5BE), new UInt128(0xB018000000000000, 0x00022FCFF31AA0B9), new UInt128(0xAFF6CB90CE781B14, 0x60367827E211020A) };
            yield return new object[] { new UInt128(0x3016000000000000, 0x00000000025C222D), new UInt128(0xAFEE0000105989B6, 0x14E871DF7FB4E7FF), new UInt128(0xAFFD81C848A85A9B, 0x8A95F9D9E2B714E3) };
            yield return new object[] { new UInt128(0xAFE4004BC3F60664, 0xA2D28316972FB16C), new UInt128(0x3024000000000000, 0x336683344D857468), new UInt128(0xAFD64FE830A0E8A9, 0x51A5EFA9F2EE31EA) };
            yield return new object[] { new UInt128(0xB026000000000000, 0x0000000000000009), new UInt128(0xD36C269369438B15, 0x4317B7B7F941F96B), new UInt128(0x0C7838B6B6D1A502, 0x7FE0879DCC18152B) };
            yield return new object[] { new UInt128(0xB030000000000000, 0x00000006C1105459), new UInt128(0xB01E100AA3FBDB37, 0x590C5A8BE72C6FA0), new UInt128(0x2FE3B79655E99EE6, 0xC67B9FCF86368529) };
            yield return new object[] { new UInt128(0x4592000000000000, 0x0125EB92A4B6C61E), new UInt128(0xB03E000000000000, 0x00000001A8B80287), new UInt128(0xC560393E687AE539, 0x7B7E802B3D5289EF) };
            yield return new object[] { new UInt128(0xB018000000000000, 0x000016E8FC21185B), new UInt128(0xB052000000000000, 0x00000000EFF4031C), new UInt128(0x2FCB348106F6193F, 0x6654C91C46250F08) };
            yield return new object[] { new UInt128(0x3020000000000000, 0x0000009AA0434574), new UInt128(0x3048000000000000, 0x0000000000001ED7), new UInt128(0x2FE59EBC2E5756FC, 0x9ACF1030DDA3964B) };
            yield return new object[] { new UInt128(0xB058000000000000, 0x00000000000003DF), new UInt128(0x300C000000000000, 0x000179DCB4BE3FF7), new UInt128(0xB032759A8AB12BE9, 0xCFE4870EFACD0EAF) };
            yield return new object[] { new UInt128(0x9EE405C739C662F4, 0x5752242E1DB1EC20), new UInt128(0x3004000000000000, 0x0000BD66F041ECD5), new UInt128(0x9F0115774EEFB4D0, 0xFE3D466FFB652EAD) };
            yield return new object[] { new UInt128(0xAFE6000119C10D0F, 0xC98968772E7E6EF7), new UInt128(0xB0020000000002DA, 0x7B7ACAB57EE654B5), new UInt128(0x2FEF3F0D3347DCD8, 0x95957D6691C04392) };
            yield return new object[] { new UInt128(0xB00E000000000000, 0x00000000004E6990), new UInt128(0x3852000000000000, 0x000000000000031D), new UInt128(0xA7C13DE5A78B5C8A, 0x03E165F3CC9B7686) };
            yield return new object[] { new UInt128(0x3008000000000000, 0x000000014204609D), new UInt128(0xB020000000000000, 0x000000000000CCF5), new UInt128(0xAFF032C433C139C8, 0xF02601142C32D705) };
            yield return new object[] { new UInt128(0x8462000000000000, 0x00000000EAC878D0), new UInt128(0xB034000000000000, 0x0000000000000110), new UInt128(0x043A476666F4A173, 0x9A4CE8D8AE5A5A5A) };
            yield return new object[] { new UInt128(0xB02400012DE50EC6, 0xBBBA78C47B74A0E0), new UInt128(0x302200000000012F, 0x1F7FF8F741AE5567), new UInt128(0xB00E5262044C38C4, 0xFEF19903FCA8CB97) };
            yield return new object[] { new UInt128(0x3038000000000000, 0x0000000000000057), new UInt128(0xB03C000000000000, 0x000000110FC8CD30), new UInt128(0xAFE83A891034CE03, 0xC2DCA5C65F32C6C8) };
            yield return new object[] { new UInt128(0x3030000000000000, 0x17E95893CB288412), new UInt128(0xC6E2000000000000, 0x52ABCE0A9DE14E35), new UInt128(0x994A8E9AC9234B50, 0x505AA880878F3D7F) };
            yield return new object[] { new UInt128(0x0870000000000000, 0x000000DD250AAC57), new UInt128(0xB00000000000092C, 0x7754F6DC4C1D7D08), new UInt128(0x88586C18C7716200, 0xD6EFD4695899BC44) };
            yield return new object[] { new UInt128(0xB036000000000000, 0x000000130041C7D7), new UInt128(0xB016000000000000, 0x002262E6BEFE8EBF), new UInt128(0x30139FB5EFDD0FB7, 0xF49B951DA6A4F2B4) };
            yield return new object[] { new UInt128(0x3036000000000000, 0x00000001B6848ADB), new UInt128(0x301E000000000000, 0x00000000000A8089), new UInt128(0x301E34B3DBEFDA00, 0x228A6FF58A1CF2E8) };
            yield return new object[] { new UInt128(0xC3AE000000000000, 0x04CF4E8A302A8BDC), new UInt128(0xAFFA00000000002E, 0x3420D6865B6A6E4E), new UInt128(0x43AAC87D34C14517, 0x02E1E1BF053FE3E4) };
            yield return new object[] { new UInt128(0xD258000000000000, 0x000000000091C9EF), new UInt128(0x300A000000000000, 0x0000167364209915), new UInt128(0xD23EBED54CBE4A75, 0x1E37535387AACE6D) };
            yield return new object[] { new UInt128(0x301C000000000000, 0x000000000439E2E9), new UInt128(0xB014000000000032, 0x708772352DE98CF2), new UInt128(0xAFEB77B55CD376ED, 0x4CAC31E2635348AD) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000064), new UInt128(0xB03A000000000000, 0x00003B2E17E3E88B), new UInt128(0x2FEC4BC57A20CF80, 0xFB85C69A5C979594) };
            yield return new object[] { new UInt128(0xB044000000000000, 0x00000000023F5ABE), new UInt128(0xB03A000000006CA7, 0xBAF42939DF4E7BA0), new UInt128(0x2FE76A508107AB67, 0x9C4270C148A575DA) };
            yield return new object[] { new UInt128(0xC6E6000000000000, 0x00000001B8D3E89E), new UInt128(0x301C000000000007, 0x0964D911A0BBD8F4), new UInt128(0xC6B318EB37D2AA50, 0x5FE753A62F2E7B02) };
            yield return new object[] { new UInt128(0x305E000000000000, 0x000000000000A71E), new UInt128(0x3026000000000000, 0x000000795A3C873F), new UInt128(0x302794B3266BDB84, 0x37128AFACE65D3A5) };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x0000000005C867C0), new UInt128(0x302E000000000000, 0x0000000000572AE6), new UInt128(0xAFEC53BC1D980FB6, 0xE421BC3CC6718D7B) };
            yield return new object[] { new UInt128(0xB650000000D0224F, 0xFD6F80D1270DDE07), new UInt128(0xB010000000000000, 0x0000021670F50113), new UInt128(0x365A360BC5384886, 0x1E398FB62F6118F3) };
            yield return new object[] { new UInt128(0xB02C000000000006, 0x909F1381718DE51E), new UInt128(0xB01E00000000002D, 0xCE88CD810A4F6601), new UInt128(0x300A46A940A69240, 0x67EBE25A5A68E21F) };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x00000000000018F2), new UInt128(0x303C000000000000, 0x000000000046CBDD), new UInt128(0xAFD643DC59C6E179, 0x77D217EDE86BABCC) };
            yield return new object[] { new UInt128(0x5F400000000005E8, 0x2C8923BCBDB9FD3E), new UInt128(0xB034000000000000, 0x00000000000C3C2B), new UInt128(0xDF2AAB8556757D28, 0xD46648FBAAFF5F2F) };
            yield return new object[] { new UInt128(0xD95C0000000051B5, 0x62B3BD63621B218F), new UInt128(0xB01E000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB0280000000006F8, 0x435BE30981A1CB95), new UInt128(0x54FE000000000000, 0x00015E8772D1DBC6), new UInt128(0x8B37A50D2C37AC59, 0xD606984662943B6A) };
            yield return new object[] { new UInt128(0x5C72000037D35F7D, 0xF4E8427727658DEF), new UInt128(0x3042000000000000, 0x0001692E610A8ED8), new UInt128(0x5C48D6803B794C4E, 0xA6922295D7A11D64) };
            yield return new object[] { new UInt128(0x305C000000000000, 0x000000000000FE36), new UInt128(0x3014000000000000, 0x000000D1D108B375), new UInt128(0x3037640DA7EB4F3D, 0xF70D116B4CC77265) };
            yield return new object[] { new UInt128(0xB042000000000000, 0x000000000000A932), new UInt128(0x303E000000000000, 0x000002503A034940), new UInt128(0xAFF253F531466EE5, 0x20D5831D0B9F13FC) };
            yield return new object[] { new UInt128(0xB00E000000000000, 0x587FADD919E02966), new UInt128(0x3014000000000000, 0x0721E25C46337978), new UInt128(0xAFFA3D2D2C168F46, 0x26985E7DE8022E37) };
            yield return new object[] { new UInt128(0xAFFA4821D7034A02, 0xB8ACC25DF6F4CF86), new UInt128(0xB044000000000000, 0x00E1766220EE98D3), new UInt128(0x2FD471A975C830ED, 0x0211EE4AF299EBED) };
            yield return new object[] { new UInt128(0x3024000000000000, 0x000000000007C72E), new UInt128(0x1758000000000000, 0x000000041F72D9AA), new UInt128(0x48C08DEE0D62CA1A, 0x64D9A54AF884B677) };
            yield return new object[] { new UInt128(0x301A0000000061C8, 0xEEFC10C4D3DADBE3), new UInt128(0xB0100003A1125F48, 0x953E3B9231F49265), new UInt128(0xAFFC4F2E6E9690B3, 0xDCB74C8FE1EF531E) };
            yield return new object[] { new UInt128(0x301000004BB9B532, 0xE5CB89F41922BD32), new UInt128(0xDE64000000000000, 0x0000CCBC550D3404), new UInt128(0x81C6335464D97738, 0xE074EA1AE02D536C) };
            yield return new object[] { new UInt128(0xB024000000000000, 0x000000000116C396), new UInt128(0x3028000000005351, 0x729900C58D0BD408), new UInt128(0xAFD8E4ED692C9CD4, 0xAE2CE29C4965AF22) };
            yield return new object[] { new UInt128(0x300200000220753C, 0x8CA178C667F5ED50), new UInt128(0xD382000000000000, 0x0A69C7380DBC5B46), new UInt128(0x8C8FB07E9B67A8C7, 0x7F8CF2E324F5ED40) };
            yield return new object[] { new UInt128(0x300C045E788BFD23, 0xFDC576761B96F039), new UInt128(0xB01A000000000000, 0x0AE0ABB2D104776E), new UInt128(0xB00C37BD9345F786, 0x7BCC90D8781F904C) };
            yield return new object[] { new UInt128(0xCD32000000000000, 0x00000062ADC4C04D), new UInt128(0xB040000000000000, 0xD235D4832BF52B34), new UInt128(0x4CE089F3E8481FD9, 0x898A4C67B47BC8DA) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x00017F274F195D94), new UInt128(0x2FFA000000000000, 0x022F53F33F61565B), new UInt128(0xB03E83EE559A4D25, 0xE27D334E7E6D35F9) };
            yield return new object[] { new UInt128(0xB044000000000000, 0x0000000000000046), new UInt128(0x300A00000005E4A6, 0x3DBCC6C875ABB5B6), new UInt128(0xB009E46DEC94605E, 0xC7571D5C27FE5D49) };
            yield return new object[] { new UInt128(0x3066000000000000, 0x0000000000000015), new UInt128(0x3014000000008E7B, 0xA150E858F5A1264C), new UInt128(0x302299E0D48E6734, 0x207E4332183038FA) };
            yield return new object[] { new UInt128(0x21A60000000048E8, 0xDC7FE586D3707752), new UInt128(0xB024000000000023, 0x2E0D31C20845EFD5), new UInt128(0xA1850595706B4AF7, 0x28FE4C6B3E8361ED) };
            yield return new object[] { new UInt128(0xAFF403A1D73F5ACD, 0x96A762C7BAA70840), new UInt128(0x3042000000000000, 0x000000019FF4DE39), new UInt128(0xAFDC340C30509846, 0x39B2D63AA1FE08FB) };
            yield return new object[] { new UInt128(0x303C000000000000, 0x00113925FCD09CDA), new UInt128(0x3000000000007140, 0x43CE4908B69E66F9), new UInt128(0x3029BEEC6622872B, 0x7A151FE09435B4FF) };
            yield return new object[] { new UInt128(0x3004000000000000, 0x00003C65D7E4774B), new UInt128(0x301825B99EFDAE69, 0x303766A98D4C34B5), new UInt128(0x2FC3ABE8CF0915E5, 0xDDD46FB7F4B41926) };
            yield return new object[] { new UInt128(0xB0220000004AE762, 0xF65E345A6F0E69F9), new UInt128(0x301A000000000000, 0x00000000000DC8E9), new UInt128(0xB02E316B8C450FA4, 0x5CB5EF3047A81DAF) };
            yield return new object[] { new UInt128(0x3052000000000000, 0x000000000000002A), new UInt128(0x300000001C3D6C1F, 0x26F3E82E5A4692F0), new UInt128(0x301AECEEFA9A5EDB, 0x6AF2737449986B22) };
            yield return new object[] { new UInt128(0xB048000000000000, 0x0000000000000280), new UInt128(0xAFFC000000000000, 0x0AE997D86F01F581), new UInt128(0x302B914A094EF4D1, 0x6321CF5E46D6E827) };
            yield return new object[] { new UInt128(0xB018000000120CBF, 0x23F4CD9940BD2B9A), new UInt128(0xDFCC005830554042, 0x149E930F0D337664), new UInt128(0x003E99FA6C26C43F, 0x6C3B7206FB91392C) };
            yield return new object[] { new UInt128(0x300E000000000000, 0x0112E59C50FC31A5), new UInt128(0x2FEC16CD5779F833, 0xFD5BB8EB3F0C6FD3), new UInt128(0x3000527D27495963, 0xEE1F2C36110345C1) };
            yield return new object[] { new UInt128(0x3D3E000000000000, 0x00000000E4F7F4D6), new UInt128(0xD270000000000000, 0x00000815ABADD29E), new UInt128(0x9AC4D51105256889, 0x3089FD94A8A3BBBF) };
            yield return new object[] { new UInt128(0x3034000000000000, 0x00181648272743F6), new UInt128(0x569600000002FBFF, 0x4C2693B7C7D3A38C), new UInt128(0x098A5CA6C49E66A5, 0x1E6D169ACA7DC27B) };
        }

        [Theory]
        [MemberData(nameof(op_Division_TestData))]
        public static void op_Division(UInt128 left, UInt128 right, UInt128 expected)
        {
            Decimal128 result = Unsafe.BitCast<UInt128, Decimal128>(left) / Unsafe.BitCast<UInt128, Decimal128>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        public static IEnumerable<object[]> op_Increment_TestData()
        {
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // NaN
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // +Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) }; // -Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000002), new UInt128(0x7800000000000000, 0x0000000000000000) }; // non-canonical +Inf (canonicalizes)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001) }; // +0 -> 1
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001) }; // -0 -> 1
            yield return new object[] { new UInt128(0x304A000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001) }; // 0e5 (preferred exponent min(exp,0))
            yield return new object[] { new UInt128(0x3036000000000000, 0x0000000000000000), new UInt128(0x3036000000000000, 0x00000000000186A0) }; // 0e-5
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000002) }; // 1 -> 2
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000000) }; // -1 -> 0
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000003) }; // 2
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000002), new UInt128(0xB040000000000000, 0x0000000000000001) }; // -2
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x000000000000000B) }; // 10
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000005), new UInt128(0x303E000000000000, 0x000000000000000F) }; // 0.5 -> 1.5
            yield return new object[] { new UInt128(0xB03E000000000000, 0x0000000000000005), new UInt128(0x303E000000000000, 0x0000000000000005) }; // -0.5
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000019), new UInt128(0x303E000000000000, 0x0000000000000023) }; // 2.5
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), new UInt128(0x303E000000000000, 0x000000000000000B) }; // 0.1 -> 1.1
            yield return new object[] { new UInt128(0x303C000000000000, 0x0000000000000177), new UInt128(0x303C000000000000, 0x00000000000001DB) }; // 3.75
            yield return new object[] { new UInt128(0xB03C000000000000, 0x0000000000000019), new UInt128(0x303C000000000000, 0x000000000000004B) }; // -0.25
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000009), new UInt128(0x3040000000000000, 0x000000000000000A) }; // 9 -> 10
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3042314DC6448D93, 0x38C15B0A00000000) }; // all-nines (overflows precision, rounds)
            yield return new object[] { new UInt128(0x3040314DC6448D93, 0x38C15B0A00000000), new UInt128(0x3040314DC6448D93, 0x38C15B0A00000001) }; // 10^(P-1)
            yield return new object[] { new UInt128(0x3040314DC6448D93, 0x38C15B09FFFFFFFF), new UInt128(0x3040314DC6448D93, 0x38C15B0A00000000) }; // (P-1)-nines
            yield return new object[] { new UInt128(0x3088000000000000, 0x0000000000000001), new UInt128(0x3046314DC6448D93, 0x38C15B0A00000000) }; // 1e(P+2) (1 below quantum, no visible change)
            yield return new object[] { new UInt128(0x5FFFBBBBF868FA2C, 0xFECC335A00000000), new UInt128(0x5FFFBBBBF868FA2C, 0xFECC335A00000000) }; // near-max (1 negligible)
            yield return new object[] { new UInt128(0xDFFFBBBBF868FA2C, 0xFECC335A00000000), new UInt128(0xDFFFBBBBF868FA2C, 0xFECC335A00000000) }; // near -max (1 negligible)
            yield return new object[] { new UInt128(0x0040000000000000, 0x0000000000000001), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) }; // tiny positive subnormal (++ ~ 1)
            yield return new object[] { new UInt128(0x8040000000000000, 0x0000000000000001), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) }; // tiny negative subnormal (++ ~ 1)
            yield return new object[] { new UInt128(0x3036000000000000, 0x0000000000000001), new UInt128(0x3036000000000000, 0x00000000000186A1) }; // 1e-5
            yield return new object[] { new UInt128(0x2FFE41BD085B676E, 0xF657240D55555555), new UInt128(0x2FFE730ACE9FF502, 0x2F187F1755555555) }; // 1.333... full precision
            yield return new object[] { new UInt128(0x2FFFED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3000363BF3B1CEEE, 0xBE6E4A8B00000000) }; // 9.999... full precision (carry)
            yield return new object[] { new UInt128(0xB0000000000000B1, 0x4C1E4DF5ED261C68), new UInt128(0x300004EE2D6D40AA, 0x398EA18B12D9E398) };
            yield return new object[] { new UInt128(0xB02C000000008117, 0x952633E9760D3996), new UInt128(0xB02C000000008117, 0x952633E722015596) };
            yield return new object[] { new UInt128(0xAFF80000000000BC, 0x57570583228BEDE5), new UInt128(0x2FFDED09BEAD87BE, 0x556649372B2CAE43) };
            yield return new object[] { new UInt128(0x27C800000029E4B4, 0x709EF360CF7CC897), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0x3062000000000000, 0x0000000000000006), new UInt128(0x3040000000000000, 0x0853A0D2313C0001) };
            yield return new object[] { new UInt128(0xB02C000000000000, 0x0000467047DBE843), new UInt128(0xB02C000000000000, 0x0000466DF3D00443) };
            yield return new object[] { new UInt128(0x301204EA7A9BDA00, 0xB6F5E4F86A521036), new UInt128(0x301204EA7A9BEF2D, 0xB9BDC64360D21036) };
            yield return new object[] { new UInt128(0x30020000000085EF, 0xF5A15252168838A9), new UInt128(0x3002007E37BEA612, 0xB6329D78968838A9) };
            yield return new object[] { new UInt128(0x3018000000000000, 0x5DF1A27C94EB2D4F), new UInt128(0x3018000000000005, 0xC9B900A9F7FB2D4F) };
            yield return new object[] { new UInt128(0x304C000000000000, 0x0000000000000009), new UInt128(0x3040000000000000, 0x0000000000895441) };
            yield return new object[] { new UInt128(0xB0180000003AF28B, 0xE740E213A5668D80), new UInt128(0xB0180000003AF286, 0x7B7983E642568D80) };
            yield return new object[] { new UInt128(0xB01E000000000000, 0x0000000616E4354B), new UInt128(0x301E000000000000, 0x0163457246A5CAB5) };
            yield return new object[] { new UInt128(0x3024000000000000, 0x0000000000000005), new UInt128(0x3024000000000000, 0x00005AF3107A4005) };
            yield return new object[] { new UInt128(0xCA2C00012586FEF1, 0xA857CDDF6237FE18), new UInt128(0xCA23BFE31CC37D7E, 0xFA82FB3EB0175F00) };
            yield return new object[] { new UInt128(0xB014000000000000, 0x00000000293EF566), new UInt128(0x301400000000021E, 0x19E0C9BA89010A9A) };
            yield return new object[] { new UInt128(0x81D4000000000000, 0x5D3FA1D19308E202), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0xB038000000000000, 0x0000000004FDAAAC), new UInt128(0xB038000000000000, 0x0000000004FD839C) };
            yield return new object[] { new UInt128(0xB6D2000000000000, 0x000000000000001D), new UInt128(0xB6928EFB2560675E, 0x2497219D00000000) };
            yield return new object[] { new UInt128(0x3026000101B0C4EA, 0xAB917F63F245E3FF), new UInt128(0x3026000101B0C4EA, 0xAB91887C40B883FF) };
            yield return new object[] { new UInt128(0xB0140000BC4F01DB, 0x04D54985FC8DE0DB), new UInt128(0xB0140000BC4EFFBC, 0xEAF47FCB4A4DE0DB) };
            yield return new object[] { new UInt128(0x3008000000000000, 0x000007AFAEE4D58A), new UInt128(0x30080000204FCE5E, 0x3E250A10BEE4D58A) };
            yield return new object[] { new UInt128(0x84840000C3B35E24, 0xD944C9B454C06345), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0xD9EE000000000000, 0x000000000000005A), new UInt128(0xD9AFBBBBF868FA2C, 0xFECC335A00000000) };
            yield return new object[] { new UInt128(0x2FF6000000000007, 0x1BF19A20781F7D89), new UInt128(0x2FFE314DC6448D93, 0x38EFF238F422346A) };
            yield return new object[] { new UInt128(0x301C0000086A265B, 0x3BC9B1C066BAF9CB), new UInt128(0x301C0000086A265B, 0x49AA68740E1EF9CB) };
            yield return new object[] { new UInt128(0x3030000000000000, 0x000000002A8CA672), new UInt128(0x3030000000000000, 0x0000000030828772) };
            yield return new object[] { new UInt128(0x2FF229E50E8F6A9F, 0xAE76568536C55686), new UInt128(0x2FFE314DC9036E2B, 0xC0531A6B0824E929) };
            yield return new object[] { new UInt128(0x4BF6000000000000, 0x0000000000015DFE), new UInt128(0x4BBDB9C093345A45, 0x21A2D72AC0000000) };
            yield return new object[] { new UInt128(0x3010000001BB3680, 0x155F5B37B10809BD), new UInt128(0x3010000001BC0A42, 0x312E2825520809BD) };
            yield return new object[] { new UInt128(0xAFF8000000000056, 0xAD502213EE481751), new UInt128(0x2FFDED09BEAD87BF, 0x59A8EA35B87F4772) };
            yield return new object[] { new UInt128(0x2FF20060F3D32F96, 0xC08CDAE35E8F6169), new UInt128(0x2FFE314DC64AE82A, 0xC92FA2EF72CB9C9E) };
            yield return new object[] { new UInt128(0x301C000000000000, 0x0A29F51D6081C988), new UInt128(0x301C000000000000, 0x180AABD107E5C988) };
            yield return new object[] { new UInt128(0xAFFA000000046488, 0x16C345A9670E0BDD), new UInt128(0x2FFDED09BEAD174C, 0x3546D43975B1CB9D) };
            yield return new object[] { new UInt128(0x2CFA000000000000, 0x00000000003305E2), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0x9646000000F92EA4, 0x0C7335403F583DB1), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0xB02C005606ABA329, 0xA60456ECE8C0B9E7), new UInt128(0xB02C005606ABA329, 0xA60456EA94B4D5E7) };
            yield return new object[] { new UInt128(0xB05E000000000000, 0x0000000000000014), new UInt128(0xB040000000000000, 0x00470DE4DF81FFFF) };
            yield return new object[] { new UInt128(0x300A000000000000, 0x004433E77852218C), new UInt128(0x300A0000033B2E3C, 0xA014B4246052218C) };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x0000000000000003), new UInt128(0x301A000000000000, 0x8AC7230489E7FFFD) };
            yield return new object[] { new UInt128(0x303A000000000000, 0x0132602F948B8E0D), new UInt128(0x303A000000000000, 0x0132602F948B91F5) };
            yield return new object[] { new UInt128(0xB00000000016390D, 0xBD0D18E1CAF1E112), new UInt128(0x300004EE2D57084D, 0xC89FD69F350E1EEE) };
            yield return new object[] { new UInt128(0x301A000000000000, 0x00000793A637ABF6), new UInt128(0x301A000000000000, 0x8AC72A98301FABF6) };
            yield return new object[] { new UInt128(0x8EA6000000000000, 0x0000000000000037), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0xB016000000000175, 0x8A684F948BA6A804), new UInt128(0xB01600000000013F, 0x549EA1CEAD06A804) };
            yield return new object[] { new UInt128(0xB0260000C03688D2, 0x7AC2450F1321F8C8), new UInt128(0xB0260000C03688D2, 0x7AC23BF6C4AF58C8) };
            yield return new object[] { new UInt128(0x5402000000000000, 0x0000000CF771D12A), new UInt128(0x53D51293F938DCC4, 0xF7A5780AF1000000) };
            yield return new object[] { new UInt128(0xB00404F6A0D84B65, 0x900E7A5542B364B4), new UInt128(0xB00404EA01ABAE95, 0x49998C6B02B364B4) };
            yield return new object[] { new UInt128(0x3006000000000000, 0x07737E4214E44C17), new UInt128(0x30060001431E0FAE, 0x74E5960CB4E44C17) };
            yield return new object[] { new UInt128(0xB02C00013631CF73, 0x2562CDE5A526311A), new UInt128(0xB02C00013631CF73, 0x2562CDE3511A4D1A) };
            yield return new object[] { new UInt128(0x3000000000000000, 0x000DC3F18ACD96FF), new UInt128(0x300004EE2D6D415B, 0x85BAB3728ACD96FF) };
            yield return new object[] { new UInt128(0xB05A000000000000, 0x000000000000002F), new UInt128(0xB040000000000000, 0x0001AB76670B5FFF) };
            yield return new object[] { new UInt128(0x56600000000000DA, 0xB478C74AE7160E75), new UInt128(0x5648C6E937EC9E0B, 0x7316FDA996505000) };
            yield return new object[] { new UInt128(0x30CA000000000000, 0x027CB1C80FA6BE2D), new UInt128(0x30AA585BEE5EDC33, 0xD9EF6DB4E2ED0000) };
            yield return new object[] { new UInt128(0x30120000000002CF, 0x9740029D9E1DA392), new UInt128(0x30120000000017FC, 0x9A07E3E8949DA392) };
            yield return new object[] { new UInt128(0x30111C665687A600, 0x659DFC592140D366), new UInt128(0x30111C66568879C2, 0x816CC946C240D366) };
            yield return new object[] { new UInt128(0x3048000000000000, 0x00000000001799B2), new UInt128(0x3040000000000000, 0x0000000399E3B921) };
            yield return new object[] { new UInt128(0x9028000000000026, 0x8DD5910A10EB5AFD), new UInt128(0x2FFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0xAFEA00008584F9E7, 0xA777461B117E33B1), new UInt128(0x2FFDED09BEAD87BD, 0xFA17619EAFF9BE0F) };
            yield return new object[] { new UInt128(0xB03A000000000000, 0x00033EF697D6BABE), new UInt128(0xB03A000000000000, 0x00033EF697D6B6D6) };
            yield return new object[] { new UInt128(0x3032000000000000, 0x000049D9BB8E8CD9), new UInt128(0x3032000000000000, 0x000049D9BC272359) };
        }

        [Theory]
        [MemberData(nameof(op_Increment_TestData))]
        public static void op_Increment(UInt128 value, UInt128 expected)
        {
            Decimal128 result = Unsafe.BitCast<UInt128, Decimal128>(value);
            result++;
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        public static IEnumerable<object[]> op_Decrement_TestData()
        {
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) }; // NaN
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // +Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) }; // -Inf
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000002), new UInt128(0x7800000000000000, 0x0000000000000000) }; // non-canonical +Inf (canonicalizes)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000001) }; // +0 -> -1
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000001) }; // -0 -> -1
            yield return new object[] { new UInt128(0x304A000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000001) }; // 0e5 (preferred exponent min(exp,0))
            yield return new object[] { new UInt128(0x3036000000000000, 0x0000000000000000), new UInt128(0xB036000000000000, 0x00000000000186A0) }; // 0e-5
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000000) }; // 1 -> 0
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0xB040000000000000, 0x0000000000000002) }; // -1 -> -2
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000001) }; // 2
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000002), new UInt128(0xB040000000000000, 0x0000000000000003) }; // -2
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000009) }; // 10
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000005), new UInt128(0xB03E000000000000, 0x0000000000000005) }; // 0.5 -> -0.5
            yield return new object[] { new UInt128(0xB03E000000000000, 0x0000000000000005), new UInt128(0xB03E000000000000, 0x000000000000000F) }; // -0.5
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000019), new UInt128(0x303E000000000000, 0x000000000000000F) }; // 2.5
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), new UInt128(0xB03E000000000000, 0x0000000000000009) }; // 0.1 -> -0.9
            yield return new object[] { new UInt128(0x303C000000000000, 0x0000000000000177), new UInt128(0x303C000000000000, 0x0000000000000113) }; // 3.75
            yield return new object[] { new UInt128(0xB03C000000000000, 0x0000000000000019), new UInt128(0xB03C000000000000, 0x000000000000007D) }; // -0.25
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000009), new UInt128(0x3040000000000000, 0x0000000000000008) }; // 9 -> 8
            yield return new object[] { new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x3041ED09BEAD87C0, 0x378D8E63FFFFFFFE) }; // all-nines (decrement in last place)
            yield return new object[] { new UInt128(0x3040314DC6448D93, 0x38C15B0A00000000), new UInt128(0x3040314DC6448D93, 0x38C15B09FFFFFFFF) }; // 10^(P-1)
            yield return new object[] { new UInt128(0x3040314DC6448D93, 0x38C15B09FFFFFFFF), new UInt128(0x3040314DC6448D93, 0x38C15B09FFFFFFFE) }; // (P-1)-nines
            yield return new object[] { new UInt128(0x3088000000000000, 0x0000000000000001), new UInt128(0x3046314DC6448D93, 0x38C15B0A00000000) }; // 1e(P+2) (1 below quantum, no visible change)
            yield return new object[] { new UInt128(0x5FFFBBBBF868FA2C, 0xFECC335A00000000), new UInt128(0x5FFFBBBBF868FA2C, 0xFECC335A00000000) }; // near-max (1 negligible)
            yield return new object[] { new UInt128(0xDFFFBBBBF868FA2C, 0xFECC335A00000000), new UInt128(0xDFFFBBBBF868FA2C, 0xFECC335A00000000) }; // near -max (1 negligible)
            yield return new object[] { new UInt128(0x0040000000000000, 0x0000000000000001), new UInt128(0xAFFE314DC6448D93, 0x38C15B0A00000000) }; // tiny positive subnormal (-- ~ -1)
            yield return new object[] { new UInt128(0x8040000000000000, 0x0000000000000001), new UInt128(0xAFFE314DC6448D93, 0x38C15B0A00000000) }; // tiny negative subnormal (-- ~ -1)
            yield return new object[] { new UInt128(0x3036000000000000, 0x0000000000000001), new UInt128(0xB036000000000000, 0x000000000001869F) }; // 1e-5
            yield return new object[] { new UInt128(0x2FFE41BD085B676E, 0xF657240D55555555), new UInt128(0x2FFE106F4216D9DB, 0xBD95C90355555555) }; // 1.333... full precision
            yield return new object[] { new UInt128(0x2FFFED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x2FFFBBBBF868FA2C, 0xFECC3359FFFFFFFF) }; // 9.999... full precision
            yield return new object[] { new UInt128(0xB0000000000000B1, 0x4C1E4DF5ED261C68), new UInt128(0xB00004EE2D6D420C, 0xD1CB3D76ED261C68) };
            yield return new object[] { new UInt128(0xB02C000000008117, 0x952633E9760D3996), new UInt128(0xB02C000000008117, 0x952633EBCA191D96) };
            yield return new object[] { new UInt128(0xAFF80000000000BC, 0x57570583228BEDE5), new UInt128(0xAFFE314DC6448D93, 0x68F87B8E7BAEBB60) };
            yield return new object[] { new UInt128(0x27C800000029E4B4, 0x709EF360CF7CC897), new UInt128(0xAFFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0x3062000000000000, 0x0000000000000006), new UInt128(0x3040000000000000, 0x0853A0D2313BFFFF) };
            yield return new object[] { new UInt128(0xB02C000000000000, 0x0000467047DBE843), new UInt128(0xB02C000000000000, 0x000046729BE7CC43) };
            yield return new object[] { new UInt128(0x301204EA7A9BDA00, 0xB6F5E4F86A521036), new UInt128(0x301204EA7A9BC4D3, 0xB42E03AD73D21036) };
            yield return new object[] { new UInt128(0x30020000000085EF, 0xF5A15252168838A9), new UInt128(0xB002007E37BD9A32, 0xCAEFF8D46977C757) };
            yield return new object[] { new UInt128(0x3018000000000000, 0x5DF1A27C94EB2D4F), new UInt128(0xB018000000000005, 0x0DD5BBB0CE24D2B1) };
            yield return new object[] { new UInt128(0x304C000000000000, 0x0000000000000009), new UInt128(0x3040000000000000, 0x000000000089543F) };
            yield return new object[] { new UInt128(0xB0180000003AF28B, 0xE740E213A5668D80), new UInt128(0xB0180000003AF291, 0x5308404108768D80) };
            yield return new object[] { new UInt128(0xB01E000000000000, 0x0000000616E4354B), new UInt128(0xB01E000000000000, 0x0163457E746E354B) };
            yield return new object[] { new UInt128(0x3024000000000000, 0x0000000000000005), new UInt128(0xB024000000000000, 0x00005AF3107A3FFB) };
            yield return new object[] { new UInt128(0xCA2C00012586FEF1, 0xA857CDDF6237FE18), new UInt128(0xCA23BFE31CC37D7E, 0xFA82FB3EB0175F00) };
            yield return new object[] { new UInt128(0xB014000000000000, 0x00000000293EF566), new UInt128(0xB01400000000021E, 0x19E0C9BADB7EF566) };
            yield return new object[] { new UInt128(0x81D4000000000000, 0x5D3FA1D19308E202), new UInt128(0xAFFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0xB038000000000000, 0x0000000004FDAAAC), new UInt128(0xB038000000000000, 0x0000000004FDD1BC) };
            yield return new object[] { new UInt128(0xB6D2000000000000, 0x000000000000001D), new UInt128(0xB6928EFB2560675E, 0x2497219D00000000) };
            yield return new object[] { new UInt128(0x3026000101B0C4EA, 0xAB917F63F245E3FF), new UInt128(0x3026000101B0C4EA, 0xAB91764BA3D343FF) };
            yield return new object[] { new UInt128(0xB0140000BC4F01DB, 0x04D54985FC8DE0DB), new UInt128(0xB0140000BC4F03F9, 0x1EB61340AECDE0DB) };
            yield return new object[] { new UInt128(0x3008000000000000, 0x000007AFAEE4D58A), new UInt128(0xB0080000204FCE5E, 0x3E24FAB1611B2A76) };
            yield return new object[] { new UInt128(0x84840000C3B35E24, 0xD944C9B454C06345), new UInt128(0xAFFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0xD9EE000000000000, 0x000000000000005A), new UInt128(0xD9AFBBBBF868FA2C, 0xFECC335A00000000) };
            yield return new object[] { new UInt128(0x2FF6000000000007, 0x1BF19A20781F7D89), new UInt128(0xAFFDED09BEAD87C0, 0x35BBA68E76A9F3D8) };
            yield return new object[] { new UInt128(0x301C0000086A265B, 0x3BC9B1C066BAF9CB), new UInt128(0x301C0000086A265B, 0x2DE8FB0CBF56F9CB) };
            yield return new object[] { new UInt128(0x3030000000000000, 0x000000002A8CA672), new UInt128(0x3030000000000000, 0x000000002496C572) };
            yield return new object[] { new UInt128(0x2FF229E50E8F6A9F, 0xAE76568536C55686), new UInt128(0xAFFDED09A338C1CA, 0xEBDC1499AE8EE461) };
            yield return new object[] { new UInt128(0x4BF6000000000000, 0x0000000000015DFE), new UInt128(0x4BBDB9C093345A45, 0x21A2D72AC0000000) };
            yield return new object[] { new UInt128(0x3010000001BB3680, 0x155F5B37B10809BD), new UInt128(0x3010000001BA62BD, 0xF9908E4A100809BD) };
            yield return new object[] { new UInt128(0xAFF8000000000056, 0xAD502213EE481751), new UInt128(0xAFFE314DC6448D93, 0x4EF1D1DB6D8CDF41) };
            yield return new object[] { new UInt128(0x2FF20060F3D32F96, 0xC08CDAE35E8F6169), new UInt128(0xAFFDED09BE6DFDD4, 0x933EBF6D840BE1D0) };
            yield return new object[] { new UInt128(0x301C000000000000, 0x0A29F51D6081C988), new UInt128(0xB01C000000000000, 0x03B6C19646E23678) };
            yield return new object[] { new UInt128(0xAFFA000000046488, 0x16C345A9670E0BDD), new UInt128(0xAFFE314DC64498D2, 0x05C86DA7DAA16BA3) };
            yield return new object[] { new UInt128(0x2CFA000000000000, 0x00000000003305E2), new UInt128(0xAFFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0x9646000000F92EA4, 0x0C7335403F583DB1), new UInt128(0xAFFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0xB02C005606ABA329, 0xA60456ECE8C0B9E7), new UInt128(0xB02C005606ABA329, 0xA60456EF3CCC9DE7) };
            yield return new object[] { new UInt128(0xB05E000000000000, 0x0000000000000014), new UInt128(0xB040000000000000, 0x00470DE4DF820001) };
            yield return new object[] { new UInt128(0x300A000000000000, 0x004433E77852218C), new UInt128(0xB00A0000033B2E3C, 0x9F8C4C556FADDE74) };
            yield return new object[] { new UInt128(0xB01A000000000000, 0x0000000000000003), new UInt128(0xB01A000000000000, 0x8AC7230489E80003) };
            yield return new object[] { new UInt128(0x303A000000000000, 0x0132602F948B8E0D), new UInt128(0x303A000000000000, 0x0132602F948B8A25) };
            yield return new object[] { new UInt128(0xB00000000016390D, 0xBD0D18E1CAF1E112), new UInt128(0xB00004EE2D837A69, 0x42BA0862CAF1E112) };
            yield return new object[] { new UInt128(0x301A000000000000, 0x00000793A637ABF6), new UInt128(0xB01A000000000000, 0x8AC71B70E3B0540A) };
            yield return new object[] { new UInt128(0x8EA6000000000000, 0x0000000000000037), new UInt128(0xAFFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0xB016000000000175, 0x8A684F948BA6A804), new UInt128(0xB0160000000001AB, 0xC031FD5A6A46A804) };
            yield return new object[] { new UInt128(0xB0260000C03688D2, 0x7AC2450F1321F8C8), new UInt128(0xB0260000C03688D2, 0x7AC24E27619498C8) };
            yield return new object[] { new UInt128(0x5402000000000000, 0x0000000CF771D12A), new UInt128(0x53D51293F938DCC4, 0xF7A5780AF1000000) };
            yield return new object[] { new UInt128(0xB00404F6A0D84B65, 0x900E7A5542B364B4), new UInt128(0xB00405034004E835, 0xD683683F82B364B4) };
            yield return new object[] { new UInt128(0x3006000000000000, 0x07737E4214E44C17), new UInt128(0xB0060001431E0FAE, 0x65FE99888B1BB3E9) };
            yield return new object[] { new UInt128(0xB02C00013631CF73, 0x2562CDE5A526311A), new UInt128(0xB02C00013631CF73, 0x2562CDE7F932151A) };
            yield return new object[] { new UInt128(0x3000000000000000, 0x000DC3F18ACD96FF), new UInt128(0xB00004EE2D6D415B, 0x859F2B8F75326901) };
            yield return new object[] { new UInt128(0xB05A000000000000, 0x000000000000002F), new UInt128(0xB040000000000000, 0x0001AB76670B6001) };
            yield return new object[] { new UInt128(0x56600000000000DA, 0xB478C74AE7160E75), new UInt128(0x5648C6E937EC9E0B, 0x7316FDA996505000) };
            yield return new object[] { new UInt128(0x30CA000000000000, 0x027CB1C80FA6BE2D), new UInt128(0x30AA585BEE5EDC33, 0xD9EF6DB4E2ED0000) };
            yield return new object[] { new UInt128(0x30120000000002CF, 0x9740029D9E1DA392), new UInt128(0xB01200000000125D, 0x6B87DEAD58625C6E) };
            yield return new object[] { new UInt128(0x30111C665687A600, 0x659DFC592140D366), new UInt128(0x30111C665686D23E, 0x49CF2F6B8040D366) };
            yield return new object[] { new UInt128(0x3048000000000000, 0x00000000001799B2), new UInt128(0x3040000000000000, 0x0000000399E3B91F) };
            yield return new object[] { new UInt128(0x9028000000000026, 0x8DD5910A10EB5AFD), new UInt128(0xAFFE314DC6448D93, 0x38C15B0A00000000) };
            yield return new object[] { new UInt128(0xAFEA00008584F9E7, 0xA777461B117E33B1), new UInt128(0xAFFE314DC6448D93, 0x7219F91DBB33D365) };
            yield return new object[] { new UInt128(0xB03A000000000000, 0x00033EF697D6BABE), new UInt128(0xB03A000000000000, 0x00033EF697D6BEA6) };
            yield return new object[] { new UInt128(0x3032000000000000, 0x000049D9BB8E8CD9), new UInt128(0x3032000000000000, 0x000049D9BAF5F659) };
        }

        [Theory]
        [MemberData(nameof(op_Decrement_TestData))]
        public static void op_Decrement(UInt128 value, UInt128 expected)
        {
            Decimal128 result = Unsafe.BitCast<UInt128, Decimal128>(value);
            result--;
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        public static IEnumerable<object[]> Parse_AllowTrailingInvalidCharacters_TestData()
        {
            NumberStyles style = NumberStyles.Float | NumberStyles.AllowTrailingInvalidCharacters;

            // Trailing invalid characters after a valid number
            yield return new object[] { "123abc", style, CultureInfo.InvariantCulture, Decimal128.Parse("123", CultureInfo.InvariantCulture), 3 };
            yield return new object[] { "12.5xyz", style, CultureInfo.InvariantCulture, Decimal128.Parse("12.5", CultureInfo.InvariantCulture), 4 };
            yield return new object[] { "+7e2!!", style, CultureInfo.InvariantCulture, Decimal128.Parse("7e2", CultureInfo.InvariantCulture), 4 };
            yield return new object[] { "-8.0#", style, CultureInfo.InvariantCulture, Decimal128.Parse("-8.0", CultureInfo.InvariantCulture), 4 };

            // No trailing invalid characters
            yield return new object[] { "123", style, CultureInfo.InvariantCulture, Decimal128.Parse("123", CultureInfo.InvariantCulture), 3 };

            // Special values with trailing invalid characters
            yield return new object[] { "Infinityabc", style, CultureInfo.InvariantCulture, Decimal128.PositiveInfinity, 8 };
            yield return new object[] { "-Infinityxyz", style, CultureInfo.InvariantCulture, Decimal128.NegativeInfinity, 9 };
            yield return new object[] { "NaNabc", style, CultureInfo.InvariantCulture, Decimal128.NaN, 3 };

            // Special values always consume surrounding whitespace (independent of AllowLeadingWhite/AllowTrailingWhite) before stopping on the first non-whitespace invalid character
            yield return new object[] { "Infinity   ", style, CultureInfo.InvariantCulture, Decimal128.PositiveInfinity, 11 };
            yield return new object[] { "Infinity  x", style, CultureInfo.InvariantCulture, Decimal128.PositiveInfinity, 10 };
            yield return new object[] { "+Infinity  x", style, CultureInfo.InvariantCulture, Decimal128.PositiveInfinity, 11 };
            yield return new object[] { "-Infinity  x", style, CultureInfo.InvariantCulture, Decimal128.NegativeInfinity, 11 };
            yield return new object[] { "NaN  x", style, CultureInfo.InvariantCulture, Decimal128.NaN, 5 };

            // AllowTrailingWhite has no effect on special values; the surrounding whitespace is still consumed
            yield return new object[] { "Infinity  x", (NumberStyles.Float & ~NumberStyles.AllowTrailingWhite) | NumberStyles.AllowTrailingInvalidCharacters, CultureInfo.InvariantCulture, Decimal128.PositiveInfinity, 10 };
        }

        [Theory]
        [MemberData(nameof(Parse_AllowTrailingInvalidCharacters_TestData))]
        public static void Parse_AllowTrailingInvalidCharacters(string value, NumberStyles style, IFormatProvider provider, Decimal128 expected, int expectedCharsConsumed)
        {
            Assert.True(Decimal128.TryParse(value, style, provider, out Decimal128 result, out int charsConsumed));
            Assert.Equal(expected, result);
            Assert.Equal(expectedCharsConsumed, charsConsumed);

            Assert.True(Decimal128.TryParse(value.AsSpan(), style, provider, out result, out charsConsumed));
            Assert.Equal(expected, result);
            Assert.Equal(expectedCharsConsumed, charsConsumed);

            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);
            Assert.True(Decimal128.TryParse(utf8Bytes.AsSpan(), style, provider, out result, out int bytesConsumed));
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
            Assert.False(Decimal128.TryParse(value, style, provider, out Decimal128 result, out int charsConsumed));
            Assert.Equal(0, charsConsumed);

            Assert.False(Decimal128.TryParse(value.AsSpan(), style, provider, out result, out charsConsumed));
            Assert.Equal(0, charsConsumed);

            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);
            Assert.False(Decimal128.TryParse(utf8Bytes.AsSpan(), style, provider, out result, out int bytesConsumed));
            Assert.Equal(0, bytesConsumed);
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128Arithmetic), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void op_Arithmetic_IntelReferenceVectors(string operation, UInt128 left, UInt128 right, UInt128 expected)
        {
            Decimal128 l = Unsafe.BitCast<UInt128, Decimal128>(left);
            Decimal128 r = Unsafe.BitCast<UInt128, Decimal128>(right);

            Decimal128 result = operation switch
            {
                "add" => l + r,
                "sub" => l - r,
                "mul" => l * r,
                "div" => l / r,
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128Comparison), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void op_Comparison_IntelReferenceVectors(string operation, UInt128 left, UInt128 right, bool expected)
        {
            Decimal128 l = Unsafe.BitCast<UInt128, Decimal128>(left);
            Decimal128 r = Unsafe.BitCast<UInt128, Decimal128>(right);

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
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128Unary), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void UnaryOperation_IntelReferenceVectors(string operation, UInt128 value, UInt128 expected)
        {
            Decimal128 v = Unsafe.BitCast<UInt128, Decimal128>(value);

            Decimal128 result = operation switch
            {
                "abs" => Decimal128.Abs(v),
                "negate" => -v,
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128BinaryValue), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void BinaryValueOperation_IntelReferenceVectors(string operation, UInt128 left, UInt128 right, UInt128 expected)
        {
            Decimal128 l = Unsafe.BitCast<UInt128, Decimal128>(left);
            Decimal128 r = Unsafe.BitCast<UInt128, Decimal128>(right);

            Decimal128 result = operation switch
            {
                "copySign" => Decimal128.CopySign(l, r),
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128Predicate), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void Predicate_IntelReferenceVectors(string operation, UInt128 value, bool expected)
        {
            Decimal128 v = Unsafe.BitCast<UInt128, Decimal128>(value);

            bool result = operation switch
            {
                "isNaN" => Decimal128.IsNaN(v),
                "isInf" => Decimal128.IsInfinity(v),
                "isFinite" => Decimal128.IsFinite(v),
                "isSigned" => Decimal128.IsNegative(v),
                "isNormal" => Decimal128.IsNormal(v),
                "isSubnormal" => Decimal128.IsSubnormal(v),
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            Assert.Equal(expected, result);
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal(new UInt128(0x3040000000000000, 0x0000000000000001), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.One));
        }

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(new UInt128(0xB040000000000000, 0x0000000000000001), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.NegativeOne));
        }

        [Fact]
        public static void ETest()
        {
            Assert.Equal(new UInt128(0x2FFE86058A4BF4DE, 0x4E906ACCB26ABB56), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.E)); // +2.718281828459045235360287471352662
        }

        [Fact]
        public static void PiTest()
        {
            Assert.Equal(new UInt128(0x2FFE9AE4795796A7, 0xBABE5564E6F39F8F), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Pi)); // +3.141592653589793238462643383279503
        }

        [Fact]
        public static void TauTest()
        {
            Assert.Equal(new UInt128(0x2FFF35C8F2AF2D4F, 0x757CAAC9CDE73F1E), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Tau)); // +6.283185307179586476925286766559006
        }

        public static IEnumerable<object[]> Abs_TestData()
        {
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // Abs(NaN)
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // Abs(-NaN)
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // Abs(+Inf)
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) }; // Abs(-Inf)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) }; // Abs(0)
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) }; // Abs(-0)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000001) }; // Abs(1)
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000001) }; // Abs(-1)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000002) }; // Abs(2)
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000002) }; // Abs(-2)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) }; // Abs(3)
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) }; // Abs(-3)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) }; // Abs(5)
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) }; // Abs(-5)
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x000000000000000A) }; // Abs(10)
            yield return new object[] { new UInt128(0xB040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x000000000000000A) }; // Abs(-10)
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000005), new UInt128(0x303E000000000000, 0x0000000000000005) }; // Abs(0.5)
            yield return new object[] { new UInt128(0xB03E000000000000, 0x0000000000000005), new UInt128(0x303E000000000000, 0x0000000000000005) }; // Abs(-0.5)
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000019), new UInt128(0x303E000000000000, 0x0000000000000019) }; // Abs(2.5)
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000F), new UInt128(0x303E000000000000, 0x000000000000000F) }; // Abs(1.5)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000064), new UInt128(0x3040000000000000, 0x0000000000000064) }; // Abs(100)
            yield return new object[] { new UInt128(0x3040000000000000, 0x00000000000003E8), new UInt128(0x3040000000000000, 0x00000000000003E8) }; // Abs(1000)
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), new UInt128(0x303E000000000000, 0x0000000000000001) }; // Abs(0.1)
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000009), new UInt128(0x3040000000000000, 0x0000000000000009) }; // Abs(9)
            yield return new object[] { new UInt128(0x3046000000000000, 0x0000000000000001), new UInt128(0x3046000000000000, 0x0000000000000001) }; // Abs(1E+3)
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), new UInt128(0x303E000000000000, 0x000000000000000A) }; // Abs(1.0)
            yield return new object[] { new UInt128(0x303C000000000000, 0x00000000000000C8), new UInt128(0x303C000000000000, 0x00000000000000C8) }; // Abs(2.00)
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), new UInt128(0x3042000000000000, 0x0000000000000002) }; // Abs(2E+1)
            yield return new object[] { new UInt128(0x0000000000000000, 0x0000000000000001), new UInt128(0x0000000000000000, 0x0000000000000001) }; // Abs(epsilon)
            yield return new object[] { new UInt128(0x8000000000000000, 0x0000000000000001), new UInt128(0x0000000000000000, 0x0000000000000001) }; // Abs(-epsilon)
            yield return new object[] { new UInt128(0x0000314DC6448D93, 0x38C15B09FFFFFFFF), new UInt128(0x0000314DC6448D93, 0x38C15B09FFFFFFFF) }; // Abs(largest_subnormal)
            yield return new object[] { new UInt128(0x8000314DC6448D93, 0x38C15B09FFFFFFFF), new UInt128(0x0000314DC6448D93, 0x38C15B09FFFFFFFF) }; // Abs(-largest_subnormal)
            yield return new object[] { new UInt128(0x0042000000000000, 0x0000000000000001), new UInt128(0x0042000000000000, 0x0000000000000001) }; // Abs(smallest_normal)
            yield return new object[] { new UInt128(0x8042000000000000, 0x0000000000000001), new UInt128(0x0042000000000000, 0x0000000000000001) }; // Abs(-smallest_normal)
            yield return new object[] { new UInt128(0x5FFFED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x5FFFED09BEAD87C0, 0x378D8E63FFFFFFFF) }; // Abs(maxvalue)
            yield return new object[] { new UInt128(0xDFFFED09BEAD87C0, 0x378D8E63FFFFFFFF), new UInt128(0x5FFFED09BEAD87C0, 0x378D8E63FFFFFFFF) }; // Abs(-maxvalue)
        }

        [Theory]
        [MemberData(nameof(Abs_TestData))]
        public static void AbsTest(UInt128 value, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Abs(Unsafe.BitCast<UInt128, Decimal128>(value))));
        }

        public static IEnumerable<object[]> Classification_TestData()
        {
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), false, false, true, false, true, false, false, false }; // NaN
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), false, false, true, true, false, false, false, false }; // -NaN
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), false, true, false, false, true, false, true, true }; // +Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), false, true, false, true, false, true, false, true }; // -Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), true, false, false, false, true, false, false, true }; // 0
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), true, false, false, true, false, false, false, true }; // -0
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), true, false, false, false, true, false, false, true }; // 1
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), true, false, false, true, false, false, false, true }; // -1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), true, false, false, false, true, false, false, true }; // 2
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000002), true, false, false, true, false, false, false, true }; // -2
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), true, false, false, false, true, false, false, true }; // 3
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), true, false, false, true, false, false, false, true }; // -3
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), true, false, false, false, true, false, false, true }; // 5
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), true, false, false, true, false, false, false, true }; // -5
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), true, false, false, false, true, false, false, true }; // 10
            yield return new object[] { new UInt128(0xB040000000000000, 0x000000000000000A), true, false, false, true, false, false, false, true }; // -10
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000005), true, false, false, false, true, false, false, true }; // 0.5
            yield return new object[] { new UInt128(0xB03E000000000000, 0x0000000000000005), true, false, false, true, false, false, false, true }; // -0.5
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000019), true, false, false, false, true, false, false, true }; // 2.5
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000F), true, false, false, false, true, false, false, true }; // 1.5
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000064), true, false, false, false, true, false, false, true }; // 100
            yield return new object[] { new UInt128(0x3040000000000000, 0x00000000000003E8), true, false, false, false, true, false, false, true }; // 1000
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), true, false, false, false, true, false, false, true }; // 0.1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000009), true, false, false, false, true, false, false, true }; // 9
            yield return new object[] { new UInt128(0x3046000000000000, 0x0000000000000001), true, false, false, false, true, false, false, true }; // 1E+3
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), true, false, false, false, true, false, false, true }; // 1.0
            yield return new object[] { new UInt128(0x303C000000000000, 0x00000000000000C8), true, false, false, false, true, false, false, true }; // 2.00
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), true, false, false, false, true, false, false, true }; // 2E+1
            yield return new object[] { new UInt128(0x0000000000000000, 0x0000000000000001), true, false, false, false, true, false, false, true }; // epsilon
            yield return new object[] { new UInt128(0x8000000000000000, 0x0000000000000001), true, false, false, true, false, false, false, true }; // -epsilon
            yield return new object[] { new UInt128(0x0000314DC6448D93, 0x38C15B09FFFFFFFF), true, false, false, false, true, false, false, true }; // largest_subnormal
            yield return new object[] { new UInt128(0x8000314DC6448D93, 0x38C15B09FFFFFFFF), true, false, false, true, false, false, false, true }; // -largest_subnormal
            yield return new object[] { new UInt128(0x0042000000000000, 0x0000000000000001), true, false, false, false, true, false, false, true }; // smallest_normal
            yield return new object[] { new UInt128(0x8042000000000000, 0x0000000000000001), true, false, false, true, false, false, false, true }; // -smallest_normal
            yield return new object[] { new UInt128(0x5FFFED09BEAD87C0, 0x378D8E63FFFFFFFF), true, false, false, false, true, false, false, true }; // maxvalue
            yield return new object[] { new UInt128(0xDFFFED09BEAD87C0, 0x378D8E63FFFFFFFF), true, false, false, true, false, false, false, true }; // -maxvalue
        }

        [Theory]
        [MemberData(nameof(Classification_TestData))]
        public static void ClassificationTest(UInt128 value, bool isFinite, bool isInfinity, bool isNaN, bool isNegative, bool isPositive, bool isNegativeInfinity, bool isPositiveInfinity, bool isRealNumber)
        {
            Decimal128 d = Unsafe.BitCast<UInt128, Decimal128>(value);
            Assert.Equal(isFinite, Decimal128.IsFinite(d));
            Assert.Equal(isInfinity, Decimal128.IsInfinity(d));
            Assert.Equal(isNaN, Decimal128.IsNaN(d));
            Assert.Equal(isNegative, Decimal128.IsNegative(d));
            Assert.Equal(isPositive, Decimal128.IsPositive(d));
            Assert.Equal(isNegativeInfinity, Decimal128.IsNegativeInfinity(d));
            Assert.Equal(isPositiveInfinity, Decimal128.IsPositiveInfinity(d));
            Assert.Equal(isRealNumber, Decimal128.IsRealNumber(d));
        }

        public static IEnumerable<object[]> IsNormalIsSubnormal_TestData()
        {
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), false, false }; // NaN
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), false, false }; // -NaN
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), false, false }; // +Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), false, false }; // -Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), false, false }; // 0
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), false, false }; // -0
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), true, false }; // 1
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), true, false }; // -1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), true, false }; // 2
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000002), true, false }; // -2
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), true, false }; // 3
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), true, false }; // -3
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), true, false }; // 5
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), true, false }; // -5
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), true, false }; // 10
            yield return new object[] { new UInt128(0xB040000000000000, 0x000000000000000A), true, false }; // -10
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000005), true, false }; // 0.5
            yield return new object[] { new UInt128(0xB03E000000000000, 0x0000000000000005), true, false }; // -0.5
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000019), true, false }; // 2.5
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000F), true, false }; // 1.5
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000064), true, false }; // 100
            yield return new object[] { new UInt128(0x3040000000000000, 0x00000000000003E8), true, false }; // 1000
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), true, false }; // 0.1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000009), true, false }; // 9
            yield return new object[] { new UInt128(0x3046000000000000, 0x0000000000000001), true, false }; // 1E+3
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), true, false }; // 1.0
            yield return new object[] { new UInt128(0x303C000000000000, 0x00000000000000C8), true, false }; // 2.00
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), true, false }; // 2E+1
            yield return new object[] { new UInt128(0x0000000000000000, 0x0000000000000001), false, true }; // epsilon
            yield return new object[] { new UInt128(0x8000000000000000, 0x0000000000000001), false, true }; // -epsilon
            yield return new object[] { new UInt128(0x0000314DC6448D93, 0x38C15B09FFFFFFFF), false, true }; // largest_subnormal
            yield return new object[] { new UInt128(0x8000314DC6448D93, 0x38C15B09FFFFFFFF), false, true }; // -largest_subnormal
            yield return new object[] { new UInt128(0x0042000000000000, 0x0000000000000001), true, false }; // smallest_normal
            yield return new object[] { new UInt128(0x8042000000000000, 0x0000000000000001), true, false }; // -smallest_normal
            yield return new object[] { new UInt128(0x5FFFED09BEAD87C0, 0x378D8E63FFFFFFFF), true, false }; // maxvalue
            yield return new object[] { new UInt128(0xDFFFED09BEAD87C0, 0x378D8E63FFFFFFFF), true, false }; // -maxvalue
        }

        [Theory]
        [MemberData(nameof(IsNormalIsSubnormal_TestData))]
        public static void IsNormalIsSubnormalTest(UInt128 value, bool isNormal, bool isSubnormal)
        {
            Decimal128 d = Unsafe.BitCast<UInt128, Decimal128>(value);
            Assert.Equal(isNormal, Decimal128.IsNormal(d));
            Assert.Equal(isSubnormal, Decimal128.IsSubnormal(d));
        }

        public static IEnumerable<object[]> IsInteger_TestData()
        {
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), false, false, false }; // NaN
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), false, false, false }; // -NaN
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), false, false, false }; // +Inf
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), false, false, false }; // -Inf
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), true, true, false }; // 0
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), true, true, false }; // -0
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), true, false, true }; // 1
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), true, false, true }; // -1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), true, true, false }; // 2
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000002), true, true, false }; // -2
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), true, false, true }; // 3
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), true, false, true }; // -3
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), true, false, true }; // 5
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), true, false, true }; // -5
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), true, true, false }; // 10
            yield return new object[] { new UInt128(0xB040000000000000, 0x000000000000000A), true, true, false }; // -10
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000005), false, false, false }; // 0.5
            yield return new object[] { new UInt128(0xB03E000000000000, 0x0000000000000005), false, false, false }; // -0.5
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000019), false, false, false }; // 2.5
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000F), false, false, false }; // 1.5
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000064), true, true, false }; // 100
            yield return new object[] { new UInt128(0x3040000000000000, 0x00000000000003E8), true, true, false }; // 1000
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), false, false, false }; // 0.1
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000009), true, false, true }; // 9
            yield return new object[] { new UInt128(0x3046000000000000, 0x0000000000000001), true, true, false }; // 1E+3
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), true, false, true }; // 1.0
            yield return new object[] { new UInt128(0x303C000000000000, 0x00000000000000C8), true, true, false }; // 2.00
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), true, true, false }; // 2E+1
            yield return new object[] { new UInt128(0x0000000000000000, 0x0000000000000001), false, false, false }; // epsilon
            yield return new object[] { new UInt128(0x8000000000000000, 0x0000000000000001), false, false, false }; // -epsilon
            yield return new object[] { new UInt128(0x0000314DC6448D93, 0x38C15B09FFFFFFFF), false, false, false }; // largest_subnormal
            yield return new object[] { new UInt128(0x8000314DC6448D93, 0x38C15B09FFFFFFFF), false, false, false }; // -largest_subnormal
            yield return new object[] { new UInt128(0x0042000000000000, 0x0000000000000001), false, false, false }; // smallest_normal
            yield return new object[] { new UInt128(0x8042000000000000, 0x0000000000000001), false, false, false }; // -smallest_normal
            yield return new object[] { new UInt128(0x5FFFED09BEAD87C0, 0x378D8E63FFFFFFFF), true, true, false }; // maxvalue
            yield return new object[] { new UInt128(0xDFFFED09BEAD87C0, 0x378D8E63FFFFFFFF), true, true, false }; // -maxvalue
        }

        [Theory]
        [MemberData(nameof(IsInteger_TestData))]
        public static void IsIntegerTest(UInt128 value, bool isInteger, bool isEvenInteger, bool isOddInteger)
        {
            Decimal128 d = Unsafe.BitCast<UInt128, Decimal128>(value);
            Assert.Equal(isInteger, Decimal128.IsInteger(d));
            Assert.Equal(isEvenInteger, Decimal128.IsEvenInteger(d));
            Assert.Equal(isOddInteger, Decimal128.IsOddInteger(d));
        }

        public static IEnumerable<object[]> MaxMagnitude_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) };
        }

        [Theory]
        [MemberData(nameof(MaxMagnitude_TestData))]
        public static void MaxMagnitudeTest(UInt128 x, UInt128 y, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.MaxMagnitude(Unsafe.BitCast<UInt128, Decimal128>(x), Unsafe.BitCast<UInt128, Decimal128>(y))));
        }

        public static IEnumerable<object[]> MinMagnitude_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000005) };
        }

        [Theory]
        [MemberData(nameof(MinMagnitude_TestData))]
        public static void MinMagnitudeTest(UInt128 x, UInt128 y, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.MinMagnitude(Unsafe.BitCast<UInt128, Decimal128>(x), Unsafe.BitCast<UInt128, Decimal128>(y))));
        }

        public static IEnumerable<object[]> MaxMagnitudeNumber_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) };
        }

        [Theory]
        [MemberData(nameof(MaxMagnitudeNumber_TestData))]
        public static void MaxMagnitudeNumberTest(UInt128 x, UInt128 y, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.MaxMagnitudeNumber(Unsafe.BitCast<UInt128, Decimal128>(x), Unsafe.BitCast<UInt128, Decimal128>(y))));
        }

        public static IEnumerable<object[]> MinMagnitudeNumber_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000005) };
        }

        [Theory]
        [MemberData(nameof(MinMagnitudeNumber_TestData))]
        public static void MinMagnitudeNumberTest(UInt128 x, UInt128 y, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.MinMagnitudeNumber(Unsafe.BitCast<UInt128, Decimal128>(x), Unsafe.BitCast<UInt128, Decimal128>(y))));
        }

        public static IEnumerable<object[]> MultiplyAddEstimate_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000011) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0xB040000000000000, 0x000000000000000D) };
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000001), new UInt128(0x303E000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x303C000000000000, 0x0000000000000065) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x303E000000000000, 0x0000000000000019), new UInt128(0xB03E000000000000, 0x0000000000000005), new UInt128(0x303E000000000000, 0x000000000000002D) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000065) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
        }

        [Theory]
        [MemberData(nameof(MultiplyAddEstimate_TestData))]
        public static void MultiplyAddEstimateTest(UInt128 left, UInt128 right, UInt128 addend, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.MultiplyAddEstimate(Unsafe.BitCast<UInt128, Decimal128>(left), Unsafe.BitCast<UInt128, Decimal128>(right), Unsafe.BitCast<UInt128, Decimal128>(addend))));
        }

        public static IEnumerable<object[]> Max_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), new UInt128(0x303C000000000000, 0x0000000000000064), new UInt128(0x303C000000000000, 0x0000000000000064) };
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000014), new UInt128(0x3040000000000000, 0x0000000000000014) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
        }

        [Theory]
        [MemberData(nameof(Max_TestData))]
        public static void MaxTest(UInt128 x, UInt128 y, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Max(Unsafe.BitCast<UInt128, Decimal128>(x), Unsafe.BitCast<UInt128, Decimal128>(y))));
        }

        public static IEnumerable<object[]> Min_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), new UInt128(0x303C000000000000, 0x0000000000000064), new UInt128(0x303C000000000000, 0x0000000000000064) };
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000014), new UInt128(0x3040000000000000, 0x0000000000000014) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
        }

        [Theory]
        [MemberData(nameof(Min_TestData))]
        public static void MinTest(UInt128 x, UInt128 y, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Min(Unsafe.BitCast<UInt128, Decimal128>(x), Unsafe.BitCast<UInt128, Decimal128>(y))));
        }

        public static IEnumerable<object[]> MaxNative_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), new UInt128(0x303C000000000000, 0x0000000000000064), new UInt128(0x303C000000000000, 0x0000000000000064) };
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000014), new UInt128(0x3040000000000000, 0x0000000000000014) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
        }

        [Theory]
        [MemberData(nameof(MaxNative_TestData))]
        public static void MaxNativeTest(UInt128 x, UInt128 y, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.MaxNative(Unsafe.BitCast<UInt128, Decimal128>(x), Unsafe.BitCast<UInt128, Decimal128>(y))));
        }

        public static IEnumerable<object[]> MinNative_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), new UInt128(0x303C000000000000, 0x0000000000000064), new UInt128(0x303C000000000000, 0x0000000000000064) };
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000014), new UInt128(0x3040000000000000, 0x0000000000000014) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
        }

        [Theory]
        [MemberData(nameof(MinNative_TestData))]
        public static void MinNativeTest(UInt128 x, UInt128 y, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.MinNative(Unsafe.BitCast<UInt128, Decimal128>(x), Unsafe.BitCast<UInt128, Decimal128>(y))));
        }

        public static IEnumerable<object[]> MaxNumber_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), new UInt128(0x303C000000000000, 0x0000000000000064), new UInt128(0x303C000000000000, 0x0000000000000064) };
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000014), new UInt128(0x3040000000000000, 0x0000000000000014) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
        }

        [Theory]
        [MemberData(nameof(MaxNumber_TestData))]
        public static void MaxNumberTest(UInt128 x, UInt128 y, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.MaxNumber(Unsafe.BitCast<UInt128, Decimal128>(x), Unsafe.BitCast<UInt128, Decimal128>(y))));
        }

        public static IEnumerable<object[]> MinNumber_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), new UInt128(0x303C000000000000, 0x0000000000000064), new UInt128(0x303C000000000000, 0x0000000000000064) };
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000014), new UInt128(0x3040000000000000, 0x0000000000000014) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000) };
        }

        [Theory]
        [MemberData(nameof(MinNumber_TestData))]
        public static void MinNumberTest(UInt128 x, UInt128 y, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.MinNumber(Unsafe.BitCast<UInt128, Decimal128>(x), Unsafe.BitCast<UInt128, Decimal128>(y))));
        }

        public static IEnumerable<object[]> CopySign_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), new UInt128(0x303C000000000000, 0x0000000000000064), new UInt128(0x303E000000000000, 0x000000000000000A) };
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000014), new UInt128(0x3042000000000000, 0x0000000000000002) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
        }

        [Theory]
        [MemberData(nameof(CopySign_TestData))]
        public static void CopySignTest(UInt128 value, UInt128 sign, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CopySign(Unsafe.BitCast<UInt128, Decimal128>(value), Unsafe.BitCast<UInt128, Decimal128>(sign))));
        }

        public static IEnumerable<object[]> Sign_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), 0 };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), 0 };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), 1 };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000001), -1 };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), 1 };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), -1 };
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000005), 1 };
            yield return new object[] { new UInt128(0xB03E000000000000, 0x0000000000000005), -1 };
            yield return new object[] { new UInt128(0x303E000000000000, 0x000000000000000A), 1 };
            yield return new object[] { new UInt128(0x3042000000000000, 0x0000000000000002), 1 };
            yield return new object[] { new UInt128(0x303A000000000000, 0x0000000000000001), 1 };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), 1 };
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), -1 };
        }

        [Theory]
        [MemberData(nameof(Sign_TestData))]
        public static void SignTest(UInt128 value, int expected)
        {
            Assert.Equal(expected, Decimal128.Sign(Unsafe.BitCast<UInt128, Decimal128>(value)));
        }

        [Fact]
        public static void SignNaNTest()
        {
            Assert.Throws<ArithmeticException>(() => Decimal128.Sign(Decimal128.NaN));
            Assert.Throws<ArithmeticException>(() => Decimal128.Sign(-Decimal128.NaN));
        }

        public static IEnumerable<object[]> Clamp_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000F), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x000000000000000A) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x000000000000000A) };
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000019), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x303E000000000000, 0x0000000000000019) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x000000000000000A) };
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001) };
        }

        [Theory]
        [MemberData(nameof(Clamp_TestData))]
        public static void ClampTest(UInt128 value, UInt128 min, UInt128 max, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Clamp(Unsafe.BitCast<UInt128, Decimal128>(value), Unsafe.BitCast<UInt128, Decimal128>(min), Unsafe.BitCast<UInt128, Decimal128>(max))));
        }

        [Fact]
        public static void ClampMinGreaterThanMaxTest()
        {
            Assert.Throws<ArgumentException>(() => Decimal128.Clamp(Decimal128.One, Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x3040000000000000, 0x000000000000000A)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x3040000000000000, 0x0000000000000001))));
        }

        public static IEnumerable<object[]> ClampNative_TestData()
        {
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000005) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000F), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x000000000000000A) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x000000000000000A) };
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000019), new UInt128(0x3040000000000000, 0x0000000000000002), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x303E000000000000, 0x0000000000000019) };
            yield return new object[] { new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0xB040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0xB040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003), new UInt128(0x3040000000000000, 0x0000000000000003) };
            yield return new object[] { new UInt128(0x7C00000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x000000000000000A) };
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x3040000000000000, 0x0000000000000001), new UInt128(0x3040000000000000, 0x000000000000000A), new UInt128(0x3040000000000000, 0x0000000000000001) };
        }

        [Theory]
        [MemberData(nameof(ClampNative_TestData))]
        public static void ClampNativeTest(UInt128 value, UInt128 min, UInt128 max, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.ClampNative(Unsafe.BitCast<UInt128, Decimal128>(value), Unsafe.BitCast<UInt128, Decimal128>(min), Unsafe.BitCast<UInt128, Decimal128>(max))));
        }

        [Fact]
        public static void ClampNativeMinGreaterThanMaxTest()
        {
            Assert.Throws<ArgumentException>(() => Decimal128.ClampNative(Decimal128.One, Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x3040000000000000, 0x000000000000000A)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x3040000000000000, 0x0000000000000001))));
        }
    }
}
