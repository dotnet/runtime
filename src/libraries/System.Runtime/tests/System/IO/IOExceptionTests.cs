// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

using Xunit;

namespace System.Tests
{
    public static class IOExceptionTests
    {
        private const int COR_E_IO = unchecked((int)0x80131620);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new IOException();
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_IO, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "IO failure";
            var exception = new IOException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_IO, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "IO failure";
            var innerException = new Exception("Inner exception");
            var exception = new IOException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_IO, innerException: innerException, message: message);
        }

        [Fact]
        public static void Ctor_String_Int32()
        {
            string message = "IO failure";
            int hResult = unchecked((int)0x80424242);
            var exception = new IOException(message, hResult);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: hResult, message: message);
        }
    }
}
