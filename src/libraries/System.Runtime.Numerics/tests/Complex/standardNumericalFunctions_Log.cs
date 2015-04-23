// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
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
            double equalityCondition = Math.Atan2(c1.Imaginary, c1.Real) + Math.Atan2(c2.Imaginary, c2.Real);
            if (equalityCondition <= -Math.PI || equalityCondition > Math.PI)
            {
                return;
            }

            Complex logComplex = Complex.Log(c1 * c2);
            Complex logExpectedComplex = Complex.Log(c1) + Complex.Log(c2);

            Support.VerifyRealImaginaryProperties(logComplex, logExpectedComplex.Real, logExpectedComplex.Imaginary,
                string.Format("Log({0}*{1}):{2} != {3})", c1, c2, logComplex, logExpectedComplex));
        }

        private static void VerifyLogWithPowerMinusOne(Complex c)
        {
            // Log(c) == -Log(1/c)
            Complex logComplex = Complex.Log(c);
            Complex logPowerMinusOne = Complex.Log(1 / c);

            Support.VerifyRealImaginaryProperties(logComplex, -logPowerMinusOne.Real, -logPowerMinusOne.Imaginary,
                string.Format("Log({0}):{1} != {2} as expected", c, logComplex, -logPowerMinusOne));
        }

        private static void VerifyLogWithExp(Complex c)
        {
            // Exp(log(c)) == c, if c != Zero
            Complex logComplex = Complex.Log(c);
            Complex expLogComplex = Complex.Exp(logComplex);

            Support.VerifyRealImaginaryProperties(expLogComplex, c.Real, c.Imaginary,
                string.Format("Exp(Log({0}):{1} != {2})", c, expLogComplex, c));
        }

        private static void VerifyLogWithBase10(Complex complex)
        {
            Complex logValue = Complex.Log10(complex);
            Complex logComplex = Complex.Log(complex);
            double baseLog = Math.Log(10);
            Complex expectedLog = logComplex / baseLog;

            Support.VerifyRealImaginaryProperties(logValue, expectedLog.Real, expectedLog.Imaginary, 
                string.Format("Log({0}, {1}):{2} != {3} as expected", complex, 10, logValue, expectedLog));
        }

        private static void VerifyLogWithBase(Complex complex)
        {
            double baseValue = 0.0;
            double baseLog;

            // Verify with Random Int32
            do
            {
                baseValue = Support.GetRandomInt32Value(false);
            }
            while (0 == baseValue);

            Complex logValue = Complex.Log(complex, baseValue);

            Complex logComplex = Complex.Log(complex);
            baseLog = Math.Log(baseValue);
            Complex expectedLog = logComplex / baseLog;

            Support.VerifyRealImaginaryProperties(logValue, expectedLog.Real, expectedLog.Imaginary, 
                string.Format("Log({0}, {1}):{2} != {3} as expected", complex, baseValue, logValue, expectedLog));

            // Verify with Random double value
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

            Support.VerifyRealImaginaryProperties(logValue, expectedLog.Real, expectedLog.Imaginary, 
                string.Format("Log({0}, {1}):{2} != {3} as expected", complex, baseValue, logValue, expectedLog));
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            Complex logValue = Complex.Log(Complex.Zero);
            Support.VerifyRealImaginaryProperties(logValue, double.NegativeInfinity, 0.0, "Verify log of zero");

            // Verify log10 with Zero
            logValue = Complex.Log10(Complex.Zero);
            Support.VerifyRealImaginaryProperties(logValue, double.NegativeInfinity, 0.0, "Verify log10 of zero");

            // Verify log base with Zero
            double baseValue = Support.GetRandomInt32Value(false);
            logValue = Complex.Log(Complex.Zero, baseValue);
            Support.VerifyRealImaginaryProperties(logValue, double.NegativeInfinity, double.NaN, "Verify log base of zero");

            // Verify test results with Zero - One
            VerifyLogWithProperties(Complex.One, Complex.Zero);

            // Verify test results with One - ImaginaryOne
            VerifyLogWithProperties(Complex.One, Complex.ImaginaryOne);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            Complex c_maxReal = new Complex(double.MaxValue, 0.0);
            Complex c_maxImg = new Complex(0.0, double.MaxValue);
            Complex c_minReal = new Complex(double.MinValue, 0.0);
            Complex c_minImg = new Complex(0.0, double.MinValue);

            // MaxReal
            VerifyLogWithProperties(c_maxReal, Complex.One, false);

            // MaxImaginary
            VerifyLogWithProperties(c_maxImg, Complex.ImaginaryOne, false);

            // MinReal
            VerifyLogWithProperties(c_minReal, Complex.One, false);

            // MinImaginary
            VerifyLogWithProperties(c_minImg, Complex.ImaginaryOne, false);
        }
    }
}
