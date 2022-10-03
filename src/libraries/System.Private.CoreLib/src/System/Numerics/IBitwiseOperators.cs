// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines a mechanism for performing bitwise operations over two values.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="TOther">The type that will is used in the operation with <typeparamref name="TSelf" />.</typeparam>
    /// <typeparam name="TResult">The type that contains the result of <typeparamref name="TSelf" /> op <typeparamref name="TOther" />.</typeparam>
    public interface IBitwiseOperators<TSelf, TOther, TResult>
        where TSelf : IBitwiseOperators<TSelf, TOther, TResult>
    {
        /// <summary>Computes the bitwise-and of two values.</summary>
        /// <param name="left">The value to and with <paramref name="right" />.</param>
        /// <param name="right">The value to and with <paramref name="left" />.</param>
        /// <returns>The bitwise-and of <paramref name="left" /> and <paramref name="right" />.</returns>
        static abstract TResult operator &(TSelf left, TOther right);

        /// <summary>Computes the bitwise-or of two values.</summary>
        /// <param name="left">The value to or with <paramref name="right" />.</param>
        /// <param name="right">The value to or with <paramref name="left" />.</param>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        static abstract TResult operator |(TSelf left, TOther right);

        /// <summary>Computes the exclusive-or of two values.</summary>
        /// <param name="left">The value to xor with <paramref name="right" />.</param>
        /// <param name="right">The value to xorwith <paramref name="left" />.</param>
        /// <returns>The exclusive-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        static abstract TResult operator ^(TSelf left, TOther right);

        /// <summary>Computes the ones-complement representation of a given value.</summary>
        /// <param name="value">The value for which to compute its ones-complement.</param>
        /// <returns>The ones-complement of <paramref name="value" />.</returns>
        static abstract TResult operator ~(TSelf value);
    }
}
