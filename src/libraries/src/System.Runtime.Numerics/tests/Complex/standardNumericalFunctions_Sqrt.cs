// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class standardNumericalFunctions_SqrtTest
    {
        private static void VerifySqrtWithRectangularForm(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Complex complex = new Complex(real, imaginary);
            Complex sqrtComplex = Complex.Sqrt(complex);
            Support.VerifyRealImaginaryProperties(sqrtComplex, expectedReal, expectedImaginary, 
                string.Format("Sqrt({0}):{1} != ({2},{3})", complex, sqrtComplex, expectedReal, expectedImaginary));
        }

        private static void VerifySqrtWithRectangularForm(double real, double imaginary)
        {
            // sqrt(a+bi) = +- (sqrt(r + a) + i sqrt(r - a) sign(b)) sqrt(2) / 2, unless a=-r and y = 0
            Complex complex = new Complex(real, imaginary);

            double expectedReal = 0.0;
            double expectedImaginary = 0.0;

            if (0 == imaginary)
            {
                if (real == -complex.Magnitude)
                    expectedImaginary = Math.Sqrt(-real);
                else
                    expectedReal = Math.Sqrt(real);
            }
            else
            {
                double scale = 1 / Math.Sqrt(2);
                expectedReal = scale * Math.Sqrt(complex.Magnitude + complex.Real);
                expectedImaginary = scale * Math.Sqrt(complex.Magnitude - complex.Real);
                if (complex.Imaginary < 0)
                {
                    expectedImaginary = -expectedImaginary;
                }
            }
            VerifySqrtWithRectangularForm(real, imaginary, expectedReal, expectedImaginary);
        }

        [Fact]
        public static void RunTests_ZeroOneImaginaryOne()
        {
            // verify sqrt with Zero
            VerifySqrtWithRectangularForm(0.0, 0.0, 0.0, 0.0);

            // Verify test results with One
            VerifySqrtWithRectangularForm(1.0, 0.0, 1.0, 0.0);

            // Verify test results with ImaginaryOne
            VerifySqrtWithRectangularForm(0.0, 1.0, 0.707106781186547, 0.707106781186547);

            // Verify test results with MinusImaginaryOne
            VerifySqrtWithRectangularForm(0.0, -1.0, 0.707106781186547, -0.707106781186547);
        }


        [Fact]
        public static void RunTests_BoundaryValues()
        {
            // Verify test results with MaxReal
            VerifySqrtWithRectangularForm(double.MaxValue, 0.0, 1.34078079299426E+154, 0.0);

            // Verify test results with MaxImg
            VerifySqrtWithRectangularForm(0.0, double.MaxValue, 9.48075190810917E+153, 9.48075190810917E+153);

            // Verify test results with MinImaginary
            VerifySqrtWithRectangularForm(0.0, double.MinValue, 9.48075190810917E+153, -9.48075190810917E+153);
        }
    }
}
