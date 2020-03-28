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
        private readonly QuicClientConnectionOptions _clientOpts;
        private readonly QuicListenerOptions _serverOpts;

        private readonly IntPtr _ssl;
        private GCHandle _gcHandle;

        private bool _isServer;

        private readonly EpochData[] _epochs;

        private readonly TransportParameters localTransportParams;
        private TransportParameters? remoteTransportParams;

        private QuicVersion version = QuicVersion.Draft27;

        private ConnectionId? _scid;

        private ConnectionId? _dcid;


        // TODO-RZ: flow control counts


        // TODO-RZ: remove these

        private string cert;

        private string privateKey;

        public List<(SslEncryptionLevel, byte[])> ToSend { get; } = new List<(SslEncryptionLevel, byte[])>();

        public int OnDataReceived(SslEncryptionLevel level, ReadOnlySpan<byte> data)
        {
            return Interop.OpenSslQuic.SslProvideQuicData(_ssl, level, data);
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

            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMinProtoVersion, (long)OpenSslTlsVersion.Tls13, IntPtr.Zero);
            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMaxProtoVersion, (long)OpenSslTlsVersion.Tls13, IntPtr.Zero);

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
                HandleSetEncryptionSecrets(SslEncryptionLevel.Initial, client, server);
            }
            else
            {
                HandleSetEncryptionSecrets(SslEncryptionLevel.Initial, server, client);
            }
        }

        internal void ReceiveData(byte[] buffer, int count, IPEndPoint sender)
        {
            var reader = new QuicReader(buffer, count);

            while (reader.BytesLeft > 0)
                ReceiveOne(reader);
        }

        private void ReceiveOne(QuicReader reader)
        {
            byte first = reader.PeekUInt8();

            // TODO-RZ: check fixed bit, drop too small packets etc.

            // TODO-RZ: not all packet types can be guessed from the first byte
            var type = HeaderHelpers.GetPacketType(first);

            switch (type)
            {
                case PacketType.Initial:
                    break;
                case PacketType.ZeroRtt:
                case PacketType.Handshake:
                case PacketType.Retry:
                case PacketType.OneRtt:
                case PacketType.VersionNegotiation:
                    throw new NotImplementedException("Packet type not implemented");
                default:
                    throw new ArgumentOutOfRangeException();
            }

            int startOffset = reader.BytesRead;
            var epoch = GetEpoch(GetEncryptionLevel(type));

            // parse long header to get connection IDs
            if (!LongPacketHeader.Read(reader, out var header))
                //TODO-RZ check connection id lengths
                throw new InvalidOperationException("TODO: handle failure");

            if (!SharedPacketData.Read(reader, first, out var headerData))
                throw new InvalidOperationException("TODO: handle failure");

            // our source connection Id is clients destination Id
            _scid = new ConnectionId(header.DestinationConnectionId.ToArray());
            _dcid = new ConnectionId(header.SourceConnectionId.ToArray());

            DeriveInitialProtectionKeys(_scid.Data);

            int pnOffset = reader.BytesRead;
            int payloadLength = (int) headerData.Length;

            if (!epoch.RecvCryptoSeal.DecryptPacket(reader.Buffer.AsSpan(startOffset), pnOffset, payloadLength, epoch.LargestTransportedPacketNumber))
            {
                // decryption failed, drop the packet.
                reader.Advance(payloadLength);
                return;
            }

            // TODO-RZ: read in a better way
            reader.TryReadTruncatedPacketNumber(HeaderHelpers.GetPacketNumberLength(reader.Buffer[0]),
                out uint truncatedPn);

            var tagStart = pnOffset + payloadLength - epoch.RecvCryptoSeal.TagLength;

            // process the payload
            // TODO-RZ: check permitted frames by the packet type
            while (reader.BytesRead < tagStart)
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
                        OnDataReceived(SslEncryptionLevel.Initial, crypto.CryptoData);

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

            reader.Advance(epoch.RecvCryptoSeal.TagLength);
        }

        internal int SendData(byte[] targetBuffer, out IPEndPoint receiver)
        {
            // TODO-RZ: move this to epoch's crypto stream
            if (ToSend.Count <= 0)
            {
                receiver = default;
                return 0;
            }

            var (level, cryptoData) = ToSend[0];
            ToSend.RemoveAt(0);

            PacketType packetType;

            switch (level)
            {
                case SslEncryptionLevel.Initial:
                    packetType = PacketType.Initial;
                    break;
                case SslEncryptionLevel.EarlyData:
                case SslEncryptionLevel.Handshake:
                case SslEncryptionLevel.Application:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException();
            }

            var epoch = GetEpoch(level);

            var writer = new QuicWriter(targetBuffer);

            // TODO-RZ: get packet number from epoch data
            uint truncatedPn = 0;
            int pnLength = 1;

            LongPacketHeader.Write(writer, new LongPacketHeader(
                packetType,
                pnLength,
                version,
                _dcid.Data,
                _scid.Data));

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
            int paddingLength = QuicConstants.MinimumClientInitialDatagramSize - epoch.SendCryptoSeal.TagLength - writer.BytesWritten;
            writer.GetWritableSpan(paddingLength).Clear();

            // fill in the payload length retrospectively
            int payloadLength = writer.BytesWritten - pnOffset + epoch.SendCryptoSeal.TagLength;
            // TODO-RZ: this is ugly
            BinaryPrimitives.WriteUInt16BigEndian(payloadLengthSpan, (ushort) (payloadLength | 0x4000));

            // encryption adds tag after the packet data
            // TODO-RZ not all packets are encrypted, also make sure the padding was at least TagLength
            epoch.SendCryptoSeal.EncryptPacket(targetBuffer, pnOffset, payloadLength, truncatedPn);
            writer.GetWritableSpan(epoch.SendCryptoSeal.TagLength);

            receiver = _clientOpts.RemoteEndPoint;
            return writer.BytesWritten;
        }

        internal SslError DoHandshake()
        {
            var status = Interop.OpenSslQuic.SslDoHandshake(_ssl);
            if (status < 0)
            {
                return (SslError) Interop.OpenSslQuic.SslGetError(_ssl, status);
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
                var reader = new QuicReader(buffer, length.ToInt32());
                if (!TransportParameters.Read(reader, _isServer, out remoteTransportParams))
                {
                    throw new InvalidOperationException("Failed to get peers transport params");
                }
            }

            return remoteTransportParams;
        }

        private SslEncryptionLevel GetEncryptionLevel(PacketType packetType)
        {
            return packetType switch
            {
                PacketType.Initial => SslEncryptionLevel.Initial,
                PacketType.Handshake => SslEncryptionLevel.Handshake,
                PacketType.ZeroRtt => SslEncryptionLevel.EarlyData,
                PacketType.OneRtt => SslEncryptionLevel.Application,
                _ => throw new ArgumentOutOfRangeException(nameof(packetType), packetType, null)
            };
        }

        private EpochData GetEpoch(SslEncryptionLevel encryptionLevel)
        {
            return encryptionLevel switch
            {
                SslEncryptionLevel.Initial => _epochs[0],
                SslEncryptionLevel.Handshake => _epochs[1],
                SslEncryptionLevel.EarlyData => _epochs[2],
                SslEncryptionLevel.Application => _epochs[2],
                _ => throw new ArgumentOutOfRangeException(nameof(encryptionLevel), encryptionLevel, null)
            };
        }

        #region Public API implementation

        internal override bool Connected { get; }

        internal override IPEndPoint LocalEndPoint => _clientOpts.LocalEndPoint!;

        internal override IPEndPoint RemoteEndPoint => _clientOpts.RemoteEndPoint!;

        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override QuicStreamProvider OpenUnidirectionalStream() => throw new NotImplementedException();

        internal override QuicStreamProvider OpenBidirectionalStream() => throw new NotImplementedException();

        internal override long GetRemoteAvailableUnidirectionalStreamCount() => throw new NotImplementedException();

        internal override long GetRemoteAvailableBidirectionalStreamCount() => throw new NotImplementedException();

        internal override ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override SslApplicationProtocol NegotiatedApplicationProtocol { get; }

        internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        #endregion


        public override void Dispose()
        {
            QuicMethods.DeinitCallbacks(_ssl);
            _gcHandle.Free();
            Interop.OpenSslQuic.SslFree(_ssl);
        }

        internal int HandleSetEncryptionSecrets(SslEncryptionLevel level, ReadOnlySpan<byte> readSecret, ReadOnlySpan<byte> writeSecret)
        {
            Console.WriteLine($"SetEncryptionSecrets({level})");

            var epoch = GetEpoch(level);
            Debug.Assert(epoch.SendCryptoSeal == null, "Protection keys already derived");

            epoch.RecvCryptoSeal = new CryptoSeal(CipherAlgorithm.AEAD_AES_128_GCM, readSecret);
            epoch.SendCryptoSeal = new CryptoSeal(CipherAlgorithm.AEAD_AES_128_GCM, writeSecret);

            return 1;
        }

        internal int HandleAddHandshakeData(SslEncryptionLevel level, ReadOnlySpan<byte> data)
        {
            Console.WriteLine($"AddHandshakeData({level})");
            ToSend.Add((level, data.ToArray()));

            return 1;
        }

        internal int HandleFlush()
        {
            Console.WriteLine("FlushFlight");

            return 1;
        }

        internal int HandleSendAlert(SslEncryptionLevel level, TlsAlert alert)
        {
            Console.WriteLine($"SendAlert({level}): {(byte) alert} (0x{(byte) alert:x2}) - {alert}");

            return 1;
        }
    }
}
