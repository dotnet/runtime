// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class SystemExceptionTests
    {
        private const int COR_E_SYSTEM = -2146233087;

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new SystemException();
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_SYSTEM, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created SystemException";
            var exception = new SystemException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_SYSTEM, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created SystemException";
            var innerException = new Exception("Created inner exception");
            var exception = new SystemException(message, innerException);

            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_SYSTEM, exception.HResult);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }
    }
}
