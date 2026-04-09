// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Runtime.Serialization.Tests
{
    public class InvalidDataContractExceptionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var exception = new InvalidDataContractException();
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [InlineData("message")]
        public void Ctor_String(string message)
        {
            var exception = new InvalidDataContractException(message);
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [InlineData("message")]
        public void Ctor_String_Exception(string message)
        {
            var innerException = new Exception();
            var exception = new InvalidDataContractException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        public void Ctor_SerializationInfo_StreamingContext()
        {
            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, new InvalidDataContractException());

                memoryStream.Seek(0, SeekOrigin.Begin);
                Assert.IsType<InvalidDataContractException>(formatter.Deserialize(memoryStream));
            }
        }
    }
}
