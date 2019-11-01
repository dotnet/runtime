// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Xunit;

namespace System.Tests
{
    public static class KeyNotFoundExceptionTests
    {
        private const int COR_E_KEYNOTFOUND = unchecked((int)0x80131577);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new KeyNotFoundException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_KEYNOTFOUND, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "this is not the key you're looking for";
            var exception = new KeyNotFoundException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_KEYNOTFOUND, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "this is not the key you're looking for";
            var innerException = new Exception("Inner exception");
            var exception = new KeyNotFoundException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_KEYNOTFOUND, innerException: innerException, message: message);
        }
    }
}
