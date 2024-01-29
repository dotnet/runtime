// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Net.Quic;

/// <summary>
/// Collection of receive window sizes for <see cref="QuicConnection"/> as a whole and for individual <see cref="QuicStream"/> types.
/// </summary>
public sealed class QuicReceiveWindowSizes
{
    /// <summary>
    /// The initial flow-control window size for the connection.
    /// </summary>
    public int Connection { get; set; } = QuicDefaults.DefaultConnectionMaxData;

    /// <summary>
    /// The initial flow-control window size for locally initiated bidirectional streams.
    /// </summary>
    public int LocallyInitiatedBidirectionalStream { get; set; } = QuicDefaults.DefaultStreamMaxData;

    /// <summary>
    /// The initial flow-control window size for remotely initiated bidirectional streams.
    /// </summary>
    public int RemotelyInitiatedBidirectionalStream { get; set; } = QuicDefaults.DefaultStreamMaxData;

    /// <summary>
    /// The initial flow-control window size for (remotely initiated) unidirectional streams.
    /// </summary>
    public int UnidirectionalStream { get; set; } = QuicDefaults.DefaultStreamMaxData;

    internal void Validate(string argumentName)
    {
        ValidatePowerOf2(argumentName, Connection);
        ValidatePowerOf2(argumentName, LocallyInitiatedBidirectionalStream);
        ValidatePowerOf2(argumentName, RemotelyInitiatedBidirectionalStream);
        ValidatePowerOf2(argumentName, UnidirectionalStream);

        static void ValidatePowerOf2(string argumentName, int value, [CallerArgumentExpression(nameof(value))] string? propertyName = null)
        {
            if (value <= 0 || ((value - 1) & value) != 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, value, SR.Format(SR.net_quic_power_of_2, $"{nameof(QuicConnectionOptions.InitialReceiveWindowSizes)}.{propertyName}"));
            }
        }
    }
}

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
    /// The maximum number of concurrent bidirectional streams that the remote peer connection can create on an open connection.
    /// Default 0 for client and 100 for server connection.
    /// </summary>
    public int MaxInboundBidirectionalStreams { get; set; }

    /// <summary>
    /// The maximum number of concurrent unidirectional streams that the remote peer connection can create on an open connection.
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
    /// This property is mandatory and not setting it will result in validation error when establishing a connection.
    /// </summary>
    // QUIC doesn't allow negative value: https://www.rfc-editor.org/rfc/rfc9000.html#integer-encoding
    // We can safely use this to distinguish if user provided value during validation.
    public long DefaultStreamErrorCode { get; set; } = -1;

    /// <summary>
    /// Error code used for <see cref="QuicConnection.CloseAsync(long, Threading.CancellationToken)"/> when the connection gets disposed.
    /// This property is mandatory and not setting it will result in validation error when establishing a connection.
    /// </summary>
    /// <remarks>
    /// To use different close error code, call  <see cref="QuicConnection.CloseAsync(long, Threading.CancellationToken)"/> explicitly before disposing.
    /// </remarks>
    // QUIC doesn't allow negative value: https://www.rfc-editor.org/rfc/rfc9000.html#integer-encoding
    // We can safely use this to distinguish if user provided value during validation.
    public long DefaultCloseErrorCode { get; set; } = -1;

    internal QuicReceiveWindowSizes? _initialRecieveWindowSizes;

    /// <summary>
    /// The initial receive window sizes for the connection and individual stream types.
    /// </summary>
    public QuicReceiveWindowSizes InitialReceiveWindowSizes
    {
        get => _initialRecieveWindowSizes ??= new QuicReceiveWindowSizes();
        set => _initialRecieveWindowSizes = value;
    }

    /// <summary>
    /// The interval at which keep alive packets are sent on the connection.
    /// Value <see cref="TimeSpan.Zero"/> means underlying implementation default timeout.
    /// Default <see cref="Timeout.InfiniteTimeSpan"/> means never sending keep alive packets.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; } = Timeout.InfiniteTimeSpan;

    /// <summary>
    /// The upper bound on time when the handshake must complete. If the handshake does not
    /// complete in this time, the connection is aborted.
    /// Value <see cref="TimeSpan.Zero"/> means underlying implementation default timeout.
    /// Default timeout is 10 seconds.
    /// </summary>
    public TimeSpan HandshakeTimeout { get; set; } = QuicDefaults.HandshakeTimeout;

    /// <summary>
    /// Validates the options and potentially sets platform specific defaults.
    /// </summary>
    /// <param name="argumentName">Name of the from the caller.</param>
    internal virtual void Validate(string argumentName)
    {
        ValidateInRange(argumentName, MaxInboundBidirectionalStreams, ushort.MaxValue);
        ValidateInRange(argumentName, MaxInboundUnidirectionalStreams, ushort.MaxValue);
        ValidateTimespan(argumentName, IdleTimeout);
        ValidateTimespan(argumentName, KeepAliveInterval);
        ValidateInRange(argumentName, DefaultCloseErrorCode, QuicDefaults.MaxErrorCodeValue);
        ValidateInRange(argumentName, DefaultStreamErrorCode, QuicDefaults.MaxErrorCodeValue);
        ValidateTimespan(argumentName, HandshakeTimeout);

        _initialRecieveWindowSizes?.Validate(argumentName);

        static void ValidateInRange(string argumentName, long value, long max, [CallerArgumentExpression(nameof(value))] string? propertyName = null)
        {
            if (value < 0 || value > max)
            {
                throw new ArgumentOutOfRangeException(argumentName, value, SR.Format(SR.net_quic_in_range, propertyName, max));
            }
        }

        static void ValidateTimespan(string argumentName, TimeSpan value, [CallerArgumentExpression(nameof(value))] string? propertyName = null)
        {
            if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(argumentName, value, SR.Format(SR.net_quic_timeout_use_gt_zero, propertyName));
            }
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
        MaxInboundUnidirectionalStreams = QuicDefaults.DefaultClientMaxInboundUnidirectionalStreams;
    }

    /// <summary>
    /// Client authentication options to use when establishing a new connection.
    /// This property is mandatory and not setting it will result in validation error when establishing a connection.
    /// </summary>
    public SslClientAuthenticationOptions ClientAuthenticationOptions { get; set; } = null!;

    /// <summary>
    /// The remote endpoint to connect to. May be both <see cref="DnsEndPoint"/>, which will get resolved to an IP before connecting, or directly <see cref="IPEndPoint"/>.
    /// This property is mandatory and not setting it will result in validation error when establishing a connection.
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
        ValidateNotNull(argumentName, ClientAuthenticationOptions);
        ValidateNotNull(argumentName, RemoteEndPoint);

        static void ValidateNotNull(string argumentName, object value, [CallerArgumentExpression(nameof(value))] string? propertyName = null)
        {
            if (value is null)
            {
                throw new ArgumentNullException(argumentName, SR.Format(SR.net_quic_not_null_open_connection, propertyName));
            }
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
    /// This property is mandatory and not setting it will result in validation error when establishing a connection.
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
        ValidateNotNull(argumentName, ServerAuthenticationOptions);

        static void ValidateNotNull(string argumentName, object value, [CallerArgumentExpression(nameof(value))] string? propertyName = null)
        {
            if (value is null)
            {
                throw new ArgumentNullException(argumentName, SR.Format(SR.net_quic_not_null_accept_connection, propertyName));
            }
        }
    }
}
