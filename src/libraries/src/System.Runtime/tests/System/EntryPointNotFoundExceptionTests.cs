// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class EntryPointNotFoundExceptionTests
    {
        public const int COR_E_ENTRYPOINTNOTFOUND = -2146233053; 

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new EntryPointNotFoundException();
            Assert.NotNull(exception);
            Assert.NotEmpty(exception.Message);
            Assert.NotNull(exception.ToString());
            Assert.Equal(COR_E_ENTRYPOINTNOTFOUND, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created EntryPointNotFoundException";
            var exception = new EntryPointNotFoundException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_ENTRYPOINTNOTFOUND, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created EntryPointNotFoundException";
            var innerException = new Exception("Created inner exception");
            var exception = new EntryPointNotFoundException(message, innerException);

            Assert.Equal(message, exception.Message);
            Assert.Equal(exception.GetBaseException().Message, "Created inner exception");
            Assert.Equal(COR_E_ENTRYPOINTNOTFOUND, exception.HResult);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }
    }
}