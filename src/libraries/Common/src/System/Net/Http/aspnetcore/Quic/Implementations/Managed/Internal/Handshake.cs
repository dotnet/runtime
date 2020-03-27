#nullable enable
using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    internal class Handshake : IQuicCallback, IDisposable
    {
        private readonly IntPtr _ssl;
        private readonly bool isServer;

        private readonly TransportParameters localTransportParams;
        private TransportParameters? remoteTransportParams;

        public Handshake(IntPtr ctx, TransportParameters localTransportParams, string? address = null, string? cert = null, string? privateKey = null)
        {
            this.localTransportParams = localTransportParams;
            isServer = address == null;

            var gcHandle = GCHandle.Alloc(this);
            ToSend = new List<(SslEncryptionLevel, byte[])>();

            _ssl = Interop.OpenSslQuic.SslNew(ctx);

            QuicMethods.InitCallbacks(this, _ssl);

            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMinProtoVersion, (long)OpenSslTlsVersion.Tls13, IntPtr.Zero);
            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMaxProtoVersion, (long)OpenSslTlsVersion.Tls13, IntPtr.Zero);

            SetTransportParams(localTransportParams);

            if (cert != null)
                Interop.OpenSslQuic.SslUseCertificateFile(_ssl, cert, SslFiletype.Pem);
            if (privateKey != null)
                Interop.OpenSslQuic.SslUsePrivateKeyFile(_ssl, privateKey, SslFiletype.Pem);

            if (address == null)
            {
                Interop.OpenSslQuic.SslSetAcceptState(_ssl);
            }
            else
            {
                Interop.OpenSslQuic.SslSetConnectState(_ssl);
                Interop.OpenSslQuic.SslSetTlsExHostName(_ssl, address);
            }
        }

        private unsafe int SetTransportParams(TransportParameters transportParameters)
        {
            byte[] buffer = new byte[1024];
            var writer = new QuicWriter(buffer);
            TransportParameters.Write(writer, isServer, transportParameters);
            fixed (byte* pData = buffer)
            {
                return Interop.OpenSslQuic.SslSetQuicTransportParams(_ssl, pData, new IntPtr(writer.BytesWritten));
            }
        }

        public unsafe TransportParameters GetPeerTransportParams()
        {
            if (remoteTransportParams == null)
            {
                byte[] buffer = new byte[1024];
                byte* data;
                IntPtr length;
                Interop.OpenSslQuic.SslGetPeerQuicTransportParams(_ssl, out data, out length);

                new Span<byte>(data, length.ToInt32()).CopyTo(buffer);
                var reader = new QuicReader(buffer, length.ToInt32());
                if (!TransportParameters.Read(reader, isServer, out remoteTransportParams))
                {
                    throw new InvalidOperationException("Failed to get peers transport params");
                }
            }

            return remoteTransportParams;
        }

        public List<(SslEncryptionLevel, byte[])> ToSend { get; }

        public void Dispose()
        {
            QuicMethods.DeinitCallbacks(_ssl);
            Interop.OpenSslQuic.SslFree(_ssl);
        }

        public int SetEncryptionSecrets(SslEncryptionLevel level, byte[] readSecret, byte[] writeSecret)
        {
            Console.WriteLine($"SetEncryptionSecrets({level})");

            return 1;
        }

        public int AddHandshakeData(SslEncryptionLevel level, byte[] data)
        {
            Console.WriteLine($"AddHandshakeData({level})");
            ToSend.Add((level, data));
            return 1;
        }

        public int Flush()
        {
            Console.WriteLine("FlushFlight");

            return 1;
        }

        public int SendAlert(SslEncryptionLevel level, TlsAlert alert)
        {
            Console.WriteLine($"SendAlert({level}): {(byte) alert} (0x{(byte) alert:x2}) - {alert}");

            return 1;
        }

        public SslError DoHandshake()
        {
            var status = Interop.OpenSslQuic.SslDoHandshake(_ssl);
            if (status < 0)
            {
                return (SslError) Interop.OpenSslQuic.SslGetError(_ssl, status);
            }

            return SslError.None;
        }

        public int OnDataReceived(SslEncryptionLevel level, byte[] data)
        {
            return Interop.OpenSslQuic.SslProvideQuicData(_ssl, level, data);
        }
    }
}
