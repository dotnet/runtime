// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Quic;
using static System.Net.Quic.MsQuicHelpers;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic;

/// <summary>
/// Represents a QUIC connection, see <see href="https://www.rfc-editor.org/rfc/rfc9000.html#name-connections">RFC 9000: Connections</see> for more details.
/// <see cref="QuicConnection" /> itself doesn't send or receive data but rather allows opening and/or accepting multiple <see cref="QuicStream" />.
/// </summary>
/// <remarks>
/// <see cref="QuicConnection" /> can either be accepted from <see cref="QuicListener.AcceptConnectionAsync(CancellationToken)" /> (inbound connection),
/// or create with a static method <see cref="QuicConnection.ConnectAsync(System.Net.Quic.QuicClientConnectionOptions, CancellationToken)" /> (outbound connection).
///
/// Each connection can then open outbound stream: <see cref="QuicConnection.OpenOutboundStreamAsync(QuicStreamType, CancellationToken)" />,
/// or accept an inbound stream: <see cref="QuicConnection.AcceptInboundStreamAsync(CancellationToken)" />.
///
/// After all the streams have been finished, connection should be properly closed with an application code: <see cref="CloseAsync(long, CancellationToken)" />.
/// If not, the connection will not send the peer information about being closed and the peer's connection will have to wait on its idle timeout.
/// </remarks>
public sealed partial class QuicConnection : IAsyncDisposable
{
    /// <summary>
    /// Returns <c>true</c> if QUIC is supported on the current machine and can be used; otherwise, <c>false</c>.
    /// </summary>
    /// <remarks>
    /// The current implementation depends on <see href="https://github.com/microsoft/msquic">MsQuic</see> native library, this property checks its presence (Linux machines).
    /// It also checks whether TLS 1.3, requirement for QUIC protocol, is available and enabled (Windows machines).
    /// </remarks>
    public static bool IsSupported => MsQuicApi.IsQuicSupported;

    /// <summary>
    /// Creates a new <see cref="QuicConnection"/> and connects it to the peer.
    /// </summary>
    /// <param name="options">Options for the connection.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes with the connected connection.</returns>
    public static async ValueTask<QuicConnection> ConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            throw new PlatformNotSupportedException(SR.SystemNetQuic_PlatformNotSupported);
        }

        QuicConnection connection = new QuicConnection();
        try
        {
            await connection.FinishConnectAsync(options, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
        return connection;
    }

    /// <summary>
    /// Handle to MsQuic connection object.
    /// </summary>
    private MsQuicContextSafeHandle _handle;

    /// <summary>
    /// Set to non-zero once disposed. Prevents double and/or concurrent disposal.
    /// </summary>
    private int _disposed;

    private readonly ValueTaskSource _connectedTcs = new ValueTaskSource();
    private readonly ValueTaskSource _shutdownTcs = new ValueTaskSource();

    private readonly Channel<QuicStream> _acceptQueue = Channel.CreateUnbounded<QuicStream>(new UnboundedChannelOptions()
    {
        SingleWriter = true
    });

    /// <summary>
    /// Holds options to validate peer certificate.
    /// Set up either in <see cref="FinishHandshakeAsync"/> for an inbound connection or in <see cref="FinishConnectAsync"/> for an outbound.
    /// </summary>
    private SslConnectionOptions _sslConnectionOptions;
    /// <summary>
    /// Holds MsQuic connection configuration.
    /// Set up either in <see cref="FinishHandshakeAsync"/> for an inbound connection or in <see cref="FinishConnectAsync"/> for an outbound.
    /// </summary>
    private MsQuicSafeHandle? _configuration;

    /// <summary>
    /// Set when SHUTDOWN_INITIATED_BY_PEER is received.
    /// </summary>
    private long _abortErrorCode = -1;

    /// <summary>
    /// Used by <see cref="AcceptInboundStreamAsync(CancellationToken)" /> to throw in case no stream can be opened from the peer.
    /// <c>true</c> when at least one of <see cref="QuicConnectionOptions.MaxInboundBidirectionalStreams" /> or <see cref="QuicConnectionOptions.MaxInboundUnidirectionalStreams" /> is greater than <c>0</c>.
    /// </summary>
    private bool _canAccept;

    // TODO: remove once/if https://github.com/microsoft/msquic/pull/2872 is merged
    internal sealed class State
    {
        public long AbortErrorCode = -1;
    }
    private State _state = new State();

    /// <summary>
    /// Set when CONNECTED is received or inside the constructor for an inbound connection from NEW_CONNECTION data.
    /// </summary>
    private IPEndPoint _remoteEndPoint = null!;
    /// <summary>
    /// Set when CONNECTED is received or inside the constructor for an inbound connection from NEW_CONNECTION data.
    /// </summary>
    private IPEndPoint _localEndPoint = null!;
    /// <summary>
    /// Keeps track whether <see cref="RemoteCertificate"/> has been accessed so that we know whether to dispose the certificate or not.
    /// </summary>
    private bool _remoteCertificateExposed;
    /// <summary>
    /// Set when PEER_CERTIFICATE_RECEIVED is received (before CONNECTED).
    /// For an outbound/client connection will always have the peer's (server) certificate; for an inbound/server one, only if the connection requested and the peer (client) provided one.
    /// </summary>
    private X509Certificate2? _remoteCertificate;
    /// <summary>
    /// Set when CONNECTED is received.
    /// </summary>
    private SslApplicationProtocol _negotiatedApplicationProtocol;

    /// <summary>
    /// The remote endpoint used for this connection.
    /// </summary>
    public IPEndPoint RemoteEndPoint => _remoteEndPoint;
    /// <summary>
    /// The local endpoint used for this connection.
    /// </summary>
    public IPEndPoint LocalEndPoint => _localEndPoint;

    /// <summary>
    /// The certificate provided by the peer.
    /// For an outbound/client connection will always have the peer's (server) certificate; for an inbound/server one, only if the connection requested and the peer (client) provided one.
    /// </summary>
    public X509Certificate? RemoteCertificate
    {
        get
        {
            _remoteCertificateExposed = true;
            return _remoteCertificate;
        }
    }

    /// <summary>
    /// Final, negotiated application protocol.
    /// </summary>
    public SslApplicationProtocol NegotiatedApplicationProtocol => _negotiatedApplicationProtocol;

    public override string ToString() => _handle.ToString();

    private unsafe QuicConnection()
    {
        GCHandle context = GCHandle.Alloc(this, GCHandleType.Weak);
        try
        {
            QUIC_HANDLE* handle;
            ThrowIfFailure(MsQuicApi.Api.ApiTable->ConnectionOpen(
                MsQuicApi.Api.Registration.QuicHandle,
                &NativeCallback,
                (void*)GCHandle.ToIntPtr(context),
                &handle));
            _handle = new MsQuicContextSafeHandle(handle, context, MsQuicApi.Api.ApiTable->ConnectionClose, SafeHandleType.Connection);
        }
        catch
        {
            context.Free();
            throw;
        }
    }

    internal unsafe QuicConnection(QUIC_HANDLE* handle, QUIC_NEW_CONNECTION_INFO* info)
    {
        GCHandle context = GCHandle.Alloc(this, GCHandleType.Weak);
        try
        {
            delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int> nativeCallback = &NativeCallback;
            MsQuicApi.Api.ApiTable->SetCallbackHandler(
                handle,
                nativeCallback,
                (void*)GCHandle.ToIntPtr(context));
            _handle = new MsQuicContextSafeHandle(handle, context, MsQuicApi.Api.ApiTable->ConnectionClose, SafeHandleType.Connection);
        }
        catch
        {
            context.Free();
            throw;
        }

        _remoteEndPoint = info->RemoteAddress->ToIPEndPoint();
        _localEndPoint = info->LocalAddress->ToIPEndPoint();
    }

    private async ValueTask FinishConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (_connectedTcs.TryInitialize(out ValueTask valueTask, this, cancellationToken))
        {
            _canAccept = options.MaxInboundBidirectionalStreams > 0 || options.MaxInboundUnidirectionalStreams > 0;

            if (!options.RemoteEndPoint.TryParse(out string? host, out IPAddress? address, out int port))
            {
                throw new ArgumentException($"Unsupported remote endpoint type '{options.RemoteEndPoint.GetType()}', expected IP or DNS endpoint.", nameof(options));
            }
            int addressFamily = QUIC_ADDRESS_FAMILY_UNSPEC;

            // RemoteEndPoint is either IPEndPoint or DnsEndPoint containing IPAddress string.
            // --> Set the IP directly, no name resolution needed.
            if (address is not null)
            {
                QuicAddr quicAddress = new IPEndPoint(address, port).ToQuicAddr();
                SetMsQuicParameter(_handle, QUIC_PARAM_CONN_REMOTE_ADDRESS, quicAddress);
            }
            // RemoteEndPoint is DnsEndPoint containing hostname that is different from requested SNI.
            // --> Resolve the hostname and set the IP directly, use requested SNI in ConnectionStart.
            else if (host is not null &&
                    !host.Equals(options.ClientAuthenticationOptions.TargetHost, StringComparison.InvariantCultureIgnoreCase))
            {
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(host!, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (addresses.Length == 0)
                {
                    throw new SocketException((int)SocketError.HostNotFound);
                }

                QuicAddr quicAddress = new IPEndPoint(addresses[0], port).ToQuicAddr();
                SetMsQuicParameter(_handle, QUIC_PARAM_CONN_REMOTE_ADDRESS, quicAddress);
            }
            // RemoteEndPoint is DnsEndPoint containing hostname that is the same as the requested SNI.
            // --> Let MsQuic resolve the hostname/SNI, give address family hint is specified in DnsEndPoint.
            else
            {
                if (options.RemoteEndPoint.AddressFamily == AddressFamily.InterNetwork)
                {
                    addressFamily = QUIC_ADDRESS_FAMILY_INET;
                }
                if (options.RemoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    addressFamily = QUIC_ADDRESS_FAMILY_INET6;
                }
            }

            if (options.LocalEndPoint is not null)
            {
                QuicAddr quicAddress = options.LocalEndPoint.ToQuicAddr();
                SetMsQuicParameter(_handle, QUIC_PARAM_CONN_LOCAL_ADDRESS, quicAddress);
            }

            _sslConnectionOptions = new SslConnectionOptions(
                this,
                isClient: true,
                options.ClientAuthenticationOptions.TargetHost,
                certificateRequired: true,
                options.ClientAuthenticationOptions.CertificateRevocationCheckMode,
                options.ClientAuthenticationOptions.RemoteCertificateValidationCallback);
            _configuration = MsQuicConfiguration.Create(options);

            IntPtr targetHostPtr = Marshal.StringToCoTaskMemUTF8(options.ClientAuthenticationOptions.TargetHost ?? host ?? address?.ToString());
            try
            {
                unsafe
                {
                    ThrowIfFailure(MsQuicApi.Api.ApiTable->ConnectionStart(
                        _handle.QuicHandle,
                        _configuration.QuicHandle,
                        (ushort)addressFamily,
                        (sbyte*)targetHostPtr,
                        (ushort)port));
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(targetHostPtr);
            }
        }

        await valueTask.ConfigureAwait(false);
    }

    internal ValueTask FinishHandshakeAsync(QuicServerConnectionOptions options, string? targetHost, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (_connectedTcs.TryInitialize(out ValueTask valueTask, this, cancellationToken))
        {
            _canAccept = options.MaxInboundBidirectionalStreams > 0 || options.MaxInboundUnidirectionalStreams > 0;

            _sslConnectionOptions = new SslConnectionOptions(
                this,
                isClient: false,
                targetHost: null,
                options.ServerAuthenticationOptions.ClientCertificateRequired,
                options.ServerAuthenticationOptions.CertificateRevocationCheckMode,
                options.ServerAuthenticationOptions.RemoteCertificateValidationCallback);
            _configuration = MsQuicConfiguration.Create(options, targetHost);

            unsafe
            {
                ThrowIfFailure(MsQuicApi.Api.ApiTable->ConnectionSetConfiguration(
                    _handle.QuicHandle,
                    _configuration.QuicHandle));
            }
        }

        return valueTask;
    }

    /// <summary>
    /// Create an outbound uni/bidirectional <see cref="QuicStream" />.
    /// In case the connection doesn't have any available stream capacity, i.e.: the peer limits the concurrent stream count,
    /// the operation will pend until the stream can be opened (other stream gets closed or peer increases the limit).
    /// </summary>
    /// <param name="type">The type of the stream, i.e. unidirectional or bidirectional.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes with the opened <see cref="QuicStream" />.</returns>
    public async ValueTask<QuicStream> OpenOutboundStreamAsync(QuicStreamType type, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        QuicStream stream = new QuicStream(new Implementations.MsQuic.MsQuicStream(_state, _handle, type));
        try
        {
            await stream.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
        return stream;
    }

    /// <summary>
    /// Accepts an inbound <see cref="QuicStream" />.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes with the accepted <see cref="QuicStream" />.</returns>
    public async ValueTask<QuicStream> AcceptInboundStreamAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (!_canAccept)
        {
            throw new InvalidOperationException(SR.net_quic_accept_not_allowed);
        }

        try
        {
            return await _acceptQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    /// <summary>
    /// Closes the connection with the application provided code, see <see href="https://www.rfc-editor.org/rfc/rfc9000.html#immediate-close">RFC 9000: Connection Termination</see> for more details.
    /// </summary>
    /// <remarks>
    /// Connection close is not graceful in regards to its streams, i.e.: calling <see cref="CloseAsync(long, CancellationToken)"/> will immediately abort all streams associated with this connection.
    /// Please make sure, that all streams have been closed and all their data consumed before calling this method;
    /// otherwise, all the data that were received but not consumed yet, will be lost.
    ///
    /// If <see cref="CloseAsync(long, CancellationToken)"/> is not called before <see cref="DisposeAsync">disposing</see> the connection, the connection will be closed silently.
    /// Meaning that the peer will not be informed about it and will eventually get its connection closed by idle timeout.
    /// </remarks>
    /// <param name="errorCode">Application provided code with the reason for closure.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes when the connection is closed.</returns>
    public unsafe ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (_shutdownTcs.TryInitialize(out ValueTask valueTask, this, cancellationToken))
        {
            MsQuicApi.Api.ApiTable->ConnectionShutdown(
                _handle.QuicHandle,
                QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE,
                (ulong)errorCode);
        }

        return valueTask;
    }

    private unsafe int HandleConnectionEvent(ref QUIC_CONNECTION_EVENT connectionEvent)
    {
        if (connectionEvent.Type == QUIC_CONNECTION_EVENT_TYPE.CONNECTED)
        {
            ref var data = ref connectionEvent.CONNECTED;

            _negotiatedApplicationProtocol = new SslApplicationProtocol(new Span<byte>(data.NegotiatedAlpn, data.NegotiatedAlpnLength).ToArray());

            QuicAddr remoteAddress = GetMsQuicParameter<QuicAddr>(_handle, QUIC_PARAM_CONN_REMOTE_ADDRESS);
            _remoteEndPoint = remoteAddress.ToIPEndPoint();

            QuicAddr localAddress = GetMsQuicParameter<QuicAddr>(_handle, QUIC_PARAM_CONN_LOCAL_ADDRESS);
            _localEndPoint = localAddress.ToIPEndPoint();

            _connectedTcs.TrySetResult();

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"{this} Connection connected {LocalEndPoint} -> {RemoteEndPoint}");
            }
            return QUIC_STATUS_SUCCESS;
        }
        if (connectionEvent.Type == QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_TRANSPORT)
        {
            ref var data = ref connectionEvent.SHUTDOWN_INITIATED_BY_TRANSPORT;
            _connectedTcs.TrySetException(new MsQuicException(data.Status));
            // To throw QuicConnectionAbortedException (instead of QuicOperationAbortedException) out of AcceptStreamAsync() since
            // it wasn't our side who shutdown the connection.
            // We should rather keep the Status and propagate it either in a different exception or as a different field of QuicConnectionAbortedException.
            // See: https://github.com/dotnet/runtime/issues/60133
            _abortErrorCode = 0;
            _state.AbortErrorCode = _abortErrorCode;
            _acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicConnectionAbortedException($"Connection shutdown by transport {MsQuicException.GetErrorCodeForStatus(data.Status)}", _abortErrorCode)));
            return QUIC_STATUS_SUCCESS;
        }
        if (connectionEvent.Type == QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_PEER)
        {
            ref var data = ref connectionEvent.SHUTDOWN_INITIATED_BY_PEER;
            _abortErrorCode = (long)data.ErrorCode;
            _state.AbortErrorCode = _abortErrorCode;
            _acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicConnectionAbortedException($"Connection shutdown by peer", _abortErrorCode)));
            return QUIC_STATUS_SUCCESS;
        }
        if (connectionEvent.Type == QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_COMPLETE)
        {
            _shutdownTcs.TrySetResult();
            _acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException()));
            return QUIC_STATUS_SUCCESS;
        }
        if (connectionEvent.Type == QUIC_CONNECTION_EVENT_TYPE.LOCAL_ADDRESS_CHANGED)
        {
            ref var data = ref connectionEvent.LOCAL_ADDRESS_CHANGED;
            _localEndPoint = data.Address->ToIPEndPoint();
            return QUIC_STATUS_SUCCESS;
        }
        if (connectionEvent.Type == QUIC_CONNECTION_EVENT_TYPE.PEER_ADDRESS_CHANGED)
        {
            ref var data = ref connectionEvent.PEER_ADDRESS_CHANGED;
            _remoteEndPoint = data.Address->ToIPEndPoint();
            return QUIC_STATUS_SUCCESS;
        }
        if (connectionEvent.Type == QUIC_CONNECTION_EVENT_TYPE.PEER_STREAM_STARTED)
        {
            ref var data = ref connectionEvent.PEER_STREAM_STARTED;
            QuicStream stream = new QuicStream(new Implementations.MsQuic.MsQuicStream(_state, _handle, data.Stream, data.Flags));
            if (!_acceptQueue.Writer.TryWrite(stream))
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(this, $"{this} Unable to enqueue incoming stream {stream}");
                }
                stream.Dispose();
                return QUIC_STATUS_SUCCESS;
            }
            return QUIC_STATUS_SUCCESS;
        }
        if (connectionEvent.Type == QUIC_CONNECTION_EVENT_TYPE.PEER_CERTIFICATE_RECEIVED)
        {
            ref var data = ref connectionEvent.PEER_CERTIFICATE_RECEIVED;
            try
            {
                return _sslConnectionOptions.ValidateCertificate((QUIC_BUFFER*)data.Certificate, (QUIC_BUFFER*)data.Chain, out _remoteCertificate);
            }
            catch (Exception ex)
            {
                _connectedTcs.TrySetException(ex);
                return QUIC_STATUS_HANDSHAKE_FAILURE;
            }
        }

        return QUIC_STATUS_SUCCESS;
    }

#pragma warning disable CS3016
    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
    private static unsafe int NativeCallback(QUIC_HANDLE* connection, void* context, QUIC_CONNECTION_EVENT* connectionEvent)
    {
        GCHandle stateHandle = GCHandle.FromIntPtr((IntPtr)context);

        // Check if the instance hasn't been collected.
        if (!stateHandle.IsAllocated || stateHandle.Target is not QuicConnection instance)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(null, $"Received event {connectionEvent->Type}");
            }
            return QUIC_STATUS_INVALID_STATE;
        }

        try
        {
            // Process the event.
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(instance, $"{instance} Received event {connectionEvent->Type}");
            }
            return instance.HandleConnectionEvent(ref *connectionEvent);
        }
        catch (Exception ex)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(instance, $"{instance} Exception while processing event {connectionEvent->Type}: {ex}");
            }
            return QUIC_STATUS_INTERNAL_ERROR;
        }
    }

    /// <summary>
    /// If not closed explicitly by <see cref="CloseAsync(long, CancellationToken)" />, closes the connection silently (leading to idle timeout on the peer side).
    /// And releases all resources associated with the connection.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // Check if the connection has been shut down and if not, shut it down silently.
        if (_shutdownTcs.TryInitialize(out ValueTask valueTask, this))
        {
            unsafe
            {
                MsQuicApi.Api.ApiTable->ConnectionShutdown(
                    _handle.QuicHandle,
                    QUIC_CONNECTION_SHUTDOWN_FLAGS.SILENT,
                    default);
            }
        }

        await valueTask.ConfigureAwait(false);
        _handle.Dispose();

        _configuration?.Dispose();

        // Dispose remote certificate only if it hasn't been accessed via getter, in which case the accessing code becomes the owner of the certificate lifetime.
        if (!_remoteCertificateExposed)
        {
            _remoteCertificate?.Dispose();
        }

        // Flush the queue and dispose all remaining streams.
        _acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException()));
        while (_acceptQueue.Reader.TryRead(out QuicStream? stream))
        {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
