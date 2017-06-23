// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Composition.Hosting.Tests
{
    public class CompositionFailedExceptionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var exception = new CompositionFailedException();
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [InlineData("")]
        [InlineData("message")]
        public void Ctor_Message(string message)
        {
            var exception = new CompositionFailedException(message);
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [InlineData("")]
        [InlineData("message")]
        public void Ctor_Message_InnerException(string message)
        {
            var innerException = new DivideByZeroException();
            var exception = new CompositionFailedException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }
    }
}
