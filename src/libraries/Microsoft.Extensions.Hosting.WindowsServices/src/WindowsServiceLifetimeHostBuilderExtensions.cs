// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extension methods for setting up WindowsServiceLifetime.
    /// </summary>
    public static class WindowsServiceLifetimeHostBuilderExtensions
    {
        /// <summary>
        /// Sets the host lifetime to <see cref="WindowsServiceLifetime"/> and enables logging to the event log with
        /// the application name as the default source name.
        /// </summary>
        /// <remarks>
        /// This is context aware and will only activate if it detects the process is running as a Windows Service.
        /// </remarks>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to operate on.</param>
        /// <returns>The <paramref name="hostBuilder"/> instance for chaining.</returns>
        public static IHostBuilder UseWindowsService(this IHostBuilder hostBuilder)
        {
            return UseWindowsService(hostBuilder, _ => { });
        }

        /// <summary>
        /// Sets the host lifetime to <see cref="WindowsServiceLifetime"/> and enables logging to the event log with the application
        /// name as the default source name.
        /// </summary>
        /// <remarks>
        /// This is context aware and will only activate if it detects the process is running
        /// as a Windows Service.
        /// </remarks>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to operate on.</param>
        /// <param name="configure">An <see cref="Action{WindowsServiceLifetimeOptions}"/> to configure the provided <see cref="WindowsServiceLifetimeOptions"/>.</param>
        /// <returns>The <paramref name="hostBuilder"/> instance for chaining.</returns>
        public static IHostBuilder UseWindowsService(this IHostBuilder hostBuilder, Action<WindowsServiceLifetimeOptions> configure)
        {
            ThrowHelper.ThrowIfNull(hostBuilder);

            if (WindowsServiceHelpers.IsWindowsService())
            {
                hostBuilder.ConfigureServices(services =>
                {
                    AddWindowsServiceLifetime(services, configure);
                });
            }

            return hostBuilder;
        }

        /// <summary>
        /// Configures the lifetime of the <see cref="IHost"/> built from <paramref name="services"/> to
        /// <see cref="WindowsServiceLifetime"/> and enables logging to the event log with the application
        /// name as the default source name.
        /// </summary>
        /// <remarks>
        /// This is context aware and will only activate if it detects the process is running
        /// as a Windows Service.
        /// </remarks>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> used to build the <see cref="IHost"/>.
        /// For example, <see cref="HostApplicationBuilder.Services"/> or the <see cref="IServiceCollection"/> passed to the
        /// <see cref="IHostBuilder.ConfigureServices(Action{HostBuilderContext, IServiceCollection})"/> callback.
        /// </param>
        /// <returns>The <paramref name="services"/> instance for chaining.</returns>
        public static IServiceCollection AddWindowsService(this IServiceCollection services)
        {
            return AddWindowsService(services, _ => { });
        }

        /// <summary>
        /// Configures the lifetime of the <see cref="IHost"/> built from <paramref name="services"/> to
        /// <see cref="WindowsServiceLifetime"/> and enables logging to the event log with the application name as the default source name.
        /// </summary>
        /// <remarks>
        /// This is context aware and will only activate if it detects the process is running
        /// as a Windows Service.
        /// </remarks>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> used to build the <see cref="IHost"/>.
        /// For example, <see cref="HostApplicationBuilder.Services"/> or the <see cref="IServiceCollection"/> passed to the
        /// <see cref="IHostBuilder.ConfigureServices(Action{HostBuilderContext, IServiceCollection})"/> callback.
        /// </param>
        /// <param name="configure">An <see cref="Action{WindowsServiceLifetimeOptions}"/> to configure the provided <see cref="WindowsServiceLifetimeOptions"/>.</param>
        /// <returns>The <paramref name="services"/> instance for chaining.</returns>
        public static IServiceCollection AddWindowsService(this IServiceCollection services, Action<WindowsServiceLifetimeOptions> configure)
        {
            ThrowHelper.ThrowIfNull(services);

            if (WindowsServiceHelpers.IsWindowsService())
            {
                AddWindowsServiceLifetime(services, configure);
            }

            return services;
        }

        private static void AddWindowsServiceLifetime(IServiceCollection services, Action<WindowsServiceLifetimeOptions> configure)
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            services.AddLogging(logging =>
            {
                Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
                logging.AddEventLog();
            });
            services.AddSingleton<IHostLifetime, WindowsServiceLifetime>();
            services.AddSingleton<IConfigureOptions<EventLogSettings>, EventLogSettingsSetup>();
            services.Configure(configure);
        }

        private sealed class EventLogSettingsSetup : IConfigureOptions<EventLogSettings>
        {
            private readonly string? _applicationName;

            public EventLogSettingsSetup(IHostEnvironment environment)
            {
                _applicationName = environment.ApplicationName;
            }

            public void Configure(EventLogSettings settings)
            {
                Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

                if (string.IsNullOrEmpty(settings.SourceName))
                {
                    settings.SourceName = _applicationName;
                }
            }
        }
    }
}
