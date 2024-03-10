// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

namespace System.Tests
{
    public class UInt16Tests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new ushort();
            Assert.Equal(0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            ushort i = 41;
            Assert.Equal(41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0xFFFF, ushort.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(0, ushort.MinValue);
        }

        [Theory]
        [InlineData((ushort)234, (ushort)234, 0)]
        [InlineData((ushort)234, ushort.MinValue, 1)]
        [InlineData((ushort)234, (ushort)123, 1)]
        [InlineData((ushort)234, (ushort)456, -1)]
        [InlineData((ushort)234, ushort.MaxValue, -1)]
        [InlineData((ushort)234, null, 1)]
        public void CompareTo_Other_ReturnsExpected(ushort i, object value, int expected)
        {
            if (value is ushort ushortValue)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(ushortValue)));
            }

            Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData(234)]
        public void CompareTo_ObjectNotUshort_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((ushort)123).CompareTo(value));
        }

        [Theory]
        [InlineData((ushort)789, (ushort)789, true)]
        [InlineData((ushort)788, (ushort)0, false)]
        [InlineData((ushort)0, (ushort)0, true)]
        [InlineData((ushort)789, null, false)]
        [InlineData((ushort)789, "789", false)]
        [InlineData((ushort)789, 789, false)]
        public static void EqualsTest(ushort i1, object obj, bool expected)
        {
            if (obj is ushort)
            {
                Assert.Equal(expected, i1.Equals((ushort)obj));
                Assert.Equal(expected, i1.GetHashCode().Equals(((ushort)obj).GetHashCode()));
                Assert.Equal(i1, i1.GetHashCode());
            }
            Assert.Equal(expected, i1.Equals(obj));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsUInt16()
        {
            Assert.Equal(TypeCode.UInt16, ((ushort)1).GetTypeCode());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "", "R" })
                {
                    yield return new object[] { (ushort)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (ushort)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { ushort.MaxValue, defaultSpecifier, defaultFormat, "65535" };
                }

                yield return new object[] { (ushort)123, "D", defaultFormat, "123" };
                yield return new object[] { (ushort)123, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000123" };

                yield return new object[] { (ushort)0, "x", defaultFormat, "0" };
                yield return new object[] { (ushort)0x2468, "x", defaultFormat, "2468" };

                yield return new object[] { (ushort)0, "b", defaultFormat, "0" };
                yield return new object[] { (ushort)0x2468, "b", defaultFormat, "10010001101000" };

                yield return new object[] { (ushort)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
            }

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { (ushort)32, "C100", invariantFormat, "\u00A432.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (ushort)32, "P100", invariantFormat, "3,200.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { (ushort)32, "D100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000032" };
            yield return new object[] { (ushort)32, "E100", invariantFormat, "3.2000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { (ushort)32, "F100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (ushort)32, "N100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (ushort)32, "X100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020" };
            yield return new object[] { (ushort)32, "B100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000100000" };

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
            yield return new object[] { (ushort)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (ushort)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (ushort)123, "F", customFormat, "123~00" };
            yield return new object[] { (ushort)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(ushort i, string format, IFormatProvider provider, string expected)
        {
            // Format should be case insensitive
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
            ushort i = 123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Integer;
            NumberFormatInfo emptyFormat = new NumberFormatInfo();

            NumberFormatInfo customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";

            yield return new object[] { "0", defaultStyle, null, (ushort)0 };
            yield return new object[] { "123", defaultStyle, null, (ushort)123 };
            yield return new object[] { "+123", defaultStyle, null, (ushort)123 };
            yield return new object[] { "  123  ", defaultStyle, null, (ushort)123 };
            yield return new object[] { "65535", defaultStyle, null, (ushort)65535 };

            yield return new object[] { "12", NumberStyles.HexNumber, null, (ushort)0x12 };
            yield return new object[] { "1000", NumberStyles.AllowThousands, null, (ushort)1000 };

            yield return new object[] { "123", defaultStyle, emptyFormat, (ushort)123 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, (ushort)123 };
            yield return new object[] { "12", NumberStyles.HexNumber, emptyFormat, (ushort)0x12 };
            yield return new object[] { "abc", NumberStyles.HexNumber, emptyFormat, (ushort)0xabc };
            yield return new object[] { "ABC", NumberStyles.HexNumber, null, (ushort)0xabc };
            yield return new object[] { "10010", NumberStyles.BinaryNumber, emptyFormat, (ushort)0b10010};
            yield return new object[] { "101010111100", NumberStyles.BinaryNumber, emptyFormat, (ushort)0b101010111100 };
            yield return new object[] { "101010111100", NumberStyles.BinaryNumber, null, (ushort)0b101010111100 };
            yield return new object[] { "$1,000", NumberStyles.Currency, customFormat, (ushort)1000 };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, ushort expected)
        {
            ushort result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(ushort.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ushort.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, ushort.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(ushort.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ushort.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ushort.Parse(value, provider));
            }

            // Full overloads
            Assert.True(ushort.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, ushort.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // Include the test data for wider primitives.
            foreach (object[] widerTests in UInt32Tests.Parse_Invalid_TestData())
            {
                yield return widerTests;
            }

            // > max value
            yield return new object[] { "65536", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "10000", NumberStyles.HexNumber, null, typeof(OverflowException) };
            yield return new object[] { "10000000000000000", NumberStyles.BinaryNumber, null, typeof(OverflowException) };
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            ushort result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(ushort.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => ushort.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => ushort.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(ushort.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => ushort.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ushort.Parse(value, provider));
            }

            // Full overloads
            Assert.False(ushort.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => ushort.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.BinaryNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.HexNumber | NumberStyles.BinaryNumber)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00))]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style)
        {
            ushort result = 0;
            AssertExtensions.Throws<ArgumentException>("style", () => ushort.TryParse("1", style, null, out result));
            Assert.Equal(default(ushort), result);

            AssertExtensions.Throws<ArgumentException>("style", () => ushort.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>("style", () => ushort.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "123", 0, 2, NumberStyles.Integer, null, (ushort)12 };
            yield return new object[] { "123", 1, 2, NumberStyles.Integer, null, (ushort)23 };
            yield return new object[] { "+123", 0, 2, NumberStyles.Integer, null, (ushort)1 };
            yield return new object[] { "+123", 1, 3, NumberStyles.Integer, null, (ushort)123 };
            yield return new object[] { "AJK", 0, 1, NumberStyles.HexNumber, new NumberFormatInfo(), (ushort)0XA };
            yield return new object[] { "111", 0, 1, NumberStyles.BinaryNumber, new NumberFormatInfo(), (ushort)0b1 };
            yield return new object[] { "$1,000", 0, 2, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (ushort)1 };
            yield return new object[] { "$1,000", 1, 3, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (ushort)10 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, ushort expected)
        {
            ushort result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(ushort.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, ushort.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(ushort.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                ushort result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(ushort.TryParse(value.AsSpan(), out result));
                    Assert.Equal(0u, result);
                }

                Assert.Throws(exceptionType, () => ushort.Parse(value.AsSpan(), style, provider));

                Assert.False(ushort.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(0u, result);
            }
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Utf8Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, ushort expected)
        {
            ushort result;
            ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value, offset, count);

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(ushort.TryParse(valueUtf8, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, ushort.Parse(valueUtf8, style, provider));

            Assert.True(ushort.TryParse(valueUtf8, style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Utf8Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                ushort result;
                ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value);

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(ushort.TryParse(valueUtf8, out result));
                    Assert.Equal(0u, result);
                }

                Exception e = Assert.Throws(exceptionType, () => ushort.Parse(Encoding.UTF8.GetBytes(value), style, provider));
                if (e is FormatException fe)
                {
                    Assert.Contains(value, fe.Message);
                }

                Assert.False(ushort.TryParse(valueUtf8, style, provider, out result));
                Assert.Equal(0u, result);
            }
        }

        [Fact]
        public static void Parse_Utf8Span_InvalidUtf8()
        {
            FormatException fe = Assert.Throws<FormatException>(() => ushort.Parse([0xA0]));
            Assert.DoesNotContain("A0", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadOnlySpan", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("\uFFFD", fe.Message, StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(ushort i, string format, IFormatProvider provider, string expected) =>
            NumberFormatTestHelper.TryFormatNumberTest(i, format, provider, expected);
    }
}
