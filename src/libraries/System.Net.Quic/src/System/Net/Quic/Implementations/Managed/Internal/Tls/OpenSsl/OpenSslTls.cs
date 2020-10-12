// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Quic.Implementations.Managed.Internal.Tls.OpenSsl
{
    /// <summary>
    ///     Class encapsulating TLS related logic and interop.
    /// </summary>
    internal sealed class OpenSslTls : ITls
    {
        // keep static reference to the delegates passed to native code to prevent their deallocation
        private static readonly IntPtr _sslCtxGlobal = Interop.OpenSslQuic.SslCtxNew(Interop.OpenSslQuic.TlsMethod());
        private static readonly unsafe Interop.OpenSslQuic.AlpnSelectCb AlpnSelectCbDelegate = AlpnSelectCbImpl;
        private static readonly unsafe OpenSslQuicMethods.AddHandshakeDataFunc AddHandshakeDelegate = AddHandshakeDataImpl;
        private static readonly unsafe OpenSslQuicMethods.SetEncryptionSecretsFunc SetEncryptionSecretsDelegate = SetEncryptionSecretsImpl;
        private static readonly OpenSslQuicMethods.FlushFlightFunc FlushFlightDelegate = FlushFlightImpl;
        private static readonly OpenSslQuicMethods.SendAlertFunc SendAlertDelegate = SendAlertImpl;

        // private static readonly Interop.OpenSslQuic.TlsExtServernameCallback tlsExtServernameCallbackDelegate =
        //     TlsExtCallbackImpl;

        private readonly X509CertificateCollection? _clientCertificateCollection;
        private readonly ManagedQuicConnection _connection;
        private readonly RemoteCertificateValidationCallback? _remoteCertificateValidationCallback;
        private readonly LocalCertificateSelectionCallback? _clientCertificateSelectionCallback;
        private readonly ServerCertificateSelectionCallback? _serverCertificateSelectionCallback;
        private readonly bool _isServer;

        private readonly List<SslApplicationProtocol> _alpnProtocols;

        /// <summary>
        ///     GCHandle for the connection object.
        /// </summary>
        private GCHandle _gcHandle;

        private static readonly int _tlsInstanceIndex =
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

        // this initialization actually cannot be done in a field initializer
#pragma warning disable CA1810
        static unsafe OpenSslTls()
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

            Interop.OpenSslQuic.SslCtxSetAlpnSelectCb(_sslCtxGlobal,
                Marshal.GetFunctionPointerForDelegate(AlpnSelectCbDelegate), IntPtr.Zero);

            // Interop.OpenSslQuic.SslCtxSetTlsExtServernameCallback(
            //     Interop.OpenSslQuic.__globalSslCtx,
            //     tlsExtServernameCallbackDelegate);
        }
#pragma warning restore CA1810

        internal OpenSslTls(ManagedQuicConnection connection, QuicClientConnectionOptions options, TransportParameters localTransportParams)
            : this(connection,
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

        internal OpenSslTls(ManagedQuicConnection connection, QuicListenerOptions options, TransportParameters localTransportParameters)
            : this(connection,
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

        private unsafe OpenSslTls(ManagedQuicConnection connection,
            bool isServer,
            TransportParameters localTransportParams,
            List<SslApplicationProtocol>? applicationProtocols,
            RemoteCertificateValidationCallback? remoteCertificateValidationCallback,
            CipherSuitesPolicy? cipherSuitesPolicy)
        {
            _gcHandle = GCHandle.Alloc(this);
            _connection = connection;
            _remoteCertificateValidationCallback = remoteCertificateValidationCallback;
            _isServer = isServer;

            _ssl = Interop.OpenSslQuic.SslNew(_sslCtxGlobal);
            Interop.OpenSslQuic.SslSetQuicMethod(_ssl, _callbacksPtr);

            // add the current instance as contextual data so we can retrieve it inside the callback
            Interop.OpenSslQuic.SslSetExData(_ssl, _tlsInstanceIndex, GCHandle.ToIntPtr(_gcHandle));

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

            if (applicationProtocols == null)
            {
                throw new ArgumentNullException(nameof(SslServerAuthenticationOptions.ApplicationProtocols));
            }

            _alpnProtocols = applicationProtocols;
            SetAlpn(applicationProtocols);
        }

        private unsafe void SetServerCertificate(X509Certificate serverCertificate)
        {
            var cert = (serverCertificate as X509Certificate2) ?? new X509Certificate2(serverCertificate.Handle);
            if (!cert.HasPrivateKey)
            {
                throw new ArgumentException("Selected server certificate does not contain a private key.");
            }

            // TODO-RZ: Find out why I can't use RuntimeInformation when building inside .NET Runtime
#if FEATURE_QUIC_STANDALONE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // .NET runtime on linux uses OpenSSL, so we do not need to marshal the certificate.
                Interop.OpenSslQuic.SslUseCertificate(_ssl, cert.Handle);
                return;
            }
#endif

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
                var cert = _serverCertificateSelectionCallback(this, hostname);
                if (cert != null)
                {
                    SetServerCertificate(cert);
                }
            }
            return true;
        }

        private void SetCiphersuites(CipherSuitesPolicy? policy)
        {
            // if no policy supplied, use all supported ciphers
            IEnumerable<TlsCipherSuite> ciphers = _supportedCiphers;

            if (policy != null)
            {
                // do a set intersection with supported ciphers
                List<TlsCipherSuite> filteredCiphers = new List<TlsCipherSuite>();

                foreach (var cipher in policy.AllowedCipherSuites)
                {
                    foreach (var supportedCipher in _supportedCiphers)
                    {
                        if (cipher == supportedCipher)
                        {
                            filteredCiphers.Add(cipher);
                            break;
                        }
                    }
                }

                ciphers = filteredCiphers;
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

        public bool IsHandshakeComplete => Interop.OpenSslQuic.SslIsInitFinished(_ssl) == 1;
        public EncryptionLevel WriteLevel { get; private set; }

        public void Dispose()
        {
            Interop.OpenSslQuic.SslFree(_ssl);
            _gcHandle.Free();
        }

        private static EncryptionLevel ToManagedEncryptionLevel(OpenSslEncryptionLevel level)
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

        private static OpenSslTls GetTlsInstance(IntPtr ssl)
        {
            var ptr = Interop.OpenSslQuic.SslGetExData(ssl, _tlsInstanceIndex);
            var tls = (OpenSslTls)GCHandle.FromIntPtr(ptr).Target!;

            return tls;
        }

        public void OnHandshakeDataReceived(EncryptionLevel level, ReadOnlySpan<byte> data)
        {
            // TODO-RZ: utilize the return status code
            // if (status == 1)
            // var error = (SslError)Interop.OpenSslQuic.SslGetError(_ssl, status);
            Interop.OpenSslQuic.SslProvideQuicData(_ssl, ToOpenSslEncryptionLevel(level), data);
        }

        public bool TryAdvanceHandshake()
        {
            if (IsHandshakeComplete)
                return true;

            int status = Interop.OpenSslQuic.SslDoHandshake(_ssl);

            // update also write level
            WriteLevel = ToManagedEncryptionLevel(Interop.OpenSslQuic.SslQuicWriteLevel(_ssl));

            if (status <= 0)
            {
                var error = (SslError)Interop.OpenSslQuic.SslGetError(_ssl, status);
                return error != SslError.Ssl;
            }

            return true;
        }

        public TlsCipherSuite GetNegotiatedCipher()
        {
            return Interop.OpenSslQuic.SslGetCipherId(_ssl);
        }

        public unsafe TransportParameters? GetPeerTransportParameters(bool isServer)
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

        public SslApplicationProtocol GetNegotiatedProtocol()
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
            var tls = GetTlsInstance(ssl);
            string? name = Marshal.PtrToStringAnsi(namePtr);
            if (name != null && tls.ValidateClientHostname(name!))
            {
                return 0; // SSL_TLSEXT_ERR_OK
            }
            return 3; // SSL_TLSEXT_ERR_NOACK
        }

        private static unsafe int SetEncryptionSecretsImpl(IntPtr ssl, OpenSslEncryptionLevel level, byte* readSecret,
            byte* writeSecret, UIntPtr secretLen)
        {
            var tls = GetTlsInstance(ssl);

            var readS = new ReadOnlySpan<byte>(readSecret, (int)secretLen.ToUInt32());
            var writeS = new ReadOnlySpan<byte>(writeSecret, (int)secretLen.ToUInt32());

            return tls._connection.SetEncryptionSecrets(ToManagedEncryptionLevel(level), readS, writeS);
        }

        private static unsafe int AddHandshakeDataImpl(IntPtr ssl, OpenSslEncryptionLevel level, byte* data,
            UIntPtr len)
        {
            var tls = GetTlsInstance(ssl);

            var span = new ReadOnlySpan<byte>(data, (int)len.ToUInt32());

            return tls._connection.AddHandshakeData(ToManagedEncryptionLevel(level), span);
        }

        private static int FlushFlightImpl(IntPtr ssl)
        {
            var tls = GetTlsInstance(ssl);

            return tls._connection.FlushHandshakeData();
        }

        private static int SendAlertImpl(IntPtr ssl, OpenSslEncryptionLevel level, byte alert)
        {
            var tls = GetTlsInstance(ssl);

            return tls._connection.SendTlsAlert(ToManagedEncryptionLevel(level), alert);
        }

        private static unsafe int AlpnSelectCbImpl(IntPtr ssl, ref byte* pOut, ref byte outLen, byte* pIn, int inLen, IntPtr arg)
        {
            var tls = GetTlsInstance(ssl);
            var localProtocols = tls._alpnProtocols;

            var remoteProtocolList = MemoryMarshal.CreateSpan(ref *pIn, inLen);

            // remoteProtocols are length-prefixed, non-null-terminated byte strings
            while (remoteProtocolList.Length > 0)
            {
                int length = remoteProtocolList[0];
                if (length > remoteProtocolList.Length - 1)
                {
                    // this should not really happen, this means that the protocols are in wrong format
                    return Interop.OpenSslQuic.SSL_TLSEXT_ERR_NOACK;
                }

                Span<byte> remoteProtocol = remoteProtocolList.Slice(1, length);

                for (int i = 0; i < localProtocols.Count; i++)
                {
                    if (localProtocols[i].Protocol.Span.SequenceEqual(remoteProtocol))
                    {
                        // accept the protocol
                        outLen = (byte) length;
                        pOut = (byte*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(remoteProtocol));

                        return Interop.OpenSslQuic.SSL_TLSEXT_ERR_OK;
                    }
                }

                remoteProtocolList = remoteProtocolList.Slice(length + 1);
            }

            // no protocol negotiated
            return Interop.OpenSslQuic.SSL_TLSEXT_ERR_NOACK;
        }
    }
}
