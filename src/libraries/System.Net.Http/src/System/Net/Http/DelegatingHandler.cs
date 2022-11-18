// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    public abstract class DelegatingHandler : HttpMessageHandler
    {
        private HttpMessageHandler? _innerHandler;
        private volatile bool _operationStarted;
        private volatile bool _disposed;

        [DisallowNull]
        public HttpMessageHandler? InnerHandler
        {
            get
            {
                return _innerHandler;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                CheckDisposedOrStarted();

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Associate(this, value);
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

        protected internal override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            SetOperationStarted();
            return _innerHandler!.Send(request, cancellationToken);
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            SetOperationStarted();
            return _innerHandler!.SendAsync(request, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _innerHandler?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void CheckDisposedOrStarted()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_operationStarted)
            {
                throw new InvalidOperationException(SR.net_http_operation_started);
            }
        }

        private void SetOperationStarted()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

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
