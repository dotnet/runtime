// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;

namespace System.Text.EncodingTests
{
    // Tests the EncoderFallbackException class.
    public class EncoderFallbackExceptionTests
    {
        [Fact]
        public static void Ctor()
        {
            EncoderFallbackException ex = new EncoderFallbackException();
            Assert.Equal(default(char), ex.CharUnknown);
            Assert.Equal(default(char), ex.CharUnknownHigh);
            Assert.Equal(default(char), ex.CharUnknownLow);
            Assert.Equal(default(int), ex.Index);

            Assert.Null(ex.StackTrace);
            Assert.Null(ex.InnerException);
            Assert.Equal(0, ex.Data.Count);

            ArgumentException arg = new ArgumentException();
            Assert.Equal(arg.Message, ex.Message);
        }

        [Fact]
        public static void Ctor2()
        {
            string message = "Test message.";
            EncoderFallbackException ex = new EncoderFallbackException(message);
            Assert.Equal(default(char), ex.CharUnknown);
            Assert.Equal(default(char), ex.CharUnknownHigh);
            Assert.Equal(default(char), ex.CharUnknownLow);
            Assert.Equal(default(int), ex.Index);

            Assert.Null(ex.StackTrace);
            Assert.Null(ex.InnerException);
            Assert.Equal(0, ex.Data.Count);

            Assert.Equal(message, ex.Message);

            message = "";
            ex = new EncoderFallbackException(message);
            Assert.Equal(default(char), ex.CharUnknown);
            Assert.Equal(default(char), ex.CharUnknownHigh);
            Assert.Equal(default(char), ex.CharUnknownLow);
            Assert.Equal(default(int), ex.Index);

            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public static void Ctor3()
        {
            string message = "Test message.";
            string innerMsg = "Invalid Op Message.";
            Exception innerException = new InvalidOperationException(innerMsg);
            EncoderFallbackException ex = new EncoderFallbackException(message, innerException);
            Assert.Equal(default(char), ex.CharUnknown);
            Assert.Equal(default(char), ex.CharUnknownHigh);
            Assert.Equal(default(char), ex.CharUnknownLow);
            Assert.Equal(default(int), ex.Index);

            Assert.Null(ex.StackTrace);
            Assert.Equal(0, ex.Data.Count);

            Assert.Equal(innerException, ex.InnerException);
            Assert.Equal(innerMsg, ex.InnerException.Message);

            Assert.Equal(message, ex.Message);

            message = "";
            ex = new EncoderFallbackException(message, null);
            Assert.Equal(default(char), ex.CharUnknown);
            Assert.Equal(default(char), ex.CharUnknownHigh);
            Assert.Equal(default(char), ex.CharUnknownLow);
            Assert.Equal(default(int), ex.Index);

            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);
        }
    }
}
