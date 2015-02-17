// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class ComplexConstructorTest
    {
        private static void VerifyCtor(double real, double imaginary)
        {
            Complex c_new = new Complex(real, imaginary);
            if (false == Support.VerifyRealImaginaryProperties(c_new, real, imaginary))
            {
                Console.WriteLine("Error_ctor(Complex): ({0}, {1})", real, imaginary);
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
                    VerifyCtor(real, imaginary);
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
                VerifyCtor(realRandom, imaginaryRandom);
            }

            // Complex numbers in the second quadrant: (negative real, positive imaginary)
            for (int i = 0; i < Support.randomSample; i++)
            {
                realRandom = Support.GetRandomDoubleValue(true);
                imaginaryRandom = Support.GetRandomDoubleValue(false);
                VerifyCtor(realRandom, imaginaryRandom);
            }

            // Complex numbers in the third quadrant: (negative real, negative imaginary)
            for (int i = 0; i < Support.randomSample; i++)
            {
                realRandom = Support.GetRandomDoubleValue(true);
                imaginaryRandom = Support.GetRandomDoubleValue(true);
                VerifyCtor(realRandom, imaginaryRandom);
            }

            // Complex numbers in the fourth quadrant: (postive real, negative imaginary)
            for (int i = 0; i < Support.randomSample; i++)
            {
                realRandom = Support.GetRandomDoubleValue(false);
                imaginaryRandom = Support.GetRandomDoubleValue(true);
                VerifyCtor(realRandom, imaginaryRandom);
            }
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // local variables
            Double realRandom = Support.GetRandomDoubleValue(false);
            Double imaginaryRandom = Support.GetRandomDoubleValue(false);

            // Complex number with a valid positive/negative real and an invalid imaginary part
            foreach (double imaginaryInvalid in Support.doubleInvalidValues)
            {
                VerifyCtor(realRandom, imaginaryInvalid);
                VerifyCtor(-realRandom, imaginaryInvalid);
            }

            // Complex number with an invalid real and an a positive/negative imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                VerifyCtor(realInvalid, imaginaryRandom);
                VerifyCtor(realInvalid, -imaginaryRandom);
            }

            // Complex number with an invalid real and an invalid imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                foreach (double imaginaryInvalid in Support.doubleInvalidValues)
                {
                    VerifyCtor(realInvalid, imaginaryInvalid);
                }
            }
        }
    }
}
