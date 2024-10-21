// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using Xunit;

namespace System.Tests
{
    public static class AmbiguousImplementationExceptionTests
    {
        private const int COR_E_AMBIGUOUSIMPLEMENTATION = -2146234262;

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new AmbiguousImplementationException();
            Assert.NotNull(exception);
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_AMBIGUOUSIMPLEMENTATION, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created AmbiguousImplementationException";
            var exception = new AmbiguousImplementationException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_AMBIGUOUSIMPLEMENTATION, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created AmbiguousImplementationException";
            var innerException = new Exception("Created inner exception");
            var exception = new AmbiguousImplementationException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_AMBIGUOUSIMPLEMENTATION, exception.HResult);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }
    }
}
