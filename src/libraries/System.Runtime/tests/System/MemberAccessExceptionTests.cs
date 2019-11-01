// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class MemberAccessExceptionTests
    {
        private const int COR_E_MEMBERACCESS = unchecked((int)0x8013151A);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new MemberAccessException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_MEMBERACCESS, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "you cannot access this member";
            var exception = new MemberAccessException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_MEMBERACCESS, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "you cannot access this member";
            var innerException = new Exception("Inner exception");
            var exception = new MemberAccessException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_MEMBERACCESS, innerException: innerException, message: message);
        }
    }
}
