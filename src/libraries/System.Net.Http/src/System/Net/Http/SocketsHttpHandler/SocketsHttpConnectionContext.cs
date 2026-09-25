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
        /// reported through <see cref="EventSource"/> telemetry, the
        /// <see cref="SocketsHttpConnectionEvictionContext.ConnectionId"/> passed to
        /// <see cref="SocketsHttpHandler.ShouldEvictConnection"/>, and the
        /// <see cref="HttpRequestMessage.ConnectionId"/> stamped on requests sent over the connection. It can be used
        /// to associate caller state (for example, the resolved address used) with the connection and to correlate it
        /// with the requests it serves, so that state can be recovered later (for example when deciding on eviction).
        /// </summary>
        /// <remarks>
        /// When establishing the transport for an HTTP CONNECT proxy tunnel, this id identifies that transport
        /// connection to the proxy; the tunneled connection layered over it serves the requests and carries a distinct
        /// id, which is the one reported to <see cref="SocketsHttpHandler.ShouldEvictConnection"/> and stamped on those
        /// requests. A <see cref="SocketsHttpHandler.PlaintextStreamFilter"/> runs on each hop and surfaces both: this
        /// (transport) id on the CONNECT hop and the tunneled connection's id on the subsequent hop.
        /// </remarks>
        [Experimental(Experimentals.SocketsHttpHandlerExperimentalDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public long ConnectionId => _connectionId;
    }
}
