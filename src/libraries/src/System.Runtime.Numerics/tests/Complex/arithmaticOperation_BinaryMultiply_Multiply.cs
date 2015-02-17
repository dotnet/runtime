// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_BinaryMultiply_MultiplyTest
    {
        private static void VerifyBinaryMultiplyResult(Double realFirst, Double imgFirst, Double realSecond, Double imgSecond)
        {
            // calculate the expected results
            Double realExpectedResult = realFirst * realSecond - imgFirst * imgSecond;
            Double imaginaryExpectedResult = realFirst * imgSecond + imgFirst * realSecond;

            // Create complex numbers
            Complex cFirst = new Complex(realFirst, imgFirst);
            Complex cSecond = new Complex(realSecond, imgSecond);

            // arithmetic multiply (binary) operation
            Complex cResult = cFirst * cSecond;

            // verify the result
            if (false == Support.VerifyRealImaginaryProperties(cResult, realExpectedResult, imaginaryExpectedResult))
            {
                Console.WriteLine("ErRoR! Binary Multiply Error!");
                Console.WriteLine("Binary Multiply test = ({0}, {1}) * ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond);
                Assert.True(false, "Verification Failed");
            }

            // arithmetic multiply (static) operation
            cResult = Complex.Multiply(cFirst, cSecond);

            // verify the result
            if (false == Support.VerifyRealImaginaryProperties(cResult, realExpectedResult, imaginaryExpectedResult))
            {
                Console.WriteLine("ErRoR! Multiply (Static) Error!");
                Console.WriteLine("Multiply (Static) test = ({0}, {1}) * ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond);
                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            Double real = Support.GetRandomDoubleValue(false);
            Double imaginary = Support.GetRandomPhaseValue(false);

            // Test with Zero
            VerifyBinaryMultiplyResult(0.0, 0.0, real, imaginary); // Verify 0*x=0
            VerifyBinaryMultiplyResult(real, imaginary, 0.0, 0.0); // Verify x*0=0

            // Test with One
            VerifyBinaryMultiplyResult(1.0, 0.0, real, imaginary); // Verify 1*x=x
            VerifyBinaryMultiplyResult(real, imaginary, 1.0, 0.0); // Verify x*1=x

            // Test with ImaginaryOne
            VerifyBinaryMultiplyResult(0.0, 1.0, real, imaginary); // Verify -i*x
            VerifyBinaryMultiplyResult(real, imaginary, 0.0, 1.0); // Verify x*-i
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // local variables
            Double real;
            Double img;

            // test with 'Max' * (SmallPositive, SmallPositive)
            real = Support.GetSmallRandomDoubleValue(false);
            img = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(Double.MaxValue, Double.MaxValue, real, img);

            // test with 'Min' * (SmallPositive, SmallPositive)
            real = Support.GetSmallRandomDoubleValue(false);
            img = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(Double.MinValue, Double.MinValue, real, img);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComlexInFirstQuad * ComplexInFirstQuad
            Double realFirst = Support.GetSmallRandomDoubleValue(false);
            Double imgFirst = Support.GetSmallRandomDoubleValue(false);
            Double realSecond = Support.GetSmallRandomDoubleValue(false);
            Double imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad * ComplexInSecondQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad * ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFirstQuad * ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad * ComplexInSecondQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad * ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInSecondQuad * ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInThirdQuad * ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(true);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInThirdQuad * ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(true);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComlexInFourthQuad * ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidImaginaryValues()
        {
            // local variables
            Double realFirst;
            Double imgFirst;
            Double realSecond;
            Double imgSecond;

            // Verify with (valid, PositiveInfinity) * (valid, PositiveValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.PositiveInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, PositiveInfinity) * (valid, NegativeValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.PositiveInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) * (valid, PositiveValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.NegativeInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) * (valid, NegativeValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.NegativeInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NaN) * (valid, Valid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = Double.NaN;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidRealValues()
        {
            // local variables
            Double realFirst;
            Double imgFirst;
            Double realSecond;
            Double imgSecond;

            // Verify with (PositiveInfinity, valid) * (PositiveValid, valid)
            realFirst = Double.PositiveInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (PositiveInfinity, valid) * (NegativeValid, valid)
            realFirst = Double.PositiveInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) * (PositiveValid, valid)
            realFirst = Double.NegativeInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) * (NegativeValid, valid)
            realFirst = Double.NegativeInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NaN, valid) * (valid, valid)
            realFirst = Double.NaN;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);
        }
    }
}
