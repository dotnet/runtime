// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extension methods for setting up <see cref="SystemdLifetime" />.
    /// </summary>
    public static class SystemdHostBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="IHost"/> lifetime to <see cref="SystemdLifetime"/>,
        /// provides notification messages for application started and stopping,
        /// and configures console logging to the systemd format.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This is context aware and will only activate if it detects the process is running
        ///     as a systemd Service.
        ///   </para>
        ///   <para>
        ///     The systemd service file must be configured with <c>Type=notify</c> to enable
        ///     notifications. See https://www.freedesktop.org/software/systemd/man/systemd.service.html.
        ///   </para>
        /// </remarks>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <returns>The <paramref name="hostBuilder"/> instance for chaining.</returns>
        public static IHostBuilder UseSystemd(this IHostBuilder hostBuilder)
        {
            ThrowHelper.ThrowIfNull(hostBuilder);

            if (SystemdHelpers.IsSystemdService())
            {
                hostBuilder.ConfigureServices((hostContext, services) =>
                {
                    AddSystemdLifetime(services);
                });
            }
            return hostBuilder;
        }

        /// <summary>
        /// Configures the lifetime of the <see cref="IHost"/> built from <paramref name="services"/> to
        /// <see cref="SystemdLifetime"/>, provides notification messages for application started
        /// and stopping, and configures console logging to the systemd format.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This is context aware and will only activate if it detects the process is running
        ///     as a systemd Service.
        ///   </para>
        ///   <para>
        ///     The systemd service file must be configured with <c>Type=notify</c> to enable
        ///     notifications. See <see href="https://www.freedesktop.org/software/systemd/man/systemd.service.html"/>.
        ///   </para>
        /// </remarks>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> used to build the <see cref="IHost"/>.
        /// For example, <see cref="HostApplicationBuilder.Services"/> or the <see cref="IServiceCollection"/> passed to the
        /// <see cref="IHostBuilder.ConfigureServices(System.Action{HostBuilderContext, IServiceCollection})"/> callback.
        /// </param>
        /// <returns>The <paramref name="services"/> instance for chaining.</returns>
        public static IServiceCollection AddSystemd(this IServiceCollection services)
        {
            ThrowHelper.ThrowIfNull(services);

            if (SystemdHelpers.IsSystemdService())
            {
                AddSystemdLifetime(services);
            }
            return services;
        }

        private static void AddSystemdLifetime(IServiceCollection services)
        {
            services.Configure<ConsoleLoggerOptions>(options =>
            {
                options.FormatterName = ConsoleFormatterNames.Systemd;
            });

            // IsSystemdService() will never return true for android/browser/iOS/tvOS
#pragma warning disable CA1416 // Validate platform compatibility
            services.AddSingleton<ISystemdNotifier, SystemdNotifier>();
            services.AddSingleton<IHostLifetime, SystemdLifetime>();
#pragma warning restore CA1416 // Validate platform compatibility

        }
    }
}
