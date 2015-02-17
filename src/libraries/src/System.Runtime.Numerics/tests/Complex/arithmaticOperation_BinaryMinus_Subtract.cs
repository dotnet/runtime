// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_BinaryMinus_SubtractTest
    {
        private static void VerifyBinaryMinusResult(Double realFirst, Double imgFirst, Double realSecond, Double imgSecond)
        {
            // Create complex numbers
            Complex cFirst = new Complex(realFirst, imgFirst);
            Complex cSecond = new Complex(realSecond, imgSecond);

            // calculate the expected results
            Double realExpectedResult = realFirst - realSecond;
            Double imgExpectedResult = imgFirst - imgSecond;

            // local varuables
            Complex cResult;

            // arithmetic binary minus operation
            cResult = cFirst - cSecond;

            // verify the result
            if (false == Support.VerifyRealImaginaryProperties(cResult, realExpectedResult, imgExpectedResult))
            {
                Console.WriteLine("ErRoR! Binary Minus Error!");
                Console.WriteLine("Binary Minus test = ({0}, {1}) - ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond);
                Assert.True(false, "Verification Failed");
            }

            // arithmetic substract operation
            cResult = Complex.Subtract(cFirst, cSecond);

            // verify the result
            if (false == Support.VerifyRealImaginaryProperties(cResult, realExpectedResult, imgExpectedResult))
            {
                Console.WriteLine("ErRoR! Substract Error!");
                Console.WriteLine("Substract test = ({0}, {1}) - ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond);
                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_Zero()
        {
            // local variables
            Double real = Support.GetRandomDoubleValue(false);
            Double imaginary = Support.GetRandomDoubleValue(false);

            // Test with Zero
            VerifyBinaryMinusResult(real, imaginary, 0.0, 0.0); // Verify x-0=0
            VerifyBinaryMinusResult(0.0, 0.0, real, imaginary); // Verify 0-x=-x
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // local variables
            Double real;
            Double img;

            // test with 'Max' - (Positive, Positive)
            real = Support.GetRandomDoubleValue(false);
            img = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(Double.MaxValue, Double.MaxValue, real, img);

            // test with (Positive, Positive) - 'Max'
            real = Support.GetRandomDoubleValue(false);
            img = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(real, img, Double.MaxValue, Double.MaxValue);

            // test with 'Max' - (Negative, Negative)
            real = Support.GetRandomDoubleValue(true);
            img = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(Double.MaxValue, Double.MaxValue, real, img);

            // test with (Negative, Negative) - 'Max'
            real = Support.GetRandomDoubleValue(true);
            img = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(real, img, Double.MaxValue, Double.MaxValue);

            // test with 'Min' - (Positive, Positive)
            real = Support.GetRandomDoubleValue(false);
            img = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(Double.MinValue, Double.MinValue, real, img);

            // test with (Positive, Positive) - 'Min'
            real = Support.GetRandomDoubleValue(false);
            img = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(real, img, Double.MinValue, Double.MinValue);

            // test with 'Min' - (Negative, Negative)
            real = Support.GetRandomDoubleValue(true);
            img = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(Double.MinValue, Double.MinValue, real, img);

            // test with (Negative, Negative) - 'Min'
            real = Support.GetRandomDoubleValue(true);
            img = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(real, img, Double.MinValue, Double.MinValue);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComlexInFirstQuad - ComplexInFirstQuad
            Double realFirst = Support.GetRandomDoubleValue(false);
            Double imgFirst = Support.GetRandomDoubleValue(false);
            Double realSecond = Support.GetRandomDoubleValue(false);
            Double imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad - ComplexInSecondQuad
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad - ComplexInThirdQuad
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad - ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad - ComplexInSecondQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad - ComplexInThirdQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad - ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInThirdQuad - ComplexInThirdQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(true);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInThirdQuad - ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(true);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFourthQuad - ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidImaginaryValues()
        {
            // local variables
            Double realFirst;
            Double imgFirst;
            Double realSecond;
            Double imgSecond;

            // Verify with (valid, PositiveInfinity) - (valid, PositiveValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.PositiveInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, PositiveInfinity) - (valid, NegativeValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.PositiveInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) - (valid, PositiveValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.NegativeInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) - (valid, NegativeValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.NegativeInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NaN) - (valid, Valid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.NaN;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidRealValues()
        {
            // local variables
            Double realFirst;
            Double imgFirst;
            Double realSecond;
            Double imgSecond;

            // Verify with (PositiveInfinity, valid) - (PositiveValid, valid)
            realFirst = Double.PositiveInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (PositiveInfinity, valid) - (NegativeValid, valid)
            realFirst = Double.PositiveInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) - (PositiveValid, valid)
            realFirst = Double.NegativeInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) - (NegativeValid, valid)
            realFirst = Double.NegativeInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NaN, valid) - (valid, valid)
            realFirst = Double.NaN;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMinusResult(realFirst, imgFirst, realSecond, imgSecond);
        }
    }
}
