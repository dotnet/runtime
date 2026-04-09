// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class HostApplicationBuilderSettingsTests
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var settings = new HostApplicationBuilderSettings();

            Assert.False(settings.DisableDefaults);
            Assert.Null(settings.Args);
            Assert.Null(settings.Configuration);
            Assert.Null(settings.EnvironmentName);
            Assert.Null(settings.ApplicationName);
            Assert.Null(settings.ContentRootPath);
        }

        [Fact]
        public void AllProperties_CanBeSetTogether()
        {
            var args = new[] { "--key", "value" };
            var configuration = new ConfigurationManager();
            var settings = new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
                Args = args,
                Configuration = configuration,
                EnvironmentName = "Development",
                ApplicationName = "TestApp",
                ContentRootPath = "/test"
            };

            Assert.True(settings.DisableDefaults);
            Assert.Same(args, settings.Args);
            Assert.Same(configuration, settings.Configuration);
            Assert.Equal("Development", settings.EnvironmentName);
            Assert.Equal("TestApp", settings.ApplicationName);
            Assert.Equal("/test", settings.ContentRootPath);
        }

        [Fact]
        public void EmptyArgs_CanBeSet()
        {
            var settings = new HostApplicationBuilderSettings
            {
                Args = new string[0]
            };

            Assert.NotNull(settings.Args);
            Assert.Empty(settings.Args);
        }

        [Fact]
        public void Properties_CanBeCleared()
        {
            var settings = new HostApplicationBuilderSettings
            {
                Args = new[] { "test" },
                EnvironmentName = "Test",
                ApplicationName = "App",
                ContentRootPath = "/path"
            };

            settings.Args = null;
            settings.EnvironmentName = null;
            settings.ApplicationName = null;
            settings.ContentRootPath = null;

            Assert.Null(settings.Args);
            Assert.Null(settings.EnvironmentName);
            Assert.Null(settings.ApplicationName);
            Assert.Null(settings.ContentRootPath);
        }
    }
}
