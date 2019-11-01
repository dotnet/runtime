// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class InvalidCastExceptionTests
    {
        private const int COR_E_INVALIDCAST = unchecked((int)0x80004002);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new InvalidCastException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDCAST, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "wrong type";
            var exception = new InvalidCastException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDCAST, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "wrong type";
            var innerException = new Exception("Inner exception");
            var exception = new InvalidCastException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDCAST, innerException: innerException, message: message);
        }

        [Fact]
        public static void Ctor_String_Int32()
        {
            string message = "wrong type";
            int errorCode = unchecked((int)0x80424242);
            var exception = new InvalidCastException(message, errorCode);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: errorCode, message: message);
        }

    }
}
