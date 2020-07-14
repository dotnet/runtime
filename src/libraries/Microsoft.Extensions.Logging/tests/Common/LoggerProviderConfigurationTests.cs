// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerProviderConfigurationTests
    {
        [Fact]
        public void ReturnsConfigurationSectionByFullName()
        {
            var serviceProvider = BuildServiceProvider(Pair("Microsoft.Extensions.Logging.Test.TestLoggerProvider:Key", "Value"));

            var providerConfiguration = serviceProvider.GetRequiredService<ILoggerProviderConfigurationFactory>();
            var configuration = providerConfiguration.GetConfiguration(typeof(TestLoggerProvider));

            Assert.Equal("Value", configuration["Key"]);
        }

        [Fact]
        public void ReturnsConfigurationSectionByAlias()
        {
            var serviceProvider = BuildServiceProvider(Pair("TestLogger:Key", "Value"));

            var providerConfiguration = serviceProvider.GetRequiredService<ILoggerProviderConfigurationFactory>();
            var configuration = providerConfiguration.GetConfiguration(typeof(TestLoggerProvider));

            Assert.Equal("Value", configuration["Key"]);
        }

        [Fact]
        public void ReturnsConfigurationSectionByFullNameGeneric()
        {
            var serviceProvider = BuildServiceProvider(Pair("Microsoft.Extensions.Logging.Test.TestLoggerProvider:Key", "Value"));

            var providerConfiguration = serviceProvider.GetRequiredService<ILoggerProviderConfiguration<TestLoggerProvider>>();

            Assert.Equal("Value", providerConfiguration.Configuration["Key"]);
        }

        [Fact]
        public void ReturnsConfigurationSectionByAliasGeneric()
        {
            var serviceProvider = BuildServiceProvider(Pair("TestLogger:Key", "Value"));

            var providerConfiguration = serviceProvider.GetRequiredService<ILoggerProviderConfiguration<TestLoggerProvider>>();

            Assert.Equal("Value", providerConfiguration.Configuration["Key"]);
        }

        [Fact]
        public void MergesSectionsPreferringAlias()
        {
            var serviceProvider = BuildServiceProvider(Pair("TestLogger:Key", "Value1"), Pair("Microsoft.Extensions.Logging.Test.TestLoggerProvider:Key", "Value2"));

            var providerConfiguration = serviceProvider.GetRequiredService<ILoggerProviderConfiguration<TestLoggerProvider>>();

            Assert.Equal("Value1", providerConfiguration.Configuration["Key"]);
        }

        [Fact]
        public void MergesConfigurationsInOrder()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(
                    builder => builder
                        .AddConfiguration(new ConfigurationBuilder().AddInMemoryCollection(new [] { Pair("TestLogger:Key", "Value1") }).Build())
                        .AddConfiguration(new ConfigurationBuilder().AddInMemoryCollection(new [] { Pair("Microsoft.Extensions.Logging.Test.TestLoggerProvider:Key", "Value2") }).Build()))
                .BuildServiceProvider();

            var providerConfiguration = serviceProvider.GetRequiredService<ILoggerProviderConfiguration<TestLoggerProvider>>();

            Assert.Equal("Value2", providerConfiguration.Configuration["Key"]);
        }

        private KeyValuePair<string, string> Pair(string key, string value) => new KeyValuePair<string, string>(key, value);

        private static ServiceProvider BuildServiceProvider(params KeyValuePair<string, string>[] values)
        {
            return new ServiceCollection()
                .AddLogging(
                    builder => builder.AddConfiguration(
                        new ConfigurationBuilder().AddInMemoryCollection(values).Build()))
                .BuildServiceProvider();
        }
    }
}
