// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class SingleTests : RemoteExecutorTestBase
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
        [InlineData(float.NegativeInfinity, true)]      // Negative Infinity
        [InlineData(float.MinValue, false)]             // Min Negative Normal
        [InlineData(-1.17549435E-38f, false)]           // Max Negative Normal
        [InlineData(-1.17549421E-38f, false)]           // Min Negative Subnormal
        [InlineData(-1.401298E-45, false)]              // Max Negative Subnormal
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(1.401298E-45, false)]               // Min Positive Subnormal
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, true)]      // Positive Infinity
        public static void IsInfinity(float d, bool expected)
        {
            Assert.Equal(expected, float.IsInfinity(d));
        }

        [Fact]
        public void GetTypeCode_Invoke_ReturnsSingle()
        {
            Assert.Equal(TypeCode.Single, 0.0f.GetTypeCode());
        }

        [Fact]
        public static void NaN()
        {
            Assert.Equal((float)0.0 / (float)0.0, float.NaN);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, false)]     // Negative Infinity
        [InlineData(float.MinValue, false)]             // Min Negative Normal
        [InlineData(-1.17549435E-38f, false)]           // Max Negative Normal
        [InlineData(-1.17549421E-38f, false)]           // Min Negative Subnormal
        [InlineData(-1.401298E-45, false)]              // Max Negative Subnormal
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, true)]                   // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(1.401298E-45, false)]               // Min Positive Subnormal
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsNaN(float d, bool expected)
        {
            Assert.Equal(expected, float.IsNaN(d));
        }

        [Fact]
        public static void NegativeInfinity()
        {
            Assert.Equal((float)-1.0 / (float)0.0, float.NegativeInfinity);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, true)]      // Negative Infinity
        [InlineData(float.MinValue, false)]             // Min Negative Normal
        [InlineData(-1.17549435E-38f, false)]           // Max Negative Normal
        [InlineData(-1.17549421E-38f, false)]           // Min Negative Subnormal
        [InlineData(-1.401298E-45, false)]              // Max Negative Subnormal
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(1.401298E-45, false)]               // Min Positive Subnormal
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsNegativeInfinity(float d, bool expected)
        {
            Assert.Equal(expected, float.IsNegativeInfinity(d));
        }

        [Fact]
        public static void PositiveInfinity()
        {
            Assert.Equal((float)1.0 / (float)0.0, float.PositiveInfinity);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, false)]     // Negative Infinity
        [InlineData(float.MinValue, false)]             // Min Negative Normal
        [InlineData(-1.17549435E-38f, false)]           // Max Negative Normal
        [InlineData(-1.17549421E-38f, false)]           // Min Negative Subnormal
        [InlineData(-1.401298E-45, false)]              // Max Negative Subnormal
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(1.401298E-45, false)]               // Min Positive Subnormal
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, true)]      // Positive Infinity
        public static void IsPositiveInfinity(float d, bool expected)
        {
            Assert.Equal(expected, float.IsPositiveInfinity(d));
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
        public void CompareTo_Other_ReturnsExpected(float f1, object value, int expected)
        {
            if (value is float f2)
            {
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

            Assert.Equal(expected, Math.Sign(f1.CompareTo(value)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData((double)234)]
        public void CompareTo_ObjectNotFloat_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((float)123).CompareTo(value));
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
            if (value is float f2)
            {
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
            yield return new object[] { float.MinValue, "G", null, "-3.402823E+38" };
            yield return new object[] { (float)-4567, "G", null, "-4567" };
            yield return new object[] { (float)-4567.89101, "G", null, "-4567.891" };
            yield return new object[] { (float)0, "G", null, "0" };
            yield return new object[] { (float)4567, "G", null, "4567" };
            yield return new object[] { (float)4567.89101, "G", null, "4567.891" };
            yield return new object[] { float.MaxValue, "G", null, "3.402823E+38" };

            yield return new object[] { float.Epsilon, "G", null, "1.401298E-45" };
            yield return new object[] { float.NaN, "G", null, "NaN" };

            yield return new object[] { (float)2468, "N", null, "2,468.00" };

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

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { float.Epsilon, "G", invariantFormat, "1.401298E-45" };
            yield return new object[] { float.NaN, "G", invariantFormat, "NaN" };
            yield return new object[] { float.PositiveInfinity, "G", invariantFormat, "Infinity" };
            yield return new object[] { float.NegativeInfinity, "G", invariantFormat, "-Infinity" };
        }

        [Fact]
        public static void Test_ToString()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                foreach (var testdata in ToString_TestData())
                {
                    ToString((float)testdata[0], (string)testdata[1], (IFormatProvider)testdata[2], (string)testdata[3]);
                }
                return SuccessExitCode;
            }).Dispose();
        }
        
        private static void ToString(float f, string format, IFormatProvider provider, string expected)
        {
            bool isDefaultProvider = provider == null;
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

            yield return new object[] { "-123", defaultStyle, null, (float)-123 };
            yield return new object[] { "0", defaultStyle, null, (float)0 };
            yield return new object[] { "123", defaultStyle, null, (float)123 };
            yield return new object[] { "  123  ", defaultStyle, null, (float)123 };
            yield return new object[] { (567.89f).ToString(), defaultStyle, null, (float)567.89 };
            yield return new object[] { (-567.89f).ToString(), defaultStyle, null, (float)-567.89 };
            yield return new object[] { "1E23", defaultStyle, null, (float)1E23 };

            yield return new object[] { (123.1f).ToString(), NumberStyles.AllowDecimalPoint, null, (float)123.1 };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.AllowThousands, null, (float)1000 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, (float)123 };
            yield return new object[] { (123.567f).ToString(), NumberStyles.Any, emptyFormat, 123.567 };
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
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            float result;
            if ((style & ~NumberStyles.Integer) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(float.TryParse(value, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, float.Parse(value));
                }

                Assert.Equal(expected, float.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.True(float.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, float.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.True(float.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, float.Parse(value, style));
                Assert.Equal(expected, float.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Float;

            var dollarSignDecimalSeparatorFormat = new NumberFormatInfo();
            dollarSignDecimalSeparatorFormat.CurrencySymbol = "$";
            dollarSignDecimalSeparatorFormat.NumberDecimalSeparator = ".";

            yield return new object[] { null, defaultStyle, null, typeof(ArgumentNullException) };
            yield return new object[] { "", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { " ", defaultStyle, null, typeof(FormatException) };
            yield return new object[] { "Garbage", defaultStyle, null, typeof(FormatException) };

            yield return new object[] { "ab", defaultStyle, null, typeof(FormatException) }; // Hex value
            yield return new object[] { "(123)", defaultStyle, null, typeof(FormatException) }; // Parentheses
            yield return new object[] { 100.ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency

            yield return new object[] { (123.456f).ToString(), NumberStyles.Integer, null, typeof(FormatException) }; // Decimal
            yield return new object[] { "  " + (123.456f).ToString(), NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { (123.456f).ToString() + "   ", NumberStyles.None, null, typeof(FormatException) }; // Leading space
            yield return new object[] { "1E23", NumberStyles.None, null, typeof(FormatException) }; // Exponent

            yield return new object[] { "ab", NumberStyles.None, null, typeof(FormatException) }; // Negative hex value
            yield return new object[] { "  123  ", NumberStyles.None, null, typeof(FormatException) }; // Trailing and leading whitespace
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            float result;
            if ((style & ~NumberStyles.Integer) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.False(float.TryParse(value, out result));
                    Assert.Equal(default(float), result);

                    Assert.Throws(exceptionType, () => float.Parse(value));
                }

                Assert.Throws(exceptionType, () => float.Parse(value, provider));
            }

            // Use Parse(string, NumberStyles, IFormatProvider)
            Assert.False(float.TryParse(value, style, provider, out result));
            Assert.Equal(default(float), result);

            Assert.Throws(exceptionType, () => float.Parse(value, style, provider));

            if (isDefaultProvider)
            {
                // Use Parse(string, NumberStyles) or Parse(string, NumberStyles, IFormatProvider)
                Assert.False(float.TryParse(value, style, NumberFormatInfo.CurrentInfo, out result));
                Assert.Equal(default(float), result);

                Assert.Throws(exceptionType, () => float.Parse(value, style));
                Assert.Throws(exceptionType, () => float.Parse(value, style, NumberFormatInfo.CurrentInfo));
            }
        }
    }
}
