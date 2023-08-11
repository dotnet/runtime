// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class MetricsBuilderExtensionsRulesTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("*")]
        [InlineData("foo")]
        public void BuilderEnableMetricsAddsRule(string meterName)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.EnableMetrics(meterName);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<MetricsOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal(meterName, rule.MeterName);
            Assert.Null(rule.InstrumentName);
            Assert.Null(rule.ListenerName);
            Assert.Equal(MeterScope.Local | MeterScope.Global, rule.Scopes);
            Assert.True(rule.Enable);
        }

        [Fact]
        public void BuilderEnableMetricsWithAllParamsAddsRule()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.EnableMetrics("meter", "instance", "listener", MeterScope.Local);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<MetricsOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal("meter", rule.MeterName);
            Assert.Equal("instance", rule.InstrumentName);
            Assert.Equal("listener", rule.ListenerName);
            Assert.Equal(MeterScope.Local, rule.Scopes);
            Assert.True(rule.Enable);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("*")]
        [InlineData("foo")]
        public void OptionsEnableMetricsAddsRule(string meterName)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<MetricsOptions>(options => options.EnableMetrics(meterName));

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<MetricsOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal(meterName, rule.MeterName);
            Assert.Null(rule.InstrumentName);
            Assert.Null(rule.ListenerName);
            Assert.Equal(MeterScope.Local | MeterScope.Global, rule.Scopes);
            Assert.True(rule.Enable);
        }

        [Fact]
        public void OptionsEnableMetricsAllParamsAddsRule()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<MetricsOptions>(options
                => options.EnableMetrics("meter", "instrument", "listener", MeterScope.Global));

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<MetricsOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal("meter", rule.MeterName);
            Assert.Equal("instrument", rule.InstrumentName);
            Assert.Equal("listener", rule.ListenerName);
            Assert.Equal(MeterScope.Global, rule.Scopes);
            Assert.True(rule.Enable);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("*")]
        [InlineData("foo")]
        public void BuilderDisableMetricsAddsRule(string meterName)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.DisableMetrics(meterName);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<MetricsOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal(meterName, rule.MeterName);
            Assert.Null(rule.InstrumentName);
            Assert.Null(rule.ListenerName);
            Assert.Equal(MeterScope.Local | MeterScope.Global, rule.Scopes);
            Assert.False(rule.Enable);
        }

        [Fact]
        public void BuilderDisableMetricsWithAllParamsAddsRule()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            var builder = new FakeBuilder(services);

            builder.DisableMetrics("meter", "instance", "listener", MeterScope.Local);

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<MetricsOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal("meter", rule.MeterName);
            Assert.Equal("instance", rule.InstrumentName);
            Assert.Equal("listener", rule.ListenerName);
            Assert.Equal(MeterScope.Local, rule.Scopes);
            Assert.False(rule.Enable);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("*")]
        [InlineData("foo")]
        public void OptionsDisableMetricsAddsRule(string meterName)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<MetricsOptions>(options => options.DisableMetrics(meterName));

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<MetricsOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal(meterName, rule.MeterName);
            Assert.Null(rule.InstrumentName);
            Assert.Null(rule.ListenerName);
            Assert.Equal(MeterScope.Local | MeterScope.Global, rule.Scopes);
            Assert.False(rule.Enable);
        }

        [Fact]
        public void OptionsDisableMetricsAllParamsAddsRule()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<MetricsOptions>(options
                => options.DisableMetrics("meter", "instrument", "listener", MeterScope.Global));

            var container = services.BuildServiceProvider();
            var options = container.GetRequiredService<IOptions<MetricsOptions>>();
            var instance = options.Value;
            var rule = Assert.Single(instance.Rules);
            Assert.Equal("meter", rule.MeterName);
            Assert.Equal("instrument", rule.InstrumentName);
            Assert.Equal("listener", rule.ListenerName);
            Assert.Equal(MeterScope.Global, rule.Scopes);
            Assert.False(rule.Enable);
        }

        private class FakeBuilder(IServiceCollection services) : IMetricsBuilder
        {
            public IServiceCollection Services { get; } = services;
        }
    }
}
