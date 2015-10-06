// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static class OpenSsl
    {
        #region structures
        [StructLayout(LayoutKind.Sequential)]
        private struct SslContext
        {         
            internal IntPtr sslPtr;
            internal IntPtr readBioPtr;
            internal IntPtr writeBioPtr;
            internal bool isServer;
        }
        #endregion

        #region internal methods


        //TODO (Issue #3362) Set remote certificate options
        internal static IntPtr AllocateSslContext(long options, SafeX509Handle certHandle, SafeEvpPKeyHandle certKeyHandle, bool isServer, bool remoteCertRequired)        
        {
            SslContext sslContext = new SslContext
            {
                isServer = isServer,
            };

            try
            {
                IntPtr method = GetSslMethod(isServer, options);

                IntPtr contextPtr = libssl.SSL_CTX_new(method);

                if (IntPtr.Zero == contextPtr)
                {
                    throw CreateSslException("Failed to allocate SSL/TLS context");
                }

                libssl.SSL_CTX_ctrl(contextPtr, libssl.SSL_CTRL_OPTIONS, options, IntPtr.Zero);

                libssl.SSL_CTX_set_quiet_shutdown(contextPtr, 1);

                if (certHandle != null && certKeyHandle != null)
                {
                    SetSslCertificate(contextPtr, certHandle, certKeyHandle);
                }

                sslContext.sslPtr = libssl.SSL_new(contextPtr);

                libssl.SSL_CTX_free(contextPtr);

                if (IntPtr.Zero == sslContext.sslPtr)
                {
                    throw CreateSslException("Failed to create SSSL object from SSL context");
                }

                IntPtr memMethod = libcrypto.BIO_s_mem();

                if (IntPtr.Zero == memMethod)
                {
                    throw CreateSslException("Failed to return memory BIO method function");
                }

                sslContext.readBioPtr = libssl.BIO_new(memMethod);
                sslContext.writeBioPtr = libssl.BIO_new(memMethod);

                if ((IntPtr.Zero == sslContext.readBioPtr) || (IntPtr.Zero == sslContext.writeBioPtr))
                {
                    FreeBio(sslContext);
                    throw CreateSslException("Failed to retun new BIO for a given method type");
                }

                if (isServer)
                {
                    libssl.SSL_set_accept_state(sslContext.sslPtr);
                }
                else
                {
                    libssl.SSL_set_connect_state(sslContext.sslPtr);
                }

                libssl.SSL_set_bio(sslContext.sslPtr, sslContext.readBioPtr, sslContext.writeBioPtr);
            }
            catch
            {
                Disconnect(sslContext.sslPtr);
                throw;
            }

            IntPtr sslContextPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SslContext>());
            Marshal.StructureToPtr(sslContext, sslContextPtr, false);
            return sslContextPtr;
        }

        internal static bool DoSslHandshake(IntPtr sslContextPtr, IntPtr recvPtr, int recvCount, out IntPtr sendPtr, out int sendCount)
        {
            sendPtr = IntPtr.Zero;
            sendCount = 0;
            SslContext context = Marshal.PtrToStructure<SslContext>(sslContextPtr);
            bool isServer = context.isServer;
            if ((IntPtr.Zero != recvPtr) && (recvCount > 0))
            {
                BioWrite(context.readBioPtr, recvPtr, recvCount);
            }

            int retVal = libssl.SSL_do_handshake(context.sslPtr);

            if (retVal != 1)
            {
                libssl.SslErrorCode error = GetSslError(context.sslPtr, retVal);

                if ((retVal != -1) || (error != libssl.SslErrorCode.SSL_ERROR_WANT_READ))
                {
                    throw CreateSslException(context.sslPtr, "SSL Handshake failed: ", retVal);
                }
            }

            sendCount = libssl.BIO_ctrl_pending(context.writeBioPtr);

            if (sendCount > 0)
            {
                sendPtr = Marshal.AllocHGlobal(sendCount);
                sendCount = BioRead(context.writeBioPtr, sendPtr, sendCount);
                if (sendCount <= 0)
                {
                    int errorCode = sendCount;
                    Marshal.FreeHGlobal(sendPtr);
                    sendPtr = IntPtr.Zero;
                    sendCount = 0;
                    throw CreateSslException(context.sslPtr, "Read Bio failed: ", errorCode);                   
                }
            }
        
            return ((libssl.SSL_state(context.sslPtr) == (int)libssl.SslState.SSL_ST_OK));

        }

        internal static int Encrypt(IntPtr handlePtr, IntPtr buffer, int offset, int count, int bufferCapacity, out libssl.SslErrorCode errorCode)
        {
            errorCode = libssl.SslErrorCode.SSL_ERROR_NONE;

            SslContext context = Marshal.PtrToStructure<SslContext>(handlePtr);

            var retVal = libssl.SSL_write(context.sslPtr, new IntPtr(buffer.ToInt64() + offset), count);

            if (retVal != count)
            {
                errorCode = GetSslError(context.sslPtr, retVal);
                retVal = 0;

                switch (errorCode)
                {
                    // indicate end-of-file
                    case libssl.SslErrorCode.SSL_ERROR_ZERO_RETURN:
                    case libssl.SslErrorCode.SSL_ERROR_WANT_READ:
                        break;

                    default:
                        throw CreateSslException("OpenSsl::Encrypt failed");
                }
            }
            else
            {
                int capacityNeeded = libssl.BIO_ctrl_pending(context.writeBioPtr);      

                if (capacityNeeded > bufferCapacity)
                {
                    throw CreateSslException("OpenSsl::Encrypt capacity needed is more than buffer capacity. capacityNeeded = " + capacityNeeded + "," + "bufferCapacity = " + bufferCapacity);
                }             

                retVal = BioRead(context.writeBioPtr, buffer, capacityNeeded);

                if (retVal < 0)
                {
                    throw CreateSslException("OpenSsl::Encrypt failed");
                }
            }

            return retVal;
        }

        internal static int Decrypt(IntPtr sslContextPtr, IntPtr outBufferPtr, int count, out libssl.SslErrorCode errorCode)
        {
            errorCode = libssl.SslErrorCode.SSL_ERROR_NONE;

            SslContext context = Marshal.PtrToStructure<SslContext>(sslContextPtr);

            int retVal = BioWrite(context.readBioPtr, outBufferPtr, count);

            if (retVal == count)
            {
                retVal = libssl.SSL_read(context.sslPtr, outBufferPtr, retVal);

                if (retVal > 0)
                {
                    count = retVal;
                }
            }

            if (retVal != count)
            {
                errorCode = GetSslError(context.sslPtr, retVal);
                retVal = 0;

                switch (errorCode)
                {
                    // indicate end-of-file
                    case libssl.SslErrorCode.SSL_ERROR_ZERO_RETURN:                      
                        break;

                    case libssl.SslErrorCode.SSL_ERROR_WANT_READ:
                        // update error code to renegotiate if renegotiate is pending, otherwise make it SSL_ERROR_WANT_READ
                        errorCode = libssl.SSL_renegotiate_pending(context.sslPtr) == 1 ?
                                    libssl.SslErrorCode.SSL_ERROR_RENEGOTIATE :
                                    libssl.SslErrorCode.SSL_ERROR_WANT_READ;
                        break;

                    default:
                        throw CreateSslException("OpenSsl::Decrypt failed");
                }
            }

            return retVal;
        }

        internal static IntPtr GetPeerCertificate(IntPtr sslContextPtr)
        {
            SslContext context = Marshal.PtrToStructure<SslContext>(sslContextPtr);
            IntPtr sslPtr = context.sslPtr;
            IntPtr certPtr = libssl.SSL_get_peer_certificate(sslPtr);
            return certPtr;
        }

        internal static SafeSharedX509StackHandle GetPeerCertificateChain(IntPtr sslContextPtr)
        {
            SslContext context = Marshal.PtrToStructure<SslContext>(sslContextPtr);
            IntPtr sslPtr = context.sslPtr;

            return libssl.SSL_get_peer_cert_chain(sslPtr);
        }

        internal static libssl.SSL_CIPHER GetConnectionInfo(IntPtr sslContextPtr)
        {
            SslContext context = Marshal.PtrToStructure<SslContext>(sslContextPtr);
            IntPtr sslPtr = context.sslPtr;
            IntPtr cipherPtr = libssl.SSL_get_current_cipher(sslPtr);
            var cipher = new libssl.SSL_CIPHER();
            if (IntPtr.Zero != cipherPtr)
            {
                cipher = Marshal.PtrToStructure<libssl.SSL_CIPHER>(cipherPtr);
            }

            return cipher;
        }

        internal static void FreeSslContext(IntPtr sslContextPtr)
        {
            if (IntPtr.Zero == sslContextPtr)
            {
                return;
            }

            SslContext context = Marshal.PtrToStructure<SslContext>(sslContextPtr);
            Disconnect(context.sslPtr);
            Marshal.FreeHGlobal(sslContextPtr);
            sslContextPtr = IntPtr.Zero;
        }

        #endregion

        #region private methods

        private static void FreeBio(SslContext sslContext)
        {
            if (IntPtr.Zero != sslContext.readBioPtr)
            {
                Interop.libcrypto.BIO_free(sslContext.readBioPtr);
            }

            if (IntPtr.Zero != sslContext.writeBioPtr)
            {
                Interop.libcrypto.BIO_free(sslContext.writeBioPtr);
            }
        }

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
                throw CreateSslException("Failed to get SSL method");
            }

            return method;
        }

        private static void Disconnect(IntPtr sslPtr)
        {
            if (IntPtr.Zero != sslPtr)
            {
                int retVal = libssl.SSL_shutdown(sslPtr);
                if (retVal < 0)
                {
					//TODO (Issue #3362) check this error
                    libssl.SSL_get_error(sslPtr, retVal);
                }

                libssl.SSL_free(sslPtr);
            }
        }

        //TODO (Issue #3362) should we check Bio should retry?
        private static int BioRead(IntPtr BioPtr, IntPtr buffer, int count)
        {
            int bytes = libssl.BIO_read(BioPtr, buffer, count);
            if (bytes != count)
            {
                throw CreateSslException("Failed in Read BIO");
            }
            return bytes;
        }

        //TODO (Issue #3362) should we check Bio should retry?
        private static int BioWrite(IntPtr BioPtr, IntPtr buffer, int count)
        {
            int bytes = libssl.BIO_write(BioPtr, buffer, count);
            if (bytes != count)
            {
                throw CreateSslException("Failed in Write BIO");
            }
            return bytes;
        }

        private static libssl.SslErrorCode GetSslError(IntPtr sslPtr, int result)
        {
            libssl.SslErrorCode retVal = libssl.SSL_get_error(sslPtr, result);
            if (retVal == libssl.SslErrorCode.SSL_ERROR_SYSCALL)
            {
                retVal = (libssl.SslErrorCode)libssl.ERR_get_error();
            }
            return retVal;
        }

        private static void SetSslCertificate(IntPtr contextPtr, SafeX509Handle certPtr, SafeEvpPKeyHandle keyPtr)
        {
            Debug.Assert(certPtr != null && !certPtr.IsInvalid, "certPtr != null && !certPtr.IsInvalid");
            Debug.Assert(keyPtr != null && !keyPtr.IsInvalid, "keyPtr != null && !keyPtr.IsInvalid");

            int retVal = libssl.SSL_CTX_use_certificate(contextPtr, certPtr);
            if (1 != retVal)
            {
                throw CreateSslException("Failed to use SSL certificate");
            }

            retVal = libssl.SSL_CTX_use_PrivateKey(contextPtr, keyPtr);
            if (1 != retVal)
            {
                throw CreateSslException("Failed to use SSL certificate private key");
            }
            //check private key
            retVal = libssl.SSL_CTX_check_private_key(contextPtr);
            if (1 != retVal)
            {
                throw CreateSslException("Certificate pivate key check failed");
            }
        }

        private static SslException CreateSslException(string message)
        {
            ulong errorVal = libssl.ERR_get_error();
            string msg = message + ": " + Marshal.PtrToStringAnsi(libssl.ERR_reason_error_string(errorVal));
            return new SslException(msg, (int)errorVal);
        }

        private static SslException CreateSslException(string message, libssl.SslErrorCode error)
        {
            switch (error)
            {
                case libssl.SslErrorCode.SSL_ERROR_SYSCALL:
                    return new SslException(message, error);

                case libssl.SslErrorCode.SSL_ERROR_SSL:
                    Exception innerEx = Interop.libcrypto.CreateOpenSslCryptographicException();
                    return new SslException(innerEx.Message, innerEx);

                default:
                    return new SslException(message + ": " + error, error);
            }
        }

        private static SslException CreateSslException(IntPtr sslPtr, string message, int error)
        {
            return CreateSslException(message, libssl.SSL_get_error(sslPtr, error));
        }

        private sealed class SslException : Exception
        {
            public SslException(string inputMessage, libssl.SslErrorCode error): base(inputMessage)
            {
                HResult = (int)error;
            }

            public SslException(string inputMessage, int error): base(inputMessage)
            {
                HResult = error;               
            }

            public SslException(string inputMessage, Exception ex): base(inputMessage, ex)
            {                
            }
        }
        #endregion
    }
}