// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    /// <summary>
    ///   The exception that is thrown when a decryption operation with an authenticated cipher
    ///   has an authentication tag mismatch.
    /// </summary>
    public sealed class AuthenticationTagMismatchException : CryptographicException
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="AuthenticationTagMismatchException" /> class with default
        ///   properties.
        /// </summary>
        public AuthenticationTagMismatchException() : base(SR.Cryptography_AuthTagMismatch)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AuthenticationTagMismatchException" /> class with a specified
        ///   error message.
        /// </summary>
        /// <param name="message">
        ///   The error message that explains the reason for the exception.
        /// </param>
        public AuthenticationTagMismatchException(string? message) : base(message ?? SR.Cryptography_AuthTagMismatch)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AuthenticationTagMismatchException" /> class with a specified
        ///   error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">
        ///   The error message that explains the reason for the exception.
        /// </param>
        /// <param name="inner">
        ///   The exception that is the cause of the current exception. If the parameter is not
        ///   <see langword="null" />, the current exception is raised in a catch block that handles the inner exception.
        /// </param>
        public AuthenticationTagMismatchException(string? message, Exception? inner)
            : base(message ?? SR.Cryptography_AuthTagMismatch, inner)
        {
        }
    }
}
