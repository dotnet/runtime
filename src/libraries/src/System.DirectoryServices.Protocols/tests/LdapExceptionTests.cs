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
    public class LdapExceptionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var exception = new LdapException();
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.InnerException);

            Assert.Equal(0, exception.ErrorCode);
            Assert.Null(exception.ServerErrorMessage);
            Assert.Empty(exception.PartialResults);
        }

        [Fact]
        public void Ctor_Message()
        {
            var exception = new LdapException("message");
            Assert.Equal("message", exception.Message);
            Assert.Null(exception.InnerException);

            Assert.Equal(0, exception.ErrorCode);
            Assert.Null(exception.ServerErrorMessage);
            Assert.Empty(exception.PartialResults);
        }

        [Fact]
        public void Ctor_Message_InnerException()
        {
            var innerException = new Exception();
            var exception = new LdapException("message", innerException);
            Assert.Equal("message", exception.Message);
            Assert.Same(innerException, exception.InnerException);

            Assert.Equal(0, exception.ErrorCode);
            Assert.Null(exception.ServerErrorMessage);
            Assert.Empty(exception.PartialResults);
        }

        [Fact]
        public void Ctor_ErrorCode()
        {
            var exception = new LdapException(10);
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.InnerException);

            Assert.Equal(10, exception.ErrorCode);
            Assert.Null(exception.ServerErrorMessage);
            Assert.Empty(exception.PartialResults);
        }

        [Fact]
        public void Ctor_ErrorCode_Message()
        {
            var exception = new LdapException(10, "message");
            Assert.Equal("message", exception.Message);
            Assert.Null(exception.InnerException);

            Assert.Equal(10, exception.ErrorCode);
            Assert.Null(exception.ServerErrorMessage);
            Assert.Empty(exception.PartialResults);
        }

        [Fact]
        public void Ctor_ErrorCode_Message_ServerErrorMessage()
        {
            var exception = new LdapException(10, "message", "serverErrorMessage");
            Assert.Equal("message", exception.Message);
            Assert.Null(exception.InnerException);

            Assert.Equal(10, exception.ErrorCode);
            Assert.Equal("serverErrorMessage", exception.ServerErrorMessage);
            Assert.Empty(exception.PartialResults);
        }

        [Fact]
        public void Ctor_ErrorCode_Message_InnerException()
        {
            var innerException = new Exception();
            var exception = new LdapException(10, "message", innerException);
            Assert.Equal("message", exception.Message);
            Assert.Same(innerException, exception.InnerException);

            Assert.Equal(10, exception.ErrorCode);
            Assert.Null(exception.ServerErrorMessage);
            Assert.Empty(exception.PartialResults);
        }
    }
}
