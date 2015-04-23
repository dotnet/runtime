// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_ConjugateTest
    {
        private static void VerifyConjugate(double real, double imaginary)
        {
            // Create complex numbers
            Complex c_test = new Complex(real, imaginary);
            Complex c_conj = Complex.Conjugate(c_test);

            Support.VerifyRealImaginaryProperties(c_conj, real, -imaginary,
                string.Format("Conjugate test ({0}, {1})", real, imaginary));

            Support.VerifyMagnitudePhaseProperties(c_conj, c_test.Magnitude, -c_test.Phase,
                string.Format("Conjugate test ({0}, {1})", real, imaginary));
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            VerifyConjugate(0.0, 0.0); // Verify with double.Zero
            VerifyConjugate(1.0, 0.0); // Verify with double.One
            VerifyConjugate(-1.0, 0.0); // Verify with double.MinusOne
            VerifyConjugate(0.0, 1.0); // Verify with double.ImaginaryOne
            VerifyConjugate(0.0, -1.0); // Verify with double.MinusImaginaryOne
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            double real = Support.GetRandomDoubleValue(false);
            double imaginary = Support.GetRandomDoubleValue(false);
            VerifyConjugate(real, imaginary);

            // Verify test results with Small_ComplexInFirstQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyConjugate(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetRandomDoubleValue(true);
            imaginary = Support.GetRandomDoubleValue(false);
            VerifyConjugate(real, imaginary);

            // Verify test results with Small_ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyConjugate(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetRandomDoubleValue(true);
            imaginary = Support.GetRandomDoubleValue(true);
            VerifyConjugate(real, imaginary);

            // Verify test results with Small_ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyConjugate(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetRandomDoubleValue(true);
            imaginary = Support.GetRandomDoubleValue(true);
            VerifyConjugate(real, imaginary);

            // Verify test results with Small_ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyConjugate(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Max
            VerifyConjugate(double.MaxValue, double.MaxValue);

            // MaxReal
            VerifyConjugate(double.MaxValue, 0);

            // MaxImaginary
            VerifyConjugate(0, double.MaxValue);

            // Min
            VerifyConjugate(double.MinValue, double.MinValue);

            // MinReal
            VerifyConjugate(double.MinValue, 0);

            // MinImaginary
            VerifyConjugate(0, double.MinValue);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            double realRandom;
            double imaginaryRandom;

            // Complex number with a valid positive real and an invalid imaginary part
            foreach (double imaginaryInvalid in Support.doubleInvalidValues)
            {
                realRandom = Support.GetRandomDoubleValue(false);
                VerifyConjugate(realRandom, imaginaryInvalid);
            }

            // Complex number with a valid negative real and an invalid imaginary part
            foreach (double imaginaryInvalid in Support.doubleInvalidValues)
            {
                realRandom = Support.GetRandomDoubleValue(true);
                VerifyConjugate(realRandom, imaginaryInvalid);
            }

            // Complex number with an invalid real and an a positive imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                imaginaryRandom = Support.GetRandomDoubleValue(false);
                VerifyConjugate(realInvalid, imaginaryRandom);
            }

            // Complex number with an invalid real and a negative imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                imaginaryRandom = Support.GetRandomDoubleValue(true);
                VerifyConjugate(realInvalid, imaginaryRandom);
            }

            // Complex number with an invalid real and an invalid imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                foreach (double imaginaryInvalid in Support.doubleInvalidValues)
                {
                    VerifyConjugate(realInvalid, imaginaryInvalid);
                }
            }
        }
    }
}
