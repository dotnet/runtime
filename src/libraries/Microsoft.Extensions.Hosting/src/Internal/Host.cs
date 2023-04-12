// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.Internal
{
    internal sealed class Host : IHost, IAsyncDisposable
    {
        private readonly ILogger<Host> _logger;
        private readonly IHostLifetime _hostLifetime;
        private readonly ApplicationLifetime _applicationLifetime;
        private readonly HostOptions _options;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly PhysicalFileProvider _defaultProvider;
        private IEnumerable<IHostedService>? _hostedServices;
        private volatile bool _stopCalled;

        public Host(IServiceProvider services,
                    IHostEnvironment hostEnvironment,
                    PhysicalFileProvider defaultProvider,
                    IHostApplicationLifetime applicationLifetime,
                    ILogger<Host> logger,
                    IHostLifetime hostLifetime,
                    IOptions<HostOptions> options)
        {
            ThrowHelper.ThrowIfNull(services);
            ThrowHelper.ThrowIfNull(applicationLifetime);
            ThrowHelper.ThrowIfNull(logger);
            ThrowHelper.ThrowIfNull(hostLifetime);

            Services = services;
            _applicationLifetime = (applicationLifetime as ApplicationLifetime)!;
            _hostEnvironment = hostEnvironment;
            _defaultProvider = defaultProvider;

            if (_applicationLifetime is null)
            {
                throw new ArgumentException(SR.IHostApplicationLifetimeReplacementNotSupported, nameof(applicationLifetime));
            }
            _logger = logger;
            _hostLifetime = hostLifetime;
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public IServiceProvider Services { get; }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.Starting();

            using var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _applicationLifetime.ApplicationStopping);
            CancellationToken combinedCancellationToken = combinedCancellationTokenSource.Token;

            await _hostLifetime.WaitForStartAsync(combinedCancellationToken).ConfigureAwait(false);

            combinedCancellationToken.ThrowIfCancellationRequested();
            _hostedServices = Services.GetRequiredService<IEnumerable<IHostedService>>();

            List<Exception> exceptions = new List<Exception>();

            if (_options.ServicesStartConcurrently)
            {
                Task tasks = Task.WhenAll(_hostedServices.Select(async service =>
                {
                    await service.StartAsync(combinedCancellationToken).ConfigureAwait(false);

                    if (service is BackgroundService backgroundService)
                    {
                        _ = TryExecuteBackgroundServiceAsync(backgroundService);
                    }
                }));

                try
                {
                    await tasks.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.AddRange(tasks.Exception?.InnerExceptions ?? new[] { ex }.AsEnumerable());
                }
            }
            else
            {
                foreach (IHostedService hostedService in _hostedServices)
                {
                    try
                    {
                        // Fire IHostedService.Start
                        await hostedService.StartAsync(combinedCancellationToken).ConfigureAwait(false);

                        if (hostedService is BackgroundService backgroundService)
                        {
                            _ = TryExecuteBackgroundServiceAsync(backgroundService);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                        break;
                    }
                }
            }

            if (exceptions.Count > 0)
            {
                if (exceptions.Count == 1)
                {
                    // Rethrow if it's a single error
                    Exception singleException = exceptions[0];
                    _logger.HostedServiceStartupFaulted(singleException);
                    ExceptionDispatchInfo.Capture(singleException).Throw();
                }
                else
                {
                    var ex = new AggregateException("One or more hosted services failed to start.", exceptions);
                    _logger.HostedServiceStartupFaulted(ex);
                    throw ex;
                }
            }

            // Fire IHostApplicationLifetime.Started
            _applicationLifetime.NotifyStarted();

            _logger.Started();
        }

        private async Task TryExecuteBackgroundServiceAsync(BackgroundService backgroundService)
        {
            // backgroundService.ExecuteTask may not be set (e.g. if the derived class doesn't call base.StartAsync)
            Task? backgroundTask = backgroundService.ExecuteTask;
            if (backgroundTask == null)
            {
                return;
            }

            try
            {
                await backgroundTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // When the host is being stopped, it cancels the background services.
                // This isn't an error condition, so don't log it as an error.
                if (_stopCalled && backgroundTask.IsCanceled && ex is OperationCanceledException)
                {
                    return;
                }

                _logger.BackgroundServiceFaulted(ex);
                if (_options.BackgroundServiceExceptionBehavior == BackgroundServiceExceptionBehavior.StopHost)
                {
                    _logger.BackgroundServiceStoppingHost(ex);
                    _applicationLifetime.StopApplication();
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _stopCalled = true;
            _logger.Stopping();

            using (var cts = new CancellationTokenSource(_options.ShutdownTimeout))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
            {
                CancellationToken token = linkedCts.Token;
                // Trigger IHostApplicationLifetime.ApplicationStopping
                _applicationLifetime.StopApplication();

                var exceptions = new List<Exception>();
                if (_hostedServices != null) // Started?
                {
                    // Ensure hosted services are stopped in LIFO order
                    IEnumerable<IHostedService> hostedServices = _hostedServices.Reverse();

                    if (_options.ServicesStopConcurrently)
                    {
                        Task tasks = Task.WhenAll(hostedServices.Select(async service => await service.StopAsync(token).ConfigureAwait(false)));

                        try
                        {
                            await tasks.ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            exceptions.AddRange(tasks.Exception?.InnerExceptions ?? new[] { ex }.AsEnumerable());
                        }
                    }
                    else
                    {
                        foreach (IHostedService hostedService in hostedServices)
                        {
                            try
                            {
                                await hostedService.StopAsync(token).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                            }
                        }
                    }
                }

                // Fire IHostApplicationLifetime.Stopped
                _applicationLifetime.NotifyStopped();

                try
                {
                    await _hostLifetime.StopAsync(token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

                if (exceptions.Count > 0)
                {
                    if (exceptions.Count == 1)
                    {
                        // Rethrow if it's a single error
                        Exception singleException = exceptions[0];
                        _logger.StoppedWithException(singleException);
                        ExceptionDispatchInfo.Capture(singleException).Throw();
                    }
                    else
                    {
                        var ex = new AggregateException("One or more hosted services failed to stop.", exceptions);
                        _logger.StoppedWithException(ex);
                        throw ex;
                    }
                }
            }

            _logger.Stopped();
        }

        public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

        public async ValueTask DisposeAsync()
        {
            // The user didn't change the ContentRootFileProvider instance, we can dispose it
            if (ReferenceEquals(_hostEnvironment.ContentRootFileProvider, _defaultProvider))
            {
                // Dispose the content provider
                await DisposeAsync(_hostEnvironment.ContentRootFileProvider).ConfigureAwait(false);
            }
            else
            {
                // In the rare case that the user replaced the ContentRootFileProvider, dispose it and the one
                // we originally created
                await DisposeAsync(_hostEnvironment.ContentRootFileProvider).ConfigureAwait(false);
                await DisposeAsync(_defaultProvider).ConfigureAwait(false);
            }

            // Dispose the service provider
            await DisposeAsync(Services).ConfigureAwait(false);

            static async ValueTask DisposeAsync(object o)
            {
                switch (o)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
        }
    }
}
