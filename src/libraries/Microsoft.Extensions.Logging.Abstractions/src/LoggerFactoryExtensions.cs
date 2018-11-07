// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging.Abstractions.Internal;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// ILoggerFactory extension methods for common scenarios.
    /// </summary>
    public static class LoggerFactoryExtensions
    {
        /// <summary>
        /// Creates a new ILogger instance using the full name of the given type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="factory">The factory.</param>
        public static ILogger<T> CreateLogger<T>(this ILoggerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            return new Logger<T>(factory);
        }
        /// <summary>
        /// Creates a new ILogger instance using the full name of the given type.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="type">The type.</param>
        public static ILogger CreateLogger(this ILoggerFactory factory, Type type)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return factory.CreateLogger(TypeNameHelper.GetTypeDisplayName(type));
        }
    }
}