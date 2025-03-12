// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Linq
{
    internal static class ThrowHelper
    {
        internal static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                ThrowArgumentNullException(paramName);
            }

            [DoesNotReturn]
            static void ThrowArgumentNullException(string? paramName) => throw new ArgumentNullException(paramName);
        }

        internal static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value < 0)
            {
                ThrowArgumentOutOfRangeException(paramName!);
            }
        }

        internal static void ThrowIfNegativeOrZero(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value <= 0)
            {
                ThrowArgumentOutOfRangeException(paramName!);
            }
        }

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException(string paramName) => throw new ArgumentOutOfRangeException(paramName);

        [DoesNotReturn]
        internal static void ThrowMoreThanOneElementException() => throw new InvalidOperationException(SR.MoreThanOneElement);

        [DoesNotReturn]
        internal static void ThrowMoreThanOneMatchException() => throw new InvalidOperationException(SR.MoreThanOneMatch);

        [DoesNotReturn]
        internal static void ThrowNoElementsException() => throw new InvalidOperationException(SR.NoElements);

        [DoesNotReturn]
        internal static void ThrowNoMatchException() => throw new InvalidOperationException(SR.NoMatch);
    }
}
