// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
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

        public static IEnumerable<object[]> TryParsePartial_TestData()
        {
            NumberStyles style = NumberStyles.Float;

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
            yield return new object[] { "Infinity  x", (NumberStyles.Float & ~NumberStyles.AllowTrailingWhite), CultureInfo.InvariantCulture, Decimal128.PositiveInfinity, 10 };
        }

        [Theory]
        [MemberData(nameof(TryParsePartial_TestData))]
        public static void TryParsePartial(string value, NumberStyles style, IFormatProvider provider, Decimal128 expected, int expectedCharsConsumed)
        {
            Assert.True(Decimal128.TryParsePartial(value, style, provider, out Decimal128 result, out int charsConsumed));
            Assert.Equal(expected, result);
            Assert.Equal(expectedCharsConsumed, charsConsumed);

            Assert.True(Decimal128.TryParsePartial(value.AsSpan(), style, provider, out result, out charsConsumed));
            Assert.Equal(expected, result);
            Assert.Equal(expectedCharsConsumed, charsConsumed);

            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);
            Assert.True(Decimal128.TryParsePartial(utf8Bytes.AsSpan(), style, provider, out result, out int bytesConsumed));
            Assert.Equal(expected, result);
            Assert.Equal(expectedCharsConsumed, bytesConsumed);
        }

        public static IEnumerable<object[]> TryParsePartial_Invalid_TestData()
        {
            NumberStyles style = NumberStyles.Float;

            yield return new object[] { "", style, CultureInfo.InvariantCulture };
            yield return new object[] { "abc", style, CultureInfo.InvariantCulture };
            yield return new object[] { ".abc", style, CultureInfo.InvariantCulture };
        }

        [Theory]
        [MemberData(nameof(TryParsePartial_Invalid_TestData))]
        public static void TryParsePartial_Invalid(string value, NumberStyles style, IFormatProvider provider)
        {
            Assert.False(Decimal128.TryParsePartial(value, style, provider, out Decimal128 result, out int charsConsumed));
            Assert.Equal(0, charsConsumed);

            Assert.False(Decimal128.TryParsePartial(value.AsSpan(), style, provider, out result, out charsConsumed));
            Assert.Equal(0, charsConsumed);

            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);
            Assert.False(Decimal128.TryParsePartial(utf8Bytes.AsSpan(), style, provider, out result, out int bytesConsumed));
            Assert.Equal(0, bytesConsumed);
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000001UL)] // +0 -> +MINFP
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000001UL)] // -0 -> +MINFP
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x2FFE314DC6448D93UL, 0x38C15B0A00000001UL)] // 1 -> 1.000...001
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0xAFFDED09BEAD87C0UL, 0x378D8E63FFFFFFFFUL)] // -1 -> -0.999...9
        [InlineData(0x5FFFED09BEAD87C0UL, 0x378D8E63FFFFFFFFUL, 0x7800000000000000UL, 0x0000000000000000UL)] // +MAXFP -> +Infinity
        [InlineData(0xDFFFED09BEAD87C0UL, 0x378D8E63FFFFFFFFUL, 0xDFFFED09BEAD87C0UL, 0x378D8E63FFFFFFFEUL)] // -MAXFP steps toward zero
        [InlineData(0x0000000000000000UL, 0x0000000000000001UL, 0x0000000000000000UL, 0x0000000000000002UL)] // +MINFP
        [InlineData(0x8000000000000000UL, 0x0000000000000001UL, 0x8000000000000000UL, 0x0000000000000000UL)] // -MINFP -> -0
        [InlineData(0x2FFFED09BEAD87C0UL, 0x378D8E63FFFFFFFFUL, 0x3000314DC6448D93UL, 0x38C15B0A00000000UL)] // coefficient carry
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0xDFFFED09BEAD87C0UL, 0x378D8E63FFFFFFFFUL)] // -Infinity -> -MAXFP
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // NaN
        [InlineData(0x7E00000000000000UL, 0x0000000000001234UL, 0x7C00000000000000UL, 0x0000000000001234UL)] // signaling NaN canonicalized
        [InlineData(0x7C003FFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0x7C00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload canonicalized
        public static void BitIncrementTest(ulong upper, ulong lower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.BitIncrement(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(upper, lower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x8000000000000000UL, 0x0000000000000001UL)] // +0 -> -MINFP
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x8000000000000000UL, 0x0000000000000001UL)] // -0 -> -MINFP
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x2FFDED09BEAD87C0UL, 0x378D8E63FFFFFFFFUL)] // 1 -> 0.999...9
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0xAFFE314DC6448D93UL, 0x38C15B0A00000001UL)] // -1 -> -1.000...001
        [InlineData(0x5FFFED09BEAD87C0UL, 0x378D8E63FFFFFFFFUL, 0x5FFFED09BEAD87C0UL, 0x378D8E63FFFFFFFEUL)] // +MAXFP steps toward zero
        [InlineData(0xDFFFED09BEAD87C0UL, 0x378D8E63FFFFFFFFUL, 0xF800000000000000UL, 0x0000000000000000UL)] // -MAXFP -> -Infinity
        [InlineData(0x0000000000000000UL, 0x0000000000000001UL, 0x0000000000000000UL, 0x0000000000000000UL)] // +MINFP -> +0
        [InlineData(0x8000000000000000UL, 0x0000000000000001UL, 0x8000000000000000UL, 0x0000000000000002UL)] // -MINFP
        [InlineData(0x2FFFED09BEAD87C0UL, 0x378D8E63FFFFFFFFUL, 0x2FFFED09BEAD87C0UL, 0x378D8E63FFFFFFFEUL)] // normal step
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x5FFFED09BEAD87C0UL, 0x378D8E63FFFFFFFFUL)] // +Infinity -> +MAXFP
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL)] // -Infinity
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // NaN
        [InlineData(0x7E00000000000000UL, 0x0000000000001234UL, 0x7C00000000000000UL, 0x0000000000001234UL)] // signaling NaN canonicalized
        [InlineData(0x7C003FFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0x7C00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload canonicalized
        public static void BitDecrementTest(ulong upper, ulong lower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.BitDecrement(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(upper, lower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0)] // 1
        [InlineData(0x304A000000000000UL, 0x0000000000000001UL, 5)] // 1e5
        [InlineData(0x3040000000000000UL, 0x000000000012D687UL, 6)] // 1234567
        [InlineData(0x3036000000000000UL, 0x0000000000000389UL, -3)] // 9.05e-3
        [InlineData(0x3040000000000000UL, 0x000000000000002AUL, 1)] // 42
        [InlineData(0x2F82000000000000UL, 0x0000000000000001UL, -95)] // 1e-95
        [InlineData(0x304C000000000000UL, 0x0000000000000001UL, 6)] // 1e6
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, int.MinValue)] // +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, int.MinValue)] // -0
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, int.MaxValue)] // NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, int.MaxValue)] // +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, int.MaxValue)] // -Infinity
        public static void ILogBTest(ulong upper, ulong lower, int expected)
        {
            Assert.Equal(expected, Decimal128.ILogB(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(upper, lower))));
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x000000000000007BUL, 0, 0x3040000000000000UL, 0x000000000000007BUL)] // 123 scaleB 0
        [InlineData(0x3040000000000000UL, 0x000000000000007BUL, 2, 0x3044000000000000UL, 0x000000000000007BUL)] // 123 scaleB 2
        [InlineData(0x3040000000000000UL, 0x000000000000007BUL, -2, 0x303C000000000000UL, 0x000000000000007BUL)] // 123 scaleB -2
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 34, 0x3084000000000000UL, 0x0000000000000001UL)] // absorb into coefficient
        [InlineData(0x3040000000000000UL, 0x0000000000000009UL, 6111, 0x5FFE000000000000UL, 0x0000000000000009UL)] // absorb at max quantum
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 6154, 0x7800000000000000UL, 0x0000000000000000UL)] // overflow -> +Infinity
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 6154, 0xF800000000000000UL, 0x0000000000000000UL)] // overflow -> -Infinity
        [InlineData(0x0042000000000000UL, 0x0000000000000001UL, -50, 0x0000000000000000UL, 0x0000000000000000UL)] // deep underflow -> +0
        [InlineData(0x0000000000000000UL, 0x000000000000000FUL, -1, 0x0000000000000000UL, 0x0000000000000002UL)] // gradual underflow, tie -> even (up)
        [InlineData(0x0000000000000000UL, 0x0000000000000019UL, -1, 0x0000000000000000UL, 0x0000000000000002UL)] // gradual underflow, tie -> even (stay)
        [InlineData(0x0000000000000000UL, 0x000000000000000EUL, -1, 0x0000000000000000UL, 0x0000000000000001UL)] // gradual underflow, rounds down
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 5, 0x304A000000000000UL, 0x0000000000000000UL)] // +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 5, 0xB04A000000000000UL, 0x0000000000000000UL)] // -0
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 5, 0xFC00000000000000UL, 0x0000000000000000UL)] // NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 5, 0x7800000000000000UL, 0x0000000000000000UL)] // +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 5, 0xF800000000000000UL, 0x0000000000000000UL)] // -Infinity
        [InlineData(0x7E00000000000000UL, 0x0000000000001234UL, 5, 0x7C00000000000000UL, 0x0000000000001234UL)] // signaling NaN canonicalized
        [InlineData(0x7C003FFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 5, 0x7C00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload canonicalized
        public static void ScaleBTest(ulong upper, ulong lower, int n, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.ScaleB(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(upper, lower)), n);
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000003UL, 0x3040000000000000UL, 0x0000000000000002UL, 0xB040000000000000UL, 0x0000000000000001UL)] // 3 rem 2 = -1 (quotient rounds up to even 2)
        [InlineData(0x3040000000000000UL, 0x0000000000000005UL, 0x3040000000000000UL, 0x0000000000000002UL, 0x3040000000000000UL, 0x0000000000000001UL)] // 5 rem 2 = 1 (quotient 2, exact half stays)
        [InlineData(0x3040000000000000UL, 0x0000000000000007UL, 0x3040000000000000UL, 0x0000000000000002UL, 0xB040000000000000UL, 0x0000000000000001UL)] // 7 rem 2 = -1 (quotient rounds up to even 4)
        [InlineData(0x3040000000000000UL, 0x000000000000000BUL, 0x3040000000000000UL, 0x0000000000000004UL, 0xB040000000000000UL, 0x0000000000000001UL)] // 11 rem 4 = -1 (nearest multiple 12)
        [InlineData(0xB040000000000000UL, 0x000000000000000BUL, 0x3040000000000000UL, 0x0000000000000004UL, 0x3040000000000000UL, 0x0000000000000001UL)] // -11 rem 4 = 1 (sign flips)
        [InlineData(0x3040000000000000UL, 0x000000000000000AUL, 0x3040000000000000UL, 0x0000000000000003UL, 0x3040000000000000UL, 0x0000000000000001UL)] // 10 rem 3 = 1
        [InlineData(0x3040000000000000UL, 0x0000000000000009UL, 0x3040000000000000UL, 0x0000000000000003UL, 0x3040000000000000UL, 0x0000000000000000UL)] // 9 rem 3 = +0 (exact multiple)
        [InlineData(0xB040000000000000UL, 0x0000000000000009UL, 0x3040000000000000UL, 0x0000000000000003UL, 0xB040000000000000UL, 0x0000000000000000UL)] // -9 rem 3 = -0
        [InlineData(0x3040000000000000UL, 0x000000000000002AUL, 0x7800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x000000000000002AUL)] // finite rem +Infinity = finite
        [InlineData(0xB040000000000000UL, 0x000000000000002AUL, 0xF800000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x000000000000002AUL)] // -finite rem -Infinity = -finite
        [InlineData(0x3040000000000000UL, 0x0000000000000005UL, 0x3040000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // x rem 0 = NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000005UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // +Infinity rem finite = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000005UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // NaN rem finite = NaN
        [InlineData(0x3040000000000000UL, 0x0000000000000005UL, 0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // finite rem NaN = NaN
        [InlineData(0x7E00000000000000UL, 0x0000000000001234UL, 0x3040000000000000UL, 0x0000000000000005UL, 0x7C00000000000000UL, 0x0000000000001234UL)] // signaling NaN operand quieted
        [InlineData(0x3040000000000000UL, 0x0000000000000005UL, 0x7C003FFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0x7C00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        [InlineData(0x9DD8000000000000UL, 0x00005AE240791CB6UL, 0xCDE6000000000F49UL, 0x1F2A73FEBB2E87E4UL, 0x9DD8000000000000UL, 0x00005AE240791CB6UL)]
        [InlineData(0x3492000000000000UL, 0x00000000000288ECUL, 0xCDC0000000000000UL, 0x0002B570149A7EA3UL, 0x3492000000000000UL, 0x00000000000288ECUL)]
        [InlineData(0xB91C00000E10E981UL, 0x07C99D05E9BA12D6UL, 0xA174000000000000UL, 0x00000000000047C2UL, 0xA174000000000000UL, 0x0000000000001A54UL)]
        [InlineData(0x1D2E000000000000UL, 0x0000000000000000UL, 0x127C000000000000UL, 0x7BE69A35492A0CB3UL, 0x127C000000000000UL, 0x0000000000000000UL)]
        [InlineData(0x226A000000000000UL, 0x0000000000000025UL, 0x9542000000000000UL, 0x00000000000001EDUL, 0x9542000000000000UL, 0x00000000000000F3UL)]
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xCA42000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)]
        [InlineData(0xD9F2000000002C69UL, 0xCB28F65CC738478CUL, 0xA6082E9DA99387A2UL, 0x32F5E22081376C85UL, 0xA608151AF546EF83UL, 0xD0E20672239D5124UL)]
        [InlineData(0x428C000000000000UL, 0x0000000000002582UL, 0xBC3C0208AE9D60D1UL, 0x5F293AAFE0F4E2C3UL, 0xBC3C00CAFAA4688BUL, 0xC0AF20E24D17BCE7UL)]
        [InlineData(0x2A2600000000002DUL, 0x59B9AA35DE0A76F8UL, 0xB69A000000FE255DUL, 0x9813B929D4CAB39EUL, 0x2A2600000000002DUL, 0x59B9AA35DE0A76F8UL)]
        [InlineData(0xB19C000000000000UL, 0x0000000000000000UL, 0x3A72000000000000UL, 0x00000002354B2F73UL, 0xB19C000000000000UL, 0x0000000000000000UL)]
        [InlineData(0x28920004CFEB720EUL, 0x24AE01A62DBA18B3UL, 0xF800000000000000UL, 0x0000000000000000UL, 0x28920004CFEB720EUL, 0x24AE01A62DBA18B3UL)]
        [InlineData(0xB94400000DDF9083UL, 0xB3C1E0CA40E97365UL, 0x7800000000000000UL, 0x0000000000000000UL, 0xB94400000DDF9083UL, 0xB3C1E0CA40E97365UL)]
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0x3970000000000000UL, 0x000000000000252BUL, 0xFC00000000000000UL, 0x0000000000000000UL)]
        [InlineData(0x9340000000065682UL, 0xC7C831F6DEC2DC76UL, 0x113E000000000000UL, 0x00000015259A9347UL, 0x113E000000000000UL, 0x00000007E60B3298UL)]
        [InlineData(0x3D20000000000004UL, 0xF399C4B35E6951FEUL, 0xA65E000000000000UL, 0x00000453EA66AE8FUL, 0x265E000000000000UL, 0x0000017C2BDE3FA2UL)]
        [InlineData(0x356A000000000000UL, 0x0000000000000000UL, 0x0C90000000000000UL, 0x602EED97BB4D4597UL, 0x0C90000000000000UL, 0x0000000000000000UL)]
        [InlineData(0xD71A000000000000UL, 0x000000721CC91608UL, 0x016AA9EFD4B0AC76UL, 0x62E4F61692CE4106UL, 0x016A44B7D3E5343BUL, 0x5794CD94FD5D69EAUL)]
        [InlineData(0xAF1400001734A1A0UL, 0x9F0C3C44080487E5UL, 0x1F8E000000000000UL, 0x09F6E0D6E8C1F02CUL, 0x9F8E000000000000UL, 0x0410EDE0BD429090UL)]
        [InlineData(0x1E9E00012996900AUL, 0x2DD4810C4F3A7B0DUL, 0x5A92000000000000UL, 0x0000005E4F90A79AUL, 0x1E9E00012996900AUL, 0x2DD4810C4F3A7B0DUL)]
        [InlineData(0x49E0000000000000UL, 0x00002A9775133A63UL, 0xBFBA000000000117UL, 0x5FA6AE4B2078E0E9UL, 0x3FBA00000000006AUL, 0xCF0A3BF721C3D55EUL)]
        [InlineData(0x9C6E000000000000UL, 0x0000000002F8A9ACUL, 0x80CE000000000000UL, 0x00000000000002DBUL, 0x80CE000000000000UL, 0x000000000000010CUL)]
        [InlineData(0x13AE000000000000UL, 0x0000000000000000UL, 0x80E0000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)]
        [InlineData(0x25AA000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL, 0x25AA000000000000UL, 0x0000000000000000UL)]
        [InlineData(0x3A32031DC5CD65DCUL, 0x0C477B601BC9BDCDUL, 0xC350000000000000UL, 0x00000245D961F6B4UL, 0x3A32031DC5CD65DCUL, 0x0C477B601BC9BDCDUL)]
        [InlineData(0x05D6000000000000UL, 0x0000000000000006UL, 0x33E4000000000000UL, 0x0000000000000077UL, 0x05D6000000000000UL, 0x0000000000000006UL)]
        [InlineData(0x55C40000028DC19EUL, 0xCDA4CD2EA355D0E8UL, 0x0A40000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)]
        [InlineData(0x57C600000002BD63UL, 0xF456BE60FE8E2C69UL, 0x4892000000000000UL, 0x00000001279255F0UL, 0xC892000000000000UL, 0x000000008F041E80UL)]
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x9046000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)]
        [InlineData(0x1ABE0000027A1BA1UL, 0x211F3C3E661AC81EUL, 0xD17C000000000001UL, 0xCBDCD5265DC9C066UL, 0x1ABE0000027A1BA1UL, 0x211F3C3E661AC81EUL)]
        [InlineData(0xCFEC000000074D73UL, 0x87D4951999998747UL, 0x0ABA000000000984UL, 0xE2BC16EEB77D738CUL, 0x0ABA0000000000D6UL, 0x4C32F67CB2AD5CF4UL)]
        [InlineData(0xB0A4000000000000UL, 0x0073185BC8829F34UL, 0x0CE60002C9DC01CAUL, 0xCC8CE2871759CB5BUL, 0x8CE60001426B9523UL, 0x95C4B6BEE860F29BUL)]
        [InlineData(0x9312000000000000UL, 0x0002C127DF400535UL, 0xD02A000000000000UL, 0x0000000000000001UL, 0x9312000000000000UL, 0x0002C127DF400535UL)]
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xA970000000000000UL, 0x000000002C31CA2BUL, 0xFC00000000000000UL, 0x0000000000000000UL)]
        [InlineData(0x4FFA000000000000UL, 0x000000000000166CUL, 0x16A4000000000000UL, 0x00000000362B69E9UL, 0x96A4000000000000UL, 0x000000001251435EUL)]
        [InlineData(0xAE9A000000000000UL, 0x0000000000000000UL, 0x10FA000000000000UL, 0x000000000D42601AUL, 0x90FA000000000000UL, 0x0000000000000000UL)]
        [InlineData(0x1BBA000000000000UL, 0x0000000000000386UL, 0x2E5A04231F197D99UL, 0x6F56D55DDDA3AD76UL, 0x1BBA000000000000UL, 0x0000000000000386UL)]
        public static void Ieee754RemainderTest(ulong leftUpper, ulong leftLower, ulong rightUpper, ulong rightLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Ieee754Remainder(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(leftUpper, leftLower)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(rightUpper, rightLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000004UL, 0x3040000000000000UL, 0x0000000000000002UL)] // sqrt(4) = 2
        [InlineData(0x3040000000000000UL, 0x0000000000000009UL, 0x3040000000000000UL, 0x0000000000000003UL)] // sqrt(9) = 3
        [InlineData(0x3040000000000000UL, 0x0000000000000064UL, 0x3040000000000000UL, 0x000000000000000AUL)] // sqrt(100) = 10
        [InlineData(0x3044000000000000UL, 0x0000000000000004UL, 0x3042000000000000UL, 0x0000000000000002UL)] // sqrt(4E2) = 2E1 (even exponent halves)
        [InlineData(0x3038000000000000UL, 0x0000000000000009UL, 0x303C000000000000UL, 0x0000000000000003UL)] // sqrt(9E-4) = 3E-2
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x3040000000000000UL, 0x0000000000000001UL)] // sqrt(1) = 1
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x2FFE45B9E278CDF8UL, 0xB43E0F0F10148022UL)] // sqrt(2) inexact, rounded to 34 digits
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // sqrt(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // sqrt(-0) = -0 (sign preserved)
        [InlineData(0x304A000000000000UL, 0x0000000000000000UL, 0x3044000000000000UL, 0x0000000000000000UL)] // sqrt(0E5) = 0E2 (preferred exponent floor(5/2))
        [InlineData(0xB040000000000000UL, 0x0000000000000004UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // sqrt(-4) = NaN (invalid)
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // sqrt(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // sqrt(-Infinity) = NaN (invalid)
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // sqrt(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void SqrtTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Sqrt(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // exp(+0) = 1
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // exp(-0) = 1
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // exp(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // exp(-Infinity) = +0
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // exp(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void ExpTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Exp(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(0.5)]
        [InlineData(2.5)]
        [InlineData(-3.25)]
        [InlineData(10.0)]
        [InlineData(-7.5)]
        public static void ExpAccuracyTest(double input)
        {
            // Decimal128 evaluates exp in the software binary128 engine (as Intel does). Comparing through
            // binary64 bounds the check to double precision; the full accuracy is covered elsewhere.
            double expected = double.Exp(input);
            double actual = (double)Decimal128.Exp((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(expected), $"exp({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // exp10(+0) = 1
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // exp10(-0) = 1
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // exp10(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // exp10(-Infinity) = +0
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // exp10(NaN) = NaN
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void Exp10Test(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Exp10(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(0.5)]
        [InlineData(2.5)]
        [InlineData(-3.25)]
        public static void Exp10AccuracyTest(double input)
        {
            double expected = double.Exp10(input);
            double actual = (double)Decimal128.Exp10((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(expected), $"exp10({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // exp2(+0) = 1
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // exp2(-0) = 1
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // exp2(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // exp2(-Infinity) = +0
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // exp2(NaN) = NaN
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void Exp2Test(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Exp2(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(0.5)]
        [InlineData(2.5)]
        [InlineData(-3.25)]
        public static void Exp2AccuracyTest(double input)
        {
            double expected = double.Exp2(input);
            double actual = (double)Decimal128.Exp2((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(expected), $"exp2({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // expm1(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // expm1(-0) = -0 (sign preserved)
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // expm1(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000001UL)] // expm1(-Infinity) = -1
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // expm1(NaN) = NaN
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void ExpM1Test(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.ExpM1(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(0.5)]
        [InlineData(2.5)]
        [InlineData(-3.25)]
        public static void ExpM1AccuracyTest(double input)
        {
            double expected = double.ExpM1(input);
            double actual = (double)Decimal128.ExpM1((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(expected), $"expm1({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // exp2m1(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // exp2m1(-0) = -0 (sign preserved)
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // exp2m1(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000001UL)] // exp2m1(-Infinity) = -1
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // exp2m1(NaN) = NaN
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void Exp2M1Test(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Exp2M1(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(0.5)]
        [InlineData(2.5)]
        [InlineData(-3.25)]
        public static void Exp2M1AccuracyTest(double input)
        {
            double expected = double.Exp2M1(input);
            double actual = (double)Decimal128.Exp2M1((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(expected), $"exp2m1({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // exp10m1(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // exp10m1(-0) = -0 (sign preserved)
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // exp10m1(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000001UL)] // exp10m1(-Infinity) = -1
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // exp10m1(NaN) = NaN
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void Exp10M1Test(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Exp10M1(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(0.5)]
        [InlineData(2.5)]
        [InlineData(-3.25)]
        public static void Exp10M1AccuracyTest(double input)
        {
            double expected = double.Exp10M1(input);
            double actual = (double)Decimal128.Exp10M1((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(expected), $"exp10m1({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL)] // log(+0) = -Infinity
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL)] // log(-0) = -Infinity
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x3040000000000000UL, 0x0000000000000000UL)] // log(1) = +0
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // log(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // log(-Infinity) = NaN
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // log(-1) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // log(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void LogTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Log(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(2.0)]
        [InlineData(0.5)]
        [InlineData(2.5)]
        [InlineData(100.0)]
        [InlineData(0.001)]
        public static void LogAccuracyTest(double input)
        {
            // Decimal128 evaluates log in the software binary128 engine (as Intel does). Comparing through
            // binary64 bounds the check to double precision; the full accuracy is covered elsewhere.
            double expected = double.Log(input);
            double actual = (double)Decimal128.Log((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"log({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000002UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // log(NaN, 2) = NaN
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // log(2, NaN) = NaN
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x3040000000000000UL, 0x0000000000000001UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // log(2, 1) = NaN (base 1)
        public static void LogNewBaseTest(ulong valueUpper, ulong valueLower, ulong baseUpper, ulong baseLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Log(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(baseUpper, baseLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(8.0, 2.0)]
        [InlineData(100.0, 10.0)]
        [InlineData(2.5, 3.0)]
        public static void LogNewBaseAccuracyTest(double input, double newBase)
        {
            double expected = double.Log(input, newBase);
            double actual = (double)Decimal128.Log((Decimal128)input, (Decimal128)newBase);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"log({input}, {newBase}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL)] // log2(+0) = -Infinity
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL)] // log2(-0) = -Infinity
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x3040000000000000UL, 0x0000000000000000UL)] // log2(1) = +0
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // log2(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // log2(-Infinity) = NaN
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // log2(-1) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // log2(NaN) = NaN
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void Log2Test(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Log2(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(2.0)]
        [InlineData(0.5)]
        [InlineData(8.0)]
        [InlineData(0.001)]
        public static void Log2AccuracyTest(double input)
        {
            double expected = double.Log2(input);
            double actual = (double)Decimal128.Log2((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"log2({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL)] // log10(+0) = -Infinity
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL)] // log10(-0) = -Infinity
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x3040000000000000UL, 0x0000000000000000UL)] // log10(1) = +0
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // log10(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // log10(-Infinity) = NaN
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // log10(-1) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // log10(NaN) = NaN
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void Log10Test(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Log10(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(10.0)]
        [InlineData(0.5)]
        [InlineData(1000.0)]
        [InlineData(0.001)]
        public static void Log10AccuracyTest(double input)
        {
            double expected = double.Log10(input);
            double actual = (double)Decimal128.Log10((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"log10({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // logP1(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // logP1(-0) = -0 (sign preserved)
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0xF800000000000000UL, 0x0000000000000000UL)] // logP1(-1) = -Infinity
        [InlineData(0xB040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // logP1(-2) = NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // logP1(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // logP1(-Infinity) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // logP1(NaN) = NaN
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void LogP1Test(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.LogP1(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        [InlineData(-0.5)]
        [InlineData(2.5)]
        [InlineData(1e-6)]
        public static void LogP1AccuracyTest(double input)
        {
            double expected = double.LogP1(input);
            double actual = (double)Decimal128.LogP1((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"logP1({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // log2P1(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // log2P1(-0) = -0 (sign preserved)
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0xF800000000000000UL, 0x0000000000000000UL)] // log2P1(-1) = -Infinity
        [InlineData(0xB040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // log2P1(-2) = NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // log2P1(+Infinity) = +Infinity
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // log2P1(NaN) = NaN
        public static void Log2P1Test(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Log2P1(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        [InlineData(-0.5)]
        [InlineData(7.0)]
        [InlineData(1e-6)]
        public static void Log2P1AccuracyTest(double input)
        {
            double expected = double.Log2P1(input);
            double actual = (double)Decimal128.Log2P1((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"log2P1({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // log10P1(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // log10P1(-0) = -0 (sign preserved)
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0xF800000000000000UL, 0x0000000000000000UL)] // log10P1(-1) = -Infinity
        [InlineData(0xB040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // log10P1(-2) = NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // log10P1(+Infinity) = +Infinity
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // log10P1(NaN) = NaN
        public static void Log10P1Test(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Log10P1(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        [InlineData(-0.5)]
        [InlineData(9.0)]
        [InlineData(1e-6)]
        public static void Log10P1AccuracyTest(double input)
        {
            double expected = double.Log10P1(input);
            double actual = (double)Decimal128.Log10P1((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"log10P1({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x7C00000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // cbrt(NaN) = NaN
        [InlineData(0x7C00000000000000UL, 0x0000000000001234UL, 0x7C00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared (sign preserved)
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // cbrt(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL)] // cbrt(-Infinity) = -Infinity
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // cbrt(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // cbrt(-0) = -0
        public static void CbrtTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Cbrt(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(8.0)]
        [InlineData(-8.0)]
        [InlineData(27.0)]
        [InlineData(0.125)]
        [InlineData(2.0)]
        [InlineData(-2.0)]
        [InlineData(1000000.0)]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(0.5)]
        public static void CbrtAccuracyTest(double input)
        {
            // Decimal128 evaluates cbrt through the binary128 engine (as Intel does); comparing the result
            // cast back to binary64 stays within a few ulps of double.Cbrt.
            double expected = double.Cbrt(input);
            double actual = (double)Decimal128.Cbrt((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"cbrt({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x7C00000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // hypot(NaN, +Infinity) = +Infinity
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // hypot(+Infinity, NaN) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000002UL, 0x7800000000000000UL, 0x0000000000000000UL)] // hypot(-Infinity, 2) = +Infinity
        [InlineData(0x7C00000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // hypot(NaN, 2) = NaN
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // hypot(2, NaN) = NaN
        [InlineData(0x7C00000000000000UL, 0x0000000000001234UL, 0x3040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000002UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared (sign preserved)
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // hypot(+0, +0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000003UL, 0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000003UL)] // hypot(-3, +0) = 3
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000004UL, 0x3040000000000000UL, 0x0000000000000004UL)] // hypot(+0, -4) = 4
        public static void HypotTest(ulong xUpper, ulong xLower, ulong yUpper, ulong yLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Hypot(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(xUpper, xLower)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(yUpper, yLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(3.0, 4.0)]
        [InlineData(5.0, 12.0)]
        [InlineData(-8.0, 15.0)]
        [InlineData(1.0, 1.0)]
        [InlineData(0.5, 0.25)]
        [InlineData(1000.0, 0.001)]
        [InlineData(2.5, -6.5)]
        public static void HypotAccuracyTest(double x, double y)
        {
            // Decimal128 evaluates hypot through the binary128 engine (as Intel does); comparing the result
            // cast back to binary64 stays within a few ulps of double.Hypot.
            double expected = double.Hypot(x, y);
            double actual = (double)Decimal128.Hypot((Decimal128)x, (Decimal128)y);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"hypot({x}, {y}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x7C00000000000000UL, 0x0000000000001234UL, 5, 0x7C00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 5, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared (sign preserved)
        [InlineData(0x3040000000000000UL, 0x0000000000000008UL, 0, 0x7C00000000000000UL, 0x0000000000000000UL)] // rootn(x, 0) = NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 5, 0x7800000000000000UL, 0x0000000000000000UL)] // rootn(+Infinity, odd > 0) = +Infinity
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 4, 0x7800000000000000UL, 0x0000000000000000UL)] // rootn(+Infinity, even > 0) = +Infinity
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, -5, 0x3040000000000000UL, 0x0000000000000000UL)] // rootn(+Infinity, n < 0) = +0
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 5, 0xF800000000000000UL, 0x0000000000000000UL)] // rootn(-Infinity, odd > 0) = -Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 4, 0x7C00000000000000UL, 0x0000000000000000UL)] // rootn(-Infinity, even > 0) = NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, -5, 0xB040000000000000UL, 0x0000000000000000UL)] // rootn(-Infinity, odd < 0) = -0
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 5, 0x3040000000000000UL, 0x0000000000000000UL)] // rootn(+0, odd > 0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 5, 0xB040000000000000UL, 0x0000000000000000UL)] // rootn(-0, odd > 0) = -0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 4, 0x3040000000000000UL, 0x0000000000000000UL)] // rootn(-0, even > 0) = +0
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, -5, 0x7800000000000000UL, 0x0000000000000000UL)] // rootn(+0, n < 0) = +Infinity
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, -5, 0xF800000000000000UL, 0x0000000000000000UL)] // rootn(-0, odd < 0) = -Infinity
        [InlineData(0xB040000000000000UL, 0x0000000000000004UL, 2, 0x7C00000000000000UL, 0x0000000000000000UL)] // rootn(-4, even) = NaN
        public static void RootNTest(ulong valueUpper, ulong valueLower, int n, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.RootN(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)), n);
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(8.0, 3)]
        [InlineData(-8.0, 3)]
        [InlineData(27.0, 3)]
        [InlineData(16.0, 4)]
        [InlineData(32.0, 5)]
        [InlineData(1000.0, 3)]
        [InlineData(2.0, 2)]
        [InlineData(0.5, 2)]
        [InlineData(2.0, -2)]
        [InlineData(8.0, -3)]
        [InlineData(2.0, int.MinValue)]
        public static void RootNAccuracyTest(double input, int n)
        {
            // Decimal128 evaluates rootn through the binary128 engine (as Intel does); comparing the result
            // cast back to binary64 stays within a few ulps of double.RootN.
            double expected = double.RootN(input, n);
            double actual = (double)Decimal128.RootN((Decimal128)input, n);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"rootn({input}, {n}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x7C00000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // pow(NaN, +0) = 1
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // pow(2, +0) = 1
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // pow(2, -0) = 1
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x7C00000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // pow(1, NaN) = 1
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x7800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // pow(1, +Infinity) = 1
        [InlineData(0x7C00000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // pow(NaN, 2) = NaN
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // pow(2, NaN) = NaN
        [InlineData(0x7C00000000000000UL, 0x0000000000001234UL, 0x3040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000002UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // pow(2, +Infinity) = +Infinity (|x| > 1)
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0xF800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // pow(2, -Infinity) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0x7800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // pow(-1, +Infinity) = 1 (|x| == 1)
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000002UL, 0x7800000000000000UL, 0x0000000000000000UL)] // pow(+Infinity, 2) = +Infinity
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000002UL, 0x3040000000000000UL, 0x0000000000000000UL)] // pow(+Infinity, -2) = +0
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000003UL, 0xF800000000000000UL, 0x0000000000000000UL)] // pow(-Infinity, 3) = -Infinity (odd)
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000002UL, 0x7800000000000000UL, 0x0000000000000000UL)] // pow(-Infinity, 2) = +Infinity (even)
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000003UL, 0xB040000000000000UL, 0x0000000000000000UL)] // pow(-Infinity, -3) = -0 (odd, y < 0)
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000002UL, 0x3040000000000000UL, 0x0000000000000000UL)] // pow(+0, 2) = +0
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000002UL, 0x7800000000000000UL, 0x0000000000000000UL)] // pow(+0, -2) = +Infinity
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000003UL, 0xB040000000000000UL, 0x0000000000000000UL)] // pow(-0, 3) = -0 (odd)
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000002UL, 0x3040000000000000UL, 0x0000000000000000UL)] // pow(-0, 2) = +0 (even)
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000003UL, 0xF800000000000000UL, 0x0000000000000000UL)] // pow(-0, -3) = -Infinity (odd, y < 0)
        public static void PowTest(ulong valueUpper, ulong valueLower, ulong exponentUpper, ulong exponentLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Pow(
                Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)),
                Unsafe.BitCast<UInt128, Decimal128>(new UInt128(exponentUpper, exponentLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(2.0, 10.0)]
        [InlineData(3.0, 4.0)]
        [InlineData(10.0, 3.0)]
        [InlineData(2.5, 2.0)]
        [InlineData(0.5, 3.0)]
        [InlineData(-2.0, 3.0)]  // negative base, odd integer exponent -> negative result
        [InlineData(-2.0, 2.0)]  // negative base, even integer exponent -> positive result
        [InlineData(9.0, 0.5)]   // fractional exponent (square root)
        public static void PowAccuracyTest(double x, double y)
        {
            // Decimal128 evaluates pow through the binary128 engine (as Intel does); comparing the result
            // cast back to binary64 stays within a few ulps of double.Pow.
            double expected = double.Pow(x, y);
            double actual = (double)Decimal128.Pow((Decimal128)x, (Decimal128)y);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"pow({x}, {y}): expected {expected}, got {actual}");
        }

        [Fact]
        public static void PowNegativeBaseNonIntegerReturnsNaN()
        {
            Assert.True(Decimal128.IsNaN(Decimal128.Pow((Decimal128)(-2.0), (Decimal128)0.5)));
        }

        [Theory]
        [InlineData("1E100", "1")]      // same sign, actual exponent absurdly larger
        [InlineData("-1E100", "-1")]
        [InlineData("1E-100", "123")]   // same sign, actual exponent absurdly smaller
        [InlineData("-1E-100", "-123")]
        [InlineData("1", "-1")]         // opposite sign, within the raw ULP window
        [InlineData("-1", "1")]
        public static void AssertResultWithinUlpRejectsInvalidResults(string actualText, string expectedText)
        {
            UInt128 actual = Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Parse(actualText, CultureInfo.InvariantCulture));
            UInt128 expected = Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Parse(expectedText, CultureInfo.InvariantCulture));

            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
                DecimalIeee754IntelTestData.AssertResultWithinUlp(actual, expected, recordedUlp: 0, limit: 2));
        }

        [Theory]
        [InlineData("1", "1.0000")]     // equivalent cohorts
        [InlineData("1E-100", "1")]     // same sign, within one expected ULP
        public static void AssertResultWithinUlpAcceptsValidResults(string actualText, string expectedText)
        {
            UInt128 actual = Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Parse(actualText, CultureInfo.InvariantCulture));
            UInt128 expected = Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Parse(expectedText, CultureInfo.InvariantCulture));

            DecimalIeee754IntelTestData.AssertResultWithinUlp(actual, expected, recordedUlp: 0, limit: 2);
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // sin(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // sin(-0) = -0
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // sin(+Infinity) = NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // sin(-Infinity) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // sin(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void SinTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Sin(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(-0.5)]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(2.5)]
        [InlineData(-3.25)]
        [InlineData(100.0)]
        [InlineData(-0.1)] // negative, |x| < 0.5: exercises the small-argument quadrant sign
        [InlineData(-0.25)]
        public static void SinAccuracyTest(double input)
        {
            // Decimal128 evaluates sin in the software binary128 engine (as Intel does). Comparing through
            // binary64 bounds the check to double precision; the full accuracy is covered elsewhere.
            double expected = double.Sin(input);
            double actual = (double)Decimal128.Sin((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"sin({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData("1E100", -0.37237612366127669, -0.92808190507465534, 0.40123196199081435)]
        [InlineData("1E1000", 0.65335979821036986, -0.75704753753149794, -0.86303668636289036)]
        [InlineData("1E6000", -0.72492665343763259, -0.68882606450083938, 1.0524088602294015)]
        [InlineData("9.999999999999999999999999999999999E6144", 0.55829077490925212, -0.82964535233509672, -0.67292702036828441)] // max Decimal128
        [InlineData("1.234567890123456789012345678901234E13", -0.94990422533155822, 0.31254113760791918, -3.0392934274246064)] // negative exponent, |x| >= 1
        public static void TrigLargeArgumentTest(string value, double expectedSin, double expectedCos, double expectedTan)
        {
            // Large arguments no longer convert to binary128 exactly, so the range reduction runs in the
            // decimal domain. Verify (through binary64) that sin/cos/tan reduce mod 2*pi at any magnitude.
            Decimal128 x = Decimal128.Parse(value, CultureInfo.InvariantCulture);
            AssertClose(expectedSin, (double)Decimal128.Sin(x), value, "sin");
            AssertClose(expectedCos, (double)Decimal128.Cos(x), value, "cos");
            AssertClose(expectedTan, (double)Decimal128.Tan(x), value, "tan");

            static void AssertClose(double expected, double actual, string value, string fn)
                => Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"{fn}({value}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // cos(+0) = 1
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // cos(-0) = 1
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // cos(+Infinity) = NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // cos(-Infinity) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // cos(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void CosTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Cos(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(-0.5)]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(2.5)]
        [InlineData(-3.25)]
        [InlineData(100.0)]
        [InlineData(-0.3)]
        [InlineData(-0.1)]
        public static void CosAccuracyTest(double input)
        {
            // Decimal128 evaluates cos in the software binary128 engine (as Intel does).
            double expected = double.Cos(input);
            double actual = (double)Decimal128.Cos((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"cos({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // tan(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // tan(-0) = -0
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // tan(+Infinity) = NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // tan(-Infinity) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // tan(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void TanTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Tan(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(-0.5)]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(0.25)]
        [InlineData(-0.75)]
        [InlineData(-0.2)]
        [InlineData(-0.1)]
        public static void TanAccuracyTest(double input)
        {
            // Decimal128 evaluates tan in the software binary128 engine (as Intel does).
            double expected = double.Tan(input);
            double actual = (double)Decimal128.Tan((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"tan({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // sincos(+0) = (+0, 1)
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // sincos(-0) = (-0, 1)
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // sincos(+Infinity) = (NaN, NaN)
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // sincos(NaN) = (NaN, NaN)
        public static void SinCosTest(ulong valueUpper, ulong valueLower, ulong expectedSinUpper, ulong expectedSinLower, ulong expectedCosUpper, ulong expectedCosLower)
        {
            (Decimal128 sin, Decimal128 cos) = Decimal128.SinCos(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedSinUpper, expectedSinLower), Unsafe.BitCast<Decimal128, UInt128>(sin));
            Assert.Equal(new UInt128(expectedCosUpper, expectedCosLower), Unsafe.BitCast<Decimal128, UInt128>(cos));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(-1.0)]
        [InlineData(2.5)]
        [InlineData(-0.1)]
        public static void SinCosAccuracyTest(double input)
        {
            (Decimal128 sin, Decimal128 cos) = Decimal128.SinCos((Decimal128)input);
            Assert.True(double.Abs((double)sin - double.Sin(input)) <= 1e-13 * double.Abs(double.MaxMagnitude(double.Sin(input), 1.0)), $"sincos({input}).Sin");
            Assert.True(double.Abs((double)cos - double.Cos(input)) <= 1e-13 * double.Abs(double.MaxMagnitude(double.Cos(input), 1.0)), $"sincos({input}).Cos");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // atan(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // atan(-0) = -0
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // atan(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void AtanTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Atan(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(-0.5)]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(2.5)]
        [InlineData(-3.25)]
        [InlineData(100.0)]
        [InlineData(double.PositiveInfinity)] // atan(+Infinity) = +pi/2
        [InlineData(double.NegativeInfinity)] // atan(-Infinity) = -pi/2
        public static void AtanAccuracyTest(double input)
        {
            // Decimal128 evaluates atan in the software binary128 engine (as Intel does).
            double expected = double.Atan(input);
            double actual = (double)Decimal128.Atan((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"atan({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // asin(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // asin(-0) = -0
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // asin(2) is outside [-1, 1] -> NaN
        [InlineData(0xB040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // asin(-2) is outside [-1, 1] -> NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // asin(+Infinity) = NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // asin(-Infinity) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // asin(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void AsinTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Asin(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(-0.5)]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(0.25)]
        [InlineData(-0.75)]
        public static void AsinAccuracyTest(double input)
        {
            // Decimal128 evaluates asin in the software binary128 engine (as Intel does).
            double expected = double.Asin(input);
            double actual = (double)Decimal128.Asin((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"asin({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // acos(2) is outside [-1, 1] -> NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // acos(+Infinity) = NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // acos(-Infinity) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // acos(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void AcosTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Acos(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(-0.5)]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(0.25)]
        [InlineData(-0.75)]
        public static void AcosAccuracyTest(double input)
        {
            // Decimal128 evaluates acos in the software binary128 engine (as Intel does).
            double expected = double.Acos(input);
            double actual = (double)Decimal128.Acos((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"acos({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL, 0x3040000000000000UL, 0x0000000000000000UL)] // atan2(+0, +1) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL, 0xB040000000000000UL, 0x0000000000000000UL)] // atan2(-0, +1) = -0
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // atan2(NaN, x) = NaN
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // atan2(y, NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0x3040000000000000UL, 0x0000000000000001UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        public static void Atan2Test(ulong yUpper, ulong yLower, ulong xUpper, ulong xLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Atan2(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(yUpper, yLower)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(xUpper, xLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(1.0, 1.0)]
        [InlineData(-1.0, 1.0)]
        [InlineData(1.0, -1.0)]
        [InlineData(-1.0, -1.0)]
        [InlineData(0.5, 2.0)]
        [InlineData(1.0, 0.0)]
        [InlineData(-1.0, 0.0)]
        [InlineData(0.0, -1.0)]
        [InlineData(double.PositiveInfinity, 1.0)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public static void Atan2AccuracyTest(double y, double x)
        {
            // Decimal128 evaluates atan2 in the software binary128 engine (as Intel does).
            double expected = double.Atan2(y, x);
            double actual = (double)Decimal128.Atan2((Decimal128)y, (Decimal128)x);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"atan2({y}, {x}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // sinPi(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // sinPi(-0) = -0
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // sinPi(+Infinity) = NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // sinPi(-Infinity) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // sinPi(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void SinPiTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.SinPi(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData("0.25", "0.707106781186547524400844362104849039284835938", 2.0)]
        [InlineData("-0.75", "-0.707106781186547524400844362104849039284835938", 2.0)]
        [InlineData("2.25", "0.707106781186547524400844362104849039284835938", 2.0)]
        [InlineData("0.1", "0.309016994374947424102293417182819058860154590", 2.0)]
        [InlineData("-2.75", "-0.707106781186547524400844362104849039284835938", 2.0)]
        [InlineData("1234.567", "0.977929339830721821623106314809873749321959736", 2.0)]
        [InlineData("0.5", "1.00000000000000000000000000000000000000000000", 0.0)] // sinPi(1/2) = 1 exactly
        [InlineData("-0.5", "-1.00000000000000000000000000000000000000000000", 0.0)]
        [InlineData("1", "0.0", 0.0)] // sinPi(integer) is an exact zero
        [InlineData("2", "0.0", 0.0)]
        public static void SinPiAccuracyTest(string input, string oracle, double ulpLimit)
        {
            // The engine evaluates in software binary128 (as Intel does), so the result is compared to a
            // high-precision oracle -- the true value rounded to Decimal128 by the independently tested parser --
            // in decimal ULPs. Exact identities use a 0 ULP limit; near-singular arguments a documented wider one.
            Decimal128 actual = Decimal128.SinPi(Decimal128.Parse(input, CultureInfo.InvariantCulture));
            Decimal128 expected = Decimal128.Parse(oracle, CultureInfo.InvariantCulture);
            DecimalIeee754IntelTestData.AssertResultWithinUlp(
                Unsafe.BitCast<Decimal128, UInt128>(actual),
                Unsafe.BitCast<Decimal128, UInt128>(expected),
                recordedUlp: 0.0, limit: ulpLimit);
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // cosPi(+0) = 1
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // cosPi(-0) = 1
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // cosPi(+Infinity) = NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // cosPi(-Infinity) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // cosPi(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void CosPiTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.CosPi(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.5)]
        [InlineData(-0.5)]
        [InlineData(1.5)]
        [InlineData(-1.5)]
        public static void CosPiHalfIntegerReturnsPositiveZero(double input)
        {
            // cosPi at a half-integer is +0; comparing through double hides the sign, so check the raw sign bit.
            Decimal128 cosPi = Decimal128.CosPi((Decimal128)input);
            Assert.Equal(0.0, (double)cosPi);
            Assert.Equal(UInt128.Zero, Unsafe.BitCast<Decimal128, UInt128>(cosPi) >> 127);

            (Decimal128 _, Decimal128 cos) = Decimal128.SinCosPi((Decimal128)input);
            Assert.Equal(0.0, (double)cos);
            Assert.Equal(UInt128.Zero, Unsafe.BitCast<Decimal128, UInt128>(cos) >> 127);
        }

        [Theory]
        [InlineData("0.25", "0.707106781186547524400844362104849039284835938", 2.0)]
        [InlineData("-0.75", "-0.707106781186547524400844362104849039284835938", 2.0)]
        [InlineData("2.25", "0.707106781186547524400844362104849039284835938", 2.0)]
        [InlineData("0.1", "0.951056516295153572116439333379382143405698634", 2.0)]
        [InlineData("1234.567", "-0.208935890402411702274907259384464393664923236", 2.0)]
        [InlineData("0.4999999", "0.000000314159265358974156133484288383422682765979151", 32.0)] // near a zero -> cancellation
        [InlineData("1", "-1.00000000000000000000000000000000000000000000", 0.0)] // cosPi(odd integer) = -1 exactly
        [InlineData("2", "1.00000000000000000000000000000000000000000000", 0.0)] // cosPi(even integer) = 1 exactly
        [InlineData("0.5", "0.0", 0.0)] // cosPi(half-integer) is an exact zero
        [InlineData("1.5", "0.0", 0.0)]
        public static void CosPiAccuracyTest(string input, string oracle, double ulpLimit)
        {
            Decimal128 actual = Decimal128.CosPi(Decimal128.Parse(input, CultureInfo.InvariantCulture));
            Decimal128 expected = Decimal128.Parse(oracle, CultureInfo.InvariantCulture);
            DecimalIeee754IntelTestData.AssertResultWithinUlp(
                Unsafe.BitCast<Decimal128, UInt128>(actual),
                Unsafe.BitCast<Decimal128, UInt128>(expected),
                recordedUlp: 0.0, limit: ulpLimit);
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // tanPi(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // tanPi(-0) = -0
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // tanPi(+Infinity) = NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // tanPi(-Infinity) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // tanPi(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void TanPiTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.TanPi(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Fact]
        public static void TanPiPoleTest()
        {
            // Half-integer arguments are poles; tanPi returns a signed infinity matching sinPi's sign.
            Assert.Equal(new UInt128(0x7800000000000000UL, 0x0000000000000000UL), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.TanPi((Decimal128)0.5)));
            Assert.Equal(new UInt128(0xF800000000000000UL, 0x0000000000000000UL), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.TanPi((Decimal128)1.5)));
        }

        [Theory]
        [InlineData("0.125", "0.414213562373095048801688724209698078569671875", 2.0)]
        [InlineData("-0.375", "-2.41421356237309504880168872420969807856967188", 2.0)]
        [InlineData("0.1", "0.324919696232906326155871412215134464954903472", 2.0)]
        [InlineData("0.499", "318.308838985550445921686695436921420182774937", 2.0)]
        [InlineData("0", "0.0", 0.0)]
        [InlineData("1", "-0", 0.0)] // tanPi(odd integer) = -0 (sin=+0, cos=-1)
        [InlineData("2", "0.0", 0.0)]
        public static void TanPiAccuracyTest(string input, string oracle, double ulpLimit)
        {
            Decimal128 actual = Decimal128.TanPi(Decimal128.Parse(input, CultureInfo.InvariantCulture));
            Decimal128 expected = Decimal128.Parse(oracle, CultureInfo.InvariantCulture);
            DecimalIeee754IntelTestData.AssertResultWithinUlp(
                Unsafe.BitCast<Decimal128, UInt128>(actual),
                Unsafe.BitCast<Decimal128, UInt128>(expected),
                recordedUlp: 0.0, limit: ulpLimit);
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // sinCosPi(+0) = (+0, 1)
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // sinCosPi(-0) = (-0, 1)
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // sinCosPi(+Infinity) = (NaN, NaN)
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // sinCosPi(NaN) = (NaN, NaN)
        public static void SinCosPiTest(ulong valueUpper, ulong valueLower, ulong expectedSinUpper, ulong expectedSinLower, ulong expectedCosUpper, ulong expectedCosLower)
        {
            (Decimal128 sin, Decimal128 cos) = Decimal128.SinCosPi(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedSinUpper, expectedSinLower), Unsafe.BitCast<Decimal128, UInt128>(sin));
            Assert.Equal(new UInt128(expectedCosUpper, expectedCosLower), Unsafe.BitCast<Decimal128, UInt128>(cos));
        }

        [Theory]
        [InlineData("0.25", "0.707106781186547524400844362104849039284835938", "0.707106781186547524400844362104849039284835938", 2.0)]
        [InlineData("-0.75", "-0.707106781186547524400844362104849039284835938", "-0.707106781186547524400844362104849039284835938", 2.0)]
        [InlineData("0.1", "0.309016994374947424102293417182819058860154590", "0.951056516295153572116439333379382143405698634", 2.0)]
        [InlineData("1234.567", "0.977929339830721821623106314809873749321959736", "-0.208935890402411702274907259384464393664923236", 2.0)]
        [InlineData("0.5", "1.00000000000000000000000000000000000000000000", "0.0", 0.0)]
        public static void SinCosPiAccuracyTest(string input, string sinOracle, string cosOracle, double ulpLimit)
        {
            (Decimal128 sin, Decimal128 cos) = Decimal128.SinCosPi(Decimal128.Parse(input, CultureInfo.InvariantCulture));
            DecimalIeee754IntelTestData.AssertResultWithinUlp(
                Unsafe.BitCast<Decimal128, UInt128>(sin),
                Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Parse(sinOracle, CultureInfo.InvariantCulture)),
                recordedUlp: 0.0, limit: ulpLimit);
            DecimalIeee754IntelTestData.AssertResultWithinUlp(
                Unsafe.BitCast<Decimal128, UInt128>(cos),
                Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Parse(cosOracle, CultureInfo.InvariantCulture)),
                recordedUlp: 0.0, limit: ulpLimit);
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // atanPi(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // atanPi(-0) = -0
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // atanPi(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void AtanPiTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.AtanPi(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(double.PositiveInfinity, 0.5)] // atanPi(+Infinity) = +1/2 exactly
        [InlineData(double.NegativeInfinity, -0.5)] // atanPi(-Infinity) = -1/2 exactly
        public static void AtanPiInfinityTest(double input, double expected)
        {
            Assert.Equal(expected, (double)Decimal128.AtanPi((Decimal128)input));
        }

        [Theory]
        [InlineData("0.5", "0.147583617650433274175401076224740525951134524", 2.0)]
        [InlineData("-1.25", "-0.285223287477277274422189653693486081234733538", 2.0)]
        [InlineData("0.1", "0.0317255174305535695149771186013020006193286726", 2.0)]
        [InlineData("9999999", "0.499999968169008198521858801725742756587314478", 2.0)]
        [InlineData("0.25", "0.0779791303773693254605128897731301351165246188", 2.0)]
        [InlineData("0", "0.0", 0.0)]
        public static void AtanPiAccuracyTest(string input, string oracle, double ulpLimit)
        {
            Decimal128 actual = Decimal128.AtanPi(Decimal128.Parse(input, CultureInfo.InvariantCulture));
            Decimal128 expected = Decimal128.Parse(oracle, CultureInfo.InvariantCulture);
            DecimalIeee754IntelTestData.AssertResultWithinUlp(
                Unsafe.BitCast<Decimal128, UInt128>(actual),
                Unsafe.BitCast<Decimal128, UInt128>(expected),
                recordedUlp: 0.0, limit: ulpLimit);
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // asinPi(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // asinPi(-0) = -0
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // asinPi(2) is outside [-1, 1] -> NaN
        [InlineData(0xB040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // asinPi(-2) is outside [-1, 1] -> NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // asinPi(+Infinity) = NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // asinPi(-Infinity) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // asinPi(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void AsinPiTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.AsinPi(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData("0.25", "0.0804306232551662437709501933284842555840644312", 2.0)]
        [InlineData("-0.5", "-0.166666666666666666666666666666666666666666667", 2.0)]
        [InlineData("0.999", "0.485763562593760344929193647583989467842912869", 2.0)]
        [InlineData("0.9999999", "0.499857647490130293655918256194735962900618804", 2.0)]
        [InlineData("0.5", "0.166666666666666666666666666666666666666666667", 2.0)]
        [InlineData("1", "0.500000000000000000000000000000000000000000000", 0.0)] // asinPi(1) = 1/2
        [InlineData("-1", "-0.500000000000000000000000000000000000000000000", 0.0)]
        [InlineData("0", "0.0", 0.0)]
        public static void AsinPiAccuracyTest(string input, string oracle, double ulpLimit)
        {
            Decimal128 actual = Decimal128.AsinPi(Decimal128.Parse(input, CultureInfo.InvariantCulture));
            Decimal128 expected = Decimal128.Parse(oracle, CultureInfo.InvariantCulture);
            DecimalIeee754IntelTestData.AssertResultWithinUlp(
                Unsafe.BitCast<Decimal128, UInt128>(actual),
                Unsafe.BitCast<Decimal128, UInt128>(expected),
                recordedUlp: 0.0, limit: ulpLimit);
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // acosPi(2) is outside [-1, 1] -> NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // acosPi(+Infinity) = NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // acosPi(-Infinity) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // acosPi(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void AcosPiTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.AcosPi(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Fact]
        public static void AcosPiZeroTest()
        {
            // acosPi(+/-0) = 1/2 exactly.
            Assert.Equal(0.5, (double)Decimal128.AcosPi((Decimal128)0.0));
            Assert.Equal(0.5, (double)Decimal128.AcosPi((Decimal128)(-0.0)));
        }

        [Theory]
        [InlineData("0.25", "0.419569376744833756229049806671515744415935569", 2.0)]
        [InlineData("-0.5", "0.666666666666666666666666666666666666666666667", 2.0)]
        [InlineData("0.999", "0.0142364374062396550708063524160105321570871313", 2.0)]
        [InlineData("0.9999999", "0.000142352509869706344081743805264037099381195810", 32.0)] // near 1 -> cancellation
        [InlineData("0.5", "0.333333333333333333333333333333333333333333333", 2.0)]
        [InlineData("0", "0.500000000000000000000000000000000000000000000", 0.0)] // acosPi(0) = 1/2
        [InlineData("1", "0.0", 0.0)] // acosPi(1) = 0
        [InlineData("-1", "1.00000000000000000000000000000000000000000000", 0.0)] // acosPi(-1) = 1
        public static void AcosPiAccuracyTest(string input, string oracle, double ulpLimit)
        {
            Decimal128 actual = Decimal128.AcosPi(Decimal128.Parse(input, CultureInfo.InvariantCulture));
            Decimal128 expected = Decimal128.Parse(oracle, CultureInfo.InvariantCulture);
            DecimalIeee754IntelTestData.AssertResultWithinUlp(
                Unsafe.BitCast<Decimal128, UInt128>(actual),
                Unsafe.BitCast<Decimal128, UInt128>(expected),
                recordedUlp: 0.0, limit: ulpLimit);
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL, 0x3040000000000000UL, 0x0000000000000000UL)] // atan2Pi(+0, +1) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL, 0xB040000000000000UL, 0x0000000000000000UL)] // atan2Pi(-0, +1) = -0
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // atan2Pi(NaN, x) = NaN
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // atan2Pi(y, NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0x3040000000000000UL, 0x0000000000000001UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        public static void Atan2PiTest(ulong yUpper, ulong yLower, ulong xUpper, ulong xLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Atan2Pi(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(yUpper, yLower)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(xUpper, xLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(double.PositiveInfinity, 1.0, 0.5)] // atan2Pi(+Infinity, finite) = 1/2
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, 0.25)] // atan2Pi(+Infinity, +Infinity) = 1/4
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, -0.75)] // atan2Pi(-Infinity, -Infinity) = -3/4
        public static void Atan2PiInfinityTest(double y, double x, double expected)
        {
            Assert.Equal(expected, (double)Decimal128.Atan2Pi((Decimal128)y, (Decimal128)x));
        }

        [Theory]
        [InlineData("1", "2", "0.147583617650433274175401076224740525951134524", 2.0)]
        [InlineData("-1", "2", "-0.147583617650433274175401076224740525951134524", 2.0)]
        [InlineData("2", "1", "0.352416382349566725824598923775259474048865476", 2.0)]
        [InlineData("1", "-2", "0.852416382349566725824598923775259474048865476", 2.0)]
        [InlineData("0.1", "0.7", "0.0451672353008665483508021524494810519022690478", 2.0)]
        [InlineData("1234", "-5", "0.501289741265151584446027359785209733861286641", 2.0)]
        [InlineData("-1", "-1", "-0.750000000000000000000000000000000000000000000", 0.0)] // atan2Pi(-1, -1) = -3/4
        [InlineData("1", "0", "0.500000000000000000000000000000000000000000000", 0.0)] // atan2Pi(1, 0) = 1/2
        public static void Atan2PiAccuracyTest(string y, string x, string oracle, double ulpLimit)
        {
            Decimal128 actual = Decimal128.Atan2Pi(Decimal128.Parse(y, CultureInfo.InvariantCulture), Decimal128.Parse(x, CultureInfo.InvariantCulture));
            Decimal128 expected = Decimal128.Parse(oracle, CultureInfo.InvariantCulture);
            DecimalIeee754IntelTestData.AssertResultWithinUlp(
                Unsafe.BitCast<Decimal128, UInt128>(actual),
                Unsafe.BitCast<Decimal128, UInt128>(expected),
                recordedUlp: 0.0, limit: ulpLimit);
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // sinh(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // sinh(-0) = -0
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // sinh(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL)] // sinh(-Infinity) = -Infinity
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // sinh(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void SinhTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Sinh(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        [InlineData(-1.5)]
        [InlineData(2.0)]
        public static void SinhAccuracyTest(double input)
        {
            // Decimal128 evaluates sinh in the software binary128 engine (as Intel does).
            double expected = double.Sinh(input);
            double actual = (double)Decimal128.Sinh((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"sinh({input}): expected {expected}, got {actual}");
        }

        [Fact]
        public static void SinhLargeArgumentNoOverflowTest()
        {
            // Decimal128's exponent range exceeds binary128's, so the software engine evaluates large
            // arguments without the spurious overflow a hardware binary128 path would hit (sinh(5000) ~ 1e2171).
            Decimal128 result = Decimal128.Sinh((Decimal128)5000.0);
            Assert.True(Decimal128.IsFinite(result) && Decimal128.IsPositive(result));
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // cosh(+0) = 1
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // cosh(-0) = 1
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // cosh(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // cosh(-Infinity) = +Infinity
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // cosh(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void CoshTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Cosh(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        [InlineData(-1.5)]
        [InlineData(2.0)]
        public static void CoshAccuracyTest(double input)
        {
            // Decimal128 evaluates cosh in the software binary128 engine (as Intel does).
            double expected = double.Cosh(input);
            double actual = (double)Decimal128.Cosh((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"cosh({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // tanh(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // tanh(-0) = -0
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL)] // tanh(+Infinity) = 1
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000001UL)] // tanh(-Infinity) = -1
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // tanh(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void TanhTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Tanh(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        [InlineData(-1.5)]
        [InlineData(2.0)]
        public static void TanhAccuracyTest(double input)
        {
            // Decimal128 evaluates tanh in the software binary128 engine (as Intel does).
            double expected = double.Tanh(input);
            double actual = (double)Decimal128.Tanh((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"tanh({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // asinh(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // asinh(-0) = -0
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // asinh(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL)] // asinh(-Infinity) = -Infinity
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // asinh(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void AsinhTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Asinh(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        [InlineData(-1.5)]
        [InlineData(2.0)]
        public static void AsinhAccuracyTest(double input)
        {
            // Decimal128 evaluates asinh in the software binary128 engine (as Intel does).
            double expected = double.Asinh(input);
            double actual = (double)Decimal128.Asinh((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"asinh({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x3040000000000000UL, 0x0000000000000000UL)] // acosh(1) = +0
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // acosh(+Infinity) = +Infinity
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // acosh(-Infinity) is a domain error -> NaN
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // acosh(+0) is a domain error -> NaN
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // acosh(-1) is a domain error -> NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // acosh(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void AcoshTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Acosh(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(1.5)]
        [InlineData(2.0)]
        [InlineData(5.0)]
        [InlineData(100.0)]
        public static void AcoshAccuracyTest(double input)
        {
            // Decimal128 evaluates acosh in the software binary128 engine (as Intel does).
            double expected = double.Acosh(input);
            double actual = (double)Decimal128.Acosh((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"acosh({input}): expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000000UL)] // atanh(+0) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000000UL, 0xB040000000000000UL, 0x0000000000000000UL)] // atanh(-0) = -0
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x7800000000000000UL, 0x0000000000000000UL)] // atanh(+1) = +Infinity (pole)
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0xF800000000000000UL, 0x0000000000000000UL)] // atanh(-1) = -Infinity (pole)
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // atanh(+Infinity) is a domain error -> NaN
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // atanh(-Infinity) is a domain error -> NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // atanh(NaN) = NaN
        [InlineData(0xFC00000000000000UL, 0x0000000000001234UL, 0xFC00000000000000UL, 0x0000000000001234UL)] // NaN payload preserved
        [InlineData(0xFC00400000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // out-of-range NaN payload cleared
        public static void AtanhTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Atanh(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.25)]
        [InlineData(-0.5)]
        [InlineData(0.75)]
        [InlineData(-0.9)]
        public static void AtanhAccuracyTest(double input)
        {
            // Decimal128 evaluates atanh in the software binary128 engine (as Intel does).
            double expected = double.Atanh(input);
            double actual = (double)Decimal128.Atanh((Decimal128)input);
            Assert.True(double.Abs(actual - expected) <= 1e-13 * double.Abs(double.MaxMagnitude(expected, 1.0)), $"atanh({input}): expected {expected}, got {actual}");
        }

        [Fact]
        public static void AcoshLargeArgumentNoOverflowTest()
        {
            // Decimal128's exponent range exceeds binary128's, so the software engine evaluates large
            // arguments without the spurious overflow a hardware binary128 path would hit (acosh(1e300) ~ 691).
            Decimal128 result = Decimal128.Acosh((Decimal128)1e300);
            Assert.True(Decimal128.IsFinite(result) && Decimal128.IsPositive(result));
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x303C000000000000UL, 0x0000000000000001UL, 0x303C000000000000UL, 0x0000000000000064UL)] // quantize(1, 1E-2) = 1.00 (exact scale up)
        [InlineData(0x303E000000000000UL, 0x0000000000000019UL, 0x3040000000000000UL, 0x0000000000000001UL, 0x3040000000000000UL, 0x0000000000000002UL)] // quantize(2.5, 1E0) = 2 (ties to even)
        [InlineData(0x303E000000000000UL, 0x0000000000000023UL, 0x3040000000000000UL, 0x0000000000000001UL, 0x3040000000000000UL, 0x0000000000000004UL)] // quantize(3.5, 1E0) = 4 (ties to even)
        [InlineData(0x303A000000000000UL, 0x00000000000004D2UL, 0x303C000000000000UL, 0x0000000000000001UL, 0x303C000000000000UL, 0x000000000000007BUL)] // quantize(1.234, 1E-2) = 1.23
        [InlineData(0x3040000000000000UL, 0x000000000012D687UL, 0x303E000000000000UL, 0x0000000000000001UL, 0x303E000000000000UL, 0x0000000000BC6146UL)] // quantize(1234567, 1E-1) = 1234567.0
        [InlineData(0xB04A000000000000UL, 0x0000000000000000UL, 0x303C000000000000UL, 0x0000000000000001UL, 0xB03C000000000000UL, 0x0000000000000000UL)] // quantize(-0E5, 1E-2) = -0E-2 (target quantum)
        [InlineData(0x3040000000000000UL, 0x0000000000000004UL, 0x3044000000000000UL, 0x0000000000000001UL, 0x3044000000000000UL, 0x0000000000000000UL)] // quantize(4, 1E2) = 0E2 (rounds to zero)
        [InlineData(0x3040000000000000UL, 0x000000000000003CUL, 0x3044000000000000UL, 0x0000000000000001UL, 0x3044000000000000UL, 0x0000000000000001UL)] // quantize(60, 1E2) = 1E2 (rounds up)
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // quantize(+Inf, +Inf) = +Inf
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL)] // quantize(-Inf, +Inf) = -Inf (sign of x)
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // quantize(+Inf, finite) = NaN
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // quantize(finite, +Inf) = NaN
        [InlineData(0x7C00000000000000UL, 0x0000000000001234UL, 0x3040000000000000UL, 0x0000000000000001UL, 0x7C00000000000000UL, 0x0000000000001234UL)] // quantize(qNaN, finite) = qNaN (payload preserved)
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x7C00000000000000UL, 0x0000000000002222UL, 0x7C00000000000000UL, 0x0000000000002222UL)] // quantize(finite, qNaN) = qNaN (payload preserved)
        public static void QuantizeTest(ulong valueUpper, ulong valueLower, ulong quantumUpper, ulong quantumLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.Quantize(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)), Unsafe.BitCast<UInt128, Decimal128>(new UInt128(quantumUpper, quantumLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0x303C000000000000UL, 0x0000000000003039UL, 0x303C000000000000UL, 0x0000000000000001UL)] // quantum(123.45) = 1E-2
        [InlineData(0xB040000000000000UL, 0x0000000000000007UL, 0x3040000000000000UL, 0x0000000000000001UL)] // quantum(-7) = 1E0 (always positive)
        [InlineData(0x304A000000000000UL, 0x0000000000000000UL, 0x304A000000000000UL, 0x0000000000000001UL)] // quantum(0E5) = 1E5
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // quantum(+Inf) = +Inf
        [InlineData(0xF800000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL)] // quantum(-Inf) = +Inf (sign cleared)
        [InlineData(0x7C00000000000000UL, 0x0000000000001234UL, 0x7C00000000000000UL, 0x0000000000001234UL)] // quantum(qNaN) = qNaN (payload preserved)
        [InlineData(0xFC00000000000000UL, 0x0000000000000000UL, 0xFC00000000000000UL, 0x0000000000000000UL)] // quantum(-NaN) = -NaN (propagated)
        public static void QuantumTest(ulong valueUpper, ulong valueLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 result = Decimal128.GetQuantum(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(valueUpper, valueLower)));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x3040000000000000UL, 0x00000000000003E7UL, true)]  // same exponent
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x303E000000000000UL, 0x0000000000000001UL, false)] // different exponent
        [InlineData(0x7C00000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL, true)]  // both NaN
        [InlineData(0x7C00000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL, false)] // NaN vs finite
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL, true)]  // both Infinity
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL, false)] // Infinity vs NaN
        [InlineData(0x7800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000001UL, false)] // Infinity vs finite
        public static void SameQuantumTest(ulong xUpper, ulong xLower, ulong yUpper, ulong yLower, bool expected)
        {
            Decimal128 x = Unsafe.BitCast<UInt128, Decimal128>(new UInt128(xUpper, xLower));
            Decimal128 y = Unsafe.BitCast<UInt128, Decimal128>(new UInt128(yUpper, yLower));
            Assert.Equal(expected, Decimal128.HaveSameQuantum(x, y));
        }


        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x3040000000000000UL, 0x0000000000000003UL, 0x3040000000000000UL, 0x0000000000000004UL, 0x3040000000000000UL, 0x000000000000000AUL)] // 2 * 3 + 4 = 10
        [InlineData(0x3034000000000000UL, 0x00000000000F4241UL, 0x3034000000000000UL, 0x00000000000F4241UL, 0xB034000000000000UL, 0x00000000000F4242UL, 0x3028000000000000UL, 0x0000000000000001UL)] // 1.000001 * 1.000001 - 1.000002 = 1E-12 (fused)
        [InlineData(0x304A000000000000UL, 0x0000000000000000UL, 0x3044000000000000UL, 0x0000000000000003UL, 0x303A000000000000UL, 0x0000000000000007UL, 0x303A000000000000UL, 0x0000000000000007UL)] // 0E5 * 3E2 + 7E-3
        [InlineData(0x3040000000000000UL, 0x0000000000000003UL, 0x3040000000000000UL, 0x0000000000000004UL, 0x3040000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x000000000000000CUL)] // 3 * 4 + 0 = 12
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x3040000000000000UL, 0x0000000000000003UL, 0xB040000000000000UL, 0x0000000000000006UL, 0x3040000000000000UL, 0x0000000000000000UL)] // 2 * 3 + (-6) = +0
        [InlineData(0xB040000000000000UL, 0x0000000000000002UL, 0x3040000000000000UL, 0x0000000000000003UL, 0x3040000000000000UL, 0x0000000000000006UL, 0x3040000000000000UL, 0x0000000000000000UL)] // -2 * 3 + 6 = +0
        [InlineData(0x3040000000000000UL, 0x0000000000000003UL, 0x7C00000000000000UL, 0x0000000000001234UL, 0x3040000000000000UL, 0x0000000000000004UL, 0x7C00000000000000UL, 0x0000000000001234UL)] // 3 * qNaN(0x1234) + 4 -> qNaN
        [InlineData(0x7C00000000000000UL, 0x0000000000000011UL, 0x3040000000000000UL, 0x0000000000000002UL, 0x7C00000000000000UL, 0x0000000000000022UL, 0x7C00000000000000UL, 0x0000000000000022UL)] // qNaN(x) * 2 + qNaN(z) -> z payload
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x7800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000003UL, 0x7800000000000000UL, 0x0000000000000000UL)] // 2 * +Inf + 3 = +Inf
        [InlineData(0xB040000000000000UL, 0x0000000000000002UL, 0x7800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000003UL, 0xF800000000000000UL, 0x0000000000000000UL)] // -2 * +Inf + 3 = -Inf
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x7800000000000000UL, 0x0000000000000000UL, 0x3040000000000000UL, 0x0000000000000005UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // 0 * +Inf + 5 -> qNaN
        [InlineData(0x3040000000000000UL, 0x0000000000000002UL, 0x7800000000000000UL, 0x0000000000000000UL, 0xF800000000000000UL, 0x0000000000000000UL, 0x7C00000000000000UL, 0x0000000000000000UL)] // 2 * +Inf + (-Inf) -> qNaN
        public static void FusedMultiplyAddTest(ulong xUpper, ulong xLower, ulong yUpper, ulong yLower, ulong zUpper, ulong zLower, ulong expectedUpper, ulong expectedLower)
        {
            Decimal128 x = Unsafe.BitCast<UInt128, Decimal128>(new UInt128(xUpper, xLower));
            Decimal128 y = Unsafe.BitCast<UInt128, Decimal128>(new UInt128(yUpper, yLower));
            Decimal128 z = Unsafe.BitCast<UInt128, Decimal128>(new UInt128(zUpper, zLower));
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.FusedMultiplyAdd(x, y, z)));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128BitDecrement), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void BitDecrement_IntelReferenceVectors(UInt128 value, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.BitDecrement(Unsafe.BitCast<UInt128, Decimal128>(value))));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128BitIncrement), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void BitIncrement_IntelReferenceVectors(UInt128 value, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.BitIncrement(Unsafe.BitCast<UInt128, Decimal128>(value))));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128ILogB), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void ILogB_IntelReferenceVectors(UInt128 value, int expected)
        {
            Assert.Equal(expected, Decimal128.ILogB(Unsafe.BitCast<UInt128, Decimal128>(value)));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128ScaleB), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void ScaleB_IntelReferenceVectors(UInt128 value, int n, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.ScaleB(Unsafe.BitCast<UInt128, Decimal128>(value), n)));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128FusedMultiplyAdd), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void FusedMultiplyAdd_IntelReferenceVectors(UInt128 x, UInt128 y, UInt128 z, UInt128 expected)
        {
            Decimal128 result = Decimal128.FusedMultiplyAdd(Unsafe.BitCast<UInt128, Decimal128>(x), Unsafe.BitCast<UInt128, Decimal128>(y), Unsafe.BitCast<UInt128, Decimal128>(z));
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128TranscendentalUnary), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void TranscendentalUnary_IntelReferenceVectors(string operation, UInt128 value, UInt128 expected, double recordedUlp)
        {
            Decimal128 x = Unsafe.BitCast<UInt128, Decimal128>(value);

            Decimal128 result = operation switch
            {
                "sin" => Decimal128.Sin(x),
                "cos" => Decimal128.Cos(x),
                "tan" => Decimal128.Tan(x),
                "asin" => Decimal128.Asin(x),
                "acos" => Decimal128.Acos(x),
                "atan" => Decimal128.Atan(x),
                "sinh" => Decimal128.Sinh(x),
                "cosh" => Decimal128.Cosh(x),
                "tanh" => Decimal128.Tanh(x),
                "asinh" => Decimal128.Asinh(x),
                "acosh" => Decimal128.Acosh(x),
                "atanh" => Decimal128.Atanh(x),
                "exp" => Decimal128.Exp(x),
                "exp2" => Decimal128.Exp2(x),
                "exp10" => Decimal128.Exp10(x),
                "expm1" => Decimal128.ExpM1(x),
                "log" => Decimal128.Log(x),
                "log2" => Decimal128.Log2(x),
                "log10" => Decimal128.Log10(x),
                "log1p" => Decimal128.LogP1(x),
                "cbrt" => Decimal128.Cbrt(x),
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            DecimalIeee754IntelTestData.AssertResultWithinUlp(Unsafe.BitCast<Decimal128, UInt128>(result), expected, recordedUlp);
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128TranscendentalBinary), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void TranscendentalBinary_IntelReferenceVectors(string operation, UInt128 left, UInt128 right, UInt128 expected, double recordedUlp)
        {
            Decimal128 x = Unsafe.BitCast<UInt128, Decimal128>(left);
            Decimal128 y = Unsafe.BitCast<UInt128, Decimal128>(right);

            Decimal128 result = operation switch
            {
                "atan2" => Decimal128.Atan2(x, y),
                "pow" => Decimal128.Pow(x, y),
                "hypot" => Decimal128.Hypot(x, y),
                _ => throw new InvalidOperationException($"Unexpected operation '{operation}'."),
            };

            DecimalIeee754IntelTestData.AssertResultWithinUlp(Unsafe.BitCast<Decimal128, UInt128>(result), expected, recordedUlp);
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
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128Modulus), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void op_Modulus_IntelReferenceVectors(UInt128 left, UInt128 right, UInt128 expected)
        {
            Decimal128 l = Unsafe.BitCast<UInt128, Decimal128>(left);
            Decimal128 r = Unsafe.BitCast<UInt128, Decimal128>(right);

            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(l % r));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128Remainder), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void Ieee754Remainder_IntelReferenceVectors(UInt128 left, UInt128 right, UInt128 expected)
        {
            Decimal128 l = Unsafe.BitCast<UInt128, Decimal128>(left);
            Decimal128 r = Unsafe.BitCast<UInt128, Decimal128>(right);

            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Ieee754Remainder(l, r)));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128Sqrt), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void Sqrt_IntelReferenceVectors(UInt128 value, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.Sqrt(Unsafe.BitCast<UInt128, Decimal128>(value))));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128Quantize), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void Quantize_IntelReferenceVectors(UInt128 value, UInt128 quantum, UInt128 expected)
        {
            Decimal128 result = Decimal128.Quantize(Unsafe.BitCast<UInt128, Decimal128>(value), Unsafe.BitCast<UInt128, Decimal128>(quantum));
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128Quantum), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void Quantum_IntelReferenceVectors(UInt128 value, UInt128 expected)
        {
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(Decimal128.GetQuantum(Unsafe.BitCast<UInt128, Decimal128>(value))));
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
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128RoundIntegral), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void RoundIntegral_IntelReferenceVectors(string operation, UInt128 value, UInt128 expected)
        {
            Decimal128 v = Unsafe.BitCast<UInt128, Decimal128>(value);

            Decimal128 result = operation switch
            {
                "round_integral_exact" => Decimal128.Round(v, 0, MidpointRounding.ToEven),
                "round_integral_nearest_even" => Decimal128.Round(v, 0, MidpointRounding.ToEven),
                "round_integral_nearest_away" => Decimal128.Round(v, 0, MidpointRounding.AwayFromZero),
                "round_integral_negative" => Decimal128.Floor(v),
                "round_integral_positive" => Decimal128.Ceiling(v),
                "round_integral_zero" => Decimal128.Truncate(v),
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

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128FromInteger), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void ConvertFromInteger_IntelReferenceVectors(string integerType, string operand, UInt128 expected)
        {
            Decimal128 result = integerType switch
            {
                "int32" => (Decimal128)int.Parse(operand, CultureInfo.InvariantCulture),
                "int64" => (Decimal128)long.Parse(operand, CultureInfo.InvariantCulture),
                "uint32" => (Decimal128)DecimalIeee754IntelTestData.ParseUInt32(operand),
                "uint64" => (Decimal128)DecimalIeee754IntelTestData.ParseUInt64(operand),
                _ => throw new InvalidOperationException($"Unexpected integer type '{integerType}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128ToInteger), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void ConvertToInteger_IntelReferenceVectors(string integerType, UInt128 value, string expected)
        {
            Decimal128 v = Unsafe.BitCast<UInt128, Decimal128>(value);

            switch (integerType)
            {
                case "int8": Assert.Equal(sbyte.Parse(expected, CultureInfo.InvariantCulture), (sbyte)v); break;
                case "int16": Assert.Equal(short.Parse(expected, CultureInfo.InvariantCulture), (short)v); break;
                case "int32": Assert.Equal(int.Parse(expected, CultureInfo.InvariantCulture), (int)v); break;
                case "int64": Assert.Equal(long.Parse(expected, CultureInfo.InvariantCulture), (long)v); break;
                case "uint8": Assert.Equal(DecimalIeee754IntelTestData.ParseUInt8(expected), (byte)v); break;
                case "uint16": Assert.Equal(DecimalIeee754IntelTestData.ParseUInt16(expected), (ushort)v); break;
                case "uint32": Assert.Equal(DecimalIeee754IntelTestData.ParseUInt32(expected), (uint)v); break;
                case "uint64": Assert.Equal(DecimalIeee754IntelTestData.ParseUInt64(expected), (ulong)v); break;
                default: throw new InvalidOperationException($"Unexpected integer type '{integerType}'.");
            }
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128ToBinary), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void ConvertToBinary_IntelReferenceVectors(string binaryType, UInt128 value, ulong expected)
        {
            Decimal128 v = Unsafe.BitCast<UInt128, Decimal128>(value);

            switch (binaryType)
            {
                case "binary32": Assert.Equal((uint)expected, BitConverter.SingleToUInt32Bits((float)v)); break;
                case "binary64": Assert.Equal(expected, BitConverter.DoubleToUInt64Bits((double)v)); break;
                default: throw new InvalidOperationException($"Unexpected binary type '{binaryType}'.");
            }
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128FromBinary), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void ConvertFromBinary_IntelReferenceVectors(string binaryType, ulong value, UInt128 expected)
        {
            Decimal128 result = binaryType switch
            {
                "binary32" => (Decimal128)BitConverter.UInt32BitsToSingle((uint)value),
                "binary64" => (Decimal128)BitConverter.UInt64BitsToDouble(value),
                _ => throw new InvalidOperationException($"Unexpected binary type '{binaryType}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [ConditionalTheory(typeof(DecimalIeee754IntelTestData), nameof(DecimalIeee754IntelTestData.IsAvailable))]
        [MemberData(nameof(DecimalIeee754IntelTestData.Decimal128Cross), MemberType = typeof(DecimalIeee754IntelTestData))]
        public static void ConvertFromDecimal_IntelReferenceVectors(string decimalType, UInt128 value, UInt128 expected)
        {
            Decimal128 result = decimalType switch
            {
                "bid32" => (Decimal128)Unsafe.BitCast<uint, Decimal32>((uint)value),
                "bid64" => (Decimal128)Unsafe.BitCast<ulong, Decimal64>((ulong)value),
                _ => throw new InvalidOperationException($"Unexpected decimal type '{decimalType}'."),
            };

            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Theory]
        [InlineData("0", 0x3040000000000000UL, 0x0000000000000000UL)]
        [InlineData("0.00", 0x303C000000000000UL, 0x0000000000000000UL)]
        [InlineData("1", 0x3040000000000000UL, 0x0000000000000001UL)]
        [InlineData("1.00", 0x303C000000000000UL, 0x0000000000000064UL)]
        [InlineData("-1", 0xB040000000000000UL, 0x0000000000000001UL)]
        [InlineData("-1.00", 0xB03C000000000000UL, 0x0000000000000064UL)]
        [InlineData("0.5", 0x303E000000000000UL, 0x0000000000000005UL)]
        [InlineData("-0.5", 0xB03E000000000000UL, 0x0000000000000005UL)]
        [InlineData("123.456", 0x303A000000000000UL, 0x000000000001E240UL)]
        [InlineData("-123.456", 0xB03A000000000000UL, 0x000000000001E240UL)]
        [InlineData("3.14159265358979", 0x3024000000000000UL, 0x00011DB9E76A2483UL)]
        [InlineData("79228162514264337593543950335", 0x30400000FFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL)]
        [InlineData("-79228162514264337593543950335", 0xB0400000FFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL)]
        [InlineData("0.0000000000000000000000000001", 0x3008000000000000UL, 0x0000000000000001UL)]
        [InlineData("12345678901234567890123456789", 0x3040000027E41B32UL, 0x46BEC9B16E398115UL)]
        [InlineData("9999999", 0x3040000000000000UL, 0x000000000098967FUL)]
        [InlineData("10000000", 0x3040000000000000UL, 0x0000000000989680UL)]
        [InlineData("2.5", 0x303E000000000000UL, 0x0000000000000019UL)]
        [InlineData("0.1", 0x303E000000000000UL, 0x0000000000000001UL)]
        [InlineData("0.2", 0x303E000000000000UL, 0x0000000000000002UL)]
        [InlineData("0.3", 0x303E000000000000UL, 0x0000000000000003UL)]
        public static void ConvertFromSystemDecimalTest(string value, ulong expectedUpper, ulong expectedLower)
        {
            decimal d = decimal.Parse(value, CultureInfo.InvariantCulture);
            Assert.Equal(new UInt128(expectedUpper, expectedLower), Unsafe.BitCast<Decimal128, UInt128>((Decimal128)d));
        }

        [Theory]
        [InlineData(0x3040000000000000UL, 0x0000000000000000UL, 0x00000000, 0x00000000, 0x00000000, 0, false)]
        [InlineData(0x303C000000000000UL, 0x0000000000000000UL, 0x00000000, 0x00000000, 0x00000000, 2, false)]
        [InlineData(0x3040000000000000UL, 0x0000000000000001UL, 0x00000001, 0x00000000, 0x00000000, 0, false)]
        [InlineData(0x303C000000000000UL, 0x0000000000000064UL, 0x00000064, 0x00000000, 0x00000000, 2, false)]
        [InlineData(0xB040000000000000UL, 0x0000000000000001UL, 0x00000001, 0x00000000, 0x00000000, 0, true)]
        [InlineData(0xB03C000000000000UL, 0x0000000000000064UL, 0x00000064, 0x00000000, 0x00000000, 2, true)]
        [InlineData(0x303E000000000000UL, 0x0000000000000005UL, 0x00000005, 0x00000000, 0x00000000, 1, false)]
        [InlineData(0xB03E000000000000UL, 0x0000000000000005UL, 0x00000005, 0x00000000, 0x00000000, 1, true)]
        [InlineData(0x303A000000000000UL, 0x000000000001E240UL, 0x0001E240, 0x00000000, 0x00000000, 3, false)]
        [InlineData(0xB03A000000000000UL, 0x000000000001E240UL, 0x0001E240, 0x00000000, 0x00000000, 3, true)]
        [InlineData(0x3024000000000000UL, 0x00011DB9E76A2483UL, 0xE76A2483, 0x00011DB9, 0x00000000, 14, false)]
        [InlineData(0x30400000FFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0, false)]
        [InlineData(0xB0400000FFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0, true)]
        [InlineData(0x3008000000000000UL, 0x0000000000000001UL, 0x00000001, 0x00000000, 0x00000000, 28, false)]
        [InlineData(0x3040000027E41B32UL, 0x46BEC9B16E398115UL, 0x6E398115, 0x46BEC9B1, 0x27E41B32, 0, false)]
        [InlineData(0x3040000000000000UL, 0x000000000098967FUL, 0x0098967F, 0x00000000, 0x00000000, 0, false)]
        [InlineData(0x3040000000000000UL, 0x0000000000989680UL, 0x00989680, 0x00000000, 0x00000000, 0, false)]
        [InlineData(0x303E000000000000UL, 0x0000000000000019UL, 0x00000019, 0x00000000, 0x00000000, 1, false)]
        [InlineData(0x303E000000000000UL, 0x0000000000000001UL, 0x00000001, 0x00000000, 0x00000000, 1, false)]
        [InlineData(0x303E000000000000UL, 0x0000000000000002UL, 0x00000002, 0x00000000, 0x00000000, 1, false)]
        [InlineData(0x303E000000000000UL, 0x0000000000000003UL, 0x00000003, 0x00000000, 0x00000000, 1, false)]
        public static void ConvertToSystemDecimalTest(ulong upper, ulong lower, uint lo, uint mid, uint hi, byte scale, bool isNegative)
        {
            decimal expected = new decimal((int)lo, (int)mid, (int)hi, isNegative, scale);
            decimal actual = (decimal)Unsafe.BitCast<UInt128, Decimal128>(new UInt128(upper, lower));

            Assert.Equal(expected, actual);
            Assert.Equal(decimal.GetBits(expected), decimal.GetBits(actual));
        }

        [Fact]
        public static void ConvertToSystemDecimalThrowsTest()
        {
            Assert.Throws<OverflowException>(() => (decimal)Decimal128.NaN);
            Assert.Throws<OverflowException>(() => (decimal)Decimal128.PositiveInfinity);
            Assert.Throws<OverflowException>(() => (decimal)Decimal128.NegativeInfinity);
            Assert.Throws<OverflowException>(() => (decimal)Decimal128.MaxValue);
            Assert.Throws<OverflowException>(() => (decimal)Decimal128.MinValue);
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
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x7C00000000000000, 0x0000000000001234) }; // non-canonical NaN operand is canonicalized
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x7C003FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // non-canonical NaN operand is canonicalized
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
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x7C00000000000000, 0x0000000000001234) }; // non-canonical NaN operand is canonicalized
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x7C003FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // non-canonical NaN operand is canonicalized
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
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) }; // non-canonical NaN dropped in favor of the number
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x7C003FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF), new UInt128(0x7C00000000000000, 0x0000000000001234) }; // both NaN -> first operand canonicalized
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
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) }; // non-canonical NaN dropped in favor of the number
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x7C003FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF), new UInt128(0x7C00000000000000, 0x0000000000001234) }; // both NaN -> first operand canonicalized
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
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x7C00000000000000, 0x0000000000001234) }; // non-canonical NaN operand is canonicalized
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x7C003FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // non-canonical NaN operand is canonicalized
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
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x7C00000000000000, 0x0000000000001234) }; // non-canonical NaN operand is canonicalized
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x7C003FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // non-canonical NaN operand is canonicalized
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
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x7C00000000000000, 0x0000000000001234) }; // NaN operand wins and is canonicalized
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x7C003FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // both NaN -> second operand canonicalized
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
            yield return new object[] { new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x7C00000000000000, 0x0000000000001234) }; // NaN operand wins and is canonicalized
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x7C003FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF), new UInt128(0x7C00000000000000, 0x0000000000000000) }; // both NaN -> second operand canonicalized
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
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) }; // non-canonical NaN dropped in favor of the number
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x7C003FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF), new UInt128(0x7C00000000000000, 0x0000000000001234) }; // both NaN -> first operand canonicalized
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
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x3040000000000000, 0x0000000000000005), new UInt128(0x3040000000000000, 0x0000000000000005) }; // non-canonical NaN dropped in favor of the number
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), new UInt128(0x7C003FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF), new UInt128(0x7C00000000000000, 0x0000000000001234) }; // both NaN -> first operand canonicalized
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

        [Fact]
        public static void CreateFromSourceTest()
        {
            // Create* forwards to the widening conversion operators, so bit-for-bit equality with
            // the corresponding operator confirms the dispatch. The conversion math itself is
            // covered exhaustively by the Intel reference vectors above.
            Assert.Equal(Unsafe.BitCast<Decimal128, UInt128>((Decimal128)123), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CreateChecked<int>(123)));
            Assert.Equal(Unsafe.BitCast<Decimal128, UInt128>((Decimal128)123), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CreateSaturating<int>(123)));
            Assert.Equal(Unsafe.BitCast<Decimal128, UInt128>((Decimal128)123), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CreateTruncating<int>(123)));

            Assert.Equal(Unsafe.BitCast<Decimal128, UInt128>((Decimal128)(byte)255), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CreateChecked<byte>(255)));
            Assert.Equal(Unsafe.BitCast<Decimal128, UInt128>((Decimal128)(-5L)), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CreateChecked<long>(-5)));
            Assert.Equal(Unsafe.BitCast<Decimal128, UInt128>((Decimal128)1.5f), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CreateChecked<float>(1.5f)));
            Assert.Equal(Unsafe.BitCast<Decimal128, UInt128>((Decimal128)1.5), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CreateChecked<double>(1.5)));
            Assert.Equal(Unsafe.BitCast<Decimal128, UInt128>((Decimal128)1.5m), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CreateChecked<decimal>(1.5m)));

            // The same type is returned unchanged, preserving the exact cohort member.
            Decimal128 value = (Decimal128)123456;
            Assert.Equal(Unsafe.BitCast<Decimal128, UInt128>(value), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CreateChecked<Decimal128>(value)));

            // Cross-decimal conversions forward to the widening operators.
            Assert.Equal(Unsafe.BitCast<Decimal128, UInt128>((Decimal128)(Decimal32)123), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CreateChecked<Decimal32>((Decimal32)123)));
            Assert.Equal(Unsafe.BitCast<Decimal128, UInt128>((Decimal128)(Decimal64)123), Unsafe.BitCast<Decimal128, UInt128>(Decimal128.CreateChecked<Decimal64>((Decimal64)123)));
        }

        [Fact]
        public static void CreateToIntegerCheckedTest()
        {
            // The reverse direction (int.CreateChecked(Decimal128)) routes through Decimal128's
            // TryConvertToChecked, which throws on overflow, NaN, or infinity.
            Assert.Equal(123, int.CreateChecked((Decimal128)123));

            Assert.Throws<OverflowException>(() => byte.CreateChecked((Decimal128)300));
            Assert.Throws<OverflowException>(() => byte.CreateChecked((Decimal128)(-1)));
            Assert.Throws<OverflowException>(() => byte.CreateChecked(Decimal128.NaN));
            Assert.Throws<OverflowException>(() => byte.CreateChecked(Decimal128.PositiveInfinity));
            Assert.Throws<OverflowException>(() => byte.CreateChecked(Decimal128.NegativeInfinity));

            // A negative value in the open interval (-1, 0) truncates toward zero into range, so a
            // checked conversion to an unsigned target returns zero rather than throwing, matching
            // checked((byte)(-0.5)) and the binary floating-point to integer conversions.
            Assert.Equal((byte)0, byte.CreateChecked((Decimal128)(-0.5m)));
        }

        [Fact]
        public static void CreateToIntegerSaturatingTest()
        {
            // TryConvertToSaturating and TryConvertToTruncating share the saturating integer
            // operators: NaN becomes zero and out-of-range values clamp to the target's bounds.
            Assert.Equal((byte)200, byte.CreateSaturating((Decimal128)200));
            Assert.Equal(byte.MaxValue, byte.CreateSaturating((Decimal128)300));
            Assert.Equal((byte)0, byte.CreateSaturating((Decimal128)(-1)));
            Assert.Equal((byte)0, byte.CreateSaturating(Decimal128.NaN));
            Assert.Equal(byte.MaxValue, byte.CreateSaturating(Decimal128.PositiveInfinity));
            Assert.Equal((byte)0, byte.CreateSaturating(Decimal128.NegativeInfinity));

            // Truncating toward zero drops the fractional part.
            Assert.Equal(123, int.CreateTruncating((Decimal128)123.9m));
            Assert.Equal(byte.MaxValue, byte.CreateTruncating((Decimal128)300));
        }

        [Fact]
        public static void CreateToSystemDecimalTest()
        {
            // Saturating clamps to System.Decimal's range and maps NaN to zero.
            Assert.Equal(1.5m, decimal.CreateSaturating((Decimal128)1.5m));
            Assert.Equal(decimal.MaxValue, decimal.CreateSaturating(Decimal128.MaxValue));
            Assert.Equal(decimal.MinValue, decimal.CreateSaturating(Decimal128.MinValue));
            Assert.Equal(0.0m, decimal.CreateSaturating(Decimal128.NaN));

            // Decimal128 represents decimal.MaxValue/MinValue exactly, so the nearest values round-trip.
            Assert.Equal(decimal.MaxValue, decimal.CreateSaturating((Decimal128)decimal.MaxValue));
            Assert.Equal(decimal.MinValue, decimal.CreateSaturating((Decimal128)decimal.MinValue));

            // Checked throws when the value cannot be represented, matching the (decimal) operator.
            Assert.Equal(1.5m, decimal.CreateChecked((Decimal128)1.5m));
            Assert.Throws<OverflowException>(() => decimal.CreateChecked(Decimal128.MaxValue));
            Assert.Throws<OverflowException>(() => decimal.CreateChecked(Decimal128.NaN));
        }

        public static IEnumerable<object[]> op_Modulus_TestData()
        {
            yield return new object[] { new UInt128(0x4810000000000000, 0x0000000000000000), new UInt128(0x990E000000000000, 0x00727CEB5CF62962), new UInt128(0x190E000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x02DC000000000000, 0x000000000000C323), new UInt128(0xF800000000000000, 0x0000000000000000), new UInt128(0x02DC000000000000, 0x000000000000C323) };
            yield return new object[] { new UInt128(0x0820000000000000, 0x000C7F077915197C), new UInt128(0x557C000000000000, 0x0B1E07FE86637A2F), new UInt128(0x0820000000000000, 0x000C7F077915197C) };
            yield return new object[] { new UInt128(0x19D815CE6B5B34C3, 0x8F50B6B1CC625385), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xC35E00001C26FEE5, 0x1B671598D84E4FDE), new UInt128(0xD6D6000000000000, 0x00000001979A8758), new UInt128(0xC35E00001C26FEE5, 0x1B671598D84E4FDE) };
            yield return new object[] { new UInt128(0x36840000000810A5, 0xB6D5CBDA7C41FE15), new UInt128(0xA602000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x4BE200000000885C, 0x1D87CD3186D0C6CC), new UInt128(0x8180000000000000, 0x0000000000006BD7), new UInt128(0x0180000000000000, 0x00000000000009B8) };
            yield return new object[] { new UInt128(0xA9100000000001C7, 0x5A954BD2316F738B), new UInt128(0x4E34000000000AB0, 0x19F2EB310DEB90C4), new UInt128(0xA9100000000001C7, 0x5A954BD2316F738B) };
            yield return new object[] { new UInt128(0x5B74000000BE82C5, 0xEEE7839C9CF8EDF7), new UInt128(0x4030000000000000, 0x001FFAF9A71D2883), new UInt128(0x4030000000000000, 0x001CD7AC6FE3A534) };
            yield return new object[] { new UInt128(0x8102000A63BCF670, 0xD60A6355E652AD8A), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x901C000000000000, 0x4C27457B78954A33), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xBE8800005BE01275, 0xF6418621117238DD), new UInt128(0xD932000000000000, 0x00000000000D8679), new UInt128(0xBE8800005BE01275, 0xF6418621117238DD) };
            yield return new object[] { new UInt128(0xB250000000000000, 0x0000000000000000), new UInt128(0x2504000000000000, 0x000000000002E306), new UInt128(0xA504000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x1344000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xDE2B69A7FDF37D69, 0x0C83953B47DD2851), new UInt128(0x0DB804DBC574207F, 0x31843184B6F0A80A), new UInt128(0x8DB8016B5216022A, 0x9B020AB9F393AFFC) };
            yield return new object[] { new UInt128(0xC38400001491446B, 0x673D6453971DBCAB), new UInt128(0x1A4A000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x2720000000000000, 0x0000097407D8946B), new UInt128(0xB5F4000000000000, 0x00000000000AF371), new UInt128(0x2720000000000000, 0x0000097407D8946B) };
            yield return new object[] { new UInt128(0x9BF6000000000000, 0x1A30D732F747DD0E), new UInt128(0xA3B8000ABA01A581, 0x4406F1E2E09B39B9), new UInt128(0x9BF6000000000000, 0x1A30D732F747DD0E) };
            yield return new object[] { new UInt128(0x0092000000000000, 0x00000000399224C3), new UInt128(0x5AEC000000000000, 0x0000096C2B8F87F0), new UInt128(0x0092000000000000, 0x00000000399224C3) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x8958000000000000, 0x000000000FC25A7C), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x57B2000000000000, 0x000F5DCED6652344), new UInt128(0xB5A2000000001139, 0x85837D2F6B305C5F), new UInt128(0x35A20000000009F6, 0x20430D5F4C647EA6) };
            yield return new object[] { new UInt128(0xAD78000000000000, 0x0000000000000000), new UInt128(0x346E000000000016, 0xF78007A0158F736C), new UInt128(0xAD78000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xA7B4000000000000, 0x0000000000000001), new UInt128(0x26B4000000000000, 0x03345E83946E53FD), new UInt128(0xA6B4000000000000, 0x007817AA3A3AB10A) };
            yield return new object[] { new UInt128(0x0DE2000000000003, 0xF7F0A35B2122BACC), new UInt128(0x40B6000000000000, 0x00000001C0645AF2), new UInt128(0x0DE2000000000003, 0xF7F0A35B2122BACC) };
            yield return new object[] { new UInt128(0xA984000591CB546F, 0xCDCB05B27845D2D7), new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0xA984000591CB546F, 0xCDCB05B27845D2D7) };
            yield return new object[] { new UInt128(0x99D4000000000000, 0x0000000000000000), new UInt128(0x01CA000000000000, 0x00003FCDF3F54CA3), new UInt128(0x81CA000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x5ECE0059773868D9, 0xD79D80205EF487AD), new UInt128(0x575C000000000000, 0x000000000000250A), new UInt128(0x575C000000000000, 0x000000000000012C) };
            yield return new object[] { new UInt128(0xAA26000000000000, 0x0000134F75405BA4), new UInt128(0x977C000000000000, 0x000003CC1F1EFDB2), new UInt128(0x977C000000000000, 0x000001BF28D406A8) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x97D0000000000000, 0x0000000000000347), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x546C000000000000, 0x00000000000000C7), new UInt128(0xD59C000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0x52DC000000000000, 0x0000000000000000), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x4BAC0000000015DC, 0xF9962B8B9EA1F26D), new UInt128(0x40D2000000000000, 0x0000000000000007), new UInt128(0x40D2000000000000, 0x0000000000000002) };
            yield return new object[] { new UInt128(0xBABA045882CF1D86, 0x95BA36F8663F9C4C), new UInt128(0x8220000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), new UInt128(0x386E00000000003C, 0x845CF4E0C470005B), new UInt128(0x7C00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x5DDD4F2CD7014F7C, 0xF0B4AEB4AEDB5010), new UInt128(0x8AA20000000001C6, 0x4D4C69C05E1AEEC9), new UInt128(0x0AA200000000012C, 0xBDCF5BE2645FEAEF) };
            yield return new object[] { new UInt128(0x9CF600000023177C, 0x325CB9124638EB43), new UInt128(0xC77E000000000000, 0x0000000000000009), new UInt128(0x9CF600000023177C, 0x325CB9124638EB43) };
            yield return new object[] { new UInt128(0x3A72000000000000, 0x0000000000000000), new UInt128(0x3752000000000000, 0x000000B07C9C82E5), new UInt128(0x3752000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), new UInt128(0xB3E800008DD69B34, 0x8898B05BA9A564C1), new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xBA20000000000000, 0x0000000000001C83), new UInt128(0x3D1C000000000000, 0x0000000000000000), new UInt128(0x7C00000000000000, 0x0000000000000000) };
        }

        [Theory]
        [MemberData(nameof(op_Modulus_TestData))]
        public static void op_Modulus(UInt128 left, UInt128 right, UInt128 expected)
        {
            Decimal128 result = Unsafe.BitCast<UInt128, Decimal128>(left) % Unsafe.BitCast<UInt128, Decimal128>(right);
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        public static IEnumerable<object[]> RoundToDigits_TestData()
        {
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), 0, MidpointRounding.ToEven, new UInt128(0xFC00000000000000, 0x0000000000000000) }; // canonical NaN passes through
            yield return new object[] { new UInt128(0x7E00000000000000, 0x0000000000001234), 0, MidpointRounding.ToEven, new UInt128(0x7C00000000000000, 0x0000000000001234) }; // signaling NaN -> quiet NaN (payload preserved)
            yield return new object[] { new UInt128(0x7C003FFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF), 3, MidpointRounding.ToZero, new UInt128(0x7C00000000000000, 0x0000000000000000) }; // out-of-range NaN payload cleared
            yield return new object[] { new UInt128(0xFE00000000000000, 0x0000000000000000), 2, MidpointRounding.ToNegativeInfinity, new UInt128(0xFC00000000000000, 0x0000000000000000) }; // negative signaling NaN -> quiet NaN (sign preserved)
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), 3, MidpointRounding.AwayFromZero, new UInt128(0x7800000000000000, 0x0000000000000000) }; // +Inf passes through
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), 2, MidpointRounding.ToPositiveInfinity, new UInt128(0xF800000000000000, 0x0000000000000000) }; // -Inf passes through
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000019), 0, MidpointRounding.ToEven, new UInt128(0x3040000000000000, 0x0000000000000002) }; // 2.5 -> 2 (ToEven)
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000019), 0, MidpointRounding.AwayFromZero, new UInt128(0x3040000000000000, 0x0000000000000003) }; // 2.5 -> 3 (AwayFromZero)
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000023), 0, MidpointRounding.ToEven, new UInt128(0x3040000000000000, 0x0000000000000004) }; // 3.5 -> 4 (ToEven)
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000005), 0, MidpointRounding.ToEven, new UInt128(0x3040000000000000, 0x0000000000000000) }; // 0.5 -> 0 (ToEven)
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000005), 0, MidpointRounding.ToPositiveInfinity, new UInt128(0x3040000000000000, 0x0000000000000001) }; // 0.5 -> 1 (ToPositiveInfinity)
            yield return new object[] { new UInt128(0xB03E000000000000, 0x0000000000000005), 0, MidpointRounding.ToNegativeInfinity, new UInt128(0xB040000000000000, 0x0000000000000001) }; // -0.5 -> -1 (ToNegativeInfinity)
            yield return new object[] { new UInt128(0xB03C000000000000, 0x0000000000000019), 0, MidpointRounding.ToPositiveInfinity, new UInt128(0xB040000000000000, 0x0000000000000000) }; // -0.25 -> -0 (ToPositiveInfinity)
            yield return new object[] { new UInt128(0xB03C000000000000, 0x0000000000000019), 0, MidpointRounding.ToNegativeInfinity, new UInt128(0xB040000000000000, 0x0000000000000001) }; // -0.25 -> -1 (ToNegativeInfinity)
            yield return new object[] { new UInt128(0x303C000000000000, 0x0000000000000019), 0, MidpointRounding.ToZero, new UInt128(0x3040000000000000, 0x0000000000000000) }; // 0.25 -> 0 (ToZero)
            yield return new object[] { new UInt128(0x303E000000000000, 0x0000000000000005), 5, MidpointRounding.ToEven, new UInt128(0x303E000000000000, 0x0000000000000005) }; // already finer than target, no-op
            yield return new object[] { new UInt128(0xB03E000000000000, 0x0000000000000000), 0, MidpointRounding.ToNegativeInfinity, new UInt128(0xB040000000000000, 0x0000000000000000) }; // -0 stays -0 (ToNegativeInfinity)
            yield return new object[] { new UInt128(0x303A000000000000, 0x000000000001E240), 2, MidpointRounding.ToEven, new UInt128(0x303C000000000000, 0x000000000000303A) }; // 123.456 -> 123.46 (ToEven)
            yield return new object[] { new UInt128(0xACA200001A64F0B2, 0x47116E8F4AA7655A), 1, MidpointRounding.AwayFromZero, new UInt128(0xB03E000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xBBD6000000000000, 0x0000000000000000), 34, MidpointRounding.AwayFromZero, new UInt128(0xBBD6000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x5CB4020BD4E73D0E, 0x6B8B8C501BF89C14), 19, MidpointRounding.ToEven, new UInt128(0x5CB4020BD4E73D0E, 0x6B8B8C501BF89C14) };
            yield return new object[] { new UInt128(0x4C48000000000000, 0x00000001DD06A8E5), 32, MidpointRounding.ToZero, new UInt128(0x4C48000000000000, 0x00000001DD06A8E5) };
            yield return new object[] { new UInt128(0x5008000000000000, 0x00000000008BDB96), 22, MidpointRounding.ToZero, new UInt128(0x5008000000000000, 0x00000000008BDB96) };
            yield return new object[] { new UInt128(0x44A600000004A2E4, 0x946727A4C20797B4), 35, MidpointRounding.AwayFromZero, new UInt128(0x44A600000004A2E4, 0x946727A4C20797B4) };
            yield return new object[] { new UInt128(0xA5940002963B08E8, 0x1CFEC92070EC50BB), 15, MidpointRounding.AwayFromZero, new UInt128(0xB022000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x9EE8000000000000, 0x0000000000000000), 34, MidpointRounding.ToPositiveInfinity, new UInt128(0xAFFC000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x81E4000000000000, 0x000000000000F435), 9, MidpointRounding.ToNegativeInfinity, new UInt128(0xB02E000000000000, 0x0000000000000001) };
            yield return new object[] { new UInt128(0x4878DE405BD7331D, 0x0541CCBC7509BA76), 8, MidpointRounding.ToNegativeInfinity, new UInt128(0x4878DE405BD7331D, 0x0541CCBC7509BA76) };
            yield return new object[] { new UInt128(0x1A40000000000000, 0x0000000000208C95), 30, MidpointRounding.ToZero, new UInt128(0x3004000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), 32, MidpointRounding.ToZero, new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x83BC000000000000, 0x246A51A7EDEE468A), 16, MidpointRounding.AwayFromZero, new UInt128(0xB020000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x7800000000000000, 0x0000000000000000), 36, MidpointRounding.ToZero, new UInt128(0x7800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x2A6C000000000018, 0x524FE2F25ACC517F), 13, MidpointRounding.ToEven, new UInt128(0x3026000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x4E8E295B67DFC0C6, 0xE4BC3F5859D95A1C), 18, MidpointRounding.AwayFromZero, new UInt128(0x4E8E295B67DFC0C6, 0xE4BC3F5859D95A1C) };
            yield return new object[] { new UInt128(0xC234000000000000, 0x00000000003638E6), 7, MidpointRounding.ToZero, new UInt128(0xC234000000000000, 0x00000000003638E6) };
            yield return new object[] { new UInt128(0x2448000000000003, 0xC16B45BF142025EA), 24, MidpointRounding.ToPositiveInfinity, new UInt128(0x3010000000000000, 0x0000000000000001) };
            yield return new object[] { new UInt128(0x4F96000000000000, 0x000000000000027D), 6, MidpointRounding.ToZero, new UInt128(0x4F96000000000000, 0x000000000000027D) };
            yield return new object[] { new UInt128(0xCC54000000000000, 0x00000000005DFBBC), 16, MidpointRounding.ToEven, new UInt128(0xCC54000000000000, 0x00000000005DFBBC) };
            yield return new object[] { new UInt128(0x8030000000000000, 0x000000000000002F), 13, MidpointRounding.ToEven, new UInt128(0xB026000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), 20, MidpointRounding.ToZero, new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x1F64000000000000, 0x0000000000000000), 14, MidpointRounding.ToZero, new UInt128(0x3024000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x2D9C000000000000, 0x000000000000AF5A), 11, MidpointRounding.ToEven, new UInt128(0x302A000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xAF9C000000000000, 0x0000000000000032), 24, MidpointRounding.ToPositiveInfinity, new UInt128(0xB010000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x5F9E0000000003FC, 0x7B93612B2A7B1A77), 20, MidpointRounding.ToZero, new UInt128(0x5F9E0000000003FC, 0x7B93612B2A7B1A77) };
            yield return new object[] { new UInt128(0x5ABE000000000000, 0x0000090FF790C39F), 36, MidpointRounding.ToEven, new UInt128(0x5ABE000000000000, 0x0000090FF790C39F) };
            yield return new object[] { new UInt128(0x1F34000000000000, 0x0000000000704EFF), 34, MidpointRounding.ToZero, new UInt128(0x2FFC000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0xB088000000000000, 0x00000007B16C6B74), 27, MidpointRounding.ToZero, new UInt128(0xB088000000000000, 0x00000007B16C6B74) };
            yield return new object[] { new UInt128(0xF800000000000000, 0x0000000000000000), 9, MidpointRounding.ToZero, new UInt128(0xF800000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x8848000001DF35DC, 0xB97C8D8D51686009), 13, MidpointRounding.ToNegativeInfinity, new UInt128(0xB026000000000000, 0x0000000000000001) };
            yield return new object[] { new UInt128(0x2412000000017DA7, 0xA70161F30FBAD560), 13, MidpointRounding.AwayFromZero, new UInt128(0x3026000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x43A4007DC21E3069, 0xD3DC36E844F4E8E4), 16, MidpointRounding.ToZero, new UInt128(0x43A4007DC21E3069, 0xD3DC36E844F4E8E4) };
            yield return new object[] { new UInt128(0x38E4000000027ED7, 0x90B063487ECA3640), 20, MidpointRounding.ToEven, new UInt128(0x38E4000000027ED7, 0x90B063487ECA3640) };
            yield return new object[] { new UInt128(0xC326000000000000, 0x000000021EBC0119), 30, MidpointRounding.AwayFromZero, new UInt128(0xC326000000000000, 0x000000021EBC0119) };
            yield return new object[] { new UInt128(0xFC00000000000000, 0x0000000000000000), 12, MidpointRounding.ToNegativeInfinity, new UInt128(0xFC00000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x4A40000000000000, 0x000000000000038D), 11, MidpointRounding.ToNegativeInfinity, new UInt128(0x4A40000000000000, 0x000000000000038D) };
            yield return new object[] { new UInt128(0x3382D503E84E1B51, 0x5301BCC58952CAB2), 29, MidpointRounding.ToEven, new UInt128(0x3382D503E84E1B51, 0x5301BCC58952CAB2) };
            yield return new object[] { new UInt128(0x9FBE000000000000, 0x0000000000000363), 24, MidpointRounding.ToEven, new UInt128(0xB010000000000000, 0x0000000000000000) };
            yield return new object[] { new UInt128(0x374A000000000000, 0x000000A466DBFDD5), 21, MidpointRounding.ToZero, new UInt128(0x374A000000000000, 0x000000A466DBFDD5) };
        }

        [Theory]
        [MemberData(nameof(RoundToDigits_TestData))]
        public static void RoundToDigits(UInt128 value, int digits, MidpointRounding mode, UInt128 expected)
        {
            Decimal128 result = Decimal128.Round(Unsafe.BitCast<UInt128, Decimal128>(value), digits, mode);
            Assert.Equal(expected, Unsafe.BitCast<Decimal128, UInt128>(result));
        }

        [Fact]
        public static void RoundConvenienceOverloads()
        {
            Decimal128 x = Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x303E000000000000, 0x0000000000000019)); // 2.5

            Assert.Equal(Decimal128.Round(x, 0, MidpointRounding.ToPositiveInfinity), Decimal128.Ceiling(x));
            Assert.Equal(Decimal128.Round(x, 0, MidpointRounding.ToNegativeInfinity), Decimal128.Floor(x));
            Assert.Equal(Decimal128.Round(x, 0, MidpointRounding.ToZero), Decimal128.Truncate(x));
            Assert.Equal(Decimal128.Round(x, 0, MidpointRounding.ToEven), Decimal128.Round(x));
            Assert.Equal(Decimal128.Round(x, 0, MidpointRounding.AwayFromZero), Decimal128.Round(x, MidpointRounding.AwayFromZero));
            Assert.Equal(Decimal128.Round(x, 2, MidpointRounding.ToEven), Decimal128.Round(x, 2));
        }

        [Fact]
        public static void IFloatingPoint_ExponentAndSignificand()
        {
            IFloatingPoint<Decimal128> value = Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x303C000000000000, 0x0000000000003039)); // 123.45

            Assert.Equal(sizeof(int), value.GetExponentByteCount());
            Assert.Equal(Unsafe.SizeOf<UInt128>(), value.GetSignificandByteCount());

            Span<byte> exponent = stackalloc byte[value.GetExponentByteCount()];
            Assert.True(value.TryWriteExponentLittleEndian(exponent, out int exponentWritten));
            Assert.Equal(sizeof(int), exponentWritten);
            Assert.Equal(-2, BinaryPrimitives.ReadInt32LittleEndian(exponent));

            Span<byte> significand = stackalloc byte[value.GetSignificandByteCount()];
            Assert.True(value.TryWriteSignificandLittleEndian(significand, out int significandWritten));
            Assert.Equal(Unsafe.SizeOf<UInt128>(), significandWritten);
            Assert.Equal((UInt128)12345, BinaryPrimitives.ReadUInt128LittleEndian(significand));

            Assert.Equal(2, value.GetExponentShortestBitLength());
            Assert.Equal(113, value.GetSignificandBitLength());

            Span<byte> exponentBigEndian = stackalloc byte[value.GetExponentByteCount()];
            Assert.True(value.TryWriteExponentBigEndian(exponentBigEndian, out exponentWritten));
            Assert.Equal(sizeof(int), exponentWritten);
            Assert.Equal(-2, BinaryPrimitives.ReadInt32BigEndian(exponentBigEndian));

            Span<byte> significandBigEndian = stackalloc byte[value.GetSignificandByteCount()];
            Assert.True(value.TryWriteSignificandBigEndian(significandBigEndian, out significandWritten));
            Assert.Equal(Unsafe.SizeOf<UInt128>(), significandWritten);
            Assert.Equal((UInt128)12345, BinaryPrimitives.ReadUInt128BigEndian(significandBigEndian));

            // A non-negative exponent exercises the other GetExponentShortestBitLength branch.
            IFloatingPoint<Decimal128> integer = Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x3040000000000000, 0x0000000000003039)); // 12345
            Assert.Equal(0, integer.GetExponentShortestBitLength());

            Assert.Equal(123, Decimal128.ConvertToInteger<int>(Unsafe.BitCast<UInt128, Decimal128>(new UInt128(0x303C000000000000, 0x0000000000003039))));
        }

        [Fact]
        public static void IDecimalFloatingPointIeee754_GenericSurface()
        {
            GenericIeee754Surface.Verify<Decimal128>();
        }

    }
}
