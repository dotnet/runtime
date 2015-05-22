// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;

namespace System.Text.EncodingTests
{
    // Tests the DecoderFallbackException class.
    public class DecoderFallbackExceptionTests
    {
        [Fact]
        public void Ctor()
        {
            DecoderFallbackException ex = new DecoderFallbackException();
            Assert.Null(ex.BytesUnknown);
            Assert.Equal(default(int), ex.Index);
            Assert.Null(ex.StackTrace);
            Assert.Null(ex.InnerException);
            Assert.Equal(0, ex.Data.Count);
            ArgumentException arg = new ArgumentException();

            Assert.Equal(ex.Message, arg.Message);
        }

        [Fact]
        public void Ctor2()
        {
            string message = "Test message.";
            DecoderFallbackException ex = new DecoderFallbackException(message);
            Assert.Null(ex.BytesUnknown);
            Assert.Equal(default(int), ex.Index);
            Assert.Null(ex.StackTrace);
            Assert.Null(ex.InnerException);
            Assert.Equal(0, ex.Data.Count);
            Assert.Equal(ex.Message, message);

            message = "";
            ex = new DecoderFallbackException(message);
            Assert.Null(ex.BytesUnknown);
            Assert.Equal(default(int), ex.Index);
            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void Ctor3()
        {
            string message = "Test message.";
            string innerMsg = "Invalid Op Message.";
            Exception innerException = new InvalidOperationException(innerMsg);
            DecoderFallbackException ex = new DecoderFallbackException(message, innerException);
            Assert.Null(ex.BytesUnknown);
            Assert.Equal(default(int), ex.Index);
            Assert.Null(ex.StackTrace);
            Assert.Equal(0, ex.Data.Count);
            Assert.Equal(innerException, ex.InnerException);
            Assert.Equal(innerMsg, ex.InnerException.Message);
            Assert.Equal(message, ex.Message);

            message = "";
            ex = new DecoderFallbackException(message, null);
            Assert.Null(ex.BytesUnknown);
            Assert.Equal(default(int), ex.Index);
            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);
        }
    }
}
