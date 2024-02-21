// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections
{
    internal static class ThrowHelper
    {
        public static void ThrowIfNull(object arg, [CallerArgumentExpression(nameof(arg))] string? paramName = null)
        {
            if (arg is null)
            {
                ThrowArgumentNullException(paramName);
            }
        }

        [DoesNotReturn]
        public static void ThrowIfDestinationTooSmall() =>
            throw new ArgumentException(SR.CapacityMustBeGreaterThanOrEqualToCount, "destination");

        [DoesNotReturn]
        public static void ThrowArgumentNullException(string? paramName) =>
            throw new ArgumentNullException(paramName);

        [DoesNotReturn]
        public static void ThrowKeyNotFoundException<TKey>(TKey key) =>
            throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key));

        [DoesNotReturn]
        public static void ThrowInvalidOperationException() =>
            throw new InvalidOperationException();
    }
}
