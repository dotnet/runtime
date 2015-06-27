// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
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
            if (Logging.On) Logging.Enter(Logging.Http, this, ".ctor", handler);

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            if (Logging.On) Logging.Associate(Logging.Http, this, handler);

            _handler = handler;
            _disposeHandler = disposeHandler;

            if (Logging.On) Logging.Exit(Logging.Http, this, ".ctor", null);
        }

        public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            CheckDisposed();

            if (Logging.On)
                Logging.Enter(Logging.Http, this, "SendAsync",
    Logging.GetObjectLogHash(request) + ": " + request);

            Task<HttpResponseMessage> task = _handler.SendAsync(request, cancellationToken);

            if (Logging.On) Logging.Exit(Logging.Http, this, "SendAsync", task);

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
