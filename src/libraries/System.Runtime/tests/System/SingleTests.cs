// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public static class SingleTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var f = new float();
            Assert.Equal(0, f);
        }

        [Fact]
        public static void Ctor_Value()
        {
            float f = 41;
            Assert.Equal(41, f);

            f = 41.3f;
            Assert.Equal(41.3f, f);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal((float)3.40282346638528859e+38, float.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal((float)-3.40282346638528859e+38, float.MinValue);
        }

        [Fact]
        public static void Epsilon()
        {
            Assert.Equal((float)1.4e-45, float.Epsilon);
        }

        [Theory]
        [InlineData(float.PositiveInfinity, true)]
        [InlineData(float.NegativeInfinity, true)]
        [InlineData(float.NaN, false)]
        [InlineData(0.0, false)]
        public static void IsInfinity(float f, bool expected)
        {
            Assert.Equal(expected, float.IsInfinity(f));
        }

        [Fact]
        public static void NaN()
        {
            Assert.Equal((float)0.0 / (float)0.0, float.NaN);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, false)]
        [InlineData(float.PositiveInfinity, false)]
        [InlineData(float.NaN, true)]
        [InlineData(0.0, false)]
        public static void IsNaN(float f, bool expected)
        {
            Assert.Equal(expected, float.IsNaN(f));
        }

        [Fact]
        public static void NegativeInfinity()
        {
            Assert.Equal((float)-1.0 / (float)0.0, float.NegativeInfinity);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, true)]
        [InlineData(float.PositiveInfinity, false)]
        [InlineData(float.NaN, false)]
        [InlineData(0.0, false)]
        public static void IsNegativeInfinity(float f, bool expected)
        {
            Assert.Equal(expected, float.IsNegativeInfinity(f));
        }

        [Fact]
        public static void PositiveInfinity()
        {
            Assert.Equal((float)1.0 / (float)0.0, float.PositiveInfinity);
        }

        [Theory]
        [InlineData(float.PositiveInfinity, true)]
        [InlineData(float.NegativeInfinity, false)]
        [InlineData(float.NaN, false)]
        [InlineData(0.0, false)]
        public static void IsPositiveInfinity(float f, bool expected)
        {
            Assert.Equal(expected, float.IsPositiveInfinity(f));
        }

        [Theory]
        [InlineData((float)234, (float)234, 0)]
        [InlineData((float)234, float.MinValue, 1)]
        [InlineData((float)234, (float)-123, 1)]
        [InlineData((float)234, (float)0, 1)]
        [InlineData((float)234, (float)123, 1)]
        [InlineData((float)234, (float)456, -1)]
        [InlineData((float)234, float.MaxValue, -1)]
        [InlineData((float)234, float.NaN, 1)]
        [InlineData(float.NaN, float.NaN, 0)]
        [InlineData(float.NaN, (float)0, -1)]
        [InlineData((float)234, null, 1)]
        public static void CompareTo(float f1, object value, int expected)
        {
            if (value is float)
            {
                float f2 = (float)value;
                Assert.Equal(expected, Math.Sign(f1.CompareTo(f2)));
                if (float.IsNaN(f1) || float.IsNaN(f2))
                {
                    Assert.False(f1 >= f2);
                    Assert.False(f1 > f2);
                    Assert.False(f1 <= f2);
                    Assert.False(f1 < f2);
                }
                else
                {
                    if (expected >= 0)
                    {
                        Assert.True(f1 >= f2);
                        Assert.False(f1 < f2);
                    }
                    if (expected > 0)
                    {
                        Assert.True(f1 > f2);
                        Assert.False(f1 <= f2);
                    }
                    if (expected <= 0)
                    {
                        Assert.True(f1 <= f2);
                        Assert.False(f1 > f2);
                    }
                    if (expected < 0)
                    {
                        Assert.True(f1 < f2);
                        Assert.False(f1 >= f2);
                    }
                }
            }
            IComparable comparable = f1;
            Assert.Equal(expected, Math.Sign(comparable.CompareTo(value)));
        }

        [Fact]
        public static void CompareTo_ObjectNotFloat_ThrowsArgumentException()
        {
            IComparable comparable = (float)234;
            Assert.Throws<ArgumentException>(null, () => comparable.CompareTo((double)234)); // Obj is not a float
            Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("234")); // Obj is not a float
        }

        [Theory]
        [InlineData((float)789, (float)789, true)]
        [InlineData((float)789, (float)-789, false)]
        [InlineData((float)789, (float)0, false)]
        [InlineData(float.NaN, float.NaN, true)]
        [InlineData((float)789, (double)789, false)]
        [InlineData((float)789, "789", false)]
        public static void Equals(float f1, object value, bool expected)
        {
            if (value is float)
            {
                float f2 = (float)value;
                Assert.Equal(expected, f1.Equals(f2));

                if (float.IsNaN(f1) && float.IsNaN(f2))
                {
                    Assert.Equal(!expected, f1 == f2);
                    Assert.Equal(expected, f1 != f2);
                }
                else
                {
                    Assert.Equal(expected, f1 == f2);
                    Assert.Equal(!expected, f1 != f2);
                }
                Assert.Equal(expected, f1.GetHashCode().Equals(f2.GetHashCode()));
            }
            Assert.Equal(expected, f1.Equals(value));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            var emptyFormat = NumberFormatInfo.CurrentInfo;
            yield return new object[] { float.MinValue, "G", emptyFormat, "-3.402823E+38" };
            yield return new object[] { (float)-4567, "G", emptyFormat, "-4567" };
            yield return new object[] { (float)-4567.89101, "G", emptyFormat, "-4567.891" };
            yield return new object[] { (float)0, "G", emptyFormat, "0" };
            yield return new object[] { (float)4567, "G", emptyFormat, "4567" };
            yield return new object[] { (float)4567.89101, "G", emptyFormat, "4567.891" };
            yield return new object[] { float.MaxValue, "G", emptyFormat, "3.402823E+38" };

            yield return new object[] { float.Epsilon, "G", emptyFormat, "1.401298E-45" };
            yield return new object[] { float.NaN, "G", emptyFormat, "NaN" };

            yield return new object[] { (float)2468, "N", emptyFormat, string.Format("{0:N}", 2468.00) };

            // Changing the negative pattern doesn't do anything without also passing in a format string
            var customNegativePattern = new NumberFormatInfo() { NumberNegativePattern = 0 };
            yield return new object[] { (float)-6310, "G", customNegativePattern, "-6310" };

            var customNegativeSignDecimalGroupSeparator = new NumberFormatInfo()
            {
                NegativeSign = "#",
                NumberDecimalSeparator = "~",
                NumberGroupSeparator = "*"
            };
            yield return new object[] { (float)-2468, "N", customNegativeSignDecimalGroupSeparator, "#2*468~00" };
            yield return new object[] { (float)2468, "N", customNegativeSignDecimalGroupSeparator, "2*468~00" };

            var customNegativeSignGroupSeparatorNegativePattern = new NumberFormatInfo()
            {
                NegativeSign = "xx", // Set to trash to make sure it doesn't show up
                NumberGroupSeparator = "*",
                NumberNegativePattern = 0
            };
            yield return new object[] { (float)-2468, "N", customNegativeSignGroupSeparatorNegativePattern, "(2*468.00)" };

            var invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { float.Epsilon, "G", invariantFormat, "1.401298E-45" };
            yield return new object[] { float.NaN, "G", invariantFormat, "NaN" };
            yield return new object[] { float.PositiveInfinity, "G", invariantFormat, "Infinity" };
            yield return new object[] { float.NegativeInfinity, "G", invariantFormat, "-Infinity" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString(float f, string format, IFormatProvider provider, string expected)
        {
            bool isDefaultProvider = (provider == null || provider == NumberFormatInfo.CurrentInfo);
            if (string.IsNullOrEmpty(format) || format.ToUpperInvariant() == "G")
            {
                if (isDefaultProvider)
                {
                    Assert.Equal(expected, f.ToString());
                    Assert.Equal(expected, f.ToString((IFormatProvider)null));
                }
                Assert.Equal(expected, f.ToString(provider));
            }
            if (isDefaultProvider)
            {
                Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant())); // If format is upper case, then exponents are printed in upper case
                Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant())); // If format is lower case, then exponents are printed in lower case
                Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant(), null));
                Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant(), null));
            }
            Assert.Equal(expected.Replace('e', 'E'), f.ToString(format.ToUpperInvariant(), provider));
            Assert.Equal(expected.Replace('E', 'e'), f.ToString(format.ToLowerInvariant(), provider));
        }

        [Fact]
        public static void ToString_InvalidFormat_ThrowsFormatException()
        {
            float f = 123;
            Assert.Throws<FormatException>(() => f.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => f.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            // Defaults: AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowDecimalPoint | AllowExponent | AllowThousands
            NumberFormatInfo nullFormat = null;
            NumberStyles defaultStyle = NumberStyles.Float;

            var emptyFormat = new NumberFormatInfo();

            var dollarSignCommaSeparatorFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                CurrencyGroupSeparator = ","
            };

            var decimalSeparatorFormat = new NumberFormatInfo()
            {
                NumberDecimalSeparator = "."
            };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;

            yield return new object[] { "-123", defaultStyle, nullFormat, (float)-123 };
            yield return new object[] { "0", defaultStyle, nullFormat, (float)0 };
            yield return new object[] { "123", defaultStyle, nullFormat, (float)123 };
            yield return new object[] { "  123  ", defaultStyle, nullFormat, (float)123 };
            yield return new object[] { "567.89", defaultStyle, nullFormat, (float)567.89 };
            yield return new object[] { "-567.89", defaultStyle, nullFormat, (float)-567.89 };
            yield return new object[] { "1E23", defaultStyle, nullFormat, (float)1E23 };

            yield return new object[] { "123.1", NumberStyles.AllowDecimalPoint, nullFormat, (float)123.1 };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.AllowThousands, nullFormat, (float)1000 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, (float)123 };
            yield return new object[] { "123.567", NumberStyles.Any, emptyFormat, 123.567 };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, (float)123 };
            yield return new object[] { "$1,000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, (float)1000 };
            yield return new object[] { "$1000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, (float)1000 };
            yield return new object[] { "123.123", NumberStyles.Float, decimalSeparatorFormat, (float)123.123 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, decimalSeparatorFormat, (float)-123 };

            yield return new object[] { "NaN", NumberStyles.Any, invariantFormat, float.NaN };
            yield return new object[] { "Infinity", NumberStyles.Any, invariantFormat, float.PositiveInfinity };
            yield return new object[] { "-Infinity", NumberStyles.Any, invariantFormat, float.NegativeInfinity };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, float expected)
        {
            float f;
            // If no style is specified, use the (String) or (String, IFormatProvider) overload
            if (style == NumberStyles.Float)
            {
                Assert.True(float.TryParse(value, out f));
                Assert.Equal(expected, f);

                Assert.Equal(expected, float.Parse(value));

                // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
                if (provider != null)
                {
                    Assert.Equal(expected, float.Parse(value, provider));
                }
            }

            // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
            Assert.True(float.TryParse(value, style, provider ?? new NumberFormatInfo(), out f));
            Assert.Equal(expected, f);

            // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
            if (provider == null)
            {
                Assert.Equal(expected, float.Parse(value, style));
            }
            Assert.Equal(expected, float.Parse(value, style, provider ?? new NumberFormatInfo()));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberFormatInfo nullFormat = null;
            NumberStyles defaultStyle = NumberStyles.Float;

            var dollarSignDecimalSeparatorFormat = new NumberFormatInfo();
            dollarSignDecimalSeparatorFormat.CurrencySymbol = "$";
            dollarSignDecimalSeparatorFormat.NumberDecimalSeparator = ".";

            yield return new object[] { null, defaultStyle, nullFormat, typeof(ArgumentNullException) };
            yield return new object[] { "", defaultStyle, nullFormat, typeof(FormatException) };
            yield return new object[] { " ", defaultStyle, nullFormat, typeof(FormatException) };
            yield return new object[] { "Garbage", defaultStyle, nullFormat, typeof(FormatException) };

            yield return new object[] { "ab", defaultStyle, nullFormat, typeof(FormatException) }; // Hex value
            yield return new object[] { "(123)", defaultStyle, nullFormat, typeof(FormatException) }; // Parentheses
            yield return new object[] { 100.ToString("C0"), defaultStyle, nullFormat, typeof(FormatException) }; // Currency

            yield return new object[] { "123.456", NumberStyles.Integer, nullFormat, typeof(FormatException) }; // Decimal
            yield return new object[] { "  123.456", NumberStyles.None, nullFormat, typeof(FormatException) }; // Leading space
            yield return new object[] { "123.456   ", NumberStyles.None, nullFormat, typeof(FormatException) }; // Leading space
            yield return new object[] { "1E23", NumberStyles.None, nullFormat, typeof(FormatException) }; // Exponent

            yield return new object[] { "ab", NumberStyles.None, nullFormat, typeof(FormatException) }; // Negative hex value
            yield return new object[] { "  123  ", NumberStyles.None, nullFormat, typeof(FormatException) }; // Trailing and leading whitespace
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            float f;
            // If no style is specified, use the (String) or (String, IFormatProvider) overload
            if (style == NumberStyles.Float)
            {
                Assert.False(float.TryParse(value, out f));
                Assert.Equal(default(float), f);

                Assert.Throws(exceptionType, () => float.Parse(value));

                // If a format provider is specified, but the style is the default, use the (String, IFormatProvider) overload
                if (provider != null)
                {
                    Assert.Throws(exceptionType, () => float.Parse(value, provider));
                }
            }

            // If a format provider isn't specified, test the default one, using a new instance of NumberFormatInfo
            Assert.False(float.TryParse(value, style, provider ?? new NumberFormatInfo(), out f));
            Assert.Equal(default(float), f);

            // If a format provider isn't specified, test the default one, using the (String, NumberStyles) overload
            if (provider == null)
            {
                Assert.Throws(exceptionType, () => float.Parse(value, style));
            }
            Assert.Throws(exceptionType, () => float.Parse(value, style, provider ?? new NumberFormatInfo()));
        }
    }
}
