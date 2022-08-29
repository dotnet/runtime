// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.WebSockets
{
    public sealed class ClientWebSocketOptions
    {
        private bool _isReadOnly; // After ConnectAsync is called the options cannot be modified.
        private List<string>? _requestedSubProtocols;

        internal ClientWebSocketOptions()
        { }

        #region HTTP Settings

        [UnsupportedOSPlatform("browser")]
        // Note that some headers are restricted like Host.
        public void SetRequestHeader(string headerName, string headerValue)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public bool UseDefaultCredentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public Version HttpVersion
        {
            get => Net.HttpVersion.Version11;
            [UnsupportedOSPlatform("browser")]
            set => throw new PlatformNotSupportedException();
        }

        public System.Net.Http.HttpVersionPolicy HttpVersionPolicy
        {
            get => HttpVersionPolicy.RequestVersionOrLower;
            [UnsupportedOSPlatform("browser")]
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public System.Net.ICredentials Credentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public System.Net.IWebProxy Proxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public X509CertificateCollection ClientCertificates
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public System.Net.Security.RemoteCertificateValidationCallback RemoteCertificateValidationCallback
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public System.Net.CookieContainer Cookies
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public bool CollectHttpResponseDetails
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        #endregion HTTP Settings

        #region WebSocket Settings

        public void AddSubProtocol(string subProtocol)
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.net_WebSockets_AlreadyStarted);
            }
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

        internal List<string> RequestedSubProtocols => _requestedSubProtocols ??= new List<string>();

        [UnsupportedOSPlatform("browser")]
        public TimeSpan KeepAliveInterval
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public WebSocketDeflateOptions? DangerousDeflateOptions
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public void SetBuffer(int receiveBufferSize, int sendBufferSize)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public void SetBuffer(int receiveBufferSize, int sendBufferSize, ArraySegment<byte> buffer)
        {
            throw new PlatformNotSupportedException();
        }

        #endregion WebSocket settings

        #region Helpers

        internal void SetToReadOnly()
        {
            Debug.Assert(!_isReadOnly, "Already set");
            _isReadOnly = true;
        }

        #endregion Helpers
    }
}
