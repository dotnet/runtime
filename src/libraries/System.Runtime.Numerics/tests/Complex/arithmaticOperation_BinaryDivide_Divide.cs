// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_BinaryDivide_DivideTest
    {
        //Verification is done with Abs and Conjugate methods
        private static void VerifyBinaryDivideResult(Double realFirst, Double imgFirst, Double realSecond, Double imgSecond)
        {
            // Create complex numbers
            Complex cFirst = new Complex(realFirst, imgFirst);
            Complex cSecond = new Complex(realSecond, imgSecond);


            Complex cExpectedResult = (cFirst * Complex.Conjugate(cSecond));
            Double cExpectedReal = cExpectedResult.Real;
            Double cExpectedImaginary = cExpectedResult.Imaginary;

            if (!Double.IsInfinity(cExpectedReal))
                cExpectedReal = cExpectedReal / (cSecond.Magnitude * cSecond.Magnitude);
            if (!Double.IsInfinity(cExpectedImaginary))
                cExpectedImaginary = cExpectedImaginary / (cSecond.Magnitude * cSecond.Magnitude);

            //local variables
            Complex cResult;

            // arithmetic binary divide operation
            cResult = cFirst / cSecond;

            // verify the result
            if (false == Support.VerifyRealImaginaryProperties(cResult, cExpectedReal, cExpectedImaginary))
            {
                Console.WriteLine("Error_ryt02!!! Binary Divide test = ({0}, {1}) / ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond);

                Assert.True(false, "Verification Failed");
            }

            // arithmetic divide (static) operation
            cResult = Complex.Divide(cFirst, cSecond);

            // verify the result
            if (false == Support.VerifyRealImaginaryProperties(cResult, cExpectedReal, cExpectedImaginary))
            {
                Console.WriteLine("Error_a1s12!!! Divide (Static) test = ({0}, {1}) / ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond);

                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            Double real = 10;//Support.GetRandomDoubleValue(false);
            Double imaginary = 50;//Support.GetRandomPhaseValue(false);

            // Test with Zero
            VerifyBinaryDivideResult(0.0, 0.0, real, imaginary); // Verify 0/x=0
            VerifyBinaryDivideResult(real, imaginary, 0.0, 0.0); // Verify x0=0

            // Test with One
            VerifyBinaryDivideResult(1.0, 0.0, real, imaginary); // Verify 1/x=x
            VerifyBinaryDivideResult(real, imaginary, 1.0, 0.0); // Verify x/1=x

            // Test with ImaginaryOne
            VerifyBinaryDivideResult(0.0, 1.0, real, imaginary); // Verify -i/x
            VerifyBinaryDivideResult(real, imaginary, 0.0, 1.0); // Verify x/-i
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // local variables
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double img = Support.GetSmallRandomDoubleValue(false);

            // test with 'Max'
            VerifyBinaryDivideResult(Double.MaxValue, Double.MaxValue, real, img);

            // test with 'Min'
            VerifyBinaryDivideResult(Double.MinValue, Double.MinValue, real, img);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComlexInFirstQuad / ComplexInFirstQuad
            Double realFirst = Support.GetSmallRandomDoubleValue(false);
            Double imgFirst = Support.GetSmallRandomDoubleValue(false);
            Double realSecond = Support.GetSmallRandomDoubleValue(false);
            Double imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad / ComplexInSecondQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad / ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad / ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad / ComplexInSecondQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad / ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad / ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInThirdQuad / ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(true);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInThirdQuad / ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(true);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFourthQuad / ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidImaginaryValues()
        {
            // local variables
            Double realFirst;
            Double imgFirst;
            Double realSecond;
            Double imgSecond;

            // Verify with (valid, PositiveInfinity) / (valid, PositiveValid)
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Double.PositiveInfinity;
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, PositiveInfinity) / (valid, NegativeValid)
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Double.PositiveInfinity;
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) / (valid, PositiveValid)
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Double.NegativeInfinity;
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) / (valid, NegativeValid)
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Double.NegativeInfinity;
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NaN) / (valid, Valid)
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Double.NaN;
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidRealValues()
        {
            // local variables
            Double realFirst;
            Double imgFirst;
            Double realSecond;
            Double imgSecond;

            // Verify with (PositiveInfinity, valid) / (PositiveValid, valid)
            realFirst = Double.PositiveInfinity;
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (PositiveInfinity, valid) / (NegativeValid, valid)
            realFirst = Double.PositiveInfinity;
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) / (PositiveValid, valid)
            realFirst = Double.NegativeInfinity;
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) / (NegativeValid, valid)
            realFirst = Double.NegativeInfinity;
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NaN, valid) / (valid, valid)
            realFirst = Double.NaN;
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);
        }
    }
}
