// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Tests
{
    public static class MissingFieldExceptionTests
    {
        private const int COR_E_MISSINGFIELD = unchecked((int)0x80131511);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new MissingFieldException();
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_MISSINGFIELD, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created MissingFieldException";
            var exception = new MissingFieldException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_MISSINGFIELD, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created MissingFieldException";
            var innerException = new Exception("Created inner exception");
            var exception = new MissingFieldException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_MISSINGFIELD, exception.HResult);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string className = "class";
            string memberName = "member";
            var exception = new MissingFieldException(className, memberName);
            Assert.Contains(className, exception.Message);
            Assert.Contains(memberName, exception.Message);
            Assert.Equal(COR_E_MISSINGFIELD, exception.HResult);
        }
    }
}
