// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_ConjugateTest
    {
        private static void VerifyConjugate(Double real, Double imaginary)
        {
            // Create complex numbers
            Complex c_test = new Complex(real, imaginary);
            Complex c_conj = Complex.Conjugate(c_test);

            if (false == Support.VerifyRealImaginaryProperties(c_conj, real, -imaginary))
            {
                Console.WriteLine("ErRoR! Conjugate Real and Imaginary Part Verification Error!");
                Console.WriteLine("Conjugate test ({0}, {1})", real, imaginary);
                Assert.True(false, "Verification Failed");
            }
            else if (false == Support.VerifyMagnitudePhaseProperties(c_conj, c_test.Magnitude, -c_test.Phase))
            {
                Console.WriteLine("ErRoR! Conjugate Magnitude and Phase Verification Error!");
                Console.WriteLine("Conjugate test ({0}, {1})", real, imaginary);
                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            VerifyConjugate(0.0, 0.0); // Verify with Double.Zero
            VerifyConjugate(1.0, 0.0); // Verify with Double.One
            VerifyConjugate(-1.0, 0.0); // Verify with Double.MinusOne
            VerifyConjugate(0.0, 1.0); // Verify with Double.ImaginaryOne
            VerifyConjugate(0.0, -1.0); // Verify with Double.MinusImaginaryOne
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComlexInFirstQuad
            Double real = Support.GetRandomDoubleValue(false);
            Double imaginary = Support.GetRandomDoubleValue(false);
            VerifyConjugate(real, imaginary);

            // Verify test results with Small_ComlexInFirstQuad
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
            VerifyConjugate(Double.MaxValue, Double.MaxValue); // test with 'Max'
            VerifyConjugate(Double.MaxValue, 0); // test with 'MaxReal'
            VerifyConjugate(0, Double.MaxValue); // test with 'MaxImaginary'
            VerifyConjugate(Double.MinValue, Double.MinValue); // test with 'Min'
            VerifyConjugate(Double.MinValue, 0); // test with 'MinReal'
            VerifyConjugate(0, Double.MinValue); // test with 'MinImaginary'
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // local variables
            Double realRandom;
            Double imaginaryRandom;

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
