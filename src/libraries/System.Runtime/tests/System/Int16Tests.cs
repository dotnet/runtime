// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class Int16Tests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new short();
            Assert.Equal(0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            short i = 41;
            Assert.Equal(41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0x7FFF, short.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(unchecked((short)0x8000), short.MinValue);
        }

        [Theory]
        [InlineData((short)234, (short)234, 0)]
        [InlineData((short)234, short.MinValue, 1)]
        [InlineData((short)-234, short.MinValue, 1)]
        [InlineData(short.MinValue, short.MinValue, 0)]
        [InlineData((short)234, (short)-123, 1)]
        [InlineData((short)234, (short)0, 1)]
        [InlineData((short)234, (short)123, 1)]
        [InlineData((short)234, (short)456, -1)]
        [InlineData((short)234, short.MaxValue, -1)]
        [InlineData((short)-234, short.MaxValue, -1)]
        [InlineData(short.MaxValue, short.MaxValue, 0)]
        [InlineData((short)-234, (short)-234, 0)]
        [InlineData((short)-234, (short)234, -1)]
        [InlineData((short)-234, (short)-432, 1)]
        [InlineData((short)234, null, 1)]
        public void CompareTo_Other_ReturnsExpected(short i, object value, int expected)
        {
            if (value is short shortValue)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(shortValue)));
                Assert.Equal(-expected, Math.Sign(shortValue.CompareTo(i)));
            }

            Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData(234)]
        public void CompareTo_ObjectNotShort_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((short)123).CompareTo(value));
        }

        [Theory]
        [InlineData((short)789, (short)789, true)]
        [InlineData((short)789, (short)-789, false)]
        [InlineData((short)789, (short)0, false)]
        [InlineData((short)0, (short)0, true)]
        [InlineData((short)-789, (short)-789, true)]
        [InlineData((short)-789, (short)789, false)]
        [InlineData((short)789, null, false)]
        [InlineData((short)789, "789", false)]
        [InlineData((short)789, 789, false)]
        public static void EqualsTest(short i1, object obj, bool expected)
        {
            if (obj is short)
            {
                short i2 = (short)obj;
                Assert.Equal(expected, i1.Equals(i2));
                Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
            }
            Assert.Equal(expected, i1.Equals(obj));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsInt16()
        {
            Assert.Equal(TypeCode.Int16, ((short)1).GetTypeCode());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "", "R" })
                {
                    yield return new object[] { short.MinValue, defaultSpecifier, defaultFormat, "-32768" };
                    yield return new object[] { (short)-4567, defaultSpecifier, defaultFormat, "-4567" };
                    yield return new object[] { (short)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (short)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { short.MaxValue, defaultSpecifier, defaultFormat, "32767" };
                }

                yield return new object[] { (short)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (short)4567, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (short)4567, "D99\09", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (short)-4567, "D99", defaultFormat, "-000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };

                yield return new object[] { (short)0, "x", defaultFormat, "0" };
                yield return new object[] { (short)0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { (short)-0x2468, "x", defaultFormat, "db98" };

                yield return new object[] { (short)0, "b", defaultFormat, "0" };
                yield return new object[] { (short)0x2468, "b", defaultFormat, "10010001101000" };
                yield return new object[] { (short)-0x2468, "b", defaultFormat, "1101101110011000" };

                yield return new object[] { (short)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
            }

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { (short)32, "C100", invariantFormat, "\u00A432.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (short)32, "P100", invariantFormat, "3,200.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { (short)32, "D100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000032" };
            yield return new object[] { (short)32, "E100", invariantFormat, "3.2000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { (short)32, "F100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (short)32, "N100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (short)32, "X100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020" };
            yield return new object[] { (short)32, "B100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000100000" };

            var customFormat = new NumberFormatInfo()
            {
                NegativeSign = "#",
                NumberDecimalSeparator = "~",
                NumberGroupSeparator = "*",
                PositiveSign = "&",
                NumberDecimalDigits = 2,
                PercentSymbol = "@",
                PercentGroupSeparator = ",",
                PercentDecimalSeparator = ".",
                PercentDecimalDigits = 5
            };
            yield return new object[] { (short)-2468, "N", customFormat, "#2*468~00" };
            yield return new object[] { (short)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (short)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (short)123, "F", customFormat, "123~00" };
            yield return new object[] { (short)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(short i, string format, IFormatProvider provider, string expected)
        {
            // Format is case insensitive
            string upperFormat = format.ToUpperInvariant();
            string lowerFormat = format.ToLowerInvariant();

            string upperExpected = expected.ToUpperInvariant();
            string lowerExpected = expected.ToLowerInvariant();

            bool isDefaultProvider = (provider == null || provider == NumberFormatInfo.CurrentInfo);
            if (string.IsNullOrEmpty(format) || format.ToUpperInvariant() is "G" or "R")
            {
                if (isDefaultProvider)
                {
                    Assert.Equal(upperExpected, i.ToString());
                    Assert.Equal(upperExpected, i.ToString((IFormatProvider)null));
                }
                Assert.Equal(upperExpected, i.ToString(provider));
            }
            if (isDefaultProvider)
            {
                Assert.Equal(upperExpected, i.ToString(upperFormat));
                Assert.Equal(lowerExpected, i.ToString(lowerFormat));
                Assert.Equal(upperExpected, i.ToString(upperFormat, null));
                Assert.Equal(lowerExpected, i.ToString(lowerFormat, null));
            }
            Assert.Equal(upperExpected, i.ToString(upperFormat, provider));
            Assert.Equal(lowerExpected, i.ToString(lowerFormat, provider));
        }

        [Fact]
        public static void ToString_InvalidFormat_ThrowsFormatException()
        {
            short i = 123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Integer;
            NumberFormatInfo emptyFormat = new NumberFormatInfo();

            NumberFormatInfo customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";

            yield return new object[] { "-32768", defaultStyle, null, (short)-32768 };
            yield return new object[] { "-123", defaultStyle, null, (short)-123 };
            yield return new object[] { "0", defaultStyle, null, (short)0 };
            yield return new object[] { "123", defaultStyle, null, (short)123 };
            yield return new object[] { "+123", defaultStyle, null, (short)123 };
            yield return new object[] { "  123  ", defaultStyle, null, (short)123 };
            yield return new object[] { "32767", defaultStyle, null, (short)32767 };

            yield return new object[] { "123", NumberStyles.HexNumber, null, (short)0x123 };
            yield return new object[] { "abc", NumberStyles.HexNumber, null, (short)0xabc };
            yield return new object[] { "ABC", NumberStyles.HexNumber, null, (short)0xabc };
            yield return new object[] { "101010111100", NumberStyles.BinaryNumber, null, (short)0b101010111100 };
            yield return new object[] { "1000", NumberStyles.AllowThousands, null, (short)1000 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, null, (short)-123 }; // Parentheses = negative

            yield return new object[] { "123", defaultStyle, emptyFormat, (short)123 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, (short)123 };
            yield return new object[] { "12", NumberStyles.HexNumber, emptyFormat, (short)0x12 };
            yield return new object[] { "10010", NumberStyles.BinaryNumber, emptyFormat, (short)0b10010 };
            yield return new object[] { "$1,000", NumberStyles.Currency, customFormat, (short)1000 };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, short expected)
        {
            short result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(short.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, short.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, short.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(short.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, short.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, short.Parse(value, provider));
            }

            // Full overloads
            Assert.True(short.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, short.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // Include the test data for wider primitives.
            foreach (object[] widerTests in Int32Tests.Parse_Invalid_TestData())
            {
                yield return widerTests;
            }

            yield return new object[] { "-32769", NumberStyles.Integer, null, typeof(OverflowException) }; // < min value
            yield return new object[] { "32768", NumberStyles.Integer, null, typeof(OverflowException) }; // > max value

            yield return new object[] { "FFFFF", NumberStyles.HexNumber, null, typeof(OverflowException) }; // Hex number < 0
            yield return new object[] { "10000", NumberStyles.HexNumber, null, typeof(OverflowException) }; // Hex number > max value

            yield return new object[] { "11111111111111111", NumberStyles.BinaryNumber, null, typeof(OverflowException) }; // Binary number < 0
            yield return new object[] { "10000000000000000", NumberStyles.BinaryNumber, null, typeof(OverflowException) }; // Binary number > max value
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            short result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(short.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => short.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => short.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(short.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => short.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => short.Parse(value, provider));
            }

            // Full overloads
            Assert.False(short.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => short.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.BinaryNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.HexNumber | NumberStyles.BinaryNumber)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00))]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style)
        {
            short result = 0;
            AssertExtensions.Throws<ArgumentException>("style", () => short.TryParse("1", style, null, out result));
            Assert.Equal(default(short), result);

            AssertExtensions.Throws<ArgumentException>("style", () => short.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>("style", () => short.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "-32767", 1, 5, NumberStyles.Integer, null, (short)32767 };
            yield return new object[] { "-32768", 0, 5, NumberStyles.Integer, null, (short)-3276 };
            yield return new object[] { "abc", 0, 2, NumberStyles.HexNumber, null, (short)0xab };
            yield return new object[] { "abc", 1, 2, NumberStyles.HexNumber, null, (short)0xbc };
            yield return new object[] { "1010101100", 0, 8, NumberStyles.BinaryNumber, null, (short)0b10101011 };
            yield return new object[] { "11101010110011", 2, 8, NumberStyles.BinaryNumber, null, (short)0b10101011 };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, null, (short)123 };
            yield return new object[] { "123", 0, 1, NumberStyles.Integer, new NumberFormatInfo(), (short)1 };
            yield return new object[] { "$1,000", 1, 5, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (short)1000 };
            yield return new object[] { "$1,000", 0, 2, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (short)1 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, short expected)
        {
            short result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(short.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, short.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(short.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                short result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(short.TryParse(value.AsSpan(), out result));
                    Assert.Equal(0, result);
                }

                Assert.Throws(exceptionType, () => short.Parse(value.AsSpan(), style, provider));

                Assert.False(short.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(0, result);
            }
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(short i, string format, IFormatProvider provider, string expected) =>
            NumberFormatTestHelper.TryFormatNumberTest(i, format, provider, expected);
    }
}
