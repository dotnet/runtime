// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Authentication
{
    /// <summary>
    /// This exception can be thrown from Authenticate() method of Ssl and Negotiate classes.
    /// The authentication process can be retried with different parameters subject to
    /// remote party willingness of accepting that.
    /// </summary>
    public class AuthenticationException : Exception
    {
        public AuthenticationException() { }
        public AuthenticationException(string message) : base(message) { }
        public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// <para>
    /// This exception can be thrown from Authenticate() method of Ssl and Negotiate classes.
    /// The authentication is expected to fail prematurely if called using the same
    /// underlined stream.
    /// </para>
    /// </summary>
    public class InvalidCredentialException : AuthenticationException
    {
        public InvalidCredentialException() { }
        public InvalidCredentialException(string message) : base(message) { }
        public InvalidCredentialException(string message, Exception innerException) : base(message, innerException) { }
    }
}
