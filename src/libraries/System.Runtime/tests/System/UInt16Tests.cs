// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class UInt16Tests
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
        [InlineData((ushort)234, (ushort)0, 1)]
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
        public static void Equals(ushort i1, object obj, bool expected)
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
                foreach (string defaultSpecifier in new[] { "G", "G\0", "\0N222", "\0", "" })
                {
                    yield return new object[] { (ushort)0, defaultSpecifier, defaultFormat, "0" };
                    yield return new object[] { (ushort)4567, defaultSpecifier, defaultFormat, "4567" };
                    yield return new object[] { ushort.MaxValue, defaultSpecifier, defaultFormat, "65535" };
                }

                yield return new object[] { (ushort)123, "D", defaultFormat, "123" };
                yield return new object[] { (ushort)123, "D99", defaultFormat, "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000123" };

                yield return new object[] { (ushort)0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { (ushort)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
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
            yield return new object[] { (ushort)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (ushort)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (ushort)123, "F", customFormat, "123~00" };
            yield return new object[] { (ushort)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString(ushort i, string format, IFormatProvider provider, string expected)
        {
            // Format should be case insensitive
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
            ushort i = 123;
            Assert.Throws<FormatException>(() => i.ToString("r")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("r", null)); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("R")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("R", null)); // Invalid format
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
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            ushort result = 0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => ushort.TryParse("1", style, null, out result));
            Assert.Equal(default(ushort), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => ushort.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => ushort.Parse("1", style, null));
        }
    }
}
