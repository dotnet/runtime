// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

#pragma warning disable xUnit1025 // reporting duplicate test cases due to not distinguishing 0.0 from -0.0, NaN from -NaN

namespace System.Tests
{
    public class SingleTests
    {
        // NOTE: Consider duplicating any tests added here in DoubleTests.cs

        // binary32 (float) has a machine epsilon of 2^-23 (approx. 1.19e-07). However, this
        // is slightly too accurate when writing tests meant to run against libm implementations
        // for various platforms. 2^-21 (approx. 4.76e-07) seems to be as accurate as we can get.
        //
        // The tests themselves will take CrossPlatformMachineEpsilon and adjust it according to the expected result
        // so that the delta used for comparison will compare the most significant digits and ignore
        // any digits that are outside the single precision range (6-9 digits).
        //
        // For example, a test with an expect result in the format of 0.xxxxxxxxx will use
        // CrossPlatformMachineEpsilon for the variance, while an expected result in the format of 0.0xxxxxxxxx
        // will use CrossPlatformMachineEpsilon / 10 and expected result in the format of x.xxxxxx will
        // use CrossPlatformMachineEpsilon * 10.
        private const float CrossPlatformMachineEpsilon = 4.76837158e-07f;

        [Theory]
        [InlineData("a")]
        [InlineData(234.0)]
        public static void CompareTo_ObjectNotFloat_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((float)123).CompareTo(value));
        }

        [Theory]
        [InlineData(234.0f, 234.0f, 0)]
        [InlineData(234.0f, float.MinValue, 1)]
        [InlineData(234.0f, -123.0f, 1)]
        [InlineData(234.0f, 0.0f, 1)]
        [InlineData(234.0f, 123.0f, 1)]
        [InlineData(234.0f, 456.0f, -1)]
        [InlineData(234.0f, float.MaxValue, -1)]
        [InlineData(234.0f, float.NaN, 1)]
        [InlineData(float.NaN, float.NaN, 0)]
        [InlineData(float.NaN, 0.0f, -1)]
        [InlineData(234.0f, null, 1)]
        [InlineData(float.MinValue, float.NegativeInfinity, 1)]
        [InlineData(float.NegativeInfinity, float.MinValue, -1)]
        [InlineData(-0f, float.NegativeInfinity, 1)]
        [InlineData(float.NegativeInfinity, -0f, -1)]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, 0)]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, -1)]
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, 1)]
        public static void CompareTo_Other_ReturnsExpected(float f1, object value, int expected)
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
        public static void Epsilon()
        {
            Assert.Equal(1.40129846E-45f, float.Epsilon);
            Assert.Equal(0x00000001u, BitConverter.SingleToUInt32Bits(float.Epsilon));
        }

        [Theory]
        [InlineData(789.0f, 789.0f, true)]
        [InlineData(789.0f, -789.0f, false)]
        [InlineData(789.0f, 0.0f, false)]
        [InlineData(float.NaN, float.NaN, true)]
        [InlineData(float.NaN, -float.NaN, true)]
        [InlineData(789.0f, 789.0, false)]
        [InlineData(789.0f, "789", false)]
        public static void EqualsTest(float f1, object value, bool expected)
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

        [Fact]
        public static void GetTypeCode_Invoke_ReturnsSingle()
        {
            Assert.Equal(TypeCode.Single, 0.0f.GetTypeCode());
        }

        [Theory]
        [InlineData(float.NaN,              float.NaN,              float.NaN,              0.0f)]
        [InlineData(float.NaN,              0.0f,                   float.NaN,              0.0f)]
        [InlineData(float.NaN,              1.0f,                   float.NaN,              0.0f)]
        [InlineData(float.NaN,              2.71828183f,            float.NaN,              0.0f)]
        [InlineData(float.NaN,              10.0f,                  float.NaN,              0.0f)]
        [InlineData(0.0f,                   0.0f,                   0.0f,                   0.0f)]
        [InlineData(0.0f,                   1.0f,                   1.0f,                   0.0f)]
        [InlineData(0.0f,                   1.57079633f,            1.57079633f,            0.0f)]
        [InlineData(0.0f,                   2.0f,                   2.0f,                   0.0f)]
        [InlineData(0.0f,                   2.71828183f,            2.71828183f,            0.0f)]
        [InlineData(0.0f,                   3.0f,                   3.0f,                   0.0f)]
        [InlineData(0.0f,                   10.0f,                  10.0f,                  0.0f)]
        [InlineData(1.0f,                   1.0f,                   1.41421356f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.0f,                   1e+10f,                 1e+10f,                 0.0)] // dotnet/runtime#75651
        [InlineData(1.0f,                   1e+20f,                 1e+20f,                 0.0)] // dotnet/runtime#75651
        [InlineData(2.71828183f,            0.318309886f,           2.73685536f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (1 / pi)
        [InlineData(2.71828183f,            0.434294482f,           2.75275640f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (log10(e))
        [InlineData(2.71828183f,            0.636619772f,           2.79183467f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (2 / pi)
        [InlineData(2.71828183f,            0.693147181f,           2.80526454f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (ln(2))
        [InlineData(2.71828183f,            0.707106781f,           2.80874636f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (1 / sqrt(2))
        [InlineData(2.71828183f,            0.785398163f,           2.82947104f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (pi / 4)
        [InlineData(2.71828183f,            1.0f,                   2.89638673f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)
        [InlineData(2.71828183f,            1.12837917f,            2.94317781f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (2 / sqrt(pi))
        [InlineData(2.71828183f,            1.41421356f,            3.06415667f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (sqrt(2))
        [InlineData(2.71828183f,            1.44269504f,            3.07740558f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (log2(e))
        [InlineData(2.71828183f,            1.57079633f,            3.13949951f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (pi / 2)
        [InlineData(2.71828183f,            2.30258509f,            3.56243656f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (ln(10))
        [InlineData(2.71828183f,            2.71828183f,            3.84423103f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (e)
        [InlineData(2.71828183f,            3.14159265f,            4.15435440f,            CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (pi)
        [InlineData(10.0f,                  0.318309886f,           10.0050648f,            CrossPlatformMachineEpsilon * 100)]  //          y: (1 / pi)
        [InlineData(10.0f,                  0.434294482f,           10.0094261f,            CrossPlatformMachineEpsilon * 100)]  //          y: (log10(e))
        [InlineData(10.0f,                  0.636619772f,           10.0202437f,            CrossPlatformMachineEpsilon * 100)]  //          y: (2 / pi)
        [InlineData(10.0f,                  0.693147181f,           10.0239939f,            CrossPlatformMachineEpsilon * 100)]  //          y: (ln(2))
        [InlineData(10.0f,                  0.707106781f,           10.0249688f,            CrossPlatformMachineEpsilon * 100)]  //          y: (1 / sqrt(2))
        [InlineData(10.0f,                  0.785398163f,           10.0307951f,            CrossPlatformMachineEpsilon * 100)]  //          y: (pi / 4)
        [InlineData(10.0f,                  1.0f,                   10.0498756f,            CrossPlatformMachineEpsilon * 100)]  //
        [InlineData(10.0f,                  1.12837917f,            10.0634606f,            CrossPlatformMachineEpsilon * 100)]  //          y: (2 / sqrt(pi))
        [InlineData(10.0f,                  1.41421356f,            10.0995049f,            CrossPlatformMachineEpsilon * 100)]  //          y: (sqrt(2))
        [InlineData(10.0f,                  1.44269504f,            10.1035325f,            CrossPlatformMachineEpsilon * 100)]  //          y: (log2(e))
        [InlineData(10.0f,                  1.57079633f,            10.1226183f,            CrossPlatformMachineEpsilon * 100)]  //          y: (pi / 2)
        [InlineData(10.0f,                  2.30258509f,            10.2616713f,            CrossPlatformMachineEpsilon * 100)]  //          y: (ln(10))
        [InlineData(10.0f,                  2.71828183f,            10.3628691f,            CrossPlatformMachineEpsilon * 100)]  //          y: (e)
        [InlineData(10.0f,                  3.14159265f,            10.4818703f,            CrossPlatformMachineEpsilon * 100)]  //          y: (pi)
        [InlineData(float.PositiveInfinity, float.NaN,              float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, 0.0f,                   float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, 1.0f,                   float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, 2.71828183f,            float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, 10.0f,                  float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Hypot(float x, float y, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, float.Hypot(-x, -y), allowedVariance);
            AssertExtensions.Equal(expectedResult, float.Hypot(-x, +y), allowedVariance);
            AssertExtensions.Equal(expectedResult, float.Hypot(+x, -y), allowedVariance);
            AssertExtensions.Equal(expectedResult, float.Hypot(+x, +y), allowedVariance);

            AssertExtensions.Equal(expectedResult, float.Hypot(-y, -x), allowedVariance);
            AssertExtensions.Equal(expectedResult, float.Hypot(-y, +x), allowedVariance);
            AssertExtensions.Equal(expectedResult, float.Hypot(+y, -x), allowedVariance);
            AssertExtensions.Equal(expectedResult, float.Hypot(+y, +x), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, true)]      // Negative Infinity
        [InlineData(float.MinValue, false)]             // Min Negative Normal
        [InlineData(-1.17549435E-38f, false)]           // Max Negative Normal
        [InlineData(-1.17549421E-38f, false)]           // Min Negative Subnormal
        [InlineData(-float.Epsilon, false)]             // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(float.Epsilon, false)]              // Min Positive Subnormal (Positive Epsilon)
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, true)]      // Positive Infinity
        public static void IsInfinity(float d, bool expected)
        {
            Assert.Equal(expected, float.IsInfinity(d));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, false)]     // Negative Infinity
        [InlineData(float.MinValue, false)]             // Min Negative Normal
        [InlineData(-1.17549435E-38f, false)]           // Max Negative Normal
        [InlineData(-1.17549421E-38f, false)]           // Min Negative Subnormal
        [InlineData(-float.Epsilon, false)]             // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, true)]                   // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(float.Epsilon, false)]              // Min Positive Subnormal (Positive Epsilon)
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsNaN(float d, bool expected)
        {
            Assert.Equal(expected, float.IsNaN(d));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, true)]      // Negative Infinity
        [InlineData(float.MinValue, false)]             // Min Negative Normal
        [InlineData(-1.17549435E-38f, false)]           // Max Negative Normal
        [InlineData(-1.17549421E-38f, false)]           // Min Negative Subnormal
        [InlineData(-float.Epsilon, false)]             // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(float.Epsilon, false)]              // Min Positive Subnormal (Positive Epsilon)
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsNegativeInfinity(float d, bool expected)
        {
            Assert.Equal(expected, float.IsNegativeInfinity(d));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, false)]     // Negative Infinity
        [InlineData(float.MinValue, false)]             // Min Negative Normal
        [InlineData(-1.17549435E-38f, false)]           // Max Negative Normal
        [InlineData(-1.17549421E-38f, false)]           // Min Negative Subnormal
        [InlineData(-float.Epsilon, false)]             // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(float.Epsilon, false)]              // Min Positive Subnormal (Positive Epsilon)
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, true)]      // Positive Infinity
        public static void IsPositiveInfinity(float d, bool expected)
        {
            Assert.Equal(expected, float.IsPositiveInfinity(d));
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(3.40282347E+38f, float.MaxValue);
            Assert.Equal(0x7F7FFFFFu, BitConverter.SingleToUInt32Bits(float.MaxValue));
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(-3.40282347E+38f, float.MinValue);
            Assert.Equal(0xFF7FFFFFu, BitConverter.SingleToUInt32Bits(float.MinValue));
        }

        [Fact]
        public static void NaN()
        {
            Assert.Equal(0.0f / 0.0f, float.NaN);
            Assert.Equal(0xFFC00000u, BitConverter.SingleToUInt32Bits(float.NaN));
        }

        [Fact]
        public static void NegativeInfinity()
        {
            Assert.Equal(-1.0f / 0.0f, float.NegativeInfinity);
            Assert.Equal(0xFF800000u, BitConverter.SingleToUInt32Bits(float.NegativeInfinity));
        }

        [Fact]
        public static void NegativeZero()
        {
            Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(float.NegativeZero));
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            NumberStyles defaultStyle = NumberStyles.Float | NumberStyles.AllowThousands;

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

            yield return new object[] { "-123", defaultStyle, null, -123.0f };
            yield return new object[] { "0", defaultStyle, null, 0.0f };
            yield return new object[] { "123", defaultStyle, null, 123.0f };
            yield return new object[] { "  123  ", defaultStyle, null, 123.0f };
            yield return new object[] { (567.89f).ToString(), defaultStyle, null, 567.89f };
            yield return new object[] { (-567.89f).ToString(), defaultStyle, null, -567.89f };
            yield return new object[] { "1E23", defaultStyle, null, 1E23f };

            yield return new object[] { emptyFormat.NumberDecimalSeparator + "234", defaultStyle, null, 0.234f };
            yield return new object[] { "234" + emptyFormat.NumberDecimalSeparator, defaultStyle, null, 234.0f };
            yield return new object[] { new string('0', 72) + "3" + new string('0', 38) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, 3E38f };
            yield return new object[] { new string('0', 73) + "3" + new string('0', 38) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, 3E38f };

            // 2^24 + 1. Not exactly representable
            yield return new object[] { "16777217.0", defaultStyle, invariantFormat, 16777216.0f };
            yield return new object[] { "16777217.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, 16777218.0f };
            yield return new object[] { "16777217.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, 16777218.0f };
            yield return new object[] { "16777217.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, 16777218.0f };
            yield return new object[] { "5.005", defaultStyle, invariantFormat, 5.005f };
            yield return new object[] { "5.050", defaultStyle, invariantFormat, 5.05f };
            yield return new object[] { "5.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005", defaultStyle, invariantFormat, 5.0f };
            yield return new object[] { "5.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005", defaultStyle, invariantFormat, 5.0f };
            yield return new object[] { "5.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005", defaultStyle, invariantFormat, 5.0f };
            yield return new object[] { "5.005000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 5.005f };
            yield return new object[] { "5.0050000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 5.005f };
            yield return new object[] { "5.0050000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 5.005f };

            yield return new object[] { "5005.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 5005.0f };
            yield return new object[] { "50050.0", defaultStyle, invariantFormat, 50050.0f };
            yield return new object[] { "5005", defaultStyle, invariantFormat, 5005.0f };
            yield return new object[] { "050050", defaultStyle, invariantFormat, 50050.0f };
            yield return new object[] { "0.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 0.0f };
            yield return new object[] { "0.005", defaultStyle, invariantFormat, 0.005f };
            yield return new object[] { "0.0500", defaultStyle, invariantFormat, 0.05f };
            yield return new object[] { "6250000000000000000000000000000000e-12", defaultStyle, invariantFormat, 6.25e21f };
            yield return new object[] { "6250000e0", defaultStyle, invariantFormat, 6.25e6f };
            yield return new object[] { "6250100e-5", defaultStyle, invariantFormat, 62.501f };
            yield return new object[] { "625010.00e-4", defaultStyle, invariantFormat, 62.501f };
            yield return new object[] { "62500e-4", defaultStyle, invariantFormat, 6.25f };
            yield return new object[] { "62500", defaultStyle, invariantFormat, 62500.0f };

            yield return new object[] { (123.1f).ToString(), NumberStyles.AllowDecimalPoint, null, 123.1f };
            yield return new object[] { (1000.0f).ToString("N0"), NumberStyles.AllowThousands, null, 1000.0f };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, 123.0f };
            yield return new object[] { (123.567f).ToString(), NumberStyles.Any, emptyFormat, 123.567f };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, 123.0f };
            yield return new object[] { "$1,000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, 1000.0f };
            yield return new object[] { "$1000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, 1000.0f };
            yield return new object[] { "123.123", NumberStyles.Float, decimalSeparatorFormat, 123.123f };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, decimalSeparatorFormat, -123.0f };

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
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
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
            yield return new object[] { (100.0f).ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency

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
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
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

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            const NumberStyles DefaultStyle = NumberStyles.Float | NumberStyles.AllowThousands;

            yield return new object[] { "-123", 1, 3, DefaultStyle, null, (float)123 };
            yield return new object[] { "-123", 0, 3, DefaultStyle, null, (float)-12 };
            yield return new object[] { "1E23", 0, 3, DefaultStyle, null, (float)1E2 };
            yield return new object[] { "123", 0, 2, NumberStyles.Float, new NumberFormatInfo(), (float)12 };
            yield return new object[] { "$1,000", 1, 3, NumberStyles.Currency, new NumberFormatInfo() { CurrencySymbol = "$", CurrencyGroupSeparator = "," }, (float)10 };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, (float)123 };
            yield return new object[] { "-Infinity", 1, 8, NumberStyles.Any, NumberFormatInfo.InvariantInfo, float.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, float expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            float result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(float.TryParse(value.AsSpan(offset, count), out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, float.Parse(value.AsSpan(offset, count)));
                }

                Assert.Equal(expected, float.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, float.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(float.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => float.Parse(value.AsSpan(), style, provider));

                Assert.False(float.TryParse(value.AsSpan(), style, provider, out float result));
                Assert.Equal(0, result);
            }
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Utf8Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, float expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;

            float result;
            ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value, offset, count);

            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(float.TryParse(valueUtf8, out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, float.Parse(valueUtf8));
                }

                Assert.Equal(expected, float.Parse(valueUtf8, provider: provider));
            }

            Assert.Equal(expected, float.Parse(valueUtf8, style, provider));

            Assert.True(float.TryParse(valueUtf8, style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Utf8Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                ReadOnlySpan<byte> valueUtf8 = Encoding.UTF8.GetBytes(value);
                Exception e = Assert.Throws(exceptionType, () => float.Parse(Encoding.UTF8.GetBytes(value), style, provider));
                if (e is FormatException fe)
                {
                    Assert.Contains(value, fe.Message);
                }

                Assert.False(float.TryParse(valueUtf8, style, provider, out float result));
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public static void Parse_Utf8Span_InvalidUtf8()
        {
            FormatException fe = Assert.Throws<FormatException>(() => float.Parse([0xA0]));
            Assert.DoesNotContain("A0", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadOnlySpan", fe.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("\uFFFD", fe.Message, StringComparison.Ordinal);
        }

        [Fact]
        public static void PositiveInfinity()
        {
            Assert.Equal(1.0f / 0.0f, float.PositiveInfinity);
            Assert.Equal(0x7F800000u, BitConverter.SingleToUInt32Bits(float.PositiveInfinity));
        }

        [Theory]
        [InlineData( float.NegativeInfinity, -5, -0.0f,                   0.0f)]
        [InlineData( float.NegativeInfinity, -4,  float.NaN,              0.0f)]
        [InlineData( float.NegativeInfinity, -3, -0.0f,                   0.0f)]
        [InlineData( float.NegativeInfinity, -2,  float.NaN,              0.0f)]
        [InlineData( float.NegativeInfinity, -1, -0.0f,                   0.0f)]
        [InlineData( float.NegativeInfinity,  0,  float.NaN,              0.0f)]
        [InlineData( float.NegativeInfinity,  1,  float.NegativeInfinity, 0.0f)]
        [InlineData( float.NegativeInfinity,  2,  float.NaN,              0.0f)]
        [InlineData( float.NegativeInfinity,  3,  float.NegativeInfinity, 0.0f)]
        [InlineData( float.NegativeInfinity,  4,  float.NaN,              0.0f)]
        [InlineData( float.NegativeInfinity,  5,  float.NegativeInfinity, 0.0f)]
        [InlineData(-2.71828183f,            -5, -0.81873075f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData(-2.71828183f,            -4,  float.NaN,              0.0f)]
        [InlineData(-2.71828183f,            -3, -0.71653131f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData(-2.71828183f,            -2,  float.NaN,              0.0f)]
        [InlineData(-2.71828183f,            -1, -0.36787944f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData(-2.71828183f,             0,  float.NaN,              0.0f)]
        [InlineData(-2.71828183f,             1, -2.71828183f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData(-2.71828183f,             2,  float.NaN,              0.0f)]
        [InlineData(-2.71828183f,             3, -1.39561243f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData(-2.71828183f,             4,  float.NaN,              0.0f)]
        [InlineData(-2.71828183f,             5, -1.22140276f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0f,                   -5, -1.0f,                   0.0f)]
        [InlineData(-1.0f,                   -4,  float.NaN,              0.0f)]
        [InlineData(-1.0f,                   -3, -1.0f,                   0.0f)]
        [InlineData(-1.0f,                   -2,  float.NaN,              0.0f)]
        [InlineData(-1.0f,                   -1, -1.0f,                   0.0f)]
        [InlineData(-1.0f,                    0,  float.NaN,              0.0f)]
        [InlineData(-1.0f,                    1, -1.0f,                   0.0f)]
        [InlineData(-1.0f,                    2,  float.NaN,              0.0f)]
        [InlineData(-1.0f,                    3, -1.0f,                   0.0f)]
        [InlineData(-1.0f,                    4,  float.NaN,              0.0f)]
        [InlineData(-1.0f,                    5, -1.0f,                   0.0f)]
        [InlineData(-0.0f,                   -5,  float.NegativeInfinity, 0.0f)]
        [InlineData(-0.0f,                   -4,  float.PositiveInfinity, 0.0f)]
        [InlineData(-0.0f,                   -3,  float.NegativeInfinity, 0.0f)]
        [InlineData(-0.0f,                   -2,  float.PositiveInfinity, 0.0f)]
        [InlineData(-0.0f,                   -1,  float.NegativeInfinity, 0.0f)]
        [InlineData(-0.0f,                    0,  float.NaN,              0.0f)]
        [InlineData(-0.0f,                    1, -0.0f,                   0.0f)]
        [InlineData(-0.0f,                    2,  0.0f,                   0.0f)]
        [InlineData(-0.0f,                    3, -0.0f,                   0.0f)]
        [InlineData(-0.0f,                    4,  0.0f,                   0.0f)]
        [InlineData(-0.0f,                    5, -0.0f,                   0.0f)]
        [InlineData( float.NaN,              -5,  float.NaN,              0.0f)]
        [InlineData( float.NaN,              -4,  float.NaN,              0.0f)]
        [InlineData( float.NaN,              -3,  float.NaN,              0.0f)]
        [InlineData( float.NaN,              -2,  float.NaN,              0.0f)]
        [InlineData( float.NaN,              -1,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               0,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               1,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               2,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               3,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               4,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               5,  float.NaN,              0.0f)]
        [InlineData( 0.0f,                   -5,  float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -4,  float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -3,  float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -2,  float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -1,  float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                    0,  float.NaN,              0.0f)]
        [InlineData( 0.0f,                    1,  0.0f,                   0.0f)]
        [InlineData( 0.0f,                    2,  0.0f,                   0.0f)]
        [InlineData( 0.0f,                    3,  0.0f,                   0.0f)]
        [InlineData( 0.0f,                    4,  0.0f,                   0.0f)]
        [InlineData( 0.0f,                    5,  0.0f,                   0.0f)]
        [InlineData( 1.0f,                   -5,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                   -4,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                   -3,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                   -2,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                   -1,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                    0,  float.NaN,              0.0f)]
        [InlineData( 1.0f,                    1,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                    2,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                    3,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                    4,  1.0f,                   0.0f)]
        [InlineData( 1.0f,                    5,  1.0f,                   0.0f)]
        [InlineData( 2.71828183f,            -5,  0.81873075f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.71828183f,            -4,  0.77880078f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.71828183f,            -3,  0.71653131f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.71828183f,            -2,  0.60653066f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.71828183f,            -1,  0.36787944f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.71828183f,             0,  float.NaN,              0.0f)]
        [InlineData( 2.71828183f,             1,  2.71828183f,            0.0f)]
        [InlineData( 2.71828183f,             2,  1.64872127f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.71828183f,             3,  1.39561243f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.71828183f,             4,  1.28402542f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.71828183f,             5,  1.22140276f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( float.PositiveInfinity, -5,  0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity, -4,  0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity, -3,  0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity, -2,  0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity, -1,  0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity,  0,  float.NaN,              0.0f)]
        [InlineData( float.PositiveInfinity,  1,  float.PositiveInfinity, 0.0f)]
        [InlineData( float.PositiveInfinity,  2,  float.PositiveInfinity, 0.0f)]
        [InlineData( float.PositiveInfinity,  3,  float.PositiveInfinity, 0.0f)]
        [InlineData( float.PositiveInfinity,  4,  float.PositiveInfinity, 0.0f)]
        [InlineData( float.PositiveInfinity,  5,  float.PositiveInfinity, 0.0f)]
        public static void RootN(float x, int n, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, float.RootN(x, n), allowedVariance);
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { -4567.0f, "G", null, "-4567" };
            yield return new object[] { -4567.89101f, "G", null, "-4567.891" };
            yield return new object[] { 0.0f, "G", null, "0" };
            yield return new object[] { 4567.0f, "G", null, "4567" };
            yield return new object[] { 4567.89101f, "G", null, "4567.891" };

            yield return new object[] { float.NaN, "G", null, "NaN" };

            yield return new object[] { 2468.0f, "N", null, "2,468.00" };

            // Changing the negative pattern doesn't do anything without also passing in a format string
            var customNegativePattern = new NumberFormatInfo() { NumberNegativePattern = 0 };
            yield return new object[] { -6310.0f, "G", customNegativePattern, "-6310" };

            var customNegativeSignDecimalGroupSeparator = new NumberFormatInfo()
            {
                NegativeSign = "#",
                NumberDecimalSeparator = "~",
                NumberGroupSeparator = "*"
            };
            yield return new object[] { -2468.0f, "N", customNegativeSignDecimalGroupSeparator, "#2*468~00" };
            yield return new object[] { 2468.0f, "N", customNegativeSignDecimalGroupSeparator, "2*468~00" };

            var customNegativeSignGroupSeparatorNegativePattern = new NumberFormatInfo()
            {
                NegativeSign = "xx", // Set to trash to make sure it doesn't show up
                NumberGroupSeparator = "*",
                NumberNegativePattern = 0
            };
            yield return new object[] { -2468.0f, "N", customNegativeSignGroupSeparatorNegativePattern, "(2*468.00)" };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { float.NaN, "G", invariantFormat, "NaN" };
            yield return new object[] { float.PositiveInfinity, "G", invariantFormat, "Infinity" };
            yield return new object[] { float.NegativeInfinity, "G", invariantFormat, "-Infinity" };
        }

        public static IEnumerable<object[]> ToString_TestData_NotNetFramework()
        {
            foreach (var testData in ToString_TestData())
            {
                yield return testData;
            }


            yield return new object[] { float.MinValue, "G", null, "-3.4028235E+38" };
            yield return new object[] { float.MaxValue, "G", null, "3.4028235E+38" };

            yield return new object[] { float.Epsilon, "G", null, "1E-45" };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { float.Epsilon, "G", invariantFormat, "1E-45" };
            yield return new object[] { 32.5f, "C100", invariantFormat, "\u00A432.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { 32.5f, "P100", invariantFormat, "3,250.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { 32.5f, "E100", invariantFormat, "3.2500000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { 32.5f, "F100", invariantFormat, "32.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { 32.5f, "N100", invariantFormat, "32.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
        }

        [Fact]
        public static void Test_ToString_NotNetFramework()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (object[] testdata in ToString_TestData_NotNetFramework())
                {
                    ToStringTest((float)testdata[0], (string)testdata[1], (IFormatProvider)testdata[2], (string)testdata[3]);
                }
            }
        }

        private static void ToStringTest(float f, string format, IFormatProvider provider, string expected)
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
            float f = 123.0f;
            Assert.Throws<FormatException>(() => f.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => f.ToString("Y", null)); // Invalid format
            long intMaxPlus1 = (long)int.MaxValue + 1;
            string intMaxPlus1String = intMaxPlus1.ToString();
            Assert.Throws<FormatException>(() => f.ToString("E" + intMaxPlus1String));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, false)]     // Negative Infinity
        [InlineData(float.MinValue, true)]              // Min Negative Normal
        [InlineData(-1.17549435E-38f, true)]            // Max Negative Normal
        [InlineData(-1.17549421E-38f, true)]            // Min Negative Subnormal
        [InlineData(-1.401298E-45, true)]               // Max Negative Subnormal
        [InlineData(-0.0f, true)]                       // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, true)]                        // Positive Zero
        [InlineData(1.401298E-45, true)]                // Min Positive Subnormal
        [InlineData(1.17549421E-38f, true)]             // Max Positive Subnormal
        [InlineData(1.17549435E-38f, true)]             // Min Positive Normal
        [InlineData(float.MaxValue, true)]              // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsFinite(float d, bool expected)
        {
            Assert.Equal(expected, float.IsFinite(d));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, true)]      // Negative Infinity
        [InlineData(float.MinValue, true)]              // Min Negative Normal
        [InlineData(-1.17549435E-38f, true)]            // Max Negative Normal
        [InlineData(-1.17549421E-38f, true)]            // Min Negative Subnormal
        [InlineData(-1.401298E-45, true)]               // Max Negative Subnormal
        [InlineData(-0.0f, true)]                       // Negative Zero
        [InlineData(float.NaN, true)]                   // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(1.401298E-45, false)]               // Min Positive Subnormal
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsNegative(float d, bool expected)
        {
            Assert.Equal(expected, float.IsNegative(d));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, false)]     // Negative Infinity
        [InlineData(float.MinValue, true)]              // Min Negative Normal
        [InlineData(-1.17549435E-38f, true)]            // Max Negative Normal
        [InlineData(-1.17549421E-38f, false)]           // Min Negative Subnormal
        [InlineData(-1.401298E-45, false)]              // Max Negative Subnormal
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(1.401298E-45, false)]               // Min Positive Subnormal
        [InlineData(1.17549421E-38f, false)]            // Max Positive Subnormal
        [InlineData(1.17549435E-38f, true)]             // Min Positive Normal
        [InlineData(float.MaxValue, true)]              // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsNormal(float d, bool expected)
        {
            Assert.Equal(expected, float.IsNormal(d));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, false)]     // Negative Infinity
        [InlineData(float.MinValue, false)]             // Min Negative Normal
        [InlineData(-1.17549435E-38f, false)]           // Max Negative Normal
        [InlineData(-1.17549421E-38f, true)]            // Min Negative Subnormal
        [InlineData(-1.401298E-45, true)]               // Max Negative Subnormal
        [InlineData(-0.0f, false)]                      // Negative Zero
        [InlineData(float.NaN, false)]                  // NaN
        [InlineData(0.0f, false)]                       // Positive Zero
        [InlineData(1.401298E-45, true)]                // Min Positive Subnormal
        [InlineData(1.17549421E-38f, true)]             // Max Positive Subnormal
        [InlineData(1.17549435E-38f, false)]            // Min Positive Normal
        [InlineData(float.MaxValue, false)]             // Max Positive Normal
        [InlineData(float.PositiveInfinity, false)]     // Positive Infinity
        public static void IsSubnormal(float d, bool expected)
        {
            Assert.Equal(expected, float.IsSubnormal(d));
        }

        [Fact]
        public static void TryFormat()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (object[] testdata in ToString_TestData())
                {
                    float localI = (float)testdata[0];
                    string localFormat = (string)testdata[1];
                    IFormatProvider localProvider = (IFormatProvider)testdata[2];
                    string localExpected = (string)testdata[3];

                    try
                    {
                        NumberFormatTestHelper.TryFormatNumberTest(localI, localFormat, localProvider, localExpected, formatCasingMatchesOutput: false);
                    }
                    catch (Exception exc)
                    {
                        throw new Exception($"Failed on `{localI}`, `{localFormat}`, `{localProvider}`, `{localExpected}`. {exc}");
                    }
                }
            }
        }

        public static IEnumerable<object[]> ToStringRoundtrip_TestData()
        {
            yield return new object[] { float.NegativeInfinity };
            yield return new object[] { float.MinValue };
            yield return new object[] { -MathF.PI };
            yield return new object[] { -MathF.E };
            yield return new object[] { -float.Epsilon };
            yield return new object[] { -0.845512408f };
            yield return new object[] { -0.0f };
            yield return new object[] { float.NaN };
            yield return new object[] { 0.0f };
            yield return new object[] { 0.845512408f };
            yield return new object[] { float.Epsilon };
            yield return new object[] { MathF.E };
            yield return new object[] { MathF.PI };
            yield return new object[] { float.MaxValue };
            yield return new object[] { float.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(ToStringRoundtrip_TestData))]
        public static void ToStringRoundtrip(float value)
        {
            float result = float.Parse(value.ToString());
            Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits(result));
        }

        [Theory]
        [MemberData(nameof(ToStringRoundtrip_TestData))]
        public static void ToStringRoundtrip_R(float value)
        {
            float result = float.Parse(value.ToString("R"));
            Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits(result));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MaxValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MaxValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, 1.0f)]
        [InlineData(1.0f, float.NaN, 1.0f)]
        [InlineData(float.PositiveInfinity, float.NaN, float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NegativeInfinity)]
        [InlineData(float.NaN, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(-0.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, -0.0f, 0.0f)]
        [InlineData(2.0f, -3.0f, -3.0f)]
        [InlineData(-3.0f, 2.0f, -3.0f)]
        [InlineData(3.0f, -2.0f, 3.0f)]
        [InlineData(-2.0f, 3.0f, 3.0f)]
        public static void MaxMagnitudeNumberTest(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, float.MaxMagnitudeNumber(x, y), 0.0f);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MaxValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MaxValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, 1.0f)]
        [InlineData(1.0f, float.NaN, 1.0f)]
        [InlineData(float.PositiveInfinity, float.NaN, float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NegativeInfinity)]
        [InlineData(float.NaN, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(-0.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, -0.0f, 0.0f)]
        [InlineData(2.0f, -3.0f, 2.0f)]
        [InlineData(-3.0f, 2.0f, 2.0f)]
        [InlineData(3.0f, -2.0f, 3.0f)]
        [InlineData(-2.0f, 3.0f, 3.0f)]
        public static void MaxNumberTest(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, float.MaxNumber(x, y), 0.0f);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, 1.0f)]
        [InlineData(1.0f, float.NaN, 1.0f)]
        [InlineData(float.PositiveInfinity, float.NaN, float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NegativeInfinity)]
        [InlineData(float.NaN, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(-0.0f, 0.0f, -0.0f)]
        [InlineData(0.0f, -0.0f, -0.0f)]
        [InlineData(2.0f, -3.0f, 2.0f)]
        [InlineData(-3.0f, 2.0f, 2.0f)]
        [InlineData(3.0f, -2.0f, -2.0f)]
        [InlineData(-2.0f, 3.0f, -2.0f)]
        public static void MinMagnitudeNumberTest(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, float.MinMagnitudeNumber(x, y), 0.0f);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, 1.0f)]
        [InlineData(1.0f, float.NaN, 1.0f)]
        [InlineData(float.PositiveInfinity, float.NaN, float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NegativeInfinity)]
        [InlineData(float.NaN, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(-0.0f, 0.0f, -0.0f)]
        [InlineData(0.0f, -0.0f, -0.0f)]
        [InlineData(2.0f, -3.0f, -3.0f)]
        [InlineData(-3.0f, 2.0f, -3.0f)]
        [InlineData(3.0f, -2.0f, -2.0f)]
        [InlineData(-2.0f, 3.0f, -2.0f)]
        public static void MinNumberTest(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, float.MinNumber(x, y), 0.0f);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, -1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-3.14159265f,            -0.956786082f,           CrossPlatformMachineEpsilon)]        // value: -(pi)
        [InlineData(-2.71828183f,            -0.934011964f,           CrossPlatformMachineEpsilon)]        // value: -(e)
        [InlineData(-2.30258509f,            -0.9f,                   CrossPlatformMachineEpsilon)]        // value: -(ln(10))
        [InlineData(-1.57079633f,            -0.792120424f,           CrossPlatformMachineEpsilon)]        // value: -(pi / 2)
        [InlineData(-1.44269504f,            -0.763709912f,           CrossPlatformMachineEpsilon)]        // value: -(log2(e))
        [InlineData(-1.41421356f,            -0.756883266f,           CrossPlatformMachineEpsilon)]        // value: -(sqrt(2))
        [InlineData(-1.12837917f,            -0.676442736f,           CrossPlatformMachineEpsilon)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   -0.632120559f,           CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f,           -0.544061872f,           CrossPlatformMachineEpsilon)]        // value: -(pi / 4)
        [InlineData(-0.707106781f,           -0.506931309f,           CrossPlatformMachineEpsilon)]        // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           -0.5f,                   CrossPlatformMachineEpsilon)]        // value: -(ln(2))
        [InlineData(-0.636619772f,           -0.470922192f,           CrossPlatformMachineEpsilon)]        // value: -(2 / pi)
        [InlineData(-0.434294482f,           -0.352278515f,           CrossPlatformMachineEpsilon)]        // value: -(log10(e))
        [InlineData(-0.318309886f,           -0.272622651f,           CrossPlatformMachineEpsilon)]        // value: -(1 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.318309886f,            0.374802227f,           CrossPlatformMachineEpsilon)]        // value:  (1 / pi)
        [InlineData( 0.434294482f,            0.543873444f,           CrossPlatformMachineEpsilon)]        // value:  (log10(e))
        [InlineData( 0.636619772f,            0.890081165f,           CrossPlatformMachineEpsilon)]        // value:  (2 / pi)
        [InlineData( 0.693147181f,            1.0f,                   CrossPlatformMachineEpsilon * 10)]   // value:  (ln(2))
        [InlineData( 0.707106781f,            1.02811498f,            CrossPlatformMachineEpsilon * 10)]   // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            1.19328005f,            CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 4)
        [InlineData( 1.0f,                    1.71828183f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.12837917f,             2.09064302f,            CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             3.11325038f,            CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData( 1.44269504f,             3.23208611f,            CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData( 1.57079633f,             3.81047738f,            CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData( 2.30258509f,             9.0f,                   CrossPlatformMachineEpsilon * 10)]   // value:  (ln(10))
        [InlineData( 2.71828183f,             14.1542622f,            CrossPlatformMachineEpsilon * 100)]  // value:  (e)
        [InlineData( 3.14159265f,             22.1406926f,            CrossPlatformMachineEpsilon * 100)]  // value:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0)]
        public static void ExpM1Test(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, float.ExpM1(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, 0.0f,                   0.0f)]
        [InlineData(-3.14159265f,            0.113314732f,           CrossPlatformMachineEpsilon)]        // value: -(pi)
        [InlineData(-2.71828183f,            0.151955223f,           CrossPlatformMachineEpsilon)]        // value: -(e)
        [InlineData(-2.30258509f,            0.202699566f,           CrossPlatformMachineEpsilon)]        // value: -(ln(10))
        [InlineData(-1.57079633f,            0.336622537f,           CrossPlatformMachineEpsilon)]        // value: -(pi / 2)
        [InlineData(-1.44269504f,            0.367879441f,           CrossPlatformMachineEpsilon)]        // value: -(log2(e))
        [InlineData(-1.41421356f,            0.375214227f,           CrossPlatformMachineEpsilon)]        // value: -(sqrt(2))
        [InlineData(-1.12837917f,            0.457429347f,           CrossPlatformMachineEpsilon)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   0.5f,                   CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f,           0.580191810f,           CrossPlatformMachineEpsilon)]        // value: -(pi / 4)
        [InlineData(-0.707106781f,           0.612547327f,           CrossPlatformMachineEpsilon)]        // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           0.618503138f,           CrossPlatformMachineEpsilon)]        // value: -(ln(2))
        [InlineData(-0.636619772f,           0.643218242f,           CrossPlatformMachineEpsilon)]        // value: -(2 / pi)
        [InlineData(-0.434294482f,           0.740055574f,           CrossPlatformMachineEpsilon)]        // value: -(log10(e))
        [InlineData(-0.318309886f,           0.802008879f,           CrossPlatformMachineEpsilon)]        // value: -(1 / pi)
        [InlineData(-0.0f,                   1.0f,                   0.0f)]
        [InlineData( float.NaN,              float.NaN,              0.0f)]
        [InlineData( 0.0f,                   1.0f,                   0.0f)]
        [InlineData( 0.318309886f,           1.24686899f,            CrossPlatformMachineEpsilon * 10)]   // value:  (1 / pi)
        [InlineData( 0.434294482f,           1.35124987f,            CrossPlatformMachineEpsilon * 10)]   // value:  (log10(e))
        [InlineData( 0.636619772f,           1.55468228f,            CrossPlatformMachineEpsilon * 10)]   // value:  (2 / pi)
        [InlineData( 0.693147181f,           1.61680667f,            CrossPlatformMachineEpsilon * 10)]   // value:  (ln(2))
        [InlineData( 0.707106781f,           1.63252692f,            CrossPlatformMachineEpsilon * 10)]   // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,           1.72356793f,            CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 4)
        [InlineData( 1.0f,                   2.0,                    CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.12837917f,            2.18612996f,            CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,            2.66514414f,            CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData( 1.44269504f,            2.71828183f,            CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData( 1.57079633f,            2.97068642f,            CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData( 2.30258509f,            4.93340967f,            CrossPlatformMachineEpsilon * 10)]   // value:  (ln(10))
        [InlineData( 2.71828183f,            6.58088599f,            CrossPlatformMachineEpsilon * 10)]   // value:  (e)
        [InlineData( 3.14159265f,            8.82497783f,            CrossPlatformMachineEpsilon * 10)]   // value:  (pi)
        [InlineData( float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Exp2Test(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, float.Exp2(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, -1.0f,                   0.0f)]
        [InlineData(-3.14159265f,            -0.886685268f,           CrossPlatformMachineEpsilon)]        // value: -(pi)
        [InlineData(-2.71828183f,            -0.848044777f,           CrossPlatformMachineEpsilon)]        // value: -(e)
        [InlineData(-2.30258509f,            -0.797300434f,           CrossPlatformMachineEpsilon)]        // value: -(ln(10))
        [InlineData(-1.57079633f,            -0.663377463f,           CrossPlatformMachineEpsilon)]        // value: -(pi / 2)
        [InlineData(-1.44269504f,            -0.632120559f,           CrossPlatformMachineEpsilon)]        // value: -(log2(e))
        [InlineData(-1.41421356f,            -0.624785773f,           CrossPlatformMachineEpsilon)]        // value: -(sqrt(2))
        [InlineData(-1.12837917f,            -0.542570653f,           CrossPlatformMachineEpsilon)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   -0.5f,                   CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f,           -0.419808190f,           CrossPlatformMachineEpsilon)]        // value: -(pi / 4)
        [InlineData(-0.707106781f,           -0.387452673f,           CrossPlatformMachineEpsilon)]        // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           -0.381496862f,           CrossPlatformMachineEpsilon)]        // value: -(ln(2))
        [InlineData(-0.636619772f,           -0.356781758f,           CrossPlatformMachineEpsilon)]        // value: -(2 / pi)
        [InlineData(-0.434294482f,           -0.259944426f,           CrossPlatformMachineEpsilon)]        // value: -(log10(e))
        [InlineData(-0.318309886f,           -0.197991121f,           CrossPlatformMachineEpsilon)]        // value: -(1 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.318309886f,            0.246868989f,           CrossPlatformMachineEpsilon)]        // value:  (1 / pi)
        [InlineData( 0.434294482f,            0.351249873f,           CrossPlatformMachineEpsilon)]        // value:  (log10(e))
        [InlineData( 0.636619772f,            0.554682275f,           CrossPlatformMachineEpsilon)]        // value:  (2 / pi)
        [InlineData( 0.693147181f,            0.616806672f,           CrossPlatformMachineEpsilon)]        // value:  (ln(2))
        [InlineData( 0.707106781f,            0.632526919f,           CrossPlatformMachineEpsilon)]        // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            0.723567934f,           CrossPlatformMachineEpsilon)]        // value:  (pi / 4)
        [InlineData( 1.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.12837917f,             1.18612996f,            CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             1.66514414f,            CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData( 1.44269504f,             1.71828183f,            CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData( 1.57079633f,             1.97068642f,            CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData( 2.30258509f,             3.93340967f,            CrossPlatformMachineEpsilon * 10)]   // value:  (ln(10))
        [InlineData( 2.71828183f,             5.58088599f,            CrossPlatformMachineEpsilon * 10)]   // value:  (e)
        [InlineData( 3.14159265f,             7.82497783f,            CrossPlatformMachineEpsilon * 10)]   // value:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Exp2M1Test(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, float.Exp2M1(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, 0.0f,                   0.0f)]
        [InlineData(-3.14159265f,            0.000721784159f,        CrossPlatformMachineEpsilon / 1000)]  // value: -(pi)
        [InlineData(-2.71828183f,            0.00191301410f,         CrossPlatformMachineEpsilon / 100)]   // value: -(e)
        [InlineData(-2.30258509f,            0.00498212830f,         CrossPlatformMachineEpsilon / 100)]   // value: -(ln(10))
        [InlineData(-1.57079633f,            0.0268660410f,          CrossPlatformMachineEpsilon / 10)]    // value: -(pi / 2)
        [InlineData(-1.44269504f,            0.0360831928f,          CrossPlatformMachineEpsilon / 10)]    // value: -(log2(e))
        [InlineData(-1.41421356f,            0.0385288847f,          CrossPlatformMachineEpsilon / 10)]    // value: -(sqrt(2))
        [InlineData(-1.12837917f,            0.0744082059f,          CrossPlatformMachineEpsilon / 10)]    // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   0.1f,                   CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f,           0.163908636f,           CrossPlatformMachineEpsilon)]         // value: -(pi / 4)
        [InlineData(-0.707106781f,           0.196287760f,           CrossPlatformMachineEpsilon)]         // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           0.202699566f,           CrossPlatformMachineEpsilon)]         // value: -(ln(2))
        [InlineData(-0.636619772f,           0.230876765f,           CrossPlatformMachineEpsilon)]         // value: -(2 / pi)
        [InlineData(-0.434294482f,           0.367879441f,           CrossPlatformMachineEpsilon)]         // value: -(log10(e))
        [InlineData(-0.318309886f,           0.480496373f,           CrossPlatformMachineEpsilon)]         // value: -(1 / pi)
        [InlineData(-0.0f,                   1.0f,                   0.0f)]
        [InlineData( float.NaN,              float.NaN,              0.0f)]
        [InlineData( 0.0f,                   1.0f,                   0.0f)]
        [InlineData( 0.318309886f,           2.08118116f,            CrossPlatformMachineEpsilon * 10)]    // value:  (1 / pi)
        [InlineData( 0.434294482f,           2.71828183f,            CrossPlatformMachineEpsilon * 10)]    // value:  (log10(e))
        [InlineData( 0.636619772f,           4.33131503f,            CrossPlatformMachineEpsilon * 10)]    // value:  (2 / pi)
        [InlineData( 0.693147181f,           4.93340967f,            CrossPlatformMachineEpsilon * 10)]    // value:  (ln(2))
        [InlineData( 0.707106781f,           5.09456117f,            CrossPlatformMachineEpsilon * 10)]    // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,           6.10095980f,            CrossPlatformMachineEpsilon * 10)]    // value:  (pi / 4)
        [InlineData( 1.0f,                   10.0f,                  CrossPlatformMachineEpsilon * 100)]
        [InlineData( 1.12837917f,            13.4393779f,            CrossPlatformMachineEpsilon * 100)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,            25.9545535f,            CrossPlatformMachineEpsilon * 100)]   // value:  (sqrt(2))
        [InlineData( 1.44269504f,            27.7137338f,            CrossPlatformMachineEpsilon * 100)]   // value:  (log2(e))
        [InlineData( 1.57079633f,            37.2217105f,            CrossPlatformMachineEpsilon * 100)]   // value:  (pi / 2)
        [InlineData( 2.30258509f,            200.717432f,            CrossPlatformMachineEpsilon * 1000)]  // value:  (ln(10))
        [InlineData( 2.71828183f,            522.735300f,            CrossPlatformMachineEpsilon * 1000)]  // value:  (e)
        [InlineData( 3.14159265f,            1385.45573f,            CrossPlatformMachineEpsilon * 10000)] // value:  (pi)
        [InlineData( float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Exp10Test(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, float.Exp10(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, -1.0f,                   0.0f)]
        [InlineData(-3.14159265f,            -0.999278216f,           CrossPlatformMachineEpsilon)]         // value: -(pi)
        [InlineData(-2.71828183f,            -0.998086986f,           CrossPlatformMachineEpsilon)]         // value: -(e)
        [InlineData(-2.30258509f,            -0.995017872f,           CrossPlatformMachineEpsilon)]         // value: -(ln(10))
        [InlineData(-1.57079633f,            -0.973133959f,           CrossPlatformMachineEpsilon)]         // value: -(pi / 2)
        [InlineData(-1.44269504f,            -0.963916807f,           CrossPlatformMachineEpsilon)]         // value: -(log2(e))
        [InlineData(-1.41421356f,            -0.961471115f,           CrossPlatformMachineEpsilon)]         // value: -(sqrt(2))
        [InlineData(-1.12837917f,            -0.925591794f,           CrossPlatformMachineEpsilon)]         // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   -0.9f,                   CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f,           -0.836091364f,           CrossPlatformMachineEpsilon)]         // value: -(pi / 4)
        [InlineData(-0.707106781f,           -0.803712240f,           CrossPlatformMachineEpsilon)]         // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           -0.797300434f,           CrossPlatformMachineEpsilon)]         // value: -(ln(2))
        [InlineData(-0.636619772f,           -0.769123235f,           CrossPlatformMachineEpsilon)]         // value: -(2 / pi)
        [InlineData(-0.434294482f,           -0.632120559f,           CrossPlatformMachineEpsilon)]         // value: -(log10(e))
        [InlineData(-0.318309886f,           -0.519503627f,           CrossPlatformMachineEpsilon)]         // value: -(1 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.318309886f,            1.08118116f,            CrossPlatformMachineEpsilon * 10)]    // value:  (1 / pi)
        [InlineData( 0.434294482f,            1.71828183f,            CrossPlatformMachineEpsilon * 10)]    // value:  (log10(e))
        [InlineData( 0.636619772f,            3.33131503f,            CrossPlatformMachineEpsilon * 10)]    // value:  (2 / pi)
        [InlineData( 0.693147181f,            3.93340967f,            CrossPlatformMachineEpsilon * 10)]    // value:  (ln(2))
        [InlineData( 0.707106781f,            4.09456117f,            CrossPlatformMachineEpsilon * 10)]    // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            5.10095980f,            CrossPlatformMachineEpsilon * 10)]    // value:  (pi / 4)
        [InlineData( 1.0f,                    9.0,                    CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.12837917f,             12.4393779f,            CrossPlatformMachineEpsilon * 100)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             24.9545535f,            CrossPlatformMachineEpsilon * 100)]   // value:  (sqrt(2))
        [InlineData( 1.44269504f,             26.7137338f,            CrossPlatformMachineEpsilon * 100)]   // value:  (log2(e))
        [InlineData( 1.57079633f,             36.2217105f,            CrossPlatformMachineEpsilon * 100)]   // value:  (pi / 2)
        [InlineData( 2.30258509f,             199.717432f,            CrossPlatformMachineEpsilon * 1000)]  // value:  (ln(10))
        [InlineData( 2.71828183f,             521.735300f,            CrossPlatformMachineEpsilon * 1000)]  // value:  (e)
        [InlineData( 3.14159265f,             1384.45573f,            CrossPlatformMachineEpsilon * 10000)] // value:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Exp10M1Test(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, float.Exp10M1(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData(-3.14159265f,             float.NaN,              0.0f)]                              //                              value: -(pi)
        [InlineData(-2.71828183f,             float.NaN,              0.0f)]                              //                              value: -(e)
        [InlineData(-1.41421356f,             float.NaN,              0.0f)]                              //                              value: -(sqrt(2))
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData(-1.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(-0.956786082f,           -3.14159265f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData(-0.934011964f,           -2.71828183f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-0.9f,                   -2.30258509f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-0.792120424f,           -1.57079633f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.763709912f,           -1.44269504f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.756883266f,           -1.41421356f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.676442736f,           -1.12837917f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.632120559f,           -1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.544061872f,           -0.785398163f,           CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.506931309f,           -0.707106781f,           CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.5f,                   -0.693147181f,           CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.470922192f,           -0.636619772f,           CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.374802227f,            0.318309886f,           CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.543873444f,            0.434294482f,           CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.890081165f,            0.636619772f,           CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 1.0f,                    0.693147181f,           CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 1.02811498f,             0.707106781f,           CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 1.19328005f,             0.785398163f,           CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 1.71828183f,             1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.09064302f,             1.12837917f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 3.11325038f,             1.41421356f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 3.23208611f,             1.44269504f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 3.81047738f,             1.57079633f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 9.0f,                    2.30258509f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 14.1542622f,             2.71828183f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 22.1406926f,             3.14159265f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void LogP1Test(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, float.LogP1(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData(-1.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(-0.886685268f,           -3.14159265f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData(-0.848044777f,           -2.71828183f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-0.797300434f,           -2.30258509f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-0.663377463f,           -1.57079633f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.632120559f,           -1.44269504f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.624785773f,           -1.41421356f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.542570653f,           -1.12837917f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.5f,                   -1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.419808190f,           -0.785398163f,           CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.387452673f,           -0.707106781f,           CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.381496862f,           -0.693147181f,           CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.356781758f,           -0.636619772f,           CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.259944426f,           -0.434294482f,           CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.197991121f,           -0.318309886f,           CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.246868989f,            0.318309886f,           CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.351249873f,            0.434294482f,           CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.554682275f,            0.636619772f,           CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 0.616806672f,            0.693147181f,           CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 0.632526919f,            0.707106781f,           CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 0.723567934f,            0.785398163f,           CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 1.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.18612996f,             1.12837917f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 1.66514414f,             1.41421356f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 1.71828183f,             1.44269504f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 1.97068642f,             1.57079633f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 3.93340967f,             2.30258509f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 5.58088599f,             2.71828183f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 7.82497783f,             3.14159265f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Log2P1Test(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, float.Log2P1(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData(-3.14159265f,             float.NaN,              0.0f)]                              //                              value: -(pi)
        [InlineData(-2.71828183f,             float.NaN,              0.0f)]                              //                              value: -(e)
        [InlineData(-1.41421356f,             float.NaN,              0.0f)]                              //                              value: -(sqrt(2))
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData(-1.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(-0.998086986f,           -2.71828183f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-0.995017872f,           -2.30258509f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-0.973133959f,           -1.57079633f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.963916807f,           -1.44269504f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.961471115f,           -1.41421356f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.925591794f,           -1.12837917f,            CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.9f,                   -1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.836091364f,           -0.785398163f,           CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.803712240f,           -0.707106781f,           CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.797300434f,           -0.693147181f,           CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.769123235f,           -0.636619772f,           CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.632120559f,           -0.434294482f,           CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.519503627f,           -0.318309886f,           CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 1.08118116f,             0.318309886f,           CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 1.71828183f,             0.434294482f,           CrossPlatformMachineEpsilon)]       // expected:  (log10(e))        value: (e)
        [InlineData( 3.33131503f,             0.636619772f,           CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 3.93340967f,             0.693147181f,           CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 4.09456117f,             0.707106781f,           CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 5.10095980f,             0.785398163f,           CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 9.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 12.4393779f,             1.12837917f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 24.9545535f,             1.41421356f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 26.7137338f,             1.44269504f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 36.2217105f,             1.57079633f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 199.717432f,             2.30258509f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 521.735300f,             2.71828183f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 1384.45573f,             3.14159265f,            CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Log10P1Test(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, float.Log10P1(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NaN,    float.NaN,    0.0f)]
        [InlineData( 1.0f,         0.0f,         0.0f)]
        [InlineData( 0.540302306f, 0.318309886f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.204957194f, 0.434294482f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.0f,         0.5f,         CrossPlatformMachineEpsilon)] // This should be exact, but has an issue on WASM/Unix
        [InlineData(-0.416146837f, 0.636619772f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.570233249f, 0.693147181f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.605699867f, 0.707106781f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.781211892f, 0.785398163f, CrossPlatformMachineEpsilon)]
        [InlineData(-1.0f,         1.0f,         CrossPlatformMachineEpsilon)] // This should be exact, but has an issue on WASM/Unix
        [InlineData(-0.919764995f, 0.871620833f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.266255342f, 0.585786438f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.179057946f, 0.557304959f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.220584041f, 0.429203673f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.581195664f, 0.302585093f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.633255651f, 0.718281828f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.902685362f, 0.858407346f, CrossPlatformMachineEpsilon)]
        public static void AcosPiTest(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, float.AcosPi(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NaN,     float.NaN,    0.0f)]
        [InlineData( 0.0f,          0.0f,         0.0f)]
        [InlineData( 0.841470985f,  0.318309886f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.978770938f,  0.434294482f, CrossPlatformMachineEpsilon)]
        [InlineData( 1.0f,          0.5f,         CrossPlatformMachineEpsilon)] // This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.909297427f,  0.363380228f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.821482831f,  0.306852819f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.795693202f,  0.292893219f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.624265953f,  0.214601837f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.392469559f, -0.128379167f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.963902533f, -0.414213562f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.983838529f, -0.442695041f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.975367972f, -0.429203673f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.813763848f,  0.302585093f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.773942685f,  0.281718172f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.430301217f, -0.141592654f, CrossPlatformMachineEpsilon)]
        public static void AsinPiTest(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, float.AsinPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, float.AsinPi(+value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NaN,               float.NaN,               float.NaN,    0.0f)]
        [InlineData( 0.0f,                   -1.0f,                    1.0f,         CrossPlatformMachineEpsilon)]  // y: sinpi(0)              x:  cospi(1)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.0f,                   -0.0f,                    1.0f,         CrossPlatformMachineEpsilon)]  // y: sinpi(0)              x: -cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.0f,                    0.0f,                    0.0f,         0.0f)]                         // y: sinpi(0)              x:  cospi(0.5)
        [InlineData( 0.0f,                    1.0f,                    0.0f,         0.0f)]                         // y: sinpi(0)              x:  cospi(0)
        [InlineData( 0.841470985f,            0.540302306f,            0.318309886f, CrossPlatformMachineEpsilon)]  // y: sinpi(1 / pi)         x:  cospi(1 / pi)
        [InlineData( 0.978770938f,            0.204957194f,            0.434294482f, CrossPlatformMachineEpsilon)]  // y: sinpi(log10(e))       x:  cospi(log10(e))
        [InlineData( 1.0f,                   -0.0f,                    0.5f,         CrossPlatformMachineEpsilon)]  // y: sinpi(0.5)            x: -cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 1.0f,                    0.0f,                    0.5f,         CrossPlatformMachineEpsilon)]  // y: sinpi(0.5)            x:  cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.909297427f,           -0.416146837f,            0.636619772f, CrossPlatformMachineEpsilon)]  // y: sinpi(2 / pi)         x:  cospi(2 / pi)
        [InlineData( 0.821482831f,           -0.570233249f,            0.693147181f, CrossPlatformMachineEpsilon)]  // y: sinpi(ln(2))          x:  cospi(ln(2))
        [InlineData( 0.795693202f,           -0.605699867f,            0.707106781f, CrossPlatformMachineEpsilon)]  // y: sinpi(1 / sqrt(2))    x:  cospi(1 / sqrt(2))
        [InlineData( 0.624265953f,           -0.781211892f,            0.785398163f, CrossPlatformMachineEpsilon)]  // y: sinpi(pi / 4)         x:  cospi(pi / 4)
        [InlineData(-0.392469559f,           -0.919764995f,           -0.871620833f, CrossPlatformMachineEpsilon)]  // y: sinpi(2 / sqrt(pi))   x:  cospi(2 / sqrt(pi))
        [InlineData(-0.963902533f,           -0.266255342f,           -0.585786438f, CrossPlatformMachineEpsilon)]  // y: sinpi(sqrt(2))        x:  cospi(sqrt(2))
        [InlineData(-0.983838529f,           -0.179057946f,           -0.557304959f, CrossPlatformMachineEpsilon)]  // y: sinpi(log2(e))        x:  cospi(log2(e))
        [InlineData(-0.975367972f,            0.220584041f,           -0.429203673f, CrossPlatformMachineEpsilon)]  // y: sinpi(pi / 2)         x:  cospi(pi / 2)
        [InlineData( 0.813763848f,            0.581195664f,            0.302585093f, CrossPlatformMachineEpsilon)]  // y: sinpi(ln(10))         x:  cospi(ln(10))
        [InlineData( 0.773942685f,           -0.633255651f,            0.718281828f, CrossPlatformMachineEpsilon)]  // y: sinpi(e)              x:  cospi(e)
        [InlineData(-0.430301217f,           -0.902685362f,           -0.858407346f, CrossPlatformMachineEpsilon)]  // y: sinpi(pi)             x:  cospi(pi)
        [InlineData( 1.0f,                    float.NegativeInfinity,  1.0f,         CrossPlatformMachineEpsilon)]  // y: sinpi(0.5)                                    ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 1.0f,                    float.PositiveInfinity,  0.0f,         0.0f)]                         // y: sinpi(0.5)
        [InlineData( float.PositiveInfinity, -1.0f,                    0.5f,         CrossPlatformMachineEpsilon)]  //                          x:  cospi(1)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( float.PositiveInfinity,  1.0f,                    0.5f,         CrossPlatformMachineEpsilon)]  //                          x:  cospi(0)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( float.PositiveInfinity,  float.NegativeInfinity,  0.75f,        CrossPlatformMachineEpsilon)]  //                                                  ; This should be exact, but has an issue on WASM/Unix
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity,  0.25f,        CrossPlatformMachineEpsilon)]  //                                                  ; This should be exact, but has an issue on WASM/Unix
        public static void Atan2PiTest(float y, float x, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, float.Atan2Pi(-y, +x), allowedVariance);
            AssertExtensions.Equal(+expectedResult, float.Atan2Pi(+y, +x), allowedVariance);
        }

        [Theory]
        [InlineData( float.NaN,               float.NaN,    0.0f)]
        [InlineData( 0.0f,                    0.0f,         0.0f)]
        [InlineData( 1.55740773f,             0.318309886f, CrossPlatformMachineEpsilon)]
        [InlineData( 4.77548954f,             0.434294482f, CrossPlatformMachineEpsilon)]
        [InlineData( float.PositiveInfinity,  0.5f,         CrossPlatformMachineEpsilon)] // This should be exact, but has an issue on WASM/Unix
        [InlineData(-2.18503986f,            -0.363380228f, CrossPlatformMachineEpsilon)]
        [InlineData(-1.44060844f,            -0.306852819f, CrossPlatformMachineEpsilon)]
        [InlineData(-1.31367571f,            -0.292893219f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.79909940f,            -0.214601837f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.42670634f,             0.128379167f, CrossPlatformMachineEpsilon)]
        [InlineData( 3.62021857f,             0.414213562f, CrossPlatformMachineEpsilon)]
        [InlineData( 5.49452594f,             0.442695041f, CrossPlatformMachineEpsilon)]
        [InlineData(-4.42175222f,            -0.429203673f, CrossPlatformMachineEpsilon)]
        [InlineData( 1.40015471f,             0.302585093f, CrossPlatformMachineEpsilon)]
        [InlineData(-1.22216467f,            -0.281718172f, CrossPlatformMachineEpsilon)]
        [InlineData( 0.476690146f,            0.141592654f, CrossPlatformMachineEpsilon)]
        public static void AtanPiTest(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, float.AtanPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, float.AtanPi(+value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NaN,               float.NaN,    0.0f)]
        [InlineData(0.0f,                    1.0f,         0.0f)]
        [InlineData(0.318309886f,            0.540302306f, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData(0.434294482f,            0.204957194f, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData(0.5f,                    0.0f,         0.0f)]
        [InlineData(0.636619772f,           -0.416146837f, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData(0.693147181f,           -0.570233249f, CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData(0.707106781f,           -0.605699867f, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData(0.785398163f,           -0.781211892f, CrossPlatformMachineEpsilon)]       // value:  (pi / 4)
        [InlineData(1.0f,                   -1.0f,         0.0f)]
        [InlineData(1.12837917f,            -0.919764995f, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f,            -0.266255342f, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData(1.44269504f,            -0.179057946f, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData(1.5f,                    0.0f,         0.0f)]
        [InlineData(1.57079633f,             0.220584041f, CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData(2.0f,                    1.0f,         0.0f)]
        [InlineData(2.30258509f,             0.581195664f, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData(2.5f,                    0.0f,         0.0f)]
        [InlineData(2.71828183f,            -0.633255651f, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData(3.0f,                   -1.0f,         0.0f)]
        [InlineData(3.14159265f,            -0.902685362f, CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData(3.5f,                    0.0f,         0.0f)]
        [InlineData(float.PositiveInfinity,  float.NaN,    0.0f)]
        public static void CosPiTest(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(+expectedResult, float.CosPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, float.CosPi(+value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NaN,               float.NaN,    0.0f)]
        [InlineData(0.0f,                    0.0f,         0.0f)]
        [InlineData(0.318309886f,            0.841470985f, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData(0.434294482f,            0.978770938f, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData(0.5f,                    1.0f,         0.0f)]
        [InlineData(0.636619772f,            0.909297427f, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData(0.693147181f,            0.821482831f, CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData(0.707106781f,            0.795693202f, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData(0.785398163f,            0.624265953f, CrossPlatformMachineEpsilon)]       // value:  (pi / 4)
        [InlineData(1.0f,                    0.0f,         0.0f)]
        [InlineData(1.12837917f,            -0.392469559f, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f,            -0.963902533f, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData(1.44269504f,            -0.983838529f, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData(1.5f,                   -1.0f,         0.0f)]
        [InlineData(1.57079633f,            -0.975367972f, CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData(2.0f,                    0.0f,         0.0f)]
        [InlineData(2.30258509f,             0.813763848f, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData(2.5f,                    1.0f,         0.0f)]
        [InlineData(2.71828183f,             0.773942685f, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData(3.0f,                    0.0f,         0.0f)]
        [InlineData(3.14159265f,            -0.430301217f, CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData(3.5f,                   -1.0f,         0.0f)]
        [InlineData(float.PositiveInfinity,  float.NaN,    0.0f)]
        public static void SinPiTest(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, float.SinPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, float.SinPi(+value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NaN,               float.NaN,              0.0f)]
        [InlineData(0.0f,                    0.0f,                   0.0f)]
        [InlineData(0.318309886f,            1.55740772f,            CrossPlatformMachineEpsilon * 10)]  // value:  (1 / pi)
        [InlineData(0.434294482f,            4.77548954f,            CrossPlatformMachineEpsilon * 10)]  // value:  (log10(e))
        [InlineData(0.5f,                    float.PositiveInfinity, 0.0f)]
        [InlineData(0.636619772f,           -2.18503986f,            CrossPlatformMachineEpsilon * 10)]  // value:  (2 / pi)
        [InlineData(0.693147181f,           -1.44060844f,            CrossPlatformMachineEpsilon * 10)]  // value:  (ln(2))
        [InlineData(0.707106781f,           -1.31367571f,            CrossPlatformMachineEpsilon * 10)]  // value:  (1 / sqrt(2))
        [InlineData(0.785398163f,           -0.799099398f,           CrossPlatformMachineEpsilon)]       // value:  (pi / 4)
        [InlineData(1.0f,                   -0.0f,                   0.0f)]
        [InlineData(1.12837917f,             0.426706344f,           CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f,             3.62021857f,            CrossPlatformMachineEpsilon * 10)]  // value:  (sqrt(2))
        [InlineData(1.44269504f,             5.49452594f,            CrossPlatformMachineEpsilon * 10)]  // value:  (log2(e))
        [InlineData(1.5f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(1.57079633f,            -4.42175222f,            CrossPlatformMachineEpsilon * 10)]  // value:  (pi / 2)
        [InlineData(2.0f,                    0.0f,                   0.0f)]
        [InlineData(2.30258509f,             1.40015471f,            CrossPlatformMachineEpsilon * 10)]  // value:  (ln(10))
        [InlineData(2.5f,                    float.PositiveInfinity, 0.0f)]
        [InlineData(2.71828183f,            -1.22216467f,            CrossPlatformMachineEpsilon * 10)]  // value:  (e)
        [InlineData(3.0f,                   -0.0f,                   0.0f)]
        [InlineData(3.14159265f,             0.476690146f,           CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData(3.5f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity,  float.NaN,              0.0f)]
        public static void TanPiTest(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, float.TanPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, float.TanPi(+value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity,    float.NegativeInfinity,    0.5f,    float.NegativeInfinity)]
        [InlineData(float.NegativeInfinity,    float.NaN,                 0.5f,    float.NaN)]
        [InlineData(float.NegativeInfinity,    float.PositiveInfinity,    0.5f,    float.NaN)]
        [InlineData(float.NegativeInfinity,    0.0f,                      0.5f,    float.NegativeInfinity)]
        [InlineData(float.NegativeInfinity,    1.0f,                      0.5f,    float.NegativeInfinity)]
        [InlineData(float.NaN,                 float.NegativeInfinity,    0.5f,    float.NaN)]
        [InlineData(float.NaN,                 float.NaN,                 0.5f,    float.NaN)]
        [InlineData(float.NaN,                 float.PositiveInfinity,    0.5f,    float.NaN)]
        [InlineData(float.NaN,                 0.0f,                      0.5f,    float.NaN)]
        [InlineData(float.NaN,                 1.0f,                      0.5f,    float.NaN)]
        [InlineData(float.PositiveInfinity,    float.NegativeInfinity,    0.5f,    float.NaN)]
        [InlineData(float.PositiveInfinity,    float.NaN,                 0.5f,    float.NaN)]
        [InlineData(float.PositiveInfinity,    float.PositiveInfinity,    0.5f,    float.PositiveInfinity)]
        [InlineData(float.PositiveInfinity,    0.0f,                      0.5f,    float.PositiveInfinity)]
        [InlineData(float.PositiveInfinity,    1.0f,                      0.5f,    float.PositiveInfinity)]
        [InlineData(1.0f,                      3.0f,                      0.0f,    1.0f)]
        [InlineData(1.0f,                      3.0f,                      0.5f,    2.0f)]
        [InlineData(1.0f,                      3.0f,                      1.0f,    3.0f)]
        [InlineData(1.0f,                      3.0f,                      2.0f,    5.0f)]
        [InlineData(2.0f,                      4.0f,                      0.0f,    2.0f)]
        [InlineData(2.0f,                      4.0f,                      0.5f,    3.0f)]
        [InlineData(2.0f,                      4.0f,                      1.0f,    4.0f)]
        [InlineData(2.0f,                      4.0f,                      2.0f,    6.0f)]
        [InlineData(3.0f,                      1.0f,                      0.0f,    3.0f)]
        [InlineData(3.0f,                      1.0f,                      0.5f,    2.0f)]
        [InlineData(3.0f,                      1.0f,                      1.0f,    1.0f)]
        [InlineData(3.0f,                      1.0f,                      2.0f,   -1.0f)]
        [InlineData(4.0f,                      2.0f,                      0.0f,    4.0f)]
        [InlineData(4.0f,                      2.0f,                      0.5f,    3.0f)]
        [InlineData(4.0f,                      2.0f,                      1.0f,    2.0f)]
        [InlineData(4.0f,                      2.0f,                      2.0f,    0.0f)]
        public static void LerpTest(float value1, float value2, float amount, float expectedResult)
        {
            AssertExtensions.Equal(+expectedResult, float.Lerp(+value1, +value2, amount), 0);
            AssertExtensions.Equal((expectedResult == 0.0f) ? expectedResult : -expectedResult, float.Lerp(-value1, -value2, amount), 0);
        }

        [Theory]
        [InlineData(float.NaN,                 float.NaN,                 0.0f)]
        [InlineData(0.0f,                      0.0f,                      0.0f)]
        [InlineData(0.318309886f,              0.0055555557f,             CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData(0.434294482f,              0.007579869f,              CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData(0.5f,                      0.008726646f,              CrossPlatformMachineEpsilon)]
        [InlineData(0.636619772f,              0.011111111f,              CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData(0.693147181f,              0.0120977005f,             CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData(0.707106781f,              0.012341342f,              CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData(0.785398163f,              0.013707785f,              CrossPlatformMachineEpsilon)]       // value:  (pi / 4)
        [InlineData(1.0f,                      0.017453292f,              CrossPlatformMachineEpsilon)]
        [InlineData(1.12837917f,               0.019693933f,              CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f,               0.024682684f,              CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData(1.44269504f,               0.025179777f,              CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData(1.5f,                      0.02617994f,               CrossPlatformMachineEpsilon)]
        [InlineData(1.57079633f,               0.02741557f,               CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData(2.0f,                      0.034906585f,              CrossPlatformMachineEpsilon)]
        [InlineData(2.30258509f,               0.040187694f,              CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData(2.5f,                      0.043633234f,              CrossPlatformMachineEpsilon)]
        [InlineData(2.71828183f,               0.047442965f,              CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData(3.0f,                      0.05235988f,               CrossPlatformMachineEpsilon)]
        [InlineData(3.14159265f,               0.05483114f,               CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData(3.5f,                      0.061086528f,              CrossPlatformMachineEpsilon)]
        [InlineData(float.PositiveInfinity,    float.PositiveInfinity,    0.0f)]
        public static void DegreesToRadiansTest(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, float.DegreesToRadians(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, float.DegreesToRadians(+value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NaN,                float.NaN,                  0.0)]
        [InlineData(0.0f,                     0.0f,                       0.0)]
        [InlineData(0.0055555557f,            0.318309886f,               CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData(0.007579869f,             0.434294482f,               CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData(0.008726646f,             0.5f,                       CrossPlatformMachineEpsilon)]
        [InlineData(0.011111111f,             0.636619772f,               CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData(0.0120977005f,            0.693147181f,               CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData(0.012341342f,             0.707106781f,               CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData(0.013707785f,             0.785398163f,               CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData(0.017453292f,             1.0f,                       CrossPlatformMachineEpsilon)]
        [InlineData(0.019693933f,             1.12837917f,                CrossPlatformMachineEpsilon)]       // expected:  (2 / sqrt(pi))
        [InlineData(0.024682684f,             1.41421356f,                CrossPlatformMachineEpsilon)]       // expected:  (sqrt(2))
        [InlineData(0.025179777f,             1.44269504f,                CrossPlatformMachineEpsilon)]       // expected:  (log2(e))
        [InlineData(0.02617994f,              1.5f,                       CrossPlatformMachineEpsilon)]
        [InlineData(0.02741557f,              1.57079633f,                CrossPlatformMachineEpsilon)]       // expected:  (pi / 2)
        [InlineData(0.034906585f,             2.0f,                       CrossPlatformMachineEpsilon)]
        [InlineData(0.040187694f,             2.30258509f,                CrossPlatformMachineEpsilon)]       // expected:  (ln(10))
        [InlineData(0.043633234f,             2.5f,                       CrossPlatformMachineEpsilon)]
        [InlineData(0.047442965f,             2.71828183f,                CrossPlatformMachineEpsilon)]       // expected:  (e)
        [InlineData(0.05235988f,              3.0f,                       CrossPlatformMachineEpsilon)]
        [InlineData(0.05483114f,              3.14159265f,                CrossPlatformMachineEpsilon)]       // expected:  (pi)
        [InlineData(0.061086528f,             3.5f,                       CrossPlatformMachineEpsilon)]
        [InlineData(float.PositiveInfinity,   float.PositiveInfinity,     0.0)]
        public static void RadiansToDegreesTest(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, float.RadiansToDegrees(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, float.RadiansToDegrees(+value), allowedVariance);
        }
    }
}
