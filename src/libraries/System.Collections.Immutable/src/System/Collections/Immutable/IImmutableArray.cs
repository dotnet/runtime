// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Collections.Immutable
{
    /// <summary>
    /// An internal non-generic interface implemented by ImmutableArray{T}
    /// that allows for recognition of an ImmutableArray{T} instance and access
    /// to its underlying array, without actually knowing the type of value
    /// stored in it.
    /// </summary>
    /// <remarks>
    /// Casting to this interface requires a boxed instance of the ImmutableArray{T} struct,
    /// and as such should be avoided. This interface is useful, however, where the value
    /// is already boxed and we want to try to reuse immutable arrays instead of copying them.
    /// ** This interface is INTENTIONALLY INTERNAL, as it gives access to the inner array.  **
    /// </remarks>
    internal interface IImmutableArray
    {
        /// <summary>
        /// Gets an untyped reference to the array.
        /// </summary>
        Array Array { get; }

        void ThrowInvalidOperationIfNotInitialized();
    }
}
