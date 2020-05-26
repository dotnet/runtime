#nullable enable

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Quic.Implementations.Managed
{
    /// <summary>
    ///     Class encapsulating TLS related logic and interop.
    /// </summary>
    internal sealed class Tls : IDisposable
    {
        // keep static reference to the delegates passed to native code to prevent their deallocation
        private static readonly unsafe OpenSslQuicMethods.AddHandshakeDataFunc AddHandshakeDelegate = AddHandshakeDataImpl;
        private static readonly unsafe OpenSslQuicMethods.SetEncryptionSecretsFunc SetEncryptionSecretsDelegate = SetEncryptionSecretsImpl;
        private static readonly OpenSslQuicMethods.FlushFlightFunc FlushFlightDelegate = FlushFlightImpl;
        private static readonly OpenSslQuicMethods.SendAlertFunc SendAlertDelegate = SendAlertImpl;

        private static readonly Interop.OpenSslQuic.TlsExtServernameCallback tlsExtServernameCallbackDelegate =
            TlsExtCallbackImpl;

        private readonly X509CertificateCollection? _clientCertificateCollection;
        private readonly RemoteCertificateValidationCallback? _remoteCertificateValidationCallback;
        private readonly LocalCertificateSelectionCallback? _clientCertificateSelectionCallback;
        private readonly ServerCertificateSelectionCallback? _serverCertificateSelectionCallback;
        private readonly bool _isServer;

        private static readonly int _managedInterfaceIndex =
            Interop.OpenSslQuic.CryptoGetExNewIndex(Interop.OpenSslQuic.CRYPTO_EX_INDEX_SSL, 0, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        private static readonly TlsCipherSuite[] _supportedCiphers =
        {
            TlsCipherSuite.TLS_AES_128_GCM_SHA256,
            TlsCipherSuite.TLS_AES_128_CCM_SHA256,
            TlsCipherSuite.TLS_AES_256_GCM_SHA384,
            // not supported yet
            // TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256
        };

        private static readonly IntPtr _callbacksPtr;

        static unsafe Tls()
        {
            _callbacksPtr = Marshal.AllocHGlobal(sizeof(OpenSslQuicMethods.NativeCallbacks));
            *(OpenSslQuicMethods.NativeCallbacks*)_callbacksPtr.ToPointer() = new OpenSslQuicMethods.NativeCallbacks
            {
                setEncryptionSecrets =
                    Marshal.GetFunctionPointerForDelegate(SetEncryptionSecretsDelegate),
                addHandshakeData =
                    Marshal.GetFunctionPointerForDelegate(AddHandshakeDelegate),
                flushFlight =
                    Marshal.GetFunctionPointerForDelegate(FlushFlightDelegate),
                sendAlert = Marshal.GetFunctionPointerForDelegate(SendAlertDelegate)
            };

            // Interop.OpenSslQuic.SslCtxSetTlsExtServernameCallback(
            //     Interop.OpenSslQuic.__globalSslCtx,
            //     tlsExtServernameCallbackDelegate);
        }

        internal Tls(GCHandle handle, QuicClientConnectionOptions options, TransportParameters localTransportParams)
            : this(handle,
                false,
                localTransportParams,
                options.ClientAuthenticationOptions?.ApplicationProtocols,
                options.ClientAuthenticationOptions?.RemoteCertificateValidationCallback,
                options.ClientAuthenticationOptions?.CipherSuitesPolicy)
        {
            Interop.OpenSslQuic.SslSetConnectState(_ssl);

            if (options.ClientAuthenticationOptions?.TargetHost != null)
                Interop.OpenSslQuic.SslSetTlsExtHostName(_ssl, options.ClientAuthenticationOptions.TargetHost);

            _clientCertificateCollection = options.ClientAuthenticationOptions?.ClientCertificates;
            _clientCertificateSelectionCallback =
                options.ClientAuthenticationOptions?.LocalCertificateSelectionCallback;
        }

        internal Tls(GCHandle handle, QuicListenerOptions options, TransportParameters localTransportParameters)
            : this(handle,
                true,
                localTransportParameters,
                options.ServerAuthenticationOptions?.ApplicationProtocols,
                options.ServerAuthenticationOptions?.RemoteCertificateValidationCallback,
                options.ServerAuthenticationOptions?.CipherSuitesPolicy)
        {
            Interop.OpenSslQuic.SslSetAcceptState(_ssl);

            if (options.CertificateFilePath != null)
                Interop.OpenSslQuic.SslUseCertificateFile(_ssl, options.CertificateFilePath!, SslFiletype.Pem);

            if (options.PrivateKeyFilePath != null)
                Interop.OpenSslQuic.SslUsePrivateKeyFile(_ssl, options.PrivateKeyFilePath, SslFiletype.Pem);

            var serverCertificate = options.ServerAuthenticationOptions?.ServerCertificate;
            if (serverCertificate != null)
            {
                SetServerCertificate(serverCertificate);
            }

            _serverCertificateSelectionCallback =
                options.ServerAuthenticationOptions?.ServerCertificateSelectionCallback;
        }

        private unsafe Tls(GCHandle handle,
            bool isServer,
            TransportParameters localTransportParams,
            List<SslApplicationProtocol>? applicationProtocols,
            RemoteCertificateValidationCallback? remoteCertificateValidationCallback,
            CipherSuitesPolicy? cipherSuitesPolicy)
        {
            _remoteCertificateValidationCallback = remoteCertificateValidationCallback;
            _isServer = isServer;

            _ssl = Interop.OpenSslQuic.SslCreate();
            Debug.Assert(handle.Target is ManagedQuicConnection);
            Interop.OpenSslQuic.SslSetQuicMethod(_ssl, _callbacksPtr);

            // add the callback as contextual data so we can retrieve it inside the callback
            Interop.OpenSslQuic.SslSetExData(_ssl, _managedInterfaceIndex, GCHandle.ToIntPtr(handle));

            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMinProtoVersion, (long)OpenSslTlsVersion.Tls13,
                IntPtr.Zero);
            Interop.OpenSslQuic.SslCtrl(_ssl, SslCtrlCommand.SetMaxProtoVersion, (long)OpenSslTlsVersion.Tls13,
                IntPtr.Zero);

            // explicitly set allowed suites
            SetCiphersuites(cipherSuitesPolicy);

            // init transport parameters
            byte[] buffer = new byte[1024];
            var writer = new QuicWriter(buffer);
            TransportParameters.Write(writer, isServer, localTransportParams);
            fixed (byte* pData = buffer)
            {
                Interop.OpenSslQuic.SslSetQuicTransportParams(_ssl, pData, new IntPtr(writer.BytesWritten));
            }

            if (applicationProtocols != null)
            {
                SetAlpn(applicationProtocols);
            }
        }

        private unsafe void SetServerCertificate(X509Certificate serverCertificate)
        {
            var cert = (serverCertificate as X509Certificate2) ?? new X509Certificate2(serverCertificate.Handle);
            if (!cert.HasPrivateKey)
            {
                throw new ArgumentException("Selected server certificate does not contain a private key.");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // .NET runtime on linux uses OpenSSL, so we do not need to marshal the certificate.
                Interop.OpenSslQuic.SslUseCertificate(_ssl, cert.Handle);
                return;
            }

            var data = cert.Export(X509ContentType.Pfx);
            IntPtr pkcsHandle;

            fixed (byte* pdata = data)
            {
                var localData = pdata;
                pkcsHandle = Interop.OpenSslQuic.D2iPkcs12(IntPtr.Zero, ref localData, data.Length);
            }

            IntPtr keyHandle;
            IntPtr certHandle;
            IntPtr caStackHandle;

            try
            {
                if (pkcsHandle == IntPtr.Zero)
                {
                    throw new ArgumentException("Unable to propagate certificate to OpenSSL");
                }

                IntPtr emptyString = Marshal.StringToHGlobalAnsi("");
                Interop.OpenSslQuic.Pkcs12Parse(pkcsHandle, emptyString, out keyHandle, out certHandle,
                    out caStackHandle);
                Marshal.FreeHGlobal(emptyString);
            }
            finally
            {
                Interop.OpenSslQuic.Pkcs12Free(pkcsHandle);
            }

            try
            {
                int result = Interop.OpenSslQuic.SslUseCertAndKey(_ssl, certHandle, keyHandle, caStackHandle, 0);
                if (result == 0)
                {
                    throw new ArgumentException("Failed to set server certificate, make sure it contains private key and is marked exportable.");
                }
            }
            finally
            {
                Interop.OpenSslQuic.X509Free(certHandle);
                Interop.OpenSslQuic.EvpPKeyFree(keyHandle);

                // TODO-RZ: free the stack
                // Interop.OpenSslQuic.SkX509Free(caStackHandle);
            }
        }

        private bool ValidateClientHostname(string hostname)
        {
            if (_serverCertificateSelectionCallback != null)
            {
                Debug.Assert(_isServer);
                var cert = _serverCertificateSelectionCallback(null, hostname);
                if (cert != null)
                {
                    SetServerCertificate(cert);
                }
            }
            return true;
        }

        private void SetCiphersuites(CipherSuitesPolicy? policy)
        {
            // if no policy supplied, use default ciphers
            IEnumerable<TlsCipherSuite> ciphers = _supportedCiphers;
            if (policy != null)
            {
                ciphers = _supportedCiphers.Intersect(policy.AllowedCipherSuites);
            }

            string ciphersString = string.Join(":", ciphers);
            if (ciphersString == "")
            {
                throw new ArgumentException("No supported cipher supplied.");
            }

            Interop.OpenSslQuic.SslSetCiphersuites(_ssl, ciphersString);
        }

        private unsafe void SetAlpn(List<SslApplicationProtocol> protos)
        {
            int totalLength = 0;
            for (int i = 0; i < protos.Count; i++)
            {
                totalLength += protos[i].Protocol.Length + 1;
            }
            Span<byte> buffer = stackalloc byte[totalLength];
            int offset = 0;

            for (int i = 0; i < protos.Count; i++)
            {
                var protocol = protos[i];
                buffer[offset] = (byte)protocol.Protocol.Length;
                protocol.Protocol.Span.CopyTo(buffer.Slice(offset + 1));
                offset += 1 + protocol.Protocol.Length;
            }

            int result = Interop.OpenSslQuic.SslSetAlpnProtos(_ssl,
                new IntPtr(Unsafe.AsPointer(ref MemoryMarshal.AsRef<byte>(buffer))), buffer.Length);
            Debug.Assert(result == 0);
        }

        private readonly IntPtr _ssl;

        internal bool IsHandshakeComplete => Interop.OpenSslQuic.SslIsInitFinished(_ssl) == 1;
        public EncryptionLevel WriteLevel { get; private set; }

        public void Dispose()
        {
            // call SslSetQuicMethod(ssl, null) to stop callbacks being called
            Interop.OpenSslQuic.SslSetQuicMethod(_ssl, IntPtr.Zero);
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

        internal static ManagedQuicConnection GetCallbackInterface(IntPtr ssl)
        {
            var addr = Interop.OpenSslQuic.SslGetExData(ssl, _managedInterfaceIndex);
            var callback = (ManagedQuicConnection)GCHandle.FromIntPtr(addr).Target!;

            return callback;
        }

        internal SslError OnDataReceived(EncryptionLevel level, ReadOnlySpan<byte> data)
        {
            int status = Interop.OpenSslQuic.SslProvideQuicData(_ssl, ToOpenSslEncryptionLevel(level), data);
            if (status == 1) return SslError.None;
            return (SslError)Interop.OpenSslQuic.SslGetError(_ssl, status);
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
                // nothing received yet, use default values
                return TransportParameters.Default;
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(length.ToInt32());

            new Span<byte>(data, length.ToInt32()).CopyTo(buffer);
            var reader = new QuicReader(buffer.AsMemory(0, length.ToInt32()));

            TransportParameters.Read(reader, !isServer, out var parameters);
            ArrayPool<byte>.Shared.Return(buffer);

            return parameters;
        }

        internal SslApplicationProtocol GetAlpnProtocol()
        {
            Interop.OpenSslQuic.SslGet0AlpnSelected(_ssl, out IntPtr pString, out int length);
            if (pString != IntPtr.Zero)
            {
                return new SslApplicationProtocol(Marshal.PtrToStringAnsi(pString, length));
            }

            return default;
        }

        private static int TlsExtCallbackImpl(IntPtr ssl, ref int al, IntPtr arg)
        {
            var namePtr = Interop.OpenSslQuic.SslGetServername(ssl, 0);
            var connection = Tls.GetCallbackInterface(ssl);
            string? name = Marshal.PtrToStringAnsi(namePtr);
            if (name != null && connection.Tls.ValidateClientHostname(name!))
            {
                return 0; // SSL_TLSEXT_ERR_OK
            }
            return 3; // SSL_TLSEXT_ERR_NOACK
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
