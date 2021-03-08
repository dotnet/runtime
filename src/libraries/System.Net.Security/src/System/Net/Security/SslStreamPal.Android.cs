// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;

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

        public static SecurityStatusPal AcceptSecurityContext(
            ref SafeFreeCredentials credential,
            ref SafeDeleteSslContext? context,
            ReadOnlySpan<byte> inputBuffer,
            ref byte[]? outputBuffer,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            return HandshakeInternal(credential, ref context, inputBuffer, ref outputBuffer, sslAuthenticationOptions);
        }

        public static SecurityStatusPal InitializeSecurityContext(
            ref SafeFreeCredentials credential,
            ref SafeDeleteSslContext? context,
            string? targetName,
            ReadOnlySpan<byte> inputBuffer,
            ref byte[]? outputBuffer,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            return HandshakeInternal(credential, ref context, inputBuffer, ref outputBuffer, sslAuthenticationOptions);
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(
            SslStreamCertificateContext? certificateContext,
            SslProtocols protocols,
            EncryptionPolicy policy,
            bool isServer)
        {
            return new SafeFreeSslCredentials(certificateContext, protocols, policy);
        }

        internal static byte[]? GetNegotiatedApplicationProtocol(SafeDeleteContext? context)
        {
            if (context == null)
                return null;

            throw new NotImplementedException(nameof(GetNegotiatedApplicationProtocol));
        }

        public static SecurityStatusPal EncryptMessage(
            SafeDeleteContext securityContext,
            ReadOnlyMemory<byte> input,
            int headerSize,
            int trailerSize,
            ref byte[] output,
            out int resultSize)
        {
            resultSize = 0;
            Debug.Assert(input.Length > 0, $"{nameof(input.Length)} > 0 since {nameof(CanEncryptEmptyMessage)} is false");

            throw new NotImplementedException(nameof(EncryptMessage));
        }

        public static SecurityStatusPal DecryptMessage(
            SafeDeleteContext securityContext,
            byte[] buffer,
            ref int offset,
            ref int count)
        {
            throw new NotImplementedException(nameof(DecryptMessage));
        }

        public static ChannelBinding? QueryContextChannelBinding(
            SafeDeleteContext securityContext,
            ChannelBindingKind attribute)
        {
            if (attribute == ChannelBindingKind.Endpoint)
                return EndpointChannelBindingToken.Build(securityContext);

            throw new NotImplementedException(nameof(QueryContextChannelBinding));
        }

        public static void QueryContextStreamSizes(
            SafeDeleteContext? securityContext,
            out StreamSizes streamSizes)
        {
            streamSizes = StreamSizes.Default;
        }

        public static void QueryContextConnectionInfo(
            SafeDeleteContext securityContext,
            out SslConnectionInfo connectionInfo)
        {
            connectionInfo = new SslConnectionInfo(((SafeDeleteSslContext)securityContext).SslContext);
        }

        private static SecurityStatusPal HandshakeInternal(
            SafeFreeCredentials credential,
            ref SafeDeleteSslContext? context,
            ReadOnlySpan<byte> inputBuffer,
            ref byte[]? outputBuffer,
            SslAuthenticationOptions sslAuthenticationOptions)
        {
            Debug.Assert(!credential.IsInvalid);

            try
            {
                SafeDeleteSslContext? sslContext = ((SafeDeleteSslContext?)context);

                if ((context == null) || context.IsInvalid)
                {
                    context = new SafeDeleteSslContext((credential as SafeFreeSslCredentials)!, sslAuthenticationOptions);
                    sslContext = context;
                }

                if (inputBuffer.Length > 0)
                {
                    sslContext!.Write(inputBuffer);
                }

                SafeSslHandle sslHandle = sslContext!.SslContext;

                // Do handshake
                // Interop.AndroidCrypto.SSLStreamHandshake

                outputBuffer = sslContext.ReadPendingWrites();

                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            }
            catch (Exception exc)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, exc);
            }
        }

        public static SecurityStatusPal ApplyAlertToken(
            ref SafeFreeCredentials? credentialsHandle,
            SafeDeleteContext? securityContext,
            TlsAlertType alertType,
            TlsAlertMessage alertMessage)
        {
            // There doesn't seem to be an exposed API for writing an alert.
            // The API seems to assume that all alerts are generated internally.
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }

        public static SecurityStatusPal ApplyShutdownToken(
            ref SafeFreeCredentials? credentialsHandle,
            SafeDeleteContext securityContext)
        {
            SafeDeleteSslContext sslContext = ((SafeDeleteSslContext)securityContext);
            SafeSslHandle sslHandle = sslContext.SslContext;


            // bool success = Interop.AndroidCrypto.SslShutdown(sslHandle);
            bool success = true;
            if (success)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            }

            return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError);
        }
    }
}
