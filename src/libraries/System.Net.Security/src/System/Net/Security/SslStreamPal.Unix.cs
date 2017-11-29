// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
            SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, SslAuthenticationOptions sslAuthenticationOptions)
        {
            if (inputBuffers != null)
            {
                Debug.Assert(inputBuffers.Length == 2);
                Debug.Assert(inputBuffers[1].token == null);

                return HandshakeInternal(credential, ref context, inputBuffers[0], outputBuffer, sslAuthenticationOptions);
            }
            else
            {
                return HandshakeInternal(credential, ref context, inputBuffer: null, outputBuffer, sslAuthenticationOptions);
            }
        }

        public static SecurityStatusPal InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context,
            string targetName, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, SslAuthenticationOptions sslAuthenticationOptions)
        {
            return HandshakeInternal(credential, ref context, inputBuffer, outputBuffer, sslAuthenticationOptions);
        }

        public static SecurityStatusPal InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, SslAuthenticationOptions sslAuthenticationOptions)
        {
            Debug.Assert(inputBuffers.Length == 2);
            Debug.Assert(inputBuffers[1].token == null);

            return HandshakeInternal(credential, ref context, inputBuffers[0], outputBuffer, sslAuthenticationOptions);
        }

        public static SecurityBuffer[] GetIncomingSecurityBuffers(SslAuthenticationOptions options, ref SecurityBuffer incomingSecurity)
        {
            SecurityBuffer[] incomingSecurityBuffers = null;

            if (incomingSecurity != null)
            {
                incomingSecurityBuffers = new SecurityBuffer[]
                {
                    incomingSecurity,
                    new SecurityBuffer(null, 0, 0, SecurityBufferType.SECBUFFER_EMPTY)
                };
            }

            return incomingSecurityBuffers;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(X509Certificate certificate,
            SslProtocols protocols, EncryptionPolicy policy, bool isServer)
        {
            return new SafeFreeSslCredentials(certificate, protocols, policy);
        }

        public static SecurityStatusPal EncryptMessage(SafeDeleteContext securityContext, ReadOnlyMemory<byte> input, int headerSize, int trailerSize, ref byte[] output, out int resultSize)
        {
            return EncryptDecryptHelper(securityContext, input, offset: 0, size: 0, encrypt: true, output: ref output, resultSize: out resultSize);
        }

        public static SecurityStatusPal DecryptMessage(SafeDeleteContext securityContext, byte[] buffer, ref int offset, ref int count)
        {
            SecurityStatusPal retVal = EncryptDecryptHelper(securityContext, buffer, offset, count, false, ref buffer, out int resultSize);
            if (retVal.ErrorCode == SecurityStatusPalErrorCode.OK ||
                retVal.ErrorCode == SecurityStatusPalErrorCode.Renegotiate)
            {
                count = resultSize;
            }
            return retVal;
        }

        public static ChannelBinding QueryContextChannelBinding(SafeDeleteContext securityContext, ChannelBindingKind attribute)
        {
            ChannelBinding bindingHandle;

            if (attribute == ChannelBindingKind.Endpoint)
            {
                bindingHandle = EndpointChannelBindingToken.Build(securityContext);

                if (bindingHandle == null)
                {
                    throw Interop.OpenSsl.CreateSslException(SR.net_ssl_invalid_certificate);
                }
            }
            else
            {
                bindingHandle = Interop.OpenSsl.QueryChannelBinding(
                    ((SafeDeleteSslContext)securityContext).SslContext,
                    attribute);
            }

            return bindingHandle;
        }

        public static void QueryContextStreamSizes(SafeDeleteContext securityContext, out StreamSizes streamSizes)
        {
            streamSizes = s_streamSizes;
        }

        public static void QueryContextConnectionInfo(SafeDeleteContext securityContext, out SslConnectionInfo connectionInfo)
        {
            connectionInfo = new SslConnectionInfo(((SafeDeleteSslContext)securityContext).SslContext);
        }

        public static byte[] ConvertAlpnProtocolListToByteArray(List<SslApplicationProtocol> applicationProtocols)
        {
            return Interop.Ssl.ConvertAlpnProtocolListToByteArray(applicationProtocols);
        }

        private static SecurityStatusPal HandshakeInternal(SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer,
            SecurityBuffer outputBuffer, SslAuthenticationOptions sslAuthenticationOptions)
        {
            Debug.Assert(!credential.IsInvalid);

            try
            {
                if ((null == context) || context.IsInvalid)
                {
                    context = new SafeDeleteSslContext(credential as SafeFreeSslCredentials, sslAuthenticationOptions);
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

                // When the handshake is done, and the context is server, check if the alpnHandle target was set to null during ALPN.
                // If it was, then that indiciates ALPN failed, send failure.
                // We have this workaround, as openssl supports terminating handshake only from version 1.1.0,
                // whereas ALPN is supported from version 1.0.2.
                SafeSslHandle sslContext = ((SafeDeleteSslContext)context).SslContext;
                if (done && sslAuthenticationOptions.IsServer && sslAuthenticationOptions.ApplicationProtocols != null && sslContext.AlpnHandle.IsAllocated && sslContext.AlpnHandle.Target == null)
                {
                    return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, Interop.OpenSsl.CreateSslException(SR.net_alpn_failed));
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

        internal static byte[] GetNegotiatedApplicationProtocol(SafeDeleteContext context)
        {
            if (context == null)
                return null;

            return Interop.Ssl.SslGetAlpnSelected(((SafeDeleteSslContext)context).SslContext);
        }

        private static SecurityStatusPal EncryptDecryptHelper(SafeDeleteContext securityContext, ReadOnlyMemory<byte> input, int offset, int size, bool encrypt, ref byte[] output, out int resultSize)
        {
            resultSize = 0;
            try
            {
                Interop.Ssl.SslErrorCode errorCode = Interop.Ssl.SslErrorCode.SSL_ERROR_NONE;
                SafeSslHandle scHandle = ((SafeDeleteSslContext)securityContext).SslContext;

                if (encrypt)
                {
                    resultSize = Interop.OpenSsl.Encrypt(scHandle, input, ref output, out errorCode);
                }
                else
                {
                    resultSize = Interop.OpenSsl.Decrypt(scHandle, output, offset, size, out errorCode);
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
            // There doesn't seem to be an exposed API for writing an alert,
            // the API seems to assume that all alerts are generated internally by
            // SSLHandshake.
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }

        public static SecurityStatusPal ApplyShutdownToken(ref SafeFreeCredentials credentialsHandle, SafeDeleteContext securityContext)
        {
            SafeDeleteSslContext sslContext = ((SafeDeleteSslContext)securityContext);

            // Unset the quiet shutdown option initially configured.
            Interop.Ssl.SslSetQuietShutdown(sslContext.SslContext, 0);

            int status = Interop.Ssl.SslShutdown(sslContext.SslContext);
            if (status == 0)
            {
                // Call SSL_shutdown again for a bi-directional shutdown.
                status = Interop.Ssl.SslShutdown(sslContext.SslContext);
            }

            if (status == 1)
                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);

            Interop.Ssl.SslErrorCode code = Interop.Ssl.SslGetError(sslContext.SslContext, status);
            if (code == Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_READ ||
                code == Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_WRITE)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            }
            else
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, new Interop.OpenSsl.SslException((int)code));
            }
        }
    }
}
