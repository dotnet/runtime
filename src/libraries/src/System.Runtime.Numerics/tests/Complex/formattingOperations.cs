// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace System.Numerics.Tests
{
    public class formattingOperationsTest
    {
        private static void VerifyToString(Complex complex)
        {
            Double real = complex.Real;
            Double imaginary = complex.Imaginary;
            String manual;
            String automatic;

            manual = "(" + real.ToString() + ", " + imaginary.ToString() + ")";
            automatic = complex.ToString();

            if (!automatic.Equals(manual))
            {
                Console.WriteLine("Error-str2F3P4001! automatic:{0} does not equal to manual:{1}", automatic, manual);
                Assert.True(false, "Verification Failed");
            }

            CultureInfo germanCultureInfo = new CultureInfo("de-DE");
            NumberFormatInfo germanNumberFormatInfo = CultureInfo.CurrentCulture.NumberFormat;

            manual = "(" + real.ToString(germanNumberFormatInfo) + ", " + imaginary.ToString(germanNumberFormatInfo) + ")";
            automatic = complex.ToString(germanNumberFormatInfo);
            if (!automatic.Equals(manual))
            {
                Console.WriteLine("Error-str2F3P4+NFI! automatic:{0} does not equal to manual:{1}", automatic, manual);
                Assert.True(false, "Verification Failed");
            }

            foreach (String format in Support.supportedStdNumericFormats)
            {
                manual = "(" + real.ToString(format) + ", " + imaginary.ToString(format) + ")";
                automatic = complex.ToString(format);

                if (!automatic.Equals(manual))
                {
                    Console.WriteLine("Error-str2F3P4+FMT-{0}! automatic:{1} does not equal to manual:{2}", format, automatic, manual);
                    Assert.True(false, "Verification Failed");
                }

                germanNumberFormatInfo = CultureInfo.CurrentCulture.NumberFormat;
                manual = "(" + real.ToString(format, germanNumberFormatInfo) + ", " + imaginary.ToString(format, germanNumberFormatInfo) + ")";
                automatic = complex.ToString(format, germanNumberFormatInfo);

                if (!automatic.Equals(manual))
                {
                    Console.WriteLine("Error-str2F3P4+FMT-{0}+{1}! automatic:{2} does not equal to manual:{3}", format, "de-DE", automatic, manual);
                    Assert.True(false, "Verification Failed");
                }
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
            // Verify test results with ComlexInFirstQuad
            Double real = Support.GetRandomDoubleValue(false);
            Double imaginary = Support.GetRandomDoubleValue(false);
            Complex complexFirst = new Complex(real, imaginary);
            VerifyToString(complexFirst);

            // Verify test results with Small_ComlexInFirstQuad
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


            // Verify test results with ComlexInThirdQuad
            real = Support.GetRandomDoubleValue(true);
            imaginary = Support.GetRandomDoubleValue(true);
            Complex complexThird = new Complex(real, imaginary);
            VerifyToString(complexThird);

            // Verify test results with Small_ComlexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            Complex complexThirdSmall = new Complex(real, imaginary);
            VerifyToString(complexThirdSmall);


            // Verify test results with ComlexInFourthQuad
            real = Support.GetRandomDoubleValue(false);
            imaginary = Support.GetRandomDoubleValue(true);
            Complex complexFourth = new Complex(real, imaginary);
            VerifyToString(complexFourth);

            // Verify test results with Small_ComlexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            Complex complexFourthSmall = new Complex(real, imaginary);
            VerifyToString(complexFourthSmall);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Max.ToString()
            Complex c_max = new Complex(Double.MaxValue, Double.MaxValue);
            VerifyToString(c_max);

            // MaxReal.ToString()
            Complex c_maxReal = new Complex(Double.MaxValue, 0.0);
            VerifyToString(c_maxReal);

            // MaxImaginary.ToString()
            Complex c_maxImg = new Complex(0.0, Double.MaxValue);
            VerifyToString(c_maxImg);

            // Min.ToString()
            Complex c_min = new Complex(Double.MinValue, Double.MinValue);
            VerifyToString(c_min);

            // MinReal.ToString()
            Complex c_minReal = new Complex(Double.MinValue, 0.0);
            VerifyToString(c_minReal);

            // MinImaginary.ToString()
            Complex c_minImaginary = new Complex(0.0, Double.MinValue);
            VerifyToString(c_minImaginary);
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