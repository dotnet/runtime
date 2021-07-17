// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up logging services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class LoggingServiceCollectionExtensions
    {
        /// <summary>
        /// Adds logging services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddLogging(this IServiceCollection services)
        {
            return AddLogging(services, builder => { });
        }

        /// <summary>
        /// Adds logging services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configure">The <see cref="ILoggingBuilder"/> configuration delegate.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "ServiceDescriptor has RequiresUnreferencedCode because Service and Implementation types could be generic with mis-matching trimming annotations. " +
            "Types used here don't have trimming annotations.")]
        public static IServiceCollection AddLogging(this IServiceCollection services, Action<ILoggingBuilder> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, LoggerFactory>());
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>>(
                new DefaultLoggerLevelConfigureOptions(LogLevel.Information)));

            configure(new LoggingBuilder(services));
            return services;
        }
    }
}
