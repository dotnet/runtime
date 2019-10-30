// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class StackOverflowExceptionTests
    {
        private const int COR_E_STACKOVERFLOW = -2147023895;

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new StackOverflowException();
            Assert.NotNull(exception);
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_STACKOVERFLOW, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created StackOverflowException";
            var exception = new StackOverflowException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_STACKOVERFLOW, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created StackOverflowException";
            var innerException = new Exception("Created inner exception");
            var exception = new StackOverflowException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_STACKOVERFLOW, exception.HResult);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }
    }
}
