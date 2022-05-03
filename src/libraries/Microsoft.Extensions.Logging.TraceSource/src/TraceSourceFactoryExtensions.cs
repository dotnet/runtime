// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.TraceSource;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for setting up <see cref="TraceSourceLoggerProvider"/> on a <see cref="ILoggingBuilder"/>.
    /// </summary>
    public static class TraceSourceFactoryExtensions
    {
        /// <summary>
        /// Adds a TraceSource logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="switchName">The name of the <see cref="SourceSwitch"/> to use.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        public static ILoggingBuilder AddTraceSource(
            this ILoggingBuilder builder,
            string switchName)
        {
            ThrowHelper.ThrowIfNull(builder);
            ThrowHelper.ThrowIfNull(switchName);

            return builder.AddTraceSource(new SourceSwitch(switchName));
        }

        /// <summary>
        /// Adds a TraceSource logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="switchName">The name of the <see cref="SourceSwitch"/> to use.</param>
        /// <param name="listener">The <see cref="TraceListener"/> to use.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        public static ILoggingBuilder AddTraceSource(
            this ILoggingBuilder builder,
            string switchName,
            TraceListener listener)
        {
            ThrowHelper.ThrowIfNull(builder);
            ThrowHelper.ThrowIfNull(switchName);
            ThrowHelper.ThrowIfNull(listener);

            return builder.AddTraceSource(new SourceSwitch(switchName), listener);
        }

        /// <summary>
        /// Adds a TraceSource logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="sourceSwitch">The <see cref="SourceSwitch"/> to use.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        public static ILoggingBuilder AddTraceSource(
            this ILoggingBuilder builder,
            SourceSwitch sourceSwitch)
        {
            ThrowHelper.ThrowIfNull(builder);
            ThrowHelper.ThrowIfNull(sourceSwitch);

            builder.Services.AddSingleton<ILoggerProvider>(_ => new TraceSourceLoggerProvider(sourceSwitch));

            return builder;
        }

        /// <summary>
        /// Adds a TraceSource logger named 'TraceSource' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="LoggerFactory"/> to use.</param>
        /// <param name="sourceSwitch">The <see cref="SourceSwitch"/> to use.</param>
        /// <param name="listener">The <see cref="TraceListener"/> to use.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
        public static ILoggingBuilder AddTraceSource(
            this ILoggingBuilder builder,
            SourceSwitch sourceSwitch,
            TraceListener listener)
        {
            ThrowHelper.ThrowIfNull(builder);
            ThrowHelper.ThrowIfNull(sourceSwitch);
            ThrowHelper.ThrowIfNull(listener);

            builder.Services.AddSingleton<ILoggerProvider>(_ => new TraceSourceLoggerProvider(sourceSwitch, listener));

            return builder;
        }
    }
}
