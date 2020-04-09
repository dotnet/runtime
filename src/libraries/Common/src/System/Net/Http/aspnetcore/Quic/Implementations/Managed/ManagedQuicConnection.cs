#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed
{
    internal sealed partial class ManagedQuicConnection : QuicConnectionProvider
    {
        private readonly QuicClientConnectionOptions? _clientOpts;

        private readonly EpochData[] _epochs;

        /// <summary>
        ///     QUIC transport parameters used for this endpoint.
        /// </summary>
        private readonly TransportParameters _localTransportParameters;

        private readonly Recovery _recovery = new Recovery();
        private readonly QuicListenerOptions? _serverOpts;

        private readonly Tls _tls;

        private GCHandle _gcHandle;

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

        private readonly bool _isServer;

        /// <summary>
        ///     Collection of local connection ids used by this endpoint.
        /// </summary>
        private readonly ConnectionIdCollection _localConnectionIdCollection = new ConnectionIdCollection();

        /// <summary>
        ///     Flow control limits for this endpoint for the entire connection.
        /// </summary>
        private ConnectionFlowControlLimits _localLimits;

        /// <summary>
        ///     Flow control limits for the peer endpoint for the entire connection.
        /// </summary>
        private ConnectionFlowControlLimits _peerLimits;

        /// <summary>
        ///     QUIC transport parameters requested by peer endpoint.
        /// </summary>
        private readonly TransportParameters _peerTransportParameters = TransportParameters.Default;

        /// <summary>
        ///     All streams organized by the stream type.
        /// </summary>
        private readonly List<ManagedQuicStream>[] _streams = new List<ManagedQuicStream>[4];

        // TODO-RZ: remove these, they don't need to be saved
        private readonly string? cert;

        /// <summary>
        ///     Error received via CONNECTION_CLOSE frame to be reported to the user.
        /// </summary>
        private QuicError? inboundError;

        /// <summary>
        ///     Error to send in next packet in a CONNECTION_CLOSE frame.
        /// </summary>
        private QuicError? outboundError;

        private readonly string? privateKey;

        private readonly QuicVersion version = QuicVersion.Draft27;

        public ManagedQuicConnection(QuicClientConnectionOptions options)
            : this(false)
        {
            _clientOpts = options;

            Init();
        }

        public ManagedQuicConnection(QuicListenerOptions options)
            : this(true)
        {
            _serverOpts = options;
            cert = _serverOpts.CertificateFilePath;
            privateKey = _serverOpts.PrivateKeyFilePath;

            Init();
        }

        public ManagedQuicConnection(bool isServer)
        {
            _gcHandle = GCHandle.Alloc(this);
            _tls = new Tls(_gcHandle);

            _isServer = isServer;

            // TODO-RZ: compose transport params from options
            _localTransportParameters = new TransportParameters();

            _epochs = new EpochData[3] {new EpochData(), new EpochData(), new EpochData()};
        }

        public ConnectionId? SourceConnectionId { get; private set; }

        public ConnectionId? DestinationConnectionId { get; private set; }

        private void Init()
        {
            _tls.Init(cert, privateKey, _isServer, _localTransportParameters);

            if (_isServer)
            {
                return;
            }

            // init random connection ids for the client
            SourceConnectionId = ConnectionId.Random(20);
            DestinationConnectionId = ConnectionId.Random(20);
            _localConnectionIdCollection.Add(SourceConnectionId.Data);

            // derive also clients initial secrets.
            DeriveInitialProtectionKeys(DestinationConnectionId.Data);

            // generate first Crypto frames
            _tls.DoHandshake();
        }

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

                    limits.MaxData = param.InitialMaxData;
                    limits.MaxStreamsBidi = param.InitialMaxStreamsBidi;
                    limits.MaxStreamsUni = param.InitialMaxStreamsUni;
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

        internal void ReceiveData(byte[] buffer, int count, IPEndPoint sender, DateTime now)
        {
            var segment = new ArraySegment<byte>(buffer, 0, count);
            var reader = new QuicReader(segment);
            var context = new Context(now);

            while (reader.BytesLeft > 0)
            {
                var status = ReceiveOne(reader, context);

                // Receive will adjust the buffer length once it is known
                segment = segment.Slice(reader.Buffer.Count);
                reader.Reset(segment);
            }
        }

        private ProcessPacketResult Receive1Rtt(QuicReader reader, in ShortPacketHeader header, Context context)
        {
            int pnOffset = reader.BytesRead;
            PacketType packetType = PacketType.OneRtt;
            var epoch = GetEpoch(EncryptionLevel.Application);
            int payloadLength = reader.BytesLeft;

            return ReceiveProtectedFrames(reader, epoch, pnOffset, payloadLength, packetType, context);
        }

        private ProcessPacketResult ReceiveRetry(QuicReader reader, in LongPacketHeader header, Context context)
        {
            throw new NotImplementedException();
        }

        private ProcessPacketResult ReceiveVersionNegotiation(QuicReader reader, in LongPacketHeader header,
            Context context)
        {
            throw new NotImplementedException();
        }

        private ProcessPacketResult ReceiveCommon(QuicReader reader, in LongPacketHeader header,
            in SharedPacketData headerData, Context context)
        {
            //TODO-RZ: Version negotiation
            //TODO-RZ: Check connection id length (beware that length is unbounded in initial packets)

            int pnOffset = reader.BytesRead;
            var epoch = GetEpoch(GetEncryptionLevel(header.PacketType));
            int payloadLength = (int)headerData.Length;
            PacketType packetType = header.PacketType;

            if (_isServer && packetType == PacketType.Initial)
            {
                if (epoch.RecvCryptoSeal == null)
                {
                    // initialize protection keys
                    // clients destination connection Id is ours source connection Id
                    SourceConnectionId = new ConnectionId(header.DestinationConnectionId.ToArray());
                    DestinationConnectionId = new ConnectionId(header.SourceConnectionId.ToArray());

                    _localConnectionIdCollection.Add(SourceConnectionId.Data);
                    DeriveInitialProtectionKeys(SourceConnectionId.Data);
                }

                // check UDP datagram size, by now the reader's buffer end is aligned with the UDP datagram end.
                if (reader.Buffer.Offset + reader.Buffer.Count < QuicConstants.MinimumClientInitialDatagramSize)
                {
                    return CloseConnection(TransportErrorCode.ProtocolViolation, null,
                        QuicErrors.InitialPacketTooShort);
                }
            }

            return ReceiveProtectedFrames(reader, epoch, pnOffset, payloadLength, packetType, context);
        }

        private ProcessPacketResult ReceiveProtectedFrames(QuicReader reader, EpochData epoch, int pnOffset,
            int payloadLength,
            PacketType packetType, Context context)
        {
            if (epoch.RecvCryptoSeal == null)
            {
                // Decryption keys are not available yet, drop the packet for now
                // TODO-RZ: consider buffering the packet
                return ProcessPacketResult.DropPacket;
            }

            var seal = epoch.RecvCryptoSeal!;

            if (!seal.DecryptPacket(reader.Buffer, pnOffset, payloadLength,
                epoch.LargestReceivedPacketNumber))
            {
                // decryption failed, drop the packet.
                reader.Advance(payloadLength);
                return ProcessPacketResult.DropPacket;
            }

            // TODO-RZ: read in a better way
            int pnLength = HeaderHelpers.GetPacketNumberLength(reader.Buffer[0]);
            reader.TryReadTruncatedPacketNumber(pnLength, out uint truncatedPn);

            ulong packetNumber = QuicPrimitives.DecodePacketNumber(epoch.LargestReceivedPacketNumber,
                truncatedPn, pnLength);

            // if (epoch.ReceivedPacketNumbers.Contains(packetNumber))
            // {
            //     return ProcessPacketResult.Ok; // already processed;
            // }

            if (epoch.LargestReceivedPacketNumber < packetNumber)
            {
                epoch.LargestReceivedPacketNumber = packetNumber;
                epoch.LargestReceivedPacketTimestamp = DateTime.Now; //TODO-RZ: pass time externally
            }

            epoch.UnackedPacketNumbers.Add(packetNumber);
            epoch.ReceivedPacketNumbers.Add(packetNumber);

            return ProcessFramesWithoutTag(reader, packetType, context);
        }

        private ProcessPacketResult ProcessFramesWithoutTag(QuicReader reader, PacketType packetType, Context context)
        {
            // HACK: we do not want to try processing the AEAD integrity tag as if it were frames.
            var originalSegment = reader.Buffer;
            int tagLength = GetEpoch(GetEncryptionLevel(packetType)).RecvCryptoSeal!.TagLength;
            reader.Reset(reader.Buffer.Slice(reader.BytesRead, reader.BytesLeft - tagLength));
            var retval = ProcessFrames(reader, packetType, context);
            reader.Reset(originalSegment);
            return retval;
        }

        private ProcessPacketResult ReceiveLongHeaderPackets(QuicReader reader, in LongPacketHeader header,
            Context context)
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
                        headerData.Length > (ulong)reader.BytesLeft)
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

        private ProcessPacketResult ReceiveOne(QuicReader reader, Context context)
        {
            byte first = reader.PeekUInt8();

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

            for (int i = 0; i < _epochs.Length; i++)
            {
                var level = (EncryptionLevel)i;
                var epoch = _epochs[i];

                if (epoch.CryptoOutboundStream.HasPendingData)
                    return level;

                if (epoch.AckElicited)
                    return level;
            }

            return desiredLevel;
        }

        private void SendOne(QuicWriter writer, IPEndPoint? receiver, EncryptionLevel level, Context context)
        {
            // TODO-RZ: process lost packets
            var packetType = level switch
            {
                EncryptionLevel.Initial => PacketType.Initial,
                EncryptionLevel.EarlyData => PacketType.ZeroRtt,
                EncryptionLevel.Handshake => PacketType.Handshake,
                EncryptionLevel.Application => PacketType.OneRtt,
                _ => throw new InvalidOperationException()
            };

            var epoch = GetEpoch(level);
            var seal = epoch.SendCryptoSeal!;

            (uint truncatedPn, int pnLength) = epoch.GetNextPacketNumber();
            WritePacketHeader(writer, packetType, pnLength);

            // for non 1-RTT packets, we reserved 2 bytes which we will overwrite once total payload length is known
            var payloadLengthSpan = writer.Buffer.AsSpan(writer.BytesWritten - 2, 2);

            int pnOffset = writer.BytesWritten;
            writer.WriteTruncatedPacketNumber(pnLength, truncatedPn);

            int maxPacketLength = Math.Min(12000, writer.Buffer.Count);
            // int maxPacketLength = (int)(Connected
            //     // Limit maximum size so that it can be always encoded using 2B varint
            //     ? Math.Min((1 << 14) - 1, GetPeerTransportParameters()!.MaxPacketSize)
            //     // use minimum size for packets during handshake
            //     : QuicConstants.MinimumClientInitialDatagramSize);

            // TODO-RZ: respect control flow limits
            int written = writer.BytesWritten;
            var origBuffer = writer.Buffer;

            writer.Reset(origBuffer.Slice(0, maxPacketLength - seal.TagLength), written);

            WriteFrames(writer, packetType, level, context);

            writer.Reset(origBuffer, writer.BytesWritten);
            if (writer.BytesWritten == written)
            {
                // no data to send
                // TODO-RZ: we might be able to detect this sooner
                writer.Reset(writer.Buffer);
                return;
            }

            // the frame is going to be sent, increment the next packet number
            epoch.NextPacketNumber++;

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

            // reserve space for AEAD integrity tag
            writer.GetWritableSpan(seal.TagLength);
            int payloadLength = writer.BytesWritten - pnOffset;

            // fill in the payload length retrospectively
            if (packetType != PacketType.OneRtt)
            {
                QuicPrimitives.WriteVarInt(payloadLengthSpan, (ulong)payloadLength, 2);
            }

            seal.EncryptPacket(writer.Buffer, pnOffset, payloadLength, truncatedPn);
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
                    writer.Buffer[0],
                    ReadOnlySpan<byte>.Empty,
                    1000 /*arbitrary number with 2-byte encoding*/));
            }
        }

        internal int SendData(byte[] targetBuffer, out IPEndPoint? receiver, DateTime now)
        {
            receiver = default;
            var context = new Context(now);

            var writer = new QuicWriter(targetBuffer);

            int written = 0;
            var level = GetWriteLevel();

            while (true)
            {
                if (GetEpoch(level).SendCryptoSeal == null)
                {
                    // Secrets have not been derived yet, can't send anything
                    break;
                }

                // TODO-RZ get client address
                SendOne(writer, null, level, context);

                if (writer.BytesWritten == 0)
                    break;

                written += writer.BytesWritten;

                // 0-RTT packets do not have Length, so they may not be coalesced
                if (level == EncryptionLevel.Application)
                    break;

                var nextLevel = GetWriteLevel();

                // only coalesce packets in ascending encryption level
                if (nextLevel <= level)
                    break;

                level = nextLevel;
                writer.Reset(writer.Buffer.Slice(writer.BytesWritten));
            }

            return written;
        }

        private static PacketEpoch GetEpoch(PacketType packetType)
        {
            return packetType switch
            {
                PacketType.Initial => PacketEpoch.Initial,
                PacketType.ZeroRtt => PacketEpoch.Application,
                PacketType.Handshake => PacketEpoch.Handshake,
                PacketType.OneRtt => PacketEpoch.Application,
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

        internal EpochData GetEpoch(EncryptionLevel encryptionLevel)
        {
            return encryptionLevel switch
            {
                EncryptionLevel.Initial => _epochs[0],
                EncryptionLevel.Handshake => _epochs[1],
                EncryptionLevel.EarlyData => _epochs[2],
                EncryptionLevel.Application => _epochs[2],
                _ => throw new ArgumentOutOfRangeException(nameof(encryptionLevel), encryptionLevel, null)
            };
        }

        private ProcessPacketResult CloseConnection(TransportErrorCode errorCode, FrameType? frameType, string? reason)
        {
            outboundError = new QuicError(errorCode, frameType, reason);
            return ProcessPacketResult.ConnectionClose;
        }


        public override void Dispose()
        {
            _tls.Dispose();
            _gcHandle.Free();
        }

        internal void SetEncryptionSecrets(EncryptionLevel level, TlsCipherSuite algorithm,
            ReadOnlySpan<byte> readSecret, ReadOnlySpan<byte> writeSecret)
        {
            var epoch = GetEpoch(level);
            Debug.Assert(epoch.SendCryptoSeal == null, "Protection keys already derived");

            epoch.RecvCryptoSeal = new CryptoSeal(algorithm, readSecret);
            epoch.SendCryptoSeal = new CryptoSeal(algorithm, writeSecret);
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
            GetEpoch(level).CryptoOutboundStream.Enqueue(data);
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

        private sealed class Context
        {
            public Context(DateTime now)
            {
                Now = now;
            }

            internal DateTime Now { get; }
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

        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        internal override QuicStreamProvider OpenUnidirectionalStream()
        {
            // TODO-RZ: use messages from string resources
            if (GetRemoteAvailableUnidirectionalStreamCount() == 0)
                throw new InvalidOperationException("Cannot open stream");

            return OpenStream(true);
        }

        internal override QuicStreamProvider OpenBidirectionalStream()
        {
            // TODO-RZ: use messages from string resources
            if (GetRemoteAvailableBidirectionalStreamCount() == 0)
                throw new InvalidOperationException("Cannot open stream");

            return OpenStream(false);
        }

        internal override long GetRemoteAvailableUnidirectionalStreamCount() => throw new NotImplementedException();

        internal override long GetRemoteAvailableBidirectionalStreamCount() => throw new NotImplementedException();

        internal override ValueTask<QuicStreamProvider>
            AcceptStreamAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override SslApplicationProtocol NegotiatedApplicationProtocol { get; }

        internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        #endregion
    }
}
