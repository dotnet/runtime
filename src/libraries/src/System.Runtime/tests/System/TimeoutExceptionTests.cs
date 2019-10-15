// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_TIMEOUT, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "timeout";
            var exception = new TimeoutException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_TIMEOUT, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "timeout";
            var innerException = new Exception("Inner exception");
            var exception = new TimeoutException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_TIMEOUT, innerException: innerException, message: message);
        }
    }
}
