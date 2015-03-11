// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Globalization;
using Xunit;

namespace System.Numerics.Tests
{
    public class formattingOperationsTest
    {
        private static void VerifyToString(Complex complex)
        {
            double real = complex.Real;
            double imaginary = complex.Imaginary;
            string expected;
            string actual;

            expected = "(" + real.ToString() + ", " + imaginary.ToString() + ")";
            actual = complex.ToString();
            Assert.Equal(expected, actual);

            CultureInfo germanCultureInfo = new CultureInfo("de-DE");
            NumberFormatInfo germanNumberFormatInfo = CultureInfo.CurrentCulture.NumberFormat;

            expected = "(" + real.ToString(germanNumberFormatInfo) + ", " + imaginary.ToString(germanNumberFormatInfo) + ")";
            actual = complex.ToString(germanNumberFormatInfo);
            Assert.Equal(expected, actual);

            foreach (string format in Support.supportedStdNumericFormats)
            {
                expected = "(" + real.ToString(format) + ", " + imaginary.ToString(format) + ")";
                actual = complex.ToString(format);
                Assert.True(expected.Equals(actual), string.Format("ToString() failed for format {0}.\n\tExpected: <{1}>\n\tActual: <{2}>", format, actual, expected));
                
                germanNumberFormatInfo = CultureInfo.CurrentCulture.NumberFormat;
                expected = "(" + real.ToString(format, germanNumberFormatInfo) + ", " + imaginary.ToString(format, germanNumberFormatInfo) + ")";
                actual = complex.ToString(format, germanNumberFormatInfo);
                Assert.True(expected.Equals(actual), string.Format("ToString() failed for format {0} in de-DE.\n\tExpected: <{1}>\n\tActual: <{2}>", format, actual, expected));
            }
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // Zero.ToString()
            VerifyToString(Complex.Zero);

            // One.ToString()
            VerifyToString(Complex.One);

            // ImaginaryOne.ToString()
            VerifyToString(Complex.ImaginaryOne);

            // MinusOne.ToString()
            VerifyToString(-Complex.One);

            // MinusImaginaryOne.ToString()
            VerifyToString(-Complex.ImaginaryOne);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            double real = Support.GetRandomDoubleValue(false);
            double imaginary = Support.GetRandomDoubleValue(false);
            Complex complexFirst = new Complex(real, imaginary);
            VerifyToString(complexFirst);

            // Verify test results with Small_ComplexInFirstQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            Complex complexFirstSmall = new Complex(real, imaginary);
            VerifyToString(complexFirstSmall);
            
            // Verify test results with ComplexInSecondQuad
            real = Support.GetRandomDoubleValue(true);
            imaginary = Support.GetRandomDoubleValue(false);
            Complex complexSecond = new Complex(real, imaginary);
            VerifyToString(complexSecond);

            // Verify test results with Small_ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            Complex complexSecondSmall = new Complex(real, imaginary);
            VerifyToString(complexSecondSmall);
            
            // Verify test results with ComplexInThirdQuad
            real = Support.GetRandomDoubleValue(true);
            imaginary = Support.GetRandomDoubleValue(true);
            Complex complexThird = new Complex(real, imaginary);
            VerifyToString(complexThird);

            // Verify test results with Small_ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            Complex complexThirdSmall = new Complex(real, imaginary);
            VerifyToString(complexThirdSmall);
            
            // Verify test results with ComplexInFourthQuad
            real = Support.GetRandomDoubleValue(false);
            imaginary = Support.GetRandomDoubleValue(true);
            Complex complexFourth = new Complex(real, imaginary);
            VerifyToString(complexFourth);

            // Verify test results with Small_ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            Complex complexFourthSmall = new Complex(real, imaginary);
            VerifyToString(complexFourthSmall);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Max.ToString()
            Complex c_max = new Complex(double.MaxValue, double.MaxValue);
            VerifyToString(c_max);

            // MaxReal.ToString()
            Complex c_maxReal = new Complex(double.MaxValue, 0.0);
            VerifyToString(c_maxReal);

            // MaxImaginary.ToString()
            Complex c_maxImg = new Complex(0.0, double.MaxValue);
            VerifyToString(c_maxImg);

            // Min.ToString()
            Complex c_min = new Complex(double.MinValue, double.MinValue);
            VerifyToString(c_min);

            // MinReal.ToString()
            Complex c_minReal = new Complex(double.MinValue, 0.0);
            VerifyToString(c_minReal);

            // MinImaginary.ToString()
            Complex c_minImaginary = new Complex(0.0, double.MinValue);
            VerifyToString(c_minImaginary);
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
                Complex c = new Complex(realRandom, imaginaryInvalid);
                VerifyToString(c);
            }

            // Complex number with a valid negative real and an invalid imaginary part
            foreach (double imaginaryInvalid in Support.doubleInvalidValues)
            {
                realRandom = Support.GetRandomDoubleValue(true);
                Complex c = new Complex(realRandom, imaginaryInvalid);
                VerifyToString(c);
            }

            // Complex number with an invalid real and an a positive imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                imaginaryRandom = Support.GetRandomDoubleValue(false);
                Complex c = new Complex(realInvalid, imaginaryRandom);
                VerifyToString(c);
            }

            // Complex number with an invalid real and a negative imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                imaginaryRandom = Support.GetRandomDoubleValue(true);
                Complex c = new Complex(realInvalid, imaginaryRandom);
                VerifyToString(c);
            }

            // Complex number with an invalid real and an invalid imaginary part
            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                foreach (double imaginaryInvalid in Support.doubleInvalidValues)
                {
                    imaginaryRandom = Support.GetRandomDoubleValue(true);
                    Complex c = new Complex(realInvalid, imaginaryInvalid);
                    VerifyToString(c);
                }
            }
        }
    }
}