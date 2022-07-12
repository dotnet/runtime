// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Threading;

namespace System.Net.Quic;

/// <summary>
/// Shared options for both client (outbound) and server (inbound) <see cref="QuicConnection" />.
/// </summary>
public abstract class QuicConnectionOptions
{
    /// <summary>
    /// Prevent sub-classing by code outside of this assembly.
    /// </summary>
    internal QuicConnectionOptions()
    { }

    /// <summary>
    /// Limit on the number of bidirectional streams the remote peer connection can create on an open connection.
    /// Default 0 for client and 100 for server connection.
    /// </summary>
    public int MaxInboundBidirectionalStreams { get; set; }

    /// <summary>
    /// Limit on the number of unidirectional streams the remote peer connection can create on an open connection.
    /// Default 0 for client and 10 for server connection.
    /// </summary>
    public int MaxInboundUnidirectionalStreams { get; set; }

    /// <summary>
    /// Idle timeout for connections, after which the connection will be closed.
    /// Default <see cref="TimeSpan.Zero"/> means underlying implementation default idle timeout.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Error code used when the stream needs to abort read or write side of the stream internally.
    /// </summary>
    // QUIC doesn't allow negative value: https://www.rfc-editor.org/rfc/rfc9000.html#integer-encoding
    // We can safely use this to distinguish if user provided value during validation.
    public long DefaultStreamErrorCode { get; set; } = -1;

    /// <summary>
    /// Error code used for <see cref="QuicConnection.CloseAsync(long, Threading.CancellationToken)"/> when the connection gets disposed.
    /// To use different close error code, call  <see cref="QuicConnection.CloseAsync(long, Threading.CancellationToken)"/> explicitly before disposing.
    /// </summary>
    // QUIC doesn't allow negative value: https://www.rfc-editor.org/rfc/rfc9000.html#integer-encoding
    // We can safely use this to distinguish if user provided value during validation.
    public long DefaultCloseErrorCode { get; set; } = -1;

    /// <summary>
    /// Validates the options and potentially sets platform specific defaults.
    /// </summary>
    /// <param name="argumentName">Name of the from the caller.</param>
    internal virtual void Validate(string argumentName)
    {
        if (MaxInboundUnidirectionalStreams < 0 || MaxInboundBidirectionalStreams > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException($"{nameof(QuicConnectionOptions.MaxInboundBidirectionalStreams)} should be within [0, {ushort.Max}) range.", argumentName);
        }
        if (MaxInboundUnidirectionalStreams < 0 || MaxInboundUnidirectionalStreams > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException($"{nameof(QuicConnectionOptions.MaxInboundUnidirectionalStreams)} should be within [0, {ushort.Max}) range.", argumentName);
        }
        if (IdleTimeout < TimeSpan.Zero && IdleTimeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(QuicConnectionOptions.IdleTimeout), SR.net_quic_timeout_use_gt_zero);
        }
        if (DefaultStreamErrorCode < 0 || DefaultStreamErrorCode > QuicDefaults.MaxErrorCodeValue)
        {
            throw new ArgumentOutOfRangeException($"{nameof(QuicConnectionOptions.DefaultStreamErrorCode)} should be within [0, {QuicDefaults.MaxErrorCodeValue}) range.", argumentName);
        }
        if (DefaultCloseErrorCode < 0 || DefaultCloseErrorCode > QuicDefaults.MaxErrorCodeValue)
        {
            throw new ArgumentOutOfRangeException($"{nameof(QuicConnectionOptions.DefaultCloseErrorCode)} should be within [0, {QuicDefaults.MaxErrorCodeValue}) range.", argumentName);
        }
    }
}

/// <summary>
/// Options for client (outbound) <see cref="QuicConnection" />.
/// </summary>
public sealed class QuicClientConnectionOptions : QuicConnectionOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuicClientConnectionOptions"/> class.
    /// </summary>
    public QuicClientConnectionOptions()
    {
        MaxInboundBidirectionalStreams = QuicDefaults.DefaultClientMaxInboundBidirectionalStreams;
        MaxInboundUnidirectionalStreams = QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams;
    }

    /// <summary>
    /// Client authentication options to use when establishing a new connection.
    /// </summary>
    public SslClientAuthenticationOptions ClientAuthenticationOptions { get; set; } = null!;

    /// <summary>
    /// The remote endpoint to connect to. May be both <see cref="DnsEndPoint"/>, which will get resolved to an IP before connecting, or directly <see cref="IPEndPoint"/>.
    /// </summary>
    public EndPoint RemoteEndPoint { get; set; } = null!;

    /// <summary>
    /// The optional local endpoint that will be bound to.
    /// </summary>
    public IPEndPoint? LocalEndPoint { get; set; }

    /// <summary>
    /// Validates the options and potentially sets platform specific defaults.
    /// </summary>
    /// <param name="argumentName">Name of the from the caller.</param>
    internal override void Validate(string argumentName)
    {
        base.Validate(argumentName);

        // The content of ClientAuthenticationOptions gets validate in MsQuicConfiguration.Create.
        if (ClientAuthenticationOptions is null)
        {
            throw new ArgumentNullException($"'{nameof(QuicClientConnectionOptions.ClientAuthenticationOptions)}' must be specified to open the connection.", argumentName);
        }
        if (RemoteEndPoint is null)
        {
            throw new ArgumentNullException($"'{nameof(QuicClientConnectionOptions.RemoteEndPoint)}' must be specified to open the connection.", argumentName);
        }
    }
}

/// <summary>
/// Options for server (inbound) <see cref="QuicConnection" />. Provided by <see cref="QuicListenerOptions.ConnectionOptionsCallback"/>.
/// </summary>
public sealed class QuicServerConnectionOptions : QuicConnectionOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuicClientConnectionOptions"/> class.
    /// </summary>
    public QuicServerConnectionOptions()
    {
        MaxInboundBidirectionalStreams = QuicDefaults.DefaultServerMaxInboundBidirectionalStreams;
        MaxInboundUnidirectionalStreams = QuicDefaults.DefaultServerMaxInboundUnidirectionalStreams;
    }

    /// <summary>
    /// Server authentication options to use when accepting a new connection.
    /// </summary>
    public SslServerAuthenticationOptions ServerAuthenticationOptions { get; set; } = null!;

    /// <summary>
    /// Validates the options and potentially sets platform specific defaults.
    /// </summary>
    /// <param name="argumentName">Name of the from the caller.</param>
    internal override void Validate(string argumentName)
    {
        base.Validate(argumentName);

        // The content of ServerAuthenticationOptions gets validate in MsQuicConfiguration.Create.
        if (ServerAuthenticationOptions is null)
        {
            throw new ArgumentNullException($"'{nameof(QuicServerConnectionOptions.ServerAuthenticationOptions)}' must be specified to accept the connection.", argumentName);
        }
    }
}
