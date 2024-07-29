// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace System.Tests
{
    public static class EndOfStreamExceptionTests
    {
        private const string exceptionMessage = "Created an exception";
        private const string innerExceptionMessage = "Created an InnerException";
        private const uint COR_E_ENDOFSTREAM = 0x80070026;

        [Fact]
        public static void EndOfStreamException_Ctor_Empty()
        {
            EndOfStreamException i = new EndOfStreamException();
            Assert.Equal(COR_E_ENDOFSTREAM, unchecked((uint)i.HResult));
        }

        [Fact]
        public static void EndOfStreamException_Ctor_String()
        {
            EndOfStreamException i = new EndOfStreamException(exceptionMessage);
            Assert.Equal(exceptionMessage, i.Message);
            Assert.Equal(COR_E_ENDOFSTREAM, unchecked((uint)i.HResult));
        }

        [Fact]
        public static void EndOfStreamException_Ctor_String_Exception()
        {
            Exception ex = new Exception(innerExceptionMessage);
            EndOfStreamException i = new EndOfStreamException(exceptionMessage, ex);

            Assert.Equal(exceptionMessage, i.Message);
            Assert.Equal(innerExceptionMessage, i.InnerException.Message);
            Assert.Equal(ex.HResult, i.InnerException.HResult);
            Assert.Equal(COR_E_ENDOFSTREAM, unchecked((uint)i.HResult));
        }
    }
}
