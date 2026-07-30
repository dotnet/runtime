// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    // Android-only helpers for TlsSession. JSSE's X509TrustManager is a synchronous
    // decision point with no retry-verify equivalent, so we accept-and-defer at the
    // trust-manager checkpoint (like OpenSSL 1.1.x / SecureTransport) and let the
    // caller drive validation via NeedsCertificateValidation. We still record the
    // platform verdict so AcceptWithDefaultValidation can honor it, matching
    // SslStream.Android's ShouldRespectPlatformValidation behavior.
    public abstract partial class TlsSession
    {
        private bool _platformChainRejected;

        partial void InitializePlatformSpecificSessionState()
        {
            _options.SslStreamProxy = new SslStream.JavaProxy(AcceptAndDeferPlatformValidation);
        }

        partial void SeedPlatformValidationErrors(ref SslPolicyErrors sslPolicyErrors)
        {
            if (_platformChainRejected && ShouldRespectPlatformValidation())
            {
                sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }
        }

        // Invoked synchronously from Android's DotnetProxyTrustManager while JSSE processes
        // the peer's certificate message. Always accepts so the handshake progresses; the
        // rejection reason (if any) is recorded on the session and surfaced later through
        // AcceptWithDefaultValidation or observable via _platformChainRejected.
        private SslStream.JavaProxy.RemoteCertificateValidationResult AcceptAndDeferPlatformValidation(IntPtr platformValidationError)
        {
            if (platformValidationError != IntPtr.Zero)
            {
                _platformChainRejected = true;

                if (NetEventSource.Log.IsEnabled())
                {
                    string? validationError = Interop.AndroidCrypto.GetPlatformValidationError(platformValidationError);
                    NetEventSource.Error(this, $"The Android platform trust manager rejected the remote certificate chain: {validationError}");
                }
            }

            return new SslStream.JavaProxy.RemoteCertificateValidationResult
            {
                IsValid = true,
                SslPolicyErrors = SslPolicyErrors.None,
                ChainStatus = default,
                AlertToken = default,
            };
        }

        // Mirrors SslStream.Android's ShouldRespectPlatformValidation: a caller that
        // brings its own trust anchors (CustomRootTrust or CertificateContext.Trust)
        // has taken responsibility for validation, so the OS verdict is ignored.
        private bool ShouldRespectPlatformValidation()
        {
            return _options.CertificateChainPolicy is not null
                ? _options.CertificateChainPolicy.TrustMode != X509ChainTrustMode.CustomRootTrust
                : _options.CertificateContext?.Trust is null;
        }
    }
}
