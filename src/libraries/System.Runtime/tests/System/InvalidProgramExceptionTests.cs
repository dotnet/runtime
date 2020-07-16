// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDPROGRAM, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "bad program";
            var exception = new InvalidProgramException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDPROGRAM, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "bad program";
            var innerException = new Exception("Inner exception");
            var exception = new InvalidProgramException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_INVALIDPROGRAM, innerException: innerException, message: message);
        }
    }
}
