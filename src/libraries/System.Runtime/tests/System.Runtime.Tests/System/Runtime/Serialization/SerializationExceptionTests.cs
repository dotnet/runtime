// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Runtime.Serialization.Tests
{
    public class SerializationExceptionTests
    {
        private const int COR_E_SERIALIZATION = unchecked((int)0x8013150C);

        [Fact]
        public void Ctor_Default()
        {
            var exception = new SerializationException();
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Equal(COR_E_SERIALIZATION, exception.HResult);
        }

        [Theory]
        [InlineData("message")]
        public void Ctor_String(string message)
        {
            var exception = new SerializationException(message);
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Equal(COR_E_SERIALIZATION, exception.HResult);
        }

        [Theory]
        [InlineData("message")]
        public void Ctor_String_Exception(string message)
        {
            var innerException = new Exception();
            var exception = new SerializationException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(COR_E_SERIALIZATION, exception.HResult);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        public void Ctor_SerializationInfo_StreamingContext()
        {
            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, new SerializationException());

                memoryStream.Seek(0, SeekOrigin.Begin);
                Assert.IsType<SerializationException>(formatter.Deserialize(memoryStream));
            }
        }
    }
}
