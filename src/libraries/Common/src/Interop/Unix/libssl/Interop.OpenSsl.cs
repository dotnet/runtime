// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

        //TODO Set remote certificate options
        internal static IntPtr AllocateSslContext(long options, SafeX509Handle certHandle, bool isServer, bool remoteCertRequired)
        {
            IntPtr sslContextPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SslContext>());
            SslContext sslContext = new SslContext
            {              
                sslPtr = IntPtr.Zero,
                readBioPtr = IntPtr.Zero,
                writeBioPtr = IntPtr.Zero,
                isServer = isServer,
            };

            sslContext.isServer = isServer;

            try
            {
                IntPtr method = GetSslMethod(isServer, options);

                var contextPtr = libssl.SSL_CTX_new(method);
                if (IntPtr.Zero == contextPtr)
                {
                    throw CreateSslException("Failed to allocate SSL/TLS context");
                }

                libssl.SSL_CTX_ctrl(contextPtr, libssl.SSL_CTRL_OPTIONS, options, IntPtr.Zero);

                sslContext.sslPtr = libssl.SSL_new(contextPtr);

                libssl.SSL_CTX_free(contextPtr);     

                if (IntPtr.Zero == sslContext.sslPtr)
                {
                    throw CreateSslException("Failed to create SSSL object from SSL context");
                }      

                IntPtr memMethodRead = libcrypto.BIO_s_mem();
                IntPtr memMethodWrite = libcrypto.BIO_s_mem();
                if ((IntPtr.Zero == memMethodWrite) || (IntPtr.Zero == memMethodRead))
                {
                    throw CreateSslException("Failed to return memory BIO method function");
                }

                sslContext.readBioPtr = libssl.BIO_new(memMethodRead);
                sslContext.writeBioPtr = libssl.BIO_new(memMethodWrite);
                if ((IntPtr.Zero == sslContext.readBioPtr) || (IntPtr.Zero == sslContext.writeBioPtr))
                {
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
                Marshal.StructureToPtr(sslContext, sslContextPtr, false);
                FreeSslContext(sslContextPtr);
                return IntPtr.Zero;
            }
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
            if ((retVal == 1) && !isServer)
            {            
                return true;
            }

            int error;
            if (retVal != 1)
            {
                error = GetSslError(context.sslPtr, retVal, "SSL Handshake failed: ");
                if ((retVal != -1) || (error != libssl.SslErrorCode.SSL_ERROR_WANT_READ))
                {
                    throw CreateSslException(context.sslPtr, "SSL Handshake failed: ", error);
                }
            }

            sendCount = libssl.BIO_ctrl_pending(context.writeBioPtr);

            if (sendCount > 0)
            {
                sendPtr = Marshal.AllocHGlobal(sendCount);
                sendCount = BioRead(context.writeBioPtr, sendPtr, sendCount);
                if (sendCount <= 0)
                {
                    error = sendCount;
                    Marshal.FreeHGlobal(sendPtr);
                    sendPtr = IntPtr.Zero;
                    sendCount = 0;
                    error = GetSslError(context.sslPtr, error, "Read Bio failed: ");                 
                    throw CreateSslException("SSL Handshake failed", error);                   
                }
            }
        
            return ((libssl.SSL_state(context.sslPtr) == libssl.SslState.SSL_ST_OK)) ? true : false;

        }

        internal static int Encrypt(IntPtr handlePtr, IntPtr buffer, int count, int bufferCapacity)
        {
            SslContext context = Marshal.PtrToStructure<SslContext>(handlePtr);  

            var retVal = libssl.SSL_write(context.sslPtr, buffer, count);

            if (retVal != count)
            {
                throw CreateSslException("OpenSsl::Encrypt failed");
            }

            int capacityNeeded = libssl.BIO_ctrl_pending(context.writeBioPtr);      

            if (retVal == count)
            {
                if (capacityNeeded > bufferCapacity)
                {
                    throw CreateSslException("OpenSsl::Encrypt outBufferPtr should be null");
                }

                IntPtr outBufferPtr = buffer;

                retVal = BioRead(context.writeBioPtr, outBufferPtr, capacityNeeded);

                if (retVal < 0)
                {
                    throw CreateSslException("OpenSsl::Encrypt failed");
                }
            }

            return retVal;
        }

        internal static int Decrypt(IntPtr sslContextPtr, IntPtr outBufferPtr, int count)
        {
            SslContext context = Marshal.PtrToStructure<SslContext>(sslContextPtr);

            var retVal = BioWrite(context.readBioPtr, outBufferPtr, count);

            if (retVal == count)
            {
                retVal = libssl.SSL_read(context.sslPtr, outBufferPtr, retVal);

                if (retVal > 0) count = retVal;
            }

            if (retVal != count)
            {
                throw CreateSslException("OpenSsl::Decrypt failed");
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

        internal static byte[] GetCertificateRawData(IntPtr certPtr)
        {
            byte[] buffer = null;
            unsafe
            {
                byte* bufferPtr = null;
                int len = libssl.i2d_X509(certPtr, ref bufferPtr);
                if (len > 0)
                {
                    buffer = new byte[len];
                    fixed (byte* pinnedBuffer = buffer)
                    {
                        bufferPtr = pinnedBuffer;   // TODO: creating temp var just in case
                        len = libssl.i2d_X509(certPtr, ref bufferPtr);
                    }
                }
                if (len <= 0)
                {
                    throw CreateSslException("Failed to get certificate raw data");
                }
            }

            return buffer;
        }

        internal static void FreeCertificate(IntPtr certPtr)
        {
            if (IntPtr.Zero != certPtr)
            {
                libssl.X509_free(certPtr);
            }
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

        private static IntPtr GetSslMethod(bool isServer, long options)
        {
            long protocolMask = libssl.Options.SSL_OP_NO_SSLv2 | libssl.Options.SSL_OP_NO_SSLv3 |
                                libssl.Options.SSL_OP_NO_TLSv1 | libssl.Options.SSL_OP_NO_TLSv1_1 |
                                libssl.Options.SSL_OP_NO_TLSv1_2;
            options &= protocolMask;
            Debug.Assert(options != protocolMask, "All protocols are disabled");

            var noSsl2 = (options & libssl.Options.SSL_OP_NO_SSLv2) != 0;
            var noSsl3 = (options & libssl.Options.SSL_OP_NO_SSLv3) != 0;
            var noTls10 = (options & libssl.Options.SSL_OP_NO_TLSv1) != 0;
            var noTls11 = (options & libssl.Options.SSL_OP_NO_TLSv1_1) != 0;
            var noTls12 = (options & libssl.Options.SSL_OP_NO_TLSv1_2) != 0;

            var method = IntPtr.Zero;
            if (noSsl2 && noSsl3 && noTls11 && noTls12)
            {
                method = isServer ? libssl.TLSv1_server_method() : libssl.TLSv1_client_method();
            }
            else if (noSsl2 && noSsl3 && noTls10 && noTls12)
            {
                method = isServer ? libssl.TLSv1_1_server_method() : libssl.TLSv1_1_client_method();
            }
            else if (noSsl2 && noSsl3 && noTls10 && noTls11)
            {
                method = isServer ? libssl.TLSv1_2_server_method() : libssl.TLSv1_2_client_method();
            }
            else if (noSsl2 && noTls10 && noTls11 && noTls12)
            {
                method = isServer ? libssl.SSLv3_server_method() : libssl.SSLv3_client_method();
            }
            else
            {
                method = isServer ? libssl.SSLv23_server_method() : libssl.SSLv23_client_method();
            }

            if (IntPtr.Zero == method)
            {
                throw CreateSslException("Failed to get SSL method");
            }

            return method;
        }

        //TODO See if SSL_CTX_Set_quite_shutdown can be used
        private static void Disconnect(IntPtr sslPtr)
        {
            if (IntPtr.Zero != sslPtr)
            {
                int retVal = libssl.SSL_shutdown(sslPtr);
                if (0 == retVal)
                {
                    retVal = libssl.SSL_shutdown(sslPtr);
                    if (retVal < 0)
                    {
                        libssl.SSL_get_error(sslPtr, retVal);
                    }
                }

                libssl.SSL_free(sslPtr);
            }         
        }

        //TODO should we check Bio should retry?
        private static int BioRead(IntPtr BioPtr, IntPtr buffer, int count)
        {
            int bytes = libssl.BIO_read(BioPtr, buffer, count);
            if (bytes != count)
            {
                throw CreateSslException("Failed in Read BIO");
            }
            return bytes;
        }

        //TODO should we check Bio should retry?
        private static int BioWrite(IntPtr BioPtr, IntPtr buffer, int count)
        {
            int bytes = libssl.BIO_write(BioPtr, buffer, count);
            if (bytes != count)
            {
                throw CreateSslException("Failed in Write BIO");
            }
            return bytes;
        }

        private static int GetSslError(IntPtr sslPtr, int result, string message)
        {
            int retVal = libssl.SSL_get_error(sslPtr, result);
            if (retVal == libssl.SslErrorCode.SSL_ERROR_SYSCALL)
            {
                retVal = (int)libssl.ERR_get_error();
            }
            return retVal;
        }

        private static void SetSslCertificate(IntPtr contextPtr, IntPtr certPtr, IntPtr keyPtr)
        {
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
            return new SslException(message);
        }

        private static SslException CreateSslException(string message, int error)
        {
            if (error == libssl.SslErrorCode.SSL_ERROR_SYSCALL)
            {
                return new SslException(message);
            }
            else
            {
                return new SslException(message, error);
            }
        }

        private static SslException CreateSslException(IntPtr sslPtr, string message, int error)
        {
            return CreateSslException(message, libssl.SSL_get_error(sslPtr, error));
        }

        private sealed class SslException : Exception
        {
            public SslException(string message)
                : base(message + ": " + Marshal.PtrToStringAnsi(libssl.ERR_reason_error_string(libssl.ERR_get_error())))
            {
                HResult = (int)libssl.ERR_get_error();
            }

            public SslException(string message, int error)
                : base(message + ": " + error)
            {
                HResult = error;
            }
        }
        #endregion
    }
}
