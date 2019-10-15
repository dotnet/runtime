// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class OverflowExceptionTests
    {
        private const int COR_E_OVERFLOW = unchecked((int)0x80131516);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new OverflowException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_OVERFLOW, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "overflow";
            var exception = new OverflowException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_OVERFLOW, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "overflow";
            var innerException = new Exception("Inner exception");
            var exception = new OverflowException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_OVERFLOW, innerException: innerException, message: message);
        }
    }
}
