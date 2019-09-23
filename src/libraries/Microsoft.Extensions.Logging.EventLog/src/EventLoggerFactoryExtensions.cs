// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.EventLog;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for the <see cref="ILoggerFactory"/> class.
    /// </summary>
    public static class EventLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds an event logger named 'EventLog' to the factory.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        public static ILoggingBuilder AddEventLog(this ILoggingBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, EventLogLoggerProvider>());

            return builder;
        }

        /// <summary>
        /// Adds an event logger. Use <paramref name="settings"/> to enable logging for specific <see cref="LogLevel"/>s.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        /// <param name="settings">The <see cref="EventLogSettings"/>.</param>
        public static ILoggingBuilder AddEventLog(this ILoggingBuilder builder, EventLogSettings settings)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider>(new EventLogLoggerProvider(settings)));

            return builder;
        }

        /// <summary>
        /// Adds an event logger that is enabled for <see cref="LogLevel"/>.Information or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is AddEventLog(this ILoggingBuilder builder).")]
        public static ILoggerFactory AddEventLog(this ILoggerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return AddEventLog(factory, LogLevel.Information);
        }

        /// <summary>
        /// Adds an event logger that is enabled for <see cref="LogLevel"/>s of minLevel or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> to be logged</param>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is AddEventLog(this ILoggingBuilder builder).")]
        public static ILoggerFactory AddEventLog(this ILoggerFactory factory, LogLevel minLevel)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return AddEventLog(factory, new EventLogSettings()
            {
                Filter = (_, logLevel) => logLevel >= minLevel
            });
        }

        /// <summary>
        /// Adds an event logger. Use <paramref name="settings"/> to enable logging for specific <see cref="LogLevel"/>s.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="settings">The <see cref="EventLogSettings"/>.</param>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is AddEventLog(this ILoggingBuilder builder).")]
        public static ILoggerFactory AddEventLog(
            this ILoggerFactory factory,
            EventLogSettings settings)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            factory.AddProvider(new EventLogLoggerProvider(settings));
            return factory;
        }
    }
}
