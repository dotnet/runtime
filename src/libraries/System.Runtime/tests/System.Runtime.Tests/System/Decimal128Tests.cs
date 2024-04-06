// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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

            var customFormat3 = new NumberFormatInfo();
            customFormat3.NumberGroupSeparator = ",";

            var customFormat4 = new NumberFormatInfo();
            customFormat4.NumberDecimalSeparator = ".";

            yield return new object[] { "-123", defaultStyle, null, new Decimal128(-123, 0) };
            yield return new object[] { "0", defaultStyle, null, new Decimal128(0, 0) };
            yield return new object[] { "123", defaultStyle, null, new Decimal128(123, 0) };
            yield return new object[] { "  123  ", defaultStyle, null, new Decimal128(123, 0) };
            yield return new object[] { (567.89).ToString(), defaultStyle, null, new Decimal128(56789, -2) };
            yield return new object[] { (-567.89).ToString(), defaultStyle, null, new Decimal128(-56789, -2) };
            yield return new object[] { "0.666666666666666666666666666666666650000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, new Decimal128(Int128.Parse(new string('6', 34)), -34) };

            yield return new object[] { emptyFormat.NumberDecimalSeparator + "234", defaultStyle, null, new Decimal128(234, -3) };
            yield return new object[] { "234" + emptyFormat.NumberDecimalSeparator, defaultStyle, null, new Decimal128(234, 0) };
            yield return new object[] { "7" + new string('0', 6144) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, new Decimal128(7, 6144) };
            yield return new object[] { "07" + new string('0', 6144) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, new Decimal128(7, 6144) };

            yield return new object[] { (123.1).ToString(), NumberStyles.AllowDecimalPoint, null, new Decimal128(1231, -1) };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.AllowThousands, null, new Decimal128(1000, 0) };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, new Decimal128(123, 0) };
            yield return new object[] { (123.567).ToString(), NumberStyles.Any, emptyFormat, new Decimal128(123567, -3) };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, new Decimal128(123, 0) };
            yield return new object[] { "$1000", NumberStyles.Currency, customFormat1, new Decimal128(1, 3) };
            yield return new object[] { "123.123", NumberStyles.Float, customFormat2, new Decimal128(123123, -3) };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, customFormat2, new Decimal128(-123, 0) };
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

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Number;

            var customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";
            customFormat.NumberDecimalSeparator = ".";

            yield return new object[] { null, defaultStyle, null, typeof(ArgumentNullException) };
            yield return new object[] { "1" + new string('0', 6145), defaultStyle, null, typeof(OverflowException) };
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

            yield return new object[] { "-123", 1, 3, NumberStyles.Number, null, new Decimal128(123, 0) };
            yield return new object[] { "-123", 0, 3, NumberStyles.Number, null, new Decimal128(-12, 0) };
            yield return new object[] { 1000.ToString("N0"), 0, 4, NumberStyles.AllowThousands, null, new Decimal128(100, 0) };
            yield return new object[] { 1000.ToString("N0"), 2, 3, NumberStyles.AllowThousands, null, new Decimal128(0, 0) };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, new Decimal128(123, 0) };
            yield return new object[] { "1234567890123456789012345.678456", 1, 4, NumberStyles.Number, new NumberFormatInfo() { NumberDecimalSeparator = "." }, new Decimal128(2345, 0) };
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

        [Fact]
        public static void Midpoint_Rounding()
        {
            var number = new Decimal128(Int128.Parse("12345688888888881234568888888888885"), 0);
            Assert.Equal(new Decimal128(Int128.Parse("1234568888888888123456888888888888"), 1), number);
        }

        [Fact]
        public static void Rounding()
        {
            var number = new Decimal128(Int128.Parse("12345677777777771234567777777777778"), 0);
            Assert.Equal(new Decimal128(Int128.Parse("1234567777777777123456777777777778"), 1), number);

            number = new Decimal128(Int128.Parse("12345677777777771234567777777777771"), 0);
            Assert.Equal(new Decimal128(Int128.Parse("1234567777777777123456777777777777"), 1), number);
        }

        [Fact]
        public static void CompareTo_Other_ReturnsExpected()
        {
            for (int i = 1; i < 30; i++)
            {
                var d1 = new Decimal128(1, i);
                var d2 = new Decimal128(Int128.Parse("1" + new string('0', i)), 0);
                Assert.Equal(d1, d2);
            }
            Assert.Equal(new Decimal128(-1, 1), new Decimal128(-10, 0));
            Assert.NotEqual(new Decimal128(1, 1), new Decimal128(-10, 0));
            Assert.NotEqual(new Decimal128(-1, 1), new Decimal128(10, 0));
        }

        [Fact]
        public static void GetHashCodeTest()
        {
            var d = new Decimal128(10, 20);
            Assert.Equal(d.GetHashCode(), d.GetHashCode());
        }
    }
}
