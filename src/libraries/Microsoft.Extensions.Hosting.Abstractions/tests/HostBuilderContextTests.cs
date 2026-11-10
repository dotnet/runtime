// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

#if NET
    /// <summary>
    /// Tests the default interface method (DIM) for <see cref="IHostBuilder.UseServiceProviderFactory{TContainerBuilder}(Func{HostBuilderContext, IServiceProviderFactory{TContainerBuilder}})"/>.
    /// This DIM ensures old IHostBuilder implementations (e.g., from Microsoft.Extensions.Hosting 2.2.0.0) can be
    /// loaded without TypeLoadException on .NET even though they only implement the non-Func overload.
    /// </summary>
    public class IHostBuilderDefaultInterfaceMethodTests
    {
        [Fact]
        public void UseServiceProviderFactory_FuncOverload_WithoutOverride_ThrowsNotSupportedException()
        {
            IHostBuilder builder = new MinimalHostBuilder();
            Assert.Throws<NotSupportedException>(() => builder.UseServiceProviderFactory<IServiceCollection>(
                _ => new MinimalServiceProviderFactory()));
        }

        [Fact]
        public void UseServiceProviderFactory_FuncOverload_WithNullFactory_ThrowsArgumentNullException()
        {
            IHostBuilder builder = new MinimalHostBuilder();
            Func<HostBuilderContext, IServiceProviderFactory<IServiceCollection>>? factory = null;
            AssertExtensions.Throws<ArgumentNullException>("factory", () => builder.UseServiceProviderFactory<IServiceCollection>(factory));
        }

        [Fact]
        public void UseServiceProviderFactory_NonFuncOverload_WithoutDIM_CanBeImplementedAndCalled()
        {
            IHostBuilder builder = new MinimalHostBuilder();
            // This overload does not have a DIM and the MinimalHostBuilder provides its own implementation.
            Assert.Same(builder, builder.UseServiceProviderFactory(new MinimalServiceProviderFactory()));
        }

        /// <summary>
        /// Simulates an old IHostBuilder implementation (e.g., from v2.2) that only implements the
        /// non-Func overload of UseServiceProviderFactory. The Func overload falls back to the DIM.
        /// </summary>
        private class MinimalHostBuilder : IHostBuilder
        {
            public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

            public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate) => this;
            public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate) => this;
            public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate) => this;
            public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate) => this;
            public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull => this;
            // Note: UseServiceProviderFactory(Func<...>) is intentionally not overridden here;
            // the DIM on IHostBuilder provides the fallback implementation.
            public IHost Build() => null!;
        }

        private class MinimalServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
        {
            public IServiceCollection CreateBuilder(IServiceCollection services) => services;
            public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder) => null!;
        }
    }
#endif
}
