// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.Missing
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class DuplicateWaitObjectExceptionTests
    {
        private const int COR_E_DUPLICATEWAITOBJECT = unchecked((int)0x80131529);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new DuplicateWaitObjectException();
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_DUPLICATEWAITOBJECT, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string parameterName = "THISISAPARAMETERNAME";
            var exception = new DuplicateWaitObjectException(parameterName);
            Assert.True(exception.Message.Contains(parameterName));
            Assert.Equal(COR_E_DUPLICATEWAITOBJECT, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "CreatedDuplicateWaitObjectException";
            var innerException = new Exception("Created inner exception");
            var exception = new DuplicateWaitObjectException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_DUPLICATEWAITOBJECT, exception.HResult);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string parameterName = "THISISAPARAMETERNAME";
            string message = "CreatedDuplicateWaitObjectException";
            var exception = new DuplicateWaitObjectException(parameterName, message);
            Assert.True(exception.Message.Contains(parameterName));
            Assert.True(exception.Message.Contains(message));
        }
    }
}
