// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public static class TimeoutExceptionTests
    {
        private const int COR_E_TIMEOUT = unchecked((int)0x80131505);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new TimeoutException();
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_TIMEOUT, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "timeout";
            var exception = new TimeoutException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_TIMEOUT, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "timeout";
            var innerException = new Exception("Inner exception");
            var exception = new TimeoutException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_TIMEOUT, innerException: innerException, message: message);
        }
    }
}
