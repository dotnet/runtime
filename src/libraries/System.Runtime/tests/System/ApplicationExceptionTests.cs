// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Tests
{
    public static class ApplicationExceptionTests
    {
        private const int COR_E_APPLICATION = -2146232832;

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new ApplicationException();
            Assert.NotNull(exception);
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_APPLICATION, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created ApplicationException";
            var exception = new ApplicationException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_APPLICATION, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created ApplicationException";
            var innerException = new Exception("Created inner exception");
            var exception = new ApplicationException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_APPLICATION, exception.HResult);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }
    }
}
