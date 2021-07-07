// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

#if !FEATURE_GENERIC_MATH
#error FEATURE_GENERIC_MATH is not defined
#endif

namespace System
{
    /// <summary>Defines a mechanism for computing the quotient of two values.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="TOther">The type that will divide <typeparamref name="TSelf" />.</typeparam>
    /// <typeparam name="TResult">The type that contains the quotient of <typeparamref name="TSelf" /> and <typeparamref name="TOther" />.</typeparam>
    [RequiresPreviewFeatures]
    public interface IDivisionOperators<TSelf, TOther, TResult>
        where TSelf : IDivisionOperators<TSelf, TOther, TResult>
    {
        /// <summary>Divides two values together to compute their quotient.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        static abstract TResult operator /(TSelf left, TOther right);

        // /// <summary>Divides two values together to compute their quotient.</summary>
        // /// <param name="left">The value which <paramref name="right" /> divides.</param>
        // /// <param name="right">The value which divides <paramref name="left" />.</param>
        // /// <returns>The quotient of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        // /// <exception cref="OverflowException">The quotient of <paramref name="left" /> divided-by <paramref name="right" /> is not representable by <typeparamref name="TResult" />.</exception>
        // static abstract checked TResult operator /(TSelf left, TOther right);
    }
}
