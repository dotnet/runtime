// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using Xunit;

namespace System.Tests
{
    public class UInt32Tests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new uint();
            Assert.Equal((uint)0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            uint i = 41;
            Assert.Equal((uint)41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0xFFFFFFFF, uint.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal((uint)0, uint.MinValue);
        }

        [Theory]
        [InlineData((uint)234, (uint)234, 0)]
        [InlineData((uint)234, uint.MinValue, 1)]
        [InlineData((uint)234, (uint)123, 1)]
        [InlineData((uint)234, (uint)456, -1)]
        [InlineData((uint)234, uint.MaxValue, -1)]
        [InlineData((uint)234, null, 1)]
        public void CompareTo_Other_ReturnsExpected(uint i, object value, int expected)
        {
            if (value is uint uintValue)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(uintValue)));
            }

            Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData(234)]
        public void CompareTo_ObjectNotUint_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((uint)123).CompareTo(value));
        }

        [Theory]
        [InlineData((uint)789, (uint)789, true)]
        [InlineData((uint)788, (uint)0, false)]
        [InlineData((uint)0, (uint)0, true)]
        [InlineData((uint)789, null, false)]
        [InlineData((uint)789, "789", false)]
        [InlineData((uint)789, 789, false)]
        public static void EqualsTest(uint i1, object obj, bool expected)
        {
            if (obj is uint)
            {
                uint i2 = (uint)obj;
                Assert.Equal(expected, i1.Equals(i2));
                Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
                Assert.Equal((int)i1, i1.GetHashCode());
            }
            Assert.Equal(expected, i1.Equals(obj));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsUInt32()
        {
            Assert.Equal(TypeCode.UInt32, ((uint)1).GetTypeCode());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "", "R" })
                {
                    yield return new object[] { (uint)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (uint)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { uint.MaxValue, defaultSpecifier, defaultFormat, "4294967295" };
                }

                yield return new object[] { (uint)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (uint)4567, "D18", defaultFormat, "000000000000004567" };

                yield return new object[] { (uint)0, "x", defaultFormat, "0" };
                yield return new object[] { (uint)0x2468, "x", defaultFormat, "2468" };

                yield return new object[] { (uint)0, "b", defaultFormat, "0" };
                yield return new object[] { (uint)0x2468, "b", defaultFormat, "10010001101000" };

                yield return new object[] { (uint)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
            }

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { (uint)32, "C100", invariantFormat, "\u00A432.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (uint)32, "P100", invariantFormat, "3,200.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { (uint)32, "D100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000032" };
            yield return new object[] { (uint)32, "E100", invariantFormat, "3.2000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { (uint)32, "F100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (uint)32, "N100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (uint)32, "X100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020" };
            yield return new object[] { (uint)32, "B100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000100000" };

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
            yield return new object[] { (uint)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (uint)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (uint)123, "F", customFormat, "123~00" };
            yield return new object[] { (uint)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(uint i, string format, IFormatProvider provider, string expected)
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
            uint i = 123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            // Reuse all Int32 test data that's relevant
            foreach (object[] objs in Int32Tests.Parse_Valid_TestData())
            {
                if ((int)objs[3] < 0) continue;
                yield return new object[] { objs[0], objs[1], objs[2], (uint)(int)objs[3] };
            }

            // All lengths decimal
            {
                string s = "";
                uint result = 0;
                for (int i = 1; i <= 10; i++)
                {
                    result = (uint)(result * 10 + (i % 10));
                    s += (i % 10).ToString();
                    yield return new object[] { s, NumberStyles.Integer, null, result };
                }
            }

            // All lengths hexadecimal
            {
                string s = "";
                uint result = 0;
                for (int i = 1; i <= 8; i++)
                {
                    result = (uint)((result * 16) + (i % 16));
                    s += (i % 16).ToString("X");
                    yield return new object[] { s, NumberStyles.HexNumber, null, result };
                }
            }

            // All lengths binary
            {
                string s = "";
                uint result = 0;
                for (int i = 1; i <= 32; i++)
                {
                    result = (uint)((result * 2) + (i % 2));
                    s += (i % 2).ToString("B");
                    yield return new object[] { s, NumberStyles.BinaryNumber, null, result };
                }
            }

            // And test boundary conditions for UInt32
            yield return new object[] { "4294967295", NumberStyles.Integer, null, uint.MaxValue };
            yield return new object[] { "+4294967295", NumberStyles.Integer, null, uint.MaxValue };
            yield return new object[] { "  +4294967295  ", NumberStyles.Integer, null, uint.MaxValue };
            yield return new object[] { "FFFFFFFF", NumberStyles.HexNumber, null, uint.MaxValue };
            yield return new object[] { "  FFFFFFFF  ", NumberStyles.HexNumber, null, uint.MaxValue };
            yield return new object[] { "11111111111111111111111111111111", NumberStyles.BinaryNumber, null, uint.MaxValue };
            yield return new object[] { "  11111111111111111111111111111111  ", NumberStyles.BinaryNumber, null, uint.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, uint expected)
        {
            uint result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(uint.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, uint.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, uint.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(uint.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, uint.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, uint.Parse(value, provider));
            }

            // Full overloads
            Assert.True(uint.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, uint.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // Include the test data for wider primitives.
            foreach (object[] widerTests in UInt64Tests.Parse_Invalid_TestData())
            {
                yield return widerTests;
            }

            // > max value
            yield return new object[] { "4294967296", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "100000000", NumberStyles.HexNumber, null, typeof(OverflowException) };
            yield return new object[] { "100000000000000000000000000000000", NumberStyles.BinaryNumber, null, typeof(OverflowException) };

        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            uint result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(uint.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => uint.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => uint.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(uint.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => uint.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => uint.Parse(value, provider));
            }

            // Full overloads
            Assert.False(uint.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => uint.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.BinaryNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.HexNumber | NumberStyles.BinaryNumber)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00))]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style)
        {
            uint result = 0;
            AssertExtensions.Throws<ArgumentException>("style", () => uint.TryParse("1", style, null, out result));
            Assert.Equal(default(uint), result);

            AssertExtensions.Throws<ArgumentException>("style", () => uint.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>("style", () => uint.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "123", 0, 2, NumberStyles.Integer, null, (uint)12 };
            yield return new object[] { "123", 1, 2, NumberStyles.Integer, null, (uint)23 };
            yield return new object[] { "4294967295", 0, 1, NumberStyles.Integer, null, 4 };
            yield return new object[] { "4294967295", 9, 1, NumberStyles.Integer, null, 5 };
            yield return new object[] { "12", 0, 1, NumberStyles.HexNumber, null, (uint)0x1 };
            yield return new object[] { "12", 1, 1, NumberStyles.HexNumber, null, (uint)0x2 };
            yield return new object[] { "10", 0, 1, NumberStyles.BinaryNumber, null, (uint)0b1 };
            yield return new object[] { "10", 1, 1, NumberStyles.BinaryNumber, null, (uint)0b0 };
            yield return new object[] { "$1,000", 1, 3, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (uint)10 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, uint expected)
        {
            uint result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(uint.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, uint.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(uint.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                uint result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(uint.TryParse(value.AsSpan(), out result));
                    Assert.Equal(0u, result);
                }

                Assert.Throws(exceptionType, () => uint.Parse(value.AsSpan(), style, provider));

                Assert.False(uint.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(0u, result);
            }
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Utf8Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, uint expected)
        {
            uint result;
            ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value, offset, count);

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(uint.TryParse(valueUtf8, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, uint.Parse(valueUtf8, style, provider));

            Assert.True(uint.TryParse(valueUtf8, style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Utf8Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                uint result;
                ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value);

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(uint.TryParse(valueUtf8, out result));
                    Assert.Equal(0u, result);
                }

                Exception e = Assert.Throws(exceptionType, () => uint.Parse(Encoding.UTF8.GetBytes(value), style, provider));
                if (e is FormatException fe)
                {
                    Assert.Contains(value, fe.Message);
                }

                Assert.False(uint.TryParse(valueUtf8, style, provider, out result));
                Assert.Equal(0u, result);
            }
        }

        [Fact]
        public static void Parse_Utf8Span_InvalidUtf8()
        {
            FormatException fe = Assert.Throws<FormatException>(() => uint.Parse([0xA0]));
            Assert.DoesNotContain("A0", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadOnlySpan", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("\uFFFD", fe.Message, StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(uint i, string format, IFormatProvider provider, string expected) =>
            NumberFormatTestHelper.TryFormatNumberTest(i, format, provider, expected);
    }
}
