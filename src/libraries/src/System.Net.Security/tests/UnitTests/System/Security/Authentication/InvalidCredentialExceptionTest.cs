// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication;
using Xunit;

namespace System.Net.Security.Tests
{
    public class InvalidCredentialExceptionTest
    {
        [Fact]
        public void Constructor_NoParameter_DefaultMessageIsNotNull()
        {
            InvalidCredentialException invalidCredentialException = new InvalidCredentialException();

            Assert.NotNull(invalidCredentialException.Message);
        }

        [Fact]
        public void Constructor_String_PassedInMessageCorrect()
        {
            const string passedInMessage = "base was called";

            InvalidCredentialException invalidCredentialException = new InvalidCredentialException(passedInMessage);

            Assert.Equal(passedInMessage, invalidCredentialException.Message);
        }

        [Fact]
        public void Constructor_String_Exception_MessagesCorrect()
        {
            const string passedInMessage = "base was called";
            const string innerExceptionMessage = "this is the inner exception message";

            InvalidCredentialException invalidCredentialException = new InvalidCredentialException(passedInMessage, new Exception(innerExceptionMessage));

            Assert.Equal(passedInMessage, invalidCredentialException.Message);
            Assert.Equal(innerExceptionMessage, invalidCredentialException.InnerException.Message);
        }
    }
}
