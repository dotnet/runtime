// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class DecoderFallbackExceptionTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            DecoderFallbackException decoderFallbackException = new DecoderFallbackException();
            Assert.Null(decoderFallbackException.BytesUnknown);
            Assert.Equal(0, decoderFallbackException.Index);
            Assert.Null(decoderFallbackException.StackTrace);
            Assert.Null(decoderFallbackException.InnerException);
            Assert.Equal(0, decoderFallbackException.Data.Count);

            Assert.Equal(new ArgumentException().Message, decoderFallbackException.Message);
        }

        [Theory]
        [InlineData("Test message.")]
        [InlineData(".")]
        public void Ctor_String(string message)
        {
            DecoderFallbackException decoderFallbackException = new DecoderFallbackException(message);
            Assert.Null(decoderFallbackException.BytesUnknown);
            Assert.Equal(0, decoderFallbackException.Index);
            Assert.Null(decoderFallbackException.StackTrace);
            Assert.Null(decoderFallbackException.InnerException);
            Assert.Equal(0, decoderFallbackException.Data.Count);
            Assert.Equal(message, decoderFallbackException.Message);
        }

        public static IEnumerable<object[]> Ctor_String_Exception_TestData()
        {
            yield return new object[] { "Test message.", new InvalidOperationException("Inner exception message.") };
            yield return new object[] { "", null };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_Exception_TestData))]
        public void Ctor_String_Exception(string message, Exception innerException)
        {
            DecoderFallbackException ex = new DecoderFallbackException(message, innerException);
            Assert.Null(ex.BytesUnknown);
            Assert.Equal(0, ex.Index);
            Assert.Null(ex.StackTrace);
            Assert.Equal(0, ex.Data.Count);
            Assert.Same(innerException, ex.InnerException);
            Assert.Equal(message, ex.Message);
        }
    }
}
