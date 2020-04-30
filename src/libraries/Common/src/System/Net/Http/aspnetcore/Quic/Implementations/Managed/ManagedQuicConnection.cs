#nullable enable

using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed
{
    internal enum QuicConnectionState
    {
        None,
        Connected,
        Draining,
        Closing,
        Closed
    }

    internal sealed partial class ManagedQuicConnection : QuicConnectionProvider
    {
        private readonly SingleEventValueTaskSource _connectTcs = new SingleEventValueTaskSource();

        private readonly SingleEventValueTaskSource _closeTcs = new SingleEventValueTaskSource();

        private readonly ObjectPool<SentPacket> _sentPacketPool = new ObjectPool<SentPacket>(128);

        private long _lastConnectionCloseSent;

        private readonly QuicClientConnectionOptions? _clientOpts;

        private readonly PacketNumberSpace[] _pnSpaces = new PacketNumberSpace[3]
        {
            new PacketNumberSpace(), new PacketNumberSpace(), new PacketNumberSpace()
        };

        private RecoveryController Recovery { get; } = new RecoveryController();

        private bool _isDraining;
        private bool IsClosing => _closingPeriodEnd != null;

        internal long? _closingPeriodEnd = null;

        internal bool IsClosed => _closeTcs.IsSet;

        internal QuicConnectionState ConnectionState => GetConnectionState();

        internal QuicConnectionState GetConnectionState()
        {
            if (IsClosed) return QuicConnectionState.Closed;
            if (_isDraining) return QuicConnectionState.Draining;
            if (IsClosing) return QuicConnectionState.Closing;
            if (Connected) return QuicConnectionState.Connected;
            return QuicConnectionState.None;
        }

        /// <summary>
        ///     QUIC transport parameters used for this endpoint.
        /// </summary>
        private readonly TransportParameters _localTransportParameters;

        private readonly QuicListenerOptions? _serverOpts;

        private readonly Tls _tls;

        /// <summary>
        ///     Local endpoint address, can be null for yet unconnected clients.
        /// </summary>
        private readonly IPEndPoint? _localEndpoint;

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
        ///     Flow control limits set by this endpoint for the peer for the entire connection.
        /// </summary>
        private ConnectionFlowControlLimits _localLimits = default;

        /// <summary>
        ///     Values of <see cref="_localLimits"/> that peer has confirmed received.
        /// </summary>
        private ConnectionFlowControlLimits _peerReceivedLocalLimits = default;

        /// <summary>
        ///     Flow control limits set by the peer for this endpoint for the entire connection.
        /// </summary>
        private ConnectionFlowControlLimits _peerLimits;

        /// <summary>
        ///     QUIC transport parameters requested by peer endpoint.
        /// </summary>
        private TransportParameters _peerTransportParameters = TransportParameters.Default;

        /// <summary>
        ///     Error received via CONNECTION_CLOSE frame to be reported to the user.
        /// </summary>
        private QuicError? inboundError;

        /// <summary>
        ///     Error to send in next packet in a CONNECTION_CLOSE frame.
        /// </summary>
        private QuicError? outboundError;

        /// <summary>
        ///     Version of the QUIC protocol used for this connection.
        /// </summary>
        private readonly QuicVersion version = QuicVersion.Draft27;

        /// <summary>
        ///     True if PING frame should be sent during next flight.
        ///     //TODO-RZ: this is currently only debug aid to ensure that some ack-eliciting packet is sent.
        /// </summary>
        private bool _pingWanted;

        private bool _disposed;

        internal void Ping()
        {
            _pingWanted = true;
        }

        // client constructor
        public ManagedQuicConnection(QuicClientConnectionOptions options)
        {
            _isServer = false;
            _clientOpts = options;

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
            SourceConnectionId = ConnectionId.Random(20);
            DestinationConnectionId = ConnectionId.Random(20);
            _localConnectionIdCollection.Add(SourceConnectionId);

            // derive also clients initial secrets.
            DeriveInitialProtectionKeys(DestinationConnectionId.Data);

            // generate first Crypto frames
            _tls.DoHandshake();
        }

        // server constructor
        public ManagedQuicConnection(QuicListenerOptions options, QuicServerSocketContext socketContext,
            IPEndPoint remoteEndpoint)
        {
            _isServer = true;
            _serverOpts = options;
            _socketContext = socketContext;
            _localEndpoint = options.ListenEndPoint;
            _remoteEndpoint = remoteEndpoint;
            _localTransportParameters = TransportParameters.FromListenerOptions(options);

            _gcHandle = GCHandle.Alloc(this);
            _tls = new Tls(_gcHandle, options, _localTransportParameters);
        }

        public ConnectionId? SourceConnectionId { get; private set; }

        public ConnectionId? DestinationConnectionId { get; private set; }

        /// <summary>
        ///     Returns timestamp of the next timer event, after timeout, <see cref="OnTimeout"/> should be called.
        /// </summary>
        /// <returns>Timestamp in ticks of the next timer or long.MaxValue if no timer is needed.</returns>
        internal long GetNextTimerTimestamp()
        {
            long timer = Recovery.LossRecoveryTimer;

            // TODO-RZ: find a way to get shutdown reliably without hammering the peer with packets.
            if (_closingPeriodEnd != null)
                timer = Math.Min(timer, _closingPeriodEnd.Value);

            return timer;
        }

        private void DoHandshake()
        {
            // TODO-RZ: handle failed handshake attempts
            var status = _tls.DoHandshake();

            // TODO-RZ: application level protocol negotiation

            if (ReferenceEquals(_peerTransportParameters, TransportParameters.Default))
            {
                var param = _tls.GetPeerTransportParameters(_isServer);

                if (param != null)
                {
                    ref ConnectionFlowControlLimits limits = ref _peerLimits;

                    limits.UpdateMaxData(param.InitialMaxData);
                    limits.UpdateMaxStreamsBidi(param.InitialMaxStreamsBidi);
                    limits.UpdateMaxStreamsUni(param.InitialMaxStreamsUni);

                    Recovery.MaxAckDelay = Timestamp.FromMilliseconds(param.MaxAckDelay);

                    _peerTransportParameters = param;
                }
            }
        }

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

        internal void ReceiveData(QuicReader reader, IPEndPoint sender, long now)
        {
            if (_closingPeriodEnd != null)
            {
                // discard any incoming data
                return;
            }

            var buffer = reader.Buffer;
            var context = new RecvContext(now);

            while (reader.BytesLeft > 0)
            {
                var status = ReceiveOne(reader, context);

                if (status == ProcessPacketResult.DropPacket)
                {
                    Console.WriteLine("Packet dropped");
                }

                // Receive will adjust the buffer length once it is known, thus the length here skips the
                // just processed coalesced packet
                buffer = buffer.Slice(reader.Buffer.Length);
                reader.Reset(buffer);
            }
        }

        private ProcessPacketResult Receive1Rtt(QuicReader reader, in ShortPacketHeader header, RecvContext context)
        {
            int pnOffset = reader.BytesRead;
            PacketType packetType = PacketType.OneRtt;
            var pnSpace = GetPacketNumberSpace(EncryptionLevel.Application);
            int payloadLength = reader.BytesLeft;

            return ReceiveProtectedFrames(reader, pnSpace, pnOffset, payloadLength, packetType, context);
        }

        private ProcessPacketResult ReceiveRetry(QuicReader reader, in LongPacketHeader header, RecvContext context)
        {
            throw new NotImplementedException();
        }

        private ProcessPacketResult ReceiveVersionNegotiation(QuicReader reader, in LongPacketHeader header,
            RecvContext context)
        {
            throw new NotImplementedException();
        }

        private ProcessPacketResult ReceiveCommon(QuicReader reader, in LongPacketHeader header,
            in SharedPacketData headerData, RecvContext context)
        {
            //TODO-RZ: Version negotiation
            //TODO-RZ: Check connection id length (beware that length is unbounded in initial packets)

            int pnOffset = reader.BytesRead;
            var pnSpace = GetPacketNumberSpace(GetEncryptionLevel(header.PacketType));
            int payloadLength = (int)headerData.Length;
            PacketType packetType = header.PacketType;

            if (_isServer && packetType == PacketType.Initial)
            {
                if (pnSpace.RecvCryptoSeal == null)
                {
                    // initialize protection keys
                    // clients destination connection Id is ours source connection Id
                    SourceConnectionId = new ConnectionId(header.DestinationConnectionId.ToArray());
                    DestinationConnectionId = new ConnectionId(header.SourceConnectionId.ToArray());

                    _localConnectionIdCollection.Add(SourceConnectionId);
                    DeriveInitialProtectionKeys(SourceConnectionId.Data);
                }

                // check UDP datagram size, by now the reader's buffer end is aligned with the UDP datagram end.
                // TODO-RZ: in rare cases when initial is not the first of the coalesced packets this can falsely close the connection.
                // as the QUIC does only recommend, not mandate order of the coalesced packets
                if (reader.Buffer.Length < QuicConstants.MinimumClientInitialDatagramSize)
                {
                    return CloseConnection(TransportErrorCode.ProtocolViolation,
                        QuicError.InitialPacketTooShort);
                }
            }

            return ReceiveProtectedFrames(reader, pnSpace, pnOffset, payloadLength, packetType, context);
        }

        private ProcessPacketResult ReceiveProtectedFrames(QuicReader reader, PacketNumberSpace pnSpace, int pnOffset,
            int payloadLength,
            PacketType packetType, RecvContext context)
        {
            if (pnSpace.RecvCryptoSeal == null)
            {
                // Decryption keys are not available yet, drop the packet for now
                // TODO-RZ: consider buffering the packet
                return ProcessPacketResult.DropPacket;
            }

            var seal = pnSpace.RecvCryptoSeal!;

            if (!seal.DecryptPacket(reader.Buffer.Span, pnOffset, payloadLength,
                pnSpace.LargestReceivedPacketNumber))
            {
                // decryption failed, drop the packet.
                reader.Advance(payloadLength);
                return ProcessPacketResult.DropPacket;
            }

            // TODO-RZ: read in a better way
            int pnLength = HeaderHelpers.GetPacketNumberLength(reader.Buffer.Span[0]);
            reader.TryReadTruncatedPacketNumber(pnLength, out int truncatedPn);

            long packetNumber = QuicPrimitives.DecodePacketNumber(pnSpace.LargestReceivedPacketNumber,
                truncatedPn, pnLength);

            if (pnSpace.ReceivedPacketNumbers.Contains(packetNumber))
            {
                // already processed or outside congestion window
                // TODO-RZ: there may be false positives if the packet number is 64 lesser than largest received
                return ProcessPacketResult.Ok;
            }

            if (pnSpace.LargestReceivedPacketNumber < packetNumber)
            {
                pnSpace.LargestReceivedPacketNumber = packetNumber;
                pnSpace.LargestReceivedPacketTimestamp = context.Timestamp;
            }

            pnSpace.UnackedPacketNumbers.Add(packetNumber);
            pnSpace.ReceivedPacketNumbers.Add(packetNumber);

            return ProcessFramesWithoutTag(reader, packetType, context);
        }

        private ProcessPacketResult ProcessFramesWithoutTag(QuicReader reader, PacketType packetType,
            RecvContext context)
        {
            // HACK: we do not want to try processing the AEAD integrity tag as if it were frames.
            var originalSegment = reader.Buffer;
            int originalBytesRead = reader.BytesRead;
            int tagLength = GetPacketNumberSpace(GetEncryptionLevel(packetType)).RecvCryptoSeal!.TagLength;
            int length = reader.BytesLeft - tagLength;
            reader.Reset(originalSegment.Slice(originalBytesRead, length));
            var retval = ProcessFrames(reader, packetType, context);
            reader.Reset(originalSegment);
            return retval;
        }

        private ProcessPacketResult ReceiveLongHeaderPackets(QuicReader reader, in LongPacketHeader header,
            RecvContext context)
        {
            var type = header.PacketType;

            // TODO-RZ: Check that connection IDs match and have correct length (not too long)

            switch (type)
            {
                case PacketType.Initial:
                // TODO-RZ: server must not send Token (Protocol violation)
                case PacketType.Handshake:
                case PacketType.ZeroRtt:
                    if (!SharedPacketData.Read(reader, header.FirstByte, out var headerData) ||
                        headerData.Length > reader.BytesLeft)
                    {
                        return ProcessPacketResult.DropPacket;
                    }

                    // total length of the packet is known and checked during header parsing.
                    // Adjust the buffer to the range belonging to the current packet.
                    reader.Reset(reader.Buffer.Slice(0, reader.BytesRead + (int)headerData.Length), reader.BytesRead);

                    return ReceiveCommon(reader, header, headerData, context);
                case PacketType.Retry:
                    return ReceiveRetry(reader, header, context);
                case PacketType.VersionNegotiation:
                    return ReceiveVersionNegotiation(reader, header, context);
                case PacketType.OneRtt:
                    // this type is handled elsewhere
                    throw new InvalidOperationException("Unreachable");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ProcessPacketResult ReceiveOne(QuicReader reader, RecvContext context)
        {
            byte first = reader.Peek();

            ProcessPacketResult result;
            if (HeaderHelpers.IsLongHeader(first))
            {
                if (!LongPacketHeader.Read(reader, out var header) ||
                    // clients SHOULD ignore fixed bit when receiving version negotiation
                    !header.FixedBit && _isServer && header.PacketType == PacketType.VersionNegotiation ||
                    // packet is not meant for us after all
                    SourceConnectionId != null &&
                    !header.DestinationConnectionId.SequenceEqual(SourceConnectionId!.Data))
                {
                    return ProcessPacketResult.DropPacket;
                }

                result = ReceiveLongHeaderPackets(reader, header, context);
            }

            else
            {
                if (!ShortPacketHeader.Read(reader, _localConnectionIdCollection, out var header) ||
                    !header.FixedBit)
                {
                    return ProcessPacketResult.DropPacket;
                }

                result = Receive1Rtt(reader, header, context);
            }

            return result;
        }

        internal EncryptionLevel GetWriteLevel()
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
                return (EncryptionLevel) probeSpace;
            }

            // if pending errors, send them in appropriate epoch,
            if (outboundError != null && outboundError.IsQuicError)
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
                    // send acknowledgements
                    pnSpace.AckElicited)
                    return level;
            }

            // else we send stream/application data
            return EncryptionLevel.Application;
        }

        private bool SendOne(QuicWriter writer, EncryptionLevel level, SendContext context)
        {
            (PacketType packetType, PacketSpace packetSpace) = level switch
            {
                EncryptionLevel.Initial => (PacketType.Initial, PacketSpace.Initial),
                EncryptionLevel.EarlyData => (PacketType.ZeroRtt, PacketSpace.Application),
                EncryptionLevel.Handshake => (PacketType.Handshake, PacketSpace.Handshake),
                EncryptionLevel.Application => (PacketType.OneRtt, PacketSpace.Application),
                _ => throw new InvalidOperationException()
            };

            var pnSpace = GetPacketNumberSpace(level);
            var recoverySpace = Recovery.GetPacketNumberSpace(packetSpace);
            var seal = pnSpace.SendCryptoSeal!;

            ProcessLostPackets(pnSpace, recoverySpace);

            (int truncatedPn, int pnLength) = pnSpace.GetNextPacketNumber(recoverySpace.LargestAckedPacketNumber);
            WritePacketHeader(writer, packetType, pnLength);

            // for non 1-RTT packets, we reserve 2 bytes which we will overwrite once total payload length is known
            var payloadLengthSpan = writer.Buffer.Span.Slice(writer.BytesWritten - 2, 2);

            int pnOffset = writer.BytesWritten;
            writer.WriteTruncatedPacketNumber(pnLength, truncatedPn);

            int maxPacketLength = (int)(_tls.IsHandshakeComplete
                // Limit maximum size so that it can be always encoded into the reserved 2 bytes of `payloadLengthSpan`
                ? Math.Min((1 << 14) - 1, _peerTransportParameters.MaxPacketSize)
                // use minimum size for packets during handshake
                : QuicConstants.MinimumClientInitialDatagramSize);

            bool isProbePacket = recoverySpace.RemainingLossProbes > 0;

            // make sure we send something if a probe is wanted
            _pingWanted |= isProbePacket;

            // TODO-RZ: Although ping should always work, the actual algorithm for probe packet is following
            // if (!isServer && GetPacketNumberSpace(EncryptionLevel.Application).RecvCryptoSeal == null)
            // {
                // TODO-RZ: Client needs to send an anti-deadlock packet:
            // }
            // else
            // {
                // TODO-RZ: PTO. Send new data if available, else retransmit old data.
                // If neither is available, send single PING frame.
            // }

            // limit outbound packet by available congestion window
            // probe packets are not limited by congestion window
            if (!isProbePacket)
            {
                maxPacketLength = Math.Min(maxPacketLength, Recovery.GetAvailableCongestionWindowBytes());
            }

            if (maxPacketLength <= seal.TagLength)
            {
                // unable to send any useful data anyway.
                return false;
            }

            int written = writer.BytesWritten;
            var origBuffer = writer.Buffer;

            writer.Reset(origBuffer.Slice(0, Math.Min(origBuffer.Length, maxPacketLength - seal.TagLength)), written);
            WriteFrames(writer, packetType, level, context);
            writer.Reset(origBuffer, writer.BytesWritten);

            if (writer.BytesWritten == written)
            {
                // no data to send
                // TODO-RZ: we might be able to detect this sooner
                writer.Reset(writer.Buffer);
                Debug.Assert(!_pingWanted);
                return false;
            }

            if (!_isServer && packetType == PacketType.Initial)
            {
                // TODO-RZ: It would be more efficient to add padding only to the last packet sent when coalescing packets.

                // Pad client initial packets to the minimum size
                int paddingLength = QuicConstants.MinimumClientInitialDatagramSize - seal.TagLength -
                                    writer.BytesWritten;
                if (paddingLength > 0)
                    // zero bytes are equivalent to PADDING frames
                    writer.GetWritableSpan(paddingLength).Clear();

                context.SentPacket.InFlight = true; // padding implies InFlight
            }

            // pad the packet payload so that it can always be sampled for header protection
            if (writer.BytesWritten - pnOffset < seal.PayloadSampleLength + 4)
            {
                writer.GetWritableSpan(seal.PayloadSampleLength + 4 - writer.BytesWritten + pnOffset).Clear();
                context.SentPacket.InFlight = true; // padding implies InFlight
            }

            // reserve space for AEAD integrity tag
            writer.GetWritableSpan(seal.TagLength);
            int payloadLength = writer.BytesWritten - pnOffset;

            // fill in the payload length retrospectively
            if (packetType != PacketType.OneRtt)
            {
                QuicPrimitives.WriteVarInt(payloadLengthSpan, payloadLength, 2);
            }

            seal.EncryptPacket(writer.Buffer.Span, pnOffset, payloadLength, truncatedPn);

            // remember what we sent in this packet
            context.SentPacket.PacketNumber = pnSpace.NextPacketNumber;
            context.SentPacket.BytesSent = writer.BytesWritten;
            context.SentPacket.TimeSent = context.Timestamp;

            if (isProbePacket)
            {
                recoverySpace.RemainingLossProbes--;
            }
            Recovery.OnPacketSent(GetPacketSpace(packetType), context.SentPacket, _tls.IsHandshakeComplete);
            pnSpace.NextPacketNumber++;

            return true;
        }

        private void WritePacketHeader(QuicWriter writer, PacketType packetType, int pnLength)
        {
            if (packetType == PacketType.OneRtt)
            {
                // 1-RTT packets are the only ones using short header
                // TODO-RZ: implement spin
                // TODO-RZ: implement key update
                ShortPacketHeader.Write(writer,
                    new ShortPacketHeader(false, false, pnLength, DestinationConnectionId!));
            }
            else
            {
                LongPacketHeader.Write(writer, new LongPacketHeader(
                    packetType,
                    pnLength,
                    version,
                    DestinationConnectionId!.Data,
                    SourceConnectionId!.Data));

                // HACK: reserve 2 bytes for payload length and overwrite it later
                SharedPacketData.Write(writer, new SharedPacketData(
                    writer.Buffer.Span[0],
                    ReadOnlySpan<byte>.Empty,
                    1000 /*arbitrary number with 2-byte encoding*/));
            }
        }

        internal void SendData(QuicWriter writer, out IPEndPoint? receiver, long now)
        {
            receiver = _remoteEndpoint;

            if (now > _closingPeriodEnd)
            {
                SignalConnectionClose();
                return;
            }

            if (_isDraining)
            {
                // While otherwise identical to the closing state, an endpoint in the draining state MUST NOT
                // send any packets
                return;
            }

            if (now >= Recovery.LossRecoveryTimer)
            {
                Recovery.OnLossDetectionTimeout(_tls.IsHandshakeComplete, now);
            }

            var context = new SendContext(now, _sentPacketPool.Rent());

            var level = GetWriteLevel();
            var origMemory = writer.Buffer;
            int written = 0;

            while (true)
            {
                if (GetPacketNumberSpace(level).SendCryptoSeal == null)
                {
                    // Secrets have not been derived yet, can't send anything
                    break;
                }

                if (!SendOne(writer, level, context))
                    break;
                written += writer.BytesWritten;

                // 0-RTT packets do not have Length, so they may not be coalesced
                if (level == EncryptionLevel.Application)
                    break;

                var nextLevel = GetWriteLevel();

                // only coalesce packets in ascending encryption level
                if (nextLevel <= level)
                    break;

                context.SentPacket = _sentPacketPool.Rent();
                level = nextLevel;
                writer.Reset(writer.Buffer.Slice(writer.BytesWritten));
            }

            writer.Reset(origMemory, written);
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

        private ProcessPacketResult CloseConnection(TransportErrorCode errorCode, string? reason,
            FrameType frameType = FrameType.Padding)
        {
            outboundError = new QuicError(errorCode, reason, frameType);
            return ProcessPacketResult.Error;
        }

        private void ProcessLostPackets(PacketNumberSpace pnSpace, RecoveryController.PacketNumberSpace recoverySpace)
        {
            var lostPackets = recoverySpace.LostPackets;
            while (lostPackets.TryDequeue(out var lostPacket))
            {
                if (lostPacket.AckEliciting || lostPacket.TimeSent == pnSpace.LastAckSent)
                {
                    pnSpace.AckElicited = true;
                }

                foreach (var data in lostPacket.StreamFrames)
                {
                    if (data.IsCryptoStream)
                    {
                        pnSpace.CryptoOutboundStream.OnLost(data.Offset, data.Count);
                    }
                    else
                    {
                        var stream = GetStream(data.StreamId);

                        // empty stream frames are only sent to send the Fin bit
                        Debug.Assert(data.Count > 0 || data.Fin);
                        if (data.Count > 0)
                        {
                            stream.OutboundBuffer!.OnLost(data.Offset, data.Count);
                        }

                        _streams.MarkFlushable(stream);
                    }
                }

                if (lostPacket.HandshakeDoneSent)
                {
                    _handshakeDoneSent = false;
                }

                foreach (var frame in lostPacket.MaxStreamDataFrames)
                {
                    var stream = GetStream(frame.StreamId);
                    // TODO-RZ: send these frames less often
                    if (frame.MaximumStreamData > stream.InboundBuffer!.RemoteMaxData)
                    {
                        _streams.MarkForFlowControlUpdate(stream);
                    }
                }

                if (lostPacket.MaxDataFrame != null)
                {
                    MaxDataFrameSent = false;
                }

                // acked ranges are deleted only when the packet was acked, so no action needed here
                _sentPacketPool.Return(lostPacket);
            }
        }

        internal async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await CloseAsync((long)TransportErrorCode.NoError).ConfigureAwait(false);

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
            var pnSpace = GetPacketNumberSpace(level);
            Debug.Assert(pnSpace.SendCryptoSeal == null, "Protection keys already derived");

            pnSpace.RecvCryptoSeal = new CryptoSeal(algorithm, readSecret);
            pnSpace.SendCryptoSeal = new CryptoSeal(algorithm, writeSecret);
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
            Console.WriteLine($"SendAlert({level}): {(byte)alert} (0x{(byte)alert:x2}) - {alert}");

            return 1;
        }

        private class ContextBase
        {
            public ContextBase(long now)
            {
                Timestamp = now;
            }

            /// <summary>
            ///     Timestamp when the next tick of internal processing was requested.
            /// </summary>
            internal long Timestamp { get; }
        }

        private sealed class RecvContext : ContextBase
        {
            public RecvContext(long now) : base(now)
            {
            }

            /// <summary>
            ///     Flag whether TLS handshake should be incremented at the end of packet processing, perhaps due to
            ///     having received crypto data.
            /// </summary>
            internal bool HandshakeWanted { get; set; }
        }


        private sealed class SendContext : ContextBase
        {
            public SendContext(long now, SentPacket sentPacket)
                : base(now)
            {
                SentPacket = sentPacket;
            }

            /// <summary>
            ///     Data about next packet that is to be sent.
            /// </summary>
            internal SentPacket SentPacket { get; set; }
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
            // TODO-RZ: finalize when do we throw these exceptions
            // ThrowIfError();

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            var stream = await _streams.IncomingStreams.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            return stream;
        }

        internal override SslApplicationProtocol NegotiatedApplicationProtocol => throw new NotImplementedException();

        internal override async ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!Connected)
            {
                // abandon connection attempt
                // TODO-RZ: can we just wink the connection out?
                // _connectTcs.TryCompleteException();
                _closeTcs.TryComplete();
            }

            if (IsClosed) return;
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            outboundError = new QuicError((TransportErrorCode)errorCode, null, FrameType.Padding, false);
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

        private void ThrowIfError()
        {
            if (inboundError != null)
            {
                throw new QuicErrorException(inboundError!);
            }
        }

        internal void SignalConnectionClose() => _closeTcs.TryComplete();

        private void StartClosing(long now)
        {
            Debug.Assert(_closingPeriodEnd == null);

            // The closing and draining states SHOULD exists for at least three times the current PTO interval
            // Note: this is to properly discard reordered/delayed packets.
            _closingPeriodEnd = now + 3 * Recovery.GetProbeTimeoutInterval();

            // TODO-RZ: data race with user who is trying to open a new stream?
            foreach (var stream in _streams.AllStreams)
            {
                stream.OnConnectionClosed();
            }
        }

        private void StartDraining()
        {
            _isDraining = true;

            // for all user's purposes, the connection is closed.
            SignalConnectionClose();
        }
    }
}
