// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Debug;
using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for the <see cref="ILoggerFactory"/> class.
    /// </summary>
    public static class DebugLoggerFactoryExtensions
    {
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is AddDebug(this ILoggingBuilder builder).")]
        public static ILoggerFactory AddDebug(this ILoggerFactory factory, LogLevel minLevel) { throw new NotImplementedException(); }

        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is AddDebug(this ILoggingBuilder builder).")]
        public static ILoggerFactory AddDebug(this ILoggerFactory factory, Func<string, LogLevel, bool> filter) { throw new NotImplementedException(); }

        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is AddDebug(this ILoggingBuilder builder).")]
        public static ILoggerFactory AddDebug(this ILoggerFactory factory) { throw new NotImplementedException(); }
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
