// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class standardNumericalFunctions_LogTest
    {
        private static void VerifyLogWithProperties(Complex c1, Complex c2)
        {
            VerifyLogWithProperties(c1, c2, true);
        }

        private static void VerifyLogWithProperties(Complex c1, Complex c2, bool extended)
        {
            if (c1 == Complex.Zero)
            {
                return;
            }

            VerifyLogWithBase10(c1);
            VerifyLogWithBase(c1);
            if (extended)
            {
                VerifyLogWithMultiply(c1, c2);
                VerifyLogWithPowerMinusOne(c1);
                VerifyLogWithExp(c1);
            }
        }

        private static void VerifyLogWithMultiply(Complex c1, Complex c2)
        {
            // Log(c1*c2) == Log(c1) + Log(c2), if -PI < Arg(c1) + Arg(c2) <= PI
            Double equalityCondition = Math.Atan2(c1.Imaginary, c1.Real) + Math.Atan2(c2.Imaginary, c2.Real);
            if (equalityCondition <= -Math.PI || equalityCondition > Math.PI)
            {
                return;
            }

            Complex logComplex = Complex.Log(c1 * c2);
            Complex logExpectedComplex = Complex.Log(c1) + Complex.Log(c2);

            if (false == Support.VerifyRealImaginaryProperties(logComplex, logExpectedComplex.Real, logExpectedComplex.Imaginary))
            {
                Console.WriteLine("Error LogMULT-ErrLoG8710! Log({0}*{1}):{2} != {3})", c1, c2, logComplex, logExpectedComplex);
                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifyLogWithPowerMinusOne(Complex c)
        {
            // Log(c) == -Log(1/c)
            Complex logComplex = Complex.Log(c);
            Complex logPowerMinusOne = Complex.Log(1 / c);

            if (false == Support.VerifyRealImaginaryProperties(logComplex, -logPowerMinusOne.Real, -logPowerMinusOne.Imaginary))
            {
                Console.WriteLine("Error LogPowMnsOne-ErrLoG6913! Log({0}):{1} != {2} as expected", c, logComplex, -logPowerMinusOne);
                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifyLogWithExp(Complex c)
        {
            // Exp(log(c)) == c, if c != Zero
            Complex logComplex = Complex.Log(c);
            Complex expLogComplex = Complex.Exp(logComplex);

            if (false == Support.VerifyRealImaginaryProperties(expLogComplex, c.Real, c.Imaginary))
            {
                Console.WriteLine("Error LogEXP-Err2113! Exp(Log({0}):{1} != {2})", c, expLogComplex, c);
                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifyLogWithBase10(Complex complex)
        {
            Complex logValue = Complex.Log10(complex);
            Complex logComplex = Complex.Log(complex);
            Double baseLog = Math.Log(10);
            Complex expectedLog = logComplex / baseLog;

            if (!Support.VerifyRealImaginaryProperties(logValue, expectedLog.Real, expectedLog.Imaginary))
            {
                Console.WriteLine("Error LogBase10-ErrLoG4211! Log({0}, {1}):{2} != {3} as expected", complex, 10, logValue, expectedLog);
                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifyLogWithBase(Complex complex)
        {
            Double baseValue = 0.0;
            Double baseLog;

            // verify with Random Int32
            do
            {
                baseValue = Support.GetRandomInt32Value(false);
            }
            while (0 == baseValue);

            Complex logValue = Complex.Log(complex, baseValue);

            Complex logComplex = Complex.Log(complex);
            baseLog = Math.Log(baseValue);
            Complex expectedLog = logComplex / baseLog;

            if (false == Support.VerifyRealImaginaryProperties(logValue, expectedLog.Real, expectedLog.Imaginary))
            {
                Console.WriteLine("Error LogBase-ErrLoG01571! Log({0}, {1}):{2} != {3} as expected", complex, baseValue, logValue, expectedLog);
                Assert.True(false, "Verification Failed");
            }

            // Verify with Random Double value

            baseValue = 0.0;
            do
            {
                baseValue = Support.GetRandomDoubleValue(false);
            }
            while (0.0 == baseValue);

            logValue = Complex.Log(complex, baseValue);
            logComplex = Complex.Log(complex);
            baseLog = Math.Log(baseValue);
            expectedLog = logComplex / baseLog;

            if (!Support.VerifyRealImaginaryProperties(logValue, expectedLog.Real, expectedLog.Imaginary))
            {
                Console.WriteLine("Error LogBaseDbL-ErrLoG1598! Log({0}, {1}):{2} != {3} as expected", complex, baseValue, logValue, expectedLog);
                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            Complex logValue = Complex.Log(Complex.Zero);
            Support.VerifyRealImaginaryProperties(logValue, Double.NegativeInfinity, 0.0);

            // verify log10 with Zero
            logValue = Complex.Log10(Complex.Zero);
            Support.VerifyRealImaginaryProperties(logValue, Double.NegativeInfinity, 0.0);

            // verify log base with Zero
            Double baseValue = Support.GetRandomInt32Value(false);
            logValue = Complex.Log(Complex.Zero, baseValue);
            Support.VerifyRealImaginaryProperties(logValue, Double.NegativeInfinity, Double.NaN);

            // Verify test results with Zero - One
            VerifyLogWithProperties(Complex.One, Complex.Zero);

            // Verify test results with One - ImaginaryOne
            VerifyLogWithProperties(Complex.One, Complex.ImaginaryOne);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
            Complex cFirst = new Complex(real, imaginary);

            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            Complex cSecond = new Complex(real, imaginary);

            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            Complex cThird = new Complex(real, imaginary);

            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            Complex cFourth = new Complex(real, imaginary);

            // Verify test results with ComplexInFirstQuad - ComplexInFirstQuad
            VerifyLogWithProperties(cFirst, cFirst);

            // Verify test results with ComplexInFirstQuad - ComplexInSecondQuad
            VerifyLogWithProperties(cFirst, cSecond);

            // Verify test results with ComplexInFirstQuad - ComplexInThirdQuad
            VerifyLogWithProperties(cFirst, cThird);

            // Verify test results with ComplexInFirstQuad - ComplexInFourthQuad
            VerifyLogWithProperties(cFirst, cFourth);

            // Verify test results with ComplexInSecondQuad - ComplexInSecondQuad
            VerifyLogWithProperties(cSecond, cSecond);

            // Verify test results with ComplexInSecondQuad - ComplexInThirdQuad
            VerifyLogWithProperties(cSecond, cThird);

            // Verify test results with ComplexInSecondQuad - ComplexInFourthQuad
            VerifyLogWithProperties(cSecond, cFourth);

            // Verify test results with ComplexInThirdQuad - ComplexInThirdQuad
            VerifyLogWithProperties(cThird, cThird);

            // Verify test results with ComplexInThirdQuad - ComplexInFourthQuad
            VerifyLogWithProperties(cThird, cFourth);

            // Verify test results with ComplexInFourthQuad - ComplexInFourthQuad
            VerifyLogWithProperties(cFourth, cFourth);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // local variables
            Complex c_maxReal = new Complex(Double.MaxValue, 0.0);
            Complex c_maxImg = new Complex(0.0, Double.MaxValue);
            Complex c_minReal = new Complex(Double.MinValue, 0.0);
            Complex c_minImg = new Complex(0.0, Double.MinValue);

            // test with 'MaxReal'
            VerifyLogWithProperties(c_maxReal, Complex.One, false);

            // test with 'MaxImaginary'
            VerifyLogWithProperties(c_maxImg, Complex.ImaginaryOne, false);

            // test with 'MinReal'
            VerifyLogWithProperties(c_minReal, Complex.One, false);

            // test with 'MinImaginary'
            VerifyLogWithProperties(c_minImg, Complex.ImaginaryOne, false);
        }
    }
}
