// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public static class FormatExceptionTests
    {
        private const int COR_E_FORMAT = unchecked((int)0x80131537);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new FormatException();
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_FORMAT, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "bad format";
            var exception = new FormatException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_FORMAT, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "bad format";
            var innerException = new Exception("Inner exception");
            var exception = new FormatException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_FORMAT, innerException: innerException, message: message);
        }
    }
}
