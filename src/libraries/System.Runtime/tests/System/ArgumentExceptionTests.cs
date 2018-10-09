// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class ArgumentExceptionTests
    {
        private const int COR_E_ARGUMENT = unchecked((int)0x80070057);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new ArgumentException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENT, validateMessage: false);
            Assert.Null(exception.ParamName);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "the argument is wrong";
            var exception = new ArgumentException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENT, message: message);
            Assert.Null(exception.ParamName);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "the argument is wrong";
            var innerException = new Exception("Inner exception");
            var exception = new ArgumentException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENT, innerException: innerException, message: message);
            Assert.Null(exception.ParamName);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "the argument is wrong";
            string argumentName = "theArgument";
            var exception = new ArgumentException(message, argumentName);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENT, validateMessage: false);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Contains(message, exception.Message);
            Assert.Contains(argumentName, exception.Message);
        }

        [Fact]
        public static void Ctor_String_String_Exception()
        {
            string message = "the argument is wrong";
            string argumentName = "theArgument";
            var innerException = new Exception("Inner exception");
            var exception = new ArgumentException(message, argumentName, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENT, innerException: innerException, validateMessage: false);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Contains(message, exception.Message);
            Assert.Contains(argumentName, exception.Message);
        }
    }
}
