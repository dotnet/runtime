// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Tests
{
    public static class AccessViolationExceptionTests
    {
        private const int COR_E_ACCESSVIOLATION = -2147467261;

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new AccessViolationException();
            Assert.NotNull(exception);
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_ACCESSVIOLATION, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created AccessViolationException";
            var exception = new AccessViolationException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_ACCESSVIOLATION, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created AccessViolationException";
            var innerException = new Exception("Created inner exception");
            var exception = new AccessViolationException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_ACCESSVIOLATION, exception.HResult);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }
    }
}
