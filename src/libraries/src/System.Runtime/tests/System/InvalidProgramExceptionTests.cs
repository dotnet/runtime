// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class InvalidProgramExceptionTests
    {
        private const int COR_E_INVALIDPROGRAM = unchecked((int)0x8013153A);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new InvalidProgramException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDPROGRAM, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "bad program";
            var exception = new InvalidProgramException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDPROGRAM, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "bad program";
            var innerException = new Exception("Inner exception");
            var exception = new InvalidProgramException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDPROGRAM, innerException: innerException, message: message);
        }
    }
}
