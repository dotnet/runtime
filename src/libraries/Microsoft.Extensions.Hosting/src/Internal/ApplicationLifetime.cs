// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Internal
{
    /// <summary>
    /// Allows consumers to perform cleanup during a graceful shutdown.
    /// </summary>
    [DebuggerDisplay("ApplicationStarted = {ApplicationStarted.IsCancellationRequested}, " +
        "ApplicationStopping = {ApplicationStopping.IsCancellationRequested}, " +
        "ApplicationStopped = {ApplicationStopped.IsCancellationRequested}")]
#pragma warning disable CS0618 // Type or member is obsolete
    public class ApplicationLifetime : IApplicationLifetime, IHostApplicationLifetime
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly CancellationTokenSource _startedSource = new CancellationTokenSource();
        private readonly CancellationTokenSource _stoppingSource = new CancellationTokenSource();
        private readonly CancellationTokenSource _stoppedSource = new CancellationTokenSource();
        private readonly ILogger<ApplicationLifetime> _logger;

        /// <summary>
        /// Initializes an <see cref="ApplicationLifetime"/> instance using the specified logger.
        /// </summary>
        /// <param name="logger">The logger to initialize this instance with.</param>
        public ApplicationLifetime(ILogger<ApplicationLifetime> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public CancellationToken ApplicationStarted => _startedSource.Token;

        /// <inheritdoc />
        public CancellationToken ApplicationStopping => _stoppingSource.Token;

        /// <inheritdoc />
        public CancellationToken ApplicationStopped => _stoppedSource.Token;

        /// <summary>
        /// Triggers <see cref="ApplicationStopping" /> and blocks until it completes.
        /// </summary>
        public void StopApplication()
        {
            // Lock on CTS to synchronize multiple calls to StopApplication. This guarantees that the first call
            // to StopApplication and its callbacks run to completion before subsequent calls to StopApplication,
            // which will no-op since the first call already requested cancellation, get a chance to execute.
            lock (_stoppingSource)
            {
                try
                {
                    _stoppingSource.Cancel();
                }
                catch (Exception ex)
                {
                    _logger.ApplicationError(LoggerEventIds.ApplicationStoppingException,
                                             "An error occurred stopping the application",
                                             ex);
                }
            }
        }

        /// <summary>
        /// Triggers <see cref="ApplicationStarted" /> and blocks until it completes.
        /// </summary>
        public void NotifyStarted()
        {
            try
            {
                _startedSource.Cancel();
            }
            catch (Exception ex)
            {
                _logger.ApplicationError(LoggerEventIds.ApplicationStartupException,
                                         "An error occurred starting the application",
                                         ex);
            }
        }

        /// <summary>
        /// Triggers <see cref="ApplicationStopped" /> and blocks until it completes.
        /// </summary>
        public void NotifyStopped()
        {
            try
            {
                _stoppedSource.Cancel();
            }
            catch (Exception ex)
            {
                _logger.ApplicationError(LoggerEventIds.ApplicationStoppedException,
                                         "An error occurred stopping the application",
                                         ex);
            }
        }
    }
}
