// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class NotSupportedExceptionTests
    {
        private const int COR_E_NOTSUPPORTED = unchecked((int)0x80131515);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new NotSupportedException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_NOTSUPPORTED, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "not supported";
            var exception = new NotSupportedException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_NOTSUPPORTED, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "not supported";
            var innerException = new Exception("Inner exception");
            var exception = new NotSupportedException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_NOTSUPPORTED, innerException: innerException, message: message);
        }
    }
}
