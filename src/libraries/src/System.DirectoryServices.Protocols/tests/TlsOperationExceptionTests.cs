// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class TlsOperationExceptionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var exception = new TlsOperationException();
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Ctor_Message()
        {
            var exception = new TlsOperationException("message");
            Assert.Equal("message", exception.Message);
            Assert.Null(exception.Response);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Ctor_Message_InnerException()
        {
            var innerException = new Exception();
            var exception = new TlsOperationException("message", innerException);
            Assert.Equal("message", exception.Message);
            Assert.Null(exception.Response);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact]
        public void Ctor_Response()
        {
            var exception = new TlsOperationException((DirectoryResponse)null);
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.Response);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Ctor_Response_Message()
        {
            var exception = new TlsOperationException(null, "message");
            Assert.Equal("message", exception.Message);
            Assert.Null(exception.Response);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Ctor_Response_Message_InnerException()
        {
            var innerException = new Exception();
            var exception = new TlsOperationException(null, "message", innerException);
            Assert.Equal("message", exception.Message);
            Assert.Null(exception.Response);
            Assert.Same(innerException, exception.InnerException);
        }
    }
}
