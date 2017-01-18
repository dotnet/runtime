// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public static class ExternalExceptionTests
    {
        private const int COR_E_EXTERNAL = -2147467259;

        [Fact]
        public static void Ctor_Empty()
        {
            ExternalException exception = new ExternalException();
            Assert.Null(exception.InnerException);
            Assert.NotNull(exception.Message);
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_EXTERNAL, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created ExternalException";
            ExternalException exception = new ExternalException(message);
            Assert.Equal(COR_E_EXTERNAL, exception.HResult);
            Assert.Null(exception.InnerException);
            Assert.Same(message, exception.Message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created ExternalException";
            var innerException = new Exception("Created inner exception");
            ExternalException exception = new ExternalException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(COR_E_EXTERNAL, exception.HResult);
        }

        public static void Ctor_String_int()
        {
            string msg = "Created ExternalException";
            int errorCode = -2000607220;
            ExternalException exception = new ExternalException(msg, errorCode);
            Assert.Equal(msg, exception.Message);
            Assert.Equal(COR_E_EXTERNAL, exception.HResult);
        }
    }
}