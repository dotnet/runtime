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
        private IEnumerable<IHostedLifecycleService>? _hostedLifecycleServices;
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

        /// <summary>
        /// Order:
        ///  IHostLifetime.WaitForStartAsync
        ///  IHostedLifecycleService.StartingAsync
        ///  IHostedService.Start
        ///  IHostedLifecycleService.StartedAsync
        ///  IHostApplicationLifetime.ApplicationStarted
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.Starting();

            CancellationTokenSource? cts = null;
            CancellationTokenSource linkedCts;
            if (_options.StartupTimeout != Timeout.InfiniteTimeSpan)
            {
                cts = new CancellationTokenSource(_options.StartupTimeout);
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken, _applicationLifetime.ApplicationStopping);
            }
            else
            {
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _applicationLifetime.ApplicationStopping);
            }

            using (cts)
            using (linkedCts)
            {
                CancellationToken token = linkedCts.Token;

                // This may not catch exceptions.
                await _hostLifetime.WaitForStartAsync(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                List<Exception> exceptions = new();
                _hostedServices = Services.GetRequiredService<IEnumerable<IHostedService>>();
                _hostedLifecycleServices = GetHostLifecycles(_hostedServices);

                // Run IHostedService.Start either concurrently or serially.
                if (_options.ServicesStartConcurrently)
                {
                    if (_hostedLifecycleServices is not null)
                    {
                        await CallLifeCycle_Starting(_hostedLifecycleServices, token, exceptions).ConfigureAwait(false);
                    }

                    await CallLifeCycle_Start(_hostedServices, token, exceptions).ConfigureAwait(false);

                    if (_hostedLifecycleServices is not null)
                    {
                        await CallLifeCycle_Started(_hostedLifecycleServices, token, exceptions).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (_hostedLifecycleServices is not null)
                    {
                        foreach (IHostedLifecycleService hostedService in _hostedLifecycleServices)
                        {
                            try
                            {
                                await hostedService.StartingAsync(token).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                                break;
                            }
                        }
                    }

                    foreach (IHostedService hostedService in _hostedServices)
                    {
                        try
                        {
                            await StartAndTryToExecuteAsync(hostedService, token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                            break;
                        }
                    }

                    if (_hostedLifecycleServices is not null)
                    {
                        foreach (IHostedLifecycleService hostedService in _hostedLifecycleServices)
                        {
                            try
                            {
                                await hostedService.StartedAsync(token).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                                break;
                            }
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
                // This catches all exceptions and does not re-throw.
                _applicationLifetime.NotifyStarted();
            }

            _logger.Started();
        }

        private async Task StartAndTryToExecuteAsync(IHostedService service, CancellationToken token)
        {
            await service.StartAsync(token).ConfigureAwait(false);

            if (service is BackgroundService backgroundService)
            {
                _ = TryExecuteBackgroundServiceAsync(backgroundService);
            }
        }

        private async Task TryExecuteBackgroundServiceAsync(BackgroundService backgroundService)
        {
            // backgroundService.ExecuteTask may not be set (e.g. if the derived class doesn't call base.StartAsync)
            Task? backgroundTask = backgroundService.ExecuteTask;
            if (backgroundTask is null)
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

                    // This catches all exceptions and does not re-throw.
                    _applicationLifetime.StopApplication();
                }
            }
        }

        /// <summary>
        /// Order:
        ///  IHostedLifecycleService.StoppingAsync
        ///  IHostApplicationLifetime.ApplicationStopping
        ///  IHostedService.Stop
        ///  IHostedLifecycleService.StoppedAsync
        ///  IHostApplicationLifetime.ApplicationStopped
        ///  IHostLifetime.StopAsync
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _stopCalled = true;
            _logger.Stopping();

            CancellationTokenSource? cts = null;
            CancellationTokenSource linkedCts;
            if (_options.ShutdownTimeout != Timeout.InfiniteTimeSpan)
            {
                cts = new CancellationTokenSource(_options.ShutdownTimeout);
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            }
            else
            {
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            using (cts)
            using (linkedCts)
            {
                CancellationToken token = linkedCts.Token;
                List<Exception> exceptions = new();

                if (_hostedServices is null)
                {
                    // Trigger IHostApplicationLifetime.ApplicationStopping even if StartAsync wasn't called.
                    _applicationLifetime.StopApplication();
                }
                else
                {
                    // Ensure hosted services are stopped in LIFO order
                    IEnumerable<IHostedService> reversedServices = _hostedServices.Reverse();
                    IEnumerable<IHostedLifecycleService>? reversedLifetimeServices = _hostedLifecycleServices?.Reverse();

                    if (_options.ServicesStopConcurrently)
                    {
                        if (reversedLifetimeServices is not null)
                        {
                            await CallLifeCycle_Stopping(reversedLifetimeServices, token, exceptions).ConfigureAwait(false);
                        }

                        // Trigger IHostApplicationLifetime.ApplicationStopping.
                        // This catches all exceptions and does not re-throw.
                        _applicationLifetime.StopApplication();

                        await CallLifeCycle_Stop(reversedServices, token, exceptions).ConfigureAwait(false);

                        if (reversedLifetimeServices is not null)
                        {
                            await CallLifeCycle_Stopped(reversedLifetimeServices, token, exceptions).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (reversedLifetimeServices is not null)
                        {
                            foreach (IHostedLifecycleService hostedService in reversedLifetimeServices)
                            {
                                try
                                {
                                    await hostedService.StoppingAsync(token).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    exceptions.Add(ex);
                                }
                            }
                        }

                        // Trigger IHostApplicationLifetime.ApplicationStopping.
                        // This catches all exceptions and does not re-throw.
                        _applicationLifetime.StopApplication();

                        foreach (IHostedService hostedService in reversedServices)
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

                        if (reversedLifetimeServices is not null)
                        {
                            foreach (IHostedLifecycleService hostedService in reversedLifetimeServices)
                            {
                                try
                                {
                                    await hostedService.StoppedAsync(token).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    exceptions.Add(ex);
                                }
                            }
                        }
                    }
                }

                // Fire IHostApplicationLifetime.Stopped
                // This catches all exceptions and does not re-throw.
                _applicationLifetime.NotifyStopped();

                // This may not catch exceptions, so we do it here.
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

        /// <summary>
        /// Call <see cref="IHostedLifecycleService.StartingAsync(CancellationToken)"/>.
        /// The beginning synchronous portions of the implementations are run serially in registration order for
        /// performance since it is common to return Task.Completed as a noop.
        /// Any subsequent asynchronous portions are grouped together run concurrently.
        /// </summary>
        private static async Task CallLifeCycle_Starting(IEnumerable<IHostedLifecycleService> services, CancellationToken token, List<Exception> exceptions)
        {
            List<Task>? tasks = null;

            foreach (IHostedLifecycleService service in services)
            {
                Task task;
                try
                {
                    task = service.StartingAsync(token);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex); // Log exception from sync method.
                    continue;
                }

                if (task.IsCompleted)
                {
                    if (task.Exception is not null)
                    {
                        exceptions.Add(task.Exception); // Log exception from async method.
                    }
                }
                else
                {
                    tasks ??= new();
                    tasks.Add(Task.Run(async () => await task.ConfigureAwait(false), token));
                }
            }

            if (tasks is not null)
            {
                Task groupedTasks = Task.WhenAll(tasks);

                try
                {
                    await groupedTasks.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.AddRange(groupedTasks.Exception?.InnerExceptions ?? new[] { ex }.AsEnumerable());
                }
            }
        }

        /// <summary>
        /// Call <see cref="IHostedService.StartAsync(CancellationToken)"/>.
        /// The beginning synchronous portions of the implementations are run serially in registration order for
        /// performance since it is common to return Task.Completed as a noop.
        /// Any subsequent asynchronous portions are grouped together run concurrently.
        /// </summary>
        private static async Task CallLifeCycle_Start(IEnumerable<IHostedService> services, CancellationToken token, List<Exception> exceptions)
        {
            List<Task>? tasks = null;

            foreach (IHostedService service in services)
            {
                Task task;
                try
                {
                    task = service.StartAsync(token);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex); // Log exception from sync method.
                    continue;
                }

                if (task.IsCompleted)
                {
                    if (task.Exception is not null)
                    {
                        exceptions.Add(task.Exception); // Log exception from async method.
                    }
                }
                else
                {
                    tasks ??= new();
                    tasks.Add(Task.Run(async () => await task.ConfigureAwait(false), token));
                }
            }

            if (tasks is not null)
            {
                Task groupedTasks = Task.WhenAll(tasks);

                try
                {
                    await groupedTasks.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.AddRange(groupedTasks.Exception?.InnerExceptions ?? new[] { ex }.AsEnumerable());
                }
            }
        }

        /// <summary>
        /// Call <see cref="IHostedLifecycleService.StartedAsync(CancellationToken)"/>.
        /// The beginning synchronous portions of the implementations are run serially in registration order for
        /// performance since it is common to return Task.Completed as a noop.
        /// Any subsequent asynchronous portions are grouped together run concurrently.
        /// </summary>
        private static async Task CallLifeCycle_Started(IEnumerable<IHostedLifecycleService> services, CancellationToken token, List<Exception> exceptions)
        {
            List<Task>? tasks = null;

            foreach (IHostedLifecycleService service in services)
            {
                Task task;
                try
                {
                    task = service.StartedAsync(token);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex); // Log exception from sync method.
                    continue;
                }

                if (task.IsCompleted)
                {
                    if (task.Exception is not null)
                    {
                        exceptions.Add(task.Exception); // Log exception from async method.
                    }
                }
                else
                {
                    tasks ??= new();
                    tasks.Add(Task.Run(async () => await task.ConfigureAwait(false), token));
                }

            }

            if (tasks is not null)
            {
                Task groupedTasks = Task.WhenAll(tasks);

                try
                {
                    await groupedTasks.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.AddRange(groupedTasks.Exception?.InnerExceptions ?? new[] { ex }.AsEnumerable());
                }
            }
        }

        /// <summary>
        /// Call <see cref="IHostedLifecycleService.StoppingAsync(CancellationToken)"/>.
        /// The beginning synchronous portions of the implementations are run serially in registration order for
        /// performance since it is common to return Task.Completed as a noop.
        /// Any subsequent asynchronous portions are grouped together run concurrently.
        /// </summary>
        private static async Task CallLifeCycle_Stopping(IEnumerable<IHostedLifecycleService> services, CancellationToken token, List<Exception> exceptions)
        {
            List<Task>? tasks = null;

            foreach (IHostedLifecycleService service in services)
            {
                Task task;
                try
                {
                    task = service.StoppingAsync(token);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex); // Log exception from sync method.
                    continue;
                }

                if (task.IsCompleted)
                {
                    if (task.Exception is not null)
                    {
                        exceptions.Add(task.Exception); // Log exception from async method.
                    }
                }
                else
                {
                    tasks ??= new();
                    tasks.Add(Task.Run(async () => await task.ConfigureAwait(false), token));
                }
            }

            if (tasks is not null)
            {
                Task groupedTasks = Task.WhenAll(tasks);

                try
                {
                    await groupedTasks.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.AddRange(groupedTasks.Exception?.InnerExceptions ?? new[] { ex }.AsEnumerable());
                }
            }
        }

        /// <summary>
        /// Call <see cref="IHostedService.StopAsync(CancellationToken)"/>.
        /// The beginning synchronous portions of the implementations are run serially in registration order for
        /// performance since it is common to return Task.Completed as a noop.
        /// Any subsequent asynchronous portions are grouped together run concurrently.
        /// </summary>
        private static async Task CallLifeCycle_Stop(IEnumerable<IHostedService> services, CancellationToken token, List<Exception> exceptions)
        {
            List<Task>? tasks = null;

            foreach (IHostedService service in services)
            {
                Task task;
                try
                {
                    task = service.StopAsync(token);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex); // Log exception from sync method.
                    continue;
                }

                if (task.IsCompleted)
                {
                    if (task.Exception is not null)
                    {
                        exceptions.Add(task.Exception); // Log exception from async method.
                    }
                }
                else
                {
                    tasks ??= new();
                    tasks.Add(Task.Run(async () => await task.ConfigureAwait(false), token));
                }
            }

            if (tasks is not null)
            {
                Task groupedTasks = Task.WhenAll(tasks);

                try
                {
                    await groupedTasks.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.AddRange(groupedTasks.Exception?.InnerExceptions ?? new[] { ex }.AsEnumerable());
                }
            }
        }

        /// <summary>
        /// Call <see cref="IHostedLifecycleService.StoppedAsync(CancellationToken)"/>.
        /// The beginning synchronous portions of the implementations are run serially in registration order for
        /// performance since it is common to return Task.Completed as a noop.
        /// Any subsequent asynchronous portions are grouped together run concurrently.
        /// </summary>
        private static async Task CallLifeCycle_Stopped(IEnumerable<IHostedLifecycleService> services, CancellationToken token, List<Exception> exceptions)
        {
            List<Task>? tasks = null;

            foreach (IHostedLifecycleService service in services)
            {
                Task task;
                try
                {
                    task = service.StoppedAsync(token);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex); // Log exception from sync method.
                    continue;
                }

                if (task.IsCompleted)
                {
                    if (task.Exception is not null)
                    {
                        exceptions.Add(task.Exception); // Log exception from async method.
                    }
                }
                else
                {
                    tasks ??= new();
                    tasks.Add(Task.Run(async () => await task.ConfigureAwait(false), token));
                }
            }

            if (tasks is not null)
            {
                Task groupedTasks = Task.WhenAll(tasks);

                try
                {
                    await groupedTasks.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.AddRange(groupedTasks.Exception?.InnerExceptions ?? new[] { ex }.AsEnumerable());
                }
            }
        }

        private static List<IHostedLifecycleService>? GetHostLifecycles(IEnumerable<IHostedService> hostedServices)
        {
            List<IHostedLifecycleService>? _result = null;

            foreach (IHostedService hostedService in hostedServices)
            {
                if (hostedService is IHostedLifecycleService service)
                {
                    _result ??= new List<IHostedLifecycleService>();
                    _result.Add(service);
                }
            }

            return _result;
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
