// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Xunit;

namespace System.Tests
{
    public static class UnreachableExceptionTests
    {
        [Fact]
        public static void DefaultConstructor()
        {
            UnreachableException unreachableException = new UnreachableException();

            Assert.NotNull(unreachableException.Message);
        }

        [Fact]
        public static void MessageConstructor()
        {
            string message = "MessageConstructor";
            UnreachableException unreachableException = new UnreachableException(message);

            Assert.Equal(message, unreachableException.Message);
        }

        [Fact]
        public static void MessageInnerExceptionConstructor()
        {
            string message = "MessageConstructor";
            Exception innerException = new Exception();
            UnreachableException unreachableException = new UnreachableException(message, innerException);

            Assert.Equal(message, unreachableException.Message);
            Assert.Same(innerException, unreachableException.InnerException);
        }
    }
}
