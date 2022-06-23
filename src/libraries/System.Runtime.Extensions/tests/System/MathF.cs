// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;

#pragma warning disable xUnit1025 // reporting duplicate test cases due to not distinguishing 0.0 from -0.0

namespace System.Tests
{
    public static class MathFTests
    {
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
        private const float CrossPlatformMachineEpsilon = 4.76837158e-07f;

        // The existing estimate functions either have an error of no more than 1.5 * 2^-12 (approx. 3.66e-04)
        // or perform one Newton-Raphson iteration which, for the currently tested values, gives an error of
        // no more than approx. 1.5 * 2^-7 (approx 1.17e-02).
        private const double CrossPlatformMachineEpsilonForEstimates = 1.171875e-02f;

        [Fact]
        public static void E()
        {
            Assert.Equal(0x402DF854, BitConverter.SingleToInt32Bits(MathF.E));
        }

        [Fact]
        public static void Pi()
        {
            Assert.Equal(0x40490FDB, BitConverter.SingleToInt32Bits(MathF.PI));
        }

        [Fact]
        public static void Tau()
        {
            Assert.Equal(0x40C90FDB, BitConverter.SingleToInt32Bits(MathF.Tau));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, 0.0f)]
        [InlineData(-3.14159265f, 3.14159265f, CrossPlatformMachineEpsilon * 10)]     // value: -(pi)             expected: (pi)
        [InlineData(-2.71828183f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]     // value: -(e)              expected: (e)
        [InlineData(-2.30258509f, 2.30258509f, CrossPlatformMachineEpsilon * 10)]     // value: -(ln(10))         expected: (ln(10))
        [InlineData(-1.57079633f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]     // value: -(pi / 2)         expected: (pi / 2)
        [InlineData(-1.44269504f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]     // value: -(log2(e))        expected: (log2(e))
        [InlineData(-1.41421356f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]     // value: -(sqrt(2))        expected: (sqrt(2))
        [InlineData(-1.12837917f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]     // value: -(2 / sqrt(pi))   expected: (2 / sqrt(pi))
        [InlineData(-1.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.785398163f, 0.785398163f, CrossPlatformMachineEpsilon)]          // value: -(pi / 4)         expected: (pi / 4)
        [InlineData(-0.707106781f, 0.707106781f, CrossPlatformMachineEpsilon)]          // value: -(1 / sqrt(2))    expected: (1 / sqrt(2))
        [InlineData(-0.693147181f, 0.693147181f, CrossPlatformMachineEpsilon)]          // value: -(ln(2))          expected: (ln(2))
        [InlineData(-0.636619772f, 0.636619772f, CrossPlatformMachineEpsilon)]          // value: -(2 / pi)         expected: (2 / pi)
        [InlineData(-0.434294482f, 0.434294482f, CrossPlatformMachineEpsilon)]          // value: -(log10(e))       expected: (log10(e))
        [InlineData(-0.318309886f, 0.318309886f, CrossPlatformMachineEpsilon)]          // value: -(1 / pi)         expected: (1 / pi)
        [InlineData(-0.0f, 0.0f, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f)]
        [InlineData(0.318309886f, 0.318309886f, CrossPlatformMachineEpsilon)]          // value:  (1 / pi)         expected: (1 / pi)
        [InlineData(0.434294482f, 0.434294482f, CrossPlatformMachineEpsilon)]          // value:  (log10(e))       expected: (log10(e))
        [InlineData(0.636619772f, 0.636619772f, CrossPlatformMachineEpsilon)]          // value:  (2 / pi)         expected: (2 / pi)
        [InlineData(0.693147181f, 0.693147181f, CrossPlatformMachineEpsilon)]          // value:  (ln(2))          expected: (ln(2))
        [InlineData(0.707106781f, 0.707106781f, CrossPlatformMachineEpsilon)]          // value:  (1 / sqrt(2))    expected: (1 / sqrt(2))
        [InlineData(0.785398163f, 0.785398163f, CrossPlatformMachineEpsilon)]          // value:  (pi / 4)         expected: (pi / 4)
        [InlineData(1.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.12837917f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]     // value:  (2 / sqrt(pi))   expected: (2 / sqrt(pi))
        [InlineData(1.41421356f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]     // value:  (sqrt(2))        expected: (sqrt(2))
        [InlineData(1.44269504f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]     // value:  (log2(e))        expected: (log2(e))
        [InlineData(1.57079633f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 2)         expected: (pi / 2)
        [InlineData(2.30258509f, 2.30258509f, CrossPlatformMachineEpsilon * 10)]     // value:  (ln(10))         expected: (ln(10))
        [InlineData(2.71828183f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]     // value:  (e)              expected: (e)
        [InlineData(3.14159265f, 3.14159265f, CrossPlatformMachineEpsilon * 10)]     // value:  (pi)             expected: (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Abs(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Abs(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-3.14159265f, float.NaN, 0.0f)]                               //                              value: -(pi)
        [InlineData(-2.71828183f, float.NaN, 0.0f)]                               //                              value: -(e)
        [InlineData(-1.41421356f, float.NaN, 0.0f)]                               //                              value: -(sqrt(2))
        [InlineData(-1.0f, 3.14159265f, CrossPlatformMachineEpsilon * 10)]   // expected:  (pi)
        [InlineData(-0.911733915f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]   // expected:  (e)
        [InlineData(-0.668201510f, 2.30258509f, CrossPlatformMachineEpsilon * 10)]   // expected:  (ln(10))
        [InlineData(-0.0f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]   // expected:  (pi / 2)
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]   // expected:  (pi / 2)
        [InlineData(0.127751218f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]   // expected:  (log2(e))
        [InlineData(0.155943695f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]   // expected:  (sqrt(2))
        [InlineData(0.428125148f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]   // expected:  (2 / sqrt(pi))
        [InlineData(0.540302306f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.707106781f, 0.785398163f, CrossPlatformMachineEpsilon)]        // expected:  (pi / 4),         value:  (1 / sqrt(2))
        [InlineData(0.760244597f, 0.707106781f, CrossPlatformMachineEpsilon)]        // expected:  (1 / sqrt(2))
        [InlineData(0.769238901f, 0.693147181f, CrossPlatformMachineEpsilon)]        // expected:  (ln(2))
        [InlineData(0.804109828f, 0.636619772f, CrossPlatformMachineEpsilon)]        // expected:  (2 / pi)
        [InlineData(0.907167129f, 0.434294482f, CrossPlatformMachineEpsilon)]        // expected:  (log10(e))
        [InlineData(0.949765715f, 0.318309886f, CrossPlatformMachineEpsilon)]        // expected:  (1 / pi)
        [InlineData(1.0f, 0.0f, 0.0f)]
        [InlineData(1.41421356f, float.NaN, 0.0f)]                               //                              value:  (sqrt(2))
        [InlineData(2.71828183f, float.NaN, 0.0f)]                               //                              value:  (e)
        [InlineData(3.14159265f, float.NaN, 0.0f)]                               //                              value:  (pi)
        [InlineData(float.PositiveInfinity, float.NaN, 0.0f)]
        public static void Acos(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Acos(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-3.14159265f, float.NaN, 0.0f)]                              //                                value: -(pi)
        [InlineData(-2.71828183f, float.NaN, 0.0f)]                              //                                value: -(e)
        [InlineData(-1.41421356f, float.NaN, 0.0f)]                              //                                value: -(sqrt(2))
        [InlineData(-1.0f, float.NaN, 0.0f)]
        [InlineData(-0.693147181f, float.NaN, 0.0f)]                              //                                value: -(ln(2))
        [InlineData(-0.434294482f, float.NaN, 0.0f)]                              //                                value: -(log10(e))
        [InlineData(-0.0f, float.NaN, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, float.NaN, 0.0f)]
        [InlineData(1.0f, 0.0f, CrossPlatformMachineEpsilon)]
        [InlineData(1.05108979f, 0.318309886f, CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData(1.09579746f, 0.434294482f, CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData(1.20957949f, 0.636619772f, CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData(1.25f, 0.693147181f, CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData(1.26059184f, 0.707106781f, CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData(1.32460909f, 0.785398163f, CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData(1.54308063f, 1.0, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.70710014f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData(2.17818356f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData(2.23418810f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData(2.50917848f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData(5.05f, 2.30258509f, CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData(7.61012514f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData(11.5919533f, 3.14159265f, CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Acosh(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Acosh(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-3.14159265f, float.NaN, 0.0f)]                              //                              value: -(pi)
        [InlineData(-2.71828183f, float.NaN, 0.0f)]                              //                              value: -(e)
        [InlineData(-1.41421356f, float.NaN, 0.0f)]                              //                              value: -(sqrt(2))
        [InlineData(-1.0f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.991806244f, -1.44269504f, CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.987765946f, -1.41421356f, CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.903719457f, -1.12837917f, CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.841470985f, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.743980337f, -0.839007561f, CrossPlatformMachineEpsilon)]       // expected: -(pi - ln(10))
        [InlineData(-0.707106781f, -0.785398163f, CrossPlatformMachineEpsilon)]       // expected: -(pi / 4),         value: (1 / sqrt(2))
        [InlineData(-0.649636939f, -0.707106781f, CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.638961276f, -0.693147181f, CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.594480769f, -0.636619772f, CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.420770483f, -0.434294482f, CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.410781291f, -0.423310825f, CrossPlatformMachineEpsilon)]       // expected: -(pi - e)
        [InlineData(-0.312961796f, -0.318309886f, CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0f, -0.0f, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f)]
        [InlineData(0.312961796f, 0.318309886f, CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData(0.410781291f, 0.423310825f, CrossPlatformMachineEpsilon)]       // expected:  (pi - e)
        [InlineData(0.420770483f, 0.434294482f, CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData(0.594480769f, 0.636619772f, CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData(0.638961276f, 0.693147181f, CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData(0.649636939f, 0.707106781f, CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData(0.707106781f, 0.785398163f, CrossPlatformMachineEpsilon)]       // expected:  (pi / 4),         value: (1 / sqrt(2))
        [InlineData(0.743980337f, 0.839007561f, CrossPlatformMachineEpsilon)]       // expected:  (pi - ln(10))
        [InlineData(0.841470985f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.903719457f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData(0.987765946f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData(0.991806244f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData(1.0f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData(1.41421356f, float.NaN, 0.0f)]                              //                              value:  (sqrt(2))
        [InlineData(2.71828183f, float.NaN, 0.0f)]                              //                              value:  (e)
        [InlineData(3.14159265f, float.NaN, 0.0f)]                              //                              value:  (pi)
        [InlineData(float.PositiveInfinity, float.NaN, 0.0f)]
        public static void Asin(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Asin(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, 0.0f)]
        [InlineData(-11.5487394f, -3.14159265f, CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData(-7.54413710f, -2.71828183f, CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-4.95f, -2.30258509f, CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-2.30129890f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-1.99789801f, -1.44269504f, CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-1.93506682f, -1.41421356f, CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-1.38354288f, -1.12837917f, CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-1.17520119f, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.868670961f, -0.785398163f, CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.767523145f, -0.707106781f, CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.75f, -0.693147181f, CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.680501678f, -0.636619772f, CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.448075979f, -0.434294482f, CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.323712439f, -0.318309886f, CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0f, -0.0, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0, 0.0f)]
        [InlineData(0.323712439f, 0.318309886f, CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData(0.448075979f, 0.434294482f, CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData(0.680501678f, 0.636619772f, CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData(0.75f, 0.693147181f, CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData(0.767523145f, 0.707106781f, CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData(0.868670961f, 0.785398163f, CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData(1.17520119f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.38354288f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData(1.93506682f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData(1.99789801f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData(2.30129890f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData(4.95f, 2.30258509f, CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData(7.54413710f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData(11.5487394f, 3.14159265f, CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Asinh(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Asinh(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, -1.57079633f, CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-7.76357567f, -1.44269504f, CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-6.33411917f, -1.41421356f, CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-2.11087684f, -1.12837917f, CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-1.55740772f, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.11340715f, -0.839007561f, CrossPlatformMachineEpsilon)]       // expected: -(pi - ln(10))
        [InlineData(-1.0f, -0.785398163f, CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.854510432f, -0.707106781f, CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.830640878f, -0.693147181f, CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.739302950f, -0.636619772f, CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.463829067f, -0.434294482f, CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.450549534f, -0.423310825f, CrossPlatformMachineEpsilon)]       // expected: -(pi - e)
        [InlineData(-0.329514733f, -0.318309886f, CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0f, -0.0f, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f)]
        [InlineData(0.329514733f, 0.318309886f, CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData(0.450549534f, 0.423310825f, CrossPlatformMachineEpsilon)]       // expected:  (pi - e)
        [InlineData(0.463829067f, 0.434294482f, CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData(0.739302950f, 0.636619772f, CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData(0.830640878f, 0.693147181f, CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData(0.854510432f, 0.707106781f, CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData(1.0f, 0.785398163f, CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData(1.11340715f, 0.839007561f, CrossPlatformMachineEpsilon)]       // expected:  (pi - ln(10))
        [InlineData(1.55740772f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(2.11087684f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData(6.33411917f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData(7.76357567f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData(float.PositiveInfinity, 1.57079633f, CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        public static void Atan(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Atan(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, -1.0f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData(float.NegativeInfinity, -0.0f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData(float.NegativeInfinity, float.NaN, float.NaN, 0.0f)]
        [InlineData(float.NegativeInfinity, 0.0f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData(float.NegativeInfinity, 1.0f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData(-1.0f, -1.0f, -2.35619449f, CrossPlatformMachineEpsilon * 10)]     // expected: -(3 * pi / 4)
        [InlineData(-1.0f, -0.0f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData(-1.0f, float.NaN, float.NaN, 0.0f)]
        [InlineData(-1.0f, 0.0f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData(-1.0f, 1.0f, -0.785398163f, CrossPlatformMachineEpsilon)]          // expected: -(pi / 4)
        [InlineData(-1.0f, float.PositiveInfinity, -0.0f, 0.0f)]
        [InlineData(-0.991806244f, -0.127751218f, -1.69889761f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - log2(e))
        [InlineData(-0.991806244f, 0.127751218f, -1.44269504f, CrossPlatformMachineEpsilon * 10)]     // expected: -(log2(e))
        [InlineData(-0.987765946f, -0.155943695f, -1.72737909f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - sqrt(2))
        [InlineData(-0.987765946f, 0.155943695f, -1.41421356f, CrossPlatformMachineEpsilon * 10)]     // expected: -(sqrt(2))
        [InlineData(-0.903719457f, -0.428125148f, -2.01321349f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - (2 / sqrt(pi))
        [InlineData(-0.903719457f, 0.428125148f, -1.12837917f, CrossPlatformMachineEpsilon * 10)]     // expected: -(2 / sqrt(pi)
        [InlineData(-0.841470985f, -0.540302306f, -2.14159265f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - 1)
        [InlineData(-0.841470985f, 0.540302306f, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.743980337f, -0.668201510f, -2.30258509f, CrossPlatformMachineEpsilon * 10)]     // expected: -(ln(10))
        [InlineData(-0.743980337f, 0.668201510f, -0.839007561f, CrossPlatformMachineEpsilon)]          // expected: -(pi - ln(10))
        [InlineData(-0.707106781f, -0.707106781f, -2.35619449f, CrossPlatformMachineEpsilon * 10)]     // expected: -(3 * pi / 4),         y: -(1 / sqrt(2))   x: -(1 / sqrt(2))
        [InlineData(-0.707106781f, 0.707106781f, -0.785398163f, CrossPlatformMachineEpsilon)]          // expected: -(pi / 4),             y: -(1 / sqrt(2))   x:  (1 / sqrt(2))
        [InlineData(-0.649636939f, -0.760244597f, -2.43448587f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - (1 / sqrt(2))
        [InlineData(-0.649636939f, 0.760244597f, -0.707106781f, CrossPlatformMachineEpsilon)]          // expected: -(1 / sqrt(2))
        [InlineData(-0.638961276f, -0.769238901f, -2.44844547f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - ln(2))
        [InlineData(-0.638961276f, 0.769238901f, -0.693147181f, CrossPlatformMachineEpsilon)]          // expected: -(ln(2))
        [InlineData(-0.594480769f, -0.804109828f, -2.50497288f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - (2 / pi))
        [InlineData(-0.594480769f, 0.804109828f, -0.636619772f, CrossPlatformMachineEpsilon)]          // expected: -(2 / pi)
        [InlineData(-0.420770483f, -0.907167129f, -2.70729817f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - log10(e))
        [InlineData(-0.420770483f, 0.907167129f, -0.434294482f, CrossPlatformMachineEpsilon)]          // expected: -(log10(e))
        [InlineData(-0.410781291f, -0.911733915f, -2.71828183f, CrossPlatformMachineEpsilon * 10)]     // expected: -(e)
        [InlineData(-0.410781291f, 0.911733915f, -0.423310825f, CrossPlatformMachineEpsilon)]          // expected: -(pi - e)
        [InlineData(-0.312961796f, -0.949765715f, -2.82328277f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - (1 / pi))
        [InlineData(-0.312961796f, 0.949765715f, -0.318309886f, CrossPlatformMachineEpsilon)]          // expected: -(1 / pi)
        [InlineData(-0.0f, float.NegativeInfinity, -3.14159265f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi)
        [InlineData(-0.0f, -1.0f, -3.14159265f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi)
        [InlineData(-0.0f, -0.0f, -3.14159265f, CrossPlatformMachineEpsilon * 10)]     // expected: -(pi)
        [InlineData(-0.0f, float.NaN, float.NaN, 0.0f)]
        [InlineData(-0.0f, 0.0f, -0.0f, 0.0f)]
        [InlineData(-0.0f, 1.0f, -0.0f, 0.0f)]
        [InlineData(-0.0f, float.PositiveInfinity, -0.0f, 0.0f)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(float.NaN, -1.0f, float.NaN, 0.0f)]
        [InlineData(float.NaN, -0.0f, float.NaN, 0.0f)]
        [InlineData(float.NaN, float.NaN, float.NaN, 0.0f)]
        [InlineData(float.NaN, 0.0f, float.NaN, 0.0f)]
        [InlineData(float.NaN, 1.0f, float.NaN, 0.0f)]
        [InlineData(float.NaN, float.PositiveInfinity, float.NaN, 0.0f)]
        [InlineData(0.0f, float.NegativeInfinity, 3.14159265f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi)
        [InlineData(0.0f, -1.0f, 3.14159265f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi)
        [InlineData(0.0f, -0.0f, 3.14159265f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi)
        [InlineData(0.0f, float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, 1.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, float.PositiveInfinity, 0.0f, 0.0f)]
        [InlineData(0.312961796f, -0.949765715f, 2.82328277f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - (1 / pi))
        [InlineData(0.312961796f, 0.949765715f, 0.318309886f, CrossPlatformMachineEpsilon)]          // expected:  (1 / pi)
        [InlineData(0.410781291f, -0.911733915f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]     // expected:  (e)
        [InlineData(0.410781291f, 0.911733915f, 0.423310825f, CrossPlatformMachineEpsilon)]          // expected:  (pi - e)
        [InlineData(0.420770483f, -0.907167129f, 2.70729817f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - log10(e))
        [InlineData(0.420770483f, 0.907167129f, 0.434294482f, CrossPlatformMachineEpsilon)]          // expected:  (log10(e))
        [InlineData(0.594480769f, -0.804109828f, 2.50497288f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - (2 / pi))
        [InlineData(0.594480769f, 0.804109828f, 0.636619772f, CrossPlatformMachineEpsilon)]          // expected:  (2 / pi)
        [InlineData(0.638961276f, -0.769238901f, 2.44844547f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - ln(2))
        [InlineData(0.638961276f, 0.769238901f, 0.693147181f, CrossPlatformMachineEpsilon)]          // expected:  (ln(2))
        [InlineData(0.649636939f, -0.760244597f, 2.43448587f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - (1 / sqrt(2))
        [InlineData(0.649636939f, 0.760244597f, 0.707106781f, CrossPlatformMachineEpsilon)]          // expected:  (1 / sqrt(2))
        [InlineData(0.707106781f, -0.707106781f, 2.35619449f, CrossPlatformMachineEpsilon * 10)]     // expected:  (3 * pi / 4),         y:  (1 / sqrt(2))   x: -(1 / sqrt(2))
        [InlineData(0.707106781f, 0.707106781f, 0.785398163f, CrossPlatformMachineEpsilon)]          // expected:  (pi / 4),             y:  (1 / sqrt(2))   x:  (1 / sqrt(2))
        [InlineData(0.743980337f, -0.668201510f, 2.30258509f, CrossPlatformMachineEpsilon * 10)]     // expected:  (ln(10))
        [InlineData(0.743980337f, 0.668201510f, 0.839007561f, CrossPlatformMachineEpsilon)]          // expected:  (pi - ln(10))
        [InlineData(0.841470985f, -0.540302306f, 2.14159265f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - 1)
        [InlineData(0.841470985f, 0.540302306f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.903719457f, -0.428125148f, 2.01321349f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - (2 / sqrt(pi))
        [InlineData(0.903719457f, 0.428125148f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]     // expected:  (2 / sqrt(pi))
        [InlineData(0.987765946f, -0.155943695f, 1.72737909f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - sqrt(2))
        [InlineData(0.987765946f, 0.155943695f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]     // expected:  (sqrt(2))
        [InlineData(0.991806244f, -0.127751218f, 1.69889761f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - log2(e))
        [InlineData(0.991806244f, 0.127751218f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]     // expected:  (log2(e))
        [InlineData(1.0f, -1.0f, 2.35619449f, CrossPlatformMachineEpsilon * 10)]     // expected:  (3 * pi / 4)
        [InlineData(1.0f, -0.0f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        [InlineData(1.0f, float.NaN, float.NaN, 0.0f)]
        [InlineData(1.0f, 0.0f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        [InlineData(1.0f, 1.0f, 0.785398163f, CrossPlatformMachineEpsilon)]          // expected:  (pi / 4)
        [InlineData(1.0f, float.PositiveInfinity, 0.0f, 0.0f)]
        [InlineData(float.PositiveInfinity, -1.0f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        [InlineData(float.PositiveInfinity, -0.0f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        [InlineData(float.PositiveInfinity, float.NaN, float.NaN, 0.0f)]
        [InlineData(float.PositiveInfinity, 0.0f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        [InlineData(float.PositiveInfinity, 1.0f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        public static void Atan2(float y, float x, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Atan2(y, x), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, -2.35619449f, CrossPlatformMachineEpsilon * 10)]     // expected: -(3 * pi / 4)
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, -0.785398163f, CrossPlatformMachineEpsilon)]          // expected: -(pi / 4)
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, 2.35619449f, CrossPlatformMachineEpsilon * 10)]     // expected:  (3 * pi / 4
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.785398163f, CrossPlatformMachineEpsilon)]          // expected:  (pi / 4)
        public static void Atan2_IEEE(float y, float x, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Atan2(y, x), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-3.14159265f, float.NaN, 0.0f)]                              //                                value: -(pi)
        [InlineData(-2.71828183f, float.NaN, 0.0f)]                              //                                value: -(e)
        [InlineData(-1.41421356f, float.NaN, 0.0f)]                              //                                value: -(sqrt(2))
        [InlineData(-1.0f, float.NegativeInfinity, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.996272076f, -3.14159265f, CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData(-0.991328916f, -2.71828183f, CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-0.980198020f, -2.30258509f, CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-0.917152336f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.894238946f, -1.44269504f, CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.888385562f, -1.41421356f, CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.810463806f, -1.12837917f, CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.761594156f, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.655794203f, -0.785398163f, CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.608859365f, -0.707106781f, CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.6f, -0.693147181f, CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.562593600f, -0.636619772f, CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.408904012f, -0.434294482f, CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.307977913f, -0.318309886f, CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0f, -0.0f, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0, 0.0f, 0.0f)]
        [InlineData(0.307977913f, 0.318309886f, CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData(0.408904012f, 0.434294482f, CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData(0.562593600f, 0.636619772f, CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData(0.6f, 0.693147181f, CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData(0.608859365f, 0.707106781f, CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData(0.655794203f, 0.785398163f, CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData(0.761594156f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.810463806f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData(0.888385562f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData(0.894238946f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData(0.917152336f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData(0.980198020f, 2.30258509f, CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData(0.991328916f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData(0.996272076f, 3.14159265f, CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData(1.0f, float.PositiveInfinity, 0.0f)]
        [InlineData(3.14159265f, float.NaN, 0.0f)]                              //                                value:  (pi)
        [InlineData(2.71828183f, float.NaN, 0.0f)]                              //                                value:  (e)
        [InlineData(1.41421356f, float.NaN, 0.0f)]                              //                                value:  (sqrt(2))
        [InlineData(float.PositiveInfinity, float.NaN, 0.0f)]
        public static void Atanh(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Atanh(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(-3.14159265f, -3.14159298f)]     // value: -(pi)
        [InlineData(-2.71828183f, -2.71828198f)]     // value: -(e)
        [InlineData(-2.30258509f, -2.30258536f)]     // value: -(ln(10))
        [InlineData(-1.57079633f, -1.57079649f)]     // value: -(pi / 2)
        [InlineData(-1.44269504f, -1.44269514f)]     // value: -(log2(e))
        [InlineData(-1.41421356f, -1.41421366f)]     // value: -(sqrt(2))
        [InlineData(-1.12837917f, -1.12837934f)]     // value: -(2 / sqrt(pi))
        [InlineData(-1.0f, -1.00000012f)]
        [InlineData(-0.785398163f, -0.785398245f)]    // value: -(pi / 4)
        [InlineData(-0.707106781f, -0.707106829f)]    // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f, -0.693147242f)]    // value: -(ln(2))
        [InlineData(-0.636619772f, -0.636619806f)]    // value: -(2 / pi)
        [InlineData(-0.434294482f, -0.434294522f)]    // value: -(log10(e))
        [InlineData(-0.318309886f, -0.318309903f)]    // value: -(1 / pi)
        [InlineData(-0.0f, -float.Epsilon)]
        [InlineData(float.NaN, float.NaN)]
        [InlineData(0.0f, -float.Epsilon)]
        [InlineData(0.318309886f, 0.318309844f)]    // value:  (1 / pi)
        [InlineData(0.434294482f, 0.434294462f)]    // value:  (log10(e))
        [InlineData(0.636619772f, 0.636619687f)]    // value:  (2 / pi)
        [InlineData(0.693147181f, 0.693147123f)]    // value:  (ln(2))
        [InlineData(0.707106781f, 0.707106709f)]    // value:  (1 / sqrt(2))
        [InlineData(0.785398163f, 0.785398126f)]    // value:  (pi / 4)
        [InlineData(1.0f, 0.999999940f)]
        [InlineData(1.12837917f, 1.12837911f)]     // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f, 1.41421342f)]     // value:  (sqrt(2))
        [InlineData(1.44269504f, 1.44269490f)]     // value:  (log2(e))
        [InlineData(1.57079633f, 1.57079625f)]     // value:  (pi / 2)
        [InlineData(2.30258509f, 2.30258489f)]     // value:  (ln(10))
        [InlineData(2.71828183f, 2.71828151f)]     // value:  (e)
        [InlineData(3.14159265f, 3.14159250f)]     // value:  (pi)
        [InlineData(float.PositiveInfinity, float.MaxValue)]
        public static void BitDecrement(float value, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, MathF.BitDecrement(value), 0.0f);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.MinValue)]
        [InlineData(-3.14159265f, -3.14159250f)]     // value: -(pi)
        [InlineData(-2.71828183f, -2.71828151f)]     // value: -(e)
        [InlineData(-2.30258509f, -2.30258489f)]     // value: -(ln(10))
        [InlineData(-1.57079633f, -1.57079625f)]     // value: -(pi / 2)
        [InlineData(-1.44269504f, -1.44269490f)]     // value: -(log2(e))
        [InlineData(-1.41421356f, -1.41421342f)]     // value: -(sqrt(2))
        [InlineData(-1.12837917f, -1.12837911f)]     // value: -(2 / sqrt(pi))
        [InlineData(-1.0f, -0.999999940f)]
        [InlineData(-0.785398163f, -0.785398126f)]    // value: -(pi / 4)
        [InlineData(-0.707106781f, -0.707106709f)]    // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f, -0.693147123f)]    // value: -(ln(2))
        [InlineData(-0.636619772f, -0.636619687f)]    // value: -(2 / pi)
        [InlineData(-0.434294482f, -0.434294462f)]    // value: -(log10(e))
        [InlineData(-0.318309886f, -0.318309844f)]    // value: -(1 / pi)
        [InlineData(-0.0f, float.Epsilon)]
        [InlineData(float.NaN, float.NaN)]
        [InlineData(0.0f, float.Epsilon)]
        [InlineData(0.318309886f, 0.318309903f)]    // value:  (1 / pi)
        [InlineData(0.434294482f, 0.434294522f)]    // value:  (log10(e))
        [InlineData(0.636619772f, 0.636619806f)]    // value:  (2 / pi)
        [InlineData(0.693147181f, 0.693147242f)]    // value:  (ln(2))
        [InlineData(0.707106781f, 0.707106829f)]    // value:  (1 / sqrt(2))
        [InlineData(0.785398163f, 0.785398245f)]    // value:  (pi / 4)
        [InlineData(1.0f, 1.00000012f)]
        [InlineData(1.12837917f, 1.12837934f)]     // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f, 1.41421366f)]     // value:  (sqrt(2))
        [InlineData(1.44269504f, 1.44269514f)]     // value:  (log2(e))
        [InlineData(1.57079633f, 1.57079649f)]     // value:  (pi / 2)
        [InlineData(2.30258509f, 2.30258536f)]     // value:  (ln(10))
        [InlineData(2.71828183f, 2.71828198f)]     // value:  (e)
        [InlineData(3.14159265f, 3.14159298f)]     // value:  (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity)]
        public static void BitIncrement(float value, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, MathF.BitIncrement(value), 0.0f);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, 0.0f)]
        [InlineData(-3.14159265f, -1.46459189f, CrossPlatformMachineEpsilon * 10)]   // value: -(pi)
        [InlineData(-2.71828183f, -1.39561243f, CrossPlatformMachineEpsilon * 10)]   // value: -(e)
        [InlineData(-2.30258509f, -1.32050048f, CrossPlatformMachineEpsilon * 10)]   // value: -(ln(10))
        [InlineData(-1.57079633f, -1.16244735f, CrossPlatformMachineEpsilon * 10)]   // value: -(pi / 2)
        [InlineData(-1.44269504f, -1.12994728f, CrossPlatformMachineEpsilon * 10)]   // value: -(log2(e))
        [InlineData(-1.41421356f, -1.12246205f, CrossPlatformMachineEpsilon * 10)]   // value: -(sqrt(2))
        [InlineData(-1.12837917f, -1.04108220f, CrossPlatformMachineEpsilon * 10)]   // value: -(2 / sqrt(pi))
        [InlineData(-1.0f, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.785398163f, -0.922635074f, CrossPlatformMachineEpsilon)]        // value: -(pi / 4)
        [InlineData(-0.707106781f, -0.890898718f, CrossPlatformMachineEpsilon)]        // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f, -0.884997045f, CrossPlatformMachineEpsilon)]        // value: -(ln(2))
        [InlineData(-0.636619772f, -0.860254014f, CrossPlatformMachineEpsilon)]        // value: -(2 / pi)
        [InlineData(-0.434294482f, -0.757288631f, CrossPlatformMachineEpsilon)]        // value: -(log10(e))
        [InlineData(-0.318309886f, -0.682784063f, CrossPlatformMachineEpsilon)]        // value: -(1 / pi)
        [InlineData(-0.0f, -0.0f, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f)]
        [InlineData(0.318309886f, 0.682784063f, CrossPlatformMachineEpsilon)]        // value:  (1 / pi)
        [InlineData(0.434294482f, 0.757288631f, CrossPlatformMachineEpsilon)]        // value:  (log10(e))
        [InlineData(0.636619772f, 0.860254014f, CrossPlatformMachineEpsilon)]        // value:  (2 / pi)
        [InlineData(0.693147181f, 0.884997045f, CrossPlatformMachineEpsilon)]        // value:  (ln(2))
        [InlineData(0.707106781f, 0.890898718f, CrossPlatformMachineEpsilon)]        // value:  (1 / sqrt(2))
        [InlineData(0.785398163f, 0.922635074f, CrossPlatformMachineEpsilon)]        // value:  (pi / 4)
        [InlineData(1.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.12837917f, 1.04108220f, CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f, 1.12246205f, CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData(1.44269504f, 1.12994728f, CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData(1.57079633f, 1.16244735f, CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData(2.30258509f, 1.32050048f, CrossPlatformMachineEpsilon * 10)]   // value:  (ln(10))
        [InlineData(2.71828183f, 1.39561243f, CrossPlatformMachineEpsilon * 10)]   // value:  (e)
        [InlineData(3.14159265f, 1.46459189f, CrossPlatformMachineEpsilon * 10)]   // value:  (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Cbrt(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Cbrt(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, 0.0f)]
        [InlineData(-3.14159265f, -3.0f, 0.0f)]     // value: -(pi)
        [InlineData(-2.71828183f, -2.0f, 0.0f)]     // value: -(e)
        [InlineData(-2.30258509f, -2.0f, 0.0f)]     // value: -(ln(10))
        [InlineData(-1.57079633f, -1.0f, 0.0f)]     // value: -(pi / 2)
        [InlineData(-1.44269504f, -1.0f, 0.0f)]     // value: -(log2(e))
        [InlineData(-1.41421356f, -1.0f, 0.0f)]     // value: -(sqrt(2))
        [InlineData(-1.12837917f, -1.0f, 0.0f)]     // value: -(2 / sqrt(pi))
        [InlineData(-1.0f, -1.0f, 0.0f)]
        [InlineData(-0.785398163f, -0.0f, 0.0f)]  // value: -(pi / 4)
        [InlineData(-0.707106781f, -0.0f, 0.0f)]  // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f, -0.0f, 0.0f)]  // value: -(ln(2))
        [InlineData(-0.636619772f, -0.0f, 0.0f)]  // value: -(2 / pi)
        [InlineData(-0.434294482f, -0.0f, 0.0f)]  // value: -(log10(e))
        [InlineData(-0.318309886f, -0.0f, 0.0f)]  // value: -(1 / pi)
        [InlineData(-0.0f, -0.0f, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f)]
        [InlineData(0.318309886f, 1.0f, 0.0f)]     // value:  (1 / pi)
        [InlineData(0.434294482f, 1.0f, 0.0f)]     // value:  (log10(e))
        [InlineData(0.636619772f, 1.0f, 0.0f)]     // value:  (2 / pi)
        [InlineData(0.693147181f, 1.0f, 0.0f)]     // value:  (ln(2))
        [InlineData(0.707106781f, 1.0f, 0.0f)]     // value:  (1 / sqrt(2))
        [InlineData(0.785398163f, 1.0f, 0.0f)]     // value:  (pi / 4)
        [InlineData(1.0f, 1.0f, 0.0f)]
        [InlineData(1.12837917f, 2.0f, 0.0f)]     // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f, 2.0f, 0.0f)]     // value:  (sqrt(2))
        [InlineData(1.44269504f, 2.0f, 0.0f)]     // value:  (log2(e))
        [InlineData(1.57079633f, 2.0f, 0.0f)]     // value:  (pi / 2)
        [InlineData(2.30258509f, 3.0f, 0.0f)]     // value:  (ln(10))
        [InlineData(2.71828183f, 3.0f, 0.0f)]     // value:  (e)
        [InlineData(3.14159265f, 4.0f, 0.0f)]     // value:  (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Ceiling(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Ceiling(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.NegativeInfinity, -3.14159265f, float.NegativeInfinity)]
        [InlineData(float.NegativeInfinity, -0.0f, float.NegativeInfinity)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NegativeInfinity)]
        [InlineData(float.NegativeInfinity, 0.0f, float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity, 3.14159265f, float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(-3.14159265f, float.NegativeInfinity, -3.14159265f)]
        [InlineData(-3.14159265f, -3.14159265f, -3.14159265f)]
        [InlineData(-3.14159265f, -0.0f, -3.14159265f)]
        [InlineData(-3.14159265f, float.NaN, -3.14159265f)]
        [InlineData(-3.14159265f, 0.0f, 3.14159265f)]
        [InlineData(-3.14159265f, 3.14159265f, 3.14159265f)]
        [InlineData(-3.14159265f, float.PositiveInfinity, 3.14159265f)]
        [InlineData(-0.0f, float.NegativeInfinity, -0.0f)]
        [InlineData(-0.0f, -3.14159265f, -0.0f)]
        [InlineData(-0.0f, -0.0f, -0.0f)]
        [InlineData(-0.0f, float.NaN, -0.0f)]
        [InlineData(-0.0f, 0.0f, 0.0f)]
        [InlineData(-0.0f, 3.14159265f, 0.0f)]
        [InlineData(-0.0f, float.PositiveInfinity, 0.0f)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NaN)]
        [InlineData(float.NaN, -3.14159265f, float.NaN)]
        [InlineData(float.NaN, -0.0f, float.NaN)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 0.0f, float.NaN)]
        [InlineData(float.NaN, 3.14159265f, float.NaN)]
        [InlineData(float.NaN, float.PositiveInfinity, float.NaN)]
        [InlineData(0.0f, float.NegativeInfinity, -0.0f)]
        [InlineData(0.0f, -3.14159265f, -0.0f)]
        [InlineData(0.0f, -0.0f, -0.0f)]
        [InlineData(0.0f, float.NaN, -0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, 3.14159265f, 0.0f)]
        [InlineData(0.0f, float.PositiveInfinity, 0.0f)]
        [InlineData(3.14159265f, float.NegativeInfinity, -3.14159265f)]
        [InlineData(3.14159265f, -3.14159265f, -3.14159265f)]
        [InlineData(3.14159265f, -0.0f, -3.14159265f)]
        [InlineData(3.14159265f, float.NaN, -3.14159265f)]
        [InlineData(3.14159265f, 0.0f, 3.14159265f)]
        [InlineData(3.14159265f, 3.14159265f, 3.14159265f)]
        [InlineData(3.14159265f, float.PositiveInfinity, 3.14159265f)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, -3.14159265f, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, -0.0f, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, float.NaN, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, 0.0f, float.PositiveInfinity)]
        [InlineData(float.PositiveInfinity, 3.14159265f, float.PositiveInfinity)]
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        public static void CopySign(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, MathF.CopySign(x, y), 0.0f);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-3.14159265f, -1.0f, CrossPlatformMachineEpsilon * 10)]  // value: -(pi)
        [InlineData(-2.71828183f, -0.911733918f, CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.30258509f, -0.668201510f, CrossPlatformMachineEpsilon)]       // value: -(ln(10))
        [InlineData(-1.57079633f, 0.0f, CrossPlatformMachineEpsilon)]       // value: -(pi / 2)
        [InlineData(-1.44269504f, 0.127751218f, CrossPlatformMachineEpsilon)]       // value: -(log2(e))
        [InlineData(-1.41421356f, 0.155943695f, CrossPlatformMachineEpsilon)]       // value: -(sqrt(2))
        [InlineData(-1.12837917f, 0.428125148f, CrossPlatformMachineEpsilon)]       // value: -(2 / sqrt(pi))
        [InlineData(-1.0f, 0.540302306f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f, 0.707106781f, CrossPlatformMachineEpsilon)]       // value: -(pi / 4),        expected:  (1 / sqrt(2))
        [InlineData(-0.707106781f, 0.760244597f, CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f, 0.769238901f, CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.636619772f, 0.804109828f, CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.434294482f, 0.907167129f, CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.318309886f, 0.949765715f, CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.318309886f, 0.949765715f, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData(0.434294482f, 0.907167129f, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData(0.636619772f, 0.804109828f, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData(0.693147181f, 0.769238901f, CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData(0.707106781f, 0.760244597f, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData(0.785398163f, 0.707106781f, CrossPlatformMachineEpsilon)]       // value:  (pi / 4),        expected:  (1 / sqrt(2))
        [InlineData(1.0f, 0.540302306f, CrossPlatformMachineEpsilon)]
        [InlineData(1.12837917f, 0.428125148f, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f, 0.155943695f, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData(1.44269504f, 0.127751218f, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData(1.57079633f, 0.0f, CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData(2.30258509f, -0.668201510f, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData(2.71828183f, -0.911733918f, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData(3.14159265f, -1.0f, CrossPlatformMachineEpsilon * 10)]  // value:  (pi)
        [InlineData(float.PositiveInfinity, float.NaN, 0.0f)]
        public static void Cos(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Cos(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, 0.0f)]
        [InlineData(-3.14159265f, 11.5919533f, CrossPlatformMachineEpsilon * 100)]    // value:  (pi)
        [InlineData(-2.71828183f, 7.61012514f, CrossPlatformMachineEpsilon * 10)]     // value:  (e)
        [InlineData(-2.30258509f, 5.05f, CrossPlatformMachineEpsilon * 10)]     // value:  (ln(10))
        [InlineData(-1.57079633f, 2.50917848f, CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 2)
        [InlineData(-1.44269504f, 2.23418810f, CrossPlatformMachineEpsilon * 10)]     // value:  (log2(e))
        [InlineData(-1.41421356f, 2.17818356f, CrossPlatformMachineEpsilon * 10)]     // value:  (sqrt(2))
        [InlineData(-1.12837917f, 1.70710014f, CrossPlatformMachineEpsilon * 10)]     // value:  (2 / sqrt(pi))
        [InlineData(-1.0f, 1.54308063f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.785398163f, 1.32460909f, CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 4)
        [InlineData(-0.707106781f, 1.26059184f, CrossPlatformMachineEpsilon * 10)]     // value:  (1 / sqrt(2))
        [InlineData(-0.693147181f, 1.25f, CrossPlatformMachineEpsilon * 10)]     // value:  (ln(2))
        [InlineData(-0.636619772f, 1.20957949f, CrossPlatformMachineEpsilon * 10)]     // value:  (2 / pi)
        [InlineData(-0.434294482f, 1.09579746f, CrossPlatformMachineEpsilon * 10)]     // value:  (log10(e))
        [InlineData(-0.318309886f, 1.05108979f, CrossPlatformMachineEpsilon * 10)]     // value:  (1 / pi)
        [InlineData(-0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.318309886f, 1.05108979f, CrossPlatformMachineEpsilon * 10)]     // value:  (1 / pi)
        [InlineData(0.434294482f, 1.09579746f, CrossPlatformMachineEpsilon * 10)]     // value:  (log10(e))
        [InlineData(0.636619772f, 1.20957949f, CrossPlatformMachineEpsilon * 10)]     // value:  (2 / pi)
        [InlineData(0.693147181f, 1.25f, CrossPlatformMachineEpsilon * 10)]     // value:  (ln(2))
        [InlineData(0.707106781f, 1.26059184f, CrossPlatformMachineEpsilon * 10)]     // value:  (1 / sqrt(2))
        [InlineData(0.785398163f, 1.32460909f, CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 4)
        [InlineData(1.0f, 1.54308063f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.12837917f, 1.70710014f, CrossPlatformMachineEpsilon * 10)]     // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f, 2.17818356f, CrossPlatformMachineEpsilon * 10)]     // value:  (sqrt(2))
        [InlineData(1.44269504f, 2.23418810f, CrossPlatformMachineEpsilon * 10)]     // value:  (log2(e))
        [InlineData(1.57079633f, 2.50917848f, CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 2)
        [InlineData(2.30258509f, 5.05f, CrossPlatformMachineEpsilon * 10)]     // value:  (ln(10))
        [InlineData(2.71828183f, 7.61012514f, CrossPlatformMachineEpsilon * 10)]     // value:  (e)
        [InlineData(3.14159265f, 11.5919533f, CrossPlatformMachineEpsilon * 100)]    // value:  (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Cosh(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Cosh(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, 0.0f, CrossPlatformMachineEpsilon)]
        [InlineData(-3.14159265f, 0.0432139183f, CrossPlatformMachineEpsilon / 10)]     // value: -(pi)
        [InlineData(-2.71828183f, 0.0659880358f, CrossPlatformMachineEpsilon / 10)]     // value: -(e)
        [InlineData(-2.30258509f, 0.1f, CrossPlatformMachineEpsilon)]          // value: -(ln(10))
        [InlineData(-1.57079633f, 0.207879576f, CrossPlatformMachineEpsilon)]          // value: -(pi / 2)
        [InlineData(-1.44269504f, 0.236290088f, CrossPlatformMachineEpsilon)]          // value: -(log2(e))
        [InlineData(-1.41421356f, 0.243116734f, CrossPlatformMachineEpsilon)]          // value: -(sqrt(2))
        [InlineData(-1.12837917f, 0.323557264f, CrossPlatformMachineEpsilon)]          // value: -(2 / sqrt(pi))
        [InlineData(-1.0f, 0.367879441f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f, 0.455938128f, CrossPlatformMachineEpsilon)]          // value: -(pi / 4)
        [InlineData(-0.707106781f, 0.493068691f, CrossPlatformMachineEpsilon)]          // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f, 0.5f, CrossPlatformMachineEpsilon)]          // value: -(ln(2))
        [InlineData(-0.636619772f, 0.529077808f, CrossPlatformMachineEpsilon)]          // value: -(2 / pi)
        [InlineData(-0.434294482f, 0.647721485f, CrossPlatformMachineEpsilon)]          // value: -(log10(e))
        [InlineData(-0.318309886f, 0.727377349f, CrossPlatformMachineEpsilon)]          // value: -(1 / pi)
        [InlineData(-0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.318309886f, 1.37480223f, CrossPlatformMachineEpsilon * 10)]     // value:  (1 / pi)
        [InlineData(0.434294482f, 1.54387344f, CrossPlatformMachineEpsilon * 10)]     // value:  (log10(e))
        [InlineData(0.636619772f, 1.89008116f, CrossPlatformMachineEpsilon * 10)]     // value:  (2 / pi)
        [InlineData(0.693147181f, 2.0f, CrossPlatformMachineEpsilon * 10)]     // value:  (ln(2))
        [InlineData(0.707106781f, 2.02811498f, CrossPlatformMachineEpsilon * 10)]     // value:  (1 / sqrt(2))
        [InlineData(0.785398163f, 2.19328005f, CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 4)
        [InlineData(1.0f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]     //                          expected: (e)
        [InlineData(1.12837917f, 3.09064302f, CrossPlatformMachineEpsilon * 10)]     // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f, 4.11325038f, CrossPlatformMachineEpsilon * 10)]     // value:  (sqrt(2))
        [InlineData(1.44269504f, 4.23208611f, CrossPlatformMachineEpsilon * 10)]     // value:  (log2(e))
        [InlineData(1.57079633f, 4.81047738f, CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 2)
        [InlineData(2.30258509f, 10.0f, CrossPlatformMachineEpsilon * 100)]    // value:  (ln(10))
        [InlineData(2.71828183f, 15.1542622f, CrossPlatformMachineEpsilon * 100)]    // value:  (e)
        [InlineData(3.14159265f, 23.1406926f, CrossPlatformMachineEpsilon * 100)]    // value:  (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Exp(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Exp(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, 0.0f)]
        [InlineData(-3.14159265f, -4.0f, 0.0f)]  // value: -(pi)
        [InlineData(-2.71828183f, -3.0f, 0.0f)]  // value: -(e)
        [InlineData(-2.30258509f, -3.0f, 0.0f)]  // value: -(ln(10))
        [InlineData(-1.57079633f, -2.0f, 0.0f)]  // value: -(pi / 2)
        [InlineData(-1.44269504f, -2.0f, 0.0f)]  // value: -(log2(e))
        [InlineData(-1.41421356f, -2.0f, 0.0f)]  // value: -(sqrt(2))
        [InlineData(-1.12837917f, -2.0f, 0.0f)]  // value: -(2 / sqrt(pi))
        [InlineData(-1.0f, -1.0f, 0.0f)]
        [InlineData(-0.785398163f, -1.0f, 0.0f)]  // value: -(pi / 4)
        [InlineData(-0.707106781f, -1.0f, 0.0f)]  // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f, -1.0f, 0.0f)]  // value: -(ln(2))
        [InlineData(-0.636619772f, -1.0f, 0.0f)]  // value: -(2 / pi)
        [InlineData(-0.434294482f, -1.0f, 0.0f)]  // value: -(log10(e))
        [InlineData(-0.318309886f, -1.0f, 0.0f)]  // value: -(1 / pi)
        [InlineData(-0.0f, -0.0f, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f)]
        [InlineData(0.318309886f, 0.0f, 0.0f)]  // value:  (1 / pi)
        [InlineData(0.434294482f, 0.0f, 0.0f)]  // value:  (log10(e))
        [InlineData(0.636619772f, 0.0f, 0.0f)]  // value:  (2 / pi)
        [InlineData(0.693147181f, 0.0f, 0.0f)]  // value:  (ln(2))
        [InlineData(0.707106781f, 0.0f, 0.0f)]  // value:  (1 / sqrt(2))
        [InlineData(0.785398163f, 0.0f, 0.0f)]  // value:  (pi / 4)
        [InlineData(1.0f, 1.0f, 0.0f)]
        [InlineData(1.12837917f, 1.0f, 0.0f)]  // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f, 1.0f, 0.0f)]  // value:  (sqrt(2))
        [InlineData(1.44269504f, 1.0f, 0.0f)]  // value:  (log2(e))
        [InlineData(1.57079633f, 1.0f, 0.0f)]  // value:  (pi / 2)
        [InlineData(2.30258509f, 2.0f, 0.0f)]  // value:  (ln(10))
        [InlineData(2.71828183f, 2.0f, 0.0f)]  // value:  (e)
        [InlineData(3.14159265f, 3.0f, 0.0f)]  // value:  (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Floor(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Floor(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NaN)]
        [InlineData(float.NegativeInfinity, -0.0f, float.NegativeInfinity, float.NaN)]
        [InlineData(float.NegativeInfinity, -0.0f, -3.14159265f, float.NaN)]
        [InlineData(float.NegativeInfinity, -0.0f, -0.0f, float.NaN)]
        [InlineData(float.NegativeInfinity, -0.0f, float.NaN, float.NaN)]
        [InlineData(float.NegativeInfinity, -0.0f, 0.0f, float.NaN)]
        [InlineData(float.NegativeInfinity, -0.0f, 3.14159265f, float.NaN)]
        [InlineData(float.NegativeInfinity, -0.0f, float.PositiveInfinity, float.NaN)]
        [InlineData(float.NegativeInfinity, 0.0f, float.NegativeInfinity, float.NaN)]
        [InlineData(float.NegativeInfinity, 0.0f, -3.14159265f, float.NaN)]
        [InlineData(float.NegativeInfinity, 0.0f, -0.0f, float.NaN)]
        [InlineData(float.NegativeInfinity, 0.0f, float.NaN, float.NaN)]
        [InlineData(float.NegativeInfinity, 0.0f, 0.0f, float.NaN)]
        [InlineData(float.NegativeInfinity, 0.0f, 3.14159265f, float.NaN)]
        [InlineData(float.NegativeInfinity, 0.0f, float.PositiveInfinity, float.NaN)]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity, float.NaN)]
        [InlineData(-1e38f, 2.0f, 1e38f, -1e38f)]
        [InlineData(-1e38f, 2.0f, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(-5, 4, -3, -23)]
        [InlineData(-0.0f, float.NegativeInfinity, float.NegativeInfinity, float.NaN)]
        [InlineData(-0.0f, float.NegativeInfinity, -3.14159265f, float.NaN)]
        [InlineData(-0.0f, float.NegativeInfinity, -0.0f, float.NaN)]
        [InlineData(-0.0f, float.NegativeInfinity, float.NaN, float.NaN)]
        [InlineData(-0.0f, float.NegativeInfinity, 0.0f, float.NaN)]
        [InlineData(-0.0f, float.NegativeInfinity, 3.14159265f, float.NaN)]
        [InlineData(-0.0f, float.NegativeInfinity, float.PositiveInfinity, float.NaN)]
        [InlineData(-0.0f, float.PositiveInfinity, float.NegativeInfinity, float.NaN)]
        [InlineData(-0.0f, float.PositiveInfinity, -3.14159265f, float.NaN)]
        [InlineData(-0.0f, float.PositiveInfinity, -0.0f, float.NaN)]
        [InlineData(-0.0f, float.PositiveInfinity, float.NaN, float.NaN)]
        [InlineData(-0.0f, float.PositiveInfinity, 0.0f, float.NaN)]
        [InlineData(-0.0f, float.PositiveInfinity, 3.14159265f, float.NaN)]
        [InlineData(-0.0f, float.PositiveInfinity, float.PositiveInfinity, float.NaN)]
        [InlineData(0.0f, float.NegativeInfinity, float.NegativeInfinity, float.NaN)]
        [InlineData(0.0f, float.NegativeInfinity, -3.14159265f, float.NaN)]
        [InlineData(0.0f, float.NegativeInfinity, -0.0f, float.NaN)]
        [InlineData(0.0f, float.NegativeInfinity, float.NaN, float.NaN)]
        [InlineData(0.0f, float.NegativeInfinity, 0.0f, float.NaN)]
        [InlineData(0.0f, float.NegativeInfinity, 3.14159265f, float.NaN)]
        [InlineData(0.0f, float.NegativeInfinity, float.PositiveInfinity, float.NaN)]
        [InlineData(0.0f, float.PositiveInfinity, float.NegativeInfinity, float.NaN)]
        [InlineData(0.0f, float.PositiveInfinity, -3.14159265f, float.NaN)]
        [InlineData(0.0f, float.PositiveInfinity, -0.0f, float.NaN)]
        [InlineData(0.0f, float.PositiveInfinity, float.NaN, float.NaN)]
        [InlineData(0.0f, float.PositiveInfinity, 0.0f, float.NaN)]
        [InlineData(0.0f, float.PositiveInfinity, 3.14159265f, float.NaN)]
        [InlineData(0.0f, float.PositiveInfinity, float.PositiveInfinity, float.NaN)]
        [InlineData(5, 4, 3, 23)]
        [InlineData(1e38f, 2.0f, -1e38f, 1e38f)]
        [InlineData(1e38f, 2.0f, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity, float.NaN)]
        [InlineData(float.PositiveInfinity, -0.0f, float.NegativeInfinity, float.NaN)]
        [InlineData(float.PositiveInfinity, -0.0f, -3.14159265f, float.NaN)]
        [InlineData(float.PositiveInfinity, -0.0f, -0.0f, float.NaN)]
        [InlineData(float.PositiveInfinity, -0.0f, float.NaN, float.NaN)]
        [InlineData(float.PositiveInfinity, -0.0f, 0.0f, float.NaN)]
        [InlineData(float.PositiveInfinity, -0.0f, 3.14159265f, float.NaN)]
        [InlineData(float.PositiveInfinity, -0.0f, float.PositiveInfinity, float.NaN)]
        [InlineData(float.PositiveInfinity, 0.0f, float.NegativeInfinity, float.NaN)]
        [InlineData(float.PositiveInfinity, 0.0f, -3.14159265f, float.NaN)]
        [InlineData(float.PositiveInfinity, 0.0f, -0.0f, float.NaN)]
        [InlineData(float.PositiveInfinity, 0.0f, float.NaN, float.NaN)]
        [InlineData(float.PositiveInfinity, 0.0f, 0.0f, float.NaN)]
        [InlineData(float.PositiveInfinity, 0.0f, 3.14159265f, float.NaN)]
        [InlineData(float.PositiveInfinity, 0.0f, float.PositiveInfinity, float.NaN)]
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NaN)]
        public static void FusedMultiplyAdd(float x, float y, float z, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, MathF.FusedMultiplyAdd(x, y, z), 0.0f);
        }

        [Fact]
        public static void IEEERemainder()
        {
            Assert.Equal(-1.0f, MathF.IEEERemainder(3.0f, 2.0f));
            Assert.Equal(0.0f, MathF.IEEERemainder(4.0f, 2.0f));
            Assert.Equal(1.0f, MathF.IEEERemainder(10.0f, 3.0f));
            Assert.Equal(-1.0f, MathF.IEEERemainder(11.0f, 3.0f));
            Assert.Equal(-2.0f, MathF.IEEERemainder(28.0f, 5.0f));
            AssertExtensions.Equal(1.8f, MathF.IEEERemainder(17.8f, 4.0f), CrossPlatformMachineEpsilon * 10);
            AssertExtensions.Equal(1.4f, MathF.IEEERemainder(17.8f, 4.1f), CrossPlatformMachineEpsilon * 10);
            AssertExtensions.Equal(0.1000004f, MathF.IEEERemainder(-16.3f, 4.1f), CrossPlatformMachineEpsilon / 10);
            AssertExtensions.Equal(1.4f, MathF.IEEERemainder(17.8f, -4.1f), CrossPlatformMachineEpsilon * 10);
            AssertExtensions.Equal(-1.4f, MathF.IEEERemainder(-17.8f, -4.1f), CrossPlatformMachineEpsilon * 10);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, unchecked((int)(0x7FFFFFFF)))]
        [InlineData(-0.0f, unchecked((int)(0x80000000)))]
        [InlineData(float.NaN, unchecked((int)(0x7FFFFFFF)))]
        [InlineData(0.0f, unchecked((int)(0x80000000)))]
        [InlineData(0.113314732f, -4)]
        [InlineData(0.151955223f, -3)]
        [InlineData(0.202699566f, -3)]
        [InlineData(0.336622537f, -2)]
        [InlineData(0.367879441f, -2)]
        [InlineData(0.375214227f, -2)]
        [InlineData(0.457429347f, -2)]
        [InlineData(0.5f, -1)]
        [InlineData(0.580191810f, -1)]
        [InlineData(0.612547327f, -1)]
        [InlineData(0.618503138f, -1)]
        [InlineData(0.643218242f, -1)]
        [InlineData(0.740055574f, -1)]
        [InlineData(0.802008879f, -1)]
        [InlineData(1.0f, 0)]
        [InlineData(1.24686899f, 0)]
        [InlineData(1.35124987f, 0)]
        [InlineData(1.55468228f, 0)]
        [InlineData(1.61680667f, 0)]
        [InlineData(1.63252692f, 0)]
        [InlineData(1.72356793f, 0)]
        [InlineData(2.0f, 1)]
        [InlineData(2.18612996f, 1)]
        [InlineData(2.66514414f, 1)]
        [InlineData(2.71828183f, 1)]
        [InlineData(2.97068642f, 1)]
        [InlineData(4.93340967f, 2)]
        [InlineData(6.58088599f, 2)]
        [InlineData(8.82497783f, 3)]
        [InlineData(float.PositiveInfinity, unchecked((int)(0x7FFFFFFF)))]
        [InlineData(-8.066849f, 3)]
        [InlineData(4.345240f, 2)]
        [InlineData(-8.381433f, 3)]
        [InlineData(-6.531673f, 2)]
        [InlineData(9.267057f, 3)]
        [InlineData(0.661986f, -1)]
        [InlineData(-0.406604f, -2)]
        [InlineData(0.561760f, -1)]
        [InlineData(0.774152f, -1)]
        [InlineData(-0.678764f, -1)]
        public static void ILogB(float value, int expectedResult)
        {
            Assert.Equal(expectedResult, MathF.ILogB(value));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-3.14159265f, float.NaN, 0.0f)]                               //                               value: -(pi)
        [InlineData(-2.71828183f, float.NaN, 0.0f)]                               //                               value: -(e)
        [InlineData(-1.41421356f, float.NaN, 0.0f)]                               //                               value: -(sqrt(2))
        [InlineData(-1.0f, float.NaN, 0.0f)]
        [InlineData(-0.693147181f, float.NaN, 0.0f)]                               //                               value: -(ln(2))
        [InlineData(-0.434294482f, float.NaN, 0.0f)]                               //                               value: -(log10(e))
        [InlineData(-0.0f, float.NegativeInfinity, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, float.NegativeInfinity, 0.0f)]
        [InlineData(0.0432139183f, -3.14159265f, CrossPlatformMachineEpsilon * 10)]    // expected: -(pi)
        [InlineData(0.0659880358f, -2.71828183f, CrossPlatformMachineEpsilon * 10)]    // expected: -(e)
        [InlineData(0.1f, -2.30258509f, CrossPlatformMachineEpsilon * 10)]    // expected: -(ln(10))
        [InlineData(0.207879576f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]    // expected: -(pi / 2)
        [InlineData(0.236290088f, -1.44269504f, CrossPlatformMachineEpsilon * 10)]    // expected: -(log2(e))
        [InlineData(0.243116734f, -1.41421356f, CrossPlatformMachineEpsilon * 10)]    // expected: -(sqrt(2))
        [InlineData(0.323557264f, -1.12837917f, CrossPlatformMachineEpsilon * 10)]    // expected: -(2 / sqrt(pi))
        [InlineData(0.367879441f, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.455938128f, -0.785398163f, CrossPlatformMachineEpsilon)]         // expected: -(pi / 4)
        [InlineData(0.493068691f, -0.707106781f, CrossPlatformMachineEpsilon)]         // expected: -(1 / sqrt(2))
        [InlineData(0.5f, -0.693147181f, CrossPlatformMachineEpsilon)]         // expected: -(ln(2))
        [InlineData(0.529077808f, -0.636619772f, CrossPlatformMachineEpsilon)]         // expected: -(2 / pi)
        [InlineData(0.647721485f, -0.434294482f, CrossPlatformMachineEpsilon)]         // expected: -(log10(e))
        [InlineData(0.727377349f, -0.318309886f, CrossPlatformMachineEpsilon)]         // expected: -(1 / pi)
        [InlineData(1.0f, 0.0f, 0.0f)]
        [InlineData(1.37480223f, 0.318309886f, CrossPlatformMachineEpsilon)]         // expected:  (1 / pi)
        [InlineData(1.54387344f, 0.434294482f, CrossPlatformMachineEpsilon)]         // expected:  (log10(e))
        [InlineData(1.89008116f, 0.636619772f, CrossPlatformMachineEpsilon)]         // expected:  (2 / pi)
        [InlineData(2.0f, 0.693147181f, CrossPlatformMachineEpsilon)]         // expected:  (ln(2))
        [InlineData(2.02811498f, 0.707106781f, CrossPlatformMachineEpsilon)]         // expected:  (1 / sqrt(2))
        [InlineData(2.19328005f, 0.785398163f, CrossPlatformMachineEpsilon)]         // expected:  (pi / 4)
        [InlineData(2.71828183f, 1.0f, CrossPlatformMachineEpsilon * 10)]    //                              value: (e)
        [InlineData(3.09064302f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]    // expected:  (2 / sqrt(pi))
        [InlineData(4.11325038f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]    // expected:  (sqrt(2))
        [InlineData(4.23208611f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]    // expected:  (log2(e))
        [InlineData(4.81047738f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]    // expected:  (pi / 2)
        [InlineData(10.0f, 2.30258509f, CrossPlatformMachineEpsilon * 10)]    // expected:  (ln(10))
        [InlineData(15.1542622f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]    // expected:  (e)
        [InlineData(23.1406926f, 3.14159265f, CrossPlatformMachineEpsilon * 10)]    // expected:  (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Log(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Log(value), allowedVariance);
        }

        [Fact]
        public static void LogWithBase()
        {
            Assert.Equal(1.0f, MathF.Log(3.0f, 3.0f));
            AssertExtensions.Equal(2.40217350f, MathF.Log(14.0f, 3.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(float.NegativeInfinity, MathF.Log(0.0f, 3.0f));
            Assert.Equal(float.NaN, MathF.Log(-3.0f, 3.0f));
            Assert.Equal(float.NaN, MathF.Log(float.NaN, 3.0f));
            Assert.Equal(float.PositiveInfinity, MathF.Log(float.PositiveInfinity, 3.0f));
            Assert.Equal(float.NaN, MathF.Log(float.NegativeInfinity, 3.0f));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-0.113314732f, float.NaN, 0.0f)]
        [InlineData(-0.0f, float.NegativeInfinity, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, float.NegativeInfinity, 0.0f)]
        [InlineData(0.113314732f, -3.14159265f, CrossPlatformMachineEpsilon * 10)]    // expected: -(pi)
        [InlineData(0.151955223f, -2.71828183f, CrossPlatformMachineEpsilon * 10)]    // expected: -(e)
        [InlineData(0.202699566f, -2.30258509f, CrossPlatformMachineEpsilon * 10)]    // expected: -(ln(10))
        [InlineData(0.336622537f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]    // expected: -(pi / 2)
        [InlineData(0.367879441f, -1.44269504f, CrossPlatformMachineEpsilon * 10)]    // expected: -(log2(e))
        [InlineData(0.375214227f, -1.41421356f, CrossPlatformMachineEpsilon * 10)]    // expected: -(sqrt(2))
        [InlineData(0.457429347f, -1.12837917f, CrossPlatformMachineEpsilon * 10)]    // expected: -(2 / sqrt(pi))
        [InlineData(0.5f, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.580191810f, -0.785398163f, CrossPlatformMachineEpsilon)]         // expected: -(pi / 4)
        [InlineData(0.612547327f, -0.707106781f, CrossPlatformMachineEpsilon)]         // expected: -(1 / sqrt(2))
        [InlineData(0.618503138f, -0.693147181f, CrossPlatformMachineEpsilon)]         // expected: -(ln(2))
        [InlineData(0.643218242f, -0.636619772f, CrossPlatformMachineEpsilon)]         // expected: -(2 / pi)
        [InlineData(0.740055574f, -0.434294482f, CrossPlatformMachineEpsilon)]         // expected: -(log10(e))
        [InlineData(0.802008879f, -0.318309886f, CrossPlatformMachineEpsilon)]         // expected: -(1 / pi)
        [InlineData(1, 0.0f, 0.0f)]
        [InlineData(1.24686899f, 0.318309886f, CrossPlatformMachineEpsilon)]         // expected:  (1 / pi)
        [InlineData(1.35124987f, 0.434294482f, CrossPlatformMachineEpsilon)]         // expected:  (log10(e))
        [InlineData(1.55468228f, 0.636619772f, CrossPlatformMachineEpsilon)]         // expected:  (2 / pi)
        [InlineData(1.61680667f, 0.693147181f, CrossPlatformMachineEpsilon)]         // expected:  (ln(2))
        [InlineData(1.63252692f, 0.707106781f, CrossPlatformMachineEpsilon)]         // expected:  (1 / sqrt(2))
        [InlineData(1.72356793f, 0.785398163f, CrossPlatformMachineEpsilon)]         // expected:  (pi / 4)
        [InlineData(2, 1.0f, CrossPlatformMachineEpsilon * 10)]    //                              value: (e)
        [InlineData(2.18612996f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]    // expected:  (2 / sqrt(pi))
        [InlineData(2.66514414f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]    // expected:  (sqrt(2))
        [InlineData(2.71828183f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]    // expected:  (log2(e))
        [InlineData(2.97068642f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]    // expected:  (pi / 2)
        [InlineData(4.93340967f, 2.30258509f, CrossPlatformMachineEpsilon * 10)]    // expected:  (ln(10))
        [InlineData(6.58088599f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]    // expected:  (e)
        [InlineData(8.82497783f, 3.14159265f, CrossPlatformMachineEpsilon * 10)]    // expected:  (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Log2(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Log2(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-3.14159265f, float.NaN, 0.0f)]                                //                              value: -(pi)
        [InlineData(-2.71828183f, float.NaN, 0.0f)]                                //                              value: -(e)
        [InlineData(-1.41421356f, float.NaN, 0.0f)]                                //                              value: -(sqrt(2))
        [InlineData(-1.0f, float.NaN, 0.0f)]
        [InlineData(-0.693147181f, float.NaN, 0.0f)]                                //                              value: -(ln(2))
        [InlineData(-0.434294482f, float.NaN, 0.0f)]                                //                              value: -(log10(e))
        [InlineData(-0.0f, float.NegativeInfinity, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, float.NegativeInfinity, 0.0f)]
        [InlineData(0.000721784159f, -3.14159265f, CrossPlatformMachineEpsilon * 10)]    // expected: -(pi)
        [InlineData(0.00191301410f, -2.71828183f, CrossPlatformMachineEpsilon * 10)]    // expected: -(e)
        [InlineData(0.00498212830f, -2.30258509f, CrossPlatformMachineEpsilon * 10)]    // expected: -(ln(10))
        [InlineData(0.0268660410f, -1.57079633f, CrossPlatformMachineEpsilon * 10)]    // expected: -(pi / 2)
        [InlineData(0.0360831928f, -1.44269504f, CrossPlatformMachineEpsilon * 10)]    // expected: -(log2(e))
        [InlineData(0.0385288847f, -1.41421356f, CrossPlatformMachineEpsilon * 10)]    // expected: -(sqrt(2))
        [InlineData(0.0744082059f, -1.12837917f, CrossPlatformMachineEpsilon * 10)]    // expected: -(2 / sqrt(pi))
        [InlineData(0.1f, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.163908636f, -0.785398163f, CrossPlatformMachineEpsilon)]         // expected: -(pi / 4)
        [InlineData(0.196287760f, -0.707106781f, CrossPlatformMachineEpsilon)]         // expected: -(1 / sqrt(2))
        [InlineData(0.202699566f, -0.693147181f, CrossPlatformMachineEpsilon)]         // expected: -(ln(2))
        [InlineData(0.230876765f, -0.636619772f, CrossPlatformMachineEpsilon)]         // expected: -(2 / pi)
        [InlineData(0.367879441f, -0.434294482f, CrossPlatformMachineEpsilon)]         // expected: -(log10(e))
        [InlineData(0.480496373f, -0.318309886f, CrossPlatformMachineEpsilon)]         // expected: -(1 / pi)
        [InlineData(1.0f, 0.0f, 0.0f)]
        [InlineData(2.08118116f, 0.318309886f, CrossPlatformMachineEpsilon)]         // expected:  (1 / pi)
        [InlineData(2.71828183f, 0.434294482f, CrossPlatformMachineEpsilon)]         // expected:  (log10(e))        value: (e)
        [InlineData(4.33131503f, 0.636619772f, CrossPlatformMachineEpsilon)]         // expected:  (2 / pi)
        [InlineData(4.93340967f, 0.693147181f, CrossPlatformMachineEpsilon)]         // expected:  (ln(2))
        [InlineData(5.09456117f, 0.707106781f, CrossPlatformMachineEpsilon)]         // expected:  (1 / sqrt(2))
        [InlineData(6.10095980f, 0.785398163f, CrossPlatformMachineEpsilon)]         // expected:  (pi / 4)
        [InlineData(10.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(13.4393779f, 1.12837917f, CrossPlatformMachineEpsilon * 10)]    // expected:  (2 / sqrt(pi))
        [InlineData(25.9545535f, 1.41421356f, CrossPlatformMachineEpsilon * 10)]    // expected:  (sqrt(2))
        [InlineData(27.7137338f, 1.44269504f, CrossPlatformMachineEpsilon * 10)]    // expected:  (log2(e))
        [InlineData(37.2217105f, 1.57079633f, CrossPlatformMachineEpsilon * 10)]    // expected:  (pi / 2)
        [InlineData(200.717432f, 2.30258509f, CrossPlatformMachineEpsilon * 10)]    // expected:  (ln(10))
        [InlineData(522.735300f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]    // expected:  (e)
        [InlineData(1385.45573f, 3.14159265f, CrossPlatformMachineEpsilon * 10)]    // expected:  (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Log10(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Log10(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MaxValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MaxValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, float.NaN)]
        [InlineData(1.0f, float.NaN, float.NaN)]
        [InlineData(float.PositiveInfinity, float.NaN, float.NaN)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NaN)]
        [InlineData(float.NaN, float.PositiveInfinity, float.NaN)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NaN)]
        [InlineData(-0.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, -0.0f, 0.0f)]
        [InlineData(2.0f, -3.0f, 2.0f)]
        [InlineData(-3.0f, 2.0f, 2.0f)]
        [InlineData(3.0f, -2.0f, 3.0f)]
        [InlineData(-2.0f, 3.0f, 3.0f)]
        public static void Max(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, MathF.Max(x, y), 0.0f);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MaxValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MaxValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, float.NaN)]
        [InlineData(1.0f, float.NaN, float.NaN)]
        [InlineData(float.PositiveInfinity, float.NaN, float.NaN)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NaN)]
        [InlineData(float.NaN, float.PositiveInfinity, float.NaN)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NaN)]
        [InlineData(-0.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, -0.0f, 0.0f)]
        [InlineData(2.0f, -3.0f, -3.0f)]
        [InlineData(-3.0f, 2.0f, -3.0f)]
        [InlineData(3.0f, -2.0f, 3.0f)]
        [InlineData(-2.0f, 3.0f, 3.0f)]
        public static void MaxMagnitude(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, MathF.MaxMagnitude(x, y), 0.0f);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, float.NaN)]
        [InlineData(1.0f, float.NaN, float.NaN)]
        [InlineData(float.PositiveInfinity, float.NaN, float.NaN)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NaN)]
        [InlineData(float.NaN, float.PositiveInfinity, float.NaN)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NaN)]
        [InlineData(-0.0f, 0.0f, -0.0f)]
        [InlineData(0.0f, -0.0f, -0.0f)]
        [InlineData(2.0f, -3.0f, -3.0f)]
        [InlineData(-3.0f, 2.0f, -3.0f)]
        [InlineData(3.0f, -2.0f, -2.0f)]
        [InlineData(-2.0f, 3.0f, -2.0f)]
        public static void Min(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, MathF.Min(x, y), 0.0f);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.MinValue, float.MaxValue, float.MinValue)]
        [InlineData(float.MaxValue, float.MinValue, float.MinValue)]
        [InlineData(float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1.0f, float.NaN)]
        [InlineData(1.0f, float.NaN, float.NaN)]
        [InlineData(float.PositiveInfinity, float.NaN, float.NaN)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NaN)]
        [InlineData(float.NaN, float.PositiveInfinity, float.NaN)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NaN)]
        [InlineData(-0.0f, 0.0f, -0.0f)]
        [InlineData(0.0f, -0.0f, -0.0f)]
        [InlineData(2.0f, -3.0f, 2.0f)]
        [InlineData(-3.0f, 2.0f, 2.0f)]
        [InlineData(3.0f, -2.0f, -2.0f)]
        [InlineData(-2.0f, 3.0f, -2.0f)]
        public static void MinMagnitude(float x, float y, float expectedResult)
        {
            AssertExtensions.Equal(expectedResult, MathF.MinMagnitude(x, y), 0.0f);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, 0.0f, 0.0f)]
        [InlineData(float.NegativeInfinity, -1.0f, -0.0f, 0.0f)]
        [InlineData(float.NegativeInfinity, -0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(float.NegativeInfinity, float.NaN, float.NaN, 0.0f)]
        [InlineData(float.NegativeInfinity, 0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(float.NegativeInfinity, 1.0f, float.NegativeInfinity, 0.0f)]
        [InlineData(float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        [InlineData(-10.0f, float.NegativeInfinity, 0.0f, 0.0f)]
        [InlineData(-10.0f, -1.57079633f, float.NaN, 0.0f)]                                   //          y: -(pi / 2)
        [InlineData(-10.0f, -1.0f, -0.1f, CrossPlatformMachineEpsilon)]
        [InlineData(-10.0f, -0.785398163f, float.NaN, 0.0f)]                                   //          y: -(pi / 4)
        [InlineData(-10.0f, -0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-10.0f, float.NaN, float.NaN, 0.0f)]
        [InlineData(-10.0f, 0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-10.0f, 0.785398163f, float.NaN, 0.0f)]                                   //          y:  (pi / 4)
        [InlineData(-10.0f, 1.0f, -10.0f, CrossPlatformMachineEpsilon * 100)]
        [InlineData(-10.0f, 1.57079633f, float.NaN, 0.0f)]                                   //          y:  (pi / 2)
        [InlineData(-10.0f, float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        [InlineData(-2.71828183f, float.NegativeInfinity, 0.0f, 0.0f)]                                   // x: -(e)
        [InlineData(-2.71828183f, -1.57079633f, float.NaN, 0.0f)]                                   // x: -(e)  y: -(pi / 2)
        [InlineData(-2.71828183f, -1.0f, -0.367879441f, CrossPlatformMachineEpsilon)]            // x: -(e)
        [InlineData(-2.71828183f, -0.785398163f, float.NaN, 0.0f)]                                   // x: -(e)  y: -(pi / 4)
        [InlineData(-2.71828183f, -0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]       // x: -(e)
        [InlineData(-2.71828183f, float.NaN, float.NaN, 0.0f)]
        [InlineData(-2.71828183f, 0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]       // x: -(e)
        [InlineData(-2.71828183f, 0.785398163f, float.NaN, 0.0f)]                                   // x: -(e)  y:  (pi / 4)
        [InlineData(-2.71828183f, 1.0f, -2.71828183f, CrossPlatformMachineEpsilon * 10)]       // x: -(e)  expected: (e)
        [InlineData(-2.71828183f, 1.57079633f, float.NaN, 0.0f)]                                   // x: -(e)  y:  (pi / 2)
        [InlineData(-2.71828183f, float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        [InlineData(-1.0f, -1.0f, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0f, -0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0f, float.NaN, float.NaN, 0.0f)]
        [InlineData(-1.0f, 0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0f, 1.0f, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.0f, float.NegativeInfinity, float.PositiveInfinity, 0.0f)]
        [InlineData(-0.0f, -3.0f, float.NegativeInfinity, 0.0f)]
        [InlineData(-0.0f, -2.0f, float.PositiveInfinity, 0.0f)]
        [InlineData(-0.0f, -1.57079633f, float.PositiveInfinity, 0.0f)]                                   //          y: -(pi / 2)
        [InlineData(-0.0f, -1.0f, float.NegativeInfinity, 0.0f)]
        [InlineData(-0.0f, -0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.0f, float.NaN, float.NaN, 0.0f)]
        [InlineData(-0.0f, 0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.0f, 1.0f, -0.0f, 0.0f)]
        [InlineData(-0.0f, 1.57079633f, 0.0f, 0.0f)]                                   //          y: -(pi / 2)
        [InlineData(-0.0f, 2.0f, 0.0f, 0.0f)]
        [InlineData(-0.0f, 3.0f, -0.0f, 0.0f)]
        [InlineData(-0.0f, float.PositiveInfinity, 0.0f, 0.0f)]
        [InlineData(float.NaN, float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(float.NaN, -1.0f, float.NaN, 0.0f)]
        [InlineData(float.NaN, float.NaN, float.NaN, 0.0f)]
        [InlineData(float.NaN, 1.0f, float.NaN, 0.0f)]
        [InlineData(float.NaN, float.PositiveInfinity, float.NaN, 0.0f)]
        [InlineData(0.0f, float.NegativeInfinity, float.PositiveInfinity, 0.0f)]
        [InlineData(0.0f, -3.0f, float.PositiveInfinity, 0.0f)]
        [InlineData(0.0f, -2.0f, float.PositiveInfinity, 0.0f)]
        [InlineData(0.0f, -1.57079633f, float.PositiveInfinity, 0.0f)]                                   //          y: -(pi / 2)
        [InlineData(0.0f, -1.0f, float.PositiveInfinity, 0.0f)]
        [InlineData(0.0f, -0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.0f, float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.0f, 1.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, 1.57079633f, 0.0f, 0.0f)]                                   //          y: -(pi / 2)
        [InlineData(0.0f, 2.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, 3.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, float.PositiveInfinity, 0.0f, 0.0f)]
        [InlineData(1.0f, float.NegativeInfinity, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.0f, -1.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.0f, -0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.0f, 0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.0f, 1.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.0f, float.PositiveInfinity, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(2.71828183f, float.NegativeInfinity, 0.0f, 0.0f)]
        [InlineData(2.71828183f, -3.14159265f, 0.0432139183f, CrossPlatformMachineEpsilon / 10)]       // x:  (e)  y: -(pi)
        [InlineData(2.71828183f, -2.71828183f, 0.0659880358f, CrossPlatformMachineEpsilon / 10)]       // x:  (e)  y: -(e)
        [InlineData(2.71828183f, -2.30258509f, 0.1f, CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(ln(10))
        [InlineData(2.71828183f, -1.57079633f, 0.207879576f, CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(pi / 2)
        [InlineData(2.71828183f, -1.44269504f, 0.236290088f, CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(log2(e))
        [InlineData(2.71828183f, -1.41421356f, 0.243116734f, CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(sqrt(2))
        [InlineData(2.71828183f, -1.12837917f, 0.323557264f, CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(2 / sqrt(pi))
        [InlineData(2.71828183f, -1.0f, 0.367879441f, CrossPlatformMachineEpsilon)]            // x:  (e)
        [InlineData(2.71828183f, -0.785398163f, 0.455938128f, CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(pi / 4)
        [InlineData(2.71828183f, -0.707106781f, 0.493068691f, CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(1 / sqrt(2))
        [InlineData(2.71828183f, -0.693147181f, 0.5f, CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(ln(2))
        [InlineData(2.71828183f, -0.636619772f, 0.529077808f, CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(2 / pi)
        [InlineData(2.71828183f, -0.434294482f, 0.647721485f, CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(log10(e))
        [InlineData(2.71828183f, -0.318309886f, 0.727377349f, CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(1 / pi)
        [InlineData(2.71828183f, -0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)
        [InlineData(2.71828183f, float.NaN, float.NaN, 0.0f)]
        [InlineData(2.71828183f, 0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)
        [InlineData(2.71828183f, 0.318309886f, 1.37480223f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (1 / pi)
        [InlineData(2.71828183f, 0.434294482f, 1.54387344f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (log10(e))
        [InlineData(2.71828183f, 0.636619772f, 1.89008116f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (2 / pi)
        [InlineData(2.71828183f, 0.693147181f, 2.0f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (ln(2))
        [InlineData(2.71828183f, 0.707106781f, 2.02811498f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (1 / sqrt(2))
        [InlineData(2.71828183f, 0.785398163f, 2.19328005f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (pi / 4)
        [InlineData(2.71828183f, 1.0f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)                      expected: (e)
        [InlineData(2.71828183f, 1.12837917f, 3.09064302f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (2 / sqrt(pi))
        [InlineData(2.71828183f, 1.41421356f, 4.11325038f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (sqrt(2))
        [InlineData(2.71828183f, 1.44269504f, 4.23208611f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (log2(e))
        [InlineData(2.71828183f, 1.57079633f, 4.81047738f, CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (pi / 2)
        [InlineData(2.71828183f, 2.30258509f, 10.0f, CrossPlatformMachineEpsilon * 100)]      // x:  (e)  y:  (ln(10))
        [InlineData(2.71828183f, 2.71828183f, 15.1542622f, CrossPlatformMachineEpsilon * 100)]      // x:  (e)  y:  (e)
        [InlineData(2.71828183f, 3.14159265f, 23.1406926f, CrossPlatformMachineEpsilon * 100)]      // x:  (e)  y:  (pi)
        [InlineData(2.71828183f, float.PositiveInfinity, float.PositiveInfinity, 0.0f)]                                   // x:  (e)
        [InlineData(10.0f, float.NegativeInfinity, 0.0f, 0.0f)]
        [InlineData(10.0f, -3.14159265f, 0.000721784159f, CrossPlatformMachineEpsilon / 1000)]     //          y: -(pi)
        [InlineData(10.0f, -2.71828183f, 0.00191301410f, CrossPlatformMachineEpsilon / 100)]      //          y: -(e)
        [InlineData(10.0f, -2.30258509f, 0.00498212830f, CrossPlatformMachineEpsilon / 100)]      //          y: -(ln(10))
        [InlineData(10.0f, -1.57079633f, 0.0268660410f, CrossPlatformMachineEpsilon / 10)]       //          y: -(pi / 2)
        [InlineData(10.0f, -1.44269504f, 0.0360831928f, CrossPlatformMachineEpsilon / 10)]       //          y: -(log2(e))
        [InlineData(10.0f, -1.41421356f, 0.0385288847f, CrossPlatformMachineEpsilon / 10)]       //          y: -(sqrt(2))
        [InlineData(10.0f, -1.12837917f, 0.0744082059f, CrossPlatformMachineEpsilon / 10)]       //          y: -(2 / sqrt(pi))
        [InlineData(10.0f, -1.0f, 0.1f, CrossPlatformMachineEpsilon)]
        [InlineData(10.0f, -0.785398163f, 0.163908636f, CrossPlatformMachineEpsilon)]            //          y: -(pi / 4)
        [InlineData(10.0f, -0.707106781f, 0.196287760f, CrossPlatformMachineEpsilon)]            //          y: -(1 / sqrt(2))
        [InlineData(10.0f, -0.693147181f, 0.202699566f, CrossPlatformMachineEpsilon)]            //          y: -(ln(2))
        [InlineData(10.0f, -0.636619772f, 0.230876765f, CrossPlatformMachineEpsilon)]            //          y: -(2 / pi)
        [InlineData(10.0f, -0.434294482f, 0.367879441f, CrossPlatformMachineEpsilon)]            //          y: -(log10(e))
        [InlineData(10.0f, -0.318309886f, 0.480496373f, CrossPlatformMachineEpsilon)]            //          y: -(1 / pi)
        [InlineData(10.0f, -0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(10.0f, float.NaN, float.NaN, 0.0f)]
        [InlineData(10.0f, 0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(10.0f, 0.318309886f, 2.08118116f, CrossPlatformMachineEpsilon * 10)]       //          y:  (1 / pi)
        [InlineData(10.0f, 0.434294482f, 2.71828183f, CrossPlatformMachineEpsilon * 10)]       //          y:  (log10(e))      expected: (e)
        [InlineData(10.0f, 0.636619772f, 4.33131503f, CrossPlatformMachineEpsilon * 10)]       //          y:  (2 / pi)
        [InlineData(10.0f, 0.693147181f, 4.93340967f, CrossPlatformMachineEpsilon * 10)]       //          y:  (ln(2))
        [InlineData(10.0f, 0.707106781f, 5.09456117f, CrossPlatformMachineEpsilon * 10)]       //          y:  (1 / sqrt(2))
        [InlineData(10.0f, 0.785398163f, 6.10095980f, CrossPlatformMachineEpsilon * 10)]       //          y:  (pi / 4)
        [InlineData(10.0f, 1.0f, 10.0f, CrossPlatformMachineEpsilon * 100)]
        [InlineData(10.0f, 1.12837917f, 13.4393779f, CrossPlatformMachineEpsilon * 100)]      //          y:  (2 / sqrt(pi))
        [InlineData(10.0f, 1.41421356f, 25.9545535f, CrossPlatformMachineEpsilon * 100)]      //          y:  (sqrt(2))
        [InlineData(10.0f, 1.44269504f, 27.7137338f, CrossPlatformMachineEpsilon * 100)]      //          y:  (log2(e))
        [InlineData(10.0f, 1.57079633f, 37.2217105f, CrossPlatformMachineEpsilon * 100)]      //          y:  (pi / 2)
        [InlineData(10.0f, 2.30258509f, 200.717432f, CrossPlatformMachineEpsilon * 1000)]     //          y:  (ln(10))
        [InlineData(10.0f, 2.71828183f, 522.735300f, CrossPlatformMachineEpsilon * 1000)]     //          y:  (e)
        [InlineData(10.0f, 3.14159265f, 1385.45573f, CrossPlatformMachineEpsilon * 10000)]    //          y:  (pi)
        [InlineData(10.0f, float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, 0.0f, 0.0f)]
        [InlineData(float.PositiveInfinity, -1.0f, 0.0f, 0.0f)]
        [InlineData(float.PositiveInfinity, -0.0f, 1.0f, 0.0f)]
        [InlineData(float.PositiveInfinity, float.NaN, float.NaN, 0.0f)]
        [InlineData(float.PositiveInfinity, 0.0f, 1.0f, 0.0f)]
        [InlineData(float.PositiveInfinity, 1.0f, float.PositiveInfinity, 0.0f)]
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Pow(float x, float y, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Pow(x, y), allowedVariance);
        }

        [Theory]
        [InlineData(-1.0f, float.NegativeInfinity, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0f, float.PositiveInfinity, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(float.NaN, -0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(float.NaN, 0.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.0f, float.NaN, 1.0f, CrossPlatformMachineEpsilon * 10)]
        public static void Pow_IEEE(float x, float y, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Pow(x, y), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, -0.0f,                   0.0f)]
        [InlineData(-3.14159265f,            -0.318309873f,           CrossPlatformMachineEpsilonForEstimates)] // value: (pi)
        [InlineData(-2.71828183f,            -0.36787945f,            CrossPlatformMachineEpsilonForEstimates)] // value: (e)
        [InlineData(-2.30258509f,            -0.434294462f,           CrossPlatformMachineEpsilonForEstimates)] // value: (ln(10))
        [InlineData(-1.57079633f,            -0.636619747f,           CrossPlatformMachineEpsilonForEstimates)] // value: (pi / 2)
        [InlineData(-1.44269504f,            -0.693147182f,           CrossPlatformMachineEpsilonForEstimates)] // value: (log2(e))
        [InlineData(-1.41421356f,            -0.707106769f,           CrossPlatformMachineEpsilonForEstimates)] // value: (sqrt(2))
        [InlineData(-1.12837917f,            -0.886226892f,           CrossPlatformMachineEpsilonForEstimates)] // value: (2 / sqrt(pi))
        [InlineData(-1.0f,                   -1.0f,                   CrossPlatformMachineEpsilonForEstimates)]
        [InlineData(-0.785398163f,           -1.27323949f,            CrossPlatformMachineEpsilonForEstimates)] // value: (pi / 4)
        [InlineData(-0.707106781f,           -1.41421354f,            CrossPlatformMachineEpsilonForEstimates)] // value: (1 / sqrt(2))
        [InlineData(-0.693147181f,           -1.44269502f,            CrossPlatformMachineEpsilonForEstimates)] // value: (ln(2))
        [InlineData(-0.636619772f,           -1.57079637f,            CrossPlatformMachineEpsilonForEstimates)] // value: (2 / pi)
        [InlineData(-0.434294482f,           -2.30258512f,            CrossPlatformMachineEpsilonForEstimates)] // value: (log10(e))
        [InlineData(-0.318309886f,           -3.14159274f,            CrossPlatformMachineEpsilonForEstimates)] // value: (1 / pi)
        [InlineData(-0.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    float.PositiveInfinity, 0.0f)]
        [InlineData( 0.318309886f,            3.14159274f,            CrossPlatformMachineEpsilonForEstimates)] // value: (1 / pi)
        [InlineData( 0.434294482f,            2.30258512f,            CrossPlatformMachineEpsilonForEstimates)] // value: (log10(e))
        [InlineData( 0.636619772f,            1.57079637f,            CrossPlatformMachineEpsilonForEstimates)] // value: (2 / pi)
        [InlineData( 0.693147181f,            1.44269502f,            CrossPlatformMachineEpsilonForEstimates)] // value: (ln(2))
        [InlineData( 0.707106781f,            1.41421354f,            CrossPlatformMachineEpsilonForEstimates)] // value: (1 / sqrt(2))
        [InlineData( 0.785398163f,            1.27323949f,            CrossPlatformMachineEpsilonForEstimates)] // value: (pi / 4)
        [InlineData( 1.0f,                    1.0f,                   CrossPlatformMachineEpsilonForEstimates)]
        [InlineData( 1.12837917f,             0.886226892f,           CrossPlatformMachineEpsilonForEstimates)] // value: (2 / sqrt(pi))
        [InlineData( 1.41421356f,             0.707106769f,           CrossPlatformMachineEpsilonForEstimates)] // value: (sqrt(2))
        [InlineData( 1.44269504f,             0.693147182f,           CrossPlatformMachineEpsilonForEstimates)] // value: (log2(e))
        [InlineData( 1.57079633f,             0.636619747f,           CrossPlatformMachineEpsilonForEstimates)] // value: (pi / 2)
        [InlineData( 2.30258509f,             0.434294462f,           CrossPlatformMachineEpsilonForEstimates)] // value: (ln(10))
        [InlineData( 2.71828183f,             0.36787945f,            CrossPlatformMachineEpsilonForEstimates)] // value: (e)
        [InlineData( 3.14159265f,             0.318309873f,           CrossPlatformMachineEpsilonForEstimates)] // value: (pi)
        [InlineData( float.PositiveInfinity,  0.0f,                   0.0f)]
        public static void ReciprocalEstimate(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.ReciprocalEstimate(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData(-3.14159265f,             float.NaN,              0.0f)]                                    // value: (pi)
        [InlineData(-2.71828183f,             float.NaN,              0.0f)]                                    // value: (e)
        [InlineData(-2.30258509f,             float.NaN,              0.0f)]                                    // value: (ln(10))
        [InlineData(-1.57079633f,             float.NaN,              0.0f)]                                    // value: (pi / 2)
        [InlineData(-1.44269504f,             float.NaN,              0.0f)]                                    // value: (log2(e))
        [InlineData(-1.41421356f,             float.NaN,              0.0f)]                                    // value: (sqrt(2))
        [InlineData(-1.12837917f,             float.NaN,              0.0f)]                                    // value: (2 / sqrt(pi))
        [InlineData(-1.0f,                    float.NaN,              0.0f)]
        [InlineData(-0.785398163f,            float.NaN,              0.0f)]                                    // value: (pi / 4)
        [InlineData(-0.707106781f,            float.NaN,              0.0f)]                                    // value: (1 / sqrt(2))
        [InlineData(-0.693147181f,            float.NaN,              0.0f)]                                    // value: (ln(2))
        [InlineData(-0.636619772f,            float.NaN,              0.0f)]                                    // value: (2 / pi)
        [InlineData(-0.434294482f,            float.NaN,              0.0f)]                                    // value: (log10(e))
        [InlineData(-0.318309886f,            float.NaN,              0.0f)]                                    // value: (1 / pi)
        [InlineData(-0.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    float.PositiveInfinity, 0.0f)]
        [InlineData( 0.318309886f,            1.7724539f,             CrossPlatformMachineEpsilonForEstimates)] // value: (1 / pi)
        [InlineData( 0.434294482f,            1.51742709f,            CrossPlatformMachineEpsilonForEstimates)] // value: (log10(e))
        [InlineData( 0.636619772f,            1.25331414f,            CrossPlatformMachineEpsilonForEstimates)] // value: (2 / pi)
        [InlineData( 0.693147181f,            1.2011224f,             CrossPlatformMachineEpsilonForEstimates)] // value: (ln(2))
        [InlineData( 0.707106781f,            1.18920708f,            CrossPlatformMachineEpsilonForEstimates)] // value: (1 / sqrt(2))
        [InlineData( 0.785398163f,            1.12837911f,            CrossPlatformMachineEpsilonForEstimates)] // value: (pi / 4)
        [InlineData( 1.0f,                    1.0f,                   CrossPlatformMachineEpsilonForEstimates)]
        [InlineData( 1.12837917f,             0.941396296f,           CrossPlatformMachineEpsilonForEstimates)] // value: (2 / sqrt(pi))
        [InlineData( 1.41421356f,             0.840896428f,           CrossPlatformMachineEpsilonForEstimates)] // value: (sqrt(2))
        [InlineData( 1.44269504f,             0.832554638f,           CrossPlatformMachineEpsilonForEstimates)] // value: (log2(e))
        [InlineData( 1.57079633f,             0.797884583f,           CrossPlatformMachineEpsilonForEstimates)] // value: (pi / 2)
        [InlineData( 2.30258509f,             0.659010231f,           CrossPlatformMachineEpsilonForEstimates)] // value: (ln(10))
        [InlineData( 2.71828183f,             0.606530666f,           CrossPlatformMachineEpsilonForEstimates)] // value: (e)
        [InlineData( 3.14159265f,             0.564189553f,           CrossPlatformMachineEpsilonForEstimates)] // value: (pi)
        [InlineData( float.PositiveInfinity,  0.0f,                   0.0f)]
        public static void ReciprocalSqrtEstimate(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.ReciprocalSqrtEstimate(value), allowedVariance);
        }

        public static IEnumerable<object[]> Round_Digits_TestData
        {
            get
            {
                yield return new object[] { float.NaN, float.NaN, 3, MidpointRounding.ToEven };
                yield return new object[] { float.PositiveInfinity, float.PositiveInfinity, 3, MidpointRounding.ToEven };
                yield return new object[] { float.NegativeInfinity, float.NegativeInfinity, 3, MidpointRounding.ToEven };
                yield return new object[] { 0, 0, 3, MidpointRounding.ToEven };
                yield return new object[] { 3.42156f, 3.422f, 3, MidpointRounding.ToEven };
                yield return new object[] { -3.42156f, -3.422f, 3, MidpointRounding.ToEven };

                yield return new object[] { float.NaN, float.NaN, 3, MidpointRounding.AwayFromZero };
                yield return new object[] { float.PositiveInfinity, float.PositiveInfinity, 3, MidpointRounding.AwayFromZero };
                yield return new object[] { float.NegativeInfinity, float.NegativeInfinity, 3, MidpointRounding.AwayFromZero };
                yield return new object[] { 0, 0, 3, MidpointRounding.AwayFromZero };
                yield return new object[] { 3.42156f, 3.422f, 3, MidpointRounding.AwayFromZero };
                yield return new object[] { -3.42156f, -3.422f, 3, MidpointRounding.AwayFromZero };

                yield return new object[] { float.NaN, float.NaN, 3, MidpointRounding.ToZero };
                yield return new object[] { float.PositiveInfinity, float.PositiveInfinity, 3, MidpointRounding.ToZero };
                yield return new object[] { float.NegativeInfinity, float.NegativeInfinity, 3, MidpointRounding.ToZero };
                yield return new object[] { 0, 0, 3, MidpointRounding.ToZero };
                yield return new object[] { 3.42156f, 3.421f, 3, MidpointRounding.ToZero };
                yield return new object[] { -3.42156f, -3.421f, 3, MidpointRounding.ToZero };

                yield return new object[] { float.NaN, float.NaN, 3, MidpointRounding.ToNegativeInfinity };
                yield return new object[] { float.PositiveInfinity, float.PositiveInfinity, 3, MidpointRounding.ToNegativeInfinity };
                yield return new object[] { float.NegativeInfinity, float.NegativeInfinity, 3, MidpointRounding.ToNegativeInfinity };
                yield return new object[] { 0, 0, 3, MidpointRounding.ToNegativeInfinity };
                yield return new object[] { 3.42156f, 3.421f, 3, MidpointRounding.ToNegativeInfinity };
                yield return new object[] { -3.42156f, -3.422f, 3, MidpointRounding.ToNegativeInfinity };

                yield return new object[] { float.NaN, float.NaN, 3, MidpointRounding.ToPositiveInfinity };
                yield return new object[] { float.PositiveInfinity, float.PositiveInfinity, 3, MidpointRounding.ToPositiveInfinity };
                yield return new object[] { float.NegativeInfinity, float.NegativeInfinity, 3, MidpointRounding.ToPositiveInfinity };
                yield return new object[] { 0, 0, 3, MidpointRounding.ToPositiveInfinity };
                yield return new object[] { 3.42156f, 3.422f, 3, MidpointRounding.ToPositiveInfinity };
                yield return new object[] { -3.42156f, -3.421f, 3, MidpointRounding.ToPositiveInfinity };
            }
        }

        [Fact]
        public static void Round()
        {
            Assert.Equal(0.0f, MathF.Round(0.0f));
            Assert.Equal(1.0f, MathF.Round(1.4f));
            Assert.Equal(2.0f, MathF.Round(1.5f));
            Assert.Equal(2e7f, MathF.Round(2e7f));
            Assert.Equal(0.0f, MathF.Round(-0.0f));
            Assert.Equal(-1.0f, MathF.Round(-1.4f));
            Assert.Equal(-2.0f, MathF.Round(-1.5f));
            Assert.Equal(-2e7f, MathF.Round(-2e7f));
        }

        [Theory]
        [InlineData(MidpointRounding.ToEven)]
        [InlineData(MidpointRounding.AwayFromZero)]
        [InlineData(MidpointRounding.ToNegativeInfinity)]
        [InlineData(MidpointRounding.ToPositiveInfinity)]
        public static void Round_Digits_ByMidpointRounding(MidpointRounding mode)
        {
            Assert.Equal(float.PositiveInfinity, MathF.Round(float.PositiveInfinity, 3, mode));
            Assert.Equal(float.NegativeInfinity, MathF.Round(float.NegativeInfinity, 3, mode));
        }

        [Theory]
        [MemberData(nameof(Round_Digits_TestData))]
        public static void Round_Digits(float x, float expected, int digits, MidpointRounding mode)
        {
            AssertExtensions.Equal(expected, MathF.Round(x, digits, mode), CrossPlatformMachineEpsilon * 10);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, unchecked((int)(0x7FFFFFFF)), float.NegativeInfinity, 0)]
        [InlineData(float.PositiveInfinity, unchecked((int)(0x7FFFFFFF)), float.PositiveInfinity, 0)]
        [InlineData(float.NaN, 0, float.NaN, 0)]
        [InlineData(float.NaN, 0, float.NaN, 0)]
        [InlineData(float.PositiveInfinity, 0, float.PositiveInfinity, 0)]
        [InlineData(float.NaN, 0, float.NaN, 0)]
        [InlineData(float.NaN, 1, float.NaN, 0)]
        [InlineData(float.PositiveInfinity, 2147483647, float.PositiveInfinity, 0)]
        [InlineData(float.PositiveInfinity, -2147483647, float.PositiveInfinity, 0)]
        [InlineData(float.NaN, 2147483647, float.NaN, 0)]
        [InlineData(-0.0f, unchecked((int)(0x80000000)), -0.0f, 0)]
        [InlineData(float.NaN, unchecked((int)(0x7FFFFFFF)), float.NaN, 0)]
        [InlineData(0, unchecked((int)(0x80000000)), 0, 0)]
        [InlineData(0.113314732f, -4, 0.00708217081f, CrossPlatformMachineEpsilon / 100)]
        [InlineData(-0.113314732f, -3, -0.0141643415f, CrossPlatformMachineEpsilon / 10)]
        [InlineData(0.151955223f, -3, 0.0189944021f, CrossPlatformMachineEpsilon / 10)]
        [InlineData(0.202699566f, -3, 0.0253374465f, CrossPlatformMachineEpsilon / 10)]
        [InlineData(0.336622537f, -2, 0.084155634f, CrossPlatformMachineEpsilon / 10)]
        [InlineData(0.367879441f, -2, 0.0919698626f, CrossPlatformMachineEpsilon / 10)]
        [InlineData(0.375214227f, -2, 0.0938035548f, CrossPlatformMachineEpsilon / 10)]
        [InlineData(0.457429347f, -2, 0.114357337f, CrossPlatformMachineEpsilon)]
        [InlineData(0.5f, -1, 0.25f, CrossPlatformMachineEpsilon)]
        [InlineData(0.580191810f, -1, 0.290095896f, CrossPlatformMachineEpsilon)]
        [InlineData(0.612547327f, -1, 0.306273669f, CrossPlatformMachineEpsilon)]
        [InlineData(0.618503138f, -1, 0.309251577f, CrossPlatformMachineEpsilon)]
        [InlineData(0.643218242f, -1, 0.321609110f, CrossPlatformMachineEpsilon)]
        [InlineData(0.740055574f, -1, 0.370027781f, CrossPlatformMachineEpsilon)]
        [InlineData(0.802008879f, -1, 0.401004434f, CrossPlatformMachineEpsilon)]
        [InlineData(0, 2147483647, 0, CrossPlatformMachineEpsilon)]
        [InlineData(0, -2147483647, 0, CrossPlatformMachineEpsilon)]
        [InlineData(0, 2147483647, 0, CrossPlatformMachineEpsilon)]
        [InlineData(1, -1, 0.5, CrossPlatformMachineEpsilon)]
        [InlineData(1, 0, 1, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.24686899f, 0, 1.24686899f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.35124987f, 0, 1.35124987f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.55468228f, 0, 1.55468228f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.61680667f, 0, 1.61680667f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.63252692f, 0, 1.63252692f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.72356793f, 0, 1.72356793f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(2, 1, 4, CrossPlatformMachineEpsilon * 10)]
        [InlineData(2.18612996f, 1, 4.37225992f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(2.66514414f, 1, 5.33028829f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(2.71828183f, 1, 5.43656366f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(2.97068642f, 1, 5.94137285f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1, 0, 1, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1, 1, 2, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.7014118E+38, -276, 1E-45, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1E-45, 276, 1.7014118E+38, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.0002441, -149, 1E-45, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.74999994, -148, 1E-45, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.50000066, -128, 1.46937E-39, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-8.066849, -2, -2.0167122, CrossPlatformMachineEpsilon * 10)]
        [InlineData(4.3452396, -1, 2.1726198, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-8.3814335, 0, -8.3814335, CrossPlatformMachineEpsilon * 10)]
        [InlineData(0.6619859, 3, 5.295887, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.40660393, 4, -6.505663, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-6.5316734, 1, -13.063347, CrossPlatformMachineEpsilon * 100)]
        [InlineData(9.267057, 2, 37.06823, CrossPlatformMachineEpsilon * 100)]
        [InlineData(0.56175977, 5, 17.976313, CrossPlatformMachineEpsilon * 100)]
        [InlineData(0.7741523, 6, 49.545746, CrossPlatformMachineEpsilon * 100)]
        [InlineData(-0.6787637, 7, -86.88175, CrossPlatformMachineEpsilon * 100)]
        [InlineData(4.93340967f, 2, 19.7336387f, CrossPlatformMachineEpsilon * 100)]
        [InlineData(6.58088599f, 2, 26.3235440f, CrossPlatformMachineEpsilon * 100)]
        [InlineData(8.82497783f, 3, 70.5998226f, CrossPlatformMachineEpsilon * 100)]
        public static void ScaleB(float x, int n, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.ScaleB(x, n), allowedVariance);
        }

        [Fact]
        public static void Sign()
        {
            Assert.Equal(0, MathF.Sign(0.0f));
            Assert.Equal(0, MathF.Sign(-0.0f));
            Assert.Equal(-1, MathF.Sign(-3.14f));
            Assert.Equal(1, MathF.Sign(3.14f));
            Assert.Equal(-1, MathF.Sign(float.NegativeInfinity));
            Assert.Equal(1, MathF.Sign(float.PositiveInfinity));
            Assert.Throws<ArithmeticException>(() => MathF.Sign(float.NaN));
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-3.14159265f, -0.0f, CrossPlatformMachineEpsilon)]       // value: -(pi)
        [InlineData(-2.71828183f, -0.410781291f, CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.30258509f, -0.743980337f, CrossPlatformMachineEpsilon)]       // value: -(ln(10))
        [InlineData(-1.57079633f, -1.0f, CrossPlatformMachineEpsilon * 10)]  // value: -(pi / 2)
        [InlineData(-1.44269504f, -0.991806244f, CrossPlatformMachineEpsilon)]       // value: -(log2(e))
        [InlineData(-1.41421356f, -0.987765946f, CrossPlatformMachineEpsilon)]       // value: -(sqrt(2))
        [InlineData(-1.12837917f, -0.903719457f, CrossPlatformMachineEpsilon)]       // value: -(2 / sqrt(pi))
        [InlineData(-1.0f, -0.841470985f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f, -0.707106781f, CrossPlatformMachineEpsilon)]       // value: -(pi / 4),        expected: -(1 / sqrt(2))
        [InlineData(-0.707106781f, -0.649636939f, CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f, -0.638961276f, CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.636619772f, -0.594480769f, CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.434294482f, -0.420770483f, CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.318309886f, -0.312961796f, CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0f, -0.0f, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f)]
        [InlineData(0.318309886f, 0.312961796f, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData(0.434294482f, 0.420770483f, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData(0.636619772f, 0.594480769f, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData(0.693147181f, 0.638961276f, CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData(0.707106781f, 0.649636939f, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData(0.785398163f, 0.707106781f, CrossPlatformMachineEpsilon)]       // value:  (pi / 4),        expected:  (1 / sqrt(2))
        [InlineData(1.0f, 0.841470985f, CrossPlatformMachineEpsilon)]
        [InlineData(1.12837917f, 0.903719457f, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f, 0.987765946f, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData(1.44269504f, 0.991806244f, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData(1.57079633f, 1.0f, CrossPlatformMachineEpsilon * 10)]  // value:  (pi / 2)
        [InlineData(2.30258509f, 0.743980337f, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData(2.71828183f, 0.410781291f, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData(3.14159265f, 0.0f, CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData(float.PositiveInfinity, float.NaN, 0.0f)]
        public static void Sin(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Sin(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NaN,     float.NaN,    0.0f,                             0.0f)]
        [InlineData(-3.14159265f,            -0.0f,         -1.0f,         CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon * 10)]  // value: -(pi)
        [InlineData(-2.71828183f,            -0.410781291f, -0.911733918f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.30258509f,            -0.743980337f, -0.668201510f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(ln(10))
        [InlineData(-1.57079633f,            -1.0f,          0.0f,         CrossPlatformMachineEpsilon * 10, CrossPlatformMachineEpsilon)]       // value: -(pi / 2)
        [InlineData(-1.44269504f,            -0.991806244f,  0.127751218f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(log2(e))
        [InlineData(-1.41421356f,            -0.987765946f,  0.155943695f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(sqrt(2))
        [InlineData(-1.12837917f,            -0.903719457f,  0.428125148f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   -0.841470985f,  0.540302306f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f,           -0.707106781f,  0.707106781f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(pi / 4),        expected_sin: -(1 / sqrt(2)),    expected_cos: 1
        [InlineData(-0.707106781f,           -0.649636939f,  0.760244597f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           -0.638961276f,  0.769238901f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.636619772f,           -0.594480769f,  0.804109828f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.434294482f,           -0.420770483f,  0.907167129f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.318309886f,           -0.312961796f,  0.949765715f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0f,                   -0.0f,          1.0f,         0.0f,                             CrossPlatformMachineEpsilon * 10)]
        [InlineData( float.NaN,               float.NaN,     float.NaN,    0.0f,                             0.0f)]
        [InlineData( 0.0f,                    0.0f,          1.0f,         0.0f,                             CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.318309886f,            0.312961796f,  0.949765715f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData( 0.434294482f,            0.420770483f,  0.907167129f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData( 0.636619772f,            0.594480769f,  0.804109828f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData( 0.693147181f,            0.638961276f,  0.769238901f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData( 0.707106781f,            0.649636939f,  0.760244597f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            0.707106781f,  0.707106781f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (pi / 4),        expected_sin:  (1 / sqrt(2)),    expected_cos: 1
        [InlineData( 1.0f,                    0.841470985f,  0.540302306f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]
        [InlineData( 1.12837917f,             0.903719457f,  0.428125148f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             0.987765946f,  0.155943695f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData( 1.44269504f,             0.991806244f,  0.127751218f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData( 1.57079633f,             1.0f,          0.0f,         CrossPlatformMachineEpsilon * 10, CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData( 2.30258509f,             0.743980337f, -0.668201510f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData( 2.71828183f,             0.410781291f, -0.911733918f, CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData( 3.14159265f,             0.0f,         -1.0f,         CrossPlatformMachineEpsilon,      CrossPlatformMachineEpsilon * 10)]  // value:  (pi)
        [InlineData( float.PositiveInfinity,  float.NaN,     float.NaN,    0.0f,                             0.0f)]
        public static void SinCos(float value, float expectedResultSin, float expectedResultCos, float allowedVarianceSin, float allowedVarianceCos)
        {
            (float resultSin, float resultCos) = MathF.SinCos(value);
            AssertExtensions.Equal(expectedResultSin, resultSin, allowedVarianceSin);
            AssertExtensions.Equal(expectedResultCos, resultCos, allowedVarianceCos);
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NegativeInfinity, 0.0f)]
        [InlineData(-3.14159265f,            -11.5487394f,            CrossPlatformMachineEpsilon * 100)]   // value: -(pi)
        [InlineData(-2.71828183f,            -7.54413710f,            CrossPlatformMachineEpsilon * 10)]    // value: -(e)
        [InlineData(-2.30258509f,            -4.95f,                  CrossPlatformMachineEpsilon * 10)]    // value: -(ln(10))
        [InlineData(-1.57079633f,            -2.30129890f,            CrossPlatformMachineEpsilon * 10)]    // value: -(pi / 2)
        [InlineData(-1.44269504f,            -1.99789801f,            CrossPlatformMachineEpsilon * 10)]    // value: -(log2(e))
        [InlineData(-1.41421356f,            -1.93506682f,            CrossPlatformMachineEpsilon * 10)]    // value: -(sqrt(2))
        [InlineData(-1.12837917f,            -1.38354288f,            CrossPlatformMachineEpsilon * 10)]    // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   -1.17520119f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.785398163f,           -0.868670961f,           CrossPlatformMachineEpsilon)]         // value: -(pi / 4)
        [InlineData(-0.707106781f,           -0.767523145f,           CrossPlatformMachineEpsilon)]         // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           -0.75f,                  CrossPlatformMachineEpsilon)]         // value: -(ln(2))
        [InlineData(-0.636619772f,           -0.680501678f,           CrossPlatformMachineEpsilon)]         // value: -(2 / pi)
        [InlineData(-0.434294482f,           -0.448075979f,           CrossPlatformMachineEpsilon)]         // value: -(log10(e))
        [InlineData(-0.318309886f,           -0.323712439f,           CrossPlatformMachineEpsilon)]         // value: -(1 / pi)
        [InlineData(-0.0f,                   -0.0f,                   0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.318309886f,            0.323712439f,           CrossPlatformMachineEpsilon)]         // value:  (1 / pi)
        [InlineData( 0.434294482f,            0.448075979f,           CrossPlatformMachineEpsilon)]         // value:  (log10(e))
        [InlineData( 0.636619772f,            0.680501678f,           CrossPlatformMachineEpsilon)]         // value:  (2 / pi)
        [InlineData( 0.693147181f,            0.75f,                  CrossPlatformMachineEpsilon)]         // value:  (ln(2))
        [InlineData( 0.707106781f,            0.767523145f,           CrossPlatformMachineEpsilon)]         // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            0.868670961f,           CrossPlatformMachineEpsilon)]         // value:  (pi / 4)
        [InlineData( 1.0f,                    1.17520119f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.12837917f,             1.38354288f,            CrossPlatformMachineEpsilon * 10)]    // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             1.93506682f,            CrossPlatformMachineEpsilon * 10)]    // value:  (sqrt(2))
        [InlineData( 1.44269504f,             1.99789801f,            CrossPlatformMachineEpsilon * 10)]    // value:  (log2(e))
        [InlineData( 1.57079633f,             2.30129890f,            CrossPlatformMachineEpsilon * 10)]    // value:  (pi / 2)
        [InlineData( 2.30258509f,             4.95f,                  CrossPlatformMachineEpsilon * 10)]    // value:  (ln(10))
        [InlineData( 2.71828183f,             7.54413710f,            CrossPlatformMachineEpsilon * 10)]    // value:  (e)
        [InlineData( 3.14159265f,             11.5487394f,            CrossPlatformMachineEpsilon * 100)]   // value:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Sinh(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Sinh(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-3.14159265f, float.NaN, 0.0f)]                                 // value: (pi)
        [InlineData(-2.71828183f, float.NaN, 0.0f)]                                 // value: (e)
        [InlineData(-2.30258509f, float.NaN, 0.0f)]                                 // value: (ln(10))
        [InlineData(-1.57079633f, float.NaN, 0.0f)]                                 // value: (pi / 2)
        [InlineData(-1.44269504f, float.NaN, 0.0f)]                                 // value: (log2(e))
        [InlineData(-1.41421356f, float.NaN, 0.0f)]                                 // value: (sqrt(2))
        [InlineData(-1.12837917f, float.NaN, 0.0f)]                                 // value: (2 / sqrt(pi))
        [InlineData(-1.0f, float.NaN, 0.0f)]
        [InlineData(-0.785398163f, float.NaN, 0.0f)]                                 // value: (pi / 4)
        [InlineData(-0.707106781f, float.NaN, 0.0f)]                                 // value: (1 / sqrt(2))
        [InlineData(-0.693147181f, float.NaN, 0.0f)]                                 // value: (ln(2))
        [InlineData(-0.636619772f, float.NaN, 0.0f)]                                 // value: (2 / pi)
        [InlineData(-0.434294482f, float.NaN, 0.0f)]                                 // value: (log10(e))
        [InlineData(-0.318309886f, float.NaN, 0.0f)]                                 // value: (1 / pi)
        [InlineData(-0.0f, -0.0f, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f)]
        [InlineData(0.318309886f, 0.564189584f, CrossPlatformMachineEpsilon)]          // value: (1 / pi)
        [InlineData(0.434294482f, 0.659010229f, CrossPlatformMachineEpsilon)]          // value: (log10(e))
        [InlineData(0.636619772f, 0.797884561f, CrossPlatformMachineEpsilon)]          // value: (2 / pi)
        [InlineData(0.693147181f, 0.832554611f, CrossPlatformMachineEpsilon)]          // value: (ln(2))
        [InlineData(0.707106781f, 0.840896415f, CrossPlatformMachineEpsilon)]          // value: (1 / sqrt(2))
        [InlineData(0.785398163f, 0.886226925f, CrossPlatformMachineEpsilon)]          // value: (pi / 4)
        [InlineData(1.0f, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.12837917f, 1.06225193f, CrossPlatformMachineEpsilon * 10)]     // value: (2 / sqrt(pi))
        [InlineData(1.41421356f, 1.18920712f, CrossPlatformMachineEpsilon * 10)]     // value: (sqrt(2))
        [InlineData(1.44269504f, 1.20112241f, CrossPlatformMachineEpsilon * 10)]     // value: (log2(e))
        [InlineData(1.57079633f, 1.25331414f, CrossPlatformMachineEpsilon * 10)]     // value: (pi / 2)
        [InlineData(2.30258509f, 1.51742713f, CrossPlatformMachineEpsilon * 10)]     // value: (ln(10))
        [InlineData(2.71828183f, 1.64872127f, CrossPlatformMachineEpsilon * 10)]     // value: (e)
        [InlineData(3.14159265f, 1.77245385F, CrossPlatformMachineEpsilon * 10)]     // value: (pi)
        [InlineData(float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Sqrt(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Sqrt(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-3.14159265f, -0.0f, CrossPlatformMachineEpsilon)]         // value: -(pi)
        [InlineData(-2.71828183f, 0.450549534f, CrossPlatformMachineEpsilon)]         // value: -(e)
        [InlineData(-2.30258509f, 1.11340715f, CrossPlatformMachineEpsilon * 10)]    // value: -(ln(10))
        [InlineData(-1.57079633f, 22877332.0f, 10.0f)]                               // value: -(pi / 2)
        [InlineData(-1.44269504f, -7.76357567f, CrossPlatformMachineEpsilon * 10)]    // value: -(log2(e))
        [InlineData(-1.41421356f, -6.33411917f, CrossPlatformMachineEpsilon * 10)]    // value: -(sqrt(2))
        [InlineData(-1.12837917f, -2.11087684f, CrossPlatformMachineEpsilon * 10)]    // value: -(2 / sqrt(pi))
        [InlineData(-1.0f, -1.55740772f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.785398163f, -1.0f, CrossPlatformMachineEpsilon * 10)]    // value: -(pi / 4)
        [InlineData(-0.707106781f, -0.854510432f, CrossPlatformMachineEpsilon)]         // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f, -0.830640878f, CrossPlatformMachineEpsilon)]         // value: -(ln(2))
        [InlineData(-0.636619772f, -0.739302950f, CrossPlatformMachineEpsilon)]         // value: -(2 / pi)
        [InlineData(-0.434294482f, -0.463829067f, CrossPlatformMachineEpsilon)]         // value: -(log10(e))
        [InlineData(-0.318309886f, -0.329514733f, CrossPlatformMachineEpsilon)]         // value: -(1 / pi)
        [InlineData(-0.0f, -0.0f, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f)]
        [InlineData(0.318309886f, 0.329514733f, CrossPlatformMachineEpsilon)]         // value:  (1 / pi)
        [InlineData(0.434294482f, 0.463829067f, CrossPlatformMachineEpsilon)]         // value:  (log10(e))
        [InlineData(0.636619772f, 0.739302950f, CrossPlatformMachineEpsilon)]         // value:  (2 / pi)
        [InlineData(0.693147181f, 0.830640878f, CrossPlatformMachineEpsilon)]         // value:  (ln(2))
        [InlineData(0.707106781f, 0.854510432f, CrossPlatformMachineEpsilon)]         // value:  (1 / sqrt(2))
        [InlineData(0.785398163f, 1.0f, CrossPlatformMachineEpsilon * 10)]    // value:  (pi / 4)
        [InlineData(1.0f, 1.55740772f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(1.12837917f, 2.11087684f, CrossPlatformMachineEpsilon * 10)]    // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f, 6.33411917f, CrossPlatformMachineEpsilon * 10)]    // value:  (sqrt(2))
        [InlineData(1.44269504f, 7.76357567f, CrossPlatformMachineEpsilon * 10)]    // value:  (log2(e))
        [InlineData(1.57079633f, -22877332.0f, 10.0f)]                               // value:  (pi / 2)
        [InlineData(2.30258509f, -1.11340715f, CrossPlatformMachineEpsilon * 10)]    // value:  (ln(10))
        [InlineData(2.71828183f, -0.450549534f, CrossPlatformMachineEpsilon)]         // value:  (e)
        [InlineData(3.14159265f, 0.0f, CrossPlatformMachineEpsilon)]         // value:  (pi)
        [InlineData(float.PositiveInfinity, float.NaN, 0.0f)]
        public static void Tan(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Tan(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity, -1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-3.14159265f, -0.996272076f, CrossPlatformMachineEpsilon)]       // value: -(pi)
        [InlineData(-2.71828183f, -0.991328916f, CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.30258509f, -0.980198020f, CrossPlatformMachineEpsilon)]       // value: -(ln(10))
        [InlineData(-1.57079633f, -0.917152336f, CrossPlatformMachineEpsilon)]       // value: -(pi / 2)
        [InlineData(-1.44269504f, -0.894238946f, CrossPlatformMachineEpsilon)]       // value: -(log2(e))
        [InlineData(-1.41421356f, -0.888385562f, CrossPlatformMachineEpsilon)]       // value: -(sqrt(2))
        [InlineData(-1.12837917f, -0.810463806f, CrossPlatformMachineEpsilon)]       // value: -(2 / sqrt(pi))
        [InlineData(-1.0f, -0.761594156f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f, -0.655794203f, CrossPlatformMachineEpsilon)]       // value: -(pi / 4)
        [InlineData(-0.707106781f, -0.608859365f, CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f, -0.6f, CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.636619772f, -0.562593600f, CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.434294482f, -0.408904012f, CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.318309886f, -0.307977913f, CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0f, -0.0f, 0.0f)]
        [InlineData(float.NaN, float.NaN, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f)]
        [InlineData(0.318309886f, 0.307977913f, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData(0.434294482f, 0.408904012f, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData(0.636619772f, 0.562593600f, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData(0.693147181f, 0.6f, CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData(0.707106781f, 0.608859365f, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData(0.785398163f, 0.655794203f, CrossPlatformMachineEpsilon)]       // value:  (pi / 4)
        [InlineData(1.0f, 0.761594156f, CrossPlatformMachineEpsilon)]
        [InlineData(1.12837917f, 0.810463806f, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData(1.41421356f, 0.888385562f, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData(1.44269504f, 0.894238946f, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData(1.57079633f, 0.917152336f, CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData(2.30258509f, 0.980198020f, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData(2.71828183f, 0.991328916f, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData(3.14159265f, 0.996272076f, CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData(float.PositiveInfinity, 1.0f, CrossPlatformMachineEpsilon * 10)]
        public static void Tanh(float value, float expectedResult, float allowedVariance)
        {
            AssertExtensions.Equal(expectedResult, MathF.Tanh(value), allowedVariance);
        }

        [Fact]
        public static void Truncate()
        {
            Assert.Equal(0.0f, MathF.Truncate(0.12345f));
            Assert.Equal(3.0f, MathF.Truncate(3.14159f));
            Assert.Equal(-3.0f, MathF.Truncate(-3.14159f));
        }

        public static IEnumerable<object[]> Round_ToEven_TestData()
        {
            yield return new object[] { 1f, 1f };
            yield return new object[] { 0.5f, 0f };
            yield return new object[] { 1.5f, 2f };
            yield return new object[] { 2.5f, 2f };
            yield return new object[] { 3.5f, 4f };
            yield return new object[] { 0.49999997f, 0f };
            yield return new object[] { 1.5f, 2f };
            yield return new object[] { 2.5f, 2f };
            yield return new object[] { 3.5f, 4f };
            yield return new object[] { 4.5f, 4f };
            yield return new object[] { 3.1415927f, 3f };
            yield return new object[] { 2.7182817f, 3f };
            yield return new object[] { 1385.4557f, 1385f };
            yield return new object[] { 3423.4343f, 3423f };
            yield return new object[] { 535345.5f, 535346f };
            yield return new object[] { 535345.5f, 535346f };
            yield return new object[] { 535345.5f, 535346f };
            yield return new object[] { 535345.4f, 535345f };
            yield return new object[] { 535345.6f, 535346f };
            yield return new object[] { -2.7182817f, -3f };
            yield return new object[] { 10f, 10f };
            yield return new object[] { -10f, -10f };
            yield return new object[] { -0f, -0f };
            yield return new object[] { 0f, 0f };
            yield return new object[] { float.NaN, float.NaN };
            yield return new object[] { float.PositiveInfinity, float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity, float.NegativeInfinity };
            yield return new object[] { 3.4028235E+38f, 3.4028235E+38f };
            yield return new object[] { -3.4028235E+38f, -3.4028235E+38f };
        }

        [Theory]
        [MemberData(nameof(Round_ToEven_TestData))]
        public static void Round_ToEven_0(float value, float expected)
        {
            // Math.Round has special fast paths when MidpointRounding is a const
            // Don't replace it with a variable
            Assert.Equal(expected, MathF.Round(value, MidpointRounding.ToEven));
            Assert.Equal(expected, MathF.Round(value, 0, MidpointRounding.ToEven));
        }

        public static IEnumerable<object[]> Round_AwayFromZero_TestData()
        {
            yield return new object[] { 1f, 1f };
            yield return new object[] { 0.5f, 1f };
            yield return new object[] { 1.5f, 2f };
            yield return new object[] { 2.5f, 3f };
            yield return new object[] { 3.5f, 4f };
            yield return new object[] { 0.49999997f, 0f };
            yield return new object[] { 1.5f, 2f };
            yield return new object[] { 2.5f, 3f };
            yield return new object[] { 3.5f, 4f };
            yield return new object[] { 4.5f, 5f };
            yield return new object[] { 3.1415927f, 3f };
            yield return new object[] { 2.7182817f, 3f };
            yield return new object[] { 1385.4557f, 1385f };
            yield return new object[] { 3423.4343f, 3423f };
            yield return new object[] { 535345.5f, 535346f };
            yield return new object[] { 535345.5f, 535346f };
            yield return new object[] { 535345.5f, 535346f };
            yield return new object[] { 535345.4f, 535345f };
            yield return new object[] { 535345.6f, 535346f };
            yield return new object[] { -2.7182817f, -3f };
            yield return new object[] { 10f, 10f };
            yield return new object[] { -10f, -10f };
            yield return new object[] { -0f, -0f };
            yield return new object[] { 0f, 0f };
            yield return new object[] { float.NaN, float.NaN };
            yield return new object[] { float.PositiveInfinity, float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity, float.NegativeInfinity };
            yield return new object[] { 3.4028235E+38f, 3.4028235E+38f };
            yield return new object[] { -3.4028235E+38f, -3.4028235E+38f };
        }

        [Theory]
        [MemberData(nameof(Round_AwayFromZero_TestData))]
        public static void Round_AwayFromZero_0(float value, float expected)
        {
            // Math.Round has special fast paths when MidpointRounding is a const
            // Don't replace it with a variable
            Assert.Equal(expected, MathF.Round(value, MidpointRounding.AwayFromZero));
            Assert.Equal(expected, MathF.Round(value, 0, MidpointRounding.AwayFromZero));
        }
    }
}
