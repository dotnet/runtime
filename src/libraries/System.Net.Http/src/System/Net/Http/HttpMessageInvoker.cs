// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class HttpMessageInvoker : IDisposable
    {
        private volatile bool _disposed;
        private readonly bool _disposeHandler;
        private readonly HttpMessageHandler _handler;

        public HttpMessageInvoker(HttpMessageHandler handler)
            : this(handler, true)
        {
        }

        public HttpMessageInvoker(HttpMessageHandler handler, bool disposeHandler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(this, handler);

            _handler = handler;
            _disposeHandler = disposeHandler;
        }

        public virtual HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            CheckDisposed();

            if (HttpTelemetry.Log.IsEnabled() && !request.WasSentByHttpClient() && request.RequestUri != null)
            {
                HttpTelemetry.Log.RequestStart(request);

                try
                {
                    return _handler.Send(request, cancellationToken);
                }
                catch when (LogRequestFailed(telemetryStarted: true))
                {
                    // Unreachable as LogRequestFailed will return false
                    throw;
                }
                finally
                {
                    HttpTelemetry.Log.RequestStop();
                }
            }
            else
            {
                return _handler.Send(request, cancellationToken);
            }
        }

        public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            CheckDisposed();

            if (HttpTelemetry.Log.IsEnabled() && !request.WasSentByHttpClient() && request.RequestUri != null)
            {
                return SendAsyncWithTelemetry(_handler, request, cancellationToken);
            }

            return _handler.SendAsync(request, cancellationToken);

            static async Task<HttpResponseMessage> SendAsyncWithTelemetry(HttpMessageHandler handler, HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpTelemetry.Log.RequestStart(request);

                try
                {
                    return await handler.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch when (LogRequestFailed(telemetryStarted: true))
                {
                    // Unreachable as LogRequestFailed will return false
                    throw;
                }
                finally
                {
                    HttpTelemetry.Log.RequestStop();
                }
            }
        }

        internal static bool LogRequestFailed(bool telemetryStarted)
        {
            if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
            {
                HttpTelemetry.Log.RequestFailed();
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

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }
    }
}
