// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;

namespace System.Net.Http
{
    /// <summary>
    /// Represents the context passed to the PlaintextStreamFilter for a SocketsHttpHandler instance.
    /// </summary>
    public sealed class SocketsHttpPlaintextStreamFilterContext
    {
        private readonly Stream _plaintextStream;
        private readonly Version _negotiatedHttpVersion;
        private readonly HttpRequestMessage _initialRequestMessage;
        private readonly long _connectionId;

        internal SocketsHttpPlaintextStreamFilterContext(Stream plaintextStream, Version negotiatedHttpVersion, HttpRequestMessage initialRequestMessage, long connectionId)
        {
            _plaintextStream = plaintextStream;
            _negotiatedHttpVersion = negotiatedHttpVersion;
            _initialRequestMessage = initialRequestMessage;
            _connectionId = connectionId;
        }

        /// <summary>
        /// The plaintext Stream that will be used for HTTP protocol requests and responses.
        /// </summary>
        public Stream PlaintextStream => _plaintextStream;

        /// <summary>
        /// The version of HTTP in use for this stream.
        /// </summary>
        public Version NegotiatedHttpVersion => _negotiatedHttpVersion;

        /// <summary>
        /// The initial HttpRequestMessage that is causing the stream to be used.
        /// </summary>
        public HttpRequestMessage InitialRequestMessage => _initialRequestMessage;

        /// <summary>
        /// The identifier of the connection whose stream is being filtered. This matches the connection id reported
        /// through <see cref="EventSource"/> telemetry for that connection. For a direct connection it also matches the
        /// <see cref="SocketsHttpConnectionContext.ConnectionId"/> surfaced to
        /// <see cref="SocketsHttpHandler.ConnectCallback"/>, the
        /// <see cref="SocketsHttpConnectionEvictionContext.ConnectionId"/> passed to
        /// <see cref="SocketsHttpHandler.ShouldEvictConnection"/>, and the <see cref="HttpRequestMessage.ConnectionId"/>
        /// stamped on requests sent over the connection. It can be used to associate caller state with the connection
        /// and to correlate it with the requests it serves.
        /// </summary>
        /// <remarks>
        /// For an HTTP CONNECT proxy tunnel the filter runs once per hop: on the CONNECT hop this is the transport
        /// connection's id (the one the <see cref="SocketsHttpHandler.ConnectCallback"/> observed), and on the
        /// tunneled hop it is the tunneled connection's id (the one passed to
        /// <see cref="SocketsHttpHandler.ShouldEvictConnection"/> and stamped on requests).
        /// </remarks>
        [Experimental(Experimentals.SocketsHttpHandlerExperimentalDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public long ConnectionId => _connectionId;
    }
}
