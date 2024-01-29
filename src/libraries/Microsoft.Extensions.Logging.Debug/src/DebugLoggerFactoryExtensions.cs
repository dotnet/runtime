// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
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
        /// Adds a debug logger that is enabled for <see cref="LogLevel"/>.Information or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> to be logged.  This parameter is no longer honored and will be ignored.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddDebug(this ILoggingBuilder builder).", error: true)]
        public static ILoggerFactory AddDebug(this ILoggerFactory factory, LogLevel minLevel) => AddDebug(factory);

        /// <summary>
        /// Adds a debug logger that is enabled as defined by the filter function.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="filter">The function used to filter events based on the log level.  This parameter is no longer honored and will be ignored.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddDebug(this ILoggingBuilder builder).", error: true)]
        public static ILoggerFactory AddDebug(this ILoggerFactory factory, Func<string, LogLevel, bool> filter) => AddDebug(factory);

        /// <summary>
        /// Adds a debug logger that is enabled for <see cref="LogLevel"/>s of minLevel or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddDebug(this ILoggingBuilder builder).", error: true)]
        public static ILoggerFactory AddDebug(this ILoggerFactory factory)
        {
            factory.AddProvider(new DebugLoggerProvider());
            return factory;
        }

        /// <summary>
        /// Adds a debug logger named 'Debug' to the factory.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        public static ILoggingBuilder AddDebug(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DebugLoggerProvider>());

            return builder;
        }
    }
}
