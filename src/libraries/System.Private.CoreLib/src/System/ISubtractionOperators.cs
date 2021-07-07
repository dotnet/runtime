// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

#if !FEATURE_GENERIC_MATH
#error FEATURE_GENERIC_MATH is not defined
#endif

namespace System
{
    /// <summary>Defines a mechanism for computing the difference of two values.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="TOther">The type that will be subtracted from <typeparamref name="TSelf" />.</typeparam>
    /// <typeparam name="TResult">The type that contains the difference of <typeparamref name="TOther" /> subtracted from <typeparamref name="TSelf" />.</typeparam>
    [RequiresPreviewFeatures]
    public interface ISubtractionOperators<TSelf, TOther, TResult>
        where TSelf : ISubtractionOperators<TSelf, TOther, TResult>
    {
        /// <summary>Subtracts two values to compute their difference.</summary>
        /// <param name="left">The value from which <paramref name="right" /> is subtracted.</param>
        /// <param name="right">The value which is subtracted from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="right" /> subtracted from <paramref name="left" />.</returns>
        static abstract TResult operator -(TSelf left, TOther right);

        // /// <summary>Subtracts two values to compute their difference.</summary>
        // /// <param name="left">The value from which <paramref name="right" /> is subtracted.</param>
        // /// <param name="right">The value which is subtracted from <paramref name="left" />.</param>
        // /// <returns>The difference of <paramref name="right" /> subtracted from <paramref name="left" />.</returns>
        // /// <exception cref="OverflowException">The difference of <paramref name="right" /> subtracted from <paramref name="left" /> is not representable by <typeparamref name="TResult" />.</exception>
        // static abstract checked TResult operator -(TSelf left, TOther right);
    }
}
