// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

using Xunit;

namespace System.Numerics.Tests
{
    public partial class ComplexTests
    {
        private static Random s_random = new Random(-55);

        public static readonly double[] s_validDoubleValues = new double[]
        {
            double.MinValue,
            -1,
            0,
            double.Epsilon,
            1,
            double.MaxValue,
        };

        public static readonly double[] s_invalidDoubleValues = new double[]
        {
            double.NegativeInfinity,
            double.PositiveInfinity,
            double.NaN
        };

        public static double[] s_phaseTypicalValues = new double[]
        {
            -Math.PI/2,
            0,
            Math.PI/2
        };

        public static string[] s_supportedStandardNumericFormats = new string[] { "C", "E", "F", "G", "N", "P", "R" };

        [Fact]
        public static void Zero()
        {
            Assert.Equal(0, Complex.Zero);
            VerifyRealImaginaryProperties(Complex.Zero, 0, 0);
            VerifyMagnitudePhaseProperties(Complex.Zero, 0, double.NaN);
        }

        [Fact]
        public static void One()
        {
            Assert.Equal(1, Complex.One);
            VerifyRealImaginaryProperties(Complex.One, 1, 0);
            VerifyMagnitudePhaseProperties(Complex.One, 1, 0);
        }

        [Fact]
        public static void ImaginaryOne()
        {
            VerifyRealImaginaryProperties(Complex.ImaginaryOne, 0, 1);
            VerifyMagnitudePhaseProperties(Complex.ImaginaryOne, 1, Math.PI / 2);
        }

        public static IEnumerable<object[]> Ctor_Double_Double_TestData()
        {
            foreach (double real in s_validDoubleValues)
            {
                foreach (double imaginary in s_validDoubleValues)
                {
                    yield return new object[] { real, imaginary };
                }
            }

            // First quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble() };
            // Third quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { RandomPositiveDouble(), RandomNegativeDouble() };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, RandomPositiveDouble() }; // Invalid real
                yield return new object[] { invalidReal, RandomNegativeDouble() }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomPositiveDouble(), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { RandomNegativeDouble(), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("Ctor_Double_Double_TestData")]
        public static void Ctor_Double_Double(double real, double imaginary)
        {
            var complex = new Complex(real, imaginary);
            VerifyRealImaginaryProperties(complex, real, imaginary);
        }

        public static IEnumerable<object[]> Abs_TestData()
        {
            yield return new object[] { 0, 0, 0 };
            yield return new object[] { 1, 0, 1 };
            yield return new object[] { 0, 1, 1 };

            // First quadrant
            double randomReal = RandomPositiveDouble();
            double randomImaginary = RandomPositiveDouble();
            yield return new object[] { randomReal, randomImaginary, Math.Sqrt(randomReal * randomReal + randomImaginary * randomImaginary) };

            // Second quadrant
            randomReal = RandomNegativeDouble();
            randomImaginary = RandomPositiveDouble();
            yield return new object[] { randomReal, randomImaginary, Math.Sqrt(randomReal * randomReal + randomImaginary * randomImaginary) };

            // Third quadrant
            randomReal = RandomNegativeDouble();
            randomImaginary = RandomNegativeDouble();
            yield return new object[] { randomReal, randomImaginary, Math.Sqrt(randomReal * randomReal + randomImaginary * randomImaginary) };

            // Fourth quadrant
            randomReal = RandomPositiveDouble();
            randomImaginary = RandomNegativeDouble();
            yield return new object[] { randomReal, randomImaginary, Math.Sqrt(randomReal * randomReal + randomImaginary * randomImaginary) };

            // Boundary values
            yield return new object[] { double.MaxValue, 0, double.MaxValue };
            yield return new object[] { double.MinValue, 0, double.MaxValue };

            yield return new object[] { 0, double.MaxValue, double.MaxValue };
            yield return new object[] { 0, double.MinValue, double.MaxValue };

            yield return new object[] { double.MaxValue, double.MaxValue, double.PositiveInfinity };
            yield return new object[] { double.MinValue, double.MinValue, double.PositiveInfinity };

            // Invalid values
            randomReal = RandomPositiveDouble();
            randomImaginary = RandomPositiveDouble();
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, randomReal, Math.Abs(invalidReal) }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { randomImaginary, invalidImaginary, Math.Abs(invalidImaginary) }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary, (double.IsInfinity(invalidReal) || double.IsInfinity(invalidImaginary)) ? double.PositiveInfinity : double.NaN }; // Invalid real, invalid imaginary
                }
            }

            yield return new object[] { double.NaN, 0, double.NaN };  // Regression test: Complex.Abs() is inconsistent on NaN / Complex
        }

        [Theory, MemberData("Abs_TestData")]
        public static void Abs(double real, double imaginary, double expected)
        {
            var complex = new Complex(real, imaginary);
            double abs = Complex.Abs(complex);

            Assert.True((abs.Equals(expected) || IsDiffTolerable(abs, expected)),
                string.Format("Abs ({0}, {1}) Actual: {2}, Expected: {3}", real, imaginary, abs, expected));

            double absNegative = Complex.Abs(-complex);
            Assert.True(absNegative.Equals(abs),
                string.Format("Abs ({0}, {1}) = {2} != Abs(-neg)={3}", real, imaginary, abs, absNegative));
        }

        public static IEnumerable<object[]> ACos_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            // First quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };

            // Boundary values
            yield return new object[] { 0, double.MaxValue };
            yield return new object[] { 0, double.MinValue };
            yield return new object[] { double.MaxValue, double.MaxValue };
            yield return new object[] { double.MinValue, double.MinValue };
        }

        [Theory, MemberData("ACos_Basic_TestData")]
        public static void ACos_Basic(double real, double imaginary)
        {
            // Formula used in the feature: arccos(z) = -iln(z + iSqrt(value*value-1))
            // Verification is done with z = ACos(Cos(z));
            var complex = new Complex(real, imaginary);
            Complex cosComplex = Complex.Cos(complex);
            Complex acosComplex = Complex.Acos(cosComplex);

            if (!real.Equals(acosComplex.Real) || !imaginary.Equals(acosComplex.Imaginary))
            {
                double realDiff = Math.Abs(Math.Abs(real) - Math.Abs(acosComplex.Real));
                double imaginaryDiff = Math.Abs(Math.Abs(imaginary) - Math.Abs(acosComplex.Imaginary));
                Assert.False((realDiff > 0.1) || (imaginaryDiff > 0.1), string.Format("({0}) != ACos(Cos():{1}):{2}", complex, cosComplex, acosComplex));
            }
        }

        public static IEnumerable<object[]> ACos_Advanced_TestData()
        {
            yield return new object[] { 1234000000, 0, 0, 21.62667394298955 }; // Real part is positive, imaginary part is 0
            yield return new object[] { 0, -1234000000, 1.5707963267948966, 21.62667394298955 }; // Imaginary part is negative

            // Boundary values
            yield return new object[] { double.MaxValue, 0, double.NaN, double.NaN };
            yield return new object[] { double.MinValue, 0, double.NaN, double.NaN };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, 1, double.NaN, double.NaN }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { 1, invalidImaginary, double.NaN, double.NaN }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary, double.NaN, double.NaN }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("ACos_Advanced_TestData")]
        public static void ACos_Advanced(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Acos(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Add_TestData()
        {
            yield return new object[] { 0, 0, RandomPositiveDouble(), RandomPositiveDouble() }; // 0 + x = x
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), 0, 0 }; // x + 0 = x

            // First quadrant, first quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble() };
            // First quadrant, second quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble() };
            // First quadrant, third quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), RandomNegativeDouble(), RandomNegativeDouble() };
            // First quadrant, fourth quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble(), RandomNegativeDouble() };
            // Second quadrant, second quadrant
            yield return new object[] { RandomNegativeDouble(), RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble() };
            // Second quadrant, third quadrant
            yield return new object[] { RandomNegativeDouble(), RandomPositiveDouble(), RandomNegativeDouble(), RandomNegativeDouble() };
            // Second quadrant, fouth quadrant
            yield return new object[] { RandomNegativeDouble(), RandomPositiveDouble(), RandomPositiveDouble(), RandomNegativeDouble() };
            // Third quadrant, third quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble(), RandomPositiveDouble(), RandomNegativeDouble() };
            // Third quadrant, fourth quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble(), RandomNegativeDouble(), RandomNegativeDouble() };
            // Fourth quadrant, fourth quadrant
            yield return new object[] { RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble(), RandomNegativeDouble() };

            // Boundary values
            yield return new object[] { double.MaxValue, double.MaxValue, RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { double.MaxValue, double.MaxValue, RandomNegativeDouble(), RandomNegativeDouble() };

            yield return new object[] { double.MinValue, double.MinValue, RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { double.MinValue, double.MinValue, RandomNegativeDouble(), RandomNegativeDouble() };

            // Invalid real
            yield return new object[] { double.PositiveInfinity, RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { double.PositiveInfinity, RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble() };

            yield return new object[] { double.NegativeInfinity, RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { double.NegativeInfinity, RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble() };

            yield return new object[] { double.NaN, RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble() };

            // Invalid imaginary
            yield return new object[] { RandomPositiveDouble(), double.PositiveInfinity, RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { RandomPositiveDouble(), double.PositiveInfinity, RandomPositiveDouble(), RandomNegativeDouble() };

            yield return new object[] { RandomPositiveDouble(), double.NegativeInfinity, RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { RandomPositiveDouble(), double.NegativeInfinity, RandomPositiveDouble(), RandomNegativeDouble() };

            yield return new object[] { RandomPositiveDouble(), double.NaN, RandomPositiveDouble(), RandomPositiveDouble() };
        }

        [Theory, MemberData("Add_TestData")]
        public static void Add(double realLeft, double imaginaryLeft, double realRight, double imaginaryRight)
        {
            var left = new Complex(realLeft, imaginaryLeft);
            var right = new Complex(realRight, imaginaryRight);

            // Calculate the expected results
            double expectedReal = realLeft + realRight;
            double expectedImaginary = imaginaryLeft + imaginaryRight;

            // Operator
            Complex result = left + right;
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);

            // Static method
            result = Complex.Add(left, right);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> ASin_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            // First quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
        }

        [Theory, MemberData("ASin_Basic_TestData")]
        public static void ASin_Basic(double real, double imaginary)
        {
            // Formula used in the feature: arcsin(z) = -iln(iz + Sqrt(1-z*z))
            // Verification is done with z = ASin(Sin(z));
            var complex = new Complex(real, imaginary);
            Complex sinComplex = Complex.Sin(complex);
            Complex result = Complex.Asin(sinComplex);
            VerifyRealImaginaryProperties(result, real, imaginary);
        }

        public static IEnumerable<object[]> ASin_Advanced_TestData()
        {
            yield return new object[] { -1234000000, 0, -1.5707963267948966, 21.62667394298955 }; // Real part is negative, imaginary part is 0
            yield return new object[] { 0, 1234000000, 0, 21.62667394298955 }; // Imaginary part is positive

            // Boundary values
            yield return new object[] { double.MaxValue, 0, double.NaN, double.NaN };
            yield return new object[] { double.MinValue, 0, double.NaN, double.NaN };
            yield return new object[] { 0, double.MaxValue, double.NaN, double.NaN };
            yield return new object[] { 0, double.MinValue, double.NaN, double.NaN };
            yield return new object[] { double.MaxValue, double.MaxValue, double.NaN, double.NaN };
            yield return new object[] { double.MinValue, double.MinValue, double.NaN, double.NaN };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, 1, double.NaN, double.NaN }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { 1, invalidImaginary, double.NaN, double.NaN }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary, double.NaN, double.NaN }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("ASin_Advanced_TestData")]
        public static void ASin_Advanced(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Asin(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> ATan_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 }; // Undefined - NaN should be propagated
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 }; // Undefined - NaN should be propagated

            // First quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
        }

        [Theory, MemberData("ATan_Basic_TestData")]
        public static void ATan_Basic(double real, double imaginary)
        {
            // Formula used in the feature: Atan(z) = (i/2) * (log(1-iz) - log(1+iz))
            // Verification is done with z = ATan(Tan(z));
            var complex = new Complex(real, imaginary);
            Complex tanComplex = Complex.Tan(complex);
            Complex atanComplex = Complex.Atan(tanComplex);
            VerifyRealImaginaryProperties(atanComplex, real, imaginary);
        }

        public static IEnumerable<object[]> ATan_Advanced_TestData()
        {
            // Boundary values
            yield return new object[] { double.MaxValue, 0, Math.PI / 2, 0 };
            yield return new object[] { double.MinValue, 0, -Math.PI / 2, 0 };
            yield return new object[] { 0, double.MaxValue, Math.PI / 2, 0 };
            yield return new object[] { 0, double.MinValue, -Math.PI / 2, 0 };
            yield return new object[] { double.MaxValue, double.MaxValue, double.NaN, double.NaN };
            yield return new object[] { double.MinValue, double.MinValue, double.NaN, double.NaN };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, 1, double.NaN, double.NaN }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { 1, invalidImaginary, double.NaN, double.NaN }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary, double.NaN, double.NaN }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("ATan_Advanced_TestData")]
        public static void ATan_Advanced(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Atan(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Conjugate_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, -1 };

            // First quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { RandomNegativeDouble(), RandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { RandomPositiveDouble(), RandomNegativeDouble() };

            // First quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };

            // Boundary values
            yield return new object[] { double.MaxValue, 0 };
            yield return new object[] { double.MinValue, 0 };

            yield return new object[] { 0, double.MaxValue };
            yield return new object[] { 0, double.MinValue };

            yield return new object[] { double.MaxValue, double.MaxValue };
            yield return new object[] { double.MinValue, double.MinValue };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, RandomPositiveDouble() }; // Invalid real
                yield return new object[] { invalidReal, RandomNegativeDouble() }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomPositiveDouble(), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { RandomNegativeDouble(), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary };
                }
            }
        }

        [Theory, MemberData("Conjugate_TestData")]
        public static void Conjugate(double real, double imaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Conjugate(complex);

            VerifyRealImaginaryProperties(result, real, -imaginary);
            VerifyMagnitudePhaseProperties(result, complex.Magnitude, -complex.Phase);
        }

        public static IEnumerable<object[]> Cos_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            // First quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
        }

        [Theory, MemberData("Cos_Basic_TestData")]
        public static void Cos_Basic(double real, double imaginary)
        {
            // The product formula: cos (x+iy) = cos(x)*cosh(y) - isin(x)sinh(y)
            // The verification formula: cos (z) = (Complex.Exp(i*z) + Complex.Exp(-i*z)) / 2
            // The verification formula is used not for the boundary values
            var complex = new Complex(real, imaginary);
            Complex temp = Complex.ImaginaryOne * complex;
            Complex expected = (Complex.Exp(temp) + Complex.Exp(-temp)) / 2;
            Cos_Advanced(real, imaginary, expected.Real, expected.Imaginary);
        }

        public static IEnumerable<object[]> Cos_Advanced_TestData()
        {
            // Boundary values
            yield return new object[] { double.MaxValue, 0, Math.Cos(double.MaxValue), 0 };
            yield return new object[] { double.MinValue, 0, Math.Cos(double.MinValue), 0 };

            yield return new object[] { 0, double.MaxValue, double.PositiveInfinity, double.NaN };
            yield return new object[] { 0, double.MinValue, double.PositiveInfinity, double.NaN };

            yield return new object[] { double.MaxValue, double.MaxValue, Math.Cos(double.MaxValue) * double.PositiveInfinity, double.NegativeInfinity };
            yield return new object[] { double.MinValue, double.MinValue, double.NegativeInfinity, double.NegativeInfinity };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, 1, double.NaN, double.NaN }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    if (double.IsPositiveInfinity(invalidImaginary))
                    {
                        yield return new object[] { 1, invalidImaginary, double.PositiveInfinity, double.NegativeInfinity }; // Invalid imaginary
                    }
                    else if (double.IsNegativeInfinity(invalidImaginary))
                    {
                        yield return new object[] { 1, invalidImaginary, double.PositiveInfinity, double.PositiveInfinity }; // Invalid imaginary
                    }
                    else
                    {
                        yield return new object[] { 1, invalidImaginary, double.NaN, double.NaN }; // Invalid imaginary
                    }
                    yield return new object[] { invalidReal, invalidImaginary, double.NaN, double.NaN }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("Cos_Advanced_TestData")]
        public static void Cos_Advanced(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Cos(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Cosh_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            // First quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
        }

        [Theory, MemberData("Cosh_Basic_TestData")]
        public static void Cosh_Basic(double real, double imaginary)
        {
            // The product formula: cosh (x+iy) = cosh(x)*cos(y) + isinh(x)*sin(y) 
            // The verification formula: Cosh (z) = (Exp(z) + Exp(-z))/2
            // The verification formula is used not for the boundary values
            var complex = new Complex(real, imaginary);
            Complex expected = 0.5 * (Complex.Exp(complex) + Complex.Exp(-complex));
            Cosh_Advanced(real, imaginary, expected.Real, expected.Imaginary);
        }

        public static IEnumerable<object[]> Cosh_Advanced_TestData()
        {
            // Boundary values
            yield return new object[] { double.MaxValue, 0, double.PositiveInfinity, double.NaN };
            yield return new object[] { double.MinValue, 0, double.PositiveInfinity, double.NaN };

            yield return new object[] { 0, double.MaxValue, Math.Cos(double.MaxValue), 0 };
            yield return new object[] { 0, double.MinValue, Math.Cos(double.MinValue), 0 };

            yield return new object[] { double.MaxValue, double.MaxValue, Math.Cos(double.MaxValue) * double.PositiveInfinity, double.PositiveInfinity };
            yield return new object[] { double.MinValue, double.MinValue, double.NegativeInfinity, double.PositiveInfinity };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                if (double.IsInfinity(invalidReal))
                {
                    yield return new object[] { invalidReal, 1, double.PositiveInfinity, invalidReal }; // Invalid real
                }
                else
                {
                    yield return new object[] { invalidReal, 1, double.NaN, double.NaN }; // Invalid real
                }
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { 1, invalidImaginary, double.NaN, double.NaN }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary, double.NaN, double.NaN }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("Cosh_Advanced_TestData")]
        public static void Cosh_Advanced(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Cosh(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Divide_TestData()
        {
            yield return new object[] { 0, 0, 10, 50 }; // 0 / x = 0
            yield return new object[] { 10, 50, double.NaN, double.NaN }; // 0 / x = NaN
            yield return new object[] { 10, 50, 0, 0 }; // x / 0 = 0

            yield return new object[] { 1, 0, 10, 50 }; // 1 / x = x
            yield return new object[] { 10, 50, 1, 0 }; // x / 1 = x

            yield return new object[] { 0, 1, 0, 1 }; // i / i = 1
            yield return new object[] { 0, 1, 10, 50 }; // i / x
            yield return new object[] { 10, 50, 0, 1 }; // x / i

            // First quadrant, first quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // First quadrant, second quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // First quadrant, third quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // First quadrant, fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
            // Second quadrant, second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Second quadrant, third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Second quadrant, fouth quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
            // Third quadrant, third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
            // Third quadrant, fourth quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble(), SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant, fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };

            // Boundary values
            yield return new object[] { double.MaxValue, double.MaxValue, SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            yield return new object[] { double.MinValue, double.MinValue, SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };

            // Invalid real values
            yield return new object[] { double.PositiveInfinity, SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            yield return new object[] { double.PositiveInfinity, SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };

            yield return new object[] { double.NegativeInfinity, SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            yield return new object[] { double.NegativeInfinity, SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };

            yield return new object[] { double.NaN, SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };

            // Invalid imaginary values
            yield return new object[] { SmallRandomPositiveDouble(), double.PositiveInfinity, SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            yield return new object[] { SmallRandomPositiveDouble(), double.PositiveInfinity, SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };

            yield return new object[] { SmallRandomPositiveDouble(), double.NegativeInfinity, SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            yield return new object[] { SmallRandomPositiveDouble(), double.NegativeInfinity, SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };

            yield return new object[] { SmallRandomPositiveDouble(), double.NaN, SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
        }

        [Theory, MemberData("Divide_TestData")]
        public static void Divide(double realLeft, double imaginaryLeft, double realRight, double imaginaryRight)
        {
            var dividend = new Complex(realLeft, imaginaryLeft);
            var divisor = new Complex(realRight, imaginaryRight);

            Complex expected = dividend * Complex.Conjugate(divisor);
            double expectedReal = expected.Real;
            double expectedImaginary = expected.Imaginary;

            if (!double.IsInfinity(expectedReal))
            {
                expectedReal = expectedReal / (divisor.Magnitude * divisor.Magnitude);
            }
            if (!double.IsInfinity(expectedImaginary))
            {
                expectedImaginary = expectedImaginary / (divisor.Magnitude * divisor.Magnitude);
            }

            // Operator
            Complex result = dividend / divisor;
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);

            // Static method
            result = Complex.Divide(dividend, divisor);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        [Fact]
        public static void Equals()
        {
            // This is not InlineData, to workaround a niche bug, that mainly occurs on non Windows platforms.
            // This bug moves test values around to different intermediate memory locations, causing true assertions to be false.
            // Moving these methods into a method, not an iterator fixes this.
            Equals(Complex.Zero, Complex.Zero, true, true);
            Equals(Complex.Zero, Complex.One, false, false);
            Equals(Complex.Zero, -Complex.One, false, false);
            Equals(Complex.Zero, Complex.ImaginaryOne, false, false);
            Equals(Complex.Zero, -Complex.ImaginaryOne, false, false);

            Equals(Complex.One, Complex.One, true, true);
            Equals(Complex.One, -Complex.One, false, false);
            Equals(Complex.One, Complex.ImaginaryOne, false, false);
            Equals(Complex.One, -Complex.ImaginaryOne, false, false);

            Equals(-Complex.One, -Complex.One, true, true);
            Equals(-Complex.One, Complex.ImaginaryOne, false, false);
            Equals(-Complex.One, -Complex.ImaginaryOne, false, false);

            Equals(Complex.ImaginaryOne, Complex.ImaginaryOne, true, true);
            Equals(Complex.ImaginaryOne, -Complex.ImaginaryOne, false, false);

            Equals(-Complex.ImaginaryOne, -Complex.ImaginaryOne, true, true);

            Equals(Complex.Zero, new Complex(0, 0), true, true);
            Equals(Complex.Zero, new Complex(1, 0), false, false);
            Equals(Complex.Zero, new Complex(0, 1), false, false);

            Equals(Complex.One, new Complex(1, 0), true, true);
            Equals(Complex.One, new Complex(1, 1), false, false);
            Equals(Complex.One, new Complex(0, 1), false, false);

            Equals(-Complex.One, new Complex(-1, 0), true, true);
            Equals(-Complex.One, new Complex(-1, -1), false, false);
            Equals(-Complex.One, new Complex(0, -1), false, false);

            Equals(Complex.ImaginaryOne, new Complex(0, 1), true, true);
            Equals(Complex.ImaginaryOne, new Complex(1, 1), false, false);
            Equals(Complex.ImaginaryOne, new Complex(0, -1), false, false);

            Equals(-Complex.ImaginaryOne, new Complex(0, -1), true, true);
            Equals(-Complex.ImaginaryOne, new Complex(-1, -1), false, false);
            Equals(-Complex.ImaginaryOne, new Complex(0, 1), false, false);

            Equals(new Complex(0.5, 0.5), new Complex(0.5, 0.5), true, true);
            Equals(new Complex(0.5, 0.5), new Complex(0.5, 1.5), false, false);
            Equals(new Complex(0.5, 0.5), new Complex(1.5, 0.5), false, false);

            // Boundary values
            Complex maxMax = new Complex(double.MaxValue, double.MaxValue);
            Complex maxMin = new Complex(double.MaxValue, double.MinValue);
            Complex minMax = new Complex(double.MinValue, double.MaxValue);
            Complex minMin = new Complex(double.MinValue, double.MinValue);

            Equals(maxMax, maxMax, true, true);
            Equals(maxMax, maxMin, false, false);
            Equals(maxMax, minMax, false, false);
            Equals(maxMax, minMin, false, false);
            Equals(maxMax, new Complex(1, 2), false, false);

            Equals(maxMin, maxMin, true, true);
            Equals(maxMin, minMax, false, false);
            Equals(maxMin, minMin, false, false);
            Equals(maxMin, new Complex(1, 2), false, false);

            Equals(minMax, minMax, true, true);
            Equals(minMax, minMin, false, false);
            Equals(minMax, new Complex(1, 2), false, false);

            Equals(minMin, minMin, true, true);
            Equals(minMin, new Complex(1, 2), false, false);

            Equals(new Complex(100.5, 0), 100.5, false, false);
            Equals(new Complex(0, 100.5), 100.5, false, false);
            Equals(new Complex(100.5, 0), 0, false, false);
            Equals(new Complex(0, 100.5), 0, false, false);
            Equals(new Complex(0, 100.5), "0", false, false);
            Equals(new Complex(0, 100.5), null, false, false);

            // Invalid values
            Complex invalidComplex;
            var complex = new Complex(2, 3);
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                invalidComplex = new Complex(invalidReal, 1);
                Equals(invalidComplex, complex, false, false);
                Equals(invalidComplex, invalidComplex, !double.IsNaN(invalidReal), true); // Handle double.NaN != double.NaN
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    invalidComplex = new Complex(1, invalidImaginary);
                    Equals(invalidComplex, complex, false, false);
                    Equals(invalidComplex, invalidComplex, !double.IsNaN(invalidImaginary), true); // Handle double.NaN != double.NaN

                    invalidComplex = new Complex(invalidReal, invalidImaginary);
                    Equals(invalidComplex, complex, false, false);
                    Equals(invalidComplex, invalidComplex, !double.IsNaN(invalidReal) && !double.IsNaN(invalidImaginary), true); // Handle double.NaN != double.NaN
                }
            }
        }
        
        private static void Equals(Complex complex1, object obj, bool expected, bool expectedEquals)
        {
            if (obj is Complex)
            {
                Complex complex2 = (Complex)obj;
                Assert.True(expected == (complex1 == complex2), string.Format("c1:{0} == c2{1} is not '{2}' as expected", complex1, complex2, expected));
                Assert.True(expected == (complex2 == complex1), string.Format("c2:{0} == c1{1} is not '{2}' as expected", complex2, complex1, expected));

                Assert.False(expected == (complex1 != complex2), string.Format("c1:{0} != c2{1} is not '{2}' as expected", complex1, complex2, !expected));
                Assert.False(expected == (complex2 != complex1), string.Format("c2:{0} != c1{1} is not '{2}' as expected", complex2, complex1, !expected));
                
                Assert.True(expectedEquals == complex1.Equals(complex2), string.Format("{0}.Equals({1}) is not '{2}' as expected", complex1, complex2, expectedEquals));
                Assert.True(expectedEquals == complex2.Equals(complex1), string.Format("{0}.Equals({1}) is not '{2}' as expected", complex2, complex1, expectedEquals));

                Assert.True(expectedEquals == complex1.GetHashCode().Equals(complex2.GetHashCode()), 
                    string.Format("{0}.GetHashCode().Equals({1}.GetHashCode()) is not '{2}' as expected", complex1, complex2, expectedEquals));
            }
            Assert.True(expected == complex1.Equals(obj), string.Format("{0}.Equals({1}) is not '{2}' as expected", complex1, obj, expectedEquals));
        }

        public static IEnumerable<object[]> Exp_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            // First quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };

            // Boundary values
            yield return new object[] { double.MinValue, 0 };

            yield return new object[] { 0, double.MaxValue };
            yield return new object[] { 0, double.MinValue };

            yield return new object[] { double.MaxValue, double.MaxValue };
            yield return new object[] { double.MinValue, double.MinValue };
        }

        [Theory, MemberData("Exp_TestData")]
        public static void Exp(double real, double imaginary)
        {
            Complex expected;
            // Special case the complex {double.MaxValue, double.MaxValue)
            if (real == double.MaxValue && imaginary == double.MaxValue)
            {
                expected = new Complex(Math.Cos(double.MaxValue) * double.PositiveInfinity, double.PositiveInfinity);
            }
            else
            {
                // Verify with e(x+y) = e(x)*e(y) if xy == yx
                var realComplex = new Complex(real, 0);
                var imaginaryComplex = new Complex(0, imaginary);

                Complex ri = realComplex * imaginaryComplex;
                Complex ir = imaginaryComplex * realComplex;
                if (!ri.Equals(ir))
                {
                    return;
                }

                Complex realExponential = Complex.Exp(realComplex);
                Complex imaginaryExpontential = Complex.Exp(imaginaryComplex);
                expected = realExponential * imaginaryExpontential;
            }

            var complex = new Complex(real, imaginary);
            Complex result = Complex.Exp(complex);
            VerifyRealImaginaryProperties(result, expected.Real, expected.Imaginary);
        }

        [ActiveIssue(6022, PlatformID.AnyUnix)]
        [Fact]
        public static void Exp_MaxReal()
        {
            // On Windows, the result is {double.PostiveInfinity, double.NaN}
            // On Unix, the result is {double.NegativeInfinity, double.NaN}
            // Which one is incorrect should be determined.
            var complex = new Complex(double.MaxValue, 0);
            Complex result = Complex.Exp(complex);
            VerifyRealImaginaryProperties(result, double.PositiveInfinity, double.NaN);
        }

        public static IEnumerable<object[]> FromPolarCoordinates_TestData()
        {
            foreach (double magnitude in s_validDoubleValues)
            {
                foreach (double phase in s_phaseTypicalValues)
                {
                    yield return new object[] { magnitude, phase };
                }
            }

            // First quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositivePhase() };
            // Second quadrant
            yield return new object[] { RandomNegativeDouble(), RandomPositivePhase() };
            // Third quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativePhase() };
            // Fourth quadrant
            yield return new object[] { RandomPositiveDouble(), RandomNegativePhase() };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, RandomPositiveDouble() }; // Invalid real
                yield return new object[] { invalidReal, RandomNegativeDouble() }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomPositiveDouble(), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { RandomNegativeDouble(), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("FromPolarCoordinates_TestData")]
        public static void FromPolarCoordinates(double magnitude, double phase)
        {
            Complex complex = Complex.FromPolarCoordinates(magnitude, phase);

            // double.IsNaN(magnitude) is checked in the verification method.
            if (double.IsNaN(phase) || double.IsInfinity(phase))
            {
                magnitude = double.NaN;
                phase = double.NaN;
            }
            // Special check in Complex.Abs method
            else if (double.IsInfinity(magnitude))
            {
                magnitude = double.PositiveInfinity;
                phase = double.NaN;
            }

            VerifyMagnitudePhaseProperties(complex, magnitude, phase);

            complex = new Complex(complex.Real, complex.Imaginary);
            VerifyMagnitudePhaseProperties(complex, magnitude, phase);
        }

        [Fact]
        public static void Log()
        {
            // This is not InlineData, to workaround a niche bug, that mainly occurs on non Windows platforms.
            // This bug moves test values around to different intermediate memory locations, causing true assertions to be false.
            // Moving these methods into a method, not an iterator fixes this.
            Log(Complex.One, Complex.Zero, true);
            Log(Complex.One, Complex.ImaginaryOne, true);

            var positivePositiveComplex = new Complex(SmallRandomPositiveDouble(), SmallRandomPositiveDouble());
            var positiveNegativeComplex = new Complex(SmallRandomPositiveDouble(), SmallRandomNegativeDouble());
            var negativePositiveComplex = new Complex(SmallRandomNegativeDouble(), SmallRandomPositiveDouble());
            var negativeNegativeComplex = new Complex(SmallRandomNegativeDouble(), SmallRandomNegativeDouble());

            // First quadrant, first quadrant
            Log(positivePositiveComplex, positivePositiveComplex, true);
            // First quadrant, second quadrant
            Log(positivePositiveComplex, negativePositiveComplex, true);
            // First quadrant, third quadrant
            Log(positivePositiveComplex, negativeNegativeComplex, true);
            // First quadrant, fourth quadrant
            Log(positivePositiveComplex, positiveNegativeComplex, true);
            // Second quadrant, second quadrant
            Log(negativePositiveComplex, negativePositiveComplex, true);
            // Second quadrant, third quadrant
            Log(negativePositiveComplex, negativeNegativeComplex, true);
            // Second quadrant, fourth quadrant
            Log(negativePositiveComplex, positiveNegativeComplex, true);
            // Third quadrant, third quadrant
            Log(negativeNegativeComplex, negativeNegativeComplex, true);
            // Third quadrant, fourth quadrant
            Log(negativeNegativeComplex, positiveNegativeComplex, true);
            // Fourth quadrant, fouth quadrant
            Log(positiveNegativeComplex, positiveNegativeComplex, true);

            // Boundary values
            Log(new Complex(double.MaxValue, 0), Complex.One, false);
            Log(new Complex(double.MinValue, 0), Complex.One, false);
            Log(new Complex(0, double.MaxValue), Complex.ImaginaryOne, false);
            Log(new Complex(0, double.MinValue), Complex.ImaginaryOne, false);
        }
        
        private static void Log(Complex complex1, Complex complex2, bool extended)
        {
            if (complex1 == Complex.Zero)
            {
                return;
            }

            VerifyLog10(complex1);
            VerifyLogWithBase(complex1);
            if (extended)
            {
                VerifyLogWithMultiply(complex1, complex2);
                VerifyLogWithPowerMinusOne(complex1);
                VerifyLogWithExp(complex1);
            }
        }

        [Fact]
        public static void Log_Zero()
        {
            Complex result = Complex.Log(Complex.Zero);
            VerifyRealImaginaryProperties(result, double.NegativeInfinity, 0);

            result = Complex.Log10(Complex.Zero);
            VerifyRealImaginaryProperties(result, double.NegativeInfinity, 0);

            result = Complex.Log(Complex.Zero, RandomPositiveDouble());
            VerifyRealImaginaryProperties(result, double.NegativeInfinity, double.NaN);
        }

        private static void VerifyLog10(Complex complex)
        {
            Complex logValue = Complex.Log10(complex);
            Complex logComplex = Complex.Log(complex);

            double baseLog = Math.Log(10);
            Complex expected = logComplex / baseLog;
            VerifyRealImaginaryProperties(logValue, expected.Real, expected.Imaginary);
        }

        private static void VerifyLogWithBase(Complex complex)
        {
            // Verify with Random Int32
            double baseValue = s_random.Next(1, int.MaxValue);
            Complex logValue = Complex.Log(complex, baseValue);

            Complex logComplex = Complex.Log(complex);
            double baseLog = Math.Log(baseValue);
            Complex expected = logComplex / baseLog;
            VerifyRealImaginaryProperties(logValue, expected.Real, expected.Imaginary);

            // Verify with Random double value
            baseValue = 0.0;
            while (baseValue == 0)
            {
                baseValue = RandomPositiveDouble();
            }

            logValue = Complex.Log(complex, baseValue);
            logComplex = Complex.Log(complex);
            baseLog = Math.Log(baseValue);
            expected = logComplex / baseLog;
            VerifyRealImaginaryProperties(logValue, expected.Real, expected.Imaginary);
        }

        private static void VerifyLogWithMultiply(Complex complex1, Complex complex2)
        {
            // Log(complex1 * complex2) == Log(complex1) + Log(complex2), if -PI < Arctan(complex1) + Arctan(complex2) <= PI
            double equalityCondition = Math.Atan2(complex1.Imaginary, complex1.Real) + Math.Atan2(complex2.Imaginary, complex2.Real);
            if (equalityCondition <= -Math.PI || equalityCondition > Math.PI)
            {
                return;
            }

            Complex logComplex = Complex.Log(complex1 * complex2);
            Complex expected = Complex.Log(complex1) + Complex.Log(complex2);
            VerifyRealImaginaryProperties(logComplex, expected.Real, expected.Imaginary);
        }

        private static void VerifyLogWithPowerMinusOne(Complex complex)
        {
            // Log(complex) == -Log(1 / complex)
            Complex logComplex = Complex.Log(complex);
            Complex logPowerMinusOne = Complex.Log(1 / complex);

            VerifyRealImaginaryProperties(logComplex, -logPowerMinusOne.Real, -logPowerMinusOne.Imaginary);
        }

        private static void VerifyLogWithExp(Complex complex)
        {
            // Exp(log(complex)) == complex, if complex != Zero
            Complex logComplex = Complex.Log(complex);
            Complex expLogComplex = Complex.Exp(logComplex);

            VerifyRealImaginaryProperties(expLogComplex, complex.Real, complex.Imaginary);
        }

        public static IEnumerable<object[]> Multiply_TestData()
        {
            yield return new object[] { 0, 0, 0, 0 }; // 0 * 0 = 0
            yield return new object[] { 0, 0, RandomPositiveDouble(), RandomPositivePhase() }; // 0 * x = 0
            yield return new object[] { RandomPositiveDouble(), RandomPositivePhase(), 0, 0, }; // x * 0 = 0

            yield return new object[] { 1, 0, 1, 0 }; // 1 * 1 = 0
            yield return new object[] { 1, 0, RandomPositiveDouble(), RandomPositivePhase() }; // 1 * x = x
            yield return new object[] { RandomPositiveDouble(), RandomPositivePhase(), 1, 0 }; // x * 1 = x

            yield return new object[] { 0, 1, 0, 1 }; // i * x
            yield return new object[] { 0, 1, RandomPositiveDouble(), RandomPositivePhase() }; // i * x
            yield return new object[] { RandomPositiveDouble(), RandomPositivePhase(), 0, 1 }; // x * i

            // First quadrant, first quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // First quadrant, second quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // First quadrant, third quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // First quadrant, fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
            // Second quadrant, second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Second quadrant, third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Second quadrant, fouth quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
            // Third quadrant, third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
            // Third quadrant, fourth quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble(), SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant, fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble(), SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };

            // Boundary values
            yield return new object[] { double.MaxValue, double.MaxValue, SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            yield return new object[] { double.MinValue, double.MinValue, SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };

            // Invalid real
            yield return new object[] { double.PositiveInfinity, RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { double.PositiveInfinity, RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble() };

            yield return new object[] { double.NegativeInfinity, RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { double.NegativeInfinity, RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble() };

            yield return new object[] { double.NaN, RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble() };

            // Invalid imaginary
            yield return new object[] { RandomPositiveDouble(), double.PositiveInfinity, RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { RandomPositiveDouble(), double.PositiveInfinity, RandomPositiveDouble(), RandomNegativeDouble() };

            yield return new object[] { RandomPositiveDouble(), double.NegativeInfinity, RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { RandomPositiveDouble(), double.NegativeInfinity, RandomPositiveDouble(), RandomNegativeDouble() };

            yield return new object[] { RandomPositiveDouble(), double.NaN, RandomPositiveDouble(), RandomPositiveDouble() };
        }

        [Theory, MemberData("Multiply_TestData")]
        public static void Multiply(double realLeft, double imaginaryLeft, double realRight, double imaginaryRight)
        {
            var left = new Complex(realLeft, imaginaryLeft);
            var right = new Complex(realRight, imaginaryRight);

            double expectedReal = realLeft * realRight - imaginaryLeft * imaginaryRight;
            double expectedImaginary = realLeft * imaginaryRight + imaginaryLeft * realRight;

            // Operator
            Complex result = left * right;
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);

            // Static method
            result = Complex.Multiply(left, right);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Negate_TestData()
        {
            foreach (double real in s_validDoubleValues)
            {
                foreach (double imaginary in s_validDoubleValues)
                {
                    yield return new object[] { real, imaginary };
                }
            }

            // First quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble() };
            // Third quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { RandomPositiveDouble(), RandomNegativeDouble() };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, RandomPositiveDouble() }; // Invalid real
                yield return new object[] { invalidReal, RandomNegativeDouble() }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomPositiveDouble(), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { RandomNegativeDouble(), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("Negate_TestData")]
        public static void Negate(double real, double imaginary)
        {
            var complex = new Complex(real, imaginary);

            Complex result = -complex;
            VerifyRealImaginaryProperties(result, -complex.Real, -complex.Imaginary);

            result = Complex.Negate(complex);
            VerifyRealImaginaryProperties(result, -complex.Real, -complex.Imaginary);
        }

        public static IEnumerable<object[]> Pow_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            // First quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble() };
            // Third quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { RandomPositiveDouble(), RandomNegativeDouble() };

            // Boundary values
            yield return new object[] { double.MaxValue, double.MaxValue };
            yield return new object[] { double.MinValue, double.MinValue };
        }

        [Theory, MemberData("Pow_TestData")]
        private static void Pow(double real, double imaginary)
        {
            Pow_Complex_Double(real, imaginary);
            Pow_Complex_Complex(real, imaginary);
        }

        private static void Pow_Complex_Double(double real, double imaginary)
        {
            VerifyPow_Complex_Double(real, imaginary, 0);
            VerifyPow_Complex_Double(real, imaginary, 1);

            VerifyPow_Complex_Double(real, imaginary, SmallRandomPositiveDouble()); // Positive power
            VerifyPow_Complex_Double(real, imaginary, SmallRandomNegativeDouble()); // Negative power

            foreach (double invalidPower in s_invalidDoubleValues)
            {
                VerifyPow_Complex_Double(real, imaginary, invalidPower);
            }
        }

        private static void VerifyPow_Complex_Double(double realValue, double imaginaryValue, double power)
        {
            var value = new Complex(realValue, imaginaryValue);
            Complex result = Complex.Pow(value, power);

            double expectedReal = 0;
            double expectedImaginary = 0;
            if (power == 0)
            {
                expectedReal = 1;
            }
            else if (realValue != 0 || imaginaryValue != 0)
            {
                // Pow(x,y) = Exp(ylog(x))
                Complex realComplex = new Complex(power, 0);
                Complex expected = Complex.Exp(realComplex * Complex.Log(value));
                expectedReal = expected.Real;
                expectedImaginary = expected.Imaginary;
            }
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        private static void Pow_Complex_Complex(double real, double imaginary)
        {
            VerifyPow_Complex_Complex(real, imaginary, 0, 0);
            VerifyPow_Complex_Complex(real, imaginary, 1, 0);
            VerifyPow_Complex_Complex(real, imaginary, 0, 1);
            VerifyPow_Complex_Complex(real, imaginary, 0, -1);

            // First quadrant
            VerifyPow_Complex_Complex(real, imaginary, SmallRandomPositiveDouble(), SmallRandomPositiveDouble());
            // Second quadrant
            VerifyPow_Complex_Complex(real, imaginary, SmallRandomNegativeDouble(), SmallRandomPositiveDouble());
            // Third quadrant
            VerifyPow_Complex_Complex(real, imaginary, SmallRandomNegativeDouble(), SmallRandomNegativeDouble());
            // Fourth quadrant
            VerifyPow_Complex_Complex(real, imaginary, SmallRandomPositiveDouble(), SmallRandomNegativeDouble());
        }

        private static void VerifyPow_Complex_Complex(double realValue, double imaginaryValue, double realPower, double imaginaryPower)
        {
            var value = new Complex(realValue, imaginaryValue);
            var power = new Complex(realPower, imaginaryPower);
            Complex result = Complex.Pow(value, power);

            double expectedReal = 0;
            double expectedImaginary = 0;
            if (realPower == 0 && imaginaryPower == 0)
            {
                expectedReal = 1;
            }
            else if (realValue != 0 || imaginaryValue != 0)
            {
                // Pow(x,y) = Exp(ylog(x))
                Complex expected = Complex.Exp(power * Complex.Log(value));
                expectedReal = expected.Real;
                expectedImaginary = expected.Imaginary;
            }
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Reciprocal_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, -1 };

            // First quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };

            // Boundary values
            yield return new object[] { double.MaxValue, 0 };
            yield return new object[] { double.MinValue, 0 };

            yield return new object[] { 0, double.MaxValue };
            yield return new object[] { 0, double.MinValue };

            yield return new object[] { double.MaxValue, double.MaxValue };
            yield return new object[] { double.MinValue, double.MinValue };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, RandomPositiveDouble() }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomPositiveDouble(), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary };
                }
            }
        }

        [Theory, MemberData("Reciprocal_TestData")]
        public static void Reciprocal(double real, double imaginary)
        {
            var complex = new Complex(real, imaginary);
            var result = Complex.Reciprocal(complex);

            Complex expected = Complex.Zero;
            if (Complex.Zero != complex &&
                !(double.IsInfinity(real) && !(double.IsInfinity(imaginary) || double.IsNaN(imaginary))) &&
                !(double.IsInfinity(imaginary) && !(double.IsInfinity(real) || double.IsNaN(real))))
            {
                double magnitude = complex.Magnitude;
                expected = Complex.Conjugate(complex) / magnitude; // In order to avoid Infinity = magnitude* magnitude
                expected /= magnitude;
            }

            VerifyRealImaginaryProperties(result, expected.Real, expected.Imaginary);
        }

        public static IEnumerable<object[]> Sin_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            // First quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
        }

        [Theory, MemberData("Sin_Basic_TestData")]
        public static void Sin_Basic(double real, double imaginary)
        {
            // The product formula: sin (x+iy) = sin(x)*cosh(y) + icos(x)sinh(y)
            // The verification formula: sin (z) = (Complex.Exp(i*z) - Complex.Exp(-i*z)) / (2*i)
            // The verification formula is used not for the boundary values
            Complex z = new Complex(real, imaginary);
            Complex temp = Complex.ImaginaryOne * z;
            Complex expectedComplex = (Complex.Exp(temp) - Complex.Exp(-temp)) / (2 * Complex.ImaginaryOne);
            Sin_Advanced(real, imaginary, expectedComplex.Real, expectedComplex.Imaginary);
        }

        public static IEnumerable<object[]> Sin_Advanced_TestData()
        {
            yield return new object[] { double.MaxValue, 0, Math.Sin(double.MaxValue), 0 };
            yield return new object[] { double.MinValue, 0, Math.Sin(double.MinValue), 0 };

            yield return new object[] { 0, double.MaxValue, double.NaN, double.PositiveInfinity };
            yield return new object[] { 0, double.MinValue, double.NaN, double.NegativeInfinity };

            yield return new object[] { double.MaxValue, double.MaxValue, double.PositiveInfinity, Math.Cos(double.MaxValue) * double.PositiveInfinity };
            yield return new object[] { double.MinValue, double.MinValue, double.NegativeInfinity, double.PositiveInfinity };

            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, 1, double.NaN, double.NaN }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    if (double.IsInfinity(invalidImaginary))
                    {
                        yield return new object[] { 1, invalidImaginary, double.PositiveInfinity, invalidImaginary }; // Invalid imaginary
                    }
                    else
                    {
                        yield return new object[] { 1, invalidImaginary, double.NaN, double.NaN }; // Invalid imaginary
                    }
                    yield return new object[] { invalidReal, invalidImaginary, double.NaN, double.NaN }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("Sin_Advanced_TestData")]
        public static void Sin_Advanced(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Sin(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Sinh_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() }; // Positive, positive
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() }; // Positive, negative
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() }; // Negative, positive
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() }; // Negative, Negative
        }

        [Theory, MemberData("Sinh_Basic_TestData")]
        public static void Sinh_Basic(double real, double imaginary)
        {
            // The product formula: sinh (x+iy) = sinh(x)*cos(y) + icosh(x)*sin(y)
            // The verification formula: sinh (z) = (Exp(z) - Exp(-z))/2
            // The verification formula is used not for the boundary values
            var complex = new Complex(real, imaginary);
            Complex expectedComplex = 0.5 * (Complex.Exp(complex) - Complex.Exp(-complex));
            Sinh_Advanced(real, imaginary, expectedComplex.Real, expectedComplex.Imaginary);
        }

        public static IEnumerable<object[]> Sinh_Advanced_TestData()
        {
            // Boundary values
            yield return new object[] { double.MaxValue, 0, double.PositiveInfinity, double.NaN };
            yield return new object[] { double.MinValue, 0, double.NegativeInfinity, double.NaN };

            yield return new object[] { 0, double.MaxValue, 0, Math.Sin(double.MaxValue) };
            yield return new object[] { 0, double.MinValue, 0, Math.Sin(double.MinValue) };

            yield return new object[] { double.MaxValue, double.MaxValue, Math.Cos(double.MaxValue) * double.PositiveInfinity, double.PositiveInfinity };
            yield return new object[] { double.MinValue, double.MinValue, double.PositiveInfinity, double.NegativeInfinity };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                if (double.IsInfinity(invalidReal))
                {
                    yield return new object[] { invalidReal, 1, invalidReal, double.PositiveInfinity }; // Invalid real
                }
                else
                {
                    yield return new object[] { invalidReal, 1, double.NaN, double.NaN }; // Invalid real
                }
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { 1, invalidImaginary, double.NaN, double.NaN }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary, double.NaN, double.NaN }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("Sinh_Advanced_TestData")]
        public static void Sinh_Advanced(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Sinh(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Subtract_TestData()
        {
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), 0, 0 }; // x - 0 = x
            yield return new object[] { 0, 0, RandomPositiveDouble(), RandomPositiveDouble() }; // 0 - x = -x

            // First quadrant, first quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble() };
            // First quadrant, second quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble() };
            // First quadrant, third quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), RandomNegativeDouble(), RandomNegativeDouble() };
            // First quadrant, fourth quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble(), RandomNegativeDouble() };
            // Second quadrant, second quadrant
            yield return new object[] { RandomNegativeDouble(), RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble() };
            // Second quadrant, third quadrant
            yield return new object[] { RandomNegativeDouble(), RandomPositiveDouble(), RandomNegativeDouble(), RandomNegativeDouble() };
            // Second quadrant, fouth quadrant
            yield return new object[] { RandomNegativeDouble(), RandomPositiveDouble(), RandomPositiveDouble(), RandomNegativeDouble() };
            // Third quadrant, third quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble(), RandomPositiveDouble(), RandomNegativeDouble() };
            // Third quadrant, fourth quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble(), RandomNegativeDouble(), RandomNegativeDouble() };
            // Fourth quadrant, fourth quadrant
            yield return new object[] { RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble(), RandomNegativeDouble() };

            // Boundary values
            yield return new object[] { double.MaxValue, double.MaxValue, RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { double.MinValue, double.MinValue, RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { double.MaxValue, double.MaxValue, RandomNegativeDouble(), RandomNegativeDouble() };
            yield return new object[] { double.MinValue, double.MinValue, RandomNegativeDouble(), RandomNegativeDouble() };

            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), double.MaxValue, double.MaxValue };
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble(), double.MinValue, double.MinValue };
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble(), double.MaxValue, double.MaxValue };
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble(), double.MinValue, double.MinValue };

            // Invalid real
            yield return new object[] { double.PositiveInfinity, RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { double.PositiveInfinity, RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble() };

            yield return new object[] { double.NegativeInfinity, RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { double.NegativeInfinity, RandomPositiveDouble(), RandomNegativeDouble(), RandomPositiveDouble() };

            yield return new object[] { double.NaN, RandomPositiveDouble(), RandomPositiveDouble(), RandomPositiveDouble() };

            // Invalid imaginary
            yield return new object[] { RandomPositiveDouble(), double.PositiveInfinity, RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { RandomPositiveDouble(), double.PositiveInfinity, RandomPositiveDouble(), RandomNegativeDouble() };

            yield return new object[] { RandomPositiveDouble(), double.NegativeInfinity, RandomPositiveDouble(), RandomPositiveDouble() };
            yield return new object[] { RandomPositiveDouble(), double.NegativeInfinity, RandomPositiveDouble(), RandomNegativeDouble() };

            yield return new object[] { RandomPositiveDouble(), double.NaN, RandomPositiveDouble(), RandomPositiveDouble() };
        }

        [Theory, MemberData("Subtract_TestData")]
        public static void Subtract(double realLeft, double imaginaryLeft, double realRight, double imaginaryRight)
        {
            var left = new Complex(realLeft, imaginaryLeft);
            var right = new Complex(realRight, imaginaryRight);

            // calculate the expected results
            double expectedReal = realLeft - realRight;
            double expectedImaginary = imaginaryLeft - imaginaryRight;

            // Operator
            Complex result = left - right;
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);

            // Static method
            result = Complex.Subtract(left, right);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Sqrt_TestData()
        {
            yield return new object[] { 0, 0, 0, 0 };
            yield return new object[] { 1, 0, 1, 0 };
            yield return new object[] { 0, 1, 0.707106781186547, 0.707106781186547 };
            yield return new object[] { 0, -1, 0.707106781186547, -0.707106781186547 };

            yield return new object[] { double.MaxValue, 0, 1.34078079299426E+154, 0 };
            yield return new object[] { 0, double.MaxValue, 9.48075190810917E+153, 9.48075190810917E+153 };
            yield return new object[] { 0, double.MinValue, 9.48075190810917E+153, -9.48075190810917E+153 };
        }

        [Theory, MemberData("Sqrt_TestData")]
        public static void Sqrt(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Sqrt(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Tan_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            // First quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };
        }

        [Theory, MemberData("Tan_Basic_TestData")]
        public static void Tan_Basic(double real, double imaginary)
        {
            double scale = Math.Cosh(2 * imaginary);
            if (!double.IsInfinity(scale))
            {
                scale += Math.Cos(2 * real);
            }
            double expectedReal = Math.Sin(2 * real) / scale;
            double expectedImaginary = Math.Sinh(2 * imaginary) / scale;

            if (double.IsNaN(expectedImaginary))
            {
                expectedReal = double.NaN;
            }
            Tan_Advanced(real, imaginary, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Tan_Advanced_TestData()
        {
            yield return new object[] { double.MaxValue, 0, Math.Sin(double.MaxValue) / Math.Cos(double.MaxValue), 0 };
            yield return new object[] { double.MinValue, 0, Math.Sin(double.MinValue) / Math.Cos(double.MinValue), 0 };

            yield return new object[] { 0, double.MaxValue, double.NaN, double.NaN };
            yield return new object[] { 0, double.MinValue, double.NaN, double.NaN };

            yield return new object[] { double.MaxValue, double.MaxValue, double.NaN, double.NaN };
            yield return new object[] { double.MinValue, double.MinValue, double.NaN, double.NaN };

            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, 1, double.NaN, double.NaN }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { 1, invalidImaginary, double.NaN, double.NaN }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary, double.NaN, double.NaN }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("Tan_Advanced_TestData")]
        public static void Tan_Advanced(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Tan(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> Tanh_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() }; // Positive, positive
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() }; // Positive, negative
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() }; // Negative, positive
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() }; // Negative, Negative

            // Boundary values
            yield return new object[] { double.MaxValue, 0 };
            yield return new object[] { double.MaxValue, double.MaxValue };
            yield return new object[] { double.MinValue, double.MinValue };
        }

        [Theory, MemberData("Tanh_Basic_TestData")]
        public static void Tanh_Basic(double real, double imaginary)
        {
            // The product formula: cosh (x+iy) = sinh (x+iy) / cosh (x+iy)
            // The verification formula: Tanh (z) = (Exp(2z) -1) / (Exp(2z)+1)
            // The verification formula is used not for the boundary values
            var complex = new Complex(real, imaginary);
            Complex scale = 2 * complex;
            Complex expected = (Complex.Exp(scale) - 1) / (Complex.Exp(scale) + 1);
            Tanh_Advanced(real, imaginary, expected.Real, expected.Imaginary);
        }

        public static IEnumerable<object[]> Tanh_Advanced_TestData()
        {
            // Boundary values
            yield return new object[] { double.MinValue, 0, double.NaN, double.NaN };
            yield return new object[] { 0, double.MaxValue, 0, Math.Sin(double.MaxValue) / Math.Cos(double.MaxValue) };
            yield return new object[] { 0, double.MinValue, 0, Math.Sin(double.MinValue) / Math.Cos(double.MinValue) };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, 1, double.NaN, double.NaN }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { 1, invalidImaginary, double.NaN, double.NaN }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary, double.NaN, double.NaN }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("Tanh_Advanced_TestData")]
        public static void Tanh_Advanced(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Tanh(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary);
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { Complex.Zero.Real, Complex.Zero.Imaginary };
            yield return new object[] { Complex.One.Real, Complex.One.Imaginary };
            yield return new object[] { Complex.ImaginaryOne.Real, Complex.ImaginaryOne.Imaginary };
            yield return new object[] { (-Complex.One).Real, (-Complex.One).Imaginary };
            yield return new object[] { (-Complex.ImaginaryOne).Real, (-Complex.ImaginaryOne).Imaginary };

            // First quadrant
            yield return new object[] { RandomPositiveDouble(), RandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { RandomNegativeDouble(), RandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { RandomNegativeDouble(), RandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { RandomPositiveDouble(), RandomNegativeDouble() };

            // First quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomPositiveDouble() };
            // Second quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomPositiveDouble() };
            // Third quadrant
            yield return new object[] { SmallRandomNegativeDouble(), SmallRandomNegativeDouble() };
            // Fourth quadrant
            yield return new object[] { SmallRandomPositiveDouble(), SmallRandomNegativeDouble() };

            // Boundary values
            yield return new object[] { double.MaxValue, 0 };
            yield return new object[] { double.MinValue, 0 };

            yield return new object[] { 0, double.MaxValue };
            yield return new object[] { 0, double.MinValue };

            yield return new object[] { double.MaxValue, double.MaxValue };
            yield return new object[] { double.MinValue, double.MinValue };

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, RandomPositiveDouble() }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomNegativeDouble(), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("ToString_TestData")]
        public static void ToString(double real, double imaginary)
        {
            var complex = new Complex(real, imaginary);

            string expected = "(" + real.ToString() + ", " + imaginary.ToString() + ")";
            string actual = complex.ToString();
            Assert.Equal(expected, actual);

            NumberFormatInfo numberFormatInfo = CultureInfo.CurrentCulture.NumberFormat;
            expected = "(" + real.ToString(numberFormatInfo) + ", " + imaginary.ToString(numberFormatInfo) + ")";
            actual = complex.ToString(numberFormatInfo);
            Assert.Equal(expected, complex.ToString(numberFormatInfo));

            foreach (string format in s_supportedStandardNumericFormats)
            {
                expected = "(" + real.ToString(format) + ", " + imaginary.ToString(format) + ")";
                actual = complex.ToString(format);
                Assert.Equal(expected, actual);

                expected = "(" + real.ToString(format, numberFormatInfo) + ", " + imaginary.ToString(format, numberFormatInfo) + ")";
                actual = complex.ToString(format, numberFormatInfo);
                Assert.Equal(expected, actual);
            }
        }

        public static IEnumerable<object[]> Cast_SByte_TestData()
        {
            yield return new object[] { sbyte.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { sbyte.MaxValue };

            yield return new object[] { (sbyte)s_random.Next(sbyte.MinValue, 0) };
            yield return new object[] { (sbyte)s_random.Next(1, sbyte.MaxValue) };
        }

        [Theory, MemberData("Cast_SByte_TestData")]
        public static void Cast_SByte(sbyte value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0);

            if (value != sbyte.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0);
            }
            if (value != sbyte.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0);
            }
        }

        public static IEnumerable<object[]> Cast_Int16_TestData()
        {
            yield return new object[] { short.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { short.MaxValue };

            yield return new object[] { (short)s_random.Next(1, short.MaxValue) };
            yield return new object[] { (short)s_random.Next(short.MinValue, 0) };
        }

        [Theory, MemberData("Cast_Int16_TestData")]
        public static void Cast_Int16(short value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0);

            if (value != short.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0);
            }
            if (value != short.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0);
            }
        }

        public static IEnumerable<object[]> Cast_Int32_TestData()
        {
            yield return new object[] { int.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { int.MaxValue };

            int positveInt = (int)RandomPositiveValue(int.MaxValue);
            yield return new object[] { positveInt };
            yield return new object[] { -positveInt };
        }

        [Theory, MemberData("Cast_Int32_TestData")]
        public static void Cast_Int32(int value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0);

            if (value != int.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0);
            }
            if (value != int.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0);
            }
        }

        public static IEnumerable<object[]> Cast_Int64_TestData()
        {
            yield return new object[] { long.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { long.MaxValue };

            long positiveLong = (long)RandomPositiveValue(long.MaxValue);
            yield return new object[] { positiveLong };
            yield return new object[] { -positiveLong };
        }

        [Theory, MemberData("Cast_Int64_TestData")]
        public static void Cast_Int64(long value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0);

            if (value != long.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0);
            }
            if (value != long.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0);
            }
        }

        public static IEnumerable<object[]> Cast_Byte_TestData()
        {
            yield return new object[] { byte.MinValue };
            yield return new object[] { 1 };
            yield return new object[] { byte.MaxValue };

            yield return new object[] { (byte)s_random.Next(1, byte.MaxValue) };
        }

        [Theory, MemberData("Cast_Byte_TestData")]
        public static void Cast_Byte(byte value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0);

            if (value != byte.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0);
            }
            if (value != byte.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0);
            }
        }

        public static IEnumerable<object[]> Cast_UInt16_TestData()
        {
            yield return new object[] { ushort.MinValue };
            yield return new object[] { 1 };
            yield return new object[] { ushort.MaxValue };

            yield return new object[] { (ushort)s_random.Next(1, ushort.MaxValue) };
        }

        [Theory, MemberData("Cast_UInt16_TestData")]
        public static void Cast_UInt16(ushort value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0);

            if (value != ushort.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0);
            }
            if (value != ushort.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0);
            }
        }

        public static IEnumerable<object[]> Cast_UInt32_TestData()
        {
            yield return new object[] { uint.MinValue };
            yield return new object[] { 1 };
            yield return new object[] { uint.MaxValue };

            yield return new object[] { (uint)RandomPositiveValue(uint.MaxValue) };
        }

        [Theory, MemberData("Cast_UInt32_TestData")]
        public static void Cast_UInt32(uint value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0);

            if (value != uint.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0);
            }
            if (value != uint.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0);
            }
        }

        public static IEnumerable<object[]> Cast_UInt64_TestData()
        {
            yield return new object[] { ulong.MinValue };
            yield return new object[] { 1 };
            yield return new object[] { ulong.MaxValue };

            yield return new object[] { (ulong)RandomPositiveValue(ulong.MaxValue) };
        }

        [Theory, MemberData("Cast_UInt64_TestData")]
        public static void Cast_UInt64(ulong value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0);

            if (value != ulong.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0);
            }
            if (value != ulong.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0);
            }
        }

        public static IEnumerable<object[]> Cast_Single_TestData()
        {
            yield return new object[] { float.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { float.MaxValue };

            float positiveFloat = (float)RandomPositiveValue(float.MaxValue);
            yield return new object[] { positiveFloat };
            yield return new object[] { -positiveFloat };
        }

        [Theory, MemberData("Cast_Single_TestData")]
        public static void Cast_Single(float value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0);

            if (value != float.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0);
            }
            if (value != float.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0);
            }
        }

        public static IEnumerable<object[]> Cast_Double_TestData()
        {
            yield return new object[] { double.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { double.MaxValue };

            yield return new object[] { RandomPositiveDouble() };
            yield return new object[] { RandomNegativeDouble() };
        }

        [Theory, MemberData("Cast_Double_TestData")]
        public static void Cast_Double(double value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0);

            if (value != double.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0);
            }
            if (value != double.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0);
            }
        }

        public static IEnumerable<object[]> Cast_BigInteger_TestData()
        {
            yield return new object[] { (BigInteger)double.MinValue };
            yield return new object[] { (BigInteger)(-1) };
            yield return new object[] { (BigInteger)0 };
            yield return new object[] { (BigInteger)1 };
            yield return new object[] { (BigInteger)double.MaxValue };

            yield return new object[] { (BigInteger)RandomPositiveDouble() };
            yield return new object[] { (BigInteger)RandomNegativeDouble() };
        }

        [Theory, MemberData("Cast_BigInteger_TestData")]
        public static void Cast_BigInteger(BigInteger value)
        {
            Complex complex = (Complex)value;
            VerifyRealImaginaryProperties(complex, (double)value, 0);

            if (value != (BigInteger)double.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, (double)(value + 1), 0);
            }
            if (value != (BigInteger)double.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, (double)(value - 1), 0);
            }
        }

        public static IEnumerable<object[]> Cast_Decimal_TestData()
        {
            yield return new object[] { decimal.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { decimal.MaxValue };

            decimal positiveDecimal = new decimal(
                s_random.Next(int.MinValue, int.MaxValue),
                s_random.Next(int.MinValue, int.MaxValue),
                s_random.Next(int.MinValue, int.MaxValue),
                false,
                (byte)s_random.Next(0, 29));
            yield return new object[] { positiveDecimal };
            yield return new object[] { -positiveDecimal };
        }

        [Theory, MemberData("Cast_Decimal_TestData")]
        public static void Cast_Decimal(decimal value)
        {
            Complex complex = (Complex)value;
            VerifyRealImaginaryProperties(complex, (double)value, 0);

            if (value != decimal.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, (double)(value + 1), 0);
            }
            if (value != decimal.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, (double)(value - 1), 0);
            }
        }
        
        private static double SmallRandomPositiveDouble()
        {
            return RandomPositiveValue(1);
        }

        private static double SmallRandomNegativeDouble()
        {
            return -SmallRandomPositiveDouble();
        }

        public static double RandomPositiveDouble()
        {
            return RandomPositiveValue(double.MaxValue);
        }

        public static double RandomNegativeDouble()
        {
            return -RandomPositiveDouble();
        }

        public static double RandomPositivePhase()
        {
            return RandomPositiveValue(Math.PI / 2);
        }

        public static double RandomNegativePhase()
        {
            return -RandomPositivePhase();
        }

        private static double RandomPositiveValue(double mult)
        {
            double randomDouble = (mult * s_random.NextDouble());
            randomDouble %= mult;
            return randomDouble;
        }

        private static bool IsDiffTolerable(double d1, double d2)
        {
            if (double.IsInfinity(d1))
            {
                return AreSameInfinity(d1, d2 * 10);
            }
            else if (double.IsInfinity(d2))
            {
                return AreSameInfinity(d1 * 10, d2);
            }
            else
            {
                double diffRatio = (d1 - d2) / d1;
                diffRatio *= Math.Pow(10, 6);
                diffRatio = Math.Abs(diffRatio);
                return (diffRatio < 1);
            }
        }

        private static bool AreSameInfinity(double d1, double d2)
        {
            return
                double.IsNegativeInfinity(d1) == double.IsNegativeInfinity(d2) &&
                double.IsPositiveInfinity(d1) == double.IsPositiveInfinity(d2);
        }

        private static void VerifyRealImaginaryProperties(Complex complex, double real, double imaginary, [CallerLineNumber] int lineNumber = 0)
        {
            Assert.True(real.Equals(complex.Real) || IsDiffTolerable(complex.Real, real),
                string.Format("Failure at line {0}. Expected real: {1}. Actual real: {2}", lineNumber, real, complex.Real));
            Assert.True(imaginary.Equals(complex.Imaginary) || IsDiffTolerable(complex.Imaginary, imaginary),
                string.Format("Failure at line {0}. Expected imaginary: {1}. Actual imaginary: {2}", lineNumber, imaginary, complex.Imaginary));
        }

        private static void VerifyMagnitudePhaseProperties(Complex complex, double magnitude, double phase, [CallerLineNumber] int lineNumber = 0)
        {
            // The magnitude (m) of a complex number (z = x + yi) is the absolute value - |z| = sqrt(x^2 + y^2)
            // Verification is done using the square of the magnitude since m^2 = x^2 + y^2
            double expectedMagnitudeSquared = magnitude * magnitude;
            double actualMagnitudeSquared = complex.Magnitude * complex.Magnitude;

            Assert.True(expectedMagnitudeSquared.Equals(actualMagnitudeSquared) || IsDiffTolerable(actualMagnitudeSquared, expectedMagnitudeSquared),
                string.Format("Failure at line {0}. Expected magnitude squared: {1}. Actual magnitude squared: {2}", lineNumber, expectedMagnitudeSquared, actualMagnitudeSquared));

            if (double.IsNaN(magnitude))
            {
                phase = double.NaN;
            }
            else if (magnitude == 0)
            {
                phase = 0;
            }
            else if (magnitude < 0)
            {
                phase += (phase < 0) ? Math.PI : -Math.PI;
            }

            Assert.True(phase.Equals(complex.Phase) || IsDiffTolerable(complex.Phase, phase),
                string.Format("Failure at line {0}. Expected phase: {1}. Actual phase: {2}", lineNumber, phase, complex.Phase));
        }
    }
}
