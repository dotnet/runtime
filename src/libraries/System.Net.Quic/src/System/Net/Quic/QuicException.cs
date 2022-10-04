// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.Quic
{
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
            : base(message)
        {
            QuicError = error;
            ApplicationErrorCode = applicationErrorCode;
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
    }
}
