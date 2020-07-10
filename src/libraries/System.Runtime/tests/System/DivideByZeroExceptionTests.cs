// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public static class DivideByZeroExceptionTests
    {
        private const int COR_E_DIVIDEBYZERO = unchecked((int)0x80020012);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new DivideByZeroException();
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_DIVIDEBYZERO, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "divide by zero";
            var exception = new DivideByZeroException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_DIVIDEBYZERO, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "divide by zero";
            var innerException = new Exception("Inner exception");
            var exception = new DivideByZeroException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_DIVIDEBYZERO, innerException: innerException, message: message);
        }
    }
}
