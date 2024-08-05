// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging
{
    /// <summary>
    /// Handles logging of the lifecycle for an HTTP request.
    /// </summary>
    public class LoggingHttpMessageHandler : DelegatingHandler
    {
        private readonly ILogger _logger;
        private readonly HttpClientFactoryOptions? _options;

        private static readonly Func<string, bool> _shouldNotRedactHeaderValue = (header) => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingHttpMessageHandler"/> class with a specified logger.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to log to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null"/>.</exception>
        public LoggingHttpMessageHandler(ILogger logger)
        {
            ThrowHelper.ThrowIfNull(logger);

            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingHttpMessageHandler"/> class with a specified logger and options.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to log to.</param>
        /// <param name="options">The <see cref="HttpClientFactoryOptions"/> used to configure the <see cref="LoggingHttpMessageHandler"/> instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="logger"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
        public LoggingHttpMessageHandler(ILogger logger, HttpClientFactoryOptions options)
        {
            ThrowHelper.ThrowIfNull(logger);
            ThrowHelper.ThrowIfNull(options);

            _logger = logger;
            _options = options;
        }

        private Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, bool useAsync, CancellationToken cancellationToken)
        {
            ThrowHelper.ThrowIfNull(request);
            return Core(request, useAsync, cancellationToken);

            async Task<HttpResponseMessage> Core(HttpRequestMessage request, bool useAsync, CancellationToken cancellationToken)
            {
                Func<string, bool> shouldRedactHeaderValue = _options?.ShouldRedactHeaderValue ?? _shouldNotRedactHeaderValue;

                // Not using a scope here because we always expect this to be at the end of the pipeline, thus there's
                // not really anything to surround.
                _logger.LogRequestStart(request, shouldRedactHeaderValue);
                var stopwatch = ValueStopwatch.StartNew();
                HttpResponseMessage response = useAsync
                    ? await base.SendAsync(request, cancellationToken).ConfigureAwait(false)
#if NET
                    : base.Send(request, cancellationToken);
#else
                    : throw new NotImplementedException("Unreachable code");
#endif
                _logger.LogRequestEnd(response, stopwatch.GetElapsedTime(), shouldRedactHeaderValue);

                return response;
            }
        }

        /// <inheritdoc />
        /// <remarks>Logs the request to and response from the sent <see cref="HttpRequestMessage"/>.</remarks>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => SendCoreAsync(request, useAsync: true, cancellationToken);

#if NET
        /// <inheritdoc />
        /// <remarks>Logs the request to and response from the sent <see cref="HttpRequestMessage"/>.</remarks>
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            => SendCoreAsync(request, useAsync: false, cancellationToken).GetAwaiter().GetResult();
#endif
    }
}
