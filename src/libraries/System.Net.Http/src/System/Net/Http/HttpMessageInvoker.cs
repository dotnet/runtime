// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class HttpMessageInvoker : IDisposable
    {
        private volatile bool _disposed;
        private readonly bool _disposeHandler;
        private readonly HttpMessageHandler _handler;
        private Meter? _meter;
        private HttpMetrics? _metrics;

#pragma warning disable CS3003 // Type is not CLS-compliant
        public Meter Meter
        {
            // TODO: Should the Meter and HttpMetrics be static and shared by default?
            get => _meter ??= new Meter("System.Net.Http");
            set
            {
                // TODO: Check that HttpMessageInvoker hasn't been started.
                ArgumentNullException.ThrowIfNull(value);
                if (value.Name != "System.Net.Http")
                {
                    throw new ArgumentException("Meter name must be 'System.Net.Http'.");
                }
                _meter = value;
            }
        }
#pragma warning restore CS3003 // Type is not CLS-compliant

        public HttpMessageInvoker(HttpMessageHandler handler)
            : this(handler, true)
        {
        }

        public HttpMessageInvoker(HttpMessageHandler handler, bool disposeHandler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(this, handler);

            _handler = handler;
            _disposeHandler = disposeHandler;
        }

        [MemberNotNull(nameof(_metrics))]
        private void EnsureMetrics()
        {
            _metrics ??= new HttpMetrics(Meter);
        }

        [UnsupportedOSPlatformAttribute("browser")]
        public virtual HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            ObjectDisposedException.ThrowIf(_disposed, this);

            EnsureMetrics();

            if (ShouldSendWithTelemetry(request))
            {
                long startTimestamp = Stopwatch.GetTimestamp();

                HttpTelemetry.Log.RequestStart(request);
                _metrics.RequestStart(request);

                HttpResponseMessage? response = null;
                try
                {
                    response = _handler.Send(request, cancellationToken);
                    return response;
                }
                catch (Exception ex) when (LogRequestFailed(ex, telemetryStarted: true))
                {
                    // Unreachable as LogRequestFailed will return false
                    throw;
                }
                finally
                {
                    HttpTelemetry.Log.RequestStop(response);
                    _metrics.RequestStop(request, response, startTimestamp, Stopwatch.GetTimestamp());
                }
            }
            else
            {
                return _handler.Send(request, cancellationToken);
            }
        }

        public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            ObjectDisposedException.ThrowIf(_disposed, this);

            EnsureMetrics();

            if (ShouldSendWithTelemetry(request))
            {
                return SendAsyncWithTelemetry(_handler, request, _metrics, cancellationToken);
            }

            return _handler.SendAsync(request, cancellationToken);

            static async Task<HttpResponseMessage> SendAsyncWithTelemetry(HttpMessageHandler handler, HttpRequestMessage request, HttpMetrics metrics, CancellationToken cancellationToken)
            {
                long startTimestamp = Stopwatch.GetTimestamp();

                HttpTelemetry.Log.RequestStart(request);
                metrics.RequestStart(request);

                HttpResponseMessage? response = null;
                try
                {
                    response = await handler.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    return response;
                }
                catch (Exception ex) when (LogRequestFailed(ex, telemetryStarted: true))
                {
                    // Unreachable as LogRequestFailed will return false
                    throw;
                }
                finally
                {
                    HttpTelemetry.Log.RequestStop(response);
                    metrics.RequestStop(request, response, startTimestamp, Stopwatch.GetTimestamp());
                }
            }
        }

        private bool ShouldSendWithTelemetry(HttpRequestMessage request) =>
            (HttpTelemetry.Log.IsEnabled() || _metrics!.RequestCountersEnabled()) &&
            !request.WasSentByHttpClient() &&
            request.RequestUri is Uri requestUri &&
            requestUri.IsAbsoluteUri;

        internal static bool LogRequestFailed(Exception exception, bool telemetryStarted)
        {
            if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
            {
                HttpTelemetry.Log.RequestFailed(exception);
            }
            return false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                if (_disposeHandler)
                {
                    _handler.Dispose();
                }
            }
        }
    }
}
