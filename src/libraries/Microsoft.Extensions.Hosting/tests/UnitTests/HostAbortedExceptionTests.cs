// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
            Assert.Throws<HostAbortedException>(new Action(() => throw exception));
        }

        [Theory]
        [InlineData("The host was aborted.", false)]
        [InlineData("The host was aborted.", true)]
        public void TestException(string? message, bool innerException)
        {
            HostAbortedException exception = innerException
                ? new HostAbortedException(message, new Exception())
                : new HostAbortedException(message);

            Assert.Equal(message, exception.Message);
            if (innerException)
            {
                Assert.NotNull(exception.InnerException);
            } 
            
            HostAbortedException thrownException = Assert.Throws<HostAbortedException>(new Action(() => throw exception));
            Assert.Equal(message, thrownException.Message);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        [InlineData("Test Message")]
        [InlineData(null)]
        public void TestSerialization(string? message)
        {
            var exception = new HostAbortedException(message);
            using var serializationStream = new MemoryStream();

            var formatter = new BinaryFormatter();
            formatter.Serialize(serializationStream, exception);

            using var deserializationStream = new MemoryStream(serializationStream.ToArray());
            HostAbortedException deserializedException = (HostAbortedException) formatter.Deserialize(deserializationStream);

            Assert.Equal(exception.Message, deserializedException.Message);
            Assert.Null(deserializedException.InnerException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        public void TestSerializationDefaultConstructor()
        {
            var exception = new HostAbortedException();
            using var serializationStream = new MemoryStream();

            var formatter = new BinaryFormatter();
            formatter.Serialize(serializationStream, exception);

            using var deserializationStream = new MemoryStream(serializationStream.ToArray());
            HostAbortedException deserializedException = (HostAbortedException)formatter.Deserialize(deserializationStream);

            Assert.Equal(exception.Message, deserializedException.Message);
            Assert.Null(deserializedException.InnerException);            
        }
    }
}
