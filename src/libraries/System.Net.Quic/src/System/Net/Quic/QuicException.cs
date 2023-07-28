﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.Quic;

/// <summary>
/// The exception that is thrown when a QUIC error occurs.
/// </summary>
public sealed class QuicException : IOException
{
    /// <summary>
    /// Initializes a new instance of the <see cref='QuicException'/> class.
    /// </summary>
    /// <param name="error">The error associated with the exception.</param>
    /// <param name="applicationErrorCode">The application protocol error code associated with the error.</param>
    /// <param name="message">The message for the exception.</param>
    public QuicException(QuicError error, long? applicationErrorCode, string message)
        : this(error, applicationErrorCode, null, message, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref='QuicException'/> class.
    /// </summary>
    /// <param name="error">The error associated with the exception.</param>
    /// <param name="applicationErrorCode">The application protocol error code associated with the error.</param>
    /// <param name="transportErrorCode">The transport protocol error code associated with the error.</param>
    /// <param name="message">The message for the exception.</param>
    internal QuicException(QuicError error, long? applicationErrorCode, long? transportErrorCode, string message)
        : this(error, applicationErrorCode, transportErrorCode, message, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref='QuicException'/> class.
    /// </summary>
    /// <param name="error">The error associated with the exception.</param>
    /// <param name="applicationErrorCode">The application protocol error code associated with the error.</param>
    /// <param name="message">The message for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    internal QuicException(QuicError error, long? applicationErrorCode, string message, Exception? innerException)
        : this(error, applicationErrorCode, null, message, innerException)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref='QuicException'/> class.
    /// </summary>
    /// <param name="error">The error associated with the exception.</param>
    /// <param name="applicationErrorCode">The application protocol error code associated with the error.</param>
    /// <param name="transportErrorCode">The transport protocol error code associated with the error.</param>
    /// <param name="message">The message for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    internal QuicException(QuicError error, long? applicationErrorCode, long? transportErrorCode, string message, Exception? innerException)
        : base(message, innerException)
    {
        QuicError = error;
        ApplicationErrorCode = applicationErrorCode;
        TransportErrorCode = transportErrorCode;
    }

    /// <summary>
    /// Gets the error which is associated with this exception.
    /// </summary>
    public QuicError QuicError { get; }

    /// <summary>
    /// The application protocol error code associated with the error.
    /// </summary>
    /// <remarks>
    /// This property contains the error code set by the application layer when closing the connection (<see cref="QuicError.ConnectionAborted"/>) or closing a read/write direction of a QUIC stream (<see cref="QuicError.StreamAborted"/>). Contains null for all other errors.
    /// </remarks>
    public long? ApplicationErrorCode { get; }

    /// <summary>
    /// The transport protocol error code associated with the error.
    /// </summary>
    public long? TransportErrorCode { get; }
}
