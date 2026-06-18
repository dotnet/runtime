// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class HostEnvironmentEnvExtensionsTests
    {
        [Fact]
        public void IsDevelopment_WithDevelopmentEnvironment_ReturnsTrue()
        {
            var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };

            Assert.True(environment.IsDevelopment());
        }

        [Fact]
        public void IsDevelopment_WithNonDevelopmentEnvironment_ReturnsFalse()
        {
            var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };

            Assert.False(environment.IsDevelopment());
        }

        [Fact]
        public void IsDevelopment_WithNullEnvironment_ThrowsArgumentNullException()
        {
            IHostEnvironment environment = null;

            Assert.Throws<ArgumentNullException>(() => environment.IsDevelopment());
        }

        [Fact]
        public void IsStaging_WithStagingEnvironment_ReturnsTrue()
        {
            var environment = new TestHostEnvironment { EnvironmentName = Environments.Staging };

            Assert.True(environment.IsStaging());
        }

        [Fact]
        public void IsStaging_WithNonStagingEnvironment_ReturnsFalse()
        {
            var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };

            Assert.False(environment.IsStaging());
        }

        [Fact]
        public void IsStaging_WithNullEnvironment_ThrowsArgumentNullException()
        {
            IHostEnvironment environment = null;

            Assert.Throws<ArgumentNullException>(() => environment.IsStaging());
        }

        [Fact]
        public void IsProduction_WithProductionEnvironment_ReturnsTrue()
        {
            var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };

            Assert.True(environment.IsProduction());
        }

        [Fact]
        public void IsProduction_WithNonProductionEnvironment_ReturnsFalse()
        {
            var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };

            Assert.False(environment.IsProduction());
        }

        [Fact]
        public void IsProduction_WithNullEnvironment_ThrowsArgumentNullException()
        {
            IHostEnvironment environment = null;

            Assert.Throws<ArgumentNullException>(() => environment.IsProduction());
        }

        [Theory]
        [InlineData("Development", "Development", true)]
        [InlineData("Development", "development", true)]
        [InlineData("Development", "DEVELOPMENT", true)]
        [InlineData("Development", "Production", false)]
        [InlineData("Production", "Production", true)]
        [InlineData("Staging", "staging", true)]
        [InlineData("Custom", "Custom", true)]
        [InlineData("Custom", "custom", true)]
        public void IsEnvironment_ComparesEnvironmentNameCaseInsensitive(string actualEnvironment, string testEnvironment, bool expected)
        {
            var environment = new TestHostEnvironment { EnvironmentName = actualEnvironment };

            Assert.Equal(expected, environment.IsEnvironment(testEnvironment));
        }

        [Fact]
        public void IsEnvironment_WithNullEnvironment_ThrowsArgumentNullException()
        {
            IHostEnvironment environment = null;

            Assert.Throws<ArgumentNullException>(() => environment.IsEnvironment("Development"));
        }

        [Fact]
        public void IsEnvironment_WithEmptyString_ReturnsFalseForNonEmptyEnvironmentName()
        {
            var environment = new TestHostEnvironment { EnvironmentName = "Development" };

            Assert.False(environment.IsEnvironment(string.Empty));
        }

        [Fact]
        public void IsEnvironment_WithEmptyEnvironmentName_ReturnsTrueForEmptyString()
        {
            var environment = new TestHostEnvironment { EnvironmentName = string.Empty };

            Assert.True(environment.IsEnvironment(string.Empty));
        }

        [Fact]
        public void EnvironmentChecks_AreCaseInsensitive()
        {
            var devEnv = new TestHostEnvironment { EnvironmentName = "development" };
            var stagingEnv = new TestHostEnvironment { EnvironmentName = "STAGING" };
            var prodEnv = new TestHostEnvironment { EnvironmentName = "PrOdUcTiOn" };

            Assert.True(devEnv.IsDevelopment());
            Assert.True(stagingEnv.IsStaging());
            Assert.True(prodEnv.IsProduction());
        }

        private class TestHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = string.Empty;
            public string ApplicationName { get; set; } = string.Empty;
            public string ContentRootPath { get; set; } = string.Empty;
            public IFileProvider ContentRootFileProvider { get; set; } = null!;
        }
    }
}
