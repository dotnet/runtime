// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Sdk;

namespace System.Tests
{
    public static partial class MathFTests
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

        /// <summary>Verifies that two <see cref="float"/> values are equal, within the <paramref name="variance"/>.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        private static void AssertEqual(float expected, float actual, float variance)
        {
            if (float.IsNaN(expected))
            {
                if (float.IsNaN(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (float.IsNaN(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (float.IsNegativeInfinity(expected))
            {
                if (float.IsNegativeInfinity(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (float.IsNegativeInfinity(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (float.IsPositiveInfinity(expected))
            {
                if (float.IsPositiveInfinity(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (float.IsPositiveInfinity(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (IsNegativeZero(expected))
            {
                if (IsNegativeZero(actual))
                {
                    return;
                }

                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not ±0.0, then we are handling a case where
                // the actual result is expected to not be exactly -0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }
            else if (IsNegativeZero(actual))
            {
                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not ±0.0, then we are handling a case where
                // the actual result is expected to not be exactly -0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }

            if (IsPositiveZero(expected))
            {
                if (IsPositiveZero(actual))
                {
                    return;
                }

                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not ±0.0, then we are handling a case where
                // the actual result is expected to not be exactly +0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }
            else if (IsPositiveZero(actual))
            {
                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not ±0.0, then we are handling a case where
                // the actual result is expected to not be exactly +0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }

            var delta = Math.Abs(actual - expected);

            if (delta > variance)
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
        }

        private unsafe static bool IsNegativeZero(float value)
        {
            return (*(uint*)(&value)) == 0x80000000;
        }

        private unsafe static bool IsPositiveZero(float value)
        {
            return (*(uint*)(&value)) == 0x00000000;
        }

        // We have a custom ToString here to ensure that edge cases (specifically ±0.0,
        // but also NaN and ±∞) are correctly and consistently represented.
        private static string ToStringPadded(float value)
        {
            if (double.IsNaN(value))
            {
                return "NaN".PadLeft(10);
            }
            else if (double.IsPositiveInfinity(value))
            {
                return "+∞".PadLeft(10);
            }
            else if (double.IsNegativeInfinity(value))
            {
                return "-∞".PadLeft(10);
            }
            else if (IsNegativeZero(value))
            {
                return "-0.0".PadLeft(10);
            }
            else if (IsPositiveZero(value))
            {
                return "+0.0".PadLeft(10);
            }
            else
            {
                return $"{value,10:G9}";
            }
        }

        [Theory]
        [InlineData( float.NegativeInfinity, float.PositiveInfinity, 0.0f)]
        [InlineData(-3.14159265f,            3.14159265f,            CrossPlatformMachineEpsilon * 10)]     // value: -(pi)             expected: (pi)
        [InlineData(-2.71828183f,            2.71828183f,            CrossPlatformMachineEpsilon * 10)]     // value: -(e)              expected: (e)
        [InlineData(-2.30258509f,            2.30258509f,            CrossPlatformMachineEpsilon * 10)]     // value: -(ln(10))         expected: (ln(10))
        [InlineData(-1.57079633f,            1.57079633f,            CrossPlatformMachineEpsilon * 10)]     // value: -(pi / 2)         expected: (pi / 2)
        [InlineData(-1.44269504f,            1.44269504f,            CrossPlatformMachineEpsilon * 10)]     // value: -(log2(e))        expected: (log2(e))
        [InlineData(-1.41421356f,            1.41421356f,            CrossPlatformMachineEpsilon * 10)]     // value: -(sqrt(2))        expected: (sqrt(2))
        [InlineData(-1.12837917f,            1.12837917f,            CrossPlatformMachineEpsilon * 10)]     // value: -(2 / sqrt(pi))   expected: (2 / sqrt(pi))
        [InlineData(-1.0f,                   1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.785398163f,           0.785398163f,           CrossPlatformMachineEpsilon)]          // value: -(pi / 4)         expected: (pi / 4)
        [InlineData(-0.707106781f,           0.707106781f,           CrossPlatformMachineEpsilon)]          // value: -(1 / sqrt(2))    expected: (1 / sqrt(2))
        [InlineData(-0.693147181f,           0.693147181f,           CrossPlatformMachineEpsilon)]          // value: -(ln(2))          expected: (ln(2))
        [InlineData(-0.636619772f,           0.636619772f,           CrossPlatformMachineEpsilon)]          // value: -(2 / pi)         expected: (2 / pi)
        [InlineData(-0.434294482f,           0.434294482f,           CrossPlatformMachineEpsilon)]          // value: -(log10(e))       expected: (log10(e))
        [InlineData(-0.318309886f,           0.318309886f,           CrossPlatformMachineEpsilon)]          // value: -(1 / pi)         expected: (1 / pi)
        [InlineData(-0.0f,                   0.0f,                   0.0f)]
        [InlineData( float.NaN,              float.NaN,              0.0f)]
        [InlineData( 0.0f,                   0.0f,                   0.0f)]
        [InlineData( 0.318309886f,           0.318309886f,           CrossPlatformMachineEpsilon)]          // value:  (1 / pi)         expected: (1 / pi)
        [InlineData( 0.434294482f,           0.434294482f,           CrossPlatformMachineEpsilon)]          // value:  (log10(e))       expected: (log10(e))
        [InlineData( 0.636619772f,           0.636619772f,           CrossPlatformMachineEpsilon)]          // value:  (2 / pi)         expected: (2 / pi)
        [InlineData( 0.693147181f,           0.693147181f,           CrossPlatformMachineEpsilon)]          // value:  (ln(2))          expected: (ln(2))
        [InlineData( 0.707106781f,           0.707106781f,           CrossPlatformMachineEpsilon)]          // value:  (1 / sqrt(2))    expected: (1 / sqrt(2))
        [InlineData( 0.785398163f,           0.785398163f,           CrossPlatformMachineEpsilon)]          // value:  (pi / 4)         expected: (pi / 4)
        [InlineData( 1.0f,                   1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.12837917f,            1.12837917f,            CrossPlatformMachineEpsilon * 10)]     // value:  (2 / sqrt(pi))   expected: (2 / sqrt(pi))
        [InlineData( 1.41421356f,            1.41421356f,            CrossPlatformMachineEpsilon * 10)]     // value:  (sqrt(2))        expected: (sqrt(2))
        [InlineData( 1.44269504f,            1.44269504f,            CrossPlatformMachineEpsilon * 10)]     // value:  (log2(e))        expected: (log2(e))
        [InlineData( 1.57079633f,            1.57079633f,            CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 2)         expected: (pi / 2)
        [InlineData( 2.30258509f,            2.30258509f,            CrossPlatformMachineEpsilon * 10)]     // value:  (ln(10))         expected: (ln(10))
        [InlineData( 2.71828183f,            2.71828183f,            CrossPlatformMachineEpsilon * 10)]     // value:  (e)              expected: (e)
        [InlineData( 3.14159265f,            3.14159265f,            CrossPlatformMachineEpsilon * 10)]     // value:  (pi)             expected: (pi)
        [InlineData( float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Abs(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Abs(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, float.NaN,    0.0f)]
        [InlineData(-1.0f,                   3.14159265f,  CrossPlatformMachineEpsilon * 10)]   // expected:  (pi)
        [InlineData(-0.911733915f,           2.71828183f,  CrossPlatformMachineEpsilon * 10)]   // expected:  (e)
        [InlineData(-0.668201510f,           2.30258509f,  CrossPlatformMachineEpsilon * 10)]   // expected:  (ln(10))
        [InlineData(-0.0f,                   1.57079633f,  CrossPlatformMachineEpsilon * 10)]   // expected:  (pi / 2)
        [InlineData( float.NaN,              float.NaN,    0.0f)]
        [InlineData( 0.0f,                   1.57079633f,  CrossPlatformMachineEpsilon * 10)]   // expected:  (pi / 2)
        [InlineData( 0.127751218f,           1.44269504f,  CrossPlatformMachineEpsilon * 10)]   // expected:  (log2(e))
        [InlineData( 0.155943695f,           1.41421356f,  CrossPlatformMachineEpsilon * 10)]   // expected:  (sqrt(2))
        [InlineData( 0.428125148f,           1.12837917f,  CrossPlatformMachineEpsilon * 10)]   // expected:  (2 / sqrt(pi))
        [InlineData( 0.540302306f,           1.0f,         CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.707106781f,           0.785398163f, CrossPlatformMachineEpsilon)]        // expected:  (pi / 4),         value:  (1 / sqrt(2))
        [InlineData( 0.760244597f,           0.707106781f, CrossPlatformMachineEpsilon)]        // expected:  (1 / sqrt(2))
        [InlineData( 0.769238901f,           0.693147181f, CrossPlatformMachineEpsilon)]        // expected:  (ln(2))
        [InlineData( 0.804109828f,           0.636619772f, CrossPlatformMachineEpsilon)]        // expected:  (2 / pi)
        [InlineData( 0.907167129f,           0.434294482f, CrossPlatformMachineEpsilon)]        // expected:  (log10(e))
        [InlineData( 0.949765715f,           0.318309886f, CrossPlatformMachineEpsilon)]        // expected:  (1 / pi)
        [InlineData( 1.0f,                   0.0f,         0.0f)]
        [InlineData( float.PositiveInfinity, float.NaN,    0.0f)]
        public static void Acos(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Acos(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NaN,    0.0f)]
        [InlineData(-1.0f,                   -1.57079633f,  CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.991806244f,           -1.44269504f,  CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.987765946f,           -1.41421356f,  CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.903719457f,           -1.12837917f,  CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.841470985f,           -1.0f,         CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.743980337f,           -0.839007561f, CrossPlatformMachineEpsilon)]       // expected: -(pi - ln(10))
        [InlineData(-0.707106781f,           -0.785398163f, CrossPlatformMachineEpsilon)]       // expected: -(pi / 4),         value: (1 / sqrt(2))
        [InlineData(-0.649636939f,           -0.707106781f, CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.638961276f,           -0.693147181f, CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.594480769f,           -0.636619772f, CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.420770483f,           -0.434294482f, CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.410781291f,           -0.423310825f, CrossPlatformMachineEpsilon)]       // expected: -(pi - e)
        [InlineData(-0.312961796f,           -0.318309886f, CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0f,                   -0.0f,         0.0f)]
        [InlineData( float.NaN,               float.NaN,    0.0f)]
        [InlineData( 0.0f,                    0.0f,         0.0f)]
        [InlineData( 0.312961796f,            0.318309886f, CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.410781291f,            0.423310825f, CrossPlatformMachineEpsilon)]       // expected:  (pi - e)
        [InlineData( 0.420770483f,            0.434294482f, CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.594480769f,            0.636619772f, CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 0.638961276f,            0.693147181f, CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 0.649636939f,            0.707106781f, CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 0.707106781f,            0.785398163f, CrossPlatformMachineEpsilon)]       // expected:  (pi / 4),         value: (1 / sqrt(2))
        [InlineData( 0.743980337f,            0.839007561f, CrossPlatformMachineEpsilon)]       // expected:  (pi - ln(10))
        [InlineData( 0.841470985f,            1.0f,         CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.903719457f,            1.12837917f,  CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 0.987765946f,            1.41421356f,  CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 0.991806244f,            1.44269504f,  CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 1.0f,                    1.57079633f,  CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( float.PositiveInfinity,  float.NaN,    0.0f)]
        public static void Asin(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Asin(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, -1.57079633f,  CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-7.76357567f,            -1.44269504f,  CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-6.33411917f,            -1.41421356f,  CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-2.11087684f,            -1.12837917f,  CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-1.55740772f,            -1.0f,         CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.11340715f,            -0.839007561f, CrossPlatformMachineEpsilon)]       // expected: -(pi - ln(10))
        [InlineData(-1.0f,                   -0.785398163f, CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.854510432f,           -0.707106781f, CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.830640878f,           -0.693147181f, CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.739302950f,           -0.636619772f, CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.463829067f,           -0.434294482f, CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.450549534f,           -0.423310825f, CrossPlatformMachineEpsilon)]       // expected: -(pi - e)
        [InlineData(-0.329514733f,           -0.318309886f, CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0f,                   -0.0f,         0.0f)]
        [InlineData( float.NaN,               float.NaN,    0.0f)]
        [InlineData( 0.0f,                    0.0f,         0.0f)]
        [InlineData( 0.329514733f,            0.318309886f, CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.450549534f,            0.423310825f, CrossPlatformMachineEpsilon)]       // expected:  (pi - e)
        [InlineData( 0.463829067f,            0.434294482f, CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.739302950f,            0.636619772f, CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 0.830640878f,            0.693147181f, CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 0.854510432f,            0.707106781f, CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 1.0f,                    0.785398163f, CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 1.11340715f,             0.839007561f, CrossPlatformMachineEpsilon)]       // expected:  (pi - ln(10))
        [InlineData( 1.55740772f,             1.0f,         CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.11087684f,             1.12837917f,  CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 6.33411917f,             1.41421356f,  CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 7.76357567f,             1.44269504f,  CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( float.PositiveInfinity,  1.57079633f,  CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        public static void Atan(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Atan(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, -1.0f,                   -1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData( float.NegativeInfinity, -0.0f,                   -1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData( float.NegativeInfinity,  float.NaN,               float.NaN,    0.0f)]
        [InlineData( float.NegativeInfinity,  0.0f,                   -1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData( float.NegativeInfinity,  1.0f,                   -1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData(-1.0f,                   -1.0f,                   -2.35619449f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(3 * pi / 4)
        [InlineData(-1.0f,                   -0.0f,                   -1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData(-1.0f,                    float.NaN,               float.NaN,    0.0f)]
        [InlineData(-1.0f,                    0.0f,                   -1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi / 2)
        [InlineData(-1.0f,                    1.0f,                   -0.785398163f, CrossPlatformMachineEpsilon)]          // expected: -(pi / 4)
        [InlineData(-1.0f,                    float.PositiveInfinity, -0.0f,         0.0f)]
        [InlineData(-0.991806244f,           -0.127751218f,           -1.69889761f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - log2(e))
        [InlineData(-0.991806244f,            0.127751218f,           -1.44269504f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(log2(e))
        [InlineData(-0.987765946f,           -0.155943695f,           -1.72737909f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - sqrt(2))
        [InlineData(-0.987765946f,            0.155943695f,           -1.41421356f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(sqrt(2))
        [InlineData(-0.903719457f,           -0.428125148f,           -2.01321349f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - (2 / sqrt(pi))
        [InlineData(-0.903719457f,            0.428125148f,           -1.12837917f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(2 / sqrt(pi)
        [InlineData(-0.841470985f,           -0.540302306f,           -2.14159265f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - 1)
        [InlineData(-0.841470985f,            0.540302306f,           -1.0f,         CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.743980337f,           -0.668201510f,           -2.30258509f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(ln(10))
        [InlineData(-0.743980337f,            0.668201510f,           -0.839007561f, CrossPlatformMachineEpsilon)]          // expected: -(pi - ln(10))
        [InlineData(-0.707106781f,           -0.707106781f,           -2.35619449f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(3 * pi / 4),         y: -(1 / sqrt(2))   x: -(1 / sqrt(2))
        [InlineData(-0.707106781f,            0.707106781f,           -0.785398163f, CrossPlatformMachineEpsilon)]          // expected: -(pi / 4),             y: -(1 / sqrt(2))   x:  (1 / sqrt(2))
        [InlineData(-0.649636939f,           -0.760244597f,           -2.43448587f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - (1 / sqrt(2))
        [InlineData(-0.649636939f,            0.760244597f,           -0.707106781f, CrossPlatformMachineEpsilon)]          // expected: -(1 / sqrt(2))
        [InlineData(-0.638961276f,           -0.769238901f,           -2.44844547f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - ln(2))
        [InlineData(-0.638961276f,            0.769238901f,           -0.693147181f, CrossPlatformMachineEpsilon)]          // expected: -(ln(2))
        [InlineData(-0.594480769f,           -0.804109828f,           -2.50497288f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - (2 / pi))
        [InlineData(-0.594480769f,            0.804109828f,           -0.636619772f, CrossPlatformMachineEpsilon)]          // expected: -(2 / pi)
        [InlineData(-0.420770483f,           -0.907167129f,           -2.70729817f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - log10(e))
        [InlineData(-0.420770483f,            0.907167129f,           -0.434294482f, CrossPlatformMachineEpsilon)]          // expected: -(log10(e))
        [InlineData(-0.410781291f,           -0.911733915f,           -2.71828183f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(e)
        [InlineData(-0.410781291f,            0.911733915f,           -0.423310825f, CrossPlatformMachineEpsilon)]          // expected: -(pi - e)
        [InlineData(-0.312961796f,           -0.949765715f,           -2.82328277f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi - (1 / pi))
        [InlineData(-0.312961796f,            0.949765715f,           -0.318309886f, CrossPlatformMachineEpsilon)]          // expected: -(1 / pi)
        [InlineData(-0.0f,                    float.NegativeInfinity, -3.14159265f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi)
        [InlineData(-0.0f,                   -1.0f,                   -3.14159265f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi)
        [InlineData(-0.0f,                   -0.0f,                   -3.14159265f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(pi)
        [InlineData(-0.0f,                    float.NaN,               float.NaN,    0.0f)]
        [InlineData(-0.0f,                    0.0f,                   -0.0f,         0.0f)]
        [InlineData(-0.0f,                    1.0f,                   -0.0f,         0.0f)]
        [InlineData(-0.0f,                    float.PositiveInfinity, -0.0f,         0.0f)]
        [InlineData( float.NaN,               float.NegativeInfinity,  float.NaN,    0.0f)]
        [InlineData( float.NaN,              -1.0f,                    float.NaN,    0.0f)]
        [InlineData( float.NaN,              -0.0f,                    float.NaN,    0.0f)]
        [InlineData( float.NaN,               float.NaN,               float.NaN,    0.0f)]
        [InlineData( float.NaN,               0.0f,                    float.NaN,    0.0f)]
        [InlineData( float.NaN,               1.0f,                    float.NaN,    0.0f)]
        [InlineData( float.NaN,               float.PositiveInfinity,  float.NaN,    0.0f)]
        [InlineData( 0.0f,                    float.NegativeInfinity,  3.14159265f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi)
        [InlineData( 0.0f,                   -1.0f,                    3.14159265f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi)
        [InlineData( 0.0f,                   -0.0f,                    3.14159265f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi)
        [InlineData( 0.0f,                    float.NaN,               float.NaN,    0.0f)]
        [InlineData( 0.0f,                    0.0f,                    0.0f,         0.0f)]
        [InlineData( 0.0f,                    1.0f,                    0.0f,         0.0f)]
        [InlineData( 0.0f,                    float.PositiveInfinity,  0.0f,         0.0f)]
        [InlineData( 0.312961796f,           -0.949765715f,            2.82328277f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - (1 / pi))
        [InlineData( 0.312961796f,            0.949765715f,            0.318309886f, CrossPlatformMachineEpsilon)]          // expected:  (1 / pi)
        [InlineData( 0.410781291f,           -0.911733915f,            2.71828183f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (e)
        [InlineData( 0.410781291f,            0.911733915f,            0.423310825f, CrossPlatformMachineEpsilon)]          // expected:  (pi - e)
        [InlineData( 0.420770483f,           -0.907167129f,            2.70729817f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - log10(e))
        [InlineData( 0.420770483f,            0.907167129f,            0.434294482f, CrossPlatformMachineEpsilon)]          // expected:  (log10(e))
        [InlineData( 0.594480769f,           -0.804109828f,            2.50497288f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - (2 / pi))
        [InlineData( 0.594480769f,            0.804109828f,            0.636619772f, CrossPlatformMachineEpsilon)]          // expected:  (2 / pi)
        [InlineData( 0.638961276f,           -0.769238901f,            2.44844547f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - ln(2))
        [InlineData( 0.638961276f,            0.769238901f,            0.693147181f, CrossPlatformMachineEpsilon)]          // expected:  (ln(2))
        [InlineData( 0.649636939f,           -0.760244597f,            2.43448587f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - (1 / sqrt(2))
        [InlineData( 0.649636939f,            0.760244597f,            0.707106781f, CrossPlatformMachineEpsilon)]          // expected:  (1 / sqrt(2))
        [InlineData( 0.707106781f,           -0.707106781f,            2.35619449f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (3 * pi / 4),         y:  (1 / sqrt(2))   x: -(1 / sqrt(2))
        [InlineData( 0.707106781f,            0.707106781f,            0.785398163f, CrossPlatformMachineEpsilon)]          // expected:  (pi / 4),             y:  (1 / sqrt(2))   x:  (1 / sqrt(2))
        [InlineData( 0.743980337f,           -0.668201510f,            2.30258509f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (ln(10))
        [InlineData( 0.743980337f,            0.668201510f,            0.839007561f, CrossPlatformMachineEpsilon)]          // expected:  (pi - ln(10))
        [InlineData( 0.841470985f,           -0.540302306f,            2.14159265f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - 1)
        [InlineData( 0.841470985f,            0.540302306f,            1.0f,         CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.903719457f,           -0.428125148f,            2.01321349f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - (2 / sqrt(pi))
        [InlineData( 0.903719457f,            0.428125148f,            1.12837917f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (2 / sqrt(pi))
        [InlineData( 0.987765946f,           -0.155943695f,            1.72737909f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - sqrt(2))
        [InlineData( 0.987765946f,            0.155943695f,            1.41421356f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (sqrt(2))
        [InlineData( 0.991806244f,           -0.127751218f,            1.69889761f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi - log2(e))
        [InlineData( 0.991806244f,            0.127751218f,            1.44269504f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (log2(e))
        [InlineData( 1.0f,                   -1.0f,                    2.35619449f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (3 * pi / 4)
        [InlineData( 1.0f,                   -0.0f,                    1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        [InlineData( 1.0f,                    float.NaN,               float.NaN,    0.0f)]
        [InlineData( 1.0f,                    0.0f,                    1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        [InlineData( 1.0f,                    1.0f,                    0.785398163f, CrossPlatformMachineEpsilon)]          // expected:  (pi / 4)
        [InlineData( 1.0f,                    float.PositiveInfinity,  0.0f,         0.0f)]
        [InlineData( float.PositiveInfinity, -1.0f,                    1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        [InlineData( float.PositiveInfinity, -0.0f,                    1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        [InlineData( float.PositiveInfinity,  float.NaN,               float.NaN,    0.0f)]
        [InlineData( float.PositiveInfinity,  0.0f,                    1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        [InlineData( float.PositiveInfinity,  1.0f,                    1.57079633f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (pi / 2)
        public static void Atan2(float y, float x, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Atan2(y, x), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, float.NegativeInfinity, -2.35619449f,  CrossPlatformMachineEpsilon * 10)]     // expected: -(3 * pi / 4)
        [InlineData( float.NegativeInfinity, float.PositiveInfinity, -0.785398163f, CrossPlatformMachineEpsilon)]          // expected: -(pi / 4)
        [InlineData( float.PositiveInfinity, float.NegativeInfinity,  2.35619449f,  CrossPlatformMachineEpsilon * 10)]     // expected:  (3 * pi / 4
        [InlineData( float.PositiveInfinity, float.PositiveInfinity,  0.785398163f, CrossPlatformMachineEpsilon)]          // expected:  (pi / 4)
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void Atan2_IEEE(float y, float x, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Atan2(y, x), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, float.NegativeInfinity,  float.NaN, 0.0f)]
        [InlineData( float.NegativeInfinity, float.PositiveInfinity,  float.NaN, 0.0f)]
        [InlineData( float.PositiveInfinity, float.NegativeInfinity,  float.NaN, 0.0f)]
        [InlineData( float.PositiveInfinity, float.PositiveInfinity,  float.NaN, 0.0f)]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void Atan2_IEEE_Legacy(float y, float x, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Atan2(y, x), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity,  float.NegativeInfinity, 0.0f)]
        [InlineData(-3.14159265f,           -3.0f,                   0.0f)]     // value: -(pi)
        [InlineData(-2.71828183f,           -2.0f,                   0.0f)]     // value: -(e)
        [InlineData(-2.30258509f,           -2.0f,                   0.0f)]     // value: -(ln(10))
        [InlineData(-1.57079633f,           -1.0f,                   0.0f)]     // value: -(pi / 2)
        [InlineData(-1.44269504f,           -1.0f,                   0.0f)]     // value: -(log2(e))
        [InlineData(-1.41421356f,           -1.0f,                   0.0f)]     // value: -(sqrt(2))
        [InlineData(-1.12837917f,           -1.0f,                   0.0f)]     // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                  -1.0f,                   0.0f)]
        [InlineData(-0.785398163f,          -0.0f,                   0.0f, Skip = "https://github.com/dotnet/coreclr/issues/10287")]  // value: -(pi / 4)
        [InlineData(-0.707106781f,          -0.0f,                   0.0f, Skip = "https://github.com/dotnet/coreclr/issues/10287")]  // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,          -0.0f,                   0.0f, Skip = "https://github.com/dotnet/coreclr/issues/10287")]  // value: -(ln(2))
        [InlineData(-0.636619772f,          -0.0f,                   0.0f, Skip = "https://github.com/dotnet/coreclr/issues/10287")]  // value: -(2 / pi)
        [InlineData(-0.434294482f,          -0.0f,                   0.0f, Skip = "https://github.com/dotnet/coreclr/issues/10287")]  // value: -(log10(e))
        [InlineData(-0.318309886f,          -0.0f,                   0.0f, Skip = "https://github.com/dotnet/coreclr/issues/10287")]  // value: -(1 / pi)
        [InlineData(-0.0f,                  -0.0f,                   0.0f)]
        [InlineData( float.NaN,              float.NaN,              0.0f)]
        [InlineData( 0.0f,                   0.0f,                   0.0f)]
        [InlineData( 0.318309886f,           1.0f,                   0.0f)]     // value:  (1 / pi)
        [InlineData( 0.434294482f,           1.0f,                   0.0f)]     // value:  (log10(e))
        [InlineData( 0.636619772f,           1.0f,                   0.0f)]     // value:  (2 / pi)
        [InlineData( 0.693147181f,           1.0f,                   0.0f)]     // value:  (ln(2))
        [InlineData( 0.707106781f,           1.0f,                   0.0f)]     // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,           1.0f,                   0.0f)]     // value:  (pi / 4)
        [InlineData( 1.0f,                   1.0f,                   0.0f)]
        [InlineData( 1.12837917f,            2.0f,                   0.0f)]     // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,            2.0f,                   0.0f)]     // value:  (sqrt(2))
        [InlineData( 1.44269504f,            2.0f,                   0.0f)]     // value:  (log2(e))
        [InlineData( 1.57079633f,            2.0f,                   0.0f)]     // value:  (pi / 2)
        [InlineData( 2.30258509f,            3.0f,                   0.0f)]     // value:  (ln(10))
        [InlineData( 2.71828183f,            3.0f,                   0.0f)]     // value:  (e)
        [InlineData( 3.14159265f,            4.0f,                   0.0f)]     // value:  (pi)
        [InlineData(float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Ceiling(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Ceiling(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NaN,    0.0f)]
        [InlineData(-3.14159265f,            -1.0f,         CrossPlatformMachineEpsilon * 10)]  // value: -(pi)
        [InlineData(-2.71828183f,            -0.911733918f, CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.30258509f,            -0.668201510f, CrossPlatformMachineEpsilon)]       // value: -(ln(10))
        [InlineData(-1.57079633f,             0.0f,         CrossPlatformMachineEpsilon)]       // value: -(pi / 2)
        [InlineData(-1.44269504f,             0.127751218f, CrossPlatformMachineEpsilon)]       // value: -(log2(e))
        [InlineData(-1.41421356f,             0.155943695f, CrossPlatformMachineEpsilon)]       // value: -(sqrt(2))
        [InlineData(-1.12837917f,             0.428125148f, CrossPlatformMachineEpsilon)]       // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                    0.540302306f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f,            0.707106781f, CrossPlatformMachineEpsilon)]       // value: -(pi / 4),        expected:  (1 / sqrt(2))
        [InlineData(-0.707106781f,            0.760244597f, CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,            0.769238901f, CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.636619772f,            0.804109828f, CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.434294482f,            0.907167129f, CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.318309886f,            0.949765715f, CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0f,                    1.0f,         CrossPlatformMachineEpsilon * 10)]
        [InlineData( float.NaN,               float.NaN,    0.0f)]
        [InlineData( 0.0f,                    1.0f,         CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.318309886f,            0.949765715f, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData( 0.434294482f,            0.907167129f, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData( 0.636619772f,            0.804109828f, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData( 0.693147181f,            0.769238901f, CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData( 0.707106781f,            0.760244597f, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            0.707106781f, CrossPlatformMachineEpsilon)]       // value:  (pi / 4),        expected:  (1 / sqrt(2))
        [InlineData( 1.0f,                    0.540302306f, CrossPlatformMachineEpsilon)]
        [InlineData( 1.12837917f,             0.428125148f, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             0.155943695f, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData( 1.44269504f,             0.127751218f, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData( 1.57079633f,             0.0f,         CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData( 2.30258509f,            -0.668201510f, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData( 2.71828183f,            -0.911733918f, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData( 3.14159265f,            -1.0f,         CrossPlatformMachineEpsilon * 10)]  // value:  (pi)
        [InlineData( float.PositiveInfinity,  float.NaN,    0.0f)]
        public static void Cos(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Cos(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, float.PositiveInfinity, 0.0f)]
        [InlineData(-3.14159265f,            11.5919533f,            CrossPlatformMachineEpsilon * 100)]    // value:  (pi)
        [InlineData(-2.71828183f,            7.61012514f,            CrossPlatformMachineEpsilon * 10)]     // value:  (e)
        [InlineData(-2.30258509f,            5.05f,                  CrossPlatformMachineEpsilon * 10)]     // value:  (ln(10))
        [InlineData(-1.57079633f,            2.50917848f,            CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 2)
        [InlineData(-1.44269504f,            2.23418810f,            CrossPlatformMachineEpsilon * 10)]     // value:  (log2(e))
        [InlineData(-1.41421356f,            2.17818356f,            CrossPlatformMachineEpsilon * 10)]     // value:  (sqrt(2))
        [InlineData(-1.12837917f,            1.70710014f,            CrossPlatformMachineEpsilon * 10)]     // value:  (2 / sqrt(pi))
        [InlineData(-1.0f,                   1.54308063f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.785398163f,           1.32460909f,            CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 4)
        [InlineData(-0.707106781f,           1.26059184f,            CrossPlatformMachineEpsilon * 10)]     // value:  (1 / sqrt(2))
        [InlineData(-0.693147181f,           1.25f,                  CrossPlatformMachineEpsilon * 10)]     // value:  (ln(2))
        [InlineData(-0.636619772f,           1.20957949f,            CrossPlatformMachineEpsilon * 10)]     // value:  (2 / pi)
        [InlineData(-0.434294482f,           1.09579746f,            CrossPlatformMachineEpsilon * 10)]     // value:  (log10(e))
        [InlineData(-0.318309886f,           1.05108979f,            CrossPlatformMachineEpsilon * 10)]     // value:  (1 / pi)
        [InlineData(-0.0f,                   1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( float.NaN,              float.NaN,              0.0f)]
        [InlineData( 0.0f,                   1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.318309886f,           1.05108979f,            CrossPlatformMachineEpsilon * 10)]     // value:  (1 / pi)
        [InlineData( 0.434294482f,           1.09579746f,            CrossPlatformMachineEpsilon * 10)]     // value:  (log10(e))
        [InlineData( 0.636619772f,           1.20957949f,            CrossPlatformMachineEpsilon * 10)]     // value:  (2 / pi)
        [InlineData( 0.693147181f,           1.25f,                  CrossPlatformMachineEpsilon * 10)]     // value:  (ln(2))
        [InlineData( 0.707106781f,           1.26059184f,            CrossPlatformMachineEpsilon * 10)]     // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,           1.32460909f,            CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 4)
        [InlineData( 1.0f,                   1.54308063f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.12837917f,            1.70710014f,            CrossPlatformMachineEpsilon * 10)]     // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,            2.17818356f,            CrossPlatformMachineEpsilon * 10)]     // value:  (sqrt(2))
        [InlineData( 1.44269504f,            2.23418810f,            CrossPlatformMachineEpsilon * 10)]     // value:  (log2(e))
        [InlineData( 1.57079633f,            2.50917848f,            CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 2)
        [InlineData( 2.30258509f,            5.05f,                  CrossPlatformMachineEpsilon * 10)]     // value:  (ln(10))
        [InlineData( 2.71828183f,            7.61012514f,            CrossPlatformMachineEpsilon * 10)]     // value:  (e)
        [InlineData( 3.14159265f,            11.5919533f,            CrossPlatformMachineEpsilon * 100)]    // value:  (pi)
        [InlineData( float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Cosh(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Cosh(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, 0.0f,                   CrossPlatformMachineEpsilon)]
        [InlineData(-3.14159265f,            0.0432139183f,          CrossPlatformMachineEpsilon / 10)]     // value: -(pi)
        [InlineData(-2.71828183f,            0.0659880358f,          CrossPlatformMachineEpsilon / 10)]     // value: -(e)
        [InlineData(-2.30258509f,            0.1f,                   CrossPlatformMachineEpsilon)]          // value: -(ln(10))
        [InlineData(-1.57079633f,            0.207879576f,           CrossPlatformMachineEpsilon)]          // value: -(pi / 2)
        [InlineData(-1.44269504f,            0.236290088f,           CrossPlatformMachineEpsilon)]          // value: -(log2(e))
        [InlineData(-1.41421356f,            0.243116734f,           CrossPlatformMachineEpsilon)]          // value: -(sqrt(2))
        [InlineData(-1.12837917f,            0.323557264f,           CrossPlatformMachineEpsilon)]          // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   0.367879441f,           CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f,           0.455938128f,           CrossPlatformMachineEpsilon)]          // value: -(pi / 4)
        [InlineData(-0.707106781f,           0.493068691f,           CrossPlatformMachineEpsilon)]          // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           0.5f,                   CrossPlatformMachineEpsilon)]          // value: -(ln(2))
        [InlineData(-0.636619772f,           0.529077808f,           CrossPlatformMachineEpsilon)]          // value: -(2 / pi)
        [InlineData(-0.434294482f,           0.647721485f,           CrossPlatformMachineEpsilon)]          // value: -(log10(e))
        [InlineData(-0.318309886f,           0.727377349f,           CrossPlatformMachineEpsilon)]          // value: -(1 / pi)
        [InlineData(-0.0f,                   1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( float.NaN,              float.NaN,              0.0f)]
        [InlineData( 0.0f,                   1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.318309886f,           1.37480223f,            CrossPlatformMachineEpsilon * 10)]     // value:  (1 / pi)
        [InlineData( 0.434294482f,           1.54387344f,            CrossPlatformMachineEpsilon * 10)]     // value:  (log10(e))
        [InlineData( 0.636619772f,           1.89008116f,            CrossPlatformMachineEpsilon * 10)]     // value:  (2 / pi)
        [InlineData( 0.693147181f,           2.0f,                   CrossPlatformMachineEpsilon * 10)]     // value:  (ln(2))
        [InlineData( 0.707106781f,           2.02811498f,            CrossPlatformMachineEpsilon * 10)]     // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,           2.19328005f,            CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 4)
        [InlineData( 1.0f,                   2.71828183f,            CrossPlatformMachineEpsilon * 10)]     //                          expected: (e)
        [InlineData( 1.12837917f,            3.09064302f,            CrossPlatformMachineEpsilon * 10)]     // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,            4.11325038f,            CrossPlatformMachineEpsilon * 10)]     // value:  (sqrt(2))
        [InlineData( 1.44269504f,            4.23208611f,            CrossPlatformMachineEpsilon * 10)]     // value:  (log2(e))
        [InlineData( 1.57079633f,            4.81047738f,            CrossPlatformMachineEpsilon * 10)]     // value:  (pi / 2)
        [InlineData( 2.30258509f,            10.0f,                  CrossPlatformMachineEpsilon * 100)]    // value:  (ln(10))
        [InlineData( 2.71828183f,            15.1542622f,            CrossPlatformMachineEpsilon * 100)]    // value:  (e)
        [InlineData( 3.14159265f,            23.1406926f,            CrossPlatformMachineEpsilon * 100)]    // value:  (pi)
        [InlineData( float.PositiveInfinity, float.PositiveInfinity, 0.0f)]
        public static void Exp(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Exp(value), allowedVariance);
        }

        [Theory]
        [InlineData(float.NegativeInfinity,  float.NegativeInfinity, 0.0f)]
        [InlineData(-3.14159265f,           -4.0f,                   0.0f)]  // value: -(pi)
        [InlineData(-2.71828183f,           -3.0f,                   0.0f)]  // value: -(e)
        [InlineData(-2.30258509f,           -3.0f,                   0.0f)]  // value: -(ln(10))
        [InlineData(-1.57079633f,           -2.0f,                   0.0f)]  // value: -(pi / 2)
        [InlineData(-1.44269504f,           -2.0f,                   0.0f)]  // value: -(log2(e))
        [InlineData(-1.41421356f,           -2.0f,                   0.0f)]  // value: -(sqrt(2))
        [InlineData(-1.12837917f,           -2.0f,                   0.0f)]  // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                  -1.0f,                   0.0f)]
        [InlineData(-0.785398163f,          -1.0f,                   0.0f)]  // value: -(pi / 4)
        [InlineData(-0.707106781f,          -1.0f,                   0.0f)]  // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,          -1.0f,                   0.0f)]  // value: -(ln(2))
        [InlineData(-0.636619772f,          -1.0f,                   0.0f)]  // value: -(2 / pi)
        [InlineData(-0.434294482f,          -1.0f,                   0.0f)]  // value: -(log10(e))
        [InlineData(-0.318309886f,          -1.0f,                   0.0f)]  // value: -(1 / pi)
        [InlineData(-0.0f,                  -0.0f,                   0.0f)]
        [InlineData( float.NaN,              float.NaN,              0.0f)]
        [InlineData( 0.0f,                   0.0f,                   0.0f)]
        [InlineData( 0.318309886f,           0.0f,                   0.0f)]  // value:  (1 / pi)
        [InlineData( 0.434294482f,           0.0f,                   0.0f)]  // value:  (log10(e))
        [InlineData( 0.636619772f,           0.0f,                   0.0f)]  // value:  (2 / pi)
        [InlineData( 0.693147181f,           0.0f,                   0.0f)]  // value:  (ln(2))
        [InlineData( 0.707106781f,           0.0f,                   0.0f)]  // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,           0.0f,                   0.0f)]  // value:  (pi / 4)
        [InlineData( 1.0f,                   1.0f,                   0.0f)]
        [InlineData( 1.12837917f,            1.0f,                   0.0f)]  // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,            1.0f,                   0.0f)]  // value:  (sqrt(2))
        [InlineData( 1.44269504f,            1.0f,                   0.0f)]  // value:  (log2(e))
        [InlineData( 1.57079633f,            1.0f,                   0.0f)]  // value:  (pi / 2)
        [InlineData( 2.30258509f,            2.0f,                   0.0f)]  // value:  (ln(10))
        [InlineData( 2.71828183f,            2.0f,                   0.0f)]  // value:  (e)
        [InlineData( 3.14159265f,            3.0f,                   0.0f)]  // value:  (pi)
        [InlineData(float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Floor(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Floor(value), allowedVariance);
        }

        [Fact]
        public static void IEEERemainder()
        {
            Assert.Equal(-1.0f, MathF.IEEERemainder(3.0f, 2.0f));
            Assert.Equal(0.0f, MathF.IEEERemainder(4.0f, 2.0f));
            Assert.Equal(1.0f, MathF.IEEERemainder(10.0f, 3.0f));
            Assert.Equal(-1.0f, MathF.IEEERemainder(11.0f, 3.0f));
            Assert.Equal(-2.0f, MathF.IEEERemainder(28.0f, 5.0f));
            AssertEqual(1.8f, MathF.IEEERemainder(17.8f, 4.0f), CrossPlatformMachineEpsilon * 10);
            AssertEqual(1.4f, MathF.IEEERemainder(17.8f, 4.1f), CrossPlatformMachineEpsilon * 10);
            AssertEqual(0.1000004f, MathF.IEEERemainder(-16.3f, 4.1f), CrossPlatformMachineEpsilon / 10);
            AssertEqual(1.4f, MathF.IEEERemainder(17.8f, -4.1f), CrossPlatformMachineEpsilon * 10);
            AssertEqual(-1.4f, MathF.IEEERemainder(-17.8f, -4.1f), CrossPlatformMachineEpsilon * 10);
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData(-0.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData( 0.0432139183f,          -3.14159265f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(pi)
        [InlineData( 0.0659880358f,          -2.71828183f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(e)
        [InlineData( 0.1f,                   -2.30258509f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(ln(10))
        [InlineData( 0.207879576f,           -1.57079633f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(pi / 2)
        [InlineData( 0.236290088f,           -1.44269504f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(log2(e))
        [InlineData( 0.243116734f,           -1.41421356f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(sqrt(2))
        [InlineData( 0.323557264f,           -1.12837917f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(2 / sqrt(pi))
        [InlineData( 0.367879441f,           -1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.455938128f,           -0.785398163f,           CrossPlatformMachineEpsilon)]         // expected: -(pi / 4)
        [InlineData( 0.493068691f,           -0.707106781f,           CrossPlatformMachineEpsilon)]         // expected: -(1 / sqrt(2))
        [InlineData( 0.5f,                   -0.693147181f,           CrossPlatformMachineEpsilon)]         // expected: -(ln(2))
        [InlineData( 0.529077808f,           -0.636619772f,           CrossPlatformMachineEpsilon)]         // expected: -(2 / pi)
        [InlineData( 0.647721485f,           -0.434294482f,           CrossPlatformMachineEpsilon)]         // expected: -(log10(e))
        [InlineData( 0.727377349f,           -0.318309886f,           CrossPlatformMachineEpsilon)]         // expected: -(1 / pi)
        [InlineData( 1.0f,                    0.0f,                   0.0f)]
        [InlineData( 1.37480223f,             0.318309886f,           CrossPlatformMachineEpsilon)]         // expected:  (1 / pi)
        [InlineData( 1.54387344f,             0.434294482f,           CrossPlatformMachineEpsilon)]         // expected:  (log10(e))
        [InlineData( 1.89008116f,             0.636619772f,           CrossPlatformMachineEpsilon)]         // expected:  (2 / pi)
        [InlineData( 2.0f,                    0.693147181f,           CrossPlatformMachineEpsilon)]         // expected:  (ln(2))
        [InlineData( 2.02811498f,             0.707106781f,           CrossPlatformMachineEpsilon)]         // expected:  (1 / sqrt(2))
        [InlineData( 2.19328005f,             0.785398163f,           CrossPlatformMachineEpsilon)]         // expected:  (pi / 4)
        [InlineData( 2.71828183f,             1.0f,                   CrossPlatformMachineEpsilon * 10)]    //                              value: (e)
        [InlineData( 3.09064302f,             1.12837917f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (2 / sqrt(pi))
        [InlineData( 4.11325038f,             1.41421356f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (sqrt(2))
        [InlineData( 4.23208611f,             1.44269504f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (log2(e))
        [InlineData( 4.81047738f,             1.57079633f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (pi / 2)
        [InlineData( 10.0f,                   2.30258509f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (ln(10))
        [InlineData( 15.1542622f,             2.71828183f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (e)
        [InlineData( 23.1406926f,             3.14159265f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Log(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Log(value), allowedVariance);
        }

        [Fact]
        public static void LogWithBase()
        {
            Assert.Equal(1.0f, MathF.Log(3.0f, 3.0f));
            AssertEqual(2.40217350f, MathF.Log(14.0f, 3.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(float.NegativeInfinity, MathF.Log(0.0f, 3.0f));
            Assert.Equal(float.NaN, MathF.Log(-3.0f, 3.0f));
            Assert.Equal(float.NaN, MathF.Log(float.NaN, 3.0f));
            Assert.Equal(float.PositiveInfinity, MathF.Log(float.PositiveInfinity, 3.0f));
            Assert.Equal(float.NaN, MathF.Log(float.NegativeInfinity, 3.0f));
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData(-0.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData( 0.000721784159f,        -3.14159265f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(pi)
        [InlineData( 0.00191301410f,         -2.71828183f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(e)
        [InlineData( 0.00498212830f,         -2.30258509f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(ln(10))
        [InlineData( 0.0268660410f,          -1.57079633f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(pi / 2)
        [InlineData( 0.0360831928f,          -1.44269504f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(log2(e))
        [InlineData( 0.0385288847f,          -1.41421356f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(sqrt(2))
        [InlineData( 0.0744082059f,          -1.12837917f,            CrossPlatformMachineEpsilon * 10)]    // expected: -(2 / sqrt(pi))
        [InlineData( 0.1f,                   -1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.163908636f,           -0.785398163f,           CrossPlatformMachineEpsilon)]         // expected: -(pi / 4)
        [InlineData( 0.196287760f,           -0.707106781f,           CrossPlatformMachineEpsilon)]         // expected: -(1 / sqrt(2))
        [InlineData( 0.202699566f,           -0.693147181f,           CrossPlatformMachineEpsilon)]         // expected: -(ln(2))
        [InlineData( 0.230876765f,           -0.636619772f,           CrossPlatformMachineEpsilon)]         // expected: -(2 / pi)
        [InlineData( 0.367879441f,           -0.434294482f,           CrossPlatformMachineEpsilon)]         // expected: -(log10(e))
        [InlineData( 0.480496373f,           -0.318309886f,           CrossPlatformMachineEpsilon)]         // expected: -(1 / pi)
        [InlineData( 1.0f,                    0.0f,                   0.0f)]
        [InlineData( 2.08118116f,             0.318309886f,           CrossPlatformMachineEpsilon)]         // expected:  (1 / pi)
        [InlineData( 2.71828183f,             0.434294482f,           CrossPlatformMachineEpsilon)]         // expected:  (log10(e))        value: (e)
        [InlineData( 4.33131503f,             0.636619772f,           CrossPlatformMachineEpsilon)]         // expected:  (2 / pi)
        [InlineData( 4.93340967f,             0.693147181f,           CrossPlatformMachineEpsilon)]         // expected:  (ln(2))
        [InlineData( 5.09456117f,             0.707106781f,           CrossPlatformMachineEpsilon)]         // expected:  (1 / sqrt(2))
        [InlineData( 6.10095980f,             0.785398163f,           CrossPlatformMachineEpsilon)]         // expected:  (pi / 4)
        [InlineData( 10.0f,                   1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 13.4393779f,             1.12837917f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (2 / sqrt(pi))
        [InlineData( 25.9545535f,             1.41421356f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (sqrt(2))
        [InlineData( 27.7137338f,             1.44269504f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (log2(e))
        [InlineData( 37.2217105f,             1.57079633f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (pi / 2)
        [InlineData( 200.717432f,             2.30258509f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (ln(10))
        [InlineData( 522.735300f,             2.71828183f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (e)
        [InlineData( 1385.45573f,             3.14159265f,            CrossPlatformMachineEpsilon * 10)]    // expected:  (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Log10(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Log10(value), allowedVariance);
        }

        [Fact]
        public static void Max()
        {
            Assert.Equal(3.0f, MathF.Max(3.0f, -2.0f));
            Assert.Equal(float.MaxValue, MathF.Max(float.MinValue, float.MaxValue));
            Assert.Equal(float.PositiveInfinity, MathF.Max(float.NegativeInfinity, float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Max(float.PositiveInfinity, float.NaN));
            Assert.Equal(float.NaN, MathF.Max(float.NaN, float.NaN));
        }

        [Fact]
        public static void Min()
        {
            Assert.Equal(-2.0f, MathF.Min(3.0f, -2.0f));
            Assert.Equal(float.MinValue, MathF.Min(float.MinValue, float.MaxValue));
            Assert.Equal(float.NegativeInfinity, MathF.Min(float.NegativeInfinity, float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Min(float.NegativeInfinity, float.NaN));
            Assert.Equal(float.NaN, MathF.Min(float.NaN, float.NaN));
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NegativeInfinity,  0.0f,                   0.0f)]
        [InlineData( float.NegativeInfinity, -1.0f,                   -0.0f,                   0.0f)]
        [InlineData( float.NegativeInfinity, -0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( float.NegativeInfinity,  float.NaN,               float.NaN,              0.0f)]
        [InlineData( float.NegativeInfinity,  0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( float.NegativeInfinity,  1.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData( float.NegativeInfinity,  float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        [InlineData(-10.0f,                   float.NegativeInfinity,  0.0f,                   0.0f)]
        [InlineData(-10.0f,                  -1.57079633f,             float.NaN,              0.0f)]                                   //          y: -(pi / 2)
        [InlineData(-10.0f,                  -1.0f,                   -0.1f,                   CrossPlatformMachineEpsilon)]
        [InlineData(-10.0f,                  -0.785398163f,            float.NaN,              0.0f)]                                   //          y: -(pi / 4)
        [InlineData(-10.0f,                  -0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-10.0f,                   float.NaN,               float.NaN,              0.0f)]
        [InlineData(-10.0f,                   0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-10.0f,                   0.785398163f,            float.NaN,              0.0f)]                                   //          y:  (pi / 4)
        [InlineData(-10.0f,                   1.0f,                   -10.0f,                  CrossPlatformMachineEpsilon * 100)]
        [InlineData(-10.0f,                   1.57079633f,             float.NaN,              0.0f)]                                   //          y:  (pi / 2)
        [InlineData(-10.0f,                   float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        [InlineData(-2.71828183f,             float.NegativeInfinity,  0.0f,                   0.0f)]                                   // x: -(e)
        [InlineData(-2.71828183f,            -1.57079633f,             float.NaN,              0.0f)]                                   // x: -(e)  y: -(pi / 2)
        [InlineData(-2.71828183f,            -1.0f,                   -0.367879441f,           CrossPlatformMachineEpsilon)]            // x: -(e)
        [InlineData(-2.71828183f,            -0.785398163f,            float.NaN,              0.0f)]                                   // x: -(e)  y: -(pi / 4)
        [InlineData(-2.71828183f,            -0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]       // x: -(e)
        [InlineData(-2.71828183f,             float.NaN,               float.NaN,              0.0f)]
        [InlineData(-2.71828183f,             0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]       // x: -(e)
        [InlineData(-2.71828183f,             0.785398163f,            float.NaN,              0.0f)]                                   // x: -(e)  y:  (pi / 4)
        [InlineData(-2.71828183f,             1.0f,                   -2.71828183f,            CrossPlatformMachineEpsilon * 10)]       // x: -(e)  expected: (e)
        [InlineData(-2.71828183f,             1.57079633f,             float.NaN,              0.0f)]                                   // x: -(e)  y:  (pi / 2)
        [InlineData(-2.71828183f,             float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        [InlineData(-1.0f,                   -1.0f,                   -1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0f,                   -0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0f,                    float.NaN,               float.NaN,              0.0f)]
        [InlineData(-1.0f,                    0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0f,                    1.0f,                   -1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.0f,                    float.NegativeInfinity,  float.PositiveInfinity, 0.0f)]
        [InlineData(-0.0f,                   -3.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(-0.0f,                   -2.0f,                    float.PositiveInfinity, 0.0f)]
        [InlineData(-0.0f,                   -1.57079633f,             float.PositiveInfinity, 0.0f)]                                   //          y: -(pi / 2)
        [InlineData(-0.0f,                   -1.0f,                    float.NegativeInfinity, 0.0f)]
        [InlineData(-0.0f,                   -0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.0f,                    float.NaN,               float.NaN,              0.0f)]
        [InlineData(-0.0f,                    0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.0f,                    1.0f,                   -0.0f,                   0.0f)]
        [InlineData(-0.0f,                    1.57079633f,             0.0f,                   0.0f)]                                   //          y: -(pi / 2)
        [InlineData(-0.0f,                    2.0f,                    0.0f,                   0.0f)]
        [InlineData(-0.0f,                    3.0f,                   -0.0f,                   0.0f)]
        [InlineData(-0.0f,                    float.PositiveInfinity,  0.0f,                   0.0f)]
        [InlineData( float.NaN,               float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData( float.NaN,              -1.0f,                    float.NaN,              0.0f)]
        [InlineData( float.NaN,               float.NaN,               float.NaN,              0.0f)]
        [InlineData( float.NaN,               1.0f,                    float.NaN,              0.0f)]
        [InlineData( float.NaN,               float.PositiveInfinity,  float.NaN,              0.0f)]
        [InlineData( 0.0f,                    float.NegativeInfinity,  float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -3.0f,                    float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -2.0f,                    float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -1.57079633f,             float.PositiveInfinity, 0.0f)]                                   //          y: -(pi / 2)
        [InlineData( 0.0f,                   -1.0f,                    float.PositiveInfinity, 0.0f)]
        [InlineData( 0.0f,                   -0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.0f,                    float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.0f,                    1.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.0f,                    1.57079633f,             0.0f,                   0.0f)]                                   //          y: -(pi / 2)
        [InlineData( 0.0f,                    2.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.0f,                    3.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.0f,                    float.PositiveInfinity,  0.0f,                   0.0f)]
        [InlineData( 1.0f,                    float.NegativeInfinity,  1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.0f,                   -1.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.0f,                   -0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.0f,                    0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.0f,                    1.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.0f,                    float.PositiveInfinity,  1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.71828183f,             float.NegativeInfinity,  0.0f,                   0.0f)]
        [InlineData( 2.71828183f,            -3.14159265f,             0.0432139183f,          CrossPlatformMachineEpsilon / 10)]       // x:  (e)  y: -(pi)
        [InlineData( 2.71828183f,            -2.71828183f,             0.0659880358f,          CrossPlatformMachineEpsilon / 10)]       // x:  (e)  y: -(e)
        [InlineData( 2.71828183f,            -2.30258509f,             0.1f,                   CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(ln(10))
        [InlineData( 2.71828183f,            -1.57079633f,             0.207879576f,           CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(pi / 2)
        [InlineData( 2.71828183f,            -1.44269504f,             0.236290088f,           CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(log2(e))
        [InlineData( 2.71828183f,            -1.41421356f,             0.243116734f,           CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(sqrt(2))
        [InlineData( 2.71828183f,            -1.12837917f,             0.323557264f,           CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(2 / sqrt(pi))
        [InlineData( 2.71828183f,            -1.0f,                    0.367879441f,           CrossPlatformMachineEpsilon)]            // x:  (e)
        [InlineData( 2.71828183f,            -0.785398163f,            0.455938128f,           CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(pi / 4)
        [InlineData( 2.71828183f,            -0.707106781f,            0.493068691f,           CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(1 / sqrt(2))
        [InlineData( 2.71828183f,            -0.693147181f,            0.5f,                   CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(ln(2))
        [InlineData( 2.71828183f,            -0.636619772f,            0.529077808f,           CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(2 / pi)
        [InlineData( 2.71828183f,            -0.434294482f,            0.647721485f,           CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(log10(e))
        [InlineData( 2.71828183f,            -0.318309886f,            0.727377349f,           CrossPlatformMachineEpsilon)]            // x:  (e)  y: -(1 / pi)
        [InlineData( 2.71828183f,            -0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]       // x:  (e)
        [InlineData( 2.71828183f,             float.NaN,               float.NaN,              0.0f)]
        [InlineData( 2.71828183f,             0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]       // x:  (e)
        [InlineData( 2.71828183f,             0.318309886f,            1.37480223f,            CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (1 / pi)
        [InlineData( 2.71828183f,             0.434294482f,            1.54387344f,            CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (log10(e))
        [InlineData( 2.71828183f,             0.636619772f,            1.89008116f,            CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (2 / pi)
        [InlineData( 2.71828183f,             0.693147181f,            2.0f,                   CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (ln(2))
        [InlineData( 2.71828183f,             0.707106781f,            2.02811498f,            CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (1 / sqrt(2))
        [InlineData( 2.71828183f,             0.785398163f,            2.19328005f,            CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (pi / 4)
        [InlineData( 2.71828183f,             1.0f,                    2.71828183f,            CrossPlatformMachineEpsilon * 10)]       // x:  (e)                      expected: (e)
        [InlineData( 2.71828183f,             1.12837917f,             3.09064302f,            CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (2 / sqrt(pi))
        [InlineData( 2.71828183f,             1.41421356f,             4.11325038f,            CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (sqrt(2))
        [InlineData( 2.71828183f,             1.44269504f,             4.23208611f,            CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (log2(e))
        [InlineData( 2.71828183f,             1.57079633f,             4.81047738f,            CrossPlatformMachineEpsilon * 10)]       // x:  (e)  y:  (pi / 2)
        [InlineData( 2.71828183f,             2.30258509f,             10.0f,                  CrossPlatformMachineEpsilon * 100)]      // x:  (e)  y:  (ln(10))
        [InlineData( 2.71828183f,             2.71828183f,             15.1542622f,            CrossPlatformMachineEpsilon * 100)]      // x:  (e)  y:  (e)
        [InlineData( 2.71828183f,             3.14159265f,             23.1406926f,            CrossPlatformMachineEpsilon * 100)]      // x:  (e)  y:  (pi)
        [InlineData( 2.71828183f,             float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]                                   // x:  (e)
        [InlineData( 10.0f,                   float.NegativeInfinity,  0.0f,                   0.0f)]
        [InlineData( 10.0f,                  -3.14159265f,             0.000721784159f,        CrossPlatformMachineEpsilon / 1000)]     //          y: -(pi)
        [InlineData( 10.0f,                  -2.71828183f,             0.00191301410f,         CrossPlatformMachineEpsilon / 100)]      //          y: -(e)
        [InlineData( 10.0f,                  -2.30258509f,             0.00498212830f,         CrossPlatformMachineEpsilon / 100)]      //          y: -(ln(10))
        [InlineData( 10.0f,                  -1.57079633f,             0.0268660410f,          CrossPlatformMachineEpsilon / 10)]       //          y: -(pi / 2)
        [InlineData( 10.0f,                  -1.44269504f,             0.0360831928f,          CrossPlatformMachineEpsilon / 10)]       //          y: -(log2(e))
        [InlineData( 10.0f,                  -1.41421356f,             0.0385288847f,          CrossPlatformMachineEpsilon / 10)]       //          y: -(sqrt(2))
        [InlineData( 10.0f,                  -1.12837917f,             0.0744082059f,          CrossPlatformMachineEpsilon / 10)]       //          y: -(2 / sqrt(pi))
        [InlineData( 10.0f,                  -1.0f,                    0.1f,                   CrossPlatformMachineEpsilon)]
        [InlineData( 10.0f,                  -0.785398163f,            0.163908636f,           CrossPlatformMachineEpsilon)]            //          y: -(pi / 4)
        [InlineData( 10.0f,                  -0.707106781f,            0.196287760f,           CrossPlatformMachineEpsilon)]            //          y: -(1 / sqrt(2))
        [InlineData( 10.0f,                  -0.693147181f,            0.202699566f,           CrossPlatformMachineEpsilon)]            //          y: -(ln(2))
        [InlineData( 10.0f,                  -0.636619772f,            0.230876765f,           CrossPlatformMachineEpsilon)]            //          y: -(2 / pi)
        [InlineData( 10.0f,                  -0.434294482f,            0.367879441f,           CrossPlatformMachineEpsilon)]            //          y: -(log10(e))
        [InlineData( 10.0f,                  -0.318309886f,            0.480496373f,           CrossPlatformMachineEpsilon)]            //          y: -(1 / pi)
        [InlineData( 10.0f,                  -0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 10.0f,                   float.NaN,               float.NaN,              0.0f)]
        [InlineData( 10.0f,                   0.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 10.0f,                   0.318309886f,            2.08118116f,            CrossPlatformMachineEpsilon * 10)]       //          y:  (1 / pi)
        [InlineData( 10.0f,                   0.434294482f,            2.71828183f,            CrossPlatformMachineEpsilon * 10)]       //          y:  (log10(e))      expected: (e)
        [InlineData( 10.0f,                   0.636619772f,            4.33131503f,            CrossPlatformMachineEpsilon * 10)]       //          y:  (2 / pi)
        [InlineData( 10.0f,                   0.693147181f,            4.93340967f,            CrossPlatformMachineEpsilon * 10)]       //          y:  (ln(2))
        [InlineData( 10.0f,                   0.707106781f,            5.09456117f,            CrossPlatformMachineEpsilon * 10)]       //          y:  (1 / sqrt(2))
        [InlineData( 10.0f,                   0.785398163f,            6.10095980f,            CrossPlatformMachineEpsilon * 10)]       //          y:  (pi / 4)
        [InlineData( 10.0f,                   1.0f,                    10.0f,                  CrossPlatformMachineEpsilon * 100)]
        [InlineData( 10.0f,                   1.12837917f,             13.4393779f,            CrossPlatformMachineEpsilon * 100)]      //          y:  (2 / sqrt(pi))
        [InlineData( 10.0f,                   1.41421356f,             25.9545535f,            CrossPlatformMachineEpsilon * 100)]      //          y:  (sqrt(2))
        [InlineData( 10.0f,                   1.44269504f,             27.7137338f,            CrossPlatformMachineEpsilon * 100)]      //          y:  (log2(e))
        [InlineData( 10.0f,                   1.57079633f,             37.2217105f,            CrossPlatformMachineEpsilon * 100)]      //          y:  (pi / 2)
        [InlineData( 10.0f,                   2.30258509f,             200.717432f,            CrossPlatformMachineEpsilon * 1000)]     //          y:  (ln(10))
        [InlineData( 10.0f,                   2.71828183f,             522.735300f,            CrossPlatformMachineEpsilon * 1000)]     //          y:  (e)
        [InlineData( 10.0f,                   3.14159265f,             1385.45573f,            CrossPlatformMachineEpsilon * 10000)]    //          y:  (pi)
        [InlineData( 10.0f,                   float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        [InlineData( float.PositiveInfinity,  float.NegativeInfinity,  0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity, -1.0f,                    0.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity, -0.0f,                    1.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity,  float.NaN,               float.NaN,              0.0f)]
        [InlineData( float.PositiveInfinity,  0.0f,                    1.0f,                   0.0f)]
        [InlineData( float.PositiveInfinity,  1.0f,                    float.PositiveInfinity, 0.0f)]
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Pow(float x, float y, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Pow(x, y), allowedVariance);
        }

        [Theory]
        [InlineData(-1.0f,       float.NegativeInfinity, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0f,       float.PositiveInfinity, 1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData( float.NaN, -0.0f,                   1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData( float.NaN,  0.0f,                   1.0f, CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.0f,       float.NaN,              1.0f, CrossPlatformMachineEpsilon * 10)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void Pow_IEEE(float x, float y, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Pow(x, y), allowedVariance);
        }

        [Theory]
        [InlineData(-1.0f,      float.NegativeInfinity, float.NaN, 0.0f)]
        [InlineData(-1.0f,      float.PositiveInfinity, float.NaN, 0.0f)]
        [InlineData( float.NaN,-0.0f,                   float.NaN, 0.0f)]
        [InlineData( float.NaN, 0.0f,                   float.NaN, 0.0f)]
        [InlineData( 1.0f,      float.NaN,              float.NaN, 0.0f)]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void Pow_IEEE_Legacy(float x, float y, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Pow(x, y), allowedVariance);
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

        [Fact]
        public static void Round_Digits()
        {
            AssertEqual(3.422f, MathF.Round(3.42156f, 3, MidpointRounding.AwayFromZero), CrossPlatformMachineEpsilon * 10);
            AssertEqual(-3.422f, MathF.Round(-3.42156f, 3, MidpointRounding.AwayFromZero), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(0.0f, MathF.Round(0.0f, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(float.NaN, MathF.Round(float.NaN, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(float.PositiveInfinity, MathF.Round(float.PositiveInfinity, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(float.NegativeInfinity, MathF.Round(float.NegativeInfinity, 3, MidpointRounding.AwayFromZero));
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
        [InlineData( float.NegativeInfinity,  float.NaN,    0.0f)]
        [InlineData(-3.14159265f,            -0.0f,         CrossPlatformMachineEpsilon)]       // value: -(pi)
        [InlineData(-2.71828183f,            -0.410781291f, CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.30258509f,            -0.743980337f, CrossPlatformMachineEpsilon)]       // value: -(ln(10))
        [InlineData(-1.57079633f,            -1.0f,         CrossPlatformMachineEpsilon * 10)]  // value: -(pi / 2)
        [InlineData(-1.44269504f,            -0.991806244f, CrossPlatformMachineEpsilon)]       // value: -(log2(e))
        [InlineData(-1.41421356f,            -0.987765946f, CrossPlatformMachineEpsilon)]       // value: -(sqrt(2))
        [InlineData(-1.12837917f,            -0.903719457f, CrossPlatformMachineEpsilon)]       // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   -0.841470985f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f,           -0.707106781f, CrossPlatformMachineEpsilon)]       // value: -(pi / 4),        expected: -(1 / sqrt(2))
        [InlineData(-0.707106781f,           -0.649636939f, CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           -0.638961276f, CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.636619772f,           -0.594480769f, CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.434294482f,           -0.420770483f, CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.318309886f,           -0.312961796f, CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0f,                   -0.0f,         0.0f)]
        [InlineData( float.NaN,               float.NaN,    0.0f)]
        [InlineData( 0.0f,                    0.0f,         0.0f)]
        [InlineData( 0.318309886f,            0.312961796f, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData( 0.434294482f,            0.420770483f, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData( 0.636619772f,            0.594480769f, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData( 0.693147181f,            0.638961276f, CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData( 0.707106781f,            0.649636939f, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            0.707106781f, CrossPlatformMachineEpsilon)]       // value:  (pi / 4),        expected:  (1 / sqrt(2))
        [InlineData( 1.0f,                    0.841470985f, CrossPlatformMachineEpsilon)]
        [InlineData( 1.12837917f,             0.903719457f, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             0.987765946f, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData( 1.44269504f,             0.991806244f, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData( 1.57079633f,             1.0f,         CrossPlatformMachineEpsilon * 10)]  // value:  (pi / 2)
        [InlineData( 2.30258509f,             0.743980337f, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData( 2.71828183f,             0.410781291f, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData( 3.14159265f,             0.0f,         CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData( float.PositiveInfinity,  float.NaN,    0.0f)]
        public static void Sin(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Sin(value), allowedVariance);
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
            AssertEqual(expectedResult, MathF.Sinh(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData(-3.14159265f,             float.NaN,              0.0f)]                                 // value: (pi)
        [InlineData(-2.71828183f,             float.NaN,              0.0f)]                                 // value: (e)
        [InlineData(-2.30258509f,             float.NaN,              0.0f)]                                 // value: (ln(10))
        [InlineData(-1.57079633f,             float.NaN,              0.0f)]                                 // value: (pi / 2)
        [InlineData(-1.44269504f,             float.NaN,              0.0f)]                                 // value: (log2(e))
        [InlineData(-1.41421356f,             float.NaN,              0.0f)]                                 // value: (sqrt(2))
        [InlineData(-1.12837917f,             float.NaN,              0.0f)]                                 // value: (2 / sqrt(pi))
        [InlineData(-1.0f,                    float.NaN,              0.0f)]
        [InlineData(-0.785398163f,            float.NaN,              0.0f)]                                 // value: (pi / 4)
        [InlineData(-0.707106781f,            float.NaN,              0.0f)]                                 // value: (1 / sqrt(2))
        [InlineData(-0.693147181f,            float.NaN,              0.0f)]                                 // value: (ln(2))
        [InlineData(-0.636619772f,            float.NaN,              0.0f)]                                 // value: (2 / pi)
        [InlineData(-0.434294482f,            float.NaN,              0.0f)]                                 // value: (log10(e))
        [InlineData(-0.318309886f,            float.NaN,              0.0f)]                                 // value: (1 / pi)
        [InlineData(-0.0f,                   -0.0f,                   0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.318309886f,            0.564189584f,           CrossPlatformMachineEpsilon)]          // value: (1 / pi)
        [InlineData( 0.434294482f,            0.659010229f,           CrossPlatformMachineEpsilon)]          // value: (log10(e))
        [InlineData( 0.636619772f,            0.797884561f,           CrossPlatformMachineEpsilon)]          // value: (2 / pi)
        [InlineData( 0.693147181f,            0.832554611f,           CrossPlatformMachineEpsilon)]          // value: (ln(2))
        [InlineData( 0.707106781f,            0.840896415f,           CrossPlatformMachineEpsilon)]          // value: (1 / sqrt(2))
        [InlineData( 0.785398163f,            0.886226925f,           CrossPlatformMachineEpsilon)]          // value: (pi / 4)
        [InlineData( 1.0f,                    1.0f,                   CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.12837917f,             1.06225193f,            CrossPlatformMachineEpsilon * 10)]     // value: (2 / sqrt(pi))
        [InlineData( 1.41421356f,             1.18920712f,            CrossPlatformMachineEpsilon * 10)]     // value: (sqrt(2))
        [InlineData( 1.44269504f,             1.20112241f,            CrossPlatformMachineEpsilon * 10)]     // value: (log2(e))
        [InlineData( 1.57079633f,             1.25331414f,            CrossPlatformMachineEpsilon * 10)]     // value: (pi / 2)
        [InlineData( 2.30258509f,             1.51742713f,            CrossPlatformMachineEpsilon * 10)]     // value: (ln(10))
        [InlineData( 2.71828183f,             1.64872127f,            CrossPlatformMachineEpsilon * 10)]     // value: (e)
        [InlineData( 3.14159265f,             1.77245385F,            CrossPlatformMachineEpsilon * 10)]     // value: (pi)
        [InlineData( float.PositiveInfinity,  float.PositiveInfinity, 0.0f)]
        public static void Sqrt(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Sqrt(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity,  float.NaN,              0.0f)]
        [InlineData(-3.14159265f,            -0.0f,                   CrossPlatformMachineEpsilon)]         // value: -(pi)
        [InlineData(-2.71828183f,             0.450549534f,           CrossPlatformMachineEpsilon)]         // value: -(e)
        [InlineData(-2.30258509f,             1.11340715f,            CrossPlatformMachineEpsilon * 10)]    // value: -(ln(10))
        [InlineData(-1.57079633f,             22877332.0f,            10.0f)]                               // value: -(pi / 2)
        [InlineData(-1.44269504f,            -7.76357567f,            CrossPlatformMachineEpsilon * 10)]    // value: -(log2(e))
        [InlineData(-1.41421356f,            -6.33411917f,            CrossPlatformMachineEpsilon * 10)]    // value: -(sqrt(2))
        [InlineData(-1.12837917f,            -2.11087684f,            CrossPlatformMachineEpsilon * 10)]    // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   -1.55740772f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.785398163f,           -1.0f,                   CrossPlatformMachineEpsilon * 10)]    // value: -(pi / 4)
        [InlineData(-0.707106781f,           -0.854510432f,           CrossPlatformMachineEpsilon)]         // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           -0.830640878f,           CrossPlatformMachineEpsilon)]         // value: -(ln(2))
        [InlineData(-0.636619772f,           -0.739302950f,           CrossPlatformMachineEpsilon)]         // value: -(2 / pi)
        [InlineData(-0.434294482f,           -0.463829067f,           CrossPlatformMachineEpsilon)]         // value: -(log10(e))
        [InlineData(-0.318309886f,           -0.329514733f,           CrossPlatformMachineEpsilon)]         // value: -(1 / pi)
        [InlineData(-0.0f,                   -0.0f,                   0.0f)]
        [InlineData( float.NaN,               float.NaN,              0.0f)]
        [InlineData( 0.0f,                    0.0f,                   0.0f)]
        [InlineData( 0.318309886f,            0.329514733f,           CrossPlatformMachineEpsilon)]         // value:  (1 / pi)
        [InlineData( 0.434294482f,            0.463829067f,           CrossPlatformMachineEpsilon)]         // value:  (log10(e))
        [InlineData( 0.636619772f,            0.739302950f,           CrossPlatformMachineEpsilon)]         // value:  (2 / pi)
        [InlineData( 0.693147181f,            0.830640878f,           CrossPlatformMachineEpsilon)]         // value:  (ln(2))
        [InlineData( 0.707106781f,            0.854510432f,           CrossPlatformMachineEpsilon)]         // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            1.0f,                   CrossPlatformMachineEpsilon * 10)]    // value:  (pi / 4)
        [InlineData( 1.0f,                    1.55740772f,            CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.12837917f,             2.11087684f,            CrossPlatformMachineEpsilon * 10)]    // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             6.33411917f,            CrossPlatformMachineEpsilon * 10)]    // value:  (sqrt(2))
        [InlineData( 1.44269504f,             7.76357567f,            CrossPlatformMachineEpsilon * 10)]    // value:  (log2(e))
        [InlineData( 1.57079633f,            -22877332.0f,            10.0f)]                               // value:  (pi / 2)
        [InlineData( 2.30258509f,            -1.11340715f,            CrossPlatformMachineEpsilon * 10)]    // value:  (ln(10))
        [InlineData( 2.71828183f,            -0.450549534f,           CrossPlatformMachineEpsilon)]         // value:  (e)
        [InlineData( 3.14159265f,             0.0f,                   CrossPlatformMachineEpsilon)]         // value:  (pi)
        [InlineData( float.PositiveInfinity,  float.NaN,              0.0f)]
        public static void Tan(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Tan(value), allowedVariance);
        }

        [Theory]
        [InlineData( float.NegativeInfinity, -1.0f,         CrossPlatformMachineEpsilon * 10)]
        [InlineData(-3.14159265f,            -0.996272076f, CrossPlatformMachineEpsilon)]       // value: -(pi)
        [InlineData(-2.71828183f,            -0.991328916f, CrossPlatformMachineEpsilon)]       // value: -(e)
        [InlineData(-2.30258509f,            -0.980198020f, CrossPlatformMachineEpsilon)]       // value: -(ln(10))
        [InlineData(-1.57079633f,            -0.917152336f, CrossPlatformMachineEpsilon)]       // value: -(pi / 2)
        [InlineData(-1.44269504f,            -0.894238946f, CrossPlatformMachineEpsilon)]       // value: -(log2(e))
        [InlineData(-1.41421356f,            -0.888385562f, CrossPlatformMachineEpsilon)]       // value: -(sqrt(2))
        [InlineData(-1.12837917f,            -0.810463806f, CrossPlatformMachineEpsilon)]       // value: -(2 / sqrt(pi))
        [InlineData(-1.0f,                   -0.761594156f, CrossPlatformMachineEpsilon)]
        [InlineData(-0.785398163f,           -0.655794203f, CrossPlatformMachineEpsilon)]       // value: -(pi / 4)
        [InlineData(-0.707106781f,           -0.608859365f, CrossPlatformMachineEpsilon)]       // value: -(1 / sqrt(2))
        [InlineData(-0.693147181f,           -0.6f,         CrossPlatformMachineEpsilon)]       // value: -(ln(2))
        [InlineData(-0.636619772f,           -0.562593600f, CrossPlatformMachineEpsilon)]       // value: -(2 / pi)
        [InlineData(-0.434294482f,           -0.408904012f, CrossPlatformMachineEpsilon)]       // value: -(log10(e))
        [InlineData(-0.318309886f,           -0.307977913f, CrossPlatformMachineEpsilon)]       // value: -(1 / pi)
        [InlineData(-0.0f,                   -0.0f,         0.0f)]
        [InlineData( float.NaN,               float.NaN,    0.0f)]
        [InlineData( 0.0f,                    0.0f,         0.0f)]
        [InlineData( 0.318309886f,            0.307977913f, CrossPlatformMachineEpsilon)]       // value:  (1 / pi)
        [InlineData( 0.434294482f,            0.408904012f, CrossPlatformMachineEpsilon)]       // value:  (log10(e))
        [InlineData( 0.636619772f,            0.562593600f, CrossPlatformMachineEpsilon)]       // value:  (2 / pi)
        [InlineData( 0.693147181f,            0.6f,         CrossPlatformMachineEpsilon)]       // value:  (ln(2))
        [InlineData( 0.707106781f,            0.608859365f, CrossPlatformMachineEpsilon)]       // value:  (1 / sqrt(2))
        [InlineData( 0.785398163f,            0.655794203f, CrossPlatformMachineEpsilon)]       // value:  (pi / 4)
        [InlineData( 1.0f,                    0.761594156f, CrossPlatformMachineEpsilon)]
        [InlineData( 1.12837917f,             0.810463806f, CrossPlatformMachineEpsilon)]       // value:  (2 / sqrt(pi))
        [InlineData( 1.41421356f,             0.888385562f, CrossPlatformMachineEpsilon)]       // value:  (sqrt(2))
        [InlineData( 1.44269504f,             0.894238946f, CrossPlatformMachineEpsilon)]       // value:  (log2(e))
        [InlineData( 1.57079633f,             0.917152336f, CrossPlatformMachineEpsilon)]       // value:  (pi / 2)
        [InlineData( 2.30258509f,             0.980198020f, CrossPlatformMachineEpsilon)]       // value:  (ln(10))
        [InlineData( 2.71828183f,             0.991328916f, CrossPlatformMachineEpsilon)]       // value:  (e)
        [InlineData( 3.14159265f,             0.996272076f, CrossPlatformMachineEpsilon)]       // value:  (pi)
        [InlineData( float.PositiveInfinity,  1.0f,         CrossPlatformMachineEpsilon * 10)]
        public static void Tanh(float value, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, MathF.Tanh(value), allowedVariance);
        }

        [Fact]
        public static void Truncate()
        {
            Assert.Equal(0.0f, MathF.Truncate(0.12345f));
            Assert.Equal(3.0f, MathF.Truncate(3.14159f));
            Assert.Equal(-3.0f, MathF.Truncate(-3.14159f));
        }
    }
}
