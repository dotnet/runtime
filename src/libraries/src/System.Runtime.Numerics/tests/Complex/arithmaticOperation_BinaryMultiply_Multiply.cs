// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class arithmaticOperation_BinaryMultiply_MultiplyTest
    {
        private static void VerifyBinaryMultiplyResult(double realFirst, double imgFirst, double realSecond, double imgSecond)
        {
            // calculate the expected results
            double realExpectedResult = realFirst * realSecond - imgFirst * imgSecond;
            double imaginaryExpectedResult = realFirst * imgSecond + imgFirst * realSecond;

            // Create complex numbers
            Complex cFirst = new Complex(realFirst, imgFirst);
            Complex cSecond = new Complex(realSecond, imgSecond);

            // arithmetic multiply (binary) operation
            Complex cResult = cFirst * cSecond;

            // verify the result
            Support.VerifyRealImaginaryProperties(cResult, realExpectedResult, imaginaryExpectedResult,
                string.Format("Binary Multiply test = ({0}, {1}) * ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond));

            // arithmetic multiply (static) operation
            cResult = Complex.Multiply(cFirst, cSecond);

            // verify the result
            Support.VerifyRealImaginaryProperties(cResult, realExpectedResult, imaginaryExpectedResult,
                string.Format("Multiply (Static) test = ({0}, {1}) * ({2}, {3})", realFirst, imgFirst, realSecond, imgSecond));
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            double real = Support.GetRandomDoubleValue(false);
            double imaginary = Support.GetRandomPhaseValue(false);

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
            double real;
            double img;

            // test with 'Max' * (SmallPositive, SmallPositive)
            real = Support.GetSmallRandomDoubleValue(false);
            img = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(double.MaxValue, double.MaxValue, real, img);

            // test with 'Min' * (SmallPositive, SmallPositive)
            real = Support.GetSmallRandomDoubleValue(false);
            img = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(double.MinValue, double.MinValue, real, img);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad * ComplexInFirstQuad
            double realFirst = Support.GetSmallRandomDoubleValue(false);
            double imgFirst = Support.GetSmallRandomDoubleValue(false);
            double realSecond = Support.GetSmallRandomDoubleValue(false);
            double imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFirstQuad * ComplexInSecondQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFirstQuad * ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFirstQuad * ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(false);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInSecondQuad * ComplexInSecondQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInSecondQuad * ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInSecondQuad * ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInThirdQuad * ComplexInThirdQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(true);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInThirdQuad * ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(true);
            realSecond = Support.GetSmallRandomDoubleValue(false);
            imgSecond = Support.GetSmallRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify test results with ComplexInFourthQuad * ComplexInFourthQuad
            realFirst = Support.GetSmallRandomDoubleValue(true);
            imgFirst = Support.GetSmallRandomDoubleValue(false);
            realSecond = Support.GetSmallRandomDoubleValue(true);
            imgSecond = Support.GetSmallRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidImaginaryValues()
        {
            double realFirst;
            double imgFirst;
            double realSecond;
            double imgSecond;

            // Verify with (valid, PositiveInfinity) * (valid, PositiveValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = double.PositiveInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, PositiveInfinity) * (valid, NegativeValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = double.PositiveInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) * (valid, PositiveValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = double.NegativeInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NegativeInfinity) * (valid, NegativeValid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = double.NegativeInfinity;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(true);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (valid, NaN) * (valid, Valid)
            realFirst = Support.GetRandomDoubleValue(false);
            imgFirst = double.NaN;
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);
        }

        [Fact]
        public static void RunTests_InvalidRealValues()
        {
            double realFirst;
            double imgFirst;
            double realSecond;
            double imgSecond;

            // Verify with (PositiveInfinity, valid) * (PositiveValid, valid)
            realFirst = double.PositiveInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (PositiveInfinity, valid) * (NegativeValid, valid)
            realFirst = double.PositiveInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) * (PositiveValid, valid)
            realFirst = double.NegativeInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NegativeInfinity, valid) * (NegativeValid, valid)
            realFirst = double.NegativeInfinity;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(true);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);

            // Verify with (NaN, valid) * (valid, valid)
            realFirst = double.NaN;
            imgFirst = Support.GetRandomDoubleValue(false);
            realSecond = Support.GetRandomDoubleValue(false);
            imgSecond = Support.GetRandomDoubleValue(false);
            VerifyBinaryMultiplyResult(realFirst, imgFirst, realSecond, imgSecond);
        }
    }
}
