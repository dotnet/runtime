// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class UseSystemdTests
    {
        public static bool IsRemoteExecutorSupportedOnLinux => PlatformDetection.IsLinux && RemoteExecutor.IsSupported;

        private static IHost BuildHostWithAddSystemd()
        {
            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                // Disable defaults that may not be supported on the testing platform like EventLogLoggerProvider.
                DisableDefaults = true,
            });
            builder.Services.AddSystemd();
            return builder.Build();
        }

        private static IHost BuildHostWithUseSystemd()
        {
            return new HostBuilder()
                .UseSystemd()
                .Build();
        }

        [ConditionalFact(typeof(UseSystemdTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void AddSystemd_SystemdLoggerIsNotConfiguredAndSystemdLifetimeIsNotRegisteredWhenIsSystemdServiceIsFalse()
        {
            // Simulate running outside of a systemd service
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", null);
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", null);

                using IHost host = BuildHostWithAddSystemd();
                var options = host.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;
                Assert.NotEqual(ConsoleFormatterNames.Systemd, options.FormatterName);

                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsNotType<SystemdLifetime>(lifetime);
            });
        }

        [ConditionalFact(typeof(UseSystemdTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void UseSystemd_SystemdLoggerIsNotConfiguredAndSystemdLifetimeIsNotRegisteredWhenIsSystemdServiceIsFalse()
        {
            // Simulate running outside of a systemd service
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", null);
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", null);

                using IHost host = BuildHostWithUseSystemd();
                var options = host.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;
                Assert.NotEqual(ConsoleFormatterNames.Systemd, options.FormatterName);

                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsNotType<SystemdLifetime>(lifetime);
            });
        }

        [ConditionalFact(typeof(UseSystemdTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void AddSystemd_SystemdLoggerIsConfiguredAndSystemdLifetimeIsRegisteredWhenIsSystemdServiceIsTrue()
        {
            // Simulates: Type=simple with SYSTEMD_EXEC_PID (e.g. systemd >= v248) nominal case.
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID",
                    Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", null);

                using IHost host = BuildHostWithAddSystemd();
                var options = host.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;
                Assert.Equal(ConsoleFormatterNames.Systemd, options.FormatterName);

                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<SystemdLifetime>(lifetime);
            });
        }

        [ConditionalFact(typeof(UseSystemdTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void UseSystemd_SystemdLoggerIsConfiguredAndSystemdLifetimeIsRegisteredWhenIsSystemdServiceIsTrue()
        {
            // Simulates: Type=simple with SYSTEMD_EXEC_PID (e.g. systemd >= v248) nominal case.
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID",
                    Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", null);

                using IHost host = BuildHostWithUseSystemd();
                var options = host.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;
                Assert.Equal(ConsoleFormatterNames.Systemd, options.FormatterName);

                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<SystemdLifetime>(lifetime);
            });
        }

        [ConditionalFact(typeof(UseSystemdTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void AddSystemd_SystemdLifetimeIsRegisteredAndLoggerIsNotConfiguredWhenOnlyNotifySocketIsSet()
        {
            // Simulates: Type=notify without SYSTEMD_EXEC_PID (e.g. containerized, Podman --sdnotify=container, systemd < v248).
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", null);
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", "/run/systemd/notify");

                using IHost host = BuildHostWithAddSystemd();

                Assert.Null(Environment.GetEnvironmentVariable("NOTIFY_SOCKET")); // Cleared to prevent child process inheritance.

                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<SystemdLifetime>(lifetime);

                var options = host.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;
                Assert.NotEqual(ConsoleFormatterNames.Systemd, options.FormatterName);
            });
        }

        [ConditionalFact(typeof(UseSystemdTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void UseSystemd_SystemdLifetimeIsRegisteredAndLoggerIsNotConfiguredWhenOnlyNotifySocketIsSet()
        {
            // Simulates: Type=notify without SYSTEMD_EXEC_PID (e.g. containerized, Podman --sdnotify=container, systemd < v248).
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", null);
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", "/run/systemd/notify");

                using IHost host = BuildHostWithUseSystemd();

                Assert.Null(Environment.GetEnvironmentVariable("NOTIFY_SOCKET")); // Cleared to prevent child process inheritance.

                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<SystemdLifetime>(lifetime);

                var options = host.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;
                Assert.NotEqual(ConsoleFormatterNames.Systemd, options.FormatterName);
            });
        }

        [ConditionalFact(typeof(UseSystemdTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void AddSystemd_SystemdLoggerAndLifetimeAreBothRegisteredWhenBothConditionsAreMet()
        {
            // Simulates: Type=notify with SYSTEMD_EXEC_PID (systemd >= v248).
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID",
                    Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", "/run/systemd/notify");

                using IHost host = BuildHostWithAddSystemd();

                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<SystemdLifetime>(lifetime);

                var options = host.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;
                Assert.Equal(ConsoleFormatterNames.Systemd, options.FormatterName);
            });
        }

        [ConditionalFact(typeof(UseSystemdTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void UseSystemd_SystemdLoggerAndLifetimeAreBothRegisteredWhenBothConditionsAreMet()
        {
            // Simulates: Type=notify with SYSTEMD_EXEC_PID (systemd >= v248).
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID",
                    Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", "/run/systemd/notify");

                using IHost host = BuildHostWithUseSystemd();

                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<SystemdLifetime>(lifetime);

                var options = host.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;
                Assert.Equal(ConsoleFormatterNames.Systemd, options.FormatterName);
            });
        }
    }
}
