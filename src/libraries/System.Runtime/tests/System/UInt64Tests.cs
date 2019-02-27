// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Xunit;

namespace System.Tests
{
    public partial class UInt64Tests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new ulong();
            Assert.Equal((ulong)0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            ulong i = 41;
            Assert.Equal((ulong)41, i);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(0xFFFFFFFFFFFFFFFF, ulong.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal((ulong)0, ulong.MinValue);
        }

        [Theory]
        [InlineData((ulong)234, (ulong)234, 0)]
        [InlineData((ulong)234, ulong.MinValue, 1)]
        [InlineData((ulong)234, (ulong)0, 1)]
        [InlineData((ulong)234, (ulong)123, 1)]
        [InlineData((ulong)234, (ulong)456, -1)]
        [InlineData((ulong)234, ulong.MaxValue, -1)]
        [InlineData((ulong)234, null, 1)]
        public void CompareTo_Other_ReturnsExpected(ulong i, object value, int expected)
        {
            if (value is ulong ulongValue)
            {
                Assert.Equal(expected, Math.Sign(i.CompareTo(ulongValue)));
            }

            Assert.Equal(expected, Math.Sign(i.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData(234)]
        public void CompareTo_ObjectNotUlong_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((ulong)123).CompareTo(value));
        }

        [Theory]
        [InlineData((ulong)789, (ulong)789, true)]
        [InlineData((ulong)788, (ulong)0, false)]
        [InlineData((ulong)0, (ulong)0, true)]
        [InlineData((ulong)789, null, false)]
        [InlineData((ulong)789, "789", false)]
        [InlineData((ulong)789, 789, false)]
        public static void Equals(ulong i1, object obj, bool expected)
        {
            if (obj is ulong i2)
            {
                Assert.Equal(expected, i1.Equals(i2));
                Assert.Equal(expected, i1.GetHashCode().Equals(i2.GetHashCode()));
                Assert.Equal((int)i1, i1.GetHashCode());
            }
            Assert.Equal(expected, i1.Equals(obj));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsUInt64()
        {
            Assert.Equal(TypeCode.UInt64, ((ulong)1).GetTypeCode());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            foreach (NumberFormatInfo defaultFormat in new[] { null, NumberFormatInfo.CurrentInfo })
            {
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "" })
                {
                    yield return new object[] { (ulong)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (ulong)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { ulong.MaxValue, defaultSpecifier, defaultFormat, "18446744073709551615" };
                }

                yield return new object[] { (ulong)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (ulong)4567, "D18", defaultFormat, "000000000000004567" };

                yield return new object[] { (ulong)0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { (ulong)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
            }

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
            yield return new object[] { (ulong)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (ulong)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (ulong)123, "F", customFormat, "123~00" };
            yield return new object[] { (ulong)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString(ulong i, string format, IFormatProvider provider, string expected)
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
            ulong i = 123;
            Assert.Throws<FormatException>(() => i.ToString("r")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("r", null)); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("R")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("R", null)); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            // Reuse all Int64 test data that's relevant
            foreach (object[] objs in Int64Tests.Parse_Valid_TestData())
            {
                if ((long)objs[3] < 0) continue;
                yield return new object[] { objs[0], objs[1], objs[2], (ulong)(long)objs[3] };
            }

            // All lengths decimal
            {
                string s = "";
                ulong result = 0;
                for (int i = 1; i <= 20; i++)
                {
                    result = (result * 10) + (ulong)(i % 10);
                    s += (i % 10).ToString();
                    yield return new object[] { s, NumberStyles.Integer, null, result };
                }
            }

            // All lengths hexadecimal
            {
                string s = "";
                ulong result = 0;
                for (int i = 1; i <= 16; i++)
                {
                    result = (result * 16) + (ulong)(i % 16);
                    s += (i % 16).ToString("X");
                    yield return new object[] { s, NumberStyles.HexNumber, null, result };
                }
            }

            // And test boundary conditions for UInt64
            yield return new object[] { "18446744073709551615", NumberStyles.Integer, null, ulong.MaxValue };
            yield return new object[] { "+18446744073709551615", NumberStyles.Integer, null, ulong.MaxValue };
            yield return new object[] { "    +18446744073709551615  ", NumberStyles.Integer, null, ulong.MaxValue };
            yield return new object[] { "FFFFFFFFFFFFFFFF", NumberStyles.HexNumber, null, ulong.MaxValue };
            yield return new object[] { "   FFFFFFFFFFFFFFFF   ", NumberStyles.HexNumber, null, ulong.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid(string value, NumberStyles style, IFormatProvider provider, ulong expected)
        {
            ulong result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.True(ulong.TryParse(value, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ulong.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Equal(expected, ulong.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.True(ulong.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ulong.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ulong.Parse(value, provider));
            }

            // Full overloads
            Assert.True(ulong.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, ulong.Parse(value, style, provider));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // Reuse all long test data, except for those that wouldn't overflow ulong.
            foreach (object[] objs in Int64Tests.Parse_Invalid_TestData())
            {
                if ((Type)objs[3] == typeof(OverflowException) &&
                    (!BigInteger.TryParse((string)objs[0], out BigInteger bi) || bi <= ulong.MaxValue))
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
            yield return new object[] { "18446744073709551616", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "10000000000000000", NumberStyles.HexNumber, null, typeof(OverflowException) };
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            ulong result;

            // Default style and provider
            if (style == NumberStyles.Integer && provider == null)
            {
                Assert.False(ulong.TryParse(value, out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => ulong.Parse(value));
            }

            // Default provider
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => ulong.Parse(value, style));

                // Substitute default NumberFormatInfo
                Assert.False(ulong.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default, result);
                Assert.Throws(exceptionType, () => ulong.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ulong.Parse(value, provider));
            }

            // Full overloads
            Assert.False(ulong.TryParse(value, style, provider, out result));
            Assert.Equal(default, result);
            Assert.Throws(exceptionType, () => ulong.Parse(value, style, provider));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            ulong result = 0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => ulong.TryParse("1", style, null, out result));
            Assert.Equal(default(ulong), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => ulong.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => ulong.Parse("1", style, null));
        }
    }
}
