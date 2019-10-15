// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class TypeUnloadedExceptionTests
    {
        private const int COR_E_TYPEUNLOADED = unchecked((int)0x80131013);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new TypeUnloadedException();
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_TYPEUNLOADED, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created TypeUnloadedException ";
            var exception = new TypeUnloadedException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_TYPEUNLOADED, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created TypeUnloadedException ";
            var innerException = new Exception("Created inner exception");
            var exception = new TypeUnloadedException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_TYPEUNLOADED, exception.HResult);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }
    }
}
