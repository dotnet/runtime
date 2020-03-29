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
    internal enum EncryptionLevel
    {
        Initial,
        Handshake,
        EarlyData,
        Application,
        None,
    }

    internal class ManagedQuicConnection : QuicConnectionProvider
    {
        internal enum ProcessPacketResult
        {
            Ok,
            DecryptionFail,
            ParsingFail,
        }

        private readonly QuicClientConnectionOptions? _clientOpts;
        private readonly QuicListenerOptions? _serverOpts;

        private readonly IntPtr _ssl;
        private GCHandle _gcHandle;

        private bool _isServer;

        private readonly EpochData[] _epochs;

        private readonly TransportParameters localTransportParams;
        private TransportParameters? remoteTransportParams;

        private QuicVersion version = QuicVersion.Draft27;

        private ConnectionIdCollection _connectionIdCollection = new ConnectionIdCollection();

        private ConnectionId? _scid;

        private ConnectionId? _dcid;


        // TODO-RZ: flow control counts


        // TODO-RZ: remove these

        private string? cert;

        private string? privateKey;

        public List<(OpenSslEncryptionLevel, byte[])> ToSend { get; } = new List<(OpenSslEncryptionLevel, byte[])>();

        public int OnDataReceived(OpenSslEncryptionLevel level, ReadOnlySpan<byte> data)
        {
            return Interop.OpenSslQuic.SslProvideQuicData(_ssl, level, data);
        }

        private static OpenSslEncryptionLevel GetOsslEncryptionLevel(EncryptionLevel level)
        {
            var osslLevel = level switch
            {
                EncryptionLevel.Initial => OpenSslEncryptionLevel.Initial,
                EncryptionLevel.Handshake => OpenSslEncryptionLevel.Handshake,
                EncryptionLevel.EarlyData => OpenSslEncryptionLevel.EarlyData,
                EncryptionLevel.Application => OpenSslEncryptionLevel.Application,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
            };
            return osslLevel;
        }

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
            _ssl = Interop.OpenSslQuic.SslCreate();
            _gcHandle = GCHandle.Alloc(this);

            _isServer = isServer;

            // TODO-RZ: compose transport params from options
            localTransportParams = new TransportParameters();

            _epochs = new EpochData[3] {new EpochData(), new EpochData(), new EpochData()};
        }

        private unsafe void Init()
        {
            QuicMethods.InitCallbacks(_gcHandle, _ssl);

            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMinProtoVersion, (long)OpenSslTlsVersion.Tls13,
                IntPtr.Zero);
            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMaxProtoVersion, (long)OpenSslTlsVersion.Tls13,
                IntPtr.Zero);

            if (cert != null)
                Interop.OpenSslQuic.SslUseCertificateFile(_ssl, cert, SslFiletype.Pem);
            if (privateKey != null)
                Interop.OpenSslQuic.SslUsePrivateKeyFile(_ssl, privateKey, SslFiletype.Pem);

            if (_isServer)
            {
                Interop.OpenSslQuic.SslSetAcceptState(_ssl);
            }
            else
            {
                Interop.OpenSslQuic.SslSetConnectState(_ssl);
                // TODO-RZ get hostname
                Interop.OpenSslQuic.SslSetTlsExHostName(_ssl, "localhost:2000");

                // init random connection ids for the client
                _scid = ConnectionId.Random(20);
                _dcid = ConnectionId.Random(20);
                _connectionIdCollection.Add(_dcid.Data);

                // derive encryption secrets straight away.
                DeriveInitialProtectionKeys(_dcid.Data);
            }

            // init transport parameters
            byte[] buffer = new byte[1024];
            var writer = new QuicWriter(buffer);
            TransportParameters.Write(writer, _isServer, localTransportParams);
            fixed (byte* pData = buffer)
            {
                // TODO-RZ: check return value == 1
                Interop.OpenSslQuic.SslSetQuicTransportParams(_ssl, pData, new IntPtr(writer.BytesWritten));
            }
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
                ReceiveOne(reader);

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

        private ProcessPacketResult ReceiveVeersionNegotiation(QuicReader reader, in LongPacketHeader header)
        {
            throw new NotImplementedException();
        }

        private ProcessPacketResult ProcessFrames(QuicReader reader)
        {
            // TODO-RZ: check permitted frames by the packet type
            while (reader.BytesLeft > 0)
            {
                switch (reader.PeekFrameType())
                {
                    case FrameType.Padding:
                        // discard the padding
                        reader.ReadFrameType();
                        break;
                    case FrameType.Crypto:
                    {
                        if (!CryptoFrame.Read(reader, out var crypto))
                            throw new InvalidOperationException("TODO: frame encoding error");
                        // TODO-RZ: Utilize the offset
                        OnDataReceived(OpenSslEncryptionLevel.Initial, crypto.CryptoData);

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

            epoch.ReceivedPacketNumbers.Add(QuicEncoding.DecodePacketNumber(epoch.LargestTransportedPacketNumber, truncatedPn, pnLength));

            return ProcessFramesWithoutTag(reader, seal.TagLength);
        }

        private ProcessPacketResult ProcessFramesWithoutTag(QuicReader reader, int tagLength)
        {
            // HACK: we do not want to try processing the tag as if it were frames.
            var originalSegment = reader.Buffer;
            reader.Reset(reader.Buffer.Slice(reader.BytesRead, reader.BytesLeft - tagLength));
            var retval = ProcessFrames(reader);
            reader.Reset(originalSegment);
            return retval;
        }

        private ProcessPacketResult ReceiveLongHeaderPackets(QuicReader reader, in LongPacketHeader header)
        {
            //TODO-RZ check header contents based on the type (connection id length)

            var type = header.PacketType;
            var epoch = GetEpoch(GetEncryptionLevel(type));

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
                    return ReceiveVeersionNegotiation(reader, header);
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

            if (HeaderHelpers.IsLongHeader(first))
            {
                // TODO-RZ: check encryption keys availability
                if (!LongPacketHeader.Read(reader, out var header))
                {
                    return ProcessPacketResult.ParsingFail;
                }

                return ReceiveLongHeaderPackets(reader, header);
            }

            else
            {
                if (!ShortPacketHeader.Read(reader, _connectionIdCollection, out var header))
                {
                    return ProcessPacketResult.ParsingFail;
                }

                return Receive1Rtt(reader, header);
            }
        }

        internal int SendData(byte[] targetBuffer, out IPEndPoint? receiver)
        {
            // TODO-RZ: proceess lost packets

            // TODO-RZ: get current write level from SSL
            // TODO-RZ: move this to epoch's crypto stream
            if (ToSend.Count <= 0)
            {
                receiver = default;
                return 0;
            }

            var (level, cryptoData) = ToSend[0];
            ToSend.RemoveAt(0);

            var packetType = level switch
            {
                OpenSslEncryptionLevel.Initial => PacketType.Initial,
                OpenSslEncryptionLevel.EarlyData => throw new NotImplementedException(),
                OpenSslEncryptionLevel.Handshake => throw new NotImplementedException(),
                OpenSslEncryptionLevel.Application => throw new NotImplementedException(),
                _ => throw new InvalidOperationException()
            };

            var epoch = GetEpoch(GetEncryptionLevel(packetType));
            var seal = epoch.SendCryptoSeal!;

            var writer = new QuicWriter(targetBuffer);

            // TODO-RZ: get packet number from epoch data
            uint truncatedPn = 0;
            int pnLength = 1;

            LongPacketHeader.Write(writer, new LongPacketHeader(
                packetType,
                pnLength,
                version,
                _dcid!.Data,
                _scid!.Data));

            // HACK: reserve 2 bytes for payload length and overwrite it later
            SharedPacketData.Write(writer, new SharedPacketData(
                targetBuffer[0],
                ReadOnlySpan<byte>.Empty,
                1000 /*arbitrary number with 2-byte encoding*/));

            int pnOffset = writer.BytesWritten;
            writer.WriteTruncatedPacketNumber(pnLength, truncatedPn);

            var payloadLengthSpan = targetBuffer.AsSpan(writer.BytesWritten - 2 - pnLength, 2);

            // TODO-RZ: calculate crypto data offset
            CryptoFrame.Write(writer, new CryptoFrame(0, cryptoData));

            // rest of the buffer will be padding = 0x00 bytes
            int paddingLength = QuicConstants.MinimumClientInitialDatagramSize - seal.TagLength - writer.BytesWritten;
            writer.GetWritableSpan(paddingLength).Clear();

            // fill in the payload length retrospectively
            int payloadLength = writer.BytesWritten - pnOffset + seal.TagLength;
            // TODO-RZ: this is ugly
            BinaryPrimitives.WriteUInt16BigEndian(payloadLengthSpan, (ushort)(payloadLength | 0x4000));

            // encryption adds tag after the packet data
            // TODO-RZ not all packets are encrypted, also make sure the padding was at least TagLength
            seal.EncryptPacket(targetBuffer, pnOffset, payloadLength, truncatedPn);
            writer.GetWritableSpan(seal.TagLength);

            receiver = _clientOpts!.RemoteEndPoint!;
            return writer.BytesWritten;
        }

        internal SslError DoHandshake()
        {
            var status = Interop.OpenSslQuic.SslDoHandshake(_ssl);
            if (status < 0)
            {
                return (SslError)Interop.OpenSslQuic.SslGetError(_ssl, status);
            }

            return SslError.None;
        }

        internal unsafe TransportParameters GetPeerTransportParameters()
        {
            if (remoteTransportParams == null)
            {
                byte[] buffer = new byte[1024];
                byte* data;
                IntPtr length;
                Interop.OpenSslQuic.SslGetPeerQuicTransportParams(_ssl, out data, out length);

                new Span<byte>(data, length.ToInt32()).CopyTo(buffer);
                var reader = new QuicReader(new ArraySegment<byte>(buffer, 0, length.ToInt32()));
                if (!TransportParameters.Read(reader, _isServer, out remoteTransportParams))
                {
                    throw new InvalidOperationException("Failed to get peers transport params");
                }
            }

            return remoteTransportParams!;
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

        internal override bool Connected { get; }

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
            QuicMethods.DeinitCallbacks(_ssl);
            _gcHandle.Free();
            Interop.OpenSslQuic.SslFree(_ssl);
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
            ToSend.Add((GetOsslEncryptionLevel(level), data.ToArray()));

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
