// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;

using PAL_SSLStreamStatus = Interop.AndroidCrypto.PAL_SSLStreamStatus;

#pragma warning disable IDE0060

namespace System.Net.Security
{
    internal static class SslStreamPal
    {
        public static Exception GetException(SecurityStatusPal status)
        {
            return status.Exception ?? new Interop.AndroidCrypto.SslException((int)status.ErrorCode);
        }

        internal const bool StartMutualAuthAsAnonymous = false;
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

        public static ProtocolToken AcceptSecurityContext(
            ref SafeFreeCredentials credential,
            ref SafeDeleteSslContext? context,
            ReadOnlySpan<byte> inputBuffer,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            return HandshakeInternal(credential, ref context, inputBuffer, sslAuthenticationOptions);
        }

        public static ProtocolToken InitializeSecurityContext(
            ref SafeFreeCredentials credential,
            ref SafeDeleteSslContext? context,
            string? targetName,
            ReadOnlySpan<byte> inputBuffer,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            return HandshakeInternal(credential, ref context, inputBuffer, sslAuthenticationOptions);
        }

        public static ProtocolToken Renegotiate(
            ref SafeFreeCredentials? credentialsHandle,
            ref SafeDeleteSslContext? context,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            throw new PlatformNotSupportedException();
        }

        public static SafeFreeCredentials? AcquireCredentialsHandle(SslAuthenticationOptions _1, bool _2)
        {
            return null;
        }

        public static ProtocolToken EncryptMessage(
            SafeDeleteSslContext securityContext,
            ReadOnlyMemory<byte> input,
            int headerSize,
            int trailerSize)
        {
            ProtocolToken token = default;
            Debug.Assert(input.Length > 0, $"{nameof(input.Length)} > 0 since {nameof(CanEncryptEmptyMessage)} is false");

            try
            {
                SafeSslHandle sslHandle = securityContext.SslContext;

                PAL_SSLStreamStatus ret = Interop.AndroidCrypto.SSLStreamWrite(sslHandle, input);
                SecurityStatusPalErrorCode statusCode = ret switch
                {
                    PAL_SSLStreamStatus.OK => SecurityStatusPalErrorCode.OK,
                    PAL_SSLStreamStatus.NeedData => SecurityStatusPalErrorCode.ContinueNeeded,
                    PAL_SSLStreamStatus.Renegotiate => SecurityStatusPalErrorCode.Renegotiate,
                    PAL_SSLStreamStatus.Closed => SecurityStatusPalErrorCode.ContextExpired,
                    _ => SecurityStatusPalErrorCode.InternalError
                };

                token.Status = new SecurityStatusPal(statusCode);
                securityContext.ReadPendingWrites(ref token);
            }
            catch (Exception e)
            {
                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, e);
            }

            return token;
        }

        public static SecurityStatusPal DecryptMessage(
            SafeDeleteSslContext securityContext,
            Span<byte> buffer,
            out int offset,
            out int count)
        {
            offset = 0;
            count = 0;

            try
            {
                SafeSslHandle sslHandle = securityContext.SslContext;

                securityContext.Write(buffer);

                PAL_SSLStreamStatus ret = Interop.AndroidCrypto.SSLStreamRead(sslHandle, buffer, out int read);
                if (ret == PAL_SSLStreamStatus.Error)
                    return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError);

                count = read;

                SecurityStatusPalErrorCode statusCode = ret switch
                {
                    PAL_SSLStreamStatus.OK => SecurityStatusPalErrorCode.OK,
                    PAL_SSLStreamStatus.NeedData => SecurityStatusPalErrorCode.OK,
                    PAL_SSLStreamStatus.Renegotiate => SecurityStatusPalErrorCode.Renegotiate,
                    PAL_SSLStreamStatus.Closed => SecurityStatusPalErrorCode.ContextExpired,
                    _ => SecurityStatusPalErrorCode.InternalError
                };

                return new SecurityStatusPal(statusCode);
            }
            catch (Exception e)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, e);
            }
        }

        public static ChannelBinding? QueryContextChannelBinding(
            SafeDeleteContext securityContext,
            ChannelBindingKind attribute)
        {
            if (attribute == ChannelBindingKind.Endpoint)
                return EndpointChannelBindingToken.Build(securityContext);

            // Android doesn't expose the Finished messages, so a Unique binding token cannot be built.
            // Return null for not supported kinds
            return null;
        }

        public static void QueryContextStreamSizes(
            SafeDeleteContext? securityContext,
            out StreamSizes streamSizes)
        {
            streamSizes = StreamSizes.Default;
        }

        public static void QueryContextConnectionInfo(
            SafeDeleteSslContext securityContext,
            ref SslConnectionInfo connectionInfo)
        {
            connectionInfo.UpdateSslConnectionInfo(securityContext.SslContext);
        }

        public static bool TryUpdateClintCertificate(
            SafeFreeCredentials? _1,
            SafeDeleteSslContext? _2,
            SslAuthenticationOptions _3)
        {
            return false;
        }

        private static ProtocolToken HandshakeInternal(
            SafeFreeCredentials credential,
            ref SafeDeleteSslContext? context,
            ReadOnlySpan<byte> inputBuffer,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            ProtocolToken token = default;
            try
            {
                SafeDeleteSslContext? sslContext = ((SafeDeleteSslContext?)context);

                if (context == null || context.IsInvalid)
                {
                    context = new SafeDeleteSslContext(sslAuthenticationOptions);
                    sslContext = context;
                }

                if (inputBuffer.Length > 0)
                {
                    sslContext!.Write(inputBuffer);
                }

                SafeSslHandle sslHandle = sslContext!.SslContext;

                PAL_SSLStreamStatus ret = Interop.AndroidCrypto.SSLStreamHandshake(sslHandle);
                SecurityStatusPalErrorCode statusCode = ret switch
                {
                    PAL_SSLStreamStatus.OK => SecurityStatusPalErrorCode.OK,
                    PAL_SSLStreamStatus.NeedData => SecurityStatusPalErrorCode.ContinueNeeded,
                    _ => SecurityStatusPalErrorCode.InternalError
                };

                Exception? validationException = sslContext?.SslStreamProxy.ValidationException;
                token.Status = new SecurityStatusPal(statusCode, validationException);
                sslContext!.ReadPendingWrites(ref token);
                return token;
            }
            catch (Exception exc)
            {
                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, exc);
                return token;
            }
        }

        public static SecurityStatusPal ApplyAlertToken(
            SafeDeleteContext? securityContext,
            TlsAlertType alertType,
            TlsAlertMessage alertMessage)
        {
            // There doesn't seem to be an exposed API for writing an alert.
            // The API seems to assume that all alerts are generated internally.
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }

        public static SecurityStatusPal ApplyShutdownToken(
            SafeDeleteSslContext securityContext)
        {
            SafeSslHandle sslHandle = securityContext.SslContext;


            bool success = Interop.AndroidCrypto.SSLStreamShutdown(sslHandle);
            if (success)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            }

            return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError);
        }
    }
}
