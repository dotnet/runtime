// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class staticConstantsTest
    {
        [Fact]
        public static void RunTests_Zero()
        {
            // Verify it is zero.
            if (0 != Complex.Zero)
            {
                Console.WriteLine("ErRoR ErrZero_3426! Zero does not equal to (0.0, 0.0)");
                Assert.True(false, "Verification Failed");
            }

            // Verify real and imaginary parts are 0.0

            if (false == Support.VerifyRealImaginaryProperties(Complex.Zero, 0.0, 0.0))
            {
                Console.WriteLine("ErRoR ErrZero_3429! Verify real and imaginary parts are 0.0");
                Assert.True(false, "Verification Failed");
            }

            // verify magnitude is 0.0 and phase is NaN.

            if (false == Support.VerifyMagnitudePhaseProperties(Complex.Zero, 0.0, Double.NaN))
            {
                Console.WriteLine("ErRoR ErrZero_3432! Verify magnitude is 0.0 and phase is NaN");
                Assert.True(false, "Verification Failed");
            }

            // create a complex number with random real and imaginary parts
            Double realPart = Support.GetRandomDoubleValue(false);
            Double imaginaryPart = Support.GetRandomDoubleValue(false);
            Complex complexRandom = new Complex(realPart, imaginaryPart);

            // verify x*0 = 0

            Complex multResult = complexRandom * Complex.Zero;
            if (0 != multResult)
            {
                Console.WriteLine("ErRoR ErrZero_5784! Multiplication with Zero does not equal to 0: {0}", multResult);
                Assert.True(false, "Verification Failed");
            }

            // verify x/0 = Infinity

            Complex divisorZeroResult = complexRandom / Complex.Zero;
            if (false == Support.VerifyRealImaginaryProperties(divisorZeroResult, Double.NaN, Double.NaN))
            {
                Console.WriteLine("ErRoR ErrZero_5787! Division with Zero does not equal to (NaN, NaN): {0}", divisorZeroResult);
                Assert.True(false, "Verification Failed");
            }

            // verify 0/x = 0

            Complex divZeroResult = Complex.Zero / complexRandom;
            if (0 != divZeroResult)
            {
                Console.WriteLine("ErRoR ErrZero_5791! Division of Zero does not equal to 0: {0}", divZeroResult);
                Assert.True(false, "Verification Failed");
            }

            // verify x - 0 = 0

            Complex subZeroResult = complexRandom - Complex.Zero;
            if (complexRandom != subZeroResult)
            {
                Console.WriteLine("ErRoR ErrZero_5794! Zero sub does not equal to complex {0} itself: {1}", complexRandom, subZeroResult);
                Assert.True(false, "Verification Failed");
            }

            // verify 0 - x = -x

            Complex subComplexResult = Complex.Zero - complexRandom;
            if (subComplexResult != -complexRandom)
            {
                Console.WriteLine("ErRoR ErrZero_5797! Sub from Zero does not equal to neg complex {0}: {1}", complexRandom, subComplexResult);
                Assert.True(false, "Verification Failed");
            }

            // verify x + 0 = x

            Complex addZeroResult = complexRandom + Complex.Zero;
            if (addZeroResult != complexRandom)
            {
                Console.WriteLine("ErRoR ErrZero_5701! Add Zero result does not equal to complex {0}: {1}", complexRandom, addZeroResult);
                Assert.True(false, "Verification Failed");
            }

            // verify 0 + x = x

            Complex addComplexResult = Complex.Zero + complexRandom;
            if (addComplexResult != complexRandom)
            {
                Console.WriteLine("ErRoR ErrZero_5701! Add to Zero result does not equal to complex {0}: {1}", complexRandom, addComplexResult);
                Assert.True(false, "Verification Failed");
            }

            // verify Abs(0) = 0

            Double absResult = Complex.Abs(Complex.Zero);
            if (0.0 != absResult)
            {
                Console.WriteLine("ErRoR ErrZero_3887! Abs(Zero) does NoT equal to 0: {0}", absResult);
                Assert.True(false, "Verification Failed");
            }

            // verify Conjugate(0) = 0

            Complex conjugateResult = Complex.Conjugate(Complex.Zero);
            if (0 != conjugateResult)
            {
                Console.WriteLine("ErRoR ErrZero_3891! Conjugate(Zero) does NoT equal to 0: {0}", conjugateResult);
                Assert.True(false, "Verification Failed");
            }

            // verify Reciprocal(0) goes to 0

            Complex reciprocalResult = Complex.Reciprocal(Complex.Zero);
            if (reciprocalResult != Complex.Zero)
            {
                Console.WriteLine("ErRoR ErrZero_3893! Reciprocal(Zero) does NoT go to Infinity: {0}", reciprocalResult);
                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_One()
        {
            // Verify real part is 1.0, and imaginary part is 0.0

            if (false == Support.VerifyRealImaginaryProperties(Complex.One, 1.0, 0.0))
            {
                Console.WriteLine("ErRoR ErrOne_2934! Verify real part is 1.0, and imaginary part is 0.0");
                Assert.True(false, "Verification Failed");
            }

            // verify magnitude is 1.0 and phase is 0.0.

            if (false == Support.VerifyMagnitudePhaseProperties(Complex.One, 1.0, 0.0))
            {
                Console.WriteLine("ErRoR ErrZero_2937!");
                Assert.True(false, "Verification Failed");
            }

            // create a complex number with random real and imaginary parts
            Double realPart = Support.GetRandomDoubleValue(false);
            Double imaginaryPart = Support.GetRandomDoubleValue(false);
            Complex complexRandom = new Complex(realPart, imaginaryPart);

            // verify x*1 = x

            Complex multResult = complexRandom * Complex.One;
            if (multResult != complexRandom)
            {
                Console.WriteLine("ErRoR ErrOne_3135! Mult with 1 does not equal to complex:{0} itself: {1}", complexRandom, multResult);
                Assert.True(false, "Verification Failed");
            }

            // verify x/1 = x

            Complex divisorOneResult = complexRandom / Complex.One;
            if (divisorOneResult != complexRandom)
            {
                Console.WriteLine("ErRoR ErrOne_3139! Division by 1 does not equal to complex:{0} itself: {1}", complexRandom, divisorOneResult);
                Assert.True(false, "Verification Failed");
            }

            // verify Abs(1) = 1

            Double absResult = Complex.Abs(Complex.One);
            if (1.0 != absResult)
            {
                Console.WriteLine("ErRoR ErrOne_4356! Abs(One) does NoT equal to 1: {0}", absResult);
                Assert.True(false, "Verification Failed");
            }

            // verify Conjugate(1) = 1

            Complex conjugateResult = Complex.Conjugate(Complex.One);
            if (Complex.One != conjugateResult)
            {
                Console.WriteLine("ErRoR ErrZero_4359! Conjugate(One) does NoT equal to One: {0}", conjugateResult);
                Assert.True(false, "Verification Failed");
            }

            // verify Reciprocal(1) = 1

            Complex reciprocalResult = Complex.Reciprocal(Complex.One);
            if (Complex.One != reciprocalResult)
            {
                Console.WriteLine("ErRoR ErrZero_4365! Reciprocal(One) does NoT equal to One: {0}", reciprocalResult);
                Assert.True(false, "Verification Failed");
            }
        }

        [Fact]
        public static void RunTests_ImaginaryOne()
        {
            // Verify real part is 0.0, and imaginary part is 1.0

            if (false == Support.VerifyRealImaginaryProperties(Complex.ImaginaryOne, 0.0, 1.0))
            {
                Console.WriteLine("ErRoR ErrImaginaryOne_2563-1! Verify real part is 0.0, and imaginary part is 1.0");
                Assert.True(false, "Verification Failed");
            }

            // verify magnitude is 1.0 and phase is Math.PI/2.

            if (false == Support.VerifyMagnitudePhaseProperties(Complex.ImaginaryOne, 1.0, (Double)(Math.PI / 2)))
            {
                Console.WriteLine("ErRoR ErrImaginaryOne_2567-1! verify magnitude is 1.0 and phase is Math.PI/2.");
                Assert.True(false, "Verification Failed");
            }

            // verify ImaginaryOne * ImaginaryOne = -1

            Complex multResultImgOnes = Complex.ImaginaryOne * Complex.ImaginaryOne;
            if (-1 != multResultImgOnes)
            {
                Console.WriteLine("ErRoR ErrImaginaryOne_2973-1! Multiplication of ImaginaryOnes is not -1", multResultImgOnes);
                Assert.True(false, "Verification Failed");
            }

            // verify ImaginaryOne / ImaginaryOne = 1

            Complex divResultImgOnes = Complex.ImaginaryOne / Complex.ImaginaryOne;
            if (1 != divResultImgOnes)
            {
                Console.WriteLine("ErRoR ErrImaginaryOne_2978-1! Division of ImaginaryOnes is not 1", divResultImgOnes);
                Assert.True(false, "Verification Failed");
            }

            // verify ImaginaryOne / ImaginaryOne = 1

            Double absResult = Complex.Abs(Complex.ImaginaryOne);
            if (1 != absResult)
            {
                Console.WriteLine("ErRoR ErrImaginaryOne_2981-1! Abs of ImaginaryOne is not 1", absResult);
                Assert.True(false, "Verification Failed");
            }

            // verify Conjugate(1) = 1

            Complex conjugateResult = Complex.Conjugate(Complex.ImaginaryOne);
            if (-Complex.ImaginaryOne != conjugateResult)
            {
                Console.WriteLine("ErRoR ErrImaginaryOne_2983-1! Conjugate of ImaginaryOne is not 1", conjugateResult);
                Assert.True(false, "Verification Failed");
            }

            // verify Reciprocal(1) = 1

            Complex reciprocalResult = Complex.Reciprocal(Complex.ImaginaryOne);
            if (-Complex.ImaginaryOne != reciprocalResult)
            {
                Console.WriteLine("ErRoR ErrImaginaryOne_2986-1! Reciprocal of ImaginaryOne is not 1", reciprocalResult);
                Assert.True(false, "Verification Failed");
            }

            // create a complex number with random real and imaginary parts
            Double realPart = Support.GetRandomDoubleValue(false);
            Double imaginaryPart = Support.GetRandomDoubleValue(false);
            Complex complexRandom = new Complex(realPart, imaginaryPart);

            // verify x*i = Complex(-x.Imaginary, x.Real)

            Complex multResult = complexRandom * Complex.ImaginaryOne;
            if (false == Support.VerifyRealImaginaryProperties(multResult, -complexRandom.Imaginary, complexRandom.Real))
            {
                Console.WriteLine("FAiL! ErRoR ErrImaginaryOne_2718-1! Mult with i does not equal to ({0}, {1}): {2}", -complexRandom.Imaginary, complexRandom.Real, multResult);
                Assert.True(false, "Verification Failed");
            }

            // verify x/i = Complex(x.Imaginary, -x.Real)

            Complex divResult = complexRandom / Complex.ImaginaryOne;
            if (false == Support.VerifyRealImaginaryProperties(divResult, complexRandom.Imaginary, -complexRandom.Real))
            {
                Console.WriteLine("FAiL! ErRoR ErrImaginaryOne_2738-1! Div by i does not equal to ({0}, {1}): {2}", complexRandom.Imaginary, -complexRandom.Real, divResult);
                Assert.True(false, "Verification Failed");
            }
        }
    }
}
