// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class DllNotFoundExceptionTests
    {
        private const int COR_E_DLLNOTFOUND = unchecked((int)0x80131524);

        [Fact]
        public void Ctor_Default()
        {
            var exception = new DllNotFoundException();
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Equal(COR_E_DLLNOTFOUND, exception.HResult);
        }

        [Fact]
        public void Ctor_String()
        {
            string message = "library not found";
            var exception = new DllNotFoundException(message);
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Equal(COR_E_DLLNOTFOUND, exception.HResult);
        }

        [Fact]
        public void Ctor_String_Exception()
        {
            string message = "library not found";
            var innerException = new Exception();
            var exception = new DllNotFoundException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(COR_E_DLLNOTFOUND, exception.HResult);
        }
    }
}
