// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class standardNumericalFunctions_ExpTest
    {
        private static void VerifyExpWithAddition(double real, double imaginary)
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

            Support.VerifyRealImaginaryProperties(complexExp, expectedExp.Real, expectedExp.Imaginary,
                string.Format("Exp({0}):{1} != {2})", complex, complexExp, expectedExp));
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
            double real = Support.GetSmallRandomDoubleValue(false);
            double imaginary = Support.GetSmallRandomDoubleValue(false);
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
            Complex max = new Complex(double.MaxValue, double.MaxValue);

            Complex complexExp = Complex.Exp(max);
            Support.VerifyRealImaginaryProperties(complexExp, Math.Cos(double.MaxValue) * double.PositiveInfinity, double.PositiveInfinity,
                string.Format("Exp(Max) is not (Infinity, Infinity)"));

            // Verify test results with MaxReal
            Complex maxReal = new Complex(double.MaxValue, 0.0);

            complexExp = Complex.Exp(max);
            Support.VerifyRealImaginaryProperties(complexExp, Math.Cos(double.MaxValue) * double.PositiveInfinity, double.PositiveInfinity, 
                string.Format("Exp(MaxReal) is not (Infinity, Infinity))"));

            // Verify test results with MaxImg
            VerifyExpWithAddition(0.0, double.MaxValue);

            // Verify test results with Min
            VerifyExpWithAddition(double.MinValue, double.MinValue);

            // Verify test results with MinReal
            VerifyExpWithAddition(double.MinValue, 0.0);

            // Verify test results with MinImaginary
            VerifyExpWithAddition(0.0, double.MinValue);
        }
    }
}
