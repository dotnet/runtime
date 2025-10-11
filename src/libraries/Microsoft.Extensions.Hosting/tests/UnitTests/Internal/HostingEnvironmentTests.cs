// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Microsoft.Extensions.Hosting.Internal.Tests
{
    public class HostingEnvironmentTests
    {
        [Fact]
        public void DefaultValues_AreEmptyStrings()
        {
            var environment = new HostingEnvironment();

            Assert.Equal(string.Empty, environment.EnvironmentName);
            Assert.Equal(string.Empty, environment.ApplicationName);
            Assert.Equal(string.Empty, environment.ContentRootPath);
        }

        [Fact]
        public void EnvironmentName_CanBeSet()
        {
            var environment = new HostingEnvironment
            {
                EnvironmentName = "Development"
            };

            Assert.Equal("Development", environment.EnvironmentName);
        }

        [Fact]
        public void ApplicationName_CanBeSet()
        {
            var environment = new HostingEnvironment
            {
                ApplicationName = "MyApp"
            };

            Assert.Equal("MyApp", environment.ApplicationName);
        }

        [Fact]
        public void ContentRootPath_CanBeSet()
        {
            var environment = new HostingEnvironment
            {
                ContentRootPath = "/app"
            };

            Assert.Equal("/app", environment.ContentRootPath);
        }

        [Fact]
        public void ContentRootFileProvider_CanBeSet()
        {
            var environment = new HostingEnvironment();
            var fileProvider = new PhysicalFileProvider("/");

            environment.ContentRootFileProvider = fileProvider;

            Assert.Same(fileProvider, environment.ContentRootFileProvider);
        }

        [Fact]
        public void AllProperties_CanBeSetTogether()
        {
            var fileProvider = new PhysicalFileProvider("/");
            var environment = new HostingEnvironment
            {
                EnvironmentName = "Production",
                ApplicationName = "TestApplication",
                ContentRootPath = "/test/path",
                ContentRootFileProvider = fileProvider
            };

            Assert.Equal("Production", environment.EnvironmentName);
            Assert.Equal("TestApplication", environment.ApplicationName);
            Assert.Equal("/test/path", environment.ContentRootPath);
            Assert.Same(fileProvider, environment.ContentRootFileProvider);
        }

        [Fact]
        public void ImplementsIHostEnvironment()
        {
            var environment = new HostingEnvironment();
            Assert.IsAssignableFrom<IHostEnvironment>(environment);
        }

        [Fact]
        public void ImplementsIHostingEnvironment()
        {
            var environment = new HostingEnvironment();
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.IsAssignableFrom<IHostingEnvironment>(environment);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void Properties_CanBeUpdated()
        {
            var environment = new HostingEnvironment
            {
                EnvironmentName = "Development",
                ApplicationName = "App1"
            };

            environment.EnvironmentName = "Staging";
            environment.ApplicationName = "App2";

            Assert.Equal("Staging", environment.EnvironmentName);
            Assert.Equal("App2", environment.ApplicationName);
        }
    }
}
