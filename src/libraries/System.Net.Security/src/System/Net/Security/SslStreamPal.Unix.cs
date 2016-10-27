// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    internal static class SslStreamPal
    {
        private static readonly StreamSizes s_streamSizes = new StreamSizes();

        public static Exception GetException(SecurityStatusPal status)
        {
            return status.Exception ?? new Interop.OpenSsl.SslException((int)status.ErrorCode);
        }

        internal const bool StartMutualAuthAsAnonymous = false;
        internal const bool CanEncryptEmptyMessage = false;

        public static void VerifyPackageInfo()
        {
        }

        public static SecurityStatusPal AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context,
            SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, bool remoteCertRequired)
        {
            return HandshakeInternal(credential, ref context, inputBuffer, outputBuffer, true, remoteCertRequired);
        }

        public static SecurityStatusPal InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context,
            string targetName, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer)
        {        
            return HandshakeInternal(credential, ref context, inputBuffer, outputBuffer, false, false);
        }

        public static SecurityStatusPal InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer)
        {          
            Debug.Assert(inputBuffers.Length == 2);
            Debug.Assert(inputBuffers[1].token == null);
            return HandshakeInternal(credential, ref context, inputBuffers[0], outputBuffer, false, false);
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(X509Certificate certificate,
            SslProtocols protocols, EncryptionPolicy policy, bool isServer)
        {
            return new SafeFreeSslCredentials(certificate, protocols, policy);
        }

        public static SecurityStatusPal EncryptMessage(SafeDeleteContext securityContext, byte[] input, int offset, int size, int headerSize, int trailerSize, ref byte[] output, out int resultSize)
        {
            return EncryptDecryptHelper(securityContext, input, offset, size, true, ref output, out resultSize);
        }

        public static SecurityStatusPal DecryptMessage(SafeDeleteContext securityContext, byte[] buffer, ref int offset, ref int count)
        {
            int resultSize;
            SecurityStatusPal retVal = EncryptDecryptHelper(securityContext, buffer, offset, count, false, ref buffer, out resultSize);
            if (retVal.ErrorCode == SecurityStatusPalErrorCode.OK || 
                retVal.ErrorCode == SecurityStatusPalErrorCode.Renegotiate)
            {
                count = resultSize;
            }
            return retVal;
        }

        public static SafeFreeContextBufferChannelBinding QueryContextChannelBinding(SafeDeleteContext securityContext, ChannelBindingKind attribute)
        {
            SafeChannelBindingHandle bindingHandle = Interop.OpenSsl.QueryChannelBinding(((SafeDeleteSslContext)securityContext).SslContext, attribute);
            var refHandle = bindingHandle == null ? null : new SafeFreeContextBufferChannelBinding(bindingHandle);
            return refHandle;
        }

        public static void QueryContextStreamSizes(SafeDeleteContext securityContext, out StreamSizes streamSizes)
        {
            streamSizes = s_streamSizes;
        }

        public static void QueryContextConnectionInfo(SafeDeleteContext securityContext, out SslConnectionInfo connectionInfo)
        {
            connectionInfo = new SslConnectionInfo(((SafeDeleteSslContext)securityContext).SslContext);
        }

        private static SecurityStatusPal HandshakeInternal(SafeFreeCredentials credential, ref SafeDeleteContext context,
            SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, bool isServer, bool remoteCertRequired)
        {
            Debug.Assert(!credential.IsInvalid);

            try
            {
                if ((null == context) || context.IsInvalid)
                {
                    context = new SafeDeleteSslContext(credential as SafeFreeSslCredentials, isServer, remoteCertRequired);
                }

                byte[] output = null;
                int outputSize;
                bool done;

                if (null == inputBuffer)
                {
                    done = Interop.OpenSsl.DoSslHandshake(((SafeDeleteSslContext)context).SslContext, null, 0, 0, out output, out outputSize);
                }
                else
                {
                    done = Interop.OpenSsl.DoSslHandshake(((SafeDeleteSslContext)context).SslContext, inputBuffer.token, inputBuffer.offset, inputBuffer.size, out output, out outputSize);
                }

                outputBuffer.size = outputSize;
                outputBuffer.offset = 0;
                outputBuffer.token = outputSize > 0 ? output : null;

                return new SecurityStatusPal(done ? SecurityStatusPalErrorCode.OK : SecurityStatusPalErrorCode.ContinueNeeded);
            }
            catch (Exception exc)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, exc);     
            }
        }

        private static SecurityStatusPal EncryptDecryptHelper(SafeDeleteContext securityContext, byte[] input, int offset, int size, bool encrypt, ref byte[] output, out int resultSize)
        {
            resultSize = 0;
            try
            {
                Interop.Ssl.SslErrorCode errorCode = Interop.Ssl.SslErrorCode.SSL_ERROR_NONE;
                SafeSslHandle scHandle = ((SafeDeleteSslContext)securityContext).SslContext;

                if (encrypt)
                {
                    resultSize = Interop.OpenSsl.Encrypt(scHandle, input, offset, size, ref output, out errorCode);
                }
                else
                {
                    Debug.Assert(offset == 0, "Expected offset 0 when decrypting");
                    Debug.Assert(ReferenceEquals(input, output), "Expected input==output when decrypting");
                    resultSize = Interop.OpenSsl.Decrypt(scHandle, input, size, out errorCode);
                }

                switch (errorCode)
                {
                    case Interop.Ssl.SslErrorCode.SSL_ERROR_RENEGOTIATE:
                        return new SecurityStatusPal(SecurityStatusPalErrorCode.Renegotiate);
                    case Interop.Ssl.SslErrorCode.SSL_ERROR_ZERO_RETURN:
                        return new SecurityStatusPal(SecurityStatusPalErrorCode.ContextExpired);
                    case Interop.Ssl.SslErrorCode.SSL_ERROR_NONE:
                    case Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_READ:
                        return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
                    default:
                        return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, new Interop.OpenSsl.SslException((int)errorCode));
                }
            }
            catch (Exception ex)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, ex);
            }
        }

        public static SecurityStatusPal ApplyAlertToken(ref SafeFreeCredentials credentialsHandle, SafeDeleteContext securityContext, TlsAlertType alertType, TlsAlertMessage alertMessage)
        {
            // TODO (#12319): Not implemented.
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }

        public static SecurityStatusPal ApplyShutdownToken(ref SafeFreeCredentials credentialsHandle, SafeDeleteContext securityContext)
        {
            // TODO (#12319): Not implemented.
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }
    }
}
