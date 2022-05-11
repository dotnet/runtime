// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// ILoggerFactory extension methods for common scenarios.
    /// </summary>
    public static class LoggerFactoryExtensions
    {
        /// <summary>
        /// Creates a new <see cref="ILogger"/> instance using the full name of the given type.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>The <see cref="ILogger"/> that was created.</returns>
        public static ILogger<T> CreateLogger<T>(this ILoggerFactory factory)
        {
            ThrowHelper.ThrowIfNull(factory);

            return new Logger<T>(factory);
        }
        /// <summary>
        /// Creates a new <see cref="ILogger"/> instance using the full name of the given <paramref name="type"/>.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="type">The type.</param>
        /// <return>The <see cref="ILogger"/> that was created.</return>
        public static ILogger CreateLogger(this ILoggerFactory factory, Type type)
        {
            ThrowHelper.ThrowIfNull(factory);
            ThrowHelper.ThrowIfNull(type);

            return factory.CreateLogger(TypeNameHelper.GetTypeDisplayName(type, includeGenericParameters: false, nestedTypeDelimiter: '.'));
        }
    }
}
