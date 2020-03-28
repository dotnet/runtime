using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal;
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

        // TODO-RZ: remove this

        private string cert;
        private string privateKey;
        public List<(SslEncryptionLevel, byte[])> ToSend { get; } = new List<(SslEncryptionLevel, byte[])>();

        public int OnDataReceived(SslEncryptionLevel level, ReadOnlySpan<byte> data)
        {
            return Interop.OpenSslQuic.SslProvideQuicData(_ssl, level, data);
        }

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

        internal EpochData GetEpochFromPacketType(PacketType packetType)
        {
            switch (packetType)
            {
                case PacketType.Initial:
                    return _epochs[0];
                case PacketType.Handshake:
                    return _epochs[1];
                case PacketType.ZeroRtt:
                case PacketType.OneRtt:
                    return _epochs[2];

                default:
                    throw new ArgumentOutOfRangeException(nameof(packetType), packetType, null);
            }
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
