// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for setting up logging services in an <see cref="ILoggingBuilder" />.
    /// </summary>
    public static class LoggingBuilderExtensions
    {
        /// <summary>
        /// Sets a minimum <see cref="LogLevel"/> requirement for log messages to be logged.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to set the minimum level on.</param>
        /// <param name="level">The <see cref="LogLevel"/> to set as the minimum.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        public static ILoggingBuilder SetMinimumLevel(this ILoggingBuilder builder, LogLevel level)
        {
            builder.Services.Add(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>>(
                new DefaultLoggerLevelConfigureOptions(level)));
            return builder;
        }

        /// <summary>
        /// Adds the given <see cref="ILoggerProvider"/> to the <see cref="ILoggingBuilder"/>
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to add to the <paramref name="provider"/>.</param>
        /// <param name="provider">The <see cref="ILoggerProvider"/> to add to the <paramref name="builder"/>.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        public static ILoggingBuilder AddProvider(this ILoggingBuilder builder, ILoggerProvider provider)
        {
            builder.Services.AddSingleton(provider);
            return builder;
        }

        /// <summary>
        /// Removes all <see cref="ILoggerProvider"/>s from <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to remove <see cref="ILoggerProvider"/>s from.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        public static ILoggingBuilder ClearProviders(this ILoggingBuilder builder)
        {
            builder.Services.RemoveAll<ILoggerProvider>();
            return builder;
        }
    }
}
