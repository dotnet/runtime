// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class HostDefaultsTests
    {
        [Fact]
        public void ApplicationKey_HasExpectedValue()
        {
            Assert.Equal("applicationName", HostDefaults.ApplicationKey);
        }

        [Fact]
        public void EnvironmentKey_HasExpectedValue()
        {
            Assert.Equal("environment", HostDefaults.EnvironmentKey);
        }

        [Fact]
        public void ContentRootKey_HasExpectedValue()
        {
            Assert.Equal("contentRoot", HostDefaults.ContentRootKey);
        }

        [Fact]
        public void AllKeys_AreDistinct()
        {
            Assert.NotEqual(HostDefaults.ApplicationKey, HostDefaults.EnvironmentKey);
            Assert.NotEqual(HostDefaults.ApplicationKey, HostDefaults.ContentRootKey);
            Assert.NotEqual(HostDefaults.EnvironmentKey, HostDefaults.ContentRootKey);
        }

        [Fact]
        public void Keys_AreNotNull()
        {
            Assert.NotNull(HostDefaults.ApplicationKey);
            Assert.NotNull(HostDefaults.EnvironmentKey);
            Assert.NotNull(HostDefaults.ContentRootKey);
        }

        [Fact]
        public void Keys_AreNotEmpty()
        {
            Assert.NotEmpty(HostDefaults.ApplicationKey);
            Assert.NotEmpty(HostDefaults.EnvironmentKey);
            Assert.NotEmpty(HostDefaults.ContentRootKey);
        }
    }
}
