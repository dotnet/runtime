// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class EncoderFallbackExceptionTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            EncoderFallbackException encoderFallbackException = new EncoderFallbackException();
            Assert.Equal(default(char), encoderFallbackException.CharUnknown);
            Assert.Equal(default(char), encoderFallbackException.CharUnknownHigh);
            Assert.Equal(default(char), encoderFallbackException.CharUnknownLow);
            Assert.Equal(0, encoderFallbackException.Index);

            Assert.Null(encoderFallbackException.StackTrace);
            Assert.Null(encoderFallbackException.InnerException);
            Assert.Equal(0, encoderFallbackException.Data.Count);

            ArgumentException arg = new ArgumentException();
            Assert.Equal(arg.Message, encoderFallbackException.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test message.")]
        public static void Ctor_String(string message)
        {
            EncoderFallbackException encoderFallbackException = new EncoderFallbackException(message);
            Assert.Equal(default(char), encoderFallbackException.CharUnknown);
            Assert.Equal(default(char), encoderFallbackException.CharUnknownHigh);
            Assert.Equal(default(char), encoderFallbackException.CharUnknownLow);
            Assert.Equal(0, encoderFallbackException.Index);

            Assert.Null(encoderFallbackException.StackTrace);
            Assert.Null(encoderFallbackException.InnerException);
            Assert.Equal(0, encoderFallbackException.Data.Count);

            Assert.Equal(message, encoderFallbackException.Message);
        }

        public static IEnumerable<object[]> Ctor_String_Exception_TestData()
        {
            yield return new object[] { "Test message.", new InvalidOperationException("Inner exception message.") };
            yield return new object[] { "", null };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_Exception_TestData))]
        public static void Ctor_String_Exception(string message, Exception innerException)
        {
            EncoderFallbackException encoderFallbackException = new EncoderFallbackException(message, innerException);
            Assert.Equal(default(char), encoderFallbackException.CharUnknown);
            Assert.Equal(default(char), encoderFallbackException.CharUnknownHigh);
            Assert.Equal(default(char), encoderFallbackException.CharUnknownLow);
            Assert.Equal(0, encoderFallbackException.Index);

            Assert.Null(encoderFallbackException.StackTrace);
            Assert.Equal(0, encoderFallbackException.Data.Count);

            Assert.Same(innerException, encoderFallbackException.InnerException);
            Assert.Equal(message, encoderFallbackException.Message);
        }
    }
}
