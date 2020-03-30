#nullable enable

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed
{
    internal class ManagedQuicConnection : QuicConnectionProvider
    {
        private readonly Tls _tls;
        internal enum ProcessPacketResult
        {
            Ok,
            DecryptionFail,
            ParsingFail,
        }

        private readonly QuicClientConnectionOptions? _clientOpts;
        private readonly QuicListenerOptions? _serverOpts;

        private GCHandle _gcHandle;

        private bool _isServer;

        private readonly EpochData[] _epochs;

        private readonly TransportParameters localTransportParams;

        private QuicVersion version = QuicVersion.Draft27;

        private ConnectionIdCollection _connectionIdCollection = new ConnectionIdCollection();

        private ConnectionId? _scid;

        private ConnectionId? _dcid;

        // TODO-RZ: flow control counts

        // TODO-RZ: remove these
        private string? cert;

        private string? privateKey;

        public ConnectionId? SourceConnectionId => _scid;

        public ConnectionId? DestinationConnectionId => _dcid;

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
            localTransportParams = new TransportParameters();

            _epochs = new EpochData[3] {new EpochData(), new EpochData(), new EpochData()};
        }

        private void Init()
        {
            _tls.Init(cert, privateKey, _isServer, localTransportParams);

            if (_isServer)
            {
                return;
            }

            // init random connection ids for the client
            _scid = ConnectionId.Random(20);
            _dcid = ConnectionId.Random(20);
            _connectionIdCollection.Add(_dcid.Data);

            // derive also clients initial secrets.
            DeriveInitialProtectionKeys(_dcid.Data);
        }

        private void DeriveInitialProtectionKeys(byte[] dcid)
        {
            var client = KeyDerivation.DeriveClientInitialSecret(dcid);
            var server = KeyDerivation.DeriveServerInitialSecret(dcid);

            if (_isServer)
            {
                HandleSetEncryptionSecrets(EncryptionLevel.Initial, client, server);
            }
            else
            {
                HandleSetEncryptionSecrets(EncryptionLevel.Initial, server, client);
            }
        }

        internal void ReceiveData(byte[] buffer, int count, IPEndPoint sender)
        {
            var segment = new ArraySegment<byte>(buffer, 0, count);
            var reader = new QuicReader(segment);

            while (reader.BytesLeft > 0)
            {
                var status = ReceiveOne(reader);
                Console.WriteLine($"Packet: {status}");

                // Receive will adjust the buffer length once it is known
                segment = segment.Slice(reader.Buffer.Count);
                reader.Reset(segment);
            }
        }

        private ProcessPacketResult Receive1Rtt(QuicReader reader, in ShortPacketHeader header)
        {
            throw new NotImplementedException();
        }

        private ProcessPacketResult ReceiveRetry(QuicReader reader, in LongPacketHeader header)
        {
            throw new NotImplementedException();
        }

        private ProcessPacketResult ReceiveVersionNegotiation(QuicReader reader, in LongPacketHeader header)
        {
            throw new NotImplementedException();
        }

        private ProcessPacketResult ProcessFrames(QuicReader reader, PacketType packetType)
        {
            bool handshakeWanted = false;

            // TODO-RZ: check permitted frames by the packet type
            while (reader.BytesLeft > 0)
            {
                if (reader.PeekFrameType() != FrameType.Padding)
                    Console.WriteLine($"Received {packetType} - {reader.PeekFrameType()}");

                switch (reader.PeekFrameType())
                {
                    case FrameType.Padding:
                        // discard the padding
                        reader.ReadFrameType();
                        break;
                    case FrameType.Crypto:
                    {
                        handshakeWanted = true;
                        if (!CryptoFrame.Read(reader, out var crypto)) return ProcessPacketResult.ParsingFail;
                        // TODO-RZ: Utilize the offset
                        _tls.OnDataReceived(GetEncryptionLevel(packetType), crypto.CryptoData);

                        break;
                    }
                    case FrameType.Ping:
                    case FrameType.Ack:
                    case FrameType.AckWithEcn:
                    case FrameType.ResetStream:
                    case FrameType.StopSending:
                    case FrameType.NewToken:
                    case FrameType.Stream:
                    case FrameType.StreamMask:
                    case FrameType.MaxData:
                    case FrameType.MaxStreamData:
                    case FrameType.MaxStreamsBidirectional:
                    case FrameType.MaxStreamsUnidirectional:
                    case FrameType.DataBlocked:
                    case FrameType.StreamDataBlocked:
                    case FrameType.StreamsBlockedBidirectional:
                    case FrameType.StreamsBlockedUnidirectional:
                    case FrameType.NewConnectionId:
                    case FrameType.RetireConnectionId:
                    case FrameType.PathChallenge:
                    case FrameType.PathResponse:
                    case FrameType.ConnectionCloseQuic:
                    case FrameType.ConnectionCloseApplication:
                    case FrameType.HandshakeDone:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // do handshake to set encryption secrets (to be able to process coalesced packets
            if (handshakeWanted)
            {
                DoHandshake();
            }

            return ProcessPacketResult.Ok;
        }

        private ProcessPacketResult ReceiveCommon(QuicReader reader, in LongPacketHeader header,
            in SharedPacketData headerData)
        {
            int pnOffset = reader.BytesRead;

            // first, strip packet protection.
            var encryptionLevel = GetEncryptionLevel(header.PacketType);
            var epoch = GetEpoch(encryptionLevel);

            int payloadLength = (int)headerData.Length;

            // TODO-RZ: handle repeated receipt of first initial?
            if (_isServer && encryptionLevel == EncryptionLevel.Initial)
            {
                // initialize protection keys
                // clients destination connection Id is ours source connection Id
                _scid = new ConnectionId(header.DestinationConnectionId.ToArray());
                _dcid = new ConnectionId(header.SourceConnectionId.ToArray());
                _connectionIdCollection.Add(_dcid.Data);

                DeriveInitialProtectionKeys(_scid.Data);
            }

            if (epoch.RecvCryptoSeal == null)
            {
                // Decryption keys are not available yet, drop the packet for now
                // TODO-RZ: consider buffering the packet
                return ProcessPacketResult.DecryptionFail;
            }

            var seal = epoch.RecvCryptoSeal!;

            if (!seal.DecryptPacket(reader.Buffer, pnOffset, payloadLength,
                epoch.LargestTransportedPacketNumber))
            {
                // decryption failed, drop the packet.
                reader.Advance(payloadLength);
                return ProcessPacketResult.DecryptionFail;
            }

            // TODO-RZ: read in a better way
            var pnLength = HeaderHelpers.GetPacketNumberLength(reader.Buffer[0]);
            reader.TryReadTruncatedPacketNumber(pnLength, out uint truncatedPn);

            epoch.ReceivedPacketNumbers.Add(QuicPrimitives.DecodePacketNumber(epoch.LargestTransportedPacketNumber,
                truncatedPn, pnLength));

            return ProcessFramesWithoutTag(reader, header.PacketType);
        }

        private ProcessPacketResult ProcessFramesWithoutTag(QuicReader reader, PacketType packetType)
        {
            // HACK: we do not want to try processing the AEAD integrity tag as if it were frames.
            var originalSegment = reader.Buffer;
            var tagLength = GetEpoch(GetEncryptionLevel(packetType)).RecvCryptoSeal!.TagLength;
            reader.Reset(reader.Buffer.Slice(reader.BytesRead, reader.BytesLeft - tagLength));
            var retval = ProcessFrames(reader, packetType);
            reader.Reset(originalSegment);
            return retval;
        }

        private ProcessPacketResult ReceiveLongHeaderPackets(QuicReader reader, in LongPacketHeader header)
        {
            //TODO-RZ check header contents based on the type (connection id length)

            var type = header.PacketType;

            switch (type)
            {
                case PacketType.Initial:
                case PacketType.Handshake:
                case PacketType.ZeroRtt:
                    if (!SharedPacketData.Read(reader, header.FirstByte, out var headerData))
                    {
                        return ProcessPacketResult.ParsingFail;
                    }

                    // total length of the packet is known
                    // TODO-RZ: check bounds
                    reader.Reset(reader.Buffer.Slice(0, reader.BytesRead + (int)headerData.Length), reader.BytesRead);

                    return ReceiveCommon(reader, header, headerData);
                case PacketType.Retry:
                    return ReceiveRetry(reader, header);
                case PacketType.VersionNegotiation:
                    return ReceiveVersionNegotiation(reader, header);
                case PacketType.OneRtt:
                    // this type is handled elsewhere
                    throw new InvalidOperationException("Unreachable");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ProcessPacketResult ReceiveOne(QuicReader reader)
        {
            byte first = reader.PeekUInt8();
            // TODO-RZ: check fixed bit, drop too small packets etc.

            ProcessPacketResult result;
            if (HeaderHelpers.IsLongHeader(first))
            {
                // TODO-RZ: check encryption keys availability
                if (!LongPacketHeader.Read(reader, out var header))
                {
                    return ProcessPacketResult.ParsingFail;
                }

                result = ReceiveLongHeaderPackets(reader, header);
            }

            else
            {
                if (!ShortPacketHeader.Read(reader, _connectionIdCollection, out var header))
                {
                    return ProcessPacketResult.ParsingFail;
                }

                result = Receive1Rtt(reader, header);
            }

            return result;
        }

        internal EncryptionLevel GetWriteLevel()
        {
            // TODO-RZ: handle resend and packet loss of earlier levels
            EncryptionLevel desiredLevel = _tls.GetWriteLevel();
            // if not connected, then handshake is not done yet
            // if (!Connected && desiredLevel == EncryptionLevel.Application)
                // return EncryptionLevel.Handshake;

            for (int i = 0; i < _epochs.Length; i++)
            {
                if (_epochs[i].CryptoStream.HasDataToSend)
                    return (EncryptionLevel)i;
            }

            return desiredLevel;
        }

        internal void SendOne(QuicWriter writer, IPEndPoint? receiver)
        {
            if (_isServer && GetEpoch(EncryptionLevel.Initial).RecvCryptoSeal == null)
            {
                // if initial secrets have not been derived yet, we have nothing to send
                return;
            }

            // TODO-RZ: process lost packets

            var level = GetWriteLevel();

            var packetType = level switch
            {
                EncryptionLevel.Initial => PacketType.Initial,
                EncryptionLevel.EarlyData => PacketType.ZeroRtt,
                EncryptionLevel.Handshake => PacketType.Handshake,
                EncryptionLevel.Application => PacketType.OneRtt,
                _ => throw new InvalidOperationException()
            };

            var epoch = GetEpoch(GetEncryptionLevel(packetType));
            var seal = epoch.SendCryptoSeal!;

            (uint truncatedPn, int pnLength) = epoch.GetNextPacketNumber();

            // cap maximum packet size length to 2 bytes
            int maxPacketLength = (int)(Connected
                ? Math.Min(16383, GetPeerTransportParameters()!.MaxPacketSize)
                : QuicConstants.MinimumClientInitialDatagramSize);

            // TODO-RZ: respect control flow limits

            // Write header
            if (packetType == PacketType.OneRtt)
            {
                // short header
                // TODO-RZ: implement spin
                // TODO-RZ: implement key update
                ShortPacketHeader.Write(writer, new ShortPacketHeader(false, false, pnLength, DestinationConnectionId!));
            }
            else
            {
                LongPacketHeader.Write(writer, new LongPacketHeader(
                    packetType,
                    pnLength,
                    version,
                    _dcid!.Data,
                    _scid!.Data));

                // HACK: reserve 2 bytes for payload length and overwrite it later
                SharedPacketData.Write(writer, new SharedPacketData(
                    writer.Buffer[0],
                    ReadOnlySpan<byte>.Empty,
                    1000 /*arbitrary number with 2-byte encoding*/));
            }

            int pnOffset = writer.BytesWritten;
            writer.WriteTruncatedPacketNumber(pnLength, truncatedPn);

            var payloadLengthSpan = writer.Buffer.AsSpan(writer.BytesWritten - 2 - pnLength, 2);

            var written = writer.BytesWritten;
            WriteFrames(writer, packetType, level);
            if (writer.BytesWritten == written)
            {
                // no data to send
                // TODO-RZ: Can we find out sooner?
                writer.Reset(writer.Buffer, 0);
                return;
            }

            // rest of the buffer will be padding = 0x00 bytes
            if (packetType == PacketType.Initial)
            {
                int paddingLength = QuicConstants.MinimumClientInitialDatagramSize - seal.TagLength - writer.BytesWritten;
                if (paddingLength > 0)
                    writer.GetWritableSpan(paddingLength).Clear();
            }

            // reserve space for AEAD integrity tag
            writer.GetWritableSpan(seal.TagLength);
            int payloadLength = writer.BytesWritten - pnOffset;

            // fill in the payload length retrospectively
            if (packetType != PacketType.OneRtt)
            {
                // TODO-RZ: this is ugly
                BinaryPrimitives.WriteUInt16BigEndian(payloadLengthSpan, (ushort)(payloadLength | 0x4000));
            }

            // encryption adds tag after the packet data
            // TODO-RZ not all packets are encrypted, also make sure the padding was at least TagLength
            seal.EncryptPacket(writer.Buffer, pnOffset, payloadLength, truncatedPn);
        }

        internal int SendData(byte[] targetBuffer, out IPEndPoint? receiver)
        {
            var writer = new QuicWriter(targetBuffer);

            int written = 0;
            while (true)
            {
                // TODO-RZ get client address
                SendOne(writer, null);
                if (writer.BytesWritten == 0)
                    break;
                written += writer.BytesWritten;

                writer.Reset(writer.Buffer.Slice(writer.BytesWritten));
            }

            receiver = default;
            return written;
        }

        // TODO-RZ: calculate crypto data offset
        private void WriteFrames(QuicWriter writer, PacketType packetType, EncryptionLevel level)
        {
            var epoch = GetEpoch(level);

            // TODO-RZ other frames

            while (epoch.CryptoStream.HasDataToSend)
            {
                var (data, offset) = epoch.CryptoStream.GetDataToSend();
                CryptoFrame.Write(writer, new CryptoFrame((ulong) offset, data));
            }
        }

        internal SslError DoHandshake()
        {
            return _tls.DoHandshake();
        }

        internal TransportParameters GetPeerTransportParameters()
        {
            return _tls.GetPeerTransportParameters(_isServer);
        }

        private EncryptionLevel GetEncryptionLevel(PacketType packetType)
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

        private EpochData GetEpoch(EncryptionLevel encryptionLevel)
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

        #region Public API implementation

        internal override bool Connected => _tls.IsHandshakeFinishhed;

        internal override IPEndPoint LocalEndPoint => _clientOpts!.LocalEndPoint!;

        internal override IPEndPoint RemoteEndPoint => _clientOpts!.RemoteEndPoint!;

        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        internal override QuicStreamProvider OpenUnidirectionalStream() => throw new NotImplementedException();

        internal override QuicStreamProvider OpenBidirectionalStream() => throw new NotImplementedException();

        internal override long GetRemoteAvailableUnidirectionalStreamCount() => throw new NotImplementedException();

        internal override long GetRemoteAvailableBidirectionalStreamCount() => throw new NotImplementedException();

        internal override ValueTask<QuicStreamProvider>
            AcceptStreamAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override SslApplicationProtocol NegotiatedApplicationProtocol { get; }

        internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        #endregion


        public override void Dispose()
        {
            _tls.Dispose();
            _gcHandle.Free();
        }

        internal int HandleSetEncryptionSecrets(EncryptionLevel level, ReadOnlySpan<byte> readSecret,
            ReadOnlySpan<byte> writeSecret)
        {
            Console.WriteLine($"SetEncryptionSecrets({level})");

            var epoch = GetEpoch(level);
            Debug.Assert(epoch.SendCryptoSeal == null, "Protection keys already derived");

            epoch.RecvCryptoSeal = new CryptoSeal(CipherAlgorithm.AEAD_AES_128_GCM, readSecret);
            epoch.SendCryptoSeal = new CryptoSeal(CipherAlgorithm.AEAD_AES_128_GCM, writeSecret);

            return 1;
        }

        internal int HandleAddHandshakeData(EncryptionLevel level, ReadOnlySpan<byte> data)
        {
            Console.WriteLine($"AddHandshakeData({level})");
            GetEpoch(level).CryptoStream.Add(data);
            return 1;
        }

        internal int HandleFlush()
        {
            Console.WriteLine("FlushFlight");

            return 1;
        }

        internal int HandleSendAlert(EncryptionLevel level, TlsAlert alert)
        {
            Console.WriteLine($"SendAlert({level}): {(byte)alert} (0x{(byte)alert:x2}) - {alert}");

            return 1;
        }
    }
}
