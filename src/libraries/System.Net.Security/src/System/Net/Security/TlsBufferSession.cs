// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Net.Security
{
    /// <summary>
    /// Non-blocking TLS state machine driven by caller-supplied byte spans.
    /// The session performs no I/O; the caller feeds ciphertext in and drains
    /// ciphertext out via the buffered <see cref="Handshake"/>, <see cref="Read"/>,
    /// <see cref="Write"/>, <see cref="Shutdown"/>, and <see cref="DrainPendingOutput"/>
    /// methods.
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
        public TlsBufferSession()
        {
        }

        /// <summary>Drives the TLS handshake forward using caller-supplied ciphertext.</summary>
        public TlsOperationStatus Handshake(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
            => HandshakeBufferedCore(source, destination, out bytesConsumed, out bytesWritten);

        /// <summary>Encrypts application-plaintext into ciphertext records.</summary>
        public TlsOperationStatus Write(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
            => WriteBufferedCore(source, destination, out bytesConsumed, out bytesWritten);

        /// <summary>Decrypts ciphertext records into application-plaintext.</summary>
        public TlsOperationStatus Read(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
            => ReadBufferedCore(source, destination, out bytesConsumed, out bytesWritten);

        /// <summary>Initiates a TLS <c>close_notify</c> alert; writes the alert record into <paramref name="ciphertext"/>.</summary>
        public TlsOperationStatus Shutdown(Span<byte> ciphertext, out int bytesWritten)
            => ShutdownBufferedCore(ciphertext, out bytesWritten);

        /// <summary>Drains any staged pending output (handshake fragments, alerts, encrypted records) into <paramref name="ciphertext"/>.</summary>
        public TlsOperationStatus DrainPendingOutput(Span<byte> ciphertext, out int bytesWritten)
            => DrainPendingOutputCore(ciphertext, out bytesWritten);

        /// <summary>Server-side only. Sends a <c>CertificateRequest</c> to the peer as part of TLS 1.3 post-handshake authentication.</summary>
        public TlsOperationStatus RequestClientCertificate(Span<byte> ciphertext, out int bytesWritten)
            => RequestClientCertificateBufferedCore(ciphertext, out bytesWritten);
    }
}
