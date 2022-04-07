// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines support for logarithmic functions.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface ILogarithmicFunctions<TSelf>
        where TSelf : ILogarithmicFunctions<TSelf>
    {
        /// <summary>Computes the natural (<c>base-E</c> logarithm of a value.</summary>
        /// <param name="x">The value whose natural logarithm is to be computed.</param>
        /// <returns>The natural logarithm of <paramref name="x" />.</returns>
        static abstract TSelf Log(TSelf x);

        /// <summary>Computes the logarithm of a value in the specified base.</summary>
        /// <param name="x">The value whose logarithm is to be computed.</param>
        /// <param name="newBase">The base in which the logarithm is to be computed.</param>
        /// <returns>The base-<paramref name="newBase" /> logarithm of <paramref name="x" />.</returns>
        static abstract TSelf Log(TSelf x, TSelf newBase);

        /// <summary>Computes the base-2 logarithm of a value.</summary>
        /// <param name="x">The value whose base-2 logarithm is to be computed.</param>
        /// <returns>The base-2 logarithm of <paramref name="x" />.</returns>
        static abstract TSelf Log2(TSelf x);

        /// <summary>Computes the base-10 logarithm of a value.</summary>
        /// <param name="x">The value whose base-10 logarithm is to be computed.</param>
        /// <returns>The base-10 logarithm of <paramref name="x" />.</returns>
        static abstract TSelf Log10(TSelf x);

        // The following methods are approved but not yet implemented in the libraries
        // * static abstract TSelf LogP1(TSelf x);
        // * static abstract TSelf Log2P1(TSelf x);
        // * static abstract TSelf Log10P1(TSelf x);
    }
}
