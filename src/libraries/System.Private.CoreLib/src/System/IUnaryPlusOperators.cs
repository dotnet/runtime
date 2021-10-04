// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

#if !FEATURE_GENERIC_MATH
#error FEATURE_GENERIC_MATH is not defined
#endif

namespace System
{
    /// <summary>Defines a mechanism for computing the unary plus of a value.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="TResult">The type that contains the result of negating <typeparamref name="TSelf" />.</typeparam>
    [RequiresPreviewFeatures]
    public interface IUnaryPlusOperators<TSelf, TResult>
        where TSelf : IUnaryPlusOperators<TSelf, TResult>
    {
        /// <summary>Computes the unary plus of a value.</summary>
        /// <param name="value">The value for which to compute its unary plus.</param>
        /// <returns>The unary plus of <paramref name="value" />.</returns>
        static abstract TResult operator +(TSelf value);

        // /// <summary>Computes the unary plus of a value.</summary>
        // /// <param name="value">The value for which to compute its unary plus.</param>
        // /// <returns>The unary plus of <paramref name="value" />.</returns>
        // /// <exception cref="OverflowException">The unary plus of <paramref name="value" /> is not representable by <typeparamref name="TResult" />.</exception>
        // static abstract checked TResult operator +(TSelf value);
    }
}
