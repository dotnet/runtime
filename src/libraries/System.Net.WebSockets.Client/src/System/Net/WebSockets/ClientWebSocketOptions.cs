// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.WebSockets
{
    public sealed class ClientWebSocketOptions
    {
        private bool _isReadOnly; // After ConnectAsync is called the options cannot be modified.
        private TimeSpan _keepAliveInterval = WebSocketDefaults.DefaultClientKeepAliveInterval;
        private TimeSpan _keepAliveTimeout = WebSocketDefaults.DefaultKeepAliveTimeout;
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
        private Version _version = Net.HttpVersion.Version11;
        private HttpVersionPolicy _versionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        private bool _collectHttpResponseDetails;

        internal bool AreCompatibleWithCustomInvoker() =>
            !UseDefaultCredentials &&
            Credentials is null &&
            (_clientCertificates?.Count ?? 0) == 0 &&
            RemoteCertificateValidationCallback is null &&
            Cookies is null &&
            (Proxy is null || Proxy == WebSocketHandle.DefaultWebProxy.Instance);

        internal ClientWebSocketOptions() { } // prevent external instantiation

        #region HTTP Settings

        /// <summary>Gets or sets the HTTP version to use.</summary>
        /// <value>The HTTP message version. The default value is <c>1.1</c>.</value>
        public Version HttpVersion
        {
            get => _version;
            [UnsupportedOSPlatform("browser")]
            set
            {
                ThrowIfReadOnly();
                ArgumentNullException.ThrowIfNull(value);
                _version = value;
            }
        }

        /// <summary>Gets or sets the policy that determines how <see cref="ClientWebSocketOptions.HttpVersion" /> is interpreted and how the final HTTP version is negotiated with the server.</summary>
        /// <value>The version policy used when the HTTP connection is established.</value>
        public HttpVersionPolicy HttpVersionPolicy
        {
            get => _versionPolicy;
            [UnsupportedOSPlatform("browser")]
            set
            {
                ThrowIfReadOnly();
                _versionPolicy = value;
            }
        }

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
                ArgumentNullException.ThrowIfNull(value);
                _clientCertificates = value;
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

        /// <summary>
        /// The keep-alive interval to use, or <see cref="TimeSpan.Zero"/> or <see cref="Timeout.InfiniteTimeSpan"/> to disable keep-alives.
        /// If <see cref="ClientWebSocketOptions.KeepAliveTimeout"/> is set, then PING messages are sent and peer's PONG responses are expected, otherwise,
        /// unsolicited PONG messages are used as a keep-alive heartbeat.
        /// The default is <see cref="WebSocket.DefaultKeepAliveInterval"/> (typically 30 seconds).
        /// </summary>
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
        /// The timeout to use when waiting for the peer's PONG in response to us sending a PING; or <see cref="TimeSpan.Zero"/> or
        /// <see cref="Timeout.InfiniteTimeSpan"/> to disable waiting for peer's response, and use an unsolicited PONG as a Keep-Alive heartbeat instead.
        /// The default is <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </summary>
        [UnsupportedOSPlatform("browser")]
        public TimeSpan KeepAliveTimeout
        {
            get => _keepAliveTimeout;
            set
            {
                ThrowIfReadOnly();
                if (value != Timeout.InfiniteTimeSpan && value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall,
                        Timeout.InfiniteTimeSpan.ToString()));
                }
                _keepAliveTimeout = value;
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

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(receiveBufferSize);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sendBufferSize);

            _receiveBufferSize = receiveBufferSize;
            _buffer = null;
        }

        [UnsupportedOSPlatform("browser")]
        public void SetBuffer(int receiveBufferSize, int sendBufferSize, ArraySegment<byte> buffer)
        {
            ThrowIfReadOnly();

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(receiveBufferSize);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sendBufferSize);

            WebSocketValidate.ValidateArraySegment(buffer, nameof(buffer));
            ArgumentOutOfRangeException.ThrowIfZero(buffer.Count, nameof(buffer));

            _receiveBufferSize = receiveBufferSize;
            _buffer = buffer;
        }

        /// <summary>
        /// Indicates whether <see cref="ClientWebSocket.HttpStatusCode" /> and <see cref="ClientWebSocket.HttpResponseHeaders" /> should be set when establishing the connection.
        /// </summary>
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public bool CollectHttpResponseDetails
        {
            get => _collectHttpResponseDetails;
            set
            {
                ThrowIfReadOnly();
                _collectHttpResponseDetails = value;
            }
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
