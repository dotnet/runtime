// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_TanTest
    {
        private static void VerifyTan(double x, double y, double expectedReal, double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex tanComplex = Complex.Tan(complex);

            Support.VerifyRealImaginaryProperties(tanComplex, expectedReal, expectedImaginary,
                string.Format("Tan({0}):{1} != ({2},{3})", complex, tanComplex, expectedReal, expectedImaginary));

            // The following verification can be added: tan(x) = -tan(-x)
        }

        private static void VerifyTan(double x, double y)
        {
            double scale = Math.Cosh(2 * y);
            if (!double.IsInfinity(scale))
            {
                scale += Math.Cos(2 * x);
            }
            double expectedReal = Math.Sin(2 * x) / scale;
            double expectedImaginary = Math.Sinh(2 * y) / scale;

            if (double.IsNaN(expectedImaginary))
            {
                expectedReal = double.NaN;
            }
            VerifyTan(x, y, expectedReal, expectedImaginary);
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify sqrt with Zero
            VerifyTan(0.0, 0.0);

            // Verify test results with One
            VerifyTan(1.0, 0.0);

            // Verify test results with MinusOne
            VerifyTan(-1.0, 0.0);

            // Verify test results with ImaginaryOne
            VerifyTan(0.0, 1.0);

            // Verify test results with MinusImaginaryOne
            VerifyTan(0.0, -1.0);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyTan(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyTan(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyTan(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyTan(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with Max
            VerifyTan(double.MaxValue, double.MaxValue, double.NaN, double.NaN);

            // Verify test results with MaxReal
            VerifyTan(double.MaxValue, 0.0, Math.Sin(double.MaxValue) / Math.Cos(double.MaxValue), 0); //for IA64

            // Verify test results with MaxImg
            VerifyTan(0.0, double.MaxValue, double.NaN, double.NaN);

            // Verify test results with Min
            VerifyTan(double.MinValue, double.MinValue, double.NaN, double.NaN);

            // Verify test results with MinReal
            VerifyTan(double.MinValue, 0.0, Math.Sin(double.MinValue) / Math.Cos(double.MinValue), 0); //for IA64

            // Verify test results with MinImaginary
            VerifyTan(0.0, double.MinValue, double.NaN, double.NaN);
        }
    }
}
