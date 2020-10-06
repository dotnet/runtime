// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_OUTOFMEMORY, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "out of memory";
            var exception = new OutOfMemoryException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_OUTOFMEMORY, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "out of memory";
            var innerException = new Exception("Inner exception");
            var exception = new OutOfMemoryException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_OUTOFMEMORY, innerException: innerException, message: message);
        }
    }
}
