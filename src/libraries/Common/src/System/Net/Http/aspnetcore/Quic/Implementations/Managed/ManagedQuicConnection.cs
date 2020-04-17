#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
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
    internal sealed partial class ManagedQuicConnection : QuicConnectionProvider
    {
        private readonly ResettableCompletionSource<int> _connectTcs = new ResettableCompletionSource<int>();

        private readonly QuicClientConnectionOptions? _clientOpts;

        private readonly PacketNumberSpace[] _pnSpaces = new PacketNumberSpace[3]
        {
            new PacketNumberSpace(), new PacketNumberSpace(), new PacketNumberSpace()
        };

        private RecoveryController Recovery { get; } = new RecoveryController();

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
        ///     Flow control limits for this endpoint for the entire connection.
        /// </summary>
        private ConnectionFlowControlLimits _localLimits = default;

        /// <summary>
        ///     Flow control limits for the peer endpoint for the entire connection.
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

            var localEndpoint = options.LocalEndPoint ?? new IPEndPoint(_remoteEndpoint.AddressFamily == AddressFamily.InterNetwork
                ? IPAddress.Any
                : IPAddress.IPv6Any, 0);
            _socketContext = new QuicSocketContext(localEndpoint, this);
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
        public ManagedQuicConnection(QuicListenerOptions options, QuicSocketContext socketContext, IPEndPoint remoteEndpoint)
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

        private void DoHandshake()
        {
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

                    Recovery.MaxAckDelay = TimeSpan.FromMilliseconds(param.MaxAckDelay);

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

        internal void ReceiveData(QuicReader reader, IPEndPoint sender, DateTime now)
        {
            var buffer = reader.Buffer;
            var context = new RecvContext(now);

            while (reader.BytesLeft > 0)
            {
                var status = ReceiveOne(reader, context);

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

            // if (pnSpace.ReceivedPacketNumbers.Contains(packetNumber))
            // {
            //     return ProcessPacketResult.Ok; // already processed;
            // }

            if (pnSpace.LargestReceivedPacketNumber < packetNumber)
            {
                pnSpace.LargestReceivedPacketNumber = packetNumber;
                pnSpace.LargestReceivedPacketTimestamp = DateTime.Now; //TODO-RZ: pass time externally
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
            int tagLength = GetPacketNumberSpace(GetEncryptionLevel(packetType)).RecvCryptoSeal!.TagLength;
            reader.Reset(reader.Buffer.Slice(reader.BytesRead, reader.BytesLeft - tagLength));
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
                    !header.FixedBit && _isServer && header.PacketType == PacketType.VersionNegotiation)
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
            // TODO-RZ: handle resend and packet loss of earlier levels
            EncryptionLevel desiredLevel = _tls.WriteLevel;
            // if not connected, then handshake is not done yet
            // if (!Connected && desiredLevel == EncryptionLevel.Application)
            // return EncryptionLevel.Handshake;

            for (int i = 0; i < _pnSpaces.Length; i++)
            {
                var level = (EncryptionLevel)i;
                var pnSpace = _pnSpaces[i];

                if (pnSpace.CryptoOutboundStream.IsFlushable)
                    return level;

                if (pnSpace.AckElicited)
                    return level;
            }

            return desiredLevel;
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
            var seal = pnSpace.SendCryptoSeal!;

            (int truncatedPn, int pnLength) = pnSpace.GetNextPacketNumber(Recovery.GetLargestAckedPacketNumber(packetSpace));
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

            // TODO-RZ: respect control flow limits and congestion window
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
                return false;
            }

            // remember what we sent in this packet
            Recovery.OnPacketSent(pnSpace.NextPacketNumber, GetPacketSpace(packetType), context.SentPacket, _tls.IsHandshakeComplete);
            pnSpace.NextPacketNumber++;

            if (!_isServer && packetType == PacketType.Initial)
            {
                // TODO-RZ: It would be more efficient to add padding only to the last packet sent when coalescing packets.

                // Pad client initial packets to the minimum size
                int paddingLength = QuicConstants.MinimumClientInitialDatagramSize - seal.TagLength -
                                    writer.BytesWritten;
                if (paddingLength > 0)
                    // zero bytes are equivalent to PADDING frames
                    writer.GetWritableSpan(paddingLength).Clear();
            }

            // pad the packet payload so that it can always be sampled for header protection
            if (writer.BytesWritten - pnOffset < seal.PayloadSampleLength + 4)
            {
                writer.GetWritableSpan(seal.PayloadSampleLength + 4 - writer.BytesWritten + pnOffset).Clear();
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

        internal void SendData(QuicWriter writer, out IPEndPoint? receiver, DateTime now)
        {
            receiver = _remoteEndpoint;
            // TODO-RZ: process lost packets
            // TODO-RZ: pool SentPacket, SendContext and QuicWriter instances

            var context = new SendContext(now);

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

                context.SentPacket = new SentPacket {TimeSent = context.Now};
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

        private ProcessPacketResult CloseConnection(TransportErrorCode errorCode, string? reason, FrameType frameType = FrameType.Padding)
        {
            outboundError = new QuicError(errorCode, reason, frameType);
            return ProcessPacketResult.ConnectionClose;
        }


        public override void Dispose()
        {
            _tls.Dispose();
            _gcHandle.Free();
            _disposed = true;
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
            GetPacketNumberSpace(level).CryptoOutboundStream.Enqueue(data);
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
            public ContextBase(DateTime now)
            {
                Now = now;
            }

            /// <summary>
            ///     Timestamp when the next tick of internal processing was requested.
            /// </summary>
            internal DateTime Now { get; }
        }

        private sealed class RecvContext : ContextBase
        {
            public RecvContext(DateTime now) : base(now)
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
            public SendContext(DateTime now)
                : base(now)
            {
            }

            /// <summary>
            ///     Data about next packet that is to be sent.
            /// </summary>
            internal SentPacket SentPacket { get; set; } = new SentPacket();
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
            ConnectionClose
        }

        #region Public API

        internal override bool Connected => HandshakeConfirmed;

        internal override IPEndPoint LocalEndPoint => _clientOpts!.LocalEndPoint!;

        internal override IPEndPoint RemoteEndPoint => _clientOpts!.RemoteEndPoint!;

        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Connected) return new ValueTask();
            _socketContext.Start();
            _socketContext.Ping();

            return _connectTcs.GetTypelessValueTask();
        }

        internal override QuicStreamProvider OpenUnidirectionalStream()
        {
            ThrowIfDisposed();
            return OpenStream(true);
        }

        internal override QuicStreamProvider OpenBidirectionalStream()
        {
            ThrowIfDisposed();
            return OpenStream(false);
        }

        internal override long GetRemoteAvailableUnidirectionalStreamCount()
        {
            ThrowIfDisposed();
            return _peerLimits.MaxStreamsUni;
        }

        internal override long GetRemoteAvailableBidirectionalStreamCount()
        {
            ThrowIfDisposed();
            return _peerLimits.MaxStreamsBidi;
        }

        internal override async ValueTask<QuicStreamProvider>
            AcceptStreamAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return await _streams.IncomingStreams.Reader.ReadAsync(cancellationToken);
        }

        internal override SslApplicationProtocol NegotiatedApplicationProtocol { get; }

        internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        #endregion

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ManagedQuicConnection));
            }
        }
    }
}
