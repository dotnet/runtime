// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.Versioning;
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

        [UnsupportedOSPlatform("browser")]
        // Note that some headers are restricted like Host.
        public void SetRequestHeader(string headerName, string? headerValue)
        {
            ThrowIfReadOnly();

            // WebHeaderCollection performs validation of headerName/headerValue.
            RequestHeaders.Set(headerName, headerValue);
        }

        internal WebHeaderCollection RequestHeaders => _requestHeaders ??= new WebHeaderCollection();

        internal List<string> RequestedSubProtocols => _requestedSubProtocols ??= new List<string>();

        [UnsupportedOSPlatform("browser")]
        public bool UseDefaultCredentials
        {
            get => _useDefaultCredentials;
            set
            {
                ThrowIfReadOnly();
                _useDefaultCredentials = value;
            }
        }

        [UnsupportedOSPlatform("browser")]
        public ICredentials? Credentials
        {
            get => _credentials;
            set
            {
                ThrowIfReadOnly();
                _credentials = value;
            }
        }

        [UnsupportedOSPlatform("browser")]
        public IWebProxy? Proxy
        {
            get => _proxy;
            set
            {
                ThrowIfReadOnly();
                _proxy = value;
            }
        }

        [UnsupportedOSPlatform("browser")]
        public X509CertificateCollection ClientCertificates
        {
            get => _clientCertificates ??= new X509CertificateCollection();
            set
            {
                ThrowIfReadOnly();
                _clientCertificates = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        [UnsupportedOSPlatform("browser")]
        public RemoteCertificateValidationCallback? RemoteCertificateValidationCallback
        {
            get => _remoteCertificateValidationCallback;
            set
            {
                ThrowIfReadOnly();
                _remoteCertificateValidationCallback = value;
            }
        }

        [UnsupportedOSPlatform("browser")]
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

        [UnsupportedOSPlatform("browser")]
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

        /// <summary>
        /// Gets or sets the options for the per-message-deflate extension.
        /// When present, the options are sent to the server during the handshake phase. If the server
        /// supports per-message-deflate and the options are accepted, the <see cref="WebSocket"/> instance
        /// will be created with compression enabled by default for all messages.<para />
        /// Be aware that enabling compression makes the application subject to CRIME/BREACH type of attacks.
        /// It is strongly advised to turn off compression when sending data containing secrets by
        /// specifying <see cref="WebSocketMessageFlags.DisableCompression" /> flag for such messages.
        /// </summary>
        [UnsupportedOSPlatform("browser")]
        public WebSocketDeflateOptions? DangerousDeflateOptions { get; set; }

        internal int ReceiveBufferSize => _receiveBufferSize;
        internal ArraySegment<byte>? Buffer => _buffer;

        [UnsupportedOSPlatform("browser")]
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

        [UnsupportedOSPlatform("browser")]
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
