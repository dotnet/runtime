// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

#if !FEATURE_GENERIC_MATH
#error FEATURE_GENERIC_MATH is not defined
#endif

namespace System
{
    /// <summary>Defines a mechanism for computing the sum of two values.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="TOther">The type that will be added to <typeparamref name="TSelf" />.</typeparam>
    /// <typeparam name="TResult">The type that contains the sum of <typeparamref name="TSelf" /> and <typeparamref name="TOther" />.</typeparam>
    [RequiresPreviewFeatures]
    public interface IAdditionOperators<TSelf, TOther, TResult>
        where TSelf : IAdditionOperators<TSelf, TOther, TResult>
    {
        /// <summary>Adds two values together to compute their sum.</summary>
        /// <param name="left">The value to which <paramref name="right" /> is added.</param>
        /// <param name="right">The value which is added to <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        static abstract TResult operator +(TSelf left, TOther right);

        // /// <summary>Adds two values together to compute their sum.</summary>
        // /// <param name="left">The value to which <paramref name="right" /> is added.</param>
        // /// <param name="right">The value which is added to <paramref name="left" />.</param>
        // /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        // /// <exception cref="OverflowException">The sum of <paramref name="left" /> and <paramref name="right" /> is not representable by <typeparamref name="TResult" />.</exception>
        // static abstract checked TResult operator +(TSelf left, TOther right);
    }
}
