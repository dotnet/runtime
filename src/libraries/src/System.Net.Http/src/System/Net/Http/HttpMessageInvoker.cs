// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class HttpMessageInvoker : IDisposable
    {
        private volatile bool _disposed;
        private bool _disposeHandler;
        private HttpMessageHandler _handler;

        public HttpMessageInvoker(HttpMessageHandler handler)
            : this(handler, true)
        {
        }

        public HttpMessageInvoker(HttpMessageHandler handler, bool disposeHandler)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, handler);

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (NetEventSource.IsEnabled) NetEventSource.Associate(this, handler);

            _handler = handler;
            _disposeHandler = disposeHandler;

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            CheckDisposed();

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, request);

            Task<HttpResponseMessage> task = _handler.SendAsync(request, cancellationToken);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this, task);

            return task;
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
