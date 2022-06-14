// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines support for root functions.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface IRootFunctions<TSelf>
        where TSelf : IRootFunctions<TSelf>, INumberBase<TSelf>
    {
        /// <summary>Computes the cube-root of a value.</summary>
        /// <param name="x">The value whose cube-root is to be computed.</param>
        /// <returns>The cube-root of <paramref name="x" />.</returns>
        static abstract TSelf Cbrt(TSelf x);

        /// <summary>Computes the square-root of a value.</summary>
        /// <param name="x">The value whose square-root is to be computed.</param>
        /// <returns>The square-root of <paramref name="x" />.</returns>
        static abstract TSelf Sqrt(TSelf x);

        // The following methods are approved but not yet implemented in the libraries
        // * static abstract TSelf Hypot(TSelf x, TSelf y);
        // * static abstract TSelf Root(TSelf x, TSelf n);
    }
}
