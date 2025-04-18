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

        [DoesNotReturn]
        private static void ThrowArgumentNullException(string? paramName) =>
            throw new ArgumentNullException(paramName);

        extension(ObjectDisposedException)
        {
            [StackTraceHidden]
            public static void ThrowIf([DoesNotReturnIf(true)] bool condition, object instance)
            {
                if (condition)
                {
                    ThrowHelper.ThrowObjectDisposedException(instance);
                }
            }

            [StackTraceHidden]
            public static void ThrowIf([DoesNotReturnIf(true)] bool condition, Type type)
            {
                if (condition)
                {
                    ThrowHelper.ThrowObjectDisposedException(type);
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
