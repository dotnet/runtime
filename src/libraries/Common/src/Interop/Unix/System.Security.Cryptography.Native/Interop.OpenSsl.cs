// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class OpenSsl
    {
        private const string DisableTlsResumeCtxSwitch = "System.Net.Security.DisableTlsResume";
        private const string DisableTlsResumeEnvironmentVariable = "DOTNET_SYSTEM_NET_SECURITY_DISABLETLSRESUME";
        private const string TlsCacheSizeCtxName = "System.Net.Security.TlsCacheSize";
        private const string TlsCacheSizeEnvironmentVariable = "DOTNET_SYSTEM_NET_SECURITY_TLSCACHESIZE";
        private const SslProtocols FakeAlpnSslProtocol = (SslProtocols)1;   // used to distinguish server sessions with ALPN
        private static readonly IdnMapping s_idnMapping = new IdnMapping();
        private static readonly ConcurrentDictionary<SslProtocols, SafeSslContextHandle> s_clientSslContexts = new ConcurrentDictionary<SslProtocols, SafeSslContextHandle>();

        #region internal methods
        internal static SafeChannelBindingHandle? QueryChannelBinding(SafeSslHandle context, ChannelBindingKind bindingType)
        {
            Debug.Assert(
                bindingType != ChannelBindingKind.Endpoint,
                "Endpoint binding should be handled by EndpointChannelBindingToken");

            SafeChannelBindingHandle? bindingHandle;
            switch (bindingType)
            {
                case ChannelBindingKind.Unique:
                    bindingHandle = new SafeChannelBindingHandle(bindingType);
                    QueryUniqueChannelBinding(context, bindingHandle);
                    break;

                default:
                    // Keeping parity with windows, we should return null in this case.
                    bindingHandle = null;
                    break;
            }

            return bindingHandle;
        }

        private static int s_cacheSize = GetCacheSize();

        private static volatile int s_disableTlsResume = -1;

        private static bool DisableTlsResume
        {
            get
            {
                int disableTlsResume = s_disableTlsResume;
                if (disableTlsResume != -1)
                {
                    return disableTlsResume != 0;
                }

                // First check for the AppContext switch, giving it priority over the environment variable.
                if (AppContext.TryGetSwitch(DisableTlsResumeCtxSwitch, out bool value))
                {
                    s_disableTlsResume = value ? 1 : 0;
                }
                else
                {
                    // AppContext switch wasn't used. Check the environment variable.
                    s_disableTlsResume =
                        Environment.GetEnvironmentVariable(DisableTlsResumeEnvironmentVariable) is string envVar &&
                        (envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase)) ? 1 : 0;
                }

                return s_disableTlsResume != 0;
            }
        }

        private static int GetCacheSize()
        {
            int cacheSize = -1;
            string? value = AppContext.GetData(TlsCacheSizeCtxName) as string ?? Environment.GetEnvironmentVariable(TlsCacheSizeEnvironmentVariable);
            try
            {
                if (value != null)
                {
                    cacheSize = int.Parse(value);
                }
            }
            catch { };

            return cacheSize;
        }

        // This is helper function to adjust requested protocols based on CipherSuitePolicy and system capability.
        private static SslProtocols CalculateEffectiveProtocols(SslAuthenticationOptions sslAuthenticationOptions)
        {
            // make sure low bit is not set since we use it in context dictionary to distinguish use with ALPN
            Debug.Assert((sslAuthenticationOptions.EnabledSslProtocols & FakeAlpnSslProtocol) == 0);
            SslProtocols protocols = sslAuthenticationOptions.EnabledSslProtocols & ~((SslProtocols)1);

            if (!Interop.Ssl.Capabilities.Tls13Supported)
            {
                if (protocols != SslProtocols.None &&
                    CipherSuitesPolicyPal.WantsTls13(protocols))
                {
                    protocols = protocols & (~SslProtocols.Tls13);
                }
            }
            else if (CipherSuitesPolicyPal.WantsTls13(protocols) &&
                CipherSuitesPolicyPal.ShouldOptOutOfTls13(sslAuthenticationOptions.CipherSuitesPolicy, sslAuthenticationOptions.EncryptionPolicy))
            {
                if (protocols == SslProtocols.None)
                {
                    // we are using default settings but cipher suites policy says that TLS 1.3
                    // is not compatible with our settings (i.e. we requested no encryption or disabled
                    // all TLS 1.3 cipher suites)
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                    protocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
#pragma warning restore SYSLIB0039
                }
                else
                {
                    // user explicitly asks for TLS 1.3 but their policy is not compatible with TLS 1.3
                    throw new SslException(
                        SR.Format(SR.net_ssl_encryptionpolicy_notsupported, sslAuthenticationOptions.EncryptionPolicy));
                }
            }

            if (CipherSuitesPolicyPal.ShouldOptOutOfLowerThanTls13(sslAuthenticationOptions.CipherSuitesPolicy, sslAuthenticationOptions.EncryptionPolicy))
            {
                if (!CipherSuitesPolicyPal.WantsTls13(protocols))
                {
                    // We cannot provide neither TLS 1.3 or non TLS 1.3, user disabled all cipher suites
                    throw new SslException(
                        SR.Format(SR.net_ssl_encryptionpolicy_notsupported, sslAuthenticationOptions.EncryptionPolicy));
                }

                protocols = SslProtocols.Tls13;
            }

            return protocols;
        }

        // This essentially wraps SSL_CTX* aka SSL_CTX_new + setting
        internal static unsafe SafeSslContextHandle AllocateSslContext(SafeFreeSslCredentials credential, SslAuthenticationOptions sslAuthenticationOptions, SslProtocols protocols, bool enableResume)
        {
            SafeX509Handle? certHandle = credential.CertHandle;
            SafeEvpPKeyHandle? certKeyHandle = credential.CertKeyHandle;

            // Always use SSLv23_method, regardless of protocols.  It supports negotiating to the highest
            // mutually supported version and can thus handle any of the set protocols, and we then use
            // SetProtocolOptions to ensure we only allow the ones requested.
            SafeSslContextHandle sslCtx = Ssl.SslCtxCreate(Ssl.SslMethods.SSLv23_method);
            try
            {
                if (sslCtx.IsInvalid)
                {
                    throw CreateSslException(SR.net_allocate_ssl_context_failed);
                }

                Ssl.SslCtxSetProtocolOptions(sslCtx, protocols);

                if (sslAuthenticationOptions.EncryptionPolicy != EncryptionPolicy.RequireEncryption)
                {
                    // Sets policy and security level
                    if (!Ssl.SetEncryptionPolicy(sslCtx, sslAuthenticationOptions.EncryptionPolicy))
                    {
                        throw new SslException(SR.Format(SR.net_ssl_encryptionpolicy_notsupported, sslAuthenticationOptions.EncryptionPolicy));
                    }
                }

                ReadOnlySpan<byte> cipherList = CipherSuitesPolicyPal.GetOpenSslCipherList(sslAuthenticationOptions.CipherSuitesPolicy, protocols, sslAuthenticationOptions.EncryptionPolicy);
                Debug.Assert(cipherList.IsEmpty || cipherList[^1] == 0);

                byte[]? cipherSuites = CipherSuitesPolicyPal.GetOpenSslCipherSuites(sslAuthenticationOptions.CipherSuitesPolicy, protocols, sslAuthenticationOptions.EncryptionPolicy);
                Debug.Assert(cipherSuites == null || (cipherSuites.Length >= 1 && cipherSuites[cipherSuites.Length - 1] == 0));

                fixed (byte* cipherListStr = cipherList)
                fixed (byte* cipherSuitesStr = cipherSuites)
                {
                    if (!Ssl.SslCtxSetCiphers(sslCtx, cipherListStr, cipherSuitesStr))
                    {
                        Crypto.ErrClearError();
                        throw new PlatformNotSupportedException(SR.Format(SR.net_ssl_encryptionpolicy_notsupported, sslAuthenticationOptions.EncryptionPolicy));
                    }
                }

                // The logic in SafeSslHandle.Disconnect is simple because we are doing a quiet
                // shutdown (we aren't negotiating for session close to enable later session
                // restoration).
                //
                // If you find yourself wanting to remove this line to enable bidirectional
                // close-notify, you'll probably need to rewrite SafeSslHandle.Disconnect().
                // https://www.openssl.org/docs/manmaster/ssl/SSL_shutdown.html
                Ssl.SslCtxSetQuietShutdown(sslCtx);

                if (enableResume)
                {
                    if (sslAuthenticationOptions.IsServer)
                    {
                        Ssl.SslCtxSetCaching(sslCtx, 1, s_cacheSize, null, null);
                    }
                    else
                    {
                        int result = Ssl.SslCtxSetCaching(sslCtx, 1, s_cacheSize, &NewSessionCallback, &RemoveSessionCallback);
                        Debug.Assert(result == 1);
                        sslCtx.EnableSessionCache();
                    }
                }
                else
                {
                    Ssl.SslCtxSetCaching(sslCtx, 0, -1, null, null);
                }

                if (sslAuthenticationOptions.IsServer && sslAuthenticationOptions.ApplicationProtocols != null && sslAuthenticationOptions.ApplicationProtocols.Count != 0)
                {
                    Interop.Ssl.SslCtxSetAlpnSelectCb(sslCtx, &AlpnServerSelectCallback, IntPtr.Zero);
                }

                bool hasCertificateAndKey =
                    certHandle != null && !certHandle.IsInvalid
                    && certKeyHandle != null && !certKeyHandle.IsInvalid;

                if (hasCertificateAndKey)
                {
                    SetSslCertificate(sslCtx, certHandle!, certKeyHandle!);
                }

                if (sslAuthenticationOptions.CertificateContext != null)
                {
                    if (sslAuthenticationOptions.CertificateContext.IntermediateCertificates.Length > 0)
                    {
                        if (!Ssl.AddExtraChainCertificates(sslCtx, sslAuthenticationOptions.CertificateContext.IntermediateCertificates))
                        {
                            throw CreateSslException(SR.net_ssl_use_cert_failed);
                        }
                    }

                    if (sslAuthenticationOptions.CertificateContext.OcspStaplingAvailable)
                    {
                        Ssl.SslCtxSetDefaultOcspCallback(sslCtx);
                    }
                }
            }
            catch
            {
                sslCtx.Dispose();
                throw;
            }

            return sslCtx;
        }

        internal static void UpdateClientCertiticate(SafeSslHandle ssl, SslAuthenticationOptions sslAuthenticationOptions)
        {
            // Disable certificate selection callback. We either got certificate or we will try to proceed without it.
            Interop.Ssl.SslSetClientCertCallback(ssl, 0);

            if (sslAuthenticationOptions.CertificateContext == null)
            {
                return;
            }

            var credential = new SafeFreeSslCredentials(sslAuthenticationOptions.CertificateContext, sslAuthenticationOptions.EnabledSslProtocols, sslAuthenticationOptions.EncryptionPolicy, sslAuthenticationOptions.IsServer);
            SafeX509Handle? certHandle = credential.CertHandle;
            SafeEvpPKeyHandle? certKeyHandle = credential.CertKeyHandle;

            Debug.Assert(certHandle != null);
            Debug.Assert(certKeyHandle != null);

            int retVal = Ssl.SslUseCertificate(ssl, certHandle);
            if (1 != retVal)
            {
                throw CreateSslException(SR.net_ssl_use_cert_failed);
            }

            retVal = Ssl.SslUsePrivateKey(ssl, certKeyHandle);
            if (1 != retVal)
            {
                throw CreateSslException(SR.net_ssl_use_private_key_failed);
            }

            if (sslAuthenticationOptions.CertificateContext.IntermediateCertificates.Length > 0)
            {
                if (!Ssl.AddExtraChainCertificates(ssl, sslAuthenticationOptions.CertificateContext.IntermediateCertificates))
                {
                    throw CreateSslException(SR.net_ssl_use_cert_failed);
                }
            }

        }

        // This essentially wraps SSL* SSL_new()
        internal static SafeSslHandle AllocateSslHandle(SafeFreeSslCredentials credential, SslAuthenticationOptions sslAuthenticationOptions)
        {
            SafeSslHandle? sslHandle = null;
            SafeSslContextHandle? sslCtxHandle = null;
            SafeSslContextHandle? newCtxHandle = null;
            SslProtocols protocols = CalculateEffectiveProtocols(sslAuthenticationOptions);
            bool hasAlpn = sslAuthenticationOptions.ApplicationProtocols != null && sslAuthenticationOptions.ApplicationProtocols.Count != 0;
            bool cacheSslContext = !DisableTlsResume && sslAuthenticationOptions.EncryptionPolicy == EncryptionPolicy.RequireEncryption && sslAuthenticationOptions.CipherSuitesPolicy == null;

            if (cacheSslContext)
            {
                if (sslAuthenticationOptions.IsClient)
                {
                    // We don't support client resume on old OpenSSL versions.
                    // We don't want to try on empty TargetName since that is our key.
                    // And we don't want to mess up with client authentication. It may be possible
                    // but it seems safe to get full new session.
                    if (!Interop.Ssl.Capabilities.Tls13Supported ||
                       string.IsNullOrEmpty(sslAuthenticationOptions.TargetHost) ||
                       sslAuthenticationOptions.CertificateContext != null ||
                        sslAuthenticationOptions.CertSelectionDelegate != null)
                    {
                        cacheSslContext = false;
                    }
                }
                else
                {
                    // Server should always have certificate
                    Debug.Assert(sslAuthenticationOptions.CertificateContext != null);
                    if (sslAuthenticationOptions.CertificateContext == null ||
                       sslAuthenticationOptions.CertificateContext.SslContexts == null)
                    {
                        cacheSslContext = false;
                    }
                }
            }

            if (cacheSslContext)
            {
                if (sslAuthenticationOptions.IsServer)
                {
                    sslAuthenticationOptions.CertificateContext!.SslContexts!.TryGetValue(protocols | (hasAlpn ? FakeAlpnSslProtocol : SslProtocols.None), out sslCtxHandle);
                }
                else
                {

                    s_clientSslContexts.TryGetValue(protocols, out sslCtxHandle);
                }
            }

            if (sslCtxHandle == null)
            {
                // We did not get SslContext from cache
                sslCtxHandle = newCtxHandle = AllocateSslContext(credential, sslAuthenticationOptions, protocols, cacheSslContext);

                if (cacheSslContext)
                {
                    bool added = sslAuthenticationOptions.IsServer ?
                                    sslAuthenticationOptions.CertificateContext!.SslContexts!.TryAdd(protocols | (SslProtocols)(hasAlpn ? 1 : 0), newCtxHandle) :
                                    s_clientSslContexts.TryAdd(protocols, newCtxHandle);
                    if (added)
                    {
                        newCtxHandle = null;
                    }
                }
            }

            GCHandle alpnHandle = default;
            try
            {
                sslHandle = SafeSslHandle.Create(sslCtxHandle, sslAuthenticationOptions.IsServer);
                Debug.Assert(sslHandle != null, "Expected non-null return value from SafeSslHandle.Create");
                if (sslHandle.IsInvalid)
                {
                    sslHandle.Dispose();
                    throw CreateSslException(SR.net_allocate_ssl_context_failed);
                }

                if (sslAuthenticationOptions.ApplicationProtocols != null && sslAuthenticationOptions.ApplicationProtocols.Count != 0)
                {
                    if (sslAuthenticationOptions.IsServer)
                    {
                        Debug.Assert(Interop.Ssl.SslGetData(sslHandle) == IntPtr.Zero);
                        alpnHandle = GCHandle.Alloc(sslAuthenticationOptions.ApplicationProtocols);
                        Interop.Ssl.SslSetData(sslHandle, GCHandle.ToIntPtr(alpnHandle));
                        sslHandle.AlpnHandle = alpnHandle;
                    }
                    else
                    {
                        if (Interop.Ssl.SslSetAlpnProtos(sslHandle, sslAuthenticationOptions.ApplicationProtocols) != 0)
                        {
                            throw CreateSslException(SR.net_alpn_config_failed);
                        }
                    }
                }

                if (sslAuthenticationOptions.IsClient)
                {
                    // The IdnMapping converts unicode input into the IDNA punycode sequence.
                    string punyCode = string.IsNullOrEmpty(sslAuthenticationOptions.TargetHost) ? string.Empty : s_idnMapping.GetAscii(sslAuthenticationOptions.TargetHost!);

                    // Similar to windows behavior, set SNI on openssl by default for client context, ignore errors.
                    if (!Ssl.SslSetTlsExtHostName(sslHandle, punyCode))
                    {
                        Crypto.ErrClearError();
                    }

                    if (cacheSslContext && !string.IsNullOrEmpty(punyCode))
                    {
                        sslCtxHandle.TrySetSession(sslHandle, punyCode);
                    }

                    // relevant to TLS 1.3 only: if user supplied a client cert or cert callback,
                    // advertise that we are willing to send the certificate post-handshake.
                    if (sslAuthenticationOptions.ClientCertificates?.Count > 0 ||
                        sslAuthenticationOptions.CertSelectionDelegate != null)
                    {
                        Ssl.SslSetPostHandshakeAuth(sslHandle, 1);
                    }

                    // Set client cert callback, this will interrupt the handshake with SecurityStatusPalErrorCode.CredentialsNeeded
                    // if server actually requests a certificate.
                    Ssl.SslSetClientCertCallback(sslHandle, 1);
                }
                else // sslAuthenticationOptions.IsServer
                {
                    if (sslAuthenticationOptions.RemoteCertRequired)
                    {
                        Ssl.SslSetVerifyPeer(sslHandle);
                    }

                    if (sslAuthenticationOptions.CertificateContext != null)
                    {
                        if (sslAuthenticationOptions.CertificateContext.Trust?._sendTrustInHandshake == true)
                        {
                            SslCertificateTrust trust = sslAuthenticationOptions.CertificateContext!.Trust!;
                            X509Certificate2Collection certList = (trust._trustList ?? trust._store!.Certificates);

                            Debug.Assert(certList != null, "certList != null");
                            Span<IntPtr> handles = certList.Count <= 256 ?
                                stackalloc IntPtr[256] :
                                new IntPtr[certList.Count];

                            for (int i = 0; i < certList.Count; i++)
                            {
                                handles[i] = certList[i].Handle;
                            }

                            if (!Ssl.SslAddClientCAs(sslHandle, handles.Slice(0, certList.Count)))
                            {
                                // The method can fail only when the number of cert names exceeds the maximum capacity
                                // supported by STACK_OF(X509_NAME) structure, which should not happen under normal
                                // operation.
                                Debug.Fail("Failed to add issuer to trusted CA list.");
                            }
                        }

                        byte[]? ocspResponse = sslAuthenticationOptions.CertificateContext.GetOcspResponseNoWaiting();

                        if (ocspResponse != null)
                        {
                            Ssl.SslStapleOcsp(sslHandle, ocspResponse);
                        }
                    }
                }
            }
            catch
            {
                if (alpnHandle.IsAllocated)
                {
                    alpnHandle.Free();
                }

                throw;
            }
            finally
            {
                newCtxHandle?.Dispose();
            }

            return sslHandle;
        }

        internal static SecurityStatusPal SslRenegotiate(SafeSslHandle sslContext, out byte[]? outputBuffer)
        {
            int ret = Interop.Ssl.SslRenegotiate(sslContext, out Ssl.SslErrorCode errorCode);

            outputBuffer = Array.Empty<byte>();
            if (ret != 1)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, GetSslError(ret, errorCode));
            }
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }

        internal static SecurityStatusPalErrorCode DoSslHandshake(SafeSslHandle context, ReadOnlySpan<byte> input, out byte[]? sendBuf, out int sendCount)
        {
            sendBuf = null;
            sendCount = 0;
            Exception? handshakeException = null;

            if (input.Length > 0)
            {
                if (Ssl.BioWrite(context.InputBio!, ref MemoryMarshal.GetReference(input), input.Length) != input.Length)
                {
                    // Make sure we clear out the error that is stored in the queue
                    throw Crypto.CreateOpenSslCryptographicException();
                }
            }

            int retVal = Ssl.SslDoHandshake(context, out Ssl.SslErrorCode errorCode);
            if (retVal != 1)
            {
                if (errorCode == Ssl.SslErrorCode.SSL_ERROR_WANT_X509_LOOKUP)
                {
                    return SecurityStatusPalErrorCode.CredentialsNeeded;
                }

                if ((retVal != -1) || (errorCode != Ssl.SslErrorCode.SSL_ERROR_WANT_READ))
                {
                    Exception? innerError = GetSslError(retVal, errorCode);

                    // Handshake failed, but even if the handshake does not need to read, there may be an Alert going out.
                    // To handle that we will fall-through the block below to pull it out, and we will fail after.
                    handshakeException = new SslException(SR.Format(SR.net_ssl_handshake_failed_error, errorCode), innerError);
                }
            }

            sendCount = Crypto.BioCtrlPending(context.OutputBio!);
            if (sendCount > 0)
            {
                sendBuf = new byte[sendCount];

                try
                {
                    sendCount = BioRead(context.OutputBio!, sendBuf, sendCount);
                }
                catch (Exception) when (handshakeException != null)
                {
                    // If we already have handshake exception, ignore any exception from BioRead().
                }
                finally
                {
                    if (sendCount <= 0)
                    {
                        // Make sure we clear out the error that is stored in the queue
                        Crypto.ErrClearError();
                        sendBuf = null;
                        sendCount = 0;
                    }
                }
            }

            if (handshakeException != null)
            {
                throw handshakeException;
            }

            bool stateOk = Ssl.IsSslStateOK(context);
            if (stateOk)
            {
                context.MarkHandshakeCompleted();
            }

            return stateOk ? SecurityStatusPalErrorCode.OK : SecurityStatusPalErrorCode.ContinueNeeded;
        }

        internal static int Encrypt(SafeSslHandle context, ReadOnlySpan<byte> input, ref byte[] output, out Ssl.SslErrorCode errorCode)
        {
            int retVal = Ssl.SslWrite(context, ref MemoryMarshal.GetReference(input), input.Length, out errorCode);

            if (retVal != input.Length)
            {
                retVal = 0;

                switch (errorCode)
                {
                    // indicate end-of-file
                    case Ssl.SslErrorCode.SSL_ERROR_ZERO_RETURN:
                    case Ssl.SslErrorCode.SSL_ERROR_WANT_READ:
                        break;

                    default:
                        throw new SslException(SR.Format(SR.net_ssl_encrypt_failed, errorCode), GetSslError(retVal, errorCode));
                }
            }
            else
            {
                int capacityNeeded = Crypto.BioCtrlPending(context.OutputBio!);

                if (output == null || output.Length < capacityNeeded)
                {
                    output = new byte[capacityNeeded];
                }

                retVal = BioRead(context.OutputBio!, output, capacityNeeded);

                if (retVal <= 0)
                {
                    // Make sure we clear out the error that is stored in the queue
                    Crypto.ErrClearError();
                }
            }

            return retVal;
        }

        internal static int Decrypt(SafeSslHandle context, Span<byte> buffer, out Ssl.SslErrorCode errorCode)
        {
            BioWrite(context.InputBio!, buffer);

            int retVal = Ssl.SslRead(context, ref MemoryMarshal.GetReference(buffer), buffer.Length, out errorCode);
            if (retVal > 0)
            {
                return retVal;
            }

            switch (errorCode)
            {
                // indicate end-of-file
                case Ssl.SslErrorCode.SSL_ERROR_ZERO_RETURN:
                    break;

                case Ssl.SslErrorCode.SSL_ERROR_WANT_READ:
                    // update error code to renegotiate if renegotiate is pending, otherwise make it SSL_ERROR_WANT_READ
                    errorCode = Ssl.IsSslRenegotiatePending(context)
                        ? Ssl.SslErrorCode.SSL_ERROR_RENEGOTIATE
                        : Ssl.SslErrorCode.SSL_ERROR_WANT_READ;
                    break;

                case Ssl.SslErrorCode.SSL_ERROR_WANT_X509_LOOKUP:
                    // This happens in TLS 1.3 when server requests post-handshake authentication
                    // but no certificate is provided by client. We can process it the same way as
                    // renegotiation on older TLS versions
                    errorCode = Ssl.SslErrorCode.SSL_ERROR_RENEGOTIATE;
                    break;

                default:
                    throw new SslException(SR.Format(SR.net_ssl_decrypt_failed, errorCode), GetSslError(retVal, errorCode));
            }

            return 0;
        }

        internal static SafeX509Handle GetPeerCertificate(SafeSslHandle context)
        {
            return Ssl.SslGetPeerCertificate(context);
        }

        internal static SafeSharedX509StackHandle GetPeerCertificateChain(SafeSslHandle context)
        {
            return Ssl.SslGetPeerCertChain(context);
        }

        #endregion

        #region private methods

        private static void QueryUniqueChannelBinding(SafeSslHandle context, SafeChannelBindingHandle bindingHandle)
        {
            bool sessionReused = Ssl.SslSessionReused(context);
            int certHashLength = context.IsServer ^ sessionReused ?
                                 Ssl.SslGetPeerFinished(context, bindingHandle.CertHashPtr, bindingHandle.Length) :
                                 Ssl.SslGetFinished(context, bindingHandle.CertHashPtr, bindingHandle.Length);

            if (0 == certHashLength)
            {
                throw CreateSslException(SR.net_ssl_get_channel_binding_token_failed);
            }

            bindingHandle.SetCertHashLength(certHashLength);
        }

        [UnmanagedCallersOnly]
        private static int VerifyClientCertificate(int preverify_ok, IntPtr x509_ctx_ptr)
        {
            // Full validation is handled after the handshake in VerifyCertificateProperties and the
            // user callback.  It's also up to those handlers to decide if a null certificate
            // is appropriate.  So just return success to tell OpenSSL that the cert is acceptable,
            // we'll process it after the handshake finishes.
            const int OpenSslSuccess = 1;
            return OpenSslSuccess;
        }

        [UnmanagedCallersOnly]
        private static unsafe int AlpnServerSelectCallback(IntPtr ssl, byte** outp, byte* outlen, byte* inp, uint inlen, IntPtr arg)
        {
            *outp = null;
            *outlen = 0;
            IntPtr sslData = Ssl.SslGetData(ssl);

            // reset application data to avoid dangling pointer.
            Ssl.SslSetData(ssl, IntPtr.Zero);

            GCHandle protocolHandle = GCHandle.FromIntPtr(sslData);
            if (!(protocolHandle.Target is List<SslApplicationProtocol> protocolList))
            {
                return Ssl.SSL_TLSEXT_ERR_ALERT_FATAL;
            }

            try
            {
                for (int i = 0; i < protocolList.Count; i++)
                {
                    var clientList = new Span<byte>(inp, (int)inlen);
                    while (clientList.Length > 0)
                    {
                        byte length = clientList[0];
                        Span<byte> clientProto = clientList.Slice(1, length);
                        if (clientProto.SequenceEqual(protocolList[i].Protocol.Span))
                        {
                            fixed (byte* p = &MemoryMarshal.GetReference(clientProto)) *outp = p;
                            *outlen = length;
                            return Ssl.SSL_TLSEXT_ERR_OK;
                        }

                        clientList = clientList.Slice(1 + length);
                    }
                }
            }
            catch
            {
                // No common application protocol was negotiated, set the target on the alpnHandle to null.
                // It is ok to clear the handle value here, this results in handshake failure, so the SslStream object is disposed.
                protocolHandle.Target = null;

                return Ssl.SSL_TLSEXT_ERR_ALERT_FATAL;
            }

            // No common application protocol was negotiated, set the target on the alpnHandle to null.
            // It is ok to clear the handle value here, this results in handshake failure, so the SslStream object is disposed.
            protocolHandle.Target = null;

            return Ssl.SSL_TLSEXT_ERR_ALERT_FATAL;
        }

        [UnmanagedCallersOnly]
        // Invoked from OpenSSL when new session is created.
        // We attached GCHandle to the SSL so we can find back SafeSslContextHandle holding the cache.
        // New session has refCount of 1.
        // If this function returns 0, OpenSSL will drop the refCount and discard the session.
        // If we return 1, the ownership is transfered to us and we will need to call SessionFree().
        private static unsafe int NewSessionCallback(IntPtr ssl, IntPtr session)
        {
            Debug.Assert(ssl != IntPtr.Zero);
            Debug.Assert(session != IntPtr.Zero);

            IntPtr ptr = Ssl.SslGetData(ssl);
            Debug.Assert(ptr != IntPtr.Zero);
            GCHandle gch = GCHandle.FromIntPtr(ptr);

            SafeSslContextHandle? ctxHandle = gch.Target as SafeSslContextHandle;
            // There is no relation between SafeSslContextHandle and SafeSslHandle so the handle
            // may be released while the ssl session is still active.
            if (ctxHandle != null && ctxHandle.TryAddSession(Ssl.SslGetServerName(ssl), session))
            {
                // offered session was stored in our cache.
                return 1;
            }

            // OpenSSL will destroy session.
            return 0;
        }

        [UnmanagedCallersOnly]
        private static unsafe void RemoveSessionCallback(IntPtr ctx, IntPtr session)
        {
            Debug.Assert(ctx != IntPtr.Zero && session != IntPtr.Zero);

            IntPtr ptr = Ssl.SslCtxGetData(ctx);
            if (ptr == IntPtr.Zero)
            {
                // Same as above, SafeSslContextHandle could be released while OpenSSL still holds reference.
                return;
            }

            GCHandle gch = GCHandle.FromIntPtr(ptr);
            SafeSslContextHandle? ctxHandle = gch.Target as SafeSslContextHandle;
            if (ctxHandle == null)
            {
                return;
            }

            IntPtr name = Ssl.SessionGetHostname(session);
            Debug.Assert(name != IntPtr.Zero);
            ctxHandle.RemoveSession(name, session);
        }

        private static int BioRead(SafeBioHandle bio, byte[] buffer, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(count >= 0);
            Debug.Assert(buffer.Length >= count);

            int bytes = Crypto.BioRead(bio, buffer, count);
            if (bytes != count)
            {
                throw CreateSslException(SR.net_ssl_read_bio_failed_error);
            }
            return bytes;
        }

        private static void BioWrite(SafeBioHandle bio, ReadOnlySpan<byte> buffer)
        {
            int bytes = Ssl.BioWrite(bio, ref MemoryMarshal.GetReference(buffer), buffer.Length);
            if (bytes != buffer.Length)
            {
                throw CreateSslException(SR.net_ssl_write_bio_failed_error);
            }
        }

        private static Exception? GetSslError(int result, Ssl.SslErrorCode retVal)
        {
            Exception? innerError;
            switch (retVal)
            {
                case Ssl.SslErrorCode.SSL_ERROR_SYSCALL:
                    ErrorInfo lastErrno = Sys.GetLastErrorInfo();
                    // Some I/O error occurred
                    innerError =
                        Crypto.ErrPeekError() != 0 ? Crypto.CreateOpenSslCryptographicException() : // crypto error queue not empty
                        result == 0 ? new EndOfStreamException() : // end of file that violates protocol
                        result == -1 && lastErrno.Error != Error.SUCCESS ? new IOException(lastErrno.GetErrorMessage(), lastErrno.RawErrno) : // underlying I/O error
                        null; // no additional info available
                    break;

                case Ssl.SslErrorCode.SSL_ERROR_SSL:
                    // OpenSSL failure occurred.  The error queue contains more details, when building the exception the queue will be cleared.
                    innerError = Interop.Crypto.CreateOpenSslCryptographicException();
                    break;

                default:
                    // No additional info available.
                    innerError = null;
                    break;
            }

            return innerError;
        }

        private static void SetSslCertificate(SafeSslContextHandle contextPtr, SafeX509Handle certPtr, SafeEvpPKeyHandle keyPtr)
        {
            Debug.Assert(certPtr != null && !certPtr.IsInvalid, "certPtr != null && !certPtr.IsInvalid");
            Debug.Assert(keyPtr != null && !keyPtr.IsInvalid, "keyPtr != null && !keyPtr.IsInvalid");

            int retVal = Ssl.SslCtxUseCertificate(contextPtr, certPtr);

            if (1 != retVal)
            {
                throw CreateSslException(SR.net_ssl_use_cert_failed);
            }

            retVal = Ssl.SslCtxUsePrivateKey(contextPtr, keyPtr);

            if (1 != retVal)
            {
                throw CreateSslException(SR.net_ssl_use_private_key_failed);
            }

            //check private key
            retVal = Ssl.SslCtxCheckPrivateKey(contextPtr);

            if (1 != retVal)
            {
                throw CreateSslException(SR.net_ssl_check_private_key_failed);
            }
        }

        internal static SslException CreateSslException(string message)
        {
            // Capture last error to be consistent with CreateOpenSslCryptographicException
            ulong errorVal = Crypto.ErrPeekLastError();
            Crypto.ErrClearError();
            string msg = SR.Format(message, Marshal.PtrToStringAnsi(Crypto.ErrReasonErrorString(errorVal)));
            return new SslException(msg, (int)errorVal);
        }

        #endregion

        #region Internal class

        internal sealed class SslException : Exception
        {
            public SslException(string? inputMessage)
                : base(inputMessage)
            {
            }

            public SslException(string? inputMessage, Exception? ex)
                : base(inputMessage, ex)
            {
            }

            public SslException(string? inputMessage, int error)
                : this(inputMessage)
            {
                HResult = error;
            }

            public SslException(int error)
                : this(SR.Format(SR.net_generic_operation_failed, error))
            {
                HResult = error;
            }
        }

        #endregion
    }
}
