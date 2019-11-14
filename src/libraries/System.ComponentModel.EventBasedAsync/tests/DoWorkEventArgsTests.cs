// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.EventBasedAsync.Tests
{
    public static class DoWorkEventArgsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("non null test argument")]
        public static void CtorTest(object expectedArgument)
        {
            var target = new DoWorkEventArgs(expectedArgument);
            Assert.Equal(expectedArgument, target.Argument);
            Assert.False(target.Cancel);
            Assert.Null(target.Result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void CancelPropertyTest(bool expectedCancel)
        {
            var target = new DoWorkEventArgs(null) { Cancel = expectedCancel };
            Assert.Equal(expectedCancel, target.Cancel);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("non null test result")]
        public static void ResultPropertyTest(object expectedResult)
        {
            var target = new DoWorkEventArgs(null) { Result = expectedResult };
            Assert.Equal(expectedResult, target.Result);
        }
    }
}
