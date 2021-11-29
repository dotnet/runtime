// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: E_POINTER, validateMessage: false);
            Assert.Null(exception.ParamName);
        }

        [Fact]
        public static void Ctor_String()
        {
            string argumentName = "theNullArgument";
            var exception = new ArgumentNullException(argumentName);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: E_POINTER, validateMessage: false);
            Assert.Contains(argumentName, exception.Message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "the argument is null";
            var innerException = new Exception("Inner exception");
            var exception = new ArgumentNullException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: E_POINTER, innerException: innerException, message: message);
            Assert.Null(exception.ParamName);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "the argument is null";
            string argumentName = "theNullArgument";
            var exception = new ArgumentNullException(argumentName, message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: E_POINTER, validateMessage: false);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Contains(message, exception.Message);
            Assert.Contains(argumentName, exception.Message);
        }

        [Fact]
        public static void ThrowIfNull_NonNull_DoesntThrow()
        {
            foreach (object o in new[] { new object(), "", "argument" })
            {
                ArgumentNullException.ThrowIfNull(o);
                ArgumentNullException.ThrowIfNull(o, "paramName");
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public static void ThrowIfNull_Null_ThrowsArgumentNullException(string paramName)
        {
            AssertExtensions.Throws<ArgumentNullException>(paramName, () => ArgumentNullException.ThrowIfNull(null, paramName));
        }

        [Fact]
        public static void ThrowIfNull_UsesArgumentExpression()
        {
            object something = null;
            AssertExtensions.Throws<ArgumentNullException>(nameof(something), () => ArgumentNullException.ThrowIfNull(something));
        }
    }
}
