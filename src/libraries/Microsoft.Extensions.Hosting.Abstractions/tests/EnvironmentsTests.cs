// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class EnvironmentsTests
    {
        [Fact]
        public void Development_HasExpectedValue()
        {
            Assert.Equal("Development", Environments.Development);
        }

        [Fact]
        public void Staging_HasExpectedValue()
        {
            Assert.Equal("Staging", Environments.Staging);
        }

        [Fact]
        public void Production_HasExpectedValue()
        {
            Assert.Equal("Production", Environments.Production);
        }

        [Fact]
        public void AllEnvironments_AreDistinct()
        {
            Assert.NotEqual(Environments.Development, Environments.Staging);
            Assert.NotEqual(Environments.Development, Environments.Production);
            Assert.NotEqual(Environments.Staging, Environments.Production);
        }

        [Fact]
        public void Environments_AreNotNull()
        {
            Assert.NotNull(Environments.Development);
            Assert.NotNull(Environments.Staging);
            Assert.NotNull(Environments.Production);
        }

        [Fact]
        public void Environments_AreNotEmpty()
        {
            Assert.NotEmpty(Environments.Development);
            Assert.NotEmpty(Environments.Staging);
            Assert.NotEmpty(Environments.Production);
        }
    }
}
