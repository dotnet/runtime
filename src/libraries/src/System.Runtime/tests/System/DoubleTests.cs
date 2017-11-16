// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class DoubleTests : RemoteExecutorTestBase
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var i = new double();
            Assert.Equal(0, i);
        }

        [Fact]
        public static void Ctor_Value()
        {
            double d = 41;
            Assert.Equal(41, d);

            d = 41.3;
            Assert.Equal(41.3, d);
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(1.7976931348623157E+308, double.MaxValue);
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(-1.7976931348623157E+308, double.MinValue);
        }

        [Fact]
        public static void Epsilon()
        {
            Assert.Equal(4.9406564584124654E-324, double.Epsilon);
        }

        [Theory]
        [InlineData(double.NegativeInfinity, true)]     // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, false)]     // Max Negative Subnormal
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(4.94065645841247E-324, false)]      // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, true)]     // Positive Infinity
        public static void IsInfinity(double d, bool expected)
        {
            Assert.Equal(expected, double.IsInfinity(d));
        }

        [Fact]
        public static void NaN()
        {
            Assert.Equal(0.0 / 0.0, double.NaN);
        }

        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, false)]     // Max Negative Subnormal
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, true)]                  // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(4.94065645841247E-324, false)]      // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsNaN(double d, bool expected)
        {
            Assert.Equal(expected, double.IsNaN(d));
        }

        [Fact]
        public static void NegativeInfinity()
        {
            Assert.Equal(-1.0 / 0.0, double.NegativeInfinity);
        }

        [Theory]
        [InlineData(double.NegativeInfinity, true)]     // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, false)]     // Max Negative Subnormal
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(4.94065645841247E-324, false)]      // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsNegativeInfinity(double d, bool expected)
        {
            Assert.Equal(expected, double.IsNegativeInfinity(d));
        }

        [Fact]
        public static void PositiveInfinity()
        {
            Assert.Equal(1.0 / 0.0, double.PositiveInfinity);
        }

        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, false)]     // Max Negative Subnormal
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(4.94065645841247E-324, false)]      // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, true)]     // Positive Infinity
        public static void IsPositiveInfinity(double d, bool expected)
        {
            Assert.Equal(expected, double.IsPositiveInfinity(d));
        }

        [Theory]
        [InlineData((double)234, (double)234, 0)]
        [InlineData((double)234, double.MinValue, 1)]
        [InlineData((double)234, (double)-123, 1)]
        [InlineData((double)234, (double)0, 1)]
        [InlineData((double)234, (double)123, 1)]
        [InlineData((double)234, (double)456, -1)]
        [InlineData((double)234, double.MaxValue, -1)]
        [InlineData((double)234, double.NaN, 1)]
        [InlineData(double.NaN, double.NaN, 0)]
        [InlineData(double.NaN, (double)0, -1)]
        [InlineData((double)234, null, 1)]
        public void CompareTo_Other_ReturnsExpected(double d1, object value, int expected)
        {
            if (value is double d2)
            {
                Assert.Equal(expected, Math.Sign(d1.CompareTo(d2)));
                if (double.IsNaN(d1) || double.IsNaN(d2))
                {
                    Assert.False(d1 >= d2);
                    Assert.False(d1 > d2);
                    Assert.False(d1 <= d2);
                    Assert.False(d1 < d2);
                }
                else
                {
                    if (expected >= 0)
                    {
                        Assert.True(d1 >= d2);
                        Assert.False(d1 < d2);
                    }
                    if (expected > 0)
                    {
                        Assert.True(d1 > d2);
                        Assert.False(d1 <= d2);
                    }
                    if (expected <= 0)
                    {
                        Assert.True(d1 <= d2);
                        Assert.False(d1 > d2);
                    }
                    if (expected < 0)
                    {
                        Assert.True(d1 < d2);
                        Assert.False(d1 >= d2);
                    }
                }
            }

            Assert.Equal(expected, Math.Sign(d1.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData((float)234)]
        public void CompareTo_ObjectNotDouble_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((double)123).CompareTo(value));
        }

        [Theory]
        [InlineData((double)789, (double)789, true)]
        [InlineData((double)789, (double)-789, false)]
        [InlineData((double)789, (double)0, false)]
        [InlineData(double.NaN, double.NaN, true)]
        [InlineData((double)789, (float)789, false)]
        [InlineData((double)789, "789", false)]
        public static void Equals(double d1, object value, bool expected)
        {
            if (value is double d2)
            {
                Assert.Equal(expected, d1.Equals(d2));

                if (double.IsNaN(d1) && double.IsNaN(d2))
                {
                    Assert.Equal(!expected, d1 == d2);
                    Assert.Equal(expected, d1 != d2);
                }
                else
                {
                    Assert.Equal(expected, d1 == d2);
                    Assert.Equal(!expected, d1 != d2);
                }
                Assert.Equal(expected, d1.GetHashCode().Equals(d2.GetHashCode()));
            }
            Assert.Equal(expected, d1.Equals(value));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsDouble()
        {
            Assert.Equal(TypeCode.Double, 0.0.GetTypeCode());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { double.MinValue, "G", null, "-1.79769313486232E+308" };
            yield return new object[] { (double)-4567, "G", null, "-4567" };
            yield return new object[] { -4567.89101, "G", null, "-4567.89101" };
            yield return new object[] { (double)0, "G", null, "0" };
            yield return new object[] { (double)4567, "G", null, "4567" };
            yield return new object[] { 4567.89101, "G", null, "4567.89101" };
            yield return new object[] { double.MaxValue, "G", null, "1.79769313486232E+308" };

            yield return new object[] { double.Epsilon, "G", null, "4.94065645841247E-324" };
            yield return new object[] { double.NaN, "G", null, "NaN" };

            yield return new object[] { (double)2468, "N", null, "2,468.00" };

            // Changing the negative pattern doesn't do anything without also passing in a format string
            var customNegativePattern = new NumberFormatInfo() { NumberNegativePattern = 0 };
            yield return new object[] { (double)-6310, "G", customNegativePattern, "-6310" };

            var customNegativeSignDecimalGroupSeparator = new NumberFormatInfo()
            {
                NegativeSign = "#",
                NumberDecimalSeparator = "~",
                NumberGroupSeparator = "*"
            };
            yield return new object[] { (double)-2468, "N", customNegativeSignDecimalGroupSeparator, "#2*468~00" };
            yield return new object[] { (double)2468, "N", customNegativeSignDecimalGroupSeparator, "2*468~00" };

            var customNegativeSignGroupSeparatorNegativePattern = new NumberFormatInfo()
            {
                NegativeSign = "xx", // Set to trash to make sure it doesn't show up
                NumberGroupSeparator = "*",
                NumberNegativePattern = 0,
            };
            yield return new object[] { (double)-2468, "N", customNegativeSignGroupSeparatorNegativePattern, "(2*468.00)" };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { double.Epsilon, "G", invariantFormat, "4.94065645841247E-324" };
            yield return new object[] { double.NaN, "G", invariantFormat, "NaN" };
            yield return new object[] { double.PositiveInfinity, "G", invariantFormat, "Infinity" };
            yield return new object[] { double.NegativeInfinity, "G", invariantFormat, "-Infinity" };
        }


        [Fact]
        public static void Test_ToString()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                foreach (var testdata in ToString_TestData())
                {
                    ToString((double)testdata[0], (string)testdata[1], (IFormatProvider)testdata[2], (string)testdata[3]);
                }
                return SuccessExitCode;
            }).Dispose();
        }
        
        private static void ToString(double d, string format, IFormatProvider provider, string expected)
        {
            bool isDefaultProvider = (provider == null || provider == NumberFormatInfo.CurrentInfo);
            if (string.IsNullOrEmpty(format) || format.ToUpperInvariant() == "G")
            {
                if (isDefaultProvider)
                {
                    Assert.Equal(expected, d.ToString());
                    Assert.Equal(expected, d.ToString((IFormatProvider)null));
                }
                Assert.Equal(expected, d.ToString(provider));
            }
            if (isDefaultProvider)
            {
                Assert.Equal(expected.Replace('e', 'E'), d.ToString(format.ToUpperInvariant())); // If format is upper case, then exponents are printed in upper case
                Assert.Equal(expected.Replace('E', 'e'), d.ToString(format.ToLowerInvariant())); // If format is lower case, then exponents are printed in upper case
                Assert.Equal(expected.Replace('e', 'E'), d.ToString(format.ToUpperInvariant(), null));
                Assert.Equal(expected.Replace('E', 'e'), d.ToString(format.ToLowerInvariant(), null));
            }
            Assert.Equal(expected.Replace('e', 'E'), d.ToString(format.ToUpperInvariant(), provider));
            Assert.Equal(expected.Replace('E', 'e'), d.ToString(format.ToLowerInvariant(), provider));
        }

        [Fact]
        public static void ToString_InvalidFormat_ThrowsFormatException()
        {
            double d = 123;
            Assert.Throws<FormatException>(() => d.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => d.ToString("Y", null)); // Invalid format
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            // Defaults: AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowDecimalPoint | AllowExponent | AllowThousands
            NumberStyles defaultStyle = NumberStyles.Float;

            NumberFormatInfo emptyFormat = NumberFormatInfo.CurrentInfo;

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

            yield return new object[] { "-123", defaultStyle, null, (double)-123 };
            yield return new object[] { "0", defaultStyle, null, (double)0 };
            yield return new object[] { "123", defaultStyle, null, (double)123 };
            yield return new object[] { "  123  ", defaultStyle, null, (double)123 };
            yield return new object[] { (567.89).ToString(), defaultStyle, null, 567.89 };
            yield return new object[] { (-567.89).ToString(), defaultStyle, null, -567.89 };
            yield return new object[] { "1E23", defaultStyle, null, 1E23 };

            yield return new object[] { (123.1).ToString(), NumberStyles.AllowDecimalPoint, null, 123.1 };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.AllowThousands, null, (double)1000 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, (double)123 };
            yield return new object[] { (123.567).ToString(), NumberStyles.Any, emptyFormat, 123.567 };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, (double)123 };
            yield return new object[] { "$1,000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, (double)1000 };
            yield return new object[] { "$1000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, (double)1000 };
            yield return new object[] { "123.123", NumberStyles.Float, decimalSeparatorFormat, 123.123 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, decimalSeparatorFormat, -123 };

            yield return new object[] { "NaN", NumberStyles.Any, invariantFormat, double.NaN };
            yield return new object[] { "Infinity", NumberStyles.Any, invariantFormat, double.PositiveInfinity };
            yield return new object[] { "-Infinity", NumberStyles.Any, invariantFormat, double.NegativeInfinity };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse(string value, NumberStyles style, IFormatProvider provider, double expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            double result;
            if ((style & ~NumberStyles.Integer) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(double.TryParse(value, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, double.Parse(value));
                }

                Assert.Equal(expected, double.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(double.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, double.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(double.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, double.Parse(value, style));
                Assert.Equal(expected, double.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Float;

            var dollarSignDecimalSeparatorFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "$",
                NumberDecimalSeparator = "."
            };

            yield return new object[] { null, defaultStyle, null, typeof(ArgumentNullException) };
            yield return new object[] { "", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { " ", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "Garbage", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "ab", defaultStyle, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "(123)", defaultStyle, null, typeof(FormatException) }; // Parentheses
            yield return new object[] { 100.ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency

            yield return new object[] { (123.456).ToString(), NumberStyles.Integer, null, typeof(FormatException) }; // Decimal
            yield return new object[] { "  " + (123.456).ToString(), NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { (123.456).ToString() + "   ", NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { "1E23", NumberStyles.None, null, typeof(FormatException) }; // Exponent

            yield return new object[] { "ab", NumberStyles.None, null, typeof(FormatException) }; // Negative hex value
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) }; // Trailing and leading whitespace
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            double result;
            if ((style & ~NumberStyles.Integer) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(double.TryParse(value, out result));
                    Assert.Equal(default(double), result);

                    Assert.Throws(exceptionType, () => double.Parse(value));
                }

                Assert.Throws(exceptionType, () => double.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(double.TryParse(value, style, provider, out result));
            Assert.Equal(default(double), result);

            Assert.Throws(exceptionType, () => double.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(double.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(double), result);

                Assert.Throws(exceptionType, () => double.Parse(value, style));
                Assert.Throws(exceptionType, () => double.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }
    }
}
