// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines support for hyperbolic functions.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface IHyperbolicFunctions<TSelf>
        where TSelf : IHyperbolicFunctions<TSelf>
    {
        /// <summary>Computes the hyperbolic arc-cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic arc-cosine is to be computed.</param>
        /// <returns>The hyperbolic arc-cosine of <paramref name="x" />.</returns>
        static abstract TSelf Acosh(TSelf x);

        /// <summary>Computes the hyperbolic arc-sine of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic arc-sine is to be computed.</param>
        /// <returns>The hyperbolic arc-sine of <paramref name="x" />.</returns>
        static abstract TSelf Asinh(TSelf x);

        /// <summary>Computes the hyperbolic arc-tangent of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic arc-tangent is to be computed.</param>
        /// <returns>The hyperbolic arc-tangent of <paramref name="x" />.</returns>
        static abstract TSelf Atanh(TSelf x);

        /// <summary>Computes the hyperbolic cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic cosine is to be computed.</param>
        /// <returns>The hyperbolic cosine of <paramref name="x" />.</returns>
        static abstract TSelf Cosh(TSelf x);

        /// <summary>Computes the hyperbolic sine of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic sine is to be computed.</param>
        /// <returns>The hyperbolic sine of <paramref name="x" />.</returns>
        static abstract TSelf Sinh(TSelf x);

        /// <summary>Computes the hyperbolic tangent of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic tangent is to be computed.</param>
        /// <returns>The hyperbolic tangent of <paramref name="x" />.</returns>
        static abstract TSelf Tanh(TSelf x);
    }
}
