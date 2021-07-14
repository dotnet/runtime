// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicConnection : QuicConnectionProvider
    {
        private static readonly Oid s_clientAuthOid = new Oid("1.3.6.1.5.5.7.3.2", "1.3.6.1.5.5.7.3.2");
        private static readonly Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.1");
        private const uint DefaultResetValue = 0xffffffff; // Arbitrary value unlikely to conflict with application protocols.

        // Delegate that wraps the static function that will be called when receiving an event.
        private static readonly ConnectionCallbackDelegate s_connectionDelegate = new ConnectionCallbackDelegate(NativeCallbackHandler);

        // TODO: remove this.
        // This is only used for client-initiated connections, and isn't needed even then once Connect() has been called.
        private readonly SafeMsQuicConfigurationHandle? _configuration;

        private readonly State _state = new State();
        private int _disposed;

        private IPEndPoint? _localEndPoint;
        private readonly EndPoint _remoteEndPoint;
        private SslApplicationProtocol _negotiatedAlpnProtocol;
        private bool _isServer;
        private bool _remoteCertificateRequired;
        private X509RevocationMode _revocationMode = X509RevocationMode.Offline;
        private RemoteCertificateValidationCallback? _remoteCertificateValidationCallback;

        internal sealed class State
        {
            public SafeMsQuicConnectionHandle Handle = null!; // set inside of MsQuicConnection ctor.
            public string TraceId = null!; // set inside of MsQuicConnection ctor.

            public GCHandle StateGCHandle;

            // These exists to prevent GC of the MsQuicConnection in the middle of an async op (Connect or Shutdown).
            public MsQuicConnection? Connection;

            // TODO: only allocate these when there is an outstanding connect/shutdown.
            public readonly TaskCompletionSource<uint> ConnectTcs = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<uint> ShutdownTcs = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Note that there's no such thing as resetable TCS, so we cannot reuse the same instance after we've set the result.
            // We also cannot use solutions like ManualResetValueTaskSourceCore, since we can have multiple waiters on the same TCS.
            // As a result, we allocate a new TCS when needed, which is when someone explicitely asks for them in WaitForAvailableStreamsAsync.
            public TaskCompletionSource? NewUnidirectionalStreamsAvailable;
            public TaskCompletionSource? NewBidirectionalStreamsAvailable;

            public bool Connected;
            public long AbortErrorCode = -1;
            public int StreamCount;
            private bool _closing;

            // Queue for accepted streams.
            // Backlog limit is managed by MsQuic so it can be unbounded here.
            public readonly Channel<MsQuicStream> AcceptQueue = Channel.CreateUnbounded<MsQuicStream>(new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = true,
            });

            public void RemoveStream(MsQuicStream? stream)
            {
                bool releaseHandles;
                lock (this)
                {
                    StreamCount--;
                    Debug.Assert(StreamCount >= 0);
                    releaseHandles = _closing && StreamCount == 0;
                }

                if (releaseHandles)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"{TraceId} releasing handle after last stream.");
                    Handle?.Dispose();
                }
            }

            public bool TryQueueNewStream(SafeMsQuicStreamHandle streamHandle, QUIC_STREAM_OPEN_FLAGS flags)
            {
                var stream = new MsQuicStream(this, streamHandle, flags);
                if (AcceptQueue.Writer.TryWrite(stream))
                {
                    return true;
                }
                else
                {
                    stream.Dispose();
                    return false;
                }
            }

            public bool TryAddStream(MsQuicStream stream)
            {
                lock (this)
                {
                    if (_closing)
                    {
                        return false;
                    }

                    StreamCount++;
                    return true;
                }
            }

            // This is called under lock from connection dispose
            public void SetClosing()
            {
                lock (this)
                {
                    _closing = true;
                }
            }
        }

        internal string TraceId() => _state.TraceId;

        // constructor for inbound connections
        public MsQuicConnection(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, SafeMsQuicConnectionHandle handle, bool remoteCertificateRequired = false, X509RevocationMode revocationMode = X509RevocationMode.Offline, RemoteCertificateValidationCallback? remoteCertificateValidationCallback = null)
        {
            _state.Handle = handle;
            _state.StateGCHandle = GCHandle.Alloc(_state);
            _state.Connected = true;
            _isServer = true;
            _localEndPoint = localEndPoint;
            _remoteEndPoint = remoteEndPoint;
            _remoteCertificateRequired = remoteCertificateRequired;
            _revocationMode = revocationMode;
            _remoteCertificateValidationCallback = remoteCertificateValidationCallback;

            if (_remoteCertificateRequired)
            {
                // We need to link connection for the validation callback.
                // We need to be able to find the connection in HandleEventPeerCertificateReceived
                // and dispatch it as sender to validation callback.
                // After that Connection will be set back to null.
                _state.Connection = this;
            }

            try
            {
                MsQuicApi.Api.SetCallbackHandlerDelegate(
                    _state.Handle,
                    s_connectionDelegate,
                    GCHandle.ToIntPtr(_state.StateGCHandle));
            }
            catch
            {
                _state.StateGCHandle.Free();
                throw;
            }

            _state.TraceId = MsQuicTraceHelper.GetTraceId(_state.Handle);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"{TraceId()} Inbound connection created");
            }
        }

        // constructor for outbound connections
        public MsQuicConnection(QuicClientConnectionOptions options)
        {
            _remoteEndPoint = options.RemoteEndPoint!;
            _configuration = SafeMsQuicConfigurationHandle.Create(options);
            _isServer = false;
            _remoteCertificateRequired = true;
            if (options.ClientAuthenticationOptions != null)
            {
                _revocationMode = options.ClientAuthenticationOptions.CertificateRevocationCheckMode;
                _remoteCertificateValidationCallback = options.ClientAuthenticationOptions.RemoteCertificateValidationCallback;
            }

            _state.StateGCHandle = GCHandle.Alloc(_state);
            try
            {
                // this handle is ref counted by MsQuic, so safe to dispose here.
                using SafeMsQuicConfigurationHandle config = SafeMsQuicConfigurationHandle.Create(options);

                uint status = MsQuicApi.Api.ConnectionOpenDelegate(
                    MsQuicApi.Api.Registration,
                    s_connectionDelegate,
                    GCHandle.ToIntPtr(_state.StateGCHandle),
                    out _state.Handle);

                QuicExceptionHelpers.ThrowIfFailed(status, "Could not open the connection.");
            }
            catch
            {
                _state.StateGCHandle.Free();
                throw;
            }

            _state.TraceId = MsQuicTraceHelper.GetTraceId(_state.Handle);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"{TraceId()} Outbound connection created");
            }
        }

        internal override IPEndPoint? LocalEndPoint => _localEndPoint;

        internal override EndPoint RemoteEndPoint => _remoteEndPoint;

        internal override SslApplicationProtocol NegotiatedApplicationProtocol => _negotiatedAlpnProtocol;

        internal override bool Connected => _state.Connected;

        private static uint HandleEventConnected(State state, ref ConnectionEvent connectionEvent)
        {
            if (!state.Connected)
            {
                // Connected will already be true for connections accepted from a listener.

                SOCKADDR_INET inetAddress = MsQuicParameterHelpers.GetINetParam(MsQuicApi.Api, state.Handle, QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.LOCAL_ADDRESS);

                Debug.Assert(state.Connection != null);
                state.Connection._localEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref inetAddress);
                state.Connection.SetNegotiatedAlpn(connectionEvent.Data.Connected.NegotiatedAlpn, connectionEvent.Data.Connected.NegotiatedAlpnLength);
                state.Connection = null;

                state.Connected = true;
                state.ConnectTcs.SetResult(MsQuicStatusCodes.Success);
            }

            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventShutdownInitiatedByTransport(State state, ref ConnectionEvent connectionEvent)
        {
            if (!state.Connected)
            {
                Debug.Assert(state.Connection != null);
                state.Connection = null;

                uint hresult = connectionEvent.Data.ShutdownInitiatedByTransport.Status;
                Exception ex = QuicExceptionHelpers.CreateExceptionForHResult(hresult, "Connection has been shutdown by transport.");
                state.ConnectTcs.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(ex));
            }

            state.AcceptQueue.Writer.TryComplete();
            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventShutdownInitiatedByPeer(State state, ref ConnectionEvent connectionEvent)
        {
            state.AbortErrorCode = (long)connectionEvent.Data.ShutdownInitiatedByPeer.ErrorCode;
            state.AcceptQueue.Writer.TryComplete();
            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventShutdownComplete(State state, ref ConnectionEvent connectionEvent)
        {
            // This is the final event on the connection, so free the GCHandle used by the event callback.
            state.StateGCHandle.Free();

            state.Connection = null;

            state.ShutdownTcs.SetResult(MsQuicStatusCodes.Success);

            // Stop accepting new streams.
            state.AcceptQueue.Writer.TryComplete();

            // Stop notifying about available streams.
            TaskCompletionSource? unidirectionalTcs = null;
            TaskCompletionSource? bidirectionalTcs = null;
            lock (state)
            {
                unidirectionalTcs = state.NewUnidirectionalStreamsAvailable;
                bidirectionalTcs = state.NewBidirectionalStreamsAvailable;
                state.NewUnidirectionalStreamsAvailable = null;
                state.NewBidirectionalStreamsAvailable = null;
            }

            if (unidirectionalTcs is not null)
            {
                unidirectionalTcs.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException()));
            }
            if (bidirectionalTcs is not null)
            {
                bidirectionalTcs.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException()));
            }
            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventNewStream(State state, ref ConnectionEvent connectionEvent)
        {
            var streamHandle = new SafeMsQuicStreamHandle(connectionEvent.Data.PeerStreamStarted.Stream);
            if (!state.TryQueueNewStream(streamHandle, connectionEvent.Data.PeerStreamStarted.Flags))
            {
                // This will call StreamCloseDelegate and free the stream.
                // We will return Success to the MsQuic to prevent double free.
                streamHandle.Dispose();
            }

            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventStreamsAvailable(State state, ref ConnectionEvent connectionEvent)
        {
            TaskCompletionSource? unidirectionalTcs = null;
            TaskCompletionSource? bidirectionalTcs = null;
            lock (state)
            {
                if (connectionEvent.Data.StreamsAvailable.UniDirectionalCount > 0)
                {
                    unidirectionalTcs = state.NewUnidirectionalStreamsAvailable;
                    state.NewUnidirectionalStreamsAvailable = null;
                }

                if (connectionEvent.Data.StreamsAvailable.BiDirectionalCount > 0)
                {
                    bidirectionalTcs = state.NewBidirectionalStreamsAvailable;
                    state.NewBidirectionalStreamsAvailable = null;
                }
            }

            if (unidirectionalTcs is not null)
            {
                unidirectionalTcs.SetResult();
            }
            if (bidirectionalTcs is not null)
            {
                bidirectionalTcs.SetResult();
            }

            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventPeerCertificateReceived(State state, ref ConnectionEvent connectionEvent)
        {
            SslPolicyErrors sslPolicyErrors  = SslPolicyErrors.None;
            X509Chain? chain = null;
            X509Certificate2? certificate = null;
            X509Certificate2Collection? additionalCertificates = null;

            MsQuicConnection? connection = state.Connection;
            if (connection == null)
            {
                return MsQuicStatusCodes.InvalidState;
            }

            if (connection._isServer)
            {
                state.Connection = null;
            }

            try
            {
                if (connectionEvent.Data.PeerCertificateReceived.PlatformCertificateHandle != IntPtr.Zero)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        certificate = new X509Certificate2(connectionEvent.Data.PeerCertificateReceived.PlatformCertificateHandle);
                    }
                    else
                    {
                        unsafe
                        {
                            ReadOnlySpan<QuicBuffer> quicBuffer = new ReadOnlySpan<QuicBuffer>((void*)connectionEvent.Data.PeerCertificateReceived.PlatformCertificateHandle, sizeof(QuicBuffer));
                            certificate = new X509Certificate2(new ReadOnlySpan<byte>(quicBuffer[0].Buffer, (int)quicBuffer[0].Length));

                            if (connectionEvent.Data.PeerCertificateReceived.PlatformCertificateChainHandle != IntPtr.Zero)
                            {
                                quicBuffer = new ReadOnlySpan<QuicBuffer>((void*)connectionEvent.Data.PeerCertificateReceived.PlatformCertificateChainHandle, sizeof(QuicBuffer));
                                if (quicBuffer[0].Length != 0 && quicBuffer[0].Buffer != null)
                                {
                                    additionalCertificates = new X509Certificate2Collection();
                                    additionalCertificates.Import(new ReadOnlySpan<byte>(quicBuffer[0].Buffer, (int)quicBuffer[0].Length));
                                }
                            }
                        }
                    }
                }

                if (certificate == null)
                {
                    if (NetEventSource.Log.IsEnabled() && connection._remoteCertificateRequired) NetEventSource.Error(state, $"{state.TraceId} Remote certificate required, but no remote certificate received");
                    sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNotAvailable;
                }
                else
                {
                    chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = connection._revocationMode;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    chain.ChainPolicy.ApplicationPolicy.Add(connection._isServer ? s_clientAuthOid : s_serverAuthOid);

                    if (additionalCertificates != null && additionalCertificates.Count > 1)
                    {
                        chain.ChainPolicy.ExtraStore.AddRange(additionalCertificates);
                    }

                    if (!chain.Build(certificate))
                    {
                        sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
                    }
                }

                if (!connection._remoteCertificateRequired)
                {
                    sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNotAvailable;
                }

                if (connection._remoteCertificateValidationCallback != null)
                {
                    bool success = connection._remoteCertificateValidationCallback(connection, certificate, chain, sslPolicyErrors);
                    // Unset the callback to prevent multiple invocations of the callback per a single connection.
                    // Return the same value as the custom callback just did.
                    connection._remoteCertificateValidationCallback = (_, _, _, _) => success;

                    if (!success && NetEventSource.Log.IsEnabled())
                        NetEventSource.Error(state, $"{state.TraceId} Remote certificate rejected by verification callback");
                    return success ? MsQuicStatusCodes.Success : MsQuicStatusCodes.HandshakeFailure;
                }

                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(state, $"{state.TraceId} Certificate validation for '${certificate?.Subject}' finished with ${sslPolicyErrors}");

                return (sslPolicyErrors == SslPolicyErrors.None) ? MsQuicStatusCodes.Success : MsQuicStatusCodes.HandshakeFailure;
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(state, $"{state.TraceId} Certificate validation failed ${ex.Message}");
            }

            return MsQuicStatusCodes.InternalError;
        }

        internal override async ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            MsQuicStream stream;

            try
            {
                stream = await _state.AcceptQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                throw ThrowHelper.GetConnectionAbortedException(_state.AbortErrorCode);
            }

            return stream;
        }

        internal override ValueTask WaitForAvailableUnidirectionalStreamsAsync(CancellationToken cancellationToken = default)
        {
            TaskCompletionSource? tcs = _state.NewUnidirectionalStreamsAvailable;
            if (tcs is null)
            {
                lock (_state)
                {
                    if (_state.NewUnidirectionalStreamsAvailable is null)
                    {
                        if (_state.ShutdownTcs.Task.IsCompleted)
                        {
                            throw new QuicOperationAbortedException();
                        }

                        if (GetRemoteAvailableUnidirectionalStreamCount() > 0)
                        {
                            return ValueTask.CompletedTask;
                        }

                        _state.NewUnidirectionalStreamsAvailable = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    }
                    tcs = _state.NewUnidirectionalStreamsAvailable;
                }
            }

            return new ValueTask(tcs.Task.WaitAsync(cancellationToken));
        }

        internal override ValueTask WaitForAvailableBidirectionalStreamsAsync(CancellationToken cancellationToken = default)
        {
            TaskCompletionSource? tcs = _state.NewBidirectionalStreamsAvailable;
            if (tcs is null)
            {
                lock (_state)
                {
                    if (_state.NewBidirectionalStreamsAvailable is null)
                    {
                        if (_state.ShutdownTcs.Task.IsCompleted)
                        {
                            throw new QuicOperationAbortedException();
                        }

                        if (GetRemoteAvailableBidirectionalStreamCount() > 0)
                        {
                            return ValueTask.CompletedTask;
                        }

                        _state.NewBidirectionalStreamsAvailable = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    }
                    tcs = _state.NewBidirectionalStreamsAvailable;
                }
            }

            return new ValueTask(tcs.Task.WaitAsync(cancellationToken));
        }

        internal override QuicStreamProvider OpenUnidirectionalStream()
        {
            ThrowIfDisposed();

            return new MsQuicStream(_state, QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL);
        }

        internal override QuicStreamProvider OpenBidirectionalStream()
        {
            ThrowIfDisposed();

            return new MsQuicStream(_state, QUIC_STREAM_OPEN_FLAGS.NONE);
        }

        internal override int GetRemoteAvailableUnidirectionalStreamCount()
        {
            return MsQuicParameterHelpers.GetUShortParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.LOCAL_UNIDI_STREAM_COUNT);
        }

        internal override int GetRemoteAvailableBidirectionalStreamCount()
        {
            return MsQuicParameterHelpers.GetUShortParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.LOCAL_BIDI_STREAM_COUNT);
        }

        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_configuration is null)
            {
                throw new Exception($"{nameof(ConnectAsync)} must not be called on a connection obtained from a listener.");
            }

            (string address, int port) = _remoteEndPoint switch
            {
                DnsEndPoint dnsEp => (dnsEp.Host, dnsEp.Port),
                IPEndPoint ipEp => (ipEp.Address.ToString(), ipEp.Port),
                _ => throw new Exception($"Unsupported remote endpoint type '{_remoteEndPoint.GetType()}'.")
            };

            QUIC_ADDRESS_FAMILY af = _remoteEndPoint.AddressFamily switch
            {
                AddressFamily.Unspecified => QUIC_ADDRESS_FAMILY.UNSPEC,
                AddressFamily.InterNetwork => QUIC_ADDRESS_FAMILY.INET,
                AddressFamily.InterNetworkV6 => QUIC_ADDRESS_FAMILY.INET6,
                _ => throw new Exception(SR.Format(SR.net_quic_unsupported_address_family, _remoteEndPoint.AddressFamily))
            };

            Debug.Assert(_state.StateGCHandle.IsAllocated);

            _state.Connection = this;
            try
            {
                uint status = MsQuicApi.Api.ConnectionStartDelegate(
                    _state.Handle,
                    _configuration,
                    af,
                    address,
                    (ushort)port);

                QuicExceptionHelpers.ThrowIfFailed(status, "Failed to connect to peer.");
            }
            catch
            {
                _state.StateGCHandle.Free();
                _state.Connection = null;
                throw;
            }

            return new ValueTask(_state.ConnectTcs.Task);
        }

        private ValueTask ShutdownAsync(
            QUIC_CONNECTION_SHUTDOWN_FLAGS Flags,
            long ErrorCode)
        {
            // Store the connection into the GCHandle'd state to prevent GC if user calls ShutdownAsync and gets rid of all references to the MsQuicConnection.
            Debug.Assert(_state.Connection == null);
            _state.Connection = this;

            try
            {
                MsQuicApi.Api.ConnectionShutdownDelegate(
                    _state.Handle,
                    Flags,
                    ErrorCode);
            }
            catch
            {
                _state.Connection = null;
                throw;
            }

            return new ValueTask(_state.ShutdownTcs.Task);
        }

        internal void SetNegotiatedAlpn(IntPtr alpn, int alpnLength)
        {
            if (alpn != IntPtr.Zero && alpnLength != 0)
            {
                var buffer = new byte[alpnLength];
                Marshal.Copy(alpn, buffer, 0, alpnLength);
                _negotiatedAlpnProtocol = new SslApplicationProtocol(buffer);
            }
        }

        private static uint NativeCallbackHandler(
            IntPtr connection,
            IntPtr context,
            ref ConnectionEvent connectionEvent)
        {
            GCHandle gcHandle = GCHandle.FromIntPtr(context);
            Debug.Assert(gcHandle.IsAllocated);
            Debug.Assert(gcHandle.Target is not null);
            var state = (State)gcHandle.Target;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(state, $"{state.TraceId} Connection received event {connectionEvent.Type}");
            }

            try
            {
                switch (connectionEvent.Type)
                {
                    case QUIC_CONNECTION_EVENT_TYPE.CONNECTED:
                        return HandleEventConnected(state, ref connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_TRANSPORT:
                        return HandleEventShutdownInitiatedByTransport(state, ref connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_PEER:
                        return HandleEventShutdownInitiatedByPeer(state, ref connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_COMPLETE:
                        return HandleEventShutdownComplete(state, ref connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.PEER_STREAM_STARTED:
                        return HandleEventNewStream(state, ref connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.STREAMS_AVAILABLE:
                        return HandleEventStreamsAvailable(state, ref connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.PEER_CERTIFICATE_RECEIVED:
                        return HandleEventPeerCertificateReceived(state, ref connectionEvent);
                    default:
                        return MsQuicStatusCodes.Success;
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(state, $"{state.TraceId} Exception occurred during handling {connectionEvent.Type} connection callback: {ex}");
                }

                Debug.Fail($"{state.TraceId} Exception occurred during handling {connectionEvent.Type} connection callback: {ex}");

                // TODO: trigger an exception on any outstanding async calls.

                return MsQuicStatusCodes.InternalError;
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MsQuicConnection()
        {
            Dispose(false);
        }

        private async Task FlushAcceptQueue()
        {
            _state.AcceptQueue.Writer.TryComplete();
            await foreach (MsQuicStream stream in _state.AcceptQueue.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                if (stream.CanRead)
                {
                    stream.AbortRead(DefaultResetValue);
                }
                if (stream.CanWrite)
                {
                    stream.AbortWrite(DefaultResetValue);
                }
                stream.Dispose();
            }
        }

        private void Dispose(bool disposing)
        {
            int disposed = Interlocked.Exchange(ref _disposed, 1);
            if (disposed != 0)
            {
                return;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(_state, $"{TraceId()} Stream disposing {disposing}");

            // If we haven't already shutdown gracefully (via a successful CloseAsync call), then force an abortive shutdown.
            MsQuicApi.Api.ConnectionShutdownDelegate(
                _state.Handle,
                QUIC_CONNECTION_SHUTDOWN_FLAGS.SILENT,
                0);

            bool releaseHandles = false;
            lock (_state)
            {
                _state.Connection = null;
                if (_state.StreamCount == 0)
                {
                    releaseHandles = true;
                }
                else
                {
                    // We have pending streams so we need to defer cleanup until last one is gone.
                    _state.SetClosing();
                }
            }

            FlushAcceptQueue().GetAwaiter().GetResult();
            _configuration?.Dispose();
            if (releaseHandles)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(_state, $"{TraceId()} Connection releasing handle");

                // We may not be fully initialized if constructor fails.
                _state.Handle?.Dispose();
            }
        }

        // TODO: this appears abortive and will cause prior successfully shutdown and closed streams to drop data.
        // It's unclear how to gracefully wait for a connection to be 100% done.
        internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
        {
            if (_disposed == 1)
            {
                return default;
            }

            return ShutdownAsync(QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE, errorCode);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed == 1)
            {
                throw new ObjectDisposedException(nameof(MsQuicStream));
            }
        }
    }
}
