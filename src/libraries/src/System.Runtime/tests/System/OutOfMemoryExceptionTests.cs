// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class OutOfMemoryExceptionTests
    {
        private const int COR_E_OUTOFMEMORY = unchecked((int)0x8007000E);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new OutOfMemoryException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_OUTOFMEMORY, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "out of memory";
            var exception = new OutOfMemoryException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_OUTOFMEMORY, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "out of memory";
            var innerException = new Exception("Inner exception");
            var exception = new OutOfMemoryException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_OUTOFMEMORY, innerException: innerException, message: message);
        }
    }
}
