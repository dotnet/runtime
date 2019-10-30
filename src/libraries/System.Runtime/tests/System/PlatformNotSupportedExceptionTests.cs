// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_PLATFORMNOTSUPPORTED, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "platform not supported";
            var exception = new PlatformNotSupportedException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_PLATFORMNOTSUPPORTED, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "platform not supported";
            var innerException = new Exception("Inner exception");
            var exception = new PlatformNotSupportedException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_PLATFORMNOTSUPPORTED, innerException: innerException, message: message);
        }
    }
}
