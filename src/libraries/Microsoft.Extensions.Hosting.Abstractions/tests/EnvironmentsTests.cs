// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class EnvironmentsTests
    {
        [Fact]
        public void EnvironmentConstants_HaveExpectedValues()
        {
            Assert.Equal("Development", Environments.Development);
            Assert.Equal("Staging", Environments.Staging);
            Assert.Equal("Production", Environments.Production);
        }
    }
}
