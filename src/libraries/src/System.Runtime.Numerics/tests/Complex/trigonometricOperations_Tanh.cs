// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_TanhTest
    {
        private static void VerifyTanh(Double x, Double y, Double expectedReal, Double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex tanhComplex = Complex.Tanh(complex);

            if (false == Support.VerifyRealImaginaryProperties(tanhComplex, expectedReal, expectedImaginary))
            {
                Console.WriteLine("Error TaNh-ErrT6A81Nh!!! Actual Tanh({0})", complex);

                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifyTanh(Double x, Double y)
        {
            // the product formula: cosh (x+iy) = sinh (x+iy) / cosh (x+iy)
            // the verification formula: Tanh (z) = (Exp(2z) -1) / (Exp(2z)+1)
            // the verification formula is used not for the boundary values

            Complex z = new Complex(x, y);
            Complex z2 = 2 * z;
            Complex expectedComplex = (Complex.Exp(z2) - 1) / (Complex.Exp(z2) + 1);

            VerifyTanh(x, y, expectedComplex.Real, expectedComplex.Imaginary);
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify sqrt with Zero
            VerifyTanh(0.0, 0.0);

            // Verify test results with One
            VerifyTanh(1.0, 0.0);

            // Verify test results with MinusOne
            VerifyTanh(-1.0, 0.0);

            // Verify test results with ImaginaryOne
            VerifyTanh(0.0, 1.0);

            // Verify test results with MinusImaginaryOne
            VerifyTanh(0.0, -1.0);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyTanh(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyTanh(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyTanh(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyTanh(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with Max
            VerifyTanh(Double.MaxValue, Double.MaxValue);

            // Verify test results with MaxReal
            VerifyTanh(Double.MaxValue, 0.0);

            // Verify test results with MaxImg
            VerifyTanh(0.0, Double.MaxValue, 0, Math.Sin(Double.MaxValue) / Math.Cos(Double.MaxValue)); //for IA64

            // Verify test results with Min
            VerifyTanh(Double.MinValue, Double.MinValue);

            // Verify test results with MinReal
            VerifyTanh(Double.MinValue, 0.0, Double.NaN, Double.NaN);

            // Verify test results with MinImaginary
            VerifyTanh(0.0, Double.MinValue, 0, Math.Sin(Double.MinValue) / Math.Cos(Double.MinValue)); //for IA64
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyTanh(1.0, Double.PositiveInfinity, Double.NaN, Double.NaN);
            VerifyTanh(1.0, Double.NegativeInfinity, Double.NaN, Double.NaN);
            VerifyTanh(1.0, Double.NaN, Double.NaN, Double.NaN);

            // Verify test results with invalid Real
            VerifyTanh(Double.PositiveInfinity, 1.0, Double.NaN, Double.NaN);
            VerifyTanh(Double.NegativeInfinity, 1.0, Double.NaN, Double.NaN);
            VerifyTanh(Double.NaN, 1.0, Double.NaN, Double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (Double invalidReal in Support.doubleInvalidValues)
            {
                foreach (Double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyTanh(invalidReal, invalidImaginary, Double.NaN, Double.NaN);
                }
            }
        }
    }
}
