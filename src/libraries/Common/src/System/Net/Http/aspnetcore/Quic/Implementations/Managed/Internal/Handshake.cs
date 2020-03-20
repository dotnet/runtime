#nullable enable
using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    internal class Handshake : IQuicCallback, IDisposable
    {
        private readonly Ssl _ssl;

        public Handshake(SslContext ctx, string? address = null, string? cert = null, string? privateKey = null)
        {
            var gcHandle = GCHandle.Alloc(this);
            ToSend = new List<(SslEncryptionLevel, byte[])>();

            _ssl = Ssl.New(ctx);

            _ssl.SetCallbackInterface(GCHandle.ToIntPtr(gcHandle));
            _ssl.MinProtoVersion = TlsVersion.Tls13;
            _ssl.MaxProtoVersion = TlsVersion.Tls13;

            _ssl.SetQuicMethod(ref QuicMethods.Instance);

            if (cert != null)
                Ssl.UseCertificateFile(cert, SslFiletype.Pem);
            if (privateKey != null)
                Ssl.UsePrivateKeyFile(privateKey, SslFiletype.Pem);

            if (address == null)
            {
                _ssl.SetAcceptState();
            }
            else
            {
                _ssl.SetConnectState();
                _ssl.SetTlsexHostName(address);
            }
        }

        public Ssl Ssl => _ssl;

        public List<(SslEncryptionLevel, byte[])> ToSend { get; }

        public void Dispose()
        {
            var ptr = Ssl.GetCallbackInterface();
            var gcHandle = GCHandle.FromIntPtr(ptr);

            gcHandle.Free();

            Ssl.Free(Ssl);
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
            return Ssl.DoHandshake();
        }

        public int OnDataReceived(SslEncryptionLevel level, byte[] data)
        {
            return Ssl.ProvideQuicData(level, data);
        }
    }
}
