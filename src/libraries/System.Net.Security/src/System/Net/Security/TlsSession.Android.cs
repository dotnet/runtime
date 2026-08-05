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
            _options.SslStreamProxy = new SslStream.JavaProxy(AcceptAndDeferPlatformValidation);
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
        // surfaced later through AcceptWithDefaultValidation.
        private SslStream.JavaProxy.RemoteCertificateValidationResult AcceptAndDeferPlatformValidation(IntPtr platformValidationError)
        {
            if (platformValidationError != IntPtr.Zero && ShouldRespectPlatformValidation())
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

        private bool ShouldRespectPlatformValidation()
        {
            return _options.CertificateChainPolicy is not null
                ? _options.CertificateChainPolicy.TrustMode != X509ChainTrustMode.CustomRootTrust
                : _options.CertificateContext?.Trust is null;
        }
    }
}
