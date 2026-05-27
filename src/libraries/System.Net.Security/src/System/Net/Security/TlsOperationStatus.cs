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
    }
}
