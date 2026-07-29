// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Security
{
    // Android-only helpers for TlsSession. Populates the JavaProxy that
    // SafeDeleteSslContext requires. Uses an accept-and-defer trust manager
    // strategy so TlsSession's async validation model (NeedsCertificateValidation
    // + AcceptWithDefaultValidation / SetRemoteCertificateValidationResult)
    // works on Android the same way it does on OpenSSL 1.1.x and SecureTransport,
    // rather than blocking the JSSE trust manager thread on managed validation.
    public abstract partial class TlsSession
    {
        // Wires a session-owned JavaProxy onto the per-session options bag so
        // the Android SafeDeleteSslContext can look it up during construction.
        // The proxy's validator always accepts at the JSSE layer; the actual
        // validation decision is deferred to the standard OnHandshakeCompleted
        // path via CaptureRemoteCertificateForExternalValidation.
        partial void InitializePlatformSpecificSessionState()
        {
            _options.SslStreamProxy = new SslStream.JavaProxy(AcceptAndDeferPlatformValidation);
        }

        // Invoked synchronously from Android's DotnetProxyTrustManager back-channel
        // while the JSSE SSLEngine is processing the peer's certificate message.
        //
        // Always returns IsValid=true so the handshake progresses past the
        // trust-manager checkpoint. TlsSession's shared OnHandshakeCompleted path
        // then captures the peer certificate into _externalPendingCert and
        // suspends the session via NeedsCertificateValidation, giving the caller
        // the same async validation experience available on Windows / Linux /
        // macOS. Callers that want SslStream-style synchronous validation with
        // the platform trust store should either:
        //   * set SslClientAuthenticationOptions.RemoteCertificateValidationCallback
        //     and call AcceptWithDefaultValidation() when the session suspends
        //     (mirrors SslStream's callback semantics), or
        //   * use SslStream directly (which continues to invoke JSSE
        //     synchronously via its own JavaProxy in SslStream.Android.cs).
        //
        // This mirrors the accept-and-defer branch already used by the OpenSSL
        // 1.1.x CertVerifyCallback and by SecureTransport on macOS, where the
        // handshake completes on the wire before the caller records a verdict
        // and any rejection is surfaced through _externalValidationFault on the
        // next Read/Write.
        private static SslStream.JavaProxy.RemoteCertificateValidationResult AcceptAndDeferPlatformValidation(IntPtr platformValidationError)
        {
            _ = platformValidationError;
            return new SslStream.JavaProxy.RemoteCertificateValidationResult
            {
                IsValid = true,
                SslPolicyErrors = SslPolicyErrors.None,
                ChainStatus = default,
                AlertToken = default,
            };
        }
    }
}
