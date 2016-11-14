// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public static class ExceptionTests
    {
        [Fact]
        public static void TargetException()
        {
            TargetException ex = new TargetException();

            Assert.NotEmpty(ex.Message);
            Assert.Null(ex.InnerException);

            TargetException caught = Assert.Throws<TargetException>(() => ThrowGivenException(ex));
            Assert.Same(ex, caught);
        }

        [Fact]
        public static void TargetException_Message()
        {
            string message = "message";
            TargetException ex = new TargetException(message);

            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);

            TargetException caught = Assert.Throws<TargetException>(() => ThrowGivenException(ex));
            Assert.Same(ex, caught);
        }

        [Fact]
        public static void TargetException_Message_InnerException()
        {
            string message = "message";
            Exception inner = new Exception();
            TargetException ex = new TargetException(message, inner);

            Assert.Equal(message, ex.Message);
            Assert.Same(inner, ex.InnerException);

            TargetException caught = Assert.Throws<TargetException>(() => ThrowGivenException(ex));
            Assert.Same(ex, caught);
        }

        [Fact]
        public static void InvalidFilterCriteriaException()
        {
            InvalidFilterCriteriaException ex = new InvalidFilterCriteriaException();

            Assert.NotEmpty(ex.Message);
            Assert.Null(ex.InnerException);

            InvalidFilterCriteriaException caught = Assert.Throws<InvalidFilterCriteriaException>(() => ThrowGivenException(ex));
            Assert.Same(ex, caught);
        }

        [Fact]
        public static void InvalidFilterCriteriaException_Message()
        {
            string message = "message";
            InvalidFilterCriteriaException ex = new InvalidFilterCriteriaException(message);

            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);

            InvalidFilterCriteriaException caught = Assert.Throws<InvalidFilterCriteriaException>(() => ThrowGivenException(ex));
            Assert.Same(ex, caught);
        }

        [Fact]
        public static void InvalidFilterCriteriaException_Message_InnerException()
        {
            string message = "message";
            Exception inner = new Exception();
            InvalidFilterCriteriaException ex = new InvalidFilterCriteriaException(message, inner);

            Assert.Equal(message, ex.Message);
            Assert.Same(inner, ex.InnerException);

            InvalidFilterCriteriaException caught = Assert.Throws<InvalidFilterCriteriaException>(() => ThrowGivenException(ex));
            Assert.Same(ex, caught);
        }

        private static void ThrowGivenException(Exception ex)
        {
            throw ex;
        }
    }
}
