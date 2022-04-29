// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
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
        private static unsafe readonly ConnectionCallbackDelegate s_connectionDelegate = new ConnectionCallbackDelegate(NativeCallbackHandler);

        // TODO: remove this.
        // This is only used for client-initiated connections, and isn't needed even then once Connect() has been called.
        private SafeMsQuicConfigurationHandle? _configuration;

        private readonly State _state = new State();
        private int _disposed;

        private IPEndPoint? _localEndPoint;
        private readonly EndPoint _remoteEndPoint;
        private SslApplicationProtocol _negotiatedAlpnProtocol;

        internal sealed class State
        {
            public SafeMsQuicConnectionHandle Handle = null!; // set inside of MsQuicConnection ctor.
            public string TraceId = null!; // set inside of MsQuicConnection ctor.

            public GCHandle StateGCHandle;

            // These exists to prevent GC of the MsQuicConnection in the middle of an async op (Connect or Shutdown).
            public MsQuicConnection? Connection;
            public MsQuicListener.State? ListenerState;

            public TaskCompletionSource<uint>? ConnectTcs;
            // TODO: only allocate these when there is an outstanding shutdown.
            public readonly TaskCompletionSource<uint> ShutdownTcs = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool Connected;
            public long AbortErrorCode = -1;
            public int StreamCount;
            private bool _closing;

            // Certificate validation properties
            public X509Certificate? RemoteCertificate;
            public bool RemoteCertificateRequired;
            public X509RevocationMode RevocationMode = X509RevocationMode.Offline;
            public RemoteCertificateValidationCallback? RemoteCertificateValidationCallback;
            public bool IsServer;
            public string? TargetHost;

            // Queue for accepted streams.
            // Backlog limit is managed by MsQuic so it can be unbounded here.
            public readonly Channel<MsQuicStream> AcceptQueue = Channel.CreateUnbounded<MsQuicStream>(new UnboundedChannelOptions()
            {
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
        public MsQuicConnection(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, MsQuicListener.State listenerState, SafeMsQuicConnectionHandle handle, bool remoteCertificateRequired = false, X509RevocationMode revocationMode = X509RevocationMode.Offline, RemoteCertificateValidationCallback? remoteCertificateValidationCallback = null, ServerCertificateSelectionCallback? serverCertificateSelectionCallback = null)
        {
            _state.Handle = handle;
            _state.StateGCHandle = GCHandle.Alloc(_state);
            _state.RemoteCertificateRequired = remoteCertificateRequired;
            _state.RevocationMode = revocationMode;
            _state.RemoteCertificateValidationCallback = remoteCertificateValidationCallback;
            _state.IsServer = true;
            _localEndPoint = localEndPoint;
            _remoteEndPoint = remoteEndPoint;

            try
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
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

            _state.ListenerState = listenerState;
            _state.TraceId = MsQuicTraceHelper.GetTraceId(_state.Handle);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"{TraceId()} Inbound connection created");
            }
        }

        // constructor for outbound connections
        public MsQuicConnection(QuicClientConnectionOptions options)
        {
            ArgumentNullException.ThrowIfNull(options.RemoteEndPoint, nameof(options.RemoteEndPoint));

            _remoteEndPoint = options.RemoteEndPoint;
            _configuration = SafeMsQuicConfigurationHandle.Create(options);
            _state.RemoteCertificateRequired = true;
            if (options.ClientAuthenticationOptions != null)
            {
                _state.RevocationMode = options.ClientAuthenticationOptions.CertificateRevocationCheckMode;
                _state.RemoteCertificateValidationCallback = options.ClientAuthenticationOptions.RemoteCertificateValidationCallback;
                _state.TargetHost = options.ClientAuthenticationOptions.TargetHost;
            }

            _state.StateGCHandle = GCHandle.Alloc(_state);
            try
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
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

        internal override X509Certificate? RemoteCertificate => _state.RemoteCertificate;

        internal override SslApplicationProtocol NegotiatedApplicationProtocol => _negotiatedAlpnProtocol;

        internal override bool Connected => _state.Connected;

        private static uint HandleEventConnected(State state, ref ConnectionEvent connectionEvent)
        {
            if (state.Connected)
            {
                return MsQuicStatusCodes.Success;
            }

            if (state.IsServer)
            {
                state.Connected = true;
                MsQuicListener.State? listenerState = state.ListenerState;
                state.ListenerState = null;

                if (listenerState != null)
                {
                    if (listenerState.PendingConnections.TryRemove(state.Handle.DangerousGetHandle(), out MsQuicConnection? connection))
                    {
                        // Move connection from pending to Accept queue and hand it out.
                        if (listenerState.AcceptConnectionQueue.Writer.TryWrite(connection))
                        {
                            return MsQuicStatusCodes.Success;
                        }
                        // Listener is closed
                        connection.Dispose();
                    }
                }

                return MsQuicStatusCodes.UserCanceled;
            }
            else
            {
                // Connected will already be true for connections accepted from a listener.
                Debug.Assert(!Monitor.IsEntered(state));


                Debug.Assert(state.Connection != null);
                state.Connection._localEndPoint = MsQuicParameterHelpers.GetIPEndPointParam(MsQuicApi.Api, state.Handle, (uint)QUIC_PARAM_CONN.LOCAL_ADDRESS);
                state.Connection.SetNegotiatedAlpn(connectionEvent.Data.Connected.NegotiatedAlpn, connectionEvent.Data.Connected.NegotiatedAlpnLength);
                state.Connection = null;

                state.Connected = true;
                state.ConnectTcs!.SetResult(MsQuicStatusCodes.Success);
                state.ConnectTcs = null;
            }

            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventShutdownInitiatedByTransport(State state, ref ConnectionEvent connectionEvent)
        {
            if (!state.Connected && state.ConnectTcs != null)
            {
                Debug.Assert(state.Connection != null);
                state.Connection = null;

                uint hresult = connectionEvent.Data.ShutdownInitiatedByTransport.Status;
                Exception ex = QuicExceptionHelpers.CreateExceptionForHResult(hresult, "Connection has been shutdown by transport.");
                state.ConnectTcs!.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(ex));
                state.ConnectTcs = null;
            }

            // To throw QuicConnectionAbortedException (instead of QuicOperationAbortedException) out of AcceptStreamAsync() since
            // it wasn't our side who shutdown the connection.
            // We should rather keep the Status and propagate it either in a different exception or as a different field of QuicConnectionAbortedException.
            // See: https://github.com/dotnet/runtime/issues/60133
            state.AbortErrorCode = 0;
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

            if (state.ListenerState != null)
            {
                // This is inbound connection that never got connected - becasue of TLS validation or some other reason.
                // Remove connection from pending queue and dispose it.
                if (state.ListenerState.PendingConnections.TryRemove(state.Handle.DangerousGetHandle(), out MsQuicConnection? connection))
                {
                    connection.Dispose();
                }

                state.ListenerState = null;
            }

            state.Connection = null;

            state.ShutdownTcs.SetResult(MsQuicStatusCodes.Success);

            // Stop accepting new streams.
            state.AcceptQueue.Writer.TryComplete();

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
            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventPeerCertificateReceived(State state, ref ConnectionEvent connectionEvent)
        {
            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
            X509Chain? chain = null;
            X509Certificate2? certificate = null;
            X509Certificate2Collection? additionalCertificates = null;
            IntPtr certificateBuffer = IntPtr.Zero;
            int certificateLength = 0;

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
                            certificateBuffer = (IntPtr)quicBuffer[0].Buffer;
                            certificateLength = (int)quicBuffer[0].Length;

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
                    if (NetEventSource.Log.IsEnabled() && state.RemoteCertificateRequired) NetEventSource.Error(state, $"{state.TraceId} Remote certificate required, but no remote certificate received");
                    sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNotAvailable;
                }
                else
                {
                    chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = state.RevocationMode;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    chain.ChainPolicy.ApplicationPolicy.Add(state.IsServer ? s_clientAuthOid : s_serverAuthOid);

                    if (additionalCertificates != null && additionalCertificates.Count > 1)
                    {
                        chain.ChainPolicy.ExtraStore.AddRange(additionalCertificates);
                    }

                    sslPolicyErrors |= CertificateValidation.BuildChainAndVerifyProperties(chain, certificate, true, state.IsServer, state.TargetHost, certificateBuffer, certificateLength);
                }

                if (!state.RemoteCertificateRequired)
                {
                    sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNotAvailable;
                }

                state.RemoteCertificate = certificate;

                if (state.RemoteCertificateValidationCallback != null)
                {
                    bool success = state.RemoteCertificateValidationCallback(state, certificate, chain, sslPolicyErrors);
                    // Unset the callback to prevent multiple invocations of the callback per a single connection.
                    // Return the same value as the custom callback just did.
                    state.RemoteCertificateValidationCallback = (_, _, _, _) => success;

                    if (!success && NetEventSource.Log.IsEnabled())
                        NetEventSource.Error(state, $"{state.TraceId} Remote certificate rejected by verification callback");

                    if (!success)
                    {
                        if (state.IsServer)
                        {
                            return MsQuicStatusCodes.UserCanceled;
                        }

                        throw new AuthenticationException(SR.net_quic_cert_custom_validation);
                    }

                    return MsQuicStatusCodes.Success;
                }

                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(state, $"{state.TraceId} Certificate validation for '${certificate?.Subject}' finished with ${sslPolicyErrors}");


                if (sslPolicyErrors != SslPolicyErrors.None)
                {
                    if (state.IsServer)
                    {
                        return MsQuicStatusCodes.HandshakeFailure;
                    }

                    throw new AuthenticationException(SR.Format(SR.net_quic_cert_chain_validation, sslPolicyErrors));
                }

                return MsQuicStatusCodes.Success;
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(state, $"{state.TraceId} Certificate validation failed ${ex.Message}");
                throw;
            }
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

        private async ValueTask<QuicStreamProvider> OpenStreamAsync(QUIC_STREAM_OPEN_FLAGS flags, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (!Connected)
            {
                throw new InvalidOperationException(SR.net_quic_not_connected);
            }

            var stream = new MsQuicStream(_state, flags);

            try
            {
                await stream.StartAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                stream.Dispose();
                throw;
            }

            return stream;
        }

        internal override ValueTask<QuicStreamProvider> OpenUnidirectionalStreamAsync(CancellationToken cancellationToken = default) => OpenStreamAsync(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL, cancellationToken);
        internal override ValueTask<QuicStreamProvider> OpenBidirectionalStreamAsync(CancellationToken cancellationToken = default) => OpenStreamAsync(QUIC_STREAM_OPEN_FLAGS.NONE, cancellationToken);

        internal override int GetRemoteAvailableUnidirectionalStreamCount()
        {
            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            return MsQuicParameterHelpers.GetUShortParam(MsQuicApi.Api, _state.Handle, (uint)QUIC_PARAM_CONN.LOCAL_UNIDI_STREAM_COUNT);
        }

        internal override int GetRemoteAvailableBidirectionalStreamCount()
        {
            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            return MsQuicParameterHelpers.GetUShortParam(MsQuicApi.Api, _state.Handle, (uint)QUIC_PARAM_CONN.LOCAL_BIDI_STREAM_COUNT);
        }

        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_configuration is null)
            {
                throw new InvalidOperationException($"{nameof(ConnectAsync)} must not be called on a connection obtained from a listener.");
            }

            QUIC_ADDRESS_FAMILY af = _remoteEndPoint.AddressFamily switch
            {
                AddressFamily.Unspecified => QUIC_ADDRESS_FAMILY.UNSPEC,
                AddressFamily.InterNetwork => QUIC_ADDRESS_FAMILY.INET,
                AddressFamily.InterNetworkV6 => QUIC_ADDRESS_FAMILY.INET6,
                _ => throw new ArgumentException(SR.Format(SR.net_quic_unsupported_address_family, _remoteEndPoint.AddressFamily))
            };

            Debug.Assert(_state.StateGCHandle.IsAllocated);

            _state.Connection = this;
            uint status;
            string targetHost;
            int port;

            if (_remoteEndPoint is IPEndPoint ipEndPoint)
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                MsQuicParameterHelpers.SetIPEndPointParam(MsQuicApi.Api, _state.Handle, (uint)QUIC_PARAM_CONN.REMOTE_ADDRESS, ipEndPoint);
                targetHost = _state.TargetHost ?? ((IPEndPoint)_remoteEndPoint).Address.ToString();
                port = ((IPEndPoint)_remoteEndPoint).Port;

            }
            else if (_remoteEndPoint is DnsEndPoint dnsEndPoint)
            {
                port = dnsEndPoint.Port;
                string dnsHost = dnsEndPoint.Host!;

                // We don't have way how to set separate SNI and name for connection at this moment.
                // If the name is actually IP address we can use it to make at least some cases work for people
                // who want to bypass DNS but connect to specific virtual host.
                if (!string.IsNullOrEmpty(_state.TargetHost) && !dnsHost.Equals(_state.TargetHost, StringComparison.InvariantCultureIgnoreCase) && IPAddress.TryParse(dnsHost, out IPAddress? address))
                {
                    // This is form of IPAddress and _state.TargetHost is set to different string
                    Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                    MsQuicParameterHelpers.SetIPEndPointParam(MsQuicApi.Api, _state.Handle, (uint)QUIC_PARAM_CONN.REMOTE_ADDRESS, new IPEndPoint(address, port));
                    targetHost = _state.TargetHost!;
                }
                else
                {
                    targetHost = dnsHost;
                }
            }
            else
            {
                throw new ArgumentException($"Unsupported remote endpoint type '{_remoteEndPoint.GetType()}'.");
            }

            // We store TCS to local variable to avoid NRE if callbacks finish fast and set _state.ConnectTcs to null.
            var tcs = _state.ConnectTcs = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                status = MsQuicApi.Api.ConnectionStartDelegate(
                    _state.Handle,
                    _configuration,
                    af,
                    targetHost,
                    (ushort)port);

                QuicExceptionHelpers.ThrowIfFailed(status, "Failed to connect to peer.");

                // this handle is ref counted by MsQuic, so safe to dispose here.
                _configuration.Dispose();
                _configuration = null;
            }
            catch
            {
                _state.Connection = null;
                throw;
            }

            return new ValueTask(tcs.Task);
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
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
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

        private static unsafe uint NativeCallbackHandler(
            IntPtr connection,
            IntPtr context,
            ConnectionEvent* connectionEvent)
        {
            GCHandle gcHandle = GCHandle.FromIntPtr(context);
            Debug.Assert(gcHandle.IsAllocated);
            Debug.Assert(gcHandle.Target is not null);
            var state = (State)gcHandle.Target;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(state, $"{state.TraceId} Connection received event {connectionEvent->Type}");
            }

            try
            {
                switch (connectionEvent->Type)
                {
                    case QUIC_CONNECTION_EVENT_TYPE.CONNECTED:
                        return HandleEventConnected(state, ref *connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_TRANSPORT:
                        return HandleEventShutdownInitiatedByTransport(state, ref *connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_PEER:
                        return HandleEventShutdownInitiatedByPeer(state, ref *connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_COMPLETE:
                        return HandleEventShutdownComplete(state, ref *connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.PEER_STREAM_STARTED:
                        return HandleEventNewStream(state, ref *connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.STREAMS_AVAILABLE:
                        return HandleEventStreamsAvailable(state, ref *connectionEvent);
                    case QUIC_CONNECTION_EVENT_TYPE.PEER_CERTIFICATE_RECEIVED:
                        return HandleEventPeerCertificateReceived(state, ref *connectionEvent);
                    default:
                        return MsQuicStatusCodes.Success;
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(state, $"{state.TraceId} Exception occurred during handling {connectionEvent->Type} connection callback: {ex}");
                }

                if (state.ConnectTcs != null)
                {
                    // This is opportunistic if we get exception and have ability to propagate it to caller.
                    state.ConnectTcs.TrySetException(ex);
                    state.Connection = null;
                    state.ConnectTcs = null;
                }
                else
                {
                    Debug.Fail($"{state.TraceId} Exception occurred during handling {connectionEvent->Type} connection callback: {ex}");
                }

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

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(_state, $"{TraceId()} Connection disposing {disposing}");

            // If we haven't already shutdown gracefully (via a successful CloseAsync call), then force an abortive shutdown.
            if (_state.Handle != null && !_state.Handle.IsInvalid && !_state.Handle.IsClosed)
            {
                // Handle can be null if outbound constructor failed and we are called from finalizer.
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                MsQuicApi.Api.ConnectionShutdownDelegate(
                    _state.Handle,
                    QUIC_CONNECTION_SHUTDOWN_FLAGS.SILENT,
                    0);
            }

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
