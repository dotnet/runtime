// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public abstract partial class TlsSession
    {
        private bool _platformChainRejected;

        partial void InitializePlatformSpecificSessionState()
        {
            // In wedge mode the options bag is shared with SslStream, which installs its own
            // proxy in its constructor. Only supply one when the bag does not already have it,
            // so SslStream's callback routing stays intact and its JavaProxy is not leaked.
            _options.SslStreamProxy ??= new SslStream.JavaProxy(AcceptAndDeferPlatformValidation);
        }

        partial void SeedPlatformValidationErrors(ref SslPolicyErrors sslPolicyErrors)
        {
            if (_platformChainRejected)
            {
                sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }
        }

        // Invoked synchronously from Android's DotnetProxyTrustManager. Always accepts so
        // the handshake progresses; the platform verdict (if respected) is recorded and
        // surfaced later through AcceptWithDefaultValidation. The verdict is assigned rather
        // than latched so a later validation (e.g. renegotiation with a different chain) is
        // not tainted by an earlier rejection.
        private SslStream.JavaProxy.RemoteCertificateValidationResult AcceptAndDeferPlatformValidation(IntPtr platformValidationError)
        {
            bool rejected = platformValidationError != IntPtr.Zero && ShouldRespectPlatformValidation();
            _platformChainRejected = rejected;

            if (rejected && NetEventSource.Log.IsEnabled())
            {
                string? validationError = Interop.AndroidCrypto.GetPlatformValidationError(platformValidationError);
                NetEventSource.Error(this, $"The Android platform trust manager rejected the remote certificate chain: {validationError}");
            }

            return new SslStream.JavaProxy.RemoteCertificateValidationResult
            {
                IsValid = true,
                SslPolicyErrors = SslPolicyErrors.None,
                ChainStatus = default,
                AlertToken = default,
            };
        }

        private bool ShouldRespectPlatformValidation()
        {
            return _options.CertificateChainPolicy is not null
                ? _options.CertificateChainPolicy.TrustMode != X509ChainTrustMode.CustomRootTrust
                : _options.CertificateContext?.Trust is null;
        }
    }
}
