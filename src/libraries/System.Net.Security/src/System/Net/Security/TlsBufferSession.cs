// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;

namespace System.Net.Security
{
    /// <summary>
    /// Non-blocking TLS state machine driven by caller-supplied byte spans.
    /// The session object performs no I/O; the caller is responsible for
    /// performing I/O, such as transmitting the output of <see cref="Write"/>.
    /// </summary>
    /// <remarks>
    /// A newly-constructed instance has no <see cref="TlsContext"/>. Call
    /// <see cref="TlsSession.SetContext"/> with a client or server context before
    /// invoking any operation. Server-side deferred flow (SNI-driven context
    /// selection) is supported by passing an empty
    /// <see cref="SslServerAuthenticationOptions"/> to <c>TlsContext.CreateServer</c>;
    /// the first <c>Handshake</c> call then suspends on
    /// <see cref="TlsOperationStatus.NeedsTlsContext"/> so the caller can supply the
    /// resolved per-tenant context via <see cref="TlsSession.SetContext"/>.
    /// </remarks>
    [Experimental(Experimentals.LowLevelTlsDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed class TlsBufferSession : TlsSession
    {
        /// <summary>Drives the TLS handshake forward using caller-supplied ciphertext.</summary>
        /// <param name="source">Bytes received from the peer.</param>
        /// <param name="destination">Buffer to receive any handshake bytes that must be sent to the peer.</param>
        /// <param name="bytesConsumed">The number of bytes read from <paramref name="source"/>.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination"/>.</param>
        /// <returns>The outcome of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
        /// <exception cref="InvalidOperationException">A <see cref="TlsContext"/> has not been assigned via <see cref="TlsSession.SetContext"/>.</exception>
        /// <exception cref="AuthenticationException">The handshake failed.</exception>
        public TlsOperationStatus Handshake(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
            => HandshakeBufferedCore(source, destination, out bytesConsumed, out bytesWritten);

        /// <summary>Encrypts application-plaintext into ciphertext records.</summary>
        /// <param name="source">Plaintext bytes to encrypt.</param>
        /// <param name="destination">Buffer to receive the produced ciphertext records.</param>
        /// <param name="bytesConsumed">The number of plaintext bytes read from <paramref name="source"/>.</param>
        /// <param name="bytesWritten">The number of ciphertext bytes written to <paramref name="destination"/>.</param>
        /// <returns>The outcome of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The handshake has not yet completed.</exception>
        public TlsOperationStatus Write(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
            => WriteBufferedCore(source, destination, out bytesConsumed, out bytesWritten);

        /// <summary>Decrypts ciphertext records into application-plaintext.</summary>
        /// <param name="source">Ciphertext bytes received from the peer.</param>
        /// <param name="destination">Buffer to receive the decrypted plaintext.</param>
        /// <param name="bytesConsumed">The number of ciphertext bytes read from <paramref name="source"/>.</param>
        /// <param name="bytesWritten">The number of plaintext bytes written to <paramref name="destination"/>.</param>
        /// <returns>The outcome of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The handshake has not yet completed.</exception>
        public TlsOperationStatus Read(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
            => ReadBufferedCore(source, destination, out bytesConsumed, out bytesWritten);

        /// <summary>Initiates a TLS <c>close_notify</c> alert; writes the alert record into <paramref name="destination"/>.</summary>
        /// <param name="destination">Buffer to receive the <c>close_notify</c> alert record.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination"/>.</param>
        /// <returns>The outcome of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
        public TlsOperationStatus Shutdown(Span<byte> destination, out int bytesWritten)
            => ShutdownBufferedCore(destination, out bytesWritten);

        /// <summary>Drains any staged pending output (handshake fragments, alerts, encrypted records) into <paramref name="destination"/>.</summary>
        /// <param name="destination">Buffer to receive the pending ciphertext.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination"/>.</param>
        /// <returns>The outcome of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
        public TlsOperationStatus DrainPendingOutput(Span<byte> destination, out int bytesWritten)
            => DrainPendingOutputCore(destination, out bytesWritten);

        /// <summary>Server-side only. Sends a <c>CertificateRequest</c> to the peer as part of TLS 1.3 post-handshake authentication.</summary>
        /// <param name="destination">Buffer to receive the <c>CertificateRequest</c> record.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination"/>.</param>
        /// <returns>The outcome of the operation.</returns>
        /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The session is client-side, the handshake has not yet completed, or the current session is not TLS 1.3.</exception>
        /// <exception cref="PlatformNotSupportedException">The current platform does not support post-handshake authentication.</exception>
        public TlsOperationStatus RequestClientCertificate(Span<byte> destination, out int bytesWritten)
            => RequestClientCertificateBufferedCore(destination, out bytesWritten);
    }
}
