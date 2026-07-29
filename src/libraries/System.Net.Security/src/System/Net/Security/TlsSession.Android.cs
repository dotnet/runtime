// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    // Android-only helpers for TlsSession. Populates the JavaProxy that
    // SafeDeleteSslContext requires and mirrors SslStream's platform trust manager
    // routing without depending on an SslStream instance.
    public abstract partial class TlsSession
    {
        // Wires a session-owned JavaProxy onto the per-session options bag so
        // the Android SafeDeleteSslContext can look it up during construction. The proxy
        // delegates back to VerifyRemoteCertificateForAndroid on this session — mirroring
        // the model SslStream uses via SslStream.Android.VerifyRemoteCertificate(IntPtr),
        // but keeping the JSSE bridge scoped to the session that created it.
        partial void InitializePlatformSpecificSessionState()
        {
            _options.SslStreamProxy = new SslStream.JavaProxy(VerifyRemoteCertificateForAndroid);
        }

        // Invoked synchronously from Android's DotnetProxyTrustManager back-channel while the
        // JSSE SSLEngine is in the middle of processing the peer's certificate message.
        // The JSSE trust manager expects a synchronous bool decision, so we must run cert
        // validation inline here — TlsSession's async NeedsCertificateValidation model does
        // not apply on Android for the same reason it does not apply to SslStream on Android.
        private SslStream.JavaProxy.RemoteCertificateValidationResult VerifyRemoteCertificateForAndroid(IntPtr platformValidationError)
        {
            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
            if (ShouldRespectPlatformValidation() && platformValidationError != IntPtr.Zero)
            {
                sslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;

                // See SslStream.Android.VerifyRemoteCertificate for the rationale behind
                // surfacing Android's textual rejection reason via NetEventSource.
                if (NetEventSource.Log.IsEnabled())
                {
                    string? validationError = Interop.AndroidCrypto.GetPlatformValidationError(platformValidationError);
                    NetEventSource.Error(this, $"The Android platform trust manager rejected the remote certificate chain: {validationError}");
                }
            }

            ProtocolToken alertToken = default;
            X509Chain? chain = null;

            try
            {
                X509Certificate2? candidate = CertificateValidationPal.GetRemoteCertificate(_securityContext, ref chain, _options.CertificateChainPolicy);

                bool isValid = SslStream.VerifyRemoteCertificateCore(
                    sender: this,
                    _options,
                    _securityContext,
                    ref _remoteCertificate,
                    ref _connectionInfo,
                    candidate,
                    chain,
                    _options.CertificateContext?.Trust,
                    ref alertToken,
                    ref sslPolicyErrors,
                    out X509ChainStatusFlags chainStatus);

                return new SslStream.JavaProxy.RemoteCertificateValidationResult
                {
                    IsValid = isValid,
                    SslPolicyErrors = sslPolicyErrors,
                    ChainStatus = chainStatus,
                    AlertToken = alertToken,
                };
            }
            finally
            {
                // Mirror SslStream's chain-cleanup: dispose the chain elements that
                // VerifyRemoteCertificateCore populated (unless the caller has a user
                // callback that may retain them), matching the behavior of SslStream's
                // VerifyRemoteCertificate wrapper path.
                if (chain is not null && _options.CertValidationDelegate is null)
                {
                    for (int i = 0; i < chain.ChainElements.Count; i++)
                    {
                        chain.ChainElements[i].Certificate.Dispose();
                    }
                    chain.Dispose();
                }
            }
        }

        private bool ShouldRespectPlatformValidation()
        {
            // Mirrors SslStream.Android.ShouldRespectPlatformValidation: platform trust
            // wins by default, but explicit managed custom trust remains authoritative
            // and is not projected into the Android trust manager.
            return _options.CertificateChainPolicy is not null
                ? _options.CertificateChainPolicy.TrustMode != X509ChainTrustMode.CustomRootTrust
                : _options.CertificateContext?.Trust is null;
        }
    }
}
