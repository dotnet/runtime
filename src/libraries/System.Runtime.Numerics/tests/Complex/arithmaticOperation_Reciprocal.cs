// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_ReciprocalTest
    {
        private static void VerifyReciprocal(double real, double imaginary)
        {
            // Create complex numbers
            Complex c_test = new Complex(real, imaginary);
            Complex c_rcp = Complex.Reciprocal(c_test);

            Complex c_expected = Complex.Zero;
            if (Complex.Zero != c_test &&
                !(double.IsInfinity(real) && !(double.IsInfinity(imaginary) || double.IsNaN(imaginary))) &&
                !(double.IsInfinity(imaginary) && !(double.IsInfinity(real) || double.IsNaN(real))))
            {
                double magnitude = c_test.Magnitude;
                c_expected = (Complex.Conjugate(c_test) / magnitude); // in order to avoid Infinity = magnitude* magnitude
                c_expected /= magnitude;
            }

            Support.VerifyRealImaginaryProperties(c_rcp, c_expected.Real, c_expected.Imaginary,
                string.Format("Reciprocal ({0}, {1}) Actual: {2}, Expected: {3}", real, imaginary, c_rcp, c_expected));
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            VerifyReciprocal(0.0, 0.0); // Verify with double.Zero
            VerifyReciprocal(1.0, 0.0); // Verify with double.One
            VerifyReciprocal(-1.0, 0.0); // Verify with double.MinusOne
            VerifyReciprocal(0.0, 1.0); // Verify with double.ImaginaryOne
            VerifyReciprocal(0.0, -1.0); // Verify with double.MinusImaginaryOne
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            // MaxReal
            VerifyReciprocal(double.MaxValue, 0);

            // MaxImaginary
            VerifyReciprocal(0, double.MaxValue);

            // MinReal
            VerifyReciprocal(double.MinValue, 0);

            // MinImaginary
            VerifyReciprocal(0, double.MinValue);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            double realRandom = Support.GetRandomDoubleValue(false);
            double imaginaryRandom = Support.GetRandomDoubleValue(false);

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
