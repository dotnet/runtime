// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_TanhTest
    {
        private static void VerifyTanh(double x, double y, double expectedReal, double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex tanhComplex = Complex.Tanh(complex);

            Support.VerifyRealImaginaryProperties(tanhComplex, expectedReal, expectedImaginary,
                string.Format("Tanh({0}):{1} != ({2},{3})", complex, tanhComplex, expectedReal, expectedImaginary));
        }

        private static void VerifyTanh(double x, double y)
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
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            VerifyTanh(double.MaxValue, double.MaxValue);

            // Verify test results with MaxReal
            VerifyTanh(double.MaxValue, 0.0);

            // Verify test results with MaxImg
            VerifyTanh(0.0, double.MaxValue, 0, Math.Sin(double.MaxValue) / Math.Cos(double.MaxValue)); //for IA64

            // Verify test results with Min
            VerifyTanh(double.MinValue, double.MinValue);

            // Verify test results with MinReal
            VerifyTanh(double.MinValue, 0.0, double.NaN, double.NaN);

            // Verify test results with MinImaginary
            VerifyTanh(0.0, double.MinValue, 0, Math.Sin(double.MinValue) / Math.Cos(double.MinValue)); //for IA64
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyTanh(1.0, double.PositiveInfinity, double.NaN, double.NaN);
            VerifyTanh(1.0, double.NegativeInfinity, double.NaN, double.NaN);
            VerifyTanh(1.0, double.NaN, double.NaN, double.NaN);

            // Verify test results with invalid Real
            VerifyTanh(double.PositiveInfinity, 1.0, double.NaN, double.NaN);
            VerifyTanh(double.NegativeInfinity, 1.0, double.NaN, double.NaN);
            VerifyTanh(double.NaN, 1.0, double.NaN, double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (double invalidReal in Support.doubleInvalidValues)
            {
                foreach (double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyTanh(invalidReal, invalidImaginary, double.NaN, double.NaN);
                }
            }
        }
    }
}
