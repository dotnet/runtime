// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Tracing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class TracingBuilderExtensionsRulesTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("*")]
        [InlineData("foo")]
        public void BuilderEnableAddsRule(string? sourceName)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.EnableTracing(sourceName: sourceName);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal(sourceName, rule.SourceName);
            Assert.Null(rule.ListenerName);
            Assert.Equal(ActivitySourceScopes.Global | ActivitySourceScopes.Local, rule.Scopes);
            Assert.True(rule.Enable);
        }

        [Fact]
        public void BuilderDisableWithAllParamsAddsRule()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.DisableTracing(sourceName: "source", listenerName: "listener", scopes: ActivitySourceScopes.Local);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal("source", rule.SourceName);
            Assert.Equal("listener", rule.ListenerName);
            Assert.Equal(ActivitySourceScopes.Local, rule.Scopes);
            Assert.False(rule.Enable);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("*")]
        [InlineData("foo")]
        public void OptionsEnableAddsRule(string? sourceName)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<TracingOptions>(options =>
                options.EnableTracing(sourceName: sourceName));

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal(sourceName, rule.SourceName);
            Assert.Null(rule.ListenerName);
            Assert.Equal(ActivitySourceScopes.Global | ActivitySourceScopes.Local, rule.Scopes);
            Assert.True(rule.Enable);
        }

        [Fact]
        public void OptionsDisableAllParamsAddsRule()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<TracingOptions>(options =>
                options.DisableTracing(sourceName: "source", listenerName: "listener", scopes: ActivitySourceScopes.Global));

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal("source", rule.SourceName);
            Assert.Equal("listener", rule.ListenerName);
            Assert.Equal(ActivitySourceScopes.Global, rule.Scopes);
            Assert.False(rule.Enable);
        }

        [Fact]
        public void EnableTracingUsesTrue()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.EnableTracing("source", operationName: null, "listener", ActivitySourceScopes.Local);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.True(rule.Enable);
        }

        [Fact]
        public void DisableTracingUsesFalse()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.DisableTracing("source", operationName: null, "listener", ActivitySourceScopes.Local);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.False(rule.Enable);
        }

        private class FakeBuilder(IServiceCollection services) : ITracingBuilder
        {
            public IServiceCollection Services { get; } = services;
        }
    }
}
