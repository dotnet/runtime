// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Debug;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for the <see cref="ILoggerFactory"/> class.
    /// </summary>
    public static class DebugLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds a debug logger named 'Debug' to the factory.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        public static ILoggingBuilder AddDebug(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DebugLoggerProvider>());

            return builder;
        }

        /// <summary>
        /// Adds a debug logger that is enabled for <see cref="LogLevel"/>.Information or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is AddDebug(this ILoggingBuilder builder).")]
        public static ILoggerFactory AddDebug(this ILoggerFactory factory)
        {
            return AddDebug(factory, LogLevel.Information);
        }

        /// <summary>
        /// Adds a debug logger that is enabled as defined by the filter function.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="filter">The function used to filter events based on the log level.</param>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is AddDebug(this ILoggingBuilder builder).")]
        public static ILoggerFactory AddDebug(this ILoggerFactory factory, Func<string, LogLevel, bool> filter)
        {
            factory.AddProvider(new DebugLoggerProvider(filter));
            return factory;
        }

        /// <summary>
        /// Adds a debug logger that is enabled for <see cref="LogLevel"/>s of minLevel or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> to be logged</param>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is AddDebug(this ILoggingBuilder builder).")]
        public static ILoggerFactory AddDebug(this ILoggerFactory factory, LogLevel minLevel)
        {
            return AddDebug(
               factory,
               (_, logLevel) => logLevel >= minLevel);
        }
    }
}