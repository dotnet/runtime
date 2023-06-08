// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.WindowsServices
{
    /// <summary>
    /// Listens for shutdown signal and tracks the status of the Windows service.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class WindowsServiceLifetime : ServiceBase, IHostLifetime
    {
        private readonly TaskCompletionSource<object?> _delayStart = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<object?> _serviceDispatcherStopped = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ManualResetEventSlim _delayStop = new ManualResetEventSlim();
        private readonly HostOptions _hostOptions;
        private bool _serviceStopRequested;

        /// <summary>
        /// Initializes a new <see cref="WindowsServiceLifetime"/> instance.
        /// </summary>
        /// <param name="environment">Information about the host.</param>
        /// <param name="applicationLifetime">The <see cref="IHostApplicationLifetime"/> that tracks the service lifetime.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used to instantiate the lifetime logger.</param>
        /// <param name="optionsAccessor">The <see cref="IOptions{HostOptions}"/> containing options for the service.</param>
        public WindowsServiceLifetime(IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor)
            : this(environment, applicationLifetime, loggerFactory, optionsAccessor, Options.Options.Create(new WindowsServiceLifetimeOptions()))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsServiceLifetime"/> class.
        /// </summary>
        /// <param name="environment">Information about the host.</param>
        /// <param name="applicationLifetime">The <see cref="IHostApplicationLifetime"/> that tracks the service lifetime.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used to instantiate the lifetime logger.</param>
        /// <param name="optionsAccessor">The <see cref="IOptions{HostOptions}"/> containing options for the service.</param>
        /// <param name="windowsServiceOptionsAccessor">The Windows service options used to find the service name.</param>
        public WindowsServiceLifetime(IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor, IOptions<WindowsServiceLifetimeOptions> windowsServiceOptionsAccessor)
        {
            ThrowHelper.ThrowIfNull(environment);
            ThrowHelper.ThrowIfNull(applicationLifetime);
            ThrowHelper.ThrowIfNull(optionsAccessor);
            ThrowHelper.ThrowIfNull(windowsServiceOptionsAccessor);

            Environment = environment;
            ApplicationLifetime = applicationLifetime;
            Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
            _hostOptions = optionsAccessor.Value;
            ServiceName = windowsServiceOptionsAccessor.Value.ServiceName;
            CanShutdown = true;
        }

        private IHostApplicationLifetime ApplicationLifetime { get; }
        private IHostEnvironment Environment { get; }
        private ILogger Logger { get; }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _delayStart.TrySetCanceled());
            ApplicationLifetime.ApplicationStarted.Register(() =>
            {
                Logger.LogInformation("Application started. Hosting environment: {EnvName}; Content root path: {ContentRoot}",
                    Environment.EnvironmentName, Environment.ContentRootPath);
            });
            ApplicationLifetime.ApplicationStopping.Register(() =>
            {
                Logger.LogInformation("Application is shutting down...");
            });
            ApplicationLifetime.ApplicationStopped.Register(_delayStop.Set);

            Thread thread = new Thread(Run);
            thread.IsBackground = true;
            thread.Start(); // Otherwise this would block and prevent IHost.StartAsync from finishing.

            return _delayStart.Task;
        }

        private void Run()
        {
            try
            {
                Run(this); // This blocks until the service is stopped.
                _delayStart.TrySetException(new InvalidOperationException("Stopped without starting"));
                _serviceDispatcherStopped.TrySetResult(null);
            }
            catch (Exception ex)
            {
                _delayStart.TrySetException(ex);
                _serviceDispatcherStopped.TrySetException(ex);
            }
        }

        /// <summary>
        /// Called from <see cref="IHost.StopAsync"/> to stop the service if not already stopped, and wait for the service dispatcher to exit.
        /// Once this method returns the service is stopped and the process can be terminated at any time.
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_serviceStopRequested)
            {
                await Task.Run(Stop, cancellationToken).ConfigureAwait(false);
            }

            // When the underlying service is stopped this will cause the ServiceBase.Run method to complete and return, which completes _serviceDispatcherStopped.
            await _serviceDispatcherStopped.Task.ConfigureAwait(false);
        }

        // Called by base.Run when the service is ready to start.
        /// <inheritdoc />
        protected override void OnStart(string[] args)
        {
            _delayStart.TrySetResult(null);
            base.OnStart(args);
        }

        /// <summary>
        /// Executes when a Stop command is sent to the service by the Service Control Manager (SCM).
        /// Triggers <see cref="IHostApplicationLifetime.ApplicationStopping"/> and waits for <see cref="IHostApplicationLifetime.ApplicationStopped"/>.
        /// Shortly after this method returns, the Service will be marked as stopped in SCM and the process may exit at any point.
        /// </summary>
        protected override void OnStop()
        {
            _serviceStopRequested = true;
            ApplicationLifetime.StopApplication();
            // Wait for the host to shutdown before marking service as stopped.
            _delayStop.Wait(_hostOptions.ShutdownTimeout);
            base.OnStop();
        }

        /// <summary>
        /// Executes when a Shutdown command is sent to the service by the Service Control Manager (SCM).
        /// Triggers <see cref="IHostApplicationLifetime.ApplicationStopping"/> and waits for <see cref="IHostApplicationLifetime.ApplicationStopped"/>.
        /// Shortly after this method returns, the Service will be marked as stopped in SCM and the process may exit at any point.
        /// </summary>
        protected override void OnShutdown()
        {
            _serviceStopRequested = true;
            ApplicationLifetime.StopApplication();
            // Wait for the host to shutdown before marking service as stopped.
            _delayStop.Wait(_hostOptions.ShutdownTimeout);
            base.OnShutdown();
        }

        /// <summary>
        /// Releases the resources used by the <see cref="WindowsServiceLifetime"/>.
        /// </summary>
        /// <param name="disposing"><see langword="true" /> only when called from <see cref="IDisposable.Dispose"/>; otherwise, <see langword="false" />.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _delayStop.Set();
            }

            base.Dispose(disposing);
        }
    }
}
