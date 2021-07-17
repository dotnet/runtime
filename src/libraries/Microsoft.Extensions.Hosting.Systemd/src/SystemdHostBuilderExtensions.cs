// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
        /// Sets the host lifetime to <see cref="SystemdLifetime" />,
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
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to use.</param>
        /// <returns></returns>
        public static IHostBuilder UseSystemd(this IHostBuilder hostBuilder)
        {
            if (SystemdHelpers.IsSystemdService())
            {
                hostBuilder.ConfigureServices((hostContext, services) =>
                {
                    services.Configure<ConsoleLoggerOptions>(options =>
                    {
                        options.FormatterName = ConsoleFormatterNames.Systemd;
                    });

                    ConfigureLifetime(services);
                });
            }
            return hostBuilder;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "ServiceDescriptor has RequiresUnreferencedCode because Service and Implementation types could be generic with mis-matching trimming annotations. " +
            "Types used here are not generic.")]
        private static void ConfigureLifetime(IServiceCollection services)
        {
            services.AddSingleton<ISystemdNotifier, SystemdNotifier>();
            services.AddSingleton<IHostLifetime, SystemdLifetime>();
        }
    }
}
