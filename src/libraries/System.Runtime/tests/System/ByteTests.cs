// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

namespace System.Tests
{
    public class ByteTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var b = new byte();
            Assert.Equal(0, b);
        }

        [Fact]
        public static void Ctor_Value()
        {
            byte b = 41;
            Assert.Equal(41, b);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0xFF, byte.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(0, byte.MinValue);
        }

        [Theory]
        [InlineData((byte)234, (byte)234, 0)]
        [InlineData((byte)234, byte.MinValue, 1)]
        [InlineData((byte)234, (byte)123, 1)]
        [InlineData((byte)234, (byte)235, -1)]
        [InlineData((byte)234, byte.MaxValue, -1)]
        [InlineData((byte)234, null, 1)]
        public void CompareTo_Other_ReturnsExpected(byte i, object value, int expected)
        {
            if (value is byte byteValue)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(byteValue)));
            }

            Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData(234)]
        public void CompareTo_ObjectNotByte_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((byte)123).CompareTo(value));
        }

        [Theory]
        [InlineData((byte)78, (byte)78, true)]
        [InlineData((byte)78, (byte)0, false)]
        [InlineData((byte)0, (byte)0, true)]
        [InlineData((byte)78, null, false)]
        [InlineData((byte)78, "78", false)]
        [InlineData((byte)78, 78, false)]
        public static void EqualsTest(byte b, object obj, bool expected)
        {
            if (obj is byte b2)
            {
                Assert.Equal(expected, b.Equals(b2));
                Assert.Equal(expected, b.GetHashCode().Equals(b2.GetHashCode()));
                Assert.Equal(b, b.GetHashCode());
            }
            Assert.Equal(expected, b.Equals(obj));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsByte()
        {
            Assert.Equal(TypeCode.Byte, ((byte)1).GetTypeCode());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo emptyFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string glike in new[] { "G", "R" })
                {
                    yield return new object[] { (byte)0, glike, emptyFormat, "0" };
                    yield return new object[] { (byte)123, glike, emptyFormat, "123" };
                    yield return new object[] { byte.MaxValue, glike, emptyFormat, "255" };
                }

                yield return new object[] { (byte)123, "D", emptyFormat, "123" };
                yield return new object[] { (byte)123, "D99", emptyFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000123" };

                yield return new object[] { (byte)0, "x", emptyFormat, "0" };
                yield return new object[] { (byte)0x24, "x", emptyFormat, "24" };

                yield return new object[] { (byte)0, "b", emptyFormat, "0" };
                yield return new object[] { (byte)0x24, "b", emptyFormat, "100100" };

                yield return new object[] { (byte)24, "N", emptyFormat, string.Format("{0:N}", 24.00) };
            }

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { (byte)32, "C100", invariantFormat, "\u00A432.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (byte)32, "P100", invariantFormat, "3,200.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { (byte)32, "D100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000032" };
            yield return new object[] { (byte)32, "E100", invariantFormat, "3.2000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { (byte)32, "F100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (byte)32, "N100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (byte)32, "X100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020" };
            yield return new object[] { (byte)32, "B100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000100000" };

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
            yield return new object[] { (byte)24, "N", customFormat, "24~00" };
            yield return new object[] { (byte)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (byte)123, "F", customFormat, "123~00" };
            yield return new object[] { (byte)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(byte b, string format, IFormatProvider provider, string expected)
        {
            // Format is case insensitive
            string upperFormat = format.ToUpperInvariant();
            string lowerFormat = format.ToLowerInvariant();

            string upperExpected = expected.ToUpperInvariant();
            string lowerExpected = expected.ToLowerInvariant();

            bool isDefaultProvider = (provider == null || provider == NumberFormatInfo.CurrentInfo);
            if (string.IsNullOrEmpty(format) || format.ToUpperInvariant() == "G")
            {
                if (isDefaultProvider)
                {
                    Assert.Equal(upperExpected, b.ToString());
                    Assert.Equal(upperExpected, b.ToString((IFormatProvider)null));
                }
                Assert.Equal(upperExpected, b.ToString(provider));
            }
            if (isDefaultProvider)
            {
                Assert.Equal(upperExpected, b.ToString(upperFormat));
                Assert.Equal(lowerExpected, b.ToString(lowerFormat));
                Assert.Equal(upperExpected, b.ToString(upperFormat, null));
                Assert.Equal(lowerExpected, b.ToString(lowerFormat, null));
            }
            Assert.Equal(upperExpected, b.ToString(upperFormat, provider));
            Assert.Equal(lowerExpected, b.ToString(lowerFormat, provider));
        }

        [Fact]
        public static void ToString_InvalidFormat_ThrowsFormatException()
        {
            byte b = 123;
            Assert.Throws<FormatException>(() => b.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => b.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Integer;
            NumberFormatInfo emptyFormat = new NumberFormatInfo();

            NumberFormatInfo customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";

            yield return new object[] { "0", defaultStyle, null, (byte)0 };
            yield return new object[] { "123", defaultStyle, null, (byte)123 };
            yield return new object[] { "+123", defaultStyle, null, (byte)123 };
            yield return new object[] { "  123  ", defaultStyle, null, (byte)123 };
            yield return new object[] { "255", defaultStyle, null, (byte)255 };

            yield return new object[] { "12", NumberStyles.HexNumber, null, (byte)0x12 };
            yield return new object[] { "10010", NumberStyles.BinaryNumber, null, (byte)0b10010 };
            yield return new object[] { "10", NumberStyles.AllowThousands, null, (byte)10 };

            yield return new object[] { "123", defaultStyle, emptyFormat, (byte)123 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, (byte)123 };
            yield return new object[] { "12", NumberStyles.HexNumber, emptyFormat, (byte)0x12 };
            yield return new object[] { "ab", NumberStyles.HexNumber, emptyFormat, (byte)0xab };
            yield return new object[] { "AB", NumberStyles.HexNumber, null, (byte)0xab };
            yield return new object[] { "10010", NumberStyles.BinaryNumber, emptyFormat, (byte)0b10010 };
            yield return new object[] { "10101011", NumberStyles.BinaryNumber, emptyFormat, (byte)0b10101011 };
            yield return new object[] { "10101011", NumberStyles.BinaryNumber, null, (byte)0b10101011 };
            yield return new object[] { "$100", NumberStyles.Currency, customFormat, (byte)100 };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, byte expected)
        {
            byte result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(byte.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, byte.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, byte.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(byte.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, byte.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, byte.Parse(value, provider));
            }

            // Full overloads
            Assert.True(byte.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, byte.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // Include the test data for wider primitives.
            foreach (object[] widerTests in UInt16Tests.Parse_Invalid_TestData())
            {
                yield return widerTests;
            }

            // > max value
            yield return new object[] { "256", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "100", NumberStyles.HexNumber, null, typeof(OverflowException) };
            yield return new object[] { "100000000", NumberStyles.BinaryNumber, null, typeof(OverflowException) };

        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            byte result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(byte.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => byte.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => byte.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(byte.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => byte.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => byte.Parse(value, provider));
            }

            // Full overloads
            Assert.False(byte.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => byte.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.BinaryNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.HexNumber | NumberStyles.BinaryNumber)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00))]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style)
        {
            byte result = 0;
            AssertExtensions.Throws<ArgumentException>("style", () => byte.TryParse("1", style, null, out result));
            Assert.Equal(default(byte), result);

            AssertExtensions.Throws<ArgumentException>("style", () => byte.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>("style", () => byte.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "123", 0, 2, NumberStyles.Integer, null, (byte)12 };
            yield return new object[] { "+123", 0, 2, NumberStyles.Integer, null, (byte)1 };
            yield return new object[] { "+123", 1, 3, NumberStyles.Integer, null, (byte)123 };
            yield return new object[] { "  123  ", 4, 1, NumberStyles.Integer, null, (byte)3 };
            yield return new object[] { "12", 1, 1, NumberStyles.HexNumber, null, (byte)0x2 };
            yield return new object[] { "10010", 1, 4, NumberStyles.BinaryNumber, null, (byte)0b10 };
            yield return new object[] { "10", 0, 1, NumberStyles.AllowThousands, null, (byte)1 };
            yield return new object[] { "$100", 0, 2, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (byte)1 };
        }


        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, byte expected)
        {
            byte result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(byte.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, byte.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(byte.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                byte result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(byte.TryParse(value.AsSpan(), out result));
                    Assert.Equal(0u, result);
                }

                Assert.Throws(exceptionType, () => byte.Parse(value.AsSpan(), style, provider));

                Assert.False(byte.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(0u, result);
            }
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(byte i, string format, IFormatProvider provider, string expected) =>
            NumberFormatTestHelper.TryFormatNumberTest(i, format, provider, expected);
    }
}
