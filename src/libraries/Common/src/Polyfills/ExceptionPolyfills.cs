// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>Provides downlevel polyfills for static methods on Exception-derived types.</summary>
    internal static class ExceptionPolyfills
    {
        extension(ArgumentNullException)
        {
            public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                if (argument is null)
                {
                    ThrowArgumentNullException(paramName);
                }
            }
        }

        extension(ArgumentOutOfRangeException)
        {
            public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            {
                if (value < 0)
                {
                    ThrowArgumentOutOfRangeException(paramName);
                }
            }

            public static void ThrowIfNegative(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            {
                if (value < 0)
                {
                    ThrowArgumentOutOfRangeException(paramName);
                }
            }

            public static void ThrowIfLessThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : IComparable<T>
            {
                if (value.CompareTo(other) < 0)
                {
                    ThrowArgumentOutOfRangeException(paramName);
                }
            }

            public static void ThrowIfLessThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : IComparable<T>
            {
                if (value.CompareTo(other) <= 0)
                {
                    ThrowArgumentOutOfRangeException(paramName);
                }
            }
        }

        [DoesNotReturn]
        private static void ThrowArgumentNullException(string? paramName) =>
            throw new ArgumentNullException(paramName);

        [DoesNotReturn]
        private static void ThrowArgumentOutOfRangeException(string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName);

        extension(ArgumentException)
        {
            public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                if (string.IsNullOrEmpty(argument))
                {
                    ThrowNullOrEmptyException(argument, paramName);
                }
            }
        }

        [DoesNotReturn]
        private static void ThrowNullOrEmptyException(string? argument, string? paramName)
        {
            if (argument is null)
            {
                ThrowArgumentNullException(paramName);
            }
            throw new ArgumentException("The value cannot be an empty string.", paramName);
        }

        extension(ObjectDisposedException)
        {
            public static void ThrowIf([DoesNotReturnIf(true)] bool condition, object instance)
            {
                if (condition)
                {
                    ThrowObjectDisposedException(instance);
                }
            }

            public static void ThrowIf([DoesNotReturnIf(true)] bool condition, Type type)
            {
                if (condition)
                {
                    ThrowObjectDisposedException(type);
                }
            }
        }

        [DoesNotReturn]
        private static void ThrowObjectDisposedException(object? instance)
        {
            throw new ObjectDisposedException(instance?.GetType().FullName);
        }

        [DoesNotReturn]
        private static void ThrowObjectDisposedException(Type? type)
        {
            throw new ObjectDisposedException(type?.FullName);
        }
    }
}
