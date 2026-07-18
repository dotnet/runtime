// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines an IEEE 754 decimal floating-point type.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface IDecimalFloatingPointIeee754<TSelf>
        : IFloatingPointIeee754<TSelf>
        where TSelf : IDecimalFloatingPointIeee754<TSelf>?
    {
        /// <summary>Adjusts a value to the quantum (exponent) of another value, rounding to nearest with ties to even.</summary>
        /// <param name="x">The value whose quantum is adjusted.</param>
        /// <param name="y">The value that provides the target quantum.</param>
        /// <returns><paramref name="x" /> expressed with the quantum of <paramref name="y" />, or NaN when the value cannot be represented at that quantum.</returns>
        static abstract TSelf Quantize(TSelf x, TSelf y);

        /// <summary>Computes the quantum of a value: one unit in the last place sharing its exponent.</summary>
        /// <param name="x">The value whose quantum is returned.</param>
        /// <returns>The quantum of <paramref name="x" />.</returns>
        static abstract TSelf Quantum(TSelf x);

        /// <summary>Determines whether two values have the same quantum (exponent).</summary>
        /// <param name="x">The first value to compare.</param>
        /// <param name="y">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="x" /> and <paramref name="y" /> have the same quantum; otherwise, <c>false</c>.</returns>
        static abstract bool SameQuantum(TSelf x, TSelf y);
    }
}
