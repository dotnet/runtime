// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.WebSockets
{
    public sealed class ClientWebSocketOptions
    {
        private bool _isReadOnly; // After ConnectAsync is called the options cannot be modified.
        private TimeSpan _keepAliveInterval = WebSocket.DefaultKeepAliveInterval;
        private bool _useDefaultCredentials;
        private ICredentials? _credentials;
        private IWebProxy? _proxy;
        private CookieContainer? _cookies;
        private int _receiveBufferSize = 0x1000;
        private ArraySegment<byte>? _buffer;
        private RemoteCertificateValidationCallback? _remoteCertificateValidationCallback;

        internal X509CertificateCollection? _clientCertificates;
        internal WebHeaderCollection? _requestHeaders;
        internal List<string>? _requestedSubProtocols;

        internal ClientWebSocketOptions() { } // prevent external instantiation

        #region HTTP Settings

        // Note that some headers are restricted like Host.
        public void SetRequestHeader(string headerName, string? headerValue)
        {
            ThrowIfReadOnly();

            // WebHeaderCollection performs validation of headerName/headerValue.
            RequestHeaders.Set(headerName, headerValue);
        }

        internal WebHeaderCollection RequestHeaders => _requestHeaders ??= new WebHeaderCollection();

        internal List<string> RequestedSubProtocols => _requestedSubProtocols ??= new List<string>();

        public bool UseDefaultCredentials
        {
            get => _useDefaultCredentials;
            set
            {
                ThrowIfReadOnly();
                _useDefaultCredentials = value;
            }
        }

        public ICredentials? Credentials
        {
            get => _credentials;
            set
            {
                ThrowIfReadOnly();
                _credentials = value;
            }
        }

        public IWebProxy? Proxy
        {
            get => _proxy;
            set
            {
                ThrowIfReadOnly();
                _proxy = value;
            }
        }

        public X509CertificateCollection ClientCertificates
        {
            get => _clientCertificates ??= new X509CertificateCollection();
            set
            {
                ThrowIfReadOnly();
                _clientCertificates = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public RemoteCertificateValidationCallback? RemoteCertificateValidationCallback
        {
            get => _remoteCertificateValidationCallback;
            set
            {
                ThrowIfReadOnly();
                _remoteCertificateValidationCallback = value;
            }
        }

        public CookieContainer? Cookies
        {
            get => _cookies;
            set
            {
                ThrowIfReadOnly();
                _cookies = value;
            }
        }

        #endregion HTTP Settings

        #region WebSocket Settings

        public void AddSubProtocol(string subProtocol)
        {
            ThrowIfReadOnly();
            WebSocketValidate.ValidateSubprotocol(subProtocol);

            // Duplicates not allowed.
            List<string> subprotocols = RequestedSubProtocols; // force initialization of the list
            foreach (string item in subprotocols)
            {
                if (string.Equals(item, subProtocol, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(SR.Format(SR.net_WebSockets_NoDuplicateProtocol, subProtocol), nameof(subProtocol));
                }
            }
            subprotocols.Add(subProtocol);
        }

        public TimeSpan KeepAliveInterval
        {
            get => _keepAliveInterval;
            set
            {
                ThrowIfReadOnly();
                if (value != Timeout.InfiniteTimeSpan && value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall,
                        Timeout.InfiniteTimeSpan.ToString()));
                }
                _keepAliveInterval = value;
            }
        }

        internal int ReceiveBufferSize => _receiveBufferSize;
        internal ArraySegment<byte>? Buffer => _buffer;

        public void SetBuffer(int receiveBufferSize, int sendBufferSize)
        {
            ThrowIfReadOnly();

            if (receiveBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(receiveBufferSize), receiveBufferSize, SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
            }
            if (sendBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sendBufferSize), sendBufferSize, SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
            }

            _receiveBufferSize = receiveBufferSize;
            _buffer = null;
        }

        public void SetBuffer(int receiveBufferSize, int sendBufferSize, ArraySegment<byte> buffer)
        {
            ThrowIfReadOnly();

            if (receiveBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(receiveBufferSize), receiveBufferSize, SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
            }
            if (sendBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sendBufferSize), sendBufferSize, SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
            }

            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));
            if (buffer.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer));
            }

            _receiveBufferSize = receiveBufferSize;
            _buffer = buffer;
        }

        #endregion WebSocket settings

        #region Helpers

        internal void SetToReadOnly()
        {
            Debug.Assert(!_isReadOnly, "Already set");
            _isReadOnly = true;
        }

        private void ThrowIfReadOnly()
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.net_WebSockets_AlreadyStarted);
            }
        }

        #endregion Helpers
    }
}
