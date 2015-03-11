// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_ASinTest
    {
        private static void VerifyASin(double x, double y)
        {
            // formula used in the feature: arcsin(z) = -iln(iz + Sqrt(1-z*z))
            // Verification is done with z = ASin(Sin(z));

            Complex complex = new Complex(x, y);
            Complex sinComplex = Complex.Sin(complex);
            Complex asinComplex = Complex.Asin(sinComplex);

            Support.VerifyRealImaginaryProperties(asinComplex, x, y, 
                string.Format("({0}) != ASin(Sin():{1}):{2}", complex, sinComplex, asinComplex));
        }

        private static void VerifyASin(double x, double y, double expectedReal, double expectedImaginary)
        {
            // if ASin verification formula returns invalid Values

            Complex complex = new Complex(x, y);
            Complex asinComplex = Complex.Asin(complex);

            Support.VerifyRealImaginaryProperties(asinComplex, expectedReal, expectedImaginary, 
                string.Format("ASin({0}):{1}", complex, asinComplex));
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
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            VerifyASin(double.MaxValue, double.MaxValue, double.NaN, double.NaN);

            // Verify test results with MaxReal
            VerifyASin(double.MaxValue, 0.0, double.NaN, double.NaN);

            // Verify test results with MaxImg
            VerifyASin(0.0, double.MaxValue, double.NaN, double.NaN);

            // Verify test results with Min
            VerifyASin(double.MinValue, double.MinValue, double.NaN, double.NaN);

            // Verify test results with MinReal
            VerifyASin(double.MinValue, 0.0, double.NaN, double.NaN);

            // Verify test results with MinImaginary
            VerifyASin(0.0, double.MinValue, double.NaN, double.NaN);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyASin(1.0, double.PositiveInfinity, double.NaN, double.NaN);
            VerifyASin(1.0, double.NegativeInfinity, double.NaN, double.NaN);
            VerifyASin(1.0, double.NaN, double.NaN, double.NaN);

            // Verify test results with invalid Real
            VerifyASin(double.PositiveInfinity, 1.0, double.NaN, double.NaN);
            VerifyASin(double.NegativeInfinity, 1.0, double.NaN, double.NaN);
            VerifyASin(double.NaN, 1.0, double.NaN, double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (double invalidReal in Support.doubleInvalidValues)
            {
                foreach (double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyASin(invalidReal, invalidImaginary, double.NaN, double.NaN);
                }
            }
        }

        [Fact]
        public static void RunTests_EdgeCases()
        {
            // Verify edge cases where imaginary part is zero and real part is negative.
            VerifyASin(-1234000000, 0, -1.5707963267948966, 21.62667394298955);

            // Verify edge cases where imaginary part is positive.
            VerifyASin(0, 1234000000, 0, 21.62667394298955);
        }
    }
}
