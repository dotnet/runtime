// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
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
        private static Ssl.SslCtxSetVerifyCallback s_verifyClientCertificate = VerifyClientCertificate;

        #region internal methods

        internal static SafeChannelBindingHandle QueryChannelBinding(SafeSslHandle context, ChannelBindingKind bindingType)
        {
            SafeChannelBindingHandle bindingHandle;
            switch (bindingType)
            {
                case ChannelBindingKind.Endpoint:
                    bindingHandle = new SafeChannelBindingHandle(bindingType);
                    QueryEndPointChannelBinding(context, bindingHandle);
                    break;

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

        internal static SafeSslHandle AllocateSslContext(SslProtocols protocols, SafeX509Handle certHandle, SafeEvpPKeyHandle certKeyHandle, EncryptionPolicy policy, bool isServer, bool remoteCertRequired)
        {
            SafeSslHandle context = null;

            IntPtr method = GetSslMethod(protocols);

            using (SafeSslContextHandle innerContext = Ssl.SslCtxCreate(method))
            {
                if (innerContext.IsInvalid)
                {
                    throw CreateSslException(SR.net_allocate_ssl_context_failed);
                }

                // Configure allowed protocols. It's ok to use DangerousGetHandle here without AddRef/Release as we just
                // create the handle, it's rooted by the using, no one else has a reference to it, etc.
                Ssl.SetProtocolOptions(innerContext.DangerousGetHandle(), protocols);

                // The logic in SafeSslHandle.Disconnect is simple because we are doing a quiet
                // shutdown (we aren't negotiating for session close to enable later session
                // restoration).
                //
                // If you find yourself wanting to remove this line to enable bidirectional
                // close-notify, you'll probably need to rewrite SafeSslHandle.Disconnect().
                // https://www.openssl.org/docs/manmaster/ssl/SSL_shutdown.html
                Ssl.SslCtxSetQuietShutdown(innerContext);

                if (!Ssl.SetEncryptionPolicy(innerContext, policy))
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.net_ssl_encryptionpolicy_notsupported, policy));
                }

                bool hasCertificateAndKey =
                    certHandle != null && !certHandle.IsInvalid
                    && certKeyHandle != null && !certKeyHandle.IsInvalid;

                if (hasCertificateAndKey)
                {
                    SetSslCertificate(innerContext, certHandle, certKeyHandle);
                }

                if (remoteCertRequired)
                {
                    Debug.Assert(isServer, "isServer flag should be true");
                    Ssl.SslCtxSetVerify(innerContext,
                        s_verifyClientCertificate);

                    //update the client CA list 
                    UpdateCAListFromRootStore(innerContext);
                }

                context = SafeSslHandle.Create(innerContext, isServer);
                Debug.Assert(context != null, "Expected non-null return value from SafeSslHandle.Create");
                if (context.IsInvalid)
                {
                    context.Dispose();
                    throw CreateSslException(SR.net_allocate_ssl_context_failed);
                }

                if (hasCertificateAndKey)
                {
                    bool hasCertReference = false;
                    try
                    {
                        certHandle.DangerousAddRef(ref hasCertReference);
                        using (X509Certificate2 cert = new X509Certificate2(certHandle.DangerousGetHandle()))
                        {
                            using (X509Chain chain = TLSCertificateExtensions.BuildNewChain(cert, includeClientApplicationPolicy: false))
                            {
                                if (chain != null && !Ssl.AddExtraChainCertificates(context, chain))
                                    throw CreateSslException(SR.net_ssl_use_cert_failed);
                            }
                        }
                    }
                    finally
                    {
                        if (hasCertReference)
                            certHandle.DangerousRelease();
                    }
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
                BioWrite(context.InputBio, recvBuf, recvOffset, recvCount);
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

        internal static int Encrypt(SafeSslHandle context, byte[] input, int offset, int count, ref byte[] output, out Ssl.SslErrorCode errorCode)
        {
            Debug.Assert(input != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count > 0);
            Debug.Assert(offset <= input.Length);
            Debug.Assert(input.Length - offset >= count);

            errorCode = Ssl.SslErrorCode.SSL_ERROR_NONE;

            int retVal;
            unsafe
            {
                fixed (byte* fixedBuffer = input)
                {
                    retVal = Ssl.SslWrite(context, fixedBuffer + offset, count);
                }
            }

            if (retVal != count)
            {
                Exception innerError;
                errorCode = GetSslError(context, retVal, out innerError);
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
            }

            return retVal;
        }

        internal static int Decrypt(SafeSslHandle context, byte[] outBuffer, int count, out Ssl.SslErrorCode errorCode)
        {
            errorCode = Ssl.SslErrorCode.SSL_ERROR_NONE;

            int retVal = BioWrite(context.InputBio, outBuffer, 0, count);

            if (retVal == count)
            {
                retVal = Ssl.SslRead(context, outBuffer, outBuffer.Length);

                if (retVal > 0)
                {
                    count = retVal;
                }
            }

            if (retVal != count)
            {
                Exception innerError;
                errorCode = GetSslError(context, retVal, out innerError);
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

        private static void QueryEndPointChannelBinding(SafeSslHandle context, SafeChannelBindingHandle bindingHandle)
        {
            using (SafeX509Handle certSafeHandle = GetPeerCertificate(context))
            {
                if (certSafeHandle == null || certSafeHandle.IsInvalid)
                {
                    throw CreateSslException(SR.net_ssl_invalid_certificate);
                }

                bool gotReference = false;

                try
                {
                    certSafeHandle.DangerousAddRef(ref gotReference);
                    using (X509Certificate2 cert = new X509Certificate2(certSafeHandle.DangerousGetHandle()))
                    using (HashAlgorithm hashAlgo = GetHashForChannelBinding(cert))
                    {
                        byte[] bindingHash = hashAlgo.ComputeHash(cert.RawData);
                        bindingHandle.SetCertHash(bindingHash);
                    }
                }
                finally
                {
                    if (gotReference)
                    {
                        certSafeHandle.DangerousRelease();
                    }
                }
            }
        }

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

        private static IntPtr GetSslMethod(SslProtocols protocols)
        {
            Debug.Assert(protocols != SslProtocols.None, "All protocols are disabled");

#pragma warning disable 0618 // Ssl2, Ssl3 are deprecated.
            bool ssl2 = (protocols & SslProtocols.Ssl2) == SslProtocols.Ssl2;
            bool ssl3 = (protocols & SslProtocols.Ssl3) == SslProtocols.Ssl3;
#pragma warning restore
            bool tls10 = (protocols & SslProtocols.Tls) == SslProtocols.Tls;
            bool tls11 = (protocols & SslProtocols.Tls11) == SslProtocols.Tls11;
            bool tls12 = (protocols & SslProtocols.Tls12) == SslProtocols.Tls12;

            IntPtr method = Ssl.SslMethods.SSLv23_method; // default
            string methodName = "SSLv23_method";

            if (!ssl2)
            {
                if (!ssl3)
                {
                    if (!tls11 && !tls12)
                    {
                        method = Ssl.SslMethods.TLSv1_method;
                        methodName = "TLSv1_method";
                    }
                    else if (!tls10 && !tls12)
                    {
                        method = Ssl.SslMethods.TLSv1_1_method;
                        methodName = "TLSv1_1_method";
                    }
                    else if (!tls10 && !tls11)
                    {
                        method = Ssl.SslMethods.TLSv1_2_method;
                        methodName = "TLSv1_2_method";
                    }
                }
                else if (!tls10 && !tls11 && !tls12)
                {
                    method = Ssl.SslMethods.SSLv3_method;
                    methodName = "SSLv3_method";
                }
            }

            if (IntPtr.Zero == method)
            {
                throw new SslException(SR.Format(SR.net_get_ssl_method_failed, methodName));
            }

            return method;
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

        private static void UpdateCAListFromRootStore(SafeSslContextHandle context)
        {
            using (SafeX509NameStackHandle nameStack = Crypto.NewX509NameStack())
            {
                //maintaining the HashSet of Certificate's issuer name to keep track of duplicates 
                HashSet<string> issuerNameHashSet = new HashSet<string>();

                //Enumerate Certificates from LocalMachine and CurrentUser root store 
                AddX509Names(nameStack, StoreLocation.LocalMachine, issuerNameHashSet);
                AddX509Names(nameStack, StoreLocation.CurrentUser, issuerNameHashSet);

                Ssl.SslCtxSetClientCAList(context, nameStack);

                // The handle ownership has been transferred into the CTX.
                nameStack.SetHandleAsInvalid();
            }

        }

        private static void AddX509Names(SafeX509NameStackHandle nameStack, StoreLocation storeLocation, HashSet<string> issuerNameHashSet)
        {
            using (var store = new X509Store(StoreName.Root, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);

                foreach (var certificate in store.Certificates)
                {
                    //Check if issuer name is already present
                    //Avoiding duplicate names
                    if (!issuerNameHashSet.Add(certificate.Issuer))
                    {
                        continue;
                    }

                    using (SafeX509Handle certHandle = Crypto.X509UpRef(certificate.Handle))
                    {
                        using (SafeX509NameHandle nameHandle = Crypto.DuplicateX509Name(Crypto.X509GetIssuerName(certHandle)))
                        {
                            if (Crypto.PushX509NameStackField(nameStack, nameHandle))
                            {
                                // The handle ownership has been transferred into the STACK_OF(X509_NAME).
                                nameHandle.SetHandleAsInvalid();
                            }
                            else
                            {
                                throw new CryptographicException(SR.net_ssl_x509Name_push_failed_error);
                            }
                        }
                    }
                }
            }
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
                    // OpenSSL failure occurred.  The error queue contains more details.
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
            ulong errorVal = Crypto.ErrGetError();
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
