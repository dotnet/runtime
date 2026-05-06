// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENT, validateMessage: false);
            Assert.Null(exception.ParamName);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "the argument is wrong";
            var exception = new ArgumentException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENT, message: message);
            Assert.Null(exception.ParamName);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "the argument is wrong";
            var innerException = new Exception("Inner exception");
            var exception = new ArgumentException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENT, innerException: innerException, message: message);
            Assert.Null(exception.ParamName);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "the argument is wrong";
            string argumentName = "theArgument";
            var exception = new ArgumentException(message, argumentName);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENT, validateMessage: false);
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
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENT, innerException: innerException, validateMessage: false);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Contains(message, exception.Message);
            Assert.Contains(argumentName, exception.Message);
        }

        [Fact]
        public static void ThrowIfNullOrEmpty_ThrowsForInvalidInput()
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => ArgumentException.ThrowIfNullOrEmpty(null, null));
            AssertExtensions.Throws<ArgumentNullException>("something", () => ArgumentException.ThrowIfNullOrEmpty(null, "something"));

            AssertExtensions.Throws<ArgumentException>(null, () => ArgumentException.ThrowIfNullOrEmpty("", null));
            AssertExtensions.Throws<ArgumentException>("something", () => ArgumentException.ThrowIfNullOrEmpty("", "something"));

            ArgumentException.ThrowIfNullOrEmpty(" ");
            ArgumentException.ThrowIfNullOrEmpty(" ", "something");
            ArgumentException.ThrowIfNullOrEmpty("abc", "something");
        }

        [Fact]
        public static void ThrowIfNullOrEmpty_UsesArgumentExpression_ParameterNameMatches()
        {
            string someString = null;
            AssertExtensions.Throws<ArgumentNullException>(nameof(someString), () => ArgumentException.ThrowIfNullOrEmpty(someString));

            someString = "";
            AssertExtensions.Throws<ArgumentException>(nameof(someString), () => ArgumentException.ThrowIfNullOrEmpty(someString));

            someString = "abc";
            ArgumentException.ThrowIfNullOrEmpty(someString);
        }

        [Fact]
        public static void ThrowIfNullOrWhiteSpace_ThrowsForInvalidInput()
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => ArgumentException.ThrowIfNullOrWhiteSpace(null, null));
            AssertExtensions.Throws<ArgumentNullException>("something", () => ArgumentException.ThrowIfNullOrWhiteSpace(null, "something"));

            AssertExtensions.Throws<ArgumentException>(null, () => ArgumentException.ThrowIfNullOrWhiteSpace("", null));
            AssertExtensions.Throws<ArgumentException>("something", () => ArgumentException.ThrowIfNullOrWhiteSpace("", "something"));

            string allWhitespace = "\u0009\u000A\u000B\u000C\u000D\u0020\u0085\u00A0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u2028\u2029\u202F\u205F\u3000";
            AssertExtensions.Throws<ArgumentException>("something", () => ArgumentException.ThrowIfNullOrWhiteSpace(" ", "something"));
            AssertExtensions.Throws<ArgumentException>("something", () => ArgumentException.ThrowIfNullOrWhiteSpace(allWhitespace, "something"));
            ArgumentException.ThrowIfNullOrWhiteSpace("a" + allWhitespace, "something");
            ArgumentException.ThrowIfNullOrWhiteSpace(allWhitespace + "a", "something");
            ArgumentException.ThrowIfNullOrWhiteSpace(allWhitespace.Substring(0, 5) + "a" + allWhitespace.Substring(5), "something");
        }

        [Fact]
        public static void ThrowIfNullOrWhiteSpace_UsesArgumentExpression_ParameterNameMatches()
        {
            string someString = null;
            AssertExtensions.Throws<ArgumentNullException>(nameof(someString), () => ArgumentException.ThrowIfNullOrWhiteSpace(someString));

            someString = "";
            AssertExtensions.Throws<ArgumentException>(nameof(someString), () => ArgumentException.ThrowIfNullOrWhiteSpace(someString));

            someString = "    ";
            AssertExtensions.Throws<ArgumentException>(nameof(someString), () => ArgumentException.ThrowIfNullOrWhiteSpace(someString));
        }
    }
}
