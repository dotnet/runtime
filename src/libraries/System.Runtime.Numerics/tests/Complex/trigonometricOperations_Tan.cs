// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_TanTest
    {
        private static void VerifyTan(Double x, Double y, Double expectedReal, Double expectedImaginary)
        {
            Complex complex = new Complex(x, y);
            Complex tanComplex = Complex.Tan(complex);

            if (false == Support.VerifyRealImaginaryProperties(tanComplex, expectedReal, expectedImaginary))
            {
                Console.WriteLine("Error TaN-ErrT6A81N! Tan({0}):{1} != ({2},{3})", complex, tanComplex, expectedReal, expectedImaginary);
                Assert.True(false, "Verification Failed");
            }

            // The following verification can be added: tan(x) = -tan(-x)
        }

        private static void VerifyTan(Double x, Double y)
        {
            Double scale = Math.Cosh(2 * y);
            if (!Double.IsInfinity(scale))
                scale += Math.Cos(2 * x);
            Double expectedReal = Math.Sin(2 * x) / scale;
            Double expectedImaginary = Math.Sinh(2 * y) / scale;

            if (Double.IsNaN(expectedImaginary))
                expectedReal = Double.NaN;
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
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            VerifyTan(Double.MaxValue, Double.MaxValue, Double.NaN, Double.NaN);

            // Verify test results with MaxReal
            VerifyTan(Double.MaxValue, 0.0, Math.Sin(Double.MaxValue) / Math.Cos(Double.MaxValue), 0); //for IA64

            // Verify test results with MaxImg
            VerifyTan(0.0, Double.MaxValue, Double.NaN, Double.NaN);

            // Verify test results with Min
            VerifyTan(Double.MinValue, Double.MinValue, Double.NaN, Double.NaN);

            // Verify test results with MinReal
            VerifyTan(Double.MinValue, 0.0, Math.Sin(Double.MinValue) / Math.Cos(Double.MinValue), 0); //for IA64

            // Verify test results with MinImaginary
            VerifyTan(0.0, Double.MinValue, Double.NaN, Double.NaN);
        }
    }
}
