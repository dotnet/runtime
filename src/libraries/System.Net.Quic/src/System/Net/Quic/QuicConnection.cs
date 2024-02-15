// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;
using CONNECTED_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._CONNECTED_e__Struct;
using LOCAL_ADDRESS_CHANGED_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._LOCAL_ADDRESS_CHANGED_e__Struct;
using PEER_ADDRESS_CHANGED_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._PEER_ADDRESS_CHANGED_e__Struct;
using PEER_CERTIFICATE_RECEIVED_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._PEER_CERTIFICATE_RECEIVED_e__Struct;
using PEER_STREAM_STARTED_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._PEER_STREAM_STARTED_e__Struct;
using SHUTDOWN_COMPLETE_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._SHUTDOWN_COMPLETE_e__Struct;
using SHUTDOWN_INITIATED_BY_PEER_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._SHUTDOWN_INITIATED_BY_PEER_e__Struct;
using SHUTDOWN_INITIATED_BY_TRANSPORT_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct;

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
    public static ValueTask<QuicConnection> ConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.SystemNetQuic_PlatformNotSupported, MsQuicApi.NotSupportedReason));
        }

        // Validate and fill in defaults for the options.
        options.Validate(nameof(options));
        return StartConnectAsync(options, cancellationToken);

        static async ValueTask<QuicConnection> StartConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken)
        {
            QuicConnection connection = new QuicConnection();

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (options.HandshakeTimeout != Timeout.InfiniteTimeSpan && options.HandshakeTimeout != TimeSpan.Zero)
            {
                linkedCts.CancelAfter(options.HandshakeTimeout);
            }

            try
            {
                await connection.FinishConnectAsync(options, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await connection.DisposeAsync().ConfigureAwait(false);

                // throw OCE with correct token if cancellation requested by user
                cancellationToken.ThrowIfCancellationRequested();

                // cancellation by the linkedCts.CancelAfter. Convert to Timeout
                throw new QuicException(QuicError.ConnectionTimeout, null, SR.Format(SR.net_quic_handshake_timeout, options.HandshakeTimeout));
            }
            catch
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }

            return connection;
        }
    }

    /// <summary>
    /// Handle to MsQuic connection object.
    /// </summary>
    private readonly MsQuicContextSafeHandle _handle;

    /// <summary>
    /// Set to non-zero once disposed. Prevents double and/or concurrent disposal.
    /// </summary>
    private int _disposed;

    private readonly ValueTaskSource _connectedTcs = new ValueTaskSource();
    private readonly ResettableValueTaskSource _shutdownTcs = new ResettableValueTaskSource()
    {
        CancellationAction = target =>
        {
            try
            {
                if (target is QuicConnection connection)
                {
                    // The OCE will be propagated through stored CancellationToken in ResettableValueTaskSource.
                    connection._shutdownTcs.TrySetResult();
                }
            }
            catch (ObjectDisposedException)
            {
                // We collided with a Dispose in another thread. This can happen
                // when using CancellationTokenSource.CancelAfter.
                // Ignore the exception
            }
        }
    };

    private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

    // Token that fires when the connection is closed.
    internal CancellationToken ConnectionShutdownToken => _shutdownTokenSource.Token;

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
    /// Used by <see cref="AcceptInboundStreamAsync(CancellationToken)" /> to throw in case no stream can be opened from the peer.
    /// <c>true</c> when at least one of <see cref="QuicConnectionOptions.MaxInboundBidirectionalStreams" /> or <see cref="QuicConnectionOptions.MaxInboundUnidirectionalStreams" /> is greater than <c>0</c>.
    /// </summary>
    private bool _canAccept;
    /// <summary>
    /// From <see cref="QuicConnectionOptions.DefaultStreamErrorCode"/>, passed to newly created <see cref="QuicStream"/>.
    /// </summary>
    private long _defaultStreamErrorCode;
    /// <summary>
    /// From <see cref="QuicConnectionOptions.DefaultCloseErrorCode"/>, used to close connection in <see cref="DisposeAsync"/>.
    /// </summary>
    private long _defaultCloseErrorCode;

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

#if DEBUG
    /// <summary>
    /// Will contain TLS secret after CONNECTED event is received and store it into SSLKEYLOGFILE.
    /// MsQuic holds the underlying pointer so this object can be disposed only after connection native handle gets closed.
    /// </summary>
    private readonly MsQuicTlsSecret? _tlsSecret;
#endif

    /// <summary>
    /// The remote endpoint used for this connection.
    /// </summary>
    public IPEndPoint RemoteEndPoint => _remoteEndPoint;
    /// <summary>
    /// The local endpoint used for this connection.
    /// </summary>
    public IPEndPoint LocalEndPoint => _localEndPoint;

    /// <summary>
    /// Gets the name of the server the client is trying to connect to. That name is used for server certificate validation. It can be a DNS name or an IP address.
    /// </summary>
    /// <returns>The name of the server the client is trying to connect to.</returns>
    public string TargetHostName => _sslConnectionOptions.TargetHost;

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

    /// <inheritdoc />
    public override string ToString() => _handle.ToString();

    /// <summary>
    /// Initializes a new instance of an outbound <see cref="QuicConnection" />.
    /// </summary>
    private unsafe QuicConnection()
    {
        GCHandle context = GCHandle.Alloc(this, GCHandleType.Weak);
        try
        {
            QUIC_HANDLE* handle;
            ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ConnectionOpen(
                MsQuicApi.Api.Registration,
                &NativeCallback,
                (void*)GCHandle.ToIntPtr(context),
                &handle),
                "ConnectionOpen failed");
            _handle = new MsQuicContextSafeHandle(handle, context, SafeHandleType.Connection);
        }
        catch
        {
            context.Free();
            throw;
        }

#if DEBUG
        _tlsSecret = MsQuicTlsSecret.Create(_handle);
#endif
    }

    /// <summary>
    /// Initializes a new instance of an inbound <see cref="QuicConnection" />.
    /// </summary>
    /// <param name="handle">Native handle.</param>
    /// <param name="info">Related data from the NEW_CONNECTION listener event.</param>
    internal unsafe QuicConnection(QUIC_HANDLE* handle, QUIC_NEW_CONNECTION_INFO* info)
    {
        GCHandle context = GCHandle.Alloc(this, GCHandleType.Weak);
        try
        {
            _handle = new MsQuicContextSafeHandle(handle, context, SafeHandleType.Connection);
            delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int> nativeCallback = &NativeCallback;
            MsQuicApi.Api.SetCallbackHandler(
                _handle,
                nativeCallback,
                (void*)GCHandle.ToIntPtr(context));
        }
        catch
        {
            context.Free();
            throw;
        }

        _remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(info->RemoteAddress);
        _localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(info->LocalAddress);
#if DEBUG
        _tlsSecret = MsQuicTlsSecret.Create(_handle);
#endif
    }

    private async ValueTask FinishConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken = default)
    {
        if (_connectedTcs.TryInitialize(out ValueTask valueTask, this, cancellationToken))
        {
            _canAccept = options.MaxInboundBidirectionalStreams > 0 || options.MaxInboundUnidirectionalStreams > 0;
            _defaultStreamErrorCode = options.DefaultStreamErrorCode;
            _defaultCloseErrorCode = options.DefaultCloseErrorCode;

            if (!options.RemoteEndPoint.TryParse(out string? host, out IPAddress? address, out int port))
            {
                throw new ArgumentException(SR.Format(SR.net_quic_unsupported_endpoint_type, options.RemoteEndPoint.GetType()), nameof(options));
            }

            if (address is null)
            {
                Debug.Assert(host is not null);

                // Given just a ServerName to connect to, msquic would also use the first address after the resolution
                // (https://github.com/microsoft/msquic/issues/1181) and it would not return a well-known error code
                // for resolution failures we could rely on. By doing the resolution in managed code, we can guarantee
                // that a SocketException will surface to the user if the name resolution fails.
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (addresses.Length == 0)
                {
                    throw new SocketException((int)SocketError.HostNotFound);
                }
                address = addresses[0];
            }

            QuicAddr remoteQuicAddress = new IPEndPoint(address, port).ToQuicAddr();
            MsQuicHelpers.SetMsQuicParameter(_handle, QUIC_PARAM_CONN_REMOTE_ADDRESS, remoteQuicAddress);

            if (options.LocalEndPoint is not null)
            {
                QuicAddr localQuicAddress = options.LocalEndPoint.ToQuicAddr();
                MsQuicHelpers.SetMsQuicParameter(_handle, QUIC_PARAM_CONN_LOCAL_ADDRESS, localQuicAddress);
            }

            _sslConnectionOptions = new SslConnectionOptions(
                this,
                isClient: true,
                options.ClientAuthenticationOptions.TargetHost ?? host ?? address.ToString(),
                certificateRequired: true,
                options.ClientAuthenticationOptions.CertificateRevocationCheckMode,
                options.ClientAuthenticationOptions.RemoteCertificateValidationCallback,
                options.ClientAuthenticationOptions.CertificateChainPolicy?.Clone());
            _configuration = MsQuicConfiguration.Create(options);

            // RFC 6066 forbids IP literals.
            // IDN mapping is handled by MsQuic.
            string sni = (TargetHostNameHelper.IsValidAddress(options.ClientAuthenticationOptions.TargetHost) ? null : options.ClientAuthenticationOptions.TargetHost) ?? host ?? string.Empty;

            IntPtr targetHostPtr = Marshal.StringToCoTaskMemUTF8(sni);
            try
            {
                unsafe
                {
                    ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ConnectionStart(
                        _handle,
                        _configuration,
                        (ushort)remoteQuicAddress.Family,
                        (sbyte*)targetHostPtr,
                        (ushort)port),
                        "ConnectionStart failed");
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(targetHostPtr);
            }
        }

        await valueTask.ConfigureAwait(false);
    }

    internal ValueTask FinishHandshakeAsync(QuicServerConnectionOptions options, string targetHost, CancellationToken cancellationToken = default)
    {
        if (_connectedTcs.TryInitialize(out ValueTask valueTask, this, cancellationToken))
        {
            _canAccept = options.MaxInboundBidirectionalStreams > 0 || options.MaxInboundUnidirectionalStreams > 0;
            _defaultStreamErrorCode = options.DefaultStreamErrorCode;
            _defaultCloseErrorCode = options.DefaultCloseErrorCode;

            // RFC 6066 forbids IP literals, avoid setting IP address here for consistency with SslStream
            if (TargetHostNameHelper.IsValidAddress(targetHost))
            {
                targetHost = string.Empty;
            }

            _sslConnectionOptions = new SslConnectionOptions(
                this,
                isClient: false,
                targetHost,
                options.ServerAuthenticationOptions.ClientCertificateRequired,
                options.ServerAuthenticationOptions.CertificateRevocationCheckMode,
                options.ServerAuthenticationOptions.RemoteCertificateValidationCallback,
                options.ServerAuthenticationOptions.CertificateChainPolicy?.Clone());
            _configuration = MsQuicConfiguration.Create(options, targetHost);

            unsafe
            {
                ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ConnectionSetConfiguration(
                    _handle,
                    _configuration),
                    "ConnectionSetConfiguration failed");
            }
        }

        return valueTask;
    }

    /// <summary>
    /// Create an outbound uni/bidirectional <see cref="QuicStream" />.
    /// In case the connection doesn't have any available stream capacity, i.e.: the peer limits the concurrent stream count,
    /// the operation will pend until the stream can be opened (other stream gets closed or peer increases the stream limit).
    /// </summary>
    /// <param name="type">The type of the stream, i.e. unidirectional or bidirectional.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes with the opened <see cref="QuicStream" />.</returns>
    public async ValueTask<QuicStream> OpenOutboundStreamAsync(QuicStreamType type, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        QuicStream? stream = null;
        try
        {
            stream = new QuicStream(_handle, type, _defaultStreamErrorCode);
            await stream.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (stream is not null)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }

            // Propagate ODE if disposed in the meantime.
            ObjectDisposedException.ThrowIf(_disposed == 1, this);
            // Propagate connection error if present.
            if (_acceptQueue.Reader.Completion.IsFaulted)
            {
                await _acceptQueue.Reader.Completion.ConfigureAwait(false);
            }
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

        GCHandle keepObject = GCHandle.Alloc(this);
        try
        {
            return await _acceptQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Throw(ex.InnerException);
            throw;
        }
        finally
        {
            keepObject.Free();
        }
    }

    /// <summary>
    /// Closes the connection with the application provided code, see <see href="https://www.rfc-editor.org/rfc/rfc9000.html#immediate-close">RFC 9000: Connection Termination</see> for more details.
    /// </summary>
    /// <remarks>
    /// Connection close is not graceful in regards to its streams, i.e.: calling <see cref="CloseAsync(long, CancellationToken)"/> will immediately close all streams associated with this connection.
    /// Make sure, that all streams have been closed and all their data consumed before calling this method;
    /// otherwise, all the data that were received but not consumed yet, will be lost.
    ///
    /// If <see cref="CloseAsync(long, CancellationToken)"/> is not called before <see cref="DisposeAsync">disposing</see> the connection,
    /// the <see cref="QuicConnectionOptions.DefaultCloseErrorCode"/> will be used by <see cref="DisposeAsync"/> to close the connection.
    /// </remarks>
    /// <param name="errorCode">Application provided code with the reason for closure.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes when the connection is closed.</returns>
    public ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (_shutdownTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
        {
            unsafe
            {
                MsQuicApi.Api.ConnectionShutdown(
                    _handle,
                    QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE,
                    (ulong)errorCode);
            }
        }

        return valueTask;
    }

    private unsafe int HandleEventConnected(ref CONNECTED_DATA data)
    {
        _negotiatedApplicationProtocol = new SslApplicationProtocol(new Span<byte>(data.NegotiatedAlpn, data.NegotiatedAlpnLength).ToArray());

        QuicAddr remoteAddress = MsQuicHelpers.GetMsQuicParameter<QuicAddr>(_handle, QUIC_PARAM_CONN_REMOTE_ADDRESS);
        _remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(&remoteAddress);

        QuicAddr localAddress = MsQuicHelpers.GetMsQuicParameter<QuicAddr>(_handle, QUIC_PARAM_CONN_LOCAL_ADDRESS);
        _localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(&localAddress);

#if DEBUG
        _tlsSecret?.WriteSecret();
#endif

        if (NetEventSource.Log.IsEnabled())
        {
            NetEventSource.Info(this, $"{this} Connection connected {LocalEndPoint} -> {RemoteEndPoint} for {_negotiatedApplicationProtocol} protocol");
        }
        _connectedTcs.TrySetResult();
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventShutdownInitiatedByTransport(ref SHUTDOWN_INITIATED_BY_TRANSPORT_DATA data)
    {
        Exception exception = ExceptionDispatchInfo.SetCurrentStackTrace(ThrowHelper.GetExceptionForMsQuicStatus(data.Status, (long)data.ErrorCode));
        _connectedTcs.TrySetException(exception);
        _acceptQueue.Writer.TryComplete(exception);
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventShutdownInitiatedByPeer(ref SHUTDOWN_INITIATED_BY_PEER_DATA data)
    {
        _acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(ThrowHelper.GetConnectionAbortedException((long)data.ErrorCode)));
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventShutdownComplete()
    {
        Exception exception = ExceptionDispatchInfo.SetCurrentStackTrace(_disposed == 1 ? new ObjectDisposedException(GetType().FullName) : ThrowHelper.GetOperationAbortedException());
        _acceptQueue.Writer.TryComplete(exception);
        _connectedTcs.TrySetException(exception);
        _shutdownTokenSource.Cancel();
        _shutdownTcs.TrySetResult(final: true);
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventLocalAddressChanged(ref LOCAL_ADDRESS_CHANGED_DATA data)
    {
        _localEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(data.Address);
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventPeerAddressChanged(ref PEER_ADDRESS_CHANGED_DATA data)
    {
        _remoteEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(data.Address);
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventPeerStreamStarted(ref PEER_STREAM_STARTED_DATA data)
    {
        QuicStream stream = new QuicStream(_handle, data.Stream, data.Flags, _defaultStreamErrorCode);
        if (!_acceptQueue.Writer.TryWrite(stream))
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(this, $"{this} Unable to enqueue incoming stream {stream}");
            }

            stream.Dispose();
            return QUIC_STATUS_SUCCESS;
        }

        data.Flags |= QUIC_STREAM_OPEN_FLAGS.DELAY_ID_FC_UPDATES;
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventPeerCertificateReceived(ref PEER_CERTIFICATE_RECEIVED_DATA data)
    {
        //
        // the certificate validation is an expensive operation and we don't want to delay MsQuic
        // worker thread. So we offload the validation to the .NET threadpool. Incidentally, this
        // also prevents potential user RemoteCertificateValidationCallback from blocking MsQuic
        // worker threads.
        //

        _sslConnectionOptions.StartAsyncCertificateValidation(data.Certificate, data.Chain);
        return QUIC_STATUS_PENDING;
    }

    private unsafe int HandleConnectionEvent(ref QUIC_CONNECTION_EVENT connectionEvent)
        => connectionEvent.Type switch
        {
            QUIC_CONNECTION_EVENT_TYPE.CONNECTED => HandleEventConnected(ref connectionEvent.CONNECTED),
            QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_TRANSPORT => HandleEventShutdownInitiatedByTransport(ref connectionEvent.SHUTDOWN_INITIATED_BY_TRANSPORT),
            QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_PEER => HandleEventShutdownInitiatedByPeer(ref connectionEvent.SHUTDOWN_INITIATED_BY_PEER),
            QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_COMPLETE => HandleEventShutdownComplete(),
            QUIC_CONNECTION_EVENT_TYPE.LOCAL_ADDRESS_CHANGED => HandleEventLocalAddressChanged(ref connectionEvent.LOCAL_ADDRESS_CHANGED),
            QUIC_CONNECTION_EVENT_TYPE.PEER_ADDRESS_CHANGED => HandleEventPeerAddressChanged(ref connectionEvent.PEER_ADDRESS_CHANGED),
            QUIC_CONNECTION_EVENT_TYPE.PEER_STREAM_STARTED => HandleEventPeerStreamStarted(ref connectionEvent.PEER_STREAM_STARTED),
            QUIC_CONNECTION_EVENT_TYPE.PEER_CERTIFICATE_RECEIVED => HandleEventPeerCertificateReceived(ref connectionEvent.PEER_CERTIFICATE_RECEIVED),
            _ => QUIC_STATUS_SUCCESS,
        };

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
                NetEventSource.Error(null, $"Received event {connectionEvent->Type} for [conn][{(nint)connection:X11}] while connection is already disposed");
            }
            return QUIC_STATUS_INVALID_STATE;
        }

        try
        {
            // Process the event.
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(instance, $"{instance} Received event {connectionEvent->Type} {connectionEvent->ToString()}");
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
    /// If not closed explicitly by <see cref="CloseAsync(long, CancellationToken)" />, closes the connection with the <see cref="QuicConnectionOptions.DefaultCloseErrorCode"/>.
    /// And releases all resources associated with the connection.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // Check if the connection has been shut down and if not, shut it down.
        if (_shutdownTcs.TryGetValueTask(out ValueTask valueTask, this))
        {
            unsafe
            {
                MsQuicApi.Api.ConnectionShutdown(
                    _handle,
                    QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE,
                    (ulong)_defaultCloseErrorCode);
            }
        }
        else if (!valueTask.IsCompletedSuccessfully)
        {
            unsafe
            {
                MsQuicApi.Api.ConnectionShutdown(
                    _handle,
                    QUIC_CONNECTION_SHUTDOWN_FLAGS.SILENT,
                    (ulong)_defaultCloseErrorCode);
            }
        }

        // Wait for SHUTDOWN_COMPLETE, the last event, so that all resources can be safely released.
        await _shutdownTcs.GetFinalTask(this).ConfigureAwait(false);
        Debug.Assert(_connectedTcs.IsCompleted);
        _handle.Dispose();
        _shutdownTokenSource.Dispose();

        _configuration?.Dispose();

        // Dispose remote certificate only if it hasn't been accessed via getter, in which case the accessing code becomes the owner of the certificate lifetime.
        if (!_remoteCertificateExposed)
        {
            _remoteCertificate?.Dispose();
        }

        // Flush the queue and dispose all remaining streams.
        _acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(GetType().FullName)));
        while (_acceptQueue.Reader.TryRead(out QuicStream? stream))
        {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
