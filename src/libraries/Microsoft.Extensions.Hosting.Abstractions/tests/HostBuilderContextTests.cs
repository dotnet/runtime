// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    using AssertExtensions = System.AssertExtensions;

    public class HostBuilderContextTests
    {
        [Fact]
        public void Constructor_WithProperties_InitializesProperties()
        {
            var properties = new Dictionary<object, object>();
            var context = new HostBuilderContext(properties);

            Assert.Same(properties, context.Properties);
        }

        [Fact]
        public void Constructor_WithNullProperties_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("properties", () => new HostBuilderContext(null));
        }

        [Fact]
        public void HostingEnvironment_CanBeSet()
        {
            var properties = new Dictionary<object, object>();
            var context = new HostBuilderContext(properties);
            var environment = new TestHostEnvironment();

            context.HostingEnvironment = environment;

            Assert.Same(environment, context.HostingEnvironment);
        }

        [Fact]
        public void Configuration_CanBeSet()
        {
            var properties = new Dictionary<object, object>();
            var context = new HostBuilderContext(properties);
            var configuration = new ConfigurationBuilder().Build();

            context.Configuration = configuration;

            Assert.Same(configuration, context.Configuration);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            var properties = new Dictionary<object, object>();
            var context = new HostBuilderContext(properties);

            context.Properties["key1"] = "value1";
            context.Properties["key2"] = 42;

            Assert.Equal("value1", context.Properties["key1"]);
            Assert.Equal(42, context.Properties["key2"]);
        }

        [Fact]
        public void Properties_SharedWithConstructorDictionary()
        {
            var properties = new Dictionary<object, object>
            {
                ["existing"] = "value"
            };
            var context = new HostBuilderContext(properties);

            properties["new"] = "added";

            Assert.Equal("added", context.Properties["new"]);
        }

        [Fact]
        public void AllProperties_CanBeSetTogether()
        {
            var properties = new Dictionary<object, object>();
            var context = new HostBuilderContext(properties)
            {
                HostingEnvironment = new TestHostEnvironment(),
                Configuration = new ConfigurationBuilder().Build()
            };

            Assert.NotNull(context.HostingEnvironment);
            Assert.NotNull(context.Configuration);
            Assert.Same(properties, context.Properties);
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
