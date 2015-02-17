// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class operation_ComparisonHashCodeTest
    {
        private static void VerifyComplexComparison(Complex c1, Complex c2, bool expectedResult, bool expectedResultEqual)
        {
            if (expectedResult != (c1 == c2))
            {
                Console.WriteLine("Error-8592 c1:{0} == c2{1} is not '{2}' as expected", c1, c2, expectedResult);
                Assert.True(false, "Verification Failed");
            }


            if (expectedResult != (c2 == c1))
            {
                Console.WriteLine("Error-8592-1 c2:{0} == c1{1} is not '{2}' as expected", c2, c1, expectedResult);
                Assert.True(false, "Verification Failed");
            }

            if (expectedResult == (c1 != c2))
            {
                Console.WriteLine("Error-81792 c1:{0} != c2{1} is not '{2}' as expected", c1, c2, !expectedResult);
                Assert.True(false, "Verification Failed");
            }

            if (expectedResult == (c2 != c1))
            {
                Console.WriteLine("Error-81792-1 c2:{0} != c1{1} is not '{2}' as expected", c2, c1, !expectedResult);
                Assert.True(false, "Verification Failed");
            }

            bool result = c1.Equals(c2);
            if (expectedResultEqual != result)
            {
                Console.WriteLine("Error-6172 c1:{0}.Equals(c2{1}) is not '{2}' as expected", c1, c2, expectedResultEqual);
                Assert.True(false, "Verification Failed");
            }

            if (result) // then verify Hash Code equality
            {
                if (c1.GetHashCode() != c2.GetHashCode())
                {
                    Console.WriteLine("Error-9HaSh72 c1:{0}.GetHashCode() == c2:{1}.GetHashCode() is 'true' as expected", c1, c2);
                    Assert.True(false, "Verification Failed");
                }
            }

            result = c2.Equals(c1);
            if (expectedResultEqual != result)
            {
                Console.WriteLine("Error-6172-1 c2:{0}.Equals(c1{1}) is not '{2}' as expected", c2, c1, expectedResultEqual);
                Assert.True(false, "Verification Failed");
            }

            if (result) // then verify Hash Code equality
            {
                if (c2.GetHashCode() != c1.GetHashCode())
                {
                    Console.WriteLine("Error-9HaSh72-1 c2:{0}.GetHashCode() == c1:{1}.GetHashCode() is 'true' as expected", c2, c1);
                    Assert.True(false, "Verification Failed");
                }
            }

            if (expectedResult != c2.Equals((Object)c1))
            {
                Console.WriteLine("Error-6172Obj c2:{0}.Equals((object) c1{1}) is not '{2}' as expected", c2, c1, expectedResult);
                Assert.True(false, "Verification Failed");
            }

            if (expectedResult != c1.Equals((Object)c2))
            {
                Console.WriteLine("Error-6172Obj-1 c1:{0}.Equals((object) c2{1}) is not '{2}' as expected", c1, c2, expectedResult);
                Assert.True(false, "Verification Failed");
            }
        }

        private static void VerifyComplexComparison(Complex c1, Complex c2, bool expectedResult)
        {
            VerifyComplexComparison(c1, c2, expectedResult, expectedResult);
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // local variables
            bool expectedResult = false;

            Double real = Support.GetRandomDoubleValue(false);
            Double imaginary = Support.GetRandomDoubleValue(false);
            Complex randomComplex = new Complex(real, imaginary);

            real = Support.GetRandomDoubleValue(true);
            imaginary = Support.GetRandomDoubleValue(true);
            Complex randomComplexNeg = new Complex(real, imaginary);

            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            Complex randomSmallComplex = new Complex(real, imaginary);

            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            Complex randomSmallComplexNeg = new Complex(real, imaginary);

            VerifyComplexComparison(Complex.Zero, Complex.Zero, true);
            VerifyComplexComparison(Complex.Zero, Complex.One, false);
            VerifyComplexComparison(Complex.Zero, -Complex.One, false);
            VerifyComplexComparison(Complex.Zero, Complex.ImaginaryOne, false);
            VerifyComplexComparison(Complex.Zero, -Complex.ImaginaryOne, false);
            VerifyComplexComparison(Complex.Zero, -Complex.ImaginaryOne, false);

            expectedResult = (randomComplex.Real == 0.0 && randomComplex.Imaginary == 0.0);
            VerifyComplexComparison(Complex.Zero, randomComplex, expectedResult);

            expectedResult = (randomComplexNeg.Real == 0.0 && randomComplexNeg.Imaginary == 0.0);
            VerifyComplexComparison(Complex.Zero, randomComplexNeg, expectedResult);

            expectedResult = (randomSmallComplex.Real == 0.0 && randomSmallComplex.Imaginary == 0.0);
            VerifyComplexComparison(Complex.Zero, randomSmallComplex, expectedResult);

            expectedResult = (randomSmallComplexNeg.Real == 0.0 && randomSmallComplexNeg.Imaginary == 0.0);
            VerifyComplexComparison(Complex.Zero, randomSmallComplexNeg, expectedResult);

            VerifyComplexComparison(Complex.One, Complex.One, true);
            VerifyComplexComparison(Complex.One, -Complex.One, false);
            VerifyComplexComparison(Complex.One, Complex.ImaginaryOne, false);
            VerifyComplexComparison(Complex.One, -Complex.ImaginaryOne, false);

            expectedResult = (randomComplex.Real == 1.0 && randomComplex.Imaginary == 0.0);
            VerifyComplexComparison(Complex.One, randomComplex, expectedResult);

            expectedResult = (randomComplexNeg.Real == 1.0 && randomComplexNeg.Imaginary == 0.0);
            VerifyComplexComparison(Complex.One, randomComplexNeg, expectedResult);

            expectedResult = (randomSmallComplex.Real == 1.0 && randomSmallComplex.Imaginary == 0.0);
            VerifyComplexComparison(Complex.One, randomSmallComplex, expectedResult);

            expectedResult = (randomSmallComplexNeg.Real == 1.0 && randomSmallComplexNeg.Imaginary == 0.0);
            VerifyComplexComparison(Complex.One, randomSmallComplexNeg, expectedResult);


            VerifyComplexComparison(-Complex.One, -Complex.One, true);
            VerifyComplexComparison(-Complex.One, Complex.ImaginaryOne, false);
            VerifyComplexComparison(-Complex.One, -Complex.ImaginaryOne, false);

            expectedResult = (randomComplex.Real == -1.0 && randomComplex.Imaginary == 0.0);
            VerifyComplexComparison(-Complex.One, randomComplex, expectedResult);

            expectedResult = (randomComplexNeg.Real == -1.0 && randomComplexNeg.Imaginary == 0.0);
            VerifyComplexComparison(-Complex.One, randomComplexNeg, expectedResult);

            expectedResult = (randomSmallComplex.Real == -1.0 && randomSmallComplex.Imaginary == 0.0);
            VerifyComplexComparison(-Complex.One, randomSmallComplex, expectedResult);

            expectedResult = (randomSmallComplexNeg.Real == -1.0 && randomSmallComplexNeg.Imaginary == 0.0);
            VerifyComplexComparison(-Complex.One, randomSmallComplexNeg, expectedResult);


            VerifyComplexComparison(Complex.ImaginaryOne, Complex.ImaginaryOne, true);
            VerifyComplexComparison(Complex.ImaginaryOne, -Complex.ImaginaryOne, false);

            expectedResult = (randomComplex.Real == 0.0 && randomComplex.Imaginary == 1.0);
            VerifyComplexComparison(Complex.ImaginaryOne, randomComplex, expectedResult);

            expectedResult = (randomComplexNeg.Real == 0.0 && randomComplexNeg.Imaginary == 1.0);
            VerifyComplexComparison(Complex.ImaginaryOne, randomComplexNeg, expectedResult);

            expectedResult = (randomSmallComplex.Real == 0.0 && randomSmallComplex.Imaginary == 1.0);
            VerifyComplexComparison(Complex.ImaginaryOne, randomSmallComplex, expectedResult);

            expectedResult = (randomSmallComplexNeg.Real == 0.0 && randomSmallComplexNeg.Imaginary == 1.0);
            VerifyComplexComparison(Complex.ImaginaryOne, randomSmallComplexNeg, expectedResult);


            VerifyComplexComparison(-Complex.ImaginaryOne, -Complex.ImaginaryOne, true);

            expectedResult = (randomComplex.Real == 0.0 && randomComplex.Imaginary == -1.0);
            VerifyComplexComparison(-Complex.ImaginaryOne, randomComplex, expectedResult);

            expectedResult = (randomComplexNeg.Real == 0.0 && randomComplexNeg.Imaginary == -1.0);
            VerifyComplexComparison(-Complex.ImaginaryOne, randomComplexNeg, expectedResult);

            expectedResult = (randomSmallComplex.Real == 0.0 && randomSmallComplex.Imaginary == -1.0);
            VerifyComplexComparison(-Complex.ImaginaryOne, randomSmallComplex, expectedResult);

            expectedResult = (randomSmallComplexNeg.Real == 0.0 && randomSmallComplexNeg.Imaginary == -1.0);
            VerifyComplexComparison(-Complex.ImaginaryOne, randomSmallComplexNeg, expectedResult);
        }

        [Fact]
        public static void RunTests_MaxMinValues()
        {
            // local variables
            bool expectedResult;

            Double real = Support.GetRandomDoubleValue(false);
            Double imaginary = Support.GetRandomDoubleValue(false);
            Complex randomComplex = new Complex(real, imaginary);

            real = Support.GetRandomDoubleValue(true);
            imaginary = Support.GetRandomDoubleValue(true);
            Complex randomComplexNeg = new Complex(real, imaginary);

            real = Support.GetSmallRandomDoubleValue(false);
            imaginary = Support.GetSmallRandomDoubleValue(false);
            Complex randomSmallComplex = new Complex(real, imaginary);

            real = Support.GetSmallRandomDoubleValue(true);
            imaginary = Support.GetSmallRandomDoubleValue(true);
            Complex randomSmallComplexNeg = new Complex(real, imaginary);

            Complex maxComplex = new Complex(Double.MaxValue, Double.MaxValue);
            Complex minComplex = new Complex(Double.MinValue, Double.MinValue);
            Complex maxReal = new Complex(Double.MaxValue, 0.0);
            Complex minReal = new Complex(Double.MinValue, 0.0);
            Complex maxImaginary = new Complex(0.0, Double.MaxValue);
            Complex minImaginary = new Complex(0.0, Double.MinValue);

            VerifyComplexComparison(maxComplex, maxComplex, true);
            VerifyComplexComparison(maxComplex, minComplex, false);
            VerifyComplexComparison(maxComplex, maxReal, false);
            VerifyComplexComparison(maxComplex, minReal, false);
            VerifyComplexComparison(maxComplex, maxImaginary, false);
            VerifyComplexComparison(maxComplex, minImaginary, false);

            expectedResult = (randomComplex.Real == maxComplex.Real && randomComplex.Imaginary == maxComplex.Imaginary);
            VerifyComplexComparison(maxComplex, randomComplex, expectedResult);

            expectedResult = (randomComplexNeg.Real == maxComplex.Real && randomComplexNeg.Imaginary == maxComplex.Imaginary);
            VerifyComplexComparison(maxComplex, randomComplexNeg, expectedResult);

            expectedResult = (randomSmallComplex.Real == maxComplex.Real && randomSmallComplex.Imaginary == maxComplex.Imaginary);
            VerifyComplexComparison(maxComplex, randomSmallComplex, expectedResult);

            expectedResult = (randomSmallComplexNeg.Real == maxComplex.Real && randomSmallComplexNeg.Imaginary == maxComplex.Imaginary);
            VerifyComplexComparison(maxComplex, randomSmallComplexNeg, expectedResult);


            VerifyComplexComparison(minComplex, minComplex, true);
            VerifyComplexComparison(minComplex, maxReal, false);
            VerifyComplexComparison(minComplex, maxImaginary, false);
            VerifyComplexComparison(minComplex, minImaginary, false);

            expectedResult = (randomComplex.Real == minComplex.Real && randomComplex.Imaginary == minComplex.Imaginary);
            VerifyComplexComparison(minComplex, randomComplex, expectedResult);

            expectedResult = (randomComplexNeg.Real == minComplex.Real && randomComplexNeg.Imaginary == minComplex.Imaginary);
            VerifyComplexComparison(minComplex, randomComplexNeg, expectedResult);

            expectedResult = (randomSmallComplex.Real == minComplex.Real && randomSmallComplex.Imaginary == minComplex.Imaginary);
            VerifyComplexComparison(minComplex, randomSmallComplex, expectedResult);

            expectedResult = (randomSmallComplexNeg.Real == minComplex.Real && randomSmallComplexNeg.Imaginary == minComplex.Imaginary);
            VerifyComplexComparison(minComplex, randomSmallComplexNeg, expectedResult);


            VerifyComplexComparison(maxReal, maxReal, true);
            VerifyComplexComparison(maxReal, minReal, false);
            VerifyComplexComparison(maxReal, maxImaginary, false);
            VerifyComplexComparison(maxReal, minImaginary, false);

            expectedResult = (randomComplex.Real == maxReal.Real && randomComplex.Imaginary == maxReal.Imaginary);
            VerifyComplexComparison(maxReal, randomComplex, expectedResult);

            expectedResult = (randomComplexNeg.Real == maxReal.Real && randomComplexNeg.Imaginary == maxReal.Imaginary);
            VerifyComplexComparison(maxReal, randomComplexNeg, expectedResult);

            expectedResult = (randomSmallComplex.Real == maxReal.Real && randomSmallComplex.Imaginary == maxReal.Imaginary);
            VerifyComplexComparison(maxReal, randomSmallComplex, expectedResult);

            expectedResult = (randomSmallComplexNeg.Real == maxReal.Real && randomSmallComplexNeg.Imaginary == maxReal.Imaginary);
            VerifyComplexComparison(maxReal, randomSmallComplexNeg, expectedResult);


            VerifyComplexComparison(minReal, minReal, true);
            VerifyComplexComparison(minReal, maxImaginary, false);
            VerifyComplexComparison(minReal, minImaginary, false);

            expectedResult = (randomComplex.Real == minReal.Real && randomComplex.Imaginary == minReal.Imaginary);
            VerifyComplexComparison(minReal, randomComplex, expectedResult);

            expectedResult = (randomComplexNeg.Real == minReal.Real && randomComplexNeg.Imaginary == minReal.Imaginary);
            VerifyComplexComparison(minReal, randomComplexNeg, expectedResult);

            expectedResult = (randomSmallComplex.Real == minReal.Real && randomSmallComplex.Imaginary == minReal.Imaginary);
            VerifyComplexComparison(minReal, randomSmallComplex, expectedResult);

            expectedResult = (randomSmallComplexNeg.Real == minReal.Real && randomSmallComplexNeg.Imaginary == minReal.Imaginary);
            VerifyComplexComparison(minReal, randomSmallComplexNeg, expectedResult);


            VerifyComplexComparison(maxImaginary, maxImaginary, true);
            VerifyComplexComparison(maxImaginary, minImaginary, false);

            expectedResult = (randomComplex.Real == maxImaginary.Real && randomComplex.Imaginary == maxImaginary.Imaginary);
            VerifyComplexComparison(maxImaginary, randomComplex, expectedResult);

            expectedResult = (randomComplexNeg.Real == maxImaginary.Real && randomComplexNeg.Imaginary == maxImaginary.Imaginary);
            VerifyComplexComparison(maxImaginary, randomComplexNeg, expectedResult);

            expectedResult = (randomSmallComplex.Real == maxImaginary.Real && randomSmallComplex.Imaginary == maxImaginary.Imaginary);
            VerifyComplexComparison(maxImaginary, randomSmallComplex, expectedResult);

            expectedResult = (randomSmallComplexNeg.Real == maxImaginary.Real && randomSmallComplexNeg.Imaginary == maxImaginary.Imaginary);
            VerifyComplexComparison(maxImaginary, randomSmallComplexNeg, expectedResult);


            VerifyComplexComparison(minImaginary, minImaginary, true);

            expectedResult = (randomComplex.Real == minImaginary.Real && randomComplex.Imaginary == minImaginary.Imaginary);
            VerifyComplexComparison(minImaginary, randomComplex, expectedResult);

            expectedResult = (randomComplexNeg.Real == minImaginary.Real && randomComplexNeg.Imaginary == minImaginary.Imaginary);
            VerifyComplexComparison(minImaginary, randomComplexNeg, expectedResult);

            expectedResult = (randomSmallComplex.Real == minImaginary.Real && randomSmallComplex.Imaginary == minImaginary.Imaginary);
            VerifyComplexComparison(minImaginary, randomSmallComplex, expectedResult);

            expectedResult = (randomSmallComplexNeg.Real == minImaginary.Real && randomSmallComplexNeg.Imaginary == minImaginary.Imaginary);
            VerifyComplexComparison(minImaginary, randomSmallComplexNeg, expectedResult);
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            Double real = Support.GetRandomDoubleValue(false);
            Double imaginary = Support.GetRandomDoubleValue(false);
            Complex randomComplex = new Complex(real, imaginary);

            foreach (double imaginaryInvalid in Support.doubleInvalidValues)
            {
                real = Support.GetRandomDoubleValue(false);
                Complex randomInvalidComplex = new Complex(real, imaginaryInvalid);
                VerifyComplexComparison(randomInvalidComplex, randomComplex, false);
                VerifyComplexComparison(randomInvalidComplex, randomInvalidComplex, !Double.IsNaN(imaginaryInvalid), true);
            }

            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                imaginary = Support.GetRandomDoubleValue(false);
                Complex randomInvalidComplex = new Complex(realInvalid, imaginary);
                VerifyComplexComparison(randomInvalidComplex, randomComplex, false);
                VerifyComplexComparison(randomInvalidComplex, randomInvalidComplex, !Double.IsNaN(realInvalid), true);
            }

            foreach (double realInvalid in Support.doubleInvalidValues)
            {
                foreach (double imaginaryInvalid in Support.doubleInvalidValues)
                {
                    Complex randomInvalidComplex = new Complex(realInvalid, imaginaryInvalid);
                    VerifyComplexComparison(randomInvalidComplex, randomComplex, false);
                    VerifyComplexComparison(randomInvalidComplex, randomInvalidComplex, !(Double.IsNaN(realInvalid) || Double.IsNaN(imaginaryInvalid)), true);
                }
            }
        }

        [Fact]
        public static void RunTests_WithNonComplexObject()
        {
            // local variables
            Double real = Support.GetSmallRandomDoubleValue(false);
            Complex randomComplex = new Complex(real, 0.0);

            // verify with same double value

            if (randomComplex.Equals((Object)real))
            {
                Console.WriteLine("Error-9674Obj randomComplex:{0}.Equals((object) real) is not 'false' as expected", randomComplex, real);
                Assert.True(false, "Verification Failed");
            }

            // verify with null

            if (randomComplex.Equals((Object)null))
            {
                Console.WriteLine("Error-5441Obj randomComplex:{0}.Equals((object) null) is not 'false' as expected", randomComplex);
                Assert.True(false, "Verification Failed");
            }

            // verify with 0

            if (randomComplex.Equals((Object)0))
            {
                Console.WriteLine("Error-5441Obj randomComplex:{0}.Equals((object) 0) is not 'false' as expected", randomComplex);
                Assert.True(false, "Verification Failed");
            }

            // verify with string

            if (randomComplex.Equals((Object)"0"))
            {
                Console.WriteLine("Error-5441Obj randomComplex:{0}.Equals((object) \"0\") is not 'false' as expected", randomComplex);
                Assert.True(false, "Verification Failed");
            }
        }
    }
}
