// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
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
        /// Adds a logger that writes to <see cref="System.Diagnostics.TraceSource"/>.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <param name="sourceSwitch">The <see cref="SourceSwitch"/> to use.</param>
        /// <param name="listener">The <see cref="TraceListener"/> to use.</param>
        /// <returns>The <see cref="ILoggerFactory"/> so that additional calls can be chained.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddTraceSource(this ILoggingBuilder builder).", error: true)]
        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, System.Diagnostics.SourceSwitch sourceSwitch, System.Diagnostics.TraceListener listener)
        {
            ThrowHelper.ThrowIfNull(factory);
            ThrowHelper.ThrowIfNull(sourceSwitch);
            ThrowHelper.ThrowIfNull(listener);

            factory.AddProvider(new TraceSourceLoggerProvider(sourceSwitch, listener));

            return factory;
        }

        /// <summary>
        /// Adds a logger that writes to <see cref="System.Diagnostics.TraceSource"/>.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <param name="sourceSwitch">The <see cref="SourceSwitch"/> to use.</param>
        /// <returns>The <see cref="ILoggerFactory"/> so that additional calls can be chained.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddTraceSource(this ILoggingBuilder builder).", error: true)]
        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, System.Diagnostics.SourceSwitch sourceSwitch)
        {
            ThrowHelper.ThrowIfNull(factory);
            ThrowHelper.ThrowIfNull(sourceSwitch);

            factory.AddProvider(new TraceSourceLoggerProvider(sourceSwitch));

            return factory;
        }

        /// <summary>
        /// Adds a logger that writes to <see cref="System.Diagnostics.TraceSource"/>.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <param name="switchName">The name of the <see cref="SourceSwitch"/> to use.</param>
        /// <param name="listener">The <see cref="TraceListener"/> to use.</param>
        /// <returns>The <see cref="ILoggerFactory"/> so that additional calls can be chained.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddTraceSource(this ILoggingBuilder builder).", error: true)]
        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, string switchName, System.Diagnostics.TraceListener listener)
        {
            ThrowHelper.ThrowIfNull(factory);
            ThrowHelper.ThrowIfNull(switchName);
            ThrowHelper.ThrowIfNull(listener);

            factory.AddProvider(new TraceSourceLoggerProvider(new SourceSwitch(switchName), listener));

            return factory;
        }

        /// <summary>
        /// Adds a logger that writes to <see cref="System.Diagnostics.TraceSource"/>.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <param name="switchName">The name of the <see cref="SourceSwitch"/> to use.</param>
        /// <returns>The <see cref="ILoggerFactory"/> so that additional calls can be chained.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddTraceSource(this ILoggingBuilder builder).", error: true)]
        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, string switchName)
        {
            ThrowHelper.ThrowIfNull(factory);
            ThrowHelper.ThrowIfNull(switchName);

            factory.AddProvider(new TraceSourceLoggerProvider(new SourceSwitch(switchName)));

            return factory;
        }

        /// <summary>
        /// Adds a logger that writes to <see cref="System.Diagnostics.TraceSource"/>.
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
        /// Adds a logger that writes to <see cref="System.Diagnostics.TraceSource"/>.
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
        /// Adds a logger that writes to <see cref="System.Diagnostics.TraceSource"/>.
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
        /// Adds a logger that writes to <see cref="System.Diagnostics.TraceSource"/>.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
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
