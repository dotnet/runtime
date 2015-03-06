// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_BinaryPlus_AddTest
    {
        private static void VerifyBinaryPlusResult(Double realFirst, Double imgFirst, Double realSecond, Double imgSecond)
        {
            // Create complex numbers
            Complex cFirst = new Complex(realFirst, imgFirst);
            Complex cSecond = new Complex(realSecond, imgSecond);

            // calculate the expected results
            Double realExpectedResult = realFirst + realSecond;
            Double imgExpectedResult = imgFirst + imgSecond;

            //local variable
            Complex cResult;

            // arithmetic addition operation
            cResult = cFirst + cSecond;

            // verify the result
            if (!Support.VerifyRealImaginaryProperties(cResult, realExpectedResult, imgExpectedResult))
            {
                Console.WriteLine("ErRoR! Binary Plus Error!");
                Console.WriteLine("Binary Plus test = ({0}, {1}) + ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond);
                Assert.True(false, "Verification Failed");
            }

            // arithmetic static addition operation
            cResult = Complex.Add(cFirst, cSecond);

            // verify the result
            if (!Support.VerifyRealImaginaryProperties(cResult, realExpectedResult, imgExpectedResult))
            {
                Console.WriteLine("ErRoR! Add Error!");
                Console.WriteLine("Add test = ({0}, {1}) + ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond);
                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_Zero()
        {
            Double real = Support.GetRandomDoubleValue(false);
            Double imaginary = Support.GetRandomDoubleValue(false);

            VerifyBinaryPlusResult(0.0, 0.0, real, imaginary); // Verify 0+x=0
            VerifyBinaryPlusResult(real, imaginary, 0.0, 0.0); // Verify 0+x=0
        }


        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // local variables
            Double real;
            Double img;

            // test with 'Max' + (Positive, Positive)
            real = Support.GetRandomDoubleValue(false);
            img = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(Double.MaxValue, Double.MaxValue, real, img);

            // test with 'Max' + (Negative, Negative)
            real = Support.GetRandomDoubleValue(true);
            img = Support.GetRandomDoubleValue(true);
            VerifyBinaryPlusResult(Double.MaxValue, Double.MaxValue, real, img);

            // test with 'Min' + (Positive, Positive)
            real = Support.GetRandomDoubleValue(false);
            img = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(Double.MinValue, Double.MinValue, real, img);

            // test with 'Min' + (Negative, Negative)
            real = Support.GetRandomDoubleValue(true);
            img = Support.GetRandomDoubleValue(true);
            VerifyBinaryPlusResult(Double.MinValue, Double.MinValue, real, img);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComlexInFirstQuad + ComplexInFirstQuad
            Double realFirst = Support.GetRandomDoubleValue(false);
            Double imgFirst = Support.GetRandomDoubleValue(false);
            Double realSecond = Support.GetRandomDoubleValue(false);
            Double imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad + ComplexInSecondQuad
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad + ComplexInThirdQuad
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad + ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad + ComplexInSecondQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad + ComplexInThirdQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad + ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInThirdQuad + ComplexInThirdQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(true);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInThirdQuad + ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(true);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFourthQuad + ComplexInFourthQuad
            realFirst = Support.GetRandomDoubleValue(true);
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidImaginaryValues()
        {
            // local variables
            Double realFirst;
            Double imgFirst;
            Double realSecond;
            Double imgSecond;

            // Verify with (valid, PositiveInfinity) + (valid, PositiveValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.PositiveInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, PositiveInfinity) + (valid, NegativeValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.PositiveInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) + (valid, PositiveValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.NegativeInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) + (valid, NegativeValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.NegativeInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NaN) + (valid, Valid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.NaN;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidRealValues()
        {
            // local variables
            Double realFirst;
            Double imgFirst;
            Double realSecond;
            Double imgSecond;

            // Verify with (PositiveInfinity, valid) + (PositiveValid, valid)
            realFirst = Double.PositiveInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (PositiveInfinity, valid) + (NegativeValid, valid)
            realFirst = Double.PositiveInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) + (PositiveValid, valid)
            realFirst = Double.NegativeInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) + (NegativeValid, valid)
            realFirst = Double.NegativeInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NaN, valid) + (valid, valid)
            realFirst = Double.NaN;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryPlusResult(realFirst, imgFirst, realSecond, imgSecond);
        }
    }
}
