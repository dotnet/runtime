// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class BackgroundServiceExceptionBehaviorTests
    {
        [Fact]
        public void StopHost_HasExpectedValue()
        {
            Assert.Equal(0, (int)BackgroundServiceExceptionBehavior.StopHost);
        }

        [Fact]
        public void Ignore_HasExpectedValue()
        {
            Assert.Equal(1, (int)BackgroundServiceExceptionBehavior.Ignore);
        }

        [Fact]
        public void EnumValues_AreUnique()
        {
            Assert.NotEqual(
                (int)BackgroundServiceExceptionBehavior.StopHost,
                (int)BackgroundServiceExceptionBehavior.Ignore);
        }

        [Fact]
        public void CanAssignToVariable()
        {
            BackgroundServiceExceptionBehavior behavior = BackgroundServiceExceptionBehavior.StopHost;
            Assert.Equal(BackgroundServiceExceptionBehavior.StopHost, behavior);

            behavior = BackgroundServiceExceptionBehavior.Ignore;
            Assert.Equal(BackgroundServiceExceptionBehavior.Ignore, behavior);
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
