// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Tests
{
    public static class UnreachableExceptionTests
    {
        [Fact]
        public static void DefaultConstructor()
        {
            var unreachableException = new UnreachableException();

            Assert.NotNull(unreachableException.Message);
        }

        [Fact]
        public static void MessageConstructor()
        {
            var message = "MessageConstructor";
            var unreachableException = new UnreachableException(message);

            Assert.Equal(message, unreachableException.Message);
        }

        [Fact]
        public static void MessageInnerExceptionConstructor()
        {
            var message = "MessageConstructor";
            var innerException = new Exception();
            var unreachableException = new UnreachableException();

            Assert.Equal(message, unreachableException.Message);
            Assert.Same(innerException, unreachableException.InnerException);
        }
    }
}
