// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_ACosTest
    {
        private static void VerifyACos(double x, double y)
        {
            // formula used in the feature: arccos(z) = -iln(z + iSqrt(value*value-1))
            // Verification is done with z = ACos(Cos(z));

            Complex complex = new Complex(x, y);
            Complex cosComplex = Complex.Cos(complex);
            Complex acosComplex = Complex.Acos(cosComplex);

            if (false == x.Equals((Double)acosComplex.Real) || false == y.Equals((Double)acosComplex.Imaginary))
            {
                double realDiff = Math.Abs(Math.Abs(x) - Math.Abs(acosComplex.Real));
                double imaginaryDiff = Math.Abs(Math.Abs(y) - Math.Abs(acosComplex.Imaginary));
                Assert.False((realDiff > 0.1) || (imaginaryDiff > 0.1), string.Format("({0}) != ACos(Cos():{1}):{2}", complex, cosComplex, acosComplex));
            }
        }

        private static void VerifyACos(double x, double y, double expectedReal, double expectedImaginary)
        {
            // for boundary values, ACos returns invalid Values

            Complex complex = new Complex(x, y);
            Complex acosComplex = Complex.Acos(complex);

            Support.VerifyRealImaginaryProperties(acosComplex, expectedReal, expectedImaginary, 
                string.Format("AcOs({0}):{1}", complex, acosComplex));
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify sqrt with Zero
            VerifyACos(0.0, 0.0);

            // Verify test results with One
            VerifyACos(1.0, 0.0);

            // Verify test results with MinusOne
            VerifyACos(-1.0, 0.0);

            // Verify test results with ImaginaryOne
            VerifyACos(0.0, 1.0);

            // Verify test results with MinusImaginaryOne
            VerifyACos(0.0, -1.0);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyACos(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyACos(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyACos(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyACos(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with Max
            VerifyACos(double.MaxValue, double.MaxValue);

            // Verify test results with MaxReal
            VerifyACos(double.MaxValue, 0.0, double.NaN, double.NaN);

            // Verify test results with MaxImg
            VerifyACos(0.0, double.MaxValue);

            // Verify test results with Min
            VerifyACos(double.MinValue, double.MinValue);

            // Verify test results with MinReal
            VerifyACos(double.MinValue, 0.0, double.NaN, double.NaN);

            // Verify test results with MinImaginary
            VerifyACos(0.0, double.MinValue);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyACos(1.0, double.PositiveInfinity, double.NaN, double.NaN);
            VerifyACos(1.0, double.NegativeInfinity, double.NaN, double.NaN);
            VerifyACos(1.0, double.NaN, double.NaN, double.NaN);

            // Verify test results with invalid Real
            VerifyACos(double.PositiveInfinity, 1.0, double.NaN, double.NaN);
            VerifyACos(double.NegativeInfinity, 1.0, double.NaN, double.NaN);
            VerifyACos(double.NaN, 1.0, double.NaN, double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (double invalidReal in Support.doubleInvalidValues)
            {
                foreach (double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyACos(invalidReal, invalidImaginary, double.NaN, double.NaN);
                }
            }
        }

        [Fact]
        public static void RunTests_EdgeCases()
        {
            // Verify edge cases where imaginary part is zero and real part is positive.
            VerifyACos(1234000000, 0, 0, 21.62667394298955);

            // Verify edge cases where imaginary part is negative.
            VerifyACos(0, -1234000000, 1.5707963267948966, 21.62667394298955);
        }
    }
}
