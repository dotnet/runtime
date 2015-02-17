// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_CosTest
    {
        private static void VerifyCos(Double x, Double y, Double expectedReal, Double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex cosComplex = Complex.Cos(complex);

            if (false == Support.VerifyRealImaginaryProperties(cosComplex, expectedReal, expectedImaginary))
            {
                Console.WriteLine("Error cOs-Err36C6s! Cos({0}):{1} != ({2},{3})", complex, cosComplex, expectedReal, expectedImaginary);
                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifyCos(Double x, Double y)
        {
            // the product formula: cos (x+iy) = cos(x)*cosh(y) - isin(x)sinh(y)
            // the verification formula: cos (z) = (Complex.Exp(i*z) + Complex.Exp(-i*z)) / 2
            // the verification formula is used not for the boundary values

            Complex z = new Complex(x, y);
            Complex temp = Complex.ImaginaryOne * z;
            Complex expectedComplex = (Complex.Exp(temp) + Complex.Exp(-temp)) / 2;
            VerifyCos(x, y, expectedComplex.Real, expectedComplex.Imaginary);
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify sqrt with Zero
            VerifyCos(0.0, 0.0);

            // Verify test results with One
            VerifyCos(1.0, 0.0);

            // Verify test results with MinusOne
            VerifyCos(-1.0, 0.0);

            // Verify test results with ImaginaryOne
            VerifyCos(0.0, 1.0);

            // Verify test results with MinusImaginaryOne
            VerifyCos(0.0, -1.0);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyCos(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyCos(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyCos(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyCos(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with Max
            VerifyCos(Double.MaxValue, Double.MaxValue, Math.Cos(Double.MaxValue) * Double.PositiveInfinity, Double.NegativeInfinity); //for IA64

            // Verify test results with MaxReal
            VerifyCos(Double.MaxValue, 0.0, Math.Cos(Double.MaxValue), 0); //for IA64

            // Verify test results with MaxImg
            VerifyCos(0.0, Double.MaxValue, Double.PositiveInfinity, Double.NaN);

            // Verify test results with Min
            VerifyCos(Double.MinValue, Double.MinValue, Double.NegativeInfinity, Double.NegativeInfinity);

            // Verify test results with MinReal
            VerifyCos(Double.MinValue, 0.0, Math.Cos(Double.MinValue), 0); //for IA64

            // Verify test results with MinImaginary
            VerifyCos(0.0, Double.MinValue, Double.PositiveInfinity, Double.NaN);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyCos(1.0, Double.PositiveInfinity, Double.PositiveInfinity, Double.NegativeInfinity);
            VerifyCos(1.0, Double.NegativeInfinity, Double.PositiveInfinity, Double.PositiveInfinity);
            VerifyCos(1.0, Double.NaN, Double.NaN, Double.NaN);

            // Verify test results with invalid Real
            VerifyCos(Double.PositiveInfinity, 1.0, Double.NaN, Double.NaN);
            VerifyCos(Double.NegativeInfinity, 1.0, Double.NaN, Double.NaN);
            VerifyCos(Double.NaN, 1.0, Double.NaN, Double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (Double invalidReal in Support.doubleInvalidValues)
            {
                foreach (Double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyCos(invalidReal, invalidImaginary, Double.NaN, Double.NaN);
                }
            }
        }
    }
}
