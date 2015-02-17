// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_ReciprocalTest
    {
        private static void VerifyReciprocal(Double real, Double imaginary)
        {
            // Create complex numbers
            Complex c_test = new Complex(real, imaginary);
            Complex c_rcp = Complex.Reciprocal(c_test);

            Complex c_expected = Complex.Zero;
            if (Complex.Zero != c_test &&
                !(Double.IsInfinity(real) && !(Double.IsInfinity(imaginary) || Double.IsNaN(imaginary))) &&
                !(Double.IsInfinity(imaginary) && !(Double.IsInfinity(real) || Double.IsNaN(real))))
            {
                Double magnitude = c_test.Magnitude;
                c_expected = (Complex.Conjugate(c_test) / magnitude); // in order to avoid Infinity = magnitude* magnitude
                c_expected = c_expected / magnitude;
            }

            if (false == Support.VerifyRealImaginaryProperties(c_rcp, c_expected.Real, c_expected.Imaginary))
            {
                Console.WriteLine("Error_rye102!!! Reciprocal({0})", c_test);
                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            VerifyReciprocal(0.0, 0.0); // Verify with Double.Zero
            VerifyReciprocal(1.0, 0.0); // Verify with Double.One
            VerifyReciprocal(-1.0, 0.0); // Verify with Double.MinusOne
            VerifyReciprocal(0.0, 1.0); // Verify with Double.ImaginaryOne
            VerifyReciprocal(0.0, -1.0); // Verify with Double.MinusImaginaryOne
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComlexInFirstQuad
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyReciprocal(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyReciprocal(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyReciprocal(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyReciprocal(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // local variables
            VerifyReciprocal(Double.MaxValue, 0); // test with 'MaxReal'
            VerifyReciprocal(0, Double.MaxValue); // test with 'MaxImaginary'
            VerifyReciprocal(Double.MinValue, 0); // test with 'MinReal'
            VerifyReciprocal(0, Double.MinValue); // test with 'MinImaginary'
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // local variables
            Double realRandom = Support.GetRandomDoubleValue(false);
            Double imaginaryRandom = Support.GetRandomDoubleValue(false);

            // Complex number with a valid  real and an invalid imaginary part
            foreach (double imaginaryInvalid in Support.doubleInvalidValues)
            {
                VerifyReciprocal(realRandom, imaginaryInvalid);
            }

            // Complex number with an invalid real and an a real imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                VerifyReciprocal(realInvalid, imaginaryRandom);
            }

            // Complex number with an invalid real and an invalid imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                foreach (double imaginaryInvalid in Support.doubleInvalidValues)
                {
                    VerifyReciprocal(realInvalid, imaginaryInvalid);
                }
            }
        }
    }
}
