// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
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
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
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
        /// Adds an event logger. Use <paramref name="configure"/> to enable logging for specific <see cref="LogLevel"/>s.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        /// <param name="configure">A delegate to configure the <see cref="EventLogSettings"/>.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        public static ILoggingBuilder AddEventLog(this ILoggingBuilder builder, Action<EventLogSettings> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddEventLog();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
