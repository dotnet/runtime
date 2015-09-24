// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    // For SSL connections:
    internal class SSPISecureChannelType : SSPIInterface
    {
        public Exception GetException(SecurityStatus status)
        {
            // TODO: To be implemented
            throw new Exception("status = " + status);
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
            return EncryptDecryptHelper(securityContext, buffer, headerSize, size, headerSize, trailerSize, true,
                out resultSize);
        }

        public SecurityStatus DecryptMessage(SafeDeleteContext securityContext, byte[] buffer, ref int offset, ref int count)
        {
            int resultSize;
            SecurityStatus retVal = EncryptDecryptHelper(securityContext, buffer, offset, count, 0, 0, false, out resultSize);
            if (SecurityStatus.OK == retVal)
            {
                count = resultSize;
            }
            return retVal;
        }

        public int QueryContextChannelBinding(SafeDeleteContext phContext, ChannelBindingKind attribute,
            out SafeFreeContextBufferChannelBinding refHandle)
        {
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
            bool gotReference = false;
            connectionInfo = null;
            try
            {
                securityContext.DangerousAddRef(ref gotReference);
                Interop.libssl.SSL_CIPHER cipher = Interop.OpenSsl.GetConnectionInfo(securityContext.DangerousGetHandle());
                connectionInfo =  new SslConnectionInfo(cipher);
                return 0;
            }
            catch
            {
                return -1;
            }
            finally
            {
                if (gotReference)
                {
                    securityContext.DangerousRelease();
                }
            }
        }

        public int QueryContextRemoteCertificate(SafeDeleteContext securityContext, out SafeFreeCertContext remoteCertContext)
        {
            bool gotReference = false;
            remoteCertContext = null;
            try
            {
                securityContext.DangerousAddRef(ref gotReference);
                IntPtr certPtr = Interop.OpenSsl.GetPeerCertificate(securityContext.DangerousGetHandle());
                remoteCertContext = new SafeFreeCertContext(certPtr);
                return 0;
            }
            catch
            {
                return -1;
            }
            finally
            {
                if (gotReference)
                {
                    securityContext.DangerousRelease();
                }
            }
        }

        public int QueryContextIssuerList(SafeDeleteContext securityContext, out Object issuerList)
        {
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
            bool gotCredReference = false;
            bool gotContextReference = false;
            GCHandle inputHandle = new GCHandle();

            try
            {
                credential.DangerousAddRef(ref gotCredReference);

                if ((null == context) || context.IsInvalid)
                {
                    long options = GetOptions(credential.Protocols);               
                    IntPtr contextPtr = Interop.OpenSsl.AllocateSslContext(options, credential.CertHandle, isServer, remoteCertRequired);                 
                    context = new SafeDeleteContext(contextPtr, credential);
                }

                context.DangerousAddRef(ref gotContextReference);
           
                IntPtr inputPtr = IntPtr.Zero, outputPtr = IntPtr.Zero;
                int outputSize;
                bool done;

                if (null == inputBuffer)
                {
                    done = Interop.OpenSsl.DoSslHandshake(context.DangerousGetHandle(), inputPtr, 0, out outputPtr, out outputSize);
                }
                else
                {
                    inputHandle = GCHandle.Alloc(inputBuffer.token, GCHandleType.Pinned);
                    inputPtr = Marshal.UnsafeAddrOfPinnedArrayElement(inputBuffer.token, inputBuffer.offset);
                    done = Interop.OpenSsl.DoSslHandshake(context.DangerousGetHandle(), inputPtr, inputBuffer.size, out outputPtr, out outputSize);
                }           

                outputBuffer.size = outputSize;
                outputBuffer.offset = 0;
                if (outputSize > 0)
                {
                    outputBuffer.token = new byte[outputBuffer.size];
                    Marshal.Copy(outputPtr, outputBuffer.token, 0, outputBuffer.size);
                    Marshal.FreeHGlobal(outputPtr);
                }
                else
                {
                    outputBuffer.token = null;
                }      

                return done ? SecurityStatus.OK : SecurityStatus.ContinueNeeded;
            }
            catch
            {
                return SecurityStatus.InternalError;
            }
            finally
            {
                if (inputHandle.IsAllocated)
                {
                    inputHandle.Free();
                }
                if (gotContextReference)
                {
                    context.DangerousRelease();
                }
                if (gotCredReference)
                {
                    credential.DangerousRelease();
                }
            }
        }


        private SecurityStatus EncryptDecryptHelper(SafeDeleteContext securityContext, byte[] buffer, int offset, int size, int headerSize, int trailerSize, bool encrypt, out int resultSize)
        {
            bool gotReference = false;
            GCHandle inputHandle = new GCHandle();
            resultSize = 0;
            try
            {
                securityContext.DangerousAddRef(ref gotReference);
                inputHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                IntPtr inputPtr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
                if (encrypt)
                {
                    resultSize = Interop.OpenSsl.Encrypt(securityContext.DangerousGetHandle(), inputPtr, offset, size, buffer.Length);
                }
                else
                {
                    resultSize = Interop.OpenSsl.Decrypt(securityContext.DangerousGetHandle(), inputPtr, size);
                }
                return ((size == 0) || (resultSize > 0)) ? SecurityStatus.OK : SecurityStatus.ContextExpired;
            }
            catch
            {
                return SecurityStatus.InternalError;
            }
            finally
            {
                if (inputHandle.IsAllocated)
                {
                    inputHandle.Free();
                }
                if (gotReference)
                {
                    securityContext.DangerousRelease();
                }
            }
        }
    }
}
