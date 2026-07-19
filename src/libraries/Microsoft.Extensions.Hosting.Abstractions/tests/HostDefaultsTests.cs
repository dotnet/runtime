// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class HostDefaultsTests
    {
        [Fact]
        public void KeyConstants_HaveExpectedValues()
        {
            Assert.Equal("applicationName", HostDefaults.ApplicationKey);
            Assert.Equal("environment", HostDefaults.EnvironmentKey);
            Assert.Equal("contentRoot", HostDefaults.ContentRootKey);
        }
    }
}
