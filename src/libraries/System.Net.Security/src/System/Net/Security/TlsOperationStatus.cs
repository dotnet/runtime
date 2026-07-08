// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Net.Security
{
    /// <summary>
    /// Outcome of a non-blocking TLS operation on <see cref="TlsSession"/>.
    /// Provider-opaque; the same values apply across OpenSSL, Schannel, and the
    /// managed implementation.
    /// </summary>
    [Experimental(Experimentals.LowLevelTlsDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public enum TlsOperationStatus
    {
        /// <summary>The call made forward progress.</summary>
        Complete = 0,

        /// <summary>
        /// The destination buffer was too small for the pending output. Call the
        /// operation again with a larger buffer, or drain via
        /// <see cref="TlsBufferSession.DrainPendingOutput"/>.
        /// </summary>
        DestinationTooSmall = 1,

        /// <summary>
        /// The session needs more ciphertext from the peer to make progress.
        /// </summary>
        NeedMoreData = 2,

        /// <summary>
        /// The transport is gone or <c>close_notify</c> was received. Dispose the session.
        /// </summary>
        Closed = 3,

        /// <summary>
        /// The peer requested a client certificate. The caller supplies one (or
        /// <see langword="null"/> to decline) via
        /// <see cref="TlsSession.SetClientCertificateContext"/> and re-enters the handshake.
        /// </summary>
        CertificateRequested = 4,

        /// <summary>
        /// The peer presented a certificate and the TLS state machine has paused so the
        /// caller can validate it. Retrieve the peer certificate via
        /// <see cref="TlsSession.GetRemoteCertificate"/> (and any peer-sent intermediates
        /// via <see cref="TlsSession.GetRemoteCertificates"/>), perform validation - including
        /// any I/O such as AIA fetch or CRL/OCSP lookup - on any thread, and then record
        /// the result with
        /// <see cref="TlsSession.SetRemoteCertificateValidationResult(System.Net.Security.SslPolicyErrors)"/>.
        /// Callers that don't need custom validation logic can invoke
        /// <see cref="TlsSession.AcceptWithDefaultValidation"/> for the equivalent of
        /// <see cref="SslStream"/>'s default chain build plus user callback.
        /// </summary>
        NeedsCertificateValidation = 5,

        /// <summary>
        /// Server-side only. The peer's ClientHello has been received but the session
        /// has no resolved <see cref="TlsContext"/> yet (either none was assigned or the
        /// assigned context is a bootstrap without a server certificate). Inspect
        /// <see cref="TlsSession.ClientHelloInfo"/>, supply the resolved context via
        /// <see cref="TlsSession.SetContext"/>, and continue the handshake.
        /// </summary>
        NeedsTlsContext = 6,
    }
}
