// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_UnaryMinus_NegateTest
    {
        private static void VerifyUnaryMinusResult(Double real, Double imaginary)
        {
            //local variables
            Complex unaryComplex;
            Complex c_new = new Complex(real, imaginary);

            unaryComplex = -c_new;
            if (!Support.VerifyRealImaginaryProperties(unaryComplex, -c_new.Real, -c_new.Imaginary))
            {
                Console.WriteLine("ErRoR! Unary Minus Error -{0} does not equal to {1}!", c_new, unaryComplex);
                Console.WriteLine("Unary Minus test with ({0}, {1})", real, imaginary);
                Assert.True(false, "Verification Failed");
            }

            unaryComplex = Complex.Negate(c_new);
            if (!Support.VerifyRealImaginaryProperties(unaryComplex, -c_new.Real, -c_new.Imaginary))
            {
                Console.WriteLine("ErRoR! Negate Error -{0} does not equal to {1}!", c_new, unaryComplex);
                Console.WriteLine("Negate test with ({0}, {1})", real, imaginary);
                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_TypicalValidValues()
        {
            // Test with valid double value pairs
            foreach (Double real in Support.doubleValidValues)
            {
                foreach (Double imaginary in Support.doubleValidValues)
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
            for (int i = 0; i < Support.randomSample; i++)
            {
                realRandom = Support.GetRandomDoubleValue(false);
                imaginaryRandom = Support.GetRandomDoubleValue(false);
                VerifyUnaryMinusResult(realRandom, imaginaryRandom);
            }

            // Complex numbers in the second quadrant: (negative real, positive imaginary)
            for (int i = 0; i < Support.randomSample; i++)
            {
                realRandom = Support.GetRandomDoubleValue(true);
                imaginaryRandom = Support.GetRandomDoubleValue(false);
                VerifyUnaryMinusResult(realRandom, imaginaryRandom);
            }

            // Complex numbers in the third quadrant: (negative real, negative imaginary)
            for (int i = 0; i < Support.randomSample; i++)
            {
                realRandom = Support.GetRandomDoubleValue(true);
                imaginaryRandom = Support.GetRandomDoubleValue(true);
                VerifyUnaryMinusResult(realRandom, imaginaryRandom);
            }

            // Complex numbers in the fourth quadrant: (postive real, negative imaginary)
            for (int i = 0; i < Support.randomSample; i++)
            {
                realRandom = Support.GetRandomDoubleValue(false);
                imaginaryRandom = Support.GetRandomDoubleValue(true);
                VerifyUnaryMinusResult(realRandom, imaginaryRandom);
            }
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
