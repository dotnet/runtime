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

#pragma warning disable IDE0060
        public static SecurityStatusPal AcceptSecurityContext(
            ref SafeFreeCredentials? credential,
            ref SafeDeleteSslContext? context,
            ReadOnlySpan<byte> inputBuffer,
            ref byte[]? outputBuffer,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            return HandshakeInternal(ref context, inputBuffer, ref outputBuffer, sslAuthenticationOptions, null);
        }

        public static SecurityStatusPal InitializeSecurityContext(
            ref SafeFreeCredentials? credential,
            ref SafeDeleteSslContext? context,
            string? _ /*targetName*/,
            ReadOnlySpan<byte> inputBuffer,
            ref byte[]? outputBuffer,
            SslAuthenticationOptions sslAuthenticationOptions,
            SelectClientCertificate? clientCertificateSelectionCallback)
        {
            return HandshakeInternal(ref context, inputBuffer, ref outputBuffer, sslAuthenticationOptions, clientCertificateSelectionCallback);
        }

        public static SafeFreeCredentials? AcquireCredentialsHandle(SslAuthenticationOptions sslAuthenticationOptions)
        {
            return null;
        }

        public static SecurityStatusPal EncryptMessage(SafeDeleteSslContext securityContext, ReadOnlyMemory<byte> input, int _ /*headerSize*/, int _1 /*trailerSize*/, ref byte[] output, out int resultSize)
        {
            try
            {
                resultSize = Interop.OpenSsl.Encrypt((SafeSslHandle)securityContext, input.Span, ref output, out Interop.Ssl.SslErrorCode errorCode);

                return MapNativeErrorCode(errorCode);
            }
            catch (Exception ex)
            {
                resultSize = 0;
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, ex);
            }
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

        public static SecurityStatusPal Renegotiate(
            ref SafeFreeCredentials? credentialsHandle,
            ref SafeDeleteSslContext context,
            SslAuthenticationOptions sslAuthenticationOptions,
            out byte[]? outputBuffer)
        {
            SecurityStatusPal status = Interop.OpenSsl.SslRenegotiate((SafeSslHandle)context, out _);

            outputBuffer = Array.Empty<byte>();
            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                return status;
            }
            return HandshakeInternal(ref context!, null, ref outputBuffer, sslAuthenticationOptions, null);
        }

        public static void QueryContextStreamSizes(SafeDeleteContext? _ /*securityContext*/, out StreamSizes streamSizes)
        {
            streamSizes = StreamSizes.Default;
        }

        public static void QueryContextConnectionInfo(SafeDeleteSslContext securityContext, ref SslConnectionInfo connectionInfo)
        {
            connectionInfo.UpdateSslConnectionInfo((SafeSslHandle)securityContext);
        }

        private static SecurityStatusPal HandshakeInternal(ref SafeDeleteSslContext? context,
            ReadOnlySpan<byte> inputBuffer, ref byte[]? outputBuffer, SslAuthenticationOptions sslAuthenticationOptions, SelectClientCertificate? clientCertificateSelectionCallback)
        {
            byte[]? output = null;
            int outputSize = 0;

            try
            {
                if ((null == context) || context.IsInvalid)
                {
                    context = Interop.OpenSsl.AllocateSslHandle(sslAuthenticationOptions);
                }

                SecurityStatusPalErrorCode errorCode = Interop.OpenSsl.DoSslHandshake((SafeSslHandle)context, inputBuffer, out output, out outputSize);

                if (errorCode == SecurityStatusPalErrorCode.CredentialsNeeded && clientCertificateSelectionCallback != null)
                {
                    X509Certificate2? clientCertificate = clientCertificateSelectionCallback(out _);
                    if (clientCertificate != null)
                    {
                        sslAuthenticationOptions.CertificateContext = SslStreamCertificateContext.Create(clientCertificate);
                    }

                    Interop.OpenSsl.UpdateClientCertiticate((SafeSslHandle)context, sslAuthenticationOptions);
                    errorCode = Interop.OpenSsl.DoSslHandshake((SafeSslHandle)context, null, out output, out outputSize);
                }

                // sometimes during renegotiation processing message does not yield new output.
                // That seems to be flaw in OpenSSL state machine and we have workaround to peek it and try it again.
                if (outputSize == 0 && Interop.Ssl.IsSslRenegotiatePending((SafeSslHandle)context))
                {
                    errorCode = Interop.OpenSsl.DoSslHandshake((SafeSslHandle)context, ReadOnlySpan<byte>.Empty, out output, out outputSize);
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
                    return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, Interop.OpenSsl.CreateSslException(SR.net_alpn_failed));
                }

                outputBuffer =
                    outputSize == 0 ? null :
                    outputSize == output!.Length ? output :
                    new Span<byte>(output, 0, outputSize).ToArray();

                return new SecurityStatusPal(errorCode);
            }
            catch (Exception exc)
            {
                // Even if handshake failed we may have Alert to sent.
                if (outputSize > 0)
                {
                    outputBuffer = outputSize == output!.Length ? output : new Span<byte>(output, 0, outputSize).ToArray();
                }

                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, exc);
            }
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
