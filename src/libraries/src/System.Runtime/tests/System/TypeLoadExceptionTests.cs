// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class TypeLoadExceptionTests
    {
        private const int COR_E_TYPELOAD = unchecked((int)0x80131522);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new TypeLoadException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_TYPELOAD, validateMessage: false);
            Assert.Equal("", exception.TypeName);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "type failed to load";
            var exception = new TypeLoadException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_TYPELOAD, message: message);
            Assert.Equal("", exception.TypeName);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "type failed to load";
            var innerException = new Exception("Inner exception");
            var exception = new TypeLoadException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_TYPELOAD, innerException: innerException, message: message);
            Assert.Equal("", exception.TypeName);
        }
    }
}
