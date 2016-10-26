// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;
using Xunit.Sdk;

namespace System.Tests
{
    public static partial class MathFTests
    {
       // binary32 (float) has a machine epsilon of 2^-23 (approx. 1.19e-07). However, this is
       // slightly too accurate when writing tests meant to run against math library implementations
       // for various platforms. 2^-21 (approx. 4.76e-07) seems to be as accurate as we can get for
       // our current set of platforms.
       //
       // The tests themselves will take `CrossPlatformMachineEpsilon` and adjust it according to the
       // expected result so that the delta used for comparison will compare the most significant digits
       // and ignore any digits that are outside the single precision range (6-9 digits).
       //
       // For example, a test with an expect result in the format of 0.xxxxxxxxx will use CrossPlatformMachineEpsilon
       // for the variance, while an expected result in the format of 0.0xxxxxxxxx will use
       // CrossPlatformMachineEpsilon / 10 and and expected result in the format of x.xxxxxx will
       // use CrossPlatformMachineEpsilon * 10.
        public const float CrossPlatformMachineEpsilon = 4.76837158e-07f;

        /// <summary>
        /// Verifies that two <see cref="float"/> values are equal, within the <paramref name="allowedVariance"/>.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="allowedVariance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        public static void AssertEqual(float expected, float actual, float allowedVariance)
        {
            // This is essentially equivalent to the Xunit.Assert.Equal(double, double, int) implementation
            // available here: https://github.com/xunit/xunit/blob/2.1/src/xunit.assert/Asserts/EqualityAsserts.cs#L46

            var delta = MathF.Abs(actual - expected);

            if (delta > allowedVariance)
            {
                throw new EqualException($"{expected,10:G9}", $"{actual,10:G9}");
            }
        }

        [Fact]
        public static void Abs()
        {
            Assert.Equal(MathF.PI, MathF.Abs(MathF.PI));
            Assert.Equal(0.0f, MathF.Abs(0.0f));
            Assert.Equal(MathF.PI, MathF.Abs(-MathF.PI));
            Assert.Equal(float.NaN, MathF.Abs(float.NaN));
            Assert.Equal(float.PositiveInfinity, MathF.Abs(float.PositiveInfinity));
            Assert.Equal(float.PositiveInfinity, MathF.Abs(float.NegativeInfinity));
        }

        [Fact]
        public static void Acos()
        {
            Assert.Equal(0.0f, MathF.Acos(1.0f));
            AssertEqual(MathF.PI / 2.0f, MathF.Acos(0.0f), CrossPlatformMachineEpsilon * 10);
            AssertEqual(MathF.PI, MathF.Acos(-1.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(float.NaN, MathF.Acos(float.NaN));
            Assert.Equal(float.NaN, MathF.Acos(float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Acos(float.NegativeInfinity));
        }

        [Fact]
        public static void Asin()
        {
            AssertEqual(MathF.PI / 2.0f, MathF.Asin(1.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(0.0f, MathF.Asin(0.0f));
            AssertEqual(-MathF.PI / 2.0f, MathF.Asin(-1.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(float.NaN, MathF.Asin(float.NaN));
            Assert.Equal(float.NaN, MathF.Asin(float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Asin(float.NegativeInfinity));
        }

        [Fact]
        public static void Atan()
        {
            AssertEqual(MathF.PI / 4.0f, MathF.Atan(1.0f), CrossPlatformMachineEpsilon);
            Assert.Equal(0.0f, MathF.Atan(0.0f));
            AssertEqual(-MathF.PI / 4.0f, MathF.Atan(-1.0f), CrossPlatformMachineEpsilon);
            Assert.Equal(float.NaN, MathF.Atan(float.NaN));
            AssertEqual(MathF.PI / 2.0f, MathF.Atan(float.PositiveInfinity), CrossPlatformMachineEpsilon * 10);
            AssertEqual(-MathF.PI / 2.0f, MathF.Atan(float.NegativeInfinity), CrossPlatformMachineEpsilon * 10);
        }

        [Fact]
        public static void Atan2()
        {
            AssertEqual(-MathF.PI, MathF.Atan2(-0.0f, -0.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(-0.0f, MathF.Atan2(-0.0f, 0.0f));
            AssertEqual(MathF.PI, MathF.Atan2(0.0f, -0.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(0.0f, MathF.Atan2(0.0f, 0.0f));
            AssertEqual(MathF.PI / 2.0f, MathF.Atan2(1.0f, 0.0f), CrossPlatformMachineEpsilon * 10);
            AssertEqual(0.588002604f, MathF.Atan2(2.0f, 3.0f), CrossPlatformMachineEpsilon);
            Assert.Equal(0.0f, MathF.Atan2(0.0f, 3.0f));
            AssertEqual(-0.588002604f, MathF.Atan2(-2.0f, 3.0f), CrossPlatformMachineEpsilon);
            Assert.Equal(float.NaN, MathF.Atan2(float.NaN, 1.0f));
            Assert.Equal(float.NaN, MathF.Atan2(1.0f, float.NaN));
            AssertEqual(MathF.PI / 2.0f, MathF.Atan2(float.PositiveInfinity, 1.0f), CrossPlatformMachineEpsilon * 10);
            AssertEqual(-MathF.PI / 2.0f, MathF.Atan2(float.NegativeInfinity, 1.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(0.0f, MathF.Atan2(1.0f, float.PositiveInfinity));
            AssertEqual(MathF.PI, MathF.Atan2(1.0f, float.NegativeInfinity), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(float.NaN, MathF.Atan2(float.NegativeInfinity, float.NegativeInfinity));
            Assert.Equal(float.NaN, MathF.Atan2(float.NegativeInfinity, float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Atan2(float.PositiveInfinity, float.NegativeInfinity));
            Assert.Equal(float.NaN, MathF.Atan2(float.PositiveInfinity, float.PositiveInfinity));
        }

        [Fact]
        public static void Ceiling()
        {
            Assert.Equal(2.0f, MathF.Ceiling(1.1f));
            Assert.Equal(2.0f, MathF.Ceiling(1.9f));
            Assert.Equal(-1.0f, MathF.Ceiling(-1.1f));
            Assert.Equal(float.NaN, MathF.Ceiling(float.NaN));
            Assert.Equal(float.PositiveInfinity, MathF.Ceiling(float.PositiveInfinity));
            Assert.Equal(float.NegativeInfinity, MathF.Ceiling(float.NegativeInfinity));
        }

        [Fact]
        public static void Cos()
        {
            AssertEqual(0.540302306f, MathF.Cos(1.0f), CrossPlatformMachineEpsilon);
            Assert.Equal(1.0f, MathF.Cos(0.0f));
            AssertEqual(0.540302306f, MathF.Cos(-1.0f), CrossPlatformMachineEpsilon);
            Assert.Equal(float.NaN, MathF.Cos(float.NaN));
            Assert.Equal(float.NaN, MathF.Cos(float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Cos(float.NegativeInfinity));
        }

        [Fact]
        public static void Cosh()
        {
            AssertEqual(1.54308063f, MathF.Cosh(1.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(1.0f, MathF.Cosh(0.0f));
            AssertEqual(1.54308063f, MathF.Cosh(-1.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(float.NaN, MathF.Cosh(float.NaN));
            Assert.Equal(float.PositiveInfinity, MathF.Cosh(float.PositiveInfinity));
            Assert.Equal(float.PositiveInfinity, MathF.Cosh(float.NegativeInfinity));
        }

        [Fact]
        public static void Exp()
        {
            AssertEqual(20.0855369f, MathF.Exp(3.0f), CrossPlatformMachineEpsilon * 100);
            Assert.Equal(1.0f, MathF.Exp(0.0f));
            Assert.Equal(MathF.E, MathF.Exp(1.0f));
            AssertEqual(0.0497870684f, MathF.Exp(-3.0f), CrossPlatformMachineEpsilon / 10);
            Assert.Equal(float.NaN, MathF.Exp(float.NaN));
            Assert.Equal(float.PositiveInfinity, MathF.Exp(float.PositiveInfinity));
            Assert.Equal(0.0f, MathF.Exp(float.NegativeInfinity));
        }

        [Fact]
        public static void Floor()
        {
            Assert.Equal(1.0f, MathF.Floor(1.1f));
            Assert.Equal(1.0f, MathF.Floor(1.9f));
            Assert.Equal(-2.0f, MathF.Floor(-1.1f));
            Assert.Equal(float.NaN, MathF.Floor(float.NaN));
            Assert.Equal(float.PositiveInfinity, MathF.Floor(float.PositiveInfinity));
            Assert.Equal(float.NegativeInfinity, MathF.Floor(float.NegativeInfinity));
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

        [Fact]
        public static void Log()
        {
            AssertEqual(1.09861229f, MathF.Log(3.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(float.NegativeInfinity, MathF.Log(0.0f));
            Assert.Equal(float.NaN, MathF.Log(-3.0f));
            Assert.Equal(float.NaN, MathF.Log(float.NaN));
            Assert.Equal(float.PositiveInfinity, MathF.Log(float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Log(float.NegativeInfinity));
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

        [Fact]
        public static void Log10()
        {
            AssertEqual(0.477121255f, MathF.Log10(3.0f), CrossPlatformMachineEpsilon);
            Assert.Equal(float.NegativeInfinity, MathF.Log10(0.0f));
            Assert.Equal(float.NaN, MathF.Log10(-3.0f));
            Assert.Equal(float.NaN, MathF.Log10(float.NaN));
            Assert.Equal(float.PositiveInfinity, MathF.Log10(float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Log10(float.NegativeInfinity));
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

        [Fact]
        public static void Pow()
        {
            Assert.Equal(1.0f, MathF.Pow(0.0f, 0.0f));
            Assert.Equal(1.0f, MathF.Pow(1.0f, 0.0f));
            Assert.Equal(8.0f, MathF.Pow(2.0f, 3.0f));
            Assert.Equal(0.0f, MathF.Pow(0.0f, 3.0f));
            Assert.Equal(-8.0f, MathF.Pow(-2.0f, 3.0f));
            Assert.Equal(float.NaN, MathF.Pow(float.NaN, 1.0f));
            Assert.Equal(float.NaN, MathF.Pow(1.0f, float.NaN));
            Assert.Equal(float.PositiveInfinity, MathF.Pow(float.PositiveInfinity, 1.0f));
            Assert.Equal(float.NegativeInfinity, MathF.Pow(float.NegativeInfinity, 1.0f));
            Assert.Equal(1.0f, MathF.Pow(1.0f, float.PositiveInfinity));
            Assert.Equal(1.0f, MathF.Pow(1.0f, float.NegativeInfinity));
            Assert.Equal(0.0f, MathF.Pow(0.0f, float.PositiveInfinity));
            Assert.Equal(float.PositiveInfinity, MathF.Pow(0.0f, -1.0f));
            Assert.Equal(float.PositiveInfinity, MathF.Pow(0.0f, float.NegativeInfinity));
            Assert.Equal(float.NaN, MathF.Pow(-1.0f, float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Pow(-1.0f, float.NegativeInfinity));
            Assert.Equal(float.PositiveInfinity, MathF.Pow(1.1f, float.PositiveInfinity));
            Assert.Equal(0.0f, MathF.Pow(1.1f, float.NegativeInfinity));
            Assert.Equal(float.PositiveInfinity, MathF.Pow(-1.1f, float.PositiveInfinity));
            Assert.Equal(0.0f, MathF.Pow(-1.1f, float.NegativeInfinity));
            Assert.Equal(0.0f, MathF.Pow(1.1f, float.NegativeInfinity));
            Assert.Equal(float.NaN, MathF.Pow(float.NaN, 0.0f));
            Assert.Equal(1.0f, MathF.Pow(0.0f, -0.0f));
            Assert.Equal(0.0f, MathF.Pow(0.0f, 1.0f));
            Assert.Equal(float.PositiveInfinity, MathF.Pow(-0.0f, float.NegativeInfinity));
            Assert.Equal(float.NegativeInfinity, MathF.Pow(-0.0f, -1.0f));
            Assert.Equal(1.0f, MathF.Pow(-0.0f, -0.0f));
            Assert.Equal(1.0f, MathF.Pow(-0.0f, 0.0f));
            Assert.Equal(-0.0f, MathF.Pow(-0.0f, 1.0f));
            Assert.Equal(0.0f, MathF.Pow(-0.0f, float.PositiveInfinity));
            Assert.Equal(0.0f, MathF.Pow(float.NegativeInfinity, float.NegativeInfinity));
            Assert.Equal(float.PositiveInfinity, MathF.Pow(float.NegativeInfinity, float.PositiveInfinity));
            Assert.Equal(0.0f, MathF.Pow(float.PositiveInfinity, float.NegativeInfinity));
            Assert.Equal(float.PositiveInfinity, MathF.Pow(float.PositiveInfinity, float.PositiveInfinity));
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

        [Fact]
        public static void Sin()
        {
            AssertEqual(0.841470985f, MathF.Sin(1.0f), CrossPlatformMachineEpsilon);
            Assert.Equal(0.0f, MathF.Sin(0.0f));
            AssertEqual(-0.841470985f, MathF.Sin(-1.0f), CrossPlatformMachineEpsilon);
            Assert.Equal(float.NaN, MathF.Sin(float.NaN));
            Assert.Equal(float.NaN, MathF.Sin(float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Sin(float.NegativeInfinity));
        }

        [Fact]
        public static void Sinh()
        {
            AssertEqual(1.17520119f, MathF.Sinh(1.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(0.0f, MathF.Sinh(0.0f));
            AssertEqual(-1.17520119f, MathF.Sinh(-1.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(float.NaN, MathF.Sinh(float.NaN));
            Assert.Equal(float.PositiveInfinity, MathF.Sinh(float.PositiveInfinity));
            Assert.Equal(float.NegativeInfinity, MathF.Sinh(float.NegativeInfinity));
        }

        [Fact]
        public static void Sqrt()
        {
            AssertEqual(1.73205081f, MathF.Sqrt(3.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(0.0f, MathF.Sqrt(0.0f));
            Assert.Equal(float.NaN, MathF.Sqrt(-3.0f));
            Assert.Equal(float.NaN, MathF.Sqrt(float.NaN));
            Assert.Equal(float.PositiveInfinity, MathF.Sqrt(float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Sqrt(float.NegativeInfinity));
        }

        [Fact]
        public static void Tan()
        {
            AssertEqual(1.55740772f, MathF.Tan(1.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(0.0f, MathF.Tan(0.0f));
            AssertEqual(-1.55740772f, MathF.Tan(-1.0f), CrossPlatformMachineEpsilon * 10);
            Assert.Equal(float.NaN, MathF.Tan(float.NaN));
            Assert.Equal(float.NaN, MathF.Tan(float.PositiveInfinity));
            Assert.Equal(float.NaN, MathF.Tan(float.NegativeInfinity));
        }

        [Fact]
        public static void Tanh()
        {
            AssertEqual(0.761594156f, MathF.Tanh(1.0f), CrossPlatformMachineEpsilon);
            Assert.Equal(0.0f, MathF.Tanh(0.0f));
            AssertEqual(-0.761594156f, MathF.Tanh(-1.0f), CrossPlatformMachineEpsilon);
            Assert.Equal(float.NaN, MathF.Tanh(float.NaN));
            Assert.Equal(1.0f, MathF.Tanh(float.PositiveInfinity));
            Assert.Equal(-1.0f, MathF.Tanh(float.NegativeInfinity));
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
