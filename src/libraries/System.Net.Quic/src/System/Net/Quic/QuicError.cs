// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic;

/// <summary>
/// Defines the various error conditions for <see cref="QuicListener"/>, <see cref="QuicConnection"/> and <see cref="QuicStream"/> operations.
/// </summary>
public enum QuicError
{
    /// <summary>
    /// No error.
    /// </summary>
    Success,

    /// <summary>
    /// An internal implementation error has occurred.
    /// </summary>
    InternalError,

    /// <summary>
    /// The connection was aborted by the peer. This error is associated with an application-level error code.
    /// </summary>
    ConnectionAborted,

    /// <summary>
    /// The read or write direction of the stream was aborted by the peer. This error is associated with an application-level error code.
    /// </summary>
    StreamAborted,

    /// <summary>
    /// The connection timed out waiting for a response from the peer.
    /// </summary>
    ConnectionTimeout = 6,

    /// <summary>
    /// The server refused the connection.
    /// </summary>
    ConnectionRefused = 8,

    /// <summary>
    /// A version negotiation error was encountered.
    /// </summary>
    VersionNegotiationError,

    /// <summary>
    /// The connection timed out from inactivity.
    /// </summary>
    ConnectionIdle,

    /// <summary>
    /// The operation has been aborted.
    /// </summary>
    OperationAborted = 12,

    /// <summary>
    /// Another QUIC listener is already listening on one of the requested application protocols on the same port.
    /// </summary>
    AlpnInUse,

    /// <summary>
    /// Operation failed because peer transport error occurred.
    /// </summary>
    TransportError,

    /// <summary>
    /// An error occurred in user provided callback.
    /// </summary>
    CallbackError,
}
