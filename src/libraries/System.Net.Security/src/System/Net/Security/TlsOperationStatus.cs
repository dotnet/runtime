// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Net.Security
{
    /// <summary>
    /// Represents the outcome of a TLS operation.
    /// </summary>
    [Experimental(Experimentals.LowLevelTlsDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public enum TlsOperationStatus
    {
        /// <summary>The call made forward progress.</summary>
        Complete = 0,

        /// <summary>The operation could not complete because output could not be delivered.</summary>
        /// <remarks>
        /// On the buffered APIs (<see cref="TlsBufferSession"/>) this means the caller's
        /// destination span was too small for the pending output; call the operation
        /// again with a larger destination, or drain via
        /// <see cref="TlsBufferSession.DrainPendingOutput"/>. On the socket-bound APIs
        /// (<see cref="TlsSocketSession"/>) this means the underlying socket returned
        /// <see cref="System.Net.Sockets.SocketError.WouldBlock"/> mid-write; retry the
        /// operation once the socket is writable.
        /// </remarks>
        DestinationTooSmall = 1,

        /// <summary>The session needs more data from the peer to make progress.</summary>
        NeedMoreData = 2,

        /// <summary>The transport is gone or <c>close_notify</c> was received.</summary>
        Closed = 3,

        /// <summary>The peer requested a client certificate.</summary>
        /// <remarks>
        /// Specify a client certificate via <see cref="TlsSession.SetClientCertificateContext"/>.
        /// If you do not wish to provide a certificate, pass <see langword="null" />.
        /// </remarks>
        CertificateRequested = 4,

        /// <summary>The peer presented a certificate and it must be explicitly accepted or rejected.</summary>
        /// <remarks>
        /// The peer certificate can be retrieved via
        /// <see cref="TlsSession.GetRemoteCertificate"/> (and any peer-sent intermediates
        /// via <see cref="TlsSession.GetRemoteCertificates"/>).
        ///
        /// Built-in validation, or validation in conjunction with a provided
        /// <see cref="RemoteCertificateValidationCallback"/>, is
        /// performed by <see cref="TlsSession.AcceptWithDefaultValidation"/>.
        ///
        /// If performing fully custom validation, indicate acceptance
        /// (<see cref="SslPolicyErrors.None" />) or rejection (any other value) by calling
        /// <see cref="TlsSession.SetRemoteCertificateValidationResult(System.Net.Security.SslPolicyErrors)"/>.
        /// </remarks>
        NeedsCertificateValidation = 5,

        /// <summary>
        /// The peer's ClientHello has been received but the session
        /// has no resolved <see cref="TlsContext"/>.
        /// </summary>
        /// <seealso cref="TlsSession.ClientHelloInfo" />
        /// <seealso cref="TlsSession.SetContext" />
        NeedsTlsContext = 6,
    }
}
