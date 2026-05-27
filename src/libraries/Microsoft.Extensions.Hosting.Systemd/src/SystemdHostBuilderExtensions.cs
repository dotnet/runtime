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
        /// and configures console logging to the systemd format when running as a systemd service.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This is context aware and will only activate if it detects the process is running
        ///     as a systemd Service or if <c>NOTIFY_SOCKET</c> is set.
        ///   </para>
        ///   <para>
        ///     The console log formatter is enabled when the process is detected as a systemd service.
        ///     The <see cref="SystemdLifetime"/> and <see cref="SystemdNotifier"/> are registered when
        ///     <c>NOTIFY_SOCKET</c> is set or the process is detected as a systemd service.
        ///   </para>
        ///   <para>
        ///     The systemd service file must be configured with <c>Type=notify</c> to enable
        ///     notifications. See <see href="https://www.freedesktop.org/software/systemd/man/systemd.service.html"/>.
        ///   </para>
        /// </remarks>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <returns>The <paramref name="hostBuilder"/> instance for chaining.</returns>
        public static IHostBuilder UseSystemd(this IHostBuilder hostBuilder)
        {
            ArgumentNullException.ThrowIfNull(hostBuilder);

            if (SystemdHelpers.IsSystemdLogger())
            {
                hostBuilder.ConfigureServices((hostContext, services) =>
                {
                    AddSystemdLogger(services);
                });
            }

            if (SystemdHelpers.IsSystemdLifetime())
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
        /// and stopping, and configures console logging to the systemd format when running as a systemd service.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This is context aware and will only activate if it detects the process is running
        ///     as a systemd Service or if <c>NOTIFY_SOCKET</c> is set.
        ///   </para>
        ///   <para>
        ///     The console log formatter is enabled when the process is detected as a systemd service.
        ///     The <see cref="SystemdLifetime"/> and <see cref="SystemdNotifier"/> are registered when
        ///     <c>NOTIFY_SOCKET</c> is set or the process is detected as a systemd service.
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
            ArgumentNullException.ThrowIfNull(services);

            if (SystemdHelpers.IsSystemdLogger())
            {
                AddSystemdLogger(services);
            }

            if (SystemdHelpers.IsSystemdLifetime())
            {
                AddSystemdLifetime(services);
            }

            return services;
        }

        private static void AddSystemdLogger(IServiceCollection services)
        {
            services.Configure<ConsoleLoggerOptions>(options =>
            {
                options.FormatterName = ConsoleFormatterNames.Systemd;
            });
        }

        private static void AddSystemdLifetime(IServiceCollection services)
        {
            // SystemdNotifier and SystemdLifetime are Unix-only; IsSystemdLifetime() ensures
            // we only reach this code when running on Unix and when the environment indicates
            // systemd-style integration (for example, when NOTIFY_SOCKET is set or the process
            // is detected as a systemd service).
#pragma warning disable CA1416 // Validate platform compatibility
            services.AddSingleton<ISystemdNotifier>(_ =>
            {
                // Construct the notifier first so it reads (and normalizes) NOTIFY_SOCKET, then
                // clear the env var so child processes don't inherit it and accidentally notify
                // the parent's service manager. Done inside the DI factory so SystemdNotifier
                // construction stays lazy and the env var is only mutated when hosting is
                // actually wired up.
                var notifier = new SystemdNotifier();
                Environment.SetEnvironmentVariable(SystemdConstants.NotifySocket, null);
                return notifier;
            });
            services.AddSingleton<IHostLifetime, SystemdLifetime>();
#pragma warning restore CA1416 // Validate platform compatibility

        }
    }
}
