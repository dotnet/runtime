// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicConnection : IDisposable
    {
        private static readonly Oid s_clientAuthOid = new Oid("1.3.6.1.5.5.7.3.2", "1.3.6.1.5.5.7.3.2");
        private static readonly Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.1");
        private const uint DefaultResetValue = 0xffffffff; // Arbitrary value unlikely to conflict with application protocols.

        // TODO: remove this.
        // This is only used for client-initiated connections, and isn't needed even then once Connect() has been called.
        private SafeMsQuicConfigurationHandle? _configuration;

        private readonly State _state = new State();
        private int _disposed;

        private bool _canAccept;

        private IPEndPoint? _localEndPoint;
        private readonly EndPoint _remoteEndPoint;
        private SslApplicationProtocol _negotiatedAlpnProtocol;

        internal sealed class State
        {
            public SafeMsQuicConnectionHandle Handle = null!; // set inside of MsQuicConnection ctor.

            public GCHandle StateGCHandle;

            // These exists to prevent GC of the MsQuicConnection in the middle of an async op (Connect or Shutdown).
            public MsQuicConnection? Connection;

            public readonly ValueTaskSource ConnectTcs = new ValueTaskSource();
            // TODO: only allocate these when there is an outstanding shutdown.
            public readonly TaskCompletionSource<int> ShutdownTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"{Handle} releasing handle after last stream.");
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

        // constructor for inbound connections
        internal unsafe MsQuicConnection(QUIC_HANDLE* handle, QUIC_NEW_CONNECTION_INFO* info)
        {
            _state.Handle = new SafeMsQuicConnectionHandle(handle);
            _state.StateGCHandle = GCHandle.Alloc(_state);
            _state.IsServer = true;

            try
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                MsQuicApi.Api.ApiTable->SetConnectionCallback(_state.Handle.QuicHandle, &NativeCallback, (void*)GCHandle.ToIntPtr(_state.StateGCHandle));
            }
            catch
            {
                _state.StateGCHandle.Free();
                throw;
            }

            _remoteEndPoint = info->RemoteAddress->ToIPEndPoint();
            _localEndPoint = info->LocalAddress->ToIPEndPoint();
            _negotiatedAlpnProtocol = new SslApplicationProtocol(new Span<byte>(info->NegotiatedAlpn, info->NegotiatedAlpnLength).ToArray());

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"{_state.Handle} Inbound connection created");
            }
        }

        // constructor for outbound connections
        public unsafe MsQuicConnection(QuicClientConnectionOptions options)
        {
            ArgumentNullException.ThrowIfNull(options.RemoteEndPoint, nameof(options.RemoteEndPoint));

            _canAccept = options.MaxBidirectionalStreams > 0 || options.MaxUnidirectionalStreams > 0;
            _remoteEndPoint = options.RemoteEndPoint;
            _configuration = SafeMsQuicConfigurationHandle.Create(options);
            _state.RemoteCertificateRequired = true;
            _state.RevocationMode = options.ClientAuthenticationOptions.CertificateRevocationCheckMode;
            _state.RemoteCertificateValidationCallback = options.ClientAuthenticationOptions.RemoteCertificateValidationCallback;
            _state.TargetHost = options.ClientAuthenticationOptions.TargetHost;

            _state.StateGCHandle = GCHandle.Alloc(_state);
            try
            {
                QUIC_HANDLE* handle;
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                ThrowIfFailure(MsQuicApi.Api.ApiTable->ConnectionOpen(
                    MsQuicApi.Api.Registration.QuicHandle,
                    &NativeCallback,
                    (void*)GCHandle.ToIntPtr(_state.StateGCHandle),
                    &handle), "Could not open the connection");
                _state.Handle = new SafeMsQuicConnectionHandle(handle);
            }
            catch
            {
                _state.StateGCHandle.Free();
                throw;
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"{_state.Handle} Outbound connection created");
            }
        }

        internal IPEndPoint? LocalEndPoint => _localEndPoint;

        internal EndPoint RemoteEndPoint => _remoteEndPoint;

        internal X509Certificate? RemoteCertificate => _state.RemoteCertificate;

        internal SslApplicationProtocol NegotiatedApplicationProtocol => _negotiatedAlpnProtocol;

        internal bool Connected => _state.ConnectTcs.IsCompleted;

        private static unsafe int HandleEventConnected(State state, ref QUIC_CONNECTION_EVENT connectionEvent)
        {
            if (state.ConnectTcs.IsCompleted)
            {
                return QUIC_STATUS_SUCCESS;
            }

            // Connected will already be true for connections accepted from a listener.
            Debug.Assert(!Monitor.IsEntered(state));

            Debug.Assert(state.Connection != null);
            //state.Connection._remoteEndPoint = MsQuicParameterHelpers.GetIPEndPointParam(MsQuicApi.Api, state.Handle, QUIC_PARAM_CONN_REMOTE_ADDRESS);
            state.Connection._localEndPoint = MsQuicParameterHelpers.GetIPEndPointParam(MsQuicApi.Api, state.Handle, QUIC_PARAM_CONN_LOCAL_ADDRESS);
            state.Connection._negotiatedAlpnProtocol = new SslApplicationProtocol(new Span<byte>(connectionEvent.CONNECTED.NegotiatedAlpn, connectionEvent.CONNECTED.NegotiatedAlpnLength).ToArray());
            state.Connection = null;

            state.ConnectTcs.TrySetResult();

            return QUIC_STATUS_SUCCESS;
        }

        private static int HandleEventShutdownInitiatedByTransport(State state, ref QUIC_CONNECTION_EVENT connectionEvent)
        {
            if (!state.ConnectTcs.IsCompleted)
            {
                Debug.Assert(state.Connection != null);
                state.Connection = null;

                state.ConnectTcs.TrySetException(new MsQuicException(connectionEvent.SHUTDOWN_INITIATED_BY_TRANSPORT.Status, "Connection has been shutdown by transport"));
            }

            // To throw QuicConnectionAbortedException (instead of QuicOperationAbortedException) out of AcceptStreamAsync() since
            // it wasn't our side who shutdown the connection.
            // We should rather keep the Status and propagate it either in a different exception or as a different field of QuicConnectionAbortedException.
            // See: https://github.com/dotnet/runtime/issues/60133
            state.AbortErrorCode = 0;
            state.AcceptQueue.Writer.TryComplete();
            return QUIC_STATUS_SUCCESS;
        }

        private static int HandleEventShutdownInitiatedByPeer(State state, ref QUIC_CONNECTION_EVENT connectionEvent)
        {
            state.AbortErrorCode = (long)connectionEvent.SHUTDOWN_INITIATED_BY_PEER.ErrorCode;
            state.AcceptQueue.Writer.TryComplete();
            return QUIC_STATUS_SUCCESS;
        }

        private static int HandleEventShutdownComplete(State state, ref QUIC_CONNECTION_EVENT connectionEvent)
        {
            // This is the final event on the connection, so free the GCHandle used by the event callback.
            state.StateGCHandle.Free();

            state.Connection = null;

            state.ShutdownTcs.SetResult(QUIC_STATUS_SUCCESS);

            // Stop accepting new streams.
            state.AcceptQueue.Writer.TryComplete();

            return QUIC_STATUS_SUCCESS;
        }

        private static unsafe int HandleEventNewStream(State state, ref QUIC_CONNECTION_EVENT connectionEvent)
        {
            var streamHandle = new SafeMsQuicStreamHandle(connectionEvent.PEER_STREAM_STARTED.Stream);
            if (!state.TryQueueNewStream(streamHandle, connectionEvent.PEER_STREAM_STARTED.Flags))
            {
                // This will call StreamCloseDelegate and free the stream.
                // We will return Success to the MsQuic to prevent double free.
                streamHandle.Dispose();
            }

            return QUIC_STATUS_SUCCESS;
        }

        private static int HandleEventStreamsAvailable(State state, ref QUIC_CONNECTION_EVENT connectionEvent)
        {
            return QUIC_STATUS_SUCCESS;
        }

        private static unsafe int HandleEventPeerCertificateReceived(State state, ref QUIC_CONNECTION_EVENT connectionEvent)
        {
            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
            X509Chain? chain = null;
            X509Certificate2? certificate = null;
            X509Certificate2Collection? additionalCertificates = null;
            IntPtr certificateBuffer = IntPtr.Zero;
            int certificateLength = 0;

            try
            {
                IntPtr certificateHandle = (IntPtr)connectionEvent.PEER_CERTIFICATE_RECEIVED.Certificate;
                if (certificateHandle != IntPtr.Zero)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        certificate = new X509Certificate2(certificateHandle);
                    }
                    else
                    {
                        unsafe
                        {
                            QUIC_BUFFER* certBuffer = (QUIC_BUFFER*)certificateHandle;
                            certificate = new X509Certificate2(new ReadOnlySpan<byte>(certBuffer->Buffer, (int)certBuffer->Length));
                            certificateBuffer = (IntPtr)certBuffer->Buffer;
                            certificateLength = (int)certBuffer->Length;

                            IntPtr chainHandle = (IntPtr)connectionEvent.PEER_CERTIFICATE_RECEIVED.Chain;
                            if (chainHandle != IntPtr.Zero)
                            {
                                QUIC_BUFFER* chainBuffer = (QUIC_BUFFER*)chainHandle;
                                if (chainBuffer->Length != 0 && chainBuffer->Buffer != null)
                                {
                                    additionalCertificates = new X509Certificate2Collection();
                                    additionalCertificates.Import(new ReadOnlySpan<byte>(chainBuffer->Buffer, (int)chainBuffer->Length));
                                }
                            }
                        }
                    }
                }

                if (certificate == null)
                {
                    if (NetEventSource.Log.IsEnabled() && state.RemoteCertificateRequired) NetEventSource.Error(state, $"{state.Handle} Remote certificate required, but no remote certificate received");
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
                        NetEventSource.Error(state, $"{state.Handle} Remote certificate rejected by verification callback");

                    if (!success)
                    {
                        if (state.IsServer)
                        {
                            return QUIC_STATUS_USER_CANCELED;
                        }

                        throw new AuthenticationException(SR.net_quic_cert_custom_validation);
                    }

                    return QUIC_STATUS_SUCCESS;
                }

                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(state, $"{state.Handle} Certificate validation for '${certificate?.Subject}' finished with ${sslPolicyErrors}");


                if (sslPolicyErrors != SslPolicyErrors.None)
                {
                    if (state.IsServer)
                    {
                        return QUIC_STATUS_HANDSHAKE_FAILURE;
                    }

                    throw new AuthenticationException(SR.Format(SR.net_quic_cert_chain_validation, sslPolicyErrors));
                }

                return QUIC_STATUS_SUCCESS;
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(state, $"{state.Handle} Certificate validation failed ${ex.Message}");
                throw;
            }
        }

        internal async ValueTask<MsQuicStream> AcceptStreamAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed == 1, this);

            if (!_canAccept)
            {
                throw new InvalidOperationException(SR.net_quic_accept_not_allowed);
            }

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

        private async ValueTask<MsQuicStream> OpenStreamAsync(QUIC_STREAM_OPEN_FLAGS flags, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed == 1, this);
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

        internal ValueTask<MsQuicStream> OpenUnidirectionalStreamAsync(CancellationToken cancellationToken = default)
            => OpenStreamAsync(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL, cancellationToken);
        internal ValueTask<MsQuicStream> OpenBidirectionalStreamAsync(CancellationToken cancellationToken = default)
            => OpenStreamAsync(QUIC_STREAM_OPEN_FLAGS.NONE, cancellationToken);

        internal int GetRemoteAvailableUnidirectionalStreamCount()
        {
            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            return MsQuicParameterHelpers.GetUShortParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_CONN_LOCAL_UNIDI_STREAM_COUNT);
        }

        internal int GetRemoteAvailableBidirectionalStreamCount()
        {
            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            return MsQuicParameterHelpers.GetUShortParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_CONN_LOCAL_BIDI_STREAM_COUNT);
        }

        internal unsafe ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed == 1, this);

            if (_configuration is null)
            {
                throw new InvalidOperationException($"{nameof(ConnectAsync)} must not be called on a connection obtained from a listener.");
            }

            ushort af = _remoteEndPoint.AddressFamily switch
            {
                AddressFamily.Unspecified => (ushort)QUIC_ADDRESS_FAMILY_UNSPEC,
                AddressFamily.InterNetwork => (ushort)QUIC_ADDRESS_FAMILY_INET,
                AddressFamily.InterNetworkV6 => (ushort)QUIC_ADDRESS_FAMILY_INET6,
                _ => throw new ArgumentException(SR.Format(SR.net_quic_unsupported_address_family, _remoteEndPoint.AddressFamily))
            };

            Debug.Assert(_state.StateGCHandle.IsAllocated);

            if (_state.ConnectTcs.TryInitialize(out ValueTask valueTask, cancellationToken: cancellationToken))
            {
                _state.Connection = this;
                string targetHost;
                int port;

                if (_remoteEndPoint is IPEndPoint ipEndPoint)
                {
                    Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                    MsQuicParameterHelpers.SetIPEndPointParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_CONN_REMOTE_ADDRESS, ipEndPoint);
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
                    if (!dnsHost.Equals(_state.TargetHost, StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty(_state.TargetHost))
                    {
                        targetHost = _state.TargetHost!;
                        if (IPAddress.TryParse(dnsHost, out IPAddress? address))
                        {
                            // This is form of IPAddress and _state.TargetHost is set to different string
                            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                            MsQuicParameterHelpers.SetIPEndPointParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_CONN_REMOTE_ADDRESS, new IPEndPoint(address, port));
                        }
                        else
                        {
                            IPAddress[] addresses = Dns.GetHostAddressesAsync(dnsHost, cancellationToken).GetAwaiter().GetResult();
                            cancellationToken.ThrowIfCancellationRequested();
                            if (addresses.Length == 0)
                            {
                                throw new SocketException((int)SocketError.HostNotFound);
                            }
                            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                            // We can do something better than just using first IP but that is what
                            // MsQuic does today anyway.
                            MsQuicParameterHelpers.SetIPEndPointParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_CONN_REMOTE_ADDRESS, new IPEndPoint(addresses[0], port));
                        }
                    }
                    else
                    {
                        // We defer everything to MsQuic.
                        targetHost = dnsHost;
                    }
                }
                else
                {
                    throw new ArgumentException($"Unsupported remote endpoint type '{_remoteEndPoint.GetType()}'.");
                }

                IntPtr pTargetHost = Marshal.StringToCoTaskMemAnsi(targetHost);
                try
                {
                    Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                    ThrowIfFailure(MsQuicApi.Api.ApiTable->ConnectionStart(
                        _state.Handle.QuicHandle,
                        _configuration.QuicHandle,
                        af,
                        (sbyte*)pTargetHost,
                        (ushort)port), "Failed to connect to peer");

                    // this handle is ref counted by MsQuic, so safe to dispose here.
                    _configuration.Dispose();
                    _configuration = null;
                }
                catch
                {
                    _state.Connection = null;
                    throw;
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pTargetHost);
                }
            }

            return valueTask;
        }

        internal unsafe ValueTask FinishHandshakeAsync(QuicServerConnectionOptions options, string? targetHost, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed == 1, this);

            if (_state.ConnectTcs.TryInitialize(out var valueTask, this, cancellationToken))
            {
                _canAccept = options.MaxBidirectionalStreams > 0 || options.MaxUnidirectionalStreams > 0;
                _state.Connection = this;
                try
                {
                    _state.RemoteCertificateRequired = options.ServerAuthenticationOptions.ClientCertificateRequired;
                    _state.RevocationMode = options.ServerAuthenticationOptions.CertificateRevocationCheckMode;
                    _state.RemoteCertificateValidationCallback = options.ServerAuthenticationOptions.RemoteCertificateValidationCallback;
                    _configuration = SafeMsQuicConfigurationHandle.Create(options, options.ServerAuthenticationOptions, targetHost);
                    ThrowIfFailure(MsQuicApi.Api.ApiTable->ConnectionSetConfiguration(
                        _state.Handle.QuicHandle,
                        _configuration.QuicHandle));
                }
                catch
                {
                    _state.Connection = null;
                    throw;
                }
            }

            return valueTask;
        }

        private unsafe ValueTask ShutdownAsync(
            QUIC_CONNECTION_SHUTDOWN_FLAGS Flags,
            long ErrorCode)
        {
            // Store the connection into the GCHandle'd state to prevent GC if user calls ShutdownAsync and gets rid of all references to the MsQuicConnection.
            Debug.Assert(_state.Connection == null);
            _state.Connection = this;

            try
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                MsQuicApi.Api.ApiTable->ConnectionShutdown(
                    _state.Handle.QuicHandle,
                    Flags,
                    (ulong)ErrorCode);
            }
            catch
            {
                _state.Connection = null;
                throw;
            }

            return new ValueTask(_state.ShutdownTcs.Task);
        }

#pragma warning disable CS3016
        [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
        private static unsafe int NativeCallback(QUIC_HANDLE* connection, void* context, QUIC_CONNECTION_EVENT* connectionEvent)
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)context);
            Debug.Assert(gcHandle.IsAllocated);
            Debug.Assert(gcHandle.Target is not null);
            var state = (State)gcHandle.Target;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(state, $"{state.Handle} Connection received event {connectionEvent->Type}");
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
                        return QUIC_STATUS_SUCCESS;
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(state, $"{state.Handle} Exception occurred during handling {connectionEvent->Type} connection callback: {ex}");
                }

                if (!state.ConnectTcs.IsCompleted)
                {
                    // This is opportunistic if we get exception and have ability to propagate it to caller.
                    state.ConnectTcs.TrySetException(ex);
                    state.Connection = null;
                }
                else
                {
                    Debug.Fail($"{state.Handle} Exception occurred during handling {connectionEvent->Type} connection callback: {ex}");
                }

                // TODO: trigger an exception on any outstanding async calls.
                return QUIC_STATUS_INTERNAL_ERROR;
            }
        }

        public void Dispose()
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

        private unsafe void Dispose(bool disposing)
        {
            int disposed = Interlocked.Exchange(ref _disposed, 1);
            if (disposed != 0)
            {
                return;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(_state, $"{_state.Handle} Connection disposing {disposing}");

            // If we haven't already shutdown gracefully (via a successful CloseAsync call), then force an abortive shutdown.
            if (_state.Handle != null && !_state.Handle.IsInvalid && !_state.Handle.IsClosed)
            {
                // Handle can be null if outbound constructor failed and we are called from finalizer.
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                MsQuicApi.Api.ApiTable->ConnectionShutdown(
                    _state.Handle.QuicHandle,
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(_state, $"{_state.Handle} Connection releasing handle");

                // We may not be fully initialized if constructor fails.
                _state.Handle?.Dispose();
            }
        }

        // TODO: this appears abortive and will cause prior successfully shutdown and closed streams to drop data.
        // It's unclear how to gracefully wait for a connection to be 100% done.
        internal ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
        {
            if (_disposed == 1)
            {
                return default;
            }

            return ShutdownAsync(QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE, errorCode);
        }
    }
}
