// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication.ExtendedProtection;

#pragma warning disable IDE0060

namespace System.Net.Security
{
    // SslStreamPal for platforms without a TLS implementation (browser, WASI).
    // Every handshake entry point throws PlatformNotSupportedException; a platform TLS
    // backend (for example wasi:tls) can replace this file without touching SslStream.
    internal static class SslStreamPal
    {
        public static Exception GetException(SecurityStatusPal status) =>
            status.Exception ?? new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        internal const bool StartMutualAuthAsAnonymous = false;
        internal const bool CertValidationInCallback = false;
        internal const bool CanEncryptEmptyMessage = false;
        internal const bool CanGenerateCustomAlerts = false;

        internal static bool CanGenerateCustomAlertsForContext(SafeDeleteContext? _) => CanGenerateCustomAlerts;

        public static void VerifyPackageInfo()
        {
        }

        public static SecurityStatusPal SelectApplicationProtocol(
            SafeFreeCredentials? credentialsHandle,
            SafeDeleteSslContext? context,
            SslAuthenticationOptions sslAuthenticationOptions,
            ReadOnlySpan<byte> clientProtocols) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public static ProtocolToken AcceptSecurityContext(
            ref SafeFreeCredentials credential,
            ref SafeDeleteSslContext? context,
            ReadOnlySpan<byte> inputBuffer,
            out int consumed,
            SslAuthenticationOptions sslAuthenticationOptions) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public static ProtocolToken InitializeSecurityContext(
            ref SafeFreeCredentials credential,
            ref SafeDeleteSslContext? context,
            string? targetName,
            ReadOnlySpan<byte> inputBuffer,
            out int consumed,
            SslAuthenticationOptions sslAuthenticationOptions) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public static ProtocolToken Renegotiate(
            ref SafeFreeCredentials? credentialsHandle,
            ref SafeDeleteSslContext? context,
            SslAuthenticationOptions sslAuthenticationOptions) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public static SafeFreeCredentials? AcquireCredentialsHandle(SslAuthenticationOptions sslAuthenticationOptions, bool newCredentialsRequested) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public static ProtocolToken EncryptMessage(
            SafeDeleteSslContext securityContext,
            ReadOnlyMemory<byte> input,
            int headerSize,
            int trailerSize) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public static SecurityStatusPal DecryptMessage(
            SafeDeleteSslContext securityContext,
            Span<byte> encrypted,
            Span<byte> destination,
            out int bytesWritten,
            out int leftoverOffset,
            out int leftoverLength) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public static ChannelBinding? QueryContextChannelBinding(
            SafeDeleteContext securityContext,
            ChannelBindingKind attribute) =>
            null;

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
        }

        public static bool TryUpdateClintCertificate(
            SafeFreeCredentials? credentialsHandle,
            SafeDeleteSslContext? context,
            SslAuthenticationOptions sslAuthenticationOptions) =>
            false;

        public static SecurityStatusPal ApplyAlertToken(
            SafeDeleteContext? securityContext,
            TlsAlertType alertType,
            TlsAlertMessage alertMessage) =>
            new SecurityStatusPal(SecurityStatusPalErrorCode.OK);

        public static SecurityStatusPal ApplyShutdownToken(
            SafeDeleteSslContext securityContext) =>
            new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
    }
}
