// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class UInt32Tests
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
        [InlineData((uint)234, (uint)0, 1)]
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
        public static void Equals(uint i1, object obj, bool expected)
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
                yield return new object[] { (uint)0, "G", defaultFormat, "0" };
                yield return new object[] { (uint)4567, "G", defaultFormat, "4567" };
                yield return new object[] { uint.MaxValue, "G", defaultFormat, "4294967295" };

                yield return new object[] { (uint)4567, "D", defaultFormat, "4567" };
                yield return new object[] { (uint)4567, "D18", defaultFormat, "000000000000004567" };

                yield return new object[] { (uint)0x2468, "x", defaultFormat, "2468" };
                yield return new object[] { (uint)2468, "N", defaultFormat, string.Format("{0:N}", 2468.00) };
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
            yield return new object[] { (uint)2468, "N", customFormat, "2*468~00" };
            yield return new object[] { (uint)123, "E", customFormat, "1~230000E&002" };
            yield return new object[] { (uint)123, "F", customFormat, "123~00" };
            yield return new object[] { (uint)123, "P", customFormat, "12,300.00000 @" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString(uint i, string format, IFormatProvider provider, string expected)
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
            uint i = 123;
            Assert.Throws<FormatException>(() => i.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => i.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Integer;
            NumberFormatInfo emptyFormat = new NumberFormatInfo();

            NumberFormatInfo customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";

            yield return new object[] { "0", defaultStyle, null, (uint)0 };
            yield return new object[] { "123", defaultStyle, null, (uint)123 };
            yield return new object[] { "+123", defaultStyle, null, (uint)123 };
            yield return new object[] { "  123  ", defaultStyle, null, (uint)123 };
            yield return new object[] { "4294967295", defaultStyle, null, 4294967295 };

            yield return new object[] { "12", NumberStyles.HexNumber, null, (uint)0x12 };
            yield return new object[] { "1000", NumberStyles.AllowThousands, null, (uint)1000 };

            yield return new object[] { "123", defaultStyle, emptyFormat, (uint)123 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, (uint)123 };
            yield return new object[] { "12", NumberStyles.HexNumber, emptyFormat, (uint)0x12 };
            yield return new object[] { "abc", NumberStyles.HexNumber, emptyFormat, (uint)0xabc };
            yield return new object[] { "ABC", NumberStyles.HexNumber, emptyFormat, (uint)0xabc };
            yield return new object[] { "$1,000", NumberStyles.Currency, customFormat, (uint)1000 };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, uint expected)
        {
            uint result;
            // If no style is specified, use the (String) or (String, IFormatProvider) overload
            if (style == NumberStyles.Integer)
            {
                Assert.True(uint.TryParse(value, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, uint.Parse(value));

                // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
                if (provider != null)
                {
                    Assert.Equal(expected, uint.Parse(value, provider));
                }
            }

            // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
            Assert.True(uint.TryParse(value, style, provider ?? new NumberFormatInfo(), out result));
            Assert.Equal(expected, result);

            // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
            if (provider == null)
            {
                Assert.Equal(expected, uint.Parse(value, style));
            }
            Assert.Equal(expected, uint.Parse(value, style, provider ?? new NumberFormatInfo()));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Integer;

            NumberFormatInfo customFormat = new NumberFormatInfo();
            customFormat.CurrencySymbol = "$";
            customFormat.NumberDecimalSeparator = ".";

            yield return new object[] { null, defaultStyle, null, typeof(ArgumentNullException) };

            yield return new object[] { "", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { " \t \n \r ", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "Garbage", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "abc", defaultStyle, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "1E23", defaultStyle, null, typeof(FormatException) }; // Exponent
            yield return new object[] { "(123)", defaultStyle, null, typeof(FormatException) }; // Parentheses
            yield return new object[] { 100.ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency
            yield return new object[] { 1000.ToString("N0"), defaultStyle, null, typeof(FormatException) }; // Thousands
            yield return new object[] { 678.90.ToString("F2"), defaultStyle, null, typeof(FormatException) }; // Decimal
            yield return new object[] { "+-123", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "-+123", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "+abc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "-abc", NumberStyles.HexNumber, null, typeof(FormatException) };

            yield return new object[] { "- 123", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "+ 123", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "abc", NumberStyles.None, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) }; // Trailing and leading whitespace

            yield return new object[] { "678.90", defaultStyle, customFormat, typeof(FormatException) }; // Decimal

            yield return new object[] { "-1", defaultStyle, null, typeof(OverflowException) }; // < min value
            yield return new object[] { "4294967296", defaultStyle, null, typeof(OverflowException) }; // > max value
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, null, typeof(OverflowException) }; // Parentheses = negative
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            uint result;
            // If no style is specified, use the (String) or (String, IFormatProvider) overload
            if (style == NumberStyles.Integer)
            {
                Assert.False(uint.TryParse(value, out result));
                Assert.Equal(default(uint), result);

                Assert.Throws(exceptionType, () => uint.Parse(value));

                // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
                if (provider != null)
                {
                    Assert.Throws(exceptionType, () => uint.Parse(value, provider));
                }
            }

            // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
            Assert.False(uint.TryParse(value, style, provider ?? new NumberFormatInfo(), out result));
            Assert.Equal(default(uint), result);

            // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => uint.Parse(value, style));
            }
            Assert.Throws(exceptionType, () => uint.Parse(value, style, provider ?? new NumberFormatInfo()));
        }

        [Theory]
        [InlineData(NumberStyles.HexNumber | NumberStyles.AllowParentheses, null)]
        [InlineData(unchecked((NumberStyles)0xFFFFFC00), "style")]
        public static void TryParse_InvalidNumberStyle_ThrowsArgumentException(NumberStyles style, string paramName)
        {
            uint result = 0;
            AssertExtensions.Throws<ArgumentException>(paramName, () => uint.TryParse("1", style, null, out result));
            Assert.Equal(default(uint), result);

            AssertExtensions.Throws<ArgumentException>(paramName, () => uint.Parse("1", style));
            AssertExtensions.Throws<ArgumentException>(paramName, () => uint.Parse("1", style, null));
        }
    }
}
