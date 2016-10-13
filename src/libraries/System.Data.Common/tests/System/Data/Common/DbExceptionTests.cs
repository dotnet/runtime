// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Common.Tests
{
    public class DbExceptionTests
    {
        [Fact]
        public void ArgumentsStored()
        {
            Assert.Equal("test", new CustomDbException("test").Message);
            Assert.Equal("another test", new CustomDbException("test", new Exception("another test")).InnerException.Message);
        }

        private class CustomDbException : DbException
        {
            public CustomDbException(string message) : base(message)
            {
            }

            public CustomDbException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }
    }
}
