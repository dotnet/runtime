// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines a mechanism for computing the product of two values.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="TOther">The type that will multiply <typeparamref name="TSelf" />.</typeparam>
    /// <typeparam name="TResult">The type that contains the product of <typeparamref name="TSelf" /> and <typeparamref name="TOther" />.</typeparam>
    public interface IMultiplyOperators<TSelf, TOther, TResult>
        where TSelf : IMultiplyOperators<TSelf, TOther, TResult>
    {
        /// <summary>Multiplies two values together to compute their product.</summary>
        /// <param name="left">The value which <paramref name="right" /> multiplies.</param>
        /// <param name="right">The value which multiplies <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        static abstract TResult operator *(TSelf left, TOther right);

        // /// <summary>Multiplies two values together to compute their product.</summary>
        // /// <param name="left">The value which <paramref name="right" /> multiplies.</param>
        // /// <param name="right">The value which multiplies <paramref name="left" />.</param>
        // /// <returns>The product of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        // /// <exception cref="OverflowException">The product of <paramref name="left" /> multiplied-by <paramref name="right" /> is not representable by <typeparamref name="TResult" />.</exception>
        // static abstract TResult operator checked *(TSelf left, TOther right);
    }
}
