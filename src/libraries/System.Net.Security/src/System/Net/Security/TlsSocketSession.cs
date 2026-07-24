// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace System.Net.Security
{
    /// <summary>
    /// Non-blocking TLS session bound to a caller-supplied non-blocking
    /// <see cref="SafeSocketHandle"/>. The session performs its own ciphertext
    /// I/O on the socket via <see cref="Handshake"/>, <see cref="Read"/>,
    /// <see cref="Write"/>, <see cref="Shutdown"/>, and
    /// <see cref="RequestClientCertificate"/>. The socket must be configured
    /// non-blocking; behavior on a blocking socket is unspecified.
    /// </summary>
    /// <remarks>
    /// The session takes ownership of the supplied socket and disposes it with
    /// the session. Call <see cref="TlsSession.SetContext"/> with a client or
    /// server <see cref="TlsContext"/> before invoking any operation.
    /// </remarks>
    [Experimental(Experimentals.LowLevelTlsDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed class TlsSocketSession : TlsSession
    {
        private readonly SafeSocketHandle _socket;

        public TlsSocketSession(SafeSocketHandle socket)
        {
            ArgumentNullException.ThrowIfNull(socket);
            _socket = socket;
        }

        internal override void OnContextInitialized()
        {
            AttachSocket(_socket);
        }

        /// <summary>The socket the session is bound to. Owned by the session.</summary>
        public SafeSocketHandle Socket => _socket;

        /// <summary>Drives the TLS handshake to completion, sending and receiving via the socket.</summary>
        public TlsOperationStatus Handshake() => HandshakeSocketCore();

        /// <summary>Reads decrypted application bytes from the socket into <paramref name="buffer"/>.</summary>
        public TlsOperationStatus Read(Span<byte> buffer, out int bytesRead)
            => ReadSocketCore(buffer, out bytesRead);

        /// <summary>Encrypts and sends <paramref name="buffer"/> as one or more TLS records over the socket.</summary>
        public TlsOperationStatus Write(ReadOnlySpan<byte> buffer, out int bytesWritten)
            => WriteSocketCore(buffer, out bytesWritten);

        /// <summary>Sends a TLS <c>close_notify</c> alert on the socket.</summary>
        public TlsOperationStatus Shutdown() => ShutdownSocketCore();

        /// <summary>Server-side only. Sends a <c>CertificateRequest</c> on the socket for TLS 1.3 post-handshake authentication.</summary>
        public TlsOperationStatus RequestClientCertificate() => RequestClientCertificateSocketCore();
    }
}
