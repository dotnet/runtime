// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public abstract class DelegatingHandler : HttpMessageHandler
    {
        private HttpMessageHandler _innerHandler;
        private volatile bool _operationStarted = false;
        private volatile bool _disposed = false;

        public HttpMessageHandler InnerHandler
        {
            get
            {
                return _innerHandler;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                CheckDisposedOrStarted();

                if (NetEventSource.IsEnabled) NetEventSource.Associate(this, value);
                _innerHandler = value;
            }
        }

        protected DelegatingHandler()
        {
        }

        protected DelegatingHandler(HttpMessageHandler innerHandler)
        {
            InnerHandler = innerHandler;
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), SR.net_http_handler_norequest);
            }
            SetOperationStarted();
            return _innerHandler.SendAsync(request, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                if (_innerHandler != null)
                {
                    _innerHandler.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        private void CheckDisposedOrStarted()
        {
            CheckDisposed();
            if (_operationStarted)
            {
                throw new InvalidOperationException(SR.net_http_operation_started);
            }
        }

        private void SetOperationStarted()
        {
            CheckDisposed();
            if (_innerHandler == null)
            {
                throw new InvalidOperationException(SR.net_http_handler_not_assigned);
            }
            // This method flags the handler instances as "active". I.e. we executed at least one request (or are
            // in the process of doing so). This information is used to lock-down all property setters. Once a
            // Send/SendAsync operation started, no property can be changed.
            if (!_operationStarted)
            {
                _operationStarted = true;
            }
        }
    }
}
