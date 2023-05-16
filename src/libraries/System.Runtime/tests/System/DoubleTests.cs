// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Tests;
using System.Linq;
using Xunit;

#pragma warning disable xUnit1025 // reporting duplicate test cases due to not distinguishing 0.0 from -0.0, NaN from -NaN

namespace System.Tests
{
    public class DoubleTests
    {
        // NOTE: Consider duplicating any tests added here in SingleTests.cs

        // binary64 (double) has a machine epsilon of 2^-52 (approx. 2.22e-16). However, this
        // is slightly too accurate when writing tests meant to run against libm implementations
        // for various platforms. 2^-50 (approx. 8.88e-16) seems to be as accurate as we can get.
        //
        // The tests themselves will take CrossPlatformMachineEpsilon and adjust it according to the expected result
        // so that the delta used for comparison will compare the most significant digits and ignore
        // any digits that are outside the double precision range (15-17 digits).
        //
        // For example, a test with an expect result in the format of 0.xxxxxxxxxxxxxxxxx will use
        // CrossPlatformMachineEpsilon for the variance, while an expected result in the format of 0.0xxxxxxxxxxxxxxxxx
        // will use CrossPlatformMachineEpsilon / 10 and expected result in the format of x.xxxxxxxxxxxxxxxx will
        // use CrossPlatformMachineEpsilon * 10.
        internal const double CrossPlatformMachineEpsilon = 8.8817841970012523e-16;

        [Theory]
        [InlineData("a")]
        [InlineData(234.0f)]
        public static void CompareTo_ObjectNotDouble_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => ((double)123).CompareTo(value));
        }

        [Theory]
        [InlineData(234.0, 234.0, 0)]
        [InlineData(234.0, double.MinValue, 1)]
        [InlineData(234.0, -123.0, 1)]
        [InlineData(234.0, 0.0, 1)]
        [InlineData(234.0, 123.0, 1)]
        [InlineData(234.0, 456.0, -1)]
        [InlineData(234.0, double.MaxValue, -1)]
        [InlineData(234.0, double.NaN, 1)]
        [InlineData(double.NaN, double.NaN, 0)]
        [InlineData(double.NaN, 0.0, -1)]
        [InlineData(234.0, null, 1)]
        [InlineData(double.MinValue, double.NegativeInfinity, 1)]
        [InlineData(double.NegativeInfinity, double.MinValue, -1)]
        [InlineData(-0d, double.NegativeInfinity, 1)]
        [InlineData(double.NegativeInfinity, -0d, -1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, 0)]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, -1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, 0)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, 1)]
        public static void CompareTo_Other_ReturnsExpected(double d1, object value, int expected)
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
        public static void Epsilon()
        {
            Assert.Equal(4.9406564584124654E-324, double.Epsilon);
            Assert.Equal(0x00000000_00000001u, BitConverter.DoubleToUInt64Bits(double.Epsilon));
        }

        [Theory]
        [InlineData(789.0, 789.0, true)]
        [InlineData(789.0, -789.0, false)]
        [InlineData(789.0, 0.0, false)]
        [InlineData(double.NaN, double.NaN, true)]
        [InlineData(double.NaN, -double.NaN, true)]
        [InlineData(789.0, 789.0f, false)]
        [InlineData(789.0, "789", false)]
        public static void EqualsTest(double d1, object value, bool expected)
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
        public static void GetTypeCode_Invoke_ReturnsDouble()
        {
            Assert.Equal(TypeCode.Double, 0.0.GetTypeCode());
        }

        [Theory]
        [InlineData(double.NaN,              double.NaN,              double.NaN,              0.0)]
        [InlineData(double.NaN,              0.0f,                    double.NaN,              0.0)]
        [InlineData(double.NaN,              1.0f,                    double.NaN,              0.0)]
        [InlineData(double.NaN,              2.7182818284590452,      double.NaN,              0.0)]
        [InlineData(double.NaN,              10.0,                    double.NaN,              0.0)]
        [InlineData(0.0,                     0.0,                     0.0,                     0.0)]
        [InlineData(0.0,                     1.0,                     1.0,                     0.0)]
        [InlineData(0.0,                     1.5707963267948966,      1.5707963267948966,      0.0)]
        [InlineData(0.0,                     2.0,                     2.0,                     0.0)]
        [InlineData(0.0,                     2.7182818284590452,      2.7182818284590452,      0.0)]
        [InlineData(0.0,                     3.0,                     3.0,                     0.0)]
        [InlineData(0.0,                     10.0,                    10.0,                    0.0)]
        [InlineData(1.0,                     1.0,                     1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.0,                     1e+10,                   1e+10,                   0.0)] // dotnet/runtime#75651
        [InlineData(1.0,                     1e+20,                   1e+20,                   0.0)] // dotnet/runtime#75651
        [InlineData(2.7182818284590452,      0.31830988618379067,     2.7368553638387594,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (1 / pi)
        [InlineData(2.7182818284590452,      0.43429448190325183,     2.7527563996732919,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (log10(e))
        [InlineData(2.7182818284590452,      0.63661977236758134,     2.7918346715914253,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (2 / pi)
        [InlineData(2.7182818284590452,      0.69314718055994531,     2.8052645352709344,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (ln(2))
        [InlineData(2.7182818284590452,      0.70710678118654752,     2.8087463571726533,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (1 / sqrt(2))
        [InlineData(2.7182818284590452,      0.78539816339744831,     2.8294710413783590,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (pi / 4)
        [InlineData(2.7182818284590452,      1.0,                     2.8963867315900082,      CrossPlatformMachineEpsilon * 10)]   // x: (e)
        [InlineData(2.7182818284590452,      1.1283791670955126,      2.9431778138036127,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (2 / sqrt(pi))
        [InlineData(2.7182818284590452,      1.4142135623730950,      3.0641566701020120,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (sqrt(2))
        [InlineData(2.7182818284590452,      1.4426950408889634,      3.0774055761202907,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (log2(e))
        [InlineData(2.7182818284590452,      1.5707963267948966,      3.1394995141268918,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (pi / 2)
        [InlineData(2.7182818284590452,      2.3025850929940457,      3.5624365551415857,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (ln(10))
        [InlineData(2.7182818284590452,      2.7182818284590452,      3.8442310281591168,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (e)
        [InlineData(2.7182818284590452,      3.1415926535897932,      4.1543544023133136,      CrossPlatformMachineEpsilon * 10)]   // x: (e)   y: (pi)
        [InlineData(10.0,                    0.31830988618379067,     10.005064776584025,      CrossPlatformMachineEpsilon * 100)]  //          y: (1 / pi)
        [InlineData(10.0,                    0.43429448190325183,     10.009426142242702,      CrossPlatformMachineEpsilon * 100)]  //          y: (log10(e))
        [InlineData(10.0,                    0.63661977236758134,     10.020243746265325,      CrossPlatformMachineEpsilon * 100)]  //          y: (2 / pi)
        [InlineData(10.0,                    0.69314718055994531,     10.023993865417028,      CrossPlatformMachineEpsilon * 100)]  //          y: (ln(2))
        [InlineData(10.0,                    0.70710678118654752,     10.024968827881711,      CrossPlatformMachineEpsilon * 100)]  //          y: (1 / sqrt(2))
        [InlineData(10.0,                    0.78539816339744831,     10.030795096853892,      CrossPlatformMachineEpsilon * 100)]  //          y: (pi / 4)
        [InlineData(10.0,                    1.0,                     10.049875621120890,      CrossPlatformMachineEpsilon * 100)]  //
        [InlineData(10.0,                    1.1283791670955126,      10.063460614755501,      CrossPlatformMachineEpsilon * 100)]  //          y: (2 / sqrt(pi))
        [InlineData(10.0,                    1.4142135623730950,      10.099504938362078,      CrossPlatformMachineEpsilon * 100)]  //          y: (sqrt(2))
        [InlineData(10.0,                    1.4426950408889634,      10.103532500121213,      CrossPlatformMachineEpsilon * 100)]  //          y: (log2(e))
        [InlineData(10.0,                    1.5707963267948966,      10.122618292728040,      CrossPlatformMachineEpsilon * 100)]  //          y: (pi / 2)
        [InlineData(10.0,                    2.3025850929940457,      10.261671311754163,      CrossPlatformMachineEpsilon * 100)]  //          y: (ln(10))
        [InlineData(10.0,                    2.7182818284590452,      10.362869105558106,      CrossPlatformMachineEpsilon * 100)]  //          y: (e)
        [InlineData(10.0,                    3.1415926535897932,      10.481870272097884,      CrossPlatformMachineEpsilon * 100)]  //          y: (pi)
        [InlineData(double.PositiveInfinity, double.NaN,              double.PositiveInfinity, 0.0)]
        [InlineData(double.PositiveInfinity, 0.0,                     double.PositiveInfinity, 0.0)]
        [InlineData(double.PositiveInfinity, 1.0,                     double.PositiveInfinity, 0.0)]
        [InlineData(double.PositiveInfinity, 2.7182818284590452,      double.PositiveInfinity, 0.0)]
        [InlineData(double.PositiveInfinity, 10.0,                    double.PositiveInfinity, 0.0)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, 0.0)]
        public static void Hypot(double x, double y, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, double.Hypot(-x, -y), allowedVariance);
            AssertExtensions.Equal(expectedResult, double.Hypot(-x, +y), allowedVariance);
            AssertExtensions.Equal(expectedResult, double.Hypot(+x, -y), allowedVariance);
            AssertExtensions.Equal(expectedResult, double.Hypot(+x, +y), allowedVariance);

            AssertExtensions.Equal(expectedResult, double.Hypot(-y, -x), allowedVariance);
            AssertExtensions.Equal(expectedResult, double.Hypot(-y, +x), allowedVariance);
            AssertExtensions.Equal(expectedResult, double.Hypot(+y, -x), allowedVariance);
            AssertExtensions.Equal(expectedResult, double.Hypot(+y, +x), allowedVariance);
        }

        [Theory]
        [InlineData(double.NegativeInfinity, true)]     // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-double.Epsilon, false)]            // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(double.Epsilon, false)]             // Min Positive Subnormal (Positive Epsilon)
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, true)]     // Positive Infinity
        public static void IsInfinity(double d, bool expected)
        {
            Assert.Equal(expected, double.IsInfinity(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-double.Epsilon, false)]            // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, true)]                  // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(double.Epsilon, false)]             // Min Positive Subnormal (Positive Epsilon)
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsNaN(double d, bool expected)
        {
            Assert.Equal(expected, double.IsNaN(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, true)]     // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-double.Epsilon, false)]            // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(double.Epsilon, false)]             // Min Positive Subnormal (Positive Epsilon)
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsNegativeInfinity(double d, bool expected)
        {
            Assert.Equal(expected, double.IsNegativeInfinity(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-double.Epsilon, false)]            // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(double.Epsilon, false)]             // Min Positive Subnormal (Positive Epsilon)
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, true)]     // Positive Infinity
        public static void IsPositiveInfinity(double d, bool expected)
        {
            Assert.Equal(expected, double.IsPositiveInfinity(d));
        }

        [Fact]
        public static void MaxValue()
        {
            Assert.Equal(1.7976931348623157E+308, double.MaxValue);
            Assert.Equal(0x7FEFFFFF_FFFFFFFFu, BitConverter.DoubleToUInt64Bits(double.MaxValue));
        }

        [Fact]
        public static void MinValue()
        {
            Assert.Equal(-1.7976931348623157E+308, double.MinValue);
            Assert.Equal(0xFFEFFFFF_FFFFFFFFu, BitConverter.DoubleToUInt64Bits(double.MinValue));
        }

        [Fact]
        public static void NaN()
        {
            Assert.Equal(0.0 / 0.0, double.NaN);
            Assert.Equal(0xFFF80000_00000000u, BitConverter.DoubleToUInt64Bits(double.NaN));
        }

        [Fact]
        public static void NegativeInfinity()
        {
            Assert.Equal(-1.0 / 0.0, double.NegativeInfinity);
            Assert.Equal(0xFFF00000_00000000u, BitConverter.DoubleToUInt64Bits(double.NegativeInfinity));
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

            yield return new object[] { "-123", defaultStyle, null, -123.0 };
            yield return new object[] { "0", defaultStyle, null, 0.0 };
            yield return new object[] { "123", defaultStyle, null, 123.0 };
            yield return new object[] { "  123  ", defaultStyle, null, 123.0 };
            yield return new object[] { (567.89).ToString(), defaultStyle, null, 567.89 };
            yield return new object[] { (-567.89).ToString(), defaultStyle, null, -567.89 };
            yield return new object[] { "1E23", defaultStyle, null, 1E23 };
            yield return new object[] { "9007199254740997.0", defaultStyle, invariantFormat, 9007199254740996.0 };
            yield return new object[] { "9007199254740997.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 9007199254740996.0 };
            yield return new object[] { "9007199254740997.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 9007199254740996.0 };
            yield return new object[] { "9007199254740997.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, 9007199254740998.0 };
            yield return new object[] { "9007199254740997.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, 9007199254740998.0 };
            yield return new object[] { "9007199254740997.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001", defaultStyle, invariantFormat, 9007199254740998.0 };
            yield return new object[] { "5.005", defaultStyle, invariantFormat, 5.005 };
            yield return new object[] { "5.050", defaultStyle, invariantFormat, 5.05 };
            yield return new object[] { "5.005000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 5.005 };
            yield return new object[] { "5.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005", defaultStyle, invariantFormat, 5.0 };
            yield return new object[] { "5.0050000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 5.005 };

            yield return new object[] { emptyFormat.NumberDecimalSeparator + "234", defaultStyle, null, 0.234 };
            yield return new object[] { "234" + emptyFormat.NumberDecimalSeparator, defaultStyle, null, 234.0 };
            yield return new object[] { new string('0', 458) + "1" + new string('0', 308) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, 1E308 };
            yield return new object[] { new string('0', 459) + "1" + new string('0', 308) + emptyFormat.NumberDecimalSeparator, defaultStyle, null, 1E308 };

            yield return new object[] { "5005.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 5005.0 };
            yield return new object[] { "50050.0", defaultStyle, invariantFormat, 50050.0 };
            yield return new object[] { "5005", defaultStyle, invariantFormat, 5005.0 };
            yield return new object[] { "050050", defaultStyle, invariantFormat, 50050.0 };
            yield return new object[] { "0.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", defaultStyle, invariantFormat, 0.0 };
            yield return new object[] { "0.005", defaultStyle, invariantFormat, 0.005 };
            yield return new object[] { "0.0500", defaultStyle, invariantFormat, 0.05 };
            yield return new object[] { "6250000000000000000000000000000000e-12", defaultStyle, invariantFormat, 6.25e21 };
            yield return new object[] { "6250000e0", defaultStyle, invariantFormat, 6.25e6 };
            yield return new object[] { "6250100e-5", defaultStyle, invariantFormat, 62.501 };
            yield return new object[] { "625010.00e-4", defaultStyle, invariantFormat, 62.501 };
            yield return new object[] { "62500e-4", defaultStyle, invariantFormat, 6.25 };
            yield return new object[] { "62500", defaultStyle, invariantFormat, 62500.0 };
            yield return new object[] { "10e-3", defaultStyle, invariantFormat, 0.01 };

            yield return new object[] { (123.1).ToString(), NumberStyles.AllowDecimalPoint, null, 123.1 };
            yield return new object[] { (1000.0).ToString("N0"), NumberStyles.AllowThousands, null, 1000.0 };

            yield return new object[] { "123", NumberStyles.Any, emptyFormat, 123.0 };
            yield return new object[] { (123.567).ToString(), NumberStyles.Any, emptyFormat, 123.567 };
            yield return new object[] { "123", NumberStyles.Float, emptyFormat, 123.0 };
            yield return new object[] { "$1,000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, 1000.0 };
            yield return new object[] { "$1000", NumberStyles.Currency, dollarSignCommaSeparatorFormat, 1000.0 };
            yield return new object[] { "123.123", NumberStyles.Float, decimalSeparatorFormat, 123.123 };
            yield return new object[] { "(123)", NumberStyles.AllowParentheses, decimalSeparatorFormat, -123.0 };

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
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
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

        internal static string SplitPairs(string input)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return input.Replace("-", "");
            }

            return string.Concat(input.Split('-').Select(pair => Reverse(pair)));
        }

        internal static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public static void ParsePatterns()
        {
            string path = Directory.GetCurrentDirectory();
            using (FileStream file = new FileStream(Path.Combine(path, "ibm-fpgen.txt"), FileMode.Open, FileAccess.Read))
            {
                using (var streamReader = new StreamReader(file))
                {
                    string line = streamReader.ReadLine();
                    while (line != null)
                    {
                        string[] data = line.Split(' ');
                        string inputHalfBytes = data[0];
                        string inputFloatBytes = data[1];
                        string inputDoubleBytes = data[2];
                        string correctValue = data[3];

                        double doubleValue = double.Parse(correctValue, NumberFormatInfo.InvariantInfo);
                        string doubleBytes = BitConverter.ToString(BitConverter.GetBytes(doubleValue));
                        float floatValue = float.Parse(correctValue, NumberFormatInfo.InvariantInfo);
                        string floatBytes = BitConverter.ToString(BitConverter.GetBytes(floatValue));
                        Half halfValue = Half.Parse(correctValue, NumberFormatInfo.InvariantInfo);
                        string halfBytes = BitConverter.ToString(BitConverter.GetBytes(halfValue));

                        doubleBytes = SplitPairs(doubleBytes);
                        floatBytes = SplitPairs(floatBytes);
                        halfBytes = SplitPairs(halfBytes);

                        if (BitConverter.IsLittleEndian)
                        {
                            doubleBytes = Reverse(doubleBytes);
                            floatBytes = Reverse(floatBytes);
                            halfBytes = Reverse(halfBytes);
                        }

                        Assert.Equal(doubleBytes, inputDoubleBytes);
                        Assert.Equal(floatBytes, inputFloatBytes);
                        Assert.Equal(halfBytes, inputHalfBytes);
                        line = streamReader.ReadLine();
                    }
                }
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
            yield return new object[] { (100.0).ToString("C0"), defaultStyle, null, typeof(FormatException) }; // Currency

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
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None && (style & NumberStyles.AllowLeadingWhite) == (style & NumberStyles.AllowTrailingWhite))
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

        public static IEnumerable<object[]> Parse_ValidWithOffsetCount_TestData()
        {
            foreach (object[] inputs in Parse_Valid_TestData())
            {
                yield return new object[] { inputs[0], 0, ((string)inputs[0]).Length, inputs[1], inputs[2], inputs[3] };
            }

            const NumberStyles DefaultStyle = NumberStyles.Float | NumberStyles.AllowThousands;
            yield return new object[] { "-123", 0, 3, DefaultStyle, null, (double)-12 };
            yield return new object[] { "-123", 1, 3, DefaultStyle, null, (double)123 };
            yield return new object[] { "1E23", 0, 3, DefaultStyle, null, 1E2 };
            yield return new object[] { "(123)", 1, 3, NumberStyles.AllowParentheses, new NumberFormatInfo() { NumberDecimalSeparator = "." }, 123 };
            yield return new object[] { "-Infinity", 1, 8, NumberStyles.Any, NumberFormatInfo.InvariantInfo, double.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(Parse_ValidWithOffsetCount_TestData))]
        public static void Parse_Span_Valid(string value, int offset, int count, NumberStyles style, IFormatProvider provider, double expected)
        {
            bool isDefaultProvider = provider == null || provider == NumberFormatInfo.CurrentInfo;
            double result;
            if ((style & ~(NumberStyles.Float | NumberStyles.AllowThousands)) == 0 && style != NumberStyles.None)
            {
                // Use Parse(string) or Parse(string, IFormatProvider)
                if (isDefaultProvider)
                {
                    Assert.True(double.TryParse(value.AsSpan(offset, count), out result));
                    Assert.Equal(expected, result);

                    Assert.Equal(expected, double.Parse(value.AsSpan(offset, count)));
                }

                Assert.Equal(expected, double.Parse(value.AsSpan(offset, count), provider: provider));
            }

            Assert.Equal(expected, double.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(double.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Span_Invalid(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value != null)
            {
                Assert.Throws(exceptionType, () => double.Parse(value.AsSpan(), style, provider));

                Assert.False(double.TryParse(value.AsSpan(), style, provider, out double result));
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public static void PositiveInfinity()
        {
            Assert.Equal(1.0 / 0.0, double.PositiveInfinity);
            Assert.Equal(0x7FF00000_00000000u, BitConverter.DoubleToUInt64Bits(double.PositiveInfinity));
        }

        [Theory]
        [InlineData( double.NegativeInfinity, -5, -0.0,                     0.0)]
        [InlineData( double.NegativeInfinity, -4,  double.NaN,              0.0)]
        [InlineData( double.NegativeInfinity, -3, -0.0,                     0.0)]
        [InlineData( double.NegativeInfinity, -2,  double.NaN,              0.0)]
        [InlineData( double.NegativeInfinity, -1, -0.0,                     0.0)]
        [InlineData( double.NegativeInfinity,  0,  double.NaN,              0.0)]
        [InlineData( double.NegativeInfinity,  1,  double.NegativeInfinity, 0.0)]
        [InlineData( double.NegativeInfinity,  2,  double.NaN,              0.0)]
        [InlineData( double.NegativeInfinity,  3,  double.NegativeInfinity, 0.0)]
        [InlineData( double.NegativeInfinity,  4,  double.NaN,              0.0)]
        [InlineData( double.NegativeInfinity,  5,  double.NegativeInfinity, 0.0)]
        [InlineData(-2.7182818284590452,      -5, -0.8187307530779819,      CrossPlatformMachineEpsilon * 10)]
        [InlineData(-2.7182818284590452,      -4,  double.NaN,              0.0)]
        [InlineData(-2.7182818284590452,      -3, -0.7165313105737893,      CrossPlatformMachineEpsilon * 10)]
        [InlineData(-2.7182818284590452,      -2,  double.NaN,              0.0)]
        [InlineData(-2.7182818284590452,      -1, -0.3678794411714423,      CrossPlatformMachineEpsilon * 10)]
        [InlineData(-2.7182818284590452,       0,  double.NaN,              0.0)]
        [InlineData(-2.7182818284590452,       1, -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]
        [InlineData(-2.7182818284590452,       2,  double.NaN,              0.0)]
        [InlineData(-2.7182818284590452,       3, -1.3956124250860895,      CrossPlatformMachineEpsilon * 10)]
        [InlineData(-2.7182818284590452,       4,  double.NaN,              0.0)]
        [InlineData(-2.7182818284590452,       5, -1.2214027581601698,      CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0,                     -5, -1.0,                     0.0)]
        [InlineData(-1.0,                     -4,  double.NaN,              0.0)]
        [InlineData(-1.0,                     -3, -1.0,                     0.0)]
        [InlineData(-1.0,                     -2,  double.NaN,              0.0)]
        [InlineData(-1.0,                     -1, -1.0,                     0.0)]
        [InlineData(-1.0,                      0,  double.NaN,              0.0)]
        [InlineData(-1.0,                      1, -1.0,                     0.0)]
        [InlineData(-1.0,                      2,  double.NaN,              0.0)]
        [InlineData(-1.0,                      3, -1.0,                     0.0)]
        [InlineData(-1.0,                      4,  double.NaN,              0.0)]
        [InlineData(-1.0,                      5, -1.0,                     0.0)]
        [InlineData(-0.0,                     -5,  double.NegativeInfinity, 0.0)]
        [InlineData(-0.0,                     -4,  double.PositiveInfinity, 0.0)]
        [InlineData(-0.0,                     -3,  double.NegativeInfinity, 0.0)]
        [InlineData(-0.0,                     -2,  double.PositiveInfinity, 0.0)]
        [InlineData(-0.0,                     -1,  double.NegativeInfinity, 0.0)]
        [InlineData(-0.0,                      0,  double.NaN,              0.0)]
        [InlineData(-0.0,                      1, -0.0,                     0.0)]
        [InlineData(-0.0,                      2,  0.0,                     0.0)]
        [InlineData(-0.0,                      3, -0.0,                     0.0)]
        [InlineData(-0.0,                      4,  0.0,                     0.0)]
        [InlineData(-0.0,                      5, -0.0,                     0.0)]
        [InlineData( double.NaN,              -5,  double.NaN,              0.0)]
        [InlineData( double.NaN,              -4,  double.NaN,              0.0)]
        [InlineData( double.NaN,              -3,  double.NaN,              0.0)]
        [InlineData( double.NaN,              -2,  double.NaN,              0.0)]
        [InlineData( double.NaN,              -1,  double.NaN,              0.0)]
        [InlineData( double.NaN,               0,  double.NaN,              0.0)]
        [InlineData( double.NaN,               1,  double.NaN,              0.0)]
        [InlineData( double.NaN,               2,  double.NaN,              0.0)]
        [InlineData( double.NaN,               3,  double.NaN,              0.0)]
        [InlineData( double.NaN,               4,  double.NaN,              0.0)]
        [InlineData( double.NaN,               5,  double.NaN,              0.0)]
        [InlineData( 0.0,                     -5,  double.PositiveInfinity, 0.0)]
        [InlineData( 0.0,                     -4,  double.PositiveInfinity, 0.0)]
        [InlineData( 0.0,                     -3,  double.PositiveInfinity, 0.0)]
        [InlineData( 0.0,                     -2,  double.PositiveInfinity, 0.0)]
        [InlineData( 0.0,                     -1,  double.PositiveInfinity, 0.0)]
        [InlineData( 0.0,                      0,  double.NaN,              0.0)]
        [InlineData( 0.0,                      1,  0.0,                     0.0)]
        [InlineData( 0.0,                      2,  0.0,                     0.0)]
        [InlineData( 0.0,                      3,  0.0,                     0.0)]
        [InlineData( 0.0,                      4,  0.0,                     0.0)]
        [InlineData( 0.0,                      5,  0.0,                     0.0)]
        [InlineData( 1.0,                     -5,  1.0,                     0.0)]
        [InlineData( 1.0,                     -4,  1.0,                     0.0)]
        [InlineData( 1.0,                     -3,  1.0,                     0.0)]
        [InlineData( 1.0,                     -2,  1.0,                     0.0)]
        [InlineData( 1.0,                     -1,  1.0,                     0.0)]
        [InlineData( 1.0,                      0,  double.NaN,              0.0)]
        [InlineData( 1.0,                      1,  1.0,                     0.0)]
        [InlineData( 1.0,                      2,  1.0,                     0.0)]
        [InlineData( 1.0,                      3,  1.0,                     0.0)]
        [InlineData( 1.0,                      4,  1.0,                     0.0)]
        [InlineData( 1.0,                      5,  1.0,                     0.0)]
        [InlineData( 2.7182818284590452,      -5,  0.8187307530779819,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.7182818284590452,      -4,  0.7788007830714049,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.7182818284590452,      -3,  0.7165313105737893,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.7182818284590452,      -2,  0.6065306597126334,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.7182818284590452,      -1,  0.3678794411714423,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.7182818284590452,       0,  double.NaN,              0.0)]
        [InlineData( 2.7182818284590452,       1,  2.7182818284590452,      0.0)]
        [InlineData( 2.7182818284590452,       2,  1.6487212707001281,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.7182818284590452,       3,  1.3956124250860895,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.7182818284590452,       4,  1.2840254166877415,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.7182818284590452,       5,  1.2214027581601698,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( double.PositiveInfinity, -5,  0.0f,                    0.0)]
        [InlineData( double.PositiveInfinity, -4,  0.0f,                    0.0)]
        [InlineData( double.PositiveInfinity, -3,  0.0f,                    0.0)]
        [InlineData( double.PositiveInfinity, -2,  0.0f,                    0.0)]
        [InlineData( double.PositiveInfinity, -1,  0.0f,                    0.0)]
        [InlineData( double.PositiveInfinity,  0,  double.NaN,              0.0)]
        [InlineData( double.PositiveInfinity,  1,  double.PositiveInfinity, 0.0)]
        [InlineData( double.PositiveInfinity,  2,  double.PositiveInfinity, 0.0)]
        [InlineData( double.PositiveInfinity,  3,  double.PositiveInfinity, 0.0)]
        [InlineData( double.PositiveInfinity,  4,  double.PositiveInfinity, 0.0)]
        [InlineData( double.PositiveInfinity,  5,  double.PositiveInfinity, 0.0)]
        public static void RootN(double x, int n, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, double.RootN(x, n), allowedVariance);
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { -4567.0, "G", null, "-4567" };
            yield return new object[] { -4567.89101, "G", null, "-4567.89101" };
            yield return new object[] { 0.0, "G", null, "0" };
            yield return new object[] { 4567.0, "G", null, "4567" };
            yield return new object[] { 4567.89101, "G", null, "4567.89101" };

            yield return new object[] { double.NaN, "G", null, "NaN" };

            yield return new object[] { 2468.0, "N", null, "2,468.00" };

            // Changing the negative pattern doesn't do anything without also passing in a format string
            var customNegativePattern = new NumberFormatInfo() { NumberNegativePattern = 0 };
            yield return new object[] { -6310.0, "G", customNegativePattern, "-6310" };

            var customNegativeSignDecimalGroupSeparator = new NumberFormatInfo()
            {
                NegativeSign = "#",
                NumberDecimalSeparator = "~",
                NumberGroupSeparator = "*"
            };
            yield return new object[] { -2468.0, "N", customNegativeSignDecimalGroupSeparator, "#2*468~00" };
            yield return new object[] { 2468.0, "N", customNegativeSignDecimalGroupSeparator, "2*468~00" };

            var customNegativeSignGroupSeparatorNegativePattern = new NumberFormatInfo()
            {
                NegativeSign = "xx", // Set to trash to make sure it doesn't show up
                NumberGroupSeparator = "*",
                NumberNegativePattern = 0,
            };
            yield return new object[] { -2468.0, "N", customNegativeSignGroupSeparatorNegativePattern, "(2*468.00)" };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { double.NaN, "G", invariantFormat, "NaN" };
            yield return new object[] { double.PositiveInfinity, "G", invariantFormat, "Infinity" };
            yield return new object[] { double.NegativeInfinity, "G", invariantFormat, "-Infinity" };
        }

        public static IEnumerable<object[]> ToString_TestData_NotNetFramework()
        {
            foreach (var testData in ToString_TestData())
            {
                yield return testData;
            }


            yield return new object[] { double.MinValue, "G", null, "-1.7976931348623157E+308" };
            yield return new object[] { double.MaxValue, "G", null, "1.7976931348623157E+308" };

            yield return new object[] { double.Epsilon, "G", null, "5E-324" };

            NumberFormatInfo invariantFormat = NumberFormatInfo.InvariantInfo;
            yield return new object[] { double.Epsilon, "G", invariantFormat, "5E-324" };
            yield return new object[] { 32.5, "C100", invariantFormat, "\u00A432.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { 32.5, "P100", invariantFormat, "3,250.0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 %" };
            yield return new object[] { 32.5, "E100", invariantFormat, "3.2500000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E+001" };
            yield return new object[] { 32.5, "F100", invariantFormat, "32.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
            yield return new object[] { 32.5, "N100", invariantFormat, "32.5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" };
        }

        [Fact]
        public static void Test_ToString_NotNetFramework()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (object[] testdata in ToString_TestData_NotNetFramework())
                {
                    ToString((double)testdata[0], (string)testdata[1], (IFormatProvider)testdata[2], (string)testdata[3]);
                }
            }
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
            double d = 123.0;
            Assert.Throws<FormatException>(() => d.ToString("Y")); // Invalid format
            Assert.Throws<FormatException>(() => d.ToString("Y", null)); // Invalid format

            // Format precision limit is 999_999_999 (9 digits). Anything larger should throw.
            Assert.Throws<FormatException>(() => d.ToString("E" + int.MaxValue.ToString()));
            long intMaxPlus1 = (long)int.MaxValue + 1;
            string intMaxPlus1String = intMaxPlus1.ToString();
            Assert.Throws<FormatException>(() => d.ToString("E" + intMaxPlus1String));
            Assert.Throws<FormatException>(() => d.ToString("E4772185890"));
            Assert.Throws<FormatException>(() => d.ToString("E1000000000"));
            Assert.Throws<FormatException>(() => d.ToString("E000001000000000"));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))] // Requires a lot of memory
        [OuterLoop("Takes a long time, allocates a lot of memory")]
        public static void ToString_ValidLargeFormat()
        {
            double d = 123.0;

            // Format precision limit is 999_999_999 (9 digits). Anything larger should throw.
            d.ToString("E999999999"); // Should not throw
            d.ToString("E00000999999999"); // Should not throw
        }

        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, true)]             // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, true)]    // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, true)]    // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, true)]      // Max Negative Subnormal
        [InlineData(-0.0, true)]                        // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, true)]                         // Positive Zero
        [InlineData(4.94065645841247E-324, true)]       // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, true)]     // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, true)]     // Min Positive Normal
        [InlineData(double.MaxValue, true)]             // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsFinite(double d, bool expected)
        {
            Assert.Equal(expected, double.IsFinite(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, true)]     // Negative Infinity
        [InlineData(double.MinValue, true)]             // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, true)]    // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, true)]    // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, true)]      // Max Negative Subnormal
        [InlineData(-0.0, true)]                        // Negative Zero
        [InlineData(double.NaN, true)]                  // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(4.94065645841247E-324, false)]      // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsNegative(double d, bool expected)
        {
            Assert.Equal(expected, double.IsNegative(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, true)]             // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, true)]    // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, false)]   // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, false)]     // Max Negative Subnormal
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(4.94065645841247E-324, false)]      // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, false)]    // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, true)]     // Min Positive Normal
        [InlineData(double.MaxValue, true)]             // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsNormal(double d, bool expected)
        {
            Assert.Equal(expected, double.IsNormal(d));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, false)]    // Negative Infinity
        [InlineData(double.MinValue, false)]            // Min Negative Normal
        [InlineData(-2.2250738585072014E-308, false)]   // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, true)]    // Min Negative Subnormal
        [InlineData(-4.94065645841247E-324, true)]      // Max Negative Subnormal
        [InlineData(-0.0, false)]                       // Negative Zero
        [InlineData(double.NaN, false)]                 // NaN
        [InlineData(0.0, false)]                        // Positive Zero
        [InlineData(4.94065645841247E-324, true)]       // Min Positive Subnormal
        [InlineData(2.2250738585072009E-308, true)]     // Max Positive Subnormal
        [InlineData(2.2250738585072014E-308, false)]    // Min Positive Normal
        [InlineData(double.MaxValue, false)]            // Max Positive Normal
        [InlineData(double.PositiveInfinity, false)]    // Positive Infinity
        public static void IsSubnormal(double d, bool expected)
        {
            Assert.Equal(expected, double.IsSubnormal(d));
        }

        [Fact]
        public static void TryFormat()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (var testdata in ToString_TestData_NotNetFramework())
                {
                    double localI = (double)testdata[0];
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
            yield return new object[] { double.NegativeInfinity };
            yield return new object[] { double.MinValue };
            yield return new object[] { -Math.PI };
            yield return new object[] { -Math.E };
            yield return new object[] { -double.Epsilon };
            yield return new object[] { -0.84551240822557006 };
            yield return new object[] { -0.0 };
            yield return new object[] { double.NaN };
            yield return new object[] { 0.0 };
            yield return new object[] { 0.84551240822557006 };
            yield return new object[] { double.Epsilon };
            yield return new object[] { Math.E };
            yield return new object[] { Math.PI };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.PositiveInfinity };
        }

        [Theory]
        [MemberData(nameof(ToStringRoundtrip_TestData))]
        public static void ToStringRoundtrip(double value)
        {
            double result = double.Parse(value.ToString());
            Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(result));
        }

        [Theory]
        [MemberData(nameof(ToStringRoundtrip_TestData))]
        public static void ToStringRoundtrip_R(double value)
        {
            double result = double.Parse(value.ToString("R"));
            Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(result));
        }

        [Fact]
        public static void TestNegativeNumberParsingWithHyphen()
        {
            // CLDR data for Swedish culture has negative sign U+2212. This test ensure parsing with the hyphen with such cultures will succeed.
            CultureInfo ci = CultureInfo.GetCultureInfo("sv-SE");
            string s = string.Format(ci, "{0}", 158.68);
            Assert.Equal(-158.68, double.Parse("-" + s, NumberStyles.Number, ci));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.PositiveInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MaxValue)]
        [InlineData(double.MaxValue, double.MinValue, double.MaxValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1.0, 1.0)]
        [InlineData(1.0, double.NaN, 1.0)]
        [InlineData(double.PositiveInfinity, double.NaN, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NaN, double.NegativeInfinity)]
        [InlineData(double.NaN, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NaN, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(-0.0, 0.0, 0.0)]
        [InlineData(0.0, -0.0, 0.0)]
        [InlineData(2.0, -3.0, -3.0)]
        [InlineData(-3.0, 2.0, -3.0)]
        [InlineData(3.0, -2.0, 3.0)]
        [InlineData(-2.0, 3.0, 3.0)]
        public static void MaxMagnitudeNumberTest(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, double.MaxMagnitudeNumber(x, y), 0.0);
        }

        [Theory]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.PositiveInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MaxValue)]
        [InlineData(double.MaxValue, double.MinValue, double.MaxValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1.0, 1.0)]
        [InlineData(1.0, double.NaN, 1.0)]
        [InlineData(double.PositiveInfinity, double.NaN, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NaN, double.NegativeInfinity)]
        [InlineData(double.NaN, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NaN, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(-0.0, 0.0, 0.0)]
        [InlineData(0.0, -0.0, 0.0)]
        [InlineData(2.0, -3.0, 2.0)]
        [InlineData(-3.0, 2.0, 2.0)]
        [InlineData(3.0, -2.0, 3.0)]
        [InlineData(-2.0, 3.0, 3.0)]
        public static void MaxNumberTest(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, double.MaxNumber(x, y), 0.0);
        }

        [Theory]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MinValue)]
        [InlineData(double.MaxValue, double.MinValue, double.MinValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1.0, 1.0)]
        [InlineData(1.0, double.NaN, 1.0)]
        [InlineData(double.PositiveInfinity, double.NaN, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NaN, double.NegativeInfinity)]
        [InlineData(double.NaN, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NaN, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(-0.0, 0.0, -0.0)]
        [InlineData(0.0, -0.0, -0.0)]
        [InlineData(2.0, -3.0, 2.0)]
        [InlineData(-3.0, 2.0, 2.0)]
        [InlineData(3.0, -2.0, -2.0)]
        [InlineData(-2.0, 3.0, -2.0)]
        public static void MinMagnitudeNumberTest(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, double.MinMagnitudeNumber(x, y), 0.0);
        }

        [Theory]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MinValue)]
        [InlineData(double.MaxValue, double.MinValue, double.MinValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1.0, 1.0)]
        [InlineData(1.0, double.NaN, 1.0)]
        [InlineData(double.PositiveInfinity, double.NaN, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NaN, double.NegativeInfinity)]
        [InlineData(double.NaN, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NaN, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(-0.0, 0.0, -0.0)]
        [InlineData(0.0, -0.0, -0.0)]
        [InlineData(2.0, -3.0, -3.0)]
        [InlineData(-3.0, 2.0, -3.0)]
        [InlineData(3.0, -2.0, -2.0)]
        [InlineData(-2.0, 3.0, -2.0)]
        public static void MinNumberTest(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, double.MinNumber(x, y), 0.0);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData(-3.1415926535897932,      -0.95678608173622775,     CrossPlatformMachineEpsilon)]        // value: -(pi)
        [InlineData(-2.7182818284590452,      -0.93401196415468746,     CrossPlatformMachineEpsilon)]        // value: -(e)
        [InlineData(-2.3025850929940457,      -0.9,                     CrossPlatformMachineEpsilon)]        // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -0.79212042364923809,     CrossPlatformMachineEpsilon)]        // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -0.76370991165547730,     CrossPlatformMachineEpsilon)]        // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -0.75688326556578579,     CrossPlatformMachineEpsilon)]        // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -0.67644273609692890,     CrossPlatformMachineEpsilon)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -0.63212055882855768,     CrossPlatformMachineEpsilon)]
        [InlineData(-0.78539816339744831,     -0.54406187223400376,     CrossPlatformMachineEpsilon)]        // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.50693130860476021,     CrossPlatformMachineEpsilon)]        // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.5,                     CrossPlatformMachineEpsilon)]        // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.47092219173226465,     CrossPlatformMachineEpsilon)]        // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.35227851485819935,     CrossPlatformMachineEpsilon)]        // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.27262265070478353,     CrossPlatformMachineEpsilon)]        // value: -(1 / pi)
        [InlineData(-0.0,                      0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.31830988618379067,      0.37480222743935863,     CrossPlatformMachineEpsilon)]        // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.54387344397118114,     CrossPlatformMachineEpsilon)]        // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.89008116457222198,     CrossPlatformMachineEpsilon)]        // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      1.0,                     CrossPlatformMachineEpsilon * 10)]   // value:  (ln(2))
        [InlineData( 0.70710678118654752,      1.0281149816474725,      CrossPlatformMachineEpsilon * 10)]   // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      1.1932800507380155,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 4)
        [InlineData( 1.0,                      1.7182818284590452,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.1283791670955126,       2.0906430223107976,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       3.1132503787829275,      CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       3.2320861065570819,      CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,       3.8104773809653517,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       9.0,                     CrossPlatformMachineEpsilon * 10)]   // value:  (ln(10))
        [InlineData( 2.7182818284590452,       14.154262241479264,      CrossPlatformMachineEpsilon * 100)]  // value:  (e)
        [InlineData( 3.1415926535897932,       22.140692632779269,      CrossPlatformMachineEpsilon * 100)]  // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void ExpM1Test(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, double.ExpM1(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, 0.0,                     0.0)]
        [InlineData(-3.1415926535897932,      0.11331473229676087,     CrossPlatformMachineEpsilon)]        // value: -(pi)
        [InlineData(-2.7182818284590452,      0.15195522325791297,     CrossPlatformMachineEpsilon)]        // value: -(e)
        [InlineData(-2.3025850929940457,      0.20269956628651730,     CrossPlatformMachineEpsilon)]        // value: -(ln(10))
        [InlineData(-1.5707963267948966,      0.33662253682241906,     CrossPlatformMachineEpsilon)]        // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      0.36787944117144232,     CrossPlatformMachineEpsilon)]        // value: -(log2(e))
        [InlineData(-1.4142135623730950,      0.37521422724648177,     CrossPlatformMachineEpsilon)]        // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      0.45742934732229695,     CrossPlatformMachineEpsilon)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     0.5,                     CrossPlatformMachineEpsilon)]
        [InlineData(-0.78539816339744831,     0.58019181037172444,     CrossPlatformMachineEpsilon)]        // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     0.61254732653606592,     CrossPlatformMachineEpsilon)]        // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     0.61850313780157598,     CrossPlatformMachineEpsilon)]        // value: -(ln(2))
        [InlineData(-0.63661977236758134,     0.64321824193300488,     CrossPlatformMachineEpsilon)]        // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     0.74005557395545179,     CrossPlatformMachineEpsilon)]        // value: -(log10(e))
        [InlineData(-0.31830988618379067,     0.80200887896145195,     CrossPlatformMachineEpsilon)]        // value: -(1 / pi)
        [InlineData(-0.0,                     1.0,                     0.0)]
        [InlineData( double.NaN,              double.NaN,              0.0)]
        [InlineData( 0.0,                     1.0,                     0.0)]
        [InlineData( 0.31830988618379067,     1.2468689889006383,      CrossPlatformMachineEpsilon * 10)]   // value:  (1 / pi)
        [InlineData( 0.43429448190325183,     1.3512498725672672,      CrossPlatformMachineEpsilon * 10)]   // value:  (log10(e))
        [InlineData( 0.63661977236758134,     1.5546822754821001,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / pi)
        [InlineData( 0.69314718055994531,     1.6168066722416747,      CrossPlatformMachineEpsilon * 10)]   // value:  (ln(2))
        [InlineData( 0.70710678118654752,     1.6325269194381528,      CrossPlatformMachineEpsilon * 10)]   // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,     1.7235679341273495,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 4)
        [InlineData( 1.0,                     2.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.1283791670955126,      2.1861299583286618,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,      2.6651441426902252,      CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,      2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,      2.9706864235520193,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,      4.9334096679145963,      CrossPlatformMachineEpsilon * 10)]   // value:  (ln(10))
        [InlineData( 2.7182818284590452,      6.5808859910179210,      CrossPlatformMachineEpsilon * 10)]   // value:  (e)
        [InlineData( 3.1415926535897932,      8.8249778270762876,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi)
        [InlineData( double.PositiveInfinity, double.PositiveInfinity, 0.0)]
        public static void Exp2Test(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, double.Exp2(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, -1.0,                     0.0)]
        [InlineData(-3.1415926535897932,      -0.88668526770323913,     CrossPlatformMachineEpsilon)]        // value: -(pi)
        [InlineData(-2.7182818284590452,      -0.84804477674208703,     CrossPlatformMachineEpsilon)]        // value: -(e)
        [InlineData(-2.3025850929940457,      -0.79730043371348270,     CrossPlatformMachineEpsilon)]        // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -0.66337746317758094,     CrossPlatformMachineEpsilon)]        // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -0.63212055882855768,     CrossPlatformMachineEpsilon)]        // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -0.62478577275351823,     CrossPlatformMachineEpsilon)]        // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -0.54257065267770305,     CrossPlatformMachineEpsilon)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -0.5,                     CrossPlatformMachineEpsilon)]
        [InlineData(-0.78539816339744831,     -0.41980818962827556,     CrossPlatformMachineEpsilon)]        // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.38745267346393408,     CrossPlatformMachineEpsilon)]        // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.38149686219842402,     CrossPlatformMachineEpsilon)]        // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.35678175806699512,     CrossPlatformMachineEpsilon)]        // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.25994442604454821,     CrossPlatformMachineEpsilon)]        // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.19799112103854805,     CrossPlatformMachineEpsilon)]        // value: -(1 / pi)
        [InlineData(-0.0,                      0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.31830988618379067,      0.24686898890063831,     CrossPlatformMachineEpsilon)]        // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.35124987256726717,     CrossPlatformMachineEpsilon)]        // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.55468227548210009,     CrossPlatformMachineEpsilon)]        // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.61680667224167466,     CrossPlatformMachineEpsilon)]        // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.63252691943815284,     CrossPlatformMachineEpsilon)]        // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.72356793412734949,     CrossPlatformMachineEpsilon)]        // value:  (pi / 4)
        [InlineData( 1.0,                      1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.1283791670955126,       1.1861299583286618,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       1.6651441426902252,      CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       1.7182818284590452,      CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,       1.9706864235520193,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       3.9334096679145963,      CrossPlatformMachineEpsilon * 10)]   // value:  (ln(10))
        [InlineData( 2.7182818284590452,       5.5808859910179210,      CrossPlatformMachineEpsilon * 10)]   // value:  (e)
        [InlineData( 3.1415926535897932,       7.8249778270762876,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Exp2M1Test(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, double.Exp2M1(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  0.0,                     0.0)]
        [InlineData(-3.1415926535897932,       0.00072178415907472774,  CrossPlatformMachineEpsilon / 1000)]  // value: -(pi)
        [InlineData(-2.7182818284590452,       0.0019130141022243176,   CrossPlatformMachineEpsilon / 100)]   // value: -(e)
        [InlineData(-2.3025850929940457,       0.0049821282964407206,   CrossPlatformMachineEpsilon / 100)]   // value: -(ln(10))
        [InlineData(-1.5707963267948966,       0.026866041001136132,    CrossPlatformMachineEpsilon / 10)]    // value: -(pi / 2)
        [InlineData(-1.4426950408889634,       0.036083192820787210,    CrossPlatformMachineEpsilon / 10)]    // value: -(log2(e))
        [InlineData(-1.4142135623730950,       0.038528884700322026,    CrossPlatformMachineEpsilon / 10)]    // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,       0.074408205860642723,    CrossPlatformMachineEpsilon / 10)]    // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                      0.1,                     CrossPlatformMachineEpsilon)]
        [InlineData(-0.78539816339744831,      0.16390863613957665,     CrossPlatformMachineEpsilon)]         // value: -(pi / 4)
        [InlineData(-0.70710678118654752,      0.19628775993505562,     CrossPlatformMachineEpsilon)]         // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,      0.20269956628651730,     CrossPlatformMachineEpsilon)]         // value: -(ln(2))
        [InlineData(-0.63661977236758134,      0.23087676451600055,     CrossPlatformMachineEpsilon)]         // value: -(2 / pi)
        [InlineData(-0.43429448190325183,      0.36787944117144232,     CrossPlatformMachineEpsilon)]         // value: -(log10(e))
        [InlineData(-0.31830988618379067,      0.48049637305186868,     CrossPlatformMachineEpsilon)]         // value: -(1 / pi)
        [InlineData(-0.0,                      1.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      1.0,                     0.0)]
        [InlineData( 0.31830988618379067,      2.0811811619898573,      CrossPlatformMachineEpsilon * 10)]    // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]    // value:  (log10(e))
        [InlineData( 0.63661977236758134,      4.3313150290214525,      CrossPlatformMachineEpsilon * 10)]    // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      4.9334096679145963,      CrossPlatformMachineEpsilon * 10)]    // value:  (ln(2))
        [InlineData( 0.70710678118654752,      5.0945611704512962,      CrossPlatformMachineEpsilon * 10)]    // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      6.1009598002416937,      CrossPlatformMachineEpsilon * 10)]    // value:  (pi / 4)
        [InlineData( 1.0,                      10.0,                    CrossPlatformMachineEpsilon * 100)]
        [InlineData( 1.1283791670955126,       13.439377934644401,      CrossPlatformMachineEpsilon * 100)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       25.954553519470081,      CrossPlatformMachineEpsilon * 100)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       27.713733786437790,      CrossPlatformMachineEpsilon * 100)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,       37.221710484165167,      CrossPlatformMachineEpsilon * 100)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       200.71743249053009,      CrossPlatformMachineEpsilon * 1000)]  // value:  (ln(10))
        [InlineData( 2.7182818284590452,       522.73529967043665,      CrossPlatformMachineEpsilon * 1000)]  // value:  (e)
        [InlineData( 3.1415926535897932,       1385.4557313670111,      CrossPlatformMachineEpsilon * 10000)] // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Exp10Test(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, double.Exp10(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, -1.0,                     0.0)]
        [InlineData(-3.1415926535897932,      -0.99927821584092527,     CrossPlatformMachineEpsilon)]         // value: -(pi)
        [InlineData(-2.7182818284590452,      -0.99808698589777568,     CrossPlatformMachineEpsilon)]         // value: -(e)
        [InlineData(-2.3025850929940457,      -0.99501787170355928,     CrossPlatformMachineEpsilon)]         // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -0.97313395899886387,     CrossPlatformMachineEpsilon)]         // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -0.96391680717921279,     CrossPlatformMachineEpsilon)]         // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -0.96147111529967797,     CrossPlatformMachineEpsilon)]         // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -0.92559179413935728,     CrossPlatformMachineEpsilon)]         // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -0.9,                     CrossPlatformMachineEpsilon)]
        [InlineData(-0.78539816339744831,     -0.83609136386042335,     CrossPlatformMachineEpsilon)]         // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.80371224006494438,     CrossPlatformMachineEpsilon)]         // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.79730043371348270,     CrossPlatformMachineEpsilon)]         // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.76912323548399945,     CrossPlatformMachineEpsilon)]         // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.63212055882855768,     CrossPlatformMachineEpsilon)]         // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.51950362694813132,     CrossPlatformMachineEpsilon)]         // value: -(1 / pi)
        [InlineData(-0.0,                      0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.31830988618379067,      1.0811811619898573,      CrossPlatformMachineEpsilon * 10)]    // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      1.7182818284590452,      CrossPlatformMachineEpsilon * 10)]    // value:  (log10(e))
        [InlineData( 0.63661977236758134,      3.3313150290214525,      CrossPlatformMachineEpsilon * 10)]    // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      3.9334096679145963,      CrossPlatformMachineEpsilon * 10)]    // value:  (ln(2))
        [InlineData( 0.70710678118654752,      4.0945611704512962,      CrossPlatformMachineEpsilon * 10)]    // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      5.1009598002416937,      CrossPlatformMachineEpsilon * 10)]    // value:  (pi / 4)
        [InlineData( 1.0,                      9.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.1283791670955126,       12.439377934644401,      CrossPlatformMachineEpsilon * 100)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       24.954553519470081,      CrossPlatformMachineEpsilon * 100)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       26.713733786437790,      CrossPlatformMachineEpsilon * 100)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,       36.221710484165167,      CrossPlatformMachineEpsilon * 100)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       199.71743249053009,      CrossPlatformMachineEpsilon * 1000)]  // value:  (ln(10))
        [InlineData( 2.7182818284590452,       521.73529967043665,      CrossPlatformMachineEpsilon * 1000)]  // value:  (e)
        [InlineData( 3.1415926535897932,       1384.4557313670111,      CrossPlatformMachineEpsilon * 10000)] // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Exp10M1Test(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, double.Exp10M1(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, double.NaN,              0.0)]
        [InlineData(-3.1415926535897932,      double.NaN,              0.0)]                               //                              value: -(pi)
        [InlineData(-2.7182818284590452,      double.NaN,              0.0)]                               //                              value: -(e)
        [InlineData(-1.4142135623730950,      double.NaN,              0.0)]                               //                              value: -(sqrt(2))
        [InlineData( double.NaN,              double.NaN,              0.0)]
        [InlineData(-1.0,                     double.NegativeInfinity, 0.0)]
        [InlineData(-0.95678608173622775,    -3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData(-0.93401196415468746,    -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-0.9,                    -2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-0.79212042364923809,    -1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.76370991165547730,    -1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.75688326556578579,    -1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.67644273609692890,    -1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.63212055882855768,    -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.54406187223400376,    -0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.50693130860476021,    -0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.5,                    -0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.47092219173226465,    -0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.0,                     0.0,                     0.0)]
        [InlineData( 0.0,                     0.0,                     0.0)]
        [InlineData( 0.37480222743935863,     0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.54387344397118114,     0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.89008116457222198,     0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 1.0,                     0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 1.0281149816474725,      0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 1.1932800507380155,      0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 1.7182818284590452,      1.0,                     CrossPlatformMachineEpsilon * 10)]  //                              value: (e)
        [InlineData( 2.0906430223107976,      1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 3.1132503787829275,      1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 3.2320861065570819,      1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 3.8104773809653517,      1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 9.0,                     2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 14.154262241479264,      2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 22.140692632779269,      3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void LogP1Test(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, double.LogP1(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData(-1.0,                      double.NegativeInfinity, 0.0)]
        [InlineData(-0.88668526770323913,     -3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData(-0.84804477674208703,     -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-0.79730043371348270,     -2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-0.66337746317758094,     -1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.63212055882855768,     -1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.62478577275351823,     -1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.54257065267770305,     -1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.5,                     -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.41980818962827556,     -0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.38745267346393408,     -0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.38149686219842402,     -0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.35678175806699512,     -0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.25994442604454821,     -0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.19799112103854805,     -0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0,                      0.0,                     0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.24686898890063831,      0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.35124987256726717,      0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.55468227548210009,      0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 0.61680667224167466,      0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 0.63252691943815284,      0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 0.72356793412734949,      0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 1.0,                      1.0,                     CrossPlatformMachineEpsilon * 10)]  //                              value: (e)
        [InlineData( 1.1861299583286618,       1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 1.6651441426902252,       1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 1.7182818284590452,       1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 1.9706864235520193,       1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 3.9334096679145963,       2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 5.5808859910179210,       2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 7.8249778270762876,       3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Log2P1Test(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, double.Log2P1(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData(-3.1415926535897932,       double.NaN,              0.0)]                               //                              value: -(pi)
        [InlineData(-2.7182818284590452,       double.NaN,              0.0)]                               //                              value: -(e)
        [InlineData(-1.4142135623730950,       double.NaN,              0.0)]                               //                              value: -(sqrt(2))
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData(-1.0,                      double.NegativeInfinity, 0.0)]
        [InlineData(-0.99808698589777568,     -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-0.99501787170355928,     -2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-0.97313395899886387,     -1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.96391680717921279,     -1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.96147111529967797,     -1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.92559179413935728,     -1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.9,                     -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.83609136386042335,     -0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.80371224006494438,     -0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.79730043371348270,     -0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.76912323548399945,     -0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.63212055882855768,     -0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.51950362694813132,     -0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0,                      0.0,                     0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 1.0811811619898573,       0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 1.7182818284590452,       0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))        value: (e)
        [InlineData( 3.3313150290214525,       0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 3.9334096679145963,       0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 4.0945611704512962,       0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 5.1009598002416937,       0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 9.0,                      1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 12.439377934644401,       1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 24.954553519470081,       1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 26.713733786437790,       1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 36.221710484165167,       1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 199.71743249053009,       2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 521.73529967043665,       2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 1384.4557313670111,       3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Log10P1Test(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, double.Log10P1(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NaN,          double.NaN,          0.0)]
        [InlineData( 1.0,                 0.0,                 0.0)]
        [InlineData( 0.54030230586813972, 0.31830988618379067, CrossPlatformMachineEpsilon)]
        [InlineData( 0.20495719432643395, 0.43429448190325183, CrossPlatformMachineEpsilon)]
        [InlineData( 0.0,                 0.5,                 0.0)]
        [InlineData(-0.41614683654714239, 0.63661977236758134, CrossPlatformMachineEpsilon)]
        [InlineData(-0.57023324876887755, 0.69314718055994531, CrossPlatformMachineEpsilon)]
        [InlineData(-0.60569986707881343, 0.70710678118654752, CrossPlatformMachineEpsilon)]
        [InlineData(-0.78121189211048819, 0.78539816339744831, CrossPlatformMachineEpsilon)]
        [InlineData(-1.0,                 1.0,                 0.0)]
        [InlineData(-0.91976499476851874, 0.87162083290448743, CrossPlatformMachineEpsilon)]
        [InlineData(-0.26625534204141549, 0.58578643762690495, CrossPlatformMachineEpsilon)]
        [InlineData(-0.17905794598427576, 0.55730495911103659, CrossPlatformMachineEpsilon)]
        [InlineData( 0.22058404074969809, 0.42920367320510338, CrossPlatformMachineEpsilon)]
        [InlineData( 0.58119566361426737, 0.30258509299404568, CrossPlatformMachineEpsilon)]
        [InlineData(-0.63325565131482003, 0.71828182845904523, CrossPlatformMachineEpsilon)]
        [InlineData(-0.90268536193307107, 0.85840734641020676, CrossPlatformMachineEpsilon)]
        public static void AcosPiTest(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, double.AcosPi(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NaN,           double.NaN,          0.0)]
        [InlineData( 0.0,                  0.0,                 0.0)]
        [InlineData( 0.84147098480789651,  0.31830988618379067, CrossPlatformMachineEpsilon)]
        [InlineData( 0.97877093770393305,  0.43429448190325183, CrossPlatformMachineEpsilon)]
        [InlineData( 1.0,                  0.5,                 0.0)]
        [InlineData( 0.90929742682568170,  0.36338022763241866, CrossPlatformMachineEpsilon)]
        [InlineData( 0.82148283122563883,  0.30685281944005469, CrossPlatformMachineEpsilon)]
        [InlineData( 0.79569320156748087,  0.29289321881345248, CrossPlatformMachineEpsilon)]
        [InlineData( 0.62426595263969903,  0.21460183660255169, CrossPlatformMachineEpsilon)]
        [InlineData(-0.39246955856278420, -0.12837916709551257, CrossPlatformMachineEpsilon)]
        [InlineData(-0.96390253284987733, -0.41421356237309505, CrossPlatformMachineEpsilon)]
        [InlineData(-0.98383852942436249, -0.44269504088896341, CrossPlatformMachineEpsilon)]
        [InlineData(-0.97536797208363139, -0.42920367320510338, CrossPlatformMachineEpsilon)]
        [InlineData( 0.81376384817462330,  0.30258509299404568, CrossPlatformMachineEpsilon)]
        [InlineData( 0.77394268526670828,  0.28171817154095476, CrossPlatformMachineEpsilon)]
        [InlineData(-0.43030121700009227, -0.14159265358979324, CrossPlatformMachineEpsilon)]
        public static void AsinPiTest(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, double.AsinPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, double.AsinPi(+value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NaN,               double.NaN,              double.NaN,           0.0)]
        [InlineData( 0.0,                     -1.0,                      1.0,                 CrossPlatformMachineEpsilon)] // y: sinpi(0)              x:  cospi(1)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.0,                     -0.0,                      1.0,                 CrossPlatformMachineEpsilon)] // y: sinpi(0)              x: -cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.0,                      0.0,                      0.0,                 0.0)]                         // y: sinpi(0)              x:  cospi(0.5)
        [InlineData( 0.0,                      1.0,                      0.0,                 0.0)]                         // y: sinpi(0)              x:  cospi(0)
        [InlineData( 0.84147098480789651,      0.54030230586813972,      0.31830988618379067, CrossPlatformMachineEpsilon)] // y: sinpi(1 / pi)         x:  cospi(1 / pi)
        [InlineData( 0.97877093770393305,      0.20495719432643395,      0.43429448190325183, CrossPlatformMachineEpsilon)] // y: sinpi(log10(e))       x:  cospi(log10(e))
        [InlineData( 1.0,                     -0.0,                      0.5,                 CrossPlatformMachineEpsilon)] // y: sinpi(0.5)            x: -cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 1.0,                      0.0,                      0.5,                 CrossPlatformMachineEpsilon)] // y: sinpi(0.5)            x:  cospi(0.5)          ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 0.90929742682568170,     -0.41614683654714239,      0.63661977236758134, CrossPlatformMachineEpsilon)] // y: sinpi(2 / pi)         x:  cospi(2 / pi)
        [InlineData( 0.82148283122563883,     -0.57023324876887755,      0.69314718055994531, CrossPlatformMachineEpsilon)] // y: sinpi(ln(2))          x:  cospi(ln(2))
        [InlineData( 0.79569320156748087,     -0.60569986707881343,      0.70710678118654752, CrossPlatformMachineEpsilon)] // y: sinpi(1 / sqrt(2))    x:  cospi(1 / sqrt(2))
        [InlineData( 0.62426595263969903,     -0.78121189211048819,      0.78539816339744831, CrossPlatformMachineEpsilon)] // y: sinpi(pi / 4)         x:  cospi(pi / 4)
        [InlineData(-0.39246955856278420,     -0.91976499476851874,     -0.87162083290448743, CrossPlatformMachineEpsilon)] // y: sinpi(2 / sqrt(pi))   x:  cospi(2 / sqrt(pi))
        [InlineData(-0.96390253284987733,     -0.26625534204141549,     -0.58578643762690495, CrossPlatformMachineEpsilon)] // y: sinpi(sqrt(2))        x:  cospi(sqrt(2))
        [InlineData(-0.98383852942436249,     -0.17905794598427576,     -0.55730495911103659, CrossPlatformMachineEpsilon)] // y: sinpi(log2(e))        x:  cospi(log2(e))
        [InlineData(-0.97536797208363139,      0.22058404074969809,     -0.42920367320510338, CrossPlatformMachineEpsilon)] // y: sinpi(pi / 2)         x:  cospi(pi / 2)
        [InlineData( 0.81376384817462330,      0.58119566361426737,      0.30258509299404568, CrossPlatformMachineEpsilon)] // y: sinpi(ln(10))         x:  cospi(ln(10))
        [InlineData( 0.77394268526670828,     -0.63325565131482003,      0.71828182845904524, CrossPlatformMachineEpsilon)] // y: sinpi(e)              x:  cospi(e)
        [InlineData(-0.43030121700009227,     -0.90268536193307107,     -0.85840734641020676, CrossPlatformMachineEpsilon)] // y: sinpi(pi)             x:  cospi(pi)
        [InlineData( 1.0,                      double.NegativeInfinity,  1.0,                 CrossPlatformMachineEpsilon)] // y: sinpi(0.5)                                    ; This should be exact, but has an issue on WASM/Unix
        [InlineData( 1.0,                      double.PositiveInfinity,  0.0,                 0.0)]                         // y: sinpi(0.5)
        [InlineData( double.PositiveInfinity, -1.0,                      0.5,                 CrossPlatformMachineEpsilon)] //                          x:  cospi(1)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( double.PositiveInfinity,  1.0,                      0.5,                 CrossPlatformMachineEpsilon)] //                          x:  cospi(0)            ; This should be exact, but has an issue on WASM/Unix
        [InlineData( double.PositiveInfinity,  double.NegativeInfinity,  0.75,                CrossPlatformMachineEpsilon)] //                                                  ; This should be exact, but has an issue on WASM/Unix
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity,  0.25,                CrossPlatformMachineEpsilon)] //                                                  ; This should be exact, but has an issue on WASM/Unix
        public static void Atan2PiTest(double y, double x, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, double.Atan2Pi(-y, +x), allowedVariance);
            AssertExtensions.Equal(+expectedResult, double.Atan2Pi(+y, +x), allowedVariance);
        }

        [Theory]
        [InlineData( double.NaN,               double.NaN,          0.0)]
        [InlineData( 0.0,                      0.0,                 0.0)]
        [InlineData( 1.5574077246549022,       0.31830988618379067, CrossPlatformMachineEpsilon)]
        [InlineData( 4.7754895402454188,       0.43429448190325183, CrossPlatformMachineEpsilon)]
        [InlineData( double.PositiveInfinity,  0.5,                 0.0)]
        [InlineData(-2.1850398632615190,      -0.36338022763241866, CrossPlatformMachineEpsilon)]
        [InlineData(-1.4406084404920341,      -0.30685281944005469, CrossPlatformMachineEpsilon)]
        [InlineData(-1.3136757077477542,      -0.29289321881345248, CrossPlatformMachineEpsilon)]
        [InlineData(-0.79909939792801821,     -0.21460183660255169, CrossPlatformMachineEpsilon)]
        [InlineData( 0.42670634433261806,      0.12837916709551257, CrossPlatformMachineEpsilon)]
        [InlineData( 3.6202185671074506,       0.41421356237309505, CrossPlatformMachineEpsilon)]
        [InlineData( 5.4945259425167300,       0.44269504088896341, CrossPlatformMachineEpsilon)]
        [InlineData(-4.4217522209161288,      -0.42920367320510338, CrossPlatformMachineEpsilon)]
        [InlineData( 1.4001547140150527,       0.30258509299404568, CrossPlatformMachineEpsilon)]
        [InlineData(-1.2221646718190066,      -0.28171817154095476, CrossPlatformMachineEpsilon)]
        [InlineData( 0.47669014603118892,      0.14159265358979324, CrossPlatformMachineEpsilon)]
        public static void AtanPiTest(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, double.AtanPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, double.AtanPi(+value), allowedVariance);
        }

        [Theory]
        [InlineData(double.NaN,               double.NaN,          0.0)]
        [InlineData(0.0,                      1.0,                 0.0)]
        [InlineData(0.31830988618379067,      0.54030230586813972, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData(0.43429448190325183,      0.20495719432643395, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData(0.5,                      0.0,                 0.0)]
        [InlineData(0.63661977236758134,     -0.41614683654714239, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData(0.69314718055994531,     -0.57023324876887755, CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData(0.70710678118654752,     -0.60569986707881343, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData(0.78539816339744831,     -0.78121189211048819, CrossPlatformMachineEpsilon)]       // value:  (pi / 4)
        [InlineData(1.0,                     -1.0,                 0.0)]
        [InlineData(1.1283791670955126,      -0.91976499476851874, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData(1.4142135623730950,      -0.26625534204141549, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData(1.4426950408889634,      -0.17905794598427576, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData(1.5,                      0.0,                 0.0)]
        [InlineData(1.5707963267948966,       0.22058404074969809, CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData(2.0,                      1.0,                 0.0)]
        [InlineData(2.3025850929940457,       0.58119566361426737, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData(2.5,                      0.0,                 0.0)]
        [InlineData(2.7182818284590452,      -0.63325565131482003, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData(3.0,                     -1.0,                 0.0)]
        [InlineData(3.1415926535897932,      -0.90268536193307107, CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData(3.5,                      0.0,                 0.0)]
        [InlineData(double.PositiveInfinity,  double.NaN,          0.0)]
        public static void CosPiTest(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(+expectedResult, double.CosPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, double.CosPi(+value), allowedVariance);
        }

        [Theory]
        [InlineData(double.NaN,               double.NaN,          0.0)]
        [InlineData(0.0,                      0.0,                 0.0)]
        [InlineData(0.31830988618379067,      0.84147098480789651, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData(0.43429448190325183,      0.97877093770393305, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData(0.5,                      1.0,                 0.0)]
        [InlineData(0.63661977236758134,      0.90929742682568170, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData(0.69314718055994531,      0.82148283122563883, CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData(0.70710678118654752,      0.79569320156748087, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData(0.78539816339744831,      0.62426595263969903, CrossPlatformMachineEpsilon)]       // value:  (pi / 4)
        [InlineData(1.0,                      0.0,                 0.0)]
        [InlineData(1.1283791670955126,      -0.39246955856278420, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData(1.4142135623730950,      -0.96390253284987733, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData(1.4426950408889634,      -0.98383852942436249, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData(1.5,                     -1.0,                 0.0)]
        [InlineData(1.5707963267948966,      -0.97536797208363139, CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData(2.0,                      0.0,                 0.0)]
        [InlineData(2.3025850929940457,       0.81376384817462330, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData(2.5,                      1.0,                 0.0)]
        [InlineData(2.7182818284590452,       0.77394268526670828, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData(3.0,                      0.0,                 0.0)]
        [InlineData(3.1415926535897932,      -0.43030121700009227, CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData(3.5,                     -1.0,                 0.0)]
        [InlineData(double.PositiveInfinity,  double.NaN,          0.0)]
        public static void SinPiTest(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, double.SinPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, double.SinPi(+value), allowedVariance);
        }

        [Theory]
        [InlineData(double.NaN,               double.NaN,              0.0)]
        [InlineData(0.0,                      0.0,                     0.0)]
        [InlineData(0.31830988618379067,      1.5574077246549022,      CrossPlatformMachineEpsilon * 10)]  // value:  (1 / pi)
        [InlineData(0.43429448190325183,      4.7754895402454188,      CrossPlatformMachineEpsilon * 10)]  // value:  (log10(e))
        [InlineData(0.5,                      double.PositiveInfinity, 0.0)]
        [InlineData(0.63661977236758134,     -2.1850398632615190,      CrossPlatformMachineEpsilon * 10)]  // value:  (2 / pi)
        [InlineData(0.69314718055994531,     -1.4406084404920341,      CrossPlatformMachineEpsilon * 10)]  // value:  (ln(2))
        [InlineData(0.70710678118654752,     -1.3136757077477542,      CrossPlatformMachineEpsilon * 10)]  // value:  (1 / sqrt(2))
        [InlineData(0.78539816339744831,     -0.79909939792801821,     CrossPlatformMachineEpsilon)]       // value:  (pi / 4)
        [InlineData(1.0,                     -0.0,                     0.0)]
        [InlineData(1.1283791670955126,       0.42670634433261806,     CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData(1.4142135623730950,       3.6202185671074506,      CrossPlatformMachineEpsilon * 10)]  // value:  (sqrt(2))
        [InlineData(1.4426950408889634,       5.4945259425167300,      CrossPlatformMachineEpsilon * 10)]  // value:  (log2(e))
        [InlineData(1.5,                      double.NegativeInfinity, 0.0)]
        [InlineData(1.5707963267948966,      -4.4217522209161288,      CrossPlatformMachineEpsilon * 10)]  // value:  (pi / 2)
        [InlineData(2.0,                      0.0,                     0.0)]
        [InlineData(2.3025850929940457,       1.4001547140150527,      CrossPlatformMachineEpsilon * 10)]  // value:  (ln(10))
        [InlineData(2.5,                      double.PositiveInfinity, 0.0)]
        [InlineData(2.7182818284590452,      -1.2221646718190066,      CrossPlatformMachineEpsilon * 10)]  // value:  (e)
        [InlineData(3.0,                     -0.0,                     0.0)]
        [InlineData(3.1415926535897932,       0.47669014603118892,     CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData(3.5,                      double.NegativeInfinity, 0.0)]
        [InlineData(double.PositiveInfinity,  double.NaN,              0.0)]
        public static void TanPiTest(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(-expectedResult, double.TanPi(-value), allowedVariance);
            AssertExtensions.Equal(+expectedResult, double.TanPi(+value), allowedVariance);
        }

        [Theory]
        [InlineData(double.NegativeInfinity,    double.NegativeInfinity,    0.5,    double.NegativeInfinity)]
        [InlineData(double.NegativeInfinity,    double.NaN,                 0.5,    double.NaN)]
        [InlineData(double.NegativeInfinity,    double.PositiveInfinity,    0.5,    double.NaN)]
        [InlineData(double.NegativeInfinity,    0.0,                        0.5,    double.NegativeInfinity)]
        [InlineData(double.NegativeInfinity,    1.0,                        0.5,    double.NegativeInfinity)]
        [InlineData(double.NaN,                 double.NegativeInfinity,    0.5,    double.NaN)]
        [InlineData(double.NaN,                 double.NaN,                 0.5,    double.NaN)]
        [InlineData(double.NaN,                 double.PositiveInfinity,    0.5,    double.NaN)]
        [InlineData(double.NaN,                 0.0,                        0.5,    double.NaN)]
        [InlineData(double.NaN,                 1.0,                        0.5,    double.NaN)]
        [InlineData(double.PositiveInfinity,    double.NegativeInfinity,    0.5,    double.NaN)]
        [InlineData(double.PositiveInfinity,    double.NaN,                 0.5,    double.NaN)]
        [InlineData(double.PositiveInfinity,    double.PositiveInfinity,    0.5,    double.PositiveInfinity)]
        [InlineData(double.PositiveInfinity,    0.0,                        0.5,    double.PositiveInfinity)]
        [InlineData(double.PositiveInfinity,    1.0,                        0.5,    double.PositiveInfinity)]
        [InlineData(1.0,                        3.0,                        0.0,    1.0)]
        [InlineData(1.0,                        3.0,                        0.5,    2.0)]
        [InlineData(1.0,                        3.0,                        1.0,    3.0)]
        [InlineData(1.0,                        3.0,                        2.0,    5.0)]
        [InlineData(2.0,                        4.0,                        0.0,    2.0)]
        [InlineData(2.0,                        4.0,                        0.5,    3.0)]
        [InlineData(2.0,                        4.0,                        1.0,    4.0)]
        [InlineData(2.0,                        4.0,                        2.0,    6.0)]
        [InlineData(3.0,                        1.0,                        0.0,    3.0)]
        [InlineData(3.0,                        1.0,                        0.5,    2.0)]
        [InlineData(3.0,                        1.0,                        1.0,    1.0)]
        [InlineData(3.0,                        1.0,                        2.0,   -1.0)]
        [InlineData(4.0,                        2.0,                        0.0,    4.0)]
        [InlineData(4.0,                        2.0,                        0.5,    3.0)]
        [InlineData(4.0,                        2.0,                        1.0,    2.0)]
        [InlineData(4.0,                        2.0,                        2.0,    0.0)]
        public static void LerpTest(double value1, double value2, double amount, double expectedResult)
        {
            AssertExtensions.Equal(+expectedResult, double.Lerp(+value1, +value2, amount), 0);
            AssertExtensions.Equal((expectedResult == 0.0) ? expectedResult : -expectedResult, double.Lerp(-value1, -value2, amount), 0);
        }
    }
}
