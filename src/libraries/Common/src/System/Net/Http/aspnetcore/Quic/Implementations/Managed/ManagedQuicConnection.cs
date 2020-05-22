#nullable enable

using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed
{
    internal sealed partial class ManagedQuicConnection : QuicConnectionProvider
    {
        private const int RequiredCongestionWindowSizeForSending = 50;

        private readonly SingleEventValueTaskSource _connectTcs = new SingleEventValueTaskSource();

        private readonly SingleEventValueTaskSource _closeTcs = new SingleEventValueTaskSource();

        /// <summary>
        ///     Timestamp when last <see cref="ConnectionCloseFrame"/> was sent, or 0 if no such frame was sent yet.
        /// </summary>
        private long _lastConnectionCloseSentTimestamp;

        private readonly PacketNumberSpace[] _pnSpaces = new PacketNumberSpace[3]
        {
            new PacketNumberSpace(), new PacketNumberSpace(), new PacketNumberSpace()
        };

        /// <summary>
        ///     Recovery controller used for this connection.
        /// </summary>
        private RecoveryController Recovery { get; } = new RecoveryController();

        /// <summary>
        ///     If true, the connection is in draining state. The connection MUST not send packets in such state. The
        ///     The connection transitions to closed at <see cref="_closingPeriodEndTimestamp"/> at the latest.
        /// </summary>
        private bool _isDraining;

        /// <summary>
        ///     If true, the connection is in closing or draining state and will be considered close at
        ///     <see cref="_closingPeriodEndTimestamp"/> at the latest.
        /// </summary>
        private bool IsClosing => _closingPeriodEndTimestamp != null;

        /// <summary>
        ///     Timestamp when the connection close will be initiated due to lack of packets from peer.
        /// </summary>
        private long _idleTimeout = long.MaxValue; // use infinite by default

        /// <summary>
        ///     True if an ack-eliciting packet has been sent since last receiving an ack-eliciting packet.
        /// </summary>
        private bool _ackElicitingWasSentSinceLastReceive;

        /// <summary>
        ///     Timestamp when the closing period will be end and the connection will be considered closed.
        /// </summary>
        private long? _closingPeriodEndTimestamp;

        /// <summary>
        ///     True if the connection is in closed state.
        /// </summary>
        internal bool IsClosed => _closeTcs.IsSet;

        /// <summary>
        ///     Gets the current state of the connection.
        /// </summary>
        internal QuicConnectionState ConnectionState
        {
            get
            {
                if (IsClosed) return QuicConnectionState.Closed;
                if (_isDraining) return QuicConnectionState.Draining;
                if (IsClosing) return QuicConnectionState.Closing;
                if (Connected) return QuicConnectionState.Connected;
                return QuicConnectionState.None;
            }
        }

        /// <summary>
        ///     QUIC transport parameters used for this endpoint.
        /// </summary>
        private readonly TransportParameters _localTransportParameters;

        private readonly Tls _tls;

        /// <summary>
        ///     Remote endpoint address.
        /// </summary>
        private readonly IPEndPoint _remoteEndpoint;

        /// <summary>
        ///     GCHandle for this object.
        /// </summary>
        private GCHandle _gcHandle;

        /// <summary>
        ///     Context of the socket serving this connection.
        /// </summary>
        private readonly QuicSocketContext _socketContext;

        /// <summary>
        ///     True if handshake has been confirmed by the peer. For server this means that TLS has reported handshake complete,
        ///     for client it means that a HANDSHAKE_DONE frame has been received.
        /// </summary>
        private bool HandshakeConfirmed => _isServer ? _tls.IsHandshakeComplete : _handshakeDoneReceived;

        /// <summary>
        ///     True if HANDSHAKE_DONE frame has been received. Valid only for client.
        /// </summary>
        private bool _handshakeDoneReceived;

        /// <summary>
        ///     True if HANDSHAKE_DONE frame has been sent. Valid only for server.
        /// </summary>
        private bool _handshakeDoneSent;

        /// <summary>
        ///     True if this side of connection belongs to the server.
        /// </summary>
        private readonly bool _isServer;

        /// <summary>
        ///     Collection of streams for this connection.
        /// </summary>
        private readonly StreamCollection _streams = new StreamCollection();

        /// <summary>
        ///     Collection of local connection ids used by this endpoint.
        /// </summary>
        private readonly ConnectionIdCollection _localConnectionIdCollection = new ConnectionIdCollection();

        /// <summary>
        ///     Collection of local connection ids used by remote endpoint.
        /// </summary>
        private readonly ConnectionIdCollection _remoteConnectionIdCollection = new ConnectionIdCollection();

        /// <summary>
        ///     Flow control limits set by this endpoint for the peer for the entire connection.
        /// </summary>
        private ConnectionFlowControlLimits _localLimits = default;

        /// <summary>
        ///     Values of <see cref="_localLimits"/> that peer has confirmed received.
        /// </summary>
        private ConnectionFlowControlLimits _peerReceivedLocalLimits;

        /// <summary>
        ///     Flow control limits set by the peer for this endpoint for the entire connection.
        /// </summary>
        private ConnectionFlowControlLimits _peerLimits = default;

        /// <summary>
        ///     QUIC transport parameters requested by peer endpoint.
        /// </summary>
        private TransportParameters _peerTransportParameters = TransportParameters.Default;

        /// <summary>
        ///     Error received via CONNECTION_CLOSE frame to be reported to the user.
        /// </summary>
        private QuicError? _inboundError;

        /// <summary>
        ///     Error to send in next packet in a CONNECTION_CLOSE frame.
        /// </summary>
        private QuicError? _outboundError;

        /// <summary>
        ///     Version of the QUIC protocol used for this connection.
        /// </summary>
        private readonly QuicVersion version = QuicVersion.Draft27;

        /// <summary>
        ///     Timer when at the latest the next ACK frame should be sent.
        /// </summary>
        private long _nextAckTimer = long.MaxValue;

        /// <summary>
        ///     True if PING frame should be sent during next flight.
        /// </summary>
        private bool _pingWanted;

        /// <summary>
        ///     True if this instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        ///     Requests sending PING frame to the peer, requiring the peer to send acknowledgement back.
        /// </summary>
        internal void Ping()
        {
            _pingWanted = true;
        }

        // client constructor
        public ManagedQuicConnection(QuicClientConnectionOptions options)
        {
            _isServer = false;

            _remoteEndpoint = options.RemoteEndPoint!;

            var listenEndPoint = options.LocalEndPoint ?? new IPEndPoint(
                _remoteEndpoint.AddressFamily == AddressFamily.InterNetwork
                    ? IPAddress.Any
                    : IPAddress.IPv6Any, 0);
            _socketContext = new SingleConnectionSocketContext(listenEndPoint, this);
            _localTransportParameters = TransportParameters.FromClientConnectionOptions(options);
            _gcHandle = GCHandle.Alloc(this);
            _tls = new Tls(_gcHandle, options, _localTransportParameters);

            // init random connection ids for the client
            SourceConnectionId = ConnectionId.Random(ConnectionId.DefaultCidSize);
            DestinationConnectionId = ConnectionId.Random(ConnectionId.DefaultCidSize);
            _localConnectionIdCollection.Add(SourceConnectionId);

            // derive also clients initial secrets.
            DeriveInitialProtectionKeys(DestinationConnectionId.Data);

            // generate first Crypto frames
            _tls.DoHandshake();

            _localLimits.UpdateMaxData(_localTransportParameters.InitialMaxData);
            _localLimits.UpdateMaxStreamsBidi(_localTransportParameters.InitialMaxStreamsBidi);
            _localLimits.UpdateMaxStreamsUni(_localTransportParameters.InitialMaxStreamsUni);
            _peerReceivedLocalLimits = _localLimits;
        }

        // server constructor
        public ManagedQuicConnection(QuicListenerOptions options, QuicServerSocketContext socketContext,
            IPEndPoint remoteEndpoint)
        {
            _isServer = true;
            _socketContext = socketContext;
            _remoteEndpoint = remoteEndpoint;
            _localTransportParameters = TransportParameters.FromListenerOptions(options);

            _gcHandle = GCHandle.Alloc(this);
            _tls = new Tls(_gcHandle, options, _localTransportParameters);

            _localLimits.UpdateMaxData(_localTransportParameters.InitialMaxData);
            _localLimits.UpdateMaxStreamsBidi(_localTransportParameters.InitialMaxStreamsBidi);
            _localLimits.UpdateMaxStreamsUni(_localTransportParameters.InitialMaxStreamsUni);
            _peerReceivedLocalLimits = _localLimits;
        }

        /// <summary>
        ///     Connection ID used by this endpoint to identify packets for this connection.
        /// </summary>
        public ConnectionId? SourceConnectionId { get; private set; }

        /// <summary>
        ///     Connection ID used by the peer to identify packets for this connection.
        /// </summary>
        public ConnectionId? DestinationConnectionId { get; private set; }

        /// <summary>
        ///     Returns timestamp of the next timer event, after timeout, <see cref="OnTimeout"/> should be called.
        /// </summary>
        /// <returns>Timestamp in ticks of the next timer or long.MaxValue if no timer is needed.</returns>
        internal long GetNextTimerTimestamp()
        {
            if (_closeTcs.IsSet)
            {
                // connection already closed, no timer needed
                return long.MaxValue;
            }

            long timer = _idleTimeout;

            if (_closingPeriodEndTimestamp != null)
            {
                // no other timer besides idle timeout and closing period makes sense when closing.
                return Math.Min(timer, _closingPeriodEndTimestamp.Value);
            }

            // do not incorporate next ack timer if we cannot send ack anyway
            if (Recovery.GetAvailableCongestionWindowBytes() >= RequiredCongestionWindowSizeForSending)
            {
                timer = Math.Min(timer, _nextAckTimer);
            }

            return Math.Min(timer, Recovery.LossRecoveryTimer);
        }

        internal void OnTimeout(long timestamp)
        {
            if (_closingPeriodEndTimestamp.HasValue)
            {
                if (timestamp >= _closingPeriodEndTimestamp)
                {
                    SignalConnectionClose();
                }
                return;
            }

            if (timestamp >= _idleTimeout)
            {
                // TODO-RZ: Force close the connection with error
                SignalConnectionClose();
            }

            if (timestamp >= Recovery.LossRecoveryTimer)
            {
                Recovery.OnLossDetectionTimeout(_tls.IsHandshakeComplete, timestamp);
            }
        }

        /// <summary>
        ///     Advances the cryptographic handshake based on received data.
        /// </summary>
        private void DoHandshake()
        {
            var status = _tls.DoHandshake();

            if (status == SslError.Ssl)
            {
                CloseConnection(TransportErrorCode.InternalError, "SSL error");
                return;
            }

            if (ReferenceEquals(_peerTransportParameters, TransportParameters.Default))
            {
                var param = _tls.GetPeerTransportParameters(_isServer);

                if (param == null)
                {
                    // failed to retrieve transport parameters.
                    CloseConnection(TransportErrorCode.TransportParameterError);
                    return;
                }

                ref ConnectionFlowControlLimits limits = ref _peerLimits;

                limits.UpdateMaxData(param.InitialMaxData);
                limits.UpdateMaxStreamsBidi(param.InitialMaxStreamsBidi);
                limits.UpdateMaxStreamsUni(param.InitialMaxStreamsUni);

                Recovery.MaxAckDelay = Timestamp.FromMilliseconds(param.MaxAckDelay);

                _peerTransportParameters = param;
            }
        }

        /// <summary>
        ///     Derives initial protection keys based on the destination connection id sent by the client.
        /// </summary>
        /// <param name="dcid">Destination connection ID sent from client-sent packets.</param>
        private void DeriveInitialProtectionKeys(byte[] dcid)
        {
            var client = KeyDerivation.DeriveClientInitialSecret(dcid);
            var server = KeyDerivation.DeriveServerInitialSecret(dcid);

            var algorithm = QuicConstants.InitialCipherSuite;

            if (_isServer)
            {
                SetEncryptionSecrets(EncryptionLevel.Initial, algorithm, client, server);
            }
            else
            {
                SetEncryptionSecrets(EncryptionLevel.Initial, algorithm, server, client);
            }
        }

        /// <summary>
        ///     Gets <see cref="EncryptionLevel"/> at which the next packet should be sent.
        /// </summary>
        internal EncryptionLevel GetWriteLevel(long timestamp)
        {
            // if there is a probe waiting to be sent on any level, send it.
            // Because probe packets are not limited by congestion window, this avoids a live-lock in
            // scenario where there is a pending ack in e.g. Initial epoch, but the connection cannot
            // send it because it is limited by congestion window, because it has in-flight packets
            // in Handshake epoch.
            var probeSpace = PacketSpace.Initial;
            for (int i = 1; i < _pnSpaces.Length; i++)
            {
                var packetSpace = (PacketSpace)i;
                var recoverySpace = Recovery.GetPacketNumberSpace(packetSpace);
                if (recoverySpace.RemainingLossProbes > Recovery.GetPacketNumberSpace(probeSpace).RemainingLossProbes)
                {
                    probeSpace = packetSpace;
                }
            }

            if (Recovery.GetPacketNumberSpace(probeSpace).RemainingLossProbes > 0)
            {
                return (EncryptionLevel)probeSpace;
            }

            if (Recovery.GetAvailableCongestionWindowBytes() < RequiredCongestionWindowSizeForSending)
            {
                // can't send anything anyway
                return EncryptionLevel.None;
            }

            // if pending errors, send them in appropriate epoch,
            if (_outboundError != null && _outboundError.IsQuicError)
            {
                EncryptionLevel desiredLevel = _tls.WriteLevel;
                if (!Connected && desiredLevel == EncryptionLevel.Application)
                {
                    // don't use application level if handshake is not complete
                    return EncryptionLevel.Handshake;
                }

                return desiredLevel;
            }

            for (int i = 0; i < _pnSpaces.Length; i++)
            {
                var level = (EncryptionLevel)i;
                var pnSpace = _pnSpaces[i];
                var recoverySpace = Recovery.GetPacketNumberSpace((PacketSpace)i);

                // to advance handshake
                if (pnSpace.CryptoOutboundStream.IsFlushable ||
                    // resend lost data
                    recoverySpace.LostPackets.Count > 0 ||
                    // send acknowledgement if needed, prefer sending acks in Initial and Handshake
                    // immediately since there is a great chance of coalescing with next level
                    (i < 3 ? pnSpace.AckElicited : pnSpace.NextAckTimer <= timestamp))
                    return level;
            }

            // otherwise check if we have something to send.
            // TODO-RZ: this list may be incomplete
            if (_pingWanted ||
                _streams.HasFlushableStreams ||
                _streams.HasUpdateableStreams ||
                _outboundError != null)
            {
                return EncryptionLevel.Application;
            }

            // otherwise we have no data to send.
            return EncryptionLevel.None;
        }

        private static PacketSpace GetPacketSpace(PacketType packetType)
        {
            return packetType switch
            {
                PacketType.Initial => PacketSpace.Initial,
                PacketType.ZeroRtt => PacketSpace.Application,
                PacketType.Handshake => PacketSpace.Handshake,
                PacketType.OneRtt => PacketSpace.Application,
                _ => throw new ArgumentOutOfRangeException(nameof(packetType), packetType, null)
            };
        }

        private static EncryptionLevel GetEncryptionLevel(PacketType packetType)
        {
            return packetType switch
            {
                PacketType.Initial => EncryptionLevel.Initial,
                PacketType.Handshake => EncryptionLevel.Handshake,
                PacketType.ZeroRtt => EncryptionLevel.EarlyData,
                PacketType.OneRtt => EncryptionLevel.Application,
                PacketType.Retry => EncryptionLevel.None,
                PacketType.VersionNegotiation => EncryptionLevel.None,
                _ => throw new ArgumentOutOfRangeException(nameof(packetType), packetType, null)
            };
        }

        /// <summary>
        ///     Gets instance of <see cref="PacketNumberSpace"/> associated with the given encryption level.
        /// </summary>
        /// <param name="encryptionLevel">The encryption level.</param>
        internal PacketNumberSpace GetPacketNumberSpace(EncryptionLevel encryptionLevel)
        {
            return encryptionLevel switch
            {
                EncryptionLevel.Initial => _pnSpaces[0],
                EncryptionLevel.Handshake => _pnSpaces[1],
                EncryptionLevel.EarlyData => _pnSpaces[2],
                EncryptionLevel.Application => _pnSpaces[2],
                _ => throw new ArgumentOutOfRangeException(nameof(encryptionLevel), encryptionLevel, null)
            };
        }

        /// <summary>
        ///     Prepares the connection for termination due to an error. The connection will start closing once the
        ///     when the error is actually sent.
        /// </summary>
        /// <param name="errorCode">The error code identifying the nature of the error.</param>
        /// <param name="reason">Optional short human-readable reason for closing.</param>
        /// <param name="frameType">Optional type of the frame which was being processed when the error was encountered</param>
        /// <returns>Always returns <see cref="ProcessPacketResult.Error"/> to simplify packet processing code</returns>
        private ProcessPacketResult CloseConnection(TransportErrorCode errorCode, string? reason = null,
            FrameType frameType = FrameType.Padding)
        {
            _outboundError = new QuicError(errorCode, reason, frameType);
            return ProcessPacketResult.Error;
        }

        internal async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await CloseAsync((long)TransportErrorCode.NoError).ConfigureAwait(false);

            // TODO-RZ: this may be dangerous, since _tls is accessed from background thread.
            _tls.Dispose();
            _gcHandle.Free();

            _disposed = true;
        }

        public override void Dispose()
        {
            // TODO-RZ: I don't like this, but there does not seem to be a better way, unless we just want to do
            // fire-and-forget
            DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        internal void SetEncryptionSecrets(EncryptionLevel level, TlsCipherSuite algorithm,
            ReadOnlySpan<byte> readSecret, ReadOnlySpan<byte> writeSecret)
        {
            // TODO-RZ: is it wise to log secrets to event source?
            if (NetEventSource.IsEnabled) NetEventSource.SetEncryptionSecrets(this, level, algorithm, readSecret, writeSecret);

            var pnSpace = GetPacketNumberSpace(level);
            Debug.Assert(pnSpace.SendCryptoSeal == null, "Protection keys already derived");

            pnSpace.RecvCryptoSeal = CryptoSeal.Create(algorithm, readSecret);
            pnSpace.SendCryptoSeal = CryptoSeal.Create(algorithm, writeSecret);
        }

        internal int HandleSetEncryptionSecrets(EncryptionLevel level, ReadOnlySpan<byte> readSecret,
            ReadOnlySpan<byte> writeSecret)
        {
            var alg = _tls.GetNegotiatedCipher();
            SetEncryptionSecrets(level, alg, readSecret, writeSecret);

            return 1;
        }

        internal int HandleAddHandshakeData(EncryptionLevel level, ReadOnlySpan<byte> data)
        {
            OutboundBuffer cryptoOutboundStream = GetPacketNumberSpace(level).CryptoOutboundStream;
            cryptoOutboundStream.Enqueue(data);
            cryptoOutboundStream.ForceFlushPartialChunk();
            return 1;
        }

        internal int HandleFlush()
        {
            return 1;
        }

        internal int HandleSendAlert(EncryptionLevel level, TlsAlert alert)
        {
            // RFC: A TLS alert is turned into a QUIC connection error by converting the
            // one-byte alert description into a QUIC error code.  The alert
            // description is added to 0x100 to produce a QUIC error code from the
            // range reserved for CRYPTO_ERROR.  The resulting value is sent in a
            // QUIC CONNECTION_CLOSE frame.

            _outboundError = new QuicError((TransportErrorCode) alert + 0x100, $"Tls alert - {alert}");
            return 1;
        }

        internal enum ProcessPacketResult
        {
            /// <summary>
            ///     Packet processed without errors.
            /// </summary>
            Ok,

            /// <summary>
            ///     Packet is discarded. E.g. because it could not be decrypted (yet).
            /// </summary>
            DropPacket,

            /// <summary>
            ///     Packet is valid but violates the protocol, the connection should be closed.
            /// </summary>
            Error
        }

        #region Public API

        internal override bool Connected => HandshakeConfirmed;

        internal override IPEndPoint LocalEndPoint => _socketContext.LocalEndPoint;

        internal override IPEndPoint RemoteEndPoint => new IPEndPoint(_remoteEndpoint.Address, _remoteEndpoint.Port);

        internal override async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfError();

            if (Connected) return;

            if (NetEventSource.IsEnabled) NetEventSource.NewClientConnection(this, SourceConnectionId!.Data, DestinationConnectionId!.Data);
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            _socketContext.Ping();
            _socketContext.Start();

            await _connectTcs.GetTask().ConfigureAwait(false);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override QuicStreamProvider OpenUnidirectionalStream()
        {
            ThrowIfDisposed();
            ThrowIfError();

            return OpenStream(true);
        }

        internal override QuicStreamProvider OpenBidirectionalStream()
        {
            ThrowIfDisposed();
            ThrowIfError();

            return OpenStream(false);
        }

        internal override long GetRemoteAvailableUnidirectionalStreamCount()
        {
            ThrowIfDisposed();
            ThrowIfError();

            return _peerLimits.MaxStreamsUni;
        }

        internal override long GetRemoteAvailableBidirectionalStreamCount()
        {
            ThrowIfDisposed();
            ThrowIfError();

            return _peerLimits.MaxStreamsBidi;
        }

        internal override async ValueTask<QuicStreamProvider>
            AcceptStreamAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfError();

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            var stream = await _streams.IncomingStreams.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            return stream;
        }

        internal override SslApplicationProtocol NegotiatedApplicationProtocol => _tls.GetAlpnProtocol();

        internal override async ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!Connected)
            {
                // abandon connection attempt
                _connectTcs.TryCompleteException(new QuicConnectionAbortedException(errorCode));
                _closeTcs.TryComplete();
                return;
            }

            if (IsClosed) return;
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            _outboundError = new QuicError((TransportErrorCode)errorCode, null, FrameType.Padding, false);
            _socketContext.Ping();

            await _closeTcs.GetTask().ConfigureAwait(false);
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        #endregion

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ManagedQuicConnection));
            }
        }

        internal void ThrowIfError()
        {
            var error = _inboundError ?? _outboundError;
            // don't throw if connection was closed gracefully. By doing so, we still allow retrieving
            // unread data/streams if the connection was closed by the peer.
            if (error != null && error.ErrorCode != TransportErrorCode.NoError)
            {
                throw MakeAbortedException(error);
            }
        }

        private void DropPacketNumberSpace(PacketSpace space, ObjectPool<SentPacket> sentPacketPool)
        {
            // TODO-RZ: discard the PacketNumberSpace instance and let GC collect it?
            var pnSpace = _pnSpaces[(int)space];
            if (pnSpace.SendCryptoSeal == null)
            {
                // already dropped
                return;
            }

            Recovery.DropUnackedData(space, _tls.IsHandshakeComplete, sentPacketPool);

            // drop protection keys
            pnSpace.SendCryptoSeal = null;
            pnSpace.RecvCryptoSeal = null;

            pnSpace.NextAckTimer = long.MaxValue;
            ResetAckTimer();
        }

        internal void SignalConnectionClose() => _closeTcs.TryComplete();

        /// <summary>
        ///     Starts closing period.
        /// </summary>
        /// <param name="now">Timestamp of the current moment.</param>
        /// <param name="error">Error which led to connection closing.</param>
        private void StartClosing(long now, QuicError error)
        {
            Debug.Assert(_closingPeriodEndTimestamp == null);
            Debug.Assert(error != null);

            // The closing and draining states SHOULD exists for at least three times the current PTO interval
            // Note: this is to properly discard reordered/delayed packets.
            _closingPeriodEndTimestamp = now + 3 * Recovery.GetProbeTimeoutInterval();

            // disable ack timer
            _nextAckTimer = long.MaxValue;

            if (error.ErrorCode == TransportErrorCode.NoError)
            {
                _streams.IncomingStreams.Writer.TryComplete();
            }
            else
            {
                _streams.IncomingStreams.Writer.TryComplete(MakeAbortedException(error));
            }

            foreach (var stream in _streams.AllStreams)
            {
                stream.OnConnectionClosed(MakeAbortedException(error));
            }
        }

        private void StartDraining()
        {
            _isDraining = true;

            // for all user's purposes, the connection is closed.
            SignalConnectionClose();
        }

        /// <summary>
        ///     Calculates idle timeout based on the local and peer endpoints transport parameters.
        /// </summary>
        private long GetIdleTimeoutPeriod()
        {
            long localTimeout = Timestamp.FromMilliseconds(_localTransportParameters.MaxIdleTimeout);
            long peerTimeout = Timestamp.FromMilliseconds(_peerTransportParameters.MaxIdleTimeout);

            return (localTimeout, peerTimeout) switch
            {
                (0, 0) => 0,
                (long t, 0) => t,
                (0, long t) => t,
                (long t, long u) => Math.Min(t, u)
            };
        }

        private void RestartIdleTimer(long now)
        {
            long timeout = GetIdleTimeoutPeriod();
            if (timeout > 0)
            {
                // RFC: If the idle timeout is enabled by either peer, a connection is
                // silently closed and its state is discarded when it remains idle for
                // longer than the minimum of the max_idle_timeouts (see Section 18.2)
                // and three times the current Probe Timeout (PTO).
                _idleTimeout = now + timeout + 3 * Recovery.GetProbeTimeoutInterval();
            }
        }

        private void SignalConnected()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Connected(this);
            _connectTcs.TryComplete();
        }

        private static QuicConnectionAbortedException MakeAbortedException(QuicError error)
        {
            return error.ReasonPhrase != null
                // TODO-RZ: We should probably format reason phrase into the exception message
                ? new QuicConnectionAbortedException(error.ReasonPhrase, (long)error.ErrorCode)
                : new QuicConnectionAbortedException((long)error.ErrorCode);
        }
    }
}
