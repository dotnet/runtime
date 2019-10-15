// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class ArrayTypeMismatchExceptionTests
    {
        private const int COR_E_ARRAYTYPEMISMATCH = unchecked((int)0x80131503);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new ArrayTypeMismatchException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_ARRAYTYPEMISMATCH, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "array type mismatch";
            var exception = new ArrayTypeMismatchException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_ARRAYTYPEMISMATCH, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "array type mismatch";
            var innerException = new Exception("Inner exception");
            var exception = new ArrayTypeMismatchException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_ARRAYTYPEMISMATCH, innerException: innerException, message: message);
        }
    }
}
