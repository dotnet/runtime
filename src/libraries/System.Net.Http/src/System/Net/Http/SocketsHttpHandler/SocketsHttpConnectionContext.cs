// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace System.Net.Http
{
    /// <summary>
    /// Represents the context passed to the ConnectCallback for a SocketsHttpHandler instance.
    /// </summary>
    public sealed class SocketsHttpConnectionContext
    {
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly HttpRequestMessage _initialRequestMessage;
        private readonly long _connectionId;

        internal SocketsHttpConnectionContext(DnsEndPoint dnsEndPoint, HttpRequestMessage initialRequestMessage, long connectionId)
        {
            _dnsEndPoint = dnsEndPoint;
            _initialRequestMessage = initialRequestMessage;
            _connectionId = connectionId;
        }

        /// <summary>
        /// The DnsEndPoint to be used by the ConnectCallback to establish the connection.
        /// </summary>
        public DnsEndPoint DnsEndPoint => _dnsEndPoint;

        /// <summary>
        /// The initial HttpRequestMessage that is causing the connection to be created.
        /// </summary>
        public HttpRequestMessage InitialRequestMessage => _initialRequestMessage;

        /// <summary>
        /// The identifier that will be assigned to the connection being established. This matches the connection id
        /// reported through <see cref="EventSource"/> telemetry, and the
        /// <see cref="SocketsHttpConnectionEvictionContext.ConnectionId"/> passed to
        /// <see cref="SocketsHttpHandler.ShouldEvictConnection"/>. It can be used to associate caller state (for
        /// example, the resolved address used) with the connection so it can be recovered when deciding on eviction.
        /// </summary>
        [Experimental(Experimentals.SocketsHttpHandlerConnectionEvictionDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public long ConnectionId => _connectionId;
    }
}
