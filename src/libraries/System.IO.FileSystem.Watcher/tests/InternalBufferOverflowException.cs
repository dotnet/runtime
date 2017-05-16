// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.Serialization.Formatters.Tests;

namespace System.IO.Tests
{
    public static class InternalBufferOverflowExceptionTests
    {
        [Fact]
        public static void DefaultConstructor()
        {
            InternalBufferOverflowException ide = new InternalBufferOverflowException();

            Assert.NotNull(ide.Message);
        }

        [Fact]
        public static void MessageConstructor()
        {
            string message = "MessageConstructor";
            InternalBufferOverflowException ide = new InternalBufferOverflowException(message);

            Assert.Equal(message, ide.Message);
        }

        [Fact]
        public static void MessageInnerExceptionConstructor()
        {
            string message = "MessageConstructor";
            Exception innerException = new Exception();
            InternalBufferOverflowException ide = new InternalBufferOverflowException(message, innerException);

            Assert.Equal(message, ide.Message);
            Assert.Same(innerException, ide.InnerException);
        }
    }
}
