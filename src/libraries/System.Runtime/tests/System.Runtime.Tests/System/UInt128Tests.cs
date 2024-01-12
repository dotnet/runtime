// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using Xunit;

namespace System.Tests
{
    public class UInt128Tests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new UInt128();
            Assert.Equal(0U, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            UInt128 i = 41U;
            Assert.Equal(41U, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), UInt128.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(new UInt128(0x0000_0000_0000_0000, 0x0000_0000_0000_0000), UInt128.MinValue);
        }

        public static IEnumerable<object[]> CompareTo_Other_ReturnsExpected_TestData()
        {
            yield return new object[] { (UInt128)234, (UInt128)234, 0 };
            yield return new object[] { (UInt128)234, UInt128.MinValue, 1 };
            yield return new object[] { (UInt128)234, (UInt128)123, 1 };
            yield return new object[] { (UInt128)234, (UInt128)456, -1 };
            yield return new object[] { (UInt128)234, UInt128.MaxValue, -1 };
            yield return new object[] { (UInt128)234, null, 1 };
        }

        [Theory]
        [MemberData(nameof(CompareTo_Other_ReturnsExpected_TestData))]
        public void CompareTo_Other_ReturnsExpected(UInt128 i, object value, int expected)
        {
            if (value is UInt128 UInt128Value)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(UInt128Value)));
            }

            Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
        }

        public static IEnumerable<object[]> CompareTo_ObjectNotUInt128_ThrowsArgumentException_TestData()
        {
            yield return new object[] { "a" };
            yield return new object[] { 234 };
        }

        [Theory]
        [MemberData(nameof(CompareTo_ObjectNotUInt128_ThrowsArgumentException_TestData))]
        public void CompareTo_ObjectNotUInt128_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((UInt128)123).CompareTo(value));
        }

        public static IEnumerable<object[]> EqualsTest_TestData()
        {
            yield return new object[] { (UInt128)789, (UInt128)789, true };
            yield return new object[] { (UInt128)788, (UInt128)0, false };
            yield return new object[] { (UInt128)0, (UInt128)0, true };
            yield return new object[] { (UInt128)789, null, false };
            yield return new object[] { (UInt128)789, "789", false };
            yield return new object[] { (UInt128)789, 789, false };
        }

        [Theory]
        [MemberData(nameof(EqualsTest_TestData))]
        public static void EqualsTest(UInt128 i1, object obj, bool expected)
        {
            if (obj is UInt128 i2)
            {
                Assert.Equal(expected, i1.Equals(i2));
                Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
            }
            Assert.Equal(expected, i1.Equals(obj));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "", "R" })
                {
                    yield return new object[] { (UInt128)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (UInt128)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { new UInt128(0x0000_0000_0000_0001, 0x0000_0000_0000_0003), defaultSpecifier, defaultFormat, "18446744073709551619" };
                    yield return new object[] { new UInt128(0x0000_0000_0000_0001, 0x0000_0000_0000_000A), defaultSpecifier, defaultFormat, "18446744073709551626" };
                    yield return new object[] { new UInt128(0x0000_0000_0000_0005, 0x0000_0000_0000_0001), defaultSpecifier, defaultFormat, "92233720368547758081" };
                    yield return new object[] { new UInt128(0x0000_0000_0000_0005, 0x6BC7_5E2D_6310_0000), defaultSpecifier, defaultFormat, "100000000000000000000" };
                    yield return new object[] { new UInt128(0x0000_0000_0000_0036, 0x35C9_ADC5_DEA0_0000), defaultSpecifier, defaultFormat, "1000000000000000000000" };
                    yield return new object[] { new UInt128(0x0013_4261_72C7_4D82, 0x2B87_8FE8_0000_0000), defaultSpecifier, defaultFormat, "100000000000000000000000000000000000" };
                    yield return new object[] { new UInt128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), defaultSpecifier, defaultFormat, "170141183460469231731687303715884105727" };
                    yield return new object[] { new UInt128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), defaultSpecifier, defaultFormat, "170141183460469231731687303715884105728" };
                    yield return new object[] { UInt128.MaxValue, defaultSpecifier, defaultFormat, "340282366920938463463374607431768211455" };
                }

                yield return new object[] { (UInt128)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (UInt128)4567, "D18", defaultFormat, "000000000000004567" };

                yield return new object[] { (UInt128)0, "x", defaultFormat, "0" };
                yield return new object[] { (UInt128)0x2468, "x", defaultFormat, "2468" };

                yield return new object[] { (UInt128)0, "b", defaultFormat, "0" };
                yield return new object[] { (UInt128)0x2468, "b", defaultFormat, "10010001101000" };

                yield return new object[] { (UInt128)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };


            }

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { (UInt128)32, "C100", invariantFormat, "\u00A432.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (UInt128)32, "P100", invariantFormat, "3,200.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { (UInt128)32, "D100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000032" };
            yield return new object[] { (UInt128)32, "E100", invariantFormat, "3.2000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { (UInt128)32, "F100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (UInt128)32, "N100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (UInt128)32, "X100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020" };
            yield return new object[] { (UInt128)32, "B100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000100000" };

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
            yield return new object[] { (UInt128)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (UInt128)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (UInt128)123, "F", customFormat, "123~00" };
            yield return new object[] { (UInt128)123, "P", customFormat, "12,300.00000 @" };
            yield return new object[] { UInt128.MaxValue, "n5", customFormat, "340*282*366*920*938*463*463*374*607*431*768*211*455~00000" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(UInt128 i, string format, IFormatProvider provider, string expected)
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
            UInt128 i = 123U;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            // Reuse all Int128 test data that's relevant
            foreach (object[] objs in Int128Tests.Parse_Valid_TestData())
            {
                if ((Int128)objs[3] < 0) continue;
                yield return new object[] { objs[0], objs[1], objs[2], (UInt128)(Int128)objs[3] };
            }

            // All lengths decimal
            {
                string s = "";
                UInt128 result = 0U;
                for (int i = 1; i <= 39; i++)
                {
                    result = (result * 10U) + (UInt128)(i % 10);
                    s += (i % 10).ToString();
                    yield return new object[] { s, NumberStyles.Integer, null, result };
                }
            }

            // All lengths hexadecimal
            {
                string s = "";
                UInt128 result = 0U;
                for (int i = 1; i <= 32; i++)
                {
                    result = (result * 16U) + (UInt128)(i % 16);
                    s += (i % 16).ToString("X");
                    yield return new object[] { s, NumberStyles.HexNumber, null, result };
                }
            }

            // All lengths binary
            {
                string s = "";
                UInt128 result = 0U;
                for (int i = 1; i <= 128; i++)
                {
                    result = (result * 2U) + (UInt128)(i % 2);
                    s += (i % 2).ToString("b");
                    yield return new object[] { s, NumberStyles.BinaryNumber, null, result };
                }
            }

            // And test boundary conditions for UInt128
            yield return new object[] { "340282366920938463463374607431768211455", NumberStyles.Integer, null, UInt128.MaxValue };
            yield return new object[] { "+340282366920938463463374607431768211455", NumberStyles.Integer, null, UInt128.MaxValue };
            yield return new object[] { "    +340282366920938463463374607431768211455  ", NumberStyles.Integer, null, UInt128.MaxValue };
            yield return new object[] { "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber, null, UInt128.MaxValue };
            yield return new object[] { "   FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF   ", NumberStyles.HexNumber, null, UInt128.MaxValue };
            yield return new object[] { "11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111", NumberStyles.BinaryNumber, null, UInt128.MaxValue };
            yield return new object[] { "   11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111   ", NumberStyles.BinaryNumber, null, UInt128.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, UInt128 expected)
        {
            UInt128 result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(UInt128.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, UInt128.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, UInt128.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(UInt128.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, UInt128.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, UInt128.Parse(value, provider));
            }

            // Full overloads
            Assert.True(UInt128.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, UInt128.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // Reuse all Int128 test data, except for those that wouldn't overflow UInt128.
            foreach (object[] objs in Int128Tests.Parse_Invalid_TestData())
            {
                if ((Type)objs[3] == typeof(OverflowException) &&
                    (((NumberStyles)objs[1] & NumberStyles.AllowBinarySpecifier) != 0 || // TODO https://github.com/dotnet/runtime/issues/83619: Remove once BigInteger supports binary parsing
                     !BigInteger.TryParse((string)objs[0], (NumberStyles)objs[1], null, out BigInteger bi) ||
                     bi <= UInt128.MaxValue))
                {
                    continue;
                }

                yield return objs;
            }

            // < min value
            foreach (string ws in new[] { "", "    " })
            {
                yield return new object[] { ws + "-1" + ws, NumberStyles.Integer, null, typeof(OverflowException) };
                yield return new object[] { ws + "abc123" + ws, NumberStyles.Integer, new NumberFormatInfo { NegativeSign = "abc" }, typeof(OverflowException) };
            }

            // > max value
            yield return new object[] { "340282366920938463463374607431768211456", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "100000000000000000000000000000000", NumberStyles.HexNumber, null, typeof(OverflowException) };
            yield return new object[] { "100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", NumberStyles.BinaryNumber, null, typeof(OverflowException) };
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            UInt128 result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(UInt128.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => UInt128.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => UInt128.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(UInt128.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => UInt128.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => UInt128.Parse(value, provider));
            }

            // Full overloads
            Assert.False(UInt128.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => UInt128.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.BinaryNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.HexNumber | NumberStyles.BinaryNumber)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00))]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style)
        {
            UInt128 result = 0U;
            AssertExtensions.Throws<ArgumentException>("style", () => UInt128.TryParse("1", style, null, out result));
            Assert.Equal(default(UInt128), result);

            AssertExtensions.Throws<ArgumentException>("style", () => UInt128.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>("style", () => UInt128.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "+123", 1, 3, NumberStyles.Integer, null, (UInt128)123 };
            yield return new object[] { "+123", 0, 3, NumberStyles.Integer, null, (UInt128)12 };
            yield return new object[] { "  123  ", 1, 2, NumberStyles.Integer, null, (UInt128)1 };
            yield return new object[] { "12", 0, 1, NumberStyles.HexNumber, null, (UInt128)0x1 };
            yield return new object[] { "ABC", 1, 1, NumberStyles.HexNumber, null, (UInt128)0xb };
            yield return new object[] { "12", 0, 1, NumberStyles.BinaryNumber, null, (UInt128)0b1 };
            yield return new object[] { "A0C", 1, 1, NumberStyles.BinaryNumber, null, (UInt128)0b0 };
            yield return new object[] { "$1,000", 1, 3, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (UInt128)10 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, UInt128 expected)
        {
            UInt128 result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(UInt128.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, UInt128.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(UInt128.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                UInt128 result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(UInt128.TryParse(value.AsSpan(), out result));
                    Assert.Equal(0u, result);
                }

                Assert.Throws(exceptionType, () => UInt128.Parse(value.AsSpan(), style, provider));

                Assert.False(UInt128.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(0u, result);
            }
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Utf8Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, UInt128 expected)
        {
            UInt128 result;
            ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value, offset, count);

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(UInt128.TryParse(valueUtf8, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, UInt128.Parse(valueUtf8, style, provider));

            Assert.True(UInt128.TryParse(valueUtf8, style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Utf8Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                UInt128 result;
                ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value);

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(UInt128.TryParse(valueUtf8, out result));
                    Assert.Equal(0u, result);
                }

                Exception e = Assert.Throws(exceptionType, () => UInt128.Parse(Encoding.UTF8.GetBytes(value), style, provider));
                if (e is FormatException fe)
                {
                    Assert.Contains(value, fe.Message);
                }

                Assert.False(UInt128.TryParse(valueUtf8, style, provider, out result));
                Assert.Equal(0u, result);
            }
        }

        [Fact]
        public static void Parse_Utf8Span_InvalidUtf8()
        {
            FormatException fe = Assert.Throws<FormatException>(() => UInt128.Parse([0xA0]));
            Assert.DoesNotContain("A0", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadOnlySpan", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("\uFFFD", fe.Message, StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(UInt128 i, string format, IFormatProvider provider, string expected) =>
            NumberFormatTestHelper.TryFormatNumberTest(i, format, provider, expected);

        [Fact]
        public static void Runtime75416()
        {
            UInt128 a = (UInt128Tests_GenericMath.Int128MaxValue - 10u) * +100u;
            Assert.Equal(a, (UInt128)(Int128)(-1100));

            UInt128 b = (UInt128Tests_GenericMath.Int128MaxValue - 10u) * (UInt128)(Int128)(-100);
            Assert.Equal(b, 1100u);
        }
    }
}
