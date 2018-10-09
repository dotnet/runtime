// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class NotImplementedExceptionTests
    {
        private const int E_NOTIMPL = unchecked((int)0x80004001);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new NotImplementedException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: E_NOTIMPL, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "not implemented";
            var exception = new NotImplementedException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: E_NOTIMPL, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "not implemented";
            var innerException = new Exception("Inner exception");
            var exception = new NotImplementedException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: E_NOTIMPL, innerException: innerException, message: message);
        }
    }
}
