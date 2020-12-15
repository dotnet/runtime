// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    /// <summary>
    /// Represents the context passed to the ConnectCallback for a SocketsHttpHandler instance.
    /// </summary>
    public sealed class SocketsHttpConnectionContext
    {
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly HttpRequestMessage _initialRequestMessage;

        internal SocketsHttpConnectionContext(DnsEndPoint dnsEndPoint, HttpRequestMessage initialRequestMessage)
        {
            _dnsEndPoint = dnsEndPoint;
            _initialRequestMessage = initialRequestMessage;
        }

        /// <summary>
        /// The DnsEndPoint to be used by the ConnectCallback to establish the connection.
        /// </summary>
        public DnsEndPoint DnsEndPoint => _dnsEndPoint;

        /// <summary>
        /// The initial HttpRequestMessage that is causing the connection to be created.
        /// </summary>
        public HttpRequestMessage InitialRequestMessage => _initialRequestMessage;
    }
}
