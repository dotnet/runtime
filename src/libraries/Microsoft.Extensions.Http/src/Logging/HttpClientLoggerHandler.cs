// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Http.Logging
{
    internal sealed class HttpClientLoggerHandler : DelegatingHandler
    {
        private readonly IHttpClientLogger _httpClientLogger;

        public HttpClientLoggerHandler(IHttpClientLogger httpClientLogger)
        {
            ThrowHelper.ThrowIfNull(httpClientLogger);

            _httpClientLogger = httpClientLogger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ThrowHelper.ThrowIfNull(request);

            var stopwatch = ValueStopwatch.StartNew();
            HttpResponseMessage? response = null;

            object? state = await _httpClientLogger.LogRequestStartAsync(request, cancellationToken).ConfigureAwait(false);

            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                await _httpClientLogger.LogRequestStopAsync(state, request, response, stopwatch.GetElapsedTime(), cancellationToken).ConfigureAwait(false);
                return response;
            }
            catch (Exception exception)
            {
                await _httpClientLogger.LogRequestFailedAsync(state, request, response, exception, stopwatch.GetElapsedTime(), cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

#if NET5_0_OR_GREATER
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ThrowHelper.ThrowIfNull(request);

            var stopwatch = ValueStopwatch.StartNew();
            HttpResponseMessage? response = null;

            ValueTask<object?> logStartTask = _httpClientLogger.LogRequestStartAsync(request, cancellationToken);
            object? state = logStartTask.IsCompletedSuccessfully
                ? logStartTask.Result
                : logStartTask.AsTask().GetAwaiter().GetResult();

            try
            {
                response = base.Send(request, cancellationToken);

                ValueTask logStopTask = _httpClientLogger.LogRequestStopAsync(state, request, response, stopwatch.GetElapsedTime(), cancellationToken);
                if (!logStopTask.IsCompletedSuccessfully)
                {
                    logStopTask.AsTask().GetAwaiter().GetResult();
                }

                return response;
            }
            catch (Exception exception)
            {
                ValueTask logFailedTask = _httpClientLogger.LogRequestFailedAsync(state, request, response, exception, stopwatch.GetElapsedTime(), cancellationToken);
                if (!logFailedTask.IsCompletedSuccessfully)
                {
                    logFailedTask.AsTask().GetAwaiter().GetResult();
                }

                throw;
            }
        }
#endif
    }
}
