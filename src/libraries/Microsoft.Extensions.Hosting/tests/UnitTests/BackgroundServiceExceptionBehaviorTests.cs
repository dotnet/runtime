// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class BackgroundServiceExceptionBehaviorTests
    {
        [Fact]
        public void EnumValues_HaveExpectedValues()
        {
            Assert.Equal(0, (int)BackgroundServiceExceptionBehavior.StopHost);
            Assert.Equal(1, (int)BackgroundServiceExceptionBehavior.Ignore);
        }

        [Fact]
        public void CanCompareValues()
        {
            var stopHost = BackgroundServiceExceptionBehavior.StopHost;
            var ignore = BackgroundServiceExceptionBehavior.Ignore;

            Assert.True(stopHost == BackgroundServiceExceptionBehavior.StopHost);
            Assert.True(ignore == BackgroundServiceExceptionBehavior.Ignore);
            Assert.False(stopHost == ignore);
        }
    }
}
