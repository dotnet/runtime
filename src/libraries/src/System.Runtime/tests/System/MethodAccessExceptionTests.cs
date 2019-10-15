// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class MethodAccessExceptionTests
    {
        private const int COR_E_METHODACCESS = unchecked((int)0x80131510);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new MethodAccessException();
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_METHODACCESS, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created MethodAccessException";
            var exception = new MethodAccessException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_METHODACCESS, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created MethodAccessException";
            var innerException = new Exception("Created inner exception");
            var exception = new MethodAccessException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_METHODACCESS, exception.HResult);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }
    }
}
