// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication;
using Xunit;

namespace System.Net.Security.Tests
{
    public class AuthenticationExceptionTest
    {
        [Fact]
        public void Constructor_NoParameter_DefaultMessageIsNotNull()
        {
            AuthenticationException authenticationException = new AuthenticationException();

            Assert.NotNull(authenticationException.Message);
        }

        [Fact]
        public void Constructor_String_PassedInMessageCorrect()
        {
            const string passedInMessage = "base was called";

            AuthenticationException authenticationException = new AuthenticationException(passedInMessage);

            Assert.Equal(passedInMessage, authenticationException.Message);
        }

        [Fact]
        public void Constructor_String_Exception_MessagesCorrect()
        {
            const string passedInMessage = "base was called";
            const string innerExceptionMessage = "this is the inner exception message";

            AuthenticationException authenticationException = new AuthenticationException(passedInMessage, new Exception(innerExceptionMessage));

            Assert.Equal(passedInMessage, authenticationException.Message);
            Assert.Equal(innerExceptionMessage, authenticationException.InnerException.Message);
        }
    }
}
