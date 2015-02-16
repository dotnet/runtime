// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class trigonometricOperations_ACosTest
    {
        private static void VerifyACos(Double x, Double y)
        {
            // formula used in the feature: arccos(z) = -iln(z + iSqrt(value*value-1))
            // Verification is done with z = ACos(Cos(z));

            Complex complex = new Complex(x, y);
            Complex cosComplex = Complex.Cos(complex);
            Complex acosComplex = Complex.Acos(cosComplex);

            if (false == x.Equals((Double)acosComplex.Real) || false == y.Equals((Double)acosComplex.Imaginary))
            {
                Double realDiff = Math.Abs(Math.Abs(x) - Math.Abs(acosComplex.Real));
                Double imaginaryDiff = Math.Abs(Math.Abs(y) - Math.Abs(acosComplex.Imaginary));
                if ((realDiff > 0.1) || (imaginaryDiff > 0.1))
                {
                    Console.WriteLine("Error AcOs-Err3A6C6s! ({0}) != ACos(Cos():{1}):{2}", complex, cosComplex, acosComplex);

                    Assert.True(false, "Verification Failed");
                }
            }
        }

        private static void VerifyACos(Double x, Double y, Double expectedReal, Double expectedImaginary)
        {
            // for boundary values, ACos returns invalid Values

            Complex complex = new Complex(x, y);
            Complex acosComplex = Complex.Acos(complex);

            if (false == Support.VerifyRealImaginaryProperties(acosComplex, expectedReal, expectedImaginary))
            {
                Console.WriteLine("Error AcOs-Err3A6s1N! AcOs({0})", complex, acosComplex);

                Assert.True(false, "Verification Failed");
            }
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
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            VerifyACos(Double.MaxValue, Double.MaxValue);

            // Verify test results with MaxReal
            VerifyACos(Double.MaxValue, 0.0, Double.NaN, Double.NaN);

            // Verify test results with MaxImg
            VerifyACos(0.0, Double.MaxValue);

            // Verify test results with Min
            VerifyACos(Double.MinValue, Double.MinValue);

            // Verify test results with MinReal
            VerifyACos(Double.MinValue, 0.0, Double.NaN, Double.NaN);

            // Verify test results with MinImaginary
            VerifyACos(0.0, Double.MinValue);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // Verify test results with invalid Imaginary
            VerifyACos(1.0, Double.PositiveInfinity, Double.NaN, Double.NaN);
            VerifyACos(1.0, Double.NegativeInfinity, Double.NaN, Double.NaN);
            VerifyACos(1.0, Double.NaN, Double.NaN, Double.NaN);

            // Verify test results with invalid Real
            VerifyACos(Double.PositiveInfinity, 1.0, Double.NaN, Double.NaN);
            VerifyACos(Double.NegativeInfinity, 1.0, Double.NaN, Double.NaN);
            VerifyACos(Double.NaN, 1.0, Double.NaN, Double.NaN);

            // Verify test results with invalid Real and Imaginary
            foreach (Double invalidReal in Support.doubleInvalidValues)
            {
                foreach (Double invalidImaginary in Support.doubleInvalidValues)
                {
                    VerifyACos(invalidReal, invalidImaginary, Double.NaN, Double.NaN);
                }
            }
        }
    }
}
