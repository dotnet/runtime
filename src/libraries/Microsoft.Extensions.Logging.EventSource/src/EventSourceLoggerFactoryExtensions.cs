// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.EventSource;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for the <see cref="ILoggerFactory"/> class.
    /// </summary>
    public static class EventSourceLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds an logger that writes messages to the <see cref="LoggingEventSource"/> instance.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <returns>The <see cref="ILoggerFactory"/> so that additional calls can be chained.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddEventSourceLogger(this ILoggingBuilder builder).", error: true)]
        public static ILoggerFactory AddEventSourceLogger(this ILoggerFactory factory)
        {
            ThrowHelper.ThrowIfNull(factory);

            factory.AddProvider(new EventSourceLoggerProvider(LoggingEventSource.Instance));

            return factory;
        }

        /// <summary>
        /// Adds an logger that writes messages to the <see cref="LoggingEventSource"/> instance.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        public static ILoggingBuilder AddEventSourceLogger(this ILoggingBuilder builder)
        {
            ThrowHelper.ThrowIfNull(builder);

            builder.Services.TryAddSingleton(LoggingEventSource.Instance);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, EventSourceLoggerProvider>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>, EventLogFiltersConfigureOptions>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<LoggerFilterOptions>, EventLogFiltersConfigureOptionsChangeSource>());
            return builder;
        }
    }
}
