// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public static class InvalidDataExceptionTests
    {
        [Fact]
        public static void DefaultConstructor()
        {
            InvalidDataException ide = new InvalidDataException();

            Assert.NotNull(ide.Message);
        }

        [Fact]
        public static void MessageConstructor()
        {
            string message = "MessageConstructor";
            InvalidDataException ide = new InvalidDataException(message);

            Assert.Equal(message, ide.Message);
        }

        [Fact]
        public static void MessageInnerExceptionConstructor()
        {
            string message = "MessageConstructor";
            Exception innerException = new Exception();
            InvalidDataException ide = new InvalidDataException(message, innerException);

            Assert.Equal(message, ide.Message);
            Assert.Same(innerException, ide.InnerException);
        }
    }
}
