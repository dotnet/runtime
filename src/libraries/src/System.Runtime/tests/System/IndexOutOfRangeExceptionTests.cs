// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class IndexOutOfRangeExceptionTests
    {
        private const int COR_E_INDEXOUTOFRANGE = unchecked((int)0x80131508);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new IndexOutOfRangeException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INDEXOUTOFRANGE, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "out of range";
            var exception = new IndexOutOfRangeException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INDEXOUTOFRANGE, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "out of range";
            var innerException = new Exception("Inner exception");
            var exception = new IndexOutOfRangeException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_INDEXOUTOFRANGE, innerException: innerException, message: message);
        }
    }
}
