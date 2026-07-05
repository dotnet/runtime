// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Security
{
    /// <summary>
    /// Outcome of a non-blocking TLS operation on <see cref="TlsSession"/>.
    /// Provider-opaque; the same values apply across OpenSSL, Schannel, and the
    /// managed implementation.
    /// </summary>
    public enum TlsOperationStatus
    {
        /// <summary>The call made forward progress.</summary>
        Complete = 0,

        /// <summary>The session needs more ciphertext from the peer to make progress.</summary>
        WantRead = 1,

        /// <summary>
        /// The session has ciphertext to send. Call <see cref="TlsSession.DrainPendingOutput"/>
        /// (and send the bytes to the peer) before retrying the operation.
        /// </summary>
        WantWrite = 2,

        /// <summary>
        /// The transport is gone or <c>close_notify</c> was received. Dispose the session.
        /// </summary>
        Closed = 3,

        /// <summary>
        /// The session requires a client certificate (or a new selection) before it can
        /// proceed. The caller should update <see cref="TlsContext"/> options as needed
        /// and call <see cref="TlsSession.ProcessHandshake"/> again with empty input.
        /// </summary>
        WantCredentials = 4,

        /// <summary>
        /// The peer presented a certificate and the TLS state machine has paused so the
        /// caller can validate it. The caller should retrieve the peer certificate via
        /// <see cref="TlsSession.GetRemoteCertificate"/> (and any peer-sent intermediates
        /// via <see cref="TlsSession.GetRemoteCertificates"/>), perform validation —
        /// including any I/O such as AIA fetch or CRL/OCSP lookup — on any thread, and
        /// then record the result with
        /// <see cref="TlsSession.SetRemoteCertificateValidationResult(System.Net.Security.SslPolicyErrors)"/>.
        /// Callers that don't need custom validation logic can invoke
        /// <see cref="TlsSession.AcceptWithDefaultValidation"/> for the equivalent of
        /// <see cref="SslStream"/>'s default chain build plus user callback. Until the
        /// result is set, <see cref="TlsSession.Encrypt"/> and <see cref="TlsSession.Decrypt"/> throw.
        /// </summary>
        NeedsCertificateValidation = 5,

        /// <summary>
        /// Server-side only. The peer's ClientHello has been received but no server options
        /// were supplied when the <see cref="TlsContext"/> was created. Inspect
        /// <see cref="TlsSession.ClientHelloInfo"/>, supply the resolved options via
        /// <see cref="TlsSession.SetServerContext"/>, and call
        /// <see cref="TlsSession.ProcessHandshake"/> again with the same input.
        /// </summary>
        NeedsServerOptions = 6,
    }
}
