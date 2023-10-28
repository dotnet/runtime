// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.Internal
{
    /// <summary>
    /// Listens for Ctrl+C or SIGTERM and initiates shutdown.
    /// </summary>
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public partial class ConsoleLifetime : IHostLifetime, IDisposable
    {
        private CancellationTokenRegistration _applicationStartedRegistration;
        private CancellationTokenRegistration _applicationStoppingRegistration;

        /// <summary>
        /// Initializes a <see cref="ConsoleLifetime"/> instance using the specified console lifetime options, host environment, host application lifetime and host options.
        /// </summary>
        /// <param name="options">An object used to retrieve <see cref="ConsoleLifetimeOptions"/> instances.</param>
        /// <param name="environment">An object that contains information about the hosting environment an application is running in.</param>
        /// <param name="applicationLifetime">An object that allows consumers to be notified of application lifetime events.</param>
        /// <param name="hostOptions">An object used to retrieve <see cref="HostOptions"/> instances.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> or <paramref name="environment"/> or <paramref name="applicationLifetime"/> or <paramref name="hostOptions"/> is <see langword="null"/>.</exception>
        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, IOptions<HostOptions> hostOptions)
            : this(options, environment, applicationLifetime, hostOptions, NullLoggerFactory.Instance) { }

        /// <summary>
        /// Initializes a <see cref="ConsoleLifetime"/> instance using the specified console lifetime options, host environment, host options and logger factory.
        /// </summary>
        /// <param name="options">An object used to retrieve <see cref="ConsoleLifetimeOptions"/> instances</param>
        /// <param name="environment">An object that contains information about the hosting environment an application is running in.</param>
        /// <param name="applicationLifetime">An object that allows consumers to be notified of application lifetime events.</param>
        /// <param name="hostOptions">An object used to retrieve <see cref="HostOptions"/> instances.</param>
        /// <param name="loggerFactory">An object to configure the logging system and create instances of <see cref="ILogger"/> from the registered <see cref="ILoggerProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> or <paramref name="environment"/> or <paramref name="applicationLifetime"/> or <paramref name="hostOptions"/> or <paramref name="loggerFactory"/> is <see langword="null"/>.</exception>
        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, IOptions<HostOptions> hostOptions, ILoggerFactory loggerFactory)
        {
            ThrowHelper.ThrowIfNull(options?.Value, nameof(options));
            ThrowHelper.ThrowIfNull(applicationLifetime);
            ThrowHelper.ThrowIfNull(environment);
            ThrowHelper.ThrowIfNull(hostOptions?.Value, nameof(hostOptions));
            ThrowHelper.ThrowIfNull(loggerFactory);

            Options = options.Value;
            Environment = environment;
            ApplicationLifetime = applicationLifetime;
            HostOptions = hostOptions.Value;
            Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
        }

        private ConsoleLifetimeOptions Options { get; }

        private IHostEnvironment Environment { get; }

        private IHostApplicationLifetime ApplicationLifetime { get; }

        private HostOptions HostOptions { get; }

        private ILogger Logger { get; }

        /// <summary>
        /// Registers the application start, application stop and shutdown handlers for this application.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous registration operation.</returns>
        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            if (!Options.SuppressStatusMessages)
            {
                _applicationStartedRegistration = ApplicationLifetime.ApplicationStarted.Register(state =>
                {
                    ((ConsoleLifetime)state!).OnApplicationStarted();
                },
                this);
                _applicationStoppingRegistration = ApplicationLifetime.ApplicationStopping.Register(state =>
                {
                    ((ConsoleLifetime)state!).OnApplicationStopping();
                },
                this);
            }

            RegisterShutdownHandlers();

            // Console applications start immediately.
            return Task.CompletedTask;
        }

        private partial void RegisterShutdownHandlers();

        private void OnApplicationStarted()
        {
            Logger.LogInformation("Application started. Press Ctrl+C to shut down.");
            Logger.LogInformation("Hosting environment: {EnvName}", Environment.EnvironmentName);
            Logger.LogInformation("Content root path: {ContentRoot}", Environment.ContentRootPath);
        }

        private void OnApplicationStopping()
        {
            Logger.LogInformation("Application is shutting down...");
        }

        /// <summary>
        /// This method does nothing.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token instance.</param>
        /// <returns>A <see cref="Task"/> that represents a completed task.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            // There's nothing to do here
            return Task.CompletedTask;
        }

        /// <summary>
        /// Unregisters the shutdown handlers and disposes the application start and application stop registrations.
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
