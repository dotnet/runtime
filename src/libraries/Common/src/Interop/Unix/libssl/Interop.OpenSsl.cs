// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static class OpenSsl
    {
        private static libssl.verify_callback s_verifyClientCertificate = VerifyClientCertificate;

        #region internal methods

        internal static SafeSslHandle AllocateSslContext(long options, SafeX509Handle certHandle, SafeEvpPKeyHandle certKeyHandle, string encryptionPolicy, bool isServer, bool remoteCertRequired)
        {
            SafeSslHandle context = null;

            IntPtr method = GetSslMethod(isServer, options);

            using (SafeSslContextHandle innerContext = Ssl.SslCtxCreate(method))
            {
                if (innerContext.IsInvalid)
                {
                    throw CreateSslException(SR.net_allocate_ssl_context_failed);
                }

                Crypto.SslCtxCtrl(innerContext, libssl.SSL_CTRL_OPTIONS, options, IntPtr.Zero);

                libssl.SSL_CTX_set_quiet_shutdown(innerContext, 1);

                libssl.SSL_CTX_set_cipher_list(innerContext, encryptionPolicy);

                if (certHandle != null && certKeyHandle != null)
                {
                    SetSslCertificate(innerContext, certHandle, certKeyHandle);
                }

                if (remoteCertRequired)
                {
                    Debug.Assert(isServer, "isServer flag should be true");
                    libssl.SSL_CTX_set_verify(innerContext,
                        (int)libssl.ClientCertOption.SSL_VERIFY_PEER |
                        (int)libssl.ClientCertOption.SSL_VERIFY_FAIL_IF_NO_PEER_CERT,
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

            libssl.SslErrorCode error;
            int retVal = libssl.SSL_do_handshake(context);

            if (retVal != 1)
            {
                error = GetSslError(context, retVal);

                if ((retVal != -1) || (error != libssl.SslErrorCode.SSL_ERROR_WANT_READ))
                {
                    throw CreateSslException(context, SR.net_ssl_handshake_failed_error, retVal);
                }
            }

            sendCount = libssl.BIO_ctrl_pending(context.OutputBio);

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

            return ((libssl.SSL_state(context) == (int)libssl.SslState.SSL_ST_OK));

        }

        internal static int Encrypt(SafeSslHandle context, byte[] buffer, int offset, int count, out libssl.SslErrorCode errorCode)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(buffer.Length >= offset + count);

            errorCode = libssl.SslErrorCode.SSL_ERROR_NONE;

            int retVal;
            unsafe
            {
                fixed (byte* fixedBuffer = buffer)
                {
                    retVal = Ssl.SslWrite(context, fixedBuffer + offset, count);
                }
            }

            if (retVal != count)
            {
                errorCode = GetSslError(context, retVal);
                retVal = 0;

                switch (errorCode)
                {
                    // indicate end-of-file
                    case libssl.SslErrorCode.SSL_ERROR_ZERO_RETURN:
                    case libssl.SslErrorCode.SSL_ERROR_WANT_READ:
                        break;

                    default:
                        throw CreateSslException(SR.net_ssl_encrypt_failed, errorCode);
                }
            }
            else
            {
                int capacityNeeded = libssl.BIO_ctrl_pending(context.OutputBio);

                Debug.Assert(buffer.Length >= capacityNeeded, "Input buffer of size " + buffer.Length +
                                                              " bytes is insufficient since encryption needs " + capacityNeeded + " bytes.");

                retVal = BioRead(context.OutputBio, buffer, capacityNeeded);
            }

            return retVal;
        }

        internal static int Decrypt(SafeSslHandle context, byte[] outBuffer, int count, out libssl.SslErrorCode errorCode)
        {
            errorCode = libssl.SslErrorCode.SSL_ERROR_NONE;

            int retVal = BioWrite(context.InputBio, outBuffer, 0, count);

            if (retVal == count)
            {
                retVal = Ssl.SslRead(context, outBuffer, retVal);

                if (retVal > 0)
                {
                    count = retVal;
                }
            }

            if (retVal != count)
            {
                errorCode = GetSslError(context, retVal);
                retVal = 0;

                switch (errorCode)
                {
                    // indicate end-of-file
                    case libssl.SslErrorCode.SSL_ERROR_ZERO_RETURN:
                        break;

                    case libssl.SslErrorCode.SSL_ERROR_WANT_READ:
                        // update error code to renegotiate if renegotiate is pending, otherwise make it SSL_ERROR_WANT_READ
                        errorCode = libssl.SSL_renegotiate_pending(context) == 1 ?
                                    libssl.SslErrorCode.SSL_ERROR_RENEGOTIATE :
                                    libssl.SslErrorCode.SSL_ERROR_WANT_READ;
                        break;

                    default:
                        throw CreateSslException(SR.net_ssl_decrypt_failed, errorCode);
                }
            }

            return retVal;
        }

        internal static SafeX509Handle GetPeerCertificate(SafeSslHandle context)
        {
            return libssl.SSL_get_peer_certificate(context);
        }

        internal static SafeSharedX509StackHandle GetPeerCertificateChain(SafeSslHandle context)
        {
            return libssl.SSL_get_peer_cert_chain(context);
        }

        internal static void FreeSslContext(SafeSslHandle context)
        {
            Debug.Assert((context != null) && !context.IsInvalid, "Expected a valid context in FreeSslContext");
            Disconnect(context);
            context.Dispose();
        }

        #endregion

        #region private methods
        private static IntPtr GetSslMethod(bool isServer, long options)
        {
            options &= libssl.ProtocolMask;
            Debug.Assert(options != libssl.ProtocolMask, "All protocols are disabled");

            bool ssl2 = (options & libssl.Options.SSL_OP_NO_SSLv2) == 0;
            bool ssl3 = (options & libssl.Options.SSL_OP_NO_SSLv3) == 0;
            bool tls10 = (options & libssl.Options.SSL_OP_NO_TLSv1) == 0;
            bool tls11 = (options & libssl.Options.SSL_OP_NO_TLSv1_1) == 0;
            bool tls12 = (options & libssl.Options.SSL_OP_NO_TLSv1_2) == 0;

            IntPtr method = libssl.SslMethods.SSLv23_method; // default
            string methodName = "SSLv23_method";

            if (!ssl2)
            {
                if (!ssl3)
                {
                    if (!tls11 && !tls12)
                    {
                        method = libssl.SslMethods.TLSv1_method;
                        methodName = "TLSv1_method";
                    }
                    else if (!tls10 && !tls12)
                    {
                        method = libssl.SslMethods.TLSv1_1_method;
                        methodName = "TLSv1_1_method";
                    }
                    else if (!tls10 && !tls11)
                    {
                        method = libssl.SslMethods.TLSv1_2_method;
                        methodName = "TLSv1_2_method";
                    }
                }
                else if (!tls10 && !tls11 && !tls12)
                {
                    method = libssl.SslMethods.SSLv3_method;
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
            using (SafeX509StoreCtxHandle storeHandle = new SafeX509StoreCtxHandle(x509_ctx_ptr, false))
            {
                using (var chain = new X509Chain())
                {
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

                    using (SafeX509StackHandle chainStack = Crypto.X509StoreCtxGetChain(storeHandle))
                    {
                        if (chainStack.IsInvalid)
                        {
                            Debug.Fail("Invalid chain stack handle");
                            return 0;
                        }

                        IntPtr certPtr = Crypto.GetX509StackField(chainStack, 0);
                        if (IntPtr.Zero == certPtr)
                        {
                            return 0;
                        }

                        using (X509Certificate2 cert = new X509Certificate2(certPtr))
                        {
                            return chain.Build(cert) ? 1 : 0;
                        }
                    }
                }
            }
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

                libssl.SSL_CTX_set_client_CA_list(context, nameStack);

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

                    using (SafeX509Handle certHandle = Crypto.X509Duplicate(certificate.Handle))
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

        private static void Disconnect(SafeSslHandle context)
        {
            int retVal = libssl.SSL_shutdown(context);
            if (retVal < 0)
            {
                //TODO (Issue #3362) check this error
                Crypto.SslGetError(context, retVal);
            }
        }

        //TODO (Issue #3362) should we check Bio should retry?
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

        //TODO (Issue #3362) should we check Bio should retry?
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

        private static libssl.SslErrorCode GetSslError(SafeSslHandle context, int result)
        {
            libssl.SslErrorCode retVal = Crypto.SslGetError(context, result);
            if (retVal == libssl.SslErrorCode.SSL_ERROR_SYSCALL)
            {
                retVal = (libssl.SslErrorCode)Crypto.ErrGetError();
            }
            return retVal;
        }

        private static void SetSslCertificate(SafeSslContextHandle contextPtr, SafeX509Handle certPtr, SafeEvpPKeyHandle keyPtr)
        {
            Debug.Assert(certPtr != null && !certPtr.IsInvalid, "certPtr != null && !certPtr.IsInvalid");
            Debug.Assert(keyPtr != null && !keyPtr.IsInvalid, "keyPtr != null && !keyPtr.IsInvalid");

            int retVal = libssl.SSL_CTX_use_certificate(contextPtr, certPtr);

            if (1 != retVal)
            {
                throw CreateSslException(SR.net_ssl_use_cert_failed);
            }

            retVal = libssl.SSL_CTX_use_PrivateKey(contextPtr, keyPtr);

            if (1 != retVal)
            {
                throw CreateSslException(SR.net_ssl_use_private_key_failed);
            }

            //check private key
            retVal = libssl.SSL_CTX_check_private_key(contextPtr);

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

        private static SslException CreateSslException(string message, libssl.SslErrorCode error)
        {
            string msg = SR.Format(message, error);
            switch (error)
            {
                case libssl.SslErrorCode.SSL_ERROR_SYSCALL:
                    return CreateSslException(msg);

                case libssl.SslErrorCode.SSL_ERROR_SSL:
                    Exception innerEx = Interop.Crypto.CreateOpenSslCryptographicException();
                    return new SslException(innerEx.Message, innerEx);

                default:
                    return new SslException(msg, error);
            }
        }

        private static SslException CreateSslException(SafeSslHandle context, string message, int error)
        {
            return CreateSslException(message, Crypto.SslGetError(context, error));
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

            public SslException(string inputMessage, libssl.SslErrorCode error)
                : this(inputMessage, (int)error)
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
