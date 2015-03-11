// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_AbsTest
    {
        private static void VerifyBinaryAbs(double real, double imaginary, double expected)
        {
            // Create complex numbers
            Complex cComplex = new Complex(real, imaginary);
            double abs = Complex.Abs(cComplex);

            Assert.True((abs.Equals((Double)expected) || Support.IsDiffTolerable(abs, expected)),
                string.Format("Abs ({0}, {1}) Actual: {2}, Expected: {3}", real, imaginary, abs, expected));
           
            double absNegative = Complex.Abs(-cComplex);
            Assert.True(absNegative.Equals((Double)abs), 
                string.Format("Abs ({0}, {1}) = {2} != Abs(-neg)={3}", real, imaginary, abs, absNegative));
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
            // Verify test results with ComplexInFirstQuad
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            VerifyBinaryAbs(double.MaxValue, double.MaxValue, double.PositiveInfinity);
            VerifyBinaryAbs(double.MaxValue, 0, double.MaxValue);
            VerifyBinaryAbs(0, double.MaxValue, double.MaxValue);

            VerifyBinaryAbs(double.MinValue, double.MinValue, double.PositiveInfinity);
            VerifyBinaryAbs(double.MinValue, 0, double.MaxValue);
            VerifyBinaryAbs(0, double.MinValue, double.MaxValue);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            double realRandom = Support.GetRandomDoubleValue(false);
            double imaginaryRandom = Support.GetRandomDoubleValue(false);

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
                    VerifyBinaryAbs(realInvalid, imaginaryInvalid, (double.IsInfinity(realInvalid) || double.IsInfinity(imaginaryInvalid)) ? double.PositiveInfinity : double.NaN);
                }
            }

            //Regression test case for issue: Complex.Abs() is inconsistent on NaN / Complex.
            VerifyBinaryAbs(double.NaN, 0, double.NaN);
        }
    }
}
