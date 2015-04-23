// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class staticConstantsTest
    {
        [Fact]
        public static void RunTests_Zero()
        {
            // Verify it is zero.
            Assert.True(0 == Complex.Zero, string.Format("Zero does not equal to (0.0, 0.0)"));

            // Verify real and imaginary parts are 0.0
            Support.VerifyRealImaginaryProperties(Complex.Zero, 0.0, 0.0, "Verify real and imaginary parts are 0.0");
            
            // verify magnitude is 0.0 and phase is NaN.
            Support.VerifyMagnitudePhaseProperties(Complex.Zero, 0.0, double.NaN, "Verify magnitude is 0.0 and phase is NaN");

            // create a complex number with random real and imaginary parts
            double realPart = Support.GetRandomDoubleValue(false);
            double imaginaryPart = Support.GetRandomDoubleValue(false);
            Complex complexRandom = new Complex(realPart, imaginaryPart);

            // verify x*0 = 0
            Complex multResult = complexRandom * Complex.Zero;
            Assert.True(0 == multResult, string.Format("Multiplication with Zero does not equal to 0: {0}", multResult));

            // verify x/0 = Infinity
            Complex divisorZeroResult = complexRandom / Complex.Zero;
            Support.VerifyRealImaginaryProperties(divisorZeroResult, double.NaN, double.NaN, 
                string.Format("Division with Zero does not equal to (NaN, NaN): {0}", divisorZeroResult));

            // verify 0/x = 0
            Complex divZeroResult = Complex.Zero / complexRandom;
            Assert.True(0 == divZeroResult, string.Format("Division of Zero does not equal to 0: {0}", divZeroResult));

            // verify x - 0 = 0
            Complex subZeroResult = complexRandom - Complex.Zero;
            Assert.True(complexRandom == subZeroResult, string.Format("Zero sub does not equal to complex {0} itself: {1}", complexRandom, subZeroResult));

            // verify 0 - x = -x
            Complex subComplexResult = Complex.Zero - complexRandom;
            Assert.True(subComplexResult == -complexRandom, string.Format("Sub from Zero does not equal to neg complex {0}: {1}", complexRandom, subComplexResult));

            // verify x + 0 = x
            Complex addZeroResult = complexRandom + Complex.Zero;
            Assert.True(addZeroResult == complexRandom, string.Format("Add Zero result does not equal to complex {0}: {1}", complexRandom, addZeroResult));

            // verify 0 + x = x
            Complex addComplexResult = Complex.Zero + complexRandom;
            Assert.True(addComplexResult == complexRandom, string.Format("Add to Zero result does not equal to complex {0}: {1}", complexRandom, addComplexResult));

            // verify Abs(0) = 0
            double absResult = Complex.Abs(Complex.Zero);
            Assert.True(0.0 == absResult, string.Format("Abs(Zero) does not equal to 0: {0}", absResult));

            // verify Conjugate(0) = 0
            Complex conjugateResult = Complex.Conjugate(Complex.Zero);
            Assert.True(0 == conjugateResult, string.Format("Conjugate(Zero) does not equal to 0: {0}", conjugateResult));

            // verify Reciprocal(0) goes to 0
            Complex reciprocalResult = Complex.Reciprocal(Complex.Zero);
            Assert.True(reciprocalResult == Complex.Zero, string.Format("Reciprocal(Zero) does not go to Infinity: {0}", reciprocalResult));
        }

        [Fact]
        public static void RunTests_One()
        {
            // Verify real part is 1.0, and imaginary part is 0.0
            Support.VerifyRealImaginaryProperties(Complex.One, 1.0, 0.0, "Verify real part is 1.0, and imaginary part is 0.0");

            // verify magnitude is 1.0 and phase is 0.0.
            Support.VerifyMagnitudePhaseProperties(Complex.One, 1.0, 0.0, "Verify magnitude is 1.0, and phase is 0.0");

            // create a complex number with random real and imaginary parts
            double realPart = Support.GetRandomDoubleValue(false);
            double imaginaryPart = Support.GetRandomDoubleValue(false);
            Complex complexRandom = new Complex(realPart, imaginaryPart);
            
            // verify x*1 = x
            Complex multResult = complexRandom * Complex.One;
            Assert.True(multResult == complexRandom, string.Format("Mult with 1 does not equal to complex:{0} itself: {1}", complexRandom, multResult));

            // verify x/1 = x
            Complex divisorOneResult = complexRandom / Complex.One;
            Assert.True(divisorOneResult == complexRandom, string.Format("Division by 1 does not equal to complex:{0} itself: {1}", complexRandom, divisorOneResult));

            // verify Abs(1) = 1
            double absResult = Complex.Abs(Complex.One);
            Assert.True(1.0 == absResult, string.Format("Abs(One) does not equal to 1: {0}", absResult));

            // verify Conjugate(1) = 1
            Complex conjugateResult = Complex.Conjugate(Complex.One);
            Assert.True(Complex.One == conjugateResult, string.Format("Conjugate(One) does not equal to One: {0}", conjugateResult));

            // verify Reciprocal(1) = 1
            Complex reciprocalResult = Complex.Reciprocal(Complex.One);
            Assert.True(Complex.One == reciprocalResult, string.Format("Reciprocal(One) does not equal to One: {0}", reciprocalResult));
        }

        [Fact]
        public static void RunTests_ImaginaryOne()
        {
            // Verify real part is 0.0, and imaginary part is 1.0
            Support.VerifyRealImaginaryProperties(Complex.ImaginaryOne, 0.0, 1.0, "Verify real part is 0.0, and imaginary part is 1.0");

            // verify magnitude is 1.0 and phase is Math.PI/2.
            Support.VerifyMagnitudePhaseProperties(Complex.ImaginaryOne, 1.0, (Double)(Math.PI / 2), "Verify magnitude is 1.0 and phase is Math.PI/2.");

            // verify ImaginaryOne * ImaginaryOne = -1
            Complex multResultImgOnes = Complex.ImaginaryOne * Complex.ImaginaryOne;
            Assert.True(-1 == multResultImgOnes, string.Format("Multiplication of ImaginaryOnes is not -1", multResultImgOnes));

            // verify ImaginaryOne / ImaginaryOne = 1
            Complex divResultImgOnes = Complex.ImaginaryOne / Complex.ImaginaryOne;
            Assert.True(1 == divResultImgOnes, string.Format("Division of ImaginaryOnes is not 1", divResultImgOnes));

            // verify ImaginaryOne / ImaginaryOne = 1
            double absResult = Complex.Abs(Complex.ImaginaryOne);
            Assert.True(1 == absResult, string.Format("Abs of ImaginaryOne is not 1", absResult));

            // verify Conjugate(1) = 1
            Complex conjugateResult = Complex.Conjugate(Complex.ImaginaryOne);
            Assert.True(-Complex.ImaginaryOne == conjugateResult, string.Format("Conjugate of ImaginaryOne is not 1", conjugateResult));

            // verify Reciprocal(1) = 1
            Complex reciprocalResult = Complex.Reciprocal(Complex.ImaginaryOne);
            Assert.True(-Complex.ImaginaryOne == reciprocalResult, string.Format("Reciprocal of ImaginaryOne is not 1", reciprocalResult));

            // create a complex number with random real and imaginary parts
            double realPart = Support.GetRandomDoubleValue(false);
            double imaginaryPart = Support.GetRandomDoubleValue(false);
            Complex complexRandom = new Complex(realPart, imaginaryPart);

            // verify x*i = Complex(-x.Imaginary, x.Real)
            Complex multResult = complexRandom * Complex.ImaginaryOne;
            Support.VerifyRealImaginaryProperties(multResult, -complexRandom.Imaginary, complexRandom.Real, 
                string.Format("Mult with i does not equal to ({0}, {1}): {2}", -complexRandom.Imaginary, complexRandom.Real, multResult));

            // verify x/i = Complex(x.Imaginary, -x.Real)
            Complex divResult = complexRandom / Complex.ImaginaryOne;
            Support.VerifyRealImaginaryProperties(divResult, complexRandom.Imaginary, -complexRandom.Real, 
                string.Format("Div by i does not equal to ({0}, {1}): {2}", complexRandom.Imaginary, -complexRandom.Real, divResult));
        }
    }
}
