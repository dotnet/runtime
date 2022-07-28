// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Systemd
{
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("tvos")]
    public partial class SystemdLifetime : IHostLifetime, IDisposable
    {
        private CancellationTokenRegistration _applicationStartedRegistration;
        private CancellationTokenRegistration _applicationStoppingRegistration;

        public SystemdLifetime(IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ISystemdNotifier systemdNotifier, ILoggerFactory loggerFactory)
        {
            ThrowHelper.ThrowIfNull(environment);
            ThrowHelper.ThrowIfNull(applicationLifetime);
            ThrowHelper.ThrowIfNull(systemdNotifier);

            Environment = environment;
            ApplicationLifetime = applicationLifetime;
            SystemdNotifier = systemdNotifier;
            Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
        }

        private IHostApplicationLifetime ApplicationLifetime { get; }
        private IHostEnvironment Environment { get; }
        private ILogger Logger { get; }
        private ISystemdNotifier SystemdNotifier { get; }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            _applicationStartedRegistration = ApplicationLifetime.ApplicationStarted.Register(state =>
            {
                ((SystemdLifetime)state!).OnApplicationStarted();
            },
            this);
            _applicationStoppingRegistration = ApplicationLifetime.ApplicationStopping.Register(state =>
            {
                ((SystemdLifetime)state!).OnApplicationStopping();
            },
            this);

            RegisterShutdownHandlers();

            return Task.CompletedTask;
        }

        private partial void RegisterShutdownHandlers();

        private void OnApplicationStarted()
        {
            Logger.LogInformation("Application started. Hosting environment: {EnvironmentName}; Content root path: {ContentRoot}",
                Environment.EnvironmentName, Environment.ContentRootPath);

            SystemdNotifier.Notify(ServiceState.Ready);
        }

        private void OnApplicationStopping()
        {
            Logger.LogInformation("Application is shutting down...");

            SystemdNotifier.Notify(ServiceState.Stopping);
        }

        public void Dispose()
        {
            UnregisterShutdownHandlers();

            _applicationStartedRegistration.Dispose();
            _applicationStoppingRegistration.Dispose();
        }

        private partial void UnregisterShutdownHandlers();
    }
}
