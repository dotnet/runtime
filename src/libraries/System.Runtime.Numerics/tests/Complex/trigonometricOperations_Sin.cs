// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_SinTest
    {
        private static void VerifySin(double x, double y, double expectedReal, double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex sinComplex = Complex.Sin(complex);

            Support.VerifyRealImaginaryProperties(sinComplex, expectedReal, expectedImaginary,
                string.Format("Sin({0}):{1} != ({2},{3})", complex, sinComplex, expectedReal, expectedImaginary));
        }

        private static void VerifySin(double x, double y)
        {
            // the product formula: sin (x+iy) = sin(x)*cosh(y) + icos(x)sinh(y)
            // the verification formula: sin (z) = (Complex.Exp(i*z) - Complex.Exp(-i*z)) / (2*i)
            // the verification formula is used not for the boundary values

            Complex z = new Complex(x, y);
            Complex temp = Complex.ImaginaryOne * z;
            Complex expectedComplex = (Complex.Exp(temp) - Complex.Exp(-temp)) / (2 * Complex.ImaginaryOne);
            VerifySin(x, y, expectedComplex.Real, expectedComplex.Imaginary);
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify sqrt with Zero
            VerifySin(0.0, 0.0);

            // Verify test results with One
            VerifySin(1.0, 0.0);

            // Verify test results with MinusOne
            VerifySin(-1.0, 0.0);

            // Verify test results with ImaginaryOne
            VerifySin(0.0, 1.0);

            // Verify test results with MinusImaginaryOne
            VerifySin(0.0, -1.0);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifySin(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifySin(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifySin(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifySin(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with Max
            VerifySin(double.MaxValue, double.MaxValue, double.PositiveInfinity, Math.Cos(double.MaxValue) * double.PositiveInfinity); //for IA64

            // Verify test results with MaxReal
            VerifySin(double.MaxValue, 0.0, Math.Sin(double.MaxValue), 0); //for IA64

            // Verify test results with MaxImg
            VerifySin(0.0, double.MaxValue, double.NaN, double.PositiveInfinity);

            // Verify test results with Min
            VerifySin(double.MinValue, double.MinValue, double.NegativeInfinity, double.PositiveInfinity);

            // Verify test results with MinReal
            VerifySin(double.MinValue, 0.0, Math.Sin(double.MinValue), 0.0); //for IA64

            // Verify test results with MinImaginary
            VerifySin(0.0, double.MinValue, double.NaN, double.NegativeInfinity);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifySin(1.0, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
            VerifySin(1.0, double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity);
            VerifySin(1.0, double.NaN, double.NaN, double.NaN);

            // Verify test results with invalid Real
            VerifySin(double.PositiveInfinity, 1.0, double.NaN, double.NaN);
            VerifySin(double.NegativeInfinity, 1.0, double.NaN, double.NaN);
            VerifySin(double.NaN, 1.0, double.NaN, double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (double invalidReal in Support.doubleInvalidValues)
            {
                foreach (double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifySin(invalidReal, invalidImaginary, double.NaN, double.NaN);
                }
            }
        }
    }
}
