// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public static class ArgumentOutOfRangeExceptionTests
    {
        private const int COR_E_ARGUMENTOUTOFRANGE = unchecked((int)0x80131502);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new ArgumentOutOfRangeException();
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENTOUTOFRANGE, validateMessage: false);
            Assert.Null(exception.ParamName);
            Assert.Null(exception.ActualValue);
        }

        [Fact]
        public static void Ctor_String()
        {
            string argumentName = "theArgument";
            var exception = new ArgumentOutOfRangeException(argumentName);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENTOUTOFRANGE, validateMessage: false);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Null(exception.ActualValue);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "the argument is out of range";
            var innerException = new Exception("Inner exception");
            var exception = new ArgumentOutOfRangeException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENTOUTOFRANGE, innerException: innerException, message: message);
            Assert.Null(exception.ParamName);
            Assert.Null(exception.ActualValue);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "the argument is out of range";
            string argumentName = "theArgument";
            var exception = new ArgumentOutOfRangeException(argumentName, message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENTOUTOFRANGE, validateMessage: false);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Null(exception.ActualValue);
            Assert.Contains(message, exception.Message);
            Assert.Contains(argumentName, exception.Message);
        }

        [Fact]
        public static void Ctor_String_Object_String()
        {
            string message = "the argument is out of range";
            string argumentName = "theArgument";
            int argumentValue = Int32.MaxValue;
            var exception = new ArgumentOutOfRangeException(argumentName, argumentValue, message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENTOUTOFRANGE, validateMessage: false);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Contains(message, exception.Message);
            Assert.Contains(argumentName, exception.Message);
            Assert.Contains(argumentValue.ToString(), exception.Message);
        }
    }
}
