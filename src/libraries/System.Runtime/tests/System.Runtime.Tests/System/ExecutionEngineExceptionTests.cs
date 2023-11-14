// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Xunit;

namespace System.Tests
{
    public static class ExecutionEngineExceptionTests
    {
        private const int COR_E_EXECUTIONENGINE = -2146233082;

        [Fact]
        public static void Ctor_Empty()
        {
#pragma warning disable 0618
            var exception = new ExecutionEngineException();
#pragma warning restore 0618
            Assert.NotNull(exception);
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_EXECUTIONENGINE, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created ExecutionEngineException";
#pragma warning disable 0618
            var exception = new ExecutionEngineException(message);
#pragma warning restore 0618
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_EXECUTIONENGINE, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created ExecutionEngineException";
            var innerException = new Exception("Created inner exception");
#pragma warning disable 0618
            var exception = new ExecutionEngineException(message, innerException);
#pragma warning restore 0618
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_EXECUTIONENGINE, exception.HResult);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }
    }
}
