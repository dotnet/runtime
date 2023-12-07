// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    internal static class SslStreamPal
    {
        public static Exception GetException(SecurityStatusPal status)
        {
            return status.Exception ?? new Interop.OpenSsl.SslException((int)status.ErrorCode);
        }

        internal const bool StartMutualAuthAsAnonymous = true;
        internal const bool CanEncryptEmptyMessage = false;

        public static void VerifyPackageInfo()
        {
        }

        public static SecurityStatusPal SelectApplicationProtocol(
            SafeFreeCredentials? credentialsHandle,
            SafeDeleteSslContext? context,
            SslAuthenticationOptions sslAuthenticationOptions,
            ReadOnlySpan<byte> clientProtocols)
        {
            throw new PlatformNotSupportedException(nameof(SelectApplicationProtocol));
        }

#pragma warning disable IDE0060
        public static ProtocolToken AcceptSecurityContext(
            ref SafeFreeCredentials? credential,
            ref SafeDeleteSslContext? context,
            ReadOnlySpan<byte> inputBuffer,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            return HandshakeInternal(ref context, inputBuffer, sslAuthenticationOptions);
        }

        public static ProtocolToken InitializeSecurityContext(
            ref SafeFreeCredentials? credential,
            ref SafeDeleteSslContext? context,
            string? _ /*targetName*/,
            ReadOnlySpan<byte> inputBuffer,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            return HandshakeInternal(ref context, inputBuffer, sslAuthenticationOptions);
        }

        public static SafeFreeCredentials? AcquireCredentialsHandle(SslAuthenticationOptions _1, bool _2)
        {
            return null;
        }

        public static ProtocolToken EncryptMessage(SafeDeleteSslContext securityContext, ReadOnlyMemory<byte> input, int _ /*headerSize*/, int _1 /*trailerSize*/)
        {
            ProtocolToken token = default;
            token.RentBuffer = true;
            try
            {
                Interop.Ssl.SslErrorCode errorCode = Interop.OpenSsl.Encrypt((SafeSslHandle)securityContext, input.Span, ref token);
                token.Status = MapNativeErrorCode(errorCode);
            }
            catch (Exception ex)
            {
                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, ex);
            }

            return token;
        }

        public static SecurityStatusPal DecryptMessage(SafeDeleteSslContext securityContext, Span<byte> buffer, out int offset, out int count)
        {
            offset = 0;
            count = 0;

            try
            {
                int resultSize = Interop.OpenSsl.Decrypt((SafeSslHandle)securityContext, buffer, out Interop.Ssl.SslErrorCode errorCode);

                SecurityStatusPal retVal = MapNativeErrorCode(errorCode);

                if (retVal.ErrorCode == SecurityStatusPalErrorCode.OK ||
                    retVal.ErrorCode == SecurityStatusPalErrorCode.Renegotiate)
                {
                    count = resultSize;
                }

                return retVal;
            }
            catch (Exception ex)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, ex);
            }
        }

        private static SecurityStatusPal MapNativeErrorCode(Interop.Ssl.SslErrorCode errorCode) =>
            errorCode switch
            {
                Interop.Ssl.SslErrorCode.SSL_ERROR_RENEGOTIATE => new SecurityStatusPal(SecurityStatusPalErrorCode.Renegotiate),
                Interop.Ssl.SslErrorCode.SSL_ERROR_ZERO_RETURN => new SecurityStatusPal(SecurityStatusPalErrorCode.ContextExpired),
                Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_X509_LOOKUP => new SecurityStatusPal(SecurityStatusPalErrorCode.CredentialsNeeded),
                Interop.Ssl.SslErrorCode.SSL_ERROR_NONE or
                Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_READ => new SecurityStatusPal(SecurityStatusPalErrorCode.OK),
                _ => new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, new Interop.OpenSsl.SslException((int)errorCode))
            };

        public static ChannelBinding? QueryContextChannelBinding(SafeDeleteSslContext securityContext, ChannelBindingKind attribute)
        {
            ChannelBinding? bindingHandle;

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
                    (SafeSslHandle)securityContext,
                    attribute);
            }

            return bindingHandle;
        }

        public static ProtocolToken Renegotiate(
            ref SafeFreeCredentials? credentialsHandle,
            ref SafeDeleteSslContext context,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            SecurityStatusPal status = Interop.OpenSsl.SslRenegotiate((SafeSslHandle)context, out _);

            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                return default;
            }
            return HandshakeInternal(ref context!, null, sslAuthenticationOptions);
        }

        public static void QueryContextStreamSizes(SafeDeleteContext? _ /*securityContext*/, out StreamSizes streamSizes)
        {
            streamSizes = StreamSizes.Default;
        }

        public static void QueryContextConnectionInfo(SafeDeleteSslContext securityContext, ref SslConnectionInfo connectionInfo)
        {
            connectionInfo.UpdateSslConnectionInfo((SafeSslHandle)securityContext);
        }

        public static bool TryUpdateClintCertificate(
            SafeFreeCredentials? _,
            SafeDeleteSslContext? context,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            Interop.OpenSsl.UpdateClientCertificate((SafeSslHandle)context!, sslAuthenticationOptions);

            return true;
        }

         private static ProtocolToken HandshakeInternal(ref SafeDeleteSslContext? context,
            ReadOnlySpan<byte> inputBuffer, SslAuthenticationOptions sslAuthenticationOptions)
        {
            ProtocolToken token = default;
            token.RentBuffer = true;
            try
            {
                if ((null == context) || context.IsInvalid)
                {
                    context = Interop.OpenSsl.AllocateSslHandle(sslAuthenticationOptions);
                }

                SecurityStatusPalErrorCode errorCode = Interop.OpenSsl.DoSslHandshake((SafeSslHandle)context, inputBuffer, ref token);

                if (errorCode == SecurityStatusPalErrorCode.CredentialsNeeded)
                {
                    // this should happen only for clients
                    Debug.Assert(sslAuthenticationOptions.IsClient);
                    token.Status = new SecurityStatusPal(errorCode);
                    return token;
                }

                // sometimes during renegotiation processing message does not yield new output.
                // That seems to be flaw in OpenSSL state machine and we have workaround to peek it and try it again.
                if (token.Size == 0 && Interop.Ssl.IsSslRenegotiatePending((SafeSslHandle)context))
                {
                    errorCode = Interop.OpenSsl.DoSslHandshake((SafeSslHandle)context, ReadOnlySpan<byte>.Empty, ref token);
                }

                // When the handshake is done, and the context is server, check if the alpnHandle target was set to null during ALPN.
                // If it was, then that indicates ALPN failed, send failure.
                // We have this workaround, as openssl supports terminating handshake only from version 1.1.0,
                // whereas ALPN is supported from version 1.0.2.
                SafeSslHandle sslContext = (SafeSslHandle)context;
                if (errorCode == SecurityStatusPalErrorCode.OK && sslAuthenticationOptions.IsServer
                    && sslAuthenticationOptions.ApplicationProtocols != null && sslAuthenticationOptions.ApplicationProtocols.Count != 0
                    && sslContext.AlpnHandle.IsAllocated && sslContext.AlpnHandle.Target == null)
                {
                    token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, Interop.OpenSsl.CreateSslException(SR.net_alpn_failed));
                    return token;
                }

                token.Status = new SecurityStatusPal(errorCode);
            }
            catch (Exception exc)
            {
                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, exc);
            }

            return token;
        }

        public static SecurityStatusPal ApplyAlertToken(SafeDeleteContext? securityContext, TlsAlertType alertType, TlsAlertMessage alertMessage)
        {
            // There doesn't seem to be an exposed API for writing an alert,
            // the API seems to assume that all alerts are generated internally by
            // SSLHandshake.
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }
#pragma warning restore IDE0060

        public static SecurityStatusPal ApplyShutdownToken(SafeDeleteSslContext context)
        {
            // Unset the quiet shutdown option initially configured.
            Interop.Ssl.SslSetQuietShutdown((SafeSslHandle)context, 0);

            int status = Interop.Ssl.SslShutdown((SafeSslHandle)context);
            if (status == 0)
            {
                // Call SSL_shutdown again for a bi-directional shutdown.
                status = Interop.Ssl.SslShutdown((SafeSslHandle)context);
            }

            if (status == 1)
                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);

            Interop.Ssl.SslErrorCode code = Interop.Ssl.SslGetError((SafeSslHandle)context, status);
            if (code == Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_READ ||
                code == Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_WRITE)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            }
            else if (code == Interop.Ssl.SslErrorCode.SSL_ERROR_SSL)
            {
                // OpenSSL failure occurred.  The error queue contains more details, when building the exception the queue will be cleared.
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, Interop.Crypto.CreateOpenSslCryptographicException());
            }
            else
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, new Interop.OpenSsl.SslException((int)code));
            }
        }
    }
}
