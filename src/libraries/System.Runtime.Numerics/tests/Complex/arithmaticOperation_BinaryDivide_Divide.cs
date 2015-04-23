// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_BinaryDivide_DivideTest
    {
        // Verification is done with Abs and Conjugate methods
        private static void VerifyBinaryDivideResult(double realFirst, double imgFirst, double realSecond, double imgSecond)
        {
            // Create complex numbers
            Complex cFirst = new Complex(realFirst, imgFirst);
            Complex cSecond = new Complex(realSecond, imgSecond);
            
            Complex cExpectedResult = (cFirst * Complex.Conjugate(cSecond));
            double cExpectedReal = cExpectedResult.Real;
            double cExpectedImaginary = cExpectedResult.Imaginary;

            if (!double.IsInfinity(cExpectedReal))
            {
                cExpectedReal = cExpectedReal / (cSecond.Magnitude * cSecond.Magnitude);
            }
            if (!double.IsInfinity(cExpectedImaginary))
            {
                cExpectedImaginary = cExpectedImaginary / (cSecond.Magnitude * cSecond.Magnitude);
            }

            // local variables
            Complex cResult;

            // arithmetic binary divide operation
            cResult = cFirst / cSecond;

            // verify the result
            Support.VerifyRealImaginaryProperties(cResult, cExpectedReal, cExpectedImaginary,
                string.Format("Binary Divide test = ({0}, {1}) / ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond));

            // arithmetic divide (static) operation
            cResult = Complex.Divide(cFirst, cSecond);

            // verify the result
            Support.VerifyRealImaginaryProperties(cResult, cExpectedReal, cExpectedImaginary,
                string.Format("Divide (Static) test = ({0}, {1}) / ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond));
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            double real = 10;
            double imaginary = 50;

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
            double real = Support.GetSmallRandomDoubleValue(false);
            double img = Support.GetSmallRandomDoubleValue(false);

            // test with 'Max'
            VerifyBinaryDivideResult(double.MaxValue, double.MaxValue, real, img);

            // test with 'Min'
            VerifyBinaryDivideResult(double.MinValue, double.MinValue, real, img);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad / ComplexInFirstQuad
            double realFirst = Support.GetSmallRandomDoubleValue(false);
            double imgFirst = Support.GetSmallRandomDoubleValue(false);
            double realSecond = Support.GetSmallRandomDoubleValue(false);
            double imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFirstQuad / ComplexInSecondQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFirstQuad / ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFirstQuad / ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInSecondQuad / ComplexInSecondQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInSecondQuad / ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInSecondQuad / ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInThirdQuad / ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(true);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInThirdQuad / ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(true);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFourthQuad / ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidImaginaryValues()
        {
            double realFirst;
            double imgFirst;
            double realSecond;
            double imgSecond;

            // Verify with (valid, PositiveInfinity) / (valid, PositiveValid)
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = double.PositiveInfinity;
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, PositiveInfinity) / (valid, NegativeValid)
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = double.PositiveInfinity;
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) / (valid, PositiveValid)
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = double.NegativeInfinity;
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) / (valid, NegativeValid)
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = double.NegativeInfinity;
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NaN) / (valid, Valid)
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = double.NaN;
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidRealValues()
        {
            double realFirst;
            double imgFirst;
            double realSecond;
            double imgSecond;

            // Verify with (PositiveInfinity, valid) / (PositiveValid, valid)
            realFirst = double.PositiveInfinity;
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (PositiveInfinity, valid) / (NegativeValid, valid)
            realFirst = double.PositiveInfinity;
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) / (PositiveValid, valid)
            realFirst = double.NegativeInfinity;
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) / (NegativeValid, valid)
            realFirst = double.NegativeInfinity;
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NaN, valid) / (valid, valid)
            realFirst = double.NaN;
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryDivideResult(realFirst, imgFirst, realSecond, imgSecond);
        }
    }
}
