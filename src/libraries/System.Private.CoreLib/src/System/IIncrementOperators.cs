// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

#if !FEATURE_GENERIC_MATH
#error FEATURE_GENERIC_MATH is not defined
#endif

namespace System
{
    /// <summary>Defines a mechanism for incrementing a given value.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    [RequiresPreviewFeatures]
    public interface IIncrementOperators<TSelf>
        where TSelf : IIncrementOperators<TSelf>
    {
        /// <summary>Increments a value.</summary>
        /// <param name="value">The value to increment.</param>
        /// <returns>The result of incrementing <paramref name="value" />.</returns>
        static abstract TSelf operator ++(TSelf value);

        // /// <summary>Increments a value.</summary>
        // /// <param name="value">The value to increment.</param>
        // /// <returns>The result of incrementing <paramref name="value" />.</returns>
        // /// <exception cref="OverflowException">The result of incrementing <paramref name="value" /> is not representable by <typeparamref name="TSelf" />.</exception>
        // static abstract checked TSelf operator ++(TSelf value);
    }
}
