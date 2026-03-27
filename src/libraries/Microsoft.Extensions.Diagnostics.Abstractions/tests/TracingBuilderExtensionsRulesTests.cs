// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Configuration;
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
        public void BuilderSetEnabledAddsRule(string? activitySourceName)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.SetEnabled(activitySourceName, enabled: true);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal(activitySourceName, rule.ActivitySourceName);
            Assert.Null(rule.ListenerName);
            Assert.Equal(ActivitySourceScope.Local | ActivitySourceScope.Global, rule.Scopes);
            Assert.True(rule.Enabled);
        }

        [Fact]
        public void BuilderSetEnabledWithAllParamsAddsRule()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.SetEnabled("source", "listener", ActivitySourceScope.Local, enabled: false);

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
        public void OptionsSetEnabledAddsRule(string? activitySourceName)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<TracingOptions>(options =>
            options.SetEnabled(activitySourceName, enabled: true));

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<TracingOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal(activitySourceName, rule.ActivitySourceName);
            Assert.Null(rule.ListenerName);
            Assert.Equal(ActivitySourceScope.Local | ActivitySourceScope.Global, rule.Scopes);
            Assert.True(rule.Enabled);
        }

        [Fact]
        public void OptionsSetEnabledAllParamsAddsRule()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<TracingOptions>(options =>
                options.SetEnabled("source", "listener", ActivitySourceScope.Global, enabled: false));

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

            builder.Enable("source", "listener", ActivitySourceScope.Local);

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

            builder.Disable("source", "listener", ActivitySourceScope.Local);

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
