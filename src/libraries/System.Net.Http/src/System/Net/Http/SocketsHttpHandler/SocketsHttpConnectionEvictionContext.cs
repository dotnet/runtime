// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace System.Net.Http
{
    /// <summary>
    /// Represents the context passed to <see cref="SocketsHttpHandler.ShouldEvictConnection"/> when a pooled
    /// connection is being considered for eviction.
    /// </summary>
    /// <remarks>
    /// The instance is only valid for the duration of the callback invocation; it must not be cached or used after
    /// the callback returns. <see cref="Age"/> reflects the elapsed time at the moment it is read.
    /// </remarks>
    [Experimental(Experimentals.SocketsHttpHandlerConnectionEvictionDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed class SocketsHttpConnectionEvictionContext
    {
        private readonly long _creationTickCount;

        internal SocketsHttpConnectionEvictionContext(
            DnsEndPoint dnsEndPoint,
            IPEndPoint? remoteEndPoint,
            long connectionId,
            Version httpVersion,
            long creationTickCount)
        {
            DnsEndPoint = dnsEndPoint;
            RemoteEndPoint = remoteEndPoint;
            ConnectionId = connectionId;
            HttpVersion = httpVersion;
            _creationTickCount = creationTickCount;
        }

        /// <summary>
        /// Gets the <see cref="Net.DnsEndPoint"/> identifying the origin (host and port) the connection targets.
        /// </summary>
        /// <remarks>
        /// This is the logical destination the connection was created for, not necessarily the host the
        /// transport is physically connected to (for example, when a proxy is in use). Use it together with
        /// <see cref="RemoteEndPoint"/> to decide whether the connection still points at a desired address.
        /// </remarks>
        public DnsEndPoint DnsEndPoint { get; }

        /// <summary>
        /// Gets the remote <see cref="IPEndPoint"/> the connection's transport is connected to, when available.
        /// </summary>
        /// <remarks>
        /// This is <see langword="null"/> when the remote endpoint is not known, for example when a custom
        /// <see cref="SocketsHttpHandler.ConnectCallback"/> returned a stream that is not backed by a socket.
        /// </remarks>
        public IPEndPoint? RemoteEndPoint { get; }

        /// <summary>
        /// Gets the identifier assigned to the connection. This matches the connection id reported through
        /// <see cref="EventSource"/> telemetry, and the
        /// <see cref="SocketsHttpConnectionContext.ConnectionId"/> seen by a custom
        /// <see cref="SocketsHttpHandler.ConnectCallback"/>, allowing the decision to be correlated with state the
        /// caller associated with the connection at creation time.
        /// </summary>
        public long ConnectionId { get; }

        /// <summary>
        /// Gets the HTTP version negotiated for the connection (for example, 1.1, 2.0, or 3.0).
        /// </summary>
        public Version HttpVersion { get; }

        /// <summary>
        /// Gets the amount of time that has elapsed since the connection was established.
        /// </summary>
        public TimeSpan Age => TimeSpan.FromMilliseconds(Environment.TickCount64 - _creationTickCount);
    }
}
