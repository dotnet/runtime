// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_ASinTest
    {
        private static void VerifyASin(Double x, Double y)
        {
            // formula used in the feature: arcsin(z) = -iln(iz + Sqrt(1-z*z))
            // Verification is done with z = ASin(Sin(z));

            Complex complex = new Complex(x, y);
            Complex sinComplex = Complex.Sin(complex);
            Complex asinComplex = Complex.Asin(sinComplex);

            if (false == Support.VerifyRealImaginaryProperties(asinComplex, x, y))
            {
                Console.WriteLine("Error AsIn-Err3A6s1N! ({0}) != ASin(Sin():{1}):{2}", complex, sinComplex, asinComplex);

                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifyASin(Double x, Double y, Double expectedReal, Double expectedImaginary)
        {
            // if ASin verification formula returns invalid Values

            Complex complex = new Complex(x, y);
            Complex asinComplex = Complex.Asin(complex);

            if (false == Support.VerifyRealImaginaryProperties(asinComplex, expectedReal, expectedImaginary))
            {
                Console.WriteLine("Error AsIn-Err3A6s1N! ASin({0})", complex, asinComplex);

                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify sqrt with Zero
            VerifyASin(0.0, 0.0);

            // Verify test results with One
            VerifyASin(1.0, 0.0);

            // Verify test results with MinusOne
            VerifyASin(-1.0, 0.0);

            // Verify test results with ImaginaryOne
            VerifyASin(0.0, 1.0);

            // Verify test results with MinusImaginaryOne
            VerifyASin(0.0, -1.0);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyASin(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyASin(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyASin(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyASin(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with Max
            VerifyASin(Double.MaxValue, Double.MaxValue, Double.NaN, Double.NaN);

            // Verify test results with MaxReal
            VerifyASin(Double.MaxValue, 0.0, Double.NaN, Double.NaN);

            // Verify test results with MaxImg
            VerifyASin(0.0, Double.MaxValue, Double.NaN, Double.NaN);

            // Verify test results with Min
            VerifyASin(Double.MinValue, Double.MinValue, Double.NaN, Double.NaN);

            // Verify test results with MinReal
            VerifyASin(Double.MinValue, 0.0, Double.NaN, Double.NaN);

            // Verify test results with MinImaginary
            VerifyASin(0.0, Double.MinValue, Double.NaN, Double.NaN);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyASin(1.0, Double.PositiveInfinity, Double.NaN, Double.NaN);
            VerifyASin(1.0, Double.NegativeInfinity, Double.NaN, Double.NaN);
            VerifyASin(1.0, Double.NaN, Double.NaN, Double.NaN);

            // Verify test results with invalid Real
            VerifyASin(Double.PositiveInfinity, 1.0, Double.NaN, Double.NaN);
            VerifyASin(Double.NegativeInfinity, 1.0, Double.NaN, Double.NaN);
            VerifyASin(Double.NaN, 1.0, Double.NaN, Double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (Double invalidReal in Support.doubleInvalidValues)
            {
                foreach (Double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyASin(invalidReal, invalidImaginary, Double.NaN, Double.NaN);
                }
            }
        }
    }
}
