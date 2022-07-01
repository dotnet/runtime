// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;

namespace System.Net.Quic;

/// <summary>
/// Shared options for both client (outbound) and server (inbound) <see cref="QuicConnection" />.
/// </summary>
public abstract class QuicConnectionOptions
{
    /// <summary>
    /// Prevent sub-classing.
    /// </summary>
    internal QuicConnectionOptions()
    { }

    /// <summary>
    /// Limit on the number of bidirectional streams the remote peer connection can create on an open connection.
    /// Default 0 for client and 100 for server connection.
    /// </summary>
    public int MaxBidirectionalStreams { get; set; }

    /// <summary>
    /// Limit on the number of unidirectional streams the remote peer connection can create on an open connection.
    /// Default 0 for client and 10 for server connection.
    /// </summary>
    public int MaxUnidirectionalStreams { get; set; }

    /// <summary>
    /// Idle timeout for connections, after which the connection will be closed.
    /// Default <see cref="TimeSpan.Zero"/> means underlying implementation default idle timeout.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.Zero;
}

/// <summary>
/// Options for client (outbound) <see cref="QuicConnection" />.
/// </summary>
public sealed class QuicClientConnectionOptions : QuicConnectionOptions
{
    /// <summary>
    /// Client authentication options to use when establishing a new connection.
    /// </summary>
    public required SslClientAuthenticationOptions ClientAuthenticationOptions { get; set; }

    /// <summary>
    /// The remote endpoint to connect to. May be both <see cref="DnsEndPoint"/>, which will get resolved to an IP before connecting, or directly <see cref="IPEndPoint"/>.
    /// </summary>
    public required EndPoint RemoteEndPoint { get; set; }

    /// <summary>
    /// The optional local endpoint that will be bound to.
    /// </summary>
    public IPEndPoint? LocalEndPoint { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuicClientConnectionOptions"/> class.
    /// </summary>
    public QuicClientConnectionOptions()
    {
        MaxBidirectionalStreams = 0;
        MaxUnidirectionalStreams = 0;
    }
}

/// <summary>
/// Options for server (inbound) <see cref="QuicConnection" />. Provided by <see cref="QuicListenerOptions.ConnectionOptionsCallback"/>.
/// </summary>
public sealed class QuicServerConnectionOptions : QuicConnectionOptions
{
    /// <summary>
    /// Server authentication options to use when accepting a new connection.
    /// </summary>
    public required SslServerAuthenticationOptions ServerAuthenticationOptions { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuicClientConnectionOptions"/> class.
    /// </summary>
    public QuicServerConnectionOptions()
    {
        MaxBidirectionalStreams = 100;
        MaxUnidirectionalStreams = 10;
    }
}
