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

        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, IOptions<HostOptions> hostOptions)
            : this(options, environment, applicationLifetime, hostOptions, NullLoggerFactory.Instance) { }

        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, IOptions<HostOptions> hostOptions, ILoggerFactory loggerFactory)
        {
            ThrowHelper.ThrowIfNull(options?.Value, nameof(options));
            ThrowHelper.ThrowIfNull(applicationLifetime);
            ThrowHelper.ThrowIfNull(environment);
            ThrowHelper.ThrowIfNull(hostOptions?.Value, nameof(hostOptions));

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
            Logger.LogInformation("Hosting environment: {envName}", Environment.EnvironmentName);
            Logger.LogInformation("Content root path: {contentRoot}", Environment.ContentRootPath);
        }

        private void OnApplicationStopping()
        {
            Logger.LogInformation("Application is shutting down...");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // There's nothing to do here
            return Task.CompletedTask;
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
