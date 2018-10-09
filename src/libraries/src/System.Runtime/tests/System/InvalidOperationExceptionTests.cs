// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class InvalidOperationExceptionTests
    {
        private const int COR_E_INVALIDOPERATION = unchecked((int)0x80131509);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new InvalidOperationException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDOPERATION, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "invalid operation";
            var exception = new InvalidOperationException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDOPERATION, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "invalid operation";
            var innerException = new Exception("Inner exception");
            var exception = new InvalidOperationException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDOPERATION, innerException: innerException, message: message);
        }
    }
}
