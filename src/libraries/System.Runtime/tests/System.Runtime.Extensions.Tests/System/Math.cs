// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable xUnit1025 // reporting duplicate test cases due to not distinguishing 0.0 from -0.0

namespace System.Tests
{
    public static partial class MathTests
    {
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
        private const double CrossPlatformMachineEpsilon = 8.8817841970012523e-16;

        // binary32 (float) has a machine epsilon of 2^-23 (approx. 1.19e-07). However, this
        // is slightly too accurate when writing tests meant to run against libm implementations
        // for various platforms. 2^-21 (approx. 4.76e-07) seems to be as accurate as we can get.
        //
        // The tests themselves will take CrossPlatformMachineEpsilon and adjust it according to the expected result
        // so that the delta used for comparison will compare the most significant digits and ignore
        // any digits that are outside the single precision range (6-9 digits).

        // For example, a test with an expect result in the format of 0.xxxxxxxxx will use
        // CrossPlatformMachineEpsilon for the variance, while an expected result in the format of 0.0xxxxxxxxx
        // will use CrossPlatformMachineEpsilon / 10 and expected result in the format of x.xxxxxx will
        // use CrossPlatformMachineEpsilon * 10.
        private const float CrossPlatformMachineEpsilonSingle = 4.76837158e-07f;

        // The existing estimate functions either have an error of no more than 1.5 * 2^-12 (approx. 3.66e-04)
        // or perform one Newton-Raphson iteration which, for the currently tested values, gives an error of
        // no more than approx. 1.5 * 2^-7 (approx 1.17e-02).
        private const double CrossPlatformMachineEpsilonForEstimates = 1.171875e-02;

        [Fact]
        public static void E()
        {
            Assert.Equal(unchecked((long)0x4005BF0A8B145769), BitConverter.DoubleToInt64Bits(Math.E));
        }

        [Fact]
        public static void Pi()
        {
            Assert.Equal(unchecked((long)0x400921FB54442D18), BitConverter.DoubleToInt64Bits(Math.PI));
        }

        [Fact]
        public static void Tau()
        {
            Assert.Equal(unchecked((long)0x401921FB54442D18), BitConverter.DoubleToInt64Bits(Math.Tau));
        }

        [Fact]
        public static void Abs_Decimal()
        {
            Assert.Equal(3.0m, Math.Abs(3.0m));
            Assert.Equal(0.0m, Math.Abs(0.0m));
            Assert.Equal(0.0m, Math.Abs(-0.0m));
            Assert.Equal(3.0m, Math.Abs(-3.0m));
            Assert.Equal(decimal.MaxValue, Math.Abs(decimal.MinValue));
        }

        [Theory]
        [InlineData( double.NegativeInfinity, double.PositiveInfinity, 0.0)]
        [InlineData(-3.1415926535897932,      3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]     // value: -(pi)             expected: (pi)
        [InlineData(-2.7182818284590452,      2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]     // value: -(e)              expected: (e)
        [InlineData(-2.3025850929940457,      2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]     // value: -(ln(10))         expected: (ln(10))
        [InlineData(-1.5707963267948966,      1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]     // value: -(pi / 2)         expected: (pi / 2)
        [InlineData(-1.4426950408889634,      1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]     // value: -(log2(e))        expected: (log2(e))
        [InlineData(-1.4142135623730950,      1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]     // value: -(sqrt(2))        expected: (sqrt(2))
        [InlineData(-1.1283791670955126,      1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]     // value: -(2 / sqrt(pi))   expected: (2 / sqrt(pi))
        [InlineData(-1.0,                     1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.78539816339744831,     0.78539816339744831,     CrossPlatformMachineEpsilon)]          // value: -(pi / 4)         expected: (pi / 4)
        [InlineData(-0.70710678118654752,     0.70710678118654752,     CrossPlatformMachineEpsilon)]          // value: -(1 / sqrt(2))    expected: (1 / sqrt(2))
        [InlineData(-0.69314718055994531,     0.69314718055994531,     CrossPlatformMachineEpsilon)]          // value: -(ln(2))          expected: (ln(2))
        [InlineData(-0.63661977236758134,     0.63661977236758134,     CrossPlatformMachineEpsilon)]          // value: -(2 / pi)         expected: (2 / pi)
        [InlineData(-0.43429448190325183,     0.43429448190325183,     CrossPlatformMachineEpsilon)]          // value: -(log10(e))       expected: (log10(e))
        [InlineData(-0.31830988618379067,     0.31830988618379067,     CrossPlatformMachineEpsilon)]          // value: -(1 / pi)         expected: (1 / pi)
        [InlineData(-0.0,                     0.0,                     0.0)]
        [InlineData( double.NaN,              double.NaN,              0.0)]
        [InlineData( 0.0,                     0.0,                     0.0)]
        [InlineData( 0.31830988618379067,     0.31830988618379067,     CrossPlatformMachineEpsilon)]          // value:  (1 / pi)         expected: (1 / pi)
        [InlineData( 0.43429448190325183,     0.43429448190325183,     CrossPlatformMachineEpsilon)]          // value:  (log10(e))       expected: (log10(e))
        [InlineData( 0.63661977236758134,     0.63661977236758134,     CrossPlatformMachineEpsilon)]          // value:  (2 / pi)         expected: (2 / pi)
        [InlineData( 0.69314718055994531,     0.69314718055994531,     CrossPlatformMachineEpsilon)]          // value:  (ln(2))          expected: (ln(2))
        [InlineData( 0.70710678118654752,     0.70710678118654752,     CrossPlatformMachineEpsilon)]          // value:  (1 / sqrt(2))    expected: (1 / sqrt(2))
        [InlineData( 0.78539816339744831,     0.78539816339744831,     CrossPlatformMachineEpsilon)]          // value:  (pi / 4)         expected: (pi / 4)
        [InlineData( 1.0,                     1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.1283791670955126,      1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]     // value:  (2 / sqrt(pi))   expected: (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,      1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]     // value:  (sqrt(2))        expected: (sqrt(2))
        [InlineData( 1.4426950408889634,      1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]     // value:  (log2(e))        expected: (log2(e))
        [InlineData( 1.5707963267948966,      1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 2)         expected: (pi / 2)
        [InlineData( 2.3025850929940457,      2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]     // value:  (ln(10))         expected: (ln(10))
        [InlineData( 2.7182818284590452,      2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]     // value:  (e)              expected: (e)
        [InlineData( 3.1415926535897932,      3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]     // value:  (pi)             expected: (pi)
        [InlineData( double.PositiveInfinity, double.PositiveInfinity, 0.0)]
        public static void Abs_Double(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Abs(value), allowedVariance);
        }

        [Fact]
        public static void Abs_Int16()
        {
            Assert.Equal((short)3, Math.Abs((short)3));
            Assert.Equal((short)0, Math.Abs((short)0));
            Assert.Equal((short)3, Math.Abs((short)(-3)));
            Assert.Throws<OverflowException>(() => Math.Abs(short.MinValue));
        }

        [Fact]
        public static void Abs_Int32()
        {
            Assert.Equal(3, Math.Abs(3));
            Assert.Equal(0, Math.Abs(0));
            Assert.Equal(3, Math.Abs(-3));
            Assert.Throws<OverflowException>(() => Math.Abs(int.MinValue));
        }

        [Fact]
        public static void Abs_Int64()
        {
            Assert.Equal(3L, Math.Abs(3L));
            Assert.Equal(0L, Math.Abs(0L));
            Assert.Equal(3L, Math.Abs(-3L));
            Assert.Throws<OverflowException>(() => Math.Abs(long.MinValue));
        }

        [Fact]
        public static void Abs_NInt()
        {
            Assert.Equal((nint)3, Math.Abs((nint)3));
            Assert.Equal((nint)0, Math.Abs((nint)0));
            Assert.Equal((nint)3, Math.Abs((nint)(-3)));
            Assert.Throws<OverflowException>(() => Math.Abs(nint.MinValue));
        }

        [Fact]
        public static void Abs_SByte()
        {
            Assert.Equal((sbyte)3, Math.Abs((sbyte)3));
            Assert.Equal((sbyte)0, Math.Abs((sbyte)0));
            Assert.Equal((sbyte)3, Math.Abs((sbyte)(-3)));
            Assert.Throws<OverflowException>(() => Math.Abs(sbyte.MinValue));
        }

        [Theory]
        [InlineData( float.NegativeInfinity, float.PositiveInfinity, 0.0f)]
        [InlineData(-3.14159265f,            3.14159265f,            CrossPlatformMachineEpsilonSingle * 10)]   // value: -(pi)             expected: (pi)
        [InlineData(-2.71828183f,            2.71828183f,            CrossPlatformMachineEpsilonSingle * 10)]   // value: -(e)              expected: (e)
        [InlineData(-2.30258509f,            2.30258509f,            CrossPlatformMachineEpsilonSingle * 10)]   // value: -(ln(10))         expected: (ln(10))
        [InlineData(-1.57079633f,            1.57079633f,            CrossPlatformMachineEpsilonSingle * 10)]   // value: -(pi / 2)         expected: (pi / 2)
        [InlineData(-1.44269504f,            1.44269504f,            CrossPlatformMachineEpsilonSingle * 10)]   // value: -(log2(e))        expected: (log2(e))
        [InlineData(-1.41421356f,            1.41421356f,            CrossPlatformMachineEpsilonSingle * 10)]   // value: -(sqrt(2))        expected: (sqrt(2))
        [InlineData(-1.12837917f,            1.12837917f,            CrossPlatformMachineEpsilonSingle * 10)]   // value: -(2 / sqrt(pi))   expected: (2 / sqrt(pi))
        [InlineData(-1.0f,                   1.0f,                   CrossPlatformMachineEpsilonSingle * 10)]
        [InlineData(-0.785398163f,           0.785398163f,           CrossPlatformMachineEpsilonSingle)]        // value: -(pi / 4)         expected: (pi / 4)
        [InlineData(-0.707106781f,           0.707106781f,           CrossPlatformMachineEpsilonSingle)]        // value: -(1 / sqrt(2))    expected: (1 / sqrt(2))
        [InlineData(-0.693147181f,           0.693147181f,           CrossPlatformMachineEpsilonSingle)]        // value: -(ln(2))          expected: (ln(2))
        [InlineData(-0.636619772f,           0.636619772f,           CrossPlatformMachineEpsilonSingle)]        // value: -(2 / pi)         expected: (2 / pi)
        [InlineData(-0.434294482f,           0.434294482f,           CrossPlatformMachineEpsilonSingle)]        // value: -(log10(e))       expected: (log10(e))
        [InlineData(-0.318309886f,           0.318309886f,           CrossPlatformMachineEpsilonSingle)]        // value: -(1 / pi)         expected: (1 / pi)
        [InlineData(-0.0f,                   0.0f,                   0.0f)]
        [InlineData( float.NaN,              float.NaN,              0.0f)]
        [InlineData( 0.0f,                   0.0f,                   0.0f)]
        [InlineData( 0.318309886f,           0.318309886f,           CrossPlatformMachineEpsilonSingle)]        // value:  (1 / pi)         expected: (1 / pi)
        [InlineData( 0.434294482f,           0.434294482f,           CrossPlatformMachineEpsilonSingle)]        // value:  (log10(e))       expected: (log10(e))
        [InlineData( 0.636619772f,           0.636619772f,           CrossPlatformMachineEpsilonSingle)]        // value:  (2 / pi)         expected: (2 / pi)
        [InlineData( 0.693147181f,           0.693147181f,           CrossPlatformMachineEpsilonSingle)]        // value:  (ln(2))          expected: (ln(2))
        [InlineData( 0.707106781f,           0.707106781f,           CrossPlatformMachineEpsilonSingle)]        // value:  (1 / sqrt(2))    expected: (1 / sqrt(2))
        [InlineData( 0.785398163f,           0.785398163f,           CrossPlatformMachineEpsilonSingle)]        // value:  (pi / 4)         expected: (pi / 4)
        [InlineData( 1.0f,                   1.0f,                   CrossPlatformMachineEpsilonSingle * 10)]
        [InlineData( 1.12837917f,            1.12837917f,            CrossPlatformMachineEpsilonSingle * 10)]   // value:  (2 / sqrt(pi))   expected: (2 / sqrt(pi))
        [InlineData( 1.41421356f,            1.41421356f,            CrossPlatformMachineEpsilonSingle * 10)]   // value:  (sqrt(2))        expected: (sqrt(2))
        [InlineData( 1.44269504f,            1.44269504f,            CrossPlatformMachineEpsilonSingle * 10)]   // value:  (log2(e))        expected: (log2(e))
        [InlineData( 1.57079633f,            1.57079633f,            CrossPlatformMachineEpsilonSingle * 10)]   // value:  (pi / 2)         expected: (pi / 2)
        [InlineData( 2.30258509f,            2.30258509f,            CrossPlatformMachineEpsilonSingle * 10)]   // value:  (ln(10))         expected: (ln(10))
        [InlineData( 2.71828183f,            2.71828183f,            CrossPlatformMachineEpsilonSingle * 10)]   // value:  (e)              expected: (e)
        [InlineData( 3.14159265f,            3.14159265f,            CrossPlatformMachineEpsilonSingle * 10)]   // value:  (pi)             expected: (pi)
        [InlineData( float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Abs_Single(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Abs(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, double.NaN,          0.0)]
        [InlineData(-3.1415926535897932,      double.NaN,          0.0)]                                //                              value: -(pi)
        [InlineData(-2.7182818284590452,      double.NaN,          0.0)]                                //                              value: -(e)
        [InlineData(-1.4142135623730950,      double.NaN,          0.0)]                                //                              value: -(sqrt(2))
        [InlineData(-1.0,                     3.1415926535897932,  CrossPlatformMachineEpsilon * 10)]   // expected:  (pi)
        [InlineData(-0.91173391478696510,     2.7182818284590452,  CrossPlatformMachineEpsilon * 10)]   // expected:  (e)
        [InlineData(-0.66820151019031295,     2.3025850929940457,  CrossPlatformMachineEpsilon * 10)]   // expected:  (ln(10))
        [InlineData(-0.0,                     1.5707963267948966,  CrossPlatformMachineEpsilon * 10)]   // expected:  (pi / 2)
        [InlineData( double.NaN,              double.NaN,          0.0)]
        [InlineData( 0.0,                     1.5707963267948966,  CrossPlatformMachineEpsilon * 10)]   // expected:  (pi / 2)
        [InlineData( 0.12775121753523991,     1.4426950408889634,  CrossPlatformMachineEpsilon * 10)]   // expected:  (log2(e))
        [InlineData( 0.15594369476537447,     1.4142135623730950,  CrossPlatformMachineEpsilon * 10)]   // expected:  (sqrt(2))
        [InlineData( 0.42812514788535792,     1.1283791670955126,  CrossPlatformMachineEpsilon * 10)]   // expected:  (2 / sqrt(pi))
        [InlineData( 0.54030230586813972,     1.0,                 CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.70710678118654752,     0.78539816339744831, CrossPlatformMachineEpsilon)]        // expected:  (pi / 4),         value:  (1 / sqrt(2))
        [InlineData( 0.76024459707563015,     0.70710678118654752, CrossPlatformMachineEpsilon)]        // expected:  (1 / sqrt(2))
        [InlineData( 0.76923890136397213,     0.69314718055994531, CrossPlatformMachineEpsilon)]        // expected:  (ln(2))
        [InlineData( 0.80410982822879171,     0.63661977236758134, CrossPlatformMachineEpsilon)]        // expected:  (2 / pi)
        [InlineData( 0.90716712923909839,     0.43429448190325183, CrossPlatformMachineEpsilon)]        // expected:  (log10(e))
        [InlineData( 0.94976571538163866,     0.31830988618379067, CrossPlatformMachineEpsilon)]        // expected:  (1 / pi)
        [InlineData( 1.0,                     0.0,                 0.0 )]
        [InlineData( 1.4142135623730950,      double.NaN,          0.0)]                                //                              value:  (sqrt(2))
        [InlineData( 2.7182818284590452,      double.NaN,          0.0)]                                //                              value:  (e)
        [InlineData( 3.1415926535897932,      double.NaN,          0.0)]                                //                              value:  (pi)
        [InlineData( double.PositiveInfinity, double.NaN,          0.0 )]
        public static void Acos(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Acos(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,          0.0)]
        [InlineData(-3.1415926535897932,       double.NaN,          0.0)]                               //                              value: -(pi)
        [InlineData(-2.7182818284590452,       double.NaN,          0.0)]                               //                              value: -(e)
        [InlineData(-1.4142135623730950,       double.NaN,          0.0)]                               //                              value: -(sqrt(2))
        [InlineData(-1.0,                     -1.5707963267948966,  CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.99180624439366372,     -1.4426950408889634,  CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.98776594599273553,     -1.4142135623730950,  CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.90371945743584630,     -1.1283791670955126,  CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.84147098480789651,     -1.0,                 CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.74398033695749319,     -0.83900756059574755, CrossPlatformMachineEpsilon)]       // expected: -(pi - ln(10))
        [InlineData(-0.70710678118654752,     -0.78539816339744831, CrossPlatformMachineEpsilon)]       // expected: -(pi / 4),         value: (1 / sqrt(2))
        [InlineData(-0.64963693908006244,     -0.70710678118654752, CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.63896127631363480,     -0.69314718055994531, CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.59448076852482208,     -0.63661977236758134, CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.42077048331375735,     -0.43429448190325183, CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.41078129050290870,     -0.42331082513074800, CrossPlatformMachineEpsilon)]       // expected: -(pi - e)
        [InlineData(-0.31296179620778659,     -0.31830988618379067, CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                 0.0)]
        [InlineData( double.NaN,               double.NaN,          0.0)]
        [InlineData( 0.0,                      0.0,                 0.0)]
        [InlineData( 0.31296179620778659,      0.31830988618379067, CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.41078129050290870,      0.42331082513074800, CrossPlatformMachineEpsilon)]       // expected:  (pi - e)
        [InlineData( 0.42077048331375735,      0.43429448190325183, CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.59448076852482208,      0.63661977236758134, CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 0.63896127631363480,      0.69314718055994531, CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 0.64963693908006244,      0.70710678118654752, CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 0.70710678118654752,      0.78539816339744831, CrossPlatformMachineEpsilon)]       // expected:  (pi / 4),         value: (1 / sqrt(2))
        [InlineData( 0.74398033695749319,      0.83900756059574755, CrossPlatformMachineEpsilon)]       // expected:  (pi - ln(10))
        [InlineData( 0.84147098480789651,      1.0,                 CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.90371945743584630,      1.1283791670955126,  CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 0.98776594599273553,      1.4142135623730950,  CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 0.99180624439366372,      1.4426950408889634,  CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 1.0,                      1.5707963267948966,  CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 1.4142135623730950,       double.NaN,          0.0)]                               //                              value:  (sqrt(2))
        [InlineData( 2.7182818284590452,       double.NaN,          0.0)]                               //                              value:  (e)
        [InlineData( 3.1415926535897932,       double.NaN,          0.0)]                               //                              value:  (pi)
        [InlineData( double.PositiveInfinity,  double.NaN,          0.0)]
        public static void Asin(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Asin(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, -1.5707963267948966,  CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-7.7635756709721848,      -1.4426950408889634,  CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-6.3341191670421916,      -1.4142135623730950,  CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-2.1108768356626451,      -1.1283791670955126,  CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-1.5574077246549022,      -1.0,                 CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.1134071468135374,      -0.83900756059574755, CrossPlatformMachineEpsilon)]       // expected: -(pi - ln(10))
        [InlineData(-1.0,                     -0.78539816339744831, CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.85451043200960189,     -0.70710678118654752, CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.83064087786078395,     -0.69314718055994531, CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.73930295048660405,     -0.63661977236758134, CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.46382906716062964,     -0.43429448190325183, CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.45054953406980750,     -0.42331082513074800, CrossPlatformMachineEpsilon)]       // expected: -(pi - e)
        [InlineData(-0.32951473309607836,     -0.31830988618379067, CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                 0.0)]
        [InlineData( double.NaN,               double.NaN,          0.0)]
        [InlineData( 0.0,                      0.0,                 0.0)]
        [InlineData( 0.32951473309607836,      0.31830988618379067, CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.45054953406980750,      0.42331082513074800, CrossPlatformMachineEpsilon)]       // expected:  (pi - e)
        [InlineData( 0.46382906716062964,      0.43429448190325183, CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.73930295048660405,      0.63661977236758134, CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 0.83064087786078395,      0.69314718055994531, CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 0.85451043200960189,      0.70710678118654752, CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 1.0,                      0.78539816339744831, CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 1.1134071468135374,       0.83900756059574755, CrossPlatformMachineEpsilon)]       // expected:  (pi - ln(10))
        [InlineData( 1.5574077246549022,       1.0,                 CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.1108768356626451,       1.1283791670955126,  CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 6.3341191670421916,       1.4142135623730950,  CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 7.7635756709721848,       1.4426950408889634,  CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( double.PositiveInfinity,  1.5707963267948966,  CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        public static void Atan(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Atan(value), allowedVariance);
        }

        public static IEnumerable<object[]> Atan2_TestData
        {
            get
            {
                yield return new object[] { double.NegativeInfinity, -1.0, -1.5707963267948966, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi / 2)
                yield return new object[] { double.NegativeInfinity, -0.0, -1.5707963267948966, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi / 2)
                yield return new object[] { double.NegativeInfinity, double.NaN, double.NaN, 0.0 };
                yield return new object[] { double.NegativeInfinity, 0.0, -1.5707963267948966, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi / 2)
                yield return new object[] { double.NegativeInfinity, 1.0, -1.5707963267948966, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi / 2)
                yield return new object[] { -1.0, -1.0, -2.3561944901923449, CrossPlatformMachineEpsilon * 10 };    // expected: -(3 * pi / 4)
                yield return new object[] { -1.0, -0.0, -1.5707963267948966, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi / 2)
                yield return new object[] { -1.0, double.NaN, double.NaN, 0.0 };
                yield return new object[] { -1.0, 0.0, -1.5707963267948966, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi / 2)
                yield return new object[] { -1.0, 1.0, -0.78539816339744831, CrossPlatformMachineEpsilon };         // expected: -(pi / 4)
                yield return new object[] { -1.0, double.PositiveInfinity, -0.0, 0.0 };
                yield return new object[] { -0.99180624439366372, -0.12775121753523991, -1.6988976127008298, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi - log2(e))
                yield return new object[] { -0.99180624439366372, 0.12775121753523991, -1.4426950408889634, CrossPlatformMachineEpsilon * 10 };    // expected: -(log2(e))
                yield return new object[] { -0.98776594599273553, -0.15594369476537447, -1.7273790912166982, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi - sqrt(2))
                yield return new object[] { -0.98776594599273553, 0.15594369476537447, -1.4142135623730950, CrossPlatformMachineEpsilon * 10 };    // expected: -(sqrt(2))
                yield return new object[] { -0.90371945743584630, -0.42812514788535792, -2.0132134864942807, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi - (2 / sqrt(pi))
                yield return new object[] { -0.90371945743584630, 0.42812514788535792, -1.1283791670955126, CrossPlatformMachineEpsilon * 10 };    // expected: -(2 / sqrt(pi)
                yield return new object[] { -0.84147098480789651, -0.54030230586813972, -2.1415926535897932, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi - 1)
                yield return new object[] { -0.84147098480789651, 0.54030230586813972, -1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { -0.74398033695749319, -0.66820151019031295, -2.3025850929940457, CrossPlatformMachineEpsilon * 10 };    // expected: -(ln(10))
                yield return new object[] { -0.74398033695749319, 0.66820151019031295, -0.83900756059574755, CrossPlatformMachineEpsilon };         // expected: -(pi - ln(10))
                yield return new object[] { -0.70710678118654752, -0.70710678118654752, -2.3561944901923449, CrossPlatformMachineEpsilon * 10 };    // expected: -(3 * pi / 4),         y: -(1 / sqrt(2))   x: -(1 / sqrt(2))
                yield return new object[] { -0.70710678118654752, 0.70710678118654752, -0.78539816339744831, CrossPlatformMachineEpsilon };         // expected: -(pi / 4),             y: -(1 / sqrt(2))   x:  (1 / sqrt(2))
                yield return new object[] { -0.64963693908006244, -0.76024459707563015, -2.4344858724032457, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi - (1 / sqrt(2))
                yield return new object[] { -0.64963693908006244, 0.76024459707563015, -0.70710678118654752, CrossPlatformMachineEpsilon };         // expected: -(1 / sqrt(2))
                yield return new object[] { -0.63896127631363480, -0.76923890136397213, -2.4484454730298479, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi - ln(2))
                yield return new object[] { -0.63896127631363480, 0.76923890136397213, -0.69314718055994531, CrossPlatformMachineEpsilon };         // expected: -(ln(2))
                yield return new object[] { -0.59448076852482208, -0.80410982822879171, -2.5049728812222119, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi - (2 / pi))
                yield return new object[] { -0.59448076852482208, 0.80410982822879171, -0.63661977236758134, CrossPlatformMachineEpsilon };         // expected: -(2 / pi)
                yield return new object[] { -0.42077048331375735, -0.90716712923909839, -2.7072981716865414, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi - log10(e))
                yield return new object[] { -0.42077048331375735, 0.90716712923909839, -0.43429448190325183, CrossPlatformMachineEpsilon };         // expected: -(log10(e))
                yield return new object[] { -0.41078129050290870, -0.91173391478696510, -2.7182818284590452, CrossPlatformMachineEpsilon * 10 };    // expected: -(e)
                yield return new object[] { -0.41078129050290870, 0.91173391478696510, -0.42331082513074800, CrossPlatformMachineEpsilon };         // expected: -(pi - e)
                yield return new object[] { -0.31296179620778659, -0.94976571538163866, -2.8232827674060026, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi - (1 / pi))
                yield return new object[] { -0.31296179620778659, 0.94976571538163866, -0.31830988618379067, CrossPlatformMachineEpsilon };         // expected: -(1 / pi)
                yield return new object[] { -0.0, double.NegativeInfinity, -3.1415926535897932, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi)
                yield return new object[] { -0.0, -1.0, -3.1415926535897932, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi)
                yield return new object[] { -0.0, -0.0, -3.1415926535897932, CrossPlatformMachineEpsilon * 10 };    // expected: -(pi)
                yield return new object[] { -0.0, double.NaN, double.NaN, 0.0 };
                yield return new object[] { -0.0, 0.0, -0.0, 0.0 };
                yield return new object[] { -0.0, 1.0, -0.0, 0.0 };
                yield return new object[] { -0.0, double.PositiveInfinity, -0.0, 0.0 };
                yield return new object[] { double.NaN, double.NegativeInfinity, double.NaN, 0.0 };
                yield return new object[] { double.NaN, -1.0, double.NaN, 0.0 };
                yield return new object[] { double.NaN, -0.0, double.NaN, 0.0 };
                yield return new object[] { double.NaN, double.NaN, double.NaN, 0.0 };
                yield return new object[] { double.NaN, 0.0, double.NaN, 0.0 };
                yield return new object[] { double.NaN, 1.0, double.NaN, 0.0 };
                yield return new object[] { double.NaN, double.PositiveInfinity, double.NaN, 0.0 };
                yield return new object[] { 0.0, double.NegativeInfinity, 3.1415926535897932, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi)
                yield return new object[] { 0.0, -1.0, 3.1415926535897932, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi)
                yield return new object[] { 0.0, -0.0, 3.1415926535897932, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi)
                yield return new object[] { 0.0, double.NaN, double.NaN, 0.0 };
                yield return new object[] { 0.0, 0.0, 0.0, 0.0 };
                yield return new object[] { 0.0, 1.0, 0.0, 0.0 };
                yield return new object[] { 0.0, double.PositiveInfinity, 0.0, 0.0 };
                yield return new object[] { 0.31296179620778659, -0.94976571538163866, 2.8232827674060026, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi - (1 / pi))
                yield return new object[] { 0.31296179620778659, 0.94976571538163866, 0.31830988618379067, CrossPlatformMachineEpsilon };          // expected:  (1 / pi)
                yield return new object[] { 0.41078129050290870, -0.91173391478696510, 2.7182818284590452, CrossPlatformMachineEpsilon * 10 };     // expected:  (e)
                yield return new object[] { 0.41078129050290870, 0.91173391478696510, 0.42331082513074800, CrossPlatformMachineEpsilon };          // expected:  (pi - e)
                yield return new object[] { 0.42077048331375735, -0.90716712923909839, 2.7072981716865414, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi - log10(e))
                yield return new object[] { 0.42077048331375735, 0.90716712923909839, 0.43429448190325183, CrossPlatformMachineEpsilon };          // expected:  (log10(e))
                yield return new object[] { 0.59448076852482208, -0.80410982822879171, 2.5049728812222119, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi - (2 / pi))
                yield return new object[] { 0.59448076852482208, 0.80410982822879171, 0.63661977236758134, CrossPlatformMachineEpsilon };          // expected:  (2 / pi)
                yield return new object[] { 0.63896127631363480, -0.76923890136397213, 2.4484454730298479, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi - ln(2))
                yield return new object[] { 0.63896127631363480, 0.76923890136397213, 0.69314718055994531, CrossPlatformMachineEpsilon };          // expected:  (ln(2))
                yield return new object[] { 0.64963693908006244, -0.76024459707563015, 2.4344858724032457, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi - (1 / sqrt(2))
                yield return new object[] { 0.64963693908006244, 0.76024459707563015, 0.70710678118654752, CrossPlatformMachineEpsilon };          // expected:  (1 / sqrt(2))
                yield return new object[] { 0.70710678118654752, -0.70710678118654752, 2.3561944901923449, CrossPlatformMachineEpsilon * 10 };     // expected:  (3 * pi / 4),         y:  (1 / sqrt(2))   x: -(1 / sqrt(2))
                yield return new object[] { 0.70710678118654752, 0.70710678118654752, 0.78539816339744831, CrossPlatformMachineEpsilon };          // expected:  (pi / 4),             y:  (1 / sqrt(2))   x:  (1 / sqrt(2))
                yield return new object[] { 0.74398033695749319, -0.66820151019031295, 2.3025850929940457, CrossPlatformMachineEpsilon * 10 };     // expected:  (ln(10))
                yield return new object[] { 0.74398033695749319, 0.66820151019031295, 0.83900756059574755, CrossPlatformMachineEpsilon };          // expected:  (pi - ln(10))
                yield return new object[] { 0.84147098480789651, -0.54030230586813972, 2.1415926535897932, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi - 1)
                yield return new object[] { 0.84147098480789651, 0.54030230586813972, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 0.90371945743584630, -0.42812514788535792, 2.0132134864942807, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi - (2 / sqrt(pi))
                yield return new object[] { 0.90371945743584630, 0.42812514788535792, 1.1283791670955126, CrossPlatformMachineEpsilon * 10 };     // expected:  (2 / sqrt(pi))
                yield return new object[] { 0.98776594599273553, -0.15594369476537447, 1.7273790912166982, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi - sqrt(2))
                yield return new object[] { 0.98776594599273553, 0.15594369476537447, 1.4142135623730950, CrossPlatformMachineEpsilon * 10 };     // expected:  (sqrt(2))
                yield return new object[] { 0.99180624439366372, -0.12775121753523991, 1.6988976127008298, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi - log2(e))
                yield return new object[] { 0.99180624439366372, 0.12775121753523991, 1.4426950408889634, CrossPlatformMachineEpsilon * 10 };     // expected:  (log2(e))
                yield return new object[] { 1.0, -1.0, 2.3561944901923449, CrossPlatformMachineEpsilon * 10 };     // expected:  (3 * pi / 4)
                yield return new object[] { 1.0, -0.0, 1.5707963267948966, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi / 2)
                yield return new object[] { 1.0, double.NaN, double.NaN, 0.0 };
                yield return new object[] { 1.0, 0.0, 1.5707963267948966, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi / 2)
                yield return new object[] { 1.0, 1.0, 0.78539816339744831, CrossPlatformMachineEpsilon };          // expected:  (pi / 4)
                yield return new object[] { 1.0, double.PositiveInfinity, 0.0, 0.0 };
                yield return new object[] { double.PositiveInfinity, -1.0, 1.5707963267948966, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi / 2)
                yield return new object[] { double.PositiveInfinity, -0.0, 1.5707963267948966, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi / 2)
                yield return new object[] { double.PositiveInfinity, double.NaN, double.NaN, 0.0 };
                yield return new object[] { double.PositiveInfinity, 0.0, 1.5707963267948966, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi / 2)
                yield return new object[] { double.PositiveInfinity, 1.0, 1.5707963267948966, CrossPlatformMachineEpsilon * 10 };     // expected:  (pi / 2)
            }
        }

        [Theory]
        [MemberData(nameof(Atan2_TestData))]
        public static void Atan2(double y, double x, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Atan2(y, x), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, double.NegativeInfinity, -2.3561944901923449,  CrossPlatformMachineEpsilon * 10)]    // expected: -(3 * pi / 4)
        [InlineData( double.NegativeInfinity, double.PositiveInfinity, -0.78539816339744831, CrossPlatformMachineEpsilon)]         // expected: -(pi / 4)
        [InlineData( double.PositiveInfinity, double.NegativeInfinity,  2.3561944901923449,  CrossPlatformMachineEpsilon * 10)]    // expected:  (3 * pi / 4)
        [InlineData( double.PositiveInfinity, double.PositiveInfinity,  0.78539816339744831, CrossPlatformMachineEpsilon)]         // expected:  (pi / 4)
        public static void Atan2_IEEE(double y, double x, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Atan2(y, x), allowedVariance);
        }

        [Fact]
        public static void Ceiling_Decimal()
        {
            Assert.Equal(2.0m, Math.Ceiling(1.1m));
            Assert.Equal(2.0m, Math.Ceiling(1.9m));
            Assert.Equal(-1.0m, Math.Ceiling(-1.1m));
        }

        [Theory]
        [InlineData(double.NegativeInfinity,  double.NegativeInfinity, 0.0)]
        [InlineData(-3.1415926535897932,     -3.0,                     0.0)]    // value: -(pi)
        [InlineData(-2.7182818284590452,     -2.0,                     0.0)]    // value: -(e)
        [InlineData(-2.3025850929940457,     -2.0,                     0.0)]    // value: -(ln(10))
        [InlineData(-1.5707963267948966,     -1.0,                     0.0)]    // value: -(pi / 2)
        [InlineData(-1.4426950408889634,     -1.0,                     0.0)]    // value: -(log2(e))
        [InlineData(-1.4142135623730950,     -1.0,                     0.0)]    // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,     -1.0,                     0.0)]    // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                    -1.0,                     0.0)]
        [InlineData(-0.0,                    -0.0,                     0.0)]
        [InlineData( double.NaN,              double.NaN,              0.0)]
        [InlineData( 0.0,                     0.0,                     0.0)]
        [InlineData( 0.31830988618379067,     1.0,                     0.0)]    // value:  (1 / pi)
        [InlineData( 0.43429448190325183,     1.0,                     0.0)]    // value:  (log10(e))
        [InlineData( 0.63661977236758134,     1.0,                     0.0)]    // value:  (2 / pi)
        [InlineData( 0.69314718055994531,     1.0,                     0.0)]    // value:  (ln(2))
        [InlineData( 0.70710678118654752,     1.0,                     0.0)]    // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,     1.0,                     0.0)]    // value:  (pi / 4)
        [InlineData( 1.0,                     1.0,                     0.0)]
        [InlineData( 1.1283791670955126,      2.0,                     0.0)]    // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,      2.0,                     0.0)]    // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,      2.0,                     0.0)]    // value:  (log2(e))
        [InlineData( 1.5707963267948966,      2.0,                     0.0)]    // value:  (pi / 2)
        [InlineData( 2.3025850929940457,      3.0,                     0.0)]    // value:  (ln(10))
        [InlineData( 2.7182818284590452,      3.0,                     0.0)]    // value:  (e)
        [InlineData( 3.1415926535897932,      4.0,                     0.0)]    // value:  (pi)
        [InlineData(double.PositiveInfinity, double.PositiveInfinity,  0.0)]
        public static void Ceiling_Double(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Ceiling(value), allowedVariance);
        }

        [Theory]
        [InlineData(-0.78539816339744831,    -0.0,                     0.0)]    // value: -(pi / 4)
        [InlineData(-0.70710678118654752,    -0.0,                     0.0)]    // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,    -0.0,                     0.0)]    // value: -(ln(2))
        [InlineData(-0.63661977236758134,    -0.0,                     0.0)]    // value: -(2 / pi)
        [InlineData(-0.43429448190325183,    -0.0,                     0.0)]    // value: -(log10(e))
        [InlineData(-0.31830988618379067,    -0.0,                     0.0)]    // value: -(1 / pi)
        public static void Ceiling_Double_IEEE(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Ceiling(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,          0.0)]
        [InlineData(-3.1415926535897932,      -1.0,                 CrossPlatformMachineEpsilon * 10)]  // value: -(pi)
        [InlineData(-2.7182818284590452,      -0.91173391478696510, CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.3025850929940457,      -0.66820151019031295, CrossPlatformMachineEpsilon)]       // value: -(ln(10))
        [InlineData(-1.5707963267948966,       0.0,                 CrossPlatformMachineEpsilon)]       // value: -(pi / 2)
        [InlineData(-1.4426950408889634,       0.12775121753523991, CrossPlatformMachineEpsilon)]       // value: -(log2(e))
        [InlineData(-1.4142135623730950,       0.15594369476537447, CrossPlatformMachineEpsilon)]       // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,       0.42812514788535792, CrossPlatformMachineEpsilon)]       // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                      0.54030230586813972, CrossPlatformMachineEpsilon)]
        [InlineData(-0.78539816339744831,      0.70710678118654752, CrossPlatformMachineEpsilon)]       // value: -(pi / 4),        expected:  (1 / sqrt(2))
        [InlineData(-0.70710678118654752,      0.76024459707563015, CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,      0.76923890136397213, CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.63661977236758134,      0.80410982822879171, CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.43429448190325183,      0.90716712923909839, CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.31830988618379067,      0.94976571538163866, CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0,                      1.0,                 CrossPlatformMachineEpsilon * 10)]
        [InlineData( double.NaN,               double.NaN,          0.0)]
        [InlineData( 0.0,                      1.0,                 CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.31830988618379067,      0.94976571538163866, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.90716712923909839, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.80410982822879171, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.76923890136397213, CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.76024459707563015, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.70710678118654752, CrossPlatformMachineEpsilon)]       // value:  (pi / 4),        expected:  (1 / sqrt(2))
        [InlineData( 1.0,                      0.54030230586813972, CrossPlatformMachineEpsilon)]
        [InlineData( 1.1283791670955126,       0.42812514788535792, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       0.15594369476537447, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       0.12775121753523991, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData( 1.5707963267948966,       0.0,                 CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData( 2.3025850929940457,      -0.66820151019031295, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData( 2.7182818284590452,      -0.91173391478696510, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData( 3.1415926535897932,      -1.0,                 CrossPlatformMachineEpsilon * 10)]  // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.NaN,          0.0)]
        public static void Cos(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Cos(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, double.PositiveInfinity, 0.0)]
        [InlineData(-3.1415926535897932,      11.591953275521521,      CrossPlatformMachineEpsilon * 100)]  // value:  (pi)
        [InlineData(-2.7182818284590452,      7.6101251386622884,      CrossPlatformMachineEpsilon * 10)]   // value:  (e)
        [InlineData(-2.3025850929940457,      5.05,                    CrossPlatformMachineEpsilon * 10)]   // value:  (ln(10))
        [InlineData(-1.5707963267948966,      2.5091784786580568,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData(-1.4426950408889634,      2.2341880974508023,      CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData(-1.4142135623730950,      2.1781835566085709,      CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData(-1.1283791670955126,      1.7071001431069344,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData(-1.0,                     1.5430806348152438,      CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.78539816339744831,     1.3246090892520058,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 4)
        [InlineData(-0.70710678118654752,     1.2605918365213561,      CrossPlatformMachineEpsilon * 10)]   // value:  (1 / sqrt(2))
        [InlineData(-0.69314718055994531,     1.25,                    CrossPlatformMachineEpsilon * 10)]   // value:  (ln(2))
        [InlineData(-0.63661977236758134,     1.2095794864199787,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / pi)
        [InlineData(-0.43429448190325183,     1.0957974645564909,      CrossPlatformMachineEpsilon * 10)]   // value:  (log10(e))
        [InlineData(-0.31830988618379067,     1.0510897883672876,      CrossPlatformMachineEpsilon * 10)]   // value:  (1 / pi)
        [InlineData(-0.0,                     1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( double.NaN,              double.NaN,              0.0)]
        [InlineData( 0.0,                     1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.31830988618379067,     1.0510897883672876,      CrossPlatformMachineEpsilon * 10)]   // value:  (1 / pi)
        [InlineData( 0.43429448190325183,     1.0957974645564909,      CrossPlatformMachineEpsilon * 10)]   // value:  (log10(e))
        [InlineData( 0.63661977236758134,     1.2095794864199787,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / pi)
        [InlineData( 0.69314718055994531,     1.25,                    CrossPlatformMachineEpsilon * 10)]   // value:  (ln(2))
        [InlineData( 0.70710678118654752,     1.2605918365213561,      CrossPlatformMachineEpsilon * 10)]   // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,     1.3246090892520058,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 4)
        [InlineData( 1.0,                     1.5430806348152438,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.1283791670955126,      1.7071001431069344,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,      2.1781835566085709,      CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,      2.2341880974508023,      CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,      2.5091784786580568,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,      5.05,                    CrossPlatformMachineEpsilon * 10)]   // value:  (ln(10))
        [InlineData( 2.7182818284590452,      7.6101251386622884,      CrossPlatformMachineEpsilon * 10)]   // value:  (e)
        [InlineData( 3.1415926535897932,      11.591953275521521,      CrossPlatformMachineEpsilon * 100)]  // value:  (pi)
        [InlineData( double.PositiveInfinity, double.PositiveInfinity, 0.0)]
        public static void Cosh(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Cosh(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, 0.0,                     CrossPlatformMachineEpsilon)]
        [InlineData(-3.1415926535897932,      0.043213918263772250,    CrossPlatformMachineEpsilon / 10)]   // value: -(pi)
        [InlineData(-2.7182818284590452,      0.065988035845312537,    CrossPlatformMachineEpsilon / 10)]   // value: -(e)
        [InlineData(-2.3025850929940457,      0.1,                     CrossPlatformMachineEpsilon)]        // value: -(ln(10))
        [InlineData(-1.5707963267948966,      0.20787957635076191,     CrossPlatformMachineEpsilon)]        // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      0.23629008834452270,     CrossPlatformMachineEpsilon)]        // value: -(log2(e))
        [InlineData(-1.4142135623730950,      0.24311673443421421,     CrossPlatformMachineEpsilon)]        // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      0.32355726390307110,     CrossPlatformMachineEpsilon)]        // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     0.36787944117144232,     CrossPlatformMachineEpsilon)]
        [InlineData(-0.78539816339744831,     0.45593812776599624,     CrossPlatformMachineEpsilon)]        // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     0.49306869139523979,     CrossPlatformMachineEpsilon)]        // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     0.5,                     CrossPlatformMachineEpsilon)]        // value: -(ln(2))
        [InlineData(-0.63661977236758134,     0.52907780826773535,     CrossPlatformMachineEpsilon)]        // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     0.64772148514180065,     CrossPlatformMachineEpsilon)]        // value: -(log10(e))
        [InlineData(-0.31830988618379067,     0.72737734929521647,     CrossPlatformMachineEpsilon)]        // value: -(1 / pi)
        [InlineData(-0.0,                     1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( double.NaN,              double.NaN,              0.0)]
        [InlineData( 0.0,                     1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.31830988618379067,     1.3748022274393586,      CrossPlatformMachineEpsilon * 10)]   // value:  (1 / pi)
        [InlineData( 0.43429448190325183,     1.5438734439711811,      CrossPlatformMachineEpsilon * 10)]   // value:  (log10(e))
        [InlineData( 0.63661977236758134,     1.8900811645722220,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / pi)
        [InlineData( 0.69314718055994531,     2.0,                     CrossPlatformMachineEpsilon * 10)]   // value:  (ln(2))
        [InlineData( 0.70710678118654752,     2.0281149816474725,      CrossPlatformMachineEpsilon * 10)]   // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,     2.1932800507380155,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 4)
        [InlineData( 1.0,                     2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]   //                          expected: (e)
        [InlineData( 1.1283791670955126,      3.0906430223107976,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,      4.1132503787829275,      CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,      4.2320861065570819,      CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,      4.8104773809653517,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,      10.0,                    CrossPlatformMachineEpsilon * 100)]  // value:  (ln(10))
        [InlineData( 2.7182818284590452,      15.154262241479264,      CrossPlatformMachineEpsilon * 100)]  // value:  (e)
        [InlineData( 3.1415926535897932,      23.140692632779269,      CrossPlatformMachineEpsilon * 100)]  // value:  (pi)
        [InlineData( double.PositiveInfinity, double.PositiveInfinity, 0.0)]
        public static void Exp(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Exp(value), allowedVariance);
        }

        [Fact]
        public static void Floor_Decimal()
        {
            Assert.Equal(1.0m, Math.Floor(1.1m));
            Assert.Equal(1.0m, Math.Floor(1.9m));
            Assert.Equal(-2.0m, Math.Floor(-1.1m));
        }

        [Theory]
        [InlineData(double.NegativeInfinity,  double.NegativeInfinity, 0.0)]
        [InlineData(-3.1415926535897932,     -4.0,                     0.0)]    // value: -(pi)
        [InlineData(-2.7182818284590452,     -3.0,                     0.0)]    // value: -(e)
        [InlineData(-2.3025850929940457,     -3.0,                     0.0)]    // value: -(ln(10))
        [InlineData(-1.5707963267948966,     -2.0,                     0.0)]    // value: -(pi / 2)
        [InlineData(-1.4426950408889634,     -2.0,                     0.0)]    // value: -(log2(e))
        [InlineData(-1.4142135623730950,     -2.0,                     0.0)]    // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,     -2.0,                     0.0)]    // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                    -1.0,                     0.0)]
        [InlineData(-0.78539816339744831,    -1.0,                     0.0)]    // value: -(pi / 4)
        [InlineData(-0.70710678118654752,    -1.0,                     0.0)]    // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,    -1.0,                     0.0)]    // value: -(ln(2))
        [InlineData(-0.63661977236758134,    -1.0,                     0.0)]    // value: -(2 / pi)
        [InlineData(-0.43429448190325183,    -1.0,                     0.0)]    // value: -(log10(e))
        [InlineData(-0.31830988618379067,    -1.0,                     0.0)]    // value: -(1 / pi)
        [InlineData( double.NaN,              double.NaN,              0.0)]
        [InlineData( 0.0,                     0.0,                     0.0)]
        [InlineData( 0.31830988618379067,     0.0,                     0.0)]    // value:  (1 / pi)
        [InlineData( 0.43429448190325183,     0.0,                     0.0)]    // value:  (log10(e))
        [InlineData( 0.63661977236758134,     0.0,                     0.0)]    // value:  (2 / pi)
        [InlineData( 0.69314718055994531,     0.0,                     0.0)]    // value:  (ln(2))
        [InlineData( 0.70710678118654752,     0.0,                     0.0)]    // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,     0.0,                     0.0)]    // value:  (pi / 4)
        [InlineData( 1.0,                     1.0,                     0.0)]
        [InlineData( 1.1283791670955126,      1.0,                     0.0)]    // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,      1.0,                     0.0)]    // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,      1.0,                     0.0)]    // value:  (log2(e))
        [InlineData( 1.5707963267948966,      1.0,                     0.0)]    // value:  (pi / 2)
        [InlineData( 2.3025850929940457,      2.0,                     0.0)]    // value:  (ln(10))
        [InlineData( 2.7182818284590452,      2.0,                     0.0)]    // value:  (e)
        [InlineData( 3.1415926535897932,      3.0,                     0.0)]    // value:  (pi)
        [InlineData(double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Floor_Double(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Floor(value), allowedVariance);
        }

        [Theory]
        [InlineData(-0.0,                    -0.0,                     0.0)]
        public static void Floor_Double_IEEE(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Floor(value), allowedVariance);
        }

        [Fact]
        public static void IEEERemainder()
        {
            Assert.Equal(-1.0, Math.IEEERemainder(3, 2));
            Assert.Equal(0.0, Math.IEEERemainder(4, 2));
            Assert.Equal(1.0, Math.IEEERemainder(10, 3));
            Assert.Equal(-1.0, Math.IEEERemainder(11, 3));
            Assert.Equal(-2.0, Math.IEEERemainder(28, 5));
            Assert.Equal(1.8, Math.IEEERemainder(17.8, 4), 10);
            Assert.Equal(1.4, Math.IEEERemainder(17.8, 4.1), 10);
            Assert.Equal(0.0999999999999979, Math.IEEERemainder(-16.3, 4.1), 10);
            Assert.Equal(1.4, Math.IEEERemainder(17.8, -4.1), 10);
            Assert.Equal(-1.4, Math.IEEERemainder(-17.8, -4.1), 10);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData(-3.1415926535897932,       double.NaN,              0.0)]                               //                              value: -(pi)
        [InlineData(-2.7182818284590452,       double.NaN,              0.0)]                               //                              value: -(e)
        [InlineData(-1.4142135623730950,       double.NaN,              0.0)]                               //                              value: -(sqrt(2))
        [InlineData(-1.0,                      double.NaN,              0.0)]
        [InlineData(-0.69314718055994531,      double.NaN,              0.0)]                               //                              value: -(ln(2))
        [InlineData(-0.43429448190325183,      double.NaN,              0.0)]                               //                              value: -(log10(e))
        [InlineData(-0.0,                      double.NegativeInfinity, 0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      double.NegativeInfinity, 0.0)]
        [InlineData( 0.043213918263772250,    -3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData( 0.065988035845312537,    -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData( 0.1,                     -2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData( 0.20787957635076191,     -1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData( 0.23629008834452270,     -1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData( 0.24311673443421421,     -1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData( 0.32355726390307110,     -1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData( 0.36787944117144232,     -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.45593812776599624,     -0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData( 0.49306869139523979,     -0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData( 0.5,                     -0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData( 0.52907780826773535,     -0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData( 0.64772148514180065,     -0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData( 0.72737734929521647,     -0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData( 1.0,                      0.0,                     0.0)]
        [InlineData( 1.3748022274393586,       0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 1.5438734439711811,       0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 1.8900811645722220,       0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 2.0,                      0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 2.0281149816474725,       0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 2.1932800507380155,       0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 2.7182818284590452,       1.0,                     CrossPlatformMachineEpsilon * 10)]  //                              value: (e)
        [InlineData( 3.0906430223107976,       1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 4.1132503787829275,       1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 4.2320861065570819,       1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 4.8104773809653517,       1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 10.0,                     2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 15.154262241479264,       2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 23.140692632779269,       3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Log(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Log(value), allowedVariance);
        }

        [Fact]
        public static void LogWithBase()
        {
            Assert.Equal(1.0, Math.Log(3.0, 3.0));
            Assert.Equal(2.40217350273, Math.Log(14, 3.0), 10);
            Assert.Equal(double.NegativeInfinity, Math.Log(0.0, 3.0));
            Assert.Equal(double.NaN, Math.Log(-3.0, 3.0));
            Assert.Equal(double.NaN, Math.Log(double.NaN, 3.0));
            Assert.Equal(double.PositiveInfinity, Math.Log(double.PositiveInfinity, 3.0));
            Assert.Equal(double.NaN, Math.Log(double.NegativeInfinity, 3.0));
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData(-3.1415926535897932,       double.NaN,              0.0)]                               //                              value: -(pi)
        [InlineData(-2.7182818284590452,       double.NaN,              0.0)]                               //                              value: -(e)
        [InlineData(-1.4142135623730950,       double.NaN,              0.0)]                               //                              value: -(sqrt(2))
        [InlineData(-1.0,                      double.NaN,              0.0)]
        [InlineData(-0.69314718055994531,      double.NaN,              0.0)]                               //                              value: -(ln(2))
        [InlineData(-0.43429448190325183,      double.NaN,              0.0)]                               //                              value: -(log10(e))
        [InlineData(-0.0,                      double.NegativeInfinity, 0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      double.NegativeInfinity, 0.0)]
        [InlineData( 0.00072178415907472774,  -3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData( 0.0019130141022243176,   -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData( 0.0049821282964407206,   -2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData( 0.026866041001136132,    -1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData( 0.036083192820787210,    -1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData( 0.038528884700322026,    -1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData( 0.074408205860642723,    -1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData( 0.1,                     -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.16390863613957665,     -0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData( 0.19628775993505562,     -0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData( 0.20269956628651730,     -0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData( 0.23087676451600055,     -0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData( 0.36787944117144232,     -0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData( 0.48049637305186868,     -0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData( 1.0,                      0.0,                     0.0)]
        [InlineData( 2.0811811619898573,       0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 2.7182818284590452,       0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))        value: (e)
        [InlineData( 4.3313150290214525,       0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 4.9334096679145963,       0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 5.0945611704512962,       0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 6.1009598002416937,       0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 10.0,                     1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 13.439377934644400,       1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 25.954553519470081,       1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 27.713733786437790,       1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 37.221710484165167,       1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 200.71743249053009,       2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 522.73529967043665,       2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 1385.4557313670111,       3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Log10(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Log10(value), allowedVariance);
        }

        [Fact]
        public static void Max_Byte()
        {
            Assert.Equal((byte)3, Math.Max((byte)2, (byte)3));
            Assert.Equal(byte.MaxValue, Math.Max(byte.MinValue, byte.MaxValue));
        }

        [Fact]
        public static void Max_Decimal()
        {
            Assert.Equal(3.0m, Math.Max(-2.0m, 3.0m));
            Assert.Equal(decimal.MaxValue, Math.Max(decimal.MinValue, decimal.MaxValue));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.PositiveInfinity)]
        [InlineData(double.MinValue,         double.MaxValue,         double.MaxValue)]
        [InlineData(double.MaxValue,         double.MinValue,         double.MaxValue)]
        [InlineData(double.NaN,              double.NaN,              double.NaN)]
        [InlineData(double.NaN,              1.0,                     double.NaN)]
        [InlineData(1.0,                     double.NaN,              double.NaN)]
        [InlineData(double.PositiveInfinity, double.NaN,              double.NaN)]
        [InlineData(double.NegativeInfinity, double.NaN,              double.NaN)]
        [InlineData(double.NaN,              double.PositiveInfinity, double.NaN)]
        [InlineData(double.NaN,              double.NegativeInfinity, double.NaN)]
        [InlineData(-0.0,                    0.0,                     0.0)]
        [InlineData( 0.0,                   -0.0,                     0.0)]
        [InlineData( 2.0,                   -3.0,                     2.0)]
        [InlineData(-3.0,                    2.0,                     2.0)]
        [InlineData( 3.0,                   -2.0,                     3.0)]
        [InlineData(-2.0,                    3.0,                     3.0)]
        public static void Max_Double_NotNetFramework(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, Math.Max(x, y), 0.0);

            if (double.IsNaN(x))
            {
                // Toggle the sign of the NaN to validate both +NaN and -NaN behave the same.
                // Negate should work as well but the JIT may constant fold or do other tricks
                // and normalize to a single NaN form so we do bitwise tricks to ensure we test
                // the right thing.

                ulong bits = BitConverter.DoubleToUInt64Bits(x);
                bits ^= BitConverter.DoubleToUInt64Bits(-0.0);
                x = BitConverter.UInt64BitsToDouble(bits);

                AssertExtensions.Equal(expectedResult, Math.Max(x, y), 0.0);
            }

            if (double.IsNaN(y))
            {
                ulong bits = BitConverter.DoubleToUInt64Bits(y);
                bits ^= BitConverter.DoubleToUInt64Bits(-0.0);
                y = BitConverter.UInt64BitsToDouble(bits);

                AssertExtensions.Equal(expectedResult, Math.Max(x, y), 0.0);
            }
        }

        [Fact]
        public static void Max_Int16()
        {
            Assert.Equal((short)3, Math.Max((short)(-2), (short)3));
            Assert.Equal(short.MaxValue, Math.Max(short.MinValue, short.MaxValue));
        }

        [Fact]
        public static void Max_Int32()
        {
            Assert.Equal(3, Math.Max(-2, 3));
            Assert.Equal(int.MaxValue, Math.Max(int.MinValue, int.MaxValue));
        }

        [Fact]
        public static void Max_Int64()
        {
            Assert.Equal(3L, Math.Max(-2L, 3L));
            Assert.Equal(long.MaxValue, Math.Max(long.MinValue, long.MaxValue));
        }

        [Fact]
        public static void Max_NInt()
        {
            Assert.Equal((nint)3, Math.Max((nint)(-2), (nint)3));
            Assert.Equal(nint.MaxValue, Math.Max(nint.MinValue, nint.MaxValue));
        }

        [Fact]
        public static void Max_SByte()
        {
            Assert.Equal((sbyte)3, Math.Max((sbyte)(-2), (sbyte)3));
            Assert.Equal(sbyte.MaxValue, Math.Max(sbyte.MinValue, sbyte.MaxValue));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity)]
        [InlineData(float.MinValue,         float.MaxValue,         float.MaxValue)]
        [InlineData(float.MaxValue,         float.MinValue,         float.MaxValue)]
        [InlineData(float.NaN,              float.NaN,              float.NaN)]
        [InlineData(float.NaN,              1.0,                    float.NaN)]
        [InlineData(1.0,                    float.NaN,              float.NaN)]
        [InlineData(float.PositiveInfinity, float.NaN,              float.NaN)]
        [InlineData(float.NegativeInfinity, float.NaN,              float.NaN)]
        [InlineData(float.NaN,              float.PositiveInfinity, float.NaN)]
        [InlineData(float.NaN,              float.NegativeInfinity, float.NaN)]
        [InlineData(-0.0,                   0.0,                    0.0)]
        [InlineData( 0.0,                  -0.0,                    0.0)]
        [InlineData( 2.0,                  -3.0,                    2.0)]
        [InlineData(-3.0,                   2.0,                    2.0)]
        [InlineData( 3.0,                  -2.0,                    3.0)]
        [InlineData(-2.0,                   3.0,                    3.0)]
        public static void Max_Single_NotNetFramework(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, Math.Max(x, y), 0.0f);

            if (float.IsNaN(x))
            {
                // Toggle the sign of the NaN to validate both +NaN and -NaN behave the same.
                // Negate should work as well but the JIT may constant fold or do other tricks
                // and normalize to a single NaN form so we do bitwise tricks to ensure we test
                // the right thing.

                uint bits = BitConverter.SingleToUInt32Bits(x);
                bits ^= BitConverter.SingleToUInt32Bits(-0.0f);
                x = BitConverter.UInt32BitsToSingle(bits);

                AssertExtensions.Equal(expectedResult, Math.Max(x, y), 0.0f);
            }

            if (float.IsNaN(y))
            {
                uint bits = BitConverter.SingleToUInt32Bits(y);
                bits ^= BitConverter.SingleToUInt32Bits(-0.0f);
                y = BitConverter.UInt32BitsToSingle(bits);

                AssertExtensions.Equal(expectedResult, Math.Max(x, y), 0.0f);
            }
        }

        [Fact]
        public static void Max_UInt16()
        {
            Assert.Equal((ushort)3, Math.Max((ushort)2, (ushort)3));
            Assert.Equal(ushort.MaxValue, Math.Max(ushort.MinValue, ushort.MaxValue));
        }

        [Fact]
        public static void Max_UInt32()
        {
            Assert.Equal((uint)3, Math.Max((uint)2, (uint)3));
            Assert.Equal(uint.MaxValue, Math.Max(uint.MinValue, uint.MaxValue));
        }

        [Fact]
        public static void Max_UInt64()
        {
            Assert.Equal((ulong)3, Math.Max((ulong)2, (ulong)3));
            Assert.Equal(ulong.MaxValue, Math.Max(ulong.MinValue, ulong.MaxValue));
        }

        [Fact]
        public static void Max_NUInt()
        {
            Assert.Equal((nuint)3, Math.Max((nuint)2, (nuint)3));
            Assert.Equal(nuint.MaxValue, Math.Max(nuint.MinValue, nuint.MaxValue));
        }

        [Fact]
        public static void Min_Byte()
        {
            Assert.Equal((byte)2, Math.Min((byte)3, (byte)2));
            Assert.Equal(byte.MinValue, Math.Min(byte.MinValue, byte.MaxValue));
        }

        [Fact]
        public static void Min_Decimal()
        {
            Assert.Equal(-2.0m, Math.Min(3.0m, -2.0m));
            Assert.Equal(decimal.MinValue, Math.Min(decimal.MinValue, decimal.MaxValue));
        }

        [Theory]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(double.MinValue,         double.MaxValue,         double.MinValue)]
        [InlineData(double.MaxValue,         double.MinValue,         double.MinValue)]
        [InlineData(double.NaN,              double.NaN,              double.NaN)]
        [InlineData(double.NaN,              1.0,                     double.NaN)]
        [InlineData(1.0,                     double.NaN,              double.NaN)]
        [InlineData(double.PositiveInfinity, double.NaN,              double.NaN)]
        [InlineData(double.NegativeInfinity, double.NaN,              double.NaN)]
        [InlineData(double.NaN,              double.PositiveInfinity, double.NaN)]
        [InlineData(double.NaN,              double.NegativeInfinity, double.NaN)]
        [InlineData(-0.0,                    0.0,                     -0.0)]
        [InlineData( 0.0,                   -0.0,                     -0.0)]
        [InlineData( 2.0,                   -3.0,                     -3.0)]
        [InlineData(-3.0,                    2.0,                     -3.0)]
        [InlineData( 3.0,                   -2.0,                     -2.0)]
        [InlineData(-2.0,                    3.0,                     -2.0)]
        public static void Min_Double_NotNetFramework(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, Math.Min(x, y), 0.0);

            if (double.IsNaN(x))
            {
                // Toggle the sign of the NaN to validate both +NaN and -NaN behave the same.
                // Negate should work as well but the JIT may constant fold or do other tricks
                // and normalize to a single NaN form so we do bitwise tricks to ensure we test
                // the right thing.

                ulong bits = BitConverter.DoubleToUInt64Bits(x);
                bits ^= BitConverter.DoubleToUInt64Bits(-0.0);
                x = BitConverter.UInt64BitsToDouble(bits);

                AssertExtensions.Equal(expectedResult, Math.Min(x, y), 0.0);
            }

            if (double.IsNaN(y))
            {
                ulong bits = BitConverter.DoubleToUInt64Bits(y);
                bits ^= BitConverter.DoubleToUInt64Bits(-0.0);
                y = BitConverter.UInt64BitsToDouble(bits);

                AssertExtensions.Equal(expectedResult, Math.Min(x, y), 0.0);
            }
        }

        [Fact]
        public static void Min_Int16()
        {
            Assert.Equal((short)(-2), Math.Min((short)3, (short)(-2)));
            Assert.Equal(short.MinValue, Math.Min(short.MinValue, short.MaxValue));
        }

        [Fact]
        public static void Min_Int32()
        {
            Assert.Equal(-2, Math.Min(3, -2));
            Assert.Equal(int.MinValue, Math.Min(int.MinValue, int.MaxValue));
        }

        [Fact]
        public static void Min_Int64()
        {
            Assert.Equal(-2L, Math.Min(3L, -2L));
            Assert.Equal(long.MinValue, Math.Min(long.MinValue, long.MaxValue));
        }

        [Fact]
        public static void Min_NInt()
        {
            Assert.Equal((nint)(-2), Math.Min((nint)3, (nint)(-2)));
            Assert.Equal(nint.MinValue, Math.Min(nint.MinValue, nint.MaxValue));
        }

        [Fact]
        public static void Min_SByte()
        {
            Assert.Equal((sbyte)(-2), Math.Min((sbyte)3, (sbyte)(-2)));
            Assert.Equal(sbyte.MinValue, Math.Min(sbyte.MinValue, sbyte.MaxValue));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.MinValue,         float.MaxValue,         float.MinValue)]
        [InlineData(float.MaxValue,         float.MinValue,         float.MinValue)]
        [InlineData(float.NaN,              float.NaN,              float.NaN)]
        [InlineData(float.NaN,              1.0,                    float.NaN)]
        [InlineData(1.0,                    float.NaN,              float.NaN)]
        [InlineData(float.PositiveInfinity, float.NaN,              float.NaN)]
        [InlineData(float.NegativeInfinity, float.NaN,              float.NaN)]
        [InlineData(float.NaN,              float.PositiveInfinity, float.NaN)]
        [InlineData(float.NaN,              float.NegativeInfinity, float.NaN)]
        [InlineData(-0.0,                   0.0,                    -0.0)]
        [InlineData( 0.0,                  -0.0,                    -0.0)]
        [InlineData( 2.0,                  -3.0,                    -3.0)]
        [InlineData(-3.0,                   2.0,                    -3.0)]
        [InlineData( 3.0,                  -2.0,                    -2.0)]
        [InlineData(-2.0,                   3.0,                    -2.0)]
        public static void Min_Single_NotNetFramework(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, Math.Min(x, y), 0.0f);

            if (float.IsNaN(x))
            {
                // Toggle the sign of the NaN to validate both +NaN and -NaN behave the same.
                // Negate should work as well but the JIT may constant fold or do other tricks
                // and normalize to a single NaN form so we do bitwise tricks to ensure we test
                // the right thing.

                uint bits = BitConverter.SingleToUInt32Bits(x);
                bits ^= BitConverter.SingleToUInt32Bits(-0.0f);
                x = BitConverter.UInt32BitsToSingle(bits);

                AssertExtensions.Equal(expectedResult, Math.Min(x, y), 0.0f);
            }

            if (float.IsNaN(y))
            {
                uint bits = BitConverter.SingleToUInt32Bits(y);
                bits ^= BitConverter.SingleToUInt32Bits(-0.0f);
                y = BitConverter.UInt32BitsToSingle(bits);

                AssertExtensions.Equal(expectedResult, Math.Min(x, y), 0.0f);
            }
        }

        [Fact]
        public static void Min_UInt16()
        {
            Assert.Equal((ushort)2, Math.Min((ushort)3, (ushort)2));
            Assert.Equal(ushort.MinValue, Math.Min(ushort.MinValue, ushort.MaxValue));
        }

        [Fact]
        public static void Min_UInt32()
        {
            Assert.Equal((uint)2, Math.Min((uint)3, (uint)2));
            Assert.Equal(uint.MinValue, Math.Min(uint.MinValue, uint.MaxValue));
        }

        [Fact]
        public static void Min_UInt64()
        {
            Assert.Equal((ulong)2, Math.Min((ulong)3, (ulong)2));
            Assert.Equal(ulong.MinValue, Math.Min(ulong.MinValue, ulong.MaxValue));
        }

        [Fact]
        public static void Min_NUInt()
        {
            Assert.Equal((nuint)2, Math.Min((nuint)3, (nuint)2));
            Assert.Equal(nuint.MinValue, Math.Min(nuint.MinValue, nuint.MaxValue));
        }

        public static IEnumerable<object[]> Pow_TestData
        {
            get
            {
                yield return new object[] { double.NegativeInfinity, double.NegativeInfinity, 0.0, 0.0 };
                yield return new object[] { double.NegativeInfinity, -1.0, -0.0, 0.0 };
                yield return new object[] { double.NegativeInfinity, -0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { double.NegativeInfinity, double.NaN, double.NaN, 0.0 };
                yield return new object[] { double.NegativeInfinity, 0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { double.NegativeInfinity, 1.0, double.NegativeInfinity, 0.0 };
                yield return new object[] { double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity, 0.0 };
                yield return new object[] { -10.0, double.NegativeInfinity, 0.0, 0.0 };
                yield return new object[] { -10.0, -1.5707963267948966, double.NaN, 0.0 };                                     //          y: -(pi / 2)
                yield return new object[] { -10.0, -1.0, -0.1, CrossPlatformMachineEpsilon };
                yield return new object[] { -10.0, -0.78539816339744831, double.NaN, 0.0 };                                     //          y: -(pi / 4)
                yield return new object[] { -10.0, -0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { -10.0, double.NaN, double.NaN, 0.0 };
                yield return new object[] { -10.0, 0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { -10.0, 0.78539816339744831, double.NaN, 0.0 };                                     //          y:  (pi / 4)
                yield return new object[] { -10.0, 1.0, -10.0, CrossPlatformMachineEpsilon * 100 };
                yield return new object[] { -10.0, 1.5707963267948966, double.NaN, 0.0 };                                     //          y:  (pi / 2)
                yield return new object[] { -10.0, double.PositiveInfinity, double.PositiveInfinity, 0.0 };
                yield return new object[] { -2.7182818284590452, double.NegativeInfinity, 0.0, 0.0 };                                     // x: -(e)
                yield return new object[] { -2.7182818284590452, -1.5707963267948966, double.NaN, 0.0 };                                     // x: -(e)  y: -(pi / 2)
                yield return new object[] { -2.7182818284590452, -1.0, -0.36787944117144232, CrossPlatformMachineEpsilon };             // x: -(e)
                yield return new object[] { -2.7182818284590452, -0.78539816339744831, double.NaN, 0.0 };                                     // x: -(e)  y: -(pi / 4)
                yield return new object[] { -2.7182818284590452, -0.0, 1.0, CrossPlatformMachineEpsilon * 10 };        // x: -(e)
                yield return new object[] { -2.7182818284590452, double.NaN, double.NaN, 0.0 };
                yield return new object[] { -2.7182818284590452, 0.0, 1.0, CrossPlatformMachineEpsilon * 10 };        // x: -(e)
                yield return new object[] { -2.7182818284590452, 0.78539816339744831, double.NaN, 0.0 };                                     // x: -(e)  y:  (pi / 4)
                yield return new object[] { -2.7182818284590452, 1.0, -2.7182818284590452, CrossPlatformMachineEpsilon * 10 };        // x: -(e)  expected: (e)
                yield return new object[] { -2.7182818284590452, 1.5707963267948966, double.NaN, 0.0 };                                     // x: -(e)  y:  (pi / 2)
                yield return new object[] { -2.7182818284590452, double.PositiveInfinity, double.PositiveInfinity, 0.0 };
                yield return new object[] { -1.0, -1.0, -1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { -1.0, -0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { -1.0, double.NaN, double.NaN, 0.0 };
                yield return new object[] { -1.0, 0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { -1.0, 1.0, -1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { -0.0, double.NegativeInfinity, double.PositiveInfinity, 0.0 };
                yield return new object[] { -0.0, -3.0, double.NegativeInfinity, 0.0 };
                yield return new object[] { -0.0, -2.0, double.PositiveInfinity, 0.0 };
                yield return new object[] { -0.0, -1.5707963267948966, double.PositiveInfinity, 0.0 };                                     //          y: -(pi / 2)
                yield return new object[] { -0.0, -1.0, double.NegativeInfinity, 0.0 };
                yield return new object[] { -0.0, -0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { -0.0, double.NaN, double.NaN, 0.0 };
                yield return new object[] { -0.0, 0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { -0.0, 1.0, -0.0, 0.0 };
                yield return new object[] { -0.0, 1.5707963267948966, 0.0, 0.0 };                                     //          y: -(pi / 2)
                yield return new object[] { -0.0, 2.0, 0.0, 0.0 };
                yield return new object[] { -0.0, 3.0, -0.0, 0.0 };
                yield return new object[] { -0.0, double.PositiveInfinity, 0.0, 0.0 };
                yield return new object[] { double.NaN, double.NegativeInfinity, double.NaN, 0.0 };
                yield return new object[] { double.NaN, -1.0, double.NaN, 0.0 };
                yield return new object[] { double.NaN, double.NaN, double.NaN, 0.0 };
                yield return new object[] { double.NaN, 1.0, double.NaN, 0.0 };
                yield return new object[] { double.NaN, double.PositiveInfinity, double.NaN, 0.0 };
                yield return new object[] { 0.0, double.NegativeInfinity, double.PositiveInfinity, 0.0 };
                yield return new object[] { 0.0, -3.0, double.PositiveInfinity, 0.0 };
                yield return new object[] { 0.0, -2.0, double.PositiveInfinity, 0.0 };
                yield return new object[] { 0.0, -1.5707963267948966, double.PositiveInfinity, 0.0 };                                     //          y: -(pi / 2)
                yield return new object[] { 0.0, -1.0, double.PositiveInfinity, 0.0 };
                yield return new object[] { 0.0, -0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 0.0, double.NaN, double.NaN, 0.0 };
                yield return new object[] { 0.0, 0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 0.0, 1.0, 0.0, 0.0 };
                yield return new object[] { 0.0, 1.5707963267948966, 0.0, 0.0 };                                     //          y: -(pi / 2)
                yield return new object[] { 0.0, 2.0, 0.0, 0.0 };
                yield return new object[] { 0.0, 3.0, 0.0, 0.0 };
                yield return new object[] { 0.0, double.PositiveInfinity, 0.0, 0.0 };
                yield return new object[] { 1.0, double.NegativeInfinity, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 1.0, -1.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 1.0, -0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 1.0, 0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 1.0, 1.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 1.0, double.PositiveInfinity, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 2.7182818284590452, double.NegativeInfinity, 0.0, 0.0 };
                yield return new object[] { 2.7182818284590452, -3.1415926535897932, 0.043213918263772250, CrossPlatformMachineEpsilon / 10 };        // x:  (e)  y: -(pi)
                yield return new object[] { 2.7182818284590452, -2.7182818284590452, 0.065988035845312537, CrossPlatformMachineEpsilon / 10 };        // x:  (e)  y: -(e)
                yield return new object[] { 2.7182818284590452, -2.3025850929940457, 0.1, CrossPlatformMachineEpsilon };             // x:  (e)  y: -(ln(10))
                yield return new object[] { 2.7182818284590452, -1.5707963267948966, 0.20787957635076191, CrossPlatformMachineEpsilon };             // x:  (e)  y: -(pi / 2)
                yield return new object[] { 2.7182818284590452, -1.4426950408889634, 0.23629008834452270, CrossPlatformMachineEpsilon };             // x:  (e)  y: -(log2(e))
                yield return new object[] { 2.7182818284590452, -1.4142135623730950, 0.24311673443421421, CrossPlatformMachineEpsilon };             // x:  (e)  y: -(sqrt(2))
                yield return new object[] { 2.7182818284590452, -1.1283791670955126, 0.32355726390307110, CrossPlatformMachineEpsilon };             // x:  (e)  y: -(2 / sqrt(pi))
                yield return new object[] { 2.7182818284590452, -1.0, 0.36787944117144232, CrossPlatformMachineEpsilon };             // x:  (e)
                yield return new object[] { 2.7182818284590452, -0.78539816339744831, 0.45593812776599624, CrossPlatformMachineEpsilon };             // x:  (e)  y: -(pi / 4)
                yield return new object[] { 2.7182818284590452, -0.70710678118654752, 0.49306869139523979, CrossPlatformMachineEpsilon };             // x:  (e)  y: -(1 / sqrt(2))
                yield return new object[] { 2.7182818284590452, -0.69314718055994531, 0.5, CrossPlatformMachineEpsilon };             // x:  (e)  y: -(ln(2))
                yield return new object[] { 2.7182818284590452, -0.63661977236758134, 0.52907780826773535, CrossPlatformMachineEpsilon };             // x:  (e)  y: -(2 / pi)
                yield return new object[] { 2.7182818284590452, -0.43429448190325183, 0.64772148514180065, CrossPlatformMachineEpsilon };             // x:  (e)  y: -(log10(e))
                yield return new object[] { 2.7182818284590452, -0.31830988618379067, 0.72737734929521647, CrossPlatformMachineEpsilon };             // x:  (e)  y: -(1 / pi)
                yield return new object[] { 2.7182818284590452, -0.0, 1.0, CrossPlatformMachineEpsilon * 10 };        // x:  (e)
                yield return new object[] { 2.7182818284590452, double.NaN, double.NaN, 0.0 };
                yield return new object[] { 2.7182818284590452, 0.0, 1.0, CrossPlatformMachineEpsilon * 10 };        // x:  (e)
                yield return new object[] { 2.7182818284590452, 0.31830988618379067, 1.3748022274393586, CrossPlatformMachineEpsilon * 10 };        // x:  (e)  y:  (1 / pi)
                yield return new object[] { 2.7182818284590452, 0.43429448190325183, 1.5438734439711811, CrossPlatformMachineEpsilon * 10 };        // x:  (e)  y:  (log10(e))
                yield return new object[] { 2.7182818284590452, 0.63661977236758134, 1.8900811645722220, CrossPlatformMachineEpsilon * 10 };        // x:  (e)  y:  (2 / pi)
                yield return new object[] { 2.7182818284590452, 0.69314718055994531, 2.0, CrossPlatformMachineEpsilon * 10 };        // x:  (e)  y:  (ln(2))
                yield return new object[] { 2.7182818284590452, 0.70710678118654752, 2.0281149816474725, CrossPlatformMachineEpsilon * 10 };        // x:  (e)  y:  (1 / sqrt(2))
                yield return new object[] { 2.7182818284590452, 0.78539816339744831, 2.1932800507380155, CrossPlatformMachineEpsilon * 10 };        // x:  (e)  y:  (pi / 4)
                yield return new object[] { 2.7182818284590452, 1.0, 2.7182818284590452, CrossPlatformMachineEpsilon * 10 };        // x:  (e)                      expected: (e)
                yield return new object[] { 2.7182818284590452, 1.1283791670955126, 3.0906430223107976, CrossPlatformMachineEpsilon * 10 };        // x:  (e)  y:  (2 / sqrt(pi))
                yield return new object[] { 2.7182818284590452, 1.4142135623730950, 4.1132503787829275, CrossPlatformMachineEpsilon * 10 };        // x:  (e)  y:  (sqrt(2))
                yield return new object[] { 2.7182818284590452, 1.4426950408889634, 4.2320861065570819, CrossPlatformMachineEpsilon * 10 };        // x:  (e)  y:  (log2(e))
                yield return new object[] { 2.7182818284590452, 1.5707963267948966, 4.8104773809653517, CrossPlatformMachineEpsilon * 10 };        // x:  (e)  y:  (pi / 2)
                yield return new object[] { 2.7182818284590452, 2.3025850929940457, 10.0, CrossPlatformMachineEpsilon * 100 };       // x:  (e)  y:  (ln(10))
                yield return new object[] { 2.7182818284590452, 2.7182818284590452, 15.154262241479264, CrossPlatformMachineEpsilon * 100 };       // x:  (e)  y:  (e)
                yield return new object[] { 2.7182818284590452, 3.1415926535897932, 23.140692632779269, CrossPlatformMachineEpsilon * 100 };       // x:  (e)  y:  (pi)
                yield return new object[] { 2.7182818284590452, double.PositiveInfinity, double.PositiveInfinity, 0.0 };                                     // x:  (e)
                yield return new object[] { 10.0, double.NegativeInfinity, 0.0, 0.0 };
                yield return new object[] { 10.0, -3.1415926535897932, 0.00072178415907472774, CrossPlatformMachineEpsilon / 1000 };      //          y: -(pi)
                yield return new object[] { 10.0, -2.7182818284590452, 0.0019130141022243176, CrossPlatformMachineEpsilon / 100 };       //          y: -(e)
                yield return new object[] { 10.0, -2.3025850929940457, 0.0049821282964407206, CrossPlatformMachineEpsilon / 100 };       //          y: -(ln(10))
                yield return new object[] { 10.0, -1.5707963267948966, 0.026866041001136132, CrossPlatformMachineEpsilon / 10 };        //          y: -(pi / 2)
                yield return new object[] { 10.0, -1.4426950408889634, 0.036083192820787210, CrossPlatformMachineEpsilon / 10 };        //          y: -(log2(e))
                yield return new object[] { 10.0, -1.4142135623730950, 0.038528884700322026, CrossPlatformMachineEpsilon / 10 };        //          y: -(sqrt(2))
                yield return new object[] { 10.0, -1.1283791670955126, 0.074408205860642723, CrossPlatformMachineEpsilon / 10 };        //          y: -(2 / sqrt(pi))
                yield return new object[] { 10.0, -1.0, 0.1, CrossPlatformMachineEpsilon };
                yield return new object[] { 10.0, -0.78539816339744831, 0.16390863613957665, CrossPlatformMachineEpsilon };             //          y: -(pi / 4)
                yield return new object[] { 10.0, -0.70710678118654752, 0.19628775993505562, CrossPlatformMachineEpsilon };             //          y: -(1 / sqrt(2))
                yield return new object[] { 10.0, -0.69314718055994531, 0.20269956628651730, CrossPlatformMachineEpsilon };             //          y: -(ln(2))
                yield return new object[] { 10.0, -0.63661977236758134, 0.23087676451600055, CrossPlatformMachineEpsilon };             //          y: -(2 / pi)
                yield return new object[] { 10.0, -0.43429448190325183, 0.36787944117144232, CrossPlatformMachineEpsilon };             //          y: -(log10(e))
                yield return new object[] { 10.0, -0.31830988618379067, 0.48049637305186868, CrossPlatformMachineEpsilon };             //          y: -(1 / pi)
                yield return new object[] { 10.0, -0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 10.0, double.NaN, double.NaN, 0.0 };
                yield return new object[] { 10.0, 0.0, 1.0, CrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 10.0, 0.31830988618379067, 2.0811811619898573, CrossPlatformMachineEpsilon * 10 };        //          y:  (1 / pi)
                yield return new object[] { 10.0, 0.43429448190325183, 2.7182818284590452, CrossPlatformMachineEpsilon * 10 };        //          y:  (log10(e))      expected: (e)
                yield return new object[] { 10.0, 0.63661977236758134, 4.3313150290214525, CrossPlatformMachineEpsilon * 10 };        //          y:  (2 / pi)
                yield return new object[] { 10.0, 0.69314718055994531, 4.9334096679145963, CrossPlatformMachineEpsilon * 10 };        //          y:  (ln(2))
                yield return new object[] { 10.0, 0.70710678118654752, 5.0945611704512962, CrossPlatformMachineEpsilon * 10 };        //          y:  (1 / sqrt(2))
                yield return new object[] { 10.0, 0.78539816339744831, 6.1009598002416937, CrossPlatformMachineEpsilon * 10 };        //          y:  (pi / 4)
                yield return new object[] { 10.0, 1.0, 10.0, CrossPlatformMachineEpsilon * 100 };
                yield return new object[] { 10.0, 1.1283791670955126, 13.439377934644400, CrossPlatformMachineEpsilon * 100 };       //          y:  (2 / sqrt(pi))
                yield return new object[] { 10.0, 1.4142135623730950, 25.954553519470081, CrossPlatformMachineEpsilon * 100 };       //          y:  (sqrt(2))
                yield return new object[] { 10.0, 1.4426950408889634, 27.713733786437790, CrossPlatformMachineEpsilon * 100 };       //          y:  (log2(e))
                yield return new object[] { 10.0, 1.5707963267948966, 37.221710484165167, CrossPlatformMachineEpsilon * 100 };       //          y:  (pi / 2)
                yield return new object[] { 10.0, 2.3025850929940457, 200.71743249053009, CrossPlatformMachineEpsilon * 1000 };      //          y:  (ln(10))
                yield return new object[] { 10.0, 2.7182818284590452, 522.73529967043665, CrossPlatformMachineEpsilon * 1000 };      //          y:  (e)
                yield return new object[] { 10.0, 3.1415926535897932, 1385.4557313670111, CrossPlatformMachineEpsilon * 10000 };     //          y:  (pi)
                yield return new object[] { 10.0, double.PositiveInfinity, double.PositiveInfinity, 0.0 };
                yield return new object[] { double.PositiveInfinity, double.NegativeInfinity, 0.0, 0.0 };
                yield return new object[] { double.PositiveInfinity, -1.0, 0.0, 0.0 };
                yield return new object[] { double.PositiveInfinity, -0.0, 1.0, 0.0 };
                yield return new object[] { double.PositiveInfinity, double.NaN, double.NaN, 0.0 };
                yield return new object[] { double.PositiveInfinity, 0.0, 1.0, 0.0 };
                yield return new object[] { double.PositiveInfinity, 1.0, double.PositiveInfinity, 0.0 };
                yield return new object[] { double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, 0.0 };
            }
        }

        [Theory]
        [MemberData(nameof(Pow_TestData))]
        public static void Pow(double x, double y, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Pow(x, y), allowedVariance);
        }

        [Theory]
        [InlineData(-1.0,         double.NegativeInfinity, 1.0, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0,         double.PositiveInfinity, 1.0, CrossPlatformMachineEpsilon * 10)]
        [InlineData( double.NaN, -0.0,                     1.0, CrossPlatformMachineEpsilon * 10)]
        [InlineData( double.NaN,  0.0,                     1.0, CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.0,         double.NaN,              1.0, CrossPlatformMachineEpsilon * 10)]
        public static void Pow_IEEE(float x, float y, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Pow(x, y), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, -0.0,                     0.0)]
        [InlineData(-3.1415926535897932,      -0.31830988618379069,     CrossPlatformMachineEpsilonForEstimates)]   // value: (pi)
        [InlineData(-2.7182818284590452,      -0.36787944117144233,     CrossPlatformMachineEpsilonForEstimates)]   // value: (e)
        [InlineData(-2.3025850929940457,      -0.43429448190325176,     CrossPlatformMachineEpsilonForEstimates)]   // value: (ln(10))
        [InlineData(-1.5707963267948966,      -0.63661977236758138,     CrossPlatformMachineEpsilonForEstimates)]   // value: (pi / 2)
        [InlineData(-1.4426950408889634,      -0.69314718055994529,     CrossPlatformMachineEpsilonForEstimates)]   // value: (log2(e))
        [InlineData(-1.4142135623730950,      -0.70710678118654757,     CrossPlatformMachineEpsilonForEstimates)]   // value: (sqrt(2))
        [InlineData(-1.1283791670955126,      -0.88622692545275805,     CrossPlatformMachineEpsilonForEstimates)]   // value: (2 / sqrt(pi))
        [InlineData(-1.0,                     -1.0,                     CrossPlatformMachineEpsilonForEstimates)]
        [InlineData(-0.78539816339744831,     -1.2732395447351628,      CrossPlatformMachineEpsilonForEstimates)]   // value: (pi / 4)
        [InlineData(-0.70710678118654752,     -1.4142135623730949,      CrossPlatformMachineEpsilonForEstimates)]   // value: (1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -1.4426950408889634,      CrossPlatformMachineEpsilonForEstimates)]   // value: (ln(2))
        [InlineData(-0.63661977236758134,     -1.5707963267948966,      CrossPlatformMachineEpsilonForEstimates)]   // value: (2 / pi)
        [InlineData(-0.43429448190325183,     -2.3025850929940459,      CrossPlatformMachineEpsilonForEstimates)]   // value: (log10(e))
        [InlineData(-0.31830988618379067,     -3.1415926535897931,      CrossPlatformMachineEpsilonForEstimates)]   // value: (1 / pi)
        [InlineData(-0.0,                      double.NegativeInfinity, 0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      double.PositiveInfinity, 0.0)]
        [InlineData( 0.31830988618379067,      3.1415926535897931,      CrossPlatformMachineEpsilonForEstimates)]   // value: (1 / pi)
        [InlineData( 0.43429448190325183,      2.3025850929940459,      CrossPlatformMachineEpsilonForEstimates)]   // value: (log10(e))
        [InlineData( 0.63661977236758134,      1.5707963267948966,      CrossPlatformMachineEpsilonForEstimates)]   // value: (2 / pi)
        [InlineData( 0.69314718055994531,      1.4426950408889634,      CrossPlatformMachineEpsilonForEstimates)]   // value: (ln(2))
        [InlineData( 0.70710678118654752,      1.4142135623730949,      CrossPlatformMachineEpsilonForEstimates)]   // value: (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      1.2732395447351628,      CrossPlatformMachineEpsilonForEstimates)]   // value: (pi / 4)
        [InlineData( 1.0,                      1.0,                     CrossPlatformMachineEpsilonForEstimates)]
        [InlineData( 1.1283791670955126,       0.88622692545275805,     CrossPlatformMachineEpsilonForEstimates)]   // value: (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       0.70710678118654757,     CrossPlatformMachineEpsilonForEstimates)]   // value: (sqrt(2))
        [InlineData( 1.4426950408889634,       0.69314718055994529,     CrossPlatformMachineEpsilonForEstimates)]   // value: (log2(e))
        [InlineData( 1.5707963267948966,       0.63661977236758138,     CrossPlatformMachineEpsilonForEstimates)]   // value: (pi / 2)
        [InlineData( 2.3025850929940457,       0.43429448190325176,     CrossPlatformMachineEpsilonForEstimates)]   // value: (ln(10))
        [InlineData( 2.7182818284590452,       0.36787944117144233,     CrossPlatformMachineEpsilonForEstimates)]   // value: (e)
        [InlineData( 3.1415926535897932,       0.31830988618379069,     CrossPlatformMachineEpsilonForEstimates)]   // value: (pi)
        [InlineData( double.PositiveInfinity,  0.0,                     0.0)]
        public static void ReciprocalEstimate(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.ReciprocalEstimate(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData(-3.1415926535897932,       double.NaN,              0.0)]                                       // value: (pi)
        [InlineData(-2.7182818284590452,       double.NaN,              0.0)]                                       // value: (e)
        [InlineData(-2.3025850929940457,       double.NaN,              0.0)]                                       // value: (ln(10))
        [InlineData(-1.5707963267948966,       double.NaN,              0.0)]                                       // value: (pi / 2)
        [InlineData(-1.4426950408889634,       double.NaN,              0.0)]                                       // value: (log2(e))
        [InlineData(-1.4142135623730950,       double.NaN,              0.0)]                                       // value: (sqrt(2))
        [InlineData(-1.1283791670955126,       double.NaN,              0.0)]                                       // value: (2 / sqrt(pi))
        [InlineData(-1.0,                      double.NaN,              0.0)]
        [InlineData(-0.78539816339744831,      double.NaN,              0.0)]                                       // value: (pi / 4)
        [InlineData(-0.70710678118654752,      double.NaN,              0.0)]                                       // value: (1 / sqrt(2))
        [InlineData(-0.69314718055994531,      double.NaN,              0.0)]                                       // value: (ln(2))
        [InlineData(-0.63661977236758134,      double.NaN,              0.0)]                                       // value: (2 / pi)
        [InlineData(-0.43429448190325183,      double.NaN,              0.0)]                                       // value: (log10(e))
        [InlineData(-0.31830988618379067,      double.NaN,              0.0)]                                       // value: (1 / pi)
        [InlineData(-0.0,                      double.NegativeInfinity, 0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      double.PositiveInfinity, 0.0)]
        [InlineData( 0.31830988618379067,      1.7724538509055161,      CrossPlatformMachineEpsilonForEstimates)]   // value: (1 / pi)
        [InlineData( 0.43429448190325183,      1.5174271293851465,      CrossPlatformMachineEpsilonForEstimates)]   // value: (log10(e))
        [InlineData( 0.63661977236758134,      1.2533141373155001,      CrossPlatformMachineEpsilonForEstimates)]   // value: (2 / pi)
        [InlineData( 0.69314718055994531,      1.2011224087864498,      CrossPlatformMachineEpsilonForEstimates)]   // value: (ln(2))
        [InlineData( 0.70710678118654752,      1.189207115002721,       CrossPlatformMachineEpsilonForEstimates)]   // value: (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      1.1283791670955126,      CrossPlatformMachineEpsilonForEstimates)]   // value: (pi / 4)
        [InlineData( 1.0,                      1.0,                     CrossPlatformMachineEpsilonForEstimates)]
        [InlineData( 1.1283791670955126,       0.94139626377671493,     CrossPlatformMachineEpsilonForEstimates)]   // value: (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       0.84089641525371461,     CrossPlatformMachineEpsilonForEstimates)]   // value: (sqrt(2))
        [InlineData( 1.4426950408889634,       0.83255461115769769,     CrossPlatformMachineEpsilonForEstimates)]   // value: (log2(e))
        [InlineData( 1.5707963267948966,       0.79788456080286541,     CrossPlatformMachineEpsilonForEstimates)]   // value: (pi / 2)
        [InlineData( 2.3025850929940457,       0.6590102289822608,      CrossPlatformMachineEpsilonForEstimates)]   // value: (ln(10))
        [InlineData( 2.7182818284590452,       0.60653065971263342,     CrossPlatformMachineEpsilonForEstimates)]   // value: (e)
        [InlineData( 3.1415926535897932,       0.56418958354775628,     CrossPlatformMachineEpsilonForEstimates)]   // value: (pi)
        [InlineData( double.PositiveInfinity,  0.0,                     0.0)]
        public static void ReciprocalSqrtEstimate(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.ReciprocalSqrtEstimate(value), allowedVariance);
        }

        [Fact]
        public static void Round_Decimal()
        {
            Assert.Equal(0.0m, Math.Round(0.0m));
            Assert.Equal(1.0m, Math.Round(1.4m));
            Assert.Equal(2.0m, Math.Round(1.5m));
            Assert.Equal(2e16m, Math.Round(2e16m));
            Assert.Equal(0.0m, Math.Round(-0.0m));
            Assert.Equal(-1.0m, Math.Round(-1.4m));
            Assert.Equal(-2.0m, Math.Round(-1.5m));
            Assert.Equal(-2e16m, Math.Round(-2e16m));
        }

        [Fact]
        public static void Round_Double()
        {
            Assert.Equal(0.0, Math.Round(0.0));
            Assert.Equal(1.0, Math.Round(1.4));
            Assert.Equal(2.0, Math.Round(1.5));
            Assert.Equal(2e16, Math.Round(2e16));
            Assert.Equal(0.0, Math.Round(-0.0));
            Assert.Equal(-1.0, Math.Round(-1.4));
            Assert.Equal(-2.0, Math.Round(-1.5));
            Assert.Equal(-2e16, Math.Round(-2e16));
        }

        [Fact]
        public static void Round_Double_Digits_SpecificCases()
        {
            Assert.Equal(3.422, Math.Round(3.42156, 3, MidpointRounding.AwayFromZero), 10);
            Assert.Equal(-3.422, Math.Round(-3.42156, 3, MidpointRounding.AwayFromZero), 10);
            Assert.Equal(0.0, Math.Round(0.0, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(double.NaN, Math.Round(double.NaN, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(double.PositiveInfinity, Math.Round(double.PositiveInfinity, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(double.NegativeInfinity, Math.Round(double.NegativeInfinity, 3, MidpointRounding.AwayFromZero));
        }

        [Fact]
        public static void Sign_Decimal()
        {
            Assert.Equal(0, Math.Sign(0.0m));
            Assert.Equal(0, Math.Sign(-0.0m));
            Assert.Equal(-1, Math.Sign(-3.14m));
            Assert.Equal(1, Math.Sign(3.14m));
        }

        [Fact]
        public static void Sign_Double()
        {
            Assert.Equal(0, Math.Sign(0.0));
            Assert.Equal(0, Math.Sign(-0.0));
            Assert.Equal(-1, Math.Sign(-3.14));
            Assert.Equal(1, Math.Sign(3.14));
            Assert.Equal(-1, Math.Sign(double.NegativeInfinity));
            Assert.Equal(1, Math.Sign(double.PositiveInfinity));
            Assert.Throws<ArithmeticException>(() => Math.Sign(double.NaN));
        }

        [Fact]
        public static void Sign_Int16()
        {
            Assert.Equal(0, Math.Sign((short)0));
            Assert.Equal(-1, Math.Sign((short)(-3)));
            Assert.Equal(1, Math.Sign((short)3));
        }

        [Fact]
        public static void Sign_Int32()
        {
            Assert.Equal(0, Math.Sign(0));
            Assert.Equal(-1, Math.Sign(-3));
            Assert.Equal(1, Math.Sign(3));
        }

        [Fact]
        public static void Sign_Int64()
        {
            Assert.Equal(0, Math.Sign(0));
            Assert.Equal(-1, Math.Sign(-3));
            Assert.Equal(1, Math.Sign(3));
        }

        [Fact]
        public static void Sign_NInt()
        {
            Assert.Equal(0, Math.Sign((nint)0));
            Assert.Equal(-1, Math.Sign((nint)(-3)));
            Assert.Equal(1, Math.Sign((nint)3));
        }

        [Fact]
        public static void Sign_SByte()
        {
            Assert.Equal(0, Math.Sign((sbyte)0));
            Assert.Equal(-1, Math.Sign((sbyte)(-3)));
            Assert.Equal(1, Math.Sign((sbyte)3));
        }

        [Fact]
        public static void Sign_Single()
        {
            Assert.Equal(0, Math.Sign(0.0f));
            Assert.Equal(0, Math.Sign(-0.0f));
            Assert.Equal(-1, Math.Sign(-3.14f));
            Assert.Equal(1, Math.Sign(3.14f));
            Assert.Equal(-1, Math.Sign(float.NegativeInfinity));
            Assert.Equal(1, Math.Sign(float.PositiveInfinity));
            Assert.Throws<ArithmeticException>(() => Math.Sign(float.NaN));
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,          0.0)]
        [InlineData(-3.1415926535897932,      -0.0,                 CrossPlatformMachineEpsilon)]       // value: -(pi)
        [InlineData(-2.7182818284590452,      -0.41078129050290870, CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.3025850929940457,      -0.74398033695749319, CrossPlatformMachineEpsilon)]       // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -1.0,                 CrossPlatformMachineEpsilon * 10)]  // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -0.99180624439366372, CrossPlatformMachineEpsilon)]       // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -0.98776594599273553, CrossPlatformMachineEpsilon)]       // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -0.90371945743584630, CrossPlatformMachineEpsilon)]       // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -0.84147098480789651, CrossPlatformMachineEpsilon)]
        [InlineData(-0.78539816339744831,     -0.70710678118654752, CrossPlatformMachineEpsilon)]       // value: -(pi / 4),        expected: -(1 / sqrt(2))
        [InlineData(-0.70710678118654752,     -0.64963693908006244, CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.63896127631363480, CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.59448076852482208, CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.42077048331375735, CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.31296179620778659, CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                 0.0)]
        [InlineData( double.NaN,               double.NaN,          0.0)]
        [InlineData( 0.0,                      0.0,                 0.0)]
        [InlineData( 0.31830988618379067,      0.31296179620778659, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.42077048331375735, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.59448076852482208, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.63896127631363480, CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.64963693908006244, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.70710678118654752, CrossPlatformMachineEpsilon)]       // value:  (pi / 4),        expected:  (1 / sqrt(2))
        [InlineData( 1.0,                      0.84147098480789651, CrossPlatformMachineEpsilon)]
        [InlineData( 1.1283791670955126,       0.90371945743584630, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       0.98776594599273553, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       0.99180624439366372, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData( 1.5707963267948966,       1.0,                 CrossPlatformMachineEpsilon * 10)]  // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       0.74398033695749319, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData( 2.7182818284590452,       0.41078129050290870, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData( 3.1415926535897932,       0.0,                 CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.NaN,          0.0)]
        public static void Sin(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Sin(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,           double.NaN,          0.0,                              0.0)]
        [InlineData(-1e18,                     0.9929693207404051,   0.11837199021871073, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // https://github.com/dotnet/runtime/issues/98204
        [InlineData(-3.1415926535897932,      -0.0,                 -1.0,                 CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon * 10)]  // value: -(pi)
        [InlineData(-2.7182818284590452,      -0.41078129050290870, -0.91173391478696510, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.3025850929940457,      -0.74398033695749319, -0.66820151019031295, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -1.0,                  0.0,                 CrossPlatformMachineEpsilon * 10, CrossPlatformMachineEpsilon)]       // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -0.99180624439366372,  0.12775121753523991, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -0.98776594599273553,  0.15594369476537447, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -0.90371945743584630,  0.42812514788535792, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -0.84147098480789651,  0.54030230586813972, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]
        [InlineData(-0.78539816339744831,     -0.70710678118654752,  0.70710678118654752, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(pi / 4),        expected_sin: -(1 / sqrt(2)),    expected_cos: 1
        [InlineData(-0.70710678118654752,     -0.64963693908006244,  0.76024459707563015, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.63896127631363480,  0.76923890136397213, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.59448076852482208,  0.80410982822879171, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.42077048331375735,  0.90716712923909839, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.31296179620778659,  0.94976571538163866, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                  1.0,                 0.0,                              CrossPlatformMachineEpsilon * 10)]
        [InlineData( double.NaN,               double.NaN,           double.NaN,          0.0,                              0.0)]
        [InlineData( 0.0,                      0.0,                  1.0,                 0.0,                              CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.31830988618379067,      0.31296179620778659,  0.94976571538163866, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.42077048331375735,  0.90716712923909839, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.59448076852482208,  0.80410982822879171, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.63896127631363480,  0.76923890136397213, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.64963693908006244,  0.76024459707563015, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.70710678118654752,  0.70710678118654752, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (pi / 4),        expected_sin:  (1 / sqrt(2)),    expected_cos: 1
        [InlineData( 1.0,                      0.84147098480789651,  0.54030230586813972, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]
        [InlineData( 1.1283791670955126,       0.90371945743584630,  0.42812514788535792, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       0.98776594599273553,  0.15594369476537447, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       0.99180624439366372,  0.12775121753523991, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData( 1.5707963267948966,       1.0,                  0.0,                 CrossPlatformMachineEpsilon * 10, CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       0.74398033695749319, -0.66820151019031295, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData( 2.7182818284590452,       0.41078129050290870, -0.91173391478696510, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData( 3.1415926535897932,       0.0,                 -1.0,                 CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon * 10)]  // value:  (pi)
        [InlineData( 1e18,                    -0.9929693207404051,   0.11837199021871073, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // https://github.com/dotnet/runtime/issues/98204
        [InlineData( double.PositiveInfinity,  double.NaN,           double.NaN,          0.0,                              0.0)]
        public static void SinCos(double value, double expectedResultSin, double expectedResultCos, double allowedVarianceSin, double allowedVarianceCos)
        {
            (double resultSin, double resultCos) = Math.SinCos(value);
            AssertExtensions.Equal(expectedResultSin, resultSin, allowedVarianceSin);
            AssertExtensions.Equal(expectedResultCos, resultCos, allowedVarianceCos);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NegativeInfinity, 0.0)]
        [InlineData(-3.1415926535897932,      -11.548739357257748,      CrossPlatformMachineEpsilon * 100)]     // value: -(pi)
        [InlineData(-2.7182818284590452,      -7.5441371028169758,      CrossPlatformMachineEpsilon * 10)]      // value: -(e)
        [InlineData(-2.3025850929940457,      -4.95,                    CrossPlatformMachineEpsilon * 10)]      // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -2.3012989023072949,      CrossPlatformMachineEpsilon * 10)]      // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -1.9978980091062796,      CrossPlatformMachineEpsilon * 10)]      // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -1.9350668221743567,      CrossPlatformMachineEpsilon * 10)]      // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -1.3835428792038633,      CrossPlatformMachineEpsilon * 10)]      // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -1.1752011936438015,      CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.78539816339744831,     -0.86867096148600961,     CrossPlatformMachineEpsilon)]           // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.76752314512611633,     CrossPlatformMachineEpsilon)]           // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.75,                    CrossPlatformMachineEpsilon)]           // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.68050167815224332,     CrossPlatformMachineEpsilon)]           // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.44807597941469025,     CrossPlatformMachineEpsilon)]           // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.32371243907207108,     CrossPlatformMachineEpsilon)]           // value: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.31830988618379067,      0.32371243907207108,     CrossPlatformMachineEpsilon)]           // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.44807597941469025,     CrossPlatformMachineEpsilon)]           // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.68050167815224332,     CrossPlatformMachineEpsilon)]           // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.75,                    CrossPlatformMachineEpsilon)]           // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.76752314512611633,     CrossPlatformMachineEpsilon)]           // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.86867096148600961,     CrossPlatformMachineEpsilon)]           // value:  (pi / 4)
        [InlineData( 1.0,                      1.1752011936438015,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.1283791670955126,       1.3835428792038633,      CrossPlatformMachineEpsilon * 10)]      // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       1.9350668221743567,      CrossPlatformMachineEpsilon * 10)]      // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       1.9978980091062796,      CrossPlatformMachineEpsilon * 10)]      // value:  (log2(e))
        [InlineData( 1.5707963267948966,       2.3012989023072949,      CrossPlatformMachineEpsilon * 10)]      // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       4.95,                    CrossPlatformMachineEpsilon * 10)]      // value:  (ln(10))
        [InlineData( 2.7182818284590452,       7.5441371028169758,      CrossPlatformMachineEpsilon * 10)]      // value:  (e)
        [InlineData( 3.1415926535897932,       11.548739357257748,      CrossPlatformMachineEpsilon * 100)]     // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Sinh(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Sinh(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,             0.0)]
        [InlineData(-3.1415926535897932,       double.NaN,             0.0)]                                 // value: (pi)
        [InlineData(-2.7182818284590452,       double.NaN,             0.0)]                                 // value: (e)
        [InlineData(-2.3025850929940457,       double.NaN,             0.0)]                                 // value: (ln(10))
        [InlineData(-1.5707963267948966,       double.NaN,             0.0)]                                 // value: (pi / 2)
        [InlineData(-1.4426950408889634,       double.NaN,             0.0)]                                 // value: (log2(e))
        [InlineData(-1.4142135623730950,       double.NaN,             0.0)]                                 // value: (sqrt(2))
        [InlineData(-1.1283791670955126,       double.NaN,             0.0)]                                 // value: (2 / sqrt(pi))
        [InlineData(-1.0,                      double.NaN,             0.0)]
        [InlineData(-0.78539816339744831,      double.NaN,             0.0)]                                 // value: (pi / 4)
        [InlineData(-0.70710678118654752,      double.NaN,             0.0)]                                 // value: (1 / sqrt(2))
        [InlineData(-0.69314718055994531,      double.NaN,             0.0)]                                 // value: (ln(2))
        [InlineData(-0.63661977236758134,      double.NaN,             0.0)]                                 // value: (2 / pi)
        [InlineData(-0.43429448190325183,      double.NaN,             0.0)]                                 // value: (log10(e))
        [InlineData(-0.31830988618379067,      double.NaN,             0.0)]                                 // value: (1 / pi)
        [InlineData(-0.0,                     -0.0,                    0.0)]
        [InlineData( double.NaN,               double.NaN,             0.0)]
        [InlineData( 0.0,                      0.0,                    0.0)]
        [InlineData( 0.31830988618379067,      0.56418958354775629,    CrossPlatformMachineEpsilon)]        // value: (1 / pi)
        [InlineData( 0.43429448190325183,      0.65901022898226081,    CrossPlatformMachineEpsilon)]        // value: (log10(e))
        [InlineData( 0.63661977236758134,      0.79788456080286536,    CrossPlatformMachineEpsilon)]        // value: (2 / pi)
        [InlineData( 0.69314718055994531,      0.83255461115769776,    CrossPlatformMachineEpsilon)]        // value: (ln(2))
        [InlineData( 0.70710678118654752,      0.84089641525371454,    CrossPlatformMachineEpsilon)]        // value: (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.88622692545275801,    CrossPlatformMachineEpsilon)]        // value: (pi / 4)
        [InlineData( 1.0,                      1.0,                    CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.1283791670955126,       1.0622519320271969,     CrossPlatformMachineEpsilon * 10)]   // value: (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       1.1892071150027211,     CrossPlatformMachineEpsilon * 10)]   // value: (sqrt(2))
        [InlineData( 1.4426950408889634,       1.2011224087864498,     CrossPlatformMachineEpsilon * 10)]   // value: (log2(e))
        [InlineData( 1.5707963267948966,       1.2533141373155003,     CrossPlatformMachineEpsilon * 10)]   // value: (pi / 2)
        [InlineData( 2.3025850929940457,       1.5174271293851464,     CrossPlatformMachineEpsilon * 10)]   // value: (ln(10))
        [InlineData( 2.7182818284590452,       1.6487212707001281,     CrossPlatformMachineEpsilon * 10)]   // value: (e)
        [InlineData( 3.1415926535897932,       1.7724538509055160,     CrossPlatformMachineEpsilon * 10)]   // value: (pi)
        [InlineData( double.PositiveInfinity, double.PositiveInfinity, 0.0)]
        public static void Sqrt(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Sqrt(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData(-3.1415926535897932,      -0.0,                     CrossPlatformMachineEpsilon)]       // value: -(pi)
        [InlineData(-2.7182818284590452,       0.45054953406980750,     CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.3025850929940457,       1.1134071468135374,      CrossPlatformMachineEpsilon * 10)]  // value: -(ln(10))
        [InlineData(-1.4426950408889634,      -7.7635756709721848,      CrossPlatformMachineEpsilon * 10)]  // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -6.3341191670421916,      CrossPlatformMachineEpsilon * 10)]  // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -2.1108768356626451,      CrossPlatformMachineEpsilon * 10)]  // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -1.5574077246549022,      CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.78539816339744831,     -1.0,                     CrossPlatformMachineEpsilon * 10)]  // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.85451043200960189,     CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.83064087786078395,     CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.73930295048660405,     CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.46382906716062964,     CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.32951473309607836,     CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.31830988618379067,      0.32951473309607836,     CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.46382906716062964,     CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.73930295048660405,     CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.83064087786078395,     CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.85451043200960189,     CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      1.0,                     CrossPlatformMachineEpsilon * 10)]  // value:  (pi / 4)
        [InlineData( 1.0,                      1.5574077246549022,      CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.1283791670955126,       2.1108768356626451,      CrossPlatformMachineEpsilon * 10)]  // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       6.3341191670421916,      CrossPlatformMachineEpsilon * 10)]  // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       7.7635756709721848,      CrossPlatformMachineEpsilon * 10)]  // value:  (log2(e))
        [InlineData( 2.3025850929940457,      -1.1134071468135374,      CrossPlatformMachineEpsilon * 10)]  // value:  (ln(10))
        [InlineData( 2.7182818284590452,      -0.45054953406980750,     CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData( 3.1415926535897932,       0.0,                     CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.NaN,              0.0)]
        public static void Tan(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Tan(value), allowedVariance);
        }

        [Theory]
        [InlineData(-1.5707963267948966,      -16331239353195370.0,     0.0)]                               // value: -(pi / 2)
        [InlineData( 1.5707963267948966,       16331239353195370.0,     0.0)]                               // value:  (pi / 2)
        public static void Tan_PiOver2(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Tan(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, -1.0,                 CrossPlatformMachineEpsilon * 10)]
        [InlineData(-3.1415926535897932,      -0.99627207622074994, CrossPlatformMachineEpsilon)]       // value: -(pi)
        [InlineData(-2.7182818284590452,      -0.99132891580059984, CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.3025850929940457,      -0.98019801980198020, CrossPlatformMachineEpsilon)]       // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -0.91715233566727435, CrossPlatformMachineEpsilon)]       // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -0.89423894585503855, CrossPlatformMachineEpsilon)]       // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -0.88838556158566054, CrossPlatformMachineEpsilon)]       // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -0.81046380599898809, CrossPlatformMachineEpsilon)]       // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -0.76159415595576489, CrossPlatformMachineEpsilon)]
        [InlineData(-0.78539816339744831,     -0.65579420263267244, CrossPlatformMachineEpsilon)]       // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.60885936501391381, CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.6,                 CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.56259360033158334, CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.40890401183401433, CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.30797791269089433, CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                 0.0)]
        [InlineData( double.NaN,               double.NaN,          0.0)]
        [InlineData( 0.0,                      0.0,                 0.0)]
        [InlineData( 0.31830988618379067,      0.30797791269089433, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.40890401183401433, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.56259360033158334, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.6,                 CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.60885936501391381, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.65579420263267244, CrossPlatformMachineEpsilon)]       // value:  (pi / 4)
        [InlineData( 1.0,                      0.76159415595576489, CrossPlatformMachineEpsilon)]
        [InlineData( 1.1283791670955126,       0.81046380599898809, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       0.88838556158566054, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       0.89423894585503855, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData( 1.5707963267948966,       0.91715233566727435, CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       0.98019801980198020, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData( 2.7182818284590452,       0.99132891580059984, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData( 3.1415926535897932,       0.99627207622074994, CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData( double.PositiveInfinity,  1.0,                 CrossPlatformMachineEpsilon * 10)]
        public static void Tanh(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Tanh(value), allowedVariance);
        }

        [Fact]
        public static void Truncate_Decimal()
        {
            Assert.Equal(0.0m, Math.Truncate(0.12345m));
            Assert.Equal(3.0m, Math.Truncate(3.14159m));
            Assert.Equal(-3.0m, Math.Truncate(-3.14159m));
        }

        [Fact]
        public static void Truncate_Double()
        {
            Assert.Equal(0.0, Math.Truncate(0.12345));
            Assert.Equal(3.0, Math.Truncate(3.14159));
            Assert.Equal(-3.0, Math.Truncate(-3.14159));
        }

        [Fact]
        public static void BigMul()
        {
            Assert.Equal(4611686014132420609L, Math.BigMul(2147483647, 2147483647));
            Assert.Equal(0L, Math.BigMul(0, 0));
        }

        [Theory]
        [InlineData(0U, 0U, "00000000000000000000000000000000")]
        [InlineData(0U, 1U, "00000000000000000000000000000000")]
        [InlineData(1U, 0U, "00000000000000000000000000000000")]
        [InlineData(2U, 3U, "00000000000000000000000000000006")]
        [InlineData(ulong.MaxValue, 2, "0000000000000001FFFFFFFFFFFFFFFE")]
        [InlineData(ulong.MaxValue, 1, "0000000000000000FFFFFFFFFFFFFFFF")]
        [InlineData(ulong.MaxValue, ulong.MaxValue, "FFFFFFFFFFFFFFFE0000000000000001")]
        [InlineData(ulong.MaxValue, 3, "0000000000000002FFFFFFFFFFFFFFFD")]
        [InlineData(0xE8FAF08929B46BB5, 0x26B442D59782BA17, "23394CF8915296631EB6255F4A612F43")]
        public static void BigMul128_Unsigned(ulong a, ulong b, string result)
        {
            ulong high = Math.BigMul(a, b, out ulong low);
            Assert.Equal(result, $"{high:X16}{low:X16}");
        }

        [Theory]
        [InlineData(0L, 0L, "00000000000000000000000000000000")]
        [InlineData(0L, 1L, "00000000000000000000000000000000")]
        [InlineData(1L, 0L, "00000000000000000000000000000000")]
        [InlineData(2L, 3L, "00000000000000000000000000000006")]
        [InlineData(3L, -2L, "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFA")]
        [InlineData(-1L, -1L, "00000000000000000000000000000001")]
        [InlineData(-1L, long.MinValue, "00000000000000008000000000000000")]
        [InlineData(1L, long.MinValue, "FFFFFFFFFFFFFFFF8000000000000000")]
        [InlineData(0x7DD8FD06E61C42C7, 0x23B8308969A5D354, "118F366A0AEB79CDB340AA067592EE4C")]
        [InlineData(0x6DACB8FC835F41B5, -0x2D90EF8C7ED29BBA, "EC7A8BB31D6035AD27742486E387AB7E")]
        [InlineData(-0x166FA7C456154C28, 0x13CF93153370AB0B, "FE43855FCCDA31541A45864AC9B70248")]
        [InlineData(-0x57A14FB8778E4F94, -0x33BDC4C7D41A44C9, "11B61855830A65CBA363C1FE50E7CB34")]
        public static void BigMul128_Signed(long a, long b, string result)
        {
            long high = Math.BigMul(a, b, out long low);
            Assert.Equal(result, $"{high:X16}{low:X16}");
        }

        [Theory]
        [InlineData(sbyte.MaxValue, sbyte.MaxValue, 1, 0)]
        [InlineData(sbyte.MaxValue, 1, sbyte.MaxValue, 0)]
        [InlineData(sbyte.MaxValue, 2, 63, 1)]
        [InlineData(sbyte.MaxValue, -1, -127, 0)]
        [InlineData(11, 22, 0, 11)]
        [InlineData(80, 22, 3, 14)]
        [InlineData(80, -22, -3, 14)]
        [InlineData(-80, 22, -3, -14)]
        [InlineData(-80, -22, 3, -14)]
        [InlineData(0, 1, 0, 0)]
        [InlineData(0, sbyte.MaxValue, 0, 0)]
        [InlineData(sbyte.MinValue, sbyte.MaxValue, -1, -1)]
        [InlineData(sbyte.MaxValue, 0, 0, 0)]
        [InlineData(1, 0, 0, 0)]
        [InlineData(0, 0, 0, 0)]
        public static void DivRemSByte(sbyte dividend, sbyte divisor, sbyte expectedQuotient, sbyte expectedRemainder)
        {
            if (divisor == 0)
            {
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor));
            }
            else
            {
                var (actualQuotient, actualRemainder) = Math.DivRem(dividend, divisor);
                Assert.Equal(expectedQuotient, actualQuotient);
                Assert.Equal(expectedRemainder, actualRemainder);
            }
        }

        [Theory]
        [InlineData(byte.MaxValue, byte.MaxValue, 1, 0)]
        [InlineData(byte.MaxValue, 1, byte.MaxValue, 0)]
        [InlineData(byte.MaxValue, 2, 127, 1)]
        [InlineData(52, 5, 10, 2)]
        [InlineData(100, 33, 3, 1)]
        [InlineData(0, 1, 0, 0)]
        [InlineData(0, byte.MaxValue, 0, 0)]
        [InlineData(250, 50, 5, 0)]
        [InlineData(byte.MaxValue, 0, 0, 0)]
        [InlineData(1, 0, 0, 0)]
        [InlineData(0, 0, 0, 0)]
        public static void DivRemByte(byte dividend, byte divisor, byte expectedQuotient, byte expectedRemainder)
        {
            if (divisor == 0)
            {
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor));
            }
            else
            {
                var (actualQuotient, actualRemainder) = Math.DivRem(dividend, divisor);
                Assert.Equal(expectedQuotient, actualQuotient);
                Assert.Equal(expectedRemainder, actualRemainder);
            }
        }

        [Theory]
        [InlineData(short.MaxValue, short.MaxValue, 1, 0)]
        [InlineData(short.MaxValue, 1, short.MaxValue, 0)]
        [InlineData(short.MaxValue, 2, 16383, 1)]
        [InlineData(short.MaxValue, -1, -32767, 0)]
        [InlineData(12345, 22424, 0, 12345)]
        [InlineData(300, 22, 13, 14)]
        [InlineData(300, -22, -13, 14)]
        [InlineData(-300, 22, -13, -14)]
        [InlineData(-300, -22, 13, -14)]
        [InlineData(0, 1, 0, 0)]
        [InlineData(0, short.MaxValue, 0, 0)]
        [InlineData(short.MinValue, short.MaxValue, -1, -1)]
        [InlineData(13952, 2000, 6, 1952)]
        [InlineData(short.MaxValue, 0, 0, 0)]
        [InlineData(1, 0, 0, 0)]
        [InlineData(0, 0, 0, 0)]
        public static void DivRemInt16(short dividend, short divisor, short expectedQuotient, short expectedRemainder)
        {
            if (divisor == 0)
            {
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor));
            }
            else
            {
                var (actualQuotient, actualRemainder) = Math.DivRem(dividend, divisor);
                Assert.Equal(expectedQuotient, actualQuotient);
                Assert.Equal(expectedRemainder, actualRemainder);
            }
        }

        [Theory]
        [InlineData(ushort.MaxValue, ushort.MaxValue, 1, 0)]
        [InlineData(ushort.MaxValue, 1, ushort.MaxValue, 0)]
        [InlineData(ushort.MaxValue, 2, 32767, 1)]
        [InlineData(12345, 42424, 0, 12345)]
        [InlineData(51474, 31474, 1, 20000)]
        [InlineData(10000, 333, 30, 10)]
        [InlineData(0, 1, 0, 0)]
        [InlineData(0, ushort.MaxValue, 0, 0)]
        [InlineData(13952, 2000, 6, 1952)]
        [InlineData(ushort.MaxValue, 0, 0, 0)]
        [InlineData(1, 0, 0, 0)]
        [InlineData(0, 0, 0, 0)]
        public static void DivRemUInt16(ushort dividend, ushort divisor, ushort expectedQuotient, ushort expectedRemainder)
        {
            if (divisor == 0)
            {
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor));
            }
            else
            {
                var (actualQuotient, actualRemainder) = Math.DivRem(dividend, divisor);
                Assert.Equal(expectedQuotient, actualQuotient);
                Assert.Equal(expectedRemainder, actualRemainder);
            }
        }

        [Theory]
        [InlineData(2147483647, 2000, 1073741, 1647)]
        [InlineData(13952, 2000, 6, 1952)]
        [InlineData(0, 2000, 0, 0)]
        [InlineData(-14032, 2000, -7, -32)]
        [InlineData(-2147483648, 2000, -1073741, -1648)]
        [InlineData(2147483647, -2000, -1073741, 1647)]
        [InlineData(13952, -2000, -6, 1952)]
        [InlineData(13952, 0, 0, 0)]
        [InlineData(int.MaxValue, 0, 0, 0)]
        [InlineData(0, 0, 0, 0)]
        public static void DivRemInt32(int dividend, int divisor, int expectedQuotient, int expectedRemainder)
        {
            if (divisor == 0)
            {
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor));
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor, out int remainder));
            }
            else
            {
                Assert.Equal(expectedQuotient, Math.DivRem(dividend, divisor, out int remainder));
                Assert.Equal(expectedRemainder, remainder);

                var (actualQuotient, actualRemainder) = Math.DivRem(dividend, divisor);
                Assert.Equal(expectedQuotient, actualQuotient);
                Assert.Equal(expectedRemainder, actualRemainder);
            }
            if (IntPtr.Size == 4)
            {
                DivRemNativeInt(dividend, divisor, expectedQuotient, expectedRemainder);
            }
        }

        [Theory]
        [InlineData(uint.MaxValue, uint.MaxValue, 1, 0)]
        [InlineData(uint.MaxValue, 1, uint.MaxValue, 0)]
        [InlineData(uint.MaxValue, 2, 2147483647, 1)]
        [InlineData(123456789, 4242424242, 0, 123456789)]
        [InlineData(514748364, 3147483647, 0, 514748364)]
        [InlineData(1000000, 333, 3003, 1)]
        [InlineData(0, 1, 0, 0)]
        [InlineData(0UL, uint.MaxValue, 0, 0)]
        [InlineData(13952, 2000, 6, 1952)]
        [InlineData(uint.MaxValue, 0, 0, 0)]
        [InlineData(1, 0, 0, 0)]
        [InlineData(0, 0, 0, 0)]
        public static void DivRemUInt32(uint dividend, uint divisor, uint expectedQuotient, uint expectedRemainder)
        {
            if (divisor == 0)
            {
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor));
            }
            else
            {
                var (actualQuotient, actualRemainder) = Math.DivRem(dividend, divisor);
                Assert.Equal(expectedQuotient, actualQuotient);
                Assert.Equal(expectedRemainder, actualRemainder);
            }
            if (IntPtr.Size == 4)
            {
                DivRemNativeUInt(dividend, divisor, expectedQuotient, expectedRemainder);
            }
        }

        [Theory]
        [InlineData(9223372036854775807L, 2000L, 4611686018427387L, 1807L)]
        [InlineData(-9223372036854775808L, -2000L, 4611686018427387L, -1808L)]
        [InlineData(9223372036854775807L, -2000L, -4611686018427387L, 1807L)]
        [InlineData(-9223372036854775808L, 2000L, -4611686018427387L, -1808L)]
        [InlineData(13952L, 2000L, 6L, 1952L)]
        [InlineData(0L, 2000L, 0L, 0L)]
        [InlineData(-14032L, 2000L, -7L, -32L)]
        [InlineData(13952L, -2000L, -6L, 1952L)]
        [InlineData(long.MaxValue, 0, 0, 0)]
        [InlineData(1, 0, 0, 0)]
        [InlineData(0, 0, 0, 0)]
        public static void DivRemInt64(long dividend, long divisor, long expectedQuotient, long expectedRemainder)
        {
            if (divisor == 0)
            {
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor));
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor, out long remainder));
            }
            else
            {
                Assert.Equal(expectedQuotient, Math.DivRem(dividend, divisor, out long remainder));
                Assert.Equal(expectedRemainder, remainder);

                var (actualQuotient, actualRemainder) = Math.DivRem(dividend, divisor);
                Assert.Equal(expectedQuotient, actualQuotient);
                Assert.Equal(expectedRemainder, actualRemainder);
            }
            if (IntPtr.Size == 8)
            {
                DivRemNativeInt((nint)dividend, (nint)divisor, (nint)expectedQuotient, (nint)expectedRemainder);
            }
        }

        [Theory]
        [InlineData(ulong.MaxValue, ulong.MaxValue, 1, 0)]
        [InlineData(ulong.MaxValue, 1, ulong.MaxValue, 0)]
        [InlineData(ulong.MaxValue, 2, 9223372036854775807, 1)]
        [InlineData(123456789, 4242424242, 0, 123456789)]
        [InlineData(5147483647, 3147483647, 1, 2000000000)]
        [InlineData(1000000, 333, 3003, 1)]
        [InlineData(0, 1, 0, 0)]
        [InlineData(0UL, ulong.MaxValue, 0, 0)]
        [InlineData(13952, 2000, 6, 1952)]
        [InlineData(ulong.MaxValue, 0, 0, 0)]
        [InlineData(1, 0, 0, 0)]
        [InlineData(0, 0, 0, 0)]
        public static void DivRemUInt64(ulong dividend, ulong divisor, ulong expectedQuotient, ulong expectedRemainder)
        {
            if (divisor == 0)
            {
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor));
            }
            else
            {
                var (actualQuotient, actualRemainder) = Math.DivRem(dividend, divisor);
                Assert.Equal(expectedQuotient, actualQuotient);
                Assert.Equal(expectedRemainder, actualRemainder);
            }
            if (IntPtr.Size == 8)
            {
                DivRemNativeUInt((nuint)dividend, (nuint)divisor, (nuint)expectedQuotient, (nuint)expectedRemainder);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DivRemNativeInt(nint dividend, nint divisor, nint expectedQuotient, nint expectedRemainder)
        {
            if (divisor == 0)
            {
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor));
            }
            else
            {
                var (actualQuotient, actualRemainder) = Math.DivRem(dividend, divisor);
                Assert.Equal(expectedQuotient, actualQuotient);
                Assert.Equal(expectedRemainder, actualRemainder);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DivRemNativeUInt(nuint dividend, nuint divisor, nuint expectedQuotient, nuint expectedRemainder)
        {
            if (divisor == 0)
            {
                Assert.Throws<DivideByZeroException>(() => Math.DivRem(dividend, divisor));
            }
            else
            {
                var (actualQuotient, actualRemainder) = Math.DivRem(dividend, divisor);
                Assert.Equal(expectedQuotient, actualQuotient);
                Assert.Equal(expectedRemainder, actualRemainder);
            }
        }

        public static IEnumerable<object[]> Clamp_UnsignedInt_TestData()
        {
            yield return new object[] { 1, 1, 3, 1 };
            yield return new object[] { 2, 1, 3, 2 };
            yield return new object[] { 3, 1, 3, 3 };
            yield return new object[] { 1, 1, 1, 1 };

            yield return new object[] { 0, 1, 3, 1 };
            yield return new object[] { 4, 1, 3, 3 };
        }

        public static IEnumerable<object[]> Clamp_SignedInt_TestData()
        {
            yield return new object[] { -1, -1, 1, -1 };
            yield return new object[] { 0, -1, 1, 0 };
            yield return new object[] { 1, -1, 1, 1 };
            yield return new object[] { 1, -1, 1, 1 };

            yield return new object[] { -2, -1, 1, -1 };
            yield return new object[] { 2, -1, 1, 1 };
        }

        [Theory]
        [InlineData( double.NegativeInfinity, double.NaN,              0.0)]
        [InlineData(-3.1415926535897932,      double.NaN,              0.0)]                               //                               value: -(pi)
        [InlineData(-2.7182818284590452,      double.NaN,              0.0)]                               //                               value: -(e)
        [InlineData(-1.4142135623730950,      double.NaN,              0.0)]                               //                               value: -(sqrt(2))
        [InlineData(-1.0,                     double.NaN,              0.0)]
        [InlineData(-0.69314718055994531,     double.NaN,              0.0)]                               //                               value: -(ln(2))
        [InlineData(-0.43429448190325183,     double.NaN,              0.0)]                               //                               value: -(log10(e))
        [InlineData(-0.0,                     double.NaN,              0.0)]
        [InlineData( double.NaN,              double.NaN,              0.0)]
        [InlineData( 0.0,                     double.NaN,              0.0)]
        [InlineData( 1.0,                     0.0,                     CrossPlatformMachineEpsilon)]
        [InlineData( 1.0510897883672876,      0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 1.0957974645564909,      0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 1.2095794864199787,      0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 1.25,                    0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 1.2605918365213561,      0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 1.3246090892520058,      0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 1.5430806348152438,      1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.7071001431069344,      1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 2.1781835566085709,      1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 2.2341880974508023,      1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 2.5091784786580568,      1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 5.05,                    2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 7.6101251386622884,      2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 11.591953275521521,      3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity, double.PositiveInfinity, 0.0)]
        public static void Acosh(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Acosh(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NegativeInfinity, 0.0)]
        [InlineData(-11.548739357257748,      -3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData(-7.5441371028169758,      -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-4.95,                    -2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-2.3012989023072949,      -1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-1.9978980091062796,      -1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-1.9350668221743567,      -1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-1.3835428792038633,      -1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-1.1752011936438015,      -1,                       CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.86867096148600961,     -0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.76752314512611633,     -0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.75,                    -0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.68050167815224332,     -0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.44807597941469025,     -0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.32371243907207108,     -0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.32371243907207108,      0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.44807597941469025,      0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.68050167815224332,      0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 0.75,                     0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 0.76752314512611633,      0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 0.86867096148600961,      0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 1.1752011936438015,       1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.3835428792038633,       1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 1.9350668221743567,       1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 1.9978980091062796,       1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 2.3012989023072949,       1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 4.95,                     2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 7.5441371028169758,       2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 11.548739357257748,       3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Asinh(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Asinh(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData(-3.1415926535897932,       double.NaN,              0.0)]                               //                              value: -(pi)
        [InlineData(-2.7182818284590452,       double.NaN,              0.0)]                               //                              value: -(e)
        [InlineData(-1.4142135623730950,       double.NaN,              0.0)]                               //                              value: -(sqrt(2))
        [InlineData(-1.0,                      double.NegativeInfinity, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.99627207622074994,     -3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData(-0.99132891580059984,     -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-0.98019801980198020,     -2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-0.91715233566727435,     -1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.89423894585503855,     -1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.88838556158566054,     -1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.81046380599898809,     -1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.76159415595576489,     -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.65579420263267244,     -0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.60885936501391381,     -0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.6,                     -0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.56259360033158334,     -0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.40890401183401433,     -0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.30797791269089433,     -0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.30797791269089433,      0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.40890401183401433,      0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.56259360033158334,      0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 0.6,                      0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 0.60885936501391381,      0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 0.65579420263267244,      0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 0.76159415595576489,      1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.81046380599898809,      1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 0.88838556158566054,      1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 0.89423894585503855,      1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 0.91715233566727435,      1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 0.98019801980198020,      2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 0.99132891580059984,      2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 0.99627207622074994,      3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( 1.0,                      double.PositiveInfinity, 0.0)]
        [InlineData( 1.4142135623730950,       double.NaN,              0.0)]                               //                              value:  (sqrt(2))
        [InlineData( 2.7182818284590452,       double.NaN,              0.0)]                               //                              value:  (e)
        [InlineData( 3.1415926535897932,       double.NaN,              0.0)]                               //                              value:  (pi)
        [InlineData( double.PositiveInfinity,  double.NaN,              0.0)]
        public static void Atanh(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Atanh(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NegativeInfinity)]
        [InlineData(-3.1415926535897932,      -3.1415926535897936)]     // value: -(pi)
        [InlineData(-2.7182818284590452,      -2.7182818284590455)]     // value: -(e)
        [InlineData(-2.3025850929940457,      -2.3025850929940463)]     // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -1.5707963267948968)]     // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -1.4426950408889636)]     // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -1.4142135623730951)]     // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -1.1283791670955128)]     // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -1.0000000000000002)]
        [InlineData(-0.78539816339744831,     -0.78539816339744839)]    // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.70710678118654768)]    // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.69314718055994540)]    // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.63661977236758149)]    // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.43429448190325187)]    // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.31830988618379075)]    // value: -(1 / pi)
        [InlineData(-0.0,                     -double.Epsilon)]
        [InlineData( double.NaN,               double.NaN)]
        [InlineData( 0.0,                     -double.Epsilon)]
        [InlineData( 0.31830988618379067,      0.31830988618379064)]    // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.43429448190325176)]    // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.63661977236758127)]    // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.69314718055994518)]    // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.70710678118654746)]    // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.78539816339744817)]    // value:  (pi / 4)
        [InlineData( 1.0,                      0.99999999999999989)]
        [InlineData( 1.1283791670955126,       1.1283791670955123)]     // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       1.4142135623730947)]     // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       1.4426950408889632)]     // value:  (log2(e))
        [InlineData( 1.5707963267948966,       1.5707963267948963)]     // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       2.3025850929940455)]     // value:  (ln(10))
        [InlineData( 2.7182818284590452,       2.7182818284590446)]     // value:  (e)
        [InlineData( 3.1415926535897932,       3.1415926535897927)]     // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.MaxValue)]
        public static void BitDecrement(double value, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, Math.BitDecrement(value), 0.0);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.MinValue)]
        [InlineData(-3.1415926535897932,      -3.1415926535897927)]     // value: -(pi)
        [InlineData(-2.7182818284590452,      -2.7182818284590446)]     // value: -(e)
        [InlineData(-2.3025850929940457,      -2.3025850929940455)]     // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -1.5707963267948963)]     // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -1.4426950408889632)]     // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -1.4142135623730947)]     // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -1.1283791670955123)]     // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -0.99999999999999989)]
        [InlineData(-0.78539816339744831,     -0.78539816339744817)]    // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.70710678118654746)]    // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.69314718055994518)]    // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.63661977236758127)]    // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.43429448190325176)]    // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.31830988618379064)]    // value: -(1 / pi)
        [InlineData(-0.0,                      double.Epsilon)]
        [InlineData( double.NaN,               double.NaN)]
        [InlineData( 0.0,                      double.Epsilon)]
        [InlineData( 0.31830988618379067,      0.31830988618379075)]    // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.43429448190325187)]    // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.63661977236758149)]    // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.69314718055994540)]    // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.70710678118654768)]    // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.78539816339744839)]    // value:  (pi / 4)
        [InlineData( 1.0,                      1.0000000000000002 )]
        [InlineData( 1.1283791670955126,       1.1283791670955128 )]     // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       1.4142135623730951 )]     // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       1.4426950408889636 )]     // value:  (log2(e))
        [InlineData( 1.5707963267948966,       1.5707963267948968 )]     // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       2.3025850929940463 )]     // value:  (ln(10))
        [InlineData( 2.7182818284590452,       2.7182818284590455 )]     // value:  (e)
        [InlineData( 3.1415926535897932,       3.1415926535897936 )]     // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity)]
        public static void BitIncrement(double value, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, Math.BitIncrement(value), 0.0);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NegativeInfinity, 0.0)]
        [InlineData(-3.1415926535897932,      -1.4645918875615233,      CrossPlatformMachineEpsilon * 10)]   // value: -(pi)
        [InlineData(-2.7182818284590452,      -1.3956124250860895,      CrossPlatformMachineEpsilon * 10)]   // value: -(e)
        [InlineData(-2.3025850929940457,      -1.3205004784536852,      CrossPlatformMachineEpsilon * 10)]   // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -1.1624473515096265,      CrossPlatformMachineEpsilon * 10)]   // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -1.1299472763373901,      CrossPlatformMachineEpsilon * 10)]   // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -1.1224620483093730,      CrossPlatformMachineEpsilon * 10)]   // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -1.0410821966965807,      CrossPlatformMachineEpsilon * 10)]   // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.78539816339744831,     -0.92263507432201421,     CrossPlatformMachineEpsilon)]        // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.89089871814033930,     CrossPlatformMachineEpsilon)]        // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.88499704450051772,     CrossPlatformMachineEpsilon)]        // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.86025401382809963,     CrossPlatformMachineEpsilon)]        // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.75728863133090766,     CrossPlatformMachineEpsilon)]        // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.68278406325529568,     CrossPlatformMachineEpsilon)]        // value: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.31830988618379067,      0.68278406325529568,     CrossPlatformMachineEpsilon)]        // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.75728863133090766,     CrossPlatformMachineEpsilon)]        // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.86025401382809963,     CrossPlatformMachineEpsilon)]        // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.88499704450051772,     CrossPlatformMachineEpsilon)]        // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.89089871814033930,     CrossPlatformMachineEpsilon)]        // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.92263507432201421,     CrossPlatformMachineEpsilon)]        // value:  (pi / 4)
        [InlineData( 1.0,                      1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.1283791670955126,       1.0410821966965807,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       1.1224620483093730,      CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       1.1299472763373901,      CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,       1.1624473515096265,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       1.3205004784536852,      CrossPlatformMachineEpsilon * 10)]   // value:  (ln(10))
        [InlineData( 2.7182818284590452,       1.3956124250860895,      CrossPlatformMachineEpsilon * 10)]   // value:  (e)
        [InlineData( 3.1415926535897932,       1.4645918875615233,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Cbrt(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Cbrt(value), allowedVariance);
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_SByte(sbyte value, sbyte min, sbyte max, sbyte expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_Byte(byte value, byte min, byte max, byte expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Short(short value, short min, short max, short expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_UShort(ushort value, ushort min, ushort max, ushort expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Int(int value, int min, int max, int expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_UInt(uint value, uint min, uint max, uint expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Long(long value, long min, long max, long expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_ULong(ulong value, ulong min, ulong max, ulong expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }


        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_NInt(int value, int min, int max, int expected)
        {
            Assert.Equal((nint)expected, Math.Clamp((nint)value, (nint)min, (nint)max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_NUInt(uint value, uint min, uint max, uint expected)
        {
            Assert.Equal((nuint)expected, Math.Clamp((nuint)value, (nuint)min, (nuint)max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity)]
        [InlineData(1, double.NegativeInfinity, double.PositiveInfinity, 1)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(1, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(1, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, double.NaN, 1, double.NaN)]
        [InlineData(double.NaN, 1, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1, 1, double.NaN)]
        [InlineData(1, double.NaN, double.NaN, 1)]
        [InlineData(1, double.NaN, 1, 1)]
        [InlineData(1, 1, double.NaN, 1)]
        public static void Clamp_Double(double value, double min, double max, double expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity)]
        [InlineData(1, float.NegativeInfinity, float.PositiveInfinity, 1)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(1, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(1, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.NaN, float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, float.NaN, 1, float.NaN)]
        [InlineData(float.NaN, 1, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1, 1, float.NaN)]
        [InlineData(1, float.NaN, float.NaN, 1)]
        [InlineData(1, float.NaN, 1, 1)]
        [InlineData(1, 1, float.NaN, 1)]
        public static void Clamp_Float(float value, float min, float max, float expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Decimal(decimal value, decimal min, decimal max, decimal expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Fact]
        public static void Clamp_MinGreaterThanMax_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((sbyte)1, (sbyte)2, (sbyte)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((byte)1, (byte)2, (byte)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((short)1, (short)2, (short)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((ushort)1, (ushort)2, (ushort)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((int)1, (int)2, (int)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((uint)1, (uint)2, (uint)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((long)1, (long)2, (long)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((ulong)1, (ulong)2, (ulong)1));

            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((float)1, (float)2, (float)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((double)1, (double)2, (double)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((decimal)1, (decimal)2, (decimal)1));
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NegativeInfinity,  double.NegativeInfinity)]
        [InlineData( double.NegativeInfinity, -3.1415926535897932,       double.NegativeInfinity)]
        [InlineData( double.NegativeInfinity, -0.0,                      double.NegativeInfinity)]
        [InlineData( double.NegativeInfinity,  double.NaN,               double.NegativeInfinity)]
        [InlineData( double.NegativeInfinity,  0.0,                      double.PositiveInfinity)]
        [InlineData( double.NegativeInfinity,  3.1415926535897932,       double.PositiveInfinity)]
        [InlineData( double.NegativeInfinity,  double.PositiveInfinity,  double.PositiveInfinity)]
        [InlineData(-3.1415926535897932,       double.NegativeInfinity, -3.1415926535897932)]
        [InlineData(-3.1415926535897932,      -3.1415926535897932,      -3.1415926535897932)]
        [InlineData(-3.1415926535897932,      -0.0,                     -3.1415926535897932)]
        [InlineData(-3.1415926535897932,       double.NaN,              -3.1415926535897932)]
        [InlineData(-3.1415926535897932,       0.0,                      3.1415926535897932)]
        [InlineData(-3.1415926535897932,       3.1415926535897932,       3.1415926535897932)]
        [InlineData(-3.1415926535897932,       double.PositiveInfinity,  3.1415926535897932)]
        [InlineData(-0.0,                      double.NegativeInfinity, -0.0)]
        [InlineData(-0.0,                     -3.1415926535897932,      -0.0)]
        [InlineData(-0.0,                     -0.0,                     -0.0)]
        [InlineData(-0.0,                      double.NaN,              -0.0)]
        [InlineData(-0.0,                      0.0,                      0.0)]
        [InlineData(-0.0,                      3.1415926535897932,       0.0)]
        [InlineData(-0.0,                      double.PositiveInfinity,  0.0)]
        [InlineData( double.NaN,               double.NegativeInfinity,  double.NaN)]
        [InlineData( double.NaN,              -3.1415926535897932,       double.NaN)]
        [InlineData( double.NaN,              -0.0,                      double.NaN)]
        [InlineData( double.NaN,               double.NaN,               double.NaN)]
        [InlineData( double.NaN,               0.0,                      double.NaN)]
        [InlineData( double.NaN,               3.1415926535897932,       double.NaN)]
        [InlineData( double.NaN,               double.PositiveInfinity,  double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity, -0.0)]
        [InlineData( 0.0,                     -3.1415926535897932,      -0.0)]
        [InlineData( 0.0,                     -0.0,                     -0.0)]
        [InlineData( 0.0,                      double.NaN,              -0.0)]
        [InlineData( 0.0,                      0.0,                      0.0)]
        [InlineData( 0.0,                      3.1415926535897932,       0.0)]
        [InlineData( 0.0,                      double.PositiveInfinity,  0.0)]
        [InlineData( 3.1415926535897932,       double.NegativeInfinity, -3.1415926535897932)]
        [InlineData( 3.1415926535897932,      -3.1415926535897932,      -3.1415926535897932)]
        [InlineData( 3.1415926535897932,      -0.0,                     -3.1415926535897932)]
        [InlineData( 3.1415926535897932,       double.NaN,              -3.1415926535897932)]
        [InlineData( 3.1415926535897932,       0.0,                      3.1415926535897932)]
        [InlineData( 3.1415926535897932,       3.1415926535897932,       3.1415926535897932)]
        [InlineData( 3.1415926535897932,       double.PositiveInfinity,  3.1415926535897932)]
        [InlineData( double.PositiveInfinity,  double.NegativeInfinity,  double.NegativeInfinity)]
        [InlineData( double.PositiveInfinity, -3.1415926535897932,       double.NegativeInfinity)]
        [InlineData( double.PositiveInfinity, -0.0,                      double.NegativeInfinity)]
        [InlineData( double.PositiveInfinity,  double.NaN,               double.NegativeInfinity)]
        [InlineData( double.PositiveInfinity,  0.0,                      double.PositiveInfinity)]
        [InlineData( double.PositiveInfinity,  3.1415926535897932,       double.PositiveInfinity)]
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity,  double.PositiveInfinity)]
        public static void CopySign(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, Math.CopySign(x, y), 0.0);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NegativeInfinity,  double.NegativeInfinity,  double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                      double.NegativeInfinity,  double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                     -3.1415926535897932,       double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                     -0.0,                      double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                      double.NaN,               double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                      0.0,                      double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                      3.1415926535897932,       double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                      double.PositiveInfinity,  double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                      double.NegativeInfinity,  double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                     -3.1415926535897932,       double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                     -0.0,                      double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                      double.NaN,               double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                      0.0,                      double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                      3.1415926535897932,       double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                      double.PositiveInfinity,  double.NaN)]
        [InlineData( double.NegativeInfinity,  double.PositiveInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData(-1e308,                    2.0,                      1e308,                   -1e308)]
        [InlineData(-1e308,                    2.0,                      double.PositiveInfinity,  double.PositiveInfinity)]
        [InlineData(-5,                        4,                       -3,                       -23)]
        [InlineData(-0.0,                      double.NegativeInfinity,  double.NegativeInfinity,  double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity, -3.1415926535897932,       double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity, -0.0,                      double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity,  double.NaN,               double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity,  0.0,                      double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity,  3.1415926535897932,       double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity,  double.NegativeInfinity,  double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity, -3.1415926535897932,       double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity, -0.0,                      double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity,  double.NaN,               double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity,  0.0,                      double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity,  3.1415926535897932,       double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity,  double.NegativeInfinity,  double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity, -3.1415926535897932,       double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity, -0.0,                      double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity,  double.NaN,               double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity,  0.0,                      double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity,  3.1415926535897932,       double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity,  double.NegativeInfinity,  double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity, -3.1415926535897932,       double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity, -0.0,                      double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity,  double.NaN,               double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity,  0.0,                      double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity,  3.1415926535897932,       double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData( 5,                        4,                        3,                        23)]
        [InlineData( 1e308,                    2.0,                     -1e308,                    1e308)]
        [InlineData( 1e308,                    2.0,                      double.NegativeInfinity,  double.NegativeInfinity)]
        [InlineData( double.PositiveInfinity,  double.NegativeInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                      double.NegativeInfinity,  double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                     -3.1415926535897932,       double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                     -0.0,                      double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                      double.NaN,               double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                      0.0,                      double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                      3.1415926535897932,       double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                      double.PositiveInfinity,  double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                      double.NegativeInfinity,  double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                     -3.1415926535897932,       double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                     -0.0,                      double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                      double.NaN,               double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                      0.0,                      double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                      3.1415926535897932,       double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                      double.PositiveInfinity,  double.NaN)]
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity,  double.NegativeInfinity,  double.NaN)]
        public static void FusedMultiplyAdd(double x, double y, double z, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, Math.FusedMultiplyAdd(x, y, z), 0.0);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  unchecked((int)(0x7FFFFFFF)))]
        [InlineData(-0.0,                      unchecked((int)(0x80000000)))]
        [InlineData( double.NaN,               unchecked((int)(0x7FFFFFFF)))]
        [InlineData( 0.0,                      unchecked((int)(0x80000000)))]
        [InlineData( 0.11331473229676087,     -4)]
        [InlineData( 0.15195522325791297,     -3)]
        [InlineData( 0.20269956628651730,     -3)]
        [InlineData( 0.33662253682241906,     -2)]
        [InlineData( 0.36787944117144232,     -2)]
        [InlineData( 0.37521422724648177,     -2)]
        [InlineData( 0.45742934732229695,     -2)]
        [InlineData( 0.5,                     -1)]
        [InlineData( 0.58019181037172444,     -1)]
        [InlineData( 0.61254732653606592,     -1)]
        [InlineData( 0.61850313780157598,     -1)]
        [InlineData( 0.64321824193300488,     -1)]
        [InlineData( 0.74005557395545179,     -1)]
        [InlineData( 0.80200887896145195,     -1)]
        [InlineData( 1,                        0)]
        [InlineData( 1.2468689889006383,       0)]
        [InlineData( 1.3512498725672678,       0)]
        [InlineData( 1.5546822754821001,       0)]
        [InlineData( 1.6168066722416747,       0)]
        [InlineData( 1.6325269194381528,       0)]
        [InlineData( 1.7235679341273495,       0)]
        [InlineData( 2,                        1)]
        [InlineData( 2.1861299583286618,       1)]
        [InlineData( 2.6651441426902252,       1)]
        [InlineData( 2.7182818284590452,       1)]
        [InlineData( 2.9706864235520193,       1)]
        [InlineData( 4.9334096679145963,       2)]
        [InlineData( 6.5808859910179210,       2)]
        [InlineData( 8.8249778270762876,       3)]
        [InlineData( double.PositiveInfinity,  unchecked((int)(0x7FFFFFFF)))]
        [InlineData( -8.066848,                3)]
        [InlineData( 4.345240,                 2)]
        [InlineData( -8.381433,                3)]
        [InlineData( -6.531674,                2)]
        [InlineData( 9.267057,                 3)]
        [InlineData( 0.661986,                -1)]
        [InlineData( -0.406604,               -2)]
        [InlineData( 0.561760,                -1)]
        [InlineData( 0.774152,                -1)]
        [InlineData( -0.678764,               -1)]
        public static void ILogB(double value, int expectedResult)
        {
            Assert.Equal(expectedResult, Math.ILogB(value));
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData(-0.11331473229676087,      double.NaN,              0.0)]
        [InlineData(-0.0,                      double.NegativeInfinity, 0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      double.NegativeInfinity, 0.0)]
        [InlineData( 0.11331473229676087,     -3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData( 0.15195522325791297,     -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData( 0.20269956628651730,     -2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData( 0.33662253682241906,     -1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData( 0.36787944117144232,     -1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData( 0.37521422724648177,     -1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData( 0.45742934732229695,     -1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData( 0.5,                     -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.58019181037172444,     -0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData( 0.61254732653606592,     -0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData( 0.61850313780157598,     -0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData( 0.64321824193300488,     -0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData( 0.74005557395545179,     -0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData( 0.80200887896145195,     -0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData( 1,                        0.0,                     0.0)]
        [InlineData( 1.2468689889006383,       0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 1.3512498725672678,       0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 1.5546822754821001,       0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 1.6168066722416747,       0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 1.6325269194381528,       0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 1.7235679341273495,       0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 2,                        1.0,                     CrossPlatformMachineEpsilon * 10)]  //                              value: (e)
        [InlineData( 2.1861299583286618,       1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 2.6651441426902252,       1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 2.7182818284590452,       1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 2.9706864235520193,       1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 4.9334096679145963,       2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 6.5808859910179210,       2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 8.8249778270762876,       3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Log2(double value, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.Log2(value), allowedVariance);
        }

        [Theory]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.PositiveInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MaxValue)]
        [InlineData(double.MaxValue, double.MinValue, double.MaxValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1.0, double.NaN)]
        [InlineData(1.0, double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.NaN, double.NaN)]
        [InlineData(double.NegativeInfinity, double.NaN, double.NaN)]
        [InlineData(double.NaN, double.PositiveInfinity, double.NaN)]
        [InlineData(double.NaN, double.NegativeInfinity, double.NaN)]
        [InlineData(-0.0, 0.0, 0.0)]
        [InlineData(0.0, -0.0, 0.0)]
        [InlineData(2.0, -3.0, -3.0)]
        [InlineData(-3.0, 2.0, -3.0)]
        [InlineData(3.0, -2.0, 3.0)]
        [InlineData(-2.0, 3.0, 3.0)]
        public static void MaxMagnitude(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, Math.MaxMagnitude(x, y), 0.0);

            if (double.IsNaN(x))
            {
                // Toggle the sign of the NaN to validate both +NaN and -NaN behave the same.
                // Negate should work as well but the JIT may constant fold or do other tricks
                // and normalize to a single NaN form so we do bitwise tricks to ensure we test
                // the right thing.

                ulong bits = BitConverter.DoubleToUInt64Bits(x);
                bits ^= BitConverter.DoubleToUInt64Bits(-0.0);
                x = BitConverter.UInt64BitsToDouble(bits);

                AssertExtensions.Equal(expectedResult, Math.MaxMagnitude(x, y), 0.0);
            }

            if (double.IsNaN(y))
            {
                ulong bits = BitConverter.DoubleToUInt64Bits(y);
                bits ^= BitConverter.DoubleToUInt64Bits(-0.0);
                y = BitConverter.UInt64BitsToDouble(bits);

                AssertExtensions.Equal(expectedResult, Math.MaxMagnitude(x, y), 0.0);
            }
        }

        [Theory]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MinValue)]
        [InlineData(double.MaxValue, double.MinValue, double.MinValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1.0, double.NaN)]
        [InlineData(1.0, double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.NaN, double.NaN)]
        [InlineData(double.NegativeInfinity, double.NaN, double.NaN)]
        [InlineData(double.NaN, double.PositiveInfinity, double.NaN)]
        [InlineData(double.NaN, double.NegativeInfinity, double.NaN)]
        [InlineData(-0.0, 0.0, -0.0)]
        [InlineData(0.0, -0.0, -0.0)]
        [InlineData(2.0, -3.0, 2.0)]
        [InlineData(-3.0, 2.0, 2.0)]
        [InlineData(3.0, -2.0, -2.0)]
        [InlineData(-2.0, 3.0, -2.0)]
        public static void MinMagnitude(double x, double y, double expectedResult)
        {
            AssertExtensions.Equal(expectedResult, Math.MinMagnitude(x, y), 0.0);

            if (double.IsNaN(x))
            {
                // Toggle the sign of the NaN to validate both +NaN and -NaN behave the same.
                // Negate should work as well but the JIT may constant fold or do other tricks
                // and normalize to a single NaN form so we do bitwise tricks to ensure we test
                // the right thing.

                ulong bits = BitConverter.DoubleToUInt64Bits(x);
                bits ^= BitConverter.DoubleToUInt64Bits(-0.0);
                x = BitConverter.UInt64BitsToDouble(bits);

                AssertExtensions.Equal(expectedResult, Math.MinMagnitude(x, y), 0.0);
            }

            if (double.IsNaN(y))
            {
                ulong bits = BitConverter.DoubleToUInt64Bits(y);
                bits ^= BitConverter.DoubleToUInt64Bits(-0.0);
                y = BitConverter.UInt64BitsToDouble(bits);

                AssertExtensions.Equal(expectedResult, Math.MinMagnitude(x, y), 0.0);
            }
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  unchecked((int)(0x7FFFFFFF)),  double.NegativeInfinity,  0)]
        [InlineData(-0.11331473229676087,     -3,                            -0.014164341537095108,     CrossPlatformMachineEpsilon / 10)]
        [InlineData(-0.0,                      unchecked((int)(0x80000000)), -0.0,                      0)]
        [InlineData( double.NaN,               unchecked((int)(0x7FFFFFFF)),  double.NaN,               0)]
        [InlineData( 0.0,                      unchecked((int)(0x80000000)),  0,                        0)]
        [InlineData( double.NaN,              0,                              double.NaN,               0)]
        [InlineData( double.PositiveInfinity, 0,                              double.PositiveInfinity,  0)]
        [InlineData( double.NegativeInfinity, 0,                              double.NegativeInfinity,  0)]
        [InlineData( 1,                       2147483647,                     double.PositiveInfinity,  0)]
        [InlineData( double.NaN,              1,                              double.NaN,               0)]
        [InlineData( double.PositiveInfinity, -1,                             double.PositiveInfinity,  0)]
        [InlineData( double.PositiveInfinity, unchecked((int)(0x7FFFFFFF)),   double.PositiveInfinity,  0)]
        [InlineData( 0.11331473229676087,     -4,                             0.0070821707685475542,    CrossPlatformMachineEpsilon / 100)]
        [InlineData( 0.15195522325791297,     -3,                             0.018994402907239121,     CrossPlatformMachineEpsilon / 10)]
        [InlineData( 0.20269956628651730,     -3,                             0.025337445785814663,     CrossPlatformMachineEpsilon / 10)]
        [InlineData( 0.33662253682241906,     -2,                             0.084155634205604762,     CrossPlatformMachineEpsilon / 10)]
        [InlineData( 0.36787944117144232,     -2,                             0.091969860292860584,     CrossPlatformMachineEpsilon / 10)]
        [InlineData( 0.37521422724648177,     -2,                             0.093803556811620448,     CrossPlatformMachineEpsilon / 10)]
        [InlineData( 0.45742934732229695,     -2,                             0.11435733683057424,      CrossPlatformMachineEpsilon)]
        [InlineData( 0.5,                     -1,                             0.25,                     CrossPlatformMachineEpsilon)]
        [InlineData( 0.58019181037172444,     -1,                             0.2900959051858622,       CrossPlatformMachineEpsilon)]
        [InlineData( 0.61254732653606592,     -1,                             0.30627366326803296,      CrossPlatformMachineEpsilon)]
        [InlineData( 0.61850313780157598,     -1,                             0.30925156890078798,      CrossPlatformMachineEpsilon)]
        [InlineData( 0.64321824193300488,     -1,                             0.32160912096650246,      CrossPlatformMachineEpsilon)]
        [InlineData( 0.74005557395545179,     -1,                             0.37002778697772587,      CrossPlatformMachineEpsilon)]
        [InlineData( 0.80200887896145195,     -1,                             0.40100443948072595,      CrossPlatformMachineEpsilon)]
        [InlineData( 0,                       2147483647,                     0,                        CrossPlatformMachineEpsilon)]
        [InlineData( 0,                       -2147483648,                    0,                        CrossPlatformMachineEpsilon)]
        [InlineData( 1,                       -1,                             0.5,                      CrossPlatformMachineEpsilon)]
        [InlineData( 8.98846567431158E+307,   -2097,                          5E-324,                   CrossPlatformMachineEpsilon)]
        [InlineData( 5E-324,                  2097,                           8.98846567431158E+307,    CrossPlatformMachineEpsilon)]
        [InlineData( 1.000244140625,          -1074,                          5E-324,                   CrossPlatformMachineEpsilon)]
        [InlineData( 0.7499999999999999,      -1073,                          5E-324,                   CrossPlatformMachineEpsilon)]
        [InlineData( 0.5000000000000012,      -1024,                          2.781342323134007E-309,   CrossPlatformMachineEpsilon)]
        [InlineData( 0.6619858980995045,      3,                              5.295887184796036,        CrossPlatformMachineEpsilon * 10)]
        [InlineData( -8.06684839057968,       -2,                             -2.01671209764492,        CrossPlatformMachineEpsilon * 10)]
        [InlineData( -8.06684839057968,       -2,                             -2.01671209764492,        CrossPlatformMachineEpsilon * 10)]
        [InlineData( 4.345239849338305,       -1,                             2.1726199246691524,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( -8.38143342755525,       0,                              -8.38143342755525,        CrossPlatformMachineEpsilon * 10)]
        [InlineData( -0.4066039223853553,     4,                              -6.505662758165685,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 4.345239849338305,       -1,                             2.1726199246691524,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( -8.38143342755525,       0,                              -8.38143342755525,        CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.6619858980995045,      3,                              5.295887184796036,        CrossPlatformMachineEpsilon * 10)]
        [InlineData( -0.4066039223853553,     4,                              -6.505662758165685,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1,                       0,                              1,                        CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1,                       1,                              2,                        CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1,                        0,                             1,                        CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.2468689889006383,       0,                             1.2468689889006384,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.3512498725672678,       0,                             1.3512498725672677,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.5546822754821001,       0,                             1.5546822754821001,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.6168066722416747,       0,                             1.6168066722416747,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.6325269194381528,       0,                             1.6325269194381529,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.7235679341273495,       0,                             1.7235679341273495,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2,                        1,                             4,                        CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.1861299583286618,       1,                             4.3722599166573239,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.6651441426902252,       1,                             5.3302882853804503,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.7182818284590452,       1,                             5.4365636569180902,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.9706864235520193,       1,                             5.9413728471040388,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 4.9334096679145963,       2,                             19.733638671658387,       CrossPlatformMachineEpsilon * 100)]
        [InlineData( 6.5808859910179210,       2,                             26.323543964071686,       CrossPlatformMachineEpsilon * 100)]
        [InlineData( 8.8249778270762876,       3,                             70.599822616610297,       CrossPlatformMachineEpsilon * 100)]
        [InlineData( -6.531673581913484,       1,                             -13.063347163826968,      CrossPlatformMachineEpsilon * 100)]
        [InlineData( 9.267056966972586,        2,                             37.06822786789034,        CrossPlatformMachineEpsilon * 100)]
        [InlineData( 0.5617597462207241,       5,                             17.97631187906317,        CrossPlatformMachineEpsilon * 100)]
        [InlineData( 0.7741522965913037,       6,                             49.545746981843436,       CrossPlatformMachineEpsilon * 100)]
        [InlineData( -0.6787637026394024,      7,                             -86.88175393784351,       CrossPlatformMachineEpsilon * 100)]
        [InlineData( -6.531673581913484,       1,                             -13.063347163826968,      CrossPlatformMachineEpsilon * 100)]
        [InlineData( 9.267056966972586,        2,                             37.06822786789034,        CrossPlatformMachineEpsilon * 100)]
        [InlineData( 0.5617597462207241,       5,                             17.97631187906317,        CrossPlatformMachineEpsilon * 100)]
        [InlineData( 0.7741522965913037,       6,                             49.545746981843436,       CrossPlatformMachineEpsilon * 100)]
        [InlineData( -0.6787637026394024,      7,                             -86.88175393784351,       CrossPlatformMachineEpsilon * 100)]
        public static void ScaleB(double x, int n, double expectedResult, double allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, Math.ScaleB(x, n), allowedVariance);
        }


        public static IEnumerable<object[]> Round_Digits_TestData
        {
            get
            {
                yield return new object[] {0, 0, 3, MidpointRounding.ToEven};
                yield return new object[] {3.42156, 3.422, 3, MidpointRounding.ToEven};
                yield return new object[] {-3.42156, -3.422, 3, MidpointRounding.ToEven};

                yield return new object[] {0, 0, 3, MidpointRounding.AwayFromZero};
                yield return new object[] {3.42156, 3.422, 3, MidpointRounding.AwayFromZero};
                yield return new object[] {-3.42156, -3.422, 3, MidpointRounding.AwayFromZero};

                yield return new object[] {0, 0, 3, MidpointRounding.ToZero};
                yield return new object[] {3.42156, 3.421, 3, MidpointRounding.ToZero};
                yield return new object[] {-3.42156, -3.421, 3, MidpointRounding.ToZero};

                yield return new object[] {0, 0, 3, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {3.42156, 3.421, 3, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {-3.42156, -3.422, 3, MidpointRounding.ToNegativeInfinity};

                yield return new object[] {0, 0, 3, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {3.42156, 3.422, 3, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {-3.42156, -3.421, 3, MidpointRounding.ToPositiveInfinity};
              }
        }

        [Theory]
        [InlineData(MidpointRounding.ToEven)]
        [InlineData(MidpointRounding.AwayFromZero)]
        [InlineData(MidpointRounding.ToZero)]
        [InlineData(MidpointRounding.ToNegativeInfinity)]
        [InlineData(MidpointRounding.ToPositiveInfinity)]
        public static void Round_Double_Digits_ByMidpointRounding(MidpointRounding mode)
        {
            Assert.Equal(double.NaN, Math.Round(double.NaN, 3, mode));
            Assert.Equal(double.PositiveInfinity, Math.Round(double.PositiveInfinity, 3, mode));
            Assert.Equal(double.NegativeInfinity, Math.Round(double.NegativeInfinity, 3, mode));
        }

        [Theory]
        [MemberData(nameof(Round_Digits_TestData))]
        public static void Round_Double_Digits(double x, double expected, int digits, MidpointRounding mode)
        {
            Assert.Equal(expected, Math.Round(x, digits, mode));
        }

        [Theory]
        [MemberData(nameof(Round_Digits_TestData))]
        public static void Round_Decimal_Digits(decimal x, decimal expected, int digits, MidpointRounding mode)
        {
            Assert.Equal(expected, Math.Round(x, digits, mode));
        }

        [Theory]
        [InlineData(MidpointRounding.ToEven)]
        [InlineData(MidpointRounding.AwayFromZero)]
        [InlineData(MidpointRounding.ToZero)]
        [InlineData(MidpointRounding.ToNegativeInfinity)]
        [InlineData(MidpointRounding.ToPositiveInfinity)]
        public static void Round_Decimal_Digits_ByMidpointRounding(MidpointRounding mode)
        {
            Assert.Equal(decimal.Zero, Math.Round(decimal.Zero, 3, mode));
        }

        public static IEnumerable<object[]> Round_Modes_TestData
        {
            get
            {
                yield return new object[] {11, 11, MidpointRounding.ToEven};
                yield return new object[] {11.4, 11, MidpointRounding.ToEven};
                yield return new object[] {11.5, 12, MidpointRounding.ToEven};
                yield return new object[] {11.6, 12, MidpointRounding.ToEven};
                yield return new object[] {-11, -11, MidpointRounding.ToEven};
                yield return new object[] {-11.4, -11, MidpointRounding.ToEven};
                yield return new object[] {-11.5, -12, MidpointRounding.ToEven};
                yield return new object[] {-11.6, -12, MidpointRounding.ToEven};
                yield return new object[] {11, 11, MidpointRounding.AwayFromZero};
                yield return new object[] {11.4, 11, MidpointRounding.AwayFromZero};
                yield return new object[] {11.5, 12, MidpointRounding.AwayFromZero};
                yield return new object[] {11.6, 12, MidpointRounding.AwayFromZero};
                yield return new object[] {-11, -11, MidpointRounding.AwayFromZero};
                yield return new object[] {-11.4, -11, MidpointRounding.AwayFromZero};
                yield return new object[] {-11.5, -12, MidpointRounding.AwayFromZero};
                yield return new object[] {-11.6, -12, MidpointRounding.AwayFromZero};
                yield return new object[] {11, 11, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {11.4, 12, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {11.5, 12, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {11.6, 12, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {-11, -11, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {-11.4, -11, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {-11.5, -11, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {-11.6, -11, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {11.0, 11, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {11.4, 11, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {11.5, 11, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {11.6, 11, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {-11.0, -11, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {-11.4, -12, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {-11.5, -12, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {-11.6, -12, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {11.0, 11, MidpointRounding.ToZero};
                yield return new object[] {11.4, 11, MidpointRounding.ToZero};
                yield return new object[] {11.5, 11, MidpointRounding.ToZero};
                yield return new object[] {11.6, 11, MidpointRounding.ToZero};
                yield return new object[] {-11.0, -11, MidpointRounding.ToZero};
                yield return new object[] {-11.4, -11, MidpointRounding.ToZero};
                yield return new object[] {-11.5, -11, MidpointRounding.ToZero};
                yield return new object[] {-11.6, -11, MidpointRounding.ToZero};
            }
        }

        [Theory]
        [MemberData(nameof(Round_Modes_TestData))]
        public static void Round_Double_Modes(double x, double expected, MidpointRounding mode)
        {
            Assert.Equal(expected, Math.Round(x, 0, mode));
        }

        [Theory]
        [MemberData(nameof(Round_Modes_TestData))]
        public static void Round_Float_Modes(float x, float expected, MidpointRounding mode)
        {
            Assert.Equal(expected, MathF.Round(x, 0, mode));
        }

        [Theory]
        [MemberData(nameof(Round_Modes_TestData))]
        public static void Round_Decimal_Modes(decimal x, decimal expected, MidpointRounding mode)
        {
            Assert.Equal(expected, Math.Round(x, 0, mode));
            Assert.Equal(expected, decimal.Round(x, 0, mode));
        }

        [Fact]
        public static void Round_Double_Constant_Arg()
        {
            Assert.Equal( 0, Math.Round( 0.5));
            Assert.Equal( 0, Math.Round(-0.5));
            Assert.Equal( 1, Math.Round( 1.0));
            Assert.Equal(-1, Math.Round(-1.0));
            Assert.Equal( 2, Math.Round( 1.5));
            Assert.Equal(-2, Math.Round(-1.5));
            Assert.Equal( 2, Math.Round( 2.0));
            Assert.Equal(-2, Math.Round(-2.0));
            Assert.Equal( 2, Math.Round( 2.5));
            Assert.Equal(-2, Math.Round(-2.5));
            Assert.Equal( 3, Math.Round( 3.0));
            Assert.Equal(-3, Math.Round(-3.0));
            Assert.Equal( 4, Math.Round( 3.5));
            Assert.Equal(-4, Math.Round(-3.5));

            Assert.Equal( 0, Math.Round( 0.5, MidpointRounding.ToZero));
            Assert.Equal( 0, Math.Round( 0.5, MidpointRounding.ToZero));
            Assert.Equal( 1, Math.Round( 1.0, MidpointRounding.ToZero));
            Assert.Equal(-1, Math.Round(-1.0, MidpointRounding.ToZero));
            Assert.Equal( 1, Math.Round( 1.5, MidpointRounding.ToZero));
            Assert.Equal(-1, Math.Round(-1.5, MidpointRounding.ToZero));
            Assert.Equal( 2, Math.Round( 2.0, MidpointRounding.ToZero));
            Assert.Equal(-2, Math.Round(-2.0, MidpointRounding.ToZero));
            Assert.Equal( 2, Math.Round( 2.5, MidpointRounding.ToZero));
            Assert.Equal(-2, Math.Round(-2.5, MidpointRounding.ToZero));
            Assert.Equal( 3, Math.Round( 3.0, MidpointRounding.ToZero));
            Assert.Equal(-3, Math.Round(-3.0, MidpointRounding.ToZero));
            Assert.Equal( 3, Math.Round( 3.5, MidpointRounding.ToZero));
            Assert.Equal(-3, Math.Round(-3.5, MidpointRounding.ToZero));

            Assert.Equal( 1, Math.Round( 0.5, MidpointRounding.AwayFromZero));
            Assert.Equal( 1, Math.Round( 0.5, MidpointRounding.AwayFromZero));
            Assert.Equal( 1, Math.Round( 1.0, MidpointRounding.AwayFromZero));
            Assert.Equal(-1, Math.Round(-1.0, MidpointRounding.AwayFromZero));
            Assert.Equal( 2, Math.Round( 1.5, MidpointRounding.AwayFromZero));
            Assert.Equal(-2, Math.Round(-1.5, MidpointRounding.AwayFromZero));
            Assert.Equal( 2, Math.Round( 2.0, MidpointRounding.AwayFromZero));
            Assert.Equal(-2, Math.Round(-2.0, MidpointRounding.AwayFromZero));
            Assert.Equal( 3, Math.Round( 2.5, MidpointRounding.AwayFromZero));
            Assert.Equal(-3, Math.Round(-2.5, MidpointRounding.AwayFromZero));
            Assert.Equal( 3, Math.Round( 3.0, MidpointRounding.AwayFromZero));
            Assert.Equal(-3, Math.Round(-3.0, MidpointRounding.AwayFromZero));
            Assert.Equal( 4, Math.Round( 3.5, MidpointRounding.AwayFromZero));
            Assert.Equal(-4, Math.Round(-3.5, MidpointRounding.AwayFromZero));
        }

        [Fact]
        public static void Round_Float_Constant_Arg()
        {
            Assert.Equal( 0, MathF.Round( 0.5f));
            Assert.Equal( 0, MathF.Round(-0.5f));
            Assert.Equal( 1, MathF.Round( 1.0f));
            Assert.Equal(-1, MathF.Round(-1.0f));
            Assert.Equal( 2, MathF.Round( 1.5f));
            Assert.Equal(-2, MathF.Round(-1.5f));
            Assert.Equal( 2, MathF.Round( 2.0f));
            Assert.Equal(-2, MathF.Round(-2.0f));
            Assert.Equal( 2, MathF.Round( 2.5f));
            Assert.Equal(-2, MathF.Round(-2.5f));
            Assert.Equal( 3, MathF.Round( 3.0f));
            Assert.Equal(-3, MathF.Round(-3.0f));
            Assert.Equal( 4, MathF.Round( 3.5f));
            Assert.Equal(-4, MathF.Round(-3.5f));

            Assert.Equal( 0, MathF.Round( 0.5f, MidpointRounding.ToZero));
            Assert.Equal( 0, MathF.Round( 0.5f, MidpointRounding.ToZero));
            Assert.Equal( 1, MathF.Round( 1.0f, MidpointRounding.ToZero));
            Assert.Equal(-1, MathF.Round(-1.0f, MidpointRounding.ToZero));
            Assert.Equal( 1, MathF.Round( 1.5f, MidpointRounding.ToZero));
            Assert.Equal(-1, MathF.Round(-1.5f, MidpointRounding.ToZero));
            Assert.Equal( 2, MathF.Round( 2.0f, MidpointRounding.ToZero));
            Assert.Equal(-2, MathF.Round(-2.0f, MidpointRounding.ToZero));
            Assert.Equal( 2, MathF.Round( 2.5f, MidpointRounding.ToZero));
            Assert.Equal(-2, MathF.Round(-2.5f, MidpointRounding.ToZero));
            Assert.Equal( 3, MathF.Round( 3.0f, MidpointRounding.ToZero));
            Assert.Equal(-3, MathF.Round(-3.0f, MidpointRounding.ToZero));
            Assert.Equal( 3, MathF.Round( 3.5f, MidpointRounding.ToZero));
            Assert.Equal(-3, MathF.Round(-3.5f, MidpointRounding.ToZero));

            Assert.Equal( 1, MathF.Round( 0.5f, MidpointRounding.AwayFromZero));
            Assert.Equal( 1, MathF.Round( 0.5f, MidpointRounding.AwayFromZero));
            Assert.Equal( 1, MathF.Round( 1.0f, MidpointRounding.AwayFromZero));
            Assert.Equal(-1, MathF.Round(-1.0f, MidpointRounding.AwayFromZero));
            Assert.Equal( 2, MathF.Round( 1.5f, MidpointRounding.AwayFromZero));
            Assert.Equal(-2, MathF.Round(-1.5f, MidpointRounding.AwayFromZero));
            Assert.Equal( 2, MathF.Round( 2.0f, MidpointRounding.AwayFromZero));
            Assert.Equal(-2, MathF.Round(-2.0f, MidpointRounding.AwayFromZero));
            Assert.Equal( 3, MathF.Round( 2.5f, MidpointRounding.AwayFromZero));
            Assert.Equal(-3, MathF.Round(-2.5f, MidpointRounding.AwayFromZero));
            Assert.Equal( 3, MathF.Round( 3.0f, MidpointRounding.AwayFromZero));
            Assert.Equal(-3, MathF.Round(-3.0f, MidpointRounding.AwayFromZero));
            Assert.Equal( 4, MathF.Round( 3.5f, MidpointRounding.AwayFromZero));
            Assert.Equal(-4, MathF.Round(-3.5f, MidpointRounding.AwayFromZero));
        }

        public static IEnumerable<object[]> Round_ToEven_TestData()
        {
            yield return new object[] { 1, 1 };
            yield return new object[] { 0.5, 0 };
            yield return new object[] { 1.5, 2 };
            yield return new object[] { 2.5, 2 };
            yield return new object[] { 3.5, 4 };

            // Math.Round(var = 0.49999999999999994) returns 1 on ARM32
            if (!PlatformDetection.IsArmProcess)
                yield return new object[] { 0.49999999999999994, 0 };

            yield return new object[] { 1.5, 2 };
            yield return new object[] { 2.5, 2 };
            yield return new object[] { 3.5, 4 };
            yield return new object[] { 4.5, 4 };
            yield return new object[] { 3.141592653589793, 3 };
            yield return new object[] { 2.718281828459045, 3 };
            yield return new object[] { 1385.4557313670111, 1385 };
            yield return new object[] { 3423423.43432, 3423423 };
            yield return new object[] { 535345.5, 535346 };
            yield return new object[] { 535345.50001, 535346 };
            yield return new object[] { 535345.5, 535346 };
            yield return new object[] { 535345.4, 535345 };
            yield return new object[] { 535345.6, 535346 };
            yield return new object[] { -2.718281828459045, -3 };
            yield return new object[] { 10, 10 };
            yield return new object[] { -10, -10 };
            yield return new object[] { -0, -0 };
            yield return new object[] { 0, 0 };
            yield return new object[] { double.NaN, double.NaN };
            yield return new object[] { double.PositiveInfinity, double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity, double.NegativeInfinity };
            yield return new object[] { 1.7976931348623157E+308, 1.7976931348623157E+308 };
            yield return new object[] { -1.7976931348623157E+308, -1.7976931348623157E+308 };
        }

        [Theory]
        [MemberData(nameof(Round_ToEven_TestData))]
        public static void Round_ToEven_0(double value, double expected)
        {
            // Math.Round has special fast paths when MidpointRounding is a const
            // Don't replace it with a variable
            Assert.Equal(expected, Math.Round(value, MidpointRounding.ToEven));
            Assert.Equal(expected, Math.Round(value, 0, MidpointRounding.ToEven));
        }

        public static IEnumerable<object[]> Round_AwayFromZero_TestData()
        {
            yield return new object[] { 1, 1 };
            yield return new object[] { 0.5, 1 };
            yield return new object[] { 1.5, 2 };
            yield return new object[] { 2.5, 3 };
            yield return new object[] { 3.5, 4 };
            yield return new object[] { 0.49999999999999994, 0 };
            yield return new object[] { 1.5, 2 };
            yield return new object[] { 2.5, 3 };
            yield return new object[] { 3.5, 4 };
            yield return new object[] { 4.5, 5 };
            yield return new object[] { 3.141592653589793, 3 };
            yield return new object[] { 2.718281828459045, 3 };
            yield return new object[] { 1385.4557313670111, 1385 };
            yield return new object[] { 3423423.43432, 3423423 };
            yield return new object[] { 535345.5, 535346 };
            yield return new object[] { 535345.50001, 535346 };
            yield return new object[] { 535345.5, 535346 };
            yield return new object[] { 535345.4, 535345 };
            yield return new object[] { 535345.6, 535346 };
            yield return new object[] { -2.718281828459045, -3 };
            yield return new object[] { 10, 10 };
            yield return new object[] { -10, -10 };
            yield return new object[] { -0, -0 };
            yield return new object[] { 0, 0 };
            yield return new object[] { double.NaN, double.NaN };
            yield return new object[] { double.PositiveInfinity, double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity, double.NegativeInfinity };
            yield return new object[] { 1.7976931348623157E+308, 1.7976931348623157E+308 };
            yield return new object[] { -1.7976931348623157E+308, -1.7976931348623157E+308 };
        }

        [Theory]
        [MemberData(nameof(Round_AwayFromZero_TestData))]
        public static void Round_AwayFromZero_0(double value, double expected)
        {
            // Math.Round has special fast paths when MidpointRounding is a const
            // Don't replace it with a variable
            Assert.Equal(expected, Math.Round(value, MidpointRounding.AwayFromZero));
            Assert.Equal(expected, Math.Round(value, 0, MidpointRounding.AwayFromZero));
        }
    }
}
