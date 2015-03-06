// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_CoshTest
    {
        private static void VerifyCosh(Double x, Double y, Double expectedReal, Double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex coshComplex = Complex.Cosh(complex);

            if (false == Support.VerifyRealImaginaryProperties(coshComplex, expectedReal, expectedImaginary))
            {
                Console.WriteLine("Error Cosh-Err3H6s1N!!!! Cosh({0})", complex);

                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifyCosh(Double x, Double y)
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
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            VerifyCosh(Double.MaxValue, Double.MaxValue, Math.Cos(Double.MaxValue) * Double.PositiveInfinity, Double.PositiveInfinity); //for IA64

            // Verify test results with MaxReal
            VerifyCosh(Double.MaxValue, 0.0, Double.PositiveInfinity, Double.NaN);

            // Verify test results with MaxImg
            VerifyCosh(0.0, Double.MaxValue, Math.Cos(Double.MaxValue), 0); //for IA64

            // Verify test results with Min
            VerifyCosh(Double.MinValue, Double.MinValue, Double.NegativeInfinity, Double.PositiveInfinity);

            // Verify test results with MinReal
            VerifyCosh(Double.MinValue, 0.0, Double.PositiveInfinity, Double.NaN);

            // Verify test results with MinImaginary
            VerifyCosh(0.0, Double.MinValue, Math.Cos(Double.MinValue), 0); //for IA64
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyCosh(1.0, Double.PositiveInfinity, Double.NaN, Double.NaN);
            VerifyCosh(1.0, Double.NegativeInfinity, Double.NaN, Double.NaN);
            VerifyCosh(1.0, Double.NaN, Double.NaN, Double.NaN);

            // Verify test results with invalid Real
            VerifyCosh(Double.PositiveInfinity, 1.0, Double.PositiveInfinity, Double.PositiveInfinity);
            VerifyCosh(Double.NegativeInfinity, 1.0, Double.PositiveInfinity, Double.NegativeInfinity);
            VerifyCosh(Double.NaN, 1.0, Double.NaN, Double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (Double invalidReal in Support.doubleInvalidValues)
            {
                foreach (Double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyCosh(invalidReal, invalidImaginary, Double.NaN, Double.NaN);
                }
            }
        }
    }
}
