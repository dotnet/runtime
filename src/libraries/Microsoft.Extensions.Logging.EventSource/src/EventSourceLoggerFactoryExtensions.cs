// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
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
        /// Adds an event logger named 'EventSource' to the factory.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "ServiceDescriptor has RequiresUnreferencedCode because Service and Implementation types could be generic with mis-matching trimming annotations. " +
            "Types used here are not generic.")]
        public static ILoggingBuilder AddEventSourceLogger(this ILoggingBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddSingleton(LoggingEventSource.Instance);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, EventSourceLoggerProvider>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>, EventLogFiltersConfigureOptions>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<LoggerFilterOptions>, EventLogFiltersConfigureOptionsChangeSource>());
            return builder;
        }
    }
}
