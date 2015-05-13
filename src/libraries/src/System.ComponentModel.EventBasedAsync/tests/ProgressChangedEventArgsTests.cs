// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.EventBasedAsync.Tests
{
    public static class ProgressChangedEventArgsTests
    {
        [Theory]
        [InlineData(int.MinValue, null)]
        [InlineData(0, "non null test state")]
        [InlineData(int.MaxValue, "non null test state")]
        public static void CtorAcceptsValuesAsIs(int expectedPercentage, object expectedState)
        {
            var target = new ProgressChangedEventArgs(expectedPercentage, expectedState);
            Assert.Equal(expectedPercentage, target.ProgressPercentage);
            Assert.Equal(expectedState, target.UserState);
        }
    }
}
