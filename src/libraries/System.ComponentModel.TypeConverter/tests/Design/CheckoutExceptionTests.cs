// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class CheckoutExceptionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var exception = new CheckoutException();
            Assert.NotEmpty(exception.Message);
            Assert.Equal(-2147467259, exception.ErrorCode);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [InlineData("Message")]
        public void Ctor_Message(string message)
        {
            var exception = new CheckoutException(message);
            Assert.Same(message, exception.Message);
            Assert.Equal(-2147467259, exception.ErrorCode);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [InlineData("Message", 10)]
        public void Ctor_Message_ErrorCode(string message, int errorCode)
        {
            var exception = new CheckoutException(message, errorCode);
            Assert.Same(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [InlineData("Message")]
        public void Ctor_Message_InnerException(string message)
        {
            var innerException = new DivideByZeroException();

            var exception = new CheckoutException(message, innerException);
            Assert.Same(message, exception.Message);
            Assert.Equal(-2147467259, exception.ErrorCode);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact]
        public void Cancelled_Get_ReturnsExpected()
        {
            const int E_ABORT = unchecked((int)0x80004004);
            CheckoutException exception = CheckoutException.Canceled;
            Assert.NotEmpty(exception.Message);
            Assert.Equal(E_ABORT, exception.ErrorCode);
            Assert.Null(exception.InnerException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        public void Ctor_SerializationInfo_StreamingContext()
        {
            using (var stream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, new CheckoutException());

                stream.Seek(0, SeekOrigin.Begin);
                Assert.IsType<CheckoutException>(binaryFormatter.Deserialize(stream));
            }
        }
    }
}
