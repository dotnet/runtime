// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_BinaryMinus_SubtractTest
    {
        private static void VerifyBinaryMinusResult(double realFirst, double imgFirst, double realSecond, double imgSecond)
        {
            // Create complex numbers
            Complex cFirst = new Complex(realFirst, imgFirst);
            Complex cSecond = new Complex(realSecond, imgSecond);

            // calculate the expected results
            double realExpectedResult = realFirst - realSecond;
            double imgExpectedResult = imgFirst - imgSecond;

            // local varuables
            Complex cResult;

            // arithmetic binary minus operation
            cResult = cFirst - cSecond;

            // verify the result
            Support.VerifyRealImaginaryProperties(cResult, realExpectedResult, imgExpectedResult,
                string.Format("Binary Minus test = ({0}, {1}) - ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond));

            // arithmetic substract operation
            cResult = Complex.Subtract(cFirst, cSecond);

            // verify the result
            Support.VerifyRealImaginaryProperties(cResult, realExpectedResult, imgExpectedResult, 
                string.Format("Substract test = ({0}, {1}) - ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond));
        }

        [Fact]
        public static void RunTests_Zero()
        {
            double real = Support.GetRandomDoubleValue(false);
            double imaginary = Support.GetRandomDoubleValue(false);

            // Test with Zero
            VerifyBinaryMinusResult(real, imaginary, 0.0, 0.0); // Verify x-0=0
            VerifyBinaryMinusResult(0.0, 0.0, real, imaginary); // Verify 0-x=-x
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            double real;
            double img;

            // test with 'Max' - (Positive, Positive)
            real = Support.GetRandomDoubleValue(false);
            img = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(double.MaxValue, double.MaxValue, real, img);

            // test with (Positive, Positive) - 'Max'
            real = Support.GetRandomDoubleValue(false);
            img = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(real, img, double.MaxValue, double.MaxValue);

            // test with 'Max' - (Negative, Negative)
            real = Support.GetRandomDoubleValue(true);
            img = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(double.MaxValue, double.MaxValue, real, img);

            // test with (Negative, Negative) - 'Max'
            real = Support.GetRandomDoubleValue(true);
            img = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(real, img, double.MaxValue, double.MaxValue);

            // test with 'Min' - (Positive, Positive)
            real = Support.GetRandomDoubleValue(false);
            img = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(double.MinValue, double.MinValue, real, img);

            // test with (Positive, Positive) - 'Min'
            real = Support.GetRandomDoubleValue(false);
            img = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(real, img, double.MinValue, double.MinValue);

            // test with 'Min' - (Negative, Negative)
            real = Support.GetRandomDoubleValue(true);
            img = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(double.MinValue, double.MinValue, real, img);

            // test with (Negative, Negative) - 'Min'
            real = Support.GetRandomDoubleValue(true);
            img = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(real, img, double.MinValue, double.MinValue);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad - ComplexInFirstQuad
            double realFirst = Support.GetRandomDoubleValue(false);
            double imgFirst = Support.GetRandomDoubleValue(false);
            double realSecond = Support.GetRandomDoubleValue(false);
            double imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFirstQuad - ComplexInSecondQuad
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFirstQuad - ComplexInThirdQuad
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFirstQuad - ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInSecondQuad - ComplexInSecondQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInSecondQuad - ComplexInThirdQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInSecondQuad - ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInThirdQuad - ComplexInThirdQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(true);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInThirdQuad - ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(true);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFourthQuad - ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidImaginaryValues()
        {
            double realFirst;
            double imgFirst;
            double realSecond;
            double imgSecond;

            // Verify with (valid, PositiveInfinity) - (valid, PositiveValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = double.PositiveInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, PositiveInfinity) - (valid, NegativeValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = double.PositiveInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) - (valid, PositiveValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = double.NegativeInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) - (valid, NegativeValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = double.NegativeInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NaN) - (valid, Valid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = double.NaN;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidRealValues()
        {
            double realFirst;
            double imgFirst;
            double realSecond;
            double imgSecond;

            // Verify with (PositiveInfinity, valid) - (PositiveValid, valid)
            realFirst = double.PositiveInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (PositiveInfinity, valid) - (NegativeValid, valid)
            realFirst = double.PositiveInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) - (PositiveValid, valid)
            realFirst = double.NegativeInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) - (NegativeValid, valid)
            realFirst = double.NegativeInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NaN, valid) - (valid, valid)
            realFirst = double.NaN;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);
        }
    }
}
