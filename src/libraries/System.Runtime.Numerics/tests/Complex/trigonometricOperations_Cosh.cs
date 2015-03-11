// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_CoshTest
    {
        private static void VerifyCosh(double x, double y, double expectedReal, double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex coshComplex = Complex.Cosh(complex);

            Support.VerifyRealImaginaryProperties(coshComplex, expectedReal, expectedImaginary, 
                string.Format("Cosh({0})", complex));
        }

        private static void VerifyCosh(double x, double y)
        {
            // the product formula: cosh (x+iy) = cosh(x)*cos(y) + isinh(x)*sin(y) 
            // the verification formula: Cosh (z) = (Exp(z) + Exp(-z))/2
            // the verification formula is used not for the boundary values

            Complex z = new Complex(x, y);
            Complex expectedComplex = 0.5 * (Complex.Exp(z) + Complex.Exp(-z));

            VerifyCosh(x, y, expectedComplex.Real, expectedComplex.Imaginary);
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify sqrt with Zero
            VerifyCosh(0.0, 0.0);

            // Verify test results with One
            VerifyCosh(1.0, 0.0);

            // Verify test results with MinusOne
            VerifyCosh(-1.0, 0.0);

            // Verify test results with ImaginaryOne
            VerifyCosh(0.0, 1.0);

            // Verify test results with MinusImaginaryOne
            VerifyCosh(0.0, -1.0);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyCosh(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyCosh(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyCosh(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyCosh(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with Max
            VerifyCosh(double.MaxValue, double.MaxValue, Math.Cos(double.MaxValue) * double.PositiveInfinity, double.PositiveInfinity); //for IA64

            // Verify test results with MaxReal
            VerifyCosh(double.MaxValue, 0.0, double.PositiveInfinity, double.NaN);

            // Verify test results with MaxImg
            VerifyCosh(0.0, double.MaxValue, Math.Cos(double.MaxValue), 0); //for IA64

            // Verify test results with Min
            VerifyCosh(double.MinValue, double.MinValue, double.NegativeInfinity, double.PositiveInfinity);

            // Verify test results with MinReal
            VerifyCosh(double.MinValue, 0.0, double.PositiveInfinity, double.NaN);

            // Verify test results with MinImaginary
            VerifyCosh(0.0, double.MinValue, Math.Cos(double.MinValue), 0); //for IA64
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyCosh(1.0, double.PositiveInfinity, double.NaN, double.NaN);
            VerifyCosh(1.0, double.NegativeInfinity, double.NaN, double.NaN);
            VerifyCosh(1.0, double.NaN, double.NaN, double.NaN);

            // Verify test results with invalid Real
            VerifyCosh(double.PositiveInfinity, 1.0, double.PositiveInfinity, double.PositiveInfinity);
            VerifyCosh(double.NegativeInfinity, 1.0, double.PositiveInfinity, double.NegativeInfinity);
            VerifyCosh(double.NaN, 1.0, double.NaN, double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (double invalidReal in Support.doubleInvalidValues)
            {
                foreach (double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyCosh(invalidReal, invalidImaginary, double.NaN, double.NaN);
                }
            }
        }
    }
}
