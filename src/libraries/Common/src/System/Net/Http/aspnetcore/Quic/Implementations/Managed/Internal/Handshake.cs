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

        public Handshake(IntPtr ctx, string? address = null, string? cert = null, string? privateKey = null)
        {
            var gcHandle = GCHandle.Alloc(this);
            ToSend = new List<(SslEncryptionLevel, byte[])>();

            _ssl = Interop.OpenSslQuic.SslNew(ctx);

            QuicMethods.InitCallbacks(this, _ssl);

            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMinProtoVersion, (long)OpenSslTlsVersion.Tls13, IntPtr.Zero);
            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMaxProtoVersion, (long)OpenSslTlsVersion.Tls13, IntPtr.Zero);

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

        public int DoHandshake()
        {
            return Interop.OpenSslQuic.SslDoHandshake(_ssl);
        }

        public int OnDataReceived(SslEncryptionLevel level, byte[] data)
        {
            return Interop.OpenSslQuic.SslProvideQuicData(_ssl, level, data);
        }
    }
}
