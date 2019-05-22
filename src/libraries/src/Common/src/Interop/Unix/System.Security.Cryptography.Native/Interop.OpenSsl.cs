// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.CompilerServices;
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
        private static readonly Ssl.SslCtxSetVerifyCallback s_verifyClientCertificate = VerifyClientCertificate;
        private unsafe static readonly Ssl.SslCtxSetAlpnCallback s_alpnServerCallback = AlpnServerSelectCallback;
        private static readonly IdnMapping s_idnMapping = new IdnMapping();

        #region internal methods
        internal static SafeChannelBindingHandle QueryChannelBinding(SafeSslHandle context, ChannelBindingKind bindingType)
        {
            Debug.Assert(
                bindingType != ChannelBindingKind.Endpoint,
                "Endpoint binding should be handled by EndpointChannelBindingToken");

            SafeChannelBindingHandle bindingHandle;
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

        internal static SafeSslHandle AllocateSslContext(SslProtocols protocols, SafeX509Handle certHandle, SafeEvpPKeyHandle certKeyHandle, EncryptionPolicy policy, SslAuthenticationOptions sslAuthenticationOptions)
        {
            SafeSslHandle context = null;

            // Always use SSLv23_method, regardless of protocols.  It supports negotiating to the highest
            // mutually supported version and can thus handle any of the set protocols, and we then use
            // SetProtocolOptions to ensure we only allow the ones requested.
            using (SafeSslContextHandle innerContext = Ssl.SslCtxCreate(Ssl.SslMethods.SSLv23_method))
            {
                if (innerContext.IsInvalid)
                {
                    throw CreateSslException(SR.net_allocate_ssl_context_failed);
                }

                if (!Interop.Ssl.Tls13Supported)
                {
                    if (protocols != SslProtocols.None &&
                        CipherSuitesPolicyPal.WantsTls13(protocols))
                    {
                        protocols = protocols & (~SslProtocols.Tls13);
                    }
                }
                else if (CipherSuitesPolicyPal.WantsTls13(protocols) &&
                    CipherSuitesPolicyPal.ShouldOptOutOfTls13(sslAuthenticationOptions.CipherSuitesPolicy, policy))
                {
                    if (protocols == SslProtocols.None)
                    {
                        // we are using default settings but cipher suites policy says that TLS 1.3
                        // is not compatible with our settings (i.e. we requested no encryption or disabled
                        // all TLS 1.3 cipher suites)
                        protocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
                    }
                    else
                    {
                        // user explicitly asks for TLS 1.3 but their policy is not compatible with TLS 1.3
                        throw new SslException(
                            SR.Format(SR.net_ssl_encryptionpolicy_notsupported, policy));
                    }
                }

                if (CipherSuitesPolicyPal.ShouldOptOutOfLowerThanTls13(sslAuthenticationOptions.CipherSuitesPolicy, policy))
                {
                    if (!CipherSuitesPolicyPal.WantsTls13(protocols))
                    {
                        // We cannot provide neither TLS 1.3 or non TLS 1.3, user disabled all cipher suites
                        throw new SslException(
                            SR.Format(SR.net_ssl_encryptionpolicy_notsupported, policy));
                    }

                    protocols = SslProtocols.Tls13;
                }

                // Configure allowed protocols. It's ok to use DangerousGetHandle here without AddRef/Release as we just
                // create the handle, it's rooted by the using, no one else has a reference to it, etc.
                Ssl.SetProtocolOptions(innerContext.DangerousGetHandle(), protocols);

                // Sets policy and security level
                if (!Ssl.SetEncryptionPolicy(innerContext, policy))
                {
                    throw new SslException(
                        SR.Format(SR.net_ssl_encryptionpolicy_notsupported, policy));
                }

                // The logic in SafeSslHandle.Disconnect is simple because we are doing a quiet
                // shutdown (we aren't negotiating for session close to enable later session
                // restoration).
                //
                // If you find yourself wanting to remove this line to enable bidirectional
                // close-notify, you'll probably need to rewrite SafeSslHandle.Disconnect().
                // https://www.openssl.org/docs/manmaster/ssl/SSL_shutdown.html
                Ssl.SslCtxSetQuietShutdown(innerContext);

                byte[] cipherList =
                    CipherSuitesPolicyPal.GetOpenSslCipherList(sslAuthenticationOptions.CipherSuitesPolicy, protocols, policy);

                Debug.Assert(cipherList == null || (cipherList.Length >= 1 && cipherList[cipherList.Length - 1] == 0));

                byte[] cipherSuites =
                    CipherSuitesPolicyPal.GetOpenSslCipherSuites(sslAuthenticationOptions.CipherSuitesPolicy, protocols, policy);

                Debug.Assert(cipherSuites == null || (cipherSuites.Length >= 1 && cipherSuites[cipherSuites.Length - 1] == 0));

                unsafe
                {
                    fixed (byte* cipherListStr = cipherList)
                    fixed (byte* cipherSuitesStr = cipherSuites)
                    {
                        if (!Ssl.SetCiphers(innerContext, cipherListStr, cipherSuitesStr))
                        {
                            Crypto.ErrClearError();
                            throw new PlatformNotSupportedException(SR.Format(SR.net_ssl_encryptionpolicy_notsupported, policy));
                        }
                    }
                }

                bool hasCertificateAndKey =
                    certHandle != null && !certHandle.IsInvalid
                    && certKeyHandle != null && !certKeyHandle.IsInvalid;

                if (hasCertificateAndKey)
                {
                    SetSslCertificate(innerContext, certHandle, certKeyHandle);
                }

                if (sslAuthenticationOptions.IsServer && sslAuthenticationOptions.RemoteCertRequired)
                {
                    Ssl.SslCtxSetVerify(innerContext, s_verifyClientCertificate);
                }

                GCHandle alpnHandle = default;
                try
                {
                    if (sslAuthenticationOptions.ApplicationProtocols != null)
                    {
                        if (sslAuthenticationOptions.IsServer)
                        {
                            alpnHandle = GCHandle.Alloc(sslAuthenticationOptions.ApplicationProtocols);
                            Interop.Ssl.SslCtxSetAlpnSelectCb(innerContext, s_alpnServerCallback, GCHandle.ToIntPtr(alpnHandle));
                        }
                        else
                        {
                            if (Interop.Ssl.SslCtxSetAlpnProtos(innerContext, sslAuthenticationOptions.ApplicationProtocols) != 0)
                            {
                                throw CreateSslException(SR.net_alpn_config_failed);
                            }
                        }
                    }

                    context = SafeSslHandle.Create(innerContext, sslAuthenticationOptions.IsServer);
                    Debug.Assert(context != null, "Expected non-null return value from SafeSslHandle.Create");
                    if (context.IsInvalid)
                    {
                        context.Dispose();
                        throw CreateSslException(SR.net_allocate_ssl_context_failed);
                    }

                    if (!sslAuthenticationOptions.IsServer)
                    {
                        // The IdnMapping converts unicode input into the IDNA punycode sequence.
                        string punyCode = s_idnMapping.GetAscii(sslAuthenticationOptions.TargetHost);

                        // Similar to windows behavior, set SNI on openssl by default for client context, ignore errors.
                        if (!Ssl.SslSetTlsExtHostName(context, punyCode))
                        {
                            Crypto.ErrClearError();
                        }
                    }

                    if (hasCertificateAndKey)
                    {
                        bool hasCertReference = false;
                        try
                        {
                            certHandle.DangerousAddRef(ref hasCertReference);
                            using (X509Certificate2 cert = new X509Certificate2(certHandle.DangerousGetHandle()))
                            {
                                X509Chain chain = null;
                                try
                                {
                                    chain = TLSCertificateExtensions.BuildNewChain(cert, includeClientApplicationPolicy: false);
                                    if (chain != null && !Ssl.AddExtraChainCertificates(context, chain))
                                    {
                                        throw CreateSslException(SR.net_ssl_use_cert_failed);
                                    }
                                }
                                finally
                                {
                                    if (chain != null)
                                    {
                                        int elementsCount = chain.ChainElements.Count;
                                        for (int i = 0; i < elementsCount; i++)
                                        {
                                            chain.ChainElements[i].Certificate.Dispose();
                                        }

                                        chain.Dispose();
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (hasCertReference)
                                certHandle.DangerousRelease();
                        }
                    }

                    context.AlpnHandle = alpnHandle;
                }
                catch
                {
                    if (alpnHandle.IsAllocated)
                    {
                        alpnHandle.Free();
                    }

                    throw;
                }
            }

            return context;
        }

        internal static bool DoSslHandshake(SafeSslHandle context, byte[] recvBuf, int recvOffset, int recvCount, out byte[] sendBuf, out int sendCount)
        {
            sendBuf = null;
            sendCount = 0;

            if ((recvBuf != null) && (recvCount > 0))
            {
                if (BioWrite(context.InputBio, recvBuf, recvOffset, recvCount) <= 0)
                {
                    // Make sure we clear out the error that is stored in the queue
                    throw Crypto.CreateOpenSslCryptographicException();
                }
            }

            int retVal = Ssl.SslDoHandshake(context);
            if (retVal != 1)
            {
                Exception innerError;
                Ssl.SslErrorCode error = GetSslError(context, retVal, out innerError);

                if ((retVal != -1) || (error != Ssl.SslErrorCode.SSL_ERROR_WANT_READ))
                {
                    throw new SslException(SR.Format(SR.net_ssl_handshake_failed_error, error), innerError);
                }
            }

            sendCount = Crypto.BioCtrlPending(context.OutputBio);
            if (sendCount > 0)
            {
                sendBuf = new byte[sendCount];

                try
                {
                    sendCount = BioRead(context.OutputBio, sendBuf, sendCount);
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

            bool stateOk = Ssl.IsSslStateOK(context);
            if (stateOk)
            {
                context.MarkHandshakeCompleted();
            }
            return stateOk;
        }

        internal static int Encrypt(SafeSslHandle context, ReadOnlyMemory<byte> input, ref byte[] output, out Ssl.SslErrorCode errorCode)
        {
#if DEBUG
            ulong assertNoError = Crypto.ErrPeekError();
            Debug.Assert(assertNoError == 0, "OpenSsl error queue is not empty, run: 'openssl errstr " + assertNoError.ToString("X") + "' for original error.");
#endif
            errorCode = Ssl.SslErrorCode.SSL_ERROR_NONE;

            int retVal;
            Exception innerError = null;

            lock (context)
            {
                unsafe
                {
                    using (MemoryHandle handle = input.Pin())
                    {
                        retVal = Ssl.SslWrite(context, (byte*)handle.Pointer, input.Length);
                    }
                }

                if (retVal != input.Length)
                {
                    errorCode = GetSslError(context, retVal, out innerError);
                }
            }

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
                        throw new SslException(SR.Format(SR.net_ssl_encrypt_failed, errorCode), innerError);
                }
            }
            else
            {
                int capacityNeeded = Crypto.BioCtrlPending(context.OutputBio);

                if (output == null || output.Length < capacityNeeded)
                {
                    output = new byte[capacityNeeded];
                }

                retVal = BioRead(context.OutputBio, output, capacityNeeded);

                if (retVal <= 0)
                {
                    // Make sure we clear out the error that is stored in the queue
                    Crypto.ErrClearError();
                }
            }

            return retVal;
        }

        internal static int Decrypt(SafeSslHandle context, byte[] outBuffer, int offset, int count, out Ssl.SslErrorCode errorCode)
        {
#if DEBUG
            ulong assertNoError = Crypto.ErrPeekError();
            Debug.Assert(assertNoError == 0, "OpenSsl error queue is not empty, run: 'openssl errstr " + assertNoError.ToString("X") + "' for original error.");
#endif
            errorCode = Ssl.SslErrorCode.SSL_ERROR_NONE;

            int retVal = BioWrite(context.InputBio, outBuffer, offset, count);
            Exception innerError = null;

            lock (context)
            {
                if (retVal == count)
                {
                    unsafe
                    {
                        fixed (byte* fixedBuffer = outBuffer)
                        {
                            retVal = Ssl.SslRead(context, fixedBuffer + offset, outBuffer.Length);
                        }
                    }

                    if (retVal > 0)
                    {
                        count = retVal;
                    }
                }

                if (retVal != count)
                {
                    errorCode = GetSslError(context, retVal, out innerError);
                }
            }

            if (retVal != count)
            {
                retVal = 0;

                switch (errorCode)
                {
                    // indicate end-of-file
                    case Ssl.SslErrorCode.SSL_ERROR_ZERO_RETURN:
                        break;

                    case Ssl.SslErrorCode.SSL_ERROR_WANT_READ:
                        // update error code to renegotiate if renegotiate is pending, otherwise make it SSL_ERROR_WANT_READ
                        errorCode = Ssl.IsSslRenegotiatePending(context) ?
                                    Ssl.SslErrorCode.SSL_ERROR_RENEGOTIATE :
                                    Ssl.SslErrorCode.SSL_ERROR_WANT_READ;
                        break;

                    default:
                        throw new SslException(SR.Format(SR.net_ssl_decrypt_failed, errorCode), innerError);
                }
            }

            return retVal;
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

        private static int VerifyClientCertificate(int preverify_ok, IntPtr x509_ctx_ptr)
        {
            // Full validation is handled after the handshake in VerifyCertificateProperties and the
            // user callback.  It's also up to those handlers to decide if a null certificate
            // is appropriate.  So just return success to tell OpenSSL that the cert is acceptable,
            // we'll process it after the handshake finishes.
            const int OpenSslSuccess = 1;
            return OpenSslSuccess;
        }

        private static unsafe int AlpnServerSelectCallback(IntPtr ssl, out byte* outp, out byte outlen, byte* inp, uint inlen, IntPtr arg)
        {
            outp = null;
            outlen = 0;

            GCHandle protocolHandle = GCHandle.FromIntPtr(arg);
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
                            fixed (byte* p = &MemoryMarshal.GetReference(clientProto)) outp = p;
                            outlen = length;
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

        private static int BioWrite(SafeBioHandle bio, byte[] buffer, int offset, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(buffer.Length >= offset + count);

            int bytes;
            unsafe
            {
                fixed (byte* bufPtr = buffer)
                {
                    bytes = Ssl.BioWrite(bio, bufPtr + offset, count);
                }
            }

            if (bytes != count)
            {
                throw CreateSslException(SR.net_ssl_write_bio_failed_error);
            }
            return bytes;
        }

        private static Ssl.SslErrorCode GetSslError(SafeSslHandle context, int result, out Exception innerError)
        {
            ErrorInfo lastErrno = Sys.GetLastErrorInfo(); // cache it before we make more P/Invoke calls, just in case we need it

            Ssl.SslErrorCode retVal = Ssl.SslGetError(context, result);
            switch (retVal)
            {
                case Ssl.SslErrorCode.SSL_ERROR_SYSCALL:
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
            return retVal;
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
            public SslException(string inputMessage)
                : base(inputMessage)
            {
            }

            public SslException(string inputMessage, Exception ex)
                : base(inputMessage, ex)
            {
            }

            public SslException(string inputMessage, int error)
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
