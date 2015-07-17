// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.WebSockets
{
    public sealed class ClientWebSocketOptions
    {
        private bool _isReadOnly; // After ConnectAsync is called the options cannot be modified.
        private readonly List<string> _requestedSubProtocols;
        private readonly WebHeaderCollection _requestHeaders;
        private TimeSpan _keepAliveInterval;
        private ICredentials _credentials;
        private IWebProxy _proxy;
        private X509CertificateCollection _clientCertificates;
        private CookieContainer _cookies;

        internal ClientWebSocketOptions()
        {
            _requestedSubProtocols = new List<string>();
            _requestHeaders = new WebHeaderCollection();
        }

        #region HTTP Settings

        // Note that some headers are restricted like Host.
        public void SetRequestHeader(string headerName, string headerValue)
        {
            ThrowIfReadOnly();

            // WebHeaderCollection performs validation of headerName/headerValue.
            _requestHeaders[headerName] = headerValue;
        }

        internal WebHeaderCollection RequestHeaders { get { return _requestHeaders; } }

        public ICredentials Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                ThrowIfReadOnly();
                _credentials = value;
            }
        }

        public IWebProxy Proxy
        {
            get
            {
                return _proxy;
            }
            set
            {
                ThrowIfReadOnly();
                _proxy = value;
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This collection will be handed off directly to HttpWebRequest.")]
        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (_clientCertificates == null)
                {
                    _clientCertificates = new X509CertificateCollection();
                }
                return _clientCertificates;
            }
            set
            {
                ThrowIfReadOnly();
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _clientCertificates = value;
            }
        }

        public CookieContainer Cookies
        {
            get
            {
                return _cookies;
            }
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
            foreach (string item in _requestedSubProtocols)
            {
                if (string.Equals(item, subProtocol, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(SR.Format(SR.net_WebSockets_NoDuplicateProtocol, subProtocol),
                        "subProtocol");
                }
            }
            _requestedSubProtocols.Add(subProtocol);
        }

        public TimeSpan KeepAliveInterval
        {
            get
            {
                return _keepAliveInterval;
            }
            set
            {
                ThrowIfReadOnly();
                if (value != Timeout.InfiniteTimeSpan && value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", value,
                        SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall,
                        Timeout.InfiniteTimeSpan.ToString()));
                }
                _keepAliveInterval = value;
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
