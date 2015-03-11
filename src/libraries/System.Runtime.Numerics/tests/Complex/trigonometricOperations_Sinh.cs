// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_SinhTest
    {
        private static void VerifySinh(double x, double y, double expectedReal, double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex sinhComplex = Complex.Sinh(complex);

            Support.VerifyRealImaginaryProperties(sinhComplex, expectedReal, expectedImaginary, 
                string.Format("Sinh({0})", complex));
        }

        private static void VerifySinh(double x, double y)
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
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            VerifySinh(double.MaxValue, double.MaxValue, Math.Cos(double.MaxValue) * double.PositiveInfinity, double.PositiveInfinity); //for IA64

            // Verify test results with MaxReal
            VerifySinh(double.MaxValue, 0.0, double.PositiveInfinity, double.NaN);

            // Verify test results with MaxImg
            VerifySinh(0.0, double.MaxValue, 0, Math.Sin(double.MaxValue)); //for IA64

            // Verify test results with Min
            VerifySinh(double.MinValue, double.MinValue, double.PositiveInfinity, double.NegativeInfinity);

            // Verify test results with MinReal
            VerifySinh(double.MinValue, 0.0, double.NegativeInfinity, double.NaN);

            // Verify test results with MinImaginary
            VerifySinh(0.0, double.MinValue, 0, Math.Sin(double.MinValue)); //for IA64
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifySinh(1.0, double.PositiveInfinity, double.NaN, double.NaN);
            VerifySinh(1.0, double.NegativeInfinity, double.NaN, double.NaN);
            VerifySinh(1.0, double.NaN, double.NaN, double.NaN);

            // Verify test results with invalid Real
            VerifySinh(double.PositiveInfinity, 1.0, double.PositiveInfinity, double.PositiveInfinity);
            VerifySinh(double.NegativeInfinity, 1.0, double.NegativeInfinity, double.PositiveInfinity);
            VerifySinh(double.NaN, 1.0, double.NaN, double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (double invalidReal in Support.doubleInvalidValues)
            {
                foreach (double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifySinh(invalidReal, invalidImaginary, double.NaN, double.NaN);
                }
            }
        }
    }
}
