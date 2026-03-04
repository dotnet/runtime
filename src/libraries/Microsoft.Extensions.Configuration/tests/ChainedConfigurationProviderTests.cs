// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ChainedConfigurationProvierTests
    {
        [Fact]
        public void ChainedConfiguration_UsingMemoryConfigurationSource_ChainedCouldExposeProvider()
        {
            var chainedConfigurationProvider = new ChainedConfigurationSource
                {
                    Configuration = new ConfigurationBuilder()
                            .Add(new MemoryConfigurationSource {
                                InitialData = new Dictionary<string, string>() { { "a:b", "c" } }
                            })
                            .Build(),
                    ShouldDisposeConfiguration = false,
                }
                .Build(new ConfigurationBuilder()) as ChainedConfigurationProvider;

            Assert.True(chainedConfigurationProvider.TryGet("a:b", out string? value));
            Assert.Equal("c", value);
            Assert.Equal("c", chainedConfigurationProvider.Configuration["a:b"]);

            var configRoot = chainedConfigurationProvider.Configuration as IConfigurationRoot;
            Assert.NotNull(configRoot);
            Assert.Equal(1, configRoot.Providers.Count());
            Assert.IsType<MemoryConfigurationProvider>(configRoot.Providers.First());
        }

        [Fact]
        public void ChainedConfiguration_ExposesProvider()
        {
            var providers = new IConfigurationProvider[] {
                new TestConfigurationProvider("foo", "foo-value")
            };
            var chainedConfigurationSource = new ChainedConfigurationSource
            {
                Configuration = new ConfigurationRoot(providers),
                ShouldDisposeConfiguration = false,
            };

            var chainedConfigurationProvider = chainedConfigurationSource
                .Build(new ConfigurationBuilder()) as ChainedConfigurationProvider;

            var configRoot = chainedConfigurationProvider.Configuration as IConfigurationRoot;
            Assert.NotNull(configRoot);
            Assert.Equal(providers, configRoot.Providers);
        }

        [Fact]
        public void ChainedConfiguration_ReloadPropagatesToInnerConfigurationRoot()
        {
            var innerConfig = new ConfigurationBuilder()
                .Add(new RandomValueConfigurationSource())
                .Build();

            var outerConfig = new ConfigurationBuilder()
                .AddConfiguration(innerConfig)
                .Build();

            string? valueBefore = outerConfig["Random"];
            Assert.NotNull(valueBefore);

            outerConfig.Reload();

            string? valueAfter = outerConfig["Random"];
            Assert.NotNull(valueAfter);
            Assert.NotEqual(valueBefore, valueAfter);
        }

        [Fact]
        public void ChainedConfiguration_ReloadDoesNotPropagateToInnerConfigurationSection()
        {
            var innerConfig = new ConfigurationBuilder()
                .Add(new RandomValueConfigurationSource("Section:Random"))
                .Build();

            var outerConfig = new ConfigurationBuilder()
                .AddConfiguration(innerConfig.GetSection("Section"))
                .Build();

            string? valueBefore = outerConfig["Random"];
            Assert.NotNull(valueBefore);

            outerConfig.Reload();

            string? valueAfter = outerConfig["Random"];
            Assert.NotNull(valueAfter);
            Assert.Equal(valueBefore, valueAfter);
        }

        private class TestConfigurationProvider : ConfigurationProvider
        {
            public TestConfigurationProvider(string key, string value)
                => Data.Add(key, value);
        }

        private class RandomValueConfigurationSource : IConfigurationSource
        {
            private readonly string _key;

            public RandomValueConfigurationSource(string key = "Random")
                => _key = key;

            public IConfigurationProvider Build(IConfigurationBuilder builder)
                => new RandomValueConfigurationProvider(_key);
        }

        private class RandomValueConfigurationProvider : ConfigurationProvider
        {
            private readonly string _key;

            public RandomValueConfigurationProvider(string key)
                => _key = key;

            public override void Load()
                => Data[_key] = Guid.NewGuid().ToString();
        }
    }
}
