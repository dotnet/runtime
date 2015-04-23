// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_ATanTest
    {
        private static void VerifyAtan(double x, double y)
        {
            // formula used in the feature: Atan(z) = (i/2) * (log(1-iz) - log(1+iz))
            // Verification is done with z = ATan(Tan(z));

            Complex complex = new Complex(x, y);
            Complex tanComplex = Complex.Tan(complex);
            Complex atanComplex = Complex.Atan(tanComplex);

            Support.VerifyRealImaginaryProperties(atanComplex, x, y, 
                string.Format("({0}) != ATan(Tan():{1}):{2}", complex, tanComplex, atanComplex));
        }

        private static void VerifyAtan(double x, double y, double expectedReal, double expectedImaginary)
        {
            // if ATan verification formula returns invalid Values
            Complex complex = new Complex(x, y);
            Complex atanComplex = Complex.Atan(complex);

            Support.VerifyRealImaginaryProperties(atanComplex, expectedReal, expectedImaginary,
                string.Format("ATan({0}):{1}", complex, atanComplex));
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
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            VerifyAtan(double.MaxValue, double.MaxValue, double.NaN, double.NaN);

            // Verify test results with MaxReal
            VerifyAtan(double.MaxValue, 0, Math.PI / 2, 0);

            // Verify test results with MaxImg
            VerifyAtan(0, double.MaxValue, Math.PI / 2, 0);

            // Verify test results with Min
            VerifyAtan(double.MinValue, double.MinValue, double.NaN, double.NaN);

            // Verify test results with MinReal
            VerifyAtan(double.MinValue, 0, -Math.PI / 2, 0);

            // Verify test results with MinImaginary
            VerifyAtan(0.0, double.MinValue, -Math.PI / 2, 0);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyAtan(1.0, double.PositiveInfinity, double.NaN, double.NaN);
            VerifyAtan(1.0, double.NegativeInfinity, double.NaN, double.NaN);
            VerifyAtan(1.0, double.NaN, double.NaN, double.NaN);

            // Verify test results with invalid Real
            VerifyAtan(double.PositiveInfinity, 1.0, double.NaN, double.NaN);
            VerifyAtan(double.NegativeInfinity, 1.0, double.NaN, double.NaN);
            VerifyAtan(double.NaN, 1.0, double.NaN, double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (double invalidReal in Support.doubleInvalidValues)
            {
                foreach (double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyAtan(invalidReal, invalidImaginary, double.NaN, double.NaN);
                }
            }
        }
    }
}
