// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class InvalidAsynchronousStateExceptionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var exception = new InvalidAsynchronousStateException();
            Assert.Null(exception.InnerException);
            Assert.Equal(-2147024809, exception.HResult);
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.ParamName);
        }

        [Theory]
        [InlineData("message")]
        public void Ctor_String(string message)
        {
            var exception = new InvalidAsynchronousStateException(message);
            Assert.Null(exception.InnerException);
            Assert.Equal(-2147024809, exception.HResult);
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.ParamName);
        }

        [Theory]
        [InlineData("message")]
        public void Ctor_String_Exception(string message)
        {
            var innerException = new DivideByZeroException();
            var exception = new InvalidAsynchronousStateException(message, innerException);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(-2147024809, exception.HResult);
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.ParamName);
        }

        [Fact]
        public void Ctor_SerializationInfo_StreamingContext()
        {
            using (var stream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, new InvalidAsynchronousStateException());

                stream.Seek(0, SeekOrigin.Begin);
                Assert.IsType<InvalidAsynchronousStateException>(binaryFormatter.Deserialize(stream));
            }
        }
    }
}
