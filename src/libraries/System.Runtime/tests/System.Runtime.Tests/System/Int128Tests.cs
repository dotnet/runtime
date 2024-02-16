// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using Xunit;

namespace System.Tests
{
    public class Int128Tests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new Int128();
            Assert.Equal(0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            Int128 i = 41;
            Assert.Equal(41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), Int128.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000), Int128.MinValue);
        }

        public static IEnumerable<object[]> CompareTo_Other_ReturnsExpected_TestData()
        {
            yield return new object[] { (Int128)234, (Int128)234, 0 };
            yield return new object[] { (Int128)234, Int128.MinValue, 1 };
            yield return new object[] { (Int128)(-234), Int128.MinValue, 1 };
            yield return new object[] { Int128.MinValue, Int128.MinValue, 0 };
            yield return new object[] { (Int128)234, (Int128)(-123), 1 };
            yield return new object[] { (Int128)234, (Int128)0, 1 };
            yield return new object[] { (Int128)234, (Int128)123, 1 };
            yield return new object[] { (Int128)234, (Int128)456, -1 };
            yield return new object[] { (Int128)234, Int128.MaxValue, -1 };
            yield return new object[] { (Int128)(-234), Int128.MaxValue, -1 };
            yield return new object[] { Int128.MaxValue, Int128.MaxValue, 0 };
            yield return new object[] { (Int128)(-234), (Int128)(-234), 0 };
            yield return new object[] { (Int128)(-234), (Int128)234, -1 };
            yield return new object[] { (Int128)(-234), (Int128)(-432), 1 };
            yield return new object[] { (Int128)234, null, 1 };
        }

        [Theory]
        [MemberData(nameof(CompareTo_Other_ReturnsExpected_TestData))]
        public void CompareTo_Other_ReturnsExpected(Int128 i, object value, int expected)
        {
            if (value is Int128 int128Value)
            {
                Assert.Equal(expected, Int128.Sign(i.CompareTo(int128Value)));
                Assert.Equal(-expected, Int128.Sign(int128Value.CompareTo(i)));
            }

            Assert.Equal(expected, Int128.Sign(i.CompareTo(value)));
        }

        public static IEnumerable<object[]> CompareTo_ObjectNotInt128_ThrowsArgumentException_TestData()
        {
            yield return new object[] { "a" };
            yield return new object[] { 234 };
        }

        [Theory]
        [MemberData(nameof(CompareTo_ObjectNotInt128_ThrowsArgumentException_TestData))]
        public void CompareTo_ObjectNotInt128_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((Int128)123).CompareTo(value));
        }

        public static IEnumerable<object[]> EqualsTest_TestData()
        {
            yield return new object[] { (Int128)789, (Int128)789, true };
            yield return new object[] { (Int128)789, (Int128)(-789), false };
            yield return new object[] { (Int128)789, (Int128)0, false };
            yield return new object[] { (Int128)0, (Int128)0, true };
            yield return new object[] { (Int128)(-789), (Int128)(-789), true };
            yield return new object[] { (Int128)(-789), (Int128)789, false };
            yield return new object[] { (Int128)789, null, false };
            yield return new object[] { (Int128)789, "789", false };
            yield return new object[] { (Int128)789, 789, false };
        }

        [Theory]
        [MemberData(nameof(EqualsTest_TestData))]
        public static void EqualsTest(Int128 i1, object obj, bool expected)
        {
            if (obj is Int128 i2)
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
                    yield return new object[] { Int128.MinValue, defaultSpecifier, defaultFormat, "-170141183460469231731687303715884105728" };
                    yield return new object[] { (Int128)(-4567), defaultSpecifier, defaultFormat, "-4567" };
                    yield return new object[] { (Int128)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (Int128)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { new Int128(0x0000_0000_0000_0001, 0x0000_0000_0000_0003), defaultSpecifier, defaultFormat, "18446744073709551619" };
                    yield return new object[] { new Int128(0x0000_0000_0000_0001, 0x0000_0000_0000_000A), defaultSpecifier, defaultFormat, "18446744073709551626" };
                    yield return new object[] { new Int128(0x0000_0000_0000_0005, 0x0000_0000_0000_0001), defaultSpecifier, defaultFormat, "92233720368547758081" };
                    yield return new object[] { new Int128(0x0000_0000_0000_0005, 0x6BC7_5E2D_6310_0000), defaultSpecifier, defaultFormat, "100000000000000000000" };
                    yield return new object[] { new Int128(0x0000_0000_0000_0036, 0x35C9_ADC5_DEA0_0000), defaultSpecifier, defaultFormat, "1000000000000000000000" };
                    yield return new object[] { new Int128(0x0013_4261_72C7_4D82, 0x2B87_8FE8_0000_0000), defaultSpecifier, defaultFormat, "100000000000000000000000000000000000" };
                    yield return new object[] { Int128.MaxValue, defaultSpecifier, defaultFormat, "170141183460469231731687303715884105727" };
                }

                yield return new object[] { (Int128)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (Int128)4567, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (Int128)4567, "D99\09", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (Int128)(-4567), "D99", defaultFormat, "-000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };

                yield return new object[] { (Int128)0, "x", defaultFormat, "0" };
                yield return new object[] { (Int128)0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { (Int128)(-0x2468), "x", defaultFormat, "ffffffffffffffffffffffffffffdb98" };

                yield return new object[] { (Int128)0, "b", defaultFormat, "0" };
                yield return new object[] { (Int128)0x2468, "b", defaultFormat, "10010001101000" };
                yield return new object[] { (Int128)(-0x2468), "b", defaultFormat, "11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111101101110011000" };

                yield return new object[] { (Int128)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
            }

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { (Int128)32, "C100", invariantFormat, "\u00A432.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (Int128)32, "P100", invariantFormat, "3,200.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { (Int128)32, "D100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000032" };
            yield return new object[] { (Int128)32, "E100", invariantFormat, "3.2000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { (Int128)32, "F100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (Int128)32, "N100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (Int128)32, "X100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020" };
            yield return new object[] { (Int128)32, "B100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000100000" };

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
            yield return new object[] { (Int128)(-2468), "N", customFormat, "#2*468~00" };
            yield return new object[] { (Int128)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (Int128)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (Int128)123, "F", customFormat, "123~00" };
            yield return new object[] { (Int128)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(Int128 i, string format, IFormatProvider provider, string expected)
        {
            // Format is case insensitive
            string upperFormat = format.ToUpperInvariant();
            string lowerFormat = format.ToLowerInvariant();

            string upperExpected = expected.ToUpperInvariant();
            string lowerExpected = expected.ToLowerInvariant();

            bool isDefaultProvider = (provider is null) || (provider == NumberFormatInfo.CurrentInfo);

            if (string.IsNullOrEmpty(format) || (format.ToUpperInvariant() is "G" or "R"))
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
            Int128 i = 123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            // Reuse all Int64 test data
            foreach (object[] objs in Int64Tests.Parse_Valid_TestData())
            {
                bool unsigned =
                    (((NumberStyles)objs[1]) & NumberStyles.HexNumber) == NumberStyles.HexNumber ||
                    (((NumberStyles)objs[1]) & NumberStyles.BinaryNumber) == NumberStyles.BinaryNumber;
                yield return new object[] { objs[0], objs[1], objs[2], unsigned ? (Int128)(ulong)(long)objs[3] : (Int128)(long)objs[3] };
            }

            // All lengths decimal
            foreach (bool neg in new[] { false, true })
            {
                string s = neg ? "-" : "";
                Int128 result = 0;
                for (int i = 1; i <= 39; i++)
                {
                    result = (result * 10) + (i % 10);
                    s += (i % 10).ToString();
                    yield return new object[] { s, NumberStyles.Integer, null, neg ? result * -1 : result };
                }
            }

            // All lengths hexadecimal
            {
                string s = "";
                Int128 result = 0;
                for (int i = 1; i <= 32; i++)
                {
                    result = (result * 16) + (i % 16);
                    s += (i % 16).ToString("X");
                    yield return new object[] { s, NumberStyles.HexNumber, null, result };
                }
            }

            // All lengths binary
            {
                string s = "";
                Int128 result = 0;
                for (int i = 1; i <= 128; i++)
                {
                    result = (result * 2) + (i % 2);
                    s += (i % 2).ToString("b");
                    yield return new object[] { s, NumberStyles.BinaryNumber, null, result };
                }
            }

            // And test boundary conditions for Int128
            yield return new object[] { "-170141183460469231731687303715884105728", NumberStyles.Integer, null, Int128.MinValue };
            yield return new object[] { "170141183460469231731687303715884105727", NumberStyles.Integer, null, Int128.MaxValue };
            yield return new object[] { "   -170141183460469231731687303715884105728   ", NumberStyles.Integer, null, Int128.MinValue };
            yield return new object[] { "   +170141183460469231731687303715884105727   ", NumberStyles.Integer, null, Int128.MaxValue };
            yield return new object[] { "7FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber, null, Int128.MaxValue };
            yield return new object[] { "80000000000000000000000000000000", NumberStyles.HexNumber, null, Int128.MinValue };
            yield return new object[] { "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber, null, (Int128)(-1) };
            yield return new object[] { "   FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF  ", NumberStyles.HexNumber, null, (Int128)(-1) };
            yield return new object[] { "01111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111", NumberStyles.BinaryNumber, null, Int128.MaxValue };
            yield return new object[] { "10000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", NumberStyles.BinaryNumber, null, Int128.MinValue };
            yield return new object[] { "11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111", NumberStyles.BinaryNumber, null, (Int128)(-1) };
            yield return new object[] { "   11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111  ", NumberStyles.BinaryNumber, null, (Int128)(-1) };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, Int128 expected)
        {
            Int128 result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(Int128.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, Int128.Parse(value));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, Int128.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(Int128.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, Int128.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, Int128.Parse(value, provider));
            }

            // Full overloads
            Assert.True(Int128.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, Int128.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // Reuse all int test data, except for those that wouldn't overflow Int128.
            foreach (object[] objs in Int32Tests.Parse_Invalid_TestData())
            {
                if ((Type)objs[3] == typeof(OverflowException) &&
                    (((NumberStyles)objs[1] & NumberStyles.AllowBinarySpecifier) != 0 || // TODO https://github.com/dotnet/runtime/issues/83619: Remove once BigInteger supports binary parsing
                     !BigInteger.TryParse((string)objs[0], (NumberStyles)objs[1], null, out BigInteger bi) ||
                     (bi >= Int128.MinValue && bi <= Int128.MaxValue)))
                {
                    continue;
                }
                yield return objs;
            }
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            Int128 result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(Int128.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => Int128.Parse(value));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => Int128.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(Int128.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => Int128.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => Int128.Parse(value, provider));
            }

            // Full overloads
            Assert.False(Int128.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => Int128.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.BinaryNumber | NumberStyles.AllowParentheses)]
        [InlineData(NumberStyles.HexNumber | NumberStyles.BinaryNumber)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00))]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style)
        {
            Int128 result = 0;
            AssertExtensions.Throws<ArgumentException>("style", () => Int128.TryParse("1", style, null, out result));
            Assert.Equal(default(Int128), result);

            AssertExtensions.Throws<ArgumentException>("style", () => Int128.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>("style", () => Int128.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "-170141183460469231731687303715884105728", 0, 39, NumberStyles.Integer, null, new Int128(0xF333_3333_3333_3333, 0x3333_3333_3333_3334) };
            yield return new object[] { "0170141183460469231731687303715884105727", 1, 39, NumberStyles.Integer, null, new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF) };
            yield return new object[] { "170141183460469231731687303715884105727", 0, 1, NumberStyles.Integer, null, 1 };
            yield return new object[] { "ABC", 0, 2, NumberStyles.HexNumber, null, (Int128)0xAB };
            yield return new object[] { "101010110101", 0, 8, NumberStyles.BinaryNumber, null, (Int128)0b10101011 };
            yield return new object[] { "1101010110101", 1, 8, NumberStyles.BinaryNumber, null, (Int128)0b10101011 };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, null, (Int128)123 };
            yield return new object[] { "$1,000", 0, 2, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (Int128)1 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, Int128 expected)
        {
            Int128 result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(Int128.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, Int128.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(Int128.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is not null)
            {
                Int128 result;

                // Default style and provider
                if ((style == NumberStyles.Integer) && (provider is null))
                {
                    Assert.False(Int128.TryParse(value.AsSpan(), out result));
                    Assert.Equal(0, result);
                }

                Assert.Throws(exceptionType, () => Int128.Parse(value.AsSpan(), style, provider));

                Assert.False(Int128.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(0, result);
            }
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Utf8Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, Int128 expected)
        {
            Int128 result;
            ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value, offset, count);

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(Int128.TryParse(valueUtf8, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, Int128.Parse(valueUtf8, style, provider));

            Assert.True(Int128.TryParse(valueUtf8, style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Utf8Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is not null)
            {
                Int128 result;
                ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value);

                // Default style and provider
                if ((style == NumberStyles.Integer) && (provider is null))
                {
                    Assert.False(Int128.TryParse(valueUtf8, out result));
                    Assert.Equal(0, result);
                }

                Exception e = Assert.Throws(exceptionType, () => Int128.Parse(Encoding.UTF8.GetBytes(value), style, provider));
                if (e is FormatException fe)
                {
                    Assert.Contains(value, fe.Message);
                }

                Assert.False(Int128.TryParse(valueUtf8, style, provider, out result));
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public static void Parse_Utf8Span_InvalidUtf8()
        {
            FormatException fe = Assert.Throws<FormatException>(() => Int128.Parse([0xA0]));
            Assert.DoesNotContain("A0", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadOnlySpan", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("\uFFFD", fe.Message, StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(Int128 i, string format, IFormatProvider provider, string expected) =>
            NumberFormatTestHelper.TryFormatNumberTest(i, format, provider, expected);

        [Fact]
        public static void TestNegativeNumberParsingWithHyphen()
        {
            // CLDR data for Swedish culture has negative sign U+2212. This test ensure parsing with the hyphen with such cultures will succeed.
            CultureInfo ci = CultureInfo.GetCultureInfo("sv-SE");
            Assert.Equal(-15868, Int128.Parse("-15868", NumberStyles.Number, ci));
        }

        [Fact]
        public static void Runtime75416()
        {
            Int128 a = (Int128.MaxValue - 10) * +100;
            Assert.Equal(a, -1100);

            Int128 b = (Int128.MaxValue - 10) * -100;
            Assert.Equal(b, +1100);
        }
    }
}
