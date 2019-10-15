// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Security.Authentication
{
    /// <summary>
    /// This exception can be thrown from Authenticate() method of Ssl and Negotiate classes.
    /// The authentication process can be retried with different parameters subject to
    /// remote party willingness of accepting that.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class AuthenticationException : SystemException
    {
        public AuthenticationException() { }

        public AuthenticationException(string message) : base(message) { }

        public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }

        protected AuthenticationException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }

    /// <summary>
    /// <para>
    /// This exception can be thrown from Authenticate() method of Ssl and Negotiate classes.
    /// The authentication is expected to fail prematurely if called using the same
    /// underlined stream.
    /// </para>
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidCredentialException : AuthenticationException
    {
        public InvalidCredentialException() { }

        public InvalidCredentialException(string message) : base(message) { }

        public InvalidCredentialException(string message, Exception innerException) : base(message, innerException) { }

        protected InvalidCredentialException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}
