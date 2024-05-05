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
        private readonly IHttpClientAsyncLogger? _httpClientAsyncLogger;

        public HttpClientLoggerHandler(IHttpClientLogger httpClientLogger)
        {
            ThrowHelper.ThrowIfNull(httpClientLogger);

            _httpClientLogger = httpClientLogger;
            _httpClientAsyncLogger = httpClientLogger as IHttpClientAsyncLogger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ThrowHelper.ThrowIfNull(request);

            var stopwatch = ValueStopwatch.StartNew();
            HttpResponseMessage? response = null;

            object? state = _httpClientAsyncLogger is not null
                ? await _httpClientAsyncLogger.LogRequestStartAsync(request, cancellationToken).ConfigureAwait(false)
                : _httpClientLogger.LogRequestStart(request);

            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (_httpClientAsyncLogger is not null)
                {
                    await _httpClientAsyncLogger.LogRequestStopAsync(state, request, response, stopwatch.GetElapsedTime(), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _httpClientLogger.LogRequestStop(state, request, response, stopwatch.GetElapsedTime());
                }
                return response;
            }
            catch (Exception exception)
            {
                if (_httpClientAsyncLogger is not null)
                {
                    await _httpClientAsyncLogger.LogRequestFailedAsync(state, request, response, exception, stopwatch.GetElapsedTime(), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _httpClientLogger.LogRequestFailed(state, request, response, exception, stopwatch.GetElapsedTime());
                }
                throw;
            }
        }

#if NET
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ThrowHelper.ThrowIfNull(request);

            var stopwatch = ValueStopwatch.StartNew();
            HttpResponseMessage? response = null;

            object? state = _httpClientLogger.LogRequestStart(request);

            try
            {
                response = base.Send(request, cancellationToken);

                _httpClientLogger.LogRequestStop(state, request, response, stopwatch.GetElapsedTime());

                return response;
            }
            catch (Exception exception)
            {
                _httpClientLogger.LogRequestFailed(state, request, response, exception, stopwatch.GetElapsedTime());
                throw;
            }
        }
#endif
    }
}
