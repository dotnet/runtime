// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Xunit;

namespace System.Tests
{
    public class Int64Tests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new long();
            Assert.Equal(0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            long i = 41;
            Assert.Equal(41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0x7FFFFFFFFFFFFFFF, long.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(unchecked((long)0x8000000000000000), long.MinValue);
        }

        [Theory]
        [InlineData((long)234, (long)234, 0)]
        [InlineData((long)234, long.MinValue, 1)]
        [InlineData((long)-234, long.MinValue, 1)]
        [InlineData((long)long.MinValue, long.MinValue, 0)]
        [InlineData((long)234, (long)-123, 1)]
        [InlineData((long)234, (long)0, 1)]
        [InlineData((long)234, (long)123, 1)]
        [InlineData((long)234, (long)456, -1)]
        [InlineData((long)234, long.MaxValue, -1)]
        [InlineData((long)-234, long.MaxValue, -1)]
        [InlineData(long.MaxValue, long.MaxValue, 0)]
        [InlineData((long)-234, (long)-234, 0)]
        [InlineData((long)-234, (long)234, -1)]
        [InlineData((long)-234, (long)-432, 1)]
        [InlineData((long)234, null, 1)]
        public void CompareTo_Other_ReturnsExpected(long i, object value, int expected)
        {
            if (value is long longValue)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(longValue)));
                Assert.Equal(-expected, Math.Sign(longValue.CompareTo(i)));
            }

            Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData(234)]
        public void CompareTo_ObjectNotLong_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((long)123).CompareTo(value));
        }

        [Theory]
        [InlineData((long)789, (long)789, true)]
        [InlineData((long)789, (long)-789, false)]
        [InlineData((long)789, (long)0, false)]
        [InlineData((long)0, (long)0, true)]
        [InlineData((long)-789, (long)-789, true)]
        [InlineData((long)-789, (long)789, false)]
        [InlineData((long)789, null, false)]
        [InlineData((long)789, "789", false)]
        [InlineData((long)789, 789, false)]
        public static void EqualsTest(long i1, object obj, bool expected)
        {
            if (obj is long)
            {
                long i2 = (long)obj;
                Assert.Equal(expected, i1.Equals(i2));
                Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
            }
            Assert.Equal(expected, i1.Equals(obj));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsInt64()
        {
            Assert.Equal(TypeCode.Int64, ((long)1).GetTypeCode());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "", "R" })
                {
                    yield return new object[] { long.MinValue, defaultSpecifier, defaultFormat, "-9223372036854775808" };
                    yield return new object[] { (long)-4567, defaultSpecifier, defaultFormat, "-4567" };
                    yield return new object[] { (long)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (long)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { long.MaxValue, defaultSpecifier, defaultFormat, "9223372036854775807" };
                }

                yield return new object[] { (long)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (long)4567, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (long)4567, "D99\09", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };
                yield return new object[] { (long)-4567, "D99", defaultFormat, "-000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004567" };

                yield return new object[] { (long)0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { (long)-0x2468, "x", defaultFormat, "ffffffffffffdb98" };
                yield return new object[] { (long)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };


            }

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { (long)32, "C100", invariantFormat, "¤32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (long)32, "P100", invariantFormat, "3,200.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { (long)32, "D100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000032" };
            yield return new object[] { (long)32, "E100", invariantFormat, "3.2000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { (long)32, "F100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (long)32, "N100", invariantFormat, "32.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { (long)32, "X100", invariantFormat, "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020" };

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
            yield return new object[] { (long)-2468, "N", customFormat, "#2*468~00" };
            yield return new object[] { (long)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (long)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (long)123, "F", customFormat, "123~00" };
            yield return new object[] { (long)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(long i, string format, IFormatProvider provider, string expected)
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
            long i = 123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            // Reuse all Int32 test data
            foreach (object[] objs in Int32Tests.Parse_Valid_TestData())
            {
                bool unsigned = (((NumberStyles)objs[1]) & NumberStyles.HexNumber) == NumberStyles.HexNumber;
                yield return new object[] { objs[0], objs[1], objs[2], unsigned ? (long)(uint)(int)objs[3] : (long)(int)objs[3] };
            }

            // All lengths decimal
            foreach (bool neg in new[] { false, true })
            {
                string s = neg ? "-" : "";
                long result = 0;
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
                long result = 0;
                for (int i = 1; i <= 16; i++)
                {
                    result = (result * 16) + (i % 16);
                    s += (i % 16).ToString("X");
                    yield return new object[] { s, NumberStyles.HexNumber, null, result };
                }
            }

            // And test boundary conditions for Int64
            yield return new object[] { "-9223372036854775808", NumberStyles.Integer, null, long.MinValue };
            yield return new object[] { "9223372036854775807", NumberStyles.Integer, null, long.MaxValue };
            yield return new object[] { "   -9223372036854775808   ", NumberStyles.Integer, null, long.MinValue };
            yield return new object[] { "   +9223372036854775807   ", NumberStyles.Integer, null, long.MaxValue };
            yield return new object[] { "7FFFFFFFFFFFFFFF", NumberStyles.HexNumber, null, long.MaxValue };
            yield return new object[] { "8000000000000000", NumberStyles.HexNumber, null, long.MinValue };
            yield return new object[] { "FFFFFFFFFFFFFFFF", NumberStyles.HexNumber, null, -1L };
            yield return new object[] { "   FFFFFFFFFFFFFFFF  ", NumberStyles.HexNumber, null, -1L };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, long expected)
        {
            long result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(long.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, long.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, long.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(long.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, long.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, long.Parse(value, provider));
            }

            // Full overloads
            Assert.True(long.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, long.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // Reuse all int test data, except for those that wouldn't overflow long.
            foreach (object[] objs in Int32Tests.Parse_Invalid_TestData())
            {
                if ((Type)objs[3] == typeof(OverflowException) &&
                    (!BigInteger.TryParse((string)objs[0], out BigInteger bi) || (bi >= long.MinValue && bi <= long.MaxValue)))
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
            long result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(long.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => long.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => long.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(long.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => long.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => long.Parse(value, provider));
            }

            // Full overloads
            Assert.False(long.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => long.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            long result = 0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => long.TryParse("1", style, null, out result));
            Assert.Equal(default(long), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => long.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => long.Parse("1", style, null));
        }

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            yield return new object[] { "-9223372036854775808", 0, 19, NumberStyles.Integer, null, -922337203685477580 };
            yield return new object[] { "09223372036854775807", 1, 19, NumberStyles.Integer, null, 9223372036854775807 };
            yield return new object[] { "9223372036854775807", 0, 1, NumberStyles.Integer, null, 9 };
            yield return new object[] { "ABC", 0, 2, NumberStyles.HexNumber, null, (long)0xAB };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, null, (long)123 };
            yield return new object[] { "$1,000", 0, 2, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$" }, (long)1 };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, long expected)
        {
            long result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(long.TryParse(value.AsSpan(offset, count), out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, long.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(long.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                long result;

                // Default style and provider
                if (style == NumberStyles.Integer && provider == null)
                {
                    Assert.False(long.TryParse(value.AsSpan(), out result));
                    Assert.Equal(0, result);
                }

                Assert.Throws(exceptionType, () => long.Parse(value.AsSpan(), style, provider));

                Assert.False(long.TryParse(value.AsSpan(), style, provider, out result));
                Assert.Equal(0, result);
            }
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat(long i, string format, IFormatProvider provider, string expected)
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

        [Fact]
        public static void TestNegativeNumberParsingWithHyphen()
        {
            // CLDR data for Swedish culture has negative sign U+2212. This test ensure parsing with the hyphen with such cultures will succeed.
            CultureInfo ci = CultureInfo.GetCultureInfo("sv-SE");
            Assert.Equal(-15868, long.Parse("-15868", NumberStyles.Number, ci));
        }
    }
}
