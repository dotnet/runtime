// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections
{
    internal static class ThrowHelper
    {
        /// <summary>Throws an exception for a key not being found in the dictionary.</summary>
        [DoesNotReturn]
        internal static void ThrowKeyNotFound<TKey>(TKey key) =>
            throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key));

        /// <summary>Throws an exception for trying to insert a duplicate key into the dictionary.</summary>
        [DoesNotReturn]
        internal static void ThrowDuplicateKey<TKey>(TKey key) =>
            throw new ArgumentException(SR.Format(SR.Argument_AddingDuplicate, key), nameof(key));

        /// <summary>Throws an exception when erroneous concurrent use of a collection is detected.</summary>
        [DoesNotReturn]
        internal static void ThrowConcurrentOperation() =>
            throw new InvalidOperationException(SR.InvalidOperation_ConcurrentOperationsNotSupported);

        /// <summary>Throws an exception for an index being out of range.</summary>
        [DoesNotReturn]
        internal static void ThrowIndexArgumentOutOfRange() =>
            throw new ArgumentOutOfRangeException("index");

        /// <summary>Throws an exception for a version check failing during enumeration.</summary>
        [DoesNotReturn]
        internal static void ThrowVersionCheckFailed() =>
            throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
    }
}
