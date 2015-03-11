// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class standardNumericalFunctions_PowTest
    {
        private static void VerifyPow(double a, double b, double c, double d)
        {
            Complex x = new Complex(a, b);
            Complex y = new Complex(c, d);
            Complex powComplex = Complex.Pow(x, y);

            double expectedReal = 0;
            double expectedImaginary = 0;
            if (0 == c && 0 == d)
                expectedReal = 1;
            else if (!(0 == a && 0 == b))
            {
                //pow(x, y) = exp(y·log(x))
                Complex expected = Complex.Exp(y * Complex.Log(x));
                expectedReal = expected.Real;
                expectedImaginary = expected.Imaginary;
            }

            Support.VerifyRealImaginaryProperties(powComplex, expectedReal, expectedImaginary, 
                string.Format("Pow (({0}, {1}), ({2}, {3}))", a, b, c, d));
        }

        private static void VerifyPow(double a, double b, double doubleVal)
        {
            Complex x = new Complex(a, b);
            Complex powComplex = Complex.Pow(x, doubleVal);

            double expectedReal = 0;
            double expectedImaginary = 0;
            if (0 == doubleVal)
                expectedReal = 1;
            else if (!(0 == a && 0 == b))
            {
                //pow(x, y) = exp(y·log(x))
                Complex y = new Complex(doubleVal, 0);
                Complex expected = Complex.Exp(y * Complex.Log(x));
                expectedReal = expected.Real;
                expectedImaginary = expected.Imaginary;
            }

            Support.VerifyRealImaginaryProperties(powComplex, expectedReal, expectedImaginary, 
                string.Format("Pow (({0}, {1}), {2})", a, b, doubleVal));
        }

        private static void RunTests_PowDouble(double a, double b)
        {
            VerifyPow(a, b, 0.0);
            VerifyPow(a, b, 1.0);

            double randomPower = Support.GetSmallRandomDoubleValue(true);
            VerifyPow(a, b, randomPower);

            randomPower = Support.GetSmallRandomDoubleValue(false);
            VerifyPow(a, b, randomPower);

            foreach (double power in Support.doubleInvalidValues)
            {
                VerifyPow(a, b, power);
            }
        }

        private static void RunTests_PowComplex(double a, double b)
        {
            VerifyPow(a, b, 0.0, -1.0);
            VerifyPow(a, b, 0.0, 0.0);
            VerifyPow(a, b, 0.0, 1.0);
            VerifyPow(a, b, 1.0, 0.0);

            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyPow(a, b, real, imaginary);

            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyPow(a, b, real, imaginary);

            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyPow(a, b, real, imaginary);

            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyPow(a, b, real, imaginary);
        }

        private static void RunTests(double a, double b)
        {
            RunTests_PowDouble(a, b);
            RunTests_PowComplex(a, b);
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify sqrt with Zero
            RunTests(0.0, 0.0);

            // Verify test results with One
            RunTests(1.0, 0.0);

            // Verify test results with MinusOne
            RunTests(-1.0, 0.0);

            // Verify test results with ImaginaryOne
            RunTests(0.0, 1.0);

            // Verify test results with MinusImaginaryOne
            RunTests(0.0, -1.0);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            double real = Support.GetRandomDoubleValue(false);
            double imaginary = Support.GetRandomDoubleValue(false);
            RunTests(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetRandomDoubleValue(true);
            imaginary = Support.GetRandomDoubleValue(false);
            RunTests(real, imaginary);

            // Verify test results with ComplexInThirdQuad
            real = Support.GetRandomDoubleValue(true);
            imaginary = Support.GetRandomDoubleValue(true);
            RunTests(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetRandomDoubleValue(false);
            imaginary = Support.GetRandomDoubleValue(true);
            RunTests(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with Max
            RunTests(double.MaxValue, double.MaxValue);

            // Verify test results with Min
            RunTests(double.MinValue, double.MinValue);
        }
    }
}
