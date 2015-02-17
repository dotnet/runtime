// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_SinTest
    {
        private static void VerifySin(Double x, Double y, Double expectedReal, Double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex sinComplex = Complex.Sin(complex);

            if (false == Support.VerifyRealImaginaryProperties(sinComplex, expectedReal, expectedImaginary))
            {
                Console.WriteLine("Error sIn-Err36s1N! Sin({0}):{1} != ({2},{3})", complex, sinComplex, expectedReal, expectedImaginary);

                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifySin(Double x, Double y)
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
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            VerifySin(Double.MaxValue, Double.MaxValue, Double.PositiveInfinity, Math.Cos(Double.MaxValue) * Double.PositiveInfinity); //for IA64

            // Verify test results with MaxReal
            VerifySin(Double.MaxValue, 0.0, Math.Sin(Double.MaxValue), 0); //for IA64

            // Verify test results with MaxImg
            VerifySin(0.0, Double.MaxValue, Double.NaN, Double.PositiveInfinity);

            // Verify test results with Min
            VerifySin(Double.MinValue, Double.MinValue, Double.NegativeInfinity, Double.PositiveInfinity);

            // Verify test results with MinReal
            VerifySin(Double.MinValue, 0.0, Math.Sin(Double.MinValue), 0.0); //for IA64

            // Verify test results with MinImaginary
            VerifySin(0.0, Double.MinValue, Double.NaN, Double.NegativeInfinity);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifySin(1.0, Double.PositiveInfinity, Double.PositiveInfinity, Double.PositiveInfinity);
            VerifySin(1.0, Double.NegativeInfinity, Double.PositiveInfinity, Double.NegativeInfinity);
            VerifySin(1.0, Double.NaN, Double.NaN, Double.NaN);

            // Verify test results with invalid Real
            VerifySin(Double.PositiveInfinity, 1.0, Double.NaN, Double.NaN);
            VerifySin(Double.NegativeInfinity, 1.0, Double.NaN, Double.NaN);
            VerifySin(Double.NaN, 1.0, Double.NaN, Double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (Double invalidReal in Support.doubleInvalidValues)
            {
                foreach (Double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifySin(invalidReal, invalidImaginary, Double.NaN, Double.NaN);
                }
            }
        }
    }
}
