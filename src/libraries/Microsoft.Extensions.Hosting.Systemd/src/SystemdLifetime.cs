// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Systemd
{
    /// <summary>
    /// Provides notification messages for application started and stopping, and configures console logging to the systemd format.
    /// </summary>
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("tvos")]
    public partial class SystemdLifetime : IHostLifetime, IDisposable
    {
        private CancellationTokenRegistration _applicationStartedRegistration;
        private CancellationTokenRegistration _applicationStoppingRegistration;

        /// <summary>
        /// Initializes a new <see cref="SystemdLifetime"/> instance.
        /// </summary>
        /// <param name="environment">Information about the host.</param>
        /// <param name="applicationLifetime">The <see cref="IHostApplicationLifetime"/> that tracks the service lifetime.</param>
        /// <param name="systemdNotifier">The <see cref="ISystemdNotifier"/> to notify Systemd about service status.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used to instantiate the lifetime logger.</param>
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

        /// <summary>
        /// Asynchronously stops and shuts down the host. This method is called from <see cref="IHost.StopAsync(CancellationToken)" />.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that indicates when stop should no longer be graceful.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous stop operation.
        /// </returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Asynchronously waits until the start operation is complete before continuing. This method is called at the beginning of <see cref="IHost.StartAsync(CancellationToken)" />. This can be used to delay startup until signaled by an external event.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that indicates when stop should no longer be graceful.
        /// </param>
        /// <returns>
        /// A task that represents the waiting for start operation.
        /// </returns>
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            UnregisterShutdownHandlers();

            _applicationStartedRegistration.Dispose();
            _applicationStoppingRegistration.Dispose();
        }

        private partial void UnregisterShutdownHandlers();
    }
}
