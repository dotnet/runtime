// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class SByteTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new sbyte();
            Assert.Equal(0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            sbyte i = 41;
            Assert.Equal(41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0x7F, sbyte.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(-0x80, sbyte.MinValue);
        }

        [Theory]
        [InlineData((sbyte)114, (sbyte)114, 0)]
        [InlineData((sbyte)114, sbyte.MinValue, 1)]
        [InlineData((sbyte)-114, sbyte.MinValue, 1)]
        [InlineData(sbyte.MinValue, sbyte.MinValue, 0)]
        [InlineData((sbyte)114, (sbyte)-123, 1)]
        [InlineData((sbyte)114, (sbyte)0, 1)]
        [InlineData((sbyte)114, (sbyte)123, -1)]
        [InlineData((sbyte)114, sbyte.MaxValue, -1)]
        [InlineData((sbyte)-114, sbyte.MaxValue, -1)]
        [InlineData(sbyte.MaxValue, sbyte.MaxValue, 0)]
        [InlineData((sbyte)114, null, 1)]
        public void CompareTo_Other_ReturnsExpected(sbyte i, object value, int expected)
        {
            if (value is sbyte sbyteValue)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(sbyteValue)));
                Assert.Equal(-expected, Math.Sign(sbyteValue.CompareTo(i)));
            }

            Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData(234)]
        public void CompareTo_ObjectNotSByte_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((sbyte)123).CompareTo(value));
        }

        [Theory]
        [InlineData((sbyte)78, (sbyte)78, true)]
        [InlineData((sbyte)78, (sbyte)-78, false)]
        [InlineData((sbyte)78, (sbyte)0, false)]
        [InlineData((sbyte)0, (sbyte)0, true)]
        [InlineData((sbyte)-78, (sbyte)-78, true)]
        [InlineData((sbyte)-78, (sbyte)78, false)]
        [InlineData((sbyte)78, null, false)]
        [InlineData((sbyte)78, "78", false)]
        [InlineData((sbyte)78, 78, false)]
        public static void EqualsTest(sbyte i1, object obj, bool expected)
        {
            if (obj is sbyte)
            {
                sbyte i2 = (sbyte)obj;
                Assert.Equal(expected, i1.Equals(i2));
                Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
            }
            Assert.Equal(expected, i1.Equals(obj));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsSByte()
        {
            Assert.Equal(TypeCode.SByte, ((sbyte)1).GetTypeCode());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "", "R" })
                {
                    yield return new object[] { sbyte.MinValue, defaultSpecifier, defaultFormat, "-128" };
                    yield return new object[] { (sbyte)-123, defaultSpecifier, defaultFormat, "-123" };
                    yield return new object[] { (sbyte)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (sbyte)123, defaultSpecifier, defaultFormat, "123" };
                    yield return new object[] { sbyte.MaxValue, defaultSpecifier, defaultFormat, "127" };
                }

                yield return new object[] { (sbyte)123, "D", defaultFormat, "123" };
                yield return new object[] { (sbyte)123, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000123" };
                yield return new object[] { (sbyte)123, "D99\09", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000123" };
                yield return new object[] { (sbyte)-123, "D99", defaultFormat, "-000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000123" };

                yield return new object[] { (sbyte)0x24, "x", defaultFormat, "24" };
                yield return new object[] { (sbyte)-0x24, "x", defaultFormat, "dc" };
                yield return new object[] { (sbyte)24, "N", defaultFormat, string.Format("{0:N}", 24.00) };


            }

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { (sbyte)32, "C100", invariantFormat, "¤32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (sbyte)32, "P100", invariantFormat, "3,200.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { (sbyte)32, "D100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000032" };
            yield return new object[] { (sbyte)32, "E100", invariantFormat, "3.2000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { (sbyte)32, "F100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (sbyte)32, "N100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (sbyte)32, "X100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020" };

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
            yield return new object[] { (sbyte)-24, "N", customFormat, "#24~00" };
            yield return new object[] { (sbyte)24, "N", customFormat, "24~00" };
            yield return new object[] { (sbyte)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (sbyte)123, "F", customFormat, "123~00" };
            yield return new object[] { (sbyte)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(sbyte i, string format, IFormatProvider provider, string expected)
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
            sbyte b = 123;
            Assert.Throws<FormatException>(() => b.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => b.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Integer;
            NumberFormatInfo emptyFormat = new NumberFormatInfo();

            NumberFormatInfo customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";

            yield return new object[] { "-123", defaultStyle, null, (sbyte)-123 };
            yield return new object[] { "0", defaultStyle, null, (sbyte)0 };
            yield return new object[] { "123", defaultStyle, null, (sbyte)123 };
            yield return new object[] { "+123", defaultStyle, null, (sbyte)123 };
            yield return new object[] { "  123  ", defaultStyle, null, (sbyte)123 };
            yield return new object[] { "127", defaultStyle, null, (sbyte)127 };

            yield return new object[] { "12", NumberStyles.HexNumber, null, (sbyte)0x12 };
            yield return new object[] { "10", NumberStyles.AllowThousands, null, (sbyte)10 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, null, (sbyte)-123 }; // Parentheses = negative

            yield return new object[] { "123", defaultStyle, emptyFormat, (sbyte)123 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, (sbyte)123 };
            yield return new object[] { "12", NumberStyles.HexNumber, emptyFormat, (sbyte)0x12 };
            yield return new object[] { "a", NumberStyles.HexNumber, null, (sbyte)0xa };
            yield return new object[] { "A", NumberStyles.HexNumber, null, (sbyte)0xa };
            yield return new object[] { "$100", NumberStyles.Currency, customFormat, (sbyte)100 };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, sbyte expected)
        {
            sbyte result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(sbyte.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, sbyte.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, sbyte.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(sbyte.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, sbyte.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, sbyte.Parse(value, provider));
            }

            // Full overloads
            Assert.True(sbyte.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, sbyte.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // Include the test data for wider primitives.
            foreach (object[] widerTests in Int32Tests.Parse_Invalid_TestData())
            {
                yield return widerTests;
            }

            yield return new object[] { "-129", NumberStyles.Integer, null, typeof(OverflowException) }; // < min value
            yield return new object[] { "128", NumberStyles.Integer, null, typeof(OverflowException) }; // > max value

            yield return new object[] { "FFFFFFFF", NumberStyles.HexNumber, null, typeof(OverflowException) }; // Hex number < 0
            yield return new object[] { "100", NumberStyles.HexNumber, null, typeof(OverflowException) }; // Hex number > max value
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            sbyte result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(sbyte.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => sbyte.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => sbyte.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(sbyte.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => sbyte.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => sbyte.Parse(value, provider));
            }

            // Full overloads
            Assert.False(sbyte.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => sbyte.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            sbyte result = 0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => sbyte.TryParse("1", style, null, out result));
            Assert.Equal(default(sbyte), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => sbyte.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => sbyte.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "-123", 0, 2, NumberStyles.Integer, null, (sbyte)-1 };
            yield return new object[] { "-123", 1, 3, NumberStyles.Integer, null, (sbyte)123 };
            yield return new object[] { "12", 0, 1, NumberStyles.HexNumber, null, (sbyte)0x1 };
            yield return new object[] { "12", 1, 1, NumberStyles.HexNumber, null, (sbyte)0x2 };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, null, (sbyte)123 };
            yield return new object[] { "$100", 1, 1, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (sbyte)1 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, sbyte expected)
        {
            sbyte result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(sbyte.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, sbyte.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(sbyte.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                sbyte result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(sbyte.TryParse(value.AsSpan(), out result));
                    Assert.Equal(0, result);
                }

                Assert.Throws(exceptionType, () => sbyte.Parse(value.AsSpan(), style, provider));

                Assert.False(sbyte.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(0, result);
            }
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(sbyte i, string format, IFormatProvider provider, string expected)
        {
            char[] actual;
            int charsWritten;

            // Just right
            actual = new char[expected.Length];
            Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(actual));

            // Longer than needed
            actual = new char[expected.Length + 1];
            Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(actual, 0, charsWritten));

            // Too short
            if (expected.Length > 0)
            {
                actual = new char[expected.Length - 1];
                Assert.False(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
                Assert.Equal(0, charsWritten);
            }

            if (format != null)
            {
                // Upper format
                actual = new char[expected.Length];
                Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToUpperInvariant(), provider));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected.ToUpperInvariant(), new string(actual));

                // Lower format
                actual = new char[expected.Length];
                Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToLowerInvariant(), provider));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected.ToLowerInvariant(), new string(actual));
            }
        }
    }
}
