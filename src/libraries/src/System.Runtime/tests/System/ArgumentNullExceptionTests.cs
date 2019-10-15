// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class ArgumentNullExceptionTests
    {
        private const int E_POINTER = unchecked((int)0x80004003);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new ArgumentNullException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: E_POINTER, validateMessage: false);
            Assert.Null(exception.ParamName);
        }

        [Fact]
        public static void Ctor_String()
        {
            string argumentName = "theNullArgument";
            var exception = new ArgumentNullException(argumentName);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: E_POINTER, validateMessage: false);
            Assert.Contains(argumentName, exception.Message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "the argument is null";
            var innerException = new Exception("Inner exception");
            var exception = new ArgumentNullException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: E_POINTER, innerException: innerException, message: message);
            Assert.Null(exception.ParamName);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "the argument is null";
            string argumentName = "theNullArgument";
            var exception = new ArgumentNullException(argumentName, message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: E_POINTER, validateMessage: false);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Contains(message, exception.Message);
            Assert.Contains(argumentName, exception.Message);
        }
    }
}
