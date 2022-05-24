// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
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
                    yield return new object[] { Int128.MaxValue, defaultSpecifier, defaultFormat, "170141183460469231731687303715884105727" };
                }

                yield return new object[] { (Int128)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (Int128)4567, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (Int128)4567, "D99\09", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (Int128)(-4567), "D99", defaultFormat, "-000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };

                yield return new object[] { (Int128)0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { (Int128)(-0x2468), "x", defaultFormat, "ffffffffffffffffffffffffffffdb98" };
                yield return new object[] { (Int128)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
            }

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { (Int128)32, "C100", invariantFormat, "¤32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (Int128)32, "P100", invariantFormat, "3,200.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { (Int128)32, "D100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000032" };
            yield return new object[] { (Int128)32, "E100", invariantFormat, "3.2000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { (Int128)32, "F100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (Int128)32, "N100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (Int128)32, "X100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020" };

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
                bool unsigned = (((NumberStyles)objs[1]) & NumberStyles.HexNumber) == NumberStyles.HexNumber;
                yield return new object[] { objs[0], objs[1], objs[2], unsigned ? (Int128)(ulong)(long)objs[3] : (Int128)(long)objs[3] };
            }

            // All lengths decimal
            foreach (bool neg in new[] { false, true })
            {
                string s = neg ? "-" : "";
                Int128 result = 0;
                for (int i = 1; i <= 19; i++)
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
                for (int i = 1; i <= 16; i++)
                {
                    result = (result * 16) + (i % 16);
                    s += (i % 16).ToString("X");
                    yield return new object[] { s, NumberStyles.HexNumber, null, result };
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
                    (!BigInteger.TryParse((string)objs[0], out BigInteger bi) || (bi >= Int128.MinValue && bi <= Int128.MaxValue)))
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
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            Int128 result = 0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => Int128.TryParse("1", style, null, out result));
            Assert.Equal(default(Int128), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => Int128.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => Int128.Parse("1", style, null));
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
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(Int128 i, string format, IFormatProvider provider, string expected)
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

            if (format is not null)
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

        [Fact]
        public static void TestNegativeNumberParsingWithHyphen()
        {
            // CLDR data for Swedish culture has negative sign U+2212. This test ensure parsing with the hyphen with such cultures will succeed.
            CultureInfo ci = CultureInfo.GetCultureInfo("sv-SE");
            Assert.Equal(-15868, Int128.Parse("-15868", NumberStyles.Number, ci));
        }
    }
}
