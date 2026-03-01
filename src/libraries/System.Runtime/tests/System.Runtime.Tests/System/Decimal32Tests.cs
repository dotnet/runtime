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

            yield return new object[] { "-123", defaultStyle, null, new Decimal32(-123, 0) };
            yield return new object[] { "0", defaultStyle, null, new Decimal32(0, 0) };
            yield return new object[] { "123", defaultStyle, null, new Decimal32(123, 0) };
            yield return new object[] { "  123  ", defaultStyle, null, new Decimal32(123, 0) };
            yield return new object[] { (567.89).ToString(), defaultStyle, null, new Decimal32(56789, -2) };
            yield return new object[] { (-567.89).ToString(), defaultStyle, null, new Decimal32(-56789, -2) };
            yield return new object[] { "0.6666666500000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, new Decimal32(6666666, -7) };
            
            yield return new object[] { "0." + new string('0', 101) + "1", defaultStyle, invariantFormat, new Decimal32(0, 0) };
            yield return new object[] { "-0." + new string('0', 101) + "1", defaultStyle, invariantFormat, new Decimal32(0, 0) };
            yield return new object[] { "0." + new string('0', 100) + "1", defaultStyle, invariantFormat, new Decimal32(1, -101) };
            yield return new object[] { "-0." + new string('0', 100) + "1", defaultStyle, invariantFormat, new Decimal32(-1, -101) };

            yield return new object[] { "0." + new string('0', 99) + "12345", defaultStyle, invariantFormat, new Decimal32(12, -101) };
            yield return new object[] { "-0." + new string('0', 99) + "12345", defaultStyle, invariantFormat, new Decimal32(-12, -101) };
            yield return new object[] { "0." + new string('0', 99) + "12562", defaultStyle, invariantFormat, new Decimal32(13, -101) };
            yield return new object[] { "-0." + new string('0', 99) + "12562", defaultStyle, invariantFormat, new Decimal32(-13, -101) };

            yield return new object[] { emptyFormat.NumberDecimalSeparator + "234", defaultStyle, null, new Decimal32(234, -3) };
            yield return new object[] { "234" + emptyFormat.NumberDecimalSeparator, defaultStyle, null, new Decimal32(234, 0) };
            yield return new object[] { "7" + new string('0', 96) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, new Decimal32(7, 96) };
            yield return new object[] { "07" + new string('0', 96) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, new Decimal32(7, 96) };

            yield return new object[] { (123.1).ToString(), NumberStyles.AllowDecimalPoint, null, new Decimal32(1231, -1) };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.AllowThousands, null, new Decimal32(1000, 0) };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, new Decimal32(123, 0) };
            yield return new object[] { (123.567).ToString(), NumberStyles.Any, emptyFormat, new Decimal32(123567, -3) };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, new Decimal32(123, 0) };
            yield return new object[] { "$1000", NumberStyles.Currency, customFormat1, new Decimal32(1, 3) };
            yield return new object[] { "123.123", NumberStyles.Float, customFormat2, new Decimal32(123123, -3) };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, customFormat2, new Decimal32(-123, 0) };

            yield return new object[] { "NaN", NumberStyles.Any, invariantFormat, Decimal32.NaN };
            yield return new object[] { "+NaN", NumberStyles.Any, invariantFormat, Decimal32.NaN };
            yield return new object[] { "Infinity", NumberStyles.Any, invariantFormat, Decimal32.PositiveInfinity };
            yield return new object[] { "+Infinity", NumberStyles.Any, invariantFormat, Decimal32.PositiveInfinity };
            yield return new object[] { "-Infinity", NumberStyles.Any, invariantFormat, Decimal32.NegativeInfinity };
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

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Number;

            var customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";
            customFormat.NumberDecimalSeparator = ".";

            yield return new object[] { null, defaultStyle, null, typeof(ArgumentNullException) };
            yield return new object[] { "", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { " ", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "Garbage", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "ab", defaultStyle, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "(123)", defaultStyle, null, typeof(FormatException) }; // Parentheses
            yield return new object[] { 100.ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency

            yield return new object[] { (123.456m).ToString(), NumberStyles.Integer, null, typeof(FormatException) }; // Decimal
            yield return new object[] { "  " + (123.456m).ToString(), NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { (123.456m).ToString() + "   ", NumberStyles.None, null, typeof(FormatException) }; // Leading space
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

            yield return new object[] { "-123", 1, 3, NumberStyles.Number, null, new Decimal32(123, 0) };
            yield return new object[] { "-123", 0, 3, NumberStyles.Number, null, new Decimal32(-12, 0) };
            yield return new object[] { 1000.ToString("N0"), 0, 4, NumberStyles.AllowThousands, null, new Decimal32(100, 0) };
            yield return new object[] { 1000.ToString("N0"), 2, 3, NumberStyles.AllowThousands, null, new Decimal32(0, 0) };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, new Decimal32(123, 0) };
            yield return new object[] { "1234567890123456789012345.678456", 1, 4, NumberStyles.Number, new NumberFormatInfo() { NumberDecimalSeparator = "." }, new Decimal32(2345, 0) };
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

        [Fact]
        public static void Midpoint_Rounding()
        {
            var number = new Decimal32(12345685, 0);
            Assert.Equal(new Decimal32(1234568, 1), number);
        }

        [Fact]
        public static void Rounding()
        {
            var number = new Decimal32(12345678, 0);
            Assert.Equal(new Decimal32(1234568, 1), number);

            number = new Decimal32(12345671, 0);
            Assert.Equal(new Decimal32(1234567, 1), number);

            number = new Decimal32(12345650, -103);
            Assert.Equal(new Decimal32(123456, -101), number);
        }

        [Fact]
        public static void MaxValue_Rounding()
        {
            Assert.Equal(Decimal32.MaxValue, Decimal32.Parse(new string('9', 7) + '4' + new string('9', 89)));
            Assert.Equal(Decimal32.PositiveInfinity, Decimal32.Parse(new string('9', 7) + '5' + new string('0', 89)));
            Assert.Equal(Decimal32.PositiveInfinity, Decimal32.Parse(new string('9', 7) + '5' + new string('0', 88) + '1'));
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
            yield return new object[] { new Decimal32(-1, 1), new Decimal32(-10, 0), 0 };
            yield return new object[] { new Decimal32(1, 90), new Decimal32(10, 89), 0 };
            yield return new object[] { new Decimal32(999999, 90), new Decimal32(9999990, 89), 0 };
            yield return new object[] { new Decimal32(1, 1), new Decimal32(-1, 0), 1 };
            yield return new object[] { new Decimal32(10, 0), new Decimal32(-1, 1), 1 };
            yield return new object[] { new Decimal32(10, 0), Decimal32.NaN, 1 };
            yield return new object[] { new Decimal32(10, 0), Decimal32.NegativeInfinity, 1 };
            yield return new object[] { new Decimal32(10, 0), Decimal32.NegativeZero, 1 };
            yield return new object[] { Decimal32.PositiveInfinity, new Decimal32(10, 20), 1 };
            yield return new object[] { Decimal32.PositiveInfinity, new Decimal32(10, 150), 0 };
            yield return new object[] { Decimal32.PositiveInfinity, Decimal32.NegativeInfinity, 1 };
            yield return new object[] { Decimal32.PositiveInfinity, Decimal32.PositiveInfinity, 0 };
            yield return new object[] { Decimal32.PositiveInfinity, Decimal32.NegativeZero, 1 };
            yield return new object[] { Decimal32.NegativeInfinity, Decimal32.NegativeInfinity, 0 };
            yield return new object[] { Decimal32.NaN, Decimal32.NaN, 0 };
            yield return new object[] { Decimal32.NegativeZero, Decimal32.NegativeInfinity, 1 };
            yield return new object[] { Decimal32.NegativeZero, new Decimal32(0, 20), 0 };
            yield return new object[] { Decimal32.NegativeZero, Decimal32.NaN, 1 };
            for (int i = 1; i < 7; i++)
            {
                var d1 = new Decimal32(1, i);
                var d2 = new Decimal32(int.Parse("1" + new string('0', i)), 0);
                yield return new object[] { d1, d2, 0 };
            }
        }

        [Fact]
        public static void GetHashCodeTest()
        {
            var d = new Decimal32(10, 20);
            Assert.Equal(d.GetHashCode(), d.GetHashCode());
        }

        [Fact]
        public static void CompareToZero()
        {
            var zero = new Decimal32(0, 1);
            Assert.Equal(zero, new Decimal32(0, 20));
            Assert.Equal(zero, new Decimal32(1, -102));
            Assert.Equal(zero, new Decimal32(234, -1000));
            Assert.Equal(zero, new Decimal32(-1, -102));
            Assert.Equal(zero, new Decimal32(-234, -1000));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                yield return new object[] { new Decimal32(3, 96), "G", defaultFormat, "3" + new string('0', 96) };
                yield return new object[] { new Decimal32(-3, 96), "G", defaultFormat, "-3" + new string('0', 96) };
                yield return new object[] { new Decimal32(-4567, 0), "G", defaultFormat, "-4567" };
                yield return new object[] { new Decimal32(-4567891, -3), "G", defaultFormat, "-4567.891" };
                yield return new object[] { new Decimal32(0, 0), "G", defaultFormat, "0" };
                yield return new object[] { new Decimal32(4567, 0), "G", defaultFormat, "4567" };
                yield return new object[] { new Decimal32(4567891, -3), "G", defaultFormat, "4567.891" };

                yield return new object[] { new Decimal32(2468, 0), "N", defaultFormat, "2,468.00" };

                yield return new object[] { new Decimal32(2467, 0), "[#-##-#]", defaultFormat, "[2-46-7]" };

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
