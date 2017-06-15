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
    public class DirectoryExceptionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var exception = new DirectoryException();
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Ctor_Message()
        {
            var exception = new DirectoryException("message");
            Assert.Equal("message", exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Ctor_Message_InnerException()
        {
            var innerException = new Exception();
            var exception = new DirectoryException("message", innerException);
            Assert.Equal("message", exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }
    }
}
