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
    [SupportedOSPlatform("windows")]
    public class WindowsServiceLifetime : ServiceBase, IHostLifetime
    {
        private readonly TaskCompletionSource<object?> _delayStart = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ManualResetEventSlim _delayStop = new ManualResetEventSlim();
        private readonly HostOptions _hostOptions;

        public WindowsServiceLifetime(IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor)
            : this(environment, applicationLifetime, loggerFactory, optionsAccessor, Options.Options.Create(new WindowsServiceLifetimeOptions()))
        {
        }

        public WindowsServiceLifetime(IHostEnvironment environment!!, IHostApplicationLifetime applicationLifetime!!, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor!!, IOptions<WindowsServiceLifetimeOptions> windowsServiceOptionsAccessor!!)
        {
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
                Logger.LogInformation("Application started. Hosting environment: {envName}; Content root path: {contentRoot}",
                    Environment.EnvironmentName, Environment.ContentRootPath);
            });
            ApplicationLifetime.ApplicationStopping.Register(() =>
            {
                Logger.LogInformation("Application is shutting down...");
            });
            ApplicationLifetime.ApplicationStopped.Register(() =>
            {
                _delayStop.Set();
            });

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
            }
            catch (Exception ex)
            {
                _delayStart.TrySetException(ex);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Avoid deadlock where host waits for StopAsync before firing ApplicationStopped,
            // and Stop waits for ApplicationStopped.
            Task.Run(Stop, CancellationToken.None);
            return Task.CompletedTask;
        }

        // Called by base.Run when the service is ready to start.
        protected override void OnStart(string[] args)
        {
            _delayStart.TrySetResult(null);
            base.OnStart(args);
        }

        // Called by base.Stop. This may be called multiple times by service Stop, ApplicationStopping, and StopAsync.
        // That's OK because StopApplication uses a CancellationTokenSource and prevents any recursion.
        protected override void OnStop()
        {
            ApplicationLifetime.StopApplication();
            // Wait for the host to shutdown before marking service as stopped.
            _delayStop.Wait(_hostOptions.ShutdownTimeout);
            base.OnStop();
        }

        protected override void OnShutdown()
        {
            ApplicationLifetime.StopApplication();
            // Wait for the host to shutdown before marking service as stopped.
            _delayStop.Wait(_hostOptions.ShutdownTimeout);
            base.OnShutdown();
        }

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
