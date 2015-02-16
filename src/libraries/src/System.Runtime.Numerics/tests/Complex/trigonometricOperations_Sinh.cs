// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_SinhTest
    {
        private static void VerifySinh(Double x, Double y, Double expectedReal, Double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex sinhComplex = Complex.Sinh(complex);

            if (false == Support.VerifyRealImaginaryProperties(sinhComplex, expectedReal, expectedImaginary))
            {
                Console.WriteLine("Error Sinh-Err3H6s1N!!!! Sinh({0})", complex);

                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifySinh(Double x, Double y)
        {
            // the product formula: sinh (x+iy) = sinh(x)*cos(y) + icosh(x)*sin(y)
            // the verification formula: sinh (z) = (Exp(z) - Exp(-z))/2
            // the verification formula is used not for the boundary values

            Complex z = new Complex(x, y);
            Complex expectedComplex = 0.5 * (Complex.Exp(z) - Complex.Exp(-z));

            VerifySinh(x, y, expectedComplex.Real, expectedComplex.Imaginary);
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify sqrt with Zero
            VerifySinh(0.0, 0.0);

            // Verify test results with One
            VerifySinh(1.0, 0.0);

            // Verify test results with MinusOne
            VerifySinh(-1.0, 0.0);

            // Verify test results with ImaginaryOne
            VerifySinh(0.0, 1.0);

            // Verify test results with MinusImaginaryOne
            VerifySinh(0.0, -1.0);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifySinh(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifySinh(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifySinh(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifySinh(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with Max
            VerifySinh(Double.MaxValue, Double.MaxValue, Math.Cos(Double.MaxValue) * Double.PositiveInfinity, Double.PositiveInfinity); //for IA64

            // Verify test results with MaxReal
            VerifySinh(Double.MaxValue, 0.0, Double.PositiveInfinity, Double.NaN);

            // Verify test results with MaxImg
            VerifySinh(0.0, Double.MaxValue, 0, Math.Sin(Double.MaxValue)); //for IA64

            // Verify test results with Min
            VerifySinh(Double.MinValue, Double.MinValue, Double.PositiveInfinity, Double.NegativeInfinity);

            // Verify test results with MinReal
            VerifySinh(Double.MinValue, 0.0, Double.NegativeInfinity, Double.NaN);

            // Verify test results with MinImaginary
            VerifySinh(0.0, Double.MinValue, 0, Math.Sin(Double.MinValue)); //for IA64
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifySinh(1.0, Double.PositiveInfinity, Double.NaN, Double.NaN);
            VerifySinh(1.0, Double.NegativeInfinity, Double.NaN, Double.NaN);
            VerifySinh(1.0, Double.NaN, Double.NaN, Double.NaN);

            // Verify test results with invalid Real
            VerifySinh(Double.PositiveInfinity, 1.0, Double.PositiveInfinity, Double.PositiveInfinity);
            VerifySinh(Double.NegativeInfinity, 1.0, Double.NegativeInfinity, Double.PositiveInfinity);
            VerifySinh(Double.NaN, 1.0, Double.NaN, Double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (Double invalidReal in Support.doubleInvalidValues)
            {
                foreach (Double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifySinh(invalidReal, invalidImaginary, Double.NaN, Double.NaN);
                }
            }
        }
    }
}
