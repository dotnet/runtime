// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

using SafeSslHandle = Interop.libssl.SafeSslHandle;

internal static partial class Interop
{
    internal static class OpenSsl
    {
        #region internal methods

        //TODO (Issue #3362) Set remote certificate options
        internal static SafeSslHandle AllocateSslContext(long options, SafeX509Handle certHandle, SafeEvpPKeyHandle certKeyHandle, bool isServer, bool remoteCertRequired)
        {
            SafeSslHandle context = null;

            IntPtr method = GetSslMethod(isServer, options);

            using (libssl.SafeSslContextHandle innerContext = new libssl.SafeSslContextHandle(method))
            {
                if (innerContext.IsInvalid)
                {
                    throw CreateSslException(SR.net_allocate_ssl_context_failed);
                }

                libssl.SSL_CTX_ctrl(innerContext, libssl.SSL_CTRL_OPTIONS, options, IntPtr.Zero);

                libssl.SSL_CTX_set_quiet_shutdown(innerContext, 1);

                if (certHandle != null && certKeyHandle != null)
                {
                    SetSslCertificate(innerContext, certHandle, certKeyHandle);
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

        internal static bool DoSslHandshake(SafeSslHandle context, IntPtr recvPtr, int recvCount, out IntPtr sendPtr, out int sendCount)
        {
            sendPtr = IntPtr.Zero;
            sendCount = 0;
            if ((IntPtr.Zero != recvPtr) && (recvCount > 0))
            {
                BioWrite(context.InputBio, recvPtr, recvCount);
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
                sendPtr = Marshal.AllocHGlobal(sendCount);

                try
                {
                    sendCount = BioRead(context.OutputBio, sendPtr, sendCount);
                }
                finally
                {
                    if (sendCount <= 0)
                    {
                        Marshal.FreeHGlobal(sendPtr);
                        sendPtr = IntPtr.Zero;
                        sendCount = 0;
                    }
                }
            }
        
            return ((libssl.SSL_state(context) == (int)libssl.SslState.SSL_ST_OK));

        }

        internal static int Encrypt(SafeSslHandle context, IntPtr buffer, int offset, int count, int bufferCapacity, out libssl.SslErrorCode errorCode)
        {
            errorCode = libssl.SslErrorCode.SSL_ERROR_NONE;

            int retVal = libssl.SSL_write(context, new IntPtr(buffer.ToInt64() + offset), count);
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

                Debug.Assert(bufferCapacity >= capacityNeeded, "Input buffer of size " + bufferCapacity +
                                                              " bytes is insufficient since encryption needs " + capacityNeeded + " bytes.");

                retVal = BioRead(context.OutputBio, buffer, capacityNeeded);
            }

            return retVal;
        }

        internal static int Decrypt(SafeSslHandle context, IntPtr outBufferPtr, int count, out libssl.SslErrorCode errorCode)
        {
            errorCode = libssl.SslErrorCode.SSL_ERROR_NONE;

            int retVal = BioWrite(context.InputBio, outBufferPtr, count);

            if (retVal == count)
            {
                retVal = libssl.SSL_read(context, outBufferPtr, retVal);

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

        internal static libssl.SSL_CIPHER GetConnectionInfo(SafeSslHandle context)
        {
            IntPtr cipherPtr = libssl.SSL_get_current_cipher(context);
            var cipher = new libssl.SSL_CIPHER();
            if (IntPtr.Zero != cipherPtr)
            {
                cipher = Marshal.PtrToStructure<libssl.SSL_CIPHER>(cipherPtr);
            }

            return cipher;
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

            if (!ssl2)
            {
                if (!ssl3)
                {
                    if (!tls11 && !tls12)
                    {
                        method = libssl.SslMethods.TLSv1_method;
                    }
                    else if (!tls10 && !tls12)
                    {
                        method = libssl.SslMethods.TLSv1_1_method;
                    }
                    else if (!tls10 && !tls11)
                    {
                        method = libssl.SslMethods.TLSv1_2_method;
                    }
                }
                else if (!tls10 && !tls11 && !tls12)
                {
                    method = libssl.SslMethods.SSLv3_method;
                }
            }

            if (IntPtr.Zero == method)
            {
                throw CreateSslException(SR.net_get_ssl_method_failed);
            }

            return method;
        }

        private static void Disconnect(SafeSslHandle context)
        {
            int retVal = libssl.SSL_shutdown(context);
            if (retVal < 0)
            {
                //TODO (Issue #3362) check this error
                libssl.SSL_get_error(context, retVal);
            }
        }

        //TODO (Issue #3362) should we check Bio should retry?
        private static int BioRead(SafeBioHandle bio, IntPtr buffer, int count)
        {
            int bytes = libssl.BIO_read(bio, buffer, count);
            if (bytes != count)
            {
                throw CreateSslException(SR.net_ssl_read_bio_failed_error);
            }
            return bytes;
        }

        //TODO (Issue #3362) should we check Bio should retry?
        private static int BioWrite(SafeBioHandle bio, IntPtr buffer, int count)
        {
            int bytes = libssl.BIO_write(bio, buffer, count);
            if (bytes != count)
            {
                throw CreateSslException(SR.net_ssl_write_bio_failed_error);
            }
            return bytes;
        }

        private static libssl.SslErrorCode GetSslError(SafeSslHandle context, int result)
        {
            libssl.SslErrorCode retVal = libssl.SSL_get_error(context, result);
            if (retVal == libssl.SslErrorCode.SSL_ERROR_SYSCALL)
            {
                retVal = (libssl.SslErrorCode)libssl.ERR_get_error();
            }
            return retVal;
        }

        private static void SetSslCertificate(libssl.SafeSslContextHandle contextPtr, SafeX509Handle certPtr, SafeEvpPKeyHandle keyPtr)
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

        private static SslException CreateSslException(string message)
        {
            ulong errorVal = libssl.ERR_get_error();
            string msg = SR.Format(message, Marshal.PtrToStringAnsi(libssl.ERR_reason_error_string(errorVal)));
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
                    Exception innerEx = Interop.libcrypto.CreateOpenSslCryptographicException();
                    return new SslException(innerEx.Message, innerEx);

                default:
                    return new SslException(msg, error);
            }
        }

        private static SslException CreateSslException(SafeSslHandle context, string message, int error)
        {
            return CreateSslException(message, libssl.SSL_get_error(context, error));
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
