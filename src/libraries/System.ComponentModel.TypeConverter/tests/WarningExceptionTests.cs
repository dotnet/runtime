// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class WarningExceptionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var exception = new WarningException();
            Assert.Null(exception.InnerException);
            Assert.Null(exception.HelpTopic);
            Assert.Null(exception.HelpUrl);
            Assert.Equal(-2146233087, exception.HResult);
            Assert.NotEmpty(exception.Message);
        }

        [Theory]
        [InlineData("message")]
        public void Ctor_String(string message)
        {
            var exception = new WarningException(message);
            Assert.Null(exception.InnerException);
            Assert.Null(exception.HelpTopic);
            Assert.Null(exception.HelpUrl);
            Assert.Equal(-2146233087, exception.HResult);
            Assert.Equal(message, exception.Message);
        }

        [Theory]
        [InlineData("message")]
        public void Ctor_String_Exception(string message)
        {
            var innerException = new DivideByZeroException();
            var exception = new WarningException(message, innerException);
            Assert.Same(innerException, exception.InnerException);
            Assert.Null(exception.HelpTopic);
            Assert.Null(exception.HelpUrl);
            Assert.Equal(-2146233087, exception.HResult);
            Assert.Equal(message, exception.Message);
        }

        [Theory]
        [InlineData("message", null)]
        [InlineData("message", "")]
        [InlineData("message", "helpUrl")]
        public void Ctor_String_String(string message, string helpUrl)
        {
            var exception = new WarningException(message, helpUrl);
            Assert.Null(exception.InnerException);
            Assert.Null(exception.HelpTopic);
            Assert.Equal(helpUrl, exception.HelpUrl);
            Assert.Equal(-2146233087, exception.HResult);
            Assert.Equal(message, exception.Message);
        }

        [Theory]
        [InlineData("message", null, null)]
        [InlineData("message", "", "")]
        [InlineData("message", "helpUrl", "helpTopic")]
        public void Ctor_String_String_String(string message, string helpUrl, string helpTopic)
        {
            var exception = new WarningException(message, helpUrl, helpTopic);
            Assert.Null(exception.InnerException);
            Assert.Equal(helpTopic, exception.HelpTopic);
            Assert.Equal(helpUrl, exception.HelpUrl);
            Assert.Equal(-2146233087, exception.HResult);
            Assert.Equal(message, exception.Message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        public void Ctor_SerializationInfo_StreamingContext()
        {
            using (var stream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, new WarningException());

                stream.Seek(0, SeekOrigin.Begin);
                Assert.IsType<WarningException>(binaryFormatter.Deserialize(stream));
            }
        }
    }
}
