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
        public void BuilderEnableAddsRule(string? activitySourceName)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.EnableTracing(activitySourceName: activitySourceName);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal(activitySourceName, rule.ActivitySourceName);
            Assert.Null(rule.ListenerName);
            Assert.Equal(ActivitySourceScope.Global | ActivitySourceScope.Local, rule.Scopes);
            Assert.True(rule.Enabled);
        }

        [Fact]
        public void BuilderDisableWithAllParamsAddsRule()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.DisableTracing(activitySourceName: "source", listenerName: "listener", scopes: ActivitySourceScope.Local);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal("source", rule.ActivitySourceName);
            Assert.Equal("listener", rule.ListenerName);
            Assert.Equal(ActivitySourceScope.Local, rule.Scopes);
            Assert.False(rule.Enabled);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("*")]
        [InlineData("foo")]
        public void OptionsEnableAddsRule(string? activitySourceName)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<TracingOptions>(options =>
                options.EnableTracing(activitySourceName: activitySourceName));

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal(activitySourceName, rule.ActivitySourceName);
            Assert.Null(rule.ListenerName);
            Assert.Equal(ActivitySourceScope.Global | ActivitySourceScope.Local, rule.Scopes);
            Assert.True(rule.Enabled);
        }

        [Fact]
        public void OptionsDisableAllParamsAddsRule()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<TracingOptions>(options =>
                options.DisableTracing(activitySourceName: "source", listenerName: "listener", scopes: ActivitySourceScope.Global));

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal("source", rule.ActivitySourceName);
            Assert.Equal("listener", rule.ListenerName);
            Assert.Equal(ActivitySourceScope.Global, rule.Scopes);
            Assert.False(rule.Enabled);
        }

        [Fact]
        public void EnableTracingUsesTrue()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.EnableTracing("source", activityName: null, "listener", ActivitySourceScope.Local);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.True(rule.Enabled);
        }

        [Fact]
        public void DisableTracingUsesFalse()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.DisableTracing("source", activityName: null, "listener", ActivitySourceScope.Local);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.False(rule.Enabled);
        }

        private class FakeBuilder(IServiceCollection services) : ITracingBuilder
        {
            public IServiceCollection Services { get; } = services;
        }
    }
}
