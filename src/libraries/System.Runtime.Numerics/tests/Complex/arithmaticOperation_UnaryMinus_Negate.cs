// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_UnaryMinus_NegateTest
    {
        private static void VerifyUnaryMinusResult(double real, double imaginary)
        {
            Complex c_new = new Complex(real, imaginary);

            Complex unaryComplex = -c_new;
            Support.VerifyRealImaginaryProperties(unaryComplex, -c_new.Real, -c_new.Imaginary,
                string.Format("Unary Minus Error -{0} does not equal to {1} with ({2}, {3})!", c_new, unaryComplex, real, imaginary));

            unaryComplex = Complex.Negate(c_new);

            Support.VerifyRealImaginaryProperties(unaryComplex, -c_new.Real, -c_new.Imaginary,
                string.Format("Negate Error -{0} does not equal to {1} with ({2}, {3})!", c_new, unaryComplex, real, imaginary));
        }

        [Fact]
        public static void RunTests_TypicalValidValues()
        {
            // Test with valid double value pairs
            foreach (double real in Support.doubleValidValues)
            {
                foreach (double imaginary in Support.doubleValidValues)
                {
                    VerifyUnaryMinusResult(real, imaginary);
                }
            }
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // local Random real and imaginary parts
            double realRandom;
            double imaginaryRandom;

            // Complex numbers in the first quadrant: (postive real, positive imaginary)
            for (int i = 0; i < 3; i++)
            {
                realRandom = Support.GetRandomDoubleValue(false);
                imaginaryRandom = Support.GetRandomDoubleValue(false);
                VerifyUnaryMinusResult(realRandom, imaginaryRandom);
            }

            // Complex numbers in the second quadrant: (negative real, positive imaginary)
            for (int i = 0; i < 3; i++)
            {
                realRandom = Support.GetRandomDoubleValue(true);
                imaginaryRandom = Support.GetRandomDoubleValue(false);
                VerifyUnaryMinusResult(realRandom, imaginaryRandom);
            }

            // Complex numbers in the third quadrant: (negative real, negative imaginary)
            for (int i = 0; i < 3; i++)
            {
                realRandom = Support.GetRandomDoubleValue(true);
                imaginaryRandom = Support.GetRandomDoubleValue(true);
                VerifyUnaryMinusResult(realRandom, imaginaryRandom);
            }

            // Complex numbers in the fourth quadrant: (postive real, negative imaginary)
            for (int i = 0; i < 3; i++)
            {
                realRandom = Support.GetRandomDoubleValue(false);
                imaginaryRandom = Support.GetRandomDoubleValue(true);
                VerifyUnaryMinusResult(realRandom, imaginaryRandom);
            }
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
                VerifyUnaryMinusResult(realRandom, imaginaryInvalid);
            }

            // Complex number with a valid negative real and an invalid imaginary part
            foreach (double imaginaryInvalid in Support.doubleInvalidValues)
            {
                realRandom = Support.GetRandomDoubleValue(true);
                VerifyUnaryMinusResult(realRandom, imaginaryInvalid);
            }

            // Complex number with an invalid real and an a positive imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                imaginaryRandom = Support.GetRandomDoubleValue(false);
                VerifyUnaryMinusResult(realInvalid, imaginaryRandom);
            }

            // Complex number with an invalid real and a negative imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                imaginaryRandom = Support.GetRandomDoubleValue(true);
                VerifyUnaryMinusResult(realInvalid, imaginaryRandom);
            }

            // Complex number with an invalid real and an invalid imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                foreach (double imaginaryInvalid in Support.doubleInvalidValues)
                {
                    VerifyUnaryMinusResult(realInvalid, imaginaryInvalid);
                }
            }
        }
    }
}
