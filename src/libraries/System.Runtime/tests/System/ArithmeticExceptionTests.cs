// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class ArithmeticExceptionTests
    {
        private const int COR_E_ARITHMETIC = unchecked((int)0x80070216);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new ArithmeticException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_ARITHMETIC, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "arithmetic operation error";
            var exception = new ArithmeticException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_ARITHMETIC, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "arithmetic operation error";
            var innerException = new Exception("Inner exception");
            var exception = new ArithmeticException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_ARITHMETIC, innerException: innerException, message: message);
        }
    }
}
