// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

#if !FEATURE_GENERIC_MATH
#error FEATURE_GENERIC_MATH is not defined
#endif

namespace System
{
    /// <summary>Defines a mechanism for getting the minimum and maximum value of a type.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    [RequiresPreviewFeatures]
    public interface IMinMaxValue<TSelf>
        where TSelf : IMinMaxValue<TSelf>
    {
        /// <summary>Gets the minimum value of the current type.</summary>
        static abstract TSelf MinValue { get; }

        /// <summary>Gets the maximum value of the current type.</summary>
        static abstract TSelf MaxValue { get; }
    }
}
