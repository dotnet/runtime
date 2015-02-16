// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class standardNumericalFunctions_ExpTest
    {
        private static void VerifyExpWithAddition(Double real, Double imaginary)
        {
            // verify with e(x+y) = e(x)*e(y) if xy == yx
            Complex realComplex = new Complex(real, 0.0);
            Complex imgComplex = new Complex(0.0, imaginary);

            Complex ri = realComplex * imgComplex;
            Complex ir = imgComplex * realComplex;
            if (!ri.Equals(ir))
            {
                return;
            }

            Complex realExp = Complex.Exp(realComplex);
            Complex imgExp = Complex.Exp(imgComplex);
            Complex expectedExp = realExp * imgExp;

            Complex complex = new Complex(real, imaginary);
            Complex complexExp = Complex.Exp(complex);

            if (false == Support.VerifyRealImaginaryProperties(complexExp, expectedExp.Real, expectedExp.Imaginary))
            {
                Console.WriteLine("Error eXp-Err3521! Exp({0}):{1} != {2})", complex, complexExp, expectedExp);
                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify log with Zero
            VerifyExpWithAddition(0.0, 0.0);

            // Verify test results with One
            VerifyExpWithAddition(1.0, 0.0);

            // Verify test results with MinusOne
            VerifyExpWithAddition(-1.0, 0.0);

            // Verify test results with ImaginaryOne
            VerifyExpWithAddition(0.0, 1.0);

            // Verify test results with MinusImaginaryOne
            VerifyExpWithAddition(0.0, -1.0);
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // Verify test results with ComplexInFirstQuad
            Double real = Support.GetSmallRandomDoubleValue(false);
            Double imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyExpWithAddition(real, imaginary);

            // Verify test results with ComplexInSecondQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            VerifyExpWithAddition(real, imaginary);


            // Verify test results with ComplexInThirdQuad
            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyExpWithAddition(real, imaginary);

            // Verify test results with ComplexInFourthQuad
            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            VerifyExpWithAddition(real, imaginary);
        }

        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with Max
            Complex max = new Complex(Double.MaxValue, Double.MaxValue);

            Complex complexExp = Complex.Exp(max);
            if (false == Support.VerifyRealImaginaryProperties(complexExp, Math.Cos(Double.MaxValue) * Double.PositiveInfinity, Double.PositiveInfinity)) //for IA64
            {
                Console.WriteLine("Error eXp-Max:Err6589! Exp(Max) is not (Infinity, Infinity))");
                Assert.True(false, "Verification Failed");
            }

            // Verify test results with MaxReal

            Complex maxReal = new Complex(Double.MaxValue, 0.0);

            complexExp = Complex.Exp(max);
            if (false == Support.VerifyRealImaginaryProperties(complexExp, Math.Cos(Double.MaxValue) * Double.PositiveInfinity, Double.PositiveInfinity)) //for IA64
            {
                Console.WriteLine("Error eXp-MAxReal:Err697.1! Exp(MaxReal) is not (Infinity, Infinity))");
                Assert.True(false, "Verification Failed");
            }

            // Verify test results with MaxImg
            VerifyExpWithAddition(0.0, Double.MaxValue);

            // Verify test results with Min
            VerifyExpWithAddition(Double.MinValue, Double.MinValue);

            // Verify test results with MinReal
            VerifyExpWithAddition(Double.MinValue, 0.0);

            // Verify test results with MinImaginary
            VerifyExpWithAddition(0.0, Double.MinValue);
        }
    }
}
