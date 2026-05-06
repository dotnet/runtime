// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Algorithms
{
    // A float implementation of the BCL Complex type that only
    // contains the bare essentials, plus a couple operations needed
    // for efficient Mandelbrot calcuation.
    internal struct ComplexFloat
    {
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public ComplexFloat(float real, float imaginary)
        {
            Real = real; Imaginary = imaginary;
        }

        public float Real;
        public float Imaginary;

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public ComplexFloat square()
        {
            return new ComplexFloat(Real * Real - Imaginary * Imaginary, 2.0f * Real * Imaginary);
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public float sqabs()
        {
            return Real * Real + Imaginary * Imaginary;
        }

        public override string ToString()
        {
            return String.Format("[{0} + {1}Imaginary]", Real, Imaginary);
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static ComplexFloat operator +(ComplexFloat a, ComplexFloat b)
        {
            return new ComplexFloat(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }
    }

    // A couple extension methods that operate on BCL Complex types to help efficiently calculate
    // the Mandelbrot set (They're instance methods on the ComplexFloat custom type)
    public static partial class extensions
    {
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static double sqabs(this Complex val)
        {
            return val.Real * val.Real + val.Imaginary * val.Imaginary;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Complex square(this Complex val)
        {
            return new Complex(val.Real * val.Real - val.Imaginary * val.Imaginary, 2.0 * val.Real * val.Imaginary);
        }
    }

    // This is an implementation of ComplexFloat that operates on Vector<float> at a time SIMD types
    internal struct ComplexVecFloat
    {
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public ComplexVecFloat(Vector<float> real, Vector<float> imaginary)
        {
            Real = real; Imaginary = imaginary;
        }

        public Vector<float> Real;
        public Vector<float> Imaginary;

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public ComplexVecFloat square()
        {
            return new ComplexVecFloat(Real * Real - Imaginary * Imaginary, Real * Imaginary + Real * Imaginary);
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public Vector<float> sqabs()
        {
            return Real * Real + Imaginary * Imaginary;
        }

        public override string ToString()
        {
            return String.Format("[{0} + {1}Imaginary]", Real, Imaginary);
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static ComplexVecFloat operator +(ComplexVecFloat a, ComplexVecFloat b)
        {
            return new ComplexVecFloat(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }
    }

    // This is an implementation of Complex that operates on Vector<double> at a time SIMD types
    internal struct ComplexVecDouble
    {
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public ComplexVecDouble(Vector<double> real, Vector<double> imaginary)
        {
            Real = real; Imaginary = imaginary;
        }

        public Vector<double> Real;
        public Vector<double> Imaginary;

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public ComplexVecDouble square()
        {
            return new ComplexVecDouble(Real * Real - Imaginary * Imaginary, Real * Imaginary + Real * Imaginary);
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public Vector<double> sqabs()
        {
            return Real * Real + Imaginary * Imaginary;
        }

        public override string ToString()
        {
            return String.Format("[{0} + {1}Imaginary]", Real, Imaginary);
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static ComplexVecDouble operator +(ComplexVecDouble a, ComplexVecDouble b)
        {
            return new ComplexVecDouble(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }
    }
}
