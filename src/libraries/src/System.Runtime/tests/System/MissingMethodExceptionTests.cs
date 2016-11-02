// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class MissingMethodExceptionTests
    {
        private const int COR_E_MISSINGMETHOD = unchecked((int)0x80131513);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new MissingMethodException();
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_MISSINGMETHOD, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created MissingMethodException";
            var exception = new MissingMethodException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_MISSINGMETHOD, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created MissingMethodException";
            var innerException = new Exception("Created inner exception");
            var exception = new MissingMethodException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_MISSINGMETHOD, exception.HResult);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }
#if netstandard17
        [Fact]
        public static void Ctor_String_String()
        {
            string className = "class";
            string memberName = "member";
            var exception = new MissingMethodException(className, memberName);
            Assert.True(exception.Message.Contains(className));
            Assert.True(exception.Message.Contains(memberName));
        }
#endif
    }
}
