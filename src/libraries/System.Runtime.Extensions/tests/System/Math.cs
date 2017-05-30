// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Sdk;
using System.Collections.Generic;

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
        // will use CrossPlatformMachineEpsilon / 10 and and expected result in the format of x.xxxxxxxxxxxxxxxx will
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

        /// <summary>Verifies that two <see cref="double"/> values are equal, within the <paramref name="allowedVariance"/>.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="allowedVariance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        public static void AssertEqual(double expected, double actual, double variance)
        {
            if (double.IsNaN(expected))
            {
                if (double.IsNaN(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (double.IsNaN(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (double.IsNegativeInfinity(expected))
            {
                if (double.IsNegativeInfinity(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (double.IsNegativeInfinity(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (double.IsPositiveInfinity(expected))
            {
                if (double.IsPositiveInfinity(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (double.IsPositiveInfinity(actual))
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

        private unsafe static bool IsNegativeZero(double value)
        {
            return (*(ulong*)(&value)) == 0x8000000000000000;
        }

        private unsafe static bool IsNegativeZero(float value)
        {
            return (*(uint*)(&value)) == 0x80000000;
        }

        private unsafe static bool IsPositiveZero(double value)
        {
            return (*(ulong*)(&value)) == 0x0000000000000000;
        }

        private unsafe static bool IsPositiveZero(float value)
        {
            return (*(uint*)(&value)) == 0x00000000;
        }

        // We have a custom ToString here to ensure that edge cases (specifically ±0.0,
        // but also NaN and ±∞) are correctly and consistently represented.
        private static string ToStringPadded(double value)
        {
            if (double.IsNaN(value))
            {
                return "NaN".PadLeft(20);
            }
            else if (double.IsPositiveInfinity(value))
            {
                return "+∞".PadLeft(20);
            }
            else if (double.IsNegativeInfinity(value))
            {
                return "-∞".PadLeft(20);
            }
            else if (IsNegativeZero(value))
            {
                return "-0.0".PadLeft(20);
            }
            else if (IsPositiveZero(value))
            {
                return "+0.0".PadLeft(20);
            }
            else
            {
                return $"{value,20:G17}";
            }
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
            AssertEqual(expectedResult, Math.Abs(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Abs(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, double.NaN,          0.0)]
        [InlineData(-1.0,                     3.1415926535897932,  CrossPlatformMachineEpsilon * 10)]   // expected:  (pi)
        [InlineData(-0.91173391478696510,     2.7182818284590452,  CrossPlatformMachineEpsilon * 10)]   // expected:  (e)
        [InlineData(-0.66820151019031295,     2.3025850929940457,  CrossPlatformMachineEpsilon * 10)]   // expected:  (ln(10))
        [InlineData( 0.0,                     1.5707963267948966,  CrossPlatformMachineEpsilon * 10)]   // expected:  (pi / 2)
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
        [InlineData( double.PositiveInfinity, double.NaN,          0.0 )]
        public static void Acos(double value, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.Acos(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,          0.0)]
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
        [InlineData( double.PositiveInfinity,  double.NaN,          0.0)]
        public static void Asin(double value, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.Asin(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Atan(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Atan2(y, x), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, double.NegativeInfinity, -2.3561944901923449,  CrossPlatformMachineEpsilon * 10)]    // expected: -(3 * pi / 4)
        [InlineData( double.NegativeInfinity, double.PositiveInfinity, -0.78539816339744831, CrossPlatformMachineEpsilon)]         // expected: -(pi / 4)
        [InlineData( double.PositiveInfinity, double.NegativeInfinity,  2.3561944901923449,  CrossPlatformMachineEpsilon * 10)]    // expected:  (3 * pi / 4)
        [InlineData( double.PositiveInfinity, double.PositiveInfinity,  0.78539816339744831, CrossPlatformMachineEpsilon)]         // expected:  (pi / 4)
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void Atan2_IEEE(double y, double x, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.Atan2(y, x), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity, double.NegativeInfinity, double.NaN, 0.0)]
        [InlineData( double.NegativeInfinity, double.PositiveInfinity, double.NaN, 0.0)]
        [InlineData( double.PositiveInfinity, double.NegativeInfinity, double.NaN, 0.0)]
        [InlineData( double.PositiveInfinity, double.PositiveInfinity, double.NaN, 0.0)]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void Atan2_IEEE_Legacy(double y, double x, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.Atan2(y, x), allowedVariance);
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
        [InlineData(-0.78539816339744831,    -0.0,                     0.0, Skip = "https://github.com/dotnet/coreclr/issues/10287")]    // value: -(pi / 4)
        [InlineData(-0.70710678118654752,    -0.0,                     0.0, Skip = "https://github.com/dotnet/coreclr/issues/10287")]    // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,    -0.0,                     0.0, Skip = "https://github.com/dotnet/coreclr/issues/10287")]    // value: -(ln(2))
        [InlineData(-0.63661977236758134,    -0.0,                     0.0, Skip = "https://github.com/dotnet/coreclr/issues/10287")]    // value: -(2 / pi)
        [InlineData(-0.43429448190325183,    -0.0,                     0.0, Skip = "https://github.com/dotnet/coreclr/issues/10287")]    // value: -(log10(e))
        [InlineData(-0.31830988618379067,    -0.0,                     0.0, Skip = "https://github.com/dotnet/coreclr/issues/10287")]    // value: -(1 / pi)
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
            AssertEqual(expectedResult, Math.Ceiling(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Cos(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Cosh(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Exp(value), allowedVariance);
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
        [InlineData(-0.0,                    -0.0,                     0.0, Skip = "https://github.com/dotnet/coreclr/issues/10288")]
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
            AssertEqual(expectedResult, Math.Floor(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Log(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Log10(value), allowedVariance);
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

        [Fact]
        public static void Max_Double()
        {
            Assert.Equal(3.0, Math.Max(3.0, -2.0));
            Assert.Equal(double.MaxValue, Math.Max(double.MinValue, double.MaxValue));
            Assert.Equal(double.PositiveInfinity, Math.Max(double.NegativeInfinity, double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Max(double.PositiveInfinity, double.NaN));
            Assert.Equal(double.NaN, Math.Max(double.NaN, double.NaN));
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
        public static void Max_SByte()
        {
            Assert.Equal((sbyte)3, Math.Max((sbyte)(-2), (sbyte)3));
            Assert.Equal(sbyte.MaxValue, Math.Max(sbyte.MinValue, sbyte.MaxValue));
        }

        [Fact]
        public static void Max_Single()
        {
            Assert.Equal(3.0f, Math.Max(3.0f, -2.0f));
            Assert.Equal(float.MaxValue, Math.Max(float.MinValue, float.MaxValue));
            Assert.Equal(float.PositiveInfinity, Math.Max(float.NegativeInfinity, float.PositiveInfinity));
            Assert.Equal(float.NaN, Math.Max(float.PositiveInfinity, float.NaN));
            Assert.Equal(float.NaN, Math.Max(float.NaN, float.NaN));
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

        [Fact]
        public static void Min_Double()
        {
            Assert.Equal(-2.0, Math.Min(3.0, -2.0));
            Assert.Equal(double.MinValue, Math.Min(double.MinValue, double.MaxValue));
            Assert.Equal(double.NegativeInfinity, Math.Min(double.NegativeInfinity, double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Min(double.NegativeInfinity, double.NaN));
            Assert.Equal(double.NaN, Math.Min(double.NaN, double.NaN));
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
        public static void Min_SByte()
        {
            Assert.Equal((sbyte)(-2), Math.Min((sbyte)3, (sbyte)(-2)));
            Assert.Equal(sbyte.MinValue, Math.Min(sbyte.MinValue, sbyte.MaxValue));
        }

        [Fact]
        public static void Min_Single()
        {
            Assert.Equal(-2.0f, Math.Min(3.0f, -2.0f));
            Assert.Equal(float.MinValue, Math.Min(float.MinValue, float.MaxValue));
            Assert.Equal(float.NegativeInfinity, Math.Min(float.NegativeInfinity, float.PositiveInfinity));
            Assert.Equal(float.NaN, Math.Min(float.NegativeInfinity, float.NaN));
            Assert.Equal(float.NaN, Math.Min(float.NaN, float.NaN));
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
            AssertEqual(expectedResult, Math.Pow(x, y), allowedVariance);
        }

        [Theory]
        [InlineData(-1.0,         double.NegativeInfinity, 1.0, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-1.0,         double.PositiveInfinity, 1.0, CrossPlatformMachineEpsilon * 10)]
        [InlineData( double.NaN, -0.0,                     1.0, CrossPlatformMachineEpsilon * 10)]
        [InlineData( double.NaN,  0.0,                     1.0, CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.0,         double.NaN,              1.0, CrossPlatformMachineEpsilon * 10)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void Pow_IEEE(float x, float y, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, Math.Pow(x, y), allowedVariance);
        }

        [Theory]
        [InlineData(-1.0,         double.NegativeInfinity, double.NaN, 0.0)]
        [InlineData(-1.0,         double.PositiveInfinity, double.NaN, 0.0)]
        [InlineData( double.NaN, -0.0,                     double.NaN, 0.0)]
        [InlineData( double.NaN,  0.0,                     double.NaN, 0.0)]
        [InlineData( 1.0,         double.NaN,              double.NaN, 0.0)]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void Pow_IEEE_Legacy(float x, float y, float expectedResult, float allowedVariance)
        {
            AssertEqual(expectedResult, Math.Pow(x, y), allowedVariance);
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
        public static void Round_Decimal_Digits()
        {
            Assert.Equal(3.422m, Math.Round(3.42156m, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(-3.422m, Math.Round(-3.42156m, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(decimal.Zero, Math.Round(decimal.Zero, 3, MidpointRounding.AwayFromZero));
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
        public static void Round_Double_Digits()
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
            AssertEqual(expectedResult, Math.Sin(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Sinh(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Sqrt(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Tan(value), allowedVariance);
        }

        [Theory]
        [InlineData(-1.5707963267948966,      -16331239353195370.0,     0.0)]                               // value: -(pi / 2)
        [InlineData( 1.5707963267948966,       16331239353195370.0,     0.0)]                               // value:  (pi / 2)
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void Tan_PiOver2(double value, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.Tan(value), allowedVariance);
        }

        [Theory]
        [InlineData(-1.5707963267948966,      -16331778728383844.0,     0.0)]                               // value: -(pi / 2)
        [InlineData( 1.5707963267948966,       16331778728383844.0,     0.0)]                               // value:  (pi / 2)
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void Tan_PiOver2_Legacy(double value, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.Tan(value), allowedVariance);
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
            AssertEqual(expectedResult, Math.Tanh(value), allowedVariance);
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
        [InlineData(1073741, 2147483647, 2000, 1647)]
        [InlineData(6, 13952, 2000, 1952)]
        [InlineData(0, 0, 2000, 0)]
        [InlineData(-7, -14032, 2000, -32)]
        [InlineData(-1073741, -2147483648, 2000, -1648)]
        [InlineData(-1073741, 2147483647, -2000, 1647)]
        [InlineData(-6, 13952, -2000, 1952)]
        public static void DivRem(int quotient, int dividend, int divisor, int expectedRemainder)
        {
            int remainder;
            Assert.Equal(quotient, Math.DivRem(dividend, divisor, out remainder));
            Assert.Equal(expectedRemainder, remainder);
        }

        [Theory]
        [InlineData(4611686018427387L, 9223372036854775807L, 2000L, 1807L)]
        [InlineData(4611686018427387L, -9223372036854775808L, -2000L, -1808L)]
        [InlineData(-4611686018427387L, 9223372036854775807L, -2000L, 1807L)]
        [InlineData(-4611686018427387L, -9223372036854775808L, 2000L, -1808L)]
        [InlineData(6L, 13952L, 2000L, 1952L)]
        [InlineData(0L, 0L, 2000L, 0L)]
        [InlineData(-7L, -14032L, 2000L, -32L)]
        [InlineData(-6L, 13952L, -2000L, 1952L)]
        public static void DivRemLong(long quotient, long dividend, long divisor, long expectedRemainder)
        {
            long remainder;
            Assert.Equal(quotient, Math.DivRem(dividend, divisor, out remainder));
            Assert.Equal(expectedRemainder, remainder);
        }
    }
}
