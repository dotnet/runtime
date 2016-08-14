// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            var exception = new ExecutionEngineException();
            Assert.NotNull(exception);
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_EXECUTIONENGINE, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created ExecutionEngineException";
            var exception = new ExecutionEngineException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_EXECUTIONENGINE, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created ExecutionEngineException";
            var innerException = new Exception("Created inner exception");
            var exception = new ExecutionEngineException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_EXECUTIONENGINE, exception.HResult);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }
    }
}
