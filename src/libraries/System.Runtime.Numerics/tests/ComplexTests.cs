// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
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

        public static string[] s_supportedStdNumericFormats = new string[] { "C", "E", "F", "G", "N", "P", "R" };

        [Fact]
        public static void Zero()
        {
            Assert.True(Complex.Zero == 0);
            VerifyRealImaginaryProperties(Complex.Zero, 0, 0, "Verify real and imaginary parts are 0");
            VerifyMagnitudePhaseProperties(Complex.Zero, 0, double.NaN, "Verify magnitude is 0 and phase is NaN");
        }

        [Fact]
        public static void One()
        {
            Assert.True(Complex.One == 1);
            VerifyRealImaginaryProperties(Complex.One, 1, 0, "Verify real part is 1, and imaginary part is 0");
            VerifyMagnitudePhaseProperties(Complex.One, 1, 0, "Verify magnitude is 1, and phase is 0");
        }

        [Fact]
        public static void ImaginaryOne()
        {
            VerifyRealImaginaryProperties(Complex.ImaginaryOne, 0, 1, "Verify real part is 0, and imaginary part is 1");
            VerifyMagnitudePhaseProperties(Complex.ImaginaryOne, 1, Math.PI / 2, "Verify magnitude is 1 and phase is Math.PI/2.");
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

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomDouble(false), RandomDouble(false) }; // +, +
                yield return new object[] { RandomDouble(false), RandomDouble(true) }; // +, -
                yield return new object[] { RandomDouble(true), RandomDouble(true) }; // +, -
                yield return new object[] { RandomDouble(true), RandomDouble(true) }; // -, -
            }

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, RandomDouble(false) }; // Invalid real
                yield return new object[] { invalidReal, RandomDouble(true) }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomDouble(false), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { RandomDouble(true), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("Ctor_Double_Double_TestData")]
        public static void Ctor_Double_Double(double real, double imaginary)
        {
            var complex = new Complex(real, imaginary);
            VerifyRealImaginaryProperties(complex, real, imaginary, "");
        }
        
        public static IEnumerable<object[]> Abs_TestData()
        {
            yield return new object[] { 0, 0, 0 };
            yield return new object[] { 1, 0, 1 };
            yield return new object[] { 0, 1, 1 };

            double randomReal = RandomDouble(false);
            double randomImaginary = RandomDouble(false);
            yield return new object[] { randomReal, randomImaginary, Math.Sqrt(randomReal * randomReal + randomImaginary * randomImaginary) }; // +, +

            randomReal = RandomDouble(true);
            randomImaginary = RandomDouble(false);
            yield return new object[] { randomReal, randomImaginary, Math.Sqrt(randomReal * randomReal + randomImaginary * randomImaginary) }; // +, -

            randomReal = RandomDouble(false);
            randomImaginary = RandomDouble(true);
            yield return new object[] { randomReal, randomImaginary, Math.Sqrt(randomReal * randomReal + randomImaginary * randomImaginary) }; // -, +

            randomReal = RandomDouble(true);
            randomImaginary = RandomDouble(true);
            yield return new object[] { randomReal, randomImaginary, Math.Sqrt(randomReal * randomReal + randomImaginary * randomImaginary) }; // -, -

            // Boundary values
            yield return new object[] { double.MaxValue, 0, double.MaxValue };
            yield return new object[] { double.MinValue, 0, double.MaxValue };

            yield return new object[] { 0, double.MaxValue, double.MaxValue };
            yield return new object[] { 0, double.MinValue, double.MaxValue };

            yield return new object[] { double.MaxValue, double.MaxValue, double.PositiveInfinity };
            yield return new object[] { double.MinValue, double.MinValue, double.PositiveInfinity };

            // Invalid values
            randomReal = RandomDouble(false);
            randomImaginary = RandomDouble(false);
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
            // Create complex numbers
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

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // -, -

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

            yield return new object[] { double.MaxValue, 0, double.NaN, double.NaN };
            yield return new object[] { double.MinValue, 0, double.NaN, double.NaN };

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
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("ACos({0}):{1}", complex, result));
        }

        public static IEnumerable<object[]> Add_TestData()
        {
            yield return new object[] { 0, 0, RandomDouble(false), RandomDouble(false) }; // 0 + x = x
            yield return new object[] { RandomDouble(false), RandomDouble(false), 0, 0 }; // x + 0 = x

            yield return new object[] { RandomDouble(false), RandomDouble(false), RandomDouble(false), RandomDouble(false) }; // +, +, +, +
            yield return new object[] { RandomDouble(false), RandomDouble(false), RandomDouble(true), RandomDouble(false) }; // +, +, -, +
            yield return new object[] { RandomDouble(false), RandomDouble(false), RandomDouble(false), RandomDouble(true) }; // +, +, +, -
            yield return new object[] { RandomDouble(false), RandomDouble(false), RandomDouble(true), RandomDouble(true) }; // +, +, -, -
            yield return new object[] { RandomDouble(true), RandomDouble(false), RandomDouble(true), RandomDouble(false) }; // -, +, -, +
            yield return new object[] { RandomDouble(true), RandomDouble(false), RandomDouble(false), RandomDouble(true) }; // -, +, +, -
            yield return new object[] { RandomDouble(true), RandomDouble(false), RandomDouble(true), RandomDouble(true) }; // -, +, -, -
            yield return new object[] { RandomDouble(true), RandomDouble(true), RandomDouble(false), RandomDouble(true) }; // -, -, +, -
            yield return new object[] { RandomDouble(true), RandomDouble(true), RandomDouble(true), RandomDouble(true) }; // -, -, -, -

            // Boundary values
            yield return new object[] { double.MaxValue, double.MaxValue, RandomDouble(false), RandomDouble(false) };
            yield return new object[] { double.MaxValue, double.MaxValue, RandomDouble(true), RandomDouble(true) };

            yield return new object[] { double.MinValue, double.MinValue, RandomDouble(false), RandomDouble(false) };
            yield return new object[] { double.MinValue, double.MinValue, RandomDouble(true), RandomDouble(true) };

            // Invalid real
            yield return new object[] { double.PositiveInfinity, RandomDouble(false), RandomDouble(false), RandomDouble(false) };
            yield return new object[] { double.PositiveInfinity, RandomDouble(false), RandomDouble(true), RandomDouble(false) };

            yield return new object[] { double.NegativeInfinity, RandomDouble(false), RandomDouble(false), RandomDouble(false) };
            yield return new object[] { double.NegativeInfinity, RandomDouble(false), RandomDouble(true), RandomDouble(false) };

            yield return new object[] { double.NaN, RandomDouble(false), RandomDouble(false), RandomDouble(false) };

            // Invalid imaginary
            yield return new object[] { RandomDouble(false), double.PositiveInfinity, RandomDouble(false), RandomDouble(false) };
            yield return new object[] { RandomDouble(false), double.PositiveInfinity, RandomDouble(false), RandomDouble(true) };

            yield return new object[] { RandomDouble(false), double.NegativeInfinity, RandomDouble(false), RandomDouble(false) };
            yield return new object[] { RandomDouble(false), double.NegativeInfinity, RandomDouble(false), RandomDouble(true) };

            yield return new object[] { RandomDouble(false), double.NaN, RandomDouble(false), RandomDouble(false) };
        }

        [Theory, MemberData("Add_TestData")]
        public static void Add(double realLeft, double imaginaryLeft, double realRight, double imaginaryRight)
        {
            var left = new Complex(realLeft, imaginaryLeft);
            var right = new Complex(realRight, imaginaryRight);

            // calculate the expected results
            double expectedReal = realLeft + realRight;
            double expectedImaginary = imaginaryLeft + imaginaryRight;

            // Operator
            Complex result = left + right;
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("Binary Plus test = ({0}, {1}) + ({2}, {3})", realLeft, imaginaryLeft, realRight, imaginaryRight));

            // Static method
            result = Complex.Add(left, right);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("Add test = ({0}, {1}) + ({2}, {3})", realLeft, imaginaryLeft, realRight, imaginaryRight));
        }

        public static IEnumerable<object[]> ASin_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // -, -
        }

        [Theory, MemberData("ASin_Basic_TestData")]
        public static void ASin_Basic(double real, double imaginary)
        {
            // Formula used in the feature: arcsin(z) = -iln(iz + Sqrt(1-z*z))
            // Verification is done with z = ASin(Sin(z));
            var complex = new Complex(real, imaginary);
            Complex sinComplex = Complex.Sin(complex);
            Complex result = Complex.Asin(sinComplex);
            VerifyRealImaginaryProperties(result, real, imaginary, 
                string.Format("({0}) != ASin(Sin():{1}):{2}", complex, sinComplex, result));
        }

        public static IEnumerable<object[]> ASin_Advanced_TestData()
        {
            yield return new object[] { 1234000000, 0, 1.5707963267948966, -21.62667394298955 }; // Real part is positive, imaginary part is 0
            yield return new object[] { 0, 1234000000, 0, 21.62667394298955 }; // Imaginary part is positive

            yield return new object[] { double.MaxValue, 0, double.NaN, double.NaN };
            yield return new object[] { double.MinValue, 0, double.NaN, double.NaN };
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

        [Theory, MemberData("ASin_Advanced_TestData")]
        public static void ASin_Advanced(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Asin(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("ASin:({0}):{1}", complex, result));
        }

        public static IEnumerable<object[]> ATan_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 }; // Undefined - NaN should be propagated
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 }; // Undefined - NaN should be propagated

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // -, -
        }

        [Theory, MemberData("ATan_Basic_TestData")]
        public static void ATan_Basic(double real, double imaginary)
        {
            // Formula used in the feature: Atan(z) = (i/2) * (log(1-iz) - log(1+iz))
            // Verification is done with z = ATan(Tan(z));
            var complex = new Complex(real, imaginary);
            Complex tanComplex = Complex.Tan(complex);
            Complex atanComplex = Complex.Atan(tanComplex);
            VerifyRealImaginaryProperties(atanComplex, real, imaginary,
                string.Format("({0}) != ATan(Tan():{1}):{2}", complex, tanComplex, atanComplex));
        }

        public static IEnumerable<object[]> ATan_Advanced_TestData()
        {
            yield return new object[] { double.MaxValue, 0, Math.PI / 2, 0 };
            yield return new object[] { double.MinValue, 0, -Math.PI / 2, 0 };
            yield return new object[] { 0, double.MaxValue, Math.PI / 2, 0 };
            yield return new object[] { 0, double.MinValue, -Math.PI / 2, 0 };
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

        [Theory, MemberData("ATan_Advanced_TestData")]
        public static void ATan_Advanced(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Atan(complex);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("ASin:({0}):{1}", complex, result));
        }

        public static IEnumerable<object[]> Conjugate_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, -1 };

            yield return new object[] { RandomDouble(false), RandomDouble(false) }; // +, +
            yield return new object[] { RandomDouble(false), RandomDouble(true) }; // +, -
            yield return new object[] { RandomDouble(true), RandomDouble(false) }; // -, +
            yield return new object[] { RandomDouble(true), RandomDouble(true) }; // -, -

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // -, -

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
                yield return new object[] { invalidReal, RandomDouble(false) }; // Invalid real
                yield return new object[] { invalidReal, RandomDouble(true) }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomDouble(false), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { RandomDouble(true), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary };
                }
            }
        }

        [Theory, MemberData("Conjugate_TestData")]
        public static void Conjugate(double real, double imaginary)
        {
            var complex = new Complex(real, imaginary);
            Complex result = Complex.Conjugate(complex);

            VerifyRealImaginaryProperties(result, real, -imaginary,
                string.Format("Conjugate test ({0}, {1})", real, imaginary));

            VerifyMagnitudePhaseProperties(result, complex.Magnitude, -complex.Phase,
                string.Format("Conjugate test ({0}, {1})", real, imaginary));
        }

        public static IEnumerable<object[]> Cos_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // -, -
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
            yield return new object[] { double.MaxValue, 0, Math.Cos(double.MaxValue), 0 };
            yield return new object[] { double.MinValue, 0, Math.Cos(double.MinValue), 0 };

            yield return new object[] { 0, double.MaxValue, double.PositiveInfinity, double.NaN };
            yield return new object[] { 0, double.MinValue, double.PositiveInfinity, double.NaN };

            yield return new object[] { double.MaxValue, double.MaxValue, Math.Cos(double.MaxValue) * double.PositiveInfinity, double.NegativeInfinity };
            yield return new object[] { double.MinValue, double.MinValue, double.NegativeInfinity, double.NegativeInfinity };

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
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("Cos({0}):{1} != ({2},{3})", complex, result, expectedReal, expectedImaginary));
        }

        public static IEnumerable<object[]> Cosh_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // Positive, positive
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // Positive, negative
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // Negative, positive
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // Negative, Negative
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
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary, string.Format("Cosh({0})", complex));
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

            // Boundary values
            double randomReal = SmallRandomDouble(false);
            double randomImaginary = SmallRandomDouble(false);
            yield return new object[] { double.MaxValue, double.MaxValue, randomReal, randomImaginary };
            yield return new object[] { double.MinValue, double.MinValue, randomReal, randomImaginary };

            // Invalid values
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +, +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(true), SmallRandomDouble(false) }; // +, +, -, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(true) }; // +, +, +, -
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(true), SmallRandomDouble(true) }; // +, +, -, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(true) }; // -, +, +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false), SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +, -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false), SmallRandomDouble(true), SmallRandomDouble(true) }; // -, +, -, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true), SmallRandomDouble(false), SmallRandomDouble(true) }; // -, -, +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true), SmallRandomDouble(true), SmallRandomDouble(true) }; // -, -, -, -

            // Invalid real values
            yield return new object[] { double.PositiveInfinity, SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(false) };
            yield return new object[] { double.PositiveInfinity, SmallRandomDouble(false), SmallRandomDouble(true), SmallRandomDouble(false) };

            yield return new object[] { double.NegativeInfinity, SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(false) };
            yield return new object[] { double.NegativeInfinity, SmallRandomDouble(false), SmallRandomDouble(true), SmallRandomDouble(false) };

            yield return new object[] { double.NaN, SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(false) };

            // Invalid imaginary values
            yield return new object[] { SmallRandomDouble(false), double.PositiveInfinity, SmallRandomDouble(false), SmallRandomDouble(false) };
            yield return new object[] { SmallRandomDouble(false), double.PositiveInfinity, SmallRandomDouble(false), SmallRandomDouble(true) };

            yield return new object[] { SmallRandomDouble(false), double.NegativeInfinity, SmallRandomDouble(false), SmallRandomDouble(false) };
            yield return new object[] { SmallRandomDouble(false), double.NegativeInfinity, SmallRandomDouble(false), SmallRandomDouble(true) };

            yield return new object[] { SmallRandomDouble(false), double.NaN, SmallRandomDouble(false), SmallRandomDouble(false) };
        }

        [Theory, MemberData("Divide_TestData")]
        public static void Divide(double realFirst, double imgFirst, double realSecond, double imgSecond)
        {
            var dividend = new Complex(realFirst, imgFirst);
            var divisor = new Complex(realSecond, imgSecond);

            Complex complexExpected = dividend * Complex.Conjugate(divisor);
            double realExpected = complexExpected.Real;
            double imaginaryExpected = complexExpected.Imaginary;

            if (!double.IsInfinity(realExpected))
            {
                realExpected = realExpected / (divisor.Magnitude * divisor.Magnitude);
            }
            if (!double.IsInfinity(imaginaryExpected))
            {
                imaginaryExpected = imaginaryExpected / (divisor.Magnitude * divisor.Magnitude);
            }

            // Operator
            Complex result = dividend / divisor;
            VerifyRealImaginaryProperties(result, realExpected, imaginaryExpected,
                string.Format("Binary Divide test = ({0}, {1}) / ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond));

            // Static method
            result = Complex.Divide(dividend, divisor);
            VerifyRealImaginaryProperties(result, realExpected, imaginaryExpected,
                string.Format("Divide (Static) test = ({0}, {1}) / ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { Complex.Zero, Complex.Zero, true, true };
            yield return new object[] { Complex.Zero, Complex.One, false, false };
            yield return new object[] { Complex.Zero, -Complex.One, false, false };
            yield return new object[] { Complex.Zero, Complex.ImaginaryOne, false, false };
            yield return new object[] { Complex.Zero, -Complex.ImaginaryOne, false, false };

            yield return new object[] { Complex.One, Complex.One, true, true };
            yield return new object[] { Complex.One, -Complex.One, false, false };
            yield return new object[] { Complex.One, Complex.ImaginaryOne, false, false };
            yield return new object[] { Complex.One, -Complex.ImaginaryOne, false, false };

            yield return new object[] { -Complex.One, -Complex.One, true, true };
            yield return new object[] { -Complex.One, Complex.ImaginaryOne, false, false };
            yield return new object[] { -Complex.One, -Complex.ImaginaryOne, false, false };

            yield return new object[] { Complex.ImaginaryOne, Complex.ImaginaryOne, true, true };
            yield return new object[] { Complex.ImaginaryOne, -Complex.ImaginaryOne, false, false };

            yield return new object[] { -Complex.ImaginaryOne, -Complex.ImaginaryOne, true, true };

            yield return new object[] { Complex.Zero, new Complex(0, 0), true, true };
            yield return new object[] { Complex.Zero, new Complex(1, 0), false, false };
            yield return new object[] { Complex.Zero, new Complex(0, 1), false, false };

            yield return new object[] { Complex.One, new Complex(1, 0), true, true };
            yield return new object[] { Complex.One, new Complex(1, 1), false, false };
            yield return new object[] { Complex.One, new Complex(0, 1), false, false };

            yield return new object[] { -Complex.One, new Complex(-1, 0), true, true };
            yield return new object[] { -Complex.One, new Complex(-1, -1), false, false };
            yield return new object[] { -Complex.One, new Complex(0, -1), false, false };

            yield return new object[] { Complex.ImaginaryOne, new Complex(0, 1), true, true };
            yield return new object[] { Complex.ImaginaryOne, new Complex(1, 1), false, false };
            yield return new object[] { Complex.ImaginaryOne, new Complex(0, -1), false, false };

            yield return new object[] { -Complex.ImaginaryOne, new Complex(0, -1), true, true };
            yield return new object[] { -Complex.ImaginaryOne, new Complex(-1, -1), false, false };
            yield return new object[] { -Complex.ImaginaryOne, new Complex(0, 1), false, false };

            yield return new object[] { new Complex(0.5, 0.5), new Complex(0.5, 0.5), true, true };
            yield return new object[] { new Complex(0.5, 0.5), new Complex(0.5, 1.5), false, false };
            yield return new object[] { new Complex(0.5, 0.5), new Complex(1.5, 0.5), false, false };

            // Boundary values
            Complex maxMax = new Complex(double.MaxValue, double.MaxValue);
            Complex maxMin = new Complex(double.MaxValue, double.MinValue);
            Complex minMax = new Complex(double.MinValue, double.MaxValue);
            Complex minMin = new Complex(double.MinValue, double.MinValue);

            yield return new object[] { maxMax, maxMax, true, true };
            yield return new object[] { maxMax, maxMin, false, false };
            yield return new object[] { maxMax, minMax, false, false };
            yield return new object[] { maxMax, minMin, false, false };
            yield return new object[] { maxMax, new Complex(1, 2), false, false };

            yield return new object[] { maxMin, maxMin, true, true };
            yield return new object[] { maxMin, minMax, false, false };
            yield return new object[] { maxMin, minMin, false, false };
            yield return new object[] { maxMin, new Complex(1, 2), false, false };

            yield return new object[] { minMax, minMax, true, true };
            yield return new object[] { minMax, minMin, false, false };
            yield return new object[] { minMax, new Complex(1, 2), false, false };

            yield return new object[] { minMin, minMin, true, true };
            yield return new object[] { minMin, new Complex(1, 2), false, false };

            yield return new object[] { new Complex(100.5, 0), 100.5, false, false };
            yield return new object[] { new Complex(0, 100.5), 100.5, false, false };
            yield return new object[] { new Complex(100.5, 0), 0, false, false };
            yield return new object[] { new Complex(0, 100.5), 0, false, false };
            yield return new object[] { new Complex(0, 100.5), "0", false, false };
            yield return new object[] { new Complex(0, 100.5), null, false, false };

            // Invalid values
            Complex invalidComplex;
            var complex = new Complex(2, 3);
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                invalidComplex = new Complex(invalidReal, 1);
                yield return new object[] { invalidComplex, complex, false, false };
                yield return new object[] { invalidComplex, invalidComplex, !double.IsNaN(invalidReal), true }; // Handle double.NaN != double.NaN
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    invalidComplex = new Complex(1, invalidImaginary);
                    yield return new object[] { invalidComplex, complex, false, false };
                    yield return new object[] { invalidComplex, invalidComplex, !double.IsNaN(invalidImaginary), true }; // Handle double.NaN != double.NaN

                    invalidComplex = new Complex(invalidReal, invalidImaginary);
                    yield return new object[] { invalidComplex, complex, false, false };
                    yield return new object[] { invalidComplex, invalidComplex, !double.IsNaN(invalidReal) && !double.IsNaN(invalidImaginary), true }; // Handle double.NaN != double.NaN
                }
            }
        }

        [Theory, MemberData("Equals_TestData")]
        public static void Equals_Advanced_TestData(Complex complex1, object obj, bool expected, bool expectedEquals)
        {
            if (obj is Complex)
            {
                Complex complex2 = (Complex)obj;
                Assert.Equal(expected, complex1 == complex2);
                Assert.Equal(!expected, complex1 != complex2);
                Assert.Equal(expected, complex2 == complex1);
                Assert.Equal(!expected, complex2 != complex1);

                Assert.Equal(expectedEquals, complex1.Equals(complex2));
                Assert.Equal(expectedEquals, complex2.Equals(complex1));

                Assert.Equal(expectedEquals, complex1.GetHashCode().Equals(complex2.GetHashCode()));
            }
            Assert.Equal(expectedEquals, complex1.Equals(obj));
        }

        public static IEnumerable<object[]> Exp_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // -, -

            // Boundary values
            yield return new object[] { double.MaxValue, 0 };
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
            if (real == double.MaxValue && imaginary == double.MaxValue)
            {
                expected = new Complex(Math.Cos(double.MaxValue) * double.PositiveInfinity, double.PositiveInfinity);
            }
            else if (real == double.MaxValue)
            {
                expected = new Complex(Math.Cos(double.MaxValue) * double.PositiveInfinity, double.NaN);
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
            VerifyRealImaginaryProperties(result, expected.Real, expected.Imaginary,
                string.Format("Exp({0}):{1} != {2})", complex, result, expected));
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

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomDouble(false), RandomPhase(false) }; // +, +
                yield return new object[] { RandomDouble(false), RandomPhase(true) }; // +, -
                yield return new object[] { RandomDouble(true), RandomPhase(false) }; // -, +
                yield return new object[] { RandomDouble(true), RandomPhase(true) }; // -, -
            }

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, RandomDouble(false) }; // Invalid real
                yield return new object[] { invalidReal, RandomDouble(true) }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomDouble(false), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { RandomDouble(true), invalidImaginary }; // Invalid imaginary
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

            VerifyMagnitudePhaseProperties(complex, magnitude, phase,
                string.Format("FromPolarCoordinates: ({0}, {1})", magnitude, phase));

            complex = new Complex(complex.Real, complex.Imaginary);
            VerifyMagnitudePhaseProperties(complex, magnitude, phase,
                string.Format("FromPolarCoordinates: ({0}, {1})", magnitude, phase));
        }

        public static IEnumerable<object[]> Log_TestData()
        {
            yield return new object[] { Complex.One, Complex.Zero, true };
            yield return new object[] { Complex.One, Complex.ImaginaryOne, true };

            var positivePositiveComplex = new Complex(SmallRandomDouble(false), SmallRandomDouble(false));
            var positiveNegativeComplex = new Complex(SmallRandomDouble(false), SmallRandomDouble(true));
            var negativePositiveComplex = new Complex(SmallRandomDouble(true), SmallRandomDouble(false));
            var negativeNegativeComplex = new Complex(SmallRandomDouble(true), SmallRandomDouble(true));

            yield return new object[] { positivePositiveComplex, positivePositiveComplex, true }; // +, +, +, +
            yield return new object[] { positivePositiveComplex, positiveNegativeComplex, true }; // +, +, +, -
            yield return new object[] { positivePositiveComplex, negativePositiveComplex, true }; // +, +, -, +
            yield return new object[] { positivePositiveComplex, negativeNegativeComplex, true }; // +, +, -, -

            yield return new object[] { positiveNegativeComplex, positiveNegativeComplex, true }; // -, -, -, -

            yield return new object[] { negativePositiveComplex, negativePositiveComplex, true }; // -, +, -, +
            yield return new object[] { negativePositiveComplex, positiveNegativeComplex, true }; // -, +, +, -
            yield return new object[] { negativePositiveComplex, negativeNegativeComplex, true }; // -, +, -, -

            yield return new object[] { negativeNegativeComplex, negativeNegativeComplex, true }; // -, -, -, -
            yield return new object[] { negativeNegativeComplex, positiveNegativeComplex, true }; // -, -, +, -

            // Boundary values
            yield return new object[] { new Complex(double.MaxValue, 0), Complex.One, false };
            yield return new object[] { new Complex(double.MinValue, 0), Complex.One, false };
            yield return new object[] { new Complex(0, double.MaxValue), Complex.ImaginaryOne, false };
            yield return new object[] { new Complex(0, double.MinValue), Complex.ImaginaryOne, false };
        }

        [Theory, MemberData("Log_TestData")]
        public static void Log(Complex complex1, Complex complex2, bool extended)
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

        public static void Log_Zero()
        {
            Complex result = Complex.Log(Complex.Zero);
            VerifyRealImaginaryProperties(result, double.NegativeInfinity, 0, "Verify log of zero");

            result = Complex.Log10(Complex.Zero);
            VerifyRealImaginaryProperties(result, double.NegativeInfinity, 0, "Verify log10 of zero");

            result = Complex.Log(Complex.Zero, RandomDouble(false));
            VerifyRealImaginaryProperties(result, double.NegativeInfinity, double.NaN, "Verify log base of zero");
        }

        private static void VerifyLog10(Complex complex)
        {
            Complex logValue = Complex.Log10(complex);
            Complex logComplex = Complex.Log(complex);

            double baseLog = Math.Log(10);
            Complex expected = logComplex / baseLog;
            VerifyRealImaginaryProperties(logValue, expected.Real, expected.Imaginary,
                string.Format("Log({0}, {1}):{2} != {3} as expected", complex, 10, logValue, expected));
        }

        private static void VerifyLogWithBase(Complex complex)
        {
            // Verify with Random Int32
            double baseValue = 0;
            while (baseValue == 0)
            {
                baseValue = RandomInt32(false);
            }

            Complex logValue = Complex.Log(complex, baseValue);

            Complex logComplex = Complex.Log(complex);
            double baseLog = Math.Log(baseValue);
            Complex expected = logComplex / baseLog;
            VerifyRealImaginaryProperties(logValue, expected.Real, expected.Imaginary,
                string.Format("Log({0}, {1}):{2} != {3} as expected", complex, baseValue, logValue, expected));

            // Verify with Random double value
            baseValue = 0.0;
            while (baseValue == 0)
            {
                baseValue = RandomDouble(false);
            }

            logValue = Complex.Log(complex, baseValue);
            logComplex = Complex.Log(complex);
            baseLog = Math.Log(baseValue);
            expected = logComplex / baseLog;
            VerifyRealImaginaryProperties(logValue, expected.Real, expected.Imaginary,
                string.Format("Log({0}, {1}):{2} != {3} as expected", complex, baseValue, logValue, expected));
        }

        private static void VerifyLogWithMultiply(Complex complex1, Complex complex2)
        {
            // Log(c1*c2) == Log(c1) + Log(c2), if -PI < Arg(c1) + Arg(c2) <= PI
            double equalityCondition = Math.Atan2(complex1.Imaginary, complex1.Real) + Math.Atan2(complex2.Imaginary, complex2.Real);
            if (equalityCondition <= -Math.PI || equalityCondition > Math.PI)
            {
                return;
            }

            Complex logComplex = Complex.Log(complex1 * complex2);
            Complex expected = Complex.Log(complex1) + Complex.Log(complex2);
            VerifyRealImaginaryProperties(logComplex, expected.Real, expected.Imaginary,
                string.Format("Log({0}*{1}):{2} != {3})", complex1, complex2, logComplex, expected));
        }

        private static void VerifyLogWithPowerMinusOne(Complex complex)
        {
            // Log(c) == -Log(1/c)
            Complex logComplex = Complex.Log(complex);
            Complex logPowerMinusOne = Complex.Log(1 / complex);

            VerifyRealImaginaryProperties(logComplex, -logPowerMinusOne.Real, -logPowerMinusOne.Imaginary,
                string.Format("Log({0}):{1} != {2} as expected", complex, logComplex, -logPowerMinusOne));
        }

        private static void VerifyLogWithExp(Complex complex)
        {
            // Exp(log(c)) == c, if c != Zero
            Complex logComplex = Complex.Log(complex);
            Complex expected = Complex.Exp(logComplex);
            VerifyRealImaginaryProperties(expected, complex.Real, complex.Imaginary,
                string.Format("Exp(Log({0}):{1} != {2})", complex, expected, complex));
        }

        public static IEnumerable<object[]> Multiply_TestData()
        {
            yield return new object[] { 0, 0, 0, 0 }; // 0 * 0 = 0
            yield return new object[] { 0, 0, RandomDouble(false), RandomPhase(false) }; // 0 * x = 0
            yield return new object[] { RandomDouble(false), RandomPhase(false), 0, 0, }; // x * 0 = 0

            yield return new object[] { 1, 0, 1, 0 }; // 1 * 1 = 0
            yield return new object[] { 1, 0, RandomDouble(false), RandomPhase(false) }; // 1 * x = x
            yield return new object[] { RandomDouble(false), RandomPhase(false), 1, 0 }; // x * 1 = x

            yield return new object[] { 0, 1, 0, 1 }; // i * x
            yield return new object[] { 0, 1, RandomDouble(false), RandomPhase(false) }; // i * x
            yield return new object[] { RandomDouble(false), RandomPhase(false), 0, 1 }; // x * i

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +, +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(true), SmallRandomDouble(false) }; // +, +, -, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(true) }; // +, +, +, -
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(true), SmallRandomDouble(true) }; // +, +, -, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false), SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +, -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false), SmallRandomDouble(false), SmallRandomDouble(true) }; // -, +, +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false), SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +, -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false), SmallRandomDouble(true), SmallRandomDouble(true) }; // -, +, -, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true), SmallRandomDouble(false), SmallRandomDouble(true) }; // -, -, +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true), SmallRandomDouble(true), SmallRandomDouble(true) }; // -, -, -, -

            // Boundary values
            yield return new object[] { double.MaxValue, double.MaxValue, SmallRandomDouble(false), SmallRandomDouble(false) };
            yield return new object[] { double.MinValue, double.MinValue, SmallRandomDouble(false), SmallRandomDouble(false) };

            // Invalid real
            yield return new object[] { double.PositiveInfinity, RandomDouble(false), RandomDouble(false), RandomDouble(false) };
            yield return new object[] { double.PositiveInfinity, RandomDouble(false), RandomDouble(true), RandomDouble(false) };

            yield return new object[] { double.NegativeInfinity, RandomDouble(false), RandomDouble(false), RandomDouble(false) };
            yield return new object[] { double.NegativeInfinity, RandomDouble(false), RandomDouble(true), RandomDouble(false) };

            yield return new object[] { double.NaN, RandomDouble(false), RandomDouble(false), RandomDouble(false) };

            // Invalid imaginary
            yield return new object[] { RandomDouble(false), double.PositiveInfinity, RandomDouble(false), RandomDouble(false) };
            yield return new object[] { RandomDouble(false), double.PositiveInfinity, RandomDouble(false), RandomDouble(true) };

            yield return new object[] { RandomDouble(false), double.NegativeInfinity, RandomDouble(false), RandomDouble(false) };
            yield return new object[] { RandomDouble(false), double.NegativeInfinity, RandomDouble(false), RandomDouble(true) };

            yield return new object[] { RandomDouble(false), double.NaN, RandomDouble(false), RandomDouble(false) };
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
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("Binary Multiply test = ({0}, {1}) * ({2}, {3})", realLeft, imaginaryLeft, realRight, imaginaryRight));

            // Static method
            result = Complex.Multiply(left, right);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("Multiply (Static) test = ({0}, {1}) * ({2}, {3})", realLeft, imaginaryLeft, realRight, imaginaryRight));
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

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomDouble(false), RandomDouble(false) }; // +, +
                yield return new object[] { RandomDouble(false), RandomDouble(true) }; // +, -
                yield return new object[] { RandomDouble(true), RandomDouble(false) }; // -, +
                yield return new object[] { RandomDouble(true), RandomDouble(true) }; // -, -
            }

            // Invalid values
            foreach (double invalidReal in s_invalidDoubleValues)
            {
                yield return new object[] { invalidReal, RandomDouble(false) }; // Invalid real
                yield return new object[] { invalidReal, RandomDouble(true) }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomDouble(false), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { RandomDouble(true), invalidImaginary }; // Invalid imaginary
                    yield return new object[] { invalidReal, invalidImaginary }; // Invalid real, invalid imaginary
                }
            }
        }

        [Theory, MemberData("Negate_TestData")]
        public static void Negate(double real, double imaginary)
        {
            var complex = new Complex(real, imaginary);

            Complex result = -complex;
            VerifyRealImaginaryProperties(result, -complex.Real, -complex.Imaginary,
                string.Format("Unary Minus Error -{0} does not equal to {1} with ({2}, {3})!", complex, result, real, imaginary));

            result = Complex.Negate(complex);
            VerifyRealImaginaryProperties(result, -complex.Real, -complex.Imaginary,
                string.Format("Negate Error -{0} does not equal to {1} with ({2}, {3})!", complex, result, real, imaginary));
        }

        public static IEnumerable<object[]> Pow_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            yield return new object[] { RandomDouble(false), RandomDouble(false) }; // +, +
            yield return new object[] { RandomDouble(false), RandomDouble(true) }; // +, -
            yield return new object[] { RandomDouble(true), RandomDouble(false) }; // -, +
            yield return new object[] { RandomDouble(true), RandomDouble(true) }; // -, -

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

            VerifyPow_Complex_Double(real, imaginary, SmallRandomDouble(false)); // +
            VerifyPow_Complex_Double(real, imaginary, SmallRandomDouble(true)); // -

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
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("Pow (({0}, {1}), {2})", realValue, imaginaryValue, power));
        }

        private static void Pow_Complex_Complex(double real, double imaginary)
        {
            VerifyPow_Complex_Complex(real, imaginary, 0, 0);
            VerifyPow_Complex_Complex(real, imaginary, 1, 0);
            VerifyPow_Complex_Complex(real, imaginary, 0, 1);
            VerifyPow_Complex_Complex(real, imaginary, 0, -1);

            VerifyPow_Complex_Complex(real, imaginary, SmallRandomDouble(false), SmallRandomDouble(false)); // +, +
            VerifyPow_Complex_Complex(real, imaginary, SmallRandomDouble(false), SmallRandomDouble(true)); // +, -
            VerifyPow_Complex_Complex(real, imaginary, SmallRandomDouble(true), SmallRandomDouble(false)); // -, +
            VerifyPow_Complex_Complex(real, imaginary, SmallRandomDouble(true), SmallRandomDouble(true)); // -, -
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
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("Pow (({0}, {1}), ({2}, {3}))", realValue, imaginaryValue, realPower, imaginaryPower));
        }

        public static IEnumerable<object[]> Reciprocal_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, -1 };

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // -, -

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
                yield return new object[] { invalidReal, RandomDouble(false) }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomDouble(false), invalidImaginary }; // Invalid imaginary
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

            VerifyRealImaginaryProperties(result, expected.Real, expected.Imaginary,
                string.Format("Reciprocal ({0}, {1}) Actual: {2}, Expected: {3}", real, imaginary, result, expected));
        }

        public static IEnumerable<object[]> Sin_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // -, -
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
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("Cos({0}):{1} != ({2},{3})", complex, result, expectedReal, expectedImaginary));
        }

        public static IEnumerable<object[]> Sinh_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // Positive, positive
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // Positive, negative
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // Negative, positive
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // Negative, Negative
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
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary, string.Format("Sinh({0})", complex));
        }

        public static IEnumerable<object[]> Subtract_TestData()
        {
            yield return new object[] { RandomDouble(false), RandomDouble(false), 0, 0 }; // x - 0 = x
            yield return new object[] { 0, 0, RandomDouble(false), RandomDouble(false) }; // 0 - x = -x

            yield return new object[] { RandomDouble(false), RandomDouble(false), RandomDouble(false), RandomDouble(false) }; // +, +, +, +
            yield return new object[] { RandomDouble(false), RandomDouble(false), RandomDouble(true), RandomDouble(false) }; // +, +, -, +
            yield return new object[] { RandomDouble(false), RandomDouble(false), RandomDouble(false), RandomDouble(true) }; // +, +, +, -
            yield return new object[] { RandomDouble(false), RandomDouble(false), RandomDouble(true), RandomDouble(true) }; // +, +, -, -
            yield return new object[] { RandomDouble(true), RandomDouble(false), RandomDouble(true), RandomDouble(false) }; // -, +, +, -
            yield return new object[] { RandomDouble(true), RandomDouble(false), RandomDouble(true), RandomDouble(true) }; // -, +, -, -
            yield return new object[] { RandomDouble(true), RandomDouble(false), RandomDouble(false), RandomDouble(true) }; // -, +, +, -
            yield return new object[] { RandomDouble(true), RandomDouble(false), RandomDouble(true), RandomDouble(false) }; // -, +, -, +
            yield return new object[] { RandomDouble(true), RandomDouble(true), RandomDouble(false), RandomDouble(true) }; // -, -, +, -
            yield return new object[] { RandomDouble(true), RandomDouble(true), RandomDouble(true), RandomDouble(true) }; // -, -, -, -

            // Boundary values
            yield return new object[] { double.MaxValue, double.MaxValue, RandomDouble(false), RandomDouble(false) };
            yield return new object[] { double.MinValue, double.MinValue, RandomDouble(false), RandomDouble(false) };
            yield return new object[] { double.MaxValue, double.MaxValue, RandomDouble(true), RandomDouble(true) };
            yield return new object[] { double.MinValue, double.MinValue, RandomDouble(true), RandomDouble(true) };

            yield return new object[] { RandomDouble(false), RandomDouble(false), double.MaxValue, double.MaxValue };
            yield return new object[] { RandomDouble(false), RandomDouble(false), double.MinValue, double.MinValue };
            yield return new object[] { RandomDouble(true), RandomDouble(true), double.MaxValue, double.MaxValue };
            yield return new object[] { RandomDouble(true), RandomDouble(true), double.MinValue, double.MinValue };

            // Invalid real
            yield return new object[] { double.PositiveInfinity, RandomDouble(false), RandomDouble(false), RandomDouble(false) };
            yield return new object[] { double.PositiveInfinity, RandomDouble(false), RandomDouble(true), RandomDouble(false) };

            yield return new object[] { double.NegativeInfinity, RandomDouble(false), RandomDouble(false), RandomDouble(false) };
            yield return new object[] { double.NegativeInfinity, RandomDouble(false), RandomDouble(true), RandomDouble(false) };

            yield return new object[] { double.NaN, RandomDouble(false), RandomDouble(false), RandomDouble(false) };

            // Invalid imaginary
            yield return new object[] { RandomDouble(false), double.PositiveInfinity, RandomDouble(false), RandomDouble(false) };
            yield return new object[] { RandomDouble(false), double.PositiveInfinity, RandomDouble(false), RandomDouble(true) };

            yield return new object[] { RandomDouble(false), double.NegativeInfinity, RandomDouble(false), RandomDouble(false) };
            yield return new object[] { RandomDouble(false), double.NegativeInfinity, RandomDouble(false), RandomDouble(true) };

            yield return new object[] { RandomDouble(false), double.NaN, RandomDouble(false), RandomDouble(false) };
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
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("Binary Minus test = ({0}, {1}) - ({2}, {3})", realLeft, imaginaryLeft, realRight, imaginaryRight));

            // Static method
            result = Complex.Subtract(left, right);
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("Subtract test = ({0}, {1}) - ({2}, {3})", realLeft, imaginaryLeft, realRight, imaginaryRight));
        }

        public static IEnumerable<object[]> Tan_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // -, -
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
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary,
                string.Format("Cos({0}):{1} != ({2},{3})", complex, result, expectedReal, expectedImaginary));
        }

        public static IEnumerable<object[]> Tanh_Basic_TestData()
        {
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 0 };
            yield return new object[] { -1, 0 };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, -1 };

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // Positive, positive
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // Positive, negative
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // Negative, positive
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // Negative, Negative

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
            VerifyRealImaginaryProperties(result, expectedReal, expectedImaginary, string.Format("Tanh({0})", complex));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { Complex.Zero.Real, Complex.Zero.Imaginary };
            yield return new object[] { Complex.One.Real, Complex.One.Imaginary };
            yield return new object[] { Complex.ImaginaryOne.Real, Complex.ImaginaryOne.Imaginary };
            yield return new object[] { (-Complex.One).Real, (-Complex.One).Imaginary };
            yield return new object[] { (-Complex.ImaginaryOne).Real, (-Complex.ImaginaryOne).Imaginary };

            yield return new object[] { RandomDouble(false), RandomDouble(false) }; // +, +
            yield return new object[] { RandomDouble(false), RandomDouble(true) }; // +, -
            yield return new object[] { RandomDouble(true), RandomDouble(false) }; // -, +
            yield return new object[] { RandomDouble(true), RandomDouble(true) }; // -, -

            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(false) }; // +, +
            yield return new object[] { SmallRandomDouble(false), SmallRandomDouble(true) }; // +, -
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(false) }; // -, +
            yield return new object[] { SmallRandomDouble(true), SmallRandomDouble(true) }; // -, -

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
                yield return new object[] { invalidReal, RandomDouble(false) }; // Invalid real
                foreach (double invalidImaginary in s_invalidDoubleValues)
                {
                    yield return new object[] { RandomDouble(true), invalidImaginary }; // Invalid imaginary
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

            foreach (string format in s_supportedStdNumericFormats)
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

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomSByte(false) }; // +
                yield return new object[] { RandomSByte(true) }; // -
            }
        }

        [Theory, MemberData("Cast_SByte_TestData")]
        public static void Cast_SByte(sbyte value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0,
                string.Format("SByteImplicitCast ({0})", value));

            if (value != sbyte.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0,
                    string.Format("Plus + SByteImplicitCast ({0})", value));
            }
            if (value != sbyte.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0,
                    string.Format("Minus - SByteImplicitCast + 1 ({0})", value));
            }
        }

        public static IEnumerable<object[]> Cast_Int16_TestData()
        {
            yield return new object[] { short.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { short.MaxValue };

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomInt16(false) }; // +
                yield return new object[] { RandomInt16(true) }; // -
            }
        }

        [Theory, MemberData("Cast_Int16_TestData")]
        public static void Cast_Int16(short value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0,
                string.Format("Int16ImplicitCast ({0})", value));

            if (value != short.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0,
                    string.Format("Plus + Int16ImplicitCast ({0})", value));
            }
            if (value != short.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0,
                    string.Format("Minus - Int16ImplicitCast + 1 ({0})", value));
            }
        }

        public static IEnumerable<object[]> Cast_Int32_TestData()
        {
            yield return new object[] { int.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { int.MaxValue };

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomInt32(false) }; // +
                yield return new object[] { RandomInt32(true) }; // -
            }
        }

        [Theory, MemberData("Cast_Int32_TestData")]
        public static void Cast_Int32(int value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0,
                string.Format("Int32ImplicitCast ({0})", value));

            if (value != int.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0,
                    string.Format("Plus + Int32ImplicitCast ({0})", value));
            }
            if (value != int.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0,
                    string.Format("Minus - Int32ImplicitCast + 1 ({0})", value));
            }
        }

        public static IEnumerable<object[]> Cast_Int64_TestData()
        {
            yield return new object[] { long.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { long.MaxValue };

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomInt64(false) }; // +
                yield return new object[] { RandomInt64(true) }; // -
            }
        }

        [Theory, MemberData("Cast_Int64_TestData")]
        public static void Cast_Int64(long value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0,
                string.Format("Int64ImplicitCast ({0})", value));

            if (value != long.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0,
                    string.Format("Plus + Int64ImplicitCast ({0})", value));
            }
            if (value != long.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0,
                    string.Format("Minus - Int64ImplicitCast + 1 ({0})", value));
            }
        }

        public static IEnumerable<object[]> Cast_Byte_TestData()
        {
            yield return new object[] { byte.MinValue };
            yield return new object[] { 1 };
            yield return new object[] { byte.MaxValue };

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomByte() };
            }
        }

        [Theory, MemberData("Cast_Byte_TestData")]
        public static void Cast_Byte(byte value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0,
                string.Format("ByteImplicitCast ({0})", value));

            if (value != byte.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0,
                    string.Format("Plus + ByteImplicitCast ({0})", value));
            }
            if (value != byte.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0,
                    string.Format("Minus - ByteImplicitCast + 1 ({0})", value));
            }
        }

        public static IEnumerable<object[]> Cast_UInt16_TestData()
        {
            yield return new object[] { ushort.MinValue };
            yield return new object[] { 1 };
            yield return new object[] { ushort.MaxValue };

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomUInt16() };
            }
        }

        [Theory, MemberData("Cast_UInt16_TestData")]
        public static void Cast_UInt16(ushort value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0,
                string.Format("UInt16ImplicitCast ({0})", value));

            if (value != ushort.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0,
                    string.Format("Plus + UInt16ImplicitCast ({0})", value));
            }
            if (value != ushort.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0,
                    string.Format("Minus - UInt16ImplicitCast + 1 ({0})", value));
            }
        }

        public static IEnumerable<object[]> Cast_UInt32_TestData()
        {
            yield return new object[] { uint.MinValue };
            yield return new object[] { 1 };
            yield return new object[] { uint.MaxValue };

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomUInt32() };
            }
        }

        [Theory, MemberData("Cast_UInt32_TestData")]
        public static void Cast_UInt32(uint value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0,
                string.Format("UInt32ImplicitCast ({0})", value));

            if (value != uint.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0,
                    string.Format("Plus + UInt32ImplicitCast ({0})", value));
            }
            if (value != uint.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0,
                    string.Format("Minus - UInt32ImplicitCast + 1 ({0})", value));
            }
        }

        public static IEnumerable<object[]> Cast_UInt64_TestData()
        {
            yield return new object[] { ulong.MinValue };
            yield return new object[] { 1 };
            yield return new object[] { ulong.MaxValue };

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomUInt64() };
            }
        }

        [Theory, MemberData("Cast_UInt64_TestData")]
        public static void Cast_UInt64(ulong value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0,
                string.Format("UInt64ImplicitCast ({0})", value));

            if (value != ulong.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0,
                    string.Format("Plus + UInt64ImplicitCast ({0})", value));
            }
            if (value != ulong.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0,
                    string.Format("Minus - UInt64ImplicitCast + 1 ({0})", value));
            }
        }

        public static IEnumerable<object[]> Cast_Single_TestData()
        {
            yield return new object[] { float.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { float.MaxValue };

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomSingle(false) }; // +
                yield return new object[] { RandomSingle(true) }; // -
            }
        }

        [Theory, MemberData("Cast_Single_TestData")]
        public static void Cast_Single(float value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0,
                string.Format("SingleImplicitCast ({0})", value));

            if (value != float.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0,
                    string.Format("Plus + SingleImplicitCast ({0})", value));
            }
            if (value != float.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0,
                    string.Format("Minus - SingleImplicitCast + 1 ({0})", value));
            }
        }

        public static IEnumerable<object[]> Cast_Double_TestData()
        {
            yield return new object[] { double.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { double.MaxValue };

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomDouble(false) }; // +
                yield return new object[] { RandomDouble(true) }; // -
            }
        }

        [Theory, MemberData("Cast_Double_TestData")]
        public static void Cast_Double(double value)
        {
            Complex complex = value;
            VerifyRealImaginaryProperties(complex, value, 0,
                string.Format("DoubleImplicitCast ({0})", value));

            if (value != double.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, value + 1, 0,
                    string.Format("Plus + DoubleImplicitCast ({0})", value));
            }
            if (value != double.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, value - 1, 0,
                    string.Format("Minus - DoubleImplicitCast + 1 ({0})", value));
            }
        }

        public static IEnumerable<object[]> Cast_BigInteger_TestData()
        {
            yield return new object[] { (BigInteger)double.MinValue };
            yield return new object[] { (BigInteger)(-1) };
            yield return new object[] { (BigInteger)0 };
            yield return new object[] { (BigInteger)1 };
            yield return new object[] { (BigInteger)double.MaxValue };

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomBigInteger(false) }; // +
                yield return new object[] { RandomBigInteger(true) }; // -
            }
        }

        [Theory, MemberData("Cast_BigInteger_TestData")]
        public static void Cast_BigInteger(BigInteger value)
        {
            Complex complex = (Complex)value;
            VerifyRealImaginaryProperties(complex, (double)value, 0,
                string.Format("DoubleImplicitCast ({0})", value));

            if (value != (BigInteger)double.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, (double)(value + 1), 0,
                    string.Format("Plus + DoubleImplicitCast ({0})", value));
            }
            if (value != (BigInteger)double.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, (double)(value - 1), 0,
                    string.Format("Minus - DoubleImplicitCast + 1 ({0})", value));
            }
        }

        public static IEnumerable<object[]> Cast_Decimal_TestData()
        {
            yield return new object[] { decimal.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { decimal.MaxValue };

            for (int i = 0; i < 3; i++)
            {
                yield return new object[] { RandomDecimal(false) }; // +
                yield return new object[] { RandomDecimal(true) }; // -
            }
        }

        [Theory, MemberData("Cast_Decimal_TestData")]
        public static void Cast_Decimal(decimal value)
        {
            Complex complex = (Complex)value;
            VerifyRealImaginaryProperties(complex, (double)value, 0,
                string.Format("DoubleImplicitCast ({0})", value));

            if (value != decimal.MaxValue)
            {
                Complex addition = complex + 1;
                VerifyRealImaginaryProperties(addition, (double)(value + 1), 0,
                    string.Format("Plus + DoubleImplicitCast ({0})", value));
            }
            if (value != decimal.MinValue)
            {
                Complex minus = complex - 1;
                VerifyRealImaginaryProperties(minus, (double)(value - 1), 0,
                    string.Format("Minus - DoubleImplicitCast + 1 ({0})", value));
            }
        }

        public static sbyte RandomSByte(bool makeNegative)
        {
            if (makeNegative)
            {
                return ((sbyte)s_random.Next(sbyte.MinValue, 0));
            }
            else
            {
                return ((sbyte)s_random.Next(1, sbyte.MaxValue));
            }
        }

        public static short RandomInt16(bool makeNegative)
        {
            if (makeNegative)
            {
                return (short)s_random.Next(short.MinValue, 0);
            }
            else
            {
                return (short)s_random.Next(1, short.MaxValue);
            }
        }

        public static int RandomInt32(bool makeNegative)
        {
            return (int)RandomValue(int.MaxValue, makeNegative);
        }

        public static long RandomInt64(bool makeNegative)
        {
            return (long)RandomValue(long.MaxValue, makeNegative);
        }

        public static byte RandomByte()
        {
            return (byte)s_random.Next(1, byte.MaxValue);
        }

        public static ushort RandomUInt16()
        {
            return (ushort)s_random.Next(1, ushort.MaxValue);
        }

        public static uint RandomUInt32()
        {
            return (uint)RandomValue(uint.MaxValue, false);
        }

        public static ulong RandomUInt64()
        {
            return (ulong)RandomValue(ulong.MaxValue, false);
        }

        private static double SmallRandomDouble(bool makeNegative)
        {
            return RandomValue(1, makeNegative);
        }

        public static double RandomDouble(bool makeNegative)
        {
            return RandomValue(double.MaxValue, makeNegative);
        }

        public static float RandomSingle(bool makeNegative)
        {
            return (float)RandomValue(float.MaxValue, makeNegative);
        }

        public static BigInteger RandomBigInteger(bool makeNegative)
        {
            return (BigInteger)RandomValue(double.MaxValue, makeNegative);
        }

        public static decimal RandomDecimal(bool makeNegative)
        {
            return new decimal(
                s_random.Next(int.MinValue, int.MaxValue),
                s_random.Next(int.MinValue, int.MaxValue),
                s_random.Next(int.MinValue, int.MaxValue),
                makeNegative,
                (byte)s_random.Next(0, 29));
        }

        public static double RandomPhase(bool makeNegative)
        {
            return RandomValue(Math.PI / 2, makeNegative);
        }

        private static double RandomValue(double mult, bool makeNegative)
        {
            double randomDouble = (mult * s_random.NextDouble());
            randomDouble %= mult;
            return makeNegative ? -randomDouble : randomDouble;
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

        private static void VerifyRealImaginaryProperties(Complex complex, double real, double imaginary, string message)
        {
            Assert.True(real.Equals(complex.Real) || IsDiffTolerable(complex.Real, real), message);
            Assert.True(imaginary.Equals(complex.Imaginary) || IsDiffTolerable(complex.Imaginary, imaginary), message);
        }

        private static void VerifyMagnitudePhaseProperties(Complex complex, double magnitude, double phase, string message)
        {
            // The magnitude (m) of a complex number (z = x + yi) is the absolute value - |z| = sqrt(x^2 + y^2)
            // Verification is done using the square of the magnitude since m^2 = x^2 + y^2
            double expectedMagnitudeSqr = magnitude * magnitude;
            double actualMagnitudeSqr = complex.Magnitude * complex.Magnitude;

            Assert.True(expectedMagnitudeSqr.Equals(actualMagnitudeSqr) || IsDiffTolerable(actualMagnitudeSqr, expectedMagnitudeSqr), message);

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

            Assert.True(phase.Equals(complex.Phase) || IsDiffTolerable(complex.Phase, phase), message);
        }
    }
}
