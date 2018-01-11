// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CardinalityMismatchExceptionTests
    {
        [Fact]
        public void Constructor1_ShouldSetMessagePropertyToDefault()
        {
            var exception = new ImportCardinalityMismatchException();

            ExceptionAssert.HasDefaultMessage(exception);
        }

        [Fact]
        public void Constructor2_NullAsMessageArgument_ShouldSetMessagePropertyToDefault()
        {
            var exception = new ImportCardinalityMismatchException((string)null);

            ExceptionAssert.HasDefaultMessage(exception);
        }

        [Fact]
        public void Constructor3_NullAsMessageArgument_ShouldSetMessagePropertyToDefault()
        {
            var exception = new ImportCardinalityMismatchException((string)null, new Exception());

            ExceptionAssert.HasDefaultMessage(exception);
        }

        [Fact]
        public void Constructor2_ValueAsMessageArgument_ShouldSetMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();

            foreach (var e in expectations)
            {
                var exception = new ImportCardinalityMismatchException(e);

                Assert.Equal(e, exception.Message);
            }
        }

        [Fact]
        public void Constructor3_ValueAsMessageArgument_ShouldSetMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();

            foreach (var e in expectations)
            {
                var exception = new ImportCardinalityMismatchException(e, new Exception());

                Assert.Equal(e, exception.Message);
            }
        }

        [Fact]
        public void Constructor1_ShouldSetInnerExceptionPropertyToNull()
        {
            var exception = new ImportCardinalityMismatchException();

            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor2_ShouldSetInnerExceptionPropertyToNull()
        {
            var exception = new ImportCardinalityMismatchException("Message");

            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor3_NullAsInnerExceptionArgument_ShouldSetInnerExceptionPropertyToNull()
        {
            var exception = new ImportCardinalityMismatchException("Message", (Exception)null);

            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor3_ValueAsInnerExceptionArgument_ShouldSetInnerExceptionProperty()
        {
            var expectations = Expectations.GetInnerExceptions();

            foreach (var e in expectations)
            {
                var exception = new ImportCardinalityMismatchException("Message", e);

                Assert.Same(e, exception.InnerException);
            }
        }
        
        private static ImportCardinalityMismatchException CreateCardinalityMismatchException()
        {
            return CreateCardinalityMismatchException((string)null, (Exception)null);
        }

        private static ImportCardinalityMismatchException CreateCardinalityMismatchException(string message)
        {
            return CreateCardinalityMismatchException(message, (Exception)null);
        }

        private static ImportCardinalityMismatchException CreateCardinalityMismatchException(Exception innerException)
        {
            return CreateCardinalityMismatchException((string)null, innerException);
        }

        private static ImportCardinalityMismatchException CreateCardinalityMismatchException(string message, Exception innerException)
        {
            return new ImportCardinalityMismatchException(message, innerException);
        }
    }
}
