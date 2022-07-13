// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines support for root functions.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface IRootFunctions<TSelf>
        : IFloatingPointConstants<TSelf>
        where TSelf : IRootFunctions<TSelf>
    {
        /// <summary>Computes the cube-root of a value.</summary>
        /// <param name="x">The value whose cube-root is to be computed.</param>
        /// <returns>The cube-root of <paramref name="x" />.</returns>
        static abstract TSelf Cbrt(TSelf x);

        /// <summary>Computes the hypotenuse given two values representing the lengths of the shorter sides in a right-angled triangle.</summary>
        /// <param name="x">The value to square and add to <paramref name="y" />.</param>
        /// <param name="y">The value to square and add to <paramref name="x" />.</param>
        /// <returns>The square root of <paramref name="x" />-squared plus <paramref name="y" />-squared.</returns>
        static abstract TSelf Hypot(TSelf x, TSelf y);

        /// <summary>Computes the n-th root of a value.</summary>
        /// <param name="x">The value whose <paramref name="n" />-th root is to be computed.</param>
        /// <param name="n">The degree of the root to be computed.</param>
        /// <returns>The <paramref name="n" />-th root of <paramref name="x" />.</returns>
        static abstract TSelf RootN(TSelf x, int n);

        /// <summary>Computes the square-root of a value.</summary>
        /// <param name="x">The value whose square-root is to be computed.</param>
        /// <returns>The square-root of <paramref name="x" />.</returns>
        static abstract TSelf Sqrt(TSelf x);
    }
}
