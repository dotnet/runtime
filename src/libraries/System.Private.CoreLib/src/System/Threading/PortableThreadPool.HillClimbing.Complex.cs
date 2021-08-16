// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        private sealed partial class HillClimbing
        {
            private struct Complex
            {
                public Complex(double real, double imaginary)
                {
                    Real = real;
                    Imaginary = imaginary;
                }

                public double Imaginary { get; }
                public double Real { get; }

                public static Complex operator *(double scalar, Complex complex) => new Complex(scalar * complex.Real, scalar * complex.Imaginary);

                public static Complex operator *(Complex complex, double scalar) => scalar * complex;

                public static Complex operator /(Complex complex, double scalar) => new Complex(complex.Real / scalar, complex.Imaginary / scalar);

                public static Complex operator -(Complex lhs, Complex rhs) => new Complex(lhs.Real - rhs.Real, lhs.Imaginary - rhs.Imaginary);

                public static Complex operator /(Complex lhs, Complex rhs)
                {
                    double denom = rhs.Real * rhs.Real + rhs.Imaginary * rhs.Imaginary;
                    return new Complex((lhs.Real * rhs.Real + lhs.Imaginary * rhs.Imaginary) / denom, (-lhs.Real * rhs.Imaginary + lhs.Imaginary * rhs.Real) / denom);
                }

                public double Abs() => Math.Sqrt(Real * Real + Imaginary * Imaginary);
            }
        }
    }
}
