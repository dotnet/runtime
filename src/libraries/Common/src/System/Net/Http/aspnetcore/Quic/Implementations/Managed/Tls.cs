#nullable enable

using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed
{
    /// <summary>
    ///     Class encapsulating TLS related logic and interop.
    /// </summary>
    internal class Tls : IDisposable
    {
        private readonly IntPtr _ssl;
        private TransportParameters? _remoteTransportParams;

        public Tls()
        {
            _ssl = Interop.OpenSslQuic.SslCreate();
        }

        internal int OnDataReceived(EncryptionLevel level, ReadOnlySpan<byte> data)
        {
            return Interop.OpenSslQuic.SslProvideQuicData(_ssl, GetOsslEncryptionLevel(level), data);
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

        internal unsafe void Init(GCHandle handle, string? cert, string? privateKey, bool isServer, TransportParameters localTransportParams)
        {
            QuicMethods.InitCallbacks(handle, _ssl);

            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMinProtoVersion, (long)OpenSslTlsVersion.Tls13,
                IntPtr.Zero);
            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMaxProtoVersion, (long)OpenSslTlsVersion.Tls13,
                IntPtr.Zero);

            if (cert != null)
                Interop.OpenSslQuic.SslUseCertificateFile(_ssl, cert, SslFiletype.Pem);
            if (privateKey != null)
                Interop.OpenSslQuic.SslUsePrivateKeyFile(_ssl, privateKey, SslFiletype.Pem);

            if (isServer)
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
            TransportParameters.Write(writer, isServer, localTransportParams);
            fixed (byte* pData = buffer)
            {
                // TODO-RZ: check return value == 1
                Interop.OpenSslQuic.SslSetQuicTransportParams(_ssl, pData, new IntPtr(writer.BytesWritten));
            }
        }

        internal EncryptionLevel GetWriteLevel()
        {
            return QuicMethods.ToManagedEncryptionLevel(Interop.OpenSslQuic.SslQuicWriteLevel(_ssl));
        }

        internal SslError DoHandshake()
        {
            int status = Interop.OpenSslQuic.SslDoHandshake(_ssl);
            if (status < 0)
            {
                return (SslError)Interop.OpenSslQuic.SslGetError(_ssl, status);
            }

            return SslError.None;
        }

        internal unsafe TransportParameters GetPeerTransportParameters(bool isServer)
        {
            if (_remoteTransportParams == null)
            {
                byte[] buffer = new byte[1024];
                byte* data;
                IntPtr length;
                Interop.OpenSslQuic.SslGetPeerQuicTransportParams(_ssl, out data, out length);

                new Span<byte>(data, length.ToInt32()).CopyTo(buffer);
                var reader = new QuicReader(new ArraySegment<byte>(buffer, 0, length.ToInt32()));
                if (!TransportParameters.Read(reader, !isServer, out _remoteTransportParams))
                {
                    throw new InvalidOperationException("Failed to get peers transport params");
                }
            }

            return _remoteTransportParams!;
        }

        internal bool IsHandshakeFinishhed => Interop.OpenSslQuic.SslIsInInit(_ssl) == 0;
        public void Dispose()
        {
            QuicMethods.DeinitCallbacks(_ssl);
            Interop.OpenSslQuic.SslFree(_ssl);
        }
    }
}
