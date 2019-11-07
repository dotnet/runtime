// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class RankExceptionTests
    {
        private const int COR_E_RANK = unchecked((int)0x80131517);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new RankException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_RANK, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "bad rank";
            var exception = new RankException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_RANK, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "bad rank";
            var innerException = new Exception("Inner exception");
            var exception = new RankException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_RANK, innerException: innerException, message: message);
        }
    }
}
