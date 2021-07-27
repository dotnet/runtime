// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

#if !FEATURE_GENERIC_MATH
#error FEATURE_GENERIC_MATH is not defined
#endif

namespace System
{
    /// <summary>Defines a mechanism for decrementing a given value.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    [RequiresPreviewFeatures]
    public interface IDecrementOperators<TSelf>
        where TSelf : IDecrementOperators<TSelf>
    {
        /// <summary>Decrements a value.</summary>
        /// <param name="value">The value to decrement.</param>
        /// <returns>The result of decrementing <paramref name="value" />.</returns>
        static abstract TSelf operator --(TSelf value);

        // /// <summary>Decrements a value.</summary>
        // /// <param name="value">The value to decrement.</param>
        // /// <returns>The result of decrementing <paramref name="value" />.</returns>
        // /// <exception cref="OverflowException">The result of decrementing <paramref name="value" /> is not representable by <typeparamref name="TSelf" />.</exception>
        // static abstract checked TSelf operator --(TSelf value);
    }
}
