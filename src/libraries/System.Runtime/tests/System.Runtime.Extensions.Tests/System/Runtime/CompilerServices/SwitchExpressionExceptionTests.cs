// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class SwitchExpressionExceptionTests
    {
        [Fact]
        public void Constructors()
        {
            string message = "exception message";
            var e = new SwitchExpressionException();
            Assert.NotEmpty(e.Message);
            Assert.Null(e.InnerException);

            e = new SwitchExpressionException(message);
            Assert.Equal(message, e.Message);
            Assert.Null(e.InnerException);

            var inner = new Exception();
            e = new SwitchExpressionException(message, inner);
            Assert.Equal(message, e.Message);
            Assert.Same(inner, e.InnerException);
        }

        [Fact]
        public static void Constructor_StringVsObjectArg()
        {
            object message = "exception message";
            var ex = new SwitchExpressionException(message as object);

            Assert.NotEqual(message, ex.Message);
            Assert.Same(message, ex.UnmatchedValue);

            ex = new SwitchExpressionException(message as string);

            Assert.Same(message, ex.Message);
            Assert.Null(ex.UnmatchedValue);
        }

        [Fact]
        public void UnmatchedValue_Null()
        {
            var ex = new SwitchExpressionException((object)null);
            Assert.Null(ex.UnmatchedValue);
        }

        [Theory]
        [InlineData(34)]
        [InlineData(new byte[] { 1, 2, 3 })]
        [InlineData(true)]
        [InlineData("34")]
        public void UnmatchedValue_NotNull(object unmatchedValue)
        {
            var ex = new SwitchExpressionException(unmatchedValue);
            Assert.Equal(unmatchedValue, ex.UnmatchedValue);
            Assert.Contains(ex.UnmatchedValue.ToString(), ex.Message);
        }
    }
}
