// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    // For SSL connections:
    internal class SSPISecureChannelType : SSPIInterface
    {
        public Exception GetException(SecurityStatus status)
        {
            return new Interop.OpenSsl.SslException((int)status);
        }

        public void VerifyPackageInfo()
        {
        }

        public SafeFreeCredentials AcquireCredentialsHandle(X509Certificate certificate,
            SslProtocols protocols, EncryptionPolicy policy, bool isServer)
        {
            return new SafeFreeCredentials(certificate, protocols, policy);
        }

        public SecurityStatus AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context,
            SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, bool remoteCertRequired)
        {
            return HandshakeInternal(credential, ref context, inputBuffer, outputBuffer, true, remoteCertRequired);
        }

        public SecurityStatus InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context,
            string targetName, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer)
        {        
            return HandshakeInternal(credential, ref context, inputBuffer, outputBuffer, false, false);
        }

        public SecurityStatus InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer)
        {          
            Debug.Assert(inputBuffers.Length == 2);
            Debug.Assert(inputBuffers[1].token == null);
            return HandshakeInternal(credential, ref context, inputBuffers[0], outputBuffer, false, false);
        }

        public SecurityStatus EncryptMessage(SafeDeleteContext securityContext, byte[] buffer, int size, int headerSize, int trailerSize, out int resultSize)
        {
            // Unencrypted data starts at an offset of headerSize
            return EncryptDecryptHelper(securityContext, buffer, headerSize, size, headerSize, trailerSize, true, out resultSize);
        }

        public SecurityStatus DecryptMessage(SafeDeleteContext securityContext, byte[] buffer, ref int offset, ref int count)
        {
            int resultSize;
            SecurityStatus retVal = EncryptDecryptHelper(securityContext, buffer, offset, count, 0, 0, false, out resultSize);
            if (SecurityStatus.OK == retVal || SecurityStatus.Renegotiate == retVal)
            {
                count = resultSize;
            }
            return retVal;
        }

        public int QueryContextChannelBinding(SafeDeleteContext phContext, ChannelBindingKind attribute,
            out SafeFreeContextBufferChannelBinding refHandle)
        {
            // TODO (Issue #3362) To be implemented
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        public int QueryContextStreamSizes(SafeDeleteContext securityContext, out StreamSizes streamSizes)
        {
            streamSizes = null;
            try
            {
                streamSizes = new StreamSizes(Interop.libssl.SslSizes.HEADER_SIZE, Interop.libssl.SslSizes.TRAILER_SIZE, Interop.libssl.SslSizes.SSL3_RT_MAX_PLAIN_LENGTH);
                return 0;
            }
            catch
            {              
                return -1;
            }
        }

        public int QueryContextConnectionInfo(SafeDeleteContext securityContext, out SslConnectionInfo connectionInfo)
        {
            connectionInfo = null;
            try
            {
                Interop.libssl.SSL_CIPHER cipher = Interop.OpenSsl.GetConnectionInfo(securityContext.SslContext);
                connectionInfo =  new SslConnectionInfo(cipher);
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        public int QueryContextRemoteCertificate(SafeDeleteContext securityContext, out SafeFreeCertContext remoteCertContext)
        {
            remoteCertContext = null;
            try
            {
                SafeX509Handle remoteCertificate = Interop.OpenSsl.GetPeerCertificate(securityContext.SslContext);
                // Note that cert ownership is transferred to SafeFreeCertContext
                remoteCertContext = new SafeFreeCertContext(remoteCertificate);
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        public int QueryContextIssuerList(SafeDeleteContext securityContext, out Object issuerList)
        {
            // TODO (Issue #3362) To be implemented
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        private long GetOptions(SslProtocols protocols)
        {
            long retVal = Interop.libssl.Options.SSL_OP_NO_SSLv2 | Interop.libssl.Options.SSL_OP_NO_SSLv3 |
                                Interop.libssl.Options.SSL_OP_NO_TLSv1 | Interop.libssl.Options.SSL_OP_NO_TLSv1_1 |
                                Interop.libssl.Options.SSL_OP_NO_TLSv1_2;

            if ((protocols & SslProtocols.Ssl2) != 0)
            {
                retVal &= ~Interop.libssl.Options.SSL_OP_NO_SSLv2;
            }
            if ((protocols & SslProtocols.Ssl3) != 0)
            {
                retVal &= ~Interop.libssl.Options.SSL_OP_NO_SSLv3;
            }
            if ((protocols & SslProtocols.Tls) != 0)
            {
                retVal &= ~Interop.libssl.Options.SSL_OP_NO_TLSv1;
            }
            if ((protocols & SslProtocols.Tls11) != 0)
            {
                retVal &= ~Interop.libssl.Options.SSL_OP_NO_TLSv1_1;
            }
            if ((protocols & SslProtocols.Tls12) != 0)
            {
                retVal &= ~Interop.libssl.Options.SSL_OP_NO_TLSv1_2;
            }

            return retVal;
        }

        private SecurityStatus HandshakeInternal(SafeFreeCredentials credential, ref SafeDeleteContext context,
            SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, bool isServer, bool remoteCertRequired)
        {
            Debug.Assert(!credential.IsInvalid);

            try
            {
                if ((null == context) || context.IsInvalid)
                {
                    long options = GetOptions(credential.Protocols);
                    context = new SafeDeleteContext(credential, options, isServer, remoteCertRequired);
                }

                IntPtr inputPtr = IntPtr.Zero, outputPtr = IntPtr.Zero;
                int outputSize;
                bool done;

                if (null == inputBuffer)
                {
                    done = Interop.OpenSsl.DoSslHandshake(context.SslContext, inputPtr, 0, out outputPtr, out outputSize);
                }
                else
                {
                    unsafe
                    {
                        fixed (byte* tokenPtr = inputBuffer.token)
                        {
                            inputPtr = new IntPtr(tokenPtr + inputBuffer.offset);
                            done = Interop.OpenSsl.DoSslHandshake(context.SslContext, inputPtr, inputBuffer.size, out outputPtr, out outputSize);
                        }
                    }
                }

                outputBuffer.size = outputSize;
                outputBuffer.offset = 0;
                if (outputSize > 0)
                {
                    outputBuffer.token = new byte[outputBuffer.size];
                    Marshal.Copy(outputPtr, outputBuffer.token, 0, outputBuffer.size);
                }
                else
                {
                    outputBuffer.token = null;
                }

                if (outputPtr != IntPtr.Zero)
                {
                     Marshal.FreeHGlobal(outputPtr);
                     outputPtr = IntPtr.Zero;
                }

                return done ? SecurityStatus.OK : SecurityStatus.ContinueNeeded;
            }
            catch (Exception ex)
            {
                Debug.Fail("Exception Caught. - " + ex);
                return SecurityStatus.InternalError;             
            }
        }


        private SecurityStatus EncryptDecryptHelper(SafeDeleteContext securityContext, byte[] buffer, int offset, int size, int headerSize, int trailerSize, bool encrypt, out int resultSize)
        {
            resultSize = 0;
            try
            {
                Interop.libssl.SslErrorCode errorCode = Interop.libssl.SslErrorCode.SSL_ERROR_NONE;

                unsafe
                {
                    fixed (byte* bufferPtr = buffer)
                    {
                        IntPtr inputPtr = new IntPtr(bufferPtr);

                        Interop.libssl.SafeSslHandle scHandle = securityContext.SslContext;

                        resultSize = encrypt ?
                            Interop.OpenSsl.Encrypt(scHandle, inputPtr, offset, size, buffer.Length, out errorCode) :
                            Interop.OpenSsl.Decrypt(scHandle, inputPtr, size, out errorCode);
                    }
                }

                switch (errorCode)
                {
                    case Interop.libssl.SslErrorCode.SSL_ERROR_RENEGOTIATE:
                        return SecurityStatus.Renegotiate;
                    case Interop.libssl.SslErrorCode.SSL_ERROR_ZERO_RETURN:
                        return SecurityStatus.ContextExpired;
                    case Interop.libssl.SslErrorCode.SSL_ERROR_NONE:
                    case Interop.libssl.SslErrorCode.SSL_ERROR_WANT_READ:
                        return SecurityStatus.OK;
                    default:
                        return SecurityStatus.InternalError;
                }
            }
            catch (Exception ex)
            {
                Debug.Fail("Exception Caught. - " + ex);
                return SecurityStatus.InternalError;
            }
        }
    }
}
