// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public static class PlatformNotSupportedExceptionTests
    {
        private const int COR_E_PLATFORMNOTSUPPORTED = unchecked((int)0x80131539);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new PlatformNotSupportedException();
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_PLATFORMNOTSUPPORTED, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "platform not supported";
            var exception = new PlatformNotSupportedException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_PLATFORMNOTSUPPORTED, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "platform not supported";
            var innerException = new Exception("Inner exception");
            var exception = new PlatformNotSupportedException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_PLATFORMNOTSUPPORTED, innerException: innerException, message: message);
        }
    }
}
