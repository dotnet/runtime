// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_ATanTest
    {
        private static void VerifyAtan(Double x, Double y)
        {
            // formula used in the feature: Atan(z) = (i/2) * (log(1-iz) - log(1+iz))
            // Verification is done with z = ATan(Tan(z));

            Complex complex = new Complex(x, y);
            Complex tanComplex = Complex.Tan(complex);
            Complex atanComplex = Complex.Atan(tanComplex);

            if (false == Support.VerifyRealImaginaryProperties(atanComplex, x, y))
            {
                Console.WriteLine("Error aTaN-ErraT6A81N! ({0}) != ATan(Tan():{1}):{2}", complex, tanComplex, atanComplex);

                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifyAtan(Double x, Double y, Double expectedReal, Double expectedImaginary)
        {
            // if ATan verification formula returns invalid Values
            Complex complex = new Complex(x, y);
            Complex atanComplex = Complex.Atan(complex);

            if (false == Support.VerifyRealImaginaryProperties(atanComplex, expectedReal, expectedImaginary))
            {
                Console.WriteLine("Error AtAn-Err3A6s1N! ATan({0})", complex, atanComplex);

                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify sqrt with Zero
            VerifyAtan(0.0, 0.0);

            // Verify test results with One
            VerifyAtan(1.0, 0.0);

            // Verify test results with MinusOne
            VerifyAtan(-1.0, 0.0);

            // Verify test results with ImaginaryOne
            // Undefined - NaN should be propagated
            VerifyAtan(0.0, 1.0);

            // Verify test results with MinusImaginaryOne
            // Undefined - NaN should be propagated
            VerifyAtan(0.0, -1.0);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyAtan(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyAtan(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyAtan(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyAtan(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with Max
            VerifyAtan(Double.MaxValue, Double.MaxValue, Double.NaN, Double.NaN);

            // Verify test results with MaxReal
            VerifyAtan(Double.MaxValue, 0, Math.PI / 2, 0);

            // Verify test results with MaxImg
            VerifyAtan(0, Double.MaxValue, Math.PI / 2, 0);

            // Verify test results with Min
            VerifyAtan(Double.MinValue, Double.MinValue, Double.NaN, Double.NaN);

            // Verify test results with MinReal
            VerifyAtan(Double.MinValue, 0, -Math.PI / 2, 0);

            // Verify test results with MinImaginary
            VerifyAtan(0.0, Double.MinValue, -Math.PI / 2, 0);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyAtan(1.0, Double.PositiveInfinity, Double.NaN, Double.NaN);
            VerifyAtan(1.0, Double.NegativeInfinity, Double.NaN, Double.NaN);
            VerifyAtan(1.0, Double.NaN, Double.NaN, Double.NaN);

            // Verify test results with invalid Real
            VerifyAtan(Double.PositiveInfinity, 1.0, Double.NaN, Double.NaN);
            VerifyAtan(Double.NegativeInfinity, 1.0, Double.NaN, Double.NaN);
            VerifyAtan(Double.NaN, 1.0, Double.NaN, Double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (Double invalidReal in Support.doubleInvalidValues)
            {
                foreach (Double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyAtan(invalidReal, invalidImaginary, Double.NaN, Double.NaN);
                }
            }
        }
    }
}
