// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.Internal
{
    internal class Host : IHost, IAsyncDisposable
    {
        private readonly ILogger<Host> _logger;
        private readonly IHostLifetime _hostLifetime;
        private readonly ApplicationLifetime _applicationLifetime;
        private readonly HostOptions _options;
        private IEnumerable<IHostedService> _hostedServices;

        public Host(IServiceProvider services, IHostApplicationLifetime applicationLifetime, ILogger<Host> logger,
            IHostLifetime hostLifetime, IOptions<HostOptions> options)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            _applicationLifetime = (applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime))) as ApplicationLifetime;
            if (_applicationLifetime is null)
            {
                throw new ArgumentException("Replacing IHostApplicationLifetime is not supported.", nameof(applicationLifetime));
            }
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public IServiceProvider Services { get; }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.Starting();

            using var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _applicationLifetime.ApplicationStopping);
            var combinedCancellationToken = combinedCancellationTokenSource.Token;

            await _hostLifetime.WaitForStartAsync(combinedCancellationToken);

            combinedCancellationToken.ThrowIfCancellationRequested();
            _hostedServices = Services.GetService<IEnumerable<IHostedService>>();

            foreach (var hostedService in _hostedServices)
            {
                // Fire IHostedService.Start
                await hostedService.StartAsync(combinedCancellationToken).ConfigureAwait(false);
            }

            // Fire IHostApplicationLifetime.Started
            _applicationLifetime.NotifyStarted();

            _logger.Started();
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.Stopping();

            using (var cts = new CancellationTokenSource(_options.ShutdownTimeout))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
            {
                var token = linkedCts.Token;
                // Trigger IHostApplicationLifetime.ApplicationStopping
                _applicationLifetime.StopApplication();

                IList<Exception> exceptions = new List<Exception>();
                if (_hostedServices != null) // Started?
                {
                    foreach (var hostedService in _hostedServices.Reverse())
                    {
                        token.ThrowIfCancellationRequested();
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

                token.ThrowIfCancellationRequested();
                await _hostLifetime.StopAsync(token);

                // Fire IHostApplicationLifetime.Stopped
                _applicationLifetime.NotifyStopped();

                if (exceptions.Count > 0)
                {
                    var ex = new AggregateException("One or more hosted services failed to stop.", exceptions);
                    _logger.StoppedWithException(ex);
                    throw ex;
                }
            }

            _logger.Stopped();
        }

        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

        public async ValueTask DisposeAsync()
        {
            switch (Services)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }
}
