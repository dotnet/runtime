#nullable enable

using System.Buffers;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed
{
    /// <summary>
    ///     Class encapsulating TLS related logic and interop.
    /// </summary>
    internal class Tls : IDisposable
    {
        private static readonly int _managedInterfaceIndex =
            Interop.OpenSslQuic.CryptoGetExNewIndex(Interop.OpenSslQuic.CRYPTO_EX_INDEX_SSL, 0, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        private static unsafe OpenSslQuicMethods.NativeCallbacks _callbacks = new OpenSslQuicMethods.NativeCallbacks
        {
            setEncryptionSecrets =
                Marshal.GetFunctionPointerForDelegate(
                    new OpenSslQuicMethods.SetEncryptionSecretsFunc(SetEncryptionSecretsImpl)),
            addHandshakeData =
                Marshal.GetFunctionPointerForDelegate(
                    new OpenSslQuicMethods.AddHandshakeDataFunc(AddHandshakeDataImpl)),
            flushFlight =
                Marshal.GetFunctionPointerForDelegate(new OpenSslQuicMethods.FlushFlightFunc(FlushFlightImpl)),
            sendAlert = Marshal.GetFunctionPointerForDelegate(new OpenSslQuicMethods.SendAlertFunc(SendAlertImpl))
        };

        private readonly IntPtr _ssl;

        public Tls(GCHandle handle)
        {
            _ssl = Interop.OpenSslQuic.SslCreate();
            Debug.Assert(handle.Target is ManagedQuicConnection);
            Interop.OpenSslQuic.SslSetQuicMethod(_ssl, ref Callbacks);

            // add the callback as contextual data so we can retrieve it inside the callback
            Interop.OpenSslQuic.SslSetExData(_ssl, _managedInterfaceIndex, GCHandle.ToIntPtr(handle));

            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMinProtoVersion, (long)OpenSslTlsVersion.Tls13,
                IntPtr.Zero);
            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMaxProtoVersion, (long)OpenSslTlsVersion.Tls13,
                IntPtr.Zero);
        }

        private static ref OpenSslQuicMethods.NativeCallbacks Callbacks => ref _callbacks;

        internal bool IsHandshakeComplete => Interop.OpenSslQuic.SslIsInitFinished(_ssl) == 1;
        public EncryptionLevel WriteLevel { get; private set; }

        public void Dispose()
        {
            // call SslSetQuicMethod(ssl, null) to stop callbacks being called
            // Interop.OpenSslQuic.SslSetQuicMethod(ssl, ref Unsafe.AsRef<OpenSslQuicMethods.NativeCallbacks>(null));
            Interop.OpenSslQuic.SslFree(_ssl);
        }

        internal static EncryptionLevel ToManagedEncryptionLevel(OpenSslEncryptionLevel level)
        {
            return level switch
            {
                OpenSslEncryptionLevel.Initial => EncryptionLevel.Initial,
                OpenSslEncryptionLevel.EarlyData => EncryptionLevel.EarlyData,
                OpenSslEncryptionLevel.Handshake => EncryptionLevel.Handshake,
                OpenSslEncryptionLevel.Application => EncryptionLevel.Application,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
            };
        }

        private static OpenSslEncryptionLevel ToOpenSslEncryptionLevel(EncryptionLevel level)
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

        private static ManagedQuicConnection GetCallbackInterface(IntPtr ssl)
        {
            var addr = Interop.OpenSslQuic.SslGetExData(ssl, _managedInterfaceIndex);
            var callback = (ManagedQuicConnection)GCHandle.FromIntPtr(addr).Target!;

            return callback;
        }

        internal int OnDataReceived(EncryptionLevel level, ReadOnlySpan<byte> data)
        {
            return Interop.OpenSslQuic.SslProvideQuicData(_ssl, ToOpenSslEncryptionLevel(level), data);
        }

        internal unsafe void Init(string? cert, string? privateKey, bool isServer,
            TransportParameters localTransportParams)
        {
            // explicitly set allowed suites
            var ciphers = new TlsCipherSuite[]
            {
                TlsCipherSuite.TLS_AES_128_GCM_SHA256,
                TlsCipherSuite.TLS_AES_128_CCM_SHA256,
                TlsCipherSuite.TLS_AES_256_GCM_SHA384,
                // not supported yet
                // TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256
            };
            Interop.OpenSslQuic.SslSetCiphersuites(_ssl, string.Join(":", ciphers));

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
                // TODO-RZ get hostname from somewhere
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

        internal SslError DoHandshake()
        {
            if (IsHandshakeComplete)
                return SslError.None;

            int status = Interop.OpenSslQuic.SslDoHandshake(_ssl);

            // update also write level
            WriteLevel = ToManagedEncryptionLevel(Interop.OpenSslQuic.SslQuicWriteLevel(_ssl));

            if (status <= 0)
            {
                return (SslError)Interop.OpenSslQuic.SslGetError(_ssl, status);
            }

            return SslError.None;
        }

        internal TlsCipherSuite GetNegotiatedCipher()
        {
            return Interop.OpenSslQuic.SslGetCipherId(_ssl);
        }

        internal unsafe TransportParameters? GetPeerTransportParameters(bool isServer)
        {
            if (Interop.OpenSslQuic.SslGetPeerQuicTransportParams(_ssl, out byte* data, out IntPtr length) == 0 ||
                length.ToInt32() == 0)
            {
                return null;
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(length.ToInt32());

            new Span<byte>(data, length.ToInt32()).CopyTo(buffer);
            var reader = new QuicReader(new ArraySegment<byte>(buffer, 0, length.ToInt32()));

            // TODO-RZ: Failure to deserialize should prompt TRANSPORT_PARAMETER_ERROR
            TransportParameters.Read(reader, !isServer, out var parameters);
            ArrayPool<byte>.Shared.Return(buffer);

            return parameters;
        }

        private static unsafe int SetEncryptionSecretsImpl(IntPtr ssl, OpenSslEncryptionLevel level, byte* readSecret,
            byte* writeSecret, UIntPtr secretLen)
        {
            var callback = GetCallbackInterface(ssl);

            var readS = new ReadOnlySpan<byte>(readSecret, (int)secretLen.ToUInt32());
            var writeS = new ReadOnlySpan<byte>(writeSecret, (int)secretLen.ToUInt32());

            return callback.HandleSetEncryptionSecrets(ToManagedEncryptionLevel(level), readS, writeS);
        }

        private static unsafe int AddHandshakeDataImpl(IntPtr ssl, OpenSslEncryptionLevel level, byte* data,
            UIntPtr len)
        {
            var callback = GetCallbackInterface(ssl);

            var span = new ReadOnlySpan<byte>(data, (int)len.ToUInt32());

            return callback.HandleAddHandshakeData(ToManagedEncryptionLevel(level), span);
        }

        private static int FlushFlightImpl(IntPtr ssl)
        {
            var callback = GetCallbackInterface(ssl);

            return callback.HandleFlush();
        }

        private static int SendAlertImpl(IntPtr ssl, OpenSslEncryptionLevel level, byte alert)
        {
            var callback = GetCallbackInterface(ssl);

            return callback.HandleSendAlert(ToManagedEncryptionLevel(level), (TlsAlert)alert);
        }
    }
}
