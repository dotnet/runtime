// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_CosTest
    {
        private static void VerifyCos(double x, double y, double expectedReal, double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex cosComplex = Complex.Cos(complex);

            Support.VerifyRealImaginaryProperties(cosComplex, expectedReal, expectedImaginary, 
                string.Format("Cos({0}):{1} != ({2},{3})", complex, cosComplex, expectedReal, expectedImaginary));
        }

        private static void VerifyCos(double x, double y)
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
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            VerifyCos(double.MaxValue, double.MaxValue, Math.Cos(double.MaxValue) * double.PositiveInfinity, double.NegativeInfinity); //for IA64

            // Verify test results with MaxReal
            VerifyCos(double.MaxValue, 0.0, Math.Cos(double.MaxValue), 0); //for IA64

            // Verify test results with MaxImg
            VerifyCos(0.0, double.MaxValue, double.PositiveInfinity, double.NaN);

            // Verify test results with Min
            VerifyCos(double.MinValue, double.MinValue, double.NegativeInfinity, double.NegativeInfinity);

            // Verify test results with MinReal
            VerifyCos(double.MinValue, 0.0, Math.Cos(double.MinValue), 0); //for IA64

            // Verify test results with MinImaginary
            VerifyCos(0.0, double.MinValue, double.PositiveInfinity, double.NaN);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyCos(1.0, double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity);
            VerifyCos(1.0, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity);
            VerifyCos(1.0, double.NaN, double.NaN, double.NaN);

            // Verify test results with invalid Real
            VerifyCos(double.PositiveInfinity, 1.0, double.NaN, double.NaN);
            VerifyCos(double.NegativeInfinity, 1.0, double.NaN, double.NaN);
            VerifyCos(double.NaN, 1.0, double.NaN, double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (double invalidReal in Support.doubleInvalidValues)
            {
                foreach (double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyCos(invalidReal, invalidImaginary, double.NaN, double.NaN);
                }
            }
        }
    }
}
