// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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

#if !NET8_0_OR_GREATER
        /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
        /// <param name="argument">The reference type argument to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
        public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                ThrowNull(paramName);
            }
        }

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative.</summary>
        /// <param name="value">The argument to validate as non-negative.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value < 0)
                ThrowNegative(value, paramName);
        }

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than <paramref name="other"/>.</summary>
        /// <param name="value">The argument to validate as less or equal than <paramref name="other"/>.</param>
        /// <param name="other">The value to compare with <paramref name="value"/>.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfGreaterThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) > 0)
                ThrowGreater(value, other, paramName);
        }

        /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is less than <paramref name="other"/>.</summary>
        /// <param name="value">The argument to validate as greatar than or equal than <paramref name="other"/>.</param>
        /// <param name="other">The value to compare with <paramref name="value"/>.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
        public static void ThrowIfLessThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) < 0)
                ThrowLess(value, other, paramName);
        }

        [DoesNotReturn]
        private static void ThrowNull(string? paramName) =>
            throw new ArgumentNullException(paramName);

        [DoesNotReturn]
        private static void ThrowNegative(int value, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeNonNegative, paramName, value));

        [DoesNotReturn]
        private static void ThrowGreater<T>(T value, T other, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeLessOrEqual, paramName, value, other));

        [DoesNotReturn]
        private static void ThrowLess<T>(T value, T other, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeGreaterOrEqual, paramName, value, other));
#endif
    }
}
