// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_AbsTest
    {
        private static void VerifyBinaryAbs(Double real, Double imaginary, Double expected)
        {
            // Create complex numbers
            Complex cComplex = new Complex(real, imaginary);
            Double abs = Complex.Abs(cComplex);

            if (!(abs.Equals((Double)expected) || Support.IsDiffTolerable(abs, expected)))
            {
                Console.WriteLine("Error_asd872!!! Abs ({0}, {1}) Actual: {2}, Expected: {3}", real, imaginary, abs, expected);
                Assert.True(false, "Verification Failed");
            }
            else // do the second verification
            {
                Double absNegative = Complex.Abs(-cComplex);
                if (!absNegative.Equals((Double)abs))
                {
                    Console.WriteLine("Error_asd872!!! Abs ({0}, {1}) = {2} != Abs(-neg)={3}", real, imaginary, abs, absNegative);
                    Assert.True(false, "Verification Failed");
                }
            }
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            VerifyBinaryAbs(0, 0, 0);
            VerifyBinaryAbs(1, 0, 1);
            VerifyBinaryAbs(0, 1, 1);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComlexInFirstQuad
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryAbs(real, imaginary, Math.Sqrt(real * real + imaginary * imaginary));

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryAbs(real, imaginary, Math.Sqrt(real * real + imaginary * imaginary));

            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryAbs(real, imaginary, Math.Sqrt(real * real + imaginary * imaginary));

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryAbs(real, imaginary, Math.Sqrt(real * real + imaginary * imaginary));
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            VerifyBinaryAbs(Double.MaxValue, Double.MaxValue, Double.PositiveInfinity);
            VerifyBinaryAbs(Double.MaxValue, 0, Double.MaxValue);
            VerifyBinaryAbs(0, Double.MaxValue, Double.MaxValue);

            VerifyBinaryAbs(Double.MinValue, Double.MinValue, Double.PositiveInfinity);
            VerifyBinaryAbs(Double.MinValue, 0, Double.MaxValue);
            VerifyBinaryAbs(0, Double.MinValue, Double.MaxValue);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // local variables
            Double realRandom = Support.GetRandomDoubleValue(false);
            Double imaginaryRandom = Support.GetRandomDoubleValue(false);

            // Complex number with a valid real and an invalid imaginary part
            foreach (double imaginaryInvalid in Support.doubleInvalidValues)
            {
                VerifyBinaryAbs(realRandom, imaginaryInvalid, Math.Abs(imaginaryInvalid));
            }

            // Complex number with an invalid real and an a real imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                VerifyBinaryAbs(realInvalid, realRandom, Math.Abs(realInvalid));
            }

            // Complex number with an invalid real and an invalid imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                foreach (double imaginaryInvalid in Support.doubleInvalidValues)
                {
                    VerifyBinaryAbs(realInvalid, imaginaryInvalid, (Double.IsInfinity(realInvalid) || Double.IsInfinity(imaginaryInvalid)) ? Double.PositiveInfinity : Double.NaN);
                }
            }

            //Regression test case for issue: Complex.Abs() is inconsistent on NaN / Complex.
            VerifyBinaryAbs(Double.NaN, 0, Double.NaN);
        }
    }
}
