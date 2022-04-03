// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Hosting.Unit.Tests
{
    public sealed class HostAbortedExceptionTests
    {
        [Fact]
        public void TestEmptyException()
        {
            var exception = new HostAbortedException();
            Assert.Null(exception.InnerException);
            Assert.Throws<HostAbortedException>(() => throw exception);
        }

        [Theory]
        [InlineData("Host aborted.", false)]
        [InlineData("Host aborted.", true)]
        public void TestException(string? message, bool innerException)
        {
            HostAbortedException exception;
            exception = innerException
                ? new HostAbortedException(message, new Exception())
                : new HostAbortedException(message);

            Assert.Equal(message, exception.Message);
            if (innerException)
            {
                Assert.NotNull(exception.InnerException);
            } 
            
            Assert.Throws<HostAbortedException>(message, () => throw exception);
        }
    }
}
